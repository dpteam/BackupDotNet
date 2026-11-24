using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using BackupDotNet.Core;
using BackupDotNet.Core.Update;
using BackupDotNet.Web;

namespace BackupDotNet;

public static class Program
{
	/// <summary>Версия продукта: берётся из атрибутов сборки (Directory.Build.props).</summary>
	public static readonly string AgentVersion = ResolveVersion();

	private static string ResolveVersion()
	{
		try
		{
			Assembly asm = typeof(Program).Assembly;
			string info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
			if (!string.IsNullOrEmpty(info))
			{
				int plus = info.IndexOf('+');
				return plus > 0 ? info.Substring(0, plus) : info;
			}
			Version v = asm.GetName().Version;
			return (v == null) ? "1.0.0" : (v.Major + "." + v.Minor + "." + v.Build);
		}
		catch
		{
			return "1.0.0";
		}
	}

	/// <summary>Каталог данных (для ранней диагностики до загрузки конфигурации).</summary>
	private static string DiagDir()
	{
		string d = "";
		try
		{
			d = Branding.Current.DataDir;
		}
		catch
		{
		}
		if (string.IsNullOrEmpty(d))
		{
			d = "data";
		}
		// Относительный путь (портативный инстанс) считаем от папки exe,
		// а не от текущего каталога процесса: служба стартует из System32.
		if (!Path.IsPathRooted(d))
		{
			d = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, d);
		}
		return Path.Combine(d, "logs");
	}

	/// <summary>
	/// Ранняя диагностика: пишется до того, как прочитана конфигурация и поднят
	/// основной журнал. Никогда не бросает исключений.
	/// </summary>
	public static void Diag(string message)
	{
		try
		{
			string dir = DiagDir();
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}
			string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + message + "\r\n";
			File.AppendAllText(Path.Combine(dir, "boot.log"), line, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		}
		catch
		{
		}
	}

	public static int Main(string[] args)
	{
		try
		{
			if (Environment.UserInteractive)
			{
				Console.OutputEncoding = Encoding.UTF8;
			}
		}
		catch
		{
		}
		string root = AppDomain.CurrentDomain.BaseDirectory;
		string mode = ((args.Length > 0) ? args[0].ToLowerInvariant() : "console");
		try
		{
			Branding.SetCurrent(Branding.For(Path.Combine(root, "config")));
		}
		catch
		{
		}
		Diag("start mode=" + mode + " root=" + root + " user=" + Environment.UserName + " interactive=" + Environment.UserInteractive + " version=" + AgentVersion);
		try
		{
			switch (mode)
			{
			case "--service":
				RunService(root);
				return 0;
			case "--console":
			case "console":
				return RunConsole(root, args);
			case "--once":
			case "once":
				return RunOnce(root);
			case "--check":
			case "check":
				return RunCheck(root, args);
			case "--zabbix":
			case "zabbix":
				return RunZabbix(root, args);
			case "--remote":
			case "remote":
				return RemoteRunner.Run(root);
			case "--service-remote":
				return RunServiceRemote(root);
			case "--version":
			case "version":
				Console.WriteLine(Branding.Current.FullName + " " + AgentVersion);
				return 0;
			case "--help":
			case "help":
			case "/?":
				PrintHelp();
				return 0;
			default:
				Console.Error.WriteLine("Неизвестный режим: " + mode);
				PrintHelp();
				return 2;
			}
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine("КРИТИЧЕСКАЯ ОШИБКА: " + ex);
			Log.Error("Критическая ошибка в Main", ex);
			return 1;
		}
		finally
		{
			Log.Shutdown();
		}
	}

	private static void PrintHelp()
	{
		string name = Branding.Current.FullName;
		Console.WriteLine(
			name + " " + AgentVersion + "\n" +
			"\n" +
			"Мониторинг бэкапов, доступности и железа. Всё управление — в веб-интерфейсе.\n" +
			"\n" +
			"Режимы:\n" +
			"  --console          агент с веб-интерфейсом (обычный запуск, отладка)\n" +
			"  --service          запуск как служба Windows\n" +
			"  --service-remote   служба удалённого агента (см. config/remote.json)\n" +
			"  --once             один прогон всех проверок и выход\n" +
			"  --check <компания> один прогон конкретной компании\n" +
			"  --zabbix discovery          JSON для LLD в Zabbix\n" +
			"  --zabbix job <имя> <поле>   значение поля задания (pull-режим)\n" +
			"  --remote           удалённый агент: забирает конфигурацию с\n" +
			"                     центрального агента и отправляет результаты\n" +
			"  --version          версия\n" +
			"  --help             эта справка\n" +
			"\n" +
			"Примеры:\n" +
			"  BackupDotNet.exe --console\n" +
			"  BackupDotNet.exe --once\n" +
			"  BackupDotNet.exe --check example\n" +
			"  BackupDotNet.exe --zabbix job \"1С · ежедневный бэкап\" status\n" +
			"\n" +
			"Журналы: <каталог данных>\\logs\\" + Branding.Current.ProductName + "_<дата>_<время>.log\n" +
			"Настройки: <каталог программы>\\config\\settings.json\n");
	}

	private static AgentCtx CreateContext(string root, out ConfigLoader cfg, out DataStore store)
	{
		cfg = new ConfigLoader(root);
		cfg.Load();
		string logDir = Path.Combine(cfg.Settings.DataDir, "logs");
		Log.Init(logDir, cfg.Settings.LogLevel, cfg.Settings.LogMaxMb, echo: true);
		Log.Cleanup(logDir, cfg.Settings.LogKeepDays);
		Log.Info("=== " + Branding.Current.FullName + " " + AgentVersion + " ===");
		Log.Info("Каталог программы: " + root);
		Log.Info("Каталог данных:    " + cfg.Settings.DataDir);
		Log.Info("Журнал:            " + Log.CurrentFile);
		foreach (string err in cfg.LoadErrors)
		{
			Log.Warn("Конфигурация: " + err);
		}
		store = new DataStore(cfg.Settings.DataDir);
		store.Load();
		PruneHistoryIfNeeded(store, cfg.Settings);
		List<string> knownIds = new List<string>();
		foreach (Company c in cfg.Companies)
		{
			knownIds.Add(c.Id);
		}
		store.PruneOrphans(knownIds);
		return new AgentCtx(cfg.Settings, cfg, store);
	}

	/// <summary>
	/// Чистит историю по политике хранения. Вызывается при старте: раз в сутки
	/// этого достаточно, а чаще — незачем.
	/// </summary>
	internal static void PruneHistoryIfNeeded(DataStore store, Settings settings)
	{
		if (!settings.HistoryAutoPrune)
		{
			Log.Debug("История: автоочистка выключена (history.autoPrune = false)");
			return;
		}
		try
		{
			store.PruneHistory(settings.HistoryJournalKeepDays, settings.HistoryRollupKeepMonths);
		}
		catch (Exception ex)
		{
			Log.Warn("История: очистка не удалась — " + ex.Message);
		}
	}

	private static int RunOnce(string root)
	{
		AgentCtx ctx = CreateContext(root, out var cfg, out var store);
		Log.Info("Разовый прогон: компаний " + cfg.Companies.Count);
		Scheduler sch = new Scheduler(ctx);
		sch.RunAll();
		Log.Info("Готово. Заданий в состоянии: " + store.GetStates().Count);
		return 0;
	}

	private static int RunCheck(string root, string[] args)
	{
		if (args.Length < 2)
		{
			Console.Error.WriteLine("Укажите компанию: --check <id>");
			return 2;
		}
		AgentCtx ctx = CreateContext(root, out var _, out var _);
		Scheduler sch = new Scheduler(ctx);
		sch.RunCompany(args[1]);
		return 0;
	}

	private static int RunConsole(string root, string[] args)
	{
		AgentCtx ctx = CreateContext(root, out var cfg, out var store);
		Log.Info("Компаний: " + cfg.Companies.Count + ", каталог данных: " + cfg.Settings.DataDir);
		Scheduler scheduler = new Scheduler(ctx);
		scheduler.Version = AgentVersion;
		scheduler.Start();
		Auth auth = new Auth(cfg.Settings.ConfigDir, cfg.Settings.WebSessionMinutes);
		if (auth.GeneratedPassword.Length > 0)
		{
			WriteGeneratedPassword(root, auth.GeneratedPassword);
		}
		AuditLog audit = new AuditLog(cfg.Settings.DataDir);
		WebHost web = new WebHost(cfg.Settings, cfg, store, scheduler, auth, audit);
		web.Start();
		GitHubUpdater.StartBootCheck(cfg.Settings, AgentVersion);
		string url = "http://localhost:" + cfg.Settings.HttpPort + "/";
		Log.Info("Веб-интерфейс: " + url);
		Console.WriteLine();
		Console.WriteLine("  Веб-интерфейс: " + url);
		Console.WriteLine("  Журнал:        " + Log.CurrentFile);
		Console.WriteLine("  Остановка:     Ctrl+C");
		Console.WriteLine();
		ManualResetEvent stop = new ManualResetEvent(initialState: false);
		Console.CancelKeyPress += delegate(object s, ConsoleCancelEventArgs e)
		{
			e.Cancel = true;
			stop.Set();
		};
		// В службе Ctrl+C не приходит — ждём вечно, остановку инициирует AgentService.
		stop.WaitOne();
		Log.Info("Останавливаюсь…");
		web.Stop();
		scheduler.Stop();
		return 0;
	}

	public static void WriteGeneratedPassword(string root, string password)
	{
		try
		{
			string dir = Path.Combine(root, "auth");
			Directory.CreateDirectory(dir);
			string path = Path.Combine(dir, "AGENT.md");
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("# " + Branding.Current.FullName + " — учётные данные агента");
			sb.AppendLine();
			sb.AppendLine("Создано: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
			sb.AppendLine();
			sb.AppendLine("| Пользователь | Пароль | Роль |");
			sb.AppendLine("|---|---|---|");
			sb.AppendLine("| `admin` | `" + password + "` | admin |");
			sb.AppendLine();
			sb.AppendLine("Веб-интерфейс: http://localhost:8080/");
			sb.AppendLine();
			sb.AppendLine("Пароль можно сменить в интерфейсе (Настройки → Сменить пароль)");
			sb.AppendLine("или вручную: `POST /api/password {\"username\":\"admin\",\"password\":\"...\"}`");
			File.WriteAllText(path, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			Log.Warn("Пароль администратора записан в auth/AGENT.md");
		}
		catch (Exception ex)
		{
			Log.Warn("Не удалось записать auth/AGENT.md: " + ex.Message);
		}
	}

	private static void RunService(string root)
	{
		ServiceBase.Run(new AgentService(root));
	}

	private static int RunServiceRemote(string root)
	{
		ServiceBase.Run(new RemoteAgentService(root));
		return 0;
	}

	private static int RunZabbix(string root, string[] args)
	{
		if (args.Length < 2)
		{
			Console.Error.WriteLine("Использование: --zabbix discovery | --zabbix job <имя> <поле>");
			return 2;
		}
		string cmd = args[1].ToLowerInvariant();
		CreateContext(root, out var _, out var store);
		if (cmd == "discovery")
		{
			List<object> data = new List<object>();
			HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (JobState s in store.GetStates())
			{
				if (seen.Add(s.Job))
				{
					Dictionary<string, object> d = Json.Obj();
					d["{#JOB}"] = s.Job;
					d["{#KIND}"] = s.Kind;
					data.Add(d);
				}
			}
			Dictionary<string, object> payload = Json.Obj();
			payload["data"] = data;
			Console.WriteLine(Json.Write(payload));
			return 0;
		}
		if (cmd == "job" && args.Length >= 4)
		{
			string job = args[2];
			string field = args[3].ToLowerInvariant();
			JobState found = null;
			foreach (JobState s2 in store.GetStates())
			{
				if (string.Equals(s2.Job, job, StringComparison.OrdinalIgnoreCase))
				{
					found = s2;
					break;
				}
			}
			if (found == null)
			{
				Console.WriteLine("0");
				return 1;
			}
			switch (field)
			{
			case "status":
			{
				int status = (int)found.Status;
				Console.WriteLine(status.ToString());
				break;
			}
			case "age":
				Console.WriteLine(found.LastSuccess.HasValue ? ((long)(DateTime.Now - found.LastSuccess.Value).TotalSeconds).ToString() : "0");
				break;
			case "size":
				Console.WriteLine(found.SizeBytes.ToString());
				break;
			case "duration":
				Console.WriteLine(found.DurationSeconds.ToString());
				break;
			case "failures":
				Console.WriteLine(found.ConsecutiveFailures.ToString());
				break;
			case "message":
				Console.WriteLine(found.Message ?? "");
				break;
			default:
				Console.Error.WriteLine("Неизвестное поле: " + field);
				return 2;
			}
			return 0;
		}
		Console.Error.WriteLine("Неизвестная команда: " + cmd);
		return 2;
	}
}
