using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Principal;
using System.Text;

namespace BackupDotNet.Core;

/// <summary>
/// Самодиагностика агента: «а всё ли у меня в порядке?».
///
/// Отвечает на вопросы, которые обычно выясняются уже при разборе аварии:
/// есть ли место на диске, пишутся ли журналы, успевает ли планировщик,
/// не растёт ли очередь Zabbix, виден ли веб-интерфейс по сети.
///
/// Ничего не меняет и не бросает исключений — это отчёт, а не действие.
/// </summary>
public static class Diagnostics
{
	/// <summary>Порог свободного места, ниже которого — предупреждение (ГБ).</summary>
	private const double DiskWarnGb = 2.0;

	/// <summary>Порог свободного места, ниже которого — ошибка (ГБ).</summary>
	private const double DiskFailGb = 0.5;

	/// <summary>Насколько тик может превысить интервал, прежде чем это станет тревогой (секунды).</summary>
	private const double TickLagWarnSeconds = 30.0;

	/// <summary>Сколько может лежать очередь Zabbix, прежде чем предупредить (МБ).</summary>
	private const double ZabbixQueueWarnMb = 8.0;

	/// <summary>
	/// Собирает полный отчёт. <paramref name="prefix"/> — фактический префикс,
	/// который занял веб-сервер (нужен, чтобы понять, слушаем ли мы сеть или только localhost).
	/// </summary>
	public static Dictionary<string, object> Build(AgentCtx ctx, Scheduler scheduler, string prefix)
	{
		Dictionary<string, object> root = Json.Obj();
		List<object> checks = new List<object>();

		DateTime now = DateTime.Now;
		Settings settings = ctx.Settings;

		root["generatedAt"] = now;
		root["version"] = scheduler?.Version ?? "";
		root["agent"] = BuildAgent(now);
		root["paths"] = BuildPaths(settings);
		root["disk"] = BuildDisk(settings.DataDir, checks);
		root["sizes"] = BuildSizes(settings);
		root["counts"] = BuildCounts(ctx);
		root["config"] = BuildConfig(ctx.Config, checks);
		root["scheduler"] = BuildScheduler(scheduler, settings, checks);
		root["web"] = BuildWeb(settings, prefix, checks);
		root["zabbix"] = BuildZabbix(settings, scheduler, checks);
		root["logs"] = BuildLogs(checks);
		root["storage"] = BuildStorage(ctx.Store, checks);

		root["checks"] = checks;
		root["ok"] = !HasLevel(checks, "error");
		root["warnings"] = CountLevel(checks, "warn");
		root["errors"] = CountLevel(checks, "error");
		return root;
	}

	// ------------------------------------------------------------------ секции

	private static Dictionary<string, object> BuildAgent(DateTime now)
	{
		Dictionary<string, object> a = Json.Obj();
		a["name"] = Branding.Current.FullName;
		a["machineName"] = Safe(() => Environment.MachineName, "");
		a["os"] = Safe(() => Environment.OSVersion.VersionString, "");
		a["runtime"] = Safe(() => Environment.Version.ToString(), "");
		a["is64Bit"] = Safe(() => Environment.Is64BitProcess, false);
		a["processorCount"] = Safe(() => Environment.ProcessorCount, 0);
		a["userName"] = Safe(() => Environment.UserName, "");
		a["isAdmin"] = IsElevated();
		a["startedAt"] = ProcessStartTime();
		a["uptimeSeconds"] = (long)Math.Max(0.0, (now - ProcessStartTime()).TotalSeconds);

		Dictionary<string, object> p = Json.Obj();
		try
		{
			using Process proc = Process.GetCurrentProcess();
			p["workingSetMb"] = Math.Round(proc.WorkingSet64 / 1048576.0, 1);
			p["privateMb"] = Math.Round(proc.PrivateMemorySize64 / 1048576.0, 1);
			p["threads"] = proc.Threads.Count;
			p["handles"] = proc.HandleCount;
		}
		catch
		{
		}
		a["process"] = p;
		return a;
	}

