using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;

using BackupDotNet.Core;
namespace BackupDotNet.Setup;

/// <summary>
/// Установка агента на эту машину.
///
/// Порядок и причины:
///  1. проверить условия (права, место, занятость порта, целостность пакета) —
///     до первого изменения, чтобы не оставить полусобранный каталог;
///  2. остановить и удалить прежнюю службу, если была;
///  3. скопировать файлы;
///  4. создать конфигурацию из шаблонов (существующую не трогаем);
///  5. зарегистрировать службу с восстановлением при падении;
///  6. разрешить порт в firewall и urlacl;
///  7. запустить и дождаться ответа /api/health.
///
/// Откат: при ошибке на шагах 5–7 служба удаляется, чтобы не осталась
/// половина. Скопированные файлы остаются — их удаление не критично,
/// а повторная установка их перезапишет.
/// </summary>
public sealed class SetupEngine
{
	/// <summary>Что просили установить.</summary>
	public class Request
	{
		public string Mode = "local";

		public string InstallDir = "";

		public string DataDir = "";

		public string ServiceName = SetupBrand.ServiceName;

		public int Port = 8080;

		public string SourceDir = "";

		public bool OverwriteConfig = false;
	}

	/// <summary>Чем закончилось.</summary>
	public class Result
	{
		public bool Ok;

		public string Message = "";

		public List<string> Steps = new List<string>();

