using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using BackupDotNet.Core;

namespace BackupDotNet.Setup;

/// <summary>
/// Точка входа установщика.
///
/// Режимы:
///   (без ключей)     — окно мастера на WebView2;
///   --browser        — мастер открывается в браузере по умолчанию;
///   --silent-local   — установка на эту машину без интерфейса (для GPO);
///   --version        — версия установщика.
///
/// Окно — это только оболочка: вся логика установки живёт в SetupEngine,
/// поэтому один и тот же код работает и из окна, и из командной строки.
/// </summary>
internal static class Program
{
	/// <summary>Версия установщика (совпадает с версией продукта).</summary>
	public const string SetupVersion = "1.0.1";

	[STAThread]
	private static int Main(string[] args)
	{
		// Логи установщика — рядом с exe: писать в ProgramData до установки
		// нельзя, прав может не быть.
		string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "setup-logs");
		try
		{
			Directory.CreateDirectory(logDir);
		}
		catch
		{
			logDir = Path.GetTempPath();
		}
		Log.Init(logDir, "info", 8, echo: false);

		bool browserMode = HasFlag(args, "--browser");
		bool silent = HasFlag(args, "--silent-local");

		if (HasFlag(args, "--version"))
		{
			Console.WriteLine(SetupVersion);
			return 0;
		}

		Log.Info("=== Установщик DPT.Backup " + SetupVersion + " ===");
		Log.Info("Каталог: " + AppDomain.CurrentDomain.BaseDirectory);
		Log.Info("Права администратора: " + (EnvProbe.IsAdmin() ? "да" : "нет"));

		if (silent)
		{
			return RunSilent();
		}

