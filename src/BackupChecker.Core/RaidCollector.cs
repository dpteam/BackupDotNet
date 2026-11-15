using System;
using System.Collections.Generic;
using System.Management;
using System.Text.RegularExpressions;

namespace BackupChecker.Core;

public class RaidCollector : ICollector
{
	public string Type => "raid";

	public List<CollectResult> Collect(Company company, Source source, AgentCtx ctx)
	{
		List<CollectResult> list = new List<CollectResult>();
		string job = source.GetStr("jobName", "RAID");
		CollectResult r = Make(company, source, job);
		Hw.RaidInfo info = ReadRaid();
		if (info == null)
		{
			r.Reachable = true;
			r.NoData = true;
			r.Message = "RAID-контроллер не найден (нет утилиты или контроллера)";
			list.Add(r);
			return list;
		}
		r.Target = info.Vendor + " " + info.Controller;
		List<string> problems = new List<string>();
		List<string> warnings = new List<string>();
		if (!IsGoodState(info.State))
		{
			if (IsFailedState(info.State))
			{
				problems.Add("контроллер: " + info.State);
			}
			else
			{
				warnings.Add("контроллер: " + info.State);
			}
		}
		foreach (Hw.RaidVolume v in info.Volumes)
		{
			string label = "VD " + v.Id + ((v.Raid.Length > 0) ? (" (" + v.Raid + ")") : "");
			if (IsFailedState(v.State))
			{
				problems.Add(label + ": " + v.State);
			}
			else if (!IsGoodState(v.State))
			{
				warnings.Add(label + ": " + v.State);
			}
		}
		foreach (Hw.RaidDisk d in info.Disks)
		{
			string label2 = "диск " + d.Slot;
			if (IsFailedState(d.State))
			{
				problems.Add(label2 + ": " + d.State);
			}
			else if (!IsGoodState(d.State))
			{
				warnings.Add(label2 + ": " + d.State);
			}
		}
		if (info.Battery.Length > 0 && !IsGoodState(info.Battery))
		{
			if (IsFailedState(info.Battery))
			{
				problems.Add("батарея: " + info.Battery);
			}
			else
			{
				warnings.Add("батарея: " + info.Battery);
			}
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
			parts.Add(info.Vendor);
			if (info.Volumes.Count > 0)
			{
				parts.Add("томов: " + info.Volumes.Count);
			}
			if (info.Disks.Count > 0)
			{
				parts.Add("дисков: " + info.Disks.Count);
			}
			if (info.Battery.Length > 0)
			{
				parts.Add("батарея: " + info.Battery);
			}
			r.Message = string.Join(", ", parts.ToArray());
		}
		list.Add(r);
		return list;
	}

	private static CollectResult Make(Company c, Source s, string job)
	{
		CollectResult r = new CollectResult();
		r.CompanyId = c.Id;
		r.CompanyName = c.Name;
		r.SourceId = s.Id;
		r.Kind = "raid";
		r.Job = job;
		r.IsHealth = true;
		r.Reachable = true;
		return r;
	}

