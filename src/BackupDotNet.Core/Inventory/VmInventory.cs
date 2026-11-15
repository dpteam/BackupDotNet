using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management;
using System.Threading;
using BackupDotNet.Core.Backup;

namespace BackupDotNet.Core.Inventory;

/// <summary>
/// Инвентарь виртуальных машин Hyper-V.
///
/// Отличие от коллектора проверок: коллектор отвечает «всё ли хорошо» и потому
/// смотрит только отклонения. Инвентарь отвечает «что у меня есть» — полный
/// список с ресурсами, дисками, сетями и снимками, включая выключенные ВМ.
///
/// WMI по десятку ВМ занимает секунды, поэтому результат кэшируется:
/// страница не должна заставлять ждать при каждом обновлении.
/// </summary>
public static class VmInventory
{
	private static readonly object Sync = new object();

	private static List<Dictionary<string, object>> _cachedVms;

	private static Dictionary<string, object> _cachedHost;

	private static DateTime _cachedAt = DateTime.MinValue;

	private static string _cachedError = "";

	/// <summary>Сколько секунд держать кэш. Ручное обновление его сбрасывает.</summary>
	private const int CacheSeconds = 20;

	/// <summary>Сбрасывает кэш — после создания или удаления ВМ.</summary>
	public static void Invalidate()
	{
		lock (Sync)
		{
			_cachedAt = DateTime.MinValue;
		}
	}

	/// <summary>Полный список ВМ. <paramref name="force"/> — мимо кэша.</summary>
	public static List<Dictionary<string, object>> Vms(out string error, bool force)
	{
		lock (Sync)
		{
			if (!force && _cachedVms != null && (DateTime.Now - _cachedAt).TotalSeconds < CacheSeconds)
			{
				error = _cachedError;
				return _cachedVms;
			}
		}

		List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
		string err = "";
		try
		{
			list = ReadVms();
		}
		catch (Exception ex)
		{
			err = ex.Message;
			Log.Warn("Инвентарь ВМ: " + ex.Message);
		}

		lock (Sync)
		{
			_cachedVms = list;
			_cachedError = err;
			_cachedAt = DateTime.Now;
		}
		error = err;
		return list;
	}

	/// <summary>Сведения о хосте: процессоры, память, диски с ВМ.</summary>
	public static Dictionary<string, object> Host(out string error, bool force)
	{
		lock (Sync)
		{
			if (!force && _cachedHost != null && (DateTime.Now - _cachedAt).TotalSeconds < CacheSeconds)
			{
				error = _cachedError;
				return _cachedHost;
			}
		}
		Dictionary<string, object> host = new Dictionary<string, object>();
		string err = "";
		try
		{
			host = ReadHost();
		}
		catch (Exception ex)
		{
			err = ex.Message;
		}
		lock (Sync)
		{
			_cachedHost = host;
		}
		error = err;
		return host;
	}

	// ------------------------------------------------------------------ чтение

