using System;
using System.Collections.Generic;

namespace BackupDotNet.Core;

public class HourAgg
{
	public int Ok;

	public int Warn;

	public int Fail;

	public long TotalMs;

	public int Counted;

	public long AvgMs
	{
		get
		{
			if (Counted <= 0)
			{
				return 0L;
			}
			return TotalMs / Counted;
		}
	}

	public BackupStatus Worst
	{
		get
		{
			if (Fail > 0)
			{
				return BackupStatus.Fail;
			}
			if (Warn > 0)
			{
				return BackupStatus.Warn;
			}
			if (Ok > 0)
			{
				return BackupStatus.Ok;
			}
			return BackupStatus.NoData;
		}
	}

	public int Total => Ok + Warn + Fail;

	public double Uptime
	{
		get
		{
			int total = Total;
			if (total == 0)
			{
				return 0.0;
			}
			return Math.Round((double)(Ok + Warn) * 100.0 / (double)total, 2);
		}
	}

	public void Add(BackupStatus s, long responseMs)
	{
		switch (s)
		{
		case BackupStatus.Ok:
			Ok++;
			break;
		case BackupStatus.Warn:
			Warn++;
			break;
		case BackupStatus.Fail:
			Fail++;
			break;
		default:
			Fail++;
			break;
		}
		if (responseMs > 0)
		{
			TotalMs += responseMs;
			Counted++;
		}
	}

	public Dictionary<string, object> ToJson()
	{
		Dictionary<string, object> d = Json.Obj();
		d["ok"] = Ok;
		d["warn"] = Warn;
		d["fail"] = Fail;
		d["avgMs"] = AvgMs;
		return d;
	}

	public static HourAgg FromJson(Dictionary<string, object> d)
	{
		HourAgg a = new HourAgg();
		a.Ok = Json.Int(Json.Get(d, "ok"));
		a.Warn = Json.Int(Json.Get(d, "warn"));
		a.Fail = Json.Int(Json.Get(d, "fail"));
		long avg = Json.Long(Json.Get(d, "avgMs"));
		int cnt = a.Ok + a.Warn + a.Fail;
		if (avg > 0 && cnt > 0)
		{
			a.TotalMs = avg * cnt;
			a.Counted = cnt;
		}
		return a;
	}
}
