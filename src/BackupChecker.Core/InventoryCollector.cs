using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management;
using Microsoft.Win32;

namespace BackupChecker.Core;

public class InventoryCollector : ICollector
{
	public class SystemInfo
	{
		public string HostName = "";

		public string OsCaption = "";

		public string OsVersion = "";

		public string OsBuild = "";

		public string OsArch = "";

		public string Domain = "";

		public string Manufacturer = "";

		public string Model = "";

		public string CpuName = "";

		public int CpuCores = -1;

		public long MemoryMb = -1L;

		public double UptimeHours = -1.0;

		public DateTime? InstalledOn;

		public bool RebootPending;

		public string UserName = "";

		public string Summary()
		{
			List<string> parts = new List<string>();
			if (OsCaption.Length > 0)
			{
				parts.Add(OsCaption);
			}
			if (Model.Length > 0)
			{
				parts.Add(Model);
			}
			if (CpuCores > 0)
			{
				parts.Add(CpuCores + " ядер");
			}
			if (MemoryMb > 0)
			{
				parts.Add(FmtSize(MemoryMb * 1024 * 1024));
			}
			if (UptimeHours >= 0.0)
			{
				parts.Add("uptime " + FmtUptime(UptimeHours));
			}
			if (parts.Count <= 0)
			{
				return "Windows";
			}
			return string.Join(", ", parts.ToArray());
		}

		public Dictionary<string, object> ToDetails()
		{
			Dictionary<string, object> d = Json.Obj();
			d["host"] = HostName;
			if (OsCaption.Length > 0)
			{
				d["os"] = OsCaption;
			}
			if (OsVersion.Length > 0)
			{
				d["osVersion"] = OsVersion;
			}
			if (OsBuild.Length > 0)
			{
				d["osBuild"] = OsBuild;
			}
			if (OsArch.Length > 0)
			{
				d["architecture"] = OsArch;
			}
			if (Domain.Length > 0)
			{
				d["domain"] = Domain;
			}
			if (Manufacturer.Length > 0)
			{
				d["manufacturer"] = Manufacturer;
			}
			if (Model.Length > 0)
			{
				d["model"] = Model;
			}
			if (CpuName.Length > 0)
			{
				d["cpu"] = CpuName;
			}
			if (CpuCores > 0)
			{
				d["cpuCores"] = CpuCores;
			}
			if (MemoryMb > 0)
			{
				d["memoryMb"] = MemoryMb;
			}
			if (UptimeHours >= 0.0)
			{
				d["uptimeHours"] = Math.Round(UptimeHours, 2);
			}
			if (InstalledOn.HasValue)
			{
				d["osInstalled"] = InstalledOn.Value;
			}
			if (RebootPending)
			{
				d["rebootPending"] = true;
			}
			return d;
		}
	}

	public class ServiceInfo
	{
		public string Name = "";

		public string DisplayName = "";

		public string State = "";

		public string StartMode = "";

		public bool Exists;
	}

	public class VolumeInfo
	{
		public string Name = "";

		public string Label = "";

		public string FileSystem = "";

		public string DriveType = "";

		public long TotalBytes;

		public long FreeBytes;

		public double FreePercent;
	}

	public class SoftwareInfo
	{
		public string Name = "";

		public string Version = "";

		public string Publisher = "";

		public string InstallDate = "";
	}

	public class HotfixInfo
	{
		public string Id = "";

		public string Description = "";

		public DateTime InstalledOn = DateTime.MinValue;
	}

	public string Type => "inventory";

