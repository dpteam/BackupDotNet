using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;

namespace BackupChecker.Core;

public class VssCollector : ICollector
{
	public class VssWriter
	{
		public string Name = "";

		public string Id = "";

		public int State = -1;

		public string StateText = "";

		public string LastError = "";
	}

	public class VssInfo
	{
		public string Error = "";

		public List<VssWriter> Writers = new List<VssWriter>();

		public int StableCount;

		public int FailedCount;
	}

	public class ShadowCopy
	{
		public string Id = "";

		public string Volume = "";

		public DateTime? Created;
	}

	public class ShadowStorage
	{
		public string Volume = "";

		public long Used;

		public long Max;
	}

	public string Type => "vss";

	public List<CollectResult> Collect(Company company, Source source, AgentCtx ctx)
	{
		List<CollectResult> list = new List<CollectResult>();
		string prefix = source.GetStr("jobName", "VSS");
		int maxShadowAge = source.GetInt("maxShadowAgeHours", 48);
		int storageWarn = source.GetInt("storageWarnPercent", 80);
		int storageFail = source.GetInt("storageFailPercent", 95);
		VssInfo info = ReadWriters();
		CollectResult w = Make(company, source, prefix + ": писатели");
		if (info.Error.Length > 0)
		{
			w.NoData = true;
			w.Message = "не удалось получить список писателей: " + info.Error;
			list.Add(w);
			return list;
		}
		if (info.Writers.Count == 0)
		{
			w.NoData = true;
			w.Message = "писатели VSS не найдены (служба VSS остановлена?)";
			list.Add(w);
			return list;
		}
		List<string> problems = new List<string>();
		List<string> warnings = new List<string>();
		foreach (VssWriter wr in info.Writers)
		{
			if (wr.State == 5 || wr.State == 6)
			{
				problems.Add(wr.Name + ": " + wr.StateText + ((wr.LastError.Length > 0) ? (" (" + wr.LastError + ")") : ""));
			}
			else if (wr.State == 7)
			{
				problems.Add(wr.Name + ": таймаут");
			}
			else if (wr.State != 1)
			{
				warnings.Add(wr.Name + ": " + wr.StateText);
			}
		}
		Dictionary<string, object> wd = Json.Obj();
		wd["count"] = info.Writers.Count;
		wd["stable"] = info.StableCount;
		wd["failed"] = info.FailedCount;
		List<object> arr = new List<object>();
		foreach (VssWriter wr2 in info.Writers)
		{
			Dictionary<string, object> x = Json.Obj();
			x["name"] = wr2.Name;
			x["state"] = wr2.State;
			x["stateText"] = wr2.StateText;
			if (wr2.LastError.Length > 0)
			{
				x["lastError"] = wr2.LastError;
			}
			if (wr2.Id.Length > 0)
			{
				x["id"] = wr2.Id;
			}
			arr.Add(x);
		}
		wd["writers"] = arr;
		w.Details = wd;
		if (problems.Count > 0)
		{
			w.Fail("проблемных писателей: " + problems.Count + " — " + string.Join("; ", problems.ToArray()));
		}
		else if (warnings.Count > 0)
		{
			w.Message = "писателей " + info.Writers.Count + ", стабильных " + info.StableCount + "; " + string.Join("; ", warnings.ToArray());
			w.Slow = true;
		}
		else
		{
			w.Message = "писателей " + info.Writers.Count + " — все стабильны";
		}
		list.Add(w);
		List<ShadowCopy> shadows = ReadShadowCopies();
		CollectResult s = Make(company, source, prefix + ": теневые копии");
		s.Target = "shadows";
		Dictionary<string, object> sd = Json.Obj();
		sd["count"] = shadows.Count;
		List<object> sarr = new List<object>();
		DateTime? oldest = null;
		DateTime? newest = null;
		foreach (ShadowCopy sc in shadows)
		{
			Dictionary<string, object> x2 = Json.Obj();
			if (sc.Id.Length > 0)
			{
				x2["id"] = sc.Id;
			}
			if (sc.Volume.Length > 0)
			{
				x2["volume"] = sc.Volume;
			}
			if (sc.Created.HasValue)
			{
				x2["created"] = sc.Created.Value;
				if (!oldest.HasValue || sc.Created.Value < oldest.Value)
				{
					oldest = sc.Created.Value;
				}
				if (!newest.HasValue || sc.Created.Value > newest.Value)
				{
					newest = sc.Created.Value;
				}
			}
			sarr.Add(x2);
		}
		sd["items"] = sarr;
		if (oldest.HasValue)
		{
			sd["oldest"] = oldest.Value;
		}
		if (newest.HasValue)
		{
			sd["newest"] = newest.Value;
		}
		s.Details = sd;
		if (shadows.Count == 0)
		{
			s.Message = "теневых копий нет";
		}
		else
		{
			List<string> sw = new List<string>();
			if (maxShadowAge > 0 && oldest.HasValue)
			{
				double ageHours = (DateTime.Now - oldest.Value).TotalHours;
				sd["oldestAgeHours"] = Math.Round(ageHours, 1);
				if (ageHours > (double)maxShadowAge)
				{
					sw.Add("самая старая копия " + (int)(ageHours / 24.0) + " дн назад (порог " + maxShadowAge + " ч) — возможно, зависла");
				}
			}
			string text = "копий: " + shadows.Count;
			if (newest.HasValue)
			{
				text = text + ", свежая " + FmtAge(newest.Value);
			}
			if (sw.Count > 0)
			{
				s.Message = text + "; " + string.Join("; ", sw.ToArray());
				s.Slow = true;
			}
			else
			{
				s.Message = text;
			}
		}
		list.Add(s);
		List<ShadowStorage> storages = ReadShadowStorage();
		if (storages.Count > 0)
		{
			CollectResult st = Make(company, source, prefix + ": хранилище");
			st.Target = "storage";
			List<string> stProblems = new List<string>();
			List<string> stWarnings = new List<string>();
			Dictionary<string, object> std = Json.Obj();
			List<object> stArr = new List<object>();
			foreach (ShadowStorage ss in storages)
			{
				Dictionary<string, object> x3 = Json.Obj();
				x3["volume"] = ss.Volume;
				x3["usedBytes"] = ss.Used;
				x3["maxBytes"] = ss.Max;
				double percent = -1.0;
				if (ss.Max > 0)
				{
					percent = (double)ss.Used * 100.0 / (double)ss.Max;
					x3["usedPercent"] = Math.Round(percent, 1);
				}
				stArr.Add(x3);
				string label = ss.Volume;
				if (percent >= 0.0 && storageFail > 0 && percent >= (double)storageFail)
				{
					stProblems.Add(label + ": занято " + (int)percent + "% (порог " + storageFail + "%)");
				}
				else if (percent >= 0.0 && storageWarn > 0 && percent >= (double)storageWarn)
				{
					stWarnings.Add(label + ": занято " + (int)percent + "%");
				}
			}
			std["items"] = stArr;
			st.Details = std;
			if (stProblems.Count > 0)
			{
				st.Fail(string.Join("; ", stProblems.ToArray()));
			}
			else if (stWarnings.Count > 0)
			{
				st.Message = string.Join("; ", stWarnings.ToArray());
				st.Slow = true;
			}
			else
			{
				List<string> parts = new List<string>();
				foreach (ShadowStorage ss2 in storages)
				{
					if (ss2.Max > 0)
					{
						parts.Add(ss2.Volume + ": " + Fmt.Size(ss2.Used) + " из " + Fmt.Size(ss2.Max));
					}
					else
					{
						parts.Add(ss2.Volume + ": " + Fmt.Size(ss2.Used) + " (без лимита)");
					}
				}
				st.Message = string.Join(", ", parts.ToArray());
			}
			list.Add(st);
		}
		return list;
	}

