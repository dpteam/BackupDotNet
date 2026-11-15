using System;
using System.Collections.Generic;
using System.Globalization;

namespace BackupDotNet.Core;

public static class BmcSsh
{
	public class BmcSensor
	{
		public string Target = "";

		public string Name = "";

		public string Kind = "";

		public double Value = -1.0;

		public string State = "";

		public bool HasValue;

		public bool Discrete;

		public string SensorType = "";
	}

	public class BmcInfo
	{
		public string Error = "";

		public string Model = "";

		public string ShellVersion = "";

		public int PowerState = -1;

		public List<BmcSensor> Sensors = new List<BmcSensor>();
	}

	public static BmcInfo Read(string host, int port, string user, string password, string dataDir, int timeoutSec)
	{
		BmcInfo info = new BmcInfo();
		if (string.IsNullOrEmpty(host))
		{
			info.Error = "не задан адрес BMC";
			return info;
		}
		List<string> commands = new List<string>();
		commands.Add("show /system1/sensors1");
		EsxiSsh.SshResult res = EsxiSsh.RunInteractive(host, port, user, password, commands.ToArray(), 1500, dataDir, timeoutSec);
		if (res.ExitCode != 0 || res.Output.Length == 0)
		{
			info.Error = ((res.Error.Length > 0) ? res.Error : "нет ответа от BMC");
			return info;
		}
		List<string> targets = ParseTargets(res.Output);
		if (targets.Count == 0)
		{
			info.Error = "SMASH-CLP не вернул список датчиков";
			return info;
		}
		for (int i = 0; i < targets.Count; i += 12)
		{
			List<string> chunk = new List<string>();
			for (int j = i; j < targets.Count && j < i + 12; j++)
			{
				chunk.Add("show /system1/sensors1/" + targets[j]);
			}
			EsxiSsh.SshResult part = EsxiSsh.RunInteractive(host, port, user, password, chunk.ToArray(), 700, dataDir, timeoutSec);
			if (part.Output.Length != 0)
			{
				List<BmcSensor> parsed = ParseSensors(part.Output);
				info.Sensors.AddRange(parsed);
			}
		}
		EsxiSsh.SshResult extra = EsxiSsh.RunInteractive(host, port, user, password, new string[2] { "show /system1/pwrmgtsvc1", "version" }, 1200, dataDir, timeoutSec);
		if (extra.Output.Length > 0)
		{
			info.PowerState = ParseIntProp(extra.Output, "PowerState");
			info.ShellVersion = ParseVersion(extra.Output);
		}
		if (info.Sensors.Count == 0)
		{
			info.Error = "датчики найдены, но не разобраны (неожиданный формат SMASH-CLP)";
		}
		return info;
	}

	public static List<string> ParseTargets(string text)
	{
		List<string> result = new List<string>();
		if (string.IsNullOrEmpty(text))
		{
			return result;
		}
		bool inTargets = false;
		string[] array = text.Replace("\r\n", "\n").Split('\n');
		foreach (string raw in array)
		{
			string line = raw.TrimEnd();
			string t = line.Trim();
			if (t.StartsWith("Targets", StringComparison.OrdinalIgnoreCase))
			{
				inTargets = true;
			}
			else if (t.StartsWith("Properties", StringComparison.OrdinalIgnoreCase) || t.StartsWith("Verbs", StringComparison.OrdinalIgnoreCase))
			{
				inTargets = false;
			}
			else if (inTargets && t.Length != 0 && t.IndexOf(' ') < 0 && !t.Equals("none", StringComparison.OrdinalIgnoreCase) && !result.Contains(t))
			{
				result.Add(t);
			}
		}
		return result;
	}

	public static List<BmcSensor> ParseSensors(string text)
	{
		List<BmcSensor> result = new List<BmcSensor>();
		if (string.IsNullOrEmpty(text))
		{
			return result;
		}
		BmcSensor cur = null;
		string[] array = text.Replace("\r\n", "\n").Split('\n');
		foreach (string raw in array)
		{
			string line = StripPrompt(raw);
			if (line.StartsWith("show /system1/sensors1/", StringComparison.OrdinalIgnoreCase))
			{
				cur = new BmcSensor();
				cur.Target = line.Substring("show /system1/sensors1/".Length).Trim();
				result.Add(cur);
			}
			else
			{
				if (cur == null)
				{
					continue;
				}
				int eq = line.IndexOf('=');
				if (eq <= 0)
				{
					continue;
				}
				string key = line.Substring(0, eq).Trim();
				string val = line.Substring(eq + 1).Trim();
				if (key.Equals("Description", StringComparison.OrdinalIgnoreCase))
				{
					cur.Name = CleanName(val);
				}
				else if (key.Equals("CurrentReading", StringComparison.OrdinalIgnoreCase))
				{
					if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
					{
						cur.Value = d;
						cur.HasValue = true;
					}
				}
				else if (key.Equals("CurrentState", StringComparison.OrdinalIgnoreCase))
				{
					cur.State = val;
				}
				else if (key.Equals("SensorType", StringComparison.OrdinalIgnoreCase))
				{
					cur.SensorType = val;
				}
			}
		}
		foreach (BmcSensor s in result)
		{
			Classify(s);
		}
		return result;
	}

