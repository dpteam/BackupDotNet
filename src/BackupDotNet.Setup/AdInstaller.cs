using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BackupDotNet.Core;
using BackupDotNet.Deploy;

namespace BackupDotNet.Setup;

/// <summary>
/// Установка на компьютеры домена: поиск в каталоге и установка на выбранные.
///
/// Поиск переиспользует <see cref="Scanner.DiscoverAd"/> из модуля развёртывания:
/// там уже учтены отключённые учётные записи компьютеров и загрузка только
/// нужных свойств.
///
/// Установка идёт **по одному компьютеру**, а не параллельно: каждый — это
/// копирование десятков мегабайт и создание службы. Параллельность здесь
/// упрётся в диск и сеть, зато сильно усложнит отчёт «где упало».
/// </summary>
public static class AdInstaller
{
	/// <summary>Компьютер из каталога.</summary>
	public class Computer
	{
		public string Name = "";

		public string DnsName = "";

		public string Os = "";

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["name"] = Name;
			d["dnsName"] = DnsName;
			d["os"] = Os;
			return d;
		}
	}

	/// <summary>Результат поиска.</summary>
	public class SearchResult
	{
		public bool Ok;

		public string Message = "";

		public List<Computer> Computers = new List<Computer>();

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["ok"] = Ok;
			d["message"] = Message;
			List<object> items = new List<object>();
			foreach (Computer c in Computers)
			{
				items.Add(c.ToJson());
			}
			d["computers"] = items;
			d["count"] = items.Count;
			if (!Ok)
			{
				d["error"] = Message;
			}
			return d;
		}
	}

	/// <summary>Итог по одному компьютеру.</summary>
	public class HostResult
	{
		public string Host = "";

		public bool Ok;

		public string Message = "";

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["host"] = Host;
			d["ok"] = Ok;
			d["message"] = Message;
			if (!Ok)
			{
				d["error"] = Message;
			}
			return d;
		}
	}

	/// <summary>
	/// Ищет компьютеры в каталоге. Пустой <paramref name="domain"/> — текущий домен.
	/// </summary>
	public static SearchResult Search(string domain, string user, string password, string filter, int maxHosts)
	{
		SearchResult r = new SearchResult();
		if (maxHosts <= 0)
		{
			maxHosts = 500;
		}

		try
		{
			List<Target> found = Scanner.DiscoverAd(domain, "", user, password, maxHosts, out string error);

			// Пустой список с ошибкой — это ошибка; пустой без ошибки — просто нет машин.
			if (found.Count == 0 && error.Length > 0)
			{
				r.Message = ExplainAdError(error);
				return r;
			}

			string q = (filter ?? "").Trim().ToLowerInvariant();
			foreach (Target t in found)
			{
				Computer c = new Computer
				{
					Name = t.Host,
					DnsName = string.IsNullOrEmpty(t.Ip) ? "" : t.Ip,
					Os = t.Os ?? ""
				};

				if (q.Length > 0 &&
					c.Name.ToLowerInvariant().IndexOf(q, StringComparison.Ordinal) < 0 &&
					(c.Os ?? "").ToLowerInvariant().IndexOf(q, StringComparison.Ordinal) < 0)
				{
					continue;
				}
				r.Computers.Add(c);
			}

			r.Computers.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
			r.Ok = true;
			r.Message = "найдено компьютеров: " + r.Computers.Count;
			if (error.Length > 0)
			{
				// Обрезали по лимиту — это не ошибка, но сказать надо.
				r.Message += " (каталог вернул больше " + maxHosts + ", список обрезан)";
			}
			return r;
		}
		catch (Exception ex)
		{
			r.Message = ExplainAdError(ex.Message);
			Log.Warn("Поиск в AD: " + ex.Message);
			return r;
		}
	}

	/// <summary>
	/// Устанавливает на список компьютеров по очереди.
	/// Возвращает итог по каждому; общий успех — если не упал ни один.
	/// </summary>
	public static List<HostResult> InstallBatch(
		List<string> hosts,
		string mode,
		string user,
		string password,
		int winrmPort,
		string installDir,
		string dataDir,
		string serviceName,
		int webPort,
		SetupEngine.ProgressHandler progress)
	{
		List<HostResult> results = new List<HostResult>();
		progress = progress ?? delegate { };

		if (hosts == null || hosts.Count == 0)
		{
			return results;
		}

		int total = hosts.Count;
		for (int i = 0; i < total; i++)
		{
			string host = (hosts[i] ?? "").Trim();
			if (host.Length == 0)
			{
				continue;
			}

			// Прогресс общий: доля каждого компьютера в общем списке.
			int basePercent = (int)(100.0 * i / total);
			int nextPercent = (int)(100.0 * (i + 1) / total);
			int span = Math.Max(1, nextPercent - basePercent);

			progress("[" + (i + 1) + " из " + total + "] " + host + ": начало", basePercent);

			HostResult hr = new HostResult { Host = host };
			try
			{
				SetupEngine.ProgressHandler inner = delegate (string step, int percent)
				{
					int p = basePercent + (int)(span * (percent / 100.0));
					progress("[" + (i + 1) + " из " + total + "] " + host + ": " + step, p);
				};

				if (string.Equals(mode, "winrm", StringComparison.OrdinalIgnoreCase))
				{
					WinRmInstaller.Options opt = new WinRmInstaller.Options
					{
						Host = host,
						User = user,
						Password = password,
						Port = winrmPort,
						InstallDir = installDir,
						DataDir = dataDir,
						ServiceName = serviceName,
						WebPort = webPort
					};
					SmbInstaller.Outcome outcome = WinRmInstaller.Install(opt, inner);
					hr.Ok = outcome.Ok;
					hr.Message = outcome.Message;
				}
				else
				{
					SmbInstaller.Options opt = new SmbInstaller.Options
					{
						Host = host,
						User = user,
						Password = password,
						InstallDir = installDir,
						DataDir = dataDir,
						ServiceName = serviceName,
						Port = webPort
					};
					SmbInstaller.Outcome outcome = SmbInstaller.Install(opt, inner);
					hr.Ok = outcome.Ok;
					hr.Message = outcome.Message;
				}
			}
			catch (Exception ex)
			{
				hr.Ok = false;
				hr.Message = ex.Message;
			}

			results.Add(hr);
			Log.Info("AD-установка " + host + ": " + (hr.Ok ? "ok" : "ошибка — " + hr.Message));
		}

		progress("Готово", 100);
		return results;
	}

	/// <summary>
	/// Готовит пакет для групповой политики: startup-скрипт и пояснение.
	/// Для машин, куда вообще нельзя достучаться по сети.
	/// </summary>
	/// <param name="sharePath">
	/// Путь к папке, откуда машины будут брать файлы, например
	/// <c>\\fileserver\Deploy\<Продукт></c>.
	/// </param>
	public static string BuildGpoScript(string sharePath, string serviceName, int port)
	{
		string share = (sharePath ?? "").Trim().TrimEnd('\\');
		if (share.Length == 0)
		{
			share = @"\\fileserver\Deploy\" + SetupBrand.ProductName;
		}

		StringBuilder sb = new StringBuilder();
		sb.AppendLine("@echo off");
		sb.AppendLine("rem ============================================================");
		sb.AppendLine("rem  " + SetupBrand.ProductName + " — установка через групповую политику");
		sb.AppendLine("rem ============================================================");
		sb.AppendLine("rem");
		sb.AppendLine("rem  Как применить:");
		sb.AppendLine("rem   1. Положить этот файл и каталог payload в папку:");
		sb.AppendLine("rem        " + share);
		sb.AppendLine("rem   2. В политике домена: Computer Configuration → Policies →");
		sb.AppendLine("rem      Windows Settings → Scripts → Startup → Add.");
		sb.AppendLine("rem   3. Указать этот файл. Машины применят его при перезагрузке.");
		sb.AppendLine("rem");
		sb.AppendLine("rem  Скрипт идемпотентен: если агент уже стоит, повторной установки");
		sb.AppendLine("rem  не будет — проверит наличие службы.");
		sb.AppendLine("rem ============================================================");
		sb.AppendLine();
		sb.AppendLine("chcp 65001 >nul");
		sb.AppendLine("set SHARE=" + share);
		sb.AppendLine("set DEST=" + SetupBrand.InstallDir);
		sb.AppendLine("set DATA=" + SetupBrand.DataDir);
		sb.AppendLine("set SVC=" + serviceName);
		sb.AppendLine();
		sb.AppendLine("rem Уже установлен — выходим.");
		sb.AppendLine("sc query \"%SVC%\" >nul 2>&1");
		sb.AppendLine("if not errorlevel 1 (");
		sb.AppendLine("  echo " + SetupBrand.ProductName + " уже установлен, пропускаю.");
		sb.AppendLine("  exit /b 0");
		sb.AppendLine(")");
		sb.AppendLine();
		sb.AppendLine("rem Пакет должен быть доступен.");
		sb.AppendLine("if not exist \"%SHARE%\\payload\\BackupDotNet.exe\" (");
		sb.AppendLine("  echo Нет доступа к %SHARE%\\payload — проверьте права компьютера на шару.");
		sb.AppendLine("  exit /b 1");
		sb.AppendLine(")");
		sb.AppendLine();
		sb.AppendLine("echo Устанавливаю в %DEST%");
		sb.AppendLine("mkdir \"%DEST%\" 2>nul");
		sb.AppendLine("mkdir \"%DATA%\" 2>nul");
		sb.AppendLine("robocopy \"%SHARE%\\payload\" \"%DEST%\" /E /NFL /NDL /NJH /NJS /NP >nul");
		sb.AppendLine("if errorlevel 8 exit /b 1");
		sb.AppendLine();
		sb.AppendLine("rem Конфигурация из шаблонов (существующую не трогаем).");
		sb.AppendLine("for %%f in (\"%DEST%\\config\\*.example.json\") do (");
		sb.AppendLine("  if not exist \"%DEST%\\config\\%%~nf.json\" copy /y \"%%f\" \"%DEST%\\config\\%%~nf.json\" >nul");
		sb.AppendLine(")");
		sb.AppendLine();
		sb.AppendLine("echo Регистрирую службу");
		sb.AppendLine("sc create \"%SVC%\" binPath= \"\\\"%DEST%\\BackupDotNet.exe\\\" --service\" start= auto >nul");
		sb.AppendLine("sc description \"%SVC%\" \"" + SetupBrand.Current.Description + "\" >nul");
		sb.AppendLine("sc failure \"%SVC%\" reset= 86400 actions= restart/60000/restart/60000/restart/60000 >nul");
		sb.AppendLine();
		sb.AppendLine("echo Открываю порт " + port);
		sb.AppendLine("netsh advfirewall firewall add rule name=\"" + SetupBrand.FirewallRule + "\" dir=in action=allow protocol=TCP localport=" + port + " >nul");
		sb.AppendLine("netsh http add urlacl url=http://+:" + port + "/ user=\"NT AUTHORITY\\SYSTEM\" >nul 2>&1");
		sb.AppendLine();
		sb.AppendLine("echo Запускаю службу");
		sb.AppendLine("sc start \"%SVC%\" >nul");
		sb.AppendLine();
		sb.AppendLine("echo Готово: http://%COMPUTERNAME%:" + port + "/");
		sb.AppendLine("exit /b 0");
		return sb.ToString();
	}

	/// <summary>Переводит типичные ошибки LDAP в понятный текст.</summary>
	private static string ExplainAdError(string error)
	{
		string msg = error ?? "";

		// 0x80005000 — неверный путь каталога. На практике это значит, что
		// машина не в домене: LDAP-путь попросту некуда строить.
		if (msg.IndexOf("0x80005000", StringComparison.OrdinalIgnoreCase) >= 0 ||
			msg.IndexOf("invalid directory pathname", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "каталог недоступен: эта машина не в домене или домен указан неверно. " +
				"Укажите домен явно или используйте установку по SMB/WinRM";
		}
		if (msg.IndexOf("0x8007052E", StringComparison.OrdinalIgnoreCase) >= 0 ||
			msg.IndexOf("logon failure", StringComparison.OrdinalIgnoreCase) >= 0 ||
			msg.IndexOf("неверное имя пользователя", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "неверное имя пользователя или пароль для каталога";
		}
		if (msg.IndexOf("0x8007203A", StringComparison.OrdinalIgnoreCase) >= 0 ||
			msg.IndexOf("server is not operational", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "контроллер домена недоступен — проверьте сеть и DNS";
		}
		if (msg.IndexOf("0x80072020", StringComparison.OrdinalIgnoreCase) >= 0 ||
			msg.IndexOf("operations error", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "контроллер домена отклонил запрос — возможно, нужен LDAPS или другие права";
		}
		if (msg.IndexOf("The specified domain", StringComparison.OrdinalIgnoreCase) >= 0 ||
			msg.IndexOf("домен не найден", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "домен не найден: машина не в домене или имя указано неверно";
		}
		return msg.Length > 200 ? msg.Substring(0, 200) + "…" : msg;
	}
}
