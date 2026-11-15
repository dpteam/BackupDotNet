using System;
using System.Collections.Generic;
using System.Management;

namespace BackupDotNet.Core;

public class SmartCollector : ICollector
{
	public string Type => "smart";

	public List<CollectResult> Collect(Company company, Source source, AgentCtx ctx)
	{
		List<CollectResult> list = new List<CollectResult>();
		string job = source.GetStr("jobName", "SMART дисков");
		bool useSmartctl = source.GetBool("checkSmartctl", dflt: true);
		int minLife = source.GetInt("minLifePercent", 20);
		int warnTemp = source.GetInt("warnTempC", 45);
		int maxTemp = source.GetInt("maxTempC", 55);
		string deviceType = source.GetStr("deviceType", "");
		Hw.SmartScan scan = ReadDisks(useSmartctl, deviceType);
		if (scan.Disks.Count == 0)
		{
			CollectResult none = Make(company, source, job);
			none.NoData = true;
			none.Message = ((scan.Note.Length > 0) ? scan.Note : "диски не обнаружены");
			list.Add(none);
			return list;
		}
		foreach (Hw.DiskInfo d in scan.Disks)
		{
			CollectResult r = Make(company, source, job + ": " + DiskLabel(d, scan.Disks));
			r.Target = d.DeviceId;
			List<string> problems = new List<string>();
			List<string> warnings = new List<string>();
			if (d.HealthCode == 1)
			{
				warnings.Add("состояние: Warning");
			}
			else if (d.HealthCode == 2)
			{
				problems.Add("состояние: Unhealthy");
			}
			else if (d.HealthCode > 2 && d.HealthCode != 5)
			{
				warnings.Add("состояние: " + d.Health);
			}
			if (d.SmartStatus.Length > 0 && d.SmartStatus != "OK" && d.SmartStatus != "Passed")
			{
				problems.Add("SMART: " + d.SmartStatus);
			}
			if (d.LifeLeft >= 0)
			{
				if (d.LifeLeft <= 5)
				{
					problems.Add("ресурс " + d.LifeLeft + "% (критично)");
				}
				else if (d.LifeLeft <= minLife)
				{
					warnings.Add("ресурс " + d.LifeLeft + "%");
				}
			}
			else if (d.Wear >= 0)
			{
				int life = 100 - d.Wear;
				if (life <= 5)
				{
					problems.Add("износ " + d.Wear + "% (критично)");
				}
				else if (life <= minLife)
				{
					warnings.Add("износ " + d.Wear + "%");
				}
			}
			if (d.Temperature >= 0)
			{
				if (d.Temperature >= maxTemp)
				{
					problems.Add("температура " + d.Temperature + " °C (порог " + maxTemp + ")");
				}
				else if (d.Temperature >= warnTemp)
				{
					warnings.Add("температура " + d.Temperature + " °C");
				}
			}
			if (d.ReadErrors > 0)
			{
				warnings.Add("ошибок чтения: " + d.ReadErrors);
			}
			if (d.WriteErrors > 0)
			{
				warnings.Add("ошибок записи: " + d.WriteErrors);
			}
			if (d.Attributes.Count > 0)
			{
				problems.AddRange(Hw.CheckSmartAttributes(d.Attributes, d.AttrNorm));
			}
			if (problems.Count > 0)
			{
				r.Fail(string.Join("; ", problems.ToArray()));
			}
			else if (warnings.Count > 0)
			{
				r.Message = string.Join("; ", warnings.ToArray());
				r.Slow = true;
			}
			else
			{
				List<string> parts = new List<string>();
				if (d.MediaType.Length > 0)
				{
					parts.Add(d.MediaType);
				}
				if (d.Model.Length > 0)
				{
					parts.Add(d.Model);
				}
				if (d.LifeLeft >= 0)
				{
					parts.Add("ресурс " + d.LifeLeft + "%");
				}
				else if (d.Wear >= 0)
				{
					parts.Add("износ " + d.Wear + "%");
				}
				if (d.Temperature >= 0)
				{
					parts.Add(d.Temperature + " °C");
				}
				if (d.PowerOnHours > 0)
				{
					parts.Add(d.PowerOnHours + " ч");
				}
				if (parts.Count == 0)
				{
					parts.Add("OK");
				}
				r.Message = string.Join(", ", parts.ToArray());
			}
			list.Add(r);
		}
		return list;
	}

