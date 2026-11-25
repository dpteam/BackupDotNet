using System;
using System.Collections.Generic;
using System.Text;

namespace BackupDotNet.Setup;

/// <summary>
/// Скрипты, которые чинят то, что мешает подключиться.
///
/// Важная особенность: чинить надо **на целевой машине**, а доступа к ней
/// как раз и нет. Поэтому скрипт не выполняется сам — он выдаётся текстом
/// для запуска через RDP, консоль или GPO. Это честнее, чем кнопка, которая
/// молча ничего не сделает.
///
/// Скрипт собирается только из тех блоков, которые нужны по результатам
/// проверки: нечего чинить — нечего и запускать.
/// </summary>
public static class FixScripts
{
	/// <summary>Один блок исправления.</summary>
	private class Block
	{
		public string Code = "";

		public string Title = "";

		public string Script = "";
	}

	/// <summary>
	/// Собирает скрипт по кодам проблемных пунктов.
	/// Возвращает пустую строку, если чинить нечего.
	/// </summary>
	public static string Build(IEnumerable<string> brokenCodes, bool forWinRm)
	{
		HashSet<string> need = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (brokenCodes != null)
		{
			foreach (string code in brokenCodes)
			{
				if (!string.IsNullOrWhiteSpace(code))
				{
					need.Add(code.Trim());
				}
			}
		}

		// Если машина не отвечает вовсе (имя не разрешается), скрипт бесполезен:
		// его некуда отнести. Честнее сказать это, чем выдать «включите шары».
		if (need.Contains("dns") || need.Contains("host"))
		{
			return "";
		}

		List<Block> blocks = new List<Block>();

		// Порядок блоков важен: сначала шары, потом права, потом WinRM.
		if (need.Contains("autoshare") || need.Contains("adminshare") || need.Contains("lanman") ||
			need.Contains("smb445"))
		{
			blocks.Add(new Block
			{
				Code = "shares",
				Title = "Включить административные шары",
				Script =
					"# Административные шары ADMIN$ и C$ (нужны для копирования по сети)\n" +
					"Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\LanmanServer\\Parameters' `\n" +
					"    -Name AutoShareWks -Value 1 -Type DWord -Force\n" +
					"Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\LanmanServer\\Parameters' `\n" +
					"    -Name AutoShareServer -Value 1 -Type DWord -Force\n" +
					"Write-Host 'Шары включены, перезапускаю службу «Сервер»…'\n" +
					"Restart-Service LanmanServer -Force\n"
			});
		}

		if (need.Contains("smb445"))
		{
			blocks.Add(new Block
			{
				Code = "firewall-smb",
				Title = "Открыть порт SMB (445) в firewall",
				Script =
					"# Файловый общий доступ: без него не работают ни шары, ни установка\n" +
					"Enable-NetFirewallRule -DisplayGroup 'Общий доступ к файлам и принтерам' -ErrorAction SilentlyContinue\n" +
					"Enable-NetFirewallRule -DisplayGroup 'File and Printer Sharing' -ErrorAction SilentlyContinue\n" +
					"New-NetFirewallRule -DisplayName 'DPT.Backup SMB' -Direction Inbound -Protocol TCP `\n" +
					"    -LocalPort 445 -Action Allow -ErrorAction SilentlyContinue | Out-Null\n" +
					"Write-Host 'Порт 445 открыт'\n"
			});
		}

		if (need.Contains("tokenfilter"))
		{
			blocks.Add(new Block
			{
				Code = "tokenfilter",
				Title = "Разрешить удалённый доступ локальным учётным записям",
				Script =
					"# Без этого локальная учётка не получит права администратора по сети\n" +
					"# (доменным учётным записям не нужно)\n" +
					"New-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System' `\n" +
					"    -Name LocalAccountTokenFilterPolicy -Value 1 -PropertyType DWord -Force | Out-Null\n" +
					"Write-Host 'LocalAccountTokenFilterPolicy = 1'\n"
			});
		}

		if (need.Contains("lanman"))
		{
			blocks.Add(new Block
			{
				Code = "lanman",
				Title = "Запустить службу «Сервер»",
				Script =
					"# Служба «Сервер» обслуживает общий доступ к файлам\n" +
					"Set-Service -Name LanmanServer -StartupType Automatic\n" +
					"Start-Service -Name LanmanServer\n" +
					"Write-Host 'Служба «Сервер» запущена'\n"
			});
		}

		if (need.Contains("winrmsvc") || need.Contains("winrmport") || forWinRm)
		{
			blocks.Add(new Block
			{
				Code = "winrm",
				Title = "Включить WinRM",
				Script =
					"# WinRM: удалённое выполнение команд PowerShell\n" +
					"Set-Service -Name WinRM -StartupType Automatic\n" +
					"Start-Service -Name WinRM\n" +
					"# Слушатель и правила firewall (идемпотентно)\n" +
					"if (-not (Get-ChildItem WSMan:\\localhost\\Listener -ErrorAction SilentlyContinue)) {\n" +
					"    Enable-PSRemoting -SkipNetworkProfileCheck -Force\n" +
					"}\n" +
					"Write-Host 'WinRM включён, порт 5985 слушается'\n"
			});
		}

		if (need.Contains("trustedhosts") && forWinRm)
		{
			blocks.Add(new Block
			{
				Code = "trustedhosts",
				Title = "Разрешить подключение из рабочей группы",
				Script =
					"# В рабочей группе WinRM не доверяет другим машинам без явного списка.\n" +
					"# В домене этот блок не нужен — пропустите его.\n" +
					"$current = (Get-Item WSMan:\\localhost\\Client\\TrustedHosts -ErrorAction SilentlyContinue).Value\n" +
					"$add = '192.168.0.0/16,10.0.0.0/8'\n" +
					"if ($current) { $new = $current + ',' + $add } else { $new = $add }\n" +
					"Set-Item WSMan:\\localhost\\Client\\TrustedHosts -Value $new -Force\n" +
					"Write-Host 'TrustedHosts: ' + $new\n"
			});
		}

		if (need.Contains("rdp3389"))
		{
			blocks.Add(new Block
			{
				Code = "rdp",
				Title = "Включить удалённый рабочий стол",
				Script =
					"# RDP нужен, если хотите зайти на машину и запустить установку вручную\n" +
					"Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Terminal Server' `\n" +
					"    -Name fDenyTSConnections -Value 0 -Force\n" +
					"Enable-NetFirewallRule -DisplayGroup 'Удаленный рабочий стол' -ErrorAction SilentlyContinue\n" +
					"Enable-NetFirewallRule -DisplayGroup 'Remote Desktop' -ErrorAction SilentlyContinue\n" +
					"Write-Host 'RDP включён'\n"
			});
		}

		if (blocks.Count == 0)
		{
			return "";
		}
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("# ============================================================");
		sb.AppendLine("#  DPT.Backup — подготовка машины к удалённой установке");
		sb.AppendLine("# ============================================================");
		sb.AppendLine("#");
		sb.AppendLine("#  Запустить НА ЦЕЛЕВОЙ машине от имени администратора:");
		sb.AppendLine("#    правой кнопкой по PowerShell → «Запуск от имени администратора»,");
		sb.AppendLine("#    затем выполнить этот текст целиком.");
		sb.AppendLine("#");
		sb.AppendLine("#  Через RDP: подключиться, скопировать текст, вставить и выполнить.");
		sb.AppendLine("#  Через GPO: положить как startup-скрипт (см. docs/deploy.md).");
		sb.AppendLine("#");
		sb.AppendLine("#  Что будет сделано:");
		foreach (Block b in blocks)
		{
			sb.AppendLine("#    - " + b.Title);
		}
		sb.AppendLine("#");
		sb.AppendLine("#  Скрипт идемпотентен: повторный запуск ничего не сломает.");
		sb.AppendLine("# ============================================================");
		sb.AppendLine();
		sb.AppendLine("$ErrorActionPreference = 'Stop'");
		sb.AppendLine();
		sb.AppendLine("if (-not ([Security.Principal.WindowsPrincipal]" +
			"[Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(" +
			"[Security.Principal.WindowsBuiltInRole]::Administrator)) {");
		sb.AppendLine("    Write-Host 'Нужны права администратора. Запустите PowerShell от имени администратора.' -ForegroundColor Red");
		sb.AppendLine("    exit 1");
		sb.AppendLine("}");
		sb.AppendLine();

		int n = 1;
		foreach (Block b in blocks)
		{
			sb.AppendLine("# ----------------------------------------------------------");
			sb.AppendLine("# " + n + ". " + b.Title);
			sb.AppendLine("# ----------------------------------------------------------");
			sb.AppendLine(b.Script);
			n++;
		}

		sb.AppendLine("# ----------------------------------------------------------");
		sb.AppendLine("Write-Host ''");
		sb.AppendLine("Write-Host 'Готово. Вернитесь в установщик и нажмите «Повторить проверку».' -ForegroundColor Green");

		return sb.ToString();
	}

