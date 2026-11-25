using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BackupDotNet.Core;
using BackupDotNet.Deploy;

namespace BackupDotNet.Setup;

/// <summary>
/// Установка агента через PowerShell Remoting (WinRM).
///
/// Это штатный путь Windows: в домене работает сразу, в рабочей группе нужен
/// <c>TrustedHosts</c> на цели и разрешённая Basic-аутентификация по HTTP.
/// Сессию и открытие соединения берём из готового <see cref="WinRmSession"/>:
/// там уже учтены варианты имени пользователя (<c>DOMAIN\user</c>, <c>user@domain</c>)
/// и разбор типичных ошибок.
/// </summary>
public static class WinRmInstaller
{
	/// <summary>Параметры.</summary>
	public class Options
	{
		public string Host = "";

		public string User = "";

		public string Password = "";

		public int Port = 5985;

		public bool Https;

		public string InstallDir = SetupBrand.InstallDir;

		public string DataDir = SetupBrand.DataDir;

		public string ServiceName = SetupBrand.ServiceName;

		public int WebPort = 8080;

		public string SourceDir = "";
	}

	/// <summary>
	/// Проверяет связь и права, ничего не меняя.
	/// </summary>
	public static SmbInstaller.Outcome Test(string host, string user, string password, int port, bool https)
	{
		SmbInstaller.Outcome o = new SmbInstaller.Outcome();
		if (string.IsNullOrWhiteSpace(host))
		{
			o.Message = "не указан компьютер";
			return o;
		}

		WinRmSession session = WinRmSession.Open(host.Trim(), port, https, user, password, 25000, out string error);
		if (session == null)
		{
			o.Message = error.Length > 0 ? error : "не удалось подключиться по WinRM";
			o.Warnings.Add("проверьте на цели: служба WinRM запущена, порт " + port + " открыт, " +
				"в рабочей группе нужен TrustedHosts (или используйте способ SMB)");
			return o;
		}

		using (session)
		{
			o.Steps.Add("WinRM-сессия открыта");

			PsResult probe = session.Run(
				"$id = [Security.Principal.WindowsIdentity]::GetCurrent(); " +
				"$isAdmin = (New-Object Security.Principal.WindowsPrincipal($id))." +
				"IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator); " +
				"\"$($env:COMPUTERNAME)|$isAdmin\"", 20000);

			if (!probe.Ok)
			{
				o.Message = "команда на цели не выполнилась: " + probe.Error;
				return o;
			}

			string text = (probe.Output ?? "").Trim();
			string[] parts = text.Split('|');
			string computer = parts.Length > 0 ? parts[0] : "";
			bool isAdmin = parts.Length > 1 && parts[1].Trim().Equals("True", StringComparison.OrdinalIgnoreCase);

			if (!isAdmin)
			{
				o.Message = "учётная запись без прав администратора на " + (computer.Length > 0 ? computer : host);
				return o;
			}

			o.Steps.Add("на " + computer + " есть права администратора");
			o.Ok = true;
			o.Message = "машина готова к установке";
			return o;
		}
	}

	/// <summary>
	/// Устанавливает агент. Файлы передаются через WinRM (base64 в скрипте):
	/// это медленнее SMB, но не требует доступа к админ-шарам.
	/// </summary>
	public static SmbInstaller.Outcome Install(Options options, SetupEngine.ProgressHandler progress)
	{
		SmbInstaller.Outcome o = new SmbInstaller.Outcome();
		progress = progress ?? delegate { };

		if (options == null || string.IsNullOrWhiteSpace(options.Host))
		{
			o.Message = "не указан компьютер";
			return o;
		}

		string source = string.IsNullOrWhiteSpace(options.SourceDir)
			? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "payload")
			: options.SourceDir;
		if (!Directory.Exists(source))
		{
			source = AppDomain.CurrentDomain.BaseDirectory;
		}

		string exe = Path.Combine(source, "BackupDotNet.exe");
		if (!File.Exists(exe))
		{
			o.Message = "в пакете нет BackupDotNet.exe";
			return o;
		}

		progress("Подключаюсь к " + options.Host, 5);
		WinRmSession session = WinRmSession.Open(options.Host.Trim(), options.Port, options.Https,
			options.User, options.Password, 30000, out string error);
		if (session == null)
		{
			o.Message = "нет подключения по WinRM: " + (error.Length > 0 ? error : "неизвестная ошибка");
			return o;
		}

