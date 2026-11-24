using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace BackupDotNet.Core.Update;

/// <summary>
/// Установка скачанного обновления.
///
/// Главное решение: <b>мы не подменяем файлы работающего процесса</b>. На Windows
/// это почти всегда заканчивается полуобновлённым каталогом: exe занят, часть
/// dll скопировалась, служба не стартует. Поэтому установка разбита на два шага:
///
///  1. здесь — распаковка в отдельный каталог и проверка, что внутри есть всё
///     нужное. Ничего в рабочем каталоге не меняется;
///  2. отдельным скриптом — после выхода процесса: резервная копия текущей
///     версии, копирование файлов, запуск обратно.
///
/// Если что-то пойдёт не так, скрипт оставляет резервную копию и пишет отчёт
/// в <c>updates/install.log</c> — откат делается копированием одного каталога.
/// </summary>
public static class UpdateInstaller
{
	/// <summary>Распаковывает архив в staging и проверяет содержимое.</summary>
	public static string Stage(Settings settings, string archivePath, string version, out string error)
	{
		error = "";
		if (string.IsNullOrEmpty(archivePath) || !File.Exists(archivePath))
		{
			error = "архив не найден";
			return null;
		}

		string root = Path.Combine(settings.DataDir, "updates");
		string staging = Path.Combine(root, "staging", SafeName(version));
		try
		{
			if (Directory.Exists(staging))
			{
				Directory.Delete(staging, recursive: true);
			}
			Directory.CreateDirectory(staging);

			using (ZipArchive zip = ZipFile.OpenRead(archivePath))
			{
				foreach (ZipArchiveEntry entry in zip.Entries)
				{
					// Compress-Archive на Windows PowerShell 5.1 пишет каталоги
					// с обратными косыми, а не с прямыми. Без нормализации запись
					// «public\vendor\» считается файлом, и распаковка падает.
					string entryName = (entry.FullName ?? "").Replace('\\', '/');
					if (entryName.Length == 0)
					{
						continue;
					}

					// Защита от «zip slip»: имя не должно выводить за staging.
					string dest = Path.GetFullPath(Path.Combine(staging,
						entryName.Replace('/', Path.DirectorySeparatorChar)));
					if (!dest.StartsWith(Path.GetFullPath(staging), StringComparison.OrdinalIgnoreCase))
					{
						error = "архив содержит недопустимый путь: " + entry.FullName;
						return null;
					}

					if (entryName.EndsWith("/", StringComparison.Ordinal))
					{
						Directory.CreateDirectory(dest);
						continue;
					}

					// Пустой файл без имени — служебная запись, пропускаем.
					if (entryName.EndsWith("/.", StringComparison.Ordinal) || entryName == ".")
					{
						continue;
					}

					string parent = Path.GetDirectoryName(dest);
					if (!string.IsNullOrEmpty(parent))
					{
						Directory.CreateDirectory(parent);
					}
					entry.ExtractToFile(dest, overwrite: true);
				}
			}

			// Проверяем, что распаковалось именно то, что нужно.
			string exe = Path.Combine(staging, "BackupDotNet.exe");
			string index = Path.Combine(staging, "public", "index.html");
			if (!File.Exists(exe))
			{
				error = "после распаковки нет BackupDotNet.exe";
				return null;
			}
			if (!File.Exists(index))
			{
				error = "после распаковки нет public/index.html";
				return null;
			}

			Log.Info("Обновление: распаковано в " + staging);
			return staging;
		}
		catch (Exception ex)
		{
			error = "не удалось распаковать архив: " + ex.Message;
			Log.Error("Обновление: распаковка", ex);
			return null;
		}
	}

