using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace BackupDotNet.Core;

public static class EsxiSsh
{
	public class SshResult
	{
		public int ExitCode;

		public string Output = "";

		public string Error = "";

		public string RawError = "";

		public bool Ok
		{
			get
			{
				if (ExitCode == 0)
				{
					return Error.Length == 0;
				}
				return false;
			}
		}
	}

	private static readonly object _sync = new object();

	public static SshResult Run(string host, int port, string user, string password, string command, string hostKeyCacheDir, int timeoutSec)
	{
		SshResult res = new SshResult();
		string plink = Hw.FindTool("plink");
		if (plink.Length == 0)
		{
			res.ExitCode = -1;
			res.Error = "plink.exe не найден (положите в " + Branding.Current.InstallDir + "\\tools)";
			return res;
		}
		string pwFile = "";
		try
		{
			pwFile = Path.Combine(Path.GetTempPath(), "bc-ssh-" + Guid.NewGuid().ToString("N") + ".tmp");
			File.WriteAllText(pwFile, password, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			RestrictFile(pwFile);
			string target = user + "@" + host;
			string portArg = ((port > 0 && port != 22) ? (" -P " + port) : "");
			SshResult first = Exec(plink, portArg, target, pwFile, command, timeoutSec, "");
			if (first.RawError.IndexOf("host key is not cached", StringComparison.OrdinalIgnoreCase) >= 0 || first.RawError.IndexOf("Cannot confirm a host key", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				string fp = ExtractFingerprint(first.RawError);
				if (fp.Length == 0)
				{
					res.ExitCode = -1;
					res.Error = "не удалось получить отпечаток ключа сервера";
					return res;
				}
				string cached = ReadCachedKey(hostKeyCacheDir, host, port);
				if (cached.Length > 0 && cached != fp)
				{
					res.ExitCode = -1;
					res.Error = "ключ сервера изменился (было " + Shorten(cached) + ", стало " + Shorten(fp) + ") — подключение отклонено";
					Log.Warn("ESXi SSH: ключ " + host + " изменился!");
					return res;
				}
				if (cached.Length == 0)
				{
					WriteCachedKey(hostKeyCacheDir, host, port, fp);
					Log.Info("ESXi SSH: запомнен ключ " + host + " (" + Shorten(fp) + ")");
				}
				return Exec(plink, portArg, target, pwFile, command, timeoutSec, fp);
			}
			return first;
		}
		catch (Exception ex)
		{
			res.ExitCode = -1;
			res.Error = ex.Message;
			return res;
		}
		finally
		{
			if (pwFile.Length > 0)
			{
				try
				{
					File.Delete(pwFile);
				}
				catch
				{
				}
			}
		}
	}

	private static SshResult Exec(string plink, string portArg, string target, string pwFile, string command, int timeoutSec, string hostKey)
	{
		SshResult res = new SshResult();
		string args = "-ssh -batch -no-antispoof" + portArg + " -pwfile \"" + pwFile + "\"";
		if (hostKey.Length > 0)
		{
			args = args + " -hostkey \"" + hostKey + "\"";
		}
		string text = args;
		args = text + " " + target + " " + QuoteCommand(command);
		Hw.ExecResult r = Hw.Run(plink, args, timeoutSec);
		res.ExitCode = r.ExitCode;
		res.Output = r.Output ?? "";
		res.RawError = r.Error ?? "";
		res.Error = CleanStderr(res.RawError);
		if (r.TimedOut)
		{
			res.ExitCode = -1;
			res.Error = "таймаут SSH (" + timeoutSec + " с)";
		}
		return res;
	}

	private static string QuoteCommand(string command)
	{
		return "\"" + command.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
	}

	private static string CleanStderr(string err)
	{
		if (string.IsNullOrEmpty(err))
		{
			return "";
		}
		List<string> keep = new List<string>();
		string[] array = err.Replace("\r\n", "\n").Split('\n');
		foreach (string raw in array)
		{
			string line = raw.Trim();
			if (line.Length != 0 && !line.StartsWith("Keyboard-interactive authentication prompts") && !line.StartsWith("End of keyboard-interactive prompts") && !line.StartsWith("Using username") && !line.StartsWith("Access granted") && !line.StartsWith("Remote working directory") && !line.StartsWith("Started a shell") && !line.StartsWith("The host key is not cached") && !line.StartsWith("You have no guarantee") && !line.StartsWith("think it is") && !line.StartsWith("The server's") && !line.StartsWith("Connection abandoned") && !line.StartsWith("FATAL ERROR: Cannot confirm") && !line.StartsWith("If you trust this host") && !line.StartsWith("Store key in cache") && !line.StartsWith("Update cached key") && !line.StartsWith("Enter ") && !line.StartsWith("Using "))
			{
				keep.Add(line);
			}
		}
		return string.Join("; ", keep.ToArray());
	}

	private static string ExtractFingerprint(string err)
	{
		Match m = Regex.Match(err, "(SHA256:[A-Za-z0-9+/=]{20,})");
		if (!m.Success)
		{
			return "";
		}
		return m.Groups[1].Value;
	}

	private static string Shorten(string fp)
	{
		if (fp.Length <= 22)
		{
			return fp;
		}
		return fp.Substring(0, 22) + "…";
	}

	private static void RestrictFile(string path)
	{
		try
		{
			FileSecurity sec = new FileSecurity();
			sec.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
			sec.AddAccessRule(new FileSystemAccessRule("SYSTEM", FileSystemRights.FullControl, AccessControlType.Allow));
			sec.AddAccessRule(new FileSystemAccessRule("Administrators", FileSystemRights.FullControl, AccessControlType.Allow));
			File.SetAccessControl(path, sec);
		}
		catch
		{
		}
	}

	public static SshResult RunInteractive(string host, int port, string user, string password, string[] commands, int delayMs, string hostKeyCacheDir, int timeoutSec)
	{
		SshResult res = new SshResult();
		string plink = Hw.FindTool("plink");
		if (plink.Length == 0)
		{
			res.ExitCode = -1;
			res.Error = "plink.exe не найден (положите в " + Branding.Current.InstallDir + "\\tools)";
			return res;
		}
		string hostKey = ReadCachedKey(hostKeyCacheDir, host, port);
		if (hostKey.Length == 0)
		{
			SshResult probe = Run(host, port, user, password, "exit", hostKeyCacheDir, timeoutSec);
			hostKey = ReadCachedKey(hostKeyCacheDir, host, port);
			if (hostKey.Length == 0)
			{
				res.ExitCode = -1;
				res.Error = ((probe.Error.Length > 0) ? probe.Error : "не удалось получить ключ хоста");
				return res;
			}
		}
		string pwFile = "";
		try
		{
			pwFile = Path.Combine(Path.GetTempPath(), "bc-ssh-" + Guid.NewGuid().ToString("N") + ".tmp");
			File.WriteAllText(pwFile, password, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			RestrictFile(pwFile);
			string args = "-ssh -batch -no-antispoof" + ((port > 0 && port != 22) ? (" -P " + port) : "") + " -pwfile \"" + pwFile + "\" -hostkey \"" + hostKey + "\" " + user + "@" + host;
			ProcessStartInfo psi = new ProcessStartInfo(plink, args);
			psi.RedirectStandardInput = true;
			psi.RedirectStandardOutput = true;
			psi.RedirectStandardError = true;
			psi.UseShellExecute = false;
			psi.CreateNoWindow = true;
			Process p = Process.Start(psi);
			try
			{
				Thread.Sleep(1500);
				foreach (string c in commands)
				{
					p.StandardInput.WriteLine(c);
					p.StandardInput.Flush();
					Thread.Sleep(delayMs);
				}
				p.StandardInput.WriteLine("exit");
				p.StandardInput.Flush();
				StringBuilder sb = new StringBuilder();
				StringBuilder eb = new StringBuilder();
				Thread readerOut = new Thread(() =>
				{
					try
					{
						sb.Append(p.StandardOutput.ReadToEnd());
					}
					catch
					{
					}
				});
				Thread readerErr = new Thread(() =>
				{
					try
					{
						eb.Append(p.StandardError.ReadToEnd());
					}
					catch
					{
					}
				});
				readerOut.IsBackground = true;
				readerErr.IsBackground = true;
				readerOut.Start();
				readerErr.Start();
				if (!p.WaitForExit(timeoutSec * 1000))
				{
					try
					{
						p.Kill();
					}
					catch
					{
					}
					res.ExitCode = -1;
					res.Error = "таймаут SSH (" + timeoutSec + " с)";
					return res;
				}
				readerOut.Join(3000);
				readerErr.Join(3000);
				res.ExitCode = p.ExitCode;
				res.Output = sb.ToString();
				res.RawError = eb.ToString();
				res.Error = CleanStderr(res.RawError);
			}
			finally
			{
				if (p != null)
				{
					((IDisposable)p).Dispose();
				}
			}
		}
		catch (Exception ex)
		{
			res.ExitCode = -1;
			res.Error = ex.Message;
		}
		finally
		{
			try
			{
				if (pwFile.Length > 0 && File.Exists(pwFile))
				{
					File.Delete(pwFile);
				}
			}
			catch
			{
			}
		}
		return res;
	}

	private static string CachePath(string dataDir)
	{
		if (string.IsNullOrEmpty(dataDir))
		{
			return "";
		}
		return Path.Combine(dataDir, "ssh-keys.json");
	}

	private static string ReadCachedKey(string dataDir, string host, int port)
	{
		string path = CachePath(dataDir);
		if (path.Length == 0 || !File.Exists(path))
		{
			return "";
		}
		lock (_sync)
		{
			try
			{
				Dictionary<string, object> root = Json.ParseObject(File.ReadAllText(path, Encoding.UTF8));
				if (!root.TryGetValue(host + ":" + port, out var v))
				{
					return "";
				}
				return (v == null) ? "" : Json.Str(v);
			}
			catch
			{
				return "";
			}
		}
	}

	private static void WriteCachedKey(string dataDir, string host, int port, string fp)
	{
		string path = CachePath(dataDir);
		if (path.Length == 0)
		{
			return;
		}
		lock (_sync)
		{
			try
			{
				Dictionary<string, object> root = new Dictionary<string, object>();
				if (File.Exists(path))
				{
					try
					{
						root = Json.ParseObject(File.ReadAllText(path, Encoding.UTF8));
					}
					catch
					{
						root = new Dictionary<string, object>();
					}
				}
				root[host + ":" + port] = fp;
				ConfigLoader.AtomicWrite(path, Json.WritePretty(root));
			}
			catch (Exception ex)
			{
				Log.Warn("ssh-keys.json: " + ex.Message);
			}
		}
	}
}
