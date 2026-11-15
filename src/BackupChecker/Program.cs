using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using BackupChecker.Core;
using BackupChecker.Web;

namespace BackupChecker;

public static class Program
{
	public const string AgentVersion = "1.0.0";

	private static string DiagDir()
	{
		string d = Branding.Current.DataDir;
		if (!string.IsNullOrEmpty(d))
		{
			return d;
		}
		return "C:\\ProgramData\\BackupDotNet";
	}

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
			File.AppendAllText(Path.Combine(dir, "diag.log"), line, Encoding.UTF8);
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
		Diag("start mode=" + mode + " root=" + root + " user=" + Environment.UserName + " interactive=" + Environment.UserInteractive);
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
				Console.WriteLine(Branding.Current.FullName + " 1.0.0");
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
			return 1;
		}
	}

	private static void PrintHelp()
	{
		Console.WriteLine(Branding.Current.FullName + " 1.0.0\n\nРежимы:\n  --console          запустить агент с веб-интерфейсом (отладка)\n  --service          запустить как службу Windows\n  --once             один прогон всех проверок и выход\n  --check <company>  один прогон конкретной компании\n  --zabbix <cmd>     режим для Zabbix agent (pull):\n                       discovery          — JSON для LLD\n                       job <name> <field> — значение поля задания\n  --remote           удалённый агент (P8): забирает конфигурацию с\n                     центрального агента и отправляет результаты\n  --version          версия\n  --help             эта справка\n\nПримеры:\n  BackupChecker.exe --once\n  BackupChecker.exe --console\n  BackupChecker.exe --remote\n  BackupChecker.exe --zabbix job \"Nightly VM Backup\" status\n");
	}

	private static AgentCtx CreateContext(string root, out ConfigLoader cfg, out DataStore store)
	{
		cfg = new ConfigLoader(root);
		cfg.Load();
		Log.Init(Path.Combine(cfg.Settings.DataDir, "logs"), cfg.Settings.LogLevel, cfg.Settings.LogMaxMb, echo: true);
		Log.Cleanup(Path.Combine(cfg.Settings.DataDir, "logs"), cfg.Settings.LogKeepDays);
		foreach (string err in cfg.LoadErrors)
		{
			Log.Warn("Конфигурация: " + err);
		}
		store = new DataStore(cfg.Settings.DataDir);
		store.Load();
		List<string> knownIds = new List<string>();
		foreach (Company c in cfg.Companies)
		{
			knownIds.Add(c.Id);
		}
		store.PruneOrphans(knownIds);
		return new AgentCtx(cfg.Settings, cfg, store);
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
		Log.Info("=== " + cfg.Settings.AgentName + " 1.0.0 ===");
		Log.Info("Компаний: " + cfg.Companies.Count + ", каталог данных: " + cfg.Settings.DataDir);
		Scheduler scheduler = new Scheduler(ctx);
		scheduler.Version = "1.0.0";
		scheduler.Start();
		Auth auth = new Auth(cfg.Settings.ConfigDir, cfg.Settings.WebSessionMinutes);
		if (auth.GeneratedPassword.Length > 0)
		{
			WriteGeneratedPassword(root, auth.GeneratedPassword);
		}
		AuditLog audit = new AuditLog(cfg.Settings.DataDir);
		WebHost web = new WebHost(cfg.Settings, cfg, store, scheduler, auth, audit);
		web.Start();
		Log.Info("Веб-интерфейс: http://localhost:" + cfg.Settings.HttpPort + "/");
		Console.WriteLine();
		Console.WriteLine("  Веб-интерфейс: http://localhost:" + cfg.Settings.HttpPort + "/");
		Console.WriteLine("  Остановка:     Ctrl+C");
		Console.WriteLine();
		ManualResetEvent stop = new ManualResetEvent(initialState: false);
		Console.CancelKeyPress += (object s, ConsoleCancelEventArgs e) =>
		{
			e.Cancel = true;
			stop.Set();
		};
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
			sb.AppendLine("# BackupChecker — учётные данные агента");
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
			Console.Error.WriteLine("Использование: --zabbix discovery | --zabbix job <name> <field>");
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
				if (!seen.Contains(s.Job))
				{
					seen.Add(s.Job);
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
