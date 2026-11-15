using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using BackupChecker.Core;

namespace BackupChecker.Deploy;

public class DeployManager
{
	private class WorkItem
	{
		public DeployJob Job;

		public DeployOptions Options;
	}

	public class CredentialInfo
	{
		public string User = "";

		public bool HasPassword;
	}

	private const string SecretKey = "deploy.cred";

	private readonly Settings _settings;

	private readonly ConfigLoader _config;

	private readonly AgentRegistry _agents;

	private readonly object _sync = new object();

	private readonly List<DeployJob> _jobs = new List<DeployJob>();

	private readonly Queue<WorkItem> _queue = new Queue<WorkItem>();

	private Thread _worker;

	private volatile bool _stopping;

	public void Stop()
	{
		_stopping = true;
	}

	public DeployManager(Settings settings, ConfigLoader config, AgentRegistry agents)
	{
		_settings = settings;
		_config = config;
		_agents = agents;
		_worker = new Thread(Loop);
		_worker.IsBackground = true;
		_worker.Name = "deploy";
		_worker.Start();
	}

	public CredentialInfo GetCredential()
	{
		CredentialInfo ci = new CredentialInfo();
		string raw = _config.Secret("deploy.cred");
		if (raw.Length == 0)
		{
			return ci;
		}
		try
		{
			Dictionary<string, object> d = Json.ParseObject(raw);
			ci.User = Json.GetStr(d, "user", "");
			ci.HasPassword = Json.GetStr(d, "password", "").Length > 0;
		}
		catch
		{
		}
		return ci;
	}

	public void SaveCredential(string user, string password)
	{
		Dictionary<string, object> d = Json.Obj();
		d["user"] = (user ?? "").Trim();
		d["password"] = password ?? "";
		_config.SaveSecret("deploy.cred", Json.Write(d), protect: true);
	}

	public void SaveCentralUrl(string url)
	{
		if (string.IsNullOrEmpty(url))
		{
			return;
		}
		try
		{
			Dictionary<string, object> d = Json.Obj();
			d["centralUrl"] = url.Trim().TrimEnd('/');
			string path = Path.Combine(_settings.ConfigDir, "deploy.json");
			ConfigLoader.AtomicWrite(path, Json.WritePretty(d));
		}
		catch (Exception ex)
		{
			Log.Warn("deploy.json: " + ex.Message);
		}
	}

	public string GetCentralUrl()
	{
		try
		{
			string path = Path.Combine(_settings.ConfigDir, "deploy.json");
			if (!File.Exists(path))
			{
				return "";
			}
			Dictionary<string, object> d = Json.ParseObject(File.ReadAllText(path, Encoding.UTF8));
			return Json.GetStr(d, "centralUrl", "");
		}
		catch
		{
			return "";
		}
	}

	private bool LoadCredential(out string user, out string password)
	{
		user = "";
		password = "";
		string raw = _config.Secret("deploy.cred");
		if (raw.Length == 0)
		{
			return false;
		}
		try
		{
			Dictionary<string, object> d = Json.ParseObject(raw);
			user = Json.GetStr(d, "user", "");
			password = Json.GetStr(d, "password", "");
		}
		catch
		{
			return false;
		}
		return user.Length > 0;
	}

	public DeployJob Enqueue(string kind, string host, DeployOptions options)
	{
		DeployJob job = new DeployJob();
		job.Id = "dpl-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 4);
		job.Kind = kind;
		job.Host = host;
		if (options == null)
		{
			options = new DeployOptions();
		}
		lock (_sync)
		{
			_jobs.Insert(0, job);
			while (_jobs.Count > 200)
			{
				_jobs.RemoveAt(_jobs.Count - 1);
			}
			WorkItem w = new WorkItem();
			w.Job = job;
			w.Options = options;
			_queue.Enqueue(w);
		}
		job.Add("в очереди: " + kind + " на " + host);
		return job;
	}

	public List<DeployJob> Jobs(int limit)
	{
		lock (_sync)
		{
			List<DeployJob> result = new List<DeployJob>();
			foreach (DeployJob j in _jobs)
			{
				result.Add(j);
				if (result.Count >= limit)
				{
					break;
				}
			}
			return result;
		}
	}

