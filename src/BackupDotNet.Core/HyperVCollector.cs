using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management;

namespace BackupDotNet.Core;

public class HyperVCollector : ICollector
{
	public class VmInfo
	{
		public string Name = "";

		public int StateCode = -1;

		public string State = "";

		public int HealthCode = -1;

		public long CpuCount = -1L;

		public long MemoryMb = -1L;

		public bool DynamicMemory;

		public double UptimeHours = -1.0;

		public int HeartbeatCode = -1;

		public string Heartbeat = "";

		public List<string> Disks = new List<string>();

		public int CheckpointCount;

		public DateTime? OldestCheckpoint;

		public string Summary()
		{
			List<string> parts = new List<string>();
			parts.Add(State);
			if (CpuCount > 0)
			{
				parts.Add(CpuCount + " vCPU");
			}
			if (MemoryMb > 0)
			{
				parts.Add(FmtMemory(MemoryMb));
			}
			if (StateCode == 2 && UptimeHours >= 0.0)
			{
				parts.Add("uptime " + FmtUptime(UptimeHours));
			}
			if (CheckpointCount > 0)
			{
				parts.Add("снимков " + CheckpointCount);
			}
			return string.Join(", ", parts.ToArray());
		}

		private static string FmtMemory(long mb)
		{
			if (mb >= 1024)
			{
				return ((double)mb / 1024.0).ToString("0.#", CultureInfo.InvariantCulture) + " ГБ";
			}
			return mb + " МБ";
		}
	}

	public class HyperVInfo
	{
		public string Note = "";

		public List<VmInfo> Vms = new List<VmInfo>();
	}

	private const string WmiNs = "root\\virtualization\\v2";

	public string Type => "hyperv";

	public List<CollectResult> Collect(Company company, Source source, AgentCtx ctx)
	{
		List<CollectResult> list = new List<CollectResult>();
		string jobPrefix = source.GetStr("jobName", "ВМ");
		int uptimeWarnMin = source.GetInt("uptimeWarnMinutes", 10);
		bool checkHeartbeat = source.GetBool("checkHeartbeat", dflt: true);
		bool checkDiskFree = source.GetBool("checkDiskFree", dflt: true);
		int diskFreeWarn = source.GetInt("diskFreeWarnPercent", 10);
		Dictionary<string, object> cp = Json.AsObject(Json.Get(source.Raw, "checkpoints"));
		int cpMaxAgeDays = Json.Int(Json.Get(cp, "maxAgeDays"), 7);
		int cpMaxCount = Json.Int(Json.Get(cp, "maxCount"), 3);
		HashSet<string> expected = ReadNames(source, "expectedRunning");
		HashSet<string> allowStopped = ReadNames(source, "allowStopped");
		HyperVInfo info = ReadLocal();
		if (info.Note.Length > 0 && info.Vms.Count == 0)
		{
			CollectResult none = Make(company, source, jobPrefix);
			none.NoData = true;
			none.Message = info.Note;
			list.Add(none);
			return list;
		}
		if (info.Vms.Count == 0)
		{
			CollectResult none2 = Make(company, source, jobPrefix);
			none2.NoData = true;
			none2.Message = "виртуальных машин на хосте нет";
			list.Add(none2);
			return list;
		}
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (VmInfo vm in info.Vms)
		{
			seen.Add(vm.Name);
			CollectResult r = Make(company, source, jobPrefix + ": " + vm.Name);
			r.Target = vm.Name;
			r.Details = BuildDetails(vm);
			List<string> problems = new List<string>();
			List<string> warnings = new List<string>();
			bool mustRun = expected.Contains(vm.Name);
			bool stopAllowed = allowStopped.Contains(vm.Name);
			if (vm.StateCode == 2)
			{
				if (checkHeartbeat && vm.HeartbeatCode >= 0 && vm.HeartbeatCode != 2)
				{
					warnings.Add("нет heartbeat" + ((vm.Heartbeat.Length > 0) ? (" (" + vm.Heartbeat + ")") : ""));
				}
				if (uptimeWarnMin > 0 && vm.UptimeHours >= 0.0 && vm.UptimeHours * 60.0 < (double)uptimeWarnMin)
				{
					warnings.Add("uptime " + FmtUptime(vm.UptimeHours) + " — возможно, перезагружается");
				}
			}
			else if (vm.StateCode == 3)
			{
				if (mustRun)
				{
					problems.Add("остановлена, а должна работать");
				}
				else if (!stopAllowed)
				{
					warnings.Add("остановлена");
				}
			}
			else if (vm.StateCode == 9 || vm.StateCode == 32768)
			{
				if (mustRun)
				{
					problems.Add("на паузе, а должна работать");
				}
				else
				{
					warnings.Add("на паузе");
				}
			}
			else if (vm.StateCode == 32769 || vm.StateCode == 32779)
			{
				if (mustRun)
				{
					problems.Add("в сохранённом состоянии, а должна работать");
				}
				else
				{
					warnings.Add("сохранена на диск");
				}
			}
			else if (mustRun)
			{
				warnings.Add("переходное состояние: " + vm.State);
			}
			if (cpMaxCount > 0 && vm.CheckpointCount > cpMaxCount)
			{
				warnings.Add("снимков " + vm.CheckpointCount + " (норма до " + cpMaxCount + ")");
			}
			if (cpMaxAgeDays > 0 && vm.OldestCheckpoint.HasValue)
			{
				double ageDays = (DateTime.Now - vm.OldestCheckpoint.Value).TotalDays;
				if (ageDays > (double)cpMaxAgeDays)
				{
					warnings.Add("снимок старше " + (int)ageDays + " дн (норма " + cpMaxAgeDays + ")");
				}
			}
			if (checkDiskFree)
			{
				foreach (string free in CheckDisks(vm.Disks, diskFreeWarn))
				{
					warnings.Add(free);
				}
			}
			if (problems.Count > 0)
			{
				r.Fail(string.Join("; ", problems.ToArray()));
			}
			else if (warnings.Count > 0)
			{
				r.Message = Join(vm.Summary(), string.Join("; ", warnings.ToArray()));
				r.Slow = true;
			}
			else
			{
				r.Message = vm.Summary();
			}
			list.Add(r);
		}
		foreach (string name in expected)
		{
			if (!seen.Contains(name))
			{
				CollectResult r2 = Make(company, source, jobPrefix + ": " + name);
				r2.Target = name;
				Dictionary<string, object> d = Json.Obj();
				d["state"] = "missing";
				d["missing"] = true;
				r2.Details = d;
				r2.Fail("ВМ не найдена на хосте (ожидалась в списке работающих)");
				list.Add(r2);
			}
		}
		return list;
	}

