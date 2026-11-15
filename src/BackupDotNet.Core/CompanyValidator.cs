using System;
using System.Collections.Generic;
using System.IO;

namespace BackupDotNet.Core;

/// <summary>
/// Проверка конфигурации компании до сохранения.
///
/// Отличие от проверки при сохранении (<c>Normalize</c> в веб-слое): здесь ошибки
/// привязаны к конкретным полям формы, чтобы интерфейс мог подсветить нужный
/// input, а не показать список «что-то не так». Плюс проверяются параметры
/// источников — самая частая причина «источник добавлен, но всегда NODATA».
///
/// Проверка ничего не сохраняет и не ходит в сеть: это статический разбор.
/// </summary>
public static class CompanyValidator
{
	/// <summary>Ошибка или предупреждение, привязанное к полю формы.</summary>
	public class Issue
	{
		/// <summary>Путь к полю в терминах формы: <c>name</c>, <c>policy.minSizeMb</c>, <c>src.0.host</c>.</summary>
		public string Field = "";

		/// <summary>Идентификатор источника (для группировки в интерфейсе).</summary>
		public string SourceId = "";

		public string Message = "";

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["field"] = Field;
			d["message"] = Message;
			if (!string.IsNullOrEmpty(SourceId))
			{
				d["sourceId"] = SourceId;
			}
			return d;
		}
	}

	/// <summary>Результат проверки.</summary>
	public class Result
	{
		public List<Issue> Errors = new List<Issue>();

		public List<Issue> Warnings = new List<Issue>();

		public bool Ok => Errors.Count == 0;

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["ok"] = Ok;
			List<object> errs = new List<object>();
			foreach (Issue i in Errors)
			{
				errs.Add(i.ToJson());
			}
			List<object> warns = new List<object>();
			foreach (Issue i in Warnings)
			{
				warns.Add(i.ToJson());
			}
			d["errors"] = errs;
			d["warnings"] = warns;
			d["errorCount"] = Errors.Count;
			d["warningCount"] = Warnings.Count;
			return d;
		}
	}

	/// <summary>
	/// Проверяет компанию. <paramref name="existingId"/> — идентификатор
	/// редактируемой компании (чтобы не считать «своё» имя занятым).
	/// </summary>
	public static Result Validate(Dictionary<string, object> payload, ConfigLoader config, string existingId)
	{
		Result res = new Result();
		if (payload == null)
		{
			Error(res, "id", "пустое тело запроса");
			return res;
		}

		// ---------------------------------------------------------- основное
		string id = Json.GetStr(payload, "id", "").Trim();
		string name = Json.GetStr(payload, "name", "").Trim();

		if (name.Length == 0)
		{
			Error(res, "name", "укажите название компании");
		}
		else if (name.Length > 120)
		{
			Error(res, "name", "название длиннее 120 символов — сократите");
		}

		if (id.Length == 0)
		{
			// Пустой id допустим: он сгенерируется из названия. Проверяем только явно заданный.
			if (name.Length > 0)
			{
				Warn(res, "id", "ID не задан — будет сгенерирован из названия");
			}
		}
		else
		{
			if (id.Length < 2)
			{
				Error(res, "id", "ID короче 2 символов");
			}
			foreach (char ch in id)
			{
				bool ok = (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '-' || ch == '_';
				if (!ok)
				{
					Error(res, "id", "в ID допустимы только a-z, 0-9, дефис и подчёркивание");
					break;
				}
			}
			// Занятость ID: разные компании не должны перетирать друг друга.
			if (config != null && !string.Equals(id, existingId, StringComparison.OrdinalIgnoreCase))
			{
				Company clash = config.Find(id);
				if (clash != null)
				{
					Error(res, "id", "ID уже занят компанией «" + clash.Name + "»");
				}
			}
		}

		string remoteAgent = Json.GetStr(payload, "remoteAgent", "").Trim();
		if (remoteAgent.Length > 0 && config != null)
		{
			// Проверяем, что такой удалённый агент вообще зарегистрирован.
			bool known = false;
			try
			{
				AgentRegistry reg = new AgentRegistry(config.ConfigDir);
				reg.Load();
				foreach (AgentRegistry.RemoteAgent a in reg.All)
				{
					if (string.Equals(a.Id, remoteAgent, StringComparison.OrdinalIgnoreCase))
					{
						known = true;
						break;
					}
				}
			}
			catch
			{
				known = true; // не смогли прочитать реестр — не мешаем сохранять
			}
			if (!known)
			{
				Error(res, "remoteAgent", "удалённый агент «" + remoteAgent + "» не зарегистрирован");
			}
		}

		// ---------------------------------------------------------- политика
		Dictionary<string, object> policy = Json.AsObject(Json.Get(payload, "policy"));
		int freshness = Json.Int(Json.Get(policy, "freshnessHours"), 26);
		if (freshness < 1 || freshness > 8760)
		{
			Error(res, "policy.freshnessHours", "свежесть должна быть от 1 до 8760 часов");
		}
		else if (freshness < 2)
		{
			Warn(res, "policy.freshnessHours", "свежесть меньше 2 часов — обычный бэкап раз в сутки будет выглядеть устаревшим");
		}
		int weekly = Json.Int(Json.Get(policy, "freshnessHoursWeekly"), 192);
		if (weekly > 0 && weekly < freshness)
		{
			Warn(res, "policy.freshnessHoursWeekly", "недельная свежесть меньше суточной — проверьте значения");
		}
		int minSize = Json.Int(Json.Get(policy, "minSizeMb"), 0);
		if (minSize < 0)
		{
			Error(res, "policy.minSizeMb", "минимальный размер не может быть отрицательным");
		}
		int fails = Json.Int(Json.Get(policy, "consecutiveFailuresToFail"), 2);
		if (fails < 1)
		{
			Error(res, "policy.consecutiveFailuresToFail", "порог неудач должен быть не меньше 1");
		}
		int drop = Json.Int(Json.Get(policy, "maxSizeDropPercent"), 0);
		if (drop < 0 || drop > 100)
		{
			Error(res, "policy.maxSizeDropPercent", "падение размера задаётся в процентах от 0 до 100");
		}

		// ---------------------------------------------------------- источники
		List<object> sources = Json.AsArray(Json.Get(payload, "sources"));
		if (sources.Count == 0)
		{
			// Совпадает с проверкой при сохранении: компания без источников не сохраняется.
			Error(res, "sources", "добавьте хотя бы один источник проверки");
		}
		HashSet<string> seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> seenJobs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		// Источники без ID получают ID = тип. Два источника одного типа без ID
		// дадут конфликт — предупреждаем только про такие, чтобы не шуметь зря.
		Dictionary<string, int> emptyIdByType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		foreach (object o in sources)
		{
			Dictionary<string, object> s = Json.AsObject(o);
			if (Json.GetStr(s, "id", "").Trim().Length > 0)
			{
				continue;
			}
			string t = Json.GetStr(s, "type", "").Trim();
			if (t.Length == 0)
			{
				continue;
			}
			emptyIdByType.TryGetValue(t, out var n);
			emptyIdByType[t] = n + 1;
		}

		for (int i = 0; i < sources.Count; i++)
		{
			Dictionary<string, object> src = Json.AsObject(sources[i]);
			ValidateSource(res, src, i, seenIds, seenJobs, emptyIdByType);
		}

		return res;
	}

	private static void ValidateSource(Result res, Dictionary<string, object> src, int index, HashSet<string> seenIds, HashSet<string> seenJobs, Dictionary<string, int> emptyIdByType)
	{
		string prefix = "src." + index + ".";
		string type = Json.GetStr(src, "type", "").Trim();
		string id = Json.GetStr(src, "id", "").Trim();
		string job = Json.GetStr(src, "jobName", "").Trim();
		string sid = id.Length > 0 ? id : (type.Length > 0 ? type : "#" + index);

		if (type.Length == 0)
		{
			Error(res, prefix + "type", "не выбран тип источника", sid);
			return;
		}
		if (Collectors.Get(type) == null)
		{
			Error(res, prefix + "type", "неизвестный тип источника: " + type, sid);
			return;
		}

		if (id.Length == 0)
		{
			emptyIdByType.TryGetValue(type, out var sameType);
			if (sameType > 1)
			{
				Warn(res, prefix + "id", "несколько источников типа " + type + " без ID — задайте ID, иначе они перезапишут друг друга", sid);
			}
		}
		else if (!seenIds.Add(id))
		{
			Error(res, prefix + "id", "ID источника повторяется: " + id, sid);
		}

		if (job.Length > 0 && !seenJobs.Add(job))
		{
			Error(res, prefix + "jobName", "имя задания повторяется: " + job, sid);
		}

		int interval = Json.Int(Json.Get(src, "intervalMinutes"), 0);
		if (interval < 0)
		{
			Error(res, prefix + "intervalMinutes", "интервал не может быть отрицательным", sid);
		}
		else if (interval > 0 && interval < 1)
		{
			Error(res, prefix + "intervalMinutes", "минимальный интервал — 1 минута", sid);
		}

		// --- параметры, специфичные для типа
		switch (type)
		{
		case "file-age":
		case "veeam-files":
			RequirePaths(res, src, prefix, sid);
			break;

		case "zip":
			RequireText(res, src, "path", prefix, sid, "укажите путь к архиву");
			break;

		case "veeam-rest":
			RequireUrl(res, src, "url", prefix, sid);
			RequireText(res, src, "username", prefix, sid, "укажите пользователя для REST API");
			WarnIfNoSecret(res, src, prefix, sid, "passwordSecret", "password", "пароль не задан");
			break;

		case "iperius-ftp":
			RequireText(res, src, "ftp.host", prefix, sid, "укажите FTP-сервер");
			WarnIfNoSecret(res, src, prefix, sid, "ftp.passwordSecret", "ftp.password", "пароль FTP не задан");
			break;

		case "http":
			RequireUrl(res, src, "url", prefix, sid);
			break;

		case "tcp":
			RequireText(res, src, "host", prefix, sid, "укажите хост");
			RequirePort(res, src, "port", prefix, sid);
			break;

		case "ping":
			RequireText(res, src, "host", prefix, sid, "укажите хост");
			break;

		case "dns":
			RequireText(res, src, "name", prefix, sid, "укажите имя для проверки");
			break;

		case "esxi":
			RequireText(res, src, "host", prefix, sid, "укажите хост ESXi");
			RequireText(res, src, "username", prefix, sid, "укажите пользователя");
			WarnIfNoSecret(res, src, prefix, sid, "passwordSecret", "password", "пароль не задан");
			break;

		case "mikrotik":
			RequireText(res, src, "host", prefix, sid, "укажите хост MikroTik");
			{
				string transport = Json.GetStr(src, "transport", "auto").ToLowerInvariant();
				if (transport != "auto" && transport != "api" && transport != "rest" && transport != "ssh" && transport != "snmp")
				{
					Error(res, prefix + "transport", "транспорт должен быть auto, api, rest, ssh или snmp", sid);
				}
				if (transport == "snmp" || transport == "auto")
				{
					string community = Json.GetStr(src, "snmpCommunity", "").Trim();
					if (transport == "snmp" && community.Length == 0)
					{
						Error(res, prefix + "snmpCommunity", "для SNMP нужна community-строка", sid);
					}
				}
			}
			break;

		case "hardware":
			{
				string bmc = Json.GetStr(src, "bmcHost", "").Trim();
				bool useIpmi = Json.Bool(Json.Get(src, "useIpmi"), dflt: true);
				if (useIpmi && bmc.Length == 0)
				{
					Warn(res, prefix + "bmcHost", "BMC не задан — останутся только датчики WMI", sid);
				}
				if (bmc.Length > 0)
				{
					WarnIfNoSecret(res, src, prefix, sid, "passwordSecret", "password", "пароль BMC не задан");
				}
			}
			break;

		case "inventory":
			{
				List<object> services = Json.AsArray(Json.Get(src, "services"));
				foreach (object o in services)
				{
					string svc = Json.Str(o).Trim();
					if (svc.Length == 0)
					{
						continue;
					}
					if (svc.IndexOf('*') >= 0 || svc.IndexOf('?') >= 0)
					{
						Error(res, prefix + "services", "в списке служб маски не поддерживаются: " + svc, sid);
					}
				}
			}
			break;

		case "push":
			{
				int stale = Json.Int(Json.Get(src, "staleMinutes"), 15);
				if (stale < 1)
				{
					Error(res, prefix + "staleMinutes", "порог молчания должен быть не меньше минуты", sid);
				}
			}
			break;

		case "smart":
		case "raid":
		case "vss":
		case "eventlog":
		case "netconfig":
		case "tasks":
		case "hyperv":
			// Параметры необязательные: коллекторы берут значения по умолчанию.
			break;
		}
	}

	// ------------------------------------------------------------- помощники

	private static void RequireText(Result res, Dictionary<string, object> src, string key, string prefix, string sid, string message)
	{
		string v = ReadPath(src, key).Trim();
		if (v.Length == 0)
		{
			Error(res, prefix + key, message, sid);
		}
	}

	private static void RequireUrl(Result res, Dictionary<string, object> src, string key, string prefix, string sid)
	{
		string v = ReadPath(src, key).Trim();
		if (v.Length == 0)
		{
			Error(res, prefix + key, "укажите адрес", sid);
			return;
		}
		if (!v.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
			!v.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			Error(res, prefix + key, "адрес должен начинаться с http:// или https://", sid);
		}
	}

	private static void RequirePort(Result res, Dictionary<string, object> src, string key, string prefix, string sid)
	{
		int port = Json.Int(Json.Get(src, key), 0);
		if (port <= 0 || port > 65535)
		{
			Error(res, prefix + key, "порт должен быть от 1 до 65535", sid);
		}
	}

	private static void RequirePaths(Result res, Dictionary<string, object> src, string prefix, string sid)
	{
		List<object> paths = Json.AsArray(Json.Get(src, "paths"));
		List<string> cleaned = new List<string>();
		foreach (object o in paths)
		{
			string p = Json.Str(o).Trim();
			if (p.Length > 0)
			{
				cleaned.Add(p);
			}
		}
		if (cleaned.Count == 0)
		{
			string single = Json.GetStr(src, "path", "").Trim();
			if (single.Length > 0)
			{
				cleaned.Add(single);
			}
		}
		if (cleaned.Count == 0)
		{
			Error(res, prefix + "paths", "укажите хотя бы один путь или маску", sid);
			return;
		}
		foreach (string p in cleaned)
		{
			if (p.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
			{
				Error(res, prefix + "paths", "недопустимые символы в пути: " + p, sid);
				break;
			}
			// Относительный путь считается от каталога агента — обычно это не то, что нужно.
			if (!Path.IsPathRooted(p) && p.IndexOf(':') < 0 && !p.StartsWith("\\\\", StringComparison.Ordinal))
			{
				Warn(res, prefix + "paths", "относительный путь считается от папки агента: " + p, sid);
			}
		}
	}

	private static void WarnIfNoSecret(Result res, Dictionary<string, object> src, string prefix, string sid, string secretKey, string plainKey, string message)
	{
		string secret = Json.GetStr(src, secretKey, "").Trim();
		string plain = Json.GetStr(src, plainKey, "").Trim();
		if (secret.Length == 0 && plain.Length == 0)
		{
			Warn(res, prefix + secretKey, message + " — проверка, скорее всего, не пройдёт", sid);
		}
		else if (plain.Length > 0 && secret.Length == 0)
		{
			Warn(res, prefix + plainKey, "пароль хранится открытым текстом — лучше вынести в секреты", sid);
		}
	}

	/// <summary>Читает значение по пути вида <c>ftp.host</c>.</summary>
	private static string ReadPath(Dictionary<string, object> src, string key)
	{
		int dot = key.IndexOf('.');
		if (dot < 0)
		{
			return Json.GetStr(src, key, "");
		}
		Dictionary<string, object> nested = Json.AsObject(Json.Get(src, key.Substring(0, dot)));
		return Json.GetStr(nested, key.Substring(dot + 1), "");
	}

	private static void Error(Result res, string field, string message, string sourceId = "")
	{
		res.Errors.Add(new Issue { Field = field, Message = message, SourceId = sourceId });
	}

	private static void Warn(Result res, string field, string message, string sourceId = "")
	{
		res.Warnings.Add(new Issue { Field = field, Message = message, SourceId = sourceId });
	}
}
