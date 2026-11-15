using System;
using System.Collections.Generic;

namespace BackupDotNet.Web;

public class SessionInfo
{
	public string User = "";

	public string Role = "admin";

	public List<string> CompanyIds = new List<string>();

	public bool MustChangePassword;

	public DateTime Expires = DateTime.Now;

	public bool CanSeeCompany(string companyId)
	{
		if (!Roles.IsRestricted(Role))
		{
			return true;
		}
		foreach (string id in CompanyIds)
		{
			if (string.Equals(id, companyId, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}
}