	private static CollectResult Make(Company c, Source s, string job)
	{
		CollectResult r = new CollectResult();
		r.CompanyId = c.Id;
		r.CompanyName = c.Name;
		r.SourceId = s.Id;
		r.Kind = "smart";
		r.Job = job;
		r.IsUptime = false;
		r.IsHealth = true;
		r.Reachable = true;
		return r;
	}

	private static string DiskLabel(Hw.DiskInfo d, List<Hw.DiskInfo> all)
	{
		string name = (string.IsNullOrEmpty(d.Model) ? ("диск " + d.DeviceId) : d.Model);
		int same = 0;
		int index = 0;
		foreach (Hw.DiskInfo x in all)
		{
			if (string.Equals(x.Model, d.Model, StringComparison.OrdinalIgnoreCase))
			{
				same++;
				if (object.ReferenceEquals(x, d))
				{
					index = same;
				}
			}
		}
		if (same > 1)
		{
			return name + " #" + index;
		}
		return name;
	}

	public static Hw.SmartScan ReadDisks(bool useSmartctl, string deviceType)
	{
		Hw.SmartScan scan = new Hw.SmartScan();
		if (useSmartctl)
		{
			string path = Hw.FindTool("smartctl");
			if (path.Length > 0)
			{
				try
				{
					scan = ReadSmartctl(path, deviceType);
					if (scan.Disks.Count > 0)
					{
						return scan;
					}
				}
				catch (Exception ex)
				{
					Log.Debug("smartctl: " + ex.Message);
				}
			}
			else
			{
				scan.Note = "smartctl не найден — данные только из WMI (обычно без дисков за RAID). Установите: tools\\install-smartmontools.ps1";
			}
		}
		Hw.SmartScan wmi = ReadWmi();
		if (wmi.Disks.Count > 0)
		{
			if (scan.Note.Length > 0)
			{
				wmi.Note = scan.Note;
			}
			return wmi;
		}
		if (scan.Note.Length == 0)
		{
			scan.Note = "диски не обнаружены: smartctl не установлен, WMI пуст";
		}
		return scan;
	}

	private static Hw.SmartScan ReadSmartctl(string path, string deviceType)
	{
		Hw.SmartScan scan = new Hw.SmartScan();
		Hw.ExecResult r = Hw.Run(path, "--scan-open -j", 30);
		if (r.TimedOut || r.Output.Length < 10)
		{
			scan.Note = "smartctl: не удалось перечислить устройства";
			return scan;
		}
		Dictionary<string, object> root;
		try
		{
			root = Json.ParseObject(r.Output);
		}
		catch
		{
			scan.Note = "smartctl: не удалось разобрать JSON сканирования";
			return scan;
		}
		List<object> devices = Json.AsArray(Json.Get(root, "devices"));
		if (devices.Count == 0)
		{
			scan.Note = "smartctl: устройства не найдены (нужны права администратора)";
			return scan;
		}
		foreach (object o in devices)
		{
			Dictionary<string, object> dev = Json.AsObject(o);
			string name = Json.GetStr(dev, "name", "");
			string dtype = Json.GetStr(dev, "type", "");
			string proto = Json.GetStr(dev, "protocol", "");
			if (name.Length == 0)
			{
				continue;
			}
			string args = "-a -j";
			if (deviceType.Length > 0)
			{
				args = args + " -d " + deviceType;
			}
			else if (dtype.Length > 0)
			{
				args = args + " -d " + dtype;
			}
			args = args + " " + name;
			Hw.ExecResult dr = Hw.Run(path, args, 40);
			if (!dr.TimedOut && dr.Output.Length >= 20)
			{
				Dictionary<string, object> devRoot;
				try
				{
					devRoot = Json.ParseObject(dr.Output);
				}
				catch
				{
					continue;
				}
				if (Json.Get(devRoot, "smart_status") != null || Json.Get(devRoot, "ata_smart_attributes") != null || Json.Get(devRoot, "nvme_smart_health_information_log") != null)
				{
					Hw.DiskInfo d = new Hw.DiskInfo();
					d.DeviceId = name;
					d.MediaType = "";
					ApplySmartctlJson(d, devRoot, proto);
					scan.Disks.Add(d);
				}
			}
		}
		if (scan.Disks.Count == 0)
		{
			scan.Note = "smartctl: SMART недоступен ни на одном устройстве (контроллер не поддерживает проброс или нет прав)";
		}
		return scan;
	}