	private static CollectResult Make(Company c, Source s, string job)
	{
		CollectResult r = new CollectResult();
		r.CompanyId = c.Id;
		r.CompanyName = c.Name;
		r.SourceId = s.Id;
		r.Kind = "hyperv";
		r.Job = job;
		r.IsHealth = true;
		r.Reachable = true;
		return r;
	}

	private static HashSet<string> ReadNames(Source source, string key)
	{
		HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (object o in source.GetArray(key))
		{
			string s = Json.Str(o).Trim();
			if (s.Length > 0)
			{
				set.Add(s);
			}
		}
		return set;
	}

	private static Dictionary<string, object> BuildDetails(VmInfo vm)
	{
		Dictionary<string, object> d = Json.Obj();
		d["state"] = vm.State;
		d["stateCode"] = vm.StateCode;
		if (vm.CpuCount > 0)
		{
			d["vcpu"] = vm.CpuCount;
		}
		if (vm.MemoryMb > 0)
		{
			d["memoryMb"] = vm.MemoryMb;
		}
		if (vm.DynamicMemory)
		{
			d["dynamicMemory"] = true;
		}
		if (vm.UptimeHours >= 0.0)
		{
			d["uptimeHours"] = Math.Round(vm.UptimeHours, 2);
		}
		if (vm.Heartbeat.Length > 0)
		{
			d["heartbeat"] = vm.Heartbeat;
		}
		if (vm.HeartbeatCode >= 0)
		{
			d["heartbeatOk"] = vm.HeartbeatCode == 2;
		}
		d["checkpoints"] = vm.CheckpointCount;
		if (vm.OldestCheckpoint.HasValue)
		{
			d["oldestCheckpoint"] = vm.OldestCheckpoint.Value;
		}
		if (vm.Disks.Count > 0)
		{
			d["disks"] = ToObjectList(vm.Disks);
		}
		return d;
	}

	private static List<object> ToObjectList(List<string> src)
	{
		List<object> list = new List<object>();
		foreach (string s in src)
		{
			list.Add(s);
		}
		return list;
	}

