namespace BackupDotNet.Web;

public static class Roles
{
	public const string Admin = "admin";

	public const string Operator = "operator";

	public const string Viewer = "viewer";

	public const string Client = "client";

	public static bool IsValid(string role)
	{
		switch (role)
		{
		default:
			return role == "client";
		case "admin":
		case "operator":
		case "viewer":
			return true;
		}
	}

	public static string Describe(string role)
	{
		return role switch
		{
			"admin" => "Полный доступ, включая пользователей", 
			"operator" => "Просмотр и изменение компаний, запуск проверок", 
			"viewer" => "Только просмотр", 
			"client" => "Только свои компании", 
			_ => role, 
		};
	}

	public static bool CanWrite(string role)
	{
		if (!(role == "admin"))
		{
			return role == "operator";
		}
		return true;
	}

	public static bool CanManageUsers(string role)
	{
		return role == "admin";
	}

	public static bool CanDeleteCompanies(string role)
	{
		return role == "admin";
	}

	public static bool IsRestricted(string role)
	{
		return role == "client";
	}
}
