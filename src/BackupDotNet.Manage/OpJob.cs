using System;
using System.Collections.Generic;
using BackupDotNet.Core;

namespace BackupDotNet.Manage;

public class OpJob
{
	public string Id = "";

	public string Op = "";

	public string OpTitle = "";

	public string Target = "";

	public string Risk = "low";

	public Dictionary<string, string> Params = new Dictionary<string, string>();

	public string Status = "pending";

	public string RequestedBy = "";

	public string ApprovedBy = "";

	public DateTime RequestedAt = DateTime.Now;

	public DateTime? FinishedAt;

	public long DurationMs;

	public string Output = "";

	public string Error = "";

	public string Detail = "";

	internal readonly object Sync = new object();

	public Dictionary<string, object> ToJson()
	{
		lock (Sync)
		{
			Dictionary<string, object> d = Json.Obj();
			d["id"] = Id;
			d["op"] = Op;
			d["opTitle"] = OpTitle;
			d["target"] = Target;
			d["risk"] = Risk;
			d["status"] = Status;
			d["requestedBy"] = RequestedBy;
			d["approvedBy"] = ApprovedBy;
			d["requestedAt"] = RequestedAt;
			if (FinishedAt.HasValue)
			{
				d["finishedAt"] = FinishedAt.Value;
			}
			d["durationMs"] = DurationMs;
			if (Output.Length > 0)
			{
				d["output"] = Output;
			}
			if (Error.Length > 0)
			{
				d["error"] = Error;
			}
			if (Detail.Length > 0)
			{
				d["detail"] = Detail;
			}
			Dictionary<string, object> ps = Json.Obj();
			foreach (KeyValuePair<string, string> kv in Params)
			{
				ps[kv.Key] = kv.Value;
			}
			d["params"] = ps;
			return d;
		}
	}
}