		return RunUi(browserMode);
	}

	/// <summary>Обычный запуск: локальный сервер плюс окно (или браузер).</summary>
	private static int RunUi(bool browserMode)
	{
		string uiDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "setup-ui");
		if (!Directory.Exists(uiDir))
		{
			MessageBox.Show(
				"Не найден каталог setup-ui рядом с установщиком.\r\n" +
				"Распакуйте архив полностью, не отделяя BackupDotNet.Setup.exe от папок.",
				"Установка DPT.Backup", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return 2;
		}

		SetupSession session = new SetupSession(uiDir);
		InstallRunner runner = new InstallRunner();

		Dictionary<string, Func<string, string>> api = new Dictionary<string, Func<string, string>>(
			StringComparer.OrdinalIgnoreCase)
		{
			{ "/api/env", delegate { return Json.Write(EnvProbe.Collect()); } },
			{ "/api/session", delegate { return Json.Write(session.ToJson()); } },

			// Проверка условий: ничего не меняет, только отвечает.
			{ "/api/preflight", delegate (string body)
				{
					SetupEngine.Request request = ParseRequest(body);
					SetupEngine engine = new SetupEngine();
					return Json.Write(engine.Check(request).ToJson());
				}
			},

			// Проверка связи с удалённой машиной: ничего не меняет.
			{ "/api/remote/test", delegate (string body)
				{
					Dictionary<string, object> d = ParseJson(body);
					string host = Json.GetStr(d, "host", "").Trim();
					string user = Json.GetStr(d, "user", "").Trim();
					string password = Json.GetStr(d, "password", "");
					string mode = Json.GetStr(d, "mode", "smb");

					SmbInstaller.Outcome outcome = mode == "winrm"
						? WinRmInstaller.Test(host, user, password,
							Json.Int(Json.Get(d, "port"), 5985),
							Json.Bool(Json.Get(d, "https"), dflt: false))
						: SmbInstaller.Test(host, user, password);

					return Json.Write(outcome.ToJson());
				}
			},

			// Запуск установки: отвечает сразу, дальше страница опрашивает прогресс.
			{ "/api/install/local", delegate (string body)
				{
					SetupEngine.Request request = ParseRequest(body);
					bool started = runner.Start(request);
					Dictionary<string, object> d = Json.Obj();
					d["ok"] = true;
					d["started"] = started;
					if (!started)
					{
						d["note"] = "установка уже идёт";
					}
					return Json.Write(d);
				}
			},

			{ "/api/install/remote", delegate (string body)
				{
					Dictionary<string, object> d = ParseJson(body);
					bool started = runner.StartRemote(d);
					Dictionary<string, object> res = Json.Obj();
					res["ok"] = true;
					res["started"] = started;
					if (!started)
					{
						res["note"] = "установка уже идёт";
					}
					return Json.Write(res);
				}
			},

			{ "/api/progress", delegate { return Json.Write(runner.Snapshot()); } },

			// «Закрыть мастер»: страница просит завершить процесс.
			{ "/api/close", delegate
				{
					Log.Info("Мастер: закрытие по запросу страницы");
					// Небольшая задержка, чтобы ответ успел уйти.
					System.Threading.Tasks.Task.Run(delegate
					{
						System.Threading.Thread.Sleep(400);
						Environment.Exit(0);
					});
					return "{\"ok\":true}";
				}
			}
		};

		using LocalServer server = new LocalServer(uiDir, api);
		if (!server.Start())
		{
			MessageBox.Show("Не удалось запустить локальный сервер мастера.",
				"Установка DPT.Backup", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return 3;
		}

		Log.Info("Мастер доступен: " + server.Url);

		// В режиме браузера окно не нужно вовсе.
		if (browserMode)
		{
			SetupWindow.OpenInBrowser(server.Url);
			// Ждём, пока страница не закроется: пользователь работает в браузере,
			// а процесс держит сервер. Ctrl+C или закрытие консоли завершит его.
			Console.WriteLine("Мастер открыт в браузере: " + server.Url);
			Console.WriteLine("Нажмите Enter, чтобы остановить сервер и выйти.");
			Console.ReadLine();
			return 0;
		}

		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);

		using SetupWindow window = new SetupWindow(server.Url, uiDir);
		Application.Run(window);

		// Если WebView2 не завёлся, окно показало кнопку «открыть в браузере».
		// Сервер живёт, пока окно открыто, — этого достаточно.
		return 0;
	}

	/// <summary>Тихая установка на эту машину (для startup-скрипта GPO).</summary>
	private static int RunSilent()
	{
		Log.Info("Тихая установка на локальную машину");
		SetupEngine engine = new SetupEngine();
		SetupEngine.Request request = new SetupEngine.Request
		{
			Mode = "local",
			InstallDir = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "DPT.Backup"),
			DataDir = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DPT.Backup"),
			ServiceName = "DPTBackupService",
			Port = 8080
		};

		SetupEngine.Result result = engine.InstallLocal(request, delegate { });
		Console.WriteLine(result.Ok ? "Установлено: " + result.Message : "Ошибка: " + result.Message);
		return result.Ok ? 0 : 1;
	}

	/// <summary>Разбирает тело запроса в словарь, не бросая исключений.</summary>
	private static Dictionary<string, object> ParseJson(string body)
	{
		try
		{
			if (!string.IsNullOrWhiteSpace(body))
			{
				return Json.ParseObject(body);
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Мастер, разбор JSON: " + ex.Message);
		}
		return Json.Obj();
	}

	/// <summary>Разбирает тело запроса страницы в параметры установки.</summary>
	private static SetupEngine.Request ParseRequest(string body)
	{
		Dictionary<string, object> d = new Dictionary<string, object>();
		try
		{
			if (!string.IsNullOrWhiteSpace(body))
			{
				d = Json.ParseObject(body);
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Мастер, разбор запроса: " + ex.Message);
		}

		SetupEngine.Request r = new SetupEngine.Request();
		r.Mode = Json.GetStr(d, "mode", "local");
		r.InstallDir = Json.GetStr(d, "installDir", "").Trim();
		r.DataDir = Json.GetStr(d, "dataDir", "").Trim();
		r.ServiceName = Json.GetStr(d, "serviceName", "DPTBackupService").Trim();
		r.Port = Json.Int(Json.Get(d, "port"), 8080);
		r.OverwriteConfig = Json.Bool(Json.Get(d, "overwriteConfig"), dflt: false);

		// Пустые каталоги — подставляем разумные умолчания, а не падаем.
		if (r.InstallDir.Length == 0)
		{
			r.InstallDir = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "DPT.Backup");
		}
		if (r.DataDir.Length == 0)
		{
			r.DataDir = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DPT.Backup");
		}
		if (r.ServiceName.Length == 0)
		{
			r.ServiceName = "DPTBackupService";
		}
		return r;
	}

	private static bool HasFlag(string[] args, string flag)	{
		if (args == null)
		{
			return false;
		}
		foreach (string a in args)
		{
			if (string.Equals(a, flag, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}
}
