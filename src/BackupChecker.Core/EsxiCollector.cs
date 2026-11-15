using System;
using System.Collections.Generic;

namespace BackupChecker.Core;

public class EsxiCollector : ICollector
{
	public string Type => "esxi";

	public List<CollectResult> Collect(Company company, Source source, AgentCtx ctx)
	{
		List<CollectResult> list = new List<CollectResult>();
		string jobPrefix = source.GetStr("jobName", "ESXi");
		string host = source.GetStr("host", "").Trim();
		int port = source.GetInt("port", 443);
		string user = source.GetStr("username", "");
		bool ignoreCert = source.GetBool("ignoreCertErrors", dflt: true);
		int timeout = source.GetInt("timeoutSeconds", 40);
		bool checkDatastores = source.GetBool("checkDatastores", dflt: true);
		int dsWarn = source.GetInt("datastoreWarnPercent", 15);
		int dsFail = source.GetInt("datastoreFailPercent", 5);
		bool checkSnapshots = source.GetBool("checkSnapshots", dflt: true);
		int snapMaxAgeDays = source.GetInt("snapshotMaxAgeDays", 7);
		bool checkSmart = source.GetBool("checkSmart", dflt: false);
		int sshPort = source.GetInt("sshPort", 22);
		string sshUser = source.GetStr("sshUser", "");
		string sshPassword = source.GetStr("sshPassword", "");
		int smartWarnTemp = source.GetInt("smartWarnTempC", 50);
		int smartMaxTemp = source.GetInt("smartMaxTempC", 60);
		HashSet<string> expected = ReadNames(source, "expectedRunning");
		HashSet<string> allowStopped = ReadNames(source, "allowStopped");
		string password = source.GetStr("password", "");
		if (password.Length == 0)
		{
			string key = source.GetStr("passwordSecret", "");
			if (key.Length > 0)
			{
				password = ctx.Config.Secret(key);
			}
		}
		if (sshUser.Length == 0)
		{
			sshUser = user;
		}
		if (sshPassword.Length == 0)
		{
			string sk = source.GetStr("sshPasswordSecret", "");
			sshPassword = ((sk.Length <= 0) ? password : ctx.Config.Secret(sk));
		}
		if (host.Length == 0)
		{
			CollectResult bad = Make(company, source, jobPrefix);
			bad.Reachable = false;
			bad.Message = "не задан адрес ESXi";
			list.Add(bad);
			return list;
		}
		if (!EsxiSoap.Connect(host, port, user, password, ignoreCert, timeout, out var conn, out var connError))
		{
			CollectResult bad2 = Make(company, source, jobPrefix);
			bad2.Reachable = false;
			bad2.Message = "ESXi недоступен: " + connError;
			list.Add(bad2);
			return list;
		}
		try
		{
			CollectResult hostRes = ReadHost(company, source, jobPrefix, conn);
			if (hostRes != null)
			{
				list.Add(hostRes);
			}
			foreach (EsxiSoap.VmData vm in conn.Vms)
			{
				CollectResult r = Make(company, source, jobPrefix + ": " + vm.Name);
				r.Target = vm.Name;
				r.Details = VmDetails(vm);
				List<string> problems = new List<string>();
				List<string> warnings = new List<string>();
				bool mustRun = expected.Contains(vm.Name);
				bool stopAllowed = allowStopped.Contains(vm.Name);
				if (vm.IsTemplate)
				{
					r.Message = "шаблон, состояние: " + vm.PowerState;
					list.Add(r);
					continue;
				}
				if (vm.PowerState == "poweredOn")
				{
					if (checkSnapshots && vm.SnapshotCount > 0 && snapMaxAgeDays > 0 && vm.OldestSnapshot.HasValue)
					{
						double ageDays = (DateTime.Now - vm.OldestSnapshot.Value).TotalDays;
						if (ageDays > (double)snapMaxAgeDays)
						{
							warnings.Add("снимок старше " + (int)ageDays + " дн (норма " + snapMaxAgeDays + ")");
						}
					}
				}
				else if (vm.PowerState == "poweredOff")
				{
					if (mustRun)
					{
						problems.Add("выключена, а должна работать");
					}
					else if (!stopAllowed)
					{
						warnings.Add("выключена");
					}
				}
				else if (vm.PowerState == "suspended")
				{
					if (mustRun)
					{
						problems.Add("в спящем режиме, а должна работать");
					}
					else
					{
						warnings.Add("приостановлена");
					}
				}
				else if (mustRun)
				{
					warnings.Add("состояние: " + vm.PowerState);
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
				bool found = false;
				foreach (EsxiSoap.VmData vm2 in conn.Vms)
				{
					if (string.Equals(vm2.Name, name, StringComparison.OrdinalIgnoreCase))
					{
						found = true;
						break;
					}
				}
				if (!found)
				{
					CollectResult r2 = Make(company, source, jobPrefix + ": " + name);
					r2.Target = name;
					Dictionary<string, object> d = Json.Obj();
					d["state"] = "missing";
					d["missing"] = true;
					r2.Details = d;
					r2.Fail("ВМ не найдена (ожидалась в списке работающих)");
					list.Add(r2);
				}
			}
			if (checkDatastores)
			{
				foreach (EsxiSoap.DsData ds in conn.Datastores)
				{
					CollectResult r3 = Make(company, source, jobPrefix + " хранилище: " + ds.Name);
					r3.Target = ds.Name;
					r3.Details = DsDetails(ds);
					List<string> problems2 = new List<string>();
					List<string> warnings2 = new List<string>();
					if (!ds.Accessible)
					{
						problems2.Add("хранилище недоступно");
					}
					else if (ds.Capacity > 0)
					{
						double freePercent = (double)ds.Free * 100.0 / (double)ds.Capacity;
						if (dsFail > 0 && freePercent < (double)dsFail)
						{
							problems2.Add("свободно " + (int)freePercent + "% (критично, порог " + dsFail + "%)");
						}
						else if (dsWarn > 0 && freePercent < (double)dsWarn)
						{
							warnings2.Add("свободно " + (int)freePercent + "% (порог " + dsWarn + "%)");
						}
					}
					string sizeText = Fmt.Size(ds.Capacity) + " из " + Fmt.Size(ds.Free) + " свободно";
					if (problems2.Count > 0)
					{
						r3.Fail(string.Join("; ", problems2.ToArray()));
					}
					else if (warnings2.Count > 0)
					{
						r3.Message = sizeText + "; " + string.Join("; ", warnings2.ToArray());
						r3.Slow = true;
					}
					else
					{
						r3.Message = sizeText;
					}
					list.Add(r3);
				}
			}
		}
		finally
		{
			EsxiSoap.Disconnect(conn);
		}
		if (checkSmart && sshPassword.Length > 0)
		{
			try
			{
				CollectSmart(company, source, jobPrefix, host, sshPort, sshUser, sshPassword, ctx.Settings.DataDir, timeout, smartWarnTemp, smartMaxTemp, list);
			}
			catch (Exception ex)
			{
				Log.Warn("ESXi SMART (" + host + "): " + ex.Message);
				CollectResult bad3 = Make(company, source, jobPrefix + ": SMART");
				bad3.NoData = true;
				bad3.Message = "SMART недоступен: " + ex.Message;
				list.Add(bad3);
			}
		}
		return list;
	}

	private static void CollectSmart(Company company, Source source, string jobPrefix, string host, int sshPort, string user, string password, string dataDir, int timeoutSec, int warnTemp, int maxTemp, List<CollectResult> list)
	{
		List<EsxiSmart.Device> devices = EsxiSmart.ListDevices(host, sshPort, user, password, dataDir, Math.Max(20, timeoutSec));
		if (devices.Count == 0)
		{
			CollectResult none = Make(company, source, jobPrefix + ": SMART");
			none.NoData = true;
			none.Message = "диски не найдены через SSH (проверьте доступ и права)";
			list.Add(none);
			return;
		}
		foreach (EsxiSmart.Device dev in devices)
		{
			string label = ((dev.Model.Length > 0) ? dev.Model : dev.Id);
			CollectResult r = Make(company, source, jobPrefix + " диск: " + label);
			r.Target = dev.Id;
			EsxiSmart.SmartData sd = EsxiSmart.ReadSmart(host, sshPort, user, password, dev.Id, dataDir, Math.Max(20, timeoutSec));
			Dictionary<string, object> d = Json.Obj();
			d["device"] = dev.Id;
			if (dev.Model.Length > 0)
			{
				d["model"] = dev.Model;
			}
			if (dev.Vendor.Length > 0)
			{
				d["vendor"] = dev.Vendor;
			}
			d["ssd"] = dev.IsSsd;
			if (dev.SizeBlocks > 0)
			{
				d["sizeGb"] = Math.Round((double)dev.SizeBlocks / 1024.0 / 1024.0, 1);
			}
			if (sd == null)
			{
				d["smart"] = false;
				r.Details = d;
				r.NoData = true;
				r.Message = "SMART недоступен для этого устройства (RAID-том или нет прав)";
				list.Add(r);
				continue;
			}
			d["smart"] = true;
			if (sd.Health.Length > 0)
			{
				d["health"] = sd.Health;
			}
			if (sd.Temperature > 0)
			{
				d["temperature"] = sd.Temperature;
			}
			if (sd.PowerOnHours >= 0)
			{
				d["powerOnHours"] = sd.PowerOnHours;
			}
			if (sd.PowerCycles >= 0)
			{
				d["powerCycles"] = sd.PowerCycles;
			}
			if (sd.Reallocated >= 0)
			{
				d["reallocated"] = sd.Reallocated;
			}
			if (sd.ReadErrors >= 0)
			{
				d["readErrors"] = sd.ReadErrors;
			}
			if (sd.WriteErrors >= 0)
			{
				d["writeErrors"] = sd.WriteErrors;
			}
			if (sd.Wearout >= 0)
			{
				d["wearout"] = sd.Wearout;
			}
			r.Details = d;
			List<string> problems = new List<string>();
			List<string> warnings = new List<string>();
			if (sd.Health.Length > 0 && !sd.Health.Equals("OK", StringComparison.OrdinalIgnoreCase) && !sd.Health.Equals("N/A", StringComparison.OrdinalIgnoreCase))
			{
				problems.Add("состояние: " + sd.Health);
			}
			if (sd.Temperature > 0)
			{
				if (sd.Temperature >= maxTemp)
				{
					problems.Add("температура " + sd.Temperature + " °C");
				}
				else if (sd.Temperature >= warnTemp)
				{
					warnings.Add("температура " + sd.Temperature + " °C");
				}
			}
			if (problems.Count > 0)
			{
				r.Fail(string.Join("; ", problems.ToArray()));
			}
			else if (warnings.Count > 0)
			{
				r.Message = JoinText(Summary(dev, sd), string.Join("; ", warnings.ToArray()));
				r.Slow = true;
			}
			else
			{
				r.Message = Summary(dev, sd);
			}
			list.Add(r);
		}
	}

	private static string Summary(EsxiSmart.Device dev, EsxiSmart.SmartData sd)
	{
		List<string> parts = new List<string>();
		parts.Add(dev.IsSsd ? "SSD" : "HDD");
		if (sd.Temperature > 0)
		{
			parts.Add(sd.Temperature + " °C");
		}
		if (sd.PowerOnHours > 100)
		{
			parts.Add(sd.PowerOnHours + " ч");
		}
		if (sd.Health.Length > 0)
		{
			parts.Add("SMART " + sd.Health);
		}
		return string.Join(", ", parts.ToArray());
	}

	private static string JoinText(string a, string b)
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

	private static CollectResult Make(Company c, Source s, string job)
	{
		CollectResult r = new CollectResult();
		r.CompanyId = c.Id;
		r.CompanyName = c.Name;
		r.SourceId = s.Id;
		r.Kind = "esxi";
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

	private static CollectResult ReadHost(Company company, Source source, string jobPrefix, EsxiSoap.Conn conn)
	{
		if (conn.Hosts.Count == 0)
		{
			CollectResult r = Make(company, source, jobPrefix);
			r.NoData = true;
			r.Message = "хост ESXi не найден в инвентаре (нет прав или пустой Datacenter)";
			return r;
		}
		EsxiSoap.HostData h = conn.Hosts[0];
		CollectResult res = Make(company, source, jobPrefix + ": " + h.Name);
		res.Target = h.Name;
		Dictionary<string, object> d = Json.Obj();
		d["role"] = "host";
		d["state"] = h.PowerState;
		if (h.ConnectionState.Length > 0)
		{
			d["connection"] = h.ConnectionState;
		}
		if (h.Version.Length > 0)
		{
			d["version"] = h.Version;
		}
		if (h.Build.Length > 0)
		{
			d["build"] = h.Build;
		}
		if (h.Model.Length > 0)
		{
			d["model"] = h.Model;
		}
		if (h.CpuCores > 0)
		{
			d["cpuCores"] = h.CpuCores;
		}
		if (h.CpuMhz > 0)
		{
			d["cpuMhz"] = h.CpuMhz;
		}
		if (h.MemoryMb > 0)
		{
			d["memoryMb"] = h.MemoryMb;
		}
		if (h.CpuUsageMhz >= 0)
		{
			d["cpuUsageMhz"] = h.CpuUsageMhz;
		}
		if (h.MemoryUsedMb >= 0)
		{
			d["memoryUsedMb"] = h.MemoryUsedMb;
		}
		if (h.UptimeHours >= 0.0)
		{
			d["uptimeHours"] = Math.Round(h.UptimeHours, 2);
		}
		d["vms"] = conn.Vms.Count;
		d["datastores"] = conn.Datastores.Count;
		res.Details = d;
		List<string> problems = new List<string>();
		List<string> warnings = new List<string>();
		if (h.ConnectionState.Length > 0 && h.ConnectionState != "connected")
		{
			problems.Add("состояние связи: " + h.ConnectionState);
		}
		if (h.PowerState != "poweredOn")
		{
			problems.Add("хост в состоянии " + h.PowerState);
		}
		if (h.CpuUsageMhz > 0 && h.CpuCores > 0 && h.CpuMhz > 0)
		{
			double totalMhz = (double)h.CpuCores * (double)h.CpuMhz;
			double loadPercent = (double)h.CpuUsageMhz * 100.0 / totalMhz;
			if (loadPercent > 90.0)
			{
				warnings.Add("CPU " + (int)loadPercent + "%");
			}
		}
		if (h.MemoryUsedMb > 0 && h.MemoryMb > 0)
		{
			double memPercent = (double)h.MemoryUsedMb * 100.0 / (double)h.MemoryMb;
			if (memPercent > 95.0)
			{
				problems.Add("память занята на " + (int)memPercent + "%");
			}
			else if (memPercent > 90.0)
			{
				warnings.Add("память занята на " + (int)memPercent + "%");
			}
		}
		if (problems.Count > 0)
		{
			res.Fail(string.Join("; ", problems.ToArray()));
		}
		else if (warnings.Count > 0)
		{
			res.Message = Join(h.Summary(), string.Join("; ", warnings.ToArray()));
			res.Slow = true;
		}
		else
		{
			res.Message = h.Summary();
		}
		return res;
	}

	private static Dictionary<string, object> VmDetails(EsxiSoap.VmData vm)
	{
		Dictionary<string, object> d = Json.Obj();
		d["role"] = "vm";
		d["state"] = vm.PowerState;
		if (vm.Cpu > 0)
		{
			d["vcpu"] = vm.Cpu;
		}
		if (vm.MemoryMb > 0)
		{
			d["memoryMb"] = vm.MemoryMb;
		}
		if (vm.GuestFullName.Length > 0)
		{
			d["guest"] = vm.GuestFullName;
		}
		if (vm.IsTemplate)
		{
			d["template"] = true;
		}
		if (vm.SnapshotCount > 0)
		{
			d["snapshots"] = vm.SnapshotCount;
			if (vm.OldestSnapshot.HasValue)
			{
				d["oldestSnapshot"] = vm.OldestSnapshot.Value;
			}
		}
		return d;
	}

	private static Dictionary<string, object> DsDetails(EsxiSoap.DsData ds)
	{
		Dictionary<string, object> d = Json.Obj();
		d["role"] = "datastore";
		d["accessible"] = ds.Accessible;
		if (ds.Capacity > 0)
		{
			d["capacityBytes"] = ds.Capacity;
		}
		if (ds.Free >= 0)
		{
			d["freeBytes"] = ds.Free;
		}
		if (ds.Capacity > 0 && ds.Free >= 0)
		{
			d["freePercent"] = Math.Round((double)ds.Free * 100.0 / (double)ds.Capacity, 1);
		}
		if (ds.Type.Length > 0)
		{
			d["type"] = ds.Type;
		}
		return d;
	}
}
