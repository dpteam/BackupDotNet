using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using BackupDotNet.Core;
namespace BackupDotNet.Setup;

/// <summary>
/// Установка агента на чужую машину без WinRM: через админ-шару и sc.exe.
///
/// Как это работает:
///  1. подключаемся к <c>\\host\ADMIN$</c> с учётными данными;
///  2. копируем пакет во временный каталог и генерируем скрипт установки;
///  3. создаём временную службу, которая этот скрипт запускает
///     (<c>sc.exe \\host create</c> работает по RPC, WinRM не нужен);
///  4. ждём появления файла-отчёта, который пишет скрипт;
///  5. удаляем временную службу и каталог.
///
/// Отчёт — обычный JSON на шаре: так не нужно ловить вывод удалённого
/// процесса, который через RPC всё равно не приходит.
/// </summary>
public static class SmbInstaller
{
	/// <summary>Параметры удалённой установки.</summary>
	public class Options
	{
		public string Host = "";

		public string User = "";

		public string Password = "";

		public string InstallDir = SetupBrand.InstallDir;

		public string DataDir = SetupBrand.DataDir;

		public string ServiceName = SetupBrand.ServiceName;

		public int Port = 8080;

		public string SourceDir = "";
	}

	/// <summary>Результат попытки.</summary>
	public class Outcome
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

	/// <summary>
	/// Проверяет, можно ли ставить на машину: связь, шары, права.
	/// Ничего не меняет.
	/// </summary>
	public static Outcome Test(string host, string user, string password)
	{
		Outcome o = new Outcome();
		if (string.IsNullOrWhiteSpace(host))
		{
			o.Message = "не указан компьютер";
			return o;
		}

		string adminShare = @"\\" + host.TrimStart('\\') + @"\ADMIN$";
		using (SmbSession session = SmbSession.Open(adminShare, user, password, out string error))
		{
			if (session == null)
			{
				o.Message = error;
				return o;
			}
			o.Steps.Add("шара ADMIN$ доступна");

			// Права на запись проверяем реальной записью: «видно» и «можно
			// писать» — разные вещи.
			string probe = Path.Combine(adminShare, "Temp", ".bnd-probe-" +
				Guid.NewGuid().ToString("N").Substring(0, 8) + ".tmp");
			try
			{
				string dir = Path.GetDirectoryName(probe);
				if (!Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}
				File.WriteAllText(probe, "probe");
				File.Delete(probe);
				o.Steps.Add("запись в ADMIN$\\Temp разрешена");
			}
			catch (Exception ex)
			{
				o.Message = "нет записи в ADMIN$\\Temp: " + ex.Message;
				return o;
			}
		}

		// sc.exe по сети: без него не создать службу.
		string output;
		int code = Run("sc.exe", "\\\\" + host.TrimStart('\\') + " query type= driver", 20000, out output);
		if (code != 0 && output.IndexOf("Access is denied", StringComparison.OrdinalIgnoreCase) < 0 &&
			output.IndexOf("отказано в доступе", StringComparison.OrdinalIgnoreCase) < 0)
		{
			o.Warnings.Add("sc.exe по сети ответил кодом " + code + ": " + FirstLine(output));
		}
		else
		{
			o.Steps.Add("управление службами по RPC доступно");
		}

		o.Ok = true;
		o.Message = "машина готова к установке";
		return o;
	}

	/// <summary>
	/// Устанавливает агент на удалённую машину.
	/// </summary>
	public static Outcome Install(Options options, SetupEngine.ProgressHandler progress)
	{
		Outcome o = new Outcome();
		progress = progress ?? delegate { };

		if (options == null || string.IsNullOrWhiteSpace(options.Host))
		{
			o.Message = "не указан компьютер";
			return o;
		}

		string host = options.Host.Trim().TrimStart('\\');
		string adminShare = @"\\" + host + @"\ADMIN$";
		string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
		string remoteTemp = Path.Combine(adminShare, "Temp", "bnd-setup-" + stamp);
		string remoteTempWin = @"C:\Windows\Temp\bnd-setup-" + stamp;
		string tempService = "BNDSetup" + stamp.Replace("-", "").Substring(8);

		string source = string.IsNullOrWhiteSpace(options.SourceDir)
			? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "payload")
			: options.SourceDir;
		if (!Directory.Exists(source))
		{
			source = AppDomain.CurrentDomain.BaseDirectory;
		}

