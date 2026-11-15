using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;

namespace BackupDotNet.Core;

public static class Hw
{
	public class DiskInfo
	{
		public string DeviceId = "";

		public string Model = "";

		public string Serial = "";

		public string MediaType = "";

		public long SizeBytes;

		public string Health = "";

		public int HealthCode = -1;

		public int Temperature = -1;

		public int Wear = -1;

		public int LifeLeft = -1;

		public long PowerOnHours = -1L;

		public long ReadErrors = -1L;

		public long WriteErrors = -1L;

		public string SmartStatus = "";

		public Dictionary<int, long> Attributes = new Dictionary<int, long>();

		public Dictionary<int, long> AttrNorm = new Dictionary<int, long>();

		public List<string> Notes = new List<string>();
	}

	public class SmartScan
	{
		public string Note = "";

		public List<DiskInfo> Disks = new List<DiskInfo>();
	}

	public class RaidInfo
	{
		public string Vendor = "";

		public string Controller = "";

		public string State = "";

		public List<RaidVolume> Volumes = new List<RaidVolume>();

		public List<RaidDisk> Disks = new List<RaidDisk>();

		public string Battery = "";

		public List<string> Notes = new List<string>();
	}

	public class RaidVolume
	{
		public string Id = "";

		public string Name = "";

		public string Raid = "";

		public string State = "";

		public long SizeBytes;
	}

	public class RaidDisk
	{
		public string Slot = "";

		public string Model = "";

		public string State = "";

		public string Size = "";
	}

	public class Sensor
	{
		public string Kind = "";

		public string Name = "";

		public long Value = -1L;

		public string Text = "";

		public bool Bad;
	}

	public class HealthInfo
	{
		public string Source = "";

		public string Note = "";

		public List<Sensor> Sensors = new List<Sensor>();
	}

	public class IpmiOptions
	{
		public bool Use = true;

		public string Host = "";

		public string User = "";

		public string Password = "";

		public string Interface = "lanplus";
	}

	public class ExecResult
	{
		public int ExitCode;

		public string Output = "";

		public string Error = "";

		public bool TimedOut;
	}

	public static ManagementObjectCollection Query(string ns, string wql)
	{
		try
		{
			ManagementScope scope = new ManagementScope("\\\\.\\" + ns);
			scope.Connect();
			using ManagementObjectSearcher s = new ManagementObjectSearcher(scope, new ObjectQuery(wql));
			return s.Get();
		}
		catch (Exception ex)
		{
			Log.Debug("WMI " + ns + ": " + ex.Message);
			return null;
		}
	}

	public static string Prop(ManagementBaseObject o, string name)
	{
		try
		{
			object v = o[name];
			if (v == null)
			{
				return "";
			}
			return Convert.ToString(v, CultureInfo.InvariantCulture);
		}
		catch
		{
			return "";
		}
	}

	public static long PropLong(ManagementBaseObject o, string name, long dflt)
	{
		try
		{
			object v = o[name];
			if (v == null)
			{
				return dflt;
			}
			return Convert.ToInt64(v, CultureInfo.InvariantCulture);
		}
		catch
		{
			return dflt;
		}
	}

	public static int PropInt(ManagementBaseObject o, string name, int dflt)
	{
		long v = PropLong(o, name, dflt);
		if (v > int.MaxValue)
		{
			return int.MaxValue;
		}
		if (v < int.MinValue)
		{
			return int.MinValue;
		}
		return (int)v;
	}

	public static List<string> PropStrings(ManagementBaseObject o, string name)
	{
		List<string> result = new List<string>();
		try
		{
			object v = o[name];
			if (v == null)
			{
				return result;
			}
			if (v is string[] arr)
			{
				string[] array = arr;
				foreach (string s in array)
				{
					if (!string.IsNullOrEmpty(s))
					{
						result.Add(s);
					}
				}
				return result;
			}
			string single = Convert.ToString(v, CultureInfo.InvariantCulture);
			if (!string.IsNullOrEmpty(single))
			{
				result.Add(single);
			}
		}
		catch
		{
		}
		return result;
	}

	public static bool PropBool(ManagementBaseObject o, string name)
	{
		try
		{
			object v = o[name];
			if (v == null)
			{
				return false;
			}
			return Convert.ToBoolean(v, CultureInfo.InvariantCulture);
		}
		catch
		{
			return false;
		}
	}

	public static ExecResult Run(string exe, string args, int timeoutSec)
	{
		return Run(exe, args, timeoutSec, null);
	}

	public static ExecResult RunExe(string exe, string args, int timeoutSec, Encoding outputEncoding)
	{
		return Run(exe, args, timeoutSec, null, outputEncoding);
	}

	public static ExecResult Run(string exe, string args, int timeoutSec, Dictionary<string, string> env)
	{
		return Run(exe, args, timeoutSec, env, null);
	}

