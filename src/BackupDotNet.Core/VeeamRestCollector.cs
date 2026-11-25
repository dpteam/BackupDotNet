using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace BackupDotNet.Core;

public class VeeamRestCollector : ICollector
{
	private class Token
	{
		public string Value = "";

		public DateTime Expires = DateTime.MinValue;
	}

	private static readonly Dictionary<string, Token> TokenCache = new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase);

	private static readonly object TokenSync = new object();

	public string Type => "veeam-rest";

	public List<CollectResult> Collect(Company company, Source source, AgentCtx ctx)
	{
		List<CollectResult> list = new List<CollectResult>();
		string baseUrl = source.GetStr("url", "").TrimEnd('/');
		string user = source.GetStr("username", "");
		string secretKey = source.GetStr("passwordSecret", "");
		string password = ctx.Config.Secret(secretKey);
		string apiVersion = source.GetStr("apiVersion", ctx.Settings.VeeamApiVersion);
		bool ignoreCert = source.GetBool("ignoreCertErrors", ctx.Settings.VeeamIgnoreCertDefault);
		List<string> wanted = new List<string>();
		foreach (object o in source.GetArray("jobs"))
		{
			wanted.Add(Json.Str(o));
		}
		if (string.IsNullOrEmpty(baseUrl))
		{
			CollectResult bad = MakeResult(company, source, source.GetStr("jobName", source.Id));
			bad.Reachable = false;
			bad.Fail("не задан url источника");
			list.Add(bad);
			return list;
		}
		if (string.IsNullOrEmpty(password))
		{
			CollectResult bad2 = MakeResult(company, source, source.GetStr("jobName", source.Id));
			bad2.Reachable = false;
			bad2.Fail("не найден секрет «" + secretKey + "» в config/secrets.json");
			list.Add(bad2);
			return list;
		}
		int timeout = ctx.Settings.VeeamTimeoutSeconds;
		if (!GetToken(baseUrl, user, password, apiVersion, ignoreCert, timeout, out var token, out var err))
		{
			CollectResult bad3 = MakeResult(company, source, source.GetStr("jobName", source.Id));
			bad3.Reachable = false;
			bad3.Fail("не удалось авторизоваться в Veeam REST: " + err);
			list.Add(bad3);
			return list;
		}
		if (!Http(baseUrl + "/api/v1/jobs?limit=1000", token, apiVersion, ignoreCert, timeout, out var status, out var body, out err))
		{
			CollectResult bad4 = MakeResult(company, source, source.GetStr("jobName", source.Id));
			bad4.Reachable = false;
			bad4.Fail("GET /api/v1/jobs: " + err);
			list.Add(bad4);
			return list;
		}
		List<object> jobs = Json.AsArray(Json.Get(Json.ParseObject(body), "data"));
		Dictionary<string, string> jobIdByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, string> jobNameById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (object o2 in jobs)
		{
			Dictionary<string, object> j = Json.AsObject(o2);
			string id = Json.GetStr(j, "id", "");
			string name = Json.GetStr(j, "name", "");
			if (id.Length != 0 && name.Length != 0)
			{
				jobIdByName[name] = id;
				jobNameById[id] = name;
			}
		}
		int pageSize = source.GetInt("sessionPageSize", 500);
		int maxPages = source.GetInt("sessionMaxPages", 20);
		if (pageSize < 50)
		{
			pageSize = 50;
		}
		if (pageSize > 1000)
		{
			pageSize = 1000;
		}
		if (maxPages < 1)
		{
			maxPages = 1;
		}
		Dictionary<string, List<Dictionary<string, object>>> byJob = new Dictionary<string, List<Dictionary<string, object>>>(StringComparer.OrdinalIgnoreCase);
		int skip = 0;
		int totalSessions = -1;
		int fetched = 0;
		for (int page = 0; page < maxPages; page++)
		{
			string url = baseUrl + "/api/v1/sessions?limit=" + pageSize + "&skip=" + skip + "&orderColumn=creationTime&orderAsc=false";
			if (!Http(url, token, apiVersion, ignoreCert, timeout, out status, out body, out err))
			{
				if (page == 0)
				{
					CollectResult bad5 = MakeResult(company, source, source.GetStr("jobName", source.Id));
					bad5.Reachable = false;
					bad5.Fail("GET /api/v1/sessions: " + err);
					list.Add(bad5);
					return list;
				}
				Log.Warn("Veeam: страница сессий " + (page + 1) + " не получена: " + err);
				break;
			}
			Dictionary<string, object> pageObj = Json.ParseObject(body);
			List<object> pageData = Json.AsArray(Json.Get(pageObj, "data"));
			if (totalSessions < 0)
			{
				totalSessions = Json.Int(Json.Get(pageObj, "pagination.total"), 0);
			}
			if (pageData.Count == 0)
			{
				break;
			}
			foreach (object o3 in pageData)
			{
				Dictionary<string, object> s = Json.AsObject(o3);
				string jid = Json.GetStr(s, "jobId", "");
				if (jid.Length != 0)
				{
					if (!byJob.TryGetValue(jid, out var lst))
					{
						lst = (byJob[jid] = new List<Dictionary<string, object>>());
					}
					lst.Add(s);
				}
			}
			fetched += pageData.Count;
			skip += pageData.Count;
			if ((wanted.Count > 0 && AllHaveSuccess(wanted, jobIdByName, byJob)) || (totalSessions > 0 && fetched >= totalSessions))
			{
				break;
			}
		}
		Log.Debug("Veeam: получено сессий " + fetched + ((totalSessions > 0) ? (" из " + totalSessions) : ""));
		List<string> targets = ((wanted.Count > 0) ? wanted : new List<string>(jobIdByName.Keys));
		if (targets.Count == 0 && byJob.Count > 0)
		{
			Log.Debug("Veeam: /api/v1/jobs пуст, использую имена из сессий");
			HashSet<string> fromSessions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (KeyValuePair<string, List<Dictionary<string, object>>> kv in byJob)
			{
				string sname = Json.GetStr(kv.Value[0], "name", "");
				if (sname.Length != 0 && !IsTechnicalSession(Json.GetStr(kv.Value[0], "sessionType", "")) && !jobIdByName.ContainsKey(sname))
				{
					jobIdByName[sname] = kv.Key;
					fromSessions.Add(sname);
				}
			}
			targets = new List<string>(fromSessions);
			targets.Sort(StringComparer.OrdinalIgnoreCase);
		}
		List<string> allowedTypes = new List<string>();
		foreach (object o4 in source.GetArray("sessionTypes"))
		{
			allowedTypes.Add(Json.Str(o4));
		}
		foreach (string jobName in targets)
		{
			CollectResult r = MakeResult(company, source, jobName);
			if (!jobIdByName.TryGetValue(jobName, out var jid2))
			{
				r.Reachable = true;
				r.Message = "задание не найдено в Veeam";
				r.Fail("задание «" + jobName + "» отсутствует на сервере");
				list.Add(r);
				continue;
			}
			if (!byJob.TryGetValue(jid2, out var lst2) || lst2.Count == 0)
			{
				r.Reachable = true;
				r.Message = "нет сессий";
				list.Add(r);
				continue;
			}
			Dictionary<string, object> latest = lst2[0];
			string sessionType = Json.GetStr(latest, "sessionType", "");
			if (allowedTypes.Count > 0)
			{
				bool typeAllowed = false;
				foreach (string t in allowedTypes)
				{
					if (string.Equals(t, sessionType, StringComparison.OrdinalIgnoreCase))
					{
						typeAllowed = true;
					}
				}
				if (!typeAllowed)
				{
					continue;
				}
			}
			else if (IsTechnicalSession(sessionType))
			{
				continue;
			}
			r.LastAttempt = BackupRun.ParseDateNullable(Json.Get(latest, "creationTime"));
			ParseSessionResult(latest, out var result, out var resultMessage);
			string state = Json.GetStr(latest, "state", "");
			r.DurationSeconds = Duration(latest);
			foreach (Dictionary<string, object> s2 in lst2)
			{
				ParseSessionResult(s2, out var rRes, out var _);
				if (string.Equals(rRes, "Success", StringComparison.OrdinalIgnoreCase))
				{
					DateTime? t2 = BackupRun.ParseDateNullable(Json.Get(s2, "endTime"));
					if (!t2.HasValue)
					{
						t2 = BackupRun.ParseDateNullable(Json.Get(s2, "creationTime"));
					}
					r.LastSuccess = t2;
					break;
				}
			}
			int consecutive = 0;
			foreach (Dictionary<string, object> s3 in lst2)
			{
				ParseSessionResult(s3, out var rRes2, out var _);
				if (!string.Equals(rRes2, "Success", StringComparison.OrdinalIgnoreCase))
				{
					consecutive++;
					continue;
				}
				break;
			}
			if (string.Equals(state, "Working", StringComparison.OrdinalIgnoreCase))
			{
				r.Message = "выполняется сейчас";
				if (!r.LastSuccess.HasValue)
				{
					r.Message = "первый запуск ещё идёт";
				}
			}
			else if (string.Equals(result, "Warning", StringComparison.OrdinalIgnoreCase))
			{
				// Warning — это «задание отработало, но с замечаниями»: копия
				// создана. Поэтому время сессии считаем успехом (иначе проверка
				// свежести даёт FAIL и на проде это ложная тревога), а текст
				// замечания кладём в ошибки — планировщик сделает из этого WARN.
				if (!r.LastSuccess.HasValue)
				{
					DateTime? t3 = BackupRun.ParseDateNullable(Json.Get(latest, "endTime"));
					if (!t3.HasValue)
					{
						t3 = BackupRun.ParseDateNullable(Json.Get(latest, "creationTime"));
					}
					r.LastSuccess = t3;
				}
				string warnText = (resultMessage.Length > 0)
					? ("предупреждение Veeam: " + resultMessage)
					: "последняя сессия завершилась с предупреждением";
				r.Errors.Add(warnText);
				r.Message = warnText;
			}
			else if (string.Equals(result, "Failed", StringComparison.OrdinalIgnoreCase))
			{
				r.Fail((resultMessage.Length > 0) ? ("ошибка: " + resultMessage) : "последняя сессия завершилась ошибкой");
			}
			else if (string.Equals(result, "Success", StringComparison.OrdinalIgnoreCase))
			{
				r.Message = "успешно";
			}
			else if (string.IsNullOrEmpty(result))
			{
				r.Message = "результат неизвестен";
			}
			r.Target = "veeam://" + baseUrl.Replace("https://", "").Replace("http://", "") + "/" + jobName;
			r.SessionFailures = consecutive;
			list.Add(r);
		}
		return list;
	}

	private static void ParseSessionResult(Dictionary<string, object> session, out string result, out string message)
	{
		result = "";
		message = "";
		object raw = Json.Get(session, "result");
		if (raw != null)
		{
			if (raw is Dictionary<string, object> obj)
			{
				result = Json.GetStr(obj, "result", "");
				message = Json.GetStr(obj, "message", "");
			}
			else
			{
				result = Json.Str(raw);
			}
		}
	}

	private static bool IsTechnicalSession(string sessionType)
	{
		if (string.IsNullOrEmpty(sessionType))
		{
			return false;
		}
		switch (sessionType)
		{
		case "ConfigurationBackup":
		case "MalwareDetection":
		case "SureBackup":
		case "Update":
		case "HealthCheck":
			return true;
		default:
			return false;
		}
	}

	private static long Duration(Dictionary<string, object> session)
	{
		DateTime? start = BackupRun.ParseDateNullable(Json.Get(session, "creationTime"));
		DateTime? end = BackupRun.ParseDateNullable(Json.Get(session, "endTime"));
		if (start.HasValue && end.HasValue)
		{
			double sec = (end.Value - start.Value).TotalSeconds;
			if (sec > 0.0)
			{
				return (long)sec;
			}
		}
		return 0L;
	}

	private static bool AllHaveSuccess(List<string> wanted, Dictionary<string, string> jobIdByName, Dictionary<string, List<Dictionary<string, object>>> byJob)
	{
		foreach (string jobName in wanted)
		{
			if (!jobIdByName.TryGetValue(jobName, out var jid))
			{
				return false;
			}
			if (!byJob.TryGetValue(jid, out var lst))
			{
				return false;
			}
			bool found = false;
			foreach (Dictionary<string, object> s in lst)
			{
				ParseSessionResult(s, out var res, out var _);
				if (string.Equals(res, "Success", StringComparison.OrdinalIgnoreCase))
				{
					found = true;
					break;
				}
			}
			if (!found)
			{
				return false;
			}
		}
		return true;
	}

	private static CollectResult MakeResult(Company c, Source s, string job)
	{
		CollectResult r = new CollectResult();
		r.CompanyId = c.Id;
		r.CompanyName = c.Name;
		r.SourceId = s.Id;
		r.Kind = "veeam";
		r.Job = job;
		return r;
	}

	internal static bool GetToken(string baseUrl, string user, string password, string apiVersion, bool ignoreCert, int timeout, out string token, out string error)
	{
		token = "";
		error = "";
		string cacheKey = baseUrl + "|" + user;
		lock (TokenSync)
		{
			if (TokenCache.TryGetValue(cacheKey, out var t) && t.Expires > DateTime.UtcNow.AddMinutes(2.0))
			{
				token = t.Value;
				return true;
			}
		}
		string body = "grant_type=password&username=" + Uri.EscapeDataString(user) + "&password=" + Uri.EscapeDataString(password);
		if (!Http(baseUrl + "/api/oauth2/token", null, apiVersion, ignoreCert, timeout, out var _, out var resp, out error, body, "application/x-www-form-urlencoded"))
		{
			return false;
		}
		Dictionary<string, object> obj = Json.ParseObject(resp);
		string at = Json.GetStr(obj, "access_token", "");
		if (string.IsNullOrEmpty(at))
		{
			error = "в ответе нет access_token: " + Truncate(resp, 200);
			return false;
		}
		long expiresIn = Json.Long(Json.Get(obj, "expires_in"), 3600L);
		Token nt = new Token();
		nt.Value = at;
		nt.Expires = DateTime.UtcNow.AddSeconds(expiresIn);
		lock (TokenSync)
		{
			TokenCache[cacheKey] = nt;
		}
		token = at;
		return true;
	}

	internal static bool Http(string url, string token, string apiVersion, bool ignoreCert, int timeoutSec, out int status, out string body, out string error, string postBody = null, string contentType = null)
	{
		status = 0;
		body = "";
		error = "";
		try
		{
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
			HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
			req.Method = ((postBody == null) ? "GET" : "POST");
			req.Timeout = timeoutSec * 1000;
			req.ReadWriteTimeout = timeoutSec * 1000;
			req.KeepAlive = false;
			req.Accept = "application/json";
			if (ignoreCert)
			{
				req.ServerCertificateValidationCallback = (object param0, X509Certificate param1, X509Chain param2, SslPolicyErrors param3) => true;
			}
			if (!string.IsNullOrEmpty(apiVersion))
			{
				req.Headers["x-api-version"] = apiVersion;
			}
			if (!string.IsNullOrEmpty(token))
			{
				req.Headers["Authorization"] = "Bearer " + token;
			}
			if (postBody != null)
			{
				byte[] data = Encoding.UTF8.GetBytes(postBody);
				req.ContentType = contentType ?? "application/x-www-form-urlencoded";
				req.ContentLength = data.Length;
				using Stream s = req.GetRequestStream();
				s.Write(data, 0, data.Length);
			}
			using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
			{
				status = (int)resp.StatusCode;
				using StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8);
				body = sr.ReadToEnd();
			}
			return true;
		}
		catch (WebException ex)
		{
			if (ex.Response is HttpWebResponse resp2)
			{
				status = (int)resp2.StatusCode;
				try
				{
					using StreamReader sr2 = new StreamReader(resp2.GetResponseStream(), Encoding.UTF8);
					body = sr2.ReadToEnd();
				}
				catch
				{
				}
				error = "HTTP " + status + " " + Truncate(body, 300);
				return false;
			}
			error = ex.Message;
			return false;
		}
		catch (Exception ex2)
		{
			error = ex2.Message;
			return false;
		}
	}

	private static string Truncate(string s, int n)
	{
		if (string.IsNullOrEmpty(s))
		{
			return "";
		}
		if (s.Length > n)
		{
			return s.Substring(0, n) + "…";
		}
		return s;
	}

	public static bool TestConnection(string baseUrl, string user, string password, string apiVersion, bool ignoreCert, int timeoutSec, out string serverInfo, out string error)
	{
		serverInfo = "";
		error = "";
		baseUrl = (baseUrl ?? "").TrimEnd('/');
		if (string.IsNullOrEmpty(baseUrl))
		{
			error = "не задан адрес сервера";
			return false;
		}
		if (string.IsNullOrEmpty(user))
		{
			error = "не задан логин";
			return false;
		}
		if (string.IsNullOrEmpty(password))
		{
			error = "не задан пароль";
			return false;
		}
		if (!GetToken(baseUrl, user, password, apiVersion, ignoreCert, timeoutSec, out var token, out error))
		{
			return false;
		}
		if (!Http(baseUrl + "/api/v1/serverInfo", token, apiVersion, ignoreCert, timeoutSec, out var _, out var body, out error))
		{
			return false;
		}
		Dictionary<string, object> info = Json.ParseObject(body);
		string name = Json.GetStr(info, "name", "");
		string build = Json.GetStr(info, "buildVersion", "");
		string db = Json.GetStr(info, "databaseVendor", "");
		serverInfo = ((name.Length > 0) ? name : "Veeam") + ((build.Length > 0) ? (" " + build) : "") + ((db.Length > 0) ? (" (" + db + ")") : "");
		return true;
	}

	public static List<string> ListJobNames(string baseUrl, string user, string password, string apiVersion, bool ignoreCert, int timeoutSec, out string error)
	{
		List<string> names = new List<string>();
		error = "";
		baseUrl = (baseUrl ?? "").TrimEnd('/');
		if (!GetToken(baseUrl, user, password, apiVersion, ignoreCert, timeoutSec, out var token, out error))
		{
			return names;
		}
		if (Http(baseUrl + "/api/v1/jobs?limit=1000", token, apiVersion, ignoreCert, timeoutSec, out var status, out var body, out error))
		{
			List<object> jobs = Json.AsArray(Json.Get(Json.ParseObject(body), "data"));
			foreach (object o in jobs)
			{
				string n = Json.GetStr(Json.AsObject(o), "name", "");
				if (n.Length > 0 && !names.Contains(n))
				{
					names.Add(n);
				}
			}
		}
		else
		{
			error = "";
		}
		if (names.Count == 0)
		{
			if (Http(baseUrl + "/api/v1/sessions?limit=500", token, apiVersion, ignoreCert, timeoutSec, out status, out body, out var err2))
			{
				List<object> sessions = Json.AsArray(Json.Get(Json.ParseObject(body), "data"));
				foreach (object o2 in sessions)
				{
					Dictionary<string, object> s = Json.AsObject(o2);
					switch (Json.GetStr(s, "sessionType", ""))
					{
					case "ConfigurationBackup":
					case "MalwareDetection":
					case "Update":
					case "HealthCheck":
						continue;
					}
					string n2 = Json.GetStr(s, "name", "");
					if (n2.Length > 0 && !names.Contains(n2))
					{
						names.Add(n2);
					}
				}
			}
			else
			{
				error = err2;
			}
		}
		names.Sort(StringComparer.OrdinalIgnoreCase);
		return names;
	}
}
