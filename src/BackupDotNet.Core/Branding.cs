using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BackupDotNet.Core;

public class Branding
{
	public string Part1 = "Backup";

	public string Part2 = "DotNet";

	public string Part1Color = "#0088FF";

	public string Part2Color = "#FFFFFF";

	public string Tagline = "Мониторинг бэкапов, доступности и железа";

	public string Title = "BackupDotNet · Мониторинг";

	public string LogoLight = "img/logo.png";

	public string LogoDark = "img/logo-dark.png";

	public string Favicon = "img/favicon.ico";

	public string FooterText = "BackupDotNet";

	public string FooterUrl = "";

	public string FooterUrlText = "";

	public string ProductName = "BackupDotNet";

	public string Company = "BackupDotNet";

	public string Description = "Мониторинг бэкапов, доступности и железа";

	public string InstallDir = "C:\\BackupDotNet\\";

	/// <summary>
	/// Каталог установки удалённого агента. Отдельный от <see cref="InstallDir"/>:
	/// агент часто ставят на ту же машину, что сервер, и общий каталог даёт
	/// конфликт двух служб за одни и те же файлы.
	///
	/// Если в брендинге не задан — выводится из <see cref="InstallDir"/>
	/// добавлением «Agent»: <c>C:\BackupDotNet\</c> → <c>C:\BackupDotNetAgent</c>.
	/// </summary>
	public string AgentInstallDir = "C:\\BackupDotNetAgent";

	public string DataDir = "C:\\ProgramData\\BackupDotNet";

	public string ServiceName = "BackupDotNetService";

	public string ServiceNameRemote = "BackupDotNetRemote";

	public string UserAgent = "BackupDotNet";

	public string ZabbixTemplateName = "BackupDotNet";

	public string FilePath = "";

	public List<string> LoadErrors = new List<string>();

	private static Branding _cached;

	private static string _cachedDir = "";

	private static DateTime _cachedStamp = DateTime.MinValue;

	private static readonly object _cacheSync = new object();

	private static Branding _current = new Branding();

	public static Branding Current => _current;

	public string FullName => (Part1 + Part2).Trim();

	public static Branding For(string configDir)
	{
		lock (_cacheSync)
		{
			string path = Path.Combine(configDir ?? "", "branding.json");
			DateTime stamp = DateTime.MinValue;
			try
			{
				if (File.Exists(path))
				{
					stamp = File.GetLastWriteTimeUtc(path);
				}
			}
			catch
			{
			}
			if (_cached != null && _cachedDir == (configDir ?? "") && _cachedStamp == stamp)
			{
				return _cached;
			}
			_cached = Load(configDir);
			_cachedDir = configDir ?? "";
			_cachedStamp = stamp;
			return _cached;
		}
	}

	public static void Invalidate()
	{
		lock (_cacheSync)
		{
			_cached = null;
			_cachedStamp = DateTime.MinValue;
		}
	}

	public static void SetCurrent(Branding b)
	{
		if (b != null)
		{
			_current = b;
		}
	}

	public static Branding Default()
	{
		return new Branding();
	}

	public static Branding Load(string configDir)
	{
		Branding b = new Branding();
		b.FilePath = Path.Combine(configDir, "branding.json");
		if (File.Exists(b.FilePath))
		{
			try
			{
				string content = File.ReadAllText(b.FilePath, Encoding.UTF8);
				b.ApplyFrom(Json.ParseObject(content), appearance: true);
			}
			catch (Exception ex)
			{
				b.LoadErrors.Add("branding.json: " + ex.Message);
			}
		}
		string envPath = Path.Combine(configDir, "env.json");
		if (File.Exists(envPath))
		{
			try
			{
				string content2 = File.ReadAllText(envPath, Encoding.UTF8);
				b.ApplyFrom(Json.ParseObject(content2), appearance: false);
			}
			catch (Exception ex2)
			{
				b.LoadErrors.Add("env.json: " + ex2.Message);
			}
		}
		return b;
	}