	/// <summary>Короткий .cmd-вариант для случаев, когда PowerShell недоступен.</summary>
	public static string BuildCmd(IEnumerable<string> brokenCodes)
	{
		HashSet<string> need = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (brokenCodes != null)
		{
			foreach (string code in brokenCodes)
			{
				if (!string.IsNullOrWhiteSpace(code))
				{
					need.Add(code.Trim());
				}
			}
		}

		// Машина не отвечает — скрипт некуда отнести.
		if (need.Contains("dns") || need.Contains("host"))
		{
			return "";
		}

		StringBuilder sb = new StringBuilder();
		sb.AppendLine("@echo off");
		sb.AppendLine("rem DPT.Backup — подготовка машины к удалённой установке");
		sb.AppendLine("rem Запустить на целевой машине от имени администратора.");
		sb.AppendLine("chcp 65001 >nul");
		sb.AppendLine("net session >nul 2>&1");
		sb.AppendLine("if errorlevel 1 (");
		sb.AppendLine("  echo Нужны права администратора: запустите от имени администратора.");
		sb.AppendLine("  pause");
		sb.AppendLine("  exit /b 1");
		sb.AppendLine(")");
		sb.AppendLine();

		if (need.Contains("autoshare") || need.Contains("adminshare") || need.Contains("lanman"))
		{
			sb.AppendLine("rem Административные шары");
			sb.AppendLine("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\LanmanServer\\Parameters\" /v AutoShareWks /t REG_DWORD /d 1 /f");
			sb.AppendLine("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\LanmanServer\\Parameters\" /v AutoShareServer /t REG_DWORD /d 1 /f");
			sb.AppendLine("sc stop LanmanServer >nul 2>&1");
			sb.AppendLine("sc start LanmanServer >nul 2>&1");
			sb.AppendLine();
		}

		if (need.Contains("smb445"))
		{
			sb.AppendLine("rem Порт SMB 445 в firewall");
			sb.AppendLine("netsh advfirewall firewall add rule name=\"DPT.Backup SMB\" dir=in action=allow protocol=TCP localport=445 >nul");
			sb.AppendLine();
		}

		if (need.Contains("tokenfilter"))
		{
			sb.AppendLine("rem Локальные учётные записи: права администратора по сети");
			sb.AppendLine("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v LocalAccountTokenFilterPolicy /t REG_DWORD /d 1 /f");
			sb.AppendLine();
		}

		if (need.Contains("winrmsvc") || need.Contains("winrmport"))
		{
			sb.AppendLine("rem WinRM");
			sb.AppendLine("sc config WinRM start= auto >nul");
			sb.AppendLine("sc start WinRM >nul");
			sb.AppendLine("netsh advfirewall firewall add rule name=\"WinRM HTTP\" dir=in action=allow protocol=TCP localport=5985 >nul");
			sb.AppendLine();
		}

		sb.AppendLine("echo Готово. Вернитесь в установщик и нажмите «Повторить проверку».");
		sb.AppendLine("pause");
		return sb.ToString();
	}
}
