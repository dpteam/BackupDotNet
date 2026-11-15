using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace BackupDotNet.Core;

public class DnsCollector : UptimeCollectorBase
{
	public override string Type => "dns";

	public override List<CollectResult> Collect(Company company, Source source, AgentCtx ctx)
	{
		List<CollectResult> list = new List<CollectResult>();
		string name = source.GetStr("name", "").Trim();
		string recordType = source.GetStr("recordType", "A").ToUpperInvariant();
		string job = UptimeCollectorBase.JobName(source, (name.Length > 0) ? name : "DNS");
		CollectResult r = Make(company, source, job);
		r.Target = "dns://" + name + "/" + recordType;
		if (string.IsNullOrEmpty(name))
		{
			r.Reachable = false;
			r.Fail("не задано имя");
			list.Add(r);
			return list;
		}
		string expected = source.GetStr("expect", "").Trim();
		int timeoutSec = source.GetInt("timeoutSeconds", 5);
		if (timeoutSec <= 0)
		{
			timeoutSec = 5;
		}
		Stopwatch sw = Stopwatch.StartNew();
		List<string> answers = new List<string>();
		string error = "";
		try
		{
			if (recordType == "A" || recordType == "AAAA")
			{
				IPHostEntry entry = Dns.GetHostEntry(name);
				IPAddress[] addressList = entry.AddressList;
				foreach (IPAddress ip in addressList)
				{
					bool isV6 = ip.AddressFamily == AddressFamily.InterNetworkV6;
					if ((!(recordType == "A") || !isV6) && (!(recordType == "AAAA") || isV6))
					{
						answers.Add(ip.ToString());
					}
				}
				if (answers.Count == 0 && recordType == "A")
				{
					IPAddress[] addressList2 = entry.AddressList;
					foreach (IPAddress ip2 in addressList2)
					{
						if (ip2.AddressFamily == AddressFamily.InterNetwork)
						{
							answers.Add(ip2.ToString());
						}
					}
				}
			}
			else
			{
				answers = ResolveViaNslookup(name, recordType, timeoutSec);
			}
		}
		catch (Exception ex)
		{
			error = ex.Message;
		}
		sw.Stop();
		long elapsed = sw.ElapsedMilliseconds;
		if (answers.Count == 0)
		{
			r.Reachable = false;
			r.ResponseMs = elapsed;
			r.Fail((error.Length > 0) ? error : "имя не разрешается");
			list.Add(r);
			return list;
		}
		r.LastSuccess = DateTime.UtcNow;
		r.LastAttempt = DateTime.UtcNow;
		r.ResponseMs = elapsed;
		if (expected.Length > 0)
		{
			bool match = false;
			foreach (string a in answers)
			{
				if (string.Equals(a, expected, StringComparison.OrdinalIgnoreCase))
				{
					match = true;
					break;
				}
			}
			if (!match)
			{
				r.Fail("ответ «" + string.Join(", ", answers.ToArray()) + "» не совпадает с ожидаемым «" + expected + "»");
				list.Add(r);
				return list;
			}
		}
		UptimeCollectorBase.ApplyLatency(r, elapsed, source);
		if (string.IsNullOrEmpty(r.Message))
		{
			r.Message = string.Join(", ", answers.ToArray()) + " за " + elapsed + " мс";
		}
		list.Add(r);
		return list;
	}

	private static List<string> ResolveViaNslookup(string name, string recordType, int timeoutSec)
	{
		List<string> result = new List<string>();
		try
		{
			ProcessStartInfo psi = new ProcessStartInfo("nslookup");
			psi.Arguments = "-type=" + recordType + " " + name;
			psi.RedirectStandardOutput = true;
			psi.RedirectStandardError = true;
			psi.UseShellExecute = false;
			psi.CreateNoWindow = true;
			using Process p = Process.Start(psi);
			if (p == null)
			{
				return result;
			}
			string output = p.StandardOutput.ReadToEnd();
			if (!p.WaitForExit(timeoutSec * 1000))
			{
				try
				{
					p.Kill();
				}
				catch
				{
				}
				return result;
			}
			bool afterName = false;
			string[] array = output.Replace("\r\n", "\n").Split('\n');
			foreach (string raw in array)
			{
				string line = raw.Trim();
				if (line.Length == 0)
				{
					continue;
				}
				if (line.StartsWith("Name:") || line.StartsWith("Имя:"))
				{
					afterName = true;
				}
				else if (afterName)
				{
					string[] parts = line.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
					if (parts.Length != 0 && IPAddress.TryParse(parts[parts.Length - 1], out var ip))
					{
						result.Add(ip.ToString());
					}
				}
			}
		}
		catch
		{
		}
		return result;
	}
}
