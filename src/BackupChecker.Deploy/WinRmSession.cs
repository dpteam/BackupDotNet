using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security;
using System.Security.Principal;
using System.Text;

namespace BackupChecker.Deploy;

public class WinRmSession : IDisposable
{
	private Runspace _rs;

	private readonly string _host;

	private readonly object _sync = new object();

	public string Host => _host;

	private WinRmSession(string host)
	{
		_host = host;
	}

	public static WinRmSession Open(string host, int port, bool https, string user, string password, int timeoutMs, out string error)
	{
		error = "";
		List<string> variants = new List<string>();
		if (user != null && (user.IndexOf('\\') >= 0 || user.IndexOf('@') >= 0))
		{
			variants.Add(user);
		}
		else if (IsSystem())
		{
			variants.Add(host + "\\" + user);
			variants.Add(user);
		}
		else
		{
			variants.Add(user);
			variants.Add(host + "\\" + user);
		}
		foreach (string u in variants)
		{
			WinRmSession s = TryOpen(host, port, https, u, password, timeoutMs, out error);
			if (s != null)
			{
				return s;
			}
			if (!LooksLikeCredentialError(error))
			{
				break;
			}
		}
		return null;
	}

	private static WinRmSession TryOpen(string host, int port, bool https, string user, string password, int timeoutMs, out string error)
	{
		error = "";
		WinRmSession s = new WinRmSession(host);
		try
		{
			SecureString sec = new SecureString();
			string text = password ?? "";
			foreach (char c in text)
			{
				sec.AppendChar(c);
			}
			sec.MakeReadOnly();
			PSCredential cred = new PSCredential(user ?? "", sec);
			if (port <= 0)
			{
				port = (https ? 5986 : 5985);
			}
			string scheme = (https ? "https" : "http");
			WSManConnectionInfo ci = new WSManConnectionInfo(new Uri(scheme + "://" + host + ":" + port + "/wsman"), "http://schemas.microsoft.com/powershell/Microsoft.PowerShell", cred);
			ci.AuthenticationMechanism = AuthenticationMechanism.Negotiate;
			ci.OperationTimeout = timeoutMs;
			ci.OpenTimeout = timeoutMs;
			if (https)
			{
				ci.SkipCACheck = true;
				ci.SkipCNCheck = true;
				ci.SkipRevocationCheck = true;
			}
			s._rs = RunspaceFactory.CreateRunspace(ci);
			s._rs.Open();
			return s;
		}
		catch (Exception ex)
		{
			error = Describe(ex);
			s.Dispose();
			return null;
		}
	}

	private static bool IsSystem()
	{
		try
		{
			return WindowsIdentity.GetCurrent().IsSystem;
		}
		catch
		{
			return false;
		}
	}

	private static bool LooksLikeCredentialError(string error)
	{
		if (string.IsNullOrEmpty(error))
		{
			return false;
		}
		if (error.IndexOf("8009030d", StringComparison.OrdinalIgnoreCase) < 0 && error.IndexOf("учетные данные", StringComparison.OrdinalIgnoreCase) < 0 && error.IndexOf("credentials", StringComparison.OrdinalIgnoreCase) < 0 && error.IndexOf("logon", StringComparison.OrdinalIgnoreCase) < 0)
		{
			return error.IndexOf("проверки подлинности", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return true;
	}

	public PsResult Run(string script, int timeoutMs)
	{
		PsResult r = new PsResult();
		if (_rs == null)
		{
			r.Ok = false;
			r.Error = "сессия WinRM не открыта";
			return r;
		}
		lock (_sync)
		{
			try
			{
				using PowerShell ps = PowerShell.Create();
				ps.Runspace = _rs;
				ps.AddScript(script);
				IAsyncResult ar = ps.BeginInvoke();
				if (!ar.AsyncWaitHandle.WaitOne(timeoutMs))
				{
					try
					{
						ps.Stop();
					}
					catch
					{
					}
					r.Ok = false;
					r.Error = "таймаут " + timeoutMs / 1000 + " с";
					return r;
				}
				PSDataCollection<PSObject> res = ps.EndInvoke(ar);
				StringBuilder sb = new StringBuilder();
				foreach (PSObject o in res)
				{
					if (o != null)
					{
						sb.AppendLine(o.ToString());
					}
				}
				r.Output = sb.ToString().TrimEnd();
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
					r.Error = eb.ToString().Trim();
					r.Ok = false;
				}
			}
			catch (Exception ex)
			{
				r.Ok = false;
				r.Error = Describe(ex);
			}
		}
		return r;
	}

	public bool IsAlive()
	{
		if (_rs == null)
		{
			return false;
		}
		try
		{
			return _rs.RunspaceStateInfo.State == RunspaceState.Opened;
		}
		catch
		{
			return false;
		}
	}

	public void Dispose()
	{
		try
		{
			if (_rs != null)
			{
				if (_rs.RunspaceStateInfo.State != RunspaceState.Closed)
				{
					_rs.Close();
				}
				_rs.Dispose();
			}
		}
		catch
		{
		}
		finally
		{
			_rs = null;
		}
	}

	public static string Describe(Exception ex)
	{
		StringBuilder sb = new StringBuilder();
		Exception cur = ex;
		int depth = 0;
		while (cur != null && depth < 5)
		{
			string msg = (cur.Message ?? "").Trim();
			if (msg.Length > 0 && sb.ToString().IndexOf(msg, StringComparison.Ordinal) < 0)
			{
				if (sb.Length > 0)
				{
					sb.Append(" · ");
				}
				sb.Append(msg);
			}
			cur = cur.InnerException;
			depth++;
		}
		if (sb.Length <= 0)
		{
			return ex.GetType().Name;
		}
		return sb.ToString();
	}
}