	public List<CollectResult> Collect(Company company, Source source, AgentCtx ctx)
	{
		List<CollectResult> list = new List<CollectResult>();
		string prefix = source.GetStr("jobName", "Инвентарь");
		bool checkVolumes = source.GetBool("checkVolumes", dflt: true);
		int volWarn = source.GetInt("volumeMinFreePercent", 10);
		int volFail = ((source.GetInt("checkDiskFree", 0) > 0) ? source.GetInt("checkDiskFree", 0) : 5);
		bool collectSoftware = source.GetBool("collectSoftware", dflt: true);
		int maxSoftware = source.GetInt("softwareMaxItems", 60);
		bool checkHotfixes = source.GetBool("checkHotfixes", dflt: false);
		int hotfixMaxAge = source.GetInt("hotfixMaxAgeDays", 60);
		SystemInfo sys = ReadSystem();
		CollectResult host = Make(company, source, prefix + ": " + sys.HostName);
		host.Target = sys.HostName;
		host.Details = sys.ToDetails();
		List<string> problems = new List<string>();
		List<string> warnings = new List<string>();
		if (sys.UptimeHours >= 0.0 && sys.UptimeHours < 1.0)
		{
			warnings.Add("uptime " + FmtUptime(sys.UptimeHours) + " — недавно перезагружалась");
		}
		if (sys.RebootPending)
		{
			warnings.Add("требуется перезагрузка (обновления)");
		}
		List<string> serviceNames = new List<string>();
		foreach (object o in source.GetArray("services"))
		{
			string s = Json.Str(o).Trim();
			if (s.Length > 0)
			{
				serviceNames.Add(s);
			}
		}
		List<ServiceInfo> services = ((serviceNames.Count > 0) ? ReadServices(serviceNames) : new List<ServiceInfo>());
		foreach (ServiceInfo sv in services)
		{
			if (!sv.Exists)
			{
				problems.Add("служба " + sv.Name + " не установлена");
			}
			else if (sv.StartMode == "Disabled")
			{
				problems.Add("служба " + sv.Name + " отключена");
			}
			else if (!string.Equals(sv.State, "Running", StringComparison.OrdinalIgnoreCase))
			{
				problems.Add("служба " + sv.Name + ": " + sv.State);
			}
		}
		if (services.Count > 0)
		{
			List<string> parts = new List<string>();
			foreach (ServiceInfo sv2 in services)
			{
				parts.Add(sv2.Name + ": " + (sv2.Exists ? sv2.State : "нет"));
			}
			Dictionary<string, object> sd = Json.Obj();
			List<object> arr = new List<object>();
			foreach (ServiceInfo sv3 in services)
			{
				Dictionary<string, object> x = Json.Obj();
				x["name"] = sv3.Name;
				x["displayName"] = sv3.DisplayName;
				x["state"] = (sv3.Exists ? sv3.State : "missing");
				x["startMode"] = sv3.StartMode;
				arr.Add(x);
			}
			sd["services"] = arr;
			host.Details["services"] = arr;
			if (problems.Count == 0 && warnings.Count == 0)
			{
				host.Message = Join(sys.Summary(), "служб: " + services.Count + " — все работают");
			}
			else if (problems.Count == 0)
			{
				host.Message = Join(sys.Summary(), string.Join("; ", warnings.ToArray()));
			}
		}
		if (problems.Count > 0)
		{
			host.Fail(string.Join("; ", problems.ToArray()));
		}
		else if (warnings.Count > 0 && host.Message.Length == 0)
		{
			host.Message = Join(sys.Summary(), string.Join("; ", warnings.ToArray()));
			host.Slow = true;
		}
		else if (host.Message.Length == 0)
		{
			host.Message = sys.Summary();
		}
		list.Add(host);
		if (checkVolumes)
		{
			foreach (VolumeInfo v in ReadVolumes())
			{
				if (v.TotalBytes > 0)
				{
					CollectResult r = Make(company, source, prefix + ": диск " + v.Name);
					r.Target = v.Name;
					Dictionary<string, object> d = Json.Obj();
					d["volume"] = v.Name;
					d["label"] = v.Label;
					d["totalBytes"] = v.TotalBytes;
					d["freeBytes"] = v.FreeBytes;
					d["freePercent"] = Math.Round(v.FreePercent, 1);
					if (v.FileSystem.Length > 0)
					{
						d["fs"] = v.FileSystem;
					}
					if (v.DriveType.Length > 0)
					{
						d["driveType"] = v.DriveType;
					}
					r.Details = d;
					string sizeText = Fmt.Size(v.TotalBytes) + " всего, " + Fmt.Size(v.FreeBytes) + " свободно (" + (int)v.FreePercent + "%)";
					if (v.FreePercent < (double)volFail)
					{
						r.Fail(sizeText + " — ниже критического порога " + volFail + "%");
					}
					else if (v.FreePercent < (double)volWarn)
					{
						r.Message = sizeText + " — ниже порога " + volWarn + "%";
						r.Slow = true;
					}
					else
					{
						r.Message = sizeText;
					}
					list.Add(r);
				}
			}
		}
		if (collectSoftware)
		{
			CollectResult sw = Make(company, source, prefix + ": программы");
			sw.Target = "software";
			List<SoftwareInfo> all = ReadSoftware();
			Dictionary<string, object> d2 = Json.Obj();
			d2["count"] = all.Count;
			List<object> items = new List<object>();
			int n = 0;
			foreach (SoftwareInfo s2 in all)
			{
				if (n++ < maxSoftware)
				{
					Dictionary<string, object> x2 = Json.Obj();
					x2["name"] = s2.Name;
					if (s2.Version.Length > 0)
					{
						x2["version"] = s2.Version;
					}
					if (s2.Publisher.Length > 0)
					{
						x2["publisher"] = s2.Publisher;
					}
					if (s2.InstallDate.Length > 0)
					{
						x2["installDate"] = s2.InstallDate;
					}
					items.Add(x2);
					continue;
				}
				break;
			}
			d2["items"] = items;
			if (all.Count > items.Count)
			{
				d2["truncated"] = true;
			}
			sw.Details = d2;
			sw.Message = "установлено программ: " + all.Count;
			List<string> backupTools = new List<string>();
			foreach (SoftwareInfo s3 in all)
			{
				string low = s3.Name.ToLowerInvariant();
				if (low.Contains("veeam"))
				{
					backupTools.Add(s3.Name);
				}
				else if (low.Contains("iperius"))
				{
					backupTools.Add(s3.Name);
				}
				else if (low.Contains("sql server"))
				{
					backupTools.Add(s3.Name);
				}
			}
			if (backupTools.Count > 0)
			{
				List<string> parts2 = new List<string>();
				foreach (string b in backupTools)
				{
					parts2.Add(b);
				}
				sw.Message = sw.Message + "; найдено: " + string.Join(", ", parts2.ToArray());
			}
			list.Add(sw);
		}
		if (checkHotfixes)
		{
			CollectResult hf = Make(company, source, prefix + ": обновления");
			hf.Target = "hotfixes";
			List<HotfixInfo> hotfixes = ReadHotfixes();
			Dictionary<string, object> d3 = Json.Obj();
			d3["count"] = hotfixes.Count;
			List<object> items2 = new List<object>();
			DateTime newest = DateTime.MinValue;
			int n2 = 0;
			foreach (HotfixInfo h in hotfixes)
			{
				if (h.InstalledOn > newest)
				{
					newest = h.InstalledOn;
				}
				if (n2++ < 30)
				{
					Dictionary<string, object> x3 = Json.Obj();
					x3["id"] = h.Id;
					if (h.Description.Length > 0)
					{
						x3["description"] = h.Description;
					}
					if (h.InstalledOn != DateTime.MinValue)
					{
						x3["installedOn"] = h.InstalledOn;
					}
					items2.Add(x3);
				}
			}
			d3["items"] = items2;
			if (newest != DateTime.MinValue)
			{
				d3["newest"] = newest;
			}
			hf.Details = d3;
			if (newest == DateTime.MinValue)
			{
				hf.Message = "обновления не найдены (список пуст)";
				hf.Slow = true;
			}
			else
			{
				double ageDays = (DateTime.Now - newest).TotalDays;
				d3["ageDays"] = Math.Round(ageDays, 1);
				string text = "последнее обновление " + (int)ageDays + " дн назад (" + newest.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + "), всего " + hotfixes.Count;
				if (hotfixMaxAge > 0 && ageDays > (double)hotfixMaxAge)
				{
					hf.Message = text + " — давно не обновлялась (порог " + hotfixMaxAge + " дн)";
					hf.Slow = true;
				}
				else
				{
					hf.Message = text;
				}
			}
			list.Add(hf);
		}
		return list;
	}

