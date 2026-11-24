using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management;
using System.Threading;

namespace BackupDotNet.Core.Inventory;

/// <summary>
/// Создание виртуальных машин Hyper-V через WMI.
///
/// Порядок операций продиктован Hyper-V, а не удобством: процессор и память
/// существуют у любой ВМ с момента создания, поэтому их нельзя «добавить» —
/// только изменить. Диск и сеть, наоборот, добавляются. Переключатель
/// привязывается не к сетевому порту, а отдельной записью
/// <c>Msvm_EthernetPortAllocationSettingData</c> — иначе адаптер создаётся,
/// но остаётся ни к чему не подключённым.
/// </summary>
public static class VmCreator
{
	private const string Namespace = "root\\virtualization\\v2";

	private const ushort TypeProcessor = 3;

	private const ushort TypeScsiController = 6;

	private const ushort TypeEthernetPort = 10;

	private const ushort TypeDiskDrive = 17;

	private const ushort TypeStorage = 31;

	private const ushort TypeEthernetConnection = 33;

	/// <summary>Параметры создаваемой машины.</summary>
	public class Spec
	{
		public string Name = "";

		public int Generation = 2;

		public int Cpu = 2;

		public int MemoryMb = 2048;

		public bool DynamicMemory;

		public int MemoryMinMb;

		public int MemoryMaxMb;

		public int DiskGb = 40;

		public string DiskPath = "";

		public string SwitchName = "";

		public string IsoPath = "";

		public bool Start;

		public bool AutoStart = true;

		public string Notes = "";
	}

	/// <summary>Результат создания.</summary>
	public class Result
	{
		public bool Ok;

		public string Message = "";

		public string VmName = "";

		public string VmId = "";

