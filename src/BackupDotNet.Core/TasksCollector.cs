using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management;
using Microsoft.Win32;

namespace BackupDotNet.Core;

public class TasksCollector : ICollector
{
	public class TaskInfo
	{
		public string Path = "";

		public int State = -1;

		public string StateText = "";

		public bool Enabled;

		public string Action = "";

		public string Trigger = "";

		public string Author = "";

		public DateTime? LastRun;

		public int LastTaskResult = -1;
	}

	public class AutoItem
	{
		public string Name = "";

		public string Command = "";

		public string Where = "";
	}

	private static readonly string[] RunKeys = new string[4] { "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce", "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run", "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce" };

	public string Type => "tasks";

	public List<CollectResult> Collect(Company company, Source source, AgentCtx ctx)
	{
		List<CollectResult> list = new List<CollectResult>();
		string prefix = source.GetStr("jobName", "Задачи");
		List<string> expect = StrList(source, "expect");
		List<string> ignore = StrList(source, "ignore");
		int depth = source.GetInt("depth", 3);
		int maxAge = source.GetInt("maxLastRunAgeHours", 0);
		bool includeAutostart = source.GetBool("includeAutostart", dflt: true);
		List<TaskInfo> tasks = new List<TaskInfo>();
		string error = ReadTasks(depth, tasks);
		CollectResult r = Make(company, source, prefix + ": задачи");
		if (error.Length > 0 && tasks.Count == 0)
		{
			r.NoData = true;
			r.Message = "не удалось прочитать планировщик: " + error;
			list.Add(r);
			return list;
		}
		List<TaskInfo> kept = new List<TaskInfo>();
		foreach (TaskInfo t in tasks)
		{
			if (!IsIgnored(t.Path, ignore))
			{
				kept.Add(t);
			}
		}
		kept.Sort((TaskInfo taskInfo, TaskInfo b) => string.Compare(taskInfo.Path, b.Path, StringComparison.OrdinalIgnoreCase));
		List<string> problems = new List<string>();
		List<string> warnings = new List<string>();
		foreach (string want in expect)
		{
			TaskInfo found = null;
			foreach (TaskInfo t2 in kept)
			{
				if (string.Equals(t2.Path, want, StringComparison.OrdinalIgnoreCase) || string.Equals(t2.Path, "\\" + want, StringComparison.OrdinalIgnoreCase))
				{
					found = t2;
					break;
				}
			}
			if (found == null)
			{
				problems.Add("нет задачи " + want);
			}
		}
		foreach (TaskInfo t3 in kept)
		{
			bool expected = WantsAny(expect, t3.Path);
			if (t3.State == 1)
			{
				if (expected)
				{
					problems.Add(t3.Path + ": отключена");
				}
				else
				{
					warnings.Add(t3.Path + ": отключена");
				}
			}
			if (t3.LastTaskResult != 0 && t3.LastTaskResult != 267009 && t3.LastTaskResult != 267011)
			{
				warnings.Add(t3.Path + ": последний код " + FormatResult(t3.LastTaskResult));
			}
			if (maxAge > 0 && t3.LastRun.HasValue && t3.State != 1)
			{
				double hours = (DateTime.Now - t3.LastRun.Value).TotalHours;
				if (hours > (double)maxAge && expected)
				{
					warnings.Add(t3.Path + ": не запускалась " + (int)(hours / 24.0) + " дн");
				}
			}
		}
		Dictionary<string, object> d = Json.Obj();
		d["count"] = kept.Count;
		d["disabled"] = CountState(kept, 1);
		d["running"] = CountState(kept, 4);
		if (error.Length > 0)
		{
			d["error"] = error;
		}
		List<object> items = new List<object>();
		foreach (TaskInfo t4 in kept)
		{
			Dictionary<string, object> x = Json.Obj();
			x["path"] = t4.Path;
			x["state"] = t4.StateText;
			x["enabled"] = t4.Enabled;
			if (t4.Action.Length > 0)
			{
				x["action"] = t4.Action;
			}
			if (t4.LastRun.HasValue)
			{
				x["lastRun"] = t4.LastRun.Value;
			}
			x["lastResult"] = t4.LastTaskResult;
			if (t4.LastTaskResult != 0)
			{
				x["lastResultText"] = FormatResult(t4.LastTaskResult);
			}
			if (t4.Trigger.Length > 0)
			{
				x["trigger"] = t4.Trigger;
			}
			if (!string.IsNullOrEmpty(t4.Author))
			{
				x["author"] = t4.Author;
			}
			items.Add(x);
		}
		d["items"] = items;
		r.Details = d;
		if (problems.Count > 0)
		{
			r.Fail(string.Join("; ", problems.ToArray()));
		}
		else if (warnings.Count > 0)
		{
			r.Message = "задач " + kept.Count + ": " + JoinLimit(warnings, 4);
			r.Slow = true;
		}
		else
		{
			r.Message = "задач " + kept.Count + ((expect.Count > 0) ? ", все ожидаемые на месте" : "");
		}
		list.Add(r);
		if (includeAutostart)
		{
			List<string> autoIgnore = StrList(source, "autostartIgnore");
			List<AutoItem> auto = ReadAutostart();
			List<AutoItem> autoKept = new List<AutoItem>();
			HashSet<string> seenAuto = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (AutoItem a in auto)
			{
				if (!IsAutoIgnored(a, autoIgnore))
				{
					string key = a.Name + "|" + a.Command;
					if (seenAuto.Add(key))
					{
						autoKept.Add(a);
					}
				}
			}
			CollectResult a2 = Make(company, source, prefix + ": автозагрузка");
			a2.Target = "autostart";
			Dictionary<string, object> ad = Json.Obj();
			ad["count"] = autoKept.Count;
			List<object> aItems = new List<object>();
			foreach (AutoItem a3 in autoKept)
			{
				Dictionary<string, object> x2 = Json.Obj();
				x2["name"] = a3.Name;
				x2["command"] = a3.Command;
				x2["where"] = a3.Where;
				aItems.Add(x2);
			}
			ad["items"] = aItems;
			a2.Details = ad;
			if (autoKept.Count == 0)
			{
				a2.Message = "записей автозагрузки нет";
			}
			else
			{
				List<string> names = new List<string>();
				int n = 0;
				foreach (AutoItem a4 in autoKept)
				{
					names.Add(a4.Name);
					if (++n >= 6)
					{
						break;
					}
				}
				a2.Message = "записей " + autoKept.Count + ": " + string.Join(", ", names.ToArray()) + ((autoKept.Count > names.Count) ? "…" : "");
			}
			list.Add(a2);
		}
		return list;
	}