		public List<string> Warnings = new List<string>();

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["ok"] = Ok;
			d["message"] = Message;
			d["steps"] = Steps;
			d["warnings"] = Warnings;
			if (!Ok)
			{
				d["error"] = Message;
			}
			return d;
		}
	}

	/// <summary>Шаг установки: подпись и процент.</summary>
	public delegate void ProgressHandler(string step, int percent);

	/// <summary>Проверка условий до установки.</summary>
	public class Preflight
	{
		public bool Ok = true;

		public List<string> Problems = new List<string>();

		public List<string> Warnings = new List<string>();

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["ok"] = Ok;
			d["problems"] = Problems;
			d["warnings"] = Warnings;
			return d;
		}
	}

	/// <summary>
	/// Проверяет, можно ли ставить. Ничего не меняет.
	/// </summary>
	public Preflight Check(Request request)
	{
		Preflight p = new Preflight();
		if (request == null)
		{
			p.Ok = false;
			p.Problems.Add("не заданы параметры установки");
			return p;
		}

		if (!EnvProbe.IsAdmin())
		{
			p.Ok = false;
			p.Problems.Add("нужны права администратора: без них нельзя создать службу");
		}

		if (string.IsNullOrWhiteSpace(request.InstallDir))
		{
			p.Ok = false;
			p.Problems.Add("не указан каталог установки");
		}
		else
		{
			try
			{
				string parent = Path.GetDirectoryName(request.InstallDir);
				if (!string.IsNullOrEmpty(parent) && !Directory.Exists(parent))
				{
					p.Warnings.Add("каталог будет создан: " + parent);
				}
			}
			catch (Exception ex)
			{
				p.Ok = false;
				p.Problems.Add("некорректный путь установки: " + ex.Message);
			}
		}

		// Пакет: без него ставить нечего.
		string source = string.IsNullOrWhiteSpace(request.SourceDir)
			? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "payload")
			: request.SourceDir;
		if (!Directory.Exists(source))
		{
			source = AppDomain.CurrentDomain.BaseDirectory;
		}
		if (!File.Exists(Path.Combine(source, "BackupDotNet.exe")))
		{
			p.Ok = false;
			p.Problems.Add("в пакете нет BackupDotNet.exe (искали в " + source + ")");
		}

		// Место: агенту нужно немного, но проверить надо.
		try
		{
			string root = Path.GetPathRoot(request.InstallDir) ?? "C:\\";
			DriveInfo di = new DriveInfo(root);
			if (di.IsReady && di.AvailableFreeSpace < 200L * 1024 * 1024)
			{
				p.Warnings.Add("на диске " + root + " меньше 200 МБ свободно");
			}
		}
		catch
		{
		}

		// Порт: если занят чужой программой, агент не поднимется.
		if (request.Port <= 0 || request.Port > 65535)
		{
			p.Ok = false;
			p.Problems.Add("некорректный порт: " + request.Port);
		}
		else if (PortBusy(request.Port))
		{
			p.Warnings.Add("порт " + request.Port + " уже занят — возможно, агент уже установлен");
		}

		// Прежняя установка: не ошибка, но предупредить надо.
		foreach (string svc in new[] { request.ServiceName, "BackupDotNetService" })
		{
			if (string.IsNullOrWhiteSpace(svc))
			{
				continue;
			}
			if (ServiceExists(svc))
			{
				p.Warnings.Add("служба " + svc + " уже существует и будет переустановлена");
			}
		}

		p.Ok = p.Problems.Count == 0;
		return p;
	}

	/// <summary>
	/// Устанавливает агент локально. Ничего не бросает наружу: любая проблема
	/// возвращается в <see cref="Result"/>.
	/// </summary>
	public Result InstallLocal(Request request, ProgressHandler progress)
	{
		Result r = new Result();
		progress = progress ?? delegate { };

		Preflight pre = Check(request);
		foreach (string w in pre.Warnings)
		{
			r.Warnings.Add(w);
		}
		if (!pre.Ok)
		{
			r.Message = string.Join("; ", pre.Problems.ToArray());
			return r;
		}

		string source = string.IsNullOrWhiteSpace(request.SourceDir)
			? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "payload")
			: request.SourceDir;
		if (!Directory.Exists(source))
		{
			source = AppDomain.CurrentDomain.BaseDirectory;
		}

		string installDir = request.InstallDir.TrimEnd('\\');
		string dataDir = string.IsNullOrWhiteSpace(request.DataDir)
			? SetupBrand.DataDir
			: request.DataDir.TrimEnd('\\');

		bool serviceCreated = false;

		try
		{
			// 1. Остановить и удалить прежнюю службу.
			progress("Останавливаю прежнюю службу", 5);
			if (ServiceExists(request.ServiceName))
			{
				StopService(request.ServiceName);
				DeleteService(request.ServiceName);
				r.Steps.Add("прежняя служба " + request.ServiceName + " остановлена и удалена");
			}

			// 2. Каталоги.
			progress("Создаю каталоги", 12);
			Directory.CreateDirectory(installDir);
			Directory.CreateDirectory(dataDir);
			r.Steps.Add("каталоги: " + installDir + ", " + dataDir);

			// 3. Копирование файлов.
			progress("Копирую файлы", 20);
			int copied = CopyTree(source, installDir, r.Warnings);
			r.Steps.Add("скопировано файлов: " + copied);

			// 4. Конфигурация из шаблонов.
			progress("Готовлю конфигурацию", 45);
			string cfgDir = Path.Combine(installDir, "config");
			int madeConfigs = CreateConfigs(cfgDir, request.OverwriteConfig, r.Warnings);
			if (madeConfigs > 0)
			{
				r.Steps.Add("создано файлов конфигурации: " + madeConfigs);
			}

			// env.json: служба должна знать свои каталоги.
			WriteEnvJson(installDir, dataDir, request.ServiceName);

			// 5. Служба.
			progress("Регистрирую службу", 60);
			string exe = Path.Combine(installDir, "BackupDotNet.exe");
			string scError = CreateService(request.ServiceName, exe, installDir);
			if (scError.Length > 0)
			{
				r.Message = "не удалось создать службу: " + scError;
				return r;
			}
			serviceCreated = true;
			r.Steps.Add("служба " + request.ServiceName + " создана");

			// 6. Firewall и urlacl.
			progress("Открываю порт", 72);
			AllowPort(request.Port, r.Warnings);
			ReserveUrl(request.Port, r.Warnings);
			r.Steps.Add("порт " + request.Port + " разрешён");

			// 7. Запуск и проверка.
			progress("Запускаю службу", 82);
			string startError = StartService(request.ServiceName, 30);
			if (startError.Length > 0)
			{
				r.Message = "служба создана, но не запустилась: " + startError;
				CleanupService(request.ServiceName);
				return r;
			}

			progress("Проверяю ответ агента", 92);
			bool alive = WaitHealth(request.Port, 20);
			if (!alive)
			{
				r.Warnings.Add("служба запущена, но /api/health не ответил за 20 с — " +
					"проверьте журнал в " + Path.Combine(dataDir, "logs"));
			}
			else
			{
				r.Steps.Add("агент отвечает на порту " + request.Port);
			}

			progress("Готово", 100);
			r.Ok = true;
			r.Message = "Установлено в " + installDir + ". Веб-интерфейс: http://localhost:" +
				request.Port + "/";
			Log.Info("Установка завершена: " + installDir + ", служба " + request.ServiceName);
			return r;
		}
		catch (Exception ex)
		{
			Log.Error("Установка: " + ex.Message, ex);
			r.Message = "ошибка установки: " + ex.Message;
			if (serviceCreated)
			{
				CleanupService(request.ServiceName);
			}
			return r;
		}
	}

	// ------------------------------------------------------------------ шаги

	private static int CopyTree(string source, string target, List<string> warnings)
	{
		int count = 0;
		string sourceFull = Path.GetFullPath(source);
		string targetFull = Path.GetFullPath(target);

		// Не копируем установщик в самого себя.
		if (string.Equals(sourceFull, targetFull, StringComparison.OrdinalIgnoreCase))
		{
			warnings.Add("пакет совпадает с каталогом установки — копирование пропущено");
			return 0;
		}

		foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
		{
			string relative = file.Substring(sourceFull.Length).TrimStart('\\');
			string dest = Path.Combine(targetFull, relative);

			// Установщик и его служебные каталоги агенту не нужны.
			if (relative.StartsWith("setup-logs", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(Path.GetFileName(file), "BackupDotNet.Setup.exe", StringComparison.OrdinalIgnoreCase) ||
				relative.StartsWith("setup-ui", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			try
			{
				string dir = Path.GetDirectoryName(dest);
				if (!string.IsNullOrEmpty(dir))
				{
					Directory.CreateDirectory(dir);
				}
				File.Copy(file, dest, overwrite: true);
				count++;
			}
			catch (Exception ex)
			{
				warnings.Add("не скопирован " + relative + ": " + ex.Message);
			}
		}
		return count;
	}

	private static int CreateConfigs(string cfgDir, bool overwrite, List<string> warnings)
	{
		int made = 0;
		if (!Directory.Exists(cfgDir))
		{
			return 0;
		}
		foreach (string template in Directory.GetFiles(cfgDir, "*.example.json"))
		{
			string name = Path.GetFileName(template).Replace(".example.json", ".json");
			string target = Path.Combine(cfgDir, name);
			if (File.Exists(target) && !overwrite)
			{
				continue;
			}
			try
			{
				File.Copy(template, target, overwrite: true);
				made++;
			}
			catch (Exception ex)
			{
				warnings.Add("не создан " + name + ": " + ex.Message);
			}
		}
		return made;
	}

	private static void WriteEnvJson(string installDir, string dataDir, string serviceName)
	{
		Dictionary<string, object> env = Json.Obj();
		env["installDir"] = installDir + "\\";
		env["dataDir"] = dataDir;
		env["serviceName"] = serviceName;
		env["serviceNameRemote"] = SetupBrand.ServiceNameRemote;
		string path = Path.Combine(installDir, "config", "env.json");
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, Json.WritePretty(env));
		}
		catch (Exception ex)
		{
			Log.Warn("env.json: " + ex.Message);
		}
	}

	private static string CreateService(string name, string exePath, string workingDir)
	{
		string binPath = "\"" + exePath + "\" --service";
		string output;
		int code = Run("sc.exe", "create \"" + name + "\" binPath= " + binPath + " start= auto", out output);
		if (code != 0)
		{
			return output;
		}
		Run("sc.exe", "description \"" + name + "\" \"" + SetupBrand.Current.Description + "\"", out output);
		// Перезапуск при падении: три попытки с интервалом в минуту.
		Run("sc.exe",
			"failure \"" + name + "\" reset= 86400 actions= restart/60000/restart/60000/restart/60000",
			out output);
		return "";
	}

	private static void AllowPort(int port, List<string> warnings)
	{
		string output;
		int code = Run("netsh",
			"advfirewall firewall add rule name=\"" + SetupBrand.FirewallRule + "\" dir=in action=allow protocol=TCP localport=" + port,
			out output);
		if (code != 0)
		{
			warnings.Add("правило firewall не добавлено: " + FirstLine(output));
		}
	}

	private static void ReserveUrl(int port, List<string> warnings)
	{
		string output;
		int code = Run("netsh",
			"http add urlacl url=http://+:" + port + "/ user=\"NT AUTHORITY\\SYSTEM\"",
			out output);
		// Код 183 — «уже существует»: это не ошибка.
		if (code != 0 && !output.Contains("183"))
		{
			warnings.Add("urlacl не добавлен: " + FirstLine(output));
		}
	}

	private static string StartService(string name, int timeoutSeconds)
	{
		try
		{
			using ServiceController sc = new ServiceController(name);
			if (sc.Status != ServiceControllerStatus.Running)
			{
				sc.Start();
				sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(timeoutSeconds));
			}
			return "";
		}
		catch (Exception ex)
		{
			return ex.Message;
		}
	}

	private static void StopService(string name)
	{
		try
		{
			using ServiceController sc = new ServiceController(name);
			if (sc.Status != ServiceControllerStatus.Stopped &&
				sc.Status != ServiceControllerStatus.StopPending)
			{
				sc.Stop();
				sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(20));
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Остановка службы " + name + ": " + ex.Message);
		}
	}

	private static void DeleteService(string name)
	{
		try
		{
			Run("sc.exe", "delete \"" + name + "\"", out string _);
		}
		catch (Exception ex)
		{
			Log.Warn("Удаление службы " + name + ": " + ex.Message);
		}
	}

	/// <summary>Убирает службу, если установка сорвалась на позднем шаге.</summary>
	private static void CleanupService(string name)
	{
		try
		{
			StopService(name);
			DeleteService(name);
			Log.Warn("Откат: служба " + name + " удалена после неудачной установки");
		}
		catch
		{
		}
	}

	private static bool WaitHealth(int port, int timeoutSeconds)
	{
		DateTime deadline = DateTime.Now.AddSeconds(timeoutSeconds);
		while (DateTime.Now < deadline)
		{
			try
			{
				using System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
				client.Timeout = TimeSpan.FromSeconds(3);
				string url = "http://127.0.0.1:" + port + "/api/health";
				string body = client.GetStringAsync(url).GetAwaiter().GetResult();
				if (!string.IsNullOrEmpty(body) && body.Contains("\"ok\""))
				{
					return true;
				}
			}
			catch
			{
			}
			System.Threading.Thread.Sleep(700);
		}
		return false;
	}

	private static bool ServiceExists(string name)
	{
		try
		{
			using ServiceController sc = new ServiceController(name);
			ServiceControllerStatus _ = sc.Status;
			return true;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
		catch
		{
			return false;
		}
	}

	private static bool PortBusy(int port)
	{
		try
		{
			System.Net.IPEndPoint[] listeners =
				System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
			foreach (System.Net.IPEndPoint ep in listeners)
			{
				if (ep.Port == port)
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static int Run(string exe, string args, out string output)
	{
		output = "";
		try
		{
			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName = exe,
				Arguments = args,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};
			using Process p = Process.Start(psi);
			output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
			p.WaitForExit(30000);
			return p.HasExited ? p.ExitCode : -1;
		}
		catch (Exception ex)
		{
			output = ex.Message;
			return -1;
		}
	}

	private static string FirstLine(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		int nl = text.IndexOfAny(new[] { '\r', '\n' });
		return (nl > 0 ? text.Substring(0, nl) : text).Trim();
	}
}