	public static ExecResult Run(string exe, string args, int timeoutSec, Dictionary<string, string> env, Encoding outputEncoding)
	{
		ExecResult r = new ExecResult();
		try
		{
			ProcessStartInfo psi = new ProcessStartInfo(exe, args);
			psi.RedirectStandardOutput = true;
			psi.RedirectStandardError = true;
			psi.UseShellExecute = false;
			psi.CreateNoWindow = true;
			psi.StandardOutputEncoding = outputEncoding ?? Encoding.UTF8;
			psi.StandardErrorEncoding = outputEncoding ?? Encoding.UTF8;
			if (env != null)
			{
				foreach (KeyValuePair<string, string> kv in env)
				{
					psi.EnvironmentVariables[kv.Key] = kv.Value ?? "";
				}
			}
			using Process p = Process.Start(psi);
			if (p == null)
			{
				r.Error = "не удалось запустить " + exe;
				return r;
			}
			StringBuilder sb = new StringBuilder();
			p.OutputDataReceived += (object s, DataReceivedEventArgs e) =>
			{
				if (e.Data != null)
				{
					sb.AppendLine(e.Data);
				}
			};
			StringBuilder eb = new StringBuilder();
			p.ErrorDataReceived += (object s, DataReceivedEventArgs e) =>
			{
				if (e.Data != null)
				{
					eb.AppendLine(e.Data);
				}
			};
			p.BeginOutputReadLine();
			p.BeginErrorReadLine();
			if (!p.WaitForExit(timeoutSec * 1000))
			{
				try
				{
					p.Kill();
				}
				catch
				{
				}
				r.TimedOut = true;
				r.Error = "таймаут " + timeoutSec + " с";
				return r;
			}
			p.WaitForExit();
			r.Output = sb.ToString();
			r.Error = eb.ToString();
			r.ExitCode = p.ExitCode;
		}
		catch (Exception ex)
		{
			r.Error = ex.Message;
		}
		return r;
	}

