using System.Collections.Generic;
using BackupChecker.Core;

namespace BackupChecker.Manage;

public class OpParam
{
	public string Name = "";

	public string Label = "";

	public string Type = "text";

	public bool Required = true;

	public string Default = "";

	public string Hint = "";

	public List<string> Choices = new List<string>();

	public Dictionary<string, object> ToJson()
	{
		Dictionary<string, object> d = Json.Obj();
		d["name"] = Name;
		d["label"] = Label;
		d["type"] = Type;
		d["required"] = Required;
		d["default"] = Default;
		if (Hint.Length > 0)
		{
			d["hint"] = Hint;
		}
		if (Choices.Count > 0)
		{
			List<object> arr = new List<object>();
			foreach (string c in Choices)
			{
				arr.Add(c);
			}
			d["choices"] = arr;
		}
		return d;
	}
}
