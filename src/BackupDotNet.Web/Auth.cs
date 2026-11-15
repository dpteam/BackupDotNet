using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using BackupDotNet.Core;

namespace BackupDotNet.Web;

public class Auth
{
	private const int MaxFailedAttempts = 5;

	private const int LockMinutes = 5;

	private readonly string _usersPath;

	private readonly Dictionary<string, SessionInfo> _sessions = new Dictionary<string, SessionInfo>(StringComparer.Ordinal);

	private readonly object _sync = new object();

	private readonly int _sessionMinutes;

	private readonly Dictionary<string, UserRecord> _users = new Dictionary<string, UserRecord>(StringComparer.OrdinalIgnoreCase);

	public string GeneratedPassword = "";

	public int UserCount
	{
		get
		{
			lock (_sync)
			{
				return _users.Count;
			}
		}
	}

	public Auth(string configDir, int sessionMinutes)
	{
		_usersPath = Path.Combine(configDir, "users.json");
		_sessionMinutes = ((sessionMinutes <= 0) ? 480 : sessionMinutes);
		LoadOrCreate();
	}

	private void LoadOrCreate()
	{
		if (File.Exists(_usersPath))
		{
			try
			{
				Dictionary<string, object> root = Json.ParseObject(File.ReadAllText(_usersPath));
				foreach (object o in Json.AsArray(Json.Get(root, "users")))
				{
					UserRecord u = UserRecord.FromJson(Json.AsObject(o));
					if (u.Username.Length != 0 && u.Hash.Length != 0)
					{
						_users[u.Username] = u;
					}
				}
				if (_users.Count > 0)
				{
					return;
				}
			}
			catch (Exception ex)
			{
				Log.Warn("users.json: " + ex.Message);
			}
		}
		string password = (GeneratedPassword = GeneratePassword(16));
		UserRecord admin = new UserRecord();
		admin.Username = "admin";
		admin.Hash = Hash(password);
		admin.Role = "admin";
		admin.DisplayName = "Администратор";
		admin.Enabled = true;
		admin.CreatedAt = DateTime.Now;
		_users["admin"] = admin;
		Save();
		Log.Warn("Создан пользователь admin с паролем: " + password + " (сохраните в auth/)");
	}

	public void Save()
	{
		List<object> list = new List<object>();
		lock (_sync)
		{
			foreach (KeyValuePair<string, UserRecord> user in _users)
			{
				list.Add(user.Value.ToJson(includeHash: true));
			}
		}
		Dictionary<string, object> root = Json.Obj();
		root["users"] = list;
		ConfigLoader.AtomicWrite(_usersPath, Json.WritePretty(root));
	}

	public static string GeneratePassword(int length)
	{
		byte[] rnd = new byte[length];
		using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
		{
			rng.GetBytes(rnd);
		}
		StringBuilder sb = new StringBuilder(length);
		for (int i = 0; i < length; i++)
		{
			sb.Append("abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789"[rnd[i] % "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789".Length]);
		}
		return sb.ToString();
	}

	public static string Hash(string password)
	{
		byte[] salt = new byte[16];
		using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
		{
			rng.GetBytes(salt);
		}
		byte[] hash = Derive(password, salt, 10000);
		return "pbkdf2$10000$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
	}

	public static bool VerifyHash(string password, string stored)
	{
		try
		{
			string[] parts = stored.Split('$');
			if (parts.Length != 4 || parts[0] != "pbkdf2")
			{
				return false;
			}
			int iter = int.Parse(parts[1], CultureInfo.InvariantCulture);
			byte[] salt = Convert.FromBase64String(parts[2]);
			byte[] expected = Convert.FromBase64String(parts[3]);
			byte[] actual = Derive(password, salt, iter);
			if (actual.Length != expected.Length)
			{
				return false;
			}
			int diff = 0;
			for (int i = 0; i < actual.Length; i++)
			{
				diff |= actual[i] ^ expected[i];
			}
			return diff == 0;
		}
		catch
		{
			return false;
		}
	}

