using System;
using System.Collections.Generic;
using BackupChecker.Core;

namespace BackupChecker.Web;

public class UserRecord
{
	public string Username = "";

	public string Hash = "";

	public string Role = "viewer";

	public bool Enabled = true;

	public string DisplayName = "";

	public string Email = "";

	public List<string> CompanyIds = new List<string>();

	public DateTime CreatedAt = DateTime.Now;

	public DateTime? LastLogin;

	public bool MustChangePassword;

	public int FailedAttempts;

	public DateTime? LockedUntil;

	public bool IsLocked
	{
		get
		{
			if (LockedUntil.HasValue)
			{
				return LockedUntil.Value > DateTime.UtcNow;
			}
			return false;
		}
	}

	public Dictionary<string, object> ToJson(bool includeHash)
	{
		Dictionary<string, object> d = Json.Obj();
		d["username"] = Username;
		if (includeHash)
		{
			d["hash"] = Hash;
		}
		d["role"] = Role;
		d["enabled"] = Enabled;
		if (DisplayName.Length > 0)
		{
			d["displayName"] = DisplayName;
		}
		if (Email.Length > 0)
		{
			d["email"] = Email;
		}
		if (CompanyIds.Count > 0)
		{
			List<object> ids = new List<object>();
			foreach (string s in CompanyIds)
			{
				ids.Add(s);
			}
			d["companyIds"] = ids;
		}
		d["createdAt"] = CreatedAt;
		if (LastLogin.HasValue)
		{
			d["lastLogin"] = LastLogin.Value;
		}
		if (MustChangePassword)
		{
			d["mustChangePassword"] = true;
		}
		if (FailedAttempts > 0)
		{
			d["failedAttempts"] = FailedAttempts;
		}
		if (LockedUntil.HasValue)
		{
			d["lockedUntil"] = LockedUntil.Value;
		}
		return d;
	}

	public static UserRecord FromJson(Dictionary<string, object> d)
	{
		UserRecord u = new UserRecord();
		u.Username = Json.GetStr(d, "username", "");
		u.Hash = Json.GetStr(d, "hash", "");
		u.Role = Json.GetStr(d, "role", "viewer");
		if (!Roles.IsValid(u.Role))
		{
			u.Role = "viewer";
		}
		u.Enabled = Json.Bool(Json.Get(d, "enabled"), dflt: true);
		u.DisplayName = Json.GetStr(d, "displayName", "");
		u.Email = Json.GetStr(d, "email", "");
		u.MustChangePassword = Json.Bool(Json.Get(d, "mustChangePassword"));
		foreach (object o in Json.AsArray(Json.Get(d, "companyIds")))
		{
			string id = Json.Str(o);
			if (id.Length > 0)
			{
				u.CompanyIds.Add(id);
			}
		}
		DateTime created = BackupRun.ParseDate(Json.Get(d, "createdAt"));
		u.CreatedAt = ((created == DateTime.MinValue) ? DateTime.Now : created);
		u.LastLogin = BackupRun.ParseDateNullable(Json.Get(d, "lastLogin"));
		u.FailedAttempts = Json.Int(Json.Get(d, "failedAttempts"));
		u.LockedUntil = BackupRun.ParseDateNullable(Json.Get(d, "lockedUntil"));
		return u;
	}
}
