namespace BackupChecker.Core;

public class ZabbixValue
{
	public string Host = "";

	public string Key = "";

	public string Value = "";

	public long Clock;

	public ZabbixValue()
	{
	}

	public ZabbixValue(string host, string key, string value, long clock)
	{
		Host = host;
		Key = key;
		Value = value;
		Clock = clock;
	}
}
