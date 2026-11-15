using System.Collections.Generic;

namespace BackupChecker.Core;

public class Source
{
	public string Id = "";

	public string Type = "";

	public bool Enabled = true;

	public int IntervalMinutes;

	public Dictionary<string, object> Raw = new Dictionary<string, object>();

	public string GetStr(string key, string dflt)
	{
		return Json.GetStr(Raw, key, dflt);
	}

	public int GetInt(string key, int dflt)
	{
		return Json.Int(Json.Get(Raw, key), dflt);
	}

	public long GetLong(string key, long dflt)
	{
		return Json.Long(Json.Get(Raw, key), dflt);
	}

	public bool GetBool(string key, bool dflt)
	{
		return Json.Bool(Json.Get(Raw, key), dflt);
	}

	public List<object> GetArray(string key)
	{
		return Json.AsArray(Json.Get(Raw, key));
	}

	public static Source FromJson(Dictionary<string, object> d)
	{
		if (d == null || d.Count == 0)
		{
			return null;
		}
		Source s = new Source();
		s.Raw = d;
		s.Type = Json.GetStr(d, "type", "");
		s.Id = Json.GetStr(d, "id", s.Type);
		s.Enabled = Json.Bool(Json.Get(d, "enabled"), dflt: true);
		s.IntervalMinutes = Json.Int(Json.Get(d, "intervalMinutes"), 0);
		if (string.IsNullOrEmpty(s.Type))
		{
			return null;
		}
		return s;
	}
}