	private static void ApplySmartctlJson(Hw.DiskInfo d, Dictionary<string, object> root, string scanProtocol)
	{
		string model = Json.GetStr(root, "model_name", "");
		if (model.Length == 0)
		{
			model = Json.GetStr(root, "scsi_model_name", "");
		}
		if (model.Length == 0)
		{
			model = Json.GetStr(root, "device_model", "");
		}
		if (model.Length > 0)
		{
			d.Model = model;
		}
		string sn = Json.GetStr(root, "serial_number", "");
		if (sn.Length > 0)
		{
			d.Serial = sn;
		}
		d.SizeBytes = Json.Long(Json.Get(root, "user_capacity.bytes"), 0L);
		long rotation = Json.Long(Json.Get(root, "rotation_rate"), -1L);
		if (rotation == 0)
		{
			d.MediaType = "SSD";
		}
		else if (rotation > 0)
		{
			d.MediaType = "HDD";
		}
		string proto = Json.GetStr(root, "device.protocol", scanProtocol);
		if (proto == "NVMe")
		{
			d.MediaType = "NVMe";
		}
		if (d.MediaType.Length == 0 && proto == "ATA")
		{
			d.MediaType = "HDD";
		}
		Dictionary<string, object> status = Json.AsObject(Json.Get(root, "smart_status"));
		if (status.Count > 0)
		{
			bool passed = Json.Bool(Json.Get(status, "passed"), dflt: true);
			d.SmartStatus = (passed ? "OK" : "FAILED");
			if (!passed)
			{
				d.HealthCode = 2;
			}
		}
		long temp = Json.Long(Json.Get(root, "temperature.current"), -1L);
		if (temp > 0)
		{
			d.Temperature = (int)temp;
		}
		long hours = Json.Long(Json.Get(root, "power_on_time.hours"), -1L);
		if (hours > 0)
		{
			d.PowerOnHours = hours;
		}
		List<object> table = Json.AsArray(Json.Get(root, "ata_smart_attributes.table"));
		foreach (object o in table)
		{
			Dictionary<string, object> a = Json.AsObject(o);
			int id = Json.Int(Json.Get(a, "id"), 0);
			if (id != 0)
			{
				long rawValue = Json.Long(Json.Get(a, "raw.value"), -1L);
				if (rawValue < 0)
				{
					rawValue = 0L;
				}
				d.Attributes[id] = rawValue;
				long norm = Json.Long(Json.Get(a, "value"), -1L);
				if (norm >= 0)
				{
					d.AttrNorm[id] = norm;
				}
				if (id == 194 && d.Temperature < 0 && rawValue > 0 && rawValue < 200)
				{
					d.Temperature = (int)rawValue;
				}
				if ((id == 231 || id == 233 || id == 177) && norm >= 0 && norm <= 100)
				{
					d.LifeLeft = (int)norm;
				}
			}
		}
		Dictionary<string, object> nvme = Json.AsObject(Json.Get(root, "nvme_smart_health_information_log"));
		if (nvme.Count > 0)
		{
			long used = Json.Long(Json.Get(nvme, "percentage_used"), -1L);
			if (used >= 0)
			{
				d.Wear = (int)used;
				d.LifeLeft = (int)(100 - used);
			}
			long t = Json.Long(Json.Get(nvme, "temperature"), -1L);
			if (t > 0)
			{
				d.Temperature = (int)t;
			}
			long h = Json.Long(Json.Get(nvme, "power_on_hours"), -1L);
			if (h > 0)
			{
				d.PowerOnHours = h;
			}
			long media = Json.Long(Json.Get(nvme, "media_errors"), -1L);
			if (media > 0)
			{
				d.ReadErrors = media;
				d.Notes.Add("NVMe media_errors = " + media);
			}
			long spare = Json.Long(Json.Get(nvme, "available_spare"), -1L);
			if (spare >= 0 && spare < 100)
			{
				d.Notes.Add("available_spare = " + spare + "%");
			}
		}
	}

