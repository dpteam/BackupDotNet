using System;
using System.Globalization;
using System.Management;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading;
using BackupChecker.Core;
using BackupChecker.Deploy;

namespace BackupChecker.Manage;

public static class Runners
{
	private const string HyperVNamespace = "root\\virtualization\\v2";

	private static string Ps(string s)
	{
		if (s == null)
		{
			return "";
		}
		return "'" + s.Replace("'", "''") + "'";
	}

	public static OpResult RunLocal(string script, int timeoutSec)
	{
		OpResult res = new OpResult();
		try
		{
			using Runspace rs = RunspaceFactory.CreateRunspace();
			rs.Open();
			using PowerShell ps = PowerShell.Create();
			ps.Runspace = rs;
			ps.AddScript(script);
			IAsyncResult ar = ps.BeginInvoke();
			if (!ar.AsyncWaitHandle.WaitOne(timeoutSec * 1000))
			{
				try
				{
					ps.Stop();
				}
				catch
				{
				}
				res.Error = "таймаут " + timeoutSec + " с";
				return res;
			}
			PSDataCollection<PSObject> outData = ps.EndInvoke(ar);
			StringBuilder sb = new StringBuilder();
			foreach (PSObject o in outData)
			{
				if (o != null)
				{
					sb.AppendLine(o.ToString());
				}
			}
			res.Output = sb.ToString().TrimEnd();
			if (ps.Streams.Error.Count > 0)
			{
				StringBuilder eb = new StringBuilder();
				int n = 0;
				foreach (ErrorRecord er in ps.Streams.Error)
				{
					if (n++ > 0)
					{
						eb.AppendLine();
					}
					eb.Append(er.ToString());
				}
				res.Error = eb.ToString().Trim();
				res.Ok = false;
			}
			else
			{
				res.Ok = true;
			}
		}
		catch (Exception ex)
		{
			res.Ok = false;
			res.Error = WinRmSession.Describe(ex);
		}
		return res;
	}

	public static bool IsLocal(string target)
	{
		if (string.IsNullOrEmpty(target))
		{
			return false;
		}
		string t = target.Trim();
		if (!string.Equals(t, "local", StringComparison.OrdinalIgnoreCase) && !string.Equals(t, "localhost", StringComparison.OrdinalIgnoreCase) && !string.Equals(t, ".", StringComparison.Ordinal))
		{
			return string.Equals(t, Environment.MachineName, StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	public static OpResult RunWinRm(string host, int port, bool https, string user, string password, string script, int timeoutSec)
	{
		OpResult res = new OpResult();
		string error;
		using WinRmSession s = WinRmSession.Open(host, port, https, user, password, 20000, out error);
		if (s == null)
		{
			res.Error = "WinRM: " + error;
			return res;
		}
		PsResult r = s.Run(script, timeoutSec * 1000);
		res.Ok = r.Ok;
		res.Output = r.Output ?? "";
		res.Error = (r.Ok ? "" : (r.Error ?? ""));
		return res;
	}

	public static string ScriptHostInfo()
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("$ErrorActionPreference = 'Stop'");
		sb.AppendLine("$os = Get-CimInstance Win32_OperatingSystem");
		sb.AppendLine("$boot = $os.LastBootUpTime");
		sb.AppendLine("$up = (Get-Date) - $boot");
		sb.AppendLine("$free = 0");
		sb.AppendLine("try { $free = [math]::Round((Get-PSDrive C).Free / 1GB, 2) } catch { }");
		sb.AppendLine("'host=' + $env:COMPUTERNAME");
		sb.AppendLine("'os=' + $os.Caption");
		sb.AppendLine("'version=' + $os.Version");
		sb.AppendLine("'uptimeHours=' + [math]::Round($up.TotalHours, 1)");
		sb.AppendLine("'bootTime=' + $boot.ToString('yyyy-MM-dd HH:mm')");
		sb.AppendLine("'freeGbC=' + $free");
		sb.AppendLine("'timeZone=' + (Get-TimeZone).Id");
		return sb.ToString();
	}

	public static string ScriptServiceList()
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("$ErrorActionPreference = 'Stop'");
		sb.AppendLine("Get-CimInstance Win32_Service |");
		sb.AppendLine("  Where-Object { $_.State -eq 'Running' -or $_.StartMode -eq 'Auto' } |");
		sb.AppendLine("  Sort-Object Name |");
		sb.AppendLine("  ForEach-Object { $_.Name + '|' + $_.State + '|' + $_.StartMode + '|' + $_.DisplayName }");
		return sb.ToString();
	}

	public static string ScriptServiceAction(string action, string service)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("$ErrorActionPreference = 'Stop'");
		sb.AppendLine("$name = " + Ps(service));
		sb.AppendLine("$svc = Get-Service -Name $name -ErrorAction SilentlyContinue");
		sb.AppendLine("if (-not $svc) { throw ('служба ' + $name + ' не найдена') }");
		sb.AppendLine("'before=' + $svc.Status");
		switch (action)
		{
		case "restart":
			sb.AppendLine("if ($svc.Status -ne 'Stopped') {");
			sb.AppendLine("  Stop-Service -Name $name -Force");
			sb.AppendLine("  $svc.WaitForStatus('Stopped', '00:00:45')");
			sb.AppendLine("}");
			sb.AppendLine("Start-Service -Name $name");
			break;
		case "start":
			sb.AppendLine("if ($svc.Status -ne 'Running') { Start-Service -Name $name }");
			break;
		case "stop":
			sb.AppendLine("if ($svc.Status -ne 'Stopped') {");
			sb.AppendLine("  Stop-Service -Name $name -Force");
			sb.AppendLine("}");
			break;
		}
		sb.AppendLine("Start-Sleep -Seconds 2");
		sb.AppendLine("$after = (Get-Service -Name $name).Status");
		sb.AppendLine("'after=' + $after");
		sb.AppendLine("if ($after -ne 'Running' -and '" + action + "' -ne 'stop') {");
		sb.AppendLine("  throw ('служба не запустилась: ' + $after)");
		sb.AppendLine("}");
		return sb.ToString();
	}