	private void Loop()
	{
		while (!_stopping)
		{
			WorkItem item = null;
			lock (_sync)
			{
				if (_queue.Count > 0)
				{
					item = _queue.Dequeue();
				}
			}
			if (item == null)
			{
				Thread.Sleep(400);
				continue;
			}
			try
			{
				DeployJob job = item.Job;
				if (item.Options.Mode == "uninstall")
				{
					RunUninstall(job, item.Options);
				}
				else
				{
					RunInstall(job, item.Options);
				}
			}
			catch (Exception ex)
			{
				item.Job.Fail("непредвиденная ошибка: " + WinRmSession.Describe(ex));
				Log.Error("deploy " + item.Job.Host, ex);
			}
		}
	}

	public Dictionary<string, object> TestHost(string host, int winRmPort, bool https)
	{
		Dictionary<string, object> d = Json.Obj();
		d["host"] = host;
		if (!LoadCredential(out var user, out var pass))
		{
			d["ok"] = false;
			d["error"] = "не заданы учётные данные для WinRM";
			return d;
		}
		WinRmSession s = WinRmSession.Open(host, winRmPort, https, user, pass, 20000, out var error);
		if (s == null)
		{
			d["ok"] = false;
			d["error"] = "WinRM: " + error;
			return d;
		}
		try
		{
			PsResult r = s.Run(PreCheckScript(), 60000);
			if (!r.Ok)
			{
				d["ok"] = false;
				d["error"] = r.Error;
				return d;
			}
			Dictionary<string, string> kv = ParsePairs(r.Output);
			d["ok"] = true;
			d["name"] = Get(kv, "NAME");
			d["os"] = Get(kv, "OS");
			d["version"] = Get(kv, "VER");
			d["admin"] = Get(kv, "ADMIN") == "True";
			d["freeGb"] = Get(kv, "FREE_GB");
			d["ps"] = Get(kv, "PS");
			return d;
		}
		finally
		{
			s.Dispose();
		}
	}