	private static Dictionary<string, object> BuildPaths(Settings settings)
	{
		Dictionary<string, object> d = Json.Obj();
		d["install"] = Safe(() => AppDomain.CurrentDomain.BaseDirectory, "");
		d["data"] = settings.DataDir;
		d["logs"] = Path.Combine(settings.DataDir, "logs");
		d["config"] = settings.ConfigDir;
		d["public"] = settings.PublicDir;
		d["logFile"] = Log.CurrentFile;
		return d;
	}

	private static Dictionary<string, object> BuildDisk(string dataDir, List<object> checks)
	{
		Dictionary<string, object> d = Json.Obj();
		string root = "";
		try
		{
			root = Path.GetPathRoot(Path.GetFullPath(dataDir)) ?? "";
		}
		catch
		{
		}
		d["drive"] = root;

		double freeGb = 0.0;
		double totalGb = 0.0;
		try
		{
			DriveInfo di = new DriveInfo(root);
			freeGb = Math.Round(di.AvailableFreeSpace / 1073741824.0, 2);
			totalGb = Math.Round(di.TotalSize / 1073741824.0, 2);
			d["label"] = di.VolumeLabel;
			d["format"] = di.DriveFormat;
		}
		catch (Exception ex)
		{
			Add(checks, "disk", "Диск с данными", "warn",
				"не удалось определить свободное место: " + ex.Message,
				"Проверьте, что каталог данных доступен процессу агента.");
		}

		d["freeGb"] = freeGb;
		d["totalGb"] = totalGb;
		d["freePercent"] = (totalGb > 0.0) ? Math.Round(freeGb / totalGb * 100.0, 1) : 0.0;

		if (totalGb > 0.0)
		{
			if (freeGb < DiskFailGb)
			{
				Add(checks, "disk", "Диск с данными", "error",
					"свободно " + Fmt(freeGb) + " ГБ из " + Fmt(totalGb) + " ГБ",
					"Бэкапы и журналы перестанут писаться. Освободите место или смените каталог данных.");
			}
			else if (freeGb < DiskWarnGb)
			{
				Add(checks, "disk", "Диск с данными", "warn",
					"свободно " + Fmt(freeGb) + " ГБ из " + Fmt(totalGb) + " ГБ",
					"Место заканчивается. Проверьте политику хранения и старые журналы.");
			}
			else
			{
				Add(checks, "disk", "Диск с данными", "ok",
					"свободно " + Fmt(freeGb) + " ГБ из " + Fmt(totalGb) + " ГБ", "");
			}
		}
		return d;
	}

	private static Dictionary<string, object> BuildSizes(Settings settings)
	{
		Dictionary<string, object> d = Json.Obj();
		d["dataMb"] = DirSizeMb(settings.DataDir);
		d["logsMb"] = DirSizeMb(Path.Combine(settings.DataDir, "logs"));
		d["stateMb"] = DirSizeMb(Path.Combine(settings.DataDir, "state"));
		d["historyMb"] = DirSizeMb(Path.Combine(settings.DataDir, "history"));
		d["cacheMb"] = DirSizeMb(Path.Combine(settings.DataDir, "cache"));
		return d;
	}

	private static Dictionary<string, object> BuildCounts(AgentCtx ctx)
	{
		Dictionary<string, object> d = Json.Obj();
		int companies = 0;
		int enabled = 0;
		int sources = 0;
		int enabledSources = 0;
		try
		{
			foreach (Company c in ctx.Config.Companies)
			{
				companies++;
				if (c.Enabled)
				{
					enabled++;
				}
				foreach (Source s in c.Sources)
				{
					sources++;
					if (s.Enabled && c.Enabled)
					{
						enabledSources++;
					}
				}
			}
		}
		catch
		{
		}
		d["companies"] = companies;
		d["companiesEnabled"] = enabled;
		d["sources"] = sources;
		d["sourcesEnabled"] = enabledSources;
		d["jobs"] = Safe(() => ctx.Store.GetStates().Count, 0);
		return d;
	}