	/// <summary>
	/// Готовит и запускает скрипт установки. Возвращает путь к скрипту.
	/// Процесс должен завершиться самостоятельно — скрипт ждёт именно этого.
	/// </summary>
	public static string Launch(Settings settings, string stagingPath, string version,
		bool restartService, string serviceName, out string error)
	{
		error = "";
		if (string.IsNullOrEmpty(stagingPath) || !Directory.Exists(stagingPath))
		{
			error = "нет подготовленных файлов для установки";
			return null;
		}

		string installDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
		string root = Path.Combine(settings.DataDir, "updates");
		string backupDir = Path.Combine(root, "backup", SafeName(version) + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
		string scriptPath = Path.Combine(root, "apply-update.cmd");
		string logPath = Path.Combine(root, "install.log");

		try
		{
			Directory.CreateDirectory(root);
			string script = BuildScript(installDir, stagingPath, backupDir, logPath,
				restartService, serviceName);
			File.WriteAllText(scriptPath, script, new UTF8Encoding(false));
		}
		catch (Exception ex)
		{
			error = "не удалось подготовить скрипт установки: " + ex.Message;
			return null;
		}

		try
		{
			// Три уровня нужны, чтобы окно закрылось само:
			//   внешний cmd /c      — запускает и сразу выходит;
			//   start "" /min       — отвязывает от нашего процесса, чтобы скрипт
			//                         пережил завершение агента;
			//   внутренний cmd /c   — гарантирует, что скрипт выполнится и закроется.
			// Без внутреннего /c Windows открывает .cmd как «cmd /K», окно остаётся
			// висеть и держит каталог обновлений заблокированным.
			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName = "cmd.exe",
				Arguments = "/c start \"\" /min cmd.exe /c \"" + scriptPath + "\"",
				UseShellExecute = false,
				CreateNoWindow = true,
				WorkingDirectory = Path.GetDirectoryName(scriptPath)
			};
			Process.Start(psi);
			Log.Warn("Обновление: скрипт установки запущен, агент сейчас завершится. " +
				"Резервная копия будет в " + backupDir);
			return scriptPath;
		}
		catch (Exception ex)
		{
			error = "не удалось запустить установку: " + ex.Message;
			return null;
		}
	}

