using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BackupDotNet.Core;

public class ConfigLoader
{
	public Settings Settings;

	public List<Company> Companies = new List<Company>();

	public List<string> LoadErrors = new List<string>();

	public string RootDir;

	public string ConfigDir;

	public string SecretsPath;

	private Dictionary<string, string> _secrets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	public ConfigLoader(string rootDir)
	{
		RootDir = Path.GetFullPath(rootDir);
		ConfigDir = Path.Combine(RootDir, "config");
		SecretsPath = Path.Combine(ConfigDir, "secrets.json");
	}

	public void Load()
	{
		LoadErrors.Clear();
		Settings = new Settings();
		string settingsPath = Path.Combine(ConfigDir, "settings.json");
		if (File.Exists(settingsPath))
		{
			try
			{
				Settings = Settings.FromJson(Json.ParseObject(File.ReadAllText(settingsPath)));
			}
			catch (Exception ex)
			{
				LoadErrors.Add("settings.json: " + ex.Message);
			}
		}
		else
		{
			LoadErrors.Add("не найден " + settingsPath + " — используются значения по умолчанию");
		}
		Settings.DataDir = Abs(Settings.DataDir);
		Settings.PublicDir = Abs(Settings.PublicDir);
		Settings.ConfigDir = ConfigDir;
		if (string.IsNullOrEmpty(Settings.ZipTempDir))
		{
			Settings.ZipTempDir = Path.Combine(Settings.DataDir, "cache");
		}
		Branding brand = Branding.For(ConfigDir);
		Branding.SetCurrent(brand);
		foreach (string err in brand.LoadErrors)
		{
			LoadErrors.Add(err);
		}
		if (!HasAgentName(settingsPath) || string.IsNullOrEmpty(Settings.AgentName))
		{
			Settings.AgentName = brand.FullName;
		}
		if (string.IsNullOrEmpty(Settings.ZabbixTemplateName))
		{
			Settings.ZabbixTemplateName = brand.ZabbixTemplateName;
		}
		LoadSecrets();
		LoadCompanies();
	}

	private static bool HasAgentName(string settingsPath)
	{
		try
		{
			if (!File.Exists(settingsPath))
			{
				return false;
			}
			Dictionary<string, object> root = Json.ParseObject(File.ReadAllText(settingsPath));
			Dictionary<string, object> agent = Json.AsObject(Json.Get(root, "agent"));
			return Json.GetStr(agent, "name", "").Trim().Length > 0;
		}
		catch
		{
			return false;
		}
	}

	private string Abs(string p)
	{
		if (string.IsNullOrEmpty(p))
		{
			return p;
		}
		if (Path.IsPathRooted(p))
		{
			return p;
		}
		return Path.GetFullPath(Path.Combine(RootDir, p));
	}

	private void LoadCompanies()
	{
		Companies.Clear();
		string dir = Path.Combine(ConfigDir, "companies");

		// Каталог создаём сами: после установки его нет, и «нет каталога»
		// выглядело как проблема конфигурации, хотя это нормальный старт
		// с нуля. Отсутствие каталога — не ошибка, ошибка — не суметь
		// его создать.
		if (!Directory.Exists(dir))
		{
			try
			{
				Directory.CreateDirectory(dir);
			}
			catch (Exception ex)
			{
				LoadErrors.Add("не удалось создать " + dir + ": " + ex.Message);
				return;
			}
		}
		List<string> files = new List<string>(Directory.GetFiles(dir, "*.json"));
		files.Sort(StringComparer.OrdinalIgnoreCase);
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string f in files)
		{
			string name = Path.GetFileName(f);
			if (name.StartsWith("_"))
			{
				continue;
			}
			try
			{
				Company c = Company.FromJson(Json.ParseObject(File.ReadAllText(f)), f);
				if (seen.Contains(c.Id))
				{
					LoadErrors.Add(name + ": дублирующийся id «" + c.Id + "» — пропущен");
					continue;
				}
				seen.Add(c.Id);
				Companies.Add(c);
			}
			catch (Exception ex)
			{
				LoadErrors.Add(name + ": " + ex.Message);
			}
		}
	}

	public void LoadSecrets()
	{
		_secrets.Clear();
		if (!File.Exists(SecretsPath))
		{
			return;
		}
		try
		{
			Dictionary<string, object> root = Json.ParseObject(File.ReadAllText(SecretsPath));
			foreach (KeyValuePair<string, object> kv in root)
			{
				string val = Json.Str(kv.Value);
				if (val.StartsWith("dpapi:", StringComparison.OrdinalIgnoreCase))
				{
					_secrets[kv.Key] = Unprotect(val.Substring(6));
				}
				else
				{
					_secrets[kv.Key] = val;
				}
			}
		}
		catch (Exception ex)
		{
			LoadErrors.Add("secrets.json: " + ex.Message);
		}
	}

	public string Secret(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return "";
		}
		if (_secrets.TryGetValue(key, out var v))
		{
			// Каждый выданный наружу секрет запоминаем для публичного журнала:
			// иначе голый пароль в тексте исключения уйдёт в файл, который
			// предназначен для передачи вендору.
			Log.RegisterSecret(v);
			return v;
		}
		return "";
	}

	public void SetSecretInMemory(string key, string value)
	{
		if (!string.IsNullOrEmpty(key))
		{
			_secrets[key] = value ?? "";
			Log.RegisterSecret(value);
		}
	}

	public void SaveSecret(string key, string value, bool protect)
	{
		Dictionary<string, object> root = new Dictionary<string, object>();
		if (File.Exists(SecretsPath))
		{
			try
			{
				root = Json.ParseObject(File.ReadAllText(SecretsPath));
			}
			catch
			{
				root = new Dictionary<string, object>();
			}
		}
		root[key] = (protect ? ("dpapi:" + Protect(value)) : value);
		Directory.CreateDirectory(Path.GetDirectoryName(SecretsPath));
		AtomicWrite(SecretsPath, Json.WritePretty(root));
		LoadSecrets();
	}

	private static string Protect(string plain)
	{
		byte[] data = Encoding.UTF8.GetBytes(plain);
		byte[] enc = ProtectedData.Protect(data, null, DataProtectionScope.LocalMachine);
		return Convert.ToBase64String(enc);
	}

	private static string Unprotect(string b64)
	{
		try
		{
			byte[] enc = Convert.FromBase64String(b64);
			byte[] data = ProtectedData.Unprotect(enc, null, DataProtectionScope.LocalMachine);
			return Encoding.UTF8.GetString(data);
		}
		catch
		{
			return "";
		}
	}

	public static void AtomicWrite(string path, string content)
	{
		string dir = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
		{
			Directory.CreateDirectory(dir);
		}
		string tmp = path + ".tmp";
		File.WriteAllText(tmp, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		if (File.Exists(path))
		{
			try
			{
				File.Replace(tmp, path, null);
				return;
			}
			catch
			{
				File.Delete(path);
			}
		}
		File.Move(tmp, path);
	}

	public Company Find(string id)
	{
		foreach (Company c in Companies)
		{
			if (string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase))
			{
				return c;
			}
		}
		return null;
	}
}
