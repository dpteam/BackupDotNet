using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BackupChecker.Core;

public class AgentRegistry
{
	public class RemoteAgent
	{
		public string Id = "";

		public string Name = "";

		public string Token = "";

		public bool Enabled = true;

		public List<string> Companies = new List<string>();

		public DateTime? LastSeen;

		public string LastIp = "";

		public string LastVersion = "";

		public string LastError = "";

		public bool RunNow;

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["id"] = Id;
			d["name"] = Name;
			d["enabled"] = Enabled;
			d["companies"] = ToObjectList(Companies);
			if (LastSeen.HasValue)
			{
				d["lastSeen"] = LastSeen.Value;
			}
			if (LastIp.Length > 0)
			{
				d["lastIp"] = LastIp;
			}
			if (LastVersion.Length > 0)
			{
				d["lastVersion"] = LastVersion;
			}
			if (LastError.Length > 0)
			{
				d["lastError"] = LastError;
			}
			bool online = LastSeen.HasValue && (DateTime.UtcNow - LastSeen.Value.ToUniversalTime()).TotalMinutes < 10.0;
			d["online"] = online;
			return d;
		}
	}

	private readonly string _path;

	private readonly object _sync = new object();

	private List<RemoteAgent> _agents = new List<RemoteAgent>();

	public List<string> LoadErrors = new List<string>();

	public string Path_ => _path;

	public List<RemoteAgent> All
	{
		get
		{
			lock (_sync)
			{
				return new List<RemoteAgent>(_agents);
			}
		}
	}

	public AgentRegistry(string configDir)
	{
		_path = Path.Combine(configDir, "agents.json");
	}

	public void Load()
	{
		lock (_sync)
		{
			_agents = new List<RemoteAgent>();
			LoadErrors.Clear();
			if (!File.Exists(_path))
			{
				return;
			}
			try
			{
				Dictionary<string, object> root = Json.ParseObject(File.ReadAllText(_path, Encoding.UTF8));
				foreach (object o in Json.AsArray(Json.Get(root, "agents")))
				{
					Dictionary<string, object> a = Json.AsObject(o);
					RemoteAgent ra = new RemoteAgent();
					ra.Id = Json.GetStr(a, "id", "").Trim();
					if (ra.Id.Length == 0)
					{
						LoadErrors.Add("агент без id — пропущен");
						continue;
					}
					ra.Name = Json.GetStr(a, "name", ra.Id);
					ra.Token = Json.GetStr(a, "token", "");
					ra.Enabled = Json.Bool(Json.Get(a, "enabled"), dflt: true);
					foreach (object c in Json.AsArray(Json.Get(a, "companies")))
					{
						string id = Json.Str(c).Trim();
						if (id.Length > 0)
						{
							ra.Companies.Add(id);
						}
					}
					ra.LastSeen = BackupRun.ParseDateNullable(Json.Get(a, "lastSeen"));
					ra.LastIp = Json.GetStr(a, "lastIp", "");
					ra.LastVersion = Json.GetStr(a, "lastVersion", "");
					ra.LastError = Json.GetStr(a, "lastError", "");
					ra.RunNow = Json.Bool(Json.Get(a, "runNow"));
					if (ra.Token.Length < 16)
					{
						LoadErrors.Add("агент " + ra.Id + ": токен короче 16 символов");
					}
					else
					{
						_agents.Add(ra);
					}
				}
			}
			catch (Exception ex)
			{
				LoadErrors.Add("agents.json: " + ex.Message);
			}
		}
	}

	public RemoteAgent ByToken(string token)
	{
		if (string.IsNullOrEmpty(token))
		{
			return null;
		}
		lock (_sync)
		{
			foreach (RemoteAgent a in _agents)
			{
				if (a.Enabled && TokenEquals(a.Token, token))
				{
					return a;
				}
			}
		}
		return null;
	}

	public RemoteAgent ById(string id)
	{
		lock (_sync)
		{
			foreach (RemoteAgent a in _agents)
			{
				if (string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase))
				{
					return a;
				}
			}
		}
		return null;
	}

	public List<Company> CompaniesFor(RemoteAgent agent, List<Company> all)
	{
		List<Company> result = new List<Company>();
		if (agent == null || agent.Companies.Count == 0)
		{
			return all;
		}
		HashSet<string> allowed = new HashSet<string>(agent.Companies, StringComparer.OrdinalIgnoreCase);
		foreach (Company c in all)
		{
			if (allowed.Contains(c.Id))
			{
				result.Add(c);
			}
		}
		return result;
	}

	public void Touch(string agentId, string ip, string version, string error)
	{
		lock (_sync)
		{
			RemoteAgent a = null;
			foreach (RemoteAgent x in _agents)
			{
				if (string.Equals(x.Id, agentId, StringComparison.OrdinalIgnoreCase))
				{
					a = x;
					break;
				}
			}
			if (a == null)
			{
				return;
			}
			a.LastSeen = DateTime.Now;
			if (!string.IsNullOrEmpty(ip))
			{
				a.LastIp = ip;
			}
			if (!string.IsNullOrEmpty(version))
			{
				a.LastVersion = version;
			}
			a.LastError = error ?? "";
			try
			{
				SaveLocked();
			}
			catch (Exception ex)
			{
				Log.Warn("Не удалось сохранить agents.json: " + ex.Message);
			}
		}
	}

	private void SaveLocked()
	{
		List<object> list = new List<object>();
		foreach (RemoteAgent a in _agents)
		{
			Dictionary<string, object> d = Json.Obj();
			d["id"] = a.Id;
			d["name"] = a.Name;
			d["token"] = a.Token;
			d["enabled"] = a.Enabled;
			d["companies"] = ToObjectList(a.Companies);
			if (a.LastSeen.HasValue)
			{
				d["lastSeen"] = a.LastSeen.Value;
			}
			if (a.LastIp.Length > 0)
			{
				d["lastIp"] = a.LastIp;
			}
			if (a.LastVersion.Length > 0)
			{
				d["lastVersion"] = a.LastVersion;
			}
			if (a.LastError.Length > 0)
			{
				d["lastError"] = a.LastError;
			}
			if (a.RunNow)
			{
				d["runNow"] = true;
			}
			list.Add(d);
		}
		Dictionary<string, object> root = Json.Obj();
		root["agents"] = list;
		Directory.CreateDirectory(Path.GetDirectoryName(_path));
		ConfigLoader.AtomicWrite(_path, Json.WritePretty(root));
	}

	public RemoteAgent Create(string id, string name, string token)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		lock (_sync)
		{
			foreach (RemoteAgent a in _agents)
			{
				if (string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase))
				{
					return a;
				}
			}
			RemoteAgent ra = new RemoteAgent();
			ra.Id = id;
			ra.Name = name;
			ra.Token = token;
			ra.Enabled = true;
			_agents.Add(ra);
			try
			{
				SaveLocked();
			}
			catch (Exception ex)
			{
				Log.Warn("agents.json: " + ex.Message);
			}
			return ra;
		}
	}

	public bool Remove(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return false;
		}
		lock (_sync)
		{
			RemoteAgent found = null;
			foreach (RemoteAgent a in _agents)
			{
				if (string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase))
				{
					found = a;
					break;
				}
			}
			if (found == null)
			{
				return false;
			}
			_agents.Remove(found);
			try
			{
				SaveLocked();
			}
			catch (Exception ex)
			{
				Log.Warn("agents.json: " + ex.Message);
			}
			return true;
		}
	}

	public bool RequestRun(string agentId)
	{
		lock (_sync)
		{
			RemoteAgent a = null;
			foreach (RemoteAgent x in _agents)
			{
				if (string.Equals(x.Id, agentId, StringComparison.OrdinalIgnoreCase))
				{
					a = x;
					break;
				}
			}
			if (a == null)
			{
				return false;
			}
			a.RunNow = true;
			try
			{
				SaveLocked();
			}
			catch (Exception ex)
			{
				Log.Warn("agents.json: " + ex.Message);
			}
			return true;
		}
	}

	public bool ConsumeRunNow(string agentId)
	{
		lock (_sync)
		{
			RemoteAgent a = null;
			foreach (RemoteAgent x in _agents)
			{
				if (string.Equals(x.Id, agentId, StringComparison.OrdinalIgnoreCase))
				{
					a = x;
					break;
				}
			}
			if (a == null || !a.RunNow)
			{
				return false;
			}
			a.RunNow = false;
			try
			{
				SaveLocked();
			}
			catch
			{
			}
			return true;
		}
	}

	private static bool TokenEquals(string a, string b)
	{
		if (a == null || b == null)
		{
			return false;
		}
		if (a.Length != b.Length)
		{
			return false;
		}
		int diff = 0;
		for (int i = 0; i < a.Length; i++)
		{
			diff |= a[i] ^ b[i];
		}
		return diff == 0;
	}

	public static List<object> ToObjectList(List<string> src)
	{
		List<object> list = new List<object>();
		foreach (string s in src)
		{
			list.Add(s);
		}
		return list;
	}

	public static string NewToken()
	{
		byte[] bytes = new byte[24];
		using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
		{
			rng.GetBytes(bytes);
		}
		StringBuilder sb = new StringBuilder();
		string alphabet = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
		byte[] array = bytes;
		foreach (byte b in array)
		{
			sb.Append(alphabet[b % alphabet.Length]);
		}
		return sb.ToString();
	}
}
