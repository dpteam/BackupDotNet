using System.Collections.Generic;

namespace BackupDotNet.Core;

public class Settings
{
	public string AgentName = "";

	public string DataDir = "data";

	public string PublicDir = "public";

	public string ConfigDir = "config";

	public int HttpPort = 8080;

	public string BindAddress = "http://+:8080/";

	public string Timezone = "Europe/Moscow";

	public string Language = "ru";

	public int TickSeconds = 60;

	public int JitterSeconds = 120;

	public int MaxParallelChecks = 4;

	public int DefaultIntervalMinutes = 60;

	public int VeeamTimeoutSeconds = 30;

	public bool VeeamIgnoreCertDefault = true;

	public string VeeamApiVersion = "1.1-rev1";

	public string ZipTempDir = "";

	public int ZipBufferKb = 256;

	public int FtpTimeoutSeconds = 60;

	public bool ZabbixEnabled;

	public string ZabbixServer = "";

	public int ZabbixPort = 10051;

	public string ZabbixMode = "sender";

	public int ZabbixTimeoutSeconds = 5;

	public int ZabbixBatchSize = 500;

	public int ZabbixBatchBytes = 65536;

	public bool ZabbixApiEnabled;

	public string ZabbixApiUrl = "";

	public string ZabbixApiToken = "";

	public bool ZabbixAutoCreateHosts;

	public string ZabbixHostGroup = "Backup";

	public string ZabbixTemplateName = "";

	public bool WebRequireAuth = true;

	public int WebSessionMinutes = 480;

	public string LogLevel = "info";

	public int LogMaxMb = 32;

	public int LogKeepDays = 90;

	public static Settings FromJson(Dictionary<string, object> root)
	{
		Settings s = new Settings();
		Dictionary<string, object> agent = Json.AsObject(Json.Get(root, "agent"));
		if (agent.Count > 0)
		{
			s.AgentName = Json.GetStr(agent, "name", s.AgentName);
			s.DataDir = Json.GetStr(agent, "dataDir", s.DataDir);
			s.PublicDir = Json.GetStr(agent, "publicDir", s.PublicDir);
			s.ConfigDir = Json.GetStr(agent, "configDir", s.ConfigDir);
			s.HttpPort = Json.Int(Json.Get(agent, "httpPort"), s.HttpPort);
			s.BindAddress = Json.GetStr(agent, "bindAddress", s.BindAddress);
			s.Timezone = Json.GetStr(agent, "timezone", s.Timezone);
			s.Language = Json.GetStr(agent, "language", s.Language);
		}
		Dictionary<string, object> sch = Json.AsObject(Json.Get(root, "scheduler"));
		s.TickSeconds = Json.Int(Json.Get(sch, "tickSeconds"), s.TickSeconds);
		s.JitterSeconds = Json.Int(Json.Get(sch, "jitterSeconds"), s.JitterSeconds);
		s.MaxParallelChecks = Json.Int(Json.Get(sch, "maxParallelChecks"), s.MaxParallelChecks);
		s.DefaultIntervalMinutes = Json.Int(Json.Get(sch, "defaultIntervalMinutes"), s.DefaultIntervalMinutes);
		Dictionary<string, object> veeam = Json.AsObject(Json.Get(root, "veeam"));
		s.VeeamTimeoutSeconds = Json.Int(Json.Get(veeam, "requestTimeoutSeconds"), s.VeeamTimeoutSeconds);
		s.VeeamIgnoreCertDefault = Json.Bool(Json.Get(veeam, "ignoreCertErrorsDefault"), s.VeeamIgnoreCertDefault);
		s.VeeamApiVersion = Json.GetStr(veeam, "restApiVersion", s.VeeamApiVersion);
		Dictionary<string, object> zip = Json.AsObject(Json.Get(root, "zip"));
		s.ZipTempDir = Json.GetStr(zip, "tempDir", s.ZipTempDir);
		s.ZipBufferKb = Json.Int(Json.Get(zip, "crcReadBufferKb"), s.ZipBufferKb);
		Dictionary<string, object> zbx = Json.AsObject(Json.Get(root, "zabbix"));
		s.ZabbixEnabled = Json.Bool(Json.Get(zbx, "enabled"), s.ZabbixEnabled);
		s.ZabbixServer = Json.GetStr(zbx, "server", s.ZabbixServer);
		s.ZabbixPort = Json.Int(Json.Get(zbx, "port"), s.ZabbixPort);
		s.ZabbixMode = Json.GetStr(zbx, "mode", s.ZabbixMode);
		s.ZabbixTimeoutSeconds = Json.Int(Json.Get(zbx, "timeoutSeconds"), s.ZabbixTimeoutSeconds);
		s.ZabbixBatchSize = Json.Int(Json.Get(zbx, "batchSize"), s.ZabbixBatchSize);
		s.ZabbixBatchBytes = Json.Int(Json.Get(zbx, "batchMaxBytes"), s.ZabbixBatchBytes);
		Dictionary<string, object> zapi = Json.AsObject(Json.Get(zbx, "api"));
		s.ZabbixApiEnabled = Json.Bool(Json.Get(zapi, "enabled"), s.ZabbixApiEnabled);
		s.ZabbixApiUrl = Json.GetStr(zapi, "url", s.ZabbixApiUrl);
		s.ZabbixApiToken = Json.GetStr(zapi, "token", s.ZabbixApiToken);
		s.ZabbixAutoCreateHosts = Json.Bool(Json.Get(zapi, "autoCreateHosts"), s.ZabbixAutoCreateHosts);
		s.ZabbixHostGroup = Json.GetStr(zapi, "hostGroup", s.ZabbixHostGroup);
		s.ZabbixTemplateName = Json.GetStr(zapi, "templateName", s.ZabbixTemplateName);
		Dictionary<string, object> web = Json.AsObject(Json.Get(root, "web"));
		s.WebRequireAuth = Json.Bool(Json.Get(web, "requireAuth"), s.WebRequireAuth);
		s.WebSessionMinutes = Json.Int(Json.Get(web, "sessionTimeoutMinutes"), s.WebSessionMinutes);
		Dictionary<string, object> lg = Json.AsObject(Json.Get(root, "logging"));
		s.LogLevel = Json.GetStr(lg, "level", s.LogLevel);
		s.LogMaxMb = Json.Int(Json.Get(lg, "maxFileSizeMb"), s.LogMaxMb);
		s.LogKeepDays = Json.Int(Json.Get(lg, "keepDays"), s.LogKeepDays);
		return s;
	}
}