	private static CollectResult Make(Company c, Source s, string job)
	{
		CollectResult r = new CollectResult();
		r.CompanyId = c.Id;
		r.CompanyName = c.Name;
		r.SourceId = s.Id;
		r.Kind = "vss";
		r.Job = job;
		r.IsHealth = true;
		r.Reachable = true;
		return r;
	}

	private static string FmtAge(DateTime dt)
	{
		double hours = (DateTime.Now - dt).TotalHours;
		if (hours < 1.0)
		{
			return (int)(hours * 60.0) + " мин назад";
		}
		if (hours < 48.0)
		{
			return (int)hours + " ч назад";
		}
		return (int)(hours / 24.0) + " дн назад";
	}

	private static string FmtSize(long bytes)
	{
		if (bytes <= 0)
		{
			return "0";
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

	public static VssInfo ReadWriters()
	{
		VssInfo info = new VssInfo();
		Hw.ExecResult r = Hw.RunExe("vssadmin.exe", "list writers", 60, Encoding.GetEncoding(866));
		if (r.ExitCode != 0 || string.IsNullOrEmpty(r.Output))
		{
			info.Error = ((r.Error.Length > 0) ? r.Error : "vssadmin не вернул данных");
			return info;
		}
		string output = r.Output;
		if (output.IndexOf("You do not have", StringComparison.OrdinalIgnoreCase) >= 0 || output.IndexOf("does not have the necessary", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			info.Error = "нужны права администратора";
			return info;
		}
		VssWriter cur = null;
		string[] array = output.Replace("\r\n", "\n").Split('\n');
		foreach (string raw in array)
		{
			string line = raw.TrimEnd();
			if (line.Trim().Length == 0)
			{
				continue;
			}
			Match nameMatch = Regex.Match(line, "\"([^\"]+)\"");
			if (nameMatch.Success && (cur == null || cur.State >= 0))
			{
				cur = new VssWriter();
				cur.Name = nameMatch.Groups[1].Value;
				info.Writers.Add(cur);
			}
			else
			{
				if (cur == null)
				{
					continue;
				}
				if (cur.Id.Length == 0)
				{
					Match idMatch = Regex.Match(line, "(\\{[0-9a-fA-F-]{36}\\})");
					if (idMatch.Success)
					{
						cur.Id = idMatch.Groups[1].Value;
						continue;
					}
				}
				if (cur.State >= 0)
				{
					continue;
				}
				Match stMatch = Regex.Match(line, "\\[(\\d+)\\]");
				if (stMatch.Success && int.TryParse(stMatch.Groups[1].Value, out var st))
				{
					cur.State = st;
					cur.StateText = StateName(st);
					switch (st)
					{
					case 1:
						info.StableCount++;
						break;
					case 5:
					case 6:
					case 7:
						info.FailedCount++;
						break;
					}
				}
			}
		}
		return info;
	}

	public static string StateName(int state)
	{
		return state switch
		{
			1 => "Stable", 
			2 => "Waiting for freeze", 
			3 => "In progress", 
			4 => "Waiting for thaw", 
			5 => "Failed", 
			6 => "Failed with error", 
			7 => "Timeout", 
			8 => "Reverted", 
			_ => "state " + state, 
		};
	}

	public static List<ShadowCopy> ReadShadowCopies()
	{
		List<ShadowCopy> result = new List<ShadowCopy>();
		ManagementObjectCollection col = Hw.Query("root\\cimv2", "SELECT ID, VolumeName, InstallDate FROM Win32_ShadowCopy");
		if (col == null)
		{
			return result;
		}
		foreach (ManagementBaseObject o in col)
		{
			using (o)
			{
				ShadowCopy sc = new ShadowCopy();
				sc.Id = Hw.Prop(o, "ID");
				sc.Volume = Hw.Prop(o, "VolumeName");
				DateTime? dt = Hw.ParseWmiDate(o["InstallDate"]);
				if (dt.HasValue)
				{
					sc.Created = dt.Value.ToLocalTime();
				}
				result.Add(sc);
			}
		}
		result.Sort((ShadowCopy a, ShadowCopy b) =>
		{
			if (!a.Created.HasValue)
			{
				return 1;
			}
			return (!b.Created.HasValue) ? (-1) : b.Created.Value.CompareTo(a.Created.Value);
		});
		return result;
	}

	public static List<ShadowStorage> ReadShadowStorage()
	{
		List<ShadowStorage> result = new List<ShadowStorage>();
		ManagementObjectCollection col = Hw.Query("root\\cimv2", "SELECT Volume, UsedSpace, MaxSpace FROM Win32_ShadowStorage");
		if (col == null)
		{
			return result;
		}
		foreach (ManagementBaseObject o in col)
		{
			using (o)
			{
				ShadowStorage ss = new ShadowStorage();
				ss.Volume = Hw.Prop(o, "Volume");
				ss.Used = Hw.PropLong(o, "UsedSpace", 0L);
				ss.Max = Hw.PropLong(o, "MaxSpace", 0L);
				if (ss.Volume.Length > 0)
				{
					result.Add(ss);
				}
			}
		}
		return result;
	}
}