	private static List<Dictionary<string, object>> ReadVms()
	{
		List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
		ManagementObjectCollection systems = Hw.Query("root\\virtualization\\v2", "SELECT * FROM Msvm_ComputerSystem");
		if (systems == null)
		{
			throw new BackupException("Hyper-V недоступен: нет прав на WMI root\\virtualization\\v2");
		}

		string hostName = Environment.MachineName;
		Dictionary<string, int> snapshotCounts = CountSnapshots();
		Dictionary<string, string> ipAddresses = ReadIpAddresses();

		try
		{
			foreach (ManagementBaseObject o in systems)
			{
				using (o)
				{
					string name = Hw.Prop(o, "ElementName");
					// Сам хост тоже приходит как ComputerSystem — пропускаем.
					if (string.IsNullOrEmpty(name) || string.Equals(name, hostName, StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}

					Dictionary<string, object> vm = Json.Obj();
					string vmId = Hw.Prop(o, "Name");
					int stateCode = Hw.PropInt(o, "EnabledState", -1);

					vm["id"] = vmId;
					vm["name"] = name;
					vm["stateCode"] = stateCode;
					vm["state"] = HyperVExportEngine.StateName(stateCode);
					vm["running"] = stateCode == 2;
					vm["healthCode"] = Hw.PropInt(o, "HealthState", -1);

					// Аптайм считается только для работающих: у выключенной это шум.
					double uptime = -1.0;
					if (stateCode == 2)
					{
						try
						{
							DateTime? changed = Hw.ParseWmiDate(o["TimeOfLastStateChange"]);
							if (changed.HasValue)
							{
								double h = (DateTime.UtcNow - changed.Value).TotalHours;
								if (h >= 0.0 && h < 100000.0)
								{
									uptime = h;
								}
							}
						}
						catch
						{
						}
					}
					vm["uptimeHours"] = uptime;

					// Ресурсы и диски — через связанные настройки.
					long cpu = -1L;
					long memMb = -1L;
					bool dynamicMem = false;
					long memStartup = -1L;
					long memMax = -1L;
					int generation = 0;
					string version = "";
					string configRoot = "";
					List<object> disks = new List<object>();
					List<object> adapters = new List<object>();
					long diskBytes = 0L;

					ReadSettings(o, out cpu, out memMb, out dynamicMem, out memStartup, out memMax,
						out generation, out version, out configRoot, disks, adapters, ref diskBytes);

					vm["cpu"] = cpu;
					vm["memoryMb"] = memMb;
					vm["memoryStartupMb"] = memStartup;
					vm["memoryMaxMb"] = memMax;
					vm["dynamicMemory"] = dynamicMem;
					vm["generation"] = generation;
					vm["version"] = version;
					vm["configRoot"] = configRoot;
					vm["disks"] = disks;
					vm["adapters"] = adapters;
					vm["diskBytes"] = diskBytes;
					vm["checkpoints"] = snapshotCounts.TryGetValue(vmId, out var n) ? n : 0;

					if (ipAddresses.TryGetValue(vmId, out var ip))
					{
						vm["ip"] = ip;
					}

					vm["heartbeat"] = ReadHeartbeat(o);

					result.Add(vm);
				}
			}
		}
		finally
		{
			try
			{
				systems.Dispose();
			}
			catch
			{
			}
		}

		result.Sort((a, b) => string.Compare(
			Json.GetStr(a, "name", ""), Json.GetStr(b, "name", ""), StringComparison.OrdinalIgnoreCase));
		return result;
	}

	private static void ReadSettings(ManagementBaseObject vmObject, out long cpu, out long memMb,
		out bool dynamicMem, out long memStartup, out long memMax, out int generation, out string version,
		out string configRoot, List<object> disks, List<object> adapters, ref long diskBytes)
	{
		cpu = -1L;
		memMb = -1L;
		dynamicMem = false;
		memStartup = -1L;
		memMax = -1L;
		generation = 0;
		version = "";
		configRoot = "";

		if (!(vmObject is ManagementObject mo))
		{
			return;
		}
		ManagementObject current = null;
		foreach (ManagementObject sd in mo.GetRelated("Msvm_VirtualSystemSettingData"))
		{
			string type = Hw.Prop(sd, "VirtualSystemType");
			if (current == null && type.IndexOf("System:Realized", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				current = sd;
			}
			else
			{
				sd.Dispose();
			}
		}
		if (current == null)
		{
			return;
		}

		using (current)
		{
			generation = Hw.PropInt(current, "VirtualSystemSubType", 0) > 0 ? 0 : 0;
			// Поколение определяется по подтипу: Microsoft:Hyper-V:SubType:2 → Gen2.
			string subtype = Hw.Prop(current, "VirtualSystemSubType");
			if (subtype.EndsWith(":2", StringComparison.Ordinal))
			{
				generation = 2;
			}
			else if (subtype.EndsWith(":1", StringComparison.Ordinal))
			{
				generation = 1;
			}
			version = Hw.Prop(current, "Version");
			configRoot = Hw.Prop(current, "ConfigurationDataRoot");

			foreach (ManagementObject mem in current.GetRelated("Msvm_MemorySettingData"))
			{
				using (mem)
				{
					long qty = Hw.PropLong(mem, "VirtualQuantity", -1L);
					if (qty > 0)
					{
						memMb = qty;
					}
					dynamicMem = Hw.PropBool(mem, "DynamicMemoryEnabled");
					long start = Hw.PropLong(mem, "Startup", -1L);
					if (start > 0)
					{
						memStartup = start;
					}
					long max = Hw.PropLong(mem, "Limit", -1L);
					if (max > 0)
					{
						memMax = max;
					}
				}
			}

			foreach (ManagementObject proc in current.GetRelated("Msvm_ProcessorSettingData"))
			{
				using (proc)
				{
					long qty = Hw.PropLong(proc, "VirtualQuantity", -1L);
					if (qty > 0)
					{
						cpu = qty;
					}
				}
			}

			foreach (ManagementObject disk in current.GetRelated("Msvm_StorageAllocationSettingData"))
			{
				using (disk)
				{
					try
					{
						object hr = disk["HostResource"];
						List<string> paths = new List<string>();
						if (hr is string[] arr)
						{
							foreach (string p in arr)
							{
								if (!string.IsNullOrEmpty(p))
								{
									paths.Add(p);
								}
							}
						}
						else if (hr != null)
						{
							string single = Convert.ToString(hr, CultureInfo.InvariantCulture);
							if (!string.IsNullOrEmpty(single))
							{
								paths.Add(single);
							}
						}

						foreach (string path in paths)
						{
							Dictionary<string, object> d = Json.Obj();
							d["path"] = path;
							d["fileName"] = Path.GetFileName(path);
							long bytes = 0L;
							string sizeNote = "";
							try
							{
								FileInfo fi = new FileInfo(path);
								if (fi.Exists)
								{
									bytes = fi.Length;
								}
								else
								{
									sizeNote = "файл недоступен";
								}
							}
							catch (Exception ex)
							{
								sizeNote = ex.Message;
							}
							d["sizeBytes"] = bytes;
							if (sizeNote.Length > 0)
							{
								d["note"] = sizeNote;
							}
							d["type"] = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
							disks.Add(d);
							diskBytes += bytes;
						}
					}
					catch
					{
					}
				}
			}

			foreach (ManagementObject nic in current.GetRelated("Msvm_SyntheticEthernetPortSettingData"))
			{
				using (nic)
				{
					Dictionary<string, object> a = Json.Obj();
					a["name"] = Hw.Prop(nic, "ElementName");
					a["mac"] = Hw.Prop(nic, "Address");
					a["connected"] = !Hw.PropBool(nic, "Disabled");
					adapters.Add(a);
				}
			}
		}
	}

	private static Dictionary<string, int> CountSnapshots()
	{
		Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		ManagementObjectCollection all = Hw.Query("root\\virtualization\\v2", "SELECT * FROM Msvm_VirtualSystemSettingData");
		if (all == null)
		{
			return counts;
		}
		try
		{
			foreach (ManagementBaseObject o in all)
			{
				using (o)
				{
					string type = Hw.Prop(o, "VirtualSystemType");
					if (type.IndexOf("Snapshot", StringComparison.OrdinalIgnoreCase) < 0)
					{
						continue;
					}
					string name = Hw.Prop(o, "ConfigurationID");
					if (name.Length == 0)
					{
						continue;
					}
					counts.TryGetValue(name, out int n);
					counts[name] = n + 1;
				}
			}
		}
		finally
		{
			try
			{
				all.Dispose();
			}
			catch
			{
			}
		}
		return counts;
	}

	private static Dictionary<string, string> ReadIpAddresses()
	{
		Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		ManagementObjectCollection all = Hw.Query("root\\virtualization\\v2",
			"SELECT * FROM Msvm_GuestNetworkAdapterConfiguration");
		if (all == null)
		{
			return map;
		}
		try
		{
			foreach (ManagementBaseObject o in all)
			{
				using (o)
				{
					try
					{
						object ip = o["IPAddresses"];
						if (ip is string[] arr)
						{
							foreach (string a in arr)
							{
								// Берём первый IPv4: IPv6 в интерфейсе только мешает.
								if (!string.IsNullOrEmpty(a) && a.IndexOf(':') < 0)
								{
									string id = Hw.Prop(o, "InstanceID");
									int dot = id.IndexOf(':');
									map[id] = a;
									break;
								}
							}
						}
					}
					catch
					{
					}
				}
			}
		}
		finally
		{
			try
			{
				all.Dispose();
			}
			catch
			{
			}
		}
		return map;
	}

	private static string ReadHeartbeat(ManagementBaseObject vmObject)
	{
		if (!(vmObject is ManagementObject mo))
		{
			return "";
		}
		try
		{
			foreach (ManagementObject hb in mo.GetRelated("Msvm_HeartbeatComponent"))
			{
				using (hb)
				{
					int code = Hw.PropInt(hb, "OperationalStatus", -1);
					switch (code)
					{
					case 2:
						return "в норме";
					case 6:
						return "нет связи";
					case 12:
						return "нет контакта";
					case 0:
						return "нет данных";
					default:
						return "код " + code;
					}
				}
			}
		}
		catch
		{
		}
		return "";
	}

	private static Dictionary<string, object> ReadHost()
	{
		Dictionary<string, object> host = Json.Obj();
		host["name"] = Environment.MachineName;
		host["os"] = Environment.OSVersion.VersionString;
		host["processorCount"] = Environment.ProcessorCount;

		long totalMemMb = 0L;
		try
		{
			ManagementObjectCollection cs = Hw.Query("root\\cimv2",
				"SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
			if (cs != null)
			{
				foreach (ManagementBaseObject o in cs)
				{
					using (o)
					{
						totalMemMb = Hw.PropLong(o, "TotalPhysicalMemory", 0L) / 1048576L;
					}
				}
				cs.Dispose();
			}
		}
		catch
		{
		}
		host["memoryTotalMb"] = totalMemMb;

		// Диски, где лежат ВМ: показываем свободное место — это первое, что
		// заканчивается при активном резервном копировании.
		List<object> drives = new List<object>();
		try
		{
			foreach (DriveInfo di in DriveInfo.GetDrives())
			{
				try
				{
					if (!di.IsReady || di.DriveType != DriveType.Fixed && di.DriveType != DriveType.Network)
					{
						continue;
					}
					Dictionary<string, object> d = Json.Obj();
					d["name"] = di.Name;
					d["label"] = di.VolumeLabel;
					d["format"] = di.DriveFormat;
					d["totalGb"] = Math.Round(di.TotalSize / 1073741824.0, 1);
					d["freeGb"] = Math.Round(di.AvailableFreeSpace / 1073741824.0, 1);
					d["freePercent"] = di.TotalSize > 0
						? Math.Round(di.AvailableFreeSpace * 100.0 / di.TotalSize, 1)
						: 0.0;
					drives.Add(d);
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		host["drives"] = drives;
		return host;
	}
}