	private void RunInstall(DeployJob job, DeployOptions o)
	{
		job.SetPhase("подключение", 3);
		if (string.IsNullOrEmpty(o.InstallDir))
		{
			o.InstallDir = Branding.Current.InstallDir;
		}
		if (string.IsNullOrEmpty(o.ServiceName))
		{
			o.ServiceName = ((o.Mode == "central") ? Branding.Current.ServiceName : Branding.Current.ServiceNameRemote);
		}
		if (!LoadCredential(out var user, out var pass))
		{
			job.Fail("не заданы учётные данные для WinRM (вкладка «Доступ» на странице развёртывания)");
			return;
		}
		WinRmSession s = WinRmSession.Open(job.Host, o.WinRmPort, o.Https, user, pass, 25000, out var error);
		if (s == null)
		{
			job.Fail("WinRM: " + error);
			return;
		}
		try
		{
			job.Add("WinRM подключён: " + job.Host);
			job.SetPhase("проверка машины", 8);
			PsResult pre = s.Run(PreCheckScript(), 60000);
			if (!pre.Ok)
			{
				job.Fail("не удалось прочитать сведения о машине: " + pre.Error);
				return;
			}
			Dictionary<string, string> kv = ParsePairs(pre.Output);
			string hostName = Get(kv, "NAME");
			if (hostName.Length == 0)
			{
				hostName = job.Host;
			}
			job.Add("машина: " + hostName + " · " + Get(kv, "OS") + " · " + Get(kv, "VER"));
			job.Add("PowerShell " + Get(kv, "PS") + ", свободно на C: " + Get(kv, "FREE_GB") + " ГБ");
			if (Get(kv, "ADMIN") != "True")
			{
				job.Fail("учётная запись не администратор на этой машине");
				return;
			}
			double freeGb = 0.0;
			double.TryParse(Get(kv, "FREE_GB"), NumberStyles.Any, CultureInfo.InvariantCulture, out freeGb);
			if (freeGb > 0.0 && freeGb < 0.5)
			{
				job.Fail("на C: меньше 500 МБ свободно (" + freeGb + " ГБ)");
				return;
			}
			string agentId = Slug(hostName);
			string centralUrl = (o.CentralUrl ?? "").Trim().TrimEnd('/');
			if (o.Mode != "central" && centralUrl.Length == 0)
			{
				job.Fail("не задан адрес центральной машины (centralUrl)");
				return;
			}
			if (centralUrl.Length > 0)
			{
				SaveCentralUrl(centralUrl);
			}
			AgentRegistry.RemoteAgent agent = _agents.Create(agentId, "Агент — " + hostName, AgentRegistry.NewToken());
			if (agent == null)
			{
				job.Fail("не удалось создать запись агента в реестре");
				return;
			}
			job.Add("агент в реестре: " + agent.Id);
			job.SetPhase("сборка пакета", 15);
			string zip = Package.Build(_config.RootDir, _settings, agent.Id, centralUrl, agent.Token, hostName, out var size, out error);
			if (zip.Length == 0)
			{
				job.Fail("не удалось собрать пакет: " + error);
				return;
			}
			job.Add("пакет: " + size / 1024 + " КБ");
			string tag = Guid.NewGuid().ToString("N").Substring(0, 8);
			string remoteB64 = "C:\\Windows\\Temp\\bc-" + tag + ".b64";
			job.SetPhase("передача пакета", 20);
			byte[] data = File.ReadAllBytes(zip);
			if (!Upload(s, data, remoteB64, job, 20, 70))
			{
				TryDeleteLocal(zip);
				return;
			}
			TryDeleteLocal(zip);
			job.SetPhase("установка службы", 75);
			string script = InstallScript(o.InstallDir, o.ServiceName, remoteB64, "C:\\Windows\\Temp\\bc-" + tag + ".zip", Branding.Current.FullName + " — " + ((o.Mode == "central") ? "сервер" : "удалённый агент"));
			PsResult inst = s.Run(script, 240000);
			string[] array = (inst.Output ?? "").Split('\n');
			foreach (string line in array)
			{
				string t = line.Trim();
				if (t.Length > 0)
				{
					job.Add(t);
				}
			}
			if (!inst.Ok)
			{
				job.Fail("установка не удалась: " + Clip(inst.Error, 400));
				return;
			}
			job.SetPhase("проверка службы", 92);
			PsResult check = s.Run("(Get-Service -Name '" + o.ServiceName + "' -ErrorAction SilentlyContinue).Status", 30000);
			string status = (check.Output ?? "").Trim();
			if (status != "Running")
			{
				job.Fail("служба не запущена (состояние: " + ((status.Length > 0) ? status : "нет") + ")");
				return;
			}
			job.Add("служба " + o.ServiceName + ": Running");
			job.SetPhase("ожидание первого отчёта", 96);
			if (WaitForHeartbeat(agent.Id, 45000))
			{
				job.Success("агент установлен и на связи");
			}
			else
			{
				job.Success("агент установлен; первый отчёт придёт в течение 5 минут");
			}
		}
		finally
		{
			s.Dispose();
		}
	}

	private void RunUninstall(DeployJob job, DeployOptions o)
	{
		job.SetPhase("подключение", 5);
		if (string.IsNullOrEmpty(o.InstallDir))
		{
			o.InstallDir = Branding.Current.InstallDir;
		}
		if (string.IsNullOrEmpty(o.ServiceName))
		{
			o.ServiceName = Branding.Current.ServiceNameRemote;
		}
		if (!LoadCredential(out var user, out var pass))
		{
			job.Fail("не заданы учётные данные для WinRM");
			return;
		}
		WinRmSession s = WinRmSession.Open(job.Host, o.WinRmPort, o.Https, user, pass, 25000, out var error);
		if (s == null)
		{
			job.Fail("WinRM: " + error);
			return;
		}
		try
		{
			job.Add("WinRM подключён: " + job.Host);
			job.SetPhase("удаление службы", 40);
			PsResult r = s.Run(UninstallScript(o.InstallDir, o.ServiceName, o.KeepFiles), 180000);
			string[] array = (r.Output ?? "").Split('\n');
			foreach (string line in array)
			{
				string t = line.Trim();
				if (t.Length > 0)
				{
					job.Add(t);
				}
			}
			if (!r.Ok)
			{
				job.Fail("удаление не удалось: " + Clip(r.Error, 400));
				return;
			}
			if (o.RemoveAgent)
			{
				PsResult pre = s.Run("$env:COMPUTERNAME", 20000);
				string name = (pre.Output ?? "").Trim();
				string agentId = Slug((name.Length > 0) ? name : job.Host);
				job.SetPhase("реестр агентов", 85);
				if (_agents.Remove(agentId))
				{
					job.Add("запись агента " + agentId + " удалена");
				}
				else
				{
					job.Add("записи агента " + agentId + " в реестре не было");
				}
			}
			job.Success("агент удалён");
		}
		finally
		{
			s.Dispose();
		}
	}

