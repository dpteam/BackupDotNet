using System;
using System.Collections.Generic;

namespace BackupDotNet.Core;

public static class PolicyEngine
{
	public static BackupRun Evaluate(Company company, Source source, CollectResult cr, JobState prev)
	{
		Policy p = company.Policy;
		BackupRun run = new BackupRun();
		run.Timestamp = DateTime.Now;
		run.CompanyId = company.Id;
		run.CompanyName = company.Name;
		run.Job = cr.Job;
		run.Kind = cr.Kind;
		run.SourceId = cr.SourceId;
		run.LastSuccess = cr.LastSuccess;
		run.LastAttempt = cr.LastAttempt;
		run.SizeBytes = cr.SizeBytes;
		run.DurationSeconds = cr.DurationSeconds;
		run.Target = cr.Target;
		run.Message = cr.Message;
		run.Details = cr.Details;
		int prevFailures = prev?.ConsecutiveFailures ?? 0;
		int failCount = ((cr.SessionFailures > 0) ? cr.SessionFailures : (prevFailures + 1));
		List<Check> checks = run.Checks;
		if (cr.IsUptime)
		{
			run.ResponseMs = cr.ResponseMs;
			run.IsUptime = true;
			if (!cr.Reachable)
			{
				checks.Add(new Check("reachable", passed: false, cr.Message));
				run.Status = BackupStatus.Fail;
				run.ConsecutiveFailures = prevFailures + 1;
				run.Message = ((cr.Message.Length > 0) ? cr.Message : "недоступен");
				return run;
			}
			checks.Add(new Check("reachable", passed: true, ""));
			if (cr.Errors.Count > 0)
			{
				checks.Add(new Check("response", passed: false, cr.Errors[0]));
				run.Status = BackupStatus.Fail;
				run.ConsecutiveFailures = prevFailures + 1;
				run.Message = cr.Errors[0];
				return run;
			}
			if (cr.Slow)
			{
				checks.Add(new Check("latency", passed: false, cr.Message));
				run.Status = BackupStatus.Warn;
				run.ConsecutiveFailures = 0;
				run.Message = cr.Message;
				return run;
			}
			checks.Add(new Check("latency", passed: true, cr.Message));
			run.Status = BackupStatus.Ok;
			run.ConsecutiveFailures = 0;
			run.Message = cr.Message;
			return run;
		}
		if (cr.IsHealth)
		{
			if (!cr.Reachable)
			{
				checks.Add(new Check("reachable", passed: false, cr.Message));
				run.Status = BackupStatus.NoData;
				run.ConsecutiveFailures = prevFailures + 1;
				run.Message = ((cr.Message.Length > 0) ? cr.Message : "недоступен");
				return run;
			}
			checks.Add(new Check("reachable", passed: true, ""));
			if (cr.Errors.Count > 0)
			{
				checks.Add(new Check("health", passed: false, cr.Errors[0]));
				run.Status = BackupStatus.Fail;
				run.ConsecutiveFailures = prevFailures + 1;
				run.Message = ((cr.Message.Length > 0) ? cr.Message : cr.Errors[0]);
				return run;
			}
			if (cr.NoData)
			{
				checks.Add(new Check("health", passed: false, cr.Message));
				run.Status = BackupStatus.NoData;
				run.ConsecutiveFailures = 0;
				run.Message = cr.Message;
				return run;
			}
			checks.Add(new Check("health", passed: true, cr.Message));
			if (cr.Slow)
			{
				run.Status = BackupStatus.Warn;
				run.ConsecutiveFailures = 0;
				run.Message = cr.Message;
				return run;
			}
			run.Status = BackupStatus.Ok;
			run.ConsecutiveFailures = 0;
			run.Message = cr.Message;
			return run;
		}
		if (!cr.Reachable)
		{
			checks.Add(new Check("reachable", passed: false, cr.Message));
			run.Status = BackupStatus.NoData;
			run.ConsecutiveFailures = prevFailures + 1;
			if (string.IsNullOrEmpty(run.Message))
			{
				run.Message = "источник недоступен";
			}
			return run;
		}
		checks.Add(new Check("reachable", passed: true, ""));
		if (!cr.LastSuccess.HasValue)
		{
			bool hasErrors = cr.Errors.Count > 0;
			run.Status = (hasErrors ? BackupStatus.Fail : BackupStatus.NoData);
			run.ConsecutiveFailures = failCount;
			checks.Add(new Check("last-success", passed: false, hasErrors ? "успешных бэкапов не найдено, есть ошибки" : "успешных бэкапов не найдено"));
			if (string.IsNullOrEmpty(run.Message))
			{
				run.Message = (hasErrors ? cr.Errors[0] : "нет успешных бэкапов");
			}
			return run;
		}
		DateTime lastUtc = ToUtc(cr.LastSuccess.Value);
		double ageHours = (DateTime.UtcNow - lastUtc).TotalHours;
		bool fresh = ageHours <= (double)p.FreshnessHours;
		checks.Add(new Check("freshness", fresh, $"{ageHours:0.#} ч назад (порог {p.FreshnessHours} ч)"));
		if (!fresh)
		{
			run.Status = BackupStatus.Fail;
			run.ConsecutiveFailures = failCount;
			string ageText = ((ageHours >= 48.0) ? $"{ageHours / 24.0:0} дн назад" : $"{ageHours:0.#} ч назад");
			if (string.IsNullOrEmpty(run.Message))
			{
				run.Message = "последний успешный бэкап " + ageText;
			}
			else
			{
				run.Message = run.Message + " | последний успех: " + ageText;
			}
			return run;
		}
		BackupStatus status = BackupStatus.Ok;
		string message = "";
		if (p.MinSizeBytes > 0 && cr.SizeBytes > 0)
		{
			bool sizeOk = cr.SizeBytes >= p.MinSizeBytes;
			checks.Add(new Check("min-size", sizeOk, $"{Fmt.Size(cr.SizeBytes)} < минимума {Fmt.Size(p.MinSizeBytes)}"));
			if (!sizeOk)
			{
				status = BackupStatus.Warn;
				message = "размер копии меньше минимума";
			}
		}
		if (p.MaxSizeDropPercent > 0 && prev != null && prev.SizeBytes > 0 && cr.SizeBytes > 0)
		{
			double drop = (double)(prev.SizeBytes - cr.SizeBytes) * 100.0 / (double)prev.SizeBytes;
			bool dropOk = drop <= (double)p.MaxSizeDropPercent;
			checks.Add(new Check("size-drop", dropOk, $"изменение {0.0 - drop:0.#}% (порог {p.MaxSizeDropPercent}%)"));
			if (!dropOk && status == BackupStatus.Ok)
			{
				status = BackupStatus.Warn;
				message = $"размер упал на {drop:0.#}%";
			}
		}
		if (cr.Errors.Count > 0)
		{
			checks.Add(new Check("source-errors", passed: false, cr.Errors[0]));
			if (status == BackupStatus.Ok)
			{
				status = BackupStatus.Warn;
				message = cr.Errors[0];
			}
		}
		int failures = 0;
		failures = status switch
		{
			BackupStatus.Fail => prevFailures + 1, 
			BackupStatus.Warn => 0, 
			_ => 0, 
		};
		if (cr.SessionFailures > 0)
		{
			failures = cr.SessionFailures;
		}
		if (status != BackupStatus.Fail && failures >= p.ConsecutiveFailuresToFail)
		{
			status = BackupStatus.Fail;
		}
		run.Status = status;
		run.ConsecutiveFailures = failures;
		run.Message = message;
		if (run.Message.Length == 0 && cr.Message.Length > 0 && status == BackupStatus.Ok)
		{
			run.Message = cr.Message;
		}
		return run;
	}

	public static DateTime ToUtc(DateTime dt)
	{
		if (dt.Kind == DateTimeKind.Utc)
		{
			return dt;
		}
		if (dt.Kind == DateTimeKind.Local)
		{
			return dt.ToUniversalTime();
		}
		return DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime();
	}
}