	private static CollectResult Make(Company c, Source s, string job)
	{
		CollectResult r = new CollectResult();
		r.CompanyId = c.Id;
		r.CompanyName = c.Name;
		r.SourceId = s.Id;
		r.Kind = "tasks";
		r.Job = job;
		r.IsHealth = true;
		r.Reachable = true;
		return r;
	}

	private static string JoinLimit(List<string> src, int max)
	{
		List<string> parts = new List<string>();
		int n = 0;
		foreach (string s in src)
		{
			parts.Add(s);
			if (++n >= max)
			{
				break;
			}
		}
		string text = string.Join("; ", parts.ToArray());
		if (src.Count > parts.Count)
		{
			object obj = text;
			text = string.Concat(obj, " (+", src.Count - parts.Count, ")");
		}
		return text;
	}

	private static int CountState(List<TaskInfo> tasks, int state)
	{
		int n = 0;
		foreach (TaskInfo t in tasks)
		{
			if (t.State == state)
			{
				n++;
			}
		}
		return n;
	}

	private static bool WantsAny(List<string> expect, string path)
	{
		foreach (string want in expect)
		{
			if (string.Equals(path, want, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			if (string.Equals(path, "\\" + want, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsIgnored(string path, List<string> ignore)
	{
		foreach (string raw in ignore)
		{
			if (raw.Length != 0)
			{
				string pat = raw;
				if (pat.EndsWith("*", StringComparison.Ordinal))
				{
					pat = pat.Substring(0, pat.Length - 1);
				}
				if (path.StartsWith(pat, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool IsAutoIgnored(AutoItem a, List<string> ignore)
	{
		foreach (string raw in ignore)
		{
			if (raw.Length != 0)
			{
				if (string.Equals(raw, a.Name, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
				if (a.Command.IndexOf(raw, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static string FormatResult(int code)
	{
		return code switch
		{
			0 => "0", 
			267009 => "выполняется", 
			267011 => "не запускалась", 
			_ => "0x" + code.ToString("X", CultureInfo.InvariantCulture) + " (" + code.ToString(CultureInfo.InvariantCulture) + ")", 
		};
	}

	public static string ReadTasks(int maxDepth, List<TaskInfo> into)
	{
		try
		{
			Type comType = System.Type.GetTypeFromProgID("Schedule.Service");
			if (comType == null)
			{
				return "планировщик заданий недоступен (Schedule.Service)";
			}
			dynamic svc = Activator.CreateInstance(comType);
			svc.Connect();
			dynamic root = svc.GetFolder("\\");
			TasksCollector.Walk(root, 0, maxDepth, into);
			return "";
		}
		catch (Exception ex)
		{
			return ex.Message;
		}
	}

	private static void Walk(dynamic folder, int level, int maxDepth, List<TaskInfo> into)
	{
		if (level > maxDepth)
		{
			return;
		}
		try
		{
			dynamic tasks = folder.GetTasks(1);
			int count = tasks.Count;
			for (int i = 1; i <= count; i++)
			{
				try
				{
					TaskInfo info = TasksCollector.ReadTask(tasks.Item(i));
					if (info != null)
					{
						into.Add(info);
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
		if (level >= maxDepth)
		{
			return;
		}
		try
		{
			dynamic folders = folder.GetFolders(0);
			int count2 = folders.Count;
			for (int j = 1; j <= count2; j++)
			{
				try
				{
					TasksCollector.Walk(folders.Item(j), level + 1, maxDepth, into);
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

	private static TaskInfo ReadTask(dynamic task)
	{
		TaskInfo t = new TaskInfo();
		try
		{
			t.Path = (string)task.Path;
		}
		catch
		{
		}
		if (string.IsNullOrEmpty(t.Path))
		{
			return null;
		}
		t.Path = t.Path ?? "";
		try
		{
			t.State = (int)task.State;
		}
		catch
		{
			t.State = -1;
		}
		t.StateText = StateName(t.State);
		try
		{
			t.Enabled = (bool)task.Enabled;
		}
		catch
		{
		}
		try
		{
			t.LastTaskResult = (int)task.LastTaskResult;
		}
		catch
		{
			t.LastTaskResult = -1;
		}
		try
		{
			DateTime dt = (DateTime)task.LastRunTime;
			if (dt.Year > 1990)
			{
				t.LastRun = dt;
			}
		}
		catch
		{
		}
		try
		{
			dynamic def = task.Definition;
			dynamic actions = def.Actions;
			int n = actions.Count;
			if (n > 0)
			{
				dynamic a = actions.Item(1);
				string exe = "";
				string args = "";
				try
				{
					exe = (string)a.Path;
				}
				catch
				{
				}
				try
				{
					args = (string)a.Arguments;
				}
				catch
				{
				}
				if (string.IsNullOrEmpty(exe))
				{
					try
					{
						exe = (string)a.Document;
					}
					catch
					{
					}
				}
				t.Action = Clip((exe ?? "") + " " + (args ?? ""), 160).Trim();
			}
			dynamic triggers = def.Triggers;
			if (triggers.Count > 0)
			{
				dynamic tr = triggers.Item(1);
				int type = (int)tr.Type;
				t.Trigger = TriggerName(type);
			}
			try
			{
				string author = (string)def.RegistrationInfo.Author;
				if (!string.IsNullOrEmpty(author))
				{
					t.Author = author;
				}
			}
			catch
			{
			}
		}
		catch
		{
		}
		return t;
	}

	private static string Clip(string s, int max)
	{
		if (s == null)
		{
			return "";
		}
		s = s.Replace("\r", " ").Replace("\n", " ").Trim();
		if (s.Length <= max)
		{
			return s;
		}
		return s.Substring(0, max - 1) + "…";
	}

	public static string StateName(int state)
	{
		return state switch
		{
			0 => "Unknown", 
			1 => "Disabled", 
			2 => "Queued", 
			3 => "Ready", 
			4 => "Running", 
			_ => "state " + state.ToString(CultureInfo.InvariantCulture), 
		};
	}

	public static string TriggerName(int type)
	{
		return type switch
		{
			1 => "по событию", 
			2 => "по времени", 
			3 => "ежедневно", 
			4 => "еженедельно", 
			5 => "ежемесячно", 
			6 => "ежемесячно (дни недели)", 
			7 => "простой", 
			8 => "при регистрации", 
			9 => "при загрузке", 
			10 => "при входе", 
			11 => "при смене сеанса", 
			_ => "тип " + type.ToString(CultureInfo.InvariantCulture), 
		};
	}

	public static List<AutoItem> ReadAutostart()
	{
		List<AutoItem> result = new List<AutoItem>();
		string[] runKeys = RunKeys;
		foreach (string key in runKeys)
		{
			ReadRunKey(RegistryHive.LocalMachine, key, "HKLM", result);
			ReadRunKey(RegistryHive.CurrentUser, key, "HKCU", result);
		}
		ReadUserHives(result);
		ReadStartupFolder(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), "общая папка «Автозагрузка»", result);
		ReadStartupFolder(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "папка «Автозагрузка» пользователя", result);
		return result;
	}

	private static void ReadUserHives(List<AutoItem> into)
	{
		try
		{
			using RegistryKey users = Registry.Users;
			if (users == null)
			{
				return;
			}
			string[] subKeyNames = users.GetSubKeyNames();
			foreach (string sid in subKeyNames)
			{
				if (!sid.EndsWith("_Classes", StringComparison.OrdinalIgnoreCase) && sid.Length >= 6)
				{
					string user = "";
					try
					{
						user = SidToName(sid);
					}
					catch
					{
					}
					string label = ((user.Length > 0) ? ("HKU\\" + user) : ("HKU\\" + sid));
					string[] array = new string[2] { "Software\\Microsoft\\Windows\\CurrentVersion\\Run", "Software\\Microsoft\\Windows\\CurrentVersion\\RunOnce" };
					foreach (string path in array)
					{
						ReadRunKey(RegistryHive.Users, sid + "\\" + path, label, into);
					}
				}
			}
		}
		catch
		{
		}
	}

	private static string SidToName(string sid)
	{
		try
		{
			ManagementObjectCollection col = Hw.Query("root\\cimv2", "SELECT Name FROM Win32_UserAccount WHERE SID = '" + sid.Replace("'", "") + "'");
			if (col == null)
			{
				return "";
			}
			foreach (ManagementBaseObject o in col)
			{
				using (o)
				{
					string name = Hw.Prop(o, "Name");
					if (name.Length > 0)
					{
						return name;
					}
				}
			}
		}
		catch
		{
		}
		return "";
	}

	private static void ReadRunKey(RegistryHive hive, string path, string label, List<AutoItem> into)
	{
		try
		{
			using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, (hive != RegistryHive.Users) ? RegistryView.Registry64 : RegistryView.Default);
			using RegistryKey k = baseKey.OpenSubKey(path, writable: false);
			if (k == null)
			{
				return;
			}
			string[] names = k.GetValueNames();
			string[] array = names;
			foreach (string name in array)
			{
				if (name.Length != 0)
				{
					string val = "";
					try
					{
						val = Convert.ToString(k.GetValue(name), CultureInfo.InvariantCulture);
					}
					catch
					{
					}
					AutoItem a = new AutoItem();
					a.Name = name;
					a.Command = Clip(val, 200);
					a.Where = label + " · " + ShortKey(path);
					into.Add(a);
				}
			}
		}
		catch
		{
		}
	}

	private static string ShortKey(string path)
	{
		string name = (path.EndsWith("RunOnce", StringComparison.OrdinalIgnoreCase) ? "RunOnce" : "Run");
		if (path.IndexOf("Wow6432Node", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			name += " (32-бит)";
		}
		return name;
	}

	private static void ReadStartupFolder(string dir, string label, List<AutoItem> into)
	{
		if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
		{
			return;
		}
		try
		{
			string[] files = Directory.GetFiles(dir);
			foreach (string file in files)
			{
				string name = Path.GetFileName(file);
				if (!name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase))
				{
					AutoItem a = new AutoItem();
					a.Name = name;
					a.Command = file;
					a.Where = label;
					into.Add(a);
				}
			}
		}
		catch
		{
		}
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
}