	private bool Upload(WinRmSession s, byte[] data, string remotePath, DeployJob job, int fromPercent, int toPercent)
	{
		string b64 = Convert.ToBase64String(data);
		PsResult r = s.Run("[IO.File]::WriteAllText('" + remotePath + "', '')", 30000);
		if (!r.Ok)
		{
			job.Fail("не удалось создать файл на машине: " + r.Error);
			return false;
		}
		for (int i = 0; i < b64.Length; i += 100000)
		{
			int len = Math.Min(100000, b64.Length - i);
			string part = b64.Substring(i, len);
			r = s.Run("[IO.File]::AppendAllText('" + remotePath + "', '" + part + "')", 60000);
			if (!r.Ok)
			{
				job.Fail("ошибка передачи пакета: " + Clip(r.Error, 300));
				return false;
			}
			int done = i + len;
			job.SetPhase("передача пакета", fromPercent + (toPercent - fromPercent) * done / b64.Length);
			job.Message = "передано " + done / 1024 + " из " + b64.Length / 1024 + " КБ";
		}
		return true;
	}

	private static void TryDeleteLocal(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch
		{
		}
	}

	private bool WaitForHeartbeat(string agentId, int timeoutMs)
	{
		for (int waited = 0; waited < timeoutMs; waited += 3000)
		{
			AgentRegistry.RemoteAgent a = _agents.ById(agentId);
			if (a != null && a.LastSeen.HasValue && (DateTime.Now - a.LastSeen.Value).TotalSeconds < 120.0)
			{
				return true;
			}
			Thread.Sleep(3000);
		}
		return false;
	}

