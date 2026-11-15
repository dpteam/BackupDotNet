using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BackupDotNet.Core;

public class MikroTikCollector : ICollector
{
	public class MtInfo
	{
		public string Version = "";

		public string Transport = "";

		public string BoardName = "";

		public string Architecture = "";

		public string Identity = "";

		public string Serial = "";

		public string Mac = "";

		public string IpAddress = "";

		public string BuildDate = "";

		public string Interface = "";

		public double UptimeMinutes = -1.0;

		public int CpuLoad = -1;

		public int CpuCount = -1;

		public long FreeMemoryMb = -1L;

		public long TotalMemoryMb = -1L;

		public int Temperature = -1;

		public bool Limited;

		public string License = "";

		public string Summary()
		{
			List<string> parts = new List<string>();
			if (Version.Length > 0)
			{
				parts.Add("RouterOS " + Version);
			}
			if (BoardName.Length > 0)
			{
				parts.Add(BoardName);
			}
			if (UptimeMinutes >= 0.0)
			{
				parts.Add("uptime " + FormatUptime(UptimeMinutes));
			}
			if (CpuLoad >= 0)
			{
				parts.Add("CPU " + CpuLoad + "%");
			}
			if (Limited)
			{
				parts.Add("только по MNDP");
			}
			if (parts.Count <= 0)
			{
				return "RouterOS";
			}
			return string.Join(", ", parts.ToArray());
		}
	}

	public string Type => "mikrotik";

	public List<CollectResult> Collect(Company company, Source source, AgentCtx ctx)
	{
		List<CollectResult> list = new List<CollectResult>();
		string job = source.GetStr("jobName", "MikroTik");
		string host = source.GetStr("host", "").Trim();
		int port = source.GetInt("port", 0);
		string user = source.GetStr("username", "");
		string transport = source.GetStr("transport", "auto").ToLowerInvariant();
		string minVersion = source.GetStr("minVersion", "");
		int cpuWarn = source.GetInt("cpuWarnPercent", 80);
		int tempWarn = source.GetInt("tempWarnC", 70);
		int uptimeWarnMin = source.GetInt("uptimeWarnMinutes", 60);
		string snmpCommunity = source.GetStr("snmpCommunity", "");
		int timeout = source.GetInt("timeoutSeconds", 10);
		string password = source.GetStr("password", "");
		if (password.Length == 0)
		{
			string key = source.GetStr("passwordSecret", "");
			if (key.Length > 0)
			{
				password = ctx.Config.Secret(key);
			}
		}
		CollectResult r = Make(company, source, job);
		r.Target = host;
		if (host.Length == 0)
		{
			r.Reachable = false;
			r.Message = "не задан адрес роутера";
			list.Add(r);
			return list;
		}
		MtInfo info = null;
		string lastError = "";
		MtErrorKind lastKind = MtErrorKind.Unknown;
		bool tryRest = transport == "auto" || transport == "rest";
		bool tryApi = transport == "auto" || transport == "api";
		bool trySnmp = (transport == "auto" && snmpCommunity.Length > 0) || transport == "snmp";
		bool tryMndp = transport == "auto" || transport == "mndp";
		if (info == null && tryRest)
		{
			int[] restPorts = ((port > 0) ? new int[1] { port } : new int[2] { 443, 80 });
			int[] array = restPorts;
			foreach (int p in array)
			{
				try
				{
					info = MtRest.Read(host, p, p == 443, user, password, timeout);
					if (info != null)
					{
						break;
					}
				}
				catch (MtException ex)
				{
					lastError = "REST " + p + ": " + ex.Message;
					lastKind = ex.Kind;
					Log.Debug("MikroTik REST " + host + ":" + p + " — " + ex.Message);
				}
				catch (Exception ex2)
				{
					lastError = "REST " + p + ": " + ex2.Message;
					lastKind = MtErrorKind.Unknown;
					Log.Debug("MikroTik REST " + host + ":" + p + " — " + ex2.Message);
				}
			}
		}
		if (info == null && tryApi)
		{
			int[] apiPorts = ((port > 0) ? new int[1] { port } : new int[2] { 8728, 8729 });
			int[] array2 = apiPorts;
			foreach (int p2 in array2)
			{
				try
				{
					info = MtApi.Read(host, p2, p2 == 8729, user, password, timeout);
					if (info != null)
					{
						break;
					}
				}
				catch (MtException ex3)
				{
					lastError = "API " + p2 + ": " + ex3.Message;
					lastKind = ex3.Kind;
					Log.Debug("MikroTik API " + host + ":" + p2 + " — " + ex3.Message);
				}
				catch (Exception ex4)
				{
					lastError = "API " + p2 + ": " + ex4.Message;
					lastKind = MtErrorKind.Unknown;
					Log.Debug("MikroTik API " + host + ":" + p2 + " — " + ex4.Message);
				}
			}
		}
		if (info == null && trySnmp && snmpCommunity.Length > 0)
		{
			try
			{
				info = MtSnmp.Read(host, (port > 0) ? port : 161, snmpCommunity, timeout);
			}
			catch (Exception ex5)
			{
				lastError = "SNMP: " + ex5.Message;
				lastKind = MtErrorKind.NoConnection;
				Log.Debug("MikroTik SNMP " + host + " — " + ex5.Message);
			}
		}
		if (info == null && tryMndp)
		{
			try
			{
				info = MtMndp.Read(host, timeout);
			}
			catch (Exception ex6)
			{
				lastError = "MNDP: " + ex6.Message;
				Log.Debug("MikroTik MNDP " + host + " — " + ex6.Message);
			}
		}
		if (info == null || string.IsNullOrEmpty(info.Version))
		{
			r.Reachable = false;
			r.Message = "RouterOS не ответил: " + DescribeFailure(transport, lastKind, lastError);
			list.Add(r);
			return list;
		}
		r.Reachable = true;
		r.Target = host + " (" + info.Transport + ")";
		r.Details = BuildDetails(info);
		List<string> problems = new List<string>();
		List<string> warnings = new List<string>();
		if (minVersion.Length > 0)
		{
			int cmp = CompareVersions(info.Version, minVersion);
			if (cmp < 0)
			{
				bool farBehind = IsFarBehind(info.Version, minVersion);
				string msg = "версия " + info.Version + " старше " + minVersion;
				if (farBehind)
				{
					problems.Add(msg + " (отставание на 2+ мажора)");
				}
				else
				{
					warnings.Add(msg);
				}
			}
		}
		if (uptimeWarnMin > 0 && info.UptimeMinutes >= 0.0 && info.UptimeMinutes < (double)uptimeWarnMin)
		{
			warnings.Add("uptime " + FormatUptime(info.UptimeMinutes) + " — возможно, перезагружается");
		}
		if (cpuWarn > 0 && info.CpuLoad > cpuWarn)
		{
			warnings.Add("CPU " + info.CpuLoad + "%");
		}
		if (tempWarn > 0 && info.Temperature > tempWarn)
		{
			warnings.Add("температура " + info.Temperature + " °C");
		}
		if (info.TotalMemoryMb > 0 && info.FreeMemoryMb >= 0)
		{
			double freePercent = (double)info.FreeMemoryMb * 100.0 / (double)info.TotalMemoryMb;
			if (freePercent < 10.0)
			{
				warnings.Add("свободной памяти " + (int)freePercent + "%");
			}
		}
		if (problems.Count > 0)
		{
			r.Fail(string.Join("; ", problems.ToArray()));
		}
		else if (warnings.Count > 0)
		{
			r.Message = Join(info.Summary(), string.Join("; ", warnings.ToArray()));
			r.Slow = true;
		}
		else
		{
			r.Message = info.Summary();
		}
		list.Add(r);
		return list;
	}

