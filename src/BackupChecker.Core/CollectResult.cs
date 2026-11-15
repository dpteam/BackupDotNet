using System;
using System.Collections.Generic;

namespace BackupChecker.Core;

public class CollectResult
{
	public string CompanyId = "";

	public string CompanyName = "";

	public string SourceId = "";

	public string Job = "";

	public string Kind = "";

	public DateTime? LastSuccess;

	public DateTime? LastAttempt;

	public long SizeBytes;

	public long DurationSeconds;

	public string Target = "";

	public string Message = "";

	public bool Reachable = true;

	public int SessionFailures;

	public long ResponseMs;

	public bool IsUptime;

	public bool IsHealth;

	public bool NoData;

	public Dictionary<string, object> Details = new Dictionary<string, object>();

	public bool Slow;

	public List<string> Errors = new List<string>();

	public void Fail(string msg)
	{
		Errors.Add(msg);
		Message = msg;
	}

	public Dictionary<string, object> ToJson()
	{
		Dictionary<string, object> d = Json.Obj();
		d["job"] = Job;
		d["kind"] = Kind;
		d["source"] = SourceId;
		d["target"] = Target;
		d["message"] = Message;
		d["reachable"] = Reachable;
		d["slow"] = Slow;
		d["noData"] = NoData;
		d["isHealth"] = IsHealth;
		d["isUptime"] = IsUptime;
		d["sessionFailures"] = SessionFailures;
		d["responseMs"] = ResponseMs;
		d["size"] = SizeBytes;
		d["duration"] = DurationSeconds;
		if (LastSuccess.HasValue)
		{
			d["lastSuccess"] = LastSuccess.Value;
		}
		if (LastAttempt.HasValue)
		{
			d["lastAttempt"] = LastAttempt.Value;
		}
		if (Errors.Count > 0)
		{
			d["errors"] = ToObjectList(Errors);
		}
		if (Details.Count > 0)
		{
			d["details"] = Details;
		}
		return d;
	}

	public static CollectResult FromJson(Dictionary<string, object> d)
	{
		CollectResult r = new CollectResult();
		r.Job = Json.GetStr(d, "job", "");
		r.Kind = Json.GetStr(d, "kind", "");
		r.SourceId = Json.GetStr(d, "source", "");
		r.Target = Json.GetStr(d, "target", "");
		r.Message = Json.GetStr(d, "message", "");
		r.Reachable = Json.Bool(Json.Get(d, "reachable"), dflt: true);
		r.Slow = Json.Bool(Json.Get(d, "slow"));
		r.NoData = Json.Bool(Json.Get(d, "noData"));
		r.IsHealth = Json.Bool(Json.Get(d, "isHealth"));
		r.IsUptime = Json.Bool(Json.Get(d, "isUptime"));
		r.SessionFailures = Json.Int(Json.Get(d, "sessionFailures"));
		r.ResponseMs = Json.Long(Json.Get(d, "responseMs"));
		r.SizeBytes = Json.Long(Json.Get(d, "size"));
		r.DurationSeconds = Json.Long(Json.Get(d, "duration"));
		r.LastSuccess = BackupRun.ParseDateNullable(Json.Get(d, "lastSuccess"));
		r.LastAttempt = BackupRun.ParseDateNullable(Json.Get(d, "lastAttempt"));
		r.Details = Json.AsObject(Json.Get(d, "details"));
		foreach (object o in Json.AsArray(Json.Get(d, "errors")))
		{
			string s = Json.Str(o);
			if (s.Length > 0)
			{
				r.Errors.Add(s);
			}
		}
		return r;
	}

	private static List<object> ToObjectList(List<string> src)
	{
		List<object> list = new List<object>();
		foreach (string s in src)
		{
			list.Add(s);
		}
		return list;
	}
}
