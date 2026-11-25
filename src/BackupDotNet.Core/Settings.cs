using System.Collections.Generic;
using BackupDotNet.Core.Backup;

namespace BackupDotNet.Core;

/// <summary>
/// Хранилище для проверки доступности: путь и подпись для интерфейса.
/// </summary>
public class StorageTarget
{
	public string Path = "";

	public string Label = "";
}
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

	/// <summary>
	/// Вести второй, «публичный» журнал: тот же поток, но с вычищенными
	/// секретами, адресами и путями. Его можно отдать вендору или приложить
	/// к тикету, не вычитывая построчно.
	/// </summary>
	public bool LogPublicEnabled = true;

	// ------------------------------------------------------------------ хранение истории

	/// <summary>Сколько дней хранить подробный журнал прогонов (файлы journal/yyyy-MM.ndjson).</summary>
	public int HistoryJournalKeepDays = 400;

	/// <summary>Сколько месяцев хранить суточные сводки (rollups/).</summary>
	public int HistoryRollupKeepMonths = 60;

	/// <summary>Удалять ли старое автоматически при запуске.</summary>
	public bool HistoryAutoPrune = true;

	// ------------------------------------------------------------------ обновления

	public bool UpdatesEnabled;

	public string UpdatesPath = "";

	public bool UpdatesCheckOnStart = true;

	// ------------------------------------------------------------------ обновления с GitHub

	/// <summary>Источник обновлений: <c>local</c> (папка) или <c>github</c>.</summary>
	public string UpdatesSource = "local";

	/// <summary>Репозиторий GitHub в виде <c>владелец/репозиторий</c>.</summary>
	public string UpdatesGitHubRepo = "";

	/// <summary>
	/// Адрес API. Пусто — <c>https://api.github.com</c>.
	/// Меняется для GitHub Enterprise и для проверки на локальной заглушке.
	/// </summary>
	public string UpdatesGitHubApiBase = "";

	/// <summary>Разрешить установку прямо из интерфейса (по умолчанию только скачивание).</summary>
	public bool UpdatesAllowInstall;

	/// <summary>Токен GitHub для приватных репозиториев. Пусто — анонимный доступ.</summary>
	public string UpdatesGitHubTokenSecret = "";

	public string UpdatesGitHubToken = "";

	// ------------------------------------------------------------------ бэкапы

	/// <summary>Куда складывать бэкапы по умолчанию. Пусто — dataDir\backups\store.</summary>
	public string BackupDestination = "";

	/// <summary>Политика хранения копий бэкапов.</summary>
	public RetentionPolicy BackupRetention = new RetentionPolicy();

	// ------------------------------------------------------------------ уведомления

	public bool NotifyEnabled;

	public bool NotifyOnFail = true;

	public bool NotifyOnWarn = true;

	public bool NotifyOnRecover = true;

	public bool NotifyOnNoData;

	public int NotifyMinIntervalMinutes = 30;

	public string NotifyQuietFrom = "";

	public string NotifyQuietTo = "";

	// SMTP
	public bool NotifySmtpEnabled;

	public string NotifySmtpHost = "";

	public int NotifySmtpPort = 25;

	public bool NotifySmtpUseSsl;

	public string NotifySmtpUser = "";

	public string NotifySmtpPassword = "";

	public string NotifySmtpPasswordSecret = "";

	public string NotifySmtpFrom = "";

	public List<string> NotifySmtpTo = new List<string>();

	public int NotifySmtpTimeoutSeconds = 20;

	// Telegram
	public bool NotifyTelegramEnabled;

	public string NotifyTelegramToken = "";

	public string NotifyTelegramTokenSecret = "";

	public List<string> NotifyTelegramChats = new List<string>();

	// Защита управления виртуальными машинами
	/// <summary>
	/// Шаблоны имён ВМ, которые нельзя гасить и удалять без явного force.
	/// Поддерживается <c>*</c> и <c>?</c>: например <c>prod-*</c>.
	/// </summary>
	public List<string> VmProtectedPatterns = new List<string>();

	/// <summary>Максимум машин в одной массовой операции.</summary>
	public int VmMaxBatchSize = 5;

	/// <summary>Сколько дней машина лежит в карантине до окончательного удаления.</summary>
	public int VmQuarantineDays = 7;

	/// <summary>Требовать force для действий над защищёнными ВМ.</summary>
	public bool VmRequireForceForProtected = true;

	/// <summary>
	/// Хранилища для проверки доступности: каталоги бэкапов, сетевые шары.
	/// </summary>
	public List<StorageTarget> StorageTargets = new List<StorageTarget>();

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
		s.LogPublicEnabled = Json.Bool(Json.Get(lg, "publicEnabled"), s.LogPublicEnabled);

		Dictionary<string, object> hist = Json.AsObject(Json.Get(root, "history"));
		s.HistoryJournalKeepDays = Json.Int(Json.Get(hist, "journalKeepDays"), s.HistoryJournalKeepDays);
		s.HistoryRollupKeepMonths = Json.Int(Json.Get(hist, "rollupKeepMonths"), s.HistoryRollupKeepMonths);
		s.HistoryAutoPrune = Json.Bool(Json.Get(hist, "autoPrune"), s.HistoryAutoPrune);

		Dictionary<string, object> upd = Json.AsObject(Json.Get(root, "updates"));
		s.UpdatesEnabled = Json.Bool(Json.Get(upd, "enabled"), s.UpdatesEnabled);
		s.UpdatesPath = Json.GetStr(upd, "path", s.UpdatesPath);
		s.UpdatesCheckOnStart = Json.Bool(Json.Get(upd, "checkOnStart"), s.UpdatesCheckOnStart);
		s.UpdatesSource = Json.GetStr(upd, "source", s.UpdatesSource);
		s.UpdatesGitHubRepo = Json.GetStr(upd, "githubRepo", s.UpdatesGitHubRepo);
		s.UpdatesGitHubApiBase = Json.GetStr(upd, "githubApiBase", s.UpdatesGitHubApiBase);
		s.UpdatesAllowInstall = Json.Bool(Json.Get(upd, "allowInstall"), s.UpdatesAllowInstall);
		s.UpdatesGitHubTokenSecret = Json.GetStr(upd, "githubTokenSecret", s.UpdatesGitHubTokenSecret);
		s.UpdatesGitHubToken = Json.GetStr(upd, "githubToken", s.UpdatesGitHubToken);

		Dictionary<string, object> bk = Json.AsObject(Json.Get(root, "backup"));
		s.BackupDestination = Json.GetStr(bk, "destination", s.BackupDestination);
		s.BackupRetention = RetentionPolicy.FromJson(Json.AsObject(Json.Get(bk, "retention")));

		Dictionary<string, object> vm = Json.AsObject(Json.Get(root, "vm"));
		s.VmProtectedPatterns = ReadStringList(Json.Get(vm, "protectedPatterns"));
		s.VmMaxBatchSize = Json.Int(Json.Get(vm, "maxBatchSize"), s.VmMaxBatchSize);
		s.VmQuarantineDays = Json.Int(Json.Get(vm, "quarantineDays"), s.VmQuarantineDays);
		s.VmRequireForceForProtected = Json.Bool(Json.Get(vm, "requireForceForProtected"), s.VmRequireForceForProtected);

		s.StorageTargets = ReadStorageTargets(Json.Get(root, "storage"));

		Dictionary<string, object> nt = Json.AsObject(Json.Get(root, "notifications"));
		s.NotifyEnabled = Json.Bool(Json.Get(nt, "enabled"), s.NotifyEnabled);
		Dictionary<string, object> on = Json.AsObject(Json.Get(nt, "on"));
		s.NotifyOnFail = Json.Bool(Json.Get(on, "fail"), s.NotifyOnFail);
		s.NotifyOnWarn = Json.Bool(Json.Get(on, "warn"), s.NotifyOnWarn);
		s.NotifyOnRecover = Json.Bool(Json.Get(on, "recover"), s.NotifyOnRecover);
		s.NotifyOnNoData = Json.Bool(Json.Get(on, "nodata"), s.NotifyOnNoData);
		s.NotifyMinIntervalMinutes = Json.Int(Json.Get(nt, "minIntervalMinutes"), s.NotifyMinIntervalMinutes);
		Dictionary<string, object> quiet = Json.AsObject(Json.Get(nt, "quietHours"));
		s.NotifyQuietFrom = Json.GetStr(quiet, "from", s.NotifyQuietFrom);
		s.NotifyQuietTo = Json.GetStr(quiet, "to", s.NotifyQuietTo);

		Dictionary<string, object> smtp = Json.AsObject(Json.Get(nt, "smtp"));
		s.NotifySmtpEnabled = Json.Bool(Json.Get(smtp, "enabled"), s.NotifySmtpEnabled);
		s.NotifySmtpHost = Json.GetStr(smtp, "host", s.NotifySmtpHost);
		s.NotifySmtpPort = Json.Int(Json.Get(smtp, "port"), s.NotifySmtpPort);
		s.NotifySmtpUseSsl = Json.Bool(Json.Get(smtp, "useSsl"), s.NotifySmtpUseSsl);
		s.NotifySmtpUser = Json.GetStr(smtp, "username", s.NotifySmtpUser);
		s.NotifySmtpPassword = Json.GetStr(smtp, "password", s.NotifySmtpPassword);
		s.NotifySmtpPasswordSecret = Json.GetStr(smtp, "passwordSecret", s.NotifySmtpPasswordSecret);
		s.NotifySmtpFrom = Json.GetStr(smtp, "from", s.NotifySmtpFrom);
		s.NotifySmtpTimeoutSeconds = Json.Int(Json.Get(smtp, "timeoutSeconds"), s.NotifySmtpTimeoutSeconds);
		s.NotifySmtpTo = ReadStringList(Json.Get(smtp, "to"));

		Dictionary<string, object> tg = Json.AsObject(Json.Get(nt, "telegram"));
		s.NotifyTelegramEnabled = Json.Bool(Json.Get(tg, "enabled"), s.NotifyTelegramEnabled);
		s.NotifyTelegramToken = Json.GetStr(tg, "token", s.NotifyTelegramToken);
		s.NotifyTelegramTokenSecret = Json.GetStr(tg, "tokenSecret", s.NotifyTelegramTokenSecret);
		s.NotifyTelegramChats = ReadStringList(Json.Get(tg, "chats"));

		return s;
	}

	/// <summary>
	/// Читает список хранилищ. Поддерживает два формата:
	/// массив объектов <c>[{"path":"\\\\srv\\share","label":"бэкапы"}]</c>
	/// и массив строк <c>["\\\\srv\\share"]</c> — так проще писать руками.
	/// </summary>
	private static List<StorageTarget> ReadStorageTargets(object value)
	{
		List<StorageTarget> list = new List<StorageTarget>();
		if (value == null)
		{
			return list;
		}

		Dictionary<string, object> obj = Json.AsObject(value);
		object items = obj.Count > 0 ? Json.Get(obj, "targets") : value;

		foreach (object o in Json.AsArray(items))
		{
			if (o == null)
			{
				continue;
			}
			// Строка — это путь без подписи.
			if (o is string plain)
			{
				string p = plain.Trim();
				if (p.Length > 0)
				{
					list.Add(new StorageTarget { Path = p });
				}
				continue;
			}

			Dictionary<string, object> d = Json.AsObject(o);
			if (d.Count == 0)
			{
				continue;
			}
			string path = Json.GetStr(d, "path", "").Trim();
			if (path.Length == 0)
			{
				continue;
			}
			list.Add(new StorageTarget
			{
				Path = path,
				Label = Json.GetStr(d, "label", "")
			});
		}
		return list;
	}

	/// <summary>
	/// Читает список строк: поддерживает и массив <c>["a","b"]</c>,
	/// и строку через запятую <c>"a, b"</c> — так удобнее править руками.
	/// </summary>
	private static List<string> ReadStringList(object value)
	{
		List<string> list = new List<string>();
		if (value == null)
		{
			return list;
		}
		// Важно различать «массив» и «строка»: пустой массив не должен
		// проваливаться в разбор строки (иначе получим "System.Object[]").
		// Парсер JSON отдаёт массивы как ArrayList или object[], не как List<object>.
		bool isArray = value is System.Collections.ArrayList || value is object[] || value is List<object>;
		if (isArray)
		{
			foreach (object o in Json.AsArray(value))
			{
				string s = Json.Str(o).Trim();
				if (s.Length > 0)
				{
					list.Add(s);
				}
			}
			return list;
		}
		string single = Json.Str(value).Trim();
		if (single.Length > 0)
		{
			foreach (string part in single.Split(','))
			{
				string s = part.Trim();
				if (s.Length > 0)
				{
					list.Add(s);
				}
			}
		}
		return list;
	}
}
