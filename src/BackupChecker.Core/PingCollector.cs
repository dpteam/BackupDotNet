using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace BackupChecker.Core;

public class PingCollector : UptimeCollectorBase
{
	public override string Type => "ping";

	public override List<CollectResult> Collect(Company company, Source source, AgentCtx ctx)
	{
		List<CollectResult> list = new List<CollectResult>();
		string host = source.GetStr("host", "").Trim();
		string job = UptimeCollectorBase.JobName(source, (host.Length > 0) ? host : "Ping");
		CollectResult r = Make(company, source, job);
		r.Target = "icmp://" + host;
		if (string.IsNullOrEmpty(host))
		{
			r.Reachable = false;
			r.Fail("не задан host");
			list.Add(r);
			return list;
		}
		int timeoutMs = source.GetInt("timeoutMs", 3000);
		if (timeoutMs <= 0)
		{
			timeoutMs = 3000;
		}
		int attempts = source.GetInt("attempts", 3);
		if (attempts <= 0)
		{
			attempts = 3;
		}
		int success = 0;
		long totalMs = 0L;
		string lastError = "";
		for (int i = 0; i < attempts; i++)
		{
			try
			{
				using Ping p = new Ping();
				PingReply reply = p.Send(host, timeoutMs);
				if (reply != null && reply.Status == IPStatus.Success)
				{
					success++;
					totalMs += reply.RoundtripTime;
				}
				else if (reply != null)
				{
					lastError = reply.Status.ToString();
				}
			}
			catch (Exception ex)
			{
				lastError = ex.Message;
			}
		}
		if (success == 0)
		{
			r.Reachable = false;
			r.ResponseMs = timeoutMs;
			r.Fail("нет ответа (" + attempts + " попыток)" + ((lastError.Length > 0) ? (": " + lastError) : ""));
			list.Add(r);
			return list;
		}
		long avg = totalMs / success;
		r.LastSuccess = DateTime.UtcNow;
		r.LastAttempt = DateTime.UtcNow;
		UptimeCollectorBase.ApplyLatency(r, avg, source);
		if (string.IsNullOrEmpty(r.Message))
		{
			r.Message = success + "/" + attempts + " ответов, " + avg + " мс";
		}
		if (success < attempts && !r.Slow)
		{
			object message = r.Message;
			r.Message = string.Concat(message, " (потери ", attempts - success, "/", attempts, ")");
		}
		list.Add(r);
		return list;
	}
}
