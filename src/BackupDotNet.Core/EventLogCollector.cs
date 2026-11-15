using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;

namespace BackupDotNet.Core;

public class EventLogCollector : ICollector
{
	private class EventItem
	{
		public string Log = "";

		public string Source = "";

		public int Id;

		public string Level = "";

		public DateTime Time;

		public string Message = "";
	}

	private class EventGroup
	{
		public string Source = "";

		public int Id;

		public string Level = "";

		public int Count;

		public DateTime Last;
	}

	public static readonly string[] DefaultIgnore = new string[5] { "SideBySide/59", "Microsoft-Windows-DistributedCOM/10010", "Microsoft-Windows-DistributedCOM/10016", "Perflib/1008", "Perflib/1020" };

	public string Type => "eventlog";

	public List<CollectResult> Collect(Company company, Source source, AgentCtx ctx)
	{
		List<CollectResult> list = new List<CollectResult>();
		string prefix = source.GetStr("jobName", "Журнал");
		int hours = source.GetInt("hours", 24);
		if (hours < 1)
		{
			hours = 1;
		}
		if (hours > 168)
		{
			hours = 168;
		}
		int warnCount = source.GetInt("warnCount", 5);
		int failCount = source.GetInt("failCount", 30);
		int maxEvents = source.GetInt("maxEvents", 500);
		if (maxEvents < 50)
		{
			maxEvents = 50;
		}
		if (maxEvents > 5000)
		{
			maxEvents = 5000;
		}
		bool includeWarnings = source.GetBool("includeWarnings", dflt: false);
		List<string> logs = StrList(source, "logs");
		if (logs.Count == 0)
		{
			logs.Add("System");
			logs.Add("Application");
		}
		List<string> ignore = StrList(source, "ignore");
		if (!source.Raw.ContainsKey("ignore"))
		{
			ignore.AddRange(DefaultIgnore);
		}
		CollectResult r = new CollectResult();
		r.CompanyId = company.Id;
		r.CompanyName = company.Name;
		r.SourceId = source.Id;
		r.Kind = "eventlog";
		r.Job = prefix + ": ошибки";
		r.IsHealth = true;
		r.Reachable = true;
		List<EventItem> events = new List<EventItem>();
		List<string> logErrors = new List<string>();
		List<string> okLogs = new List<string>();
		bool truncated = false;
		foreach (string log in logs)
		{
			int before = events.Count;
			string err = ReadLog(log, hours, includeWarnings, maxEvents, events, out var cut);
			if (err.Length > 0)
			{
				logErrors.Add(log + ": " + err);
				continue;
			}
			okLogs.Add(log);
			if (cut)
			{
				truncated = true;
			}
			if (events.Count == before && cut)
			{
				truncated = true;
			}
		}
		if (okLogs.Count == 0)
		{
			r.NoData = true;
			r.Message = "не удалось прочитать журналы — " + string.Join("; ", logErrors.ToArray());
			list.Add(r);
			return list;
		}
		List<EventItem> kept = new List<EventItem>();
		int ignored = 0;
		foreach (EventItem e in events)
		{
			if (IsIgnored(e, ignore))
			{
				ignored++;
			}
			else
			{
				kept.Add(e);
			}
		}
		kept.Sort((EventItem a, EventItem b) => b.Time.CompareTo(a.Time));
		Dictionary<string, EventGroup> groups = new Dictionary<string, EventGroup>();
		List<string> order = new List<string>();
		foreach (EventItem e2 in kept)
		{
			string key = e2.Source + "/" + e2.Id;
			if (!groups.TryGetValue(key, out var g))
			{
				g = new EventGroup();
				g.Source = e2.Source;
				g.Id = e2.Id;
				g.Level = e2.Level;
				g.Count = 0;
				g.Last = e2.Time;
				groups[key] = g;
				order.Add(key);
			}
			g.Count++;
			if (e2.Time > g.Last)
			{
				g.Last = e2.Time;
			}
		}
		order.Sort((string a, string b) => groups[b].Count.CompareTo(groups[a].Count));
		Dictionary<string, object> d = Json.Obj();
		d["count"] = kept.Count;
		d["ignored"] = ignored;
		d["hours"] = hours;
		d["logs"] = ToObjList(okLogs);
		d["truncated"] = truncated;
		if (logErrors.Count > 0)
		{
			d["logErrors"] = ToObjList(logErrors);
		}
		List<object> grouped = new List<object>();
		int shown = 0;
		foreach (string key2 in order)
		{
			EventGroup g2 = groups[key2];
			Dictionary<string, object> x = Json.Obj();
			x["source"] = g2.Source;
			x["id"] = g2.Id;
			x["level"] = g2.Level;
			x["count"] = g2.Count;
			x["last"] = g2.Last;
			grouped.Add(x);
			if (++shown >= 40)
			{
				break;
			}
		}
		d["bySource"] = grouped;
		List<object> items = new List<object>();
		int n = 0;
		foreach (EventItem e3 in kept)
		{
			Dictionary<string, object> x2 = Json.Obj();
			x2["log"] = e3.Log;
			x2["source"] = e3.Source;
			x2["id"] = e3.Id;
			x2["level"] = e3.Level;
			x2["time"] = e3.Time;
			if (n < 40)
			{
				if (e3.Message.Length > 0)
				{
					x2["message"] = e3.Message;
				}
				n++;
			}
			items.Add(x2);
			if (items.Count >= 60)
			{
				break;
			}
		}
		d["items"] = items;
		r.Details = d;
		string summary = kept.Count + " за " + hours + " ч";
		if (kept.Count > 0 && order.Count > 0)
		{
			List<string> top = new List<string>();
			int c = 0;
			foreach (string key3 in order)
			{
				EventGroup g3 = groups[key3];
				top.Add(g3.Source + " #" + g3.Id + " (" + g3.Count + ")");
				if (++c >= 5)
				{
					break;
				}
			}
			summary = summary + ": " + string.Join(", ", top.ToArray());
		}
		if (ignored > 0)
		{
			summary = summary + "; проигнорировано " + ignored;
		}
		if (truncated)
		{
			summary += " (журнал обрезан предохранителем)";
		}
		if (failCount > 0 && kept.Count > failCount)
		{
			r.Fail("ошибок " + summary + " — больше порога " + failCount);
		}
		else if (warnCount >= 0 && kept.Count > warnCount)
		{
			r.Message = "ошибок " + summary;
			r.Slow = true;
		}
		else
		{
			r.Message = ((kept.Count == 0) ? ("ошибок нет за " + hours + " ч" + ((okLogs.Count > 1) ? (" (" + string.Join(", ", okLogs.ToArray()) + ")") : "")) : ("ошибок " + summary));
		}
		if (logErrors.Count > 0)
		{
			r.Message = r.Message + " · не прочитано: " + string.Join("; ", logErrors.ToArray());
		}
		list.Add(r);
		return list;
	}