	private static Hw.SmartScan ReadWmi()
	{
		Hw.SmartScan scan = new Hw.SmartScan();
		Dictionary<string, Hw.DiskInfo> byDevice = new Dictionary<string, Hw.DiskInfo>(StringComparer.OrdinalIgnoreCase);
		ManagementObjectCollection physical = Hw.Query("root\\Microsoft\\Windows\\Storage", "SELECT * FROM MSFT_PhysicalDisk");
		if (physical != null)
		{
			foreach (ManagementBaseObject o in physical)
			{
				using (o)
				{
					Hw.DiskInfo d = new Hw.DiskInfo();
					d.DeviceId = Hw.Prop(o, "DeviceId");
					d.Model = Hw.Prop(o, "FriendlyName");
					d.Serial = Hw.Prop(o, "SerialNumber");
					d.SizeBytes = Hw.PropLong(o, "Size", 0L);
					d.MediaType = MediaName(Hw.PropInt(o, "MediaType", -1));
					d.HealthCode = Hw.PropInt(o, "HealthStatus", -1);
					d.Health = HealthName(d.HealthCode);
					if (d.DeviceId.Length > 0)
					{
						byDevice[d.DeviceId] = d;
					}
				}
			}
		}
		ManagementObjectCollection counters = Hw.Query("root\\Microsoft\\Windows\\Storage", "SELECT * FROM MSFT_StorageReliabilityCounter");
		if (counters != null)
		{
			foreach (ManagementBaseObject o2 in counters)
			{
				using (o2)
				{
					string dev = Hw.Prop(o2, "DeviceId");
					if (dev.Length != 0 && byDevice.TryGetValue(dev, out var d2))
					{
						int wear = Hw.PropInt(o2, "Wear", -1);
						if (wear >= 0)
						{
							d2.Wear = wear;
							d2.LifeLeft = 100 - wear;
						}
						int temp = Hw.PropInt(o2, "Temperature", -1);
						if (temp > 0)
						{
							d2.Temperature = temp;
						}
						long hours = Hw.PropLong(o2, "PowerOnHours", -1L);
						if (hours > 0)
						{
							d2.PowerOnHours = hours;
						}
						long re = Hw.PropLong(o2, "ReadErrorsTotal", -1L);
						if (re >= 0)
						{
							d2.ReadErrors = re;
						}
						long we = Hw.PropLong(o2, "WriteErrorsTotal", -1L);
						if (we >= 0)
						{
							d2.WriteErrors = we;
						}
					}
				}
			}
		}
		foreach (Hw.DiskInfo d3 in byDevice.Values)
		{
			scan.Disks.Add(d3);
		}
		if (scan.Disks.Count > 0)
		{
			ManagementObjectCollection status = Hw.Query("root\\wmi", "SELECT * FROM MSStorageDriver_FailurePredictStatus");
			if (status != null)
			{
				foreach (ManagementBaseObject o3 in status)
				{
					using (o3)
					{
						string inst = Hw.Prop(o3, "InstanceName");
						bool predict = Hw.PropBool(o3, "PredictFailure");
						Hw.DiskInfo target = MatchByInstance(scan.Disks, inst);
						if (target != null)
						{
							target.SmartStatus = (predict ? "FAILURE PREDICTED" : "OK");
						}
					}
				}
			}
			ManagementObjectCollection ata = Hw.Query("root\\wmi", "SELECT * FROM MSStorageDriver_ATAPISmartData");
			if (ata != null)
			{
				foreach (ManagementBaseObject o4 in ata)
				{
					using (o4)
					{
						string inst2 = Hw.Prop(o4, "InstanceName");
						Hw.DiskInfo target2 = MatchByInstance(scan.Disks, inst2);
						if (target2 == null)
						{
							continue;
						}
						try
						{
							if (o4["VendorSpecific"] is byte[] vs)
							{
								Hw.ParseAtaSmart(vs, target2.Attributes, target2.AttrNorm);
							}
						}
						catch
						{
						}
					}
				}
			}
		}
		return scan;
	}

	private static Hw.DiskInfo MatchByInstance(List<Hw.DiskInfo> disks, string instanceName)
	{
		if (string.IsNullOrEmpty(instanceName))
		{
			return null;
		}
		foreach (Hw.DiskInfo d in disks)
		{
			if (d.DeviceId.Length != 0 && instanceName.EndsWith("&" + d.DeviceId + "_0", StringComparison.OrdinalIgnoreCase))
			{
				return d;
			}
		}
		foreach (Hw.DiskInfo d2 in disks)
		{
			if (d2.SmartStatus.Length == 0)
			{
				return d2;
			}
		}
		if (disks.Count <= 0)
		{
			return null;
		}
		return disks[0];
	}

	private static string MediaName(int media)
	{
		return media switch
		{
			3 => "HDD", 
			4 => "SSD", 
			5 => "SCM", 
			_ => "", 
		};
	}

	private static string HealthName(int code)
	{
		switch (code)
		{
		case 0:
			return "Healthy";
		case 1:
			return "Warning";
		case 2:
			return "Unhealthy";
		case 5:
			return "Unknown";
		default:
			if (code >= 0)
			{
				return "state " + code;
			}
			return "";
		}
	}
}
