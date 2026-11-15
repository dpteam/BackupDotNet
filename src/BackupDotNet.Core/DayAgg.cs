using System.Collections.Generic;

namespace BackupDotNet.Core;

public class DayAgg
{
	public int Ok;

	public int Warn;

	public int Fail;

	public int NoData;

	public int Skip;

	public BackupStatus Worst = BackupStatus.Skip;

	public void Add(BackupStatus s)
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
		case BackupStatus.Skip:
			Skip++;
			break;
		default:
			NoData++;
			break;
		}
		Worst = StatusNames.Worst(Worst, s);
	}

	public Dictionary<string, object> ToJson()
	{
		Dictionary<string, object> d = Json.Obj();
		d["ok"] = Ok;
		d["warn"] = Warn;
		d["fail"] = Fail;
		d["nodata"] = NoData;
		d["skip"] = Skip;
		d["worst"] = StatusNames.Of(Worst);
		return d;
	}

	public static DayAgg FromJson(Dictionary<string, object> d)
	{
		DayAgg a = new DayAgg();
		a.Ok = Json.Int(Json.Get(d, "ok"));
		a.Warn = Json.Int(Json.Get(d, "warn"));
		a.Fail = Json.Int(Json.Get(d, "fail"));
		a.NoData = Json.Int(Json.Get(d, "nodata"));
		a.Skip = Json.Int(Json.Get(d, "skip"));
		a.Worst = StatusNames.Parse(Json.GetStr(d, "worst", "SKIP"));
		return a;
	}
}