	/// <summary>
	/// Скрипт установки. Написан максимально «тупым»: без циклов и разбора
	/// аргументов, чтобы его можно было прочитать глазами при разборе аварии.
	/// </summary>
	private static string BuildScript(string installDir, string staging, string backupDir,
		string logPath, bool restartService, string serviceName)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("@echo off");
		sb.AppendLine("setlocal");
		sb.AppendLine("rem ==== Установка обновления BackupDotNet ====");
		sb.AppendLine("rem Скрипт ждёт завершения агента, сохраняет текущую версию и");
		sb.AppendLine("rem копирует новые файлы. Конфигурация и данные не затрагиваются.");
		sb.AppendLine();
		sb.AppendLine("set \"INSTALL=" + installDir + "\"");
		sb.AppendLine("set \"STAGING=" + staging + "\"");
		sb.AppendLine("set \"BACKUP=" + backupDir + "\"");
		sb.AppendLine("set \"LOG=" + logPath + "\"");
		sb.AppendLine("set \"SVC=" + (serviceName ?? "BackupDotNetService") + "\"");
		sb.AppendLine();
		sb.AppendLine("echo [%date% %time%] старт установки >> \"%LOG%\"");
		sb.AppendLine();
		sb.AppendLine("rem --- 1. ждём, пока агент отпустит файлы (до 60 секунд)");
		sb.AppendLine("set /a tries=0");
		sb.AppendLine(":waitloop");
		sb.AppendLine("set /a tries+=1");
		sb.AppendLine("if %tries% gtr 60 goto giveup");
		sb.AppendLine("if exist \"%INSTALL%\\BackupDotNet.exe.lock\" goto waitmore");
		sb.AppendLine("tasklist /FI \"IMAGENAME eq BackupDotNet.exe\" 2>nul | find /I \"BackupDotNet.exe\" >nul");
		sb.AppendLine("if not errorlevel 1 goto waitmore");
		sb.AppendLine("goto proceed");
		sb.AppendLine(":waitmore");
		sb.AppendLine("timeout /t 1 /nobreak >nul");
		sb.AppendLine("goto waitloop");
		sb.AppendLine(":giveup");
		sb.AppendLine("echo [%date% %time%] ОШИБКА: агент не завершился за 60 секунд >> \"%LOG%\"");
		sb.AppendLine("exit /b 1");
		sb.AppendLine();
		sb.AppendLine(":proceed");
		sb.AppendLine("echo [%date% %time%] агент остановлен, сохраняю текущую версию >> \"%LOG%\"");
		sb.AppendLine();
		sb.AppendLine("rem --- 2. резервная копия текущей версии (без данных и конфигурации)");
		sb.AppendLine("mkdir \"%BACKUP%\" 2>nul");
		sb.AppendLine("robocopy \"%INSTALL%\" \"%BACKUP%\" /E /XD \"%INSTALL%\\data\" \"%INSTALL%\\config\" \"%INSTALL%\\auth\" /NFL /NDL /NJH /NJS /NP >nul");
		sb.AppendLine("if errorlevel 8 echo [%date% %time%] ПРЕДУПРЕЖДЕНИЕ: резервная копия неполная >> \"%LOG%\"");
		sb.AppendLine();
		sb.AppendLine("rem --- 3. копируем новые файлы, не трогая config, data, auth");
		sb.AppendLine("echo [%date% %time%] копирую новые файлы >> \"%LOG%\"");
		sb.AppendLine("robocopy \"%STAGING%\" \"%INSTALL%\" /E /XD \"%STAGING%\\config\" \"%STAGING%\\data\" \"%STAGING%\\auth\" /XF \"*.log\" /NFL /NDL /NJH /NJS /NP >> \"%LOG%\" 2>&1");
		sb.AppendLine("if errorlevel 8 goto failed");
		sb.AppendLine();
		sb.AppendLine("rem --- 4. запускаем обратно");
		sb.AppendLine("echo [%date% %time%] установка завершена, запускаю агент >> \"%LOG%\"");
		sb.AppendLine("sc query \"%SVC%\" >nul 2>&1");
		sb.AppendLine("if errorlevel 1 goto runconsole");
		sb.AppendLine("sc start \"%SVC%\" >> \"%LOG%\" 2>&1");
		sb.AppendLine("goto done");
		sb.AppendLine(":runconsole");
		sb.AppendLine("rem Не служба: запускаем в консоли, как было");
		sb.AppendLine("start \"BackupDotNet\" /D \"%INSTALL%\" \"%INSTALL%\\BackupDotNet.exe\" --console");
		sb.AppendLine("goto done");
		sb.AppendLine();
		sb.AppendLine(":failed");
		sb.AppendLine("echo [%date% %time%] ОШИБКА копирования. Откат: скопируйте %BACKUP% обратно в %INSTALL% >> \"%LOG%\"");
		sb.AppendLine("exit /b 1");
		sb.AppendLine();
		sb.AppendLine(":done");
		sb.AppendLine("echo [%date% %time%] готово >> \"%LOG%\"");
		sb.AppendLine("endlocal");
		sb.AppendLine("exit /b 0");
		return sb.ToString();
	}

	/// <summary>Есть ли подготовленное, но не установленное обновление.</summary>
	public static Dictionary<string, object> StagedInfo(Settings settings)
	{
		Dictionary<string, object> d = Json.Obj();
		string root = Path.Combine(settings.DataDir, "updates");
		d["stagingRoot"] = Path.Combine(root, "staging");
		d["backupRoot"] = Path.Combine(root, "backup");
		d["installLog"] = Path.Combine(root, "install.log");

		List<object> staged = new List<object>();
		try
		{
			string dir = Path.Combine(root, "staging");
			if (Directory.Exists(dir))
			{
				foreach (string sub in Directory.GetDirectories(dir))
				{
					Dictionary<string, object> s = Json.Obj();
					s["version"] = Path.GetFileName(sub);
					s["path"] = sub;
					s["bytes"] = DirSize(sub);
					staged.Add(s);
				}
			}
		}
		catch
		{
		}
		d["staged"] = staged;

		List<object> backups = new List<object>();
		try
		{
			string dir = Path.Combine(root, "backup");
			if (Directory.Exists(dir))
			{
				foreach (string sub in Directory.GetDirectories(dir))
				{
					Dictionary<string, object> s = Json.Obj();
					s["name"] = Path.GetFileName(sub);
					s["path"] = sub;
					s["createdAt"] = Directory.GetCreationTime(sub);
					backups.Add(s);
				}
			}
		}
		catch
		{
		}
		// Свежие сверху: при откате нужна последняя.
		backups.Sort((a, b) => string.Compare(
			Json.GetStr(Json.AsObject(b), "name", ""),
			Json.GetStr(Json.AsObject(a), "name", ""), StringComparison.OrdinalIgnoreCase));
		d["backups"] = backups;
		return d;
	}

	private static long DirSize(string dir)
	{
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
		return total;
	}

	private static string SafeName(string s)
	{
		if (string.IsNullOrWhiteSpace(s))
		{
			return "unknown";
		}
		StringBuilder sb = new StringBuilder();
		char[] invalid = Path.GetInvalidFileNameChars();
		foreach (char ch in s.Trim())
		{
			sb.Append(Array.IndexOf(invalid, ch) >= 0 ? '_' : ch);
		}
		return sb.ToString();
	}
}