	private static string ReadLog(string log, int hours, bool includeWarnings, int maxEvents, List<EventItem> into, out bool truncated)
	{
		truncated = false;
		try
		{
			string levels = (includeWarnings ? "(Level=1 or Level=2 or Level=3)" : "(Level=1 or Level=2)");
			string query = "*[System[" + levels + " and TimeCreated[timediff(@SystemTime) <= " + (long)hours * 3600L * 1000 + "]]]";
			EventLogQuery q = new EventLogQuery(log, PathType.LogName, query);
			q.TolerateQueryErrors = true;
			int read = 0;
			using (EventLogReader reader = new EventLogReader(q))
			{
				EventRecord rec;
				while ((rec = reader.ReadEvent()) != null)
				{
					using (rec)
					{
						if (read >= maxEvents)
						{
							truncated = true;
							break;
						}
						EventItem e = new EventItem();
						e.Log = log;
						e.Source = ((rec.ProviderName != null) ? rec.ProviderName : "");
						e.Id = rec.Id;
						e.Level = LevelName(rec.Level);
						e.Time = (rec.TimeCreated.HasValue ? rec.TimeCreated.Value.ToLocalTime() : DateTime.Now);
						if (into.Count < 60)
						{
							e.Message = FormatMessage(rec);
						}
						into.Add(e);
						read++;
					}
				}
			}
			return "";
		}
		catch (EventLogNotFoundException)
		{
			return "журнал не найден";
		}
		catch (UnauthorizedAccessException)
		{
			return "нет прав на чтение (нужен администратор)";
		}
		catch (EventLogException ex3)
		{
			return ex3.Message;
		}
		catch (Exception ex4)
		{
			return ex4.Message;
		}
	}

	private static string FormatMessage(EventRecord rec)
	{
		try
		{
			string msg = rec.FormatDescription();
			if (msg == null)
			{
				return "";
			}
			msg = msg.Replace("\r\n", " ").Replace("\n", " ").Trim();
			while (msg.IndexOf("  ") >= 0)
			{
				msg = msg.Replace("  ", " ");
			}
			if (msg.Length > 400)
			{
				msg = msg.Substring(0, 400) + "…";
			}
			return msg;
		}
		catch
		{
			return "";
		}
	}

	private static bool IsIgnored(EventItem e, List<string> ignore)
	{
		foreach (string raw in ignore)
		{
			if (raw.Length == 0)
			{
				continue;
			}
			int slash = raw.LastIndexOf('/');
			if (slash < 0)
			{
				if (string.Equals(raw, e.Source, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
				continue;
			}
			string src = raw.Substring(0, slash);
			string idText = raw.Substring(slash + 1).Trim();
			if (string.Equals(src, e.Source, StringComparison.OrdinalIgnoreCase))
			{
				if (idText.Length == 0)
				{
					return true;
				}
				if (int.TryParse(idText, out var id) && id == e.Id)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static string LevelName(int? level)
	{
		return level switch
		{
			1 => "Critical", 
			2 => "Error", 
			3 => "Warning", 
			4 => "Information", 
			_ => "Level " + (level.HasValue ? level.Value : 0), 
		};
	}

	private static List<string> StrList(Source source, string key)
	{
		List<string> result = new List<string>();
		List<object> arr = source.GetArray(key);
		if (arr == null)
		{
			return result;
		}
		foreach (object item in arr)
		{
			string s = Json.Str(item).Trim();
			if (s.Length > 0)
			{
				result.Add(s);
			}
		}
		return result;
	}

	private static List<object> ToObjList(List<string> src)
	{
		List<object> result = new List<object>();
		foreach (string s in src)
		{
			result.Add(s);
		}
		return result;
	}
}