	private static Dictionary<string, object> BuildConfig(ConfigLoader config, List<object> checks)
	{
		Dictionary<string, object> d = Json.Obj();
		List<object> errors = new List<object>();
		try
		{
			foreach (string e in config.LoadErrors)
			{
				errors.Add(e);
			}
		}
		catch
		{
		}
		d["errors"] = errors;
		d["count"] = errors.Count;

		if (errors.Count == 0)
		{
			Add(checks, "config", "Конфигурация", "ok", "ошибок нет", "");
		}
		else
		{
			Add(checks, "config", "Конфигурация", "warn",
				"предупреждений: " + errors.Count,
				"Подробности — в списке ниже и в журнале агента.");
		}
		return d;
	}

	private static Dictionary<string, object> BuildScheduler(Scheduler sch, Settings settings, List<object> checks)
	{
		Dictionary<string, object> d = Json.Obj();
		int tick = Math.Max(5, settings.TickSeconds);

		if (sch == null)
		{
			d["running"] = false;
			Add(checks, "scheduler", "Планировщик", "warn",
				"не запущен в этом процессе",
				"Так бывает в режимах --once и --check: они отрабатывают один раз и выходят.");
			return d;
		}

		bool running = sch.IsRunning;
		d["running"] = running;
		d["tickSeconds"] = tick;
		d["ticks"] = sch.Ticks;
		d["tickErrors"] = sch.TickErrors;
		d["lastTickAt"] = sch.LastTickAt;
		d["lastTickDurationSeconds"] = Math.Round(sch.LastTickDurationSeconds, 2);
		d["lastRunAt"] = sch.LastRunAt;
		d["lastRunCount"] = sch.LastRunCount;
		d["startedAt"] = sch.StartedAt;
		d["uptimeSeconds"] = (long)Math.Max(0.0, (DateTime.Now - sch.StartedAt).TotalSeconds);

		double sinceTick = sch.LastTickAt.HasValue ? (DateTime.Now - sch.LastTickAt.Value).TotalSeconds : -1.0;
		d["secondsSinceTick"] = Math.Round(sinceTick, 1);

		if (!running)
		{
			Add(checks, "scheduler", "Планировщик", "error",
				"поток планировщика не работает",
				"Проверки не выполняются. Перезапустите службу и посмотрите журнал.");
			return d;
		}

		// Тик дольше интервала — проверки не успевают, расписание начнёт разъезжаться.
		if (sch.LastTickDurationSeconds > tick + TickLagWarnSeconds)
		{
			Add(checks, "scheduler", "Планировщик", "warn",
				"последний тик длился " + Fmt(sch.LastTickDurationSeconds) + " с при интервале " + tick + " с",
				"Проверки не успевают за расписанием. Уменьшите maxParallelChecks или увеличьте интервалы источников.");
		}
		else if (sinceTick > tick * 3 + 60)
		{
			Add(checks, "scheduler", "Планировщик", "warn",
				"последний тик был " + Fmt(sinceTick) + " с назад",
				"Планировщик молчит дольше ожидаемого — проверьте журнал на ошибки.");
		}
		else
		{
			string detail = "тиков: " + sch.Ticks;
			if (sch.TickErrors > 0)
			{
				detail += ", с ошибками: " + sch.TickErrors;
			}
			detail += ", последний " + Fmt(sch.LastTickDurationSeconds) + " с";
			Add(checks, "scheduler", "Планировщик", (sch.TickErrors > 0) ? "warn" : "ok", detail,
				(sch.TickErrors > 0) ? "Часть тиков завершилась исключением — смотрите журнал." : "");
		}
		return d;
	}

