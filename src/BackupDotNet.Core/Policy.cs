using System.Collections.Generic;

namespace BackupDotNet.Core;

public class Policy
{
	public int FreshnessHours = 26;

	public int FreshnessHoursWeekly = 192;

	public long MinSizeBytes;

	public int ConsecutiveFailuresToFail = 2;

	public int MaxSizeDropPercent;

	public bool RequireOffsite;

	public string SilenceFrom = "";

	public string SilenceTo = "";

	public static Policy FromJson(Dictionary<string, object> p)
	{
		Policy pol = new Policy();
		if (p == null || p.Count == 0)
		{
			return pol;
		}
		pol.FreshnessHours = Json.Int(Json.Get(p, "freshnessHours"), pol.FreshnessHours);
		pol.FreshnessHoursWeekly = Json.Int(Json.Get(p, "freshnessHoursWeekly"), pol.FreshnessHoursWeekly);
		long minMb = Json.Long(Json.Get(p, "minSizeMb"), 0L);
		pol.MinSizeBytes = minMb * 1024 * 1024;
		pol.ConsecutiveFailuresToFail = Json.Int(Json.Get(p, "consecutiveFailuresToFail"), pol.ConsecutiveFailuresToFail);
		pol.MaxSizeDropPercent = Json.Int(Json.Get(p, "maxSizeDropPercent"), pol.MaxSizeDropPercent);
		pol.RequireOffsite = Json.Bool(Json.Get(p, "requireOffsite"), pol.RequireOffsite);
		Dictionary<string, object> sw = Json.AsObject(Json.Get(p, "silenceWindow"));
		pol.SilenceFrom = Json.GetStr(sw, "from", "");
		pol.SilenceTo = Json.GetStr(sw, "to", "");
		return pol;
	}
}