		if (!File.Exists(Path.Combine(source, "BackupDotNet.exe")))
		{
			o.Message = "в пакете нет BackupDotNet.exe";
			return o;
		}

		try
		{
			progress("Подключаюсь к " + host, 5);
			using (SmbSession session = SmbSession.Open(adminShare, options.User, options.Password, out string error))
			{
				if (session == null)
				{
					o.Message = "нет доступа к ADMIN$: " + error;
					return o;
				}
				o.Steps.Add("подключение к ADMIN$ установлено");

				// 1. Копирование пакета.
				progress("Копирую файлы", 15);
				int copied = SmbSession.CopyTree(source, remoteTemp, out string copyError, out long bytes);
				if (copied < 0)
				{
					o.Message = "копирование не удалось: " + copyError;
					Cleanup(session, remoteTemp, tempService, o);
					return o;
				}
				o.Steps.Add("скопировано файлов: " + copied +
					", " + Math.Round(bytes / 1048576.0, 1) + " МБ");

				// 2. Скрипт установки.
				progress("Готовлю скрипт", 30);
				string statusPath = Path.Combine(remoteTemp, "status.json");
				string scriptPath = Path.Combine(remoteTemp, "remote-install.cmd");
				File.WriteAllText(scriptPath, BuildScript(options, remoteTempWin), Encoding.UTF8);

				// 3. Временная служба, которая запустит скрипт.
				progress("Создаю временную службу", 40);
				string hostArg = "\\\\" + host;
				string binPath = "cmd.exe /c \"" + Path.Combine(remoteTempWin, "remote-install.cmd") + "\"";
				int code = Run("sc.exe",
					hostArg + " create \"" + tempService + "\" binPath= \"" + binPath + "\" start= demand",
					30000, out string scOut);
				if (code != 0)
				{
					o.Message = "не удалось создать временную службу: " + ExplainScError(scOut);
					Cleanup(session, remoteTemp, tempService, o);
					return o;
				}
				o.Steps.Add("временная служба " + tempService + " создана");

				// 4. Запуск.
				progress("Запускаю установку на " + host, 50);
				Run("sc.exe", hostArg + " start \"" + tempService + "\"", 30000, out scOut);

				// 5. Ожидание отчёта. Скрипт пишет status.json по завершении.
				progress("Жду завершения", 60);
				Dictionary<string, object> status = WaitStatus(statusPath, 300, progress);

				// 6. Уборка.
				progress("Убираю за собой", 92);
				Cleanup(session, remoteTemp, tempService, o);

				if (status == null)
				{
					o.Message = "установка не отчиталась за 5 минут. " +
						"Проверьте машину " + host + " вручную: возможно, скрипт не запустился";
					return o;
				}

				bool ok = Json.Bool(Json.Get(status, "ok"), dflt: false);
				foreach (object step in Json.AsArray(Json.Get(status, "steps")))
				{
					o.Steps.Add(Json.Str(step, ""));
				}
				foreach (object warn in Json.AsArray(Json.Get(status, "warnings")))
				{
					o.Warnings.Add(Json.Str(warn, ""));
				}

				o.Ok = ok;
				o.Message = Json.GetStr(status, "message", ok ? "установлено" : "ошибка установки");
				progress(ok ? "Готово" : "Ошибка", 100);
				return o;
			}
		}
		catch (Exception ex)
		{
			o.Message = "ошибка установки: " + ex.Message;
			Log.Error("SMB-установка на " + host + ": " + ex.Message, ex);
			return o;
		}
	}

	/// <summary>Генерирует скрипт, который выполнится на целевой машине.</summary>
	private static string BuildScript(Options options, string workDir)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("@echo off");
		sb.AppendLine("setlocal");
		sb.AppendLine("chcp 65001 >nul");
		sb.AppendLine("set WORK=" + workDir);
		sb.AppendLine("set OUT=%WORK%\\status.json");
		sb.AppendLine("echo {^\"ok^\":false,^\"message^\":^\"скрипт не дошёл до конца^\"} > \"%OUT%\"");
		sb.AppendLine("");

		// Копирование в целевой каталог.
		sb.AppendLine("echo [10] Создаю каталоги");
		sb.AppendLine("mkdir \"" + options.InstallDir + "\" 2>nul");
		sb.AppendLine("mkdir \"" + options.DataDir + "\" 2>nul");
		sb.AppendLine("if errorlevel 1 goto :fail");
		sb.AppendLine("");

		sb.AppendLine("echo [20] Копирую файлы");
		sb.AppendLine("robocopy \"%WORK%\" \"" + options.InstallDir +
			"\" /E /NFL /NDL /NJH /NJS /NP /XF status.json remote-install.cmd >nul");
		sb.AppendLine("if errorlevel 8 goto :fail");
		sb.AppendLine("");

		// Конфигурация из шаблонов.
		sb.AppendLine("echo [40] Готовлю конфигурацию");
		sb.AppendLine("for %%f in (\"" + options.InstallDir + "\\config\\*.example.json\") do (");
		sb.AppendLine("  if not exist \"" + options.InstallDir + "\\config\\%%~nf.json\" (");
		sb.AppendLine("    copy /y \"%%f\" \"" + options.InstallDir + "\\config\\%%~nf.json\" >nul");
		sb.AppendLine("  )");
		sb.AppendLine(")");
		sb.AppendLine("");

		// Служба.
		sb.AppendLine("echo [60] Регистрирую службу");
		sb.AppendLine("sc stop \"" + options.ServiceName + "\" >nul 2>&1");
		sb.AppendLine("sc delete \"" + options.ServiceName + "\" >nul 2>&1");
		sb.AppendLine("timeout /t 2 /nobreak >nul");
		sb.AppendLine("sc create \"" + options.ServiceName + "\" binPath= \"\\\"" + options.InstallDir +
			"\\BackupDotNet.exe\\\" --service\" start= auto >nul");
		sb.AppendLine("if errorlevel 1 goto :fail");
		sb.AppendLine("sc description \"" + options.ServiceName + "\" \"" + SetupBrand.Current.Description + "\" >nul");
		sb.AppendLine("sc failure \"" + options.ServiceName +
			"\" reset= 86400 actions= restart/60000/restart/60000/restart/60000 >nul");
		sb.AppendLine("");

		// Firewall и urlacl.
		sb.AppendLine("echo [72] Открываю порт");
		sb.AppendLine("netsh advfirewall firewall delete rule name=\"" + SetupBrand.FirewallRule + "\" >nul 2>&1");
		sb.AppendLine("netsh advfirewall firewall add rule name=\"" + SetupBrand.FirewallRule + "\" dir=in action=allow protocol=TCP localport=" +
			options.Port + " >nul");
		sb.AppendLine("netsh http add urlacl url=http://+:" + options.Port +
			"/ user=\"NT AUTHORITY\\SYSTEM\" >nul 2>&1");
		sb.AppendLine("");

		// Запуск.
		sb.AppendLine("echo [82] Запускаю службу");
		sb.AppendLine("sc start \"" + options.ServiceName + "\" >nul");
		sb.AppendLine("timeout /t 5 /nobreak >nul");
		sb.AppendLine("");

		// Отчёт.
		sb.AppendLine("echo {^\"ok^\":true,^\"message^\":^\"установлено в " +
			options.InstallDir.Replace("\\", "\\\\") + "^\",^\"steps^\":[^\"каталоги созданы^\",^\"файлы скопированы^\",^\"служба " +
			options.ServiceName + " создана^\",^\"порт " + options.Port + " открыт^\"]} > \"%OUT%\"");
		sb.AppendLine("exit /b 0");
		sb.AppendLine("");

		sb.AppendLine(":fail");
		sb.AppendLine("echo {^\"ok^\":false,^\"message^\":^\"шаг не выполнен, код %errorlevel%^\"} > \"%OUT%\"");
		sb.AppendLine("exit /b 1");

		return sb.ToString();
	}

	/// <summary>Ждёт файл-отчёт и читает его.</summary>
	private static Dictionary<string, object> WaitStatus(string path, int timeoutSeconds,
		SetupEngine.ProgressHandler progress)
	{
		DateTime deadline = DateTime.Now.AddSeconds(timeoutSeconds);
		int percent = 60;
		while (DateTime.Now < deadline)
		{
			try
			{
				if (File.Exists(path))
				{
					// Файл мог быть прочитан в момент записи: пробуем ещё раз.
					string text = File.ReadAllText(path);
					if (text.TrimEnd().EndsWith("}", StringComparison.Ordinal))
					{
						Dictionary<string, object> d = Json.ParseObject(text);
						if (d.Count > 0)
						{
							return d;
						}
					}
				}
			}
			catch
			{
			}

			System.Threading.Thread.Sleep(1000);
			if (percent < 88)
			{
				percent += 2;
				progress("Жду завершения на целевой машине", percent);
			}
		}
		return null;
	}

	/// <summary>Удаляет временную службу и каталог.</summary>
	private static void Cleanup(SmbSession session, string remoteTemp, string tempService, Outcome o)
	{
		try
		{
			string hostArg = "\\\\" + new DirectoryInfo(remoteTemp).Parent?.Parent?.Name;
			Run("sc.exe", hostArg + " stop \"" + tempService + "\"", 15000, out string _);
			Run("sc.exe", hostArg + " delete \"" + tempService + "\"", 15000, out string _);
		}
		catch (Exception ex)
		{
			o.Warnings.Add("временная служба не удалена: " + ex.Message);
		}

		try
		{
			if (Directory.Exists(remoteTemp))
			{
				Directory.Delete(remoteTemp, recursive: true);
			}
		}
		catch (Exception ex)
		{
			o.Warnings.Add("временный каталог не удалён: " + ex.Message);
		}
	}

	private static int Run(string exe, string args, int timeoutMs, out string output)
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
			p.WaitForExit(timeoutMs);
			return p.HasExited ? p.ExitCode : -1;
		}
		catch (Exception ex)
		{
			output = ex.Message;
			return -1;
		}
	}

	/// <summary>
	/// Переводит вывод sc.exe в понятную фразу. Коды Win32 вида «ошибка 5»
	/// человеку ничего не говорят.
	/// </summary>
	private static string ExplainScError(string output)
	{
		string text = output ?? "";
		if (text.IndexOf("5:", StringComparison.Ordinal) >= 0 ||
			text.IndexOf("Access is denied", StringComparison.OrdinalIgnoreCase) >= 0 ||
			text.IndexOf("отказано в доступе", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "доступ запрещён — учётная запись должна быть администратором на цели";
		}
		if (text.IndexOf("1722", StringComparison.Ordinal) >= 0)
		{
			return "служба RPC на цели недоступна (сервер выключен или блокирует RPC)";
		}
		if (text.IndexOf("53", StringComparison.Ordinal) >= 0)
		{
			return "компьютер не найден в сети";
		}
		if (text.IndexOf("1726", StringComparison.Ordinal) >= 0)
		{
			return "сбой RPC при подключении к цели";
		}
		return FirstLine(text);
	}

	private static string FirstLine(string text)	{
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		int nl = text.IndexOfAny(new[] { '\r', '\n' });
		return (nl > 0 ? text.Substring(0, nl) : text).Trim();
	}
}
