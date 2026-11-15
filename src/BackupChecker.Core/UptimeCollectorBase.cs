using System.Collections.Generic;

namespace BackupChecker.Core;

public abstract class UptimeCollectorBase : ICollector
{
	public abstract string Type { get; }

	public abstract List<CollectResult> Collect(Company company, Source source, AgentCtx ctx);

	protected CollectResult Make(Company c, Source s, string job)
	{
		CollectResult r = new CollectResult();
		r.CompanyId = c.Id;
		r.CompanyName = c.Name;
		r.SourceId = s.Id;
		r.Kind = "uptime";
		r.Job = job;
		r.IsUptime = true;
		r.Reachable = true;
		return r;
	}

	protected static string JobName(Source s, string dflt)
	{
		string jn = s.GetStr("jobName", "").Trim();
		if (jn.Length <= 0)
		{
			return dflt;
		}
		return jn;
	}

	protected static void ApplyLatency(CollectResult r, long elapsedMs, Source s)
	{
		r.ResponseMs = elapsedMs;
		int warnMs = s.GetInt("slowWarnMs", 0);
		int failMs = s.GetInt("slowFailMs", 0);
		if (failMs > 0 && elapsedMs >= failMs)
		{
			r.Fail("слишком медленный ответ: " + elapsedMs + " мс (порог " + failMs + ")");
		}
		else if (warnMs > 0 && elapsedMs >= warnMs)
		{
			r.Message = "медленный ответ: " + elapsedMs + " мс";
			r.Slow = true;
		}
	}
}