		try
		{
			o.Steps.Add("WinRM-сессия открыта");

			// 1. Подготовка каталога на цели.
			progress("Готовлю каталог на цели", 10);
			string remoteTemp = @"C:\Windows\Temp\bnd-setup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
			PsResult prep = session.Run(
				"$d = '" + remoteTemp.Replace("'", "''") + "'; " +
				"New-Item -ItemType Directory -Force -Path $d | Out-Null; " +
				"New-Item -ItemType Directory -Force -Path (Join-Path $d 'config') | Out-Null; " +
				"$d", 20000);
			if (!prep.Ok)
			{
				o.Message = "не удалось создать каталог на цели: " + prep.Error;
				return o;
			}

			// 2. Передача файлов.
			progress("Передаю файлы", 20);
			int sent = SendFiles(session, source, remoteTemp, progress, o.Warnings);
			if (sent <= 0)
			{
				o.Message = "файлы не переданы";
				return o;
			}
			o.Steps.Add("передано файлов: " + sent);

			// 3. Установка на цели.
			progress("Устанавливаю на цели", 70);
			string script = BuildInstallScript(options, remoteTemp);
			PsResult run = session.Run(script, 300000);
			if (!run.Ok)
			{
				o.Message = "установка на цели не удалась: " + run.Error;
				return o;
			}

			string result = (run.Output ?? "").Trim();
			o.Steps.Add("скрипт установки выполнен");

			// 4. Проверка, что служба поднялась.
			progress("Проверяю службу", 90);
			PsResult check = session.Run(
				"$s = Get-Service -Name '" + options.ServiceName.Replace("'", "''") +
				"' -ErrorAction SilentlyContinue; " +
				"if ($s) { $s.Status.ToString() } else { 'NOTFOUND' }", 30000);

			string state = (check.Output ?? "").Trim();
			if (state == "Running")
			{
				o.Steps.Add("служба " + options.ServiceName + " работает");
			}
			else if (state == "NOTFOUND")
			{
				o.Message = "служба " + options.ServiceName + " не найдена после установки";
				return o;
			}
			else
			{
				o.Warnings.Add("служба в состоянии " + state + " — проверьте журнал на цели");
			}

			// 5. Уборка временного каталога.
			session.Run("Remove-Item -Recurse -Force '" + remoteTemp.Replace("'", "''") +
				"' -ErrorAction SilentlyContinue", 30000);

			progress("Готово", 100);
			o.Ok = true;
			o.Message = "Установлено на " + options.Host + " в " + options.InstallDir +
				". Веб-интерфейс: http://" + options.Host + ":" + options.WebPort + "/";
			if (result.Length > 0)
			{
				o.Steps.Add(result);
			}
			return o;
		}
		catch (Exception ex)
		{
			o.Message = "ошибка установки: " + ex.Message;
			Log.Error("WinRM-установка на " + options.Host + ": " + ex.Message, ex);
			return o;
		}
		finally
		{
			session.Dispose();
		}
	}

	/// <summary>
	/// Передаёт файлы пакета по WinRM. Каждый файл кодируется в base64 и
	/// раскодируется на цели: так не нужен ни SMB, ни общий каталог.
	/// </summary>
	private static int SendFiles(WinRmSession session, string sourceDir, string remoteDir,
		SetupEngine.ProgressHandler progress, List<string> warnings)
	{
		string localFull = Path.GetFullPath(sourceDir);
		List<string> files = new List<string>(Directory.GetFiles(localFull, "*", SearchOption.AllDirectories));

		// Установщик и его служебные каталоги агенту не нужны.
		files.RemoveAll(f =>
		{
			string rel = f.Substring(localFull.Length).TrimStart('\\');
			return rel.StartsWith("setup-logs", StringComparison.OrdinalIgnoreCase) ||
				rel.StartsWith("setup-ui", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(Path.GetFileName(f), "BackupDotNet.Setup.exe", StringComparison.OrdinalIgnoreCase);
		});

		int sent = 0;
		for (int i = 0; i < files.Count; i++)
		{
			string file = files[i];
			string relative = file.Substring(localFull.Length).TrimStart('\\');
			string remotePath = remoteDir + "\\" + relative;

			try
			{
				byte[] data = File.ReadAllBytes(file);
				string b64 = Convert.ToBase64String(data);

				// Скрипт передаёт файл по частям, чтобы не упереться в лимит
				// сообщения WinRM (по умолчанию 500 КБ на команду).
				StringBuilder script = new StringBuilder();
				script.Append("$path = '").Append(remotePath.Replace("'", "''")).Append("'; ");
				script.Append("$dir = Split-Path -Parent $path; ");
				script.Append("if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }; ");
				script.Append("$b64 = ''");
				int chunk = 60000;
				for (int pos = 0; pos < b64.Length; pos += chunk)
				{
					int len = Math.Min(chunk, b64.Length - pos);
					script.Append("; $b64 += '").Append(b64.Substring(pos, len)).Append("'");
				}
				script.Append("; [IO.File]::WriteAllBytes($path, [Convert]::FromBase64String($b64))");

				PsResult r = session.Run(script.ToString(), 120000);
				if (!r.Ok)
				{
					warnings.Add("не передан " + relative + ": " + r.Error);
					continue;
				}
				sent++;
			}
			catch (Exception ex)
			{
				warnings.Add("не передан " + relative + ": " + ex.Message);
			}

			int percent = 20 + (int)(45.0 * (i + 1) / Math.Max(1, files.Count));
			progress("Передаю файлы (" + (i + 1) + " из " + files.Count + ")", percent);
		}
		return sent;
	}

	/// <summary>Скрипт установки, который выполняется на целевой машине.</summary>
	private static string BuildInstallScript(Options options, string workDir)
	{
		string installDir = options.InstallDir.TrimEnd('\\');
		string dataDir = options.DataDir.TrimEnd('\\');
		string svc = options.ServiceName;

		StringBuilder sb = new StringBuilder();
		sb.Append("$ErrorActionPreference = 'Stop'; ");
		sb.Append("$work = '").Append(workDir.Replace("'", "''")).Append("'; ");
		sb.Append("$dst = '").Append(installDir.Replace("'", "''")).Append("'; ");
		sb.Append("$data = '").Append(dataDir.Replace("'", "''")).Append("'; ");
		sb.Append("$svc = '").Append(svc.Replace("'", "''")).Append("'; ");

		// Каталоги.
		sb.Append("New-Item -ItemType Directory -Force -Path $dst, $data | Out-Null; ");

		// Копирование.
		sb.Append("Copy-Item -Path (Join-Path $work '*') -Destination $dst -Recurse -Force; ");

		// Конфигурация из шаблонов.
		sb.Append("Get-ChildItem (Join-Path $dst 'config') -Filter '*.example.json' | ForEach-Object { ");
		sb.Append("  $target = Join-Path (Split-Path $_.FullName) ($_.Name -replace '\\.example\\.json$', '.json'); ");
		sb.Append("  if (-not (Test-Path $target)) { Copy-Item $_.FullName $target } }; ");

		// env.json.
		sb.Append("$envJson = @{ installDir = ($dst + '\\'); dataDir = $data; serviceName = $svc; ");
		sb.Append("serviceNameRemote = '" + SetupBrand.ServiceNameRemote + "' } | ConvertTo-Json; ");
		sb.Append("Set-Content -Path (Join-Path $dst 'config\\env.json') -Value $envJson -Encoding UTF8; ");

		// Служба.
		sb.Append("if (Get-Service -Name $svc -ErrorAction SilentlyContinue) { ");
		sb.Append("  Stop-Service -Name $svc -Force -ErrorAction SilentlyContinue; ");
		sb.Append("  & sc.exe delete $svc | Out-Null; Start-Sleep -Seconds 2 }; ");
		sb.Append("$exe = Join-Path $dst 'BackupDotNet.exe'; ");
		sb.Append("& sc.exe create $svc binPath= ('\"' + $exe + '\" --service') start= auto | Out-Null; ");
		sb.Append("& sc.exe description $svc 'Мониторинг бэкапов, доступности и железа' | Out-Null; ");
		sb.Append("& sc.exe failure $svc reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null; ");

		// Firewall и urlacl.
		sb.Append("& netsh advfirewall firewall delete rule name='" + SetupBrand.FirewallRule + "' | Out-Null; ");
		sb.Append("& netsh advfirewall firewall add rule name='" + SetupBrand.FirewallRule + "' dir=in action=allow protocol=TCP localport=")
			.Append(options.WebPort).Append(" | Out-Null; ");
		sb.Append("& netsh http add urlacl url=http://+:").Append(options.WebPort)
			.Append("/ user='NT AUTHORITY\\SYSTEM' | Out-Null; ");

		// Запуск.
		sb.Append("Start-Service -Name $svc; Start-Sleep -Seconds 4; ");
		sb.Append("$s = Get-Service -Name $svc; ");
		sb.Append("'установлено в ' + $dst + ', служба ' + $s.Status");

		return sb.ToString();
	}
}