		/// <summary>Что получилось не так, как просили, но не сорвало создание.</summary>
		public List<string> Warnings = new List<string>();

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["ok"] = Ok;
			d["message"] = Message;
			d["vmName"] = VmName;
			d["vmId"] = VmId;
			d["warnings"] = Warnings;
			if (!Ok)
			{
				d["error"] = Message;
			}
			return d;
		}
	}

	// ------------------------------------------------------------------ чтение хоста

	/// <summary>Виртуальные коммутаторы хоста.</summary>
	public static List<string> Switches(out string error)
	{
		error = "";
		List<string> list = new List<string>();
		try
		{
			ManagementObjectCollection coll = Hw.Query(Namespace,
				"SELECT * FROM Msvm_VirtualEthernetSwitch");
			if (coll == null)
			{
				error = "Hyper-V недоступен: не удалось прочитать список коммутаторов";
				return list;
			}
			try
			{
				foreach (ManagementBaseObject o in coll)
				{
					using (o)
					{
						string name = Hw.Prop(o, "ElementName");
						if (name.Length > 0)
						{
							list.Add(name);
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
			list.Sort(StringComparer.OrdinalIgnoreCase);
		}
		catch (Exception ex)
		{
			error = "Hyper-V недоступен: " + ex.Message;
		}
		return list;
	}

	/// <summary>Каталог по умолчанию для дисков новых ВМ.</summary>
	public static string DefaultDiskDirectory()
	{
		try
		{
			ManagementObjectCollection coll = Hw.Query(Namespace,
				"SELECT * FROM Msvm_VirtualSystemManagementServiceSettingData");
			if (coll != null)
			{
				try
				{
					foreach (ManagementBaseObject o in coll)
					{
						using (o)
						{
							string path = Hw.Prop(o, "DefaultVirtualHardDiskPath");
							if (path.Length > 0 && Directory.Exists(path))
							{
								return path;
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
		}
		catch
		{
		}
		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
			"Microsoft", "Windows", "Virtual Hard Disks");
	}

	// ------------------------------------------------------------------ создание

	/// <summary>
	/// Создаёт виртуальную машину. Ничего не бросает наружу: любая проблема
	/// возвращается в <see cref="Result"/> с текстом, пригодным для показа.
	/// </summary>
	public static Result Create(Spec spec)
	{
		Result r = new Result();
		string error = Validate(spec);
		if (error.Length > 0)
		{
			r.Message = error;
			return r;
		}

		string name = spec.Name.Trim();
		r.VmName = name;

		string diskPath = (spec.DiskPath ?? "").Trim();
		if (diskPath.Length == 0)
		{
			diskPath = Path.Combine(DefaultDiskDirectory(), SafeFileName(name) + ".vhdx");
		}

		// Если машина с таким именем уже есть — не создаём вторую.
		if (FindVmPath(name).Length > 0)
		{
			r.Message = "виртуальная машина с именем «" + name + "» уже существует";
			return r;
		}
		if (File.Exists(diskPath))
		{
			r.Message = "файл диска уже существует: " + diskPath;
			return r;
		}

		bool diskCreated = false;
		try
		{
			// 1. Диск. Без него машина создастся, но будет бесполезной.
			string diskError = CreateDisk(diskPath, Math.Max(1, spec.DiskGb));
			if (diskError.Length > 0)
			{
				r.Message = "не удалось создать диск: " + diskError;
				return r;
			}
			diskCreated = true;

			// 2. Сама машина. Ресурсы добавляются отдельно: процессор и память
			//    у новой ВМ уже есть, добавить их повторно Hyper-V не даст.
			string vmPath = DefineSystem(name, spec);
			if (vmPath.Length == 0)
			{
				r.Message = "Hyper-V не создал виртуальную машину";
				CleanupDisk(diskPath);
				return r;
			}

			string guid = Hw.Prop(new ManagementObject(vmPath), "Name");
			r.VmId = guid;

			string cfgPath = ConfigPath(guid);
			if (cfgPath.Length == 0)
			{
				r.Message = "машина создана, но её конфигурация не читается";
				return r;
			}

			// 3. Процессор и память — изменяем существующие ресурсы.
			string resError = SetCpuAndMemory(guid, spec, r.Warnings);
			if (resError.Length > 0)
			{
				r.Warnings.Add(resError);
			}

			// 4. Сеть.
			if (!string.IsNullOrWhiteSpace(spec.SwitchName))
			{
				string netError = AddNetwork(cfgPath, guid, spec.SwitchName.Trim());
				if (netError.Length > 0)
				{
					r.Warnings.Add("сеть не подключена: " + netError);
				}
			}

			// 5. Диск в машину.
			string diskAttach = AttachDisk(cfgPath, guid, diskPath);
			if (diskAttach.Length > 0)
			{
				r.Warnings.Add("диск не подключён: " + diskAttach);
			}

			// 6. ISO, если задан.
			if (!string.IsNullOrWhiteSpace(spec.IsoPath))
			{
				string isoError = AttachIso(cfgPath, guid, spec.IsoPath.Trim());
				if (isoError.Length > 0)
				{
					r.Warnings.Add("ISO не подключён: " + isoError);
				}
			}

			// 7. Автозапуск.
			if (spec.AutoStart)
			{
				SetAutoStart(guid);
			}

			VmInventory.Invalidate();

			// 8. Запуск.
			if (spec.Start)
			{
				VmControl.Result started = VmControl.SetPower(name, "start", false);
				if (!started.Ok)
				{
					r.Warnings.Add("запуск не удался: " + started.Message);
				}
			}

			r.Ok = true;
			r.Message = "машина создана" +
				(spec.Start && r.Warnings.Count == 0 ? " и запущена" : "");
			Log.Info("ВМ " + name + ": создана (" + spec.Generation + " поколение, " +
				spec.Cpu + " vCPU, " + spec.MemoryMb + " МБ, диск " + spec.DiskGb + " ГБ)");
			return r;
		}
		catch (Exception ex)
		{
			r.Message = "ошибка создания: " + ex.Message;
			Log.Warn("ВМ " + name + ", создание: " + ex.Message);
			if (diskCreated)
			{
				CleanupDisk(diskPath);
			}
			return r;
		}
	}

	/// <summary>Проверка параметров до любых действий.</summary>
	private static string Validate(Spec spec)
	{
		if (spec == null)
		{
			return "не заданы параметры";
		}
		string name = (spec.Name ?? "").Trim();
		if (name.Length == 0)
		{
			return "не указано имя машины";
		}
		if (name.Length > 100)
		{
			return "имя длиннее 100 символов";
		}
		if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
		{
			return "в имени есть недопустимые символы";
		}
		if (spec.Generation != 1 && spec.Generation != 2)
		{
			return "поколение может быть только 1 или 2";
		}
		if (spec.Cpu < 1 || spec.Cpu > 256)
		{
			return "процессоров должно быть от 1 до 256";
		}
		if (spec.MemoryMb < 32 || spec.MemoryMb > 1048576)
		{
			return "память должна быть от 32 МБ до 1 ТБ";
		}
		if (spec.DiskGb < 1 || spec.DiskGb > 65536)
		{
			return "размер диска должен быть от 1 до 65536 ГБ";
		}
		return "";
	}

	// ------------------------------------------------------------------ шаги

	private static string CreateDisk(string path, int sizeGb)
	{
		try
		{
			string dir = Path.GetDirectoryName(path);
			if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}

			ManagementObject svc = FindOne("SELECT * FROM Msvm_ImageManagementService");
			if (svc == null)
			{
				return "служба образов Hyper-V недоступна";
			}
			try
			{
				using ManagementObject settings = NewSetting("Msvm_VirtualHardDiskSettingData");
				settings["Path"] = path;
				// Тип 2 — фиксированный, 3 — динамический. Именно 3: иначе
				// диск сразу занимает весь заявленный объём, и «10 ГБ»
				// превращаются в 10 ГБ на хосте при пустой машине.
				settings["Type"] = (ushort)3;
				settings["Format"] = (ushort)3; // VHDX
				settings["MaxInternalSize"] = (ulong)sizeGb * 1024UL * 1024UL * 1024UL;
				settings["BlockSize"] = (uint)0;

				ManagementBaseObject inParams = svc.GetMethodParameters("CreateVirtualHardDisk");
				inParams["VirtualDiskSettingData"] = settings.GetText(TextFormat.WmiDtd20);
				ManagementBaseObject outParams = svc.InvokeMethod("CreateVirtualHardDisk", inParams, null);
				uint code = Convert.ToUInt32(outParams["ReturnValue"], CultureInfo.InvariantCulture);
				if (code != 0u && code != 4096u)
				{
					return "Hyper-V вернул код " + code;
				}
				string job = Convert.ToString(outParams["Job"], CultureInfo.InvariantCulture) ?? "";
				string jobError = WaitJob(job, 300);
				if (jobError.Length > 0)
				{
					return jobError;
				}
				if (!File.Exists(path))
				{
					return "файл не появился: " + path;
				}
				return "";
			}
			finally
			{
				svc.Dispose();
			}
		}
		catch (Exception ex)
		{
			return ex.Message;
		}
	}

	private static string DefineSystem(string name, Spec spec)
	{
		ManagementObject svc = FindOne("SELECT * FROM Msvm_VirtualSystemManagementService");
		if (svc == null)
		{
			return "";
		}
		try
		{
			using ManagementObject settings = NewSetting("Msvm_VirtualSystemSettingData");
			settings["ElementName"] = name;
			// Поколение задаётся подтипом: 1 — BIOS, 2 — UEFI.
			settings["VirtualSystemSubType"] = spec.Generation == 2
				? "Microsoft:Hyper-V:SubType:2"
				: "Microsoft:Hyper-V:SubType:1";
			if (!string.IsNullOrWhiteSpace(spec.Notes))
			{
				settings["Notes"] = new string[] { spec.Notes.Trim() };
			}

			ManagementBaseObject inParams = svc.GetMethodParameters("DefineSystem");
			inParams["SystemSettings"] = settings.GetText(TextFormat.WmiDtd20);
			inParams["ResourceSettings"] = new string[0];
			inParams["ReferenceConfiguration"] = null;

			ManagementBaseObject outParams = svc.InvokeMethod("DefineSystem", inParams, null);
			uint code = Convert.ToUInt32(outParams["ReturnValue"], CultureInfo.InvariantCulture);
			if (code != 0u && code != 4096u)
			{
				return "";
			}
			string job = Convert.ToString(outParams["Job"], CultureInfo.InvariantCulture) ?? "";
			if (WaitJob(job, 180).Length > 0)
			{
				return "";
			}
			string resulting = Convert.ToString(outParams["ResultingSystem"], CultureInfo.InvariantCulture) ?? "";
			if (resulting.Length == 0)
			{
				// Задание могло не вернуть ссылку — ищем по имени.
				return FindVmPath(name);
			}
			return resulting;
		}
		finally
		{
			svc.Dispose();
		}
	}

	/// <summary>
	/// Процессор и память. Оба ресурса уже есть у машины, поэтому
	/// ModifyResourceSettings, а не Add.
	/// </summary>
	private static string SetCpuAndMemory(string guid, Spec spec, List<string> warnings)
	{
		string cpuError = ModifyResource(guid, "Msvm_ProcessorSettingData", delegate(ManagementObject res)
		{
			res["VirtualQuantity"] = (ulong)spec.Cpu;
		});
		if (cpuError.Length > 0)
		{
			return "процессор не изменён: " + cpuError;
		}

		string memError = ModifyResource(guid, "Msvm_MemorySettingData", delegate(ManagementObject res)
		{
			res["VirtualQuantity"] = (ulong)spec.MemoryMb;
			if (spec.DynamicMemory)
			{
				res["DynamicMemoryEnabled"] = true;
				int min = spec.MemoryMinMb > 0 ? spec.MemoryMinMb : Math.Max(512, spec.MemoryMb / 2);
				int max = spec.MemoryMaxMb > 0 ? spec.MemoryMaxMb : spec.MemoryMb * 2;
				if (min > spec.MemoryMb)
				{
					min = spec.MemoryMb;
				}
				if (max < spec.MemoryMb)
				{
					max = spec.MemoryMb;
				}
				res["Reservation"] = (ulong)min;
				res["Limit"] = (ulong)max;
			}
		});

		if (memError.Length > 0)
		{
			// Динамическая память на некоторых хостах недоступна из-за раскладки
			// NUMA. Статическая работает всегда, поэтому откатываемся на неё и
			// честно пишем, что именно не вышло.
			if (spec.DynamicMemory)
			{
				string retry = ModifyResource(guid, "Msvm_MemorySettingData", delegate(ManagementObject res)
				{
					res["VirtualQuantity"] = (ulong)spec.MemoryMb;
				});
				if (retry.Length == 0)
				{
					warnings.Add("динамическая память недоступна (" + memError + "), включена фиксированная");
					return "";
				}
			}
			return "память не изменена: " + memError;
		}
		return "";
	}

	/// <summary>Меняет единственный ресурс нужного типа у машины.</summary>
	private static string ModifyResource(string guid, string className, Action<ManagementObject> mutate)
	{
		try
		{
			ManagementObject res = FindResource(guid, className);
			if (res == null)
			{
				return "ресурс " + className + " не найден";
			}
			using (res)
			{
				mutate(res);

				ManagementObject svc = FindOne("SELECT * FROM Msvm_VirtualSystemManagementService");
				if (svc == null)
				{
					return "служба управления Hyper-V недоступна";
				}
				try
				{
					ManagementBaseObject inParams = svc.GetMethodParameters("ModifyResourceSettings");
					inParams["ResourceSettings"] = new string[] { res.GetText(TextFormat.WmiDtd20) };
					ManagementBaseObject outParams = svc.InvokeMethod("ModifyResourceSettings", inParams, null);
					uint code = Convert.ToUInt32(outParams["ReturnValue"], CultureInfo.InvariantCulture);
					if (code != 0u && code != 4096u)
					{
						return "Hyper-V вернул код " + code;
					}
					string job = Convert.ToString(outParams["Job"], CultureInfo.InvariantCulture) ?? "";
					return WaitJob(job, 120);
				}
				finally
				{
					svc.Dispose();
				}
			}
		}
		catch (Exception ex)
		{
			return ex.Message;
		}
	}

	private static string AddNetwork(string cfgPath, string guid, string switchName)
	{
		string switchPath = SwitchPath(switchName);
		if (switchPath.Length == 0)
		{
			return "коммутатор «" + switchName + "» не найден";
		}

		try
		{
			using ManagementObject port = NewSetting("Msvm_SyntheticEthernetPortSettingData");
			// Набор свойств не случаен: без AllocationUnits, Weight и
			// StaticMacAddress Hyper-V отвечает «не удалось изменить свойство
			// Synthetic Ethernet Port» и адаптер не создаётся.
			port["ElementName"] = "Сетевой адаптер";
			port["ResourceType"] = TypeEthernetPort;
			port["ResourceSubType"] = "Microsoft:Hyper-V:Synthetic Ethernet Port";
			port["VirtualQuantity"] = (ulong)1;
			port["VirtualQuantityUnits"] = "count";
			port["AllocationUnits"] = "count";
			port["Reservation"] = (ulong)1;
			port["Limit"] = (ulong)1;
			port["Weight"] = (uint)0;
			port["ConsumerVisibility"] = (ushort)3;
			port["AutomaticAllocation"] = true;
			port["AutomaticDeallocation"] = true;
			port["StaticMacAddress"] = false;

			string portError = AddResource(cfgPath, port);
			if (portError.Length > 0)
			{
				return portError;
			}

			// Сам порт ни к чему не подключён: связь с коммутатором — отдельная
			// запись Msvm_EthernetPortAllocationSettingData с Parent = порт.
			string portPath = FindPortPath(guid);
			if (portPath.Length == 0)
			{
				return "порт создан, но не найден для подключения к коммутатору";
			}

			using ManagementObject link = NewSetting("Msvm_EthernetPortAllocationSettingData");
			link["ElementName"] = "Сетевой коммутатор";
			link["ResourceType"] = TypeEthernetConnection;
			link["ResourceSubType"] = "Microsoft:Hyper-V:Ethernet Connection";
			link["HostResource"] = new string[] { switchPath };
			link["Parent"] = portPath;
			link["EnabledState"] = (ushort)2;
			link["VirtualQuantity"] = (ulong)1;
			link["VirtualQuantityUnits"] = "count";
			link["AllocationUnits"] = "count";
			link["Reservation"] = (ulong)1;
			link["Limit"] = (ulong)1;
			link["Weight"] = (uint)0;
			link["ConsumerVisibility"] = (ushort)3;
			link["AutomaticAllocation"] = true;
			link["AutomaticDeallocation"] = true;

			return AddResource(cfgPath, link);
		}
		catch (Exception ex)
		{
			return ex.Message;
		}
	}

	private static string AttachDisk(string cfgPath, string guid, string diskPath)
	{
		try
		{
			// Контроллер: у Gen2 диски живут только на SCSI.
			string scsiPath = FindScsiPath(guid);
			if (scsiPath.Length == 0)
			{
				using ManagementObject scsi = NewSetting("Msvm_ResourceAllocationSettingData");
				scsi["ElementName"] = "Контроллер SCSI";
				scsi["ResourceType"] = TypeScsiController;
				scsi["ResourceSubType"] = "Microsoft:Hyper-V:Synthetic SCSI Controller";
				string scsiError = AddResource(cfgPath, scsi);
				if (scsiError.Length > 0)
				{
					return scsiError;
				}
				scsiPath = FindScsiPath(guid);
				if (scsiPath.Length == 0)
				{
					return "контроллер SCSI не создан";
				}
			}

			using ManagementObject drive = NewSetting("Msvm_ResourceAllocationSettingData");
			drive["ElementName"] = "Жёсткий диск";
			drive["ResourceType"] = TypeDiskDrive;
			drive["ResourceSubType"] = "Microsoft:Hyper-V:Synthetic Disk Drive";
			drive["AddressOnParent"] = NextDiskAddress(guid, scsiPath);
			drive["Parent"] = scsiPath;
			string driveError = AddResource(cfgPath, drive);
			if (driveError.Length > 0)
			{
				return driveError;
			}

			string drivePath = FindDiskDrivePath(guid);
			if (drivePath.Length == 0)
			{
				return "дисковод не создан";
			}

			using ManagementObject storage = NewSetting("Msvm_StorageAllocationSettingData");
			storage["ElementName"] = "Образ диска";
			storage["ResourceType"] = TypeStorage;
			storage["ResourceSubType"] = "Microsoft:Hyper-V:Virtual Hard Disk";
			storage["HostResource"] = new string[] { diskPath };
			storage["Parent"] = drivePath;
			return AddResource(cfgPath, storage);
		}
		catch (Exception ex)
		{
			return ex.Message;
		}
	}

	private static string AttachIso(string cfgPath, string guid, string isoPath)
	{
		if (!File.Exists(isoPath))
		{
			return "файл не найден: " + isoPath;
		}
		try
		{
			string scsiPath = FindScsiPath(guid);
			if (scsiPath.Length == 0)
			{
				return "нет контроллера SCSI";
			}

			using ManagementObject drive = NewSetting("Msvm_ResourceAllocationSettingData");
			drive["ElementName"] = "Привод DVD";
			drive["ResourceType"] = (ushort)16;
			drive["ResourceSubType"] = "Microsoft:Hyper-V:Synthetic DVD Drive";
			drive["AddressOnParent"] = NextDiskAddress(guid, scsiPath);
			drive["Parent"] = scsiPath;
			string driveError = AddResource(cfgPath, drive);
			if (driveError.Length > 0)
			{
				return driveError;
			}

			string drivePath = FindBySubType(guid, "Microsoft:Hyper-V:Synthetic DVD Drive");
			if (drivePath.Length == 0)
			{
				return "привод не создан";
			}

			using ManagementObject media = NewSetting("Msvm_StorageAllocationSettingData");
			media["ElementName"] = "Образ DVD";
			media["ResourceType"] = TypeStorage;
			media["ResourceSubType"] = "Microsoft:Hyper-V:Virtual CD/DVD Disk";
			media["HostResource"] = new string[] { isoPath };
			media["Parent"] = drivePath;
			return AddResource(cfgPath, media);
		}
		catch (Exception ex)
		{
			return ex.Message;
		}
	}

	/// <summary>Автозапуск вместе с хостом.</summary>
	private static void SetAutoStart(string guid)
	{
		try
		{
			ManagementObjectCollection coll = Hw.Query(Namespace,
				"SELECT * FROM Msvm_VirtualSystemSettingData WHERE InstanceID='Microsoft:" + guid +
				"' AND VirtualSystemType='Microsoft:Hyper-V:System:Realized'");
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
						if (!(o is ManagementObject mo))
						{
							continue;
						}
						mo["AutomaticStartupAction"] = (ushort)2; // StartIfRunning
						string xml = mo.GetText(TextFormat.WmiDtd20);

						ManagementObject svc = FindOne("SELECT * FROM Msvm_VirtualSystemManagementService");
						if (svc == null)
						{
							return;
						}
						try
						{
							ManagementBaseObject inParams = svc.GetMethodParameters("ModifySystemSettings");
							inParams["SystemSettings"] = xml;
							ManagementBaseObject outParams = svc.InvokeMethod("ModifySystemSettings", inParams, null);
							string job = Convert.ToString(outParams["Job"], CultureInfo.InvariantCulture) ?? "";
							WaitJob(job, 60);
						}
						finally
						{
							svc.Dispose();
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
		catch (Exception ex)
		{
			Log.Warn("ВМ " + guid + ", автозапуск: " + ex.Message);
		}
	}

	// ------------------------------------------------------------------ WMI-помощники

	private static string AddResource(string cfgPath, ManagementObject setting)
	{
		ManagementObject svc = FindOne("SELECT * FROM Msvm_VirtualSystemManagementService");
		if (svc == null)
		{
			return "служба управления Hyper-V недоступна";
		}
		try
		{
			ManagementBaseObject inParams = svc.GetMethodParameters("AddResourceSettings");
			inParams["AffectedConfiguration"] = cfgPath;
			// Строго массив строк: одиночная строка превращается в скаляр,
			// и Hyper-V отвечает «неверный параметр» (32773).
			inParams["ResourceSettings"] = new string[] { setting.GetText(TextFormat.WmiDtd20) };

			ManagementBaseObject outParams = svc.InvokeMethod("AddResourceSettings", inParams, null);
			uint code = Convert.ToUInt32(outParams["ReturnValue"], CultureInfo.InvariantCulture);
			if (code != 0u && code != 4096u)
			{
				return "Hyper-V вернул код " + code;
			}
			string job = Convert.ToString(outParams["Job"], CultureInfo.InvariantCulture) ?? "";
			return WaitJob(job, 180);
		}
		catch (Exception ex)
		{
			return ex.Message;
		}
		finally
		{
			svc.Dispose();
		}
	}

	private static ManagementObject NewSetting(string className)
	{
		ManagementScope scope = new ManagementScope(@"\\.\" + Namespace);
		scope.Connect();
		using ManagementClass cls = new ManagementClass(scope, new ManagementPath(className), null);
		return cls.CreateInstance();
	}

	private static ManagementObject FindOne(string wql)
	{
		ManagementObjectCollection coll = Hw.Query(Namespace, wql);
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

	private static string FindVmPath(string name)
	{
		ManagementObjectCollection coll = Hw.Query(Namespace,
			"SELECT * FROM Msvm_ComputerSystem");
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
					if (string.Equals(Hw.Prop(o, "ElementName"), name, StringComparison.OrdinalIgnoreCase) &&
						o is ManagementObject mo)
					{
						return mo.Path.Path;
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

	private static string ConfigPath(string guid)
	{
		ManagementObjectCollection coll = Hw.Query(Namespace,
			"SELECT * FROM Msvm_VirtualSystemSettingData WHERE InstanceID='Microsoft:" + guid +
			"' AND VirtualSystemType='Microsoft:Hyper-V:System:Realized'");
		if (coll == null)
		{
			return "";
		}
		try
		{
			foreach (ManagementBaseObject o in coll)
			{
				if (o is ManagementObject mo)
				{
					return mo.Path.Path;
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

	private static string SwitchPath(string switchName)
	{
		ManagementObjectCollection coll = Hw.Query(Namespace,
			"SELECT * FROM Msvm_VirtualEthernetSwitch");
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
					if (string.Equals(Hw.Prop(o, "ElementName"), switchName, StringComparison.OrdinalIgnoreCase) &&
						o is ManagementObject mo)
					{
						return mo.Path.Path;
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

	private static ManagementObject FindResource(string guid, string className)
	{
		ManagementObjectCollection coll = Hw.Query(Namespace,
			"SELECT * FROM " + className + " WHERE InstanceID LIKE 'Microsoft:" + guid + "%'");
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

	private static string FindBySubType(string guid, string subType)
	{
		ManagementObjectCollection coll = Hw.Query(Namespace,
			"SELECT * FROM Msvm_ResourceAllocationSettingData WHERE InstanceID LIKE 'Microsoft:" + guid + "%'");
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
					if (string.Equals(Hw.Prop(o, "ResourceSubType"), subType, StringComparison.OrdinalIgnoreCase) &&
						o is ManagementObject mo)
					{
						return mo.Path.Path;
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

	private static string FindScsiPath(string guid)
	{
		return FindBySubType(guid, "Microsoft:Hyper-V:Synthetic SCSI Controller");
	}

	private static string FindDiskDrivePath(string guid)
	{
		return FindBySubType(guid, "Microsoft:Hyper-V:Synthetic Disk Drive");
	}

	private static string FindPortPath(string guid)
	{
		ManagementObjectCollection coll = Hw.Query(Namespace,
			"SELECT * FROM Msvm_SyntheticEthernetPortSettingData WHERE InstanceID LIKE 'Microsoft:" + guid + "%'");
		if (coll == null)
		{
			return "";
		}
		try
		{
			foreach (ManagementBaseObject o in coll)
			{
				if (o is ManagementObject mo)
				{
					return mo.Path.Path;
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

	/// <summary>
	/// Свободный адрес на контроллере: 0..63. Без этого второй диск получил бы
	/// тот же адрес, что и первый, и Hyper-V отказал бы.
	/// </summary>
	private static string NextDiskAddress(string guid, string scsiPath)
	{
		HashSet<int> used = new HashSet<int>();
		ManagementObjectCollection coll = Hw.Query(Namespace,
			"SELECT * FROM Msvm_ResourceAllocationSettingData WHERE InstanceID LIKE 'Microsoft:" + guid + "%'");
		if (coll != null)
		{
			try
			{
				foreach (ManagementBaseObject o in coll)
				{
					using (o)
					{
						if (!string.Equals(Hw.Prop(o, "Parent"), scsiPath, StringComparison.OrdinalIgnoreCase))
						{
							continue;
						}
						string addr = Hw.Prop(o, "AddressOnParent");
						if (int.TryParse(addr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
						{
							used.Add(n);
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
		for (int i = 0; i < 64; i++)
		{
			if (!used.Contains(i))
			{
				return i.ToString(CultureInfo.InvariantCulture);
			}
		}
		return "0";
	}

	/// <summary>Ждёт WMI-задание. Пустая строка — успех.</summary>
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
					if (err.Length == 0)
					{
						err = "задание завершилось с состоянием " + state;
					}
					return ExplainJobError(err);
				}
			}
			catch
			{
				return "";
			}
			Thread.Sleep(500);
		}
		return "превышено время ожидания (" + timeoutSeconds + " с)";
	}

	/// <summary>Переводит текст ошибки Hyper-V в понятную фразу.</summary>
	private static string ExplainJobError(string text)
	{
		string msg = text ?? "";
		if (msg.IndexOf("NUMA", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "хост не может выделить память с такими параметрами (раскладка NUMA)";
		}
		if (msg.IndexOf("недостаточно", StringComparison.OrdinalIgnoreCase) >= 0 &&
			msg.IndexOf("памяти", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "на хосте недостаточно свободной памяти";
		}
		if (msg.IndexOf("0x80070070", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "на диске недостаточно места";
		}
		return msg;
	}

	private static void CleanupDisk(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch
		{
		}
	}

	private static string SafeFileName(string name)
	{
		char[] bad = Path.GetInvalidFileNameChars();
		System.Text.StringBuilder sb = new System.Text.StringBuilder(name.Length);
		foreach (char c in name)
		{
			sb.Append(Array.IndexOf(bad, c) >= 0 ? '_' : c);
		}
		string s = sb.ToString().Trim();
		return s.Length > 0 ? s : "vm";
	}
}
