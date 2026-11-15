namespace BackupChecker.Core;

public static class StatusNames
{
	public static string Of(BackupStatus s)
	{
		return s switch
		{
			BackupStatus.Ok => "OK", 
			BackupStatus.Warn => "WARN", 
			BackupStatus.Fail => "FAIL", 
			BackupStatus.NoData => "NODATA", 
			BackupStatus.Skip => "SKIP", 
			_ => "NODATA", 
		};
	}

	public static BackupStatus Parse(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return BackupStatus.NoData;
		}
		return s.Trim().ToUpperInvariant() switch
		{
			"OK" => BackupStatus.Ok, 
			"WARN" => BackupStatus.Warn, 
			"FAIL" => BackupStatus.Fail, 
			"SKIP" => BackupStatus.Skip, 
			_ => BackupStatus.NoData, 
		};
	}

	public static BackupStatus Worst(BackupStatus a, BackupStatus b)
	{
		int wa = Weight(a);
		int wb = Weight(b);
		if (wa < wb)
		{
			return b;
		}
		return a;
	}

	public static int Weight(BackupStatus s)
	{
		return s switch
		{
			BackupStatus.Fail => 40, 
			BackupStatus.NoData => 30, 
			BackupStatus.Warn => 20, 
			BackupStatus.Ok => 10, 
			BackupStatus.Skip => 0, 
			_ => 30, 
		};
	}
}
