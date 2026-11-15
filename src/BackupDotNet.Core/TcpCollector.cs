using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;

namespace BackupDotNet.Core;

public class TcpCollector : UptimeCollectorBase
{
	public override string Type => "tcp";

	public override List<CollectResult> Collect(Company company, Source source, AgentCtx ctx)
	{
		List<CollectResult> list = new List<CollectResult>();
		string host = source.GetStr("host", "").Trim();
		int port = source.GetInt("port", 0);
		string job = UptimeCollectorBase.JobName(source, host + ":" + port);
		CollectResult r = Make(company, source, job);
		r.Target = "tcp://" + host + ":" + port;
		if (string.IsNullOrEmpty(host) || port <= 0)
		{
			r.Reachable = false;
			r.Fail("не задан host или port");
			list.Add(r);
			return list;
		}
		int timeoutSec = source.GetInt("timeoutSeconds", 5);
		if (timeoutSec <= 0)
		{
			timeoutSec = 5;
		}
		Stopwatch sw = Stopwatch.StartNew();
		bool ok = false;
		string error = "";
		try
		{
			using TcpClient client = new TcpClient();
			IAsyncResult ar = client.BeginConnect(host, port, null, null);
			if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(timeoutSec)))
			{
				error = "таймаут " + timeoutSec + " с";
			}
			else
			{
				client.EndConnect(ar);
				ok = true;
			}
		}
		catch (Exception ex)
		{
			error = ex.Message;
		}
		sw.Stop();
		long elapsed = sw.ElapsedMilliseconds;
		if (!ok)
		{
			r.Reachable = false;
			r.ResponseMs = elapsed;
			r.Fail((error.Length > 0) ? error : "порт закрыт");
			list.Add(r);
			return list;
		}
		r.LastSuccess = DateTime.UtcNow;
		r.LastAttempt = DateTime.UtcNow;
		UptimeCollectorBase.ApplyLatency(r, elapsed, source);
		if (string.IsNullOrEmpty(r.Message))
		{
			r.Message = "порт открыт, " + elapsed + " мс";
		}
		list.Add(r);
		return list;
	}
}