	private static Dictionary<string, object> BuildWeb(Settings settings, string prefix, List<object> checks)
	{
		Dictionary<string, object> d = Json.Obj();
		d["prefix"] = prefix ?? "";
		d["port"] = settings.HttpPort;
		d["requireAuth"] = settings.WebRequireAuth;

		// Слушаем только localhost — значит с других машин интерфейс недоступен.
		bool localOnly = (prefix ?? "").IndexOf("localhost", StringComparison.OrdinalIgnoreCase) >= 0 ||
			(prefix ?? "").IndexOf("127.0.0.1", StringComparison.Ordinal) >= 0;
		d["localOnly"] = localOnly;

		if (string.IsNullOrEmpty(prefix))
		{
			Add(checks, "web", "Веб-сервер", "error",
				"префикс не занят",
				"Смотрите журнал: скорее всего занят порт или не выдан urlacl.");
		}
		else if (localOnly)
		{
			Add(checks, "web", "Веб-сервер", "warn",
				"слушает только " + prefix,
				"С других машин интерфейс недоступен. Нужен urlacl: netsh http add urlacl url=http://+:" +
				settings.HttpPort + "/ user=\"NT AUTHORITY\\SYSTEM\"");
		}
		else
		{
			Add(checks, "web", "Веб-сервер", "ok", prefix, "");
		}

		if (!settings.WebRequireAuth)
		{
			Add(checks, "web-auth", "Авторизация", "warn",
				"вход без пароля (web.requireAuth = false)",
				"Включайте только для изолированного стенда.");
		}
		else
		{
			Add(checks, "web-auth", "Авторизация", "ok", "включена", "");
		}
		return d;
	}

	private static Dictionary<string, object> BuildZabbix(Settings settings, Scheduler sch, List<object> checks)
	{
		Dictionary<string, object> d = Json.Obj();
		bool enabled = settings.ZabbixEnabled && !string.IsNullOrEmpty(settings.ZabbixServer);
		d["enabled"] = enabled;
		d["server"] = settings.ZabbixServer;
		d["port"] = settings.ZabbixPort;
		d["apiEnabled"] = settings.ZabbixApiEnabled;

		string queue = "";
		try
		{
			queue = (sch != null) ? sch.ZabbixQueuePath : Path.Combine(settings.DataDir, "state", "zabbix-queue.ndjson");
		}
		catch
		{
		}

		long bytes = 0L;
		int entries = 0;
		try
		{
			FileInfo fi = new FileInfo(queue);
			if (fi.Exists)
			{
				bytes = fi.Length;
				entries = CountLines(queue);
			}
		}
		catch
		{
		}
		d["queueBytes"] = bytes;
		d["queueEntries"] = entries;

		if (!enabled)
		{
			Add(checks, "zabbix", "Zabbix", "ok", "отправка выключена", "");
			return d;
		}

		double queueMb = bytes / 1048576.0;
		if (queueMb >= ZabbixQueueWarnMb)
		{
			Add(checks, "zabbix", "Zabbix", "warn",
				"очередь " + Fmt(queueMb) + " МБ (" + entries + " значений)",
				"Сервер не принимает данные. Проверьте доступность " + settings.ZabbixServer + ":" + settings.ZabbixPort + ".");
		}
		else if (entries > 0)
		{
			Add(checks, "zabbix", "Zabbix", "ok",
				"очередь на досылку: " + entries + " значений (" + Fmt(queueMb) + " МБ)", "");
		}
		else
		{
			Add(checks, "zabbix", "Zabbix", "ok",
				settings.ZabbixServer + ":" + settings.ZabbixPort + ", очередь пуста", "");
		}
		return d;
	}