	private static string PreCheckScript()
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("$ErrorActionPreference = 'Stop'");
		sb.AppendLine("$os = Get-WmiObject Win32_OperatingSystem");
		sb.AppendLine("$admin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)");
		sb.AppendLine("$free = 0");
		sb.AppendLine("try { $free = [math]::Round((Get-PSDrive C).Free / 1GB, 2) } catch { }");
		sb.AppendLine("'NAME=' + $env:COMPUTERNAME");
		sb.AppendLine("'OS=' + $os.Caption");
		sb.AppendLine("'VER=' + $os.Version");
		sb.AppendLine("'ADMIN=' + $admin");
		sb.AppendLine("'FREE_GB=' + $free");
		sb.AppendLine("'PS=' + $PSVersionTable.PSVersion.ToString()");
		return sb.ToString();
	}

	private static string InstallScript(string dir, string service, string b64Path, string zipPath, string displayName)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("$ErrorActionPreference = 'Stop'");
		sb.AppendLine("$dir = '" + dir + "'");
		sb.AppendLine("$svc = '" + service + "'");
		sb.AppendLine("$disp = '" + displayName.Replace("'", "''") + "'");
		sb.AppendLine("$b64 = '" + b64Path + "'");
		sb.AppendLine("$zip = '" + zipPath + "'");
		sb.AppendLine("# 1. собрать архив");
		sb.AppendLine("$text = [IO.File]::ReadAllText($b64)");
		sb.AppendLine("[IO.File]::WriteAllBytes($zip, [Convert]::FromBase64String($text))");
		sb.AppendLine("Remove-Item $b64 -Force -ErrorAction SilentlyContinue");
		sb.AppendLine("'архив собран: ' + [math]::Round((Get-Item $zip).Length / 1KB) + ' КБ'");
		sb.AppendLine("# 2. остановить прежнюю службу");
		sb.AppendLine("$old = Get-Service -Name $svc -ErrorAction SilentlyContinue");
		sb.AppendLine("if ($old -and $old.Status -ne 'Stopped') {");
		sb.AppendLine("  Stop-Service -Name $svc -Force");
		sb.AppendLine("  $old.WaitForStatus('Stopped', '00:00:30')");
		sb.AppendLine("  'прежняя служба остановлена'");
		sb.AppendLine("}");
		sb.AppendLine("# 3. распаковать с перезаписью");
		sb.AppendLine("if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }");
		sb.AppendLine("Add-Type -AssemblyName System.IO.Compression.FileSystem");
		sb.AppendLine("$a = [IO.Compression.ZipFile]::OpenRead($zip)");
		sb.AppendLine("$n = 0");
		sb.AppendLine("foreach ($e in $a.Entries) {");
		sb.AppendLine("  $t = Join-Path $dir ($e.FullName -replace '/', '\\')");
		sb.AppendLine("  if ($e.FullName.EndsWith('/')) { if (-not (Test-Path $t)) { New-Item -ItemType Directory -Path $t -Force | Out-Null }; continue }");
		sb.AppendLine("  $p = Split-Path -Parent $t");
		sb.AppendLine("  if ($p -and -not (Test-Path $p)) { New-Item -ItemType Directory -Path $p -Force | Out-Null }");
		sb.AppendLine("  [IO.Compression.ZipFileExtensions]::ExtractToFile($e, $t, $true)");
		sb.AppendLine("  $n++");
		sb.AppendLine("}");
		sb.AppendLine("$a.Dispose()");
		sb.AppendLine("Remove-Item $zip -Force");
		sb.AppendLine("'файлов распаковано: ' + $n");
		sb.AppendLine("# 4. служба");
		sb.AppendLine("$exe = Join-Path $dir 'BackupChecker.exe'");
		sb.AppendLine("if (-not (Test-Path $exe)) { throw 'не распаковался BackupChecker.exe' }");
		sb.AppendLine("$bin = [char]34 + $exe + [char]34 + ' --service-remote'");
		sb.AppendLine("$exists = Get-Service -Name $svc -ErrorAction SilentlyContinue");
		sb.AppendLine("if (-not $exists) {");
		sb.AppendLine("  # ВАЖНО: ServiceType и ErrorControl — это UInt8, и PowerShell иначе");
		sb.AppendLine("  # отправляет Int32: CIM отвечает «несоответствие типа для параметра».");
		sb.AppendLine("  # ServiceType 16 = Own Process, ErrorControl 1 = Normal.");
		sb.AppendLine("  $r = Invoke-CimMethod -ClassName Win32_Service -MethodName Create -Arguments @{");
		sb.AppendLine("    Name = $svc; DisplayName = $disp; PathName = $bin;");
		sb.AppendLine("    ServiceType = [byte]16; ErrorControl = [byte]1;");
		sb.AppendLine("    StartMode = 'Automatic'; DesktopInteract = $false }");
		sb.AppendLine("  if ($r.ReturnValue -ne 0) { throw ('создание службы, код ' + $r.ReturnValue) }");
		sb.AppendLine("  'служба создана'");
		sb.AppendLine("} else {");
		sb.AppendLine("  $filter = \"Name='\" + $svc + \"'\"");
		sb.AppendLine("  $inst = Get-CimInstance -ClassName Win32_Service -Filter $filter");
		sb.AppendLine("  # ВАЖНО: у Win32_Service.Change НЕТ параметра Name — имя задаётся");
		sb.AppendLine("  # через InputObject, иначе CIM отвечает «неопознанный параметр Name»");
		sb.AppendLine("  $r = Invoke-CimMethod -InputObject $inst -MethodName Change -Arguments @{");
		sb.AppendLine("    PathName = $bin; StartMode = 'Automatic' }");
		sb.AppendLine("  if ($r.ReturnValue -ne 0) { throw ('изменение службы, код ' + $r.ReturnValue) }");
		sb.AppendLine("  'служба обновлена'");
		sb.AppendLine("}");
		sb.AppendLine("# автоперезапуск при падении");
		sb.AppendLine("& sc.exe failure $svc reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null");
		sb.AppendLine("Start-Service -Name $svc");
		sb.AppendLine("Start-Sleep -Seconds 2");
		sb.AppendLine("$st = (Get-Service -Name $svc).Status");
		sb.AppendLine("if ($st -ne 'Running') { throw ('служба не запустилась: ' + $st) }");
		sb.AppendLine("'служба запущена'");
		return sb.ToString();
	}

	private static string UninstallScript(string dir, string service, bool keepFiles)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("$ErrorActionPreference = 'Stop'");
		sb.AppendLine("$dir = '" + dir + "'");
		sb.AppendLine("$svc = '" + service + "'");
		sb.AppendLine("$keep = " + (keepFiles ? "$true" : "$false"));
		sb.AppendLine("$s = Get-CimInstance -ClassName Win32_Service -Filter (\"Name='\" + $svc + \"'\") -ErrorAction SilentlyContinue");
		sb.AppendLine("if ($s) {");
		sb.AppendLine("  if ($s.State -ne 'Stopped') {");
		sb.AppendLine("    Stop-Service -Name $svc -Force");
		sb.AppendLine("    Start-Sleep -Seconds 3");
		sb.AppendLine("  }");
		sb.AppendLine("  # ВАЖНО: Delete — метод ЭКЗЕМПЛЯРА, а не класса: у него нет");
		sb.AppendLine("  # параметров, поэтому -ClassName + -Arguments даёт «неопознанный параметр».");
		sb.AppendLine("  $r = Invoke-CimMethod -InputObject $s -MethodName Delete");
		sb.AppendLine("  if ($r.ReturnValue -ne 0) { throw ('удаление службы, код ' + $r.ReturnValue) }");
		sb.AppendLine("  'служба удалена'");
		sb.AppendLine("} else { 'служба не найдена' }");
		sb.AppendLine("if (-not $keep -and (Test-Path $dir)) { Remove-Item $dir -Recurse -Force; 'каталог удалён' }");
		sb.AppendLine("elseif ($keep) { 'каталог сохранён: ' + $dir }");
		sb.AppendLine("'Готово'");
		return sb.ToString();
	}

	private static Dictionary<string, string> ParsePairs(string text)
	{
		Dictionary<string, string> d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrEmpty(text))
		{
			return d;
		}
		string[] array = text.Replace("\r\n", "\n").Split('\n');
		foreach (string raw in array)
		{
			string line = raw.Trim();
			int eq = line.IndexOf('=');
			if (eq > 0)
			{
				d[line.Substring(0, eq)] = line.Substring(eq + 1).Trim();
			}
		}
		return d;
	}

	private static string Get(Dictionary<string, string> d, string key)
	{
		if (!d.TryGetValue(key, out var v))
		{
			return "";
		}
		return v;
	}

	private static string Clip(string s, int max)
	{
		if (string.IsNullOrEmpty(s))
		{
			return "";
		}
		s = s.Replace("\r", " ").Replace("\n", " ").Trim();
		if (s.Length <= max)
		{
			return s;
		}
		return s.Substring(0, max) + "…";
	}

	public static string Slug(string s)
	{
		StringBuilder sb = new StringBuilder();
		string text = (s ?? "").ToLowerInvariant();
		foreach (char c in text)
		{
			if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
			{
				sb.Append(c);
			}
			else if (c == '.' || c == '_' || c == ' ')
			{
				sb.Append('-');
			}
		}
		string r = sb.ToString().Trim('-');
		while (r.IndexOf("--") >= 0)
		{
			r = r.Replace("--", "-");
		}
		if (r.Length == 0)
		{
			r = "agent";
		}
		if (r.Length > 40)
		{
			r = r.Substring(0, 40).Trim('-');
		}
		return r;
	}
}