	public static string FindTool(string name)
	{
		try
		{
			string pathVar = Environment.GetEnvironmentVariable("PATH");
			if (!string.IsNullOrEmpty(pathVar))
			{
				string[] array = pathVar.Split(';');
				foreach (string dir in array)
				{
					if (dir.Trim().Length == 0)
					{
						continue;
					}
					try
					{
						string full = Path.Combine(dir.Trim(), name + ".exe");
						if (File.Exists(full))
						{
							return full;
						}
					}
					catch
					{
					}
				}
			}
			List<string> near = new List<string>();
			try
			{
				string baseDir = AppDomain.CurrentDomain.BaseDirectory;
				near.Add(Path.Combine(baseDir, "tools"));
				near.Add(baseDir);
				near.Add(Path.Combine(baseDir, "..", "tools"));
			}
			catch
			{
			}
			string[] extra = new string[8] { "C:\\Program Files\\smartmontools\\bin", "C:\\Program Files (x86)\\smartmontools\\bin", "C:\\Program Files\\Dell\\perccli", "C:\\Program Files\\Broadcom\\storcli", "C:\\Program Files (x86)\\MegaRAID Storage Manager", "C:\\Program Files\\HPE\\ssacli", "C:\\Program Files (x86)\\Adaptec\\arcconf", "C:\\Distrib" };
			string brandTools = Path.Combine(Branding.Current.InstallDir, "tools");
			List<string> extraList = new List<string>(extra);
			if (!extraList.Contains(brandTools))
			{
				extraList.Insert(0, brandTools);
			}
			extra = extraList.ToArray();
			foreach (string dir2 in near)
			{
				try
				{
					string full2 = Path.GetFullPath(Path.Combine(dir2, name + ".exe"));
					if (File.Exists(full2))
					{
						return full2;
					}
				}
				catch
				{
				}
			}
			string[] array2 = extra;
			foreach (string dir3 in array2)
			{
				try
				{
					string full3 = Path.Combine(dir3, name + ".exe");
					if (File.Exists(full3))
					{
						return full3;
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		return "";
	}

	public static void ParseAtaSmart(byte[] vendorSpecific, Dictionary<int, long> attrs, Dictionary<int, long> norms)
	{
		if (vendorSpecific == null || vendorSpecific.Length < 2)
		{
			return;
		}
		int count = vendorSpecific[1];
		if (count > 30)
		{
			count = 30;
		}
		for (int i = 0; i < count; i++)
		{
			int off = 2 + i * 12;
			if (off + 12 > vendorSpecific.Length)
			{
				break;
			}
			int id = vendorSpecific[off];
			if (id != 0)
			{
				if (norms != null)
				{
					norms[id] = vendorSpecific[off + 3];
				}
				long raw = 0L;
				for (int b = 0; b < 6; b++)
				{
					raw |= (long)((ulong)vendorSpecific[off + 5 + b] << 8 * b);
				}
				attrs[id] = raw;
			}
		}
	}

	public static List<string> CheckSmartAttributes(Dictionary<int, long> attrs, Dictionary<int, long> norms)
	{
		List<string> problems = new List<string>();
		CheckRaw(attrs, 5, "Reallocated_Sector_Ct", 0L, 10L, problems);
		CheckRaw(attrs, 187, "Reported_Uncorrect", 0L, 1L, problems);
		CheckRaw(attrs, 197, "Current_Pending_Sector", 0L, 1L, problems);
		CheckRaw(attrs, 198, "Offline_Uncorrectable", 0L, 1L, problems);
		CheckRaw(attrs, 199, "UDMA_CRC_Error_Count", 0L, 100L, problems);
		if (norms != null)
		{
			if (norms.TryGetValue(231, out var life) && life >= 0 && life <= 100 && life < 20)
			{
				problems.Add("SSD_Life_Left = " + life + "% (ресурс на исходе)");
			}
			if (norms.TryGetValue(233, out life) && life >= 0 && life <= 100 && life < 20)
			{
				problems.Add("Media_Wearout_Indicator = " + life + "% (ресурс на исходе)");
			}
			if (norms.TryGetValue(177, out life) && life >= 0 && life <= 100 && life < 20)
			{
				problems.Add("Wear_Leveling_Count = " + life + "% (ресурс на исходе)");
			}
		}
		return problems;
	}

	private static void CheckRaw(Dictionary<int, long> attrs, int id, string name, long warnAt, long failAt, List<string> problems)
	{
		if (attrs.TryGetValue(id, out var v))
		{
			if (v >= failAt)
			{
				problems.Add(name + " = " + v + " (критично)");
			}
			else if (v > warnAt)
			{
				problems.Add(name + " = " + v);
			}
		}
	}

	public static string FormatBytes(long bytes)
	{
		if (bytes <= 0)
		{
			return "—";
		}
		double b = bytes;
		string[] units = new string[5] { "Б", "КБ", "МБ", "ГБ", "ТБ" };
		int i = 0;
		while (b >= 1024.0 && i < units.Length - 1)
		{
			b /= 1024.0;
			i++;
		}
		return string.Format(CultureInfo.InvariantCulture, (i == 0) ? "{0:0} {1}" : "{0:0.#} {1}", b, units[i]);
	}

	public static long FirstNumber(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return -1L;
		}
		Match m = Regex.Match(s, "-?\\d[\\d\\s,.]*");
		if (!m.Success)
		{
			return -1L;
		}
		string digits = Regex.Replace(m.Value, "[^\\d-]", "");
		if (long.TryParse(digits, out var v))
		{
			return v;
		}
		return -1L;
	}

	public static DateTime? ParseWmiDate(object value)
	{
		if (value == null)
		{
			return null;
		}
		if (value is DateTime)
		{
			return (DateTime)value;
		}
		string s = value as string;
		if (s == null)
		{
			s = Convert.ToString(value, CultureInfo.InvariantCulture);
		}
		if (string.IsNullOrEmpty(s))
		{
			return null;
		}
		s = s.Trim();
		if (s.Length < 14)
		{
			return null;
		}
		if (!int.TryParse(s.Substring(0, 4), out var year))
		{
			return null;
		}
		if (!int.TryParse(s.Substring(4, 2), out var month))
		{
			return null;
		}
		if (!int.TryParse(s.Substring(6, 2), out var day))
		{
			return null;
		}
		if (!int.TryParse(s.Substring(8, 2), out var hour))
		{
			return null;
		}
		if (!int.TryParse(s.Substring(10, 2), out var minute))
		{
			return null;
		}
		if (!int.TryParse(s.Substring(12, 2), out var second))
		{
			return null;
		}
		int pos = 14;
		int ms = 0;
		if (s.Length > 15 && s[14] == '.')
		{
			int len = Math.Min(6, s.Length - 15);
			string frac = s.Substring(15, len);
			string msStr = ((frac.Length >= 3) ? frac.Substring(0, 3) : frac.PadRight(3, '0'));
			int.TryParse(msStr, out ms);
			pos = 15 + len;
		}
		int offsetMinutes = 0;
		if (s.Length >= pos + 4 && (s[pos] == '+' || s[pos] == '-') && int.TryParse(s.Substring(pos + 1, 3), out var off))
		{
			offsetMinutes = ((s[pos] == '-') ? (-off) : off);
		}
		try
		{
			DateTime dt = new DateTime(year, month, day, hour, minute, second, ms, DateTimeKind.Unspecified);
			if (offsetMinutes != 0)
			{
				dt = dt.AddMinutes(-offsetMinutes);
			}
			return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
		}
		catch
		{
			return null;
		}
	}
}
