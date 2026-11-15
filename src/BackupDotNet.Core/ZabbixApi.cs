using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace BackupDotNet.Core;

public class ZabbixApi
{
	private readonly string _url;

	private readonly string _token;

	private readonly int _timeout;

	public bool IsConfigured
	{
		get
		{
			if (!string.IsNullOrEmpty(_url))
			{
				return !string.IsNullOrEmpty(_token);
			}
			return false;
		}
	}

	public ZabbixApi(string url, string token, int timeoutSec)
	{
		_url = url;
		_token = token;
		_timeout = ((timeoutSec <= 0) ? 15 : timeoutSec);
	}

	public Dictionary<string, object> Call(string method, Dictionary<string, object> parameters)
	{
		Dictionary<string, object> req = Json.Obj();
		req["jsonrpc"] = "2.0";
		req["method"] = method;
		req["params"] = parameters ?? Json.Obj();
		req["id"] = 1;
		string body = Json.Write(req);
		HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(_url);
		wr.Method = "POST";
		wr.ContentType = "application/json-rpc";
		wr.Timeout = _timeout * 1000;
		wr.Headers["Authorization"] = "Bearer " + _token;
		byte[] data = Encoding.UTF8.GetBytes(body);
		wr.ContentLength = data.Length;
		using (Stream s = wr.GetRequestStream())
		{
			s.Write(data, 0, data.Length);
		}
		using HttpWebResponse resp = (HttpWebResponse)wr.GetResponse();
		using StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8);
		string text = sr.ReadToEnd();
		Dictionary<string, object> result = Json.ParseObject(text);
		object err = Json.Get(result, "error");
		if (err != null)
		{
			Dictionary<string, object> e = Json.AsObject(err);
			throw new Exception("Zabbix API: " + Json.GetStr(e, "message", "") + " — " + Json.GetStr(e, "data", ""));
		}
		return result;
	}

	public string FindHostId(string hostName)
	{
		Dictionary<string, object> p = Json.Obj();
		p["filter"] = Json.Obj("host", hostName);
		p["output"] = new List<object>(new object[2] { "hostid", "host" });
		Dictionary<string, object> resp = Call("host.get", p);
		List<object> hosts = Json.AsArray(Json.Get(resp, "result"));
		if (hosts.Count == 0)
		{
			return "";
		}
		return Json.GetStr(Json.AsObject(hosts[0]), "hostid", "");
	}

	public string FindGroupId(string groupName)
	{
		Dictionary<string, object> p = Json.Obj();
		p["filter"] = Json.Obj("name", groupName);
		p["output"] = new List<object>(new object[2] { "groupid", "name" });
		Dictionary<string, object> resp = Call("hostgroup.get", p);
		List<object> groups = Json.AsArray(Json.Get(resp, "result"));
		if (groups.Count == 0)
		{
			return "";
		}
		return Json.GetStr(Json.AsObject(groups[0]), "groupid", "");
	}

	public string FindTemplateId(string templateName)
	{
		Dictionary<string, object> p = Json.Obj();
		p["filter"] = Json.Obj("host", templateName);
		p["output"] = new List<object>(new object[2] { "templateid", "host" });
		Dictionary<string, object> resp = Call("template.get", p);
		List<object> tpl = Json.AsArray(Json.Get(resp, "result"));
		if (tpl.Count == 0)
		{
			return "";
		}
		return Json.GetStr(Json.AsObject(tpl[0]), "templateid", "");
	}

	public string EnsureHost(string hostName, string displayName, string groupName, string templateName, Dictionary<string, string> macros, Dictionary<string, string> tags)
	{
		string groupId = FindGroupId(groupName);
		if (string.IsNullOrEmpty(groupId))
		{
			throw new Exception("группа хостов «" + groupName + "» не найдена в Zabbix");
		}
		string templateId = "";
		if (!string.IsNullOrEmpty(templateName))
		{
			templateId = FindTemplateId(templateName);
		}
		List<object> macroList = new List<object>();
		if (macros != null)
		{
			foreach (KeyValuePair<string, string> kv in macros)
			{
				macroList.Add(Json.Obj("macro", kv.Key, "value", kv.Value));
			}
		}
		List<object> tagList = new List<object>();
		if (tags != null)
		{
			foreach (KeyValuePair<string, string> kv2 in tags)
			{
				tagList.Add(Json.Obj("tag", kv2.Key, "value", kv2.Value));
			}
		}
		string existing = FindHostId(hostName);
		if (!string.IsNullOrEmpty(existing))
		{
			Dictionary<string, object> p = Json.Obj();
			p["hostid"] = existing;
			if (!string.IsNullOrEmpty(displayName))
			{
				p["name"] = displayName;
			}
			if (macroList.Count > 0)
			{
				p["macros"] = macroList;
			}
			if (tagList.Count > 0)
			{
				p["tags"] = tagList;
			}
			Call("host.update", p);
			return existing;
		}
		Dictionary<string, object> create = Json.Obj();
		create["host"] = hostName;
		create["name"] = (string.IsNullOrEmpty(displayName) ? hostName : displayName);
		create["groups"] = new List<object>(new object[1] { Json.Obj("groupid", groupId) });
		if (!string.IsNullOrEmpty(templateId))
		{
			create["templates"] = new List<object>(new object[1] { Json.Obj("templateid", templateId) });
		}
		if (macroList.Count > 0)
		{
			create["macros"] = macroList;
		}
		if (tagList.Count > 0)
		{
			create["tags"] = tagList;
		}
		create["inventory_mode"] = -1;
		Dictionary<string, object> resp = Call("host.create", create);
		Dictionary<string, object> result = Json.AsObject(Json.Get(resp, "result"));
		List<object> ids = Json.AsArray(Json.Get(result, "hostids"));
		if (ids.Count > 0)
		{
			return Json.Str(ids[0]);
		}
		return "";
	}
}