	private static CollectResult Make(Company c, Source s, string job)
	{
		CollectResult r = new CollectResult();
		r.CompanyId = c.Id;
		r.CompanyName = c.Name;
		r.SourceId = s.Id;
		r.Kind = "inventory";
		r.Job = job;
		r.IsHealth = true;
		r.Reachable = true;
		return r;
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

	private static string FmtUptime(double hours)
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

	private static string FmtSize(long bytes)
	{
		if (bytes <= 0)
		{
			return "—";
		}
		double b = bytes;
		string[] units = new string[5] { "Б", "КБ", "МБ", "ГБ", "ТБ" };
		int i = 0;
		while (b >= 1024.0 && i < units.Length - 1)
		{
			b /= 1024.0;
			i++;
		}
		return string.Format(CultureInfo.InvariantCulture, (i == 0) ? "{0:0} {1}" : "{0:0.#} {1}", b, units[i]);
	}

	public static SystemInfo ReadSystem()
	{
		SystemInfo si = new SystemInfo();
		si.HostName = Environment.MachineName;
		ManagementObjectCollection os = Hw.Query("root\\cimv2", "SELECT Caption, Version, BuildNumber, OSArchitecture, InstallDate, LastBootUpTime, LocalDateTime FROM Win32_OperatingSystem");
		if (os != null)
		{
			using ManagementObjectCollection.ManagementObjectEnumerator managementObjectEnumerator = os.GetEnumerator();
			if (managementObjectEnumerator.MoveNext())
			{
				ManagementBaseObject o = managementObjectEnumerator.Current;
				using (o)
				{
					si.OsCaption = Hw.Prop(o, "Caption").Replace("Microsoft ", "").Trim();
					si.OsVersion = Hw.Prop(o, "Version");
					si.OsBuild = Hw.Prop(o, "BuildNumber");
					si.OsArch = Hw.Prop(o, "OSArchitecture");
					DateTime? installed = Hw.ParseWmiDate(o["InstallDate"]);
					if (installed.HasValue)
					{
						si.InstalledOn = installed.Value.ToLocalTime();
					}
					DateTime? boot = Hw.ParseWmiDate(o["LastBootUpTime"]);
					if (boot.HasValue)
					{
						double h = (DateTime.UtcNow - boot.Value).TotalHours;
						if (h >= 0.0)
						{
							si.UptimeHours = h;
						}
					}
				}
			}
		}
		ManagementObjectCollection cs = Hw.Query("root\\cimv2", "SELECT Domain, Manufacturer, Model, TotalPhysicalMemory, UserName, NumberOfLogicalProcessors FROM Win32_ComputerSystem");
		if (cs != null)
		{
			using ManagementObjectCollection.ManagementObjectEnumerator managementObjectEnumerator2 = cs.GetEnumerator();
			if (managementObjectEnumerator2.MoveNext())
			{
				ManagementBaseObject o2 = managementObjectEnumerator2.Current;
				using (o2)
				{
					si.Domain = Hw.Prop(o2, "Domain");
					si.Manufacturer = Hw.Prop(o2, "Manufacturer");
					si.Model = Hw.Prop(o2, "Model");
					si.UserName = Hw.Prop(o2, "UserName");
					long mem = Hw.PropLong(o2, "TotalPhysicalMemory", -1L);
					if (mem > 0)
					{
						si.MemoryMb = mem / 1048576;
					}
					si.CpuCores = Hw.PropInt(o2, "NumberOfLogicalProcessors", -1);
				}
			}
		}
		ManagementObjectCollection cpu = Hw.Query("root\\cimv2", "SELECT Name, NumberOfCores FROM Win32_Processor");
		if (cpu != null)
		{
			using ManagementObjectCollection.ManagementObjectEnumerator managementObjectEnumerator3 = cpu.GetEnumerator();
			if (managementObjectEnumerator3.MoveNext())
			{
				ManagementBaseObject o3 = managementObjectEnumerator3.Current;
				using (o3)
				{
					string name = Hw.Prop(o3, "Name");
					if (name.Length > 0)
					{
						si.CpuName = name.Replace("(R)", "").Replace("(TM)", "").Replace("CPU ", "")
							.Trim();
					}
				}
			}
		}
		si.RebootPending = CheckRebootPending();
		return si;
	}

	private static bool CheckRebootPending()
	{
		try
		{
			using (RegistryKey k = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Component Based Servicing\\RebootPending"))
			{
				if (k != null)
				{
					return true;
				}
			}
			using RegistryKey k2 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsUpdate\\Auto Update\\RebootRequired");
			if (k2 != null)
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	public static List<ServiceInfo> ReadServices(List<string> names)
	{
		List<ServiceInfo> result = new List<ServiceInfo>();
		ManagementObjectCollection svc = Hw.Query("root\\cimv2", "SELECT Name, DisplayName, State, StartMode FROM Win32_Service");
		Dictionary<string, ServiceInfo> found = new Dictionary<string, ServiceInfo>(StringComparer.OrdinalIgnoreCase);
		if (svc != null)
		{
			foreach (ManagementBaseObject o in svc)
			{
				using (o)
				{
					string n = Hw.Prop(o, "Name");
					if (n.Length != 0)
					{
						ServiceInfo si = new ServiceInfo();
						si.Name = n;
						si.DisplayName = Hw.Prop(o, "DisplayName");
						si.State = Hw.Prop(o, "State");
						si.StartMode = Hw.Prop(o, "StartMode");
						si.Exists = true;
						found[n] = si;
					}
				}
			}
		}
		foreach (string want in names)
		{
			if (found.TryGetValue(want, out var si2))
			{
				result.Add(si2);
				continue;
			}
			ServiceInfo missing = new ServiceInfo();
			missing.Name = want;
			missing.State = "missing";
			missing.Exists = false;
			result.Add(missing);
		}
		return result;
	}

	public static List<VolumeInfo> ReadVolumes()
	{
		List<VolumeInfo> result = new List<VolumeInfo>();
		ManagementObjectCollection disks = Hw.Query("root\\cimv2", "SELECT DeviceID, VolumeName, FileSystem, DriveType, Size, FreeSpace FROM Win32_LogicalDisk");
		if (disks != null)
		{
			foreach (ManagementBaseObject o in disks)
			{
				using (o)
				{
					int type = Hw.PropInt(o, "DriveType", 0);
					if (type == 3)
					{
						VolumeInfo v = new VolumeInfo();
						v.Name = Hw.Prop(o, "DeviceID");
						v.Label = Hw.Prop(o, "VolumeName");
						v.FileSystem = Hw.Prop(o, "FileSystem");
						v.DriveType = "local";
						v.TotalBytes = Hw.PropLong(o, "Size", 0L);
						v.FreeBytes = Hw.PropLong(o, "FreeSpace", 0L);
						if (v.TotalBytes > 0)
						{
							v.FreePercent = (double)v.FreeBytes * 100.0 / (double)v.TotalBytes;
						}
						result.Add(v);
					}
				}
			}
		}
		return result;
	}

	public static List<SoftwareInfo> ReadSoftware()
	{
		List<SoftwareInfo> result = new List<SoftwareInfo>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string[] roots = new string[2] { "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall", "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall" };
		string[] array = roots;
		foreach (string root in array)
		{
			try
			{
				using RegistryKey key = Registry.LocalMachine.OpenSubKey(root);
				if (key == null)
				{
					continue;
				}
				string[] subKeyNames = key.GetSubKeyNames();
				foreach (string sub in subKeyNames)
				{
					try
					{
						using RegistryKey app = key.OpenSubKey(sub);
						if (app == null)
						{
							continue;
						}
						string name = Convert.ToString(app.GetValue("DisplayName"), CultureInfo.InvariantCulture);
						if (!string.IsNullOrEmpty(name))
						{
							string low = name.ToLowerInvariant();
							if (!low.StartsWith("update for") && !low.StartsWith("security update") && !low.StartsWith("hotfix for") && seen.Add(name.Trim()))
							{
								SoftwareInfo s = new SoftwareInfo();
								s.Name = name.Trim();
								s.Version = Convert.ToString(app.GetValue("DisplayVersion"), CultureInfo.InvariantCulture);
								s.Publisher = Convert.ToString(app.GetValue("Publisher"), CultureInfo.InvariantCulture);
								s.InstallDate = FormatInstallDate(Convert.ToString(app.GetValue("InstallDate"), CultureInfo.InvariantCulture));
								result.Add(s);
							}
						}
					}
					catch
					{
					}
				}
			}
			catch
			{
			}
		}
		result.Sort((SoftwareInfo a, SoftwareInfo b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
		return result;
	}

	private static string FormatInstallDate(string raw)
	{
		if (string.IsNullOrEmpty(raw) || raw.Length != 8)
		{
			return "";
		}
		if (!int.TryParse(raw.Substring(0, 4), out var y))
		{
			return "";
		}
		if (!int.TryParse(raw.Substring(4, 2), out var m))
		{
			return "";
		}
		if (!int.TryParse(raw.Substring(6, 2), out var d))
		{
			return "";
		}
		try
		{
			return new DateTime(y, m, d).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}
		catch
		{
			return "";
		}
	}

	public static List<HotfixInfo> ReadHotfixes()
	{
		List<HotfixInfo> result = new List<HotfixInfo>();
		ManagementObjectCollection qfe = Hw.Query("root\\cimv2", "SELECT HotFixID, Description, InstalledOn FROM Win32_QuickFixEngineering");
		if (qfe == null)
		{
			return result;
		}
		foreach (ManagementBaseObject o in qfe)
		{
			using (o)
			{
				HotfixInfo h = new HotfixInfo();
				h.Id = Hw.Prop(o, "HotFixID");
				h.Description = Hw.Prop(o, "Description");
				string on = Hw.Prop(o, "InstalledOn");
				if (on.Length > 0 && DateTime.TryParse(on, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
				{
					h.InstalledOn = dt;
				}
				if (h.Id.Length > 0)
				{
					result.Add(h);
				}
			}
		}
		result.Sort((HotfixInfo a, HotfixInfo b) => b.InstalledOn.CompareTo(a.InstalledOn));
		return result;
	}
}