	private static byte[] Derive(string password, byte[] salt, int iterations)
	{
		using Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations);
		return pbkdf2.GetBytes(32);
	}

	public string Login(string user, string password, out string error)
	{
		error = "";
		if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
		{
			error = "Неверный логин или пароль";
			return null;
		}
		UserRecord rec;
		lock (_sync)
		{
			if (!_users.TryGetValue(user, out rec))
			{
				error = "Неверный логин или пароль";
				return null;
			}
			if (!rec.Enabled)
			{
				error = "Учётная запись отключена";
				return null;
			}
			if (rec.IsLocked)
			{
				int minutes = (int)Math.Ceiling((rec.LockedUntil.Value - DateTime.UtcNow).TotalMinutes);
				error = "Слишком много неудачных попыток. Повторите через " + minutes + " мин";
				return null;
			}
		}
		if (!VerifyHash(password, rec.Hash))
		{
			lock (_sync)
			{
				rec.FailedAttempts++;
				if (rec.FailedAttempts >= 5)
				{
					rec.LockedUntil = DateTime.UtcNow.AddMinutes(5.0);
					rec.FailedAttempts = 0;
					error = "Слишком много неудачных попыток. Блокировка на " + 5 + " мин";
				}
				else
				{
					error = "Неверный логин или пароль";
				}
			}
			Save();
			return null;
		}
		string token = Convert.ToBase64String(NewBytes(24)).Replace("+", "").Replace("/", "")
			.Replace("=", "");
		SessionInfo si = new SessionInfo();
		si.User = rec.Username;
		si.Role = rec.Role;
		si.CompanyIds = new List<string>(rec.CompanyIds);
		si.MustChangePassword = rec.MustChangePassword;
		si.Expires = DateTime.Now.AddMinutes(_sessionMinutes);
		lock (_sync)
		{
			rec.FailedAttempts = 0;
			rec.LockedUntil = null;
			rec.LastLogin = DateTime.UtcNow;
			_sessions[token] = si;
		}
		Save();
		return token;
	}

	private static byte[] NewBytes(int n)
	{
		byte[] b = new byte[n];
		using RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
		rng.GetBytes(b);
		return b;
	}

	public SessionInfo Validate(string token)
	{
		if (string.IsNullOrEmpty(token))
		{
			return null;
		}
		lock (_sync)
		{
			if (!_sessions.TryGetValue(token, out var si))
			{
				return null;
			}
			if (si.Expires < DateTime.Now)
			{
				_sessions.Remove(token);
				return null;
			}
			if (_users.TryGetValue(si.User, out var rec))
			{
				if (!rec.Enabled)
				{
					_sessions.Remove(token);
					return null;
				}
				si.Role = rec.Role;
				si.CompanyIds = new List<string>(rec.CompanyIds);
				si.MustChangePassword = rec.MustChangePassword;
			}
			si.Expires = DateTime.Now.AddMinutes(_sessionMinutes);
			return si;
		}
	}

	public void Logout(string token)
	{
		if (string.IsNullOrEmpty(token))
		{
			return;
		}
		lock (_sync)
		{
			_sessions.Remove(token);
		}
	}

	public int LogoutAll(string username)
	{
		List<string> tokens = new List<string>();
		lock (_sync)
		{
			foreach (KeyValuePair<string, SessionInfo> kv in _sessions)
			{
				if (string.Equals(kv.Value.User, username, StringComparison.OrdinalIgnoreCase))
				{
					tokens.Add(kv.Key);
				}
			}
			foreach (string t in tokens)
			{
				_sessions.Remove(t);
			}
		}
		return tokens.Count;
	}

	public bool ChangePassword(string user, string newPassword)
	{
		lock (_sync)
		{
			if (!_users.TryGetValue(user, out var rec))
			{
				return false;
			}
			rec.Hash = Hash(newPassword);
			rec.MustChangePassword = false;
		}
		Save();
		return true;
	}

	public bool ResetPassword(string user, string newPassword, bool mustChange)
	{
		lock (_sync)
		{
			if (!_users.TryGetValue(user, out var rec))
			{
				return false;
			}
			rec.Hash = Hash(newPassword);
			rec.MustChangePassword = mustChange;
			rec.FailedAttempts = 0;
			rec.LockedUntil = null;
		}
		LogoutAll(user);
		Save();
		return true;
	}

	public List<UserRecord> List()
	{
		List<UserRecord> list = new List<UserRecord>();
		lock (_sync)
		{
			foreach (UserRecord u in _users.Values)
			{
				list.Add(u);
			}
		}
		list.Sort((UserRecord a, UserRecord b) => string.Compare(a.Username, b.Username, StringComparison.OrdinalIgnoreCase));
		return list;
	}

	public UserRecord Get(string username)
	{
		lock (_sync)
		{
			if (_users.TryGetValue(username, out var u))
			{
				return u;
			}
			return null;
		}
	}

	public bool Exists(string username)
	{
		lock (_sync)
		{
			return _users.ContainsKey(username);
		}
	}

	public int CountAdmins()
	{
		int n = 0;
		lock (_sync)
		{
			foreach (UserRecord u in _users.Values)
			{
				if (u.Role == "admin" && u.Enabled)
				{
					n++;
				}
			}
			return n;
		}
	}

	public void Add(UserRecord user)
	{
		lock (_sync)
		{
			_users[user.Username] = user;
		}
		Save();
	}

	public bool Update(string username, Dictionary<string, object> payload, out string error)
	{
		error = "";
		lock (_sync)
		{
			if (!_users.TryGetValue(username, out var rec))
			{
				error = "Пользователь не найден";
				return false;
			}
			string role = Json.GetStr(payload, "role", rec.Role);
			if (!Roles.IsValid(role))
			{
				error = "Неизвестная роль: " + role;
				return false;
			}
			bool enabled = Json.Bool(Json.Get(payload, "enabled"), rec.Enabled);
			bool wasActiveAdmin = rec.Role == "admin" && rec.Enabled;
			bool willBeActiveAdmin = role == "admin" && enabled;
			if (wasActiveAdmin && !willBeActiveAdmin && CountAdmins() <= 1)
			{
				error = "Нельзя лишить прав последнего администратора";
				return false;
			}
			rec.Role = role;
			rec.Enabled = enabled;
			rec.DisplayName = Json.GetStr(payload, "displayName", rec.DisplayName);
			rec.Email = Json.GetStr(payload, "email", rec.Email);
			List<string> ids = new List<string>();
			foreach (object o in Json.AsArray(Json.Get(payload, "companyIds")))
			{
				string id = Json.Str(o);
				if (id.Length > 0)
				{
					ids.Add(id);
				}
			}
			rec.CompanyIds = ids;
		}
		Save();
		LogoutAll(username);
		return true;
	}

	public bool Delete(string username, out string error)
	{
		error = "";
		lock (_sync)
		{
			if (string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase))
			{
				error = "Нельзя удалить основного администратора";
				return false;
			}
			if (!_users.TryGetValue(username, out var rec))
			{
				error = "Пользователь не найден";
				return false;
			}
			if (rec.Role == "admin" && rec.Enabled && CountAdmins() <= 1)
			{
				error = "Нельзя удалить последнего администратора";
				return false;
			}
			_users.Remove(username);
		}
		LogoutAll(username);
		Save();
		return true;
	}
}
