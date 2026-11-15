using System;
using System.Collections.Generic;

namespace BackupChecker.Core;

public class Company
{
	public string Id = "";

	public string Name = "";

	public string ShortName = "";

	public List<string> Tags = new List<string>();

	public string Timezone = "Europe/Moscow";

	public bool Enabled = true;

	public List<Contact> Contacts = new List<Contact>();

	public List<Source> Sources = new List<Source>();

	public Policy Policy = new Policy();

	public ZabbixTarget Zabbix = new ZabbixTarget();

	public string RemoteAgent = "";

	public string FilePath = "";

	public static Company FromJson(Dictionary<string, object> root, string filePath)
	{
		Company c = new Company();
		c.FilePath = filePath;
		c.Id = Json.GetStr(root, "id", "");
		c.Name = Json.GetStr(root, "name", c.Id);
		c.ShortName = Json.GetStr(root, "shortName", c.Name);
		c.Timezone = Json.GetStr(root, "timezone", c.Timezone);
		c.Enabled = Json.Bool(Json.Get(root, "enabled"), dflt: true);
		foreach (object t in Json.AsArray(Json.Get(root, "tags")))
		{
			c.Tags.Add(Json.Str(t));
		}
		foreach (object co in Json.AsArray(Json.Get(root, "contacts")))
		{
			Dictionary<string, object> cd = Json.AsObject(co);
			Contact ct = new Contact();
			ct.Role = Json.GetStr(cd, "role", "");
			ct.Name = Json.GetStr(cd, "name", "");
			ct.Phone = Json.GetStr(cd, "phone", "");
			ct.Email = Json.GetStr(cd, "email", "");
			c.Contacts.Add(ct);
		}
		foreach (object so in Json.AsArray(Json.Get(root, "sources")))
		{
			Dictionary<string, object> sd = Json.AsObject(so);
			Source src = Source.FromJson(sd);
			if (src != null)
			{
				c.Sources.Add(src);
			}
		}
		c.Policy = Policy.FromJson(Json.AsObject(Json.Get(root, "policy")));
		c.Zabbix = ZabbixTarget.FromJson(Json.AsObject(Json.Get(root, "zabbix")));
		c.RemoteAgent = Json.GetStr(root, "remoteAgent", "");
		if (string.IsNullOrEmpty(c.Id))
		{
			throw new FormatException("У компании не задан id (" + filePath + ")");
		}
		return c;
	}
}
