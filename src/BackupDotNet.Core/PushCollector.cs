using System;
using System.Collections.Generic;

namespace BackupDotNet.Core;

public class PushCollector : ICollector
{
	public string Type => "push";

	public List<CollectResult> Collect(Company company, Source source, AgentCtx ctx)
	{
		List<CollectResult> list = new List<CollectResult>();
		string job = source.GetStr("jobName", "Push");
		int staleMinutes = source.GetInt("staleMinutes", 15);
		if (staleMinutes <= 0)
		{
			staleMinutes = 15;
		}
		CollectResult r = new CollectResult();
		r.CompanyId = company.Id;
		r.CompanyName = company.Name;
		r.SourceId = source.Id;
		r.Kind = "push";
		r.Job = job;
		r.IsUptime = true;
		r.Reachable = true;
		r.Target = "push://" + company.Id + "/" + job;
		JobState prev = ctx.Store.GetState(company.Id, job);
		if (prev == null || !prev.LastAttempt.HasValue)
		{
			r.Fail("ещё не было ни одного пинга");
			list.Add(r);
			return list;
		}
		r.LastAttempt = prev.LastAttempt;
		r.LastSuccess = prev.LastSuccess;
		double minutes = (DateTime.UtcNow - PolicyEngine.ToUtc(prev.LastAttempt.Value)).TotalMinutes;
		if (minutes > (double)staleMinutes)
		{
			r.Fail($"нет пинга {minutes:0.#} мин (порог {staleMinutes} мин)");
		}
		else
		{
			r.Message = $"последний пинг {minutes:0.#} мин назад";
		}
		list.Add(r);
		return list;
	}
}