	public static string ScriptReboot(string reason, int delaySec)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("$ErrorActionPreference = 'Stop'");
		sb.AppendLine("$r = " + Ps(reason));
		sb.AppendLine("'host=' + $env:COMPUTERNAME");
		sb.AppendLine("& shutdown.exe /r /f /t " + delaySec.ToString(CultureInfo.InvariantCulture) + " /c $r | Out-Null");
		sb.AppendLine("'scheduled=перезагрузка через " + delaySec + " с'");
		return sb.ToString();
	}

	private static OpResult WaitJob(ManagementBaseObject outParams, int timeoutSec)
	{
		OpResult res = new OpResult();
		if (outParams == null)
		{
			return OpResult.Fail("Hyper-V не вернул результат");
		}
		uint code = Convert.ToUInt32(outParams["ReturnValue"], CultureInfo.InvariantCulture);
		switch (code)
		{
		case 0u:
			res.Ok = true;
			res.Output = "выполнено";
			return res;
		default:
			return OpResult.Fail("Hyper-V вернул код " + code);
		case 4096u:
		{
			object jobObj = outParams["Job"];
			if (!(jobObj is ManagementPath jobPath))
			{
				res.Ok = true;
				res.Output = "принято";
				return res;
			}
			DateTime deadline = DateTime.Now.AddSeconds(timeoutSec);
			while (DateTime.Now < deadline)
			{
				Thread.Sleep(1000);
				try
				{
					using ManagementObject job = new ManagementObject(jobPath);
					job.Get();
					int state = Hw.PropInt(job, "JobState", -1);
					switch (state)
					{
					case 7:
						res.Ok = true;
						res.Output = "выполнено";
						return res;
					case 8:
					case 9:
					case 10:
					{
						int err = Hw.PropInt(job, "ErrorCode", 0);
						string desc = Hw.Prop(job, "ErrorDescription");
						return OpResult.Fail("задание Hyper-V не выполнено (состояние " + state + ((err != 0) ? (", код " + err) : "") + ((desc.Length > 0) ? (": " + desc) : "") + ")");
					}
					}
				}
				catch (Exception ex)
				{
					return OpResult.Fail("не удалось прочитать состояние задания: " + ex.Message);
				}
			}
			return OpResult.Fail("таймаут ожидания задания Hyper-V (" + timeoutSec + " с)");
		}
		}
	}

	private static ManagementObject FindVm(string name)
	{
		ManagementObjectCollection col = Hw.Query("root\\virtualization\\v2", "SELECT * FROM Msvm_ComputerSystem");
		if (col == null)
		{
			return null;
		}
		string hostName = Environment.MachineName;
		foreach (ManagementObject o in col)
		{
			string element = Hw.Prop(o, "ElementName");
			if (string.Equals(element, hostName, StringComparison.OrdinalIgnoreCase))
			{
				o.Dispose();
				continue;
			}
			if (string.Equals(element, name, StringComparison.OrdinalIgnoreCase))
			{
				return o;
			}
			o.Dispose();
		}
		return null;
	}

	public static OpResult VmPower(string name, bool start)
	{
		if (string.IsNullOrEmpty(name))
		{
			return OpResult.Fail("не задано имя ВМ");
		}
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("$ErrorActionPreference = 'Stop'");
		sb.AppendLine("$vm = Get-VM -Name " + Ps(name) + " -ErrorAction Stop");
		sb.AppendLine("'before=' + $vm.State");
		sb.AppendLine(start ? "Start-VM -VM $vm -ErrorAction Stop" : "Stop-VM -VM $vm -ErrorAction Stop");
		sb.AppendLine("$vm = Get-VM -Name " + Ps(name));
		sb.AppendLine("'after=' + $vm.State");
		sb.AppendLine("if ('" + (start ? "Running" : "Off") + "' -ne $vm.State.ToString()) {");
		sb.AppendLine("  throw ('состояние не изменилось: ' + $vm.State)");
		sb.AppendLine("}");
		return RunLocal(sb.ToString(), 180);
	}

	public static OpResult VmSnapshot(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return OpResult.Fail("не задано имя ВМ");
		}
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("$ErrorActionPreference = 'Stop'");
		sb.AppendLine("$vm = Get-VM -Name " + Ps(name) + " -ErrorAction Stop");
		sb.AppendLine("$snapName = 'checkpoint-' + (Get-Date -Format 'yyyyMMdd-HHmmss')");
		sb.AppendLine("Checkpoint-VM -VM $vm -SnapshotName $snapName -ErrorAction Stop");
		sb.AppendLine("$found = Get-VMSnapshot -VM $vm | Where-Object { $_.Name -eq $snapName }");
		sb.AppendLine("if (-not $found) { throw 'снимок не появился' }");
		sb.AppendLine("'snapshot=' + $snapName");
		OpResult r = RunLocal(sb.ToString(), 600);
		if (r.Ok)
		{
			r.Output = "снимок создан: " + ExtractValue(r.Output, "snapshot=");
		}
		return r;
	}

	private static string ExtractValue(string output, string key)
	{
		if (string.IsNullOrEmpty(output))
		{
			return "";
		}
		string[] array = output.Replace("\r\n", "\n").Split('\n');
		foreach (string raw in array)
		{
			string line = raw.Trim();
			if (line.StartsWith(key, StringComparison.OrdinalIgnoreCase))
			{
				return line.Substring(key.Length).Trim();
			}
		}
		return "";
	}

	public static OpResult VmList()
	{
		OpResult res = new OpResult();
		StringBuilder sb = new StringBuilder();
		try
		{
			ManagementObjectCollection col = Hw.Query("root\\virtualization\\v2", "SELECT * FROM Msvm_ComputerSystem");
			if (col == null)
			{
				return OpResult.Fail("Hyper-V недоступен (это не хост Hyper-V?)");
			}
			string hostName = Environment.MachineName;
			foreach (ManagementObject o in col)
			{
				using (o)
				{
					string name = Hw.Prop(o, "ElementName");
					if (name.Length != 0 && !string.Equals(name, hostName, StringComparison.OrdinalIgnoreCase))
					{
						int state = Hw.PropInt(o, "EnabledState", -1);
						sb.AppendLine(name + "|" + HyperVCollector.StateName(state) + "|" + state);
					}
				}
			}
			if (sb.Length == 0)
			{
				sb.AppendLine("(виртуальных машин нет)");
			}
			res.Ok = true;
			res.Output = sb.ToString().TrimEnd();
			return res;
		}
		catch (Exception ex)
		{
			return OpResult.Fail("Hyper-V: " + ex.Message);
		}
	}
}
