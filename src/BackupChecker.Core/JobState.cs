using System;
using System.Collections.Generic;

namespace BackupChecker.Core;

public class JobState
{
	public string Key = "";

	public string CompanyId = "";

	public string CompanyName = "";

	public string Job = "";

	public string Kind = "";

	public BackupStatus Status = BackupStatus.NoData;

	public DateTime? LastSuccess;

	public DateTime? LastAttempt;

	public DateTime Updated = DateTime.Now;

	public long SizeBytes;

	public long DurationSeconds;

	public int ConsecutiveFailures;

	public string Message = "";

	public string Target = "";

	public long ResponseMs;

	public bool IsUptime;

	public Dictionary<string, object> Details = new Dictionary<string, object>();

	public Dictionary<string, object> ToJson()
	{
		Dictionary<string, object> d = Json.Obj();
		d["company"] = CompanyId;
		d["companyName"] = CompanyName;
		d["job"] = Job;
		d["kind"] = Kind;
		d["status"] = StatusNames.Of(Status);
		d["lastSuccess"] = (LastSuccess.HasValue ? ((object)LastSuccess.Value) : null);
		d["lastAttempt"] = (LastAttempt.HasValue ? ((object)LastAttempt.Value) : null);
		d["updated"] = Updated;
		d["size"] = SizeBytes;
		d["duration"] = DurationSeconds;
		d["failures"] = ConsecutiveFailures;
		d["message"] = Message;
		d["target"] = Target;
		if (ResponseMs > 0)
		{
			d["responseMs"] = ResponseMs;
		}
		if (IsUptime)
		{
			d["uptime"] = true;
		}
		if (Details.Count > 0)
		{
			d["details"] = Details;
		}
		return d;
	}

	public static JobState FromJson(string key, Dictionary<string, object> d)
	{
		JobState s = new JobState();
		s.Key = key;
		s.CompanyId = Json.GetStr(d, "company", "");
		s.CompanyName = Json.GetStr(d, "companyName", "");
		s.Job = Json.GetStr(d, "job", "");
		s.Kind = Json.GetStr(d, "kind", "");
		s.Status = StatusNames.Parse(Json.GetStr(d, "status", "NODATA"));
		s.LastSuccess = BackupRun.ParseDateNullable(Json.Get(d, "lastSuccess"));
		s.LastAttempt = BackupRun.ParseDateNullable(Json.Get(d, "lastAttempt"));
		DateTime upd = BackupRun.ParseDate(Json.Get(d, "updated"));
		s.Updated = ((upd == DateTime.MinValue) ? DateTime.Now : upd);
		s.SizeBytes = Json.Long(Json.Get(d, "size"));
		s.DurationSeconds = Json.Long(Json.Get(d, "duration"));
		s.ConsecutiveFailures = Json.Int(Json.Get(d, "failures"));
		s.Message = Json.GetStr(d, "message", "");
		s.Target = Json.GetStr(d, "target", "");
		s.ResponseMs = Json.Long(Json.Get(d, "responseMs"));
		s.IsUptime = Json.Bool(Json.Get(d, "uptime"));
		s.Details = Json.AsObject(Json.Get(d, "details"));
		return s;
	}

	public static string MakeKey(string companyId, string job)
	{
		return companyId + "|" + job;
	}
}