	private static string Join(string a, string b)
	{
		if (a.Length == 0)
		{
			return b;
		}
		if (b.Length == 0)
		{
			return a;
		}
		return a + "; " + b;
	}

	public static string FmtUptime(double hours)
	{
		if (hours < 0.0)
		{
			return "—";
		}
		if (hours < 1.0)
		{
			return (int)(hours * 60.0) + " мин";
		}
		if (hours < 48.0)
		{
			return (int)hours + " ч";
		}
		return (int)(hours / 24.0) + " дн";
	}

	private static List<string> CheckDisks(List<string> disks, int warnPercent)
	{
		List<string> problems = new List<string>();
		HashSet<string> volumes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string path in disks)
		{
			try
			{
				string root = Path.GetPathRoot(path);
				if (string.IsNullOrEmpty(root) || !volumes.Add(root))
				{
					continue;
				}
				DriveInfo di = new DriveInfo(root);
				if (di.IsReady && di.TotalSize > 0)
				{
					double freePercent = (double)di.TotalFreeSpace * 100.0 / (double)di.TotalSize;
					if (freePercent < (double)warnPercent)
					{
						problems.Add("мало места на " + root.TrimEnd('\\') + " — " + (int)freePercent + "% свободно");
					}
				}
			}
			catch
			{
			}
		}
		return problems;
	}

	public static HyperVInfo ReadLocal()
	{
		HyperVInfo info = new HyperVInfo();
		string hostName = Environment.MachineName;
		ManagementObjectCollection systems = Hw.Query("root\\virtualization\\v2", "SELECT * FROM Msvm_ComputerSystem");
		if (systems == null)
		{
			info.Note = "Hyper-V недоступен: нет прав на WMI root\\virtualization\\v2 (нужны права администратора или членство в Hyper-V Administrators)";
			return info;
		}
		Dictionary<string, List<DateTime>> snapshots = ReadSnapshots();
		foreach (ManagementBaseObject o in systems)
		{
			using (o)
			{
				string name = Hw.Prop(o, "ElementName");
				if (string.IsNullOrEmpty(name) || string.Equals(name, hostName, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				VmInfo vm = new VmInfo();
				vm.Name = name;
				vm.StateCode = Hw.PropInt(o, "EnabledState", -1);
				vm.State = StateName(vm.StateCode);
				vm.HealthCode = Hw.PropInt(o, "HealthState", -1);
				if (vm.StateCode == 2)
				{
					try
					{
						DateTime? changed = Hw.ParseWmiDate(o["TimeOfLastStateChange"]);
						if (changed.HasValue)
						{
							double h = (DateTime.UtcNow - changed.Value).TotalHours;
							if (h >= 0.0 && h < 100000.0)
							{
								vm.UptimeHours = h;
							}
						}
					}
					catch (Exception ex)
					{
						Log.Debug("Hyper-V uptime " + name + ": " + ex.Message);
					}
				}
				string vmId = Hw.Prop(o, "Name");
				if (vmId.Length > 0 && snapshots.TryGetValue(vmId, out var snapTimes))
				{
					vm.CheckpointCount = snapTimes.Count;
					DateTime oldest = DateTime.MaxValue;
					foreach (DateTime dt in snapTimes)
					{
						if (dt < oldest)
						{
							oldest = dt;
						}
					}
					if (oldest != DateTime.MaxValue)
					{
						vm.OldestCheckpoint = oldest;
					}
				}
				try
				{
					ReadSettings(o, vm);
				}
				catch (Exception ex2)
				{
					Log.Debug("Hyper-V settings " + name + ": " + ex2.Message);
				}
				try
				{
					ReadHeartbeat(o, vm);
				}
				catch (Exception ex3)
				{
					Log.Debug("Hyper-V heartbeat " + name + ": " + ex3.Message);
				}
				info.Vms.Add(vm);
			}
		}
		return info;
	}

	private static Dictionary<string, List<DateTime>> ReadSnapshots()
	{
		Dictionary<string, List<DateTime>> map = new Dictionary<string, List<DateTime>>(StringComparer.OrdinalIgnoreCase);
		ManagementObjectCollection all = Hw.Query("root\\virtualization\\v2", "SELECT * FROM Msvm_VirtualSystemSettingData");
		if (all == null)
		{
			return map;
		}
		foreach (ManagementBaseObject o in all)
		{
			using (o)
			{
				string type = Hw.Prop(o, "VirtualSystemType");
				if (type.IndexOf("Snapshot", StringComparison.OrdinalIgnoreCase) < 0)
				{
					continue;
				}
				string vmId = Hw.Prop(o, "VirtualSystemIdentifier");
				if (vmId.Length != 0)
				{
					DateTime? created = null;
					try
					{
						created = Hw.ParseWmiDate(o["CreationTime"]);
					}
					catch
					{
					}
					if (!map.TryGetValue(vmId, out var list))
					{
						list = (map[vmId] = new List<DateTime>());
					}
					list.Add(created.HasValue ? created.Value.ToLocalTime() : DateTime.Now);
				}
			}
		}
		return map;
	}

	private static void ReadSettings(ManagementBaseObject vmObject, VmInfo vm)
	{
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
			using (ManagementObjectCollection.ManagementObjectEnumerator managementObjectEnumerator2 = current.GetRelated("Msvm_MemorySettingData").GetEnumerator())
			{
				if (managementObjectEnumerator2.MoveNext())
				{
					ManagementObject mem = (ManagementObject)managementObjectEnumerator2.Current;
					using (mem)
					{
						long qty = Hw.PropLong(mem, "VirtualQuantity", -1L);
						if (qty > 0)
						{
							vm.MemoryMb = qty;
						}
						vm.DynamicMemory = Hw.PropBool(mem, "DynamicMemoryEnabled");
					}
				}
			}
			using (ManagementObjectCollection.ManagementObjectEnumerator managementObjectEnumerator3 = current.GetRelated("Msvm_ProcessorSettingData").GetEnumerator())
			{
				if (managementObjectEnumerator3.MoveNext())
				{
					ManagementObject cpu = (ManagementObject)managementObjectEnumerator3.Current;
					using (cpu)
					{
						long qty2 = Hw.PropLong(cpu, "VirtualQuantity", -1L);
						if (qty2 > 0)
						{
							vm.CpuCount = qty2;
						}
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
						if (hr is string[] arr)
						{
							string[] array = arr;
							foreach (string path in array)
							{
								if (!string.IsNullOrEmpty(path))
								{
									vm.Disks.Add(path);
								}
							}
						}
						else
						{
							string one = hr as string;
							if (!string.IsNullOrEmpty(one))
							{
								vm.Disks.Add(one);
							}
						}
					}
					catch
					{
					}
				}
			}
		}
	}

	private static void ReadHeartbeat(ManagementBaseObject vmObject, VmInfo vm)
	{
		if (!(vmObject is ManagementObject mo))
		{
			return;
		}
		using ManagementObjectCollection.ManagementObjectEnumerator managementObjectEnumerator = mo.GetRelated("Msvm_HeartbeatComponent").GetEnumerator();
		if (managementObjectEnumerator.MoveNext())
		{
			ManagementObject hb = (ManagementObject)managementObjectEnumerator.Current;
			using (hb)
			{
				vm.HeartbeatCode = HeartbeatCode(hb);
				vm.Heartbeat = Hw.Prop(hb, "PrimaryStatusDescription");
				return;
			}
		}
	}

	private static int HeartbeatCode(ManagementBaseObject hb)
	{
		try
		{
			object v = hb["OperationalStatus"];
			if (v is ushort[] arr && arr.Length > 0)
			{
				return arr[0];
			}
			if (v is uint[] arr2 && arr2.Length > 0)
			{
				return (int)arr2[0];
			}
			if (v is int[] arr3 && arr3.Length > 0)
			{
				return arr3[0];
			}
		}
		catch
		{
		}
		return -1;
	}

	public static string StateName(int code)
	{
		switch (code)
		{
		case 2:
			return "Running";
		case 3:
			return "Off";
		case 9:
			return "Paused";
		case 32768:
			return "Paused-Critical";
		case 32769:
			return "Saved";
		case 32770:
			return "Starting";
		case 32771:
			return "Snapshotting";
		case 32773:
			return "Saving";
		case 32774:
			return "Stopping";
		case 32777:
			return "Restoring";
		case 32779:
			return "FastSaved";
		case 32780:
			return "FastSaving";
		default:
			if (code >= 0)
			{
				return "state " + code;
			}
			return "";
		}
	}
}