	private static CollectResult Make(Company c, Source s, string job)
	{
		CollectResult r = new CollectResult();
		r.CompanyId = c.Id;
		r.CompanyName = c.Name;
		r.SourceId = s.Id;
		r.Kind = "mikrotik";
		r.Job = job;
		r.IsHealth = true;
		r.Reachable = true;
		return r;
	}

	private static string DescribeFailure(string transport, MtErrorKind kind, string lastError)
	{
		string detail = ((lastError.Length > 0) ? (" [" + lastError + "]") : "");
		return kind switch
		{
			MtErrorKind.Auth => "сервис отвечает, но не пускает — проверьте логин и пароль" + detail, 
			MtErrorKind.NoConnection => "нет связи с роутером — проверьте адрес, порт и firewall" + detail, 
			MtErrorKind.ServiceDisabled => "сервис управления выключен на роутере — " + ServiceHint(transport) + detail, 
			_ => ServiceHint(transport) + detail, 
		};
	}

	private static string ServiceHint(string transport)
	{
		return transport switch
		{
			"rest" => "включите /ip service enable www-ssl (443) или www (80)", 
			"api" => "включите /ip service enable api (8728) или api-ssl (8729)", 
			"snmp" => "включите /snmp set enabled=yes и проверьте community", 
			_ => "включите REST (www-ssl), API (api) или SNMP на роутере, и разрешите наш IP в /ip service", 
		};
	}

