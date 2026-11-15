using System;
using System.Collections.Generic;
using System.Globalization;

namespace BackupChecker.Core;

public class BackupRun
{
	public DateTime Timestamp = DateTime.Now;

	public string CompanyId = "";

	public string CompanyName = "";

	public string Job = "";

	public string Kind = "";

	public string SourceId = "";

	public BackupStatus Status = BackupStatus.NoData;

	public DateTime? LastSuccess;

	public DateTime? LastAttempt;

	public long SizeBytes;

	public long DurationSeconds;

	public string Target = "";

	public string Message = "";

	public int ConsecutiveFailures;

	public long ResponseMs;

	public bool IsUptime;

	public List<Check> Checks = new List<Check>();

	public Dictionary<string, object> Details = new Dictionary<string, object>();

	public Dictionary<string, object> ToJson()
	{
		Dictionary<string, object> d = Json.Obj();
		d["ts"] = Timestamp;
		d["company"] = CompanyId;
		d["companyName"] = CompanyName;
		d["job"] = Job;
		d["kind"] = Kind;
		d["source"] = SourceId;
		d["status"] = StatusNames.Of(Status);
		d["lastSuccess"] = (LastSuccess.HasValue ? ((object)LastSuccess.Value) : null);
		d["lastAttempt"] = (LastAttempt.HasValue ? ((object)LastAttempt.Value) : null);
		d["size"] = SizeBytes;
		d["duration"] = DurationSeconds;
		d["target"] = Target;
		d["message"] = Message;
		d["failures"] = ConsecutiveFailures;
		if (ResponseMs > 0)
		{
			d["responseMs"] = ResponseMs;
		}
		if (IsUptime)
		{
			d["uptime"] = true;
		}
		List<object> checks = new List<object>();
		foreach (Check c in Checks)
		{
			Dictionary<string, object> cd = Json.Obj();
			cd["name"] = c.Name;
			cd["ok"] = c.Passed;
			cd["detail"] = c.Detail;
			checks.Add(cd);
		}
		d["checks"] = checks;
		if (Details.Count > 0)
		{
			d["details"] = Details;
		}
		return d;
	}

	public static BackupRun FromJson(Dictionary<string, object> d)
	{
		BackupRun r = new BackupRun();
		r.Timestamp = ParseDate(Json.Get(d, "ts"));
		r.CompanyId = Json.GetStr(d, "company", "");
		r.CompanyName = Json.GetStr(d, "companyName", "");
		r.Job = Json.GetStr(d, "job", "");
		r.Kind = Json.GetStr(d, "kind", "");
		r.SourceId = Json.GetStr(d, "source", "");
		r.Status = StatusNames.Parse(Json.GetStr(d, "status", "NODATA"));
		DateTime? ls = ParseDateNullable(Json.Get(d, "lastSuccess"));
		DateTime? la = ParseDateNullable(Json.Get(d, "lastAttempt"));
		r.LastSuccess = ls;
		r.LastAttempt = la;
		r.SizeBytes = Json.Long(Json.Get(d, "size"));
		r.DurationSeconds = Json.Long(Json.Get(d, "duration"));
		r.Target = Json.GetStr(d, "target", "");
		r.Message = Json.GetStr(d, "message", "");
		r.ConsecutiveFailures = Json.Int(Json.Get(d, "failures"));
		r.ResponseMs = Json.Long(Json.Get(d, "responseMs"));
		r.IsUptime = Json.Bool(Json.Get(d, "uptime"));
		foreach (object o in Json.AsArray(Json.Get(d, "checks")))
		{
			Dictionary<string, object> cd = Json.AsObject(o);
			Check c = new Check();
			c.Name = Json.GetStr(cd, "name", "");
			c.Passed = Json.Bool(Json.Get(cd, "ok"), dflt: true);
			c.Detail = Json.GetStr(cd, "detail", "");
			r.Checks.Add(c);
		}
		return r;
	}

	public static DateTime ParseDate(object v)
	{
		DateTime? d = ParseDateNullable(v);
		if (!d.HasValue)
		{
			return DateTime.MinValue;
		}
		return d.Value;
	}

	public static DateTime? ParseDateNullable(object v)
	{
		if (v == null)
		{
			return null;
		}
		if (v is string s)
		{
			if (s.Length == 0)
			{
				return null;
			}
			if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var r))
			{
				return r;
			}
			if (DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out r))
			{
				return r;
			}
			return null;
		}
		if (v is DateTime)
		{
			return (DateTime)v;
		}
		return null;
	}
}