	private static void Classify(BmcSensor s)
	{
		switch (s.SensorType)
		{
		case "1":
			s.Kind = "temp";
			break;
		case "2":
			s.Kind = "voltage";
			break;
		case "4":
			s.Kind = "fan";
			break;
		default:
			s.Kind = (s.HasValue ? "other" : "discrete");
			break;
		}
		if (!s.HasValue)
		{
			s.Discrete = true;
		}
		if (s.Name.Length == 0)
		{
			s.Name = s.Target;
		}
	}

	public static string CleanName(string description)
	{
		if (string.IsNullOrEmpty(description))
		{
			return "";
		}
		string s = description;
		int paren = s.IndexOf('(');
		if (paren > 0)
		{
			s = s.Substring(0, paren);
		}
		s = s.Replace("\t", " ");
		while (s.IndexOf("  ") >= 0)
		{
			s = s.Replace("  ", " ");
		}
		return s.Trim();
	}

	private static string StripPrompt(string raw)
	{
		if (raw == null)
		{
			return "";
		}
		string line = raw.Trim();
		while (line.StartsWith("->", StringComparison.Ordinal))
		{
			line = line.Substring(2).Trim();
		}
		return line;
	}

	private static int ParseIntProp(string text, string key)
	{
		string[] array = text.Replace("\r\n", "\n").Split('\n');
		foreach (string raw in array)
		{
			string line = raw.Trim();
			if (line.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase) && int.TryParse(line.Substring(key.Length + 1).Trim(), out var v))
			{
				return v;
			}
		}
		return -1;
	}

	private static string ParseVersion(string text)
	{
		string[] array = text.Replace("\r\n", "\n").Split('\n');
		foreach (string raw in array)
		{
			string line = raw.Trim();
			if (line.StartsWith("Insyde SMASH", StringComparison.OrdinalIgnoreCase) || line.StartsWith("SMASH CLP", StringComparison.OrdinalIgnoreCase))
			{
				return line;
			}
		}
		return "";
	}

	public static List<Hw.Sensor> ToHwSensors(List<BmcSensor> src, out int skippedFans)
	{
		List<Hw.Sensor> result = new List<Hw.Sensor>();
		skippedFans = 0;
		foreach (BmcSensor s in src)
		{
			if (s.Discrete)
			{
				if (s.Kind == "discrete" && s.State.Length > 0)
				{
					Hw.Sensor d = new Hw.Sensor();
					d.Kind = "other";
					d.Name = s.Name;
					d.Text = s.State;
					d.Value = -1L;
					d.Bad = s.State.IndexOf("Non-recoverable", StringComparison.OrdinalIgnoreCase) < 0 && s.State.IndexOf("Critical", StringComparison.OrdinalIgnoreCase) >= 0;
					result.Add(d);
				}
			}
			else if (s.Kind == "temp")
			{
				Hw.Sensor x = new Hw.Sensor();
				x.Kind = "temp";
				x.Name = s.Name;
				x.Value = (long)Math.Round(s.Value);
				result.Add(x);
			}
			else if (s.Kind == "fan")
			{
				if (s.Value <= 0.0)
				{
					skippedFans++;
					continue;
				}
				Hw.Sensor x2 = new Hw.Sensor();
				x2.Kind = "fan";
				x2.Name = s.Name;
				x2.Value = (long)Math.Round(s.Value);
				result.Add(x2);
			}
			else if (s.Kind == "voltage")
			{
				Hw.Sensor x3 = new Hw.Sensor();
				x3.Kind = "other";
				x3.Name = s.Name;
				x3.Value = (long)Math.Round(s.Value * 1000.0);
				x3.Text = s.Value.ToString("0.###", CultureInfo.InvariantCulture) + " В";
				result.Add(x3);
			}
		}
		return result;
	}
}