	private static Dictionary<string, object> BuildDetails(MtInfo info)
	{
		Dictionary<string, object> d = Json.Obj();
		d["version"] = info.Version;
		d["transport"] = info.Transport;
		if (info.BoardName.Length > 0)
		{
			d["board"] = info.BoardName;
		}
		if (info.Architecture.Length > 0)
		{
			d["architecture"] = info.Architecture;
		}
		if (info.Identity.Length > 0)
		{
			d["identity"] = info.Identity;
		}
		if (info.Serial.Length > 0)
		{
			d["serial"] = info.Serial;
		}
		if (info.UptimeMinutes >= 0.0)
		{
			d["uptimeMinutes"] = Math.Round(info.UptimeMinutes, 0);
		}
		if (info.CpuLoad >= 0)
		{
			d["cpuLoad"] = info.CpuLoad;
		}
		if (info.CpuCount > 0)
		{
			d["cpuCount"] = info.CpuCount;
		}
		if (info.FreeMemoryMb >= 0)
		{
			d["freeMemoryMb"] = info.FreeMemoryMb;
		}
		if (info.TotalMemoryMb > 0)
		{
			d["totalMemoryMb"] = info.TotalMemoryMb;
		}
		if (info.Temperature > 0)
		{
			d["temperature"] = info.Temperature;
		}
		if (info.License.Length > 0)
		{
			d["license"] = info.License;
		}
		if (info.Mac.Length > 0)
		{
			d["mac"] = info.Mac;
		}
		if (info.BuildDate.Length > 0)
		{
			d["buildDate"] = info.BuildDate;
		}
		if (info.Interface.Length > 0)
		{
			d["interface"] = info.Interface;
		}
		if (info.Limited)
		{
			d["limited"] = true;
		}
		return d;
	}

	private static string Join(string a, string b)
	{
		if (a.Length == 0)
		{
			return b;
		}
		if (b.Length == 0)
		{
			return a;
		}
		return a + "; " + b;
	}

	public static int CompareVersions(string actual, string required)
	{
		int[] a = ParseVersion(actual);
		int[] b = ParseVersion(required);
		if (a.Length == 0 || b.Length == 0)
		{
			return 0;
		}
		int len = Math.Max(a.Length, b.Length);
		for (int i = 0; i < len; i++)
		{
			int x = ((i < a.Length) ? a[i] : 0);
			int y = ((i < b.Length) ? b[i] : 0);
			if (x < y)
			{
				return -1;
			}
			if (x > y)
			{
				return 1;
			}
		}
		return 0;
	}

	public static bool IsFarBehind(string actual, string required)
	{
		int[] a = ParseVersion(actual);
		int[] b = ParseVersion(required);
		if (a.Length == 0 || b.Length == 0)
		{
			return false;
		}
		return b[0] - a[0] >= 2;
	}

	private static int[] ParseVersion(string v)
	{
		if (string.IsNullOrEmpty(v))
		{
			return new int[0];
		}
		Match m = Regex.Match(v, "(\\d+)(?:\\.(\\d+))?(?:\\.(\\d+))?");
		if (!m.Success)
		{
			return new int[0];
		}
		List<int> parts = new List<int>();
		for (int i = 1; i <= 3 && m.Groups[i].Success; i++)
		{
			if (int.TryParse(m.Groups[i].Value, out var n))
			{
				parts.Add(n);
			}
		}
		return parts.ToArray();
	}

	public static string FormatUptime(double minutes)
	{
		if (minutes < 0.0)
		{
			return "—";
		}
		if (minutes < 60.0)
		{
			return (int)minutes + " мин";
		}
		if (minutes < 2880.0)
		{
			return (int)(minutes / 60.0) + " ч";
		}
		return (int)(minutes / 60.0 / 24.0) + " дн";
	}

	public static double ParseUptime(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return -1.0;
		}
		double total = 0.0;
		bool found = false;
		Match m = Regex.Match(s, "(\\d+)w", RegexOptions.IgnoreCase);
		if (m.Success)
		{
			total += (double)(int.Parse(m.Groups[1].Value) * 7 * 24 * 60);
			found = true;
		}
		m = Regex.Match(s, "(\\d+)d", RegexOptions.IgnoreCase);
		if (m.Success)
		{
			total += (double)(int.Parse(m.Groups[1].Value) * 24 * 60);
			found = true;
		}
		m = Regex.Match(s, "(\\d+)h", RegexOptions.IgnoreCase);
		if (m.Success)
		{
			total += (double)(int.Parse(m.Groups[1].Value) * 60);
			found = true;
		}
		m = Regex.Match(s, "(\\d+)m(?!s)", RegexOptions.IgnoreCase);
		if (m.Success)
		{
			total += (double)int.Parse(m.Groups[1].Value);
			found = true;
		}
		m = Regex.Match(s, "(\\d+)s", RegexOptions.IgnoreCase);
		if (m.Success)
		{
			total += (double)int.Parse(m.Groups[1].Value) / 60.0;
			found = true;
		}
		if (!found)
		{
			return -1.0;
		}
		return total;
	}
}
