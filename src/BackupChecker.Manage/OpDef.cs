using System.Collections.Generic;
using BackupChecker.Core;

namespace BackupChecker.Manage;

public class OpDef
{
	public string Id = "";

	public string Title = "";

	public string Group = "";

	public string Risk = "low";

	public string Description = "";

	public string Transport = "winrm";

	public bool NeedsConfirmText;

	public string Rollback = "";

	public List<OpParam> Params = new List<OpParam>();

	public Dictionary<string, object> ToJson()
	{
		Dictionary<string, object> d = Json.Obj();
		d["id"] = Id;
		d["title"] = Title;
		d["group"] = Group;
		d["risk"] = Risk;
		d["description"] = Description;
		d["transport"] = Transport;
		d["needsConfirmText"] = NeedsConfirmText;
		if (Rollback.Length > 0)
		{
			d["rollback"] = Rollback;
		}
		List<object> ps = new List<object>();
		foreach (OpParam p in Params)
		{
			ps.Add(p.ToJson());
		}
		d["params"] = ps;
		return d;
	}
}
