using System.Collections.Generic;

namespace BackupDotNet.Core;

public class ZabbixTarget
{
	public bool Enabled = true;

	public string Host = "";

	public string DisplayName = "";

	public string Server = "";

	public int Port = 10051;

	public string Mode = "";

	public Dictionary<string, string> Macros = new Dictionary<string, string>();

	public static ZabbixTarget FromJson(Dictionary<string, object> d)
	{
		ZabbixTarget z = new ZabbixTarget();
		if (d == null || d.Count == 0)
		{
			return z;
		}
		z.Enabled = Json.Bool(Json.Get(d, "enabled"), dflt: true);
		z.Host = Json.GetStr(d, "host", "");
		z.DisplayName = Json.GetStr(d, "displayName", "");
		z.Server = Json.GetStr(d, "server", "");
		z.Port = Json.Int(Json.Get(d, "port"), 10051);
		z.Mode = Json.GetStr(d, "mode", "");
		foreach (KeyValuePair<string, object> kv in Json.AsObject(Json.Get(d, "macros")))
		{
			z.Macros[kv.Key] = Json.Str(kv.Value);
		}
		return z;
	}
}