	private static Dictionary<string, object> BuildLogs(List<object> checks)
	{
		Dictionary<string, object> d = Json.Obj();
		string dir = Log.Dir;
		d["dir"] = dir;
		d["level"] = Log.Level.ToString().ToLowerInvariant();
		d["current"] = Log.CurrentFile;

		int files = 0;
		try
		{
			if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
			{
				files = Directory.GetFiles(dir, "*.log").Length;
			}
		}
		catch
		{
		}
		d["files"] = files;

		// Главное — что журнал реально пишется прямо сейчас.
		string current = Log.CurrentFile;
		bool writable = false;
		long length = 0L;
		if (!string.IsNullOrEmpty(current))
		{
			try
			{
				FileInfo fi = new FileInfo(current);
				writable = fi.Exists;
				length = fi.Exists ? fi.Length : 0L;
			}
			catch
			{
			}
		}
		d["writable"] = writable;
		d["currentBytes"] = length;

		if (string.IsNullOrEmpty(dir))
		{
			Add(checks, "logs", "Журналы", "error",
				"каталог журналов не задан",
				"Логирование в файл выключено — разбирать инциденты будет нечем.");
		}
		else if (!writable)
		{
			Add(checks, "logs", "Журналы", "error",
				"файл журнала недоступен: " + current,
				"Проверьте права на каталог данных у учётной записи службы.");
		}
		else
		{
			Add(checks, "logs", "Журналы", "ok",
				"пишется, файлов: " + files + ", уровень " + d["level"], "");
		}
		return d;
	}

	private static Dictionary<string, object> BuildStorage(DataStore store, List<object> checks)
	{
		Dictionary<string, object> d = Json.Obj();
		int jobs = 0;
		try
		{
			jobs = store.GetStates().Count;
		}
		catch (Exception ex)
		{
			Add(checks, "storage", "Хранилище", "error",
				"состояние не читается: " + ex.Message,
				"Файлы в <dataDir>\\state повреждены. Агент пересоберёт их при следующем прогоне.");
			return d;
		}
		d["jobs"] = jobs;
		Add(checks, "storage", "Хранилище", "ok", "заданий в состоянии: " + jobs, "");
		return d;
	}

	// ---------------------------------------------------------------- утилиты

	private static void Add(List<object> checks, string id, string title, string status, string message, string hint)
	{
		Dictionary<string, object> c = Json.Obj();
		c["id"] = id;
		c["title"] = title;
		c["status"] = status;
		c["message"] = message ?? "";
		c["hint"] = hint ?? "";
		checks.Add(c);
	}

	private static bool HasLevel(List<object> checks, string status)
	{
		foreach (object o in checks)
		{
			if (Json.GetStr(Json.AsObject(o), "status", "") == status)
			{
				return true;
			}
		}
		return false;
	}

	private static int CountLevel(List<object> checks, string status)
	{
		int n = 0;
		foreach (object o in checks)
		{
			if (Json.GetStr(Json.AsObject(o), "status", "") == status)
			{
				n++;
			}
		}
		return n;
	}

	private static bool IsElevated()
	{
		try
		{
			using WindowsIdentity id = WindowsIdentity.GetCurrent();
			WindowsPrincipal p = new WindowsPrincipal(id);
			return p.IsInRole(WindowsBuiltInRole.Administrator);
		}
		catch
		{
			return false;
		}
	}

	private static DateTime ProcessStartTime()
	{
		try
		{
			using Process proc = Process.GetCurrentProcess();
			return proc.StartTime;
		}
		catch
		{
			return DateTime.Now;
		}
	}

	private static double DirSizeMb(string dir)
	{
		if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
		{
			return 0.0;
		}
		long total = 0L;
		try
		{
			foreach (string f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
			{
				try
				{
					total += new FileInfo(f).Length;
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		return Math.Round(total / 1048576.0, 2);
	}

	private static int CountLines(string path)
	{
		try
		{
			using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			using StreamReader sr = new StreamReader(fs, Encoding.UTF8);
			int n = 0;
			while (sr.ReadLine() != null)
			{
				n++;
			}
			return n;
		}
		catch
		{
			return 0;
		}
	}

	private static string Fmt(double v)
	{
		return v.ToString("0.#", CultureInfo.InvariantCulture);
	}

	private static T Safe<T>(Func<T> f, T dflt)
	{
		try
		{
			return f();
		}
		catch
		{
			return dflt;
		}
	}
}
