using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using BackupDotNet.Core;
using Microsoft.Win32;

namespace BackupDotNet.Setup;

/// <summary>
/// Почему не удаётся подключиться к машине — по пунктам, а не «не работает».
///
/// Идея: вместо одной общей ошибки «нет доступа» проверяем каждый слой
/// отдельно и говорим, какой именно сломан. Порт закрыт, шары отключены,
/// учётка без прав, WinRM не запущен — у каждого своя причина и своё лечение.
///
/// Для этой машины часть проверок делается напрямую (реестр, службы), для
/// чужой — только сетевыми пробами: чужой реестр без доступа не прочитать.
/// </summary>
public static class RemoteDiagnostics
{
	/// <summary>Один пункт проверки.</summary>
	public class Check
	{
		/// <summary>Короткий код для скрипта исправления.</summary>
		public string Code = "";

		public string Title = "";

		/// <summary>ok | warn | fail | skip</summary>
		public string State = "skip";

		public string Detail = "";

		/// <summary>Что сделать, если не в порядке.</summary>
		public string FixHint = "";

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["code"] = Code;
			d["title"] = Title;
			d["state"] = State;
			d["detail"] = Detail;
			d["fixHint"] = FixHint;
			return d;
		}
	}

	/// <summary>Полный отчёт.</summary>
	public class Report
	{
		public string Host = "";

		public bool IsLocal;

		public List<Check> Checks = new List<Check>();

		/// <summary>Что чинить: коды проблемных пунктов.</summary>
		public List<string> Broken = new List<string>();

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["host"] = Host;
			d["isLocal"] = IsLocal;
			List<object> items = new List<object>();
			foreach (Check c in Checks)
			{
				items.Add(c.ToJson());
			}
			d["checks"] = items;
			d["broken"] = Broken;
			d["ok"] = Broken.Count == 0;
			return d;
		}
	}

	/// <summary>
	/// Проверяет машину. Режим — <c>smb</c> или <c>winrm</c>: от него зависит,
	/// какие слои важны.
	/// </summary>
	public static Report Inspect(string host, string user, string password, string mode, int winrmPort)
	{
		Report r = new Report();
		r.Host = (host ?? "").Trim();
		string h = r.Host.TrimStart('\\');

		if (h.Length == 0)
		{
			Check c = new Check { Code = "host", Title = "Компьютер", State = "fail", Detail = "не указан" };
			r.Checks.Add(c);
			r.Broken.Add(c.Code);
			return r;
		}

		r.IsLocal = IsLocalHost(h);

		// 1. Имя разрешается?
		Check dns = new Check { Code = "dns", Title = "Имя компьютера" };
		if (r.IsLocal)
		{
			dns.State = "ok";
			dns.Detail = "эта машина";
		}
		else
		{
			string resolved = TryResolve(h);
			if (resolved.Length > 0)
			{
				dns.State = "ok";
				dns.Detail = "разрешается в " + resolved;
			}
			else
			{
				dns.State = "fail";
				dns.Detail = "имя не разрешается в IP";
				dns.FixHint = "проверьте DNS или укажите IP-адрес вместо имени";
				r.Broken.Add(dns.Code);
			}
		}
		r.Checks.Add(dns);

		// 2. Порты.
		Check smbPort = PortCheck(h, 445, "smb445", "Порт SMB (445)");
		Check rdpPort = PortCheck(h, 3389, "rdp3389", "Порт RDP (3389)");
		Check winrmPortCheck = PortCheck(h, winrmPort > 0 ? winrmPort : 5985, "winrmport",
			"Порт WinRM (" + (winrmPort > 0 ? winrmPort : 5985) + ")");
		r.Checks.Add(smbPort);
		r.Checks.Add(rdpPort);
		r.Checks.Add(winrmPortCheck);

		// 3. Админ-шары: доступ и запись.
		Check adminShare = new Check { Code = "adminshare", Title = "Админ-шара ADMIN$" };
		string unc = @"\\" + h + @"\ADMIN$";
		bool shareOk = false;
		try
		{
			using (SmbSession session = SmbSession.Open(unc, user, password, out string err))
			{
				if (session != null)
				{
					shareOk = true;
					adminShare.State = "ok";
					adminShare.Detail = "доступна";
				}
				else
				{
					adminShare.State = "fail";
					adminShare.Detail = err;
					adminShare.FixHint = err.IndexOf("запрещён", StringComparison.OrdinalIgnoreCase) >= 0 ||
						err.IndexOf("учётными данными", StringComparison.OrdinalIgnoreCase) >= 0
						? "нужны права администратора на цели; в рабочей группе — " +
							"LocalAccountTokenFilterPolicy = 1"
						: "проверьте, что служба «Сервер» (LanmanServer) запущена и шары не отключены";
				}
			}
		}
		catch (Exception ex)
		{
			adminShare.State = "fail";
			adminShare.Detail = ex.Message;
		}
		if (adminShare.State != "ok")
		{
			r.Broken.Add(adminShare.Code);
		}
		r.Checks.Add(adminShare);

		// 4. WinRM: только если выбран этот способ или как запасной вариант.
		Check winrmSvc = new Check { Code = "winrmsvc", Title = "WinRM" };
		if (r.IsLocal)
		{
			// Свою машину читаем напрямую.
			string state = LocalServiceState("WinRM");
			winrmSvc.State = state == "Running" ? "ok" : "fail";
			winrmSvc.Detail = state.Length > 0 ? "служба: " + state : "служба не найдена";
			if (winrmSvc.State != "ok")
			{
				winrmSvc.FixHint = "запустить службу WinRM и включить её в автозапуск";
				r.Broken.Add(winrmSvc.Code);
			}
		}
		else if (winrmPortCheck.State == "ok")
		{
			winrmSvc.State = "ok";
			winrmSvc.Detail = "порт открыт";
		}
		else
		{
			winrmSvc.State = "fail";
			winrmSvc.Detail = "порт " + (winrmPort > 0 ? winrmPort : 5985) + " закрыт";
			winrmSvc.FixHint = "включить WinRM на цели (Enable-PSRemoting) или использовать способ SMB";
			if (mode == "winrm")
			{
				r.Broken.Add(winrmSvc.Code);
			}
		}
		r.Checks.Add(winrmSvc);

		// 5. Только для своей машины: настройки, которые ломают удалённый доступ.
		if (r.IsLocal)
		{
			Check tokenFilter = LocalTokenFilterPolicy();
			r.Checks.Add(tokenFilter);
			if (tokenFilter.State == "fail")
			{
				r.Broken.Add(tokenFilter.Code);
			}

			Check autoShare = LocalAutoShare();
			r.Checks.Add(autoShare);
			if (autoShare.State == "fail")
			{
				r.Broken.Add(autoShare.Code);
			}

			Check lanman = LocalLanmanServer();
			r.Checks.Add(lanman);
			if (lanman.State == "fail")
			{
				r.Broken.Add(lanman.Code);
			}

			Check trusted = LocalTrustedHosts();
			r.Checks.Add(trusted);
			if (trusted.State == "fail" && mode == "winrm")
			{
				r.Broken.Add(trusted.Code);
			}
		}

		return r;
	}

	// ------------------------------------------------------------------ проверки

	private static Check PortCheck(string host, int port, string code, string title)
	{
		Check c = new Check { Code = code, Title = title };
		if (port <= 0)
		{
			c.State = "skip";
			return c;
		}
		try
		{
			using TcpClient client = new TcpClient();
			IAsyncResult ar = client.BeginConnect(host, port, null, null);
			bool ok = ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3)) && client.Connected;
			if (ok)
			{
				c.State = "ok";
				c.Detail = "открыт";
				try
				{
					client.EndConnect(ar);
				}
				catch
				{
				}
			}
			else
			{
				c.State = "fail";
				c.Detail = "не отвечает за 3 с";
				c.FixHint = PortFixHint(port);
			}
		}
		catch (Exception ex)
		{
			c.State = "fail";
			c.Detail = Short(ex.Message);
			c.FixHint = PortFixHint(port);
		}
		return c;
	}

	private static string PortFixHint(int port)
	{
		switch (port)
		{
		case 445:
			return "проверьте firewall на цели и доступность службы «Сервер»";
		case 3389:
			return "включите удалённый рабочий стол на цели, если нужен режим консоли";
		case 5985:
		case 5986:
			return "включите WinRM: Enable-PSRemoting -Force";
		default:
			return "проверьте firewall на цели";
		}
	}

	private static Check LocalTokenFilterPolicy()
	{
		Check c = new Check
		{
			Code = "tokenfilter",
			Title = "Локальные учётные записи (LocalAccountTokenFilterPolicy)"
		};
		try
		{
			using RegistryKey key = Registry.LocalMachine.OpenSubKey(
				@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
			object v = key?.GetValue("LocalAccountTokenFilterPolicy");
			int value = v == null ? 0 : Convert.ToInt32(v);

			if (value == 1)
			{
				c.State = "ok";
				c.Detail = "1 — удалённый доступ под локальной учёткой разрешён";
			}
			else
			{
				// Для доменных учёток это не нужно, для локальных — обязательно.
				c.State = "warn";
				c.Detail = "0 — локальная учётка не получит права администратора по сети";
				c.FixHint = "поставить 1, если подключаетесь локальной учётной записью " +
					"(не доменной)";
			}
		}
		catch (Exception ex)
		{
			c.State = "skip";
			c.Detail = Short(ex.Message);
		}
		return c;
	}

	private static Check LocalAutoShare()
	{
		Check c = new Check { Code = "autoshare", Title = "Административные шары (AutoShareWks)" };
		try
		{
			using RegistryKey key = Registry.LocalMachine.OpenSubKey(
				@"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters");
			object wks = key?.GetValue("AutoShareWks");
			object srv = key?.GetValue("AutoShareServer");
			int value = wks != null ? Convert.ToInt32(wks) : (srv != null ? Convert.ToInt32(srv) : 1);

			if (value == 1)
			{
				c.State = "ok";
				c.Detail = "1 — шары ADMIN$ и C$ создаются";
			}
			else
			{
				c.State = "fail";
				c.Detail = "0 — административные шары отключены";
				c.FixHint = "включить AutoShareWks = 1 и перезапустить службу «Сервер»";
			}
		}
		catch (Exception ex)
		{
			c.State = "skip";
			c.Detail = Short(ex.Message);
		}
		return c;
	}

	private static Check LocalLanmanServer()
	{
		Check c = new Check { Code = "lanman", Title = "Служба «Сервер» (LanmanServer)" };
		string state = LocalServiceState("LanmanServer");
		if (state == "Running")
		{
			c.State = "ok";
			c.Detail = "работает";
		}
		else if (state.Length == 0)
		{
			c.State = "fail";
			c.Detail = "служба не найдена";
			c.FixHint = "служба нужна для доступа к админ-шарам";
		}
		else
		{
			c.State = "fail";
			c.Detail = "состояние: " + state;
			c.FixHint = "запустить службу «Сервер»";
		}
		return c;
	}

	private static Check LocalTrustedHosts()
	{
		Check c = new Check { Code = "trustedhosts", Title = "TrustedHosts (для рабочей группы)" };
		try
		{
			PsBridge.Result r = PsBridge.Run(
				"try { (Get-Item WSMan:\\localhost\\Client\\TrustedHosts -ErrorAction Stop).Value } " +
				"catch { '' }", 15000, null);
			string value = (r.Output ?? "").Trim();

			if (value.Length == 0)
			{
				c.State = "warn";
				c.Detail = "пусто";
				c.FixHint = "в рабочей группе WinRM требует TrustedHosts; в домене не нужно";
			}
			else
			{
				c.State = "ok";
				c.Detail = value.Length > 80 ? value.Substring(0, 80) + "…" : value;
			}
		}
		catch (Exception ex)
		{
			c.State = "skip";
			c.Detail = Short(ex.Message);
		}
		return c;
	}

	// ------------------------------------------------------------------ помощники

	private static bool IsLocalHost(string host)
	{
		if (string.IsNullOrWhiteSpace(host))
		{
			return false;
		}
		string h = host.Trim().TrimStart('\\');
		if (h.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
			h.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
			h.Equals(".", StringComparison.OrdinalIgnoreCase) ||
			h.Equals("::1", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return h.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase);
	}

	private static string TryResolve(string host)
	{
		try
		{
			System.Net.IPAddress[] addresses = System.Net.Dns.GetHostAddresses(host);
			foreach (System.Net.IPAddress a in addresses)
			{
				if (a.AddressFamily == AddressFamily.InterNetwork)
				{
					return a.ToString();
				}
			}
			return addresses.Length > 0 ? addresses[0].ToString() : "";
		}
		catch
		{
			return "";
		}
	}

	private static string LocalServiceState(string name)
	{
		try
		{
			using System.ServiceProcess.ServiceController sc =
				new System.ServiceProcess.ServiceController(name);
			return sc.Status.ToString();
		}
		catch (InvalidOperationException)
		{
			return "";
		}
		catch
		{
			return "";
		}
	}

	private static string Short(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		int nl = text.IndexOfAny(new[] { '\r', '\n' });
		string line = nl > 0 ? text.Substring(0, nl) : text;
		return line.Length > 140 ? line.Substring(0, 140) + "…" : line.Trim();
	}
}