	private void ApplyFrom(Dictionary<string, object> root, bool appearance)
	{
		Dictionary<string, object> brand = Json.AsObject(Json.Get(root, "brand"));
		Dictionary<string, object> src = ((brand.Count > 0) ? brand : root);
		if (appearance)
		{
			Part1 = Str(src, root, "part1", Part1);
			Part2 = Str(src, root, "part2", Part2);
			Part1Color = Str(src, root, "part1Color", Part1Color);
			Part2Color = Str(src, root, "part2Color", Part2Color);
			Tagline = Str(src, root, "tagline", Tagline);
			LogoLight = Str(src, root, "logoLight", LogoLight);
			LogoDark = Str(src, root, "logoDark", LogoDark);
			Favicon = Str(src, root, "favicon", Favicon);
			FooterText = Str(src, root, "footer", FooterText);
			FooterUrl = Str(src, root, "footerUrl", FooterUrl);
			FooterUrlText = Str(src, root, "footerUrlText", FooterUrlText);
			ProductName = Str(src, root, "productName", ProductName);
			Title = Str(root, root, "documentTitle", Title);
			Company = Str(src, root, "company", Company);
			Description = Str(src, root, "description", Description);
			UserAgent = Str(src, root, "userAgent", UserAgent);
			ZabbixTemplateName = Str(src, root, "zabbixTemplateName", ZabbixTemplateName);
		}
		InstallDir = Str(src, root, "installDir", InstallDir);
		AgentInstallDir = Str(src, root, "agentInstallDir", InstallDir.TrimEnd('\\') + "Agent");
		DataDir = Str(src, root, "dataDir", DataDir);
		ServiceName = Str(src, root, "serviceName", ServiceName);
		ServiceNameRemote = Str(src, root, "serviceNameRemote", ServiceNameRemote);
		if (appearance && Title == new Branding().Title && ProductName != new Branding().ProductName)
		{
			Title = ProductName;
		}
	}

	private static string Str(Dictionary<string, object> primary, Dictionary<string, object> fallback, string key, string dflt)
	{
		string v = Json.GetStr(primary, key, "");
		if (v.Length == 0 && !object.ReferenceEquals(primary, fallback))
		{
			v = Json.GetStr(fallback, key, "");
		}
		if (v.Length <= 0)
		{
			return dflt;
		}
		return v;
	}

	public Dictionary<string, object> ToJson()
	{
		Dictionary<string, object> d = Json.Obj();
		d["part1"] = Part1;
		d["part2"] = Part2;
		d["part1Color"] = Part1Color;
		d["part2Color"] = Part2Color;
		d["tagline"] = Tagline;
		d["title"] = Title;
		d["logoLight"] = LogoLight;
		d["logoDark"] = LogoDark;
		d["favicon"] = Favicon;
		d["footerText"] = FooterText;
		d["footerUrl"] = FooterUrl;
		d["footerUrlText"] = FooterUrlText;
		d["productName"] = ProductName;
		d["company"] = Company;
		d["description"] = Description;
		d["installDir"] = InstallDir;
		d["agentInstallDir"] = AgentInstallDir;
		d["dataDir"] = DataDir;
		d["serviceName"] = ServiceName;
		d["serviceNameRemote"] = ServiceNameRemote;
		d["userAgent"] = UserAgent;
		d["zabbixTemplateName"] = ZabbixTemplateName;
		d["fullName"] = FullName;
		return d;
	}

	public static void WriteDefault(string configDir)
	{
		string path = Path.Combine(configDir, "branding.json");
		Dictionary<string, object> brand = Json.Obj();
		brand["part1"] = "Backup";
		brand["part2"] = "DotNet";
		brand["part1Color"] = "#0088FF";
		brand["part2Color"] = "#FFFFFF";
		brand["tagline"] = "Мониторинг бэкапов, доступности и железа";
		brand["logoLight"] = "img/logo.png";
		brand["logoDark"] = "img/logo-dark.png";
		brand["favicon"] = "img/favicon.ico";
		brand["footer"] = "BackupDotNet";
		brand["footerUrl"] = "";
		brand["footerUrlText"] = "";
		brand["productName"] = "BackupDotNet";
		Dictionary<string, object> root = Json.Obj();
		root["documentTitle"] = "BackupDotNet · Мониторинг";
		root["brand"] = brand;
		Directory.CreateDirectory(configDir);
		ConfigLoader.AtomicWrite(path, Json.WritePretty(root));
	}
}