	private static bool IsGoodState(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return true;
		}
		string l = s.ToLowerInvariant();
		switch (l)
		{
		default:
			return l == "yes";
		case "optimal":
		case "ok":
		case "normal":
		case "online":
		case "ready":
		case "active":
		case "healthy":
		case "success":
		case "enabled":
		case "good":
			return true;
		}
	}

	private static bool IsFailedState(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return false;
		}
		string l = s.ToLowerInvariant();
		if (!l.Contains("degraded") && !l.Contains("failed") && !l.Contains("offline") && !l.Contains("missing") && !l.Contains("critical") && !l.Contains("bad") && !l.Contains("error") && !l.Contains("dead") && !l.Contains("removed") && !l.Contains("no "))
		{
			return l.Contains("not ");
		}
		return true;
	}

	public static Hw.RaidInfo ReadRaid()
	{
		string[] array = new string[2] { "storcli", "perccli" };
		foreach (string tool in array)
		{
			string path = Hw.FindTool(tool);
			if (path.Length != 0)
			{
				Hw.RaidInfo info = TryStorCli(path, tool);
				if (info != null)
				{
					return info;
				}
			}
		}
		string[] array2 = new string[2] { "ssacli", "hpssacli" };
		foreach (string tool2 in array2)
		{
			string path2 = Hw.FindTool(tool2);
			if (path2.Length != 0)
			{
				Hw.RaidInfo info2 = TrySsaCli(path2);
				if (info2 != null)
				{
					return info2;
				}
			}
		}
		string arcconf = Hw.FindTool("arcconf");
		if (arcconf.Length > 0)
		{
			Hw.RaidInfo info3 = TryArcconf(arcconf);
			if (info3 != null)
			{
				return info3;
			}
		}
		string megacli = Hw.FindTool("MegaCli64");
		if (megacli.Length == 0)
		{
			megacli = Hw.FindTool("MegaCli");
		}
		if (megacli.Length > 0)
		{
			Hw.RaidInfo info4 = TryMegaCli(megacli);
			if (info4 != null)
			{
				return info4;
			}
		}
		Hw.RaidInfo wmi = TryWmi();
		if (wmi != null)
		{
			return wmi;
		}
		return null;
	}

	private static Hw.RaidInfo TryWmi()
	{
		Hw.RaidInfo info = new Hw.RaidInfo();
		info.Vendor = "Windows Storage";
		info.Controller = "программный уровень";
		ManagementObjectCollection disks = Hw.Query("root\\Microsoft\\Windows\\Storage", "SELECT * FROM MSFT_PhysicalDisk");
		if (disks == null)
		{
			return null;
		}
		int count = 0;
		foreach (ManagementBaseObject o in disks)
		{
			using (o)
			{
				Hw.Prop(o, "MediaType");
				string name = Hw.Prop(o, "FriendlyName");
				int health = Hw.PropInt(o, "HealthStatus", -1);
				int op = Hw.PropInt(o, "OperationalStatus", -1);
				int bus = Hw.PropInt(o, "BusType", -1);
				if (bus == 17 || name.ToLowerInvariant().Contains("raid"))
				{
					count++;
					Hw.RaidVolume v = new Hw.RaidVolume();
					v.Id = Hw.Prop(o, "DeviceId");
					v.Name = name;
					v.State = health switch
					{
						1 => "Warning", 
						0 => "Healthy", 
						_ => "Unhealthy", 
					};
					v.SizeBytes = Hw.PropLong(o, "Size", 0L);
					if (op > 0 && op != 0)
					{
						v.State = v.State + " / op=" + op;
					}
					info.Volumes.Add(v);
				}
			}
		}
		if (count == 0)
		{
			return null;
		}
		info.Notes.Add("состояние томов из Windows Storage API; диски за контроллером не видны — нужна вендорная утилита");
		return info;
	}

	private static Hw.RaidInfo TryStorCli(string path, string toolName)
	{
		Hw.RaidInfo info = new Hw.RaidInfo();
		info.Vendor = ((toolName == "perccli") ? "Dell PERC" : "LSI/Broadcom");
		info.Controller = "c0";
		bool anyData = false;
		Hw.ExecResult vd = Hw.Run(path, "/c0 /vall show J", 30);
		if (!vd.TimedOut && vd.Output.Length > 10)
		{
			try
			{
				anyData |= ParseStorCliVolumes(vd.Output, info);
			}
			catch (Exception ex)
			{
				Log.Debug("storcli vd: " + ex.Message);
			}
		}
		Hw.ExecResult pd = Hw.Run(path, "/c0 /eall /sall show J", 30);
		if (!pd.TimedOut && pd.Output.Length > 10)
		{
			try
			{
				anyData |= ParseStorCliDisks(pd.Output, info);
			}
			catch (Exception ex2)
			{
				Log.Debug("storcli pd: " + ex2.Message);
			}
		}
		Hw.ExecResult ctrl = Hw.Run(path, "/c0 show J", 30);
		if (!ctrl.TimedOut && ctrl.Output.Length > 10)
		{
			try
			{
				anyData |= ParseStorCliController(ctrl.Output, info);
			}
			catch (Exception ex3)
			{
				Log.Debug("storcli ctrl: " + ex3.Message);
			}
		}
		Hw.ExecResult bb = Hw.Run(path, "/c0 /bbu show J", 30);
		if (!bb.TimedOut && bb.Output.Length > 10)
		{
			try
			{
				ParseStorCliBattery(bb.Output, info);
			}
			catch
			{
			}
		}
		if (!anyData)
		{
			return null;
		}
		return info;
	}

	private static bool ParseStorCliVolumes(string json, Hw.RaidInfo info)
	{
		Dictionary<string, object> root = Json.ParseObject(json);
		bool found = false;
		foreach (object c in Json.AsArray(Json.Get(root, "Controllers")))
		{
			Dictionary<string, object> ctrl = Json.AsObject(c);
			List<object> list = Json.AsArray(Json.Get(ctrl, "VD LIST"));
			if (list.Count == 0)
			{
				list = Json.AsArray(Json.Get(ctrl, "Virtual Drives"));
			}
			foreach (object o in list)
			{
				Dictionary<string, object> vd = Json.AsObject(o);
				Hw.RaidVolume v = new Hw.RaidVolume();
				v.Id = Json.GetStr(vd, "DG/VD", Json.GetStr(vd, "VD", "0"));
				v.State = Json.GetStr(vd, "State", Json.GetStr(vd, "Status", ""));
				v.Raid = Json.GetStr(vd, "TYPE", Json.GetStr(vd, "RAID", ""));
				v.Name = Json.GetStr(vd, "Name", "");
				info.Volumes.Add(v);
				found = true;
			}
		}
		return found;
	}

	private static bool ParseStorCliDisks(string json, Hw.RaidInfo info)
	{
		Dictionary<string, object> root = Json.ParseObject(json);
		bool found = false;
		foreach (object c in Json.AsArray(Json.Get(root, "Controllers")))
		{
			Dictionary<string, object> ctrl = Json.AsObject(c);
			List<object> list = Json.AsArray(Json.Get(ctrl, "Drive Information"));
			foreach (object o in list)
			{
				Dictionary<string, object> d = Json.AsObject(o);
				Hw.RaidDisk disk = new Hw.RaidDisk();
				disk.Slot = Json.GetStr(d, "EID:Slt", Json.GetStr(d, "DID", "?"));
				disk.State = Json.GetStr(d, "State", "");
				disk.Model = Json.GetStr(d, "Model", "");
				disk.Size = Json.GetStr(d, "Size", "");
				info.Disks.Add(disk);
				found = true;
			}
		}
		return found;
	}

	private static bool ParseStorCliController(string json, Hw.RaidInfo info)
	{
		Dictionary<string, object> root = Json.ParseObject(json);
		bool found = false;
		foreach (object c in Json.AsArray(Json.Get(root, "Controllers")))
		{
			Dictionary<string, object> ctrl = Json.AsObject(c);
			string model = Json.GetStr(ctrl, "Product Name", Json.GetStr(ctrl, "Model", ""));
			if (model.Length > 0)
			{
				info.Controller = model;
			}
			string status = Json.GetStr(ctrl, "Controller Status", Json.GetStr(ctrl, "Status", ""));
			if (status.Length > 0)
			{
				info.State = status;
				found = true;
			}
		}
		return found;
	}

	private static void ParseStorCliBattery(string json, Hw.RaidInfo info)
	{
		Dictionary<string, object> root = Json.ParseObject(json);
		foreach (object c in Json.AsArray(Json.Get(root, "Controllers")))
		{
			Dictionary<string, object> ctrl = Json.AsObject(c);
			List<object> list = Json.AsArray(Json.Get(ctrl, "BBU_Info"));
			foreach (object o in list)
			{
				Dictionary<string, object> bb = Json.AsObject(o);
				string state = Json.GetStr(bb, "State", "");
				if (state.Length > 0)
				{
					info.Battery = state;
					return;
				}
			}
		}
	}

	private static Hw.RaidInfo TrySsaCli(string path)
	{
		Hw.ExecResult r = Hw.Run(path, "ctrl all show config", 30);
		if (r.TimedOut || r.Output.Length < 10)
		{
			return null;
		}
		Hw.RaidInfo info = new Hw.RaidInfo();
		info.Vendor = "HPE Smart Array";
		string[] lines = r.Output.Replace("\r\n", "\n").Split('\n');
		string[] array = lines;
		foreach (string raw in array)
		{
			string line = raw.Trim();
			if (line.Length == 0)
			{
				continue;
			}
			if (line.StartsWith("Smart Array", StringComparison.OrdinalIgnoreCase) || line.StartsWith("Smart HBA", StringComparison.OrdinalIgnoreCase))
			{
				info.Controller = line;
				continue;
			}
			Match ld = Regex.Match(line, "^logicaldrive\\s+(\\d+)\\s*\\((.*?)\\)", RegexOptions.IgnoreCase);
			if (ld.Success)
			{
				Hw.RaidVolume v = new Hw.RaidVolume();
				v.Id = ld.Groups[1].Value;
				string[] parts = ld.Groups[2].Value.Split(',');
				if (parts.Length >= 2)
				{
					v.Raid = parts[1].Trim();
				}
				if (parts.Length >= 3)
				{
					v.State = parts[2].Trim();
				}
				info.Volumes.Add(v);
				continue;
			}
			Match pd = Regex.Match(line, "^physicaldrive\\s+(\\S+)\\s*\\((.*?)\\)", RegexOptions.IgnoreCase);
			if (pd.Success)
			{
				Hw.RaidDisk d = new Hw.RaidDisk();
				d.Slot = pd.Groups[1].Value;
				string[] parts2 = pd.Groups[2].Value.Split(',');
				if (parts2.Length >= 1)
				{
					d.Model = parts2[(parts2.Length - 3 >= 0) ? (parts2.Length - 3) : 0].Trim();
				}
				if (parts2.Length >= 1)
				{
					d.State = parts2[parts2.Length - 1].Trim();
				}
				if (parts2.Length >= 2)
				{
					d.Size = parts2[parts2.Length - 2].Trim();
				}
				info.Disks.Add(d);
			}
		}
		if (info.Volumes.Count <= 0 && info.Disks.Count <= 0)
		{
			return null;
		}
		return info;
	}

	private static Hw.RaidInfo TryArcconf(string path)
	{
		Hw.ExecResult r = Hw.Run(path, "GETCONFIG 1", 40);
		if (r.TimedOut || r.Output.Length < 10)
		{
			return null;
		}
		Hw.RaidInfo info = new Hw.RaidInfo();
		info.Vendor = "Adaptec";
		string[] lines = r.Output.Replace("\r\n", "\n").Split('\n');
		Hw.RaidVolume current = null;
		string[] array = lines;
		foreach (string raw in array)
		{
			string line = raw.Trim();
			if (line.Length == 0)
			{
				continue;
			}
			if (line.StartsWith("Controller Model", StringComparison.OrdinalIgnoreCase))
			{
				int idx = line.IndexOf(':');
				if (idx > 0)
				{
					info.Controller = line.Substring(idx + 1).Trim();
				}
			}
			if (line.StartsWith("Logical Device number", StringComparison.OrdinalIgnoreCase))
			{
				current = new Hw.RaidVolume();
				int idx2 = line.IndexOf(':');
				if (idx2 > 0)
				{
					current.Id = line.Substring(idx2 + 1).Trim();
				}
				info.Volumes.Add(current);
				continue;
			}
			if (current != null)
			{
				if (line.StartsWith("Logical Device name", StringComparison.OrdinalIgnoreCase))
				{
					int idx3 = line.IndexOf(':');
					if (idx3 > 0)
					{
						current.Name = line.Substring(idx3 + 1).Trim();
					}
				}
				else if (line.StartsWith("RAID level", StringComparison.OrdinalIgnoreCase))
				{
					int idx4 = line.IndexOf(':');
					if (idx4 > 0)
					{
						current.Raid = line.Substring(idx4 + 1).Trim();
					}
				}
				else if (line.StartsWith("Status of Logical Device", StringComparison.OrdinalIgnoreCase))
				{
					int idx5 = line.IndexOf(':');
					if (idx5 > 0)
					{
						current.State = line.Substring(idx5 + 1).Trim();
					}
				}
			}
			Match dev = Regex.Match(line, "Device #(\\d+)\\s+(.*)", RegexOptions.IgnoreCase);
			if (dev.Success)
			{
				Hw.RaidDisk d = new Hw.RaidDisk();
				d.Slot = dev.Groups[1].Value;
				d.Model = dev.Groups[2].Value.Trim();
				info.Disks.Add(d);
			}
			Match st = Regex.Match(line, "^\\s*State\\s*:\\s*(.+)$", RegexOptions.IgnoreCase);
			if (st.Success && info.Disks.Count > 0)
			{
				Hw.RaidDisk last = info.Disks[info.Disks.Count - 1];
				if (last.State.Length == 0)
				{
					last.State = st.Groups[1].Value.Trim();
				}
			}
		}
		if (info.Volumes.Count <= 0 && info.Disks.Count <= 0)
		{
			return null;
		}
		return info;
	}

	private static Hw.RaidInfo TryMegaCli(string path)
	{
		Hw.ExecResult r = Hw.Run(path, "-LDInfo -Lall -aALL", 40);
		if (r.TimedOut || r.Output.Length < 10)
		{
			return null;
		}
		Hw.RaidInfo info = new Hw.RaidInfo();
		info.Vendor = "LSI MegaRAID";
		string[] lines = r.Output.Replace("\r\n", "\n").Split('\n');
		Hw.RaidVolume current = null;
		string[] array = lines;
		foreach (string raw in array)
		{
			string line = raw.Trim();
			if (line.Length == 0)
			{
				continue;
			}
			Match ld = Regex.Match(line, "Virtual Drive:\\s*(\\d+)", RegexOptions.IgnoreCase);
			if (ld.Success)
			{
				current = new Hw.RaidVolume();
				current.Id = ld.Groups[1].Value;
				info.Volumes.Add(current);
			}
			else
			{
				if (current == null)
				{
					continue;
				}
				if (line.StartsWith("RAID Level", StringComparison.OrdinalIgnoreCase))
				{
					int idx = line.IndexOf(':');
					if (idx > 0)
					{
						current.Raid = line.Substring(idx + 1).Trim();
					}
				}
				else if (line.StartsWith("State", StringComparison.OrdinalIgnoreCase))
				{
					int idx2 = line.IndexOf(':');
					if (idx2 > 0)
					{
						current.State = line.Substring(idx2 + 1).Trim();
					}
				}
			}
		}
		Hw.ExecResult pd = Hw.Run(path, "-PDList -aALL", 40);
		if (!pd.TimedOut && pd.Output.Length > 10)
		{
			string[] plines = pd.Output.Replace("\r\n", "\n").Split('\n');
			Hw.RaidDisk disk = null;
			string[] array2 = plines;
			foreach (string raw2 in array2)
			{
				string line2 = raw2.Trim();
				if (line2.StartsWith("Enclosure Device ID", StringComparison.OrdinalIgnoreCase) || line2.StartsWith("Slot Number", StringComparison.OrdinalIgnoreCase))
				{
					if (disk != null)
					{
						info.Disks.Add(disk);
						disk = null;
					}
					if (disk == null)
					{
						disk = new Hw.RaidDisk();
					}
					int idx3 = line2.IndexOf(':');
					if (idx3 > 0 && line2.StartsWith("Slot", StringComparison.OrdinalIgnoreCase))
					{
						disk.Slot = line2.Substring(idx3 + 1).Trim();
					}
				}
				else if (disk != null && line2.StartsWith("Firmware state", StringComparison.OrdinalIgnoreCase))
				{
					int idx4 = line2.IndexOf(':');
					if (idx4 > 0)
					{
						disk.State = line2.Substring(idx4 + 1).Trim();
					}
				}
			}
			if (disk != null)
			{
				info.Disks.Add(disk);
			}
		}
		if (info.Volumes.Count <= 0)
		{
			return null;
		}
		return info;
	}
}
