using System;
using System.Collections.Generic;
using System.Management;

namespace BackupDotNet.Core.Inventory;

/// <summary>
/// Сеть хоста: виртуальные коммутаторы, физические адаптеры и VLAN на портах ВМ.
///
/// Только чтение и изменение VLAN у существующего порта. Создание коммутаторов
/// и привязка к физическому адаптеру сознательно не автоматизированы: ошибка
/// в привязке рвёт сеть хоста, а через него — доступ к самому агенту. Такое
/// лучше делать руками, глядя на консоль, а не кнопкой из браузера.
/// </summary>
public static class NetworkInventory
{
	private const string Ns = "root\\virtualization\\v2";

	/// <summary>GUID функции VLAN в Hyper-V: часть InstanceID настроек порта.</summary>
	private const string VlanFeatureId = "952C5004-4465-451C-8CB8-FA9AB382B773";

	/// <summary>OperationMode для режима Access (проверено на живой ВМ).</summary>
	private const uint ModeAccess = 1;

	/// <summary>Виртуальный коммутатор.</summary>
	public class Switch
	{
		public string Name = "";

		/// <summary>External, Internal или Private — по типу привязки.</summary>
		public string Kind = "";

		public string Adapter = "";

		public int VmCount;

		public string State = "";

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["name"] = Name;
			d["kind"] = Kind;
			d["adapter"] = Adapter;
			d["vmCount"] = VmCount;
			d["state"] = State;
			return d;
		}
	}

	/// <summary>Сетевой адаптер ВМ вместе с VLAN.</summary>
	public class VmAdapter
	{
		public string VmName = "";

		public string AdapterName = "";

		public string SwitchName = "";

		public string Mac = "";

		/// <summary>Режим: Access, Trunk, Private или «не задан».</summary>
		public string Mode = "";

		/// <summary>VLAN для режима Access (0 — без тега).</summary>
		public int VlanId = -1;

		public List<int> TrunkVlans = new List<int>();

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["vmName"] = VmName;
			d["adapterName"] = AdapterName;
			d["switchName"] = SwitchName;
			d["mac"] = Mac;
			d["mode"] = Mode;
			if (VlanId >= 0)
			{
				d["vlanId"] = VlanId;
			}
			if (TrunkVlans.Count > 0)
			{
				d["trunkVlans"] = TrunkVlans;
			}
			return d;
		}
	}

	// ------------------------------------------------------------------ чтение

	/// <summary>Виртуальные коммутаторы с типом и привязанным адаптером.</summary>
	public static List<Switch> Switches(out string error)
	{
		error = "";
		List<Switch> list = new List<Switch>();
		try
		{
			// Тип коммутатора определяется наличием привязки к физическому
			// адаптеру: без неё это Internal (если есть внутренний порт) или
			// Private. Различаем по Msvm_ExternalEthernetPort.
			Dictionary<string, string> externalBindings = ExternalBindings();

			Dictionary<string, int> vmCounts = VmCountsBySwitch();

			ManagementObjectCollection coll = Hw.Query(Ns, "SELECT * FROM Msvm_VirtualEthernetSwitch");
			if (coll == null)
			{
				error = "Hyper-V недоступен: не удалось прочитать коммутаторы";
				return list;
			}
			try
			{
				foreach (ManagementBaseObject o in coll)
				{
					using (o)
					{
						Switch sw = new Switch();
						sw.Name = Hw.Prop(o, "ElementName");
						sw.State = Hw.PropInt(o, "EnabledState", -1) == 2 ? "включён" : "выключен";

						string guid = Hw.Prop(o, "Name");
						if (externalBindings.TryGetValue(guid, out string adapter))
						{
							sw.Kind = "External";
							sw.Adapter = adapter;
						}
						else
						{
							// Внутренний порт хоста есть только у Internal.
							sw.Kind = HasHostPort(guid) ? "Internal" : "Private";
						}

						vmCounts.TryGetValue(sw.Name, out int n);
						sw.VmCount = n;
						list.Add(sw);
					}
				}
			}
			finally
			{
				try
				{
					coll.Dispose();
				}
				catch
				{
				}
			}
			list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
		}
		catch (Exception ex)
		{
			error = "Hyper-V недоступен: " + ex.Message;
		}
		return list;
	}

	/// <summary>Сетевые адаптеры всех ВМ с VLAN-настройками.</summary>
	public static List<VmAdapter> VmAdapters(out string error)
	{
		error = "";
		List<VmAdapter> list = new List<VmAdapter>();
		try
		{
			// Сначала собираем порты: InstanceID порта = «Microsoft:<guid ВМ>\<guid порта>».
			Dictionary<string, string> vmByGuid = VmNamesByGuid();
			Dictionary<string, string> switchByPort = SwitchByPort();

			ManagementObjectCollection coll = Hw.Query(Ns, "SELECT * FROM Msvm_SyntheticEthernetPortSettingData");
			if (coll == null)
			{
				error = "Hyper-V недоступен: не удалось прочитать сетевые адаптеры";
				return list;
			}
			try
			{
				foreach (ManagementBaseObject o in coll)
				{
					using (o)
					{
						string id = Hw.Prop(o, "InstanceID");
						if (id.Length == 0 || id.IndexOf("Microsoft:", StringComparison.OrdinalIgnoreCase) != 0)
						{
							continue;
						}
						// Определения (Microsoft:Definition\...) — не адаптеры ВМ.
						if (id.IndexOf("Definition", StringComparison.OrdinalIgnoreCase) >= 0)
						{
							continue;
						}

						string[] parts = id.Split('\\');
						if (parts.Length < 2)
						{
							continue;
						}
						string guid = parts[0].Substring("Microsoft:".Length);

						VmAdapter a = new VmAdapter();
						vmByGuid.TryGetValue(guid, out string vmName);
						a.VmName = vmName ?? "";
						a.AdapterName = Hw.Prop(o, "ElementName");
						a.Mac = Hw.Prop(o, "Address");
						switchByPort.TryGetValue(id, out string sw);
						a.SwitchName = sw ?? "";

						ReadVlan(id, a);
						list.Add(a);
					}
				}
			}
			finally
			{
				try
				{
					coll.Dispose();
				}
				catch
				{
				}
			}
			list.Sort((x, y) =>
			{
				int c = string.Compare(x.VmName, y.VmName, StringComparison.OrdinalIgnoreCase);
				return c != 0 ? c : string.Compare(x.AdapterName, y.AdapterName, StringComparison.OrdinalIgnoreCase);
			});
		}
		catch (Exception ex)
		{
			error = "Hyper-V недоступен: " + ex.Message;
		}
		return list;
	}

	/// <summary>
	/// Читает VLAN-настройки порта. Отсутствие записи — это не ошибка:
	/// у порта без VLAN Hyper-V просто не создаёт настроек.
	/// </summary>
	private static void ReadVlan(string portInstanceId, VmAdapter a)
	{
		a.Mode = "не задан";
		a.VlanId = -1;
		try
		{
			ManagementObjectCollection coll = Hw.Query(Ns,
				"SELECT * FROM Msvm_EthernetSwitchPortVlanSettingData WHERE InstanceID LIKE '%" +
				portInstanceId.Replace("\\", "\\\\") + "%'");
			if (coll == null)
			{
				return;
			}
			try
			{
				foreach (ManagementBaseObject o in coll)
				{
					using (o)
					{
						int mode = Hw.PropInt(o, "OperationMode", 0);
						int access = Hw.PropInt(o, "AccessVlanId", 0);
						int native = Hw.PropInt(o, "NativeVlanId", 0);

						// Значения проверены на живой ВМ: 1 — Access, 2 — Trunk.
						// «Без тега» — это отсутствие записи, а не нулевой режим.
						switch (mode)
						{
						case 1:
							a.Mode = "Access";
							a.VlanId = access;
							break;
						case 2:
							a.Mode = "Trunk";
							a.VlanId = native;
							foreach (string t in Hw.PropStrings(o, "TrunkVlanIdArray"))
							{
								if (int.TryParse(t, out int v))
								{
									a.TrunkVlans.Add(v);
								}
							}
							break;
						case 3:
							a.Mode = "Private";
							break;
						default:
							a.Mode = "режим " + mode;
							break;
						}
						return;
					}
				}
			}
			finally
			{
				try
				{
					coll.Dispose();
				}
				catch
				{
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("VLAN порта " + portInstanceId + ": " + ex.Message);
		}
	}

	// ------------------------------------------------------------------ изменение VLAN

	/// <summary>
	/// Ставит VLAN на адаптер ВМ (режим Access).
	/// </summary>
	/// <param name="vmName">Имя виртуальной машины.</param>
	/// <param name="adapterName">Имя адаптера; пусто — первый найденный.</param>
	/// <param name="vlanId">VLAN 1..4094; 0 — снять тег.</param>
	/// <param name="confirmed">
	/// Человек подтвердил, что понимает риск потерять связь с машиной.
	/// Без этого меняем только там, где риск нулевой.
	/// </param>
	public static VmControl.Result SetVlan(string vmName, string adapterName, int vlanId, bool confirmed)
	{
		VmControl.Result r = new VmControl.Result();
		r.Destructive = true;

		if (string.IsNullOrWhiteSpace(vmName))
		{
			r.Message = "не указано имя ВМ";
			return r;
		}
		if (vlanId < 0 || vlanId > 4094)
		{
			r.Message = "VLAN должен быть от 0 до 4094";
			return r;
		}
		// 1 — служебный VLAN по умолчанию: тегировать им нельзя.
		if (vlanId == 1)
		{
			r.Message = "VLAN 1 — служебный, используйте 0, чтобы снять тег";
			return r;
		}

		string name = vmName.Trim();

		// Главный риск: смена VLAN рвёт связь с машиной. Если это машина,
		// в которой работает сам агент, — отрежем себе управление.
		string selfBlock = CheckSelfLockout(name);
		if (selfBlock.Length > 0)
		{
			r.Message = selfBlock;
			r.Protected = true;
			return r;
		}

		try
		{
			string guid = VmGuid(name);
			if (guid.Length == 0)
			{
				r.Message = "виртуальная машина не найдена: " + name;
				return r;
			}

			string portId = FindPortId(guid, adapterName);
			if (portId.Length == 0)
			{
				r.Message = "сетевой адаптер не найден" +
					(string.IsNullOrWhiteSpace(adapterName) ? "" : ": " + adapterName);
				return r;
			}

			// Работающая машина с одним адаптером после смены VLAN может
			// остаться без сети — предупреждаем до, а не после.
			if (!confirmed)
			{
				string risk = DescribeRisk(name, guid, portId);
				if (risk.Length > 0)
				{
					r.Message = risk;
					r.Protected = true;
					return r;
				}
			}

			string error = ApplyVlan(name, vlanId);
			if (error.Length > 0)
			{
				r.Message = "VLAN не изменён: " + error;
				return r;
			}

			r.Ok = true;
			r.Message = vlanId == 0
				? "тег VLAN снят"
				: "VLAN " + vlanId + " установлен";
			Log.Info("ВМ " + name + ": " + r.Message + " (порт " + portId + ")");
			return r;
		}
		catch (Exception ex)
		{
			r.Message = "VLAN не изменён: " + ex.Message;
			Log.Warn("ВМ " + name + ", VLAN: " + ex.Message);
			return r;
		}
	}

	/// <summary>
	/// Агент работает внутри этой ВМ? Тогда менять ей VLAN нельзя никак:
	/// после смены мы потеряем и её, и себя.
	/// </summary>
	private static string CheckSelfLockout(string vmName)
	{
		try
		{
			// Имя хоста, на котором работает агент.
			string self = Environment.MachineName;
			if (!string.Equals(self, vmName, StringComparison.OrdinalIgnoreCase))
			{
				return "";
			}

			// Совпадение имени ещё не значит «мы внутри»: хост Hyper-V может
			// называться так же, как ВМ. Проверяем, есть ли вообще такая ВМ.
			if (VmGuid(vmName).Length == 0)
			{
				return "";
			}

			// Если имя совпало и ВМ существует — почти наверняка агент в ней.
			return "ВМ «" + vmName + "» совпадает с именем хоста агента. " +
				"Смена VLAN отрежет управление этой машиной — сделайте это из консоли Hyper-V.";
		}
		catch
		{
			return "";
		}
	}

	/// <summary>
	/// Объясняет, чем рискует конкретная машина. Пустая строка — риска нет.
	/// </summary>
	private static string DescribeRisk(string vmName, string guid, string portId)
	{
		try
		{
			// Сколько адаптеров у машины: с двумя смена VLAN на одном
			// связь не рвёт.
			int ports = 0;
			ManagementObjectCollection coll = Hw.Query(Ns, "SELECT * FROM Msvm_SyntheticEthernetPortSettingData");
			if (coll != null)
			{
				try
				{
					foreach (ManagementBaseObject o in coll)
					{
						using (o)
						{
							if (Hw.Prop(o, "InstanceID").StartsWith("Microsoft:" + guid + "\\",
								StringComparison.OrdinalIgnoreCase))
							{
								ports++;
							}
						}
					}
				}
				finally
				{
					try
					{
						coll.Dispose();
					}
					catch
					{
					}
				}
			}

			// Работает ли машина: у выключенной смена VLAN безопасна.
			bool running = false;
			ManagementObjectCollection vms = Hw.Query(Ns, "SELECT * FROM Msvm_ComputerSystem WHERE Name='" + guid + "'");
			if (vms != null)
			{
				try
				{
					foreach (ManagementBaseObject o in vms)
					{
						using (o)
						{
							running = Hw.PropInt(o, "EnabledState", -1) == 2;
						}
					}
				}
				finally
				{
					try
					{
						vms.Dispose();
					}
					catch
					{
					}
				}
			}

			if (!running)
			{
				return "";
			}
			if (ports <= 1)
			{
				return "ВМ «" + vmName + "» работает, и это её единственный сетевой адаптер. " +
					"Смена VLAN может оборвать связь с ней. Подтвердите действие, если это ожидаемо.";
			}
			return "";
		}
		catch
		{
			return "";
		}
	}

	private static string ApplyVlan(string vmName, int vlanId)
	{
		// Почему командлет, а не WMI: ModifyFeatureSettings для VLAN отвечает
		// «ошибка не относится к параметрам настройки виртуального коммутатора
		// Ethernet» на любой XML — и с путём порта, и с путём конфигурации.
		// Set-VMNetworkAdapterVlan делает то же самое с первого раза.
		// System.Management.Automation берётся из GAC, сторонних пакетов нет.
		string script = vlanId == 0
			? "Set-VMNetworkAdapterVlan -VMName " + PsBridge.Quote(vmName) + " -Untagged -ErrorAction Stop"
			: "Set-VMNetworkAdapterVlan -VMName " + PsBridge.Quote(vmName) +
				" -Access -VlanId " + vlanId.ToString(System.Globalization.CultureInfo.InvariantCulture) +
				" -ErrorAction Stop";

		PsBridge.Result res = PsBridge.Run(script, 90, "Hyper-V");
		if (!res.Ok)
		{
			return ExplainPsError(res.Error);
		}
		return "";
	}

	/// <summary>Переводит типичные ошибки командлета в понятный текст.</summary>
	private static string ExplainPsError(string error)
	{
		string msg = error ?? "";
		if (msg.IndexOf("was not found", StringComparison.OrdinalIgnoreCase) >= 0 ||
			msg.IndexOf("не найден", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "виртуальная машина или адаптер не найдены";
		}
		if (msg.IndexOf("Hyper-V", StringComparison.OrdinalIgnoreCase) >= 0 &&
			msg.IndexOf("module", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "модуль Hyper-V недоступен в PowerShell";
		}
		return msg;
	}

	// ------------------------------------------------------------------ помощники

	private static Dictionary<string, string> ExternalBindings()
	{
		Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			// Имена физических адаптеров по DeviceID.
			Dictionary<string, string> adapterNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			ManagementObjectCollection ext = Hw.Query(Ns, "SELECT * FROM Msvm_ExternalEthernetPort");
			if (ext != null)
			{
				try
				{
					foreach (ManagementBaseObject o in ext)
					{
						using (o)
						{
							string deviceId = Hw.Prop(o, "DeviceID");
							string adapterName = Hw.Prop(o, "Name");
							if (deviceId.Length > 0)
							{
								adapterNames[deviceId] = adapterName.Length > 0 ? adapterName : deviceId;
							}
						}
					}
				}
				finally
				{
					try
					{
						ext.Dispose();
					}
					catch
					{
					}
				}
			}

			// Порты коммутатора: InstanceID = «Microsoft:<guid коммутатора>\<guid порта>»,
			// Parent пуст, а HostResource указывает либо на физический адаптер,
			// либо на сам компьютер (внутренний порт).
			ManagementObjectCollection alloc = Hw.Query(Ns, "SELECT * FROM Msvm_EthernetPortAllocationSettingData");
			if (alloc == null)
			{
				return map;
			}
			try
			{
				foreach (ManagementBaseObject o in alloc)
				{
					using (o)
					{
						if (Hw.Prop(o, "Parent").Length > 0)
						{
							// Порт ВМ, а не коммутатора.
							continue;
						}
						string id = Hw.Prop(o, "InstanceID");
						string host = First(Hw.PropStrings(o, "HostResource"));
						if (id.Length == 0 || host.Length == 0)
						{
							continue;
						}
						if (host.IndexOf("Msvm_ExternalEthernetPort", StringComparison.OrdinalIgnoreCase) < 0)
						{
							continue;
						}
						// Коммутатор — из InstanceID порта.
						string[] parts = id.Split('\\');
						if (parts.Length < 1)
						{
							continue;
						}
						string swGuid = parts[0].Substring(parts[0].LastIndexOf(':') + 1);

						string deviceId = DeviceIdOf(host);
						adapterNames.TryGetValue(deviceId, out string adapterName);
						map[swGuid] = adapterName ?? deviceId;
					}
				}
			}
			finally
			{
				try
				{
					alloc.Dispose();
				}
				catch
				{
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Привязки коммутаторов: " + ex.Message);
		}
		return map;
	}

	private static bool HasHostPort(string switchGuid)
	{
		try
		{
			ManagementObjectCollection coll = Hw.Query(Ns, "SELECT * FROM Msvm_EthernetPortAllocationSettingData");
			if (coll == null)
			{
				return false;
			}
			try
			{
				foreach (ManagementBaseObject o in coll)
				{
					using (o)
					{
						// Только порты коммутатора: у портов ВМ Parent не пуст.
						if (Hw.Prop(o, "Parent").Length > 0)
						{
							continue;
						}
						string id = Hw.Prop(o, "InstanceID");
						if (id.IndexOf(switchGuid, StringComparison.OrdinalIgnoreCase) < 0)
						{
							continue;
						}
						string host = First(Hw.PropStrings(o, "HostResource"));
						// Внутренний порт хоста ссылается на сам компьютер.
						if (host.IndexOf("Msvm_ComputerSystem", StringComparison.OrdinalIgnoreCase) >= 0)
						{
							return true;
						}
					}
				}
			}
			finally
			{
				try
				{
					coll.Dispose();
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static Dictionary<string, int> VmCountsBySwitch()
	{
		Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		try
		{
			foreach (VmAdapter a in VmAdapters(out string error))
			{
				if (a.SwitchName.Length == 0)
				{
					continue;
				}
				counts.TryGetValue(a.SwitchName, out int n);
				counts[a.SwitchName] = n + 1;
			}
		}
		catch
		{
		}
		return counts;
	}

	private static Dictionary<string, string> VmNamesByGuid()
	{
		Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		ManagementObjectCollection coll = Hw.Query(Ns, "SELECT * FROM Msvm_ComputerSystem");
		if (coll == null)
		{
			return map;
		}
		try
		{
			foreach (ManagementBaseObject o in coll)
			{
				using (o)
				{
					string guid = Hw.Prop(o, "Name");
					string name = Hw.Prop(o, "ElementName");
					if (guid.Length > 0 && name.Length > 0)
					{
						map[guid] = name;
					}
				}
			}
		}
		finally
		{
			try
			{
				coll.Dispose();
			}
			catch
			{
			}
		}
		return map;
	}

	/// <summary>Коммутатор для каждого порта: порт → имя коммутатора.</summary>
	private static Dictionary<string, string> SwitchByPort()
	{
		Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			// Порты привязки: Parent = порт ВМ, HostResource = коммутатор.
			ManagementObjectCollection coll = Hw.Query(Ns,
				"SELECT * FROM Msvm_EthernetPortAllocationSettingData");
			if (coll == null)
			{
				return map;
			}
			try
			{
				Dictionary<string, string> switchNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				ManagementObjectCollection switches = Hw.Query(Ns, "SELECT * FROM Msvm_VirtualEthernetSwitch");
				if (switches != null)
				{
					try
					{
						foreach (ManagementBaseObject o in switches)
						{
							using (o)
							{
								string guid = Hw.Prop(o, "Name");
								string name = Hw.Prop(o, "ElementName");
								if (guid.Length > 0)
								{
									switchNames[guid] = name;
								}
							}
						}
					}
					finally
					{
						try
						{
							switches.Dispose();
						}
						catch
						{
						}
					}
				}

				foreach (ManagementBaseObject o in coll)
				{
					using (o)
					{
						string parent = Hw.Prop(o, "Parent");
						string host = First(Hw.PropStrings(o, "HostResource"));
						if (parent.Length == 0 || host.Length == 0)
						{
							continue;
						}
						// Parent — порт ВМ (InstanceID), HostResource — коммутатор (Name).
						string portId = InstanceIdOf(parent);
						string swGuid = NameOf(host);
						if (portId.Length == 0 || swGuid.Length == 0)
						{
							continue;
						}
						switchNames.TryGetValue(swGuid, out string swName);
						map[portId] = swName ?? swGuid;
					}
				}
			}
			finally
			{
				try
				{
					coll.Dispose();
				}
				catch
				{
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Коммутаторы портов: " + ex.Message);
		}
		return map;
	}

	/// <summary>Достаёт значение InstanceID из строки-ссылки WMI.</summary>
	private static string InstanceIdOf(string path)
	{
		return QuotedValue(path, "InstanceID");
	}

	/// <summary>Достаёт значение Name из строки-ссылки WMI (коммутатор, компьютер).</summary>
	private static string NameOf(string path)
	{
		return QuotedValue(path, "Name");
	}

	/// <summary>Достаёт значение DeviceID из строки-ссылки WMI (физический адаптер).</summary>
	private static string DeviceIdOf(string path)
	{
		return QuotedValue(path, "DeviceID");
	}

	/// <summary>Значение свойства из строки-ссылки вида <c>Класс.Свойство="значение"</c>.</summary>
	private static string QuotedValue(string path, string property)
	{
		if (string.IsNullOrEmpty(path))
		{
			return "";
		}
		string needle = property + "=\"";
		int from = 0;
		while (from < path.Length)
		{
			int idx = path.IndexOf(needle, from, StringComparison.OrdinalIgnoreCase);
			if (idx < 0)
			{
				return "";
			}
			// «Name="» встречается внутри «CreationClassName="» и «SystemName="»,
			// «DeviceID="» — внутри «SystemDeviceID="». Свойство начинается там,
			// где перед ним не буква и не цифра.
			bool boundary = idx == 0 || !char.IsLetterOrDigit(path[idx - 1]) && path[idx - 1] != '_';
			if (boundary)
			{
				string rest = path.Substring(idx + needle.Length);
				int end = rest.IndexOf('"');
				if (end < 0)
				{
					return "";
				}
				// В WMI InstanceID внутри ссылки экранирован двойным обратным слэшем.
				return rest.Substring(0, end).Replace("\\\\", "\\");
			}
			from = idx + needle.Length;
		}
		return "";
	}

	private static string VmGuid(string vmName)
	{
		ManagementObjectCollection coll = Hw.Query(Ns, "SELECT * FROM Msvm_ComputerSystem");
		if (coll == null)
		{
			return "";
		}
		try
		{
			foreach (ManagementBaseObject o in coll)
			{
				using (o)
				{
					if (string.Equals(Hw.Prop(o, "ElementName"), vmName, StringComparison.OrdinalIgnoreCase))
					{
						return Hw.Prop(o, "Name");
					}
				}
			}
		}
		finally
		{
			try
			{
				coll.Dispose();
			}
			catch
			{
			}
		}
		return "";
	}

	private static string FindPortId(string guid, string adapterName)
	{
		ManagementObjectCollection coll = Hw.Query(Ns, "SELECT * FROM Msvm_SyntheticEthernetPortSettingData");
		if (coll == null)
		{
			return "";
		}
		try
		{
			foreach (ManagementBaseObject o in coll)
			{
				using (o)
				{
					string id = Hw.Prop(o, "InstanceID");
					if (!id.StartsWith("Microsoft:" + guid + "\\", StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					if (!string.IsNullOrWhiteSpace(adapterName) &&
						!string.Equals(Hw.Prop(o, "ElementName"), adapterName.Trim(), StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					return id;
				}
			}
		}
		finally
		{
			try
			{
				coll.Dispose();
			}
			catch
			{
			}
		}
		return "";
	}

	private static ManagementObject FindOne(string wql)
	{
		ManagementObjectCollection coll = Hw.Query(Ns, wql);
		if (coll == null)
		{
			return null;
		}
		try
		{
			foreach (ManagementBaseObject o in coll)
			{
				if (o is ManagementObject mo)
				{
					return mo;
				}
			}
		}
		catch
		{
		}
		try
		{
			coll.Dispose();
		}
		catch
		{
		}
		return null;
	}

	private static string WaitJob(string jobPath, int timeoutSeconds)
	{
		if (string.IsNullOrEmpty(jobPath))
		{
			return "";
		}
		DateTime deadline = DateTime.Now.AddSeconds(Math.Max(5, timeoutSeconds));
		while (DateTime.Now < deadline)
		{
			try
			{
				using ManagementObject job = new ManagementObject(new ManagementPath(jobPath));
				job.Get();
				int state = Hw.PropInt(job, "JobState", -1);
				if (state == 2 || state == 7)
				{
					return "";
				}
				if (state == 8 || state == 9 || state == 10)
				{
					string err = Hw.Prop(job, "ErrorDescription");
					return err.Length > 0 ? err : ("задание завершилось с состоянием " + state);
				}
			}
			catch
			{
				return "";
			}
			System.Threading.Thread.Sleep(500);
		}
		return "превышено время ожидания (" + timeoutSeconds + " с)";
	}

	private static string First(List<string> list)
	{
		return list != null && list.Count > 0 ? (list[0] ?? "") : "";
	}
}
