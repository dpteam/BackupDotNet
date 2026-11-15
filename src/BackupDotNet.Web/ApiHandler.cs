using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using BackupDotNet.Core;
using BackupDotNet.Deploy;
using BackupDotNet.Manage;

namespace BackupDotNet.Web;

public class ApiHandler
{
	private const string CookieName = "bc_sess";

	private readonly Settings _settings;

	private readonly ConfigLoader _config;

	private readonly DataStore _store;

	private readonly Scheduler _scheduler;

	private readonly Auth _auth;

	private readonly AuditLog _audit;

	private readonly AgentRegistry _agents;

	private readonly DeployManager _deploy;

	private readonly OpQueue _ops;

	private readonly string _prefix;

	private static readonly Dictionary<char, string> Translit = BuildTranslit();

	private static readonly Regex UserNameRe = new Regex("^[a-zA-Z0-9._-]{2,32}$", RegexOptions.Compiled);

	private string CompaniesDir => Path.Combine(_settings.ConfigDir, "companies");

	public ApiHandler(Settings settings, ConfigLoader config, DataStore store, Scheduler scheduler, Auth auth, AuditLog audit, AgentRegistry agents, DeployManager deploy, OpQueue ops, string prefix)
	{
		_settings = settings;
		_config = config;
		_store = store;
		_scheduler = scheduler;
		_auth = auth;
		_audit = audit;
		_agents = agents ?? new AgentRegistry(settings.ConfigDir);
		_deploy = deploy;
		_ops = ops;
		_prefix = prefix ?? "";
	}

	public void Handle(HttpListenerContext ctx)
	{
		string path = ctx.Request.Url.AbsolutePath.ToLowerInvariant();
		string method = ctx.Request.HttpMethod.ToUpperInvariant();
		try
		{
			if (path == "/api/health")
			{
				Dictionary<string, object> h = Json.Obj();
				h["ok"] = true;
				h["version"] = _scheduler.Version;
				h["time"] = DateTime.Now;
				WriteJson(ctx, h, 200);
				return;
			}
			if (path == "/api/login" && method == "POST")
			{
				HandleLogin(ctx);
				return;
			}
			if (path == "/api/branding")
			{
				HandleBranding(ctx);
				return;
			}			if (path == "/api/logout" && method == "POST")
			{
				_auth.Logout(Token(ctx));
				SetCookie(ctx, "", DateTime.Now.AddDays(-1.0));
				WriteJson(ctx, Json.Obj("ok", true), 200);
				return;
			}
			if (path.StartsWith("/api/push/"))
			{
				HandlePush(ctx, path.Substring("/api/push/".Length));
				return;
			}
			if (path == "/api/agent/config" && method == "GET")
			{
				HandleAgentConfig(ctx, Query(ctx, "token"), Query(ctx, "agent"));
				return;
			}
			if (path == "/api/agent/results" && method == "POST")
			{
				HandleAgentResults(ctx, Query(ctx, "token"));
				return;
			}
			SessionInfo session = null;
			if (_settings.WebRequireAuth)
			{
				session = _auth.Validate(Token(ctx));
				if (session == null)
				{
					WriteJson(ctx, Json.Obj("error", "unauthorized"), 401);
					return;
				}
			}
			else
			{
				session = new SessionInfo();
				session.User = "anonymous";
				session.Role = "admin";
			}
			switch (path)
			{
			case "/api/summary":
				HandleSummary(ctx, session);
				return;
			case "/api/month":
				HandleMonth(ctx, session);
				return;
			case "/api/companies":
				HandleCompanies(ctx, session);
				return;
			case "/api/me":
				HandleMe(ctx, session);
				return;
			case "/api/logs":
				HandleLogs(ctx, session);
				return;
			case "/api/logs/tail":
				HandleLogTail(ctx, session);
				return;
			case "/api/diagnostics":
				HandleDiagnostics(ctx, session);
				return;
			case "/api/notify/status":
				HandleNotifyStatus(ctx, session);
				return;
			case "/api/updates":
				HandleUpdates(ctx, session);
				return;			case "/api/users":
				if (method == "GET")
				{
					HandleUsersList(ctx, session);
					return;
				}
				break;
			}
			if (path == "/api/users" && method == "POST")
			{
				HandleUserCreate(ctx, session);
				return;
			}
			if (path.StartsWith("/api/users/") && method == "PUT")
			{
				HandleUserUpdate(ctx, session, path.Substring("/api/users/".Length));
				return;
			}
			if (path.StartsWith("/api/users/") && method == "DELETE")
			{
				HandleUserDelete(ctx, session, path.Substring("/api/users/".Length));
				return;
			}
			if (path.StartsWith("/api/users/") && path.EndsWith("/reset") && method == "POST")
			{
				string uid = path.Substring("/api/users/".Length);
				uid = uid.Substring(0, uid.Length - "/reset".Length);
				HandleUserReset(ctx, session, uid);
				return;
			}
			if (path == "/api/audit")
			{
				HandleAudit(ctx, session);
				return;
			}
			if (path == "/api/company" && method == "POST")
			{
				HandleCompanyCreate(ctx, session);
				return;
			}
			if (path == "/api/company/validate" && method == "POST")
			{
				HandleCompanyValidate(ctx, session);
				return;
			}
			if (path == "/api/companies/bulk" && method == "POST")
			{
				HandleCompaniesBulk(ctx, session);
				return;
			}
			if (path == "/api/companies/export" && method == "GET")
			{
				HandleCompaniesExport(ctx, session);
				return;
			}
			if (path == "/api/config/export" && method == "GET")
			{
				HandleConfigExport(ctx, session);
				return;
			}
			if (path == "/api/config/import" && method == "POST")
			{
				HandleConfigImport(ctx, session);
				return;
			}
			if (path == "/api/notify/test" && method == "POST")
			{
				HandleNotifyTest(ctx, session);
				return;
			}
			if (path == "/api/history/prune" && method == "POST")
			{
				HandleHistoryPrune(ctx, session);
				return;
			}
			if (path.StartsWith("/api/company/"))
			{
				string rest = path.Substring("/api/company/".Length);
				if (rest.EndsWith("/raw"))
				{
					string rid = rest.Substring(0, rest.Length - "/raw".Length);
					if (method == "GET")
					{
						HandleCompanyRaw(ctx, session, rid);
						return;
					}
					if (method == "PUT")
					{
						HandleCompanyRawSave(ctx, session, rid);
						return;
					}
				}
				if (rest.EndsWith("/rename") && method == "POST")
				{
					string rid2 = rest.Substring(0, rest.Length - "/rename".Length);
					HandleCompanyRename(ctx, session, rid2);
					return;
				}
				if (rest.EndsWith("/history") && method == "GET")
				{
					string rid3 = rest.Substring(0, rest.Length - "/history".Length);
					HandleCompanyHistory(ctx, session, rid3);
					return;
				}
				switch (method)
				{
				case "PUT":
					HandleCompanyUpdate(ctx, session, rest);
					return;
				case "DELETE":
					HandleCompanyDelete(ctx, session, rest);
					return;
				case "GET":
					HandleCompany(ctx, session, rest);
					return;
				}
			}
			if (path == "/api/agents" && method == "GET")
			{
				HandleAgentsList(ctx, session);
				return;
			}
			if (path == "/api/deploy/status" && method == "GET")
			{
				HandleDeployStatus(ctx, session);
				return;
			}
			if (path == "/api/deploy/credential" && method == "POST")
			{
				HandleDeployCredential(ctx, session);
				return;
			}
			if (path == "/api/deploy/scan" && method == "POST")
			{
				HandleDeployScan(ctx, session);
				return;
			}
			if (path == "/api/deploy/ad" && method == "POST")
			{
				HandleDeployAd(ctx, session);
				return;
			}
			if (path == "/api/deploy/probe" && method == "POST")
			{
				HandleDeployProbe(ctx, session);
				return;
			}
			if (path == "/api/deploy/test" && method == "POST")
			{
				HandleDeployTest(ctx, session);
				return;
			}
			if (path == "/api/deploy/install" && method == "POST")
			{
				HandleDeployInstall(ctx, session);
				return;
			}
			if (path == "/api/deploy/uninstall" && method == "POST")
			{
				HandleDeployUninstall(ctx, session);
				return;
			}
			if (path == "/api/deploy/jobs" && method == "GET")
			{
				HandleDeployJobs(ctx, session);
				return;
			}
			if (path == "/api/manage/catalog" && method == "GET")
			{
				HandleManageCatalog(ctx, session);
				return;
			}
			if (path == "/api/manage/targets" && method == "GET")
			{
				HandleManageTargets(ctx, session);
				return;
			}
			if (path == "/api/manage/submit" && method == "POST")
			{
				HandleManageSubmit(ctx, session);
				return;
			}
			if (path == "/api/manage/approve" && method == "POST")
			{
				HandleManageApprove(ctx, session);
				return;
			}
			if (path == "/api/manage/reject" && method == "POST")
			{
				HandleManageReject(ctx, session);
				return;
			}
			if (path == "/api/manage/jobs" && method == "GET")
			{
				HandleManageJobs(ctx, session);
				return;
			}
			if (path == "/api/manage/history" && method == "GET")
			{
				HandleManageHistory(ctx, session);
				return;
			}
			if (path == "/api/veeam/test" && method == "POST")
			{
				HandleVeeamTest(ctx);
				return;
			}
			switch (path)
			{
			case "/api/uptime":
				HandleUptime(ctx, session);
				return;
			case "/api/journal":
				HandleJournal(ctx, session);
				return;
			case "/api/day":
				HandleDay(ctx, session);
				return;
			case "/api/job-history":
				HandleJobHistory(ctx, session);
				return;
			case "/api/settings":
				HandleSettings(ctx, session);
				return;
			case "/api/branding/raw":
				if (method == "GET")
				{
					HandleBrandingRaw(ctx, session);
					return;
				}
				break;
			}
			if (path == "/api/branding/raw" && method == "PUT")
			{
				HandleBrandingSave(ctx, session);
				return;
			}
			if (path == "/api/export")
			{
				HandleExport(ctx, session);
				return;
			}
			if (path == "/api/check" && method == "POST")
			{
				HandleCheck(ctx, session);
				return;
			}
			if (path == "/api/password" && method == "POST")
			{
				HandlePassword(ctx);
				return;
			}
			WriteJson(ctx, Json.Obj("error", "not found"), 404);
		}
		catch (Exception ex)
		{
			Log.Error("API " + path, ex);
			WriteJson(ctx, Json.Obj("error", ex.Message), 500);
		}
	}

	private void HandleLogin(HttpListenerContext ctx)
	{
		Dictionary<string, object> body = ReadJson(ctx);
		string user = Json.GetStr(body, "username", "");
		string pass = Json.GetStr(body, "password", "");
		string token = _auth.Login(user, pass, out var error);
		if (token == null)
		{
			WriteJson(ctx, Json.Obj("error", (error.Length > 0) ? error : "Неверный логин или пароль"), 401);
			return;
		}
		SessionInfo si = _auth.Validate(token);
		SetCookie(ctx, token, DateTime.Now.AddMinutes(_settings.WebSessionMinutes));
		Dictionary<string, object> resp = Json.Obj();
		resp["ok"] = true;
		resp["user"] = user;
		resp["role"] = ((si != null) ? si.Role : "viewer");
		resp["mustChangePassword"] = si?.MustChangePassword ?? false;
		WriteJson(ctx, resp, 200);
		_audit.Write(user, "login", "", "");
	}

	private void HandlePassword(HttpListenerContext ctx)
	{
		SessionInfo si = _auth.Validate(Token(ctx));
		Dictionary<string, object> body = ReadJson(ctx);
		string user = Json.GetStr(body, "username", "");
		string pass = Json.GetStr(body, "password", "");
		if (si != null && !string.Equals(user, si.User, StringComparison.OrdinalIgnoreCase) && !Roles.CanManageUsers(si.Role))
		{
			WriteJson(ctx, Json.Obj("error", "Недостаточно прав"), 403);
		}
		else if (pass.Length < 8)
		{
			WriteJson(ctx, Json.Obj("error", "Пароль должен быть не короче 8 символов"), 400);
		}
		else if (!_auth.ChangePassword(user, pass))
		{
			WriteJson(ctx, Json.Obj("error", "Пользователь не найден"), 404);
		}
		else
		{
			_audit.Write((si != null) ? si.User : user, "password.change", user, "");
			WriteJson(ctx, Json.Obj("ok", true), 200);
		}
	}

	private static string Token(HttpListenerContext ctx)
	{
		Cookie c = ctx.Request.Cookies["bc_sess"];
		if (c != null)
		{
			return c.Value;
		}
		return "";
	}

	private static string Query(HttpListenerContext ctx, string name)
	{
		return QueryParam(ctx, name);
	}

	private static string QueryParam(HttpListenerContext ctx, string name)
	{
		try
		{
			string raw = ctx.Request.RawUrl ?? "";
			int q = raw.IndexOf('?');
			if (q < 0 || q + 1 >= raw.Length)
			{
				return "";
			}
			string qs = raw.Substring(q + 1);
			int hash = qs.IndexOf('#');
			if (hash >= 0)
			{
				qs = qs.Substring(0, hash);
			}
			string[] array = qs.Split('&');
			foreach (string pair in array)
			{
				if (pair.Length != 0)
				{
					int eq = pair.IndexOf('=');
					string key = ((eq >= 0) ? pair.Substring(0, eq) : pair);
					string val = ((eq >= 0) ? pair.Substring(eq + 1) : "");
					if (string.Equals(DecodeArg(key), name, StringComparison.OrdinalIgnoreCase))
					{
						return DecodeArg(val).Trim();
					}
				}
			}
			return "";
		}
		catch
		{
			return "";
		}
	}

	private static string DecodeArg(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return "";
		}
		try
		{
			return Uri.UnescapeDataString(s.Replace("+", " "));
		}
		catch
		{
			return s;
		}
	}

	private static void SetCookie(HttpListenerContext ctx, string value, DateTime expires)
	{
		string cookie = "bc_sess=" + value + "; Path=/; HttpOnly; SameSite=Lax";
		if (expires > DateTime.Now)
		{
			cookie = cookie + "; Expires=" + expires.ToUniversalTime().ToString("R", CultureInfo.InvariantCulture);
		}
		ctx.Response.Headers.Add("Set-Cookie", cookie);
	}

	private void HandleSummary(HttpListenerContext ctx, SessionInfo si)
	{
		List<JobState> states = _store.GetStates();
		Dictionary<string, List<JobState>> byCompany = new Dictionary<string, List<JobState>>(StringComparer.OrdinalIgnoreCase);
		int ok = 0;
		int warn = 0;
		int fail = 0;
		int nodata = 0;
		int skip = 0;
		foreach (JobState s in states)
		{
			if (si.CanSeeCompany(s.CompanyId))
			{
				if (!byCompany.TryGetValue(s.CompanyId, out var list))
				{
					list = new List<JobState>();
					byCompany[s.CompanyId] = list;
				}
				list.Add(s);
				switch (s.Status)
				{
				case BackupStatus.Ok:
					ok++;
					break;
				case BackupStatus.Warn:
					warn++;
					break;
				case BackupStatus.Fail:
					fail++;
					break;
				case BackupStatus.Skip:
					skip++;
					break;
				default:
					nodata++;
					break;
				}
			}
		}
		List<object> companies = new List<object>();
		int totalJobs = 0;
		foreach (Company c in _config.Companies)
		{
			if (!si.CanSeeCompany(c.Id))
			{
				continue;
			}
			if (!byCompany.TryGetValue(c.Id, out var jobs))
			{
				jobs = new List<JobState>();
			}
			BackupStatus worst = BackupStatus.Skip;
			int cok = 0;
			int cwarn = 0;
			int cfail = 0;
			int cnodata = 0;
			DateTime lastUpdate = DateTime.MinValue;
			long totalSize = 0L;
			foreach (JobState j in jobs)
			{
				worst = StatusNames.Worst(worst, j.Status);
				switch (j.Status)
				{
				case BackupStatus.Ok:
					cok++;
					break;
				case BackupStatus.Warn:
					cwarn++;
					break;
				case BackupStatus.Fail:
					cfail++;
					break;
				default:
					cnodata++;
					break;
				case BackupStatus.Skip:
					break;
				}
				if (j.Updated > lastUpdate)
				{
					lastUpdate = j.Updated;
				}
				totalSize += j.SizeBytes;
			}
			if (jobs.Count == 0)
			{
				worst = BackupStatus.NoData;
			}
			totalJobs += jobs.Count;
			Dictionary<string, object> d = Json.Obj();
			d["id"] = c.Id;
			d["name"] = c.Name;
			d["shortName"] = (string.IsNullOrEmpty(c.ShortName) ? c.Name : c.ShortName);
			d["tags"] = ToList(c.Tags);
			d["enabled"] = c.Enabled;
			d["status"] = StatusNames.Of(worst);
			d["jobs"] = jobs.Count;
			d["ok"] = cok;
			d["warn"] = cwarn;
			d["fail"] = cfail;
			d["nodata"] = cnodata;
			d["size"] = totalSize;
			d["lastUpdate"] = ((lastUpdate == DateTime.MinValue) ? null : ((object)lastUpdate));
			companies.Add(d);
		}
		Dictionary<string, object> agent = Json.Obj();
		agent["version"] = _scheduler.Version;
		agent["startedAt"] = _scheduler.StartedAt;
		agent["lastRunAt"] = (_scheduler.LastRunAt.HasValue ? ((object)_scheduler.LastRunAt.Value) : null);
		agent["lastRunCount"] = _scheduler.LastRunCount;
		agent["uptimeSec"] = (long)(DateTime.Now - _scheduler.StartedAt).TotalSeconds;
		Dictionary<string, object> totals = Json.Obj();
		totals["companies"] = _config.Companies.Count;
		totals["jobs"] = totalJobs;
		totals["ok"] = ok;
		totals["warn"] = warn;
		totals["fail"] = fail;
		totals["nodata"] = nodata;
		totals["skip"] = skip;
		Dictionary<string, object> result = Json.Obj();
		result["generatedAt"] = DateTime.Now;
		result["agent"] = agent;
		result["totals"] = totals;
		result["companies"] = companies;
		result["configErrors"] = ToList(_config.LoadErrors);
		WriteJson(ctx, result, 200);
	}

	private void HandleCompanies(HttpListenerContext ctx, SessionInfo si)
	{
		List<object> list = new List<object>();
		foreach (Company c in _config.Companies)
		{
			if (si.CanSeeCompany(c.Id))
			{
				Dictionary<string, object> d = Json.Obj();
				d["id"] = c.Id;
				d["name"] = c.Name;
				d["tags"] = ToList(c.Tags);
				d["enabled"] = c.Enabled;
				d["sources"] = c.Sources.Count;
				d["zabbixHost"] = (string.IsNullOrEmpty(c.Zabbix.Host) ? ("BC-" + c.Id) : c.Zabbix.Host);
				list.Add(d);
			}
		}
		WriteJson(ctx, Json.Obj("companies", list), 200);
	}

	private void HandleCompany(HttpListenerContext ctx, SessionInfo si, string id)
	{
		Company c = _config.Find(id);
		if (c == null)
		{
			WriteJson(ctx, Json.Obj("error", "Компания не найдена"), 404);
			return;
		}
		if (!si.CanSeeCompany(c.Id))
		{
			WriteJson(ctx, Json.Obj("error", "Нет доступа к этой компании"), 403);
			return;
		}
		List<object> jobs = new List<object>();
		foreach (JobState j in _store.GetStates())
		{
			if (string.Equals(j.CompanyId, c.Id, StringComparison.OrdinalIgnoreCase))
			{
				Dictionary<string, object> d = j.ToJson();
				if (j.LastSuccess.HasValue)
				{
					d["ageHours"] = Math.Round((DateTime.UtcNow - PolicyEngine.ToUtc(j.LastSuccess.Value)).TotalHours, 1);
				}
				jobs.Add(d);
			}
		}
		List<object> contacts = new List<object>();
		foreach (Contact ct in c.Contacts)
		{
			Dictionary<string, object> d2 = Json.Obj();
			d2["role"] = ct.Role;
			d2["name"] = ct.Name;
			d2["phone"] = ct.Phone;
			d2["email"] = ct.Email;
			contacts.Add(d2);
		}
		List<object> sources = new List<object>();
		foreach (Source s in c.Sources)
		{
			Dictionary<string, object> d3 = Json.Obj();
			d3["id"] = s.Id;
			d3["type"] = s.Type;
			d3["enabled"] = s.Enabled;
			sources.Add(d3);
		}
		List<object> history = new List<object>();
		foreach (BackupRun r in _store.GetJournal(c.Id, 100))
		{
			history.Add(r.ToJson());
		}
		Dictionary<string, object> policy = Json.Obj();
		policy["freshnessHours"] = c.Policy.FreshnessHours;
		policy["minSizeMb"] = c.Policy.MinSizeBytes / 1048576;
		policy["consecutiveFailuresToFail"] = c.Policy.ConsecutiveFailuresToFail;
		Dictionary<string, object> result = Json.Obj();
		result["id"] = c.Id;
		result["name"] = c.Name;
		result["shortName"] = c.ShortName;
		result["tags"] = ToList(c.Tags);
		result["contacts"] = contacts;
		result["sources"] = sources;
		result["policy"] = policy;
		result["jobs"] = jobs;
		result["history"] = history;
		result["zabbixHost"] = (string.IsNullOrEmpty(c.Zabbix.Host) ? ("BC-" + c.Id) : c.Zabbix.Host);
		if (!string.IsNullOrEmpty(c.RemoteAgent))
		{
			result["remoteAgent"] = c.RemoteAgent;
		}
		WriteJson(ctx, result, 200);
	}

	private void HandleMonth(HttpListenerContext ctx, SessionInfo si)
	{
		string month = QueryParam(ctx, "month");
		if (string.IsNullOrEmpty(month))
		{
			month = DateTime.Now.ToString("yyyy-MM", CultureInfo.InvariantCulture);
		}
		if (!DateTime.TryParseExact(month + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var first))
		{
			first = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
		}
		int days = DateTime.DaysInMonth(first.Year, first.Month);
		Dictionary<string, Dictionary<string, DayAgg>> data = _store.GetMonth(month);
		List<object> rows = new List<object>();
		foreach (Company c in _config.Companies)
		{
			if (!si.CanSeeCompany(c.Id))
			{
				continue;
			}
			if (!data.TryGetValue(c.Id, out var agg))
			{
				agg = new Dictionary<string, DayAgg>(StringComparer.OrdinalIgnoreCase);
			}
			Dictionary<string, object> row = Json.Obj();
			row["id"] = c.Id;
			row["name"] = c.Name;
			row["shortName"] = (string.IsNullOrEmpty(c.ShortName) ? c.Name : c.ShortName);
			Dictionary<string, object> cells = Json.Obj();
			BackupStatus worst = BackupStatus.Skip;
			for (int d = 1; d <= days; d++)
			{
				string key = month + "-" + d.ToString("00", CultureInfo.InvariantCulture);
				if (agg.TryGetValue(key, out var a))
				{
					cells[d.ToString(CultureInfo.InvariantCulture)] = StatusNames.Of(a.Worst);
					worst = StatusNames.Worst(worst, a.Worst);
				}
				else
				{
					cells[d.ToString(CultureInfo.InvariantCulture)] = null;
				}
			}
			row["days"] = cells;
			row["worst"] = StatusNames.Of(worst);
			rows.Add(row);
		}
		Dictionary<string, object> result = Json.Obj();
		result["month"] = month;
		result["days"] = days;
		result["firstDayOfWeek"] = (int)first.DayOfWeek;
		result["companies"] = rows;
		WriteJson(ctx, result, 200);
	}

	private void HandleJournal(HttpListenerContext ctx, SessionInfo si)
	{
		string company = QueryParam(ctx, "company");
		string dateText = QueryParam(ctx, "date");
		string statusText = QueryParam(ctx, "status").ToUpperInvariant();
		int limit = 200;
		string lim = QueryParam(ctx, "limit");
		if (!string.IsNullOrEmpty(lim))
		{
			int.TryParse(lim, out limit);
		}
		if (limit <= 0 || limit > 2000)
		{
			limit = 200;
		}
		if (!string.IsNullOrEmpty(company) && !si.CanSeeCompany(company))
		{
			WriteJson(ctx, Json.Obj("error", "Нет доступа к этой компании"), 403);
			return;
		}
		DateTime day = DateTime.MinValue;
		bool byDay = !string.IsNullOrEmpty(dateText) && DateTime.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day);
		List<BackupRun> runs = ((!byDay) ? _store.GetJournal(company, limit) : _store.GetDay(company, day, limit));
		List<object> list = new List<object>();
		foreach (BackupRun r in runs)
		{
			if (si.CanSeeCompany(r.CompanyId) && (statusText.Length <= 0 || string.Equals(StatusNames.Of(r.Status), statusText, StringComparison.OrdinalIgnoreCase)))
			{
				list.Add(r.ToJson());
			}
		}
		Dictionary<string, object> resp = Json.Obj();
		resp["items"] = list;
		if (byDay)
		{
			resp["date"] = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}
		WriteJson(ctx, resp, 200);
	}

	private void HandleMe(HttpListenerContext ctx, SessionInfo si)
	{
		Dictionary<string, object> d = Json.Obj();
		d["user"] = si.User;
		d["role"] = si.Role;
		d["roleDescription"] = Roles.Describe(si.Role);
		d["mustChangePassword"] = si.MustChangePassword;
		d["canWrite"] = Roles.CanWrite(si.Role);
		d["canManageUsers"] = Roles.CanManageUsers(si.Role);
		d["canDeleteCompanies"] = Roles.CanDeleteCompanies(si.Role);
		d["canEditBranding"] = si.Role == "admin";
		d["restricted"] = Roles.IsRestricted(si.Role);
		List<object> ids = new List<object>();
		foreach (string id in si.CompanyIds)
		{
			ids.Add(id);
		}
		d["companyIds"] = ids;
		WriteJson(ctx, d, 200);
	}

	/// <summary>
	/// GET /api/logs — список файлов журнала агента (только администратор).
	/// </summary>
	private void HandleLogs(HttpListenerContext ctx, SessionInfo si)
	{
		if (!Roles.CanManageUsers(si.Role))
		{
			WriteJson(ctx, Json.Obj("error", "недостаточно прав"), 403);
			return;
		}
		string dir = Log.Dir;
		if (string.IsNullOrEmpty(dir))
		{
			dir = Path.Combine(_settings.DataDir, "logs");
		}
		string current = Log.CurrentFile;
		List<object> files = new List<object>();
		foreach (FileInfo f in Log.ListFiles(dir))
		{
			Dictionary<string, object> d = Json.Obj();
			d["name"] = f.Name;
			d["size"] = f.Length;
			d["modified"] = f.LastWriteTime;
			d["current"] = string.Equals(f.FullName, current, StringComparison.OrdinalIgnoreCase);
			files.Add(d);
		}
		Dictionary<string, object> res = Json.Obj();
		res["dir"] = dir;
		res["current"] = string.IsNullOrEmpty(current) ? "" : Path.GetFileName(current);
		res["files"] = files;
		res["level"] = Log.Level.ToString().ToLowerInvariant();
		WriteJson(ctx, res, 200);
	}

	/// <summary>
	/// GET /api/logs/tail?name=BackupDotNet_....log&amp;lines=400 — хвост журнала.
	/// Имя файла всегда прогоняется через GetFileName: выйти за каталог журналов нельзя.
	/// </summary>
	private void HandleLogTail(HttpListenerContext ctx, SessionInfo si)
	{
		if (!Roles.CanManageUsers(si.Role))
		{
			WriteJson(ctx, Json.Obj("error", "недостаточно прав"), 403);
			return;
		}
		string dir = Log.Dir;
		if (string.IsNullOrEmpty(dir))
		{
			dir = Path.Combine(_settings.DataDir, "logs");
		}
		string name = Path.GetFileName(Query(ctx, "name"));
		if (name.Length == 0)
		{
			name = Path.GetFileName(Log.CurrentFile);
		}
		if (name.Length == 0)
		{
			WriteJson(ctx, Json.Obj("error", "журналы ещё не созданы"), 404);
			return;
		}
		string full = Path.GetFullPath(Path.Combine(dir, name));
		string root = Path.GetFullPath(dir);
		if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(full))
		{
			WriteJson(ctx, Json.Obj("error", "файл не найден"), 404);
			return;
		}
		int lines = Json.Int(Query(ctx, "lines"), 400);
		lines = Math.Max(50, Math.Min(5000, lines));
		Dictionary<string, object> res = Json.Obj();
		res["name"] = name;
		res["lines"] = lines;
		res["text"] = Log.Tail(full, lines, 1024 * 1024);
		WriteJson(ctx, res, 200);
	}

	/// <summary>
	/// GET /api/diagnostics — самодиагностика агента для страницы «Здоровье».
	/// Доступна всем авторизованным: смотреть полезно и оператору, и админу.
	/// </summary>
	private void HandleDiagnostics(HttpListenerContext ctx, SessionInfo si)
	{
		AgentCtx agentCtx = new AgentCtx(_settings, _config, _store);
		Dictionary<string, object> report = Diagnostics.Build(agentCtx, _scheduler, _prefix);
		WriteJson(ctx, report, 200);
	}

	/// <summary>
	/// GET /api/notify/status — состояние каналов уведомлений для страницы настроек.
	/// </summary>
	private void HandleNotifyStatus(HttpListenerContext ctx, SessionInfo si)
	{
		Notifier n = _scheduler?.Notifier;
		if (n == null)
		{
			WriteJson(ctx, Json.Obj("error", "планировщик не запущен"), 503);
			return;
		}
		WriteJson(ctx, n.Status(), 200);
	}

	/// <summary>
	/// POST /api/notify/test — пробное уведомление, чтобы проверить настройки
	/// до первой настоящей аварии. Только администратор.
	/// </summary>
	private void HandleNotifyTest(HttpListenerContext ctx, SessionInfo si)
	{
		if (!Roles.CanManageUsers(si.Role))
		{
			WriteJson(ctx, Json.Obj("error", "недостаточно прав"), 403);
			return;
		}
		Notifier n = _scheduler?.Notifier;
		if (n == null)
		{
			WriteJson(ctx, Json.Obj("error", "планировщик не запущен"), 503);
			return;
		}
		string err = n.SendTest();
		_audit.Write(si.User, "notify.test", err.Length == 0 ? "успешно" : err, ClientIp(ctx));
		Dictionary<string, object> resp = Json.Obj();
		resp["ok"] = err.Length == 0;
		if (err.Length > 0)
		{
			resp["error"] = err;
		}
		WriteJson(ctx, resp, err.Length == 0 ? 200 : 400);
	}

	/// <summary>
	/// POST /api/history/prune — прибрать историю по текущей политике.
	/// Полезно после смены настроек: не ждать следующего запуска агента.
	/// </summary>
	private void HandleHistoryPrune(HttpListenerContext ctx, SessionInfo si)
	{
		if (!Roles.CanManageUsers(si.Role))
		{
			WriteJson(ctx, Json.Obj("error", "недостаточно прав"), 403);
			return;
		}
		Dictionary<string, object> report = _store.PruneHistory(_settings.HistoryJournalKeepDays, _settings.HistoryRollupKeepMonths);
		_audit.Write(si.User, "history.prune",
			"файлов: " + Json.Int(Json.Get(report, "count")) + ", IP " + ClientIp(ctx), "history");
		report["storage"] = _store.StorageStats();
		WriteJson(ctx, report, 200);
	}

	/// <summary>
	/// GET /api/updates — есть ли в локальном каталоге версия новее текущей.
	/// Ничего не скачивает и не устанавливает: только сообщает факт.
	/// </summary>
	private void HandleUpdates(HttpListenerContext ctx, SessionInfo si)
	{
		string current = (_scheduler != null) ? _scheduler.Version : "";
		UpdateChecker.Result r = UpdateChecker.Check(_settings, current);
		WriteJson(ctx, r.ToJson(), 200);
	}

	private void HandleSettings(HttpListenerContext ctx, SessionInfo si)
	{
		Dictionary<string, object> d = Json.Obj();
		d["agentName"] = _settings.AgentName;
		d["dataDir"] = _settings.DataDir;
		d["publicDir"] = _settings.PublicDir;
		d["httpPort"] = _settings.HttpPort;
		d["timezone"] = _settings.Timezone;
		d["tickSeconds"] = _settings.TickSeconds;
		d["defaultIntervalMinutes"] = _settings.DefaultIntervalMinutes;
		d["maxParallelChecks"] = _settings.MaxParallelChecks;
		d["zabbixEnabled"] = _settings.ZabbixEnabled;
		d["zabbixServer"] = _settings.ZabbixServer;
		d["zabbixPort"] = _settings.ZabbixPort;
		d["zabbixApiEnabled"] = _settings.ZabbixApiEnabled;
		d["zabbixAutoCreateHosts"] = _settings.ZabbixAutoCreateHosts;
		d["webRequireAuth"] = _settings.WebRequireAuth;
		d["configErrors"] = ToList(_config.LoadErrors);
		d["collectorTypes"] = ToList(new List<string>(Collectors.Types));

		// --- политика хранения истории
		Dictionary<string, object> hist = Json.Obj();
		hist["journalKeepDays"] = _settings.HistoryJournalKeepDays;
		hist["rollupKeepMonths"] = _settings.HistoryRollupKeepMonths;
		hist["autoPrune"] = _settings.HistoryAutoPrune;
		hist["logKeepDays"] = _settings.LogKeepDays;
		d["history"] = hist;
		d["storage"] = _store.StorageStats();

		WriteJson(ctx, d, 200);
	}

	private void HandleExport(HttpListenerContext ctx, SessionInfo si)
	{
		string month = QueryParam(ctx, "month");
		if (string.IsNullOrEmpty(month))
		{
			month = DateTime.Now.ToString("yyyy-MM", CultureInfo.InvariantCulture);
		}
		if (!DateTime.TryParseExact(month + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var first))
		{
			first = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
		}
		int days = DateTime.DaysInMonth(first.Year, first.Month);
		Dictionary<string, Dictionary<string, DayAgg>> data = _store.GetMonth(month);
		StringBuilder sb = new StringBuilder();
		sb.Append("Компания;");
		for (int d = 1; d <= days; d++)
		{
			sb.Append(d).Append(';');
		}
		sb.AppendLine("Итог");
		foreach (Company c in _config.Companies)
		{
			data.TryGetValue(c.Id, out var agg);
			if (agg == null)
			{
				agg = new Dictionary<string, DayAgg>();
			}
			sb.Append(EscapeCsv(c.Name)).Append(';');
			BackupStatus worst = BackupStatus.Skip;
			for (int i = 1; i <= days; i++)
			{
				string key = month + "-" + i.ToString("00", CultureInfo.InvariantCulture);
				if (agg.TryGetValue(key, out var a))
				{
					sb.Append(StatusNames.Of(a.Worst));
					worst = StatusNames.Worst(worst, a.Worst);
				}
				sb.Append(';');
			}
			sb.AppendLine(StatusNames.Of(worst));
		}
		byte[] bytes = Encoding.UTF8.GetPreamble();
		byte[] body = Encoding.UTF8.GetBytes(sb.ToString());
		ctx.Response.ContentType = "text/csv; charset=utf-8";
		ctx.Response.AddHeader("Content-Disposition", "attachment; filename=\"backups-" + month + ".csv\"");
		ctx.Response.ContentLength64 = bytes.Length + body.Length;
		ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
		ctx.Response.OutputStream.Write(body, 0, body.Length);
		ctx.Response.Close();
	}

	private static string EscapeCsv(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return "";
		}
		if (s.IndexOf(';') >= 0 || s.IndexOf('"') >= 0 || s.IndexOf('\n') >= 0)
		{
			return "\"" + s.Replace("\"", "\"\"") + "\"";
		}
		return s;
	}

	private void HandleCheck(HttpListenerContext ctx, SessionInfo si)
	{
		if (!Roles.CanWrite(si.Role))
		{
			WriteJson(ctx, Json.Obj("error", "Недостаточно прав для запуска проверок"), 403);
			return;
		}
		Dictionary<string, object> body = ReadJson(ctx);
		string company = Json.GetStr(body, "company", "");
		if (!string.IsNullOrEmpty(company) && !si.CanSeeCompany(company))
		{
			WriteJson(ctx, Json.Obj("error", "Нет доступа к этой компании"), 403);
			return;
		}
		Company target = (string.IsNullOrEmpty(company) ? null : _config.Find(company));
		if (target != null && !string.IsNullOrEmpty(target.RemoteAgent))
		{
			if (!_agents.RequestRun(target.RemoteAgent))
			{
				WriteJson(ctx, Json.Obj("error", "Агент '" + target.RemoteAgent + "' не найден в config/agents.json"), 404);
				return;
			}
			WriteJson(ctx, Json.Obj("ok", true, "started", true, "remote", true, "agent", target.RemoteAgent, "note", "Агент выполнит проверку при следующем опросе (до 60 с)"), 202);
			return;
		}
		ThreadPool.QueueUserWorkItem((object param0) =>
		{
			try
			{
				if (string.IsNullOrEmpty(company))
				{
					_scheduler.RunAll();
				}
				else
				{
					_scheduler.RunCompany(company);
				}
			}
			catch (Exception ex)
			{
				Log.Error("Ручная проверка", ex);
			}
		});
		WriteJson(ctx, Json.Obj("ok", true, "started", true), 202);
	}

	private static List<object> ToList(IEnumerable<string> items)
	{
		List<object> list = new List<object>();
		foreach (string s in items)
		{
			list.Add(s);
		}
		return list;
	}

	private static Dictionary<string, object> ReadJson(HttpListenerContext ctx)
	{
		try
		{
			using StreamReader sr = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
			string text = sr.ReadToEnd();
			return Json.ParseObject(text);
		}
		catch
		{
			return Json.Obj();
		}
	}

	private static void WriteJson(HttpListenerContext ctx, object data, int status)
	{
		byte[] body = Encoding.UTF8.GetBytes(Json.Write(data));
		ctx.Response.StatusCode = status;
		ctx.Response.ContentType = "application/json; charset=utf-8";
		ctx.Response.ContentLength64 = body.Length;
		ctx.Response.Headers["Cache-Control"] = "no-store";
		ctx.Response.OutputStream.Write(body, 0, body.Length);
		ctx.Response.Close();
	}

	private void HandleAgentConfig(HttpListenerContext ctx, string token, string agentId)
	{
		AgentRegistry.RemoteAgent agent = _agents.ByToken(token);
		if (agent == null)
		{
			WriteJson(ctx, Json.Obj("error", "токен не принят"), 401);
			return;
		}
		if (agentId.Length > 0 && !string.Equals(agent.Id, agentId, StringComparison.OrdinalIgnoreCase))
		{
			WriteJson(ctx, Json.Obj("error", "токен принадлежит агенту '" + agent.Id + "', а запрошен '" + agentId + "'"), 403);
			return;
		}
		_agents.Touch(agent.Id, ClientIp(ctx), "", "");
		List<Company> assigned = AssignedCompanies(agent);
		List<object> companies = new List<object>();
		HashSet<string> secretKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Company c in assigned)
		{
			Dictionary<string, object> raw = ReadCompanyRaw(c.Id);
			if (raw != null)
			{
				companies.Add(raw);
				CollectSecretKeys(raw, secretKeys);
			}
		}
		List<object> secrets = new List<object>();
		foreach (string key in secretKeys)
		{
			string value = _config.Secret(key);
			if (value.Length != 0)
			{
				Dictionary<string, object> s = Json.Obj();
				s["key"] = key;
				s["value"] = value;
				secrets.Add(s);
			}
		}
		Dictionary<string, object> resp = Json.Obj();
		resp["agent"] = agent.Id;
		resp["name"] = agent.Name;
		resp["version"] = _scheduler.Version;
		resp["serverTime"] = DateTime.Now;
		resp["companies"] = companies;
		resp["secrets"] = secrets;
		resp["runNow"] = _agents.ConsumeRunNow(agent.Id);
		Log.Info("Агент " + agent.Id + ": выдана конфигурация (" + companies.Count + " компаний, " + secrets.Count + " секретов)");
		WriteJson(ctx, resp, 200);
	}

	private void HandleAgentResults(HttpListenerContext ctx, string token)
	{
		AgentRegistry.RemoteAgent agent = _agents.ByToken(token);
		if (agent == null)
		{
			WriteJson(ctx, Json.Obj("error", "токен не принят"), 401);
			return;
		}
		Dictionary<string, object> body = ReadJson(ctx);
		string agentId = Json.GetStr(body, "agentId", agent.Id);
		string version = Json.GetStr(body, "version", "");
		if (!string.Equals(agentId, agent.Id, StringComparison.OrdinalIgnoreCase))
		{
			WriteJson(ctx, Json.Obj("error", "agentId '" + agentId + "' не совпадает с токеном ('" + agent.Id + "')"), 403);
			return;
		}
		List<object> results = Json.AsArray(Json.Get(body, "results"));
		if (results.Count == 0)
		{
			_agents.Touch(agent.Id, ClientIp(ctx), version, "");
			WriteJson(ctx, Json.Obj("ok", true, "accepted", 0), 200);
			return;
		}
		Dictionary<string, List<Dictionary<string, object>>> byCompany = new Dictionary<string, List<Dictionary<string, object>>>(StringComparer.OrdinalIgnoreCase);
		foreach (object o in results)
		{
			Dictionary<string, object> d = Json.AsObject(o);
			string companyId = Json.GetStr(d, "company", "");
			if (companyId.Length != 0)
			{
				if (!byCompany.TryGetValue(companyId, out var list))
				{
					list = (byCompany[companyId] = new List<Dictionary<string, object>>());
				}
				list.Add(d);
			}
		}
		int accepted = 0;
		List<string> errors = new List<string>();
		foreach (KeyValuePair<string, List<Dictionary<string, object>>> kv in byCompany)
		{
			Company company = _config.Find(kv.Key);
			if (company == null)
			{
				errors.Add(kv.Key + ": компания не найдена на центральном агенте");
				continue;
			}
			if (company.RemoteAgent.Length > 0 && !string.Equals(company.RemoteAgent, agent.Id, StringComparison.OrdinalIgnoreCase))
			{
				errors.Add(kv.Key + ": назначена агенту '" + company.RemoteAgent + "', а не '" + agent.Id + "'");
				continue;
			}
			Dictionary<string, List<CollectResult>> bySource = new Dictionary<string, List<CollectResult>>(StringComparer.OrdinalIgnoreCase);
			HashSet<string> reportedJobs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (Dictionary<string, object> d2 in kv.Value)
			{
				CollectResult cr = CollectResult.FromJson(d2);
				cr.CompanyId = company.Id;
				cr.CompanyName = company.Name;
				string sourceId = cr.SourceId;
				if (sourceId.Length == 0)
				{
					sourceId = cr.Kind;
				}
				if (!bySource.TryGetValue(sourceId, out var list3))
				{
					list3 = (bySource[sourceId] = new List<CollectResult>());
				}
				list3.Add(cr);
			}
			foreach (KeyValuePair<string, List<CollectResult>> src in bySource)
			{
				Source source = FindSource(company, src.Key, src.Value[0].Kind);
				if (source == null)
				{
					errors.Add(kv.Key + ": источник '" + src.Key + "' не найден в конфигурации");
					continue;
				}
				try
				{
					_scheduler.Ingest(company, source, src.Value);
					accepted += src.Value.Count;
					foreach (CollectResult cr2 in src.Value)
					{
						reportedJobs.Add(cr2.Job);
					}
				}
				catch (Exception ex)
				{
					errors.Add(kv.Key + "/" + src.Key + ": " + ex.Message);
					Log.Error("Приём от агента " + agent.Id, ex);
				}
			}
			if (reportedJobs.Count > 0)
			{
				_store.PruneJobs(company.Id, reportedJobs);
			}
		}
		_agents.Touch(agent.Id, ClientIp(ctx), version, (errors.Count > 0) ? string.Join("; ", errors.ToArray()) : "");
		Log.Info("Агент " + agent.Id + ": принято результатов " + accepted + ((errors.Count > 0) ? (", ошибок " + errors.Count) : ""));
		Dictionary<string, object> resp = Json.Obj();
		resp["ok"] = true;
		resp["accepted"] = accepted;
		resp["errors"] = ToObjectList(errors);
		WriteJson(ctx, resp, 200);
	}

	private void HandleAgentsList(HttpListenerContext ctx, SessionInfo si)
	{
		if (!Roles.CanWrite(si.Role) && si.Role != "viewer")
		{
			WriteJson(ctx, Json.Obj("error", "Недостаточно прав"), 403);
			return;
		}
		List<object> list = new List<object>();
		foreach (AgentRegistry.RemoteAgent a in _agents.All)
		{
			Dictionary<string, object> d = a.ToJson();
			int count = 0;
			foreach (Company c in _config.Companies)
			{
				if (string.Equals(c.RemoteAgent, a.Id, StringComparison.OrdinalIgnoreCase))
				{
					count++;
				}
			}
			d["companyCount"] = count;
			list.Add(d);
		}
		Dictionary<string, object> resp = Json.Obj();
		resp["agents"] = list;
		WriteJson(ctx, resp, 200);
	}

	private List<Company> AssignedCompanies(AgentRegistry.RemoteAgent agent)
	{
		List<Company> list = new List<Company>();
		foreach (Company c in _config.Companies)
		{
			if (!string.Equals(c.RemoteAgent, agent.Id, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			bool hasEnabled = false;
			foreach (Source s in c.Sources)
			{
				if (s.Enabled)
				{
					hasEnabled = true;
					break;
				}
			}
			if (hasEnabled)
			{
				list.Add(c);
			}
		}
		return list;
	}

	private Dictionary<string, object> ReadCompanyRaw(string id)
	{
		string path = Path.Combine(CompaniesDir, id + ".json");
		if (!File.Exists(path))
		{
			return null;
		}
		try
		{
			Dictionary<string, object> raw = Json.ParseObject(File.ReadAllText(path));
			StripRuntimeFields(raw);
			return raw;
		}
		catch (Exception ex)
		{
			Log.Warn("Агент: не удалось прочитать " + id + ": " + ex.Message);
			return null;
		}
	}

	private static void CollectSecretKeys(Dictionary<string, object> company, HashSet<string> keys)
	{
		foreach (object o in Json.AsArray(Json.Get(company, "sources")))
		{
			Dictionary<string, object> src = Json.AsObject(o);
			string key = Json.GetStr(src, "passwordSecret", "");
			if (key.Length > 0)
			{
				keys.Add(key);
			}
			Dictionary<string, object> ftp = Json.AsObject(Json.Get(src, "ftp"));
			string fkey = Json.GetStr(ftp, "passwordSecret", "");
			if (fkey.Length > 0)
			{
				keys.Add(fkey);
			}
		}
	}

	private static Source FindSource(Company company, string sourceId, string kind)
	{
		foreach (Source s in company.Sources)
		{
			if (string.Equals(s.Id, sourceId, StringComparison.OrdinalIgnoreCase))
			{
				return s;
			}
		}
		foreach (Source s2 in company.Sources)
		{
			if (string.Equals(s2.Type, kind, StringComparison.OrdinalIgnoreCase))
			{
				return s2;
			}
		}
		return null;
	}

	private static string ClientIp(HttpListenerContext ctx)
	{
		try
		{
			IPEndPoint ep = ctx.Request.RemoteEndPoint;
			if (ep != null)
			{
				return ep.Address.ToString();
			}
		}
		catch
		{
		}
		return "";
	}

	private void HandleBranding(HttpListenerContext ctx)
	{
		Branding b = Branding.Load(_settings.ConfigDir);
		foreach (string err in b.LoadErrors)
		{
			Log.Warn("Бренд: " + err);
		}
		Dictionary<string, object> resp = b.ToJson();
		resp["ok"] = true;
		ctx.Response.Headers["Cache-Control"] = "no-cache";
		WriteJson(ctx, resp, 200);
	}

	private void HandleBrandingRaw(HttpListenerContext ctx, SessionInfo si)
	{
		if (si.Role != "admin")
		{
			WriteJson(ctx, Json.Obj("error", "Только для администратора"), 403);
			return;
		}
		Branding b = Branding.Load(_settings.ConfigDir);
		string text = ((!File.Exists(b.FilePath)) ? Json.WritePretty(b.ToJson()) : File.ReadAllText(b.FilePath, Encoding.UTF8));
		Dictionary<string, object> resp = Json.Obj();
		resp["path"] = b.FilePath;
		resp["exists"] = File.Exists(b.FilePath);
		resp["json"] = text;
		resp["effective"] = b.ToJson();
		WriteJson(ctx, resp, 200);
	}

	private void HandleBrandingSave(HttpListenerContext ctx, SessionInfo si)
	{
		if (si.Role != "admin")
		{
			WriteJson(ctx, Json.Obj("error", "Только для администратора"), 403);
			return;
		}
		Dictionary<string, object> body = ReadJson(ctx);
		string text = Json.GetStr(body, "json", "");
		if (text.Trim().Length == 0)
		{
			WriteJson(ctx, Json.Obj("error", "Пустой JSON"), 400);
			return;
		}
		Dictionary<string, object> parsed;
		try
		{
			parsed = Json.ParseObject(text);
		}
		catch (Exception ex)
		{
			WriteJson(ctx, Json.Obj("error", "Некорректный JSON: " + ex.Message), 400);
			return;
		}
		if (parsed.Count == 0)
		{
			WriteJson(ctx, Json.Obj("error", "JSON пустой — нужен объект"), 400);
			return;
		}
		Branding b = Branding.Load(_settings.ConfigDir);
		Directory.CreateDirectory(Path.GetDirectoryName(b.FilePath));
		try
		{
			ConfigLoader.AtomicWrite(b.FilePath, Json.WritePretty(parsed));
		}
		catch (Exception ex2)
		{
			WriteJson(ctx, Json.Obj("error", "Не удалось записать: " + ex2.Message), 500);
			return;
		}
		Branding.Invalidate();
		_audit.Write(si.User, "branding.save", "branding.json", "");
		Branding after = Branding.Load(_settings.ConfigDir);
		Dictionary<string, object> resp = Json.Obj();
		resp["ok"] = true;
		resp["effective"] = after.ToJson();
		resp["warnings"] = ToObjectList(after.LoadErrors);
		WriteJson(ctx, resp, 200);
	}

	private static Dictionary<char, string> BuildTranslit()
	{
		Dictionary<char, string> m = new Dictionary<char, string>();
		string[] ru = new string[33]
		{
			"а", "б", "в", "г", "д", "е", "ё", "ж", "з", "и",
			"й", "к", "л", "м", "н", "о", "п", "р", "с", "т",
			"у", "ф", "х", "ц", "ч", "ш", "щ", "ъ", "ы", "ь",
			"э", "ю", "я"
		};
		string[] lat = new string[33]
		{
			"a", "b", "v", "g", "d", "e", "e", "zh", "z", "i",
			"y", "k", "l", "m", "n", "o", "p", "r", "s", "t",
			"u", "f", "h", "c", "ch", "sh", "sch", "", "y", "",
			"e", "yu", "ya"
		};
		for (int i = 0; i < ru.Length; i++)
		{
			m[ru[i][0]] = lat[i];
		}
		return m;
	}

	public static string Slugify(string source)
	{
		if (string.IsNullOrEmpty(source))
		{
			return "";
		}
		StringBuilder sb = new StringBuilder(source.Length);
		string text = source.ToLowerInvariant();
		foreach (char raw in text)
		{
			char c = raw;
			if (Translit.TryGetValue(c, out var tr))
			{
				sb.Append(tr);
			}
			else if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
			{
				sb.Append(c);
			}
			else if (c == '-' || c == '_' || c == ' ' || c == '.' || c == '/')
			{
				sb.Append('-');
			}
		}
		string s = sb.ToString();
		while (s.IndexOf("--") >= 0)
		{
			s = s.Replace("--", "-");
		}
		s = s.Trim('-');
		if (s.Length > 40)
		{
			s = s.Substring(0, 40).Trim('-');
		}
		return s;
	}

	private void HandleCompanyCreate(HttpListenerContext ctx, SessionInfo si)
	{
		if (!Roles.CanWrite(si.Role))
		{
			WriteJson(ctx, Json.Obj("error", "Недостаточно прав для создания компаний"), 403);
			return;
		}
		Dictionary<string, object> payload = ReadJson(ctx);
		Dictionary<string, object> normalized = Normalize(payload, null, out var errors);
		if (errors.Count > 0)
		{
			WriteJson(ctx, Json.Obj("error", "Проверьте поля", "errors", ToObjectList(errors)), 400);
			return;
		}
		string id = Json.GetStr(normalized, "id", "");
		string path = Path.Combine(CompaniesDir, id + ".json");
		if (File.Exists(path))
		{
			WriteJson(ctx, Json.Obj("error", "Компания с таким ID уже существует: " + id), 409);
			return;
		}
		List<string> secretWarnings = ExtractSecrets(normalized, id, null);
		StripRuntimeFields(normalized);
		Directory.CreateDirectory(CompaniesDir);
		ConfigLoader.AtomicWrite(path, Json.WritePretty(normalized));
		_config.Load();
		string zabbixNote = EnsureZabbixHostFor(id);
		Log.Info("Создана компания " + id + " (" + Json.GetStr(normalized, "name", "") + ")");
		Dictionary<string, object> resp = Json.Obj();
		resp["ok"] = true;
		resp["id"] = id;
		resp["warnings"] = ToObjectList(secretWarnings);
		if (zabbixNote.Length > 0)
		{
			resp["zabbix"] = zabbixNote;
		}
		WriteJson(ctx, resp, 201);
	}

	private void HandleCompanyValidate(HttpListenerContext ctx, SessionInfo si)
	{
		Dictionary<string, object> payload = ReadJson(ctx);
		Dictionary<string, object> normalized = Normalize(payload, null, out var errors);
		string existingId = Query(ctx, "id").Trim();
		CompanyValidator.Result check = CompanyValidator.Validate(payload, _config, existingId);

		Dictionary<string, object> resp = Json.Obj();
		Dictionary<string, object> fields = check.ToJson();
		// ok = и проверка сохранения, и структурная проверка чисты.
		resp["ok"] = errors.Count == 0 && check.Ok;
		resp["errors"] = ToObjectList(errors);
		resp["fields"] = fields["errors"];
		resp["warnings"] = fields["warnings"];
		resp["id"] = Json.GetStr(normalized, "id", "");
		WriteJson(ctx, resp, 200);
	}

	private void HandleCompanyUpdate(HttpListenerContext ctx, SessionInfo si, string id)
	{
		if (!Roles.CanWrite(si.Role))
		{
			WriteJson(ctx, Json.Obj("error", "Недостаточно прав для изменения компаний"), 403);
			return;
		}
		id = Slugify(Uri.UnescapeDataString(id ?? ""));
		string path = Path.Combine(CompaniesDir, id + ".json");
		if (!File.Exists(path))
		{
			WriteJson(ctx, Json.Obj("error", "Компания не найдена: " + id), 404);
			return;
		}
		if (!si.CanSeeCompany(id))
		{
			WriteJson(ctx, Json.Obj("error", "Нет доступа к этой компании"), 403);
			return;
		}
		Dictionary<string, object> existing = new Dictionary<string, object>();
		try
		{
			existing = Json.ParseObject(File.ReadAllText(path));
		}
		catch
		{
		}
		Dictionary<string, object> payload = ReadJson(ctx);
		payload["id"] = id;
		Dictionary<string, object> normalized = Normalize(payload, existing, out var errors);
		if (errors.Count > 0)
		{
			WriteJson(ctx, Json.Obj("error", "Проверьте поля", "errors", ToObjectList(errors)), 400);
			return;
		}
		List<string> secretWarnings = ExtractSecrets(normalized, id, existing);
		StripRuntimeFields(normalized);
		ConfigLoader.AtomicWrite(path, Json.WritePretty(normalized));
		_config.Load();
		string zabbixNote = EnsureZabbixHostFor(id);
		Log.Info("Обновлена компания " + id);
		Dictionary<string, object> resp = Json.Obj();
		resp["ok"] = true;
		resp["id"] = id;
		resp["warnings"] = ToObjectList(secretWarnings);
		if (zabbixNote.Length > 0)
		{
			resp["zabbix"] = zabbixNote;
		}
		WriteJson(ctx, resp, 200);
	}

	private void HandleCompanyDelete(HttpListenerContext ctx, SessionInfo si, string id)
	{
		if (!Roles.CanDeleteCompanies(si.Role))
		{
			WriteJson(ctx, Json.Obj("error", "Удалять компании может только администратор"), 403);
			return;
		}
		id = Slugify(Uri.UnescapeDataString(id ?? ""));
		string path = Path.Combine(CompaniesDir, id + ".json");
		if (!File.Exists(path))
		{
			WriteJson(ctx, Json.Obj("error", "Компания не найдена: " + id), 404);
			return;
		}
		string archiveDir = Path.Combine(CompaniesDir, "_archive");
		Directory.CreateDirectory(archiveDir);
		string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
		string archive = Path.Combine(archiveDir, id + "-" + stamp + ".json");
		try
		{
			File.Move(path, archive);
		}
		catch (Exception ex)
		{
			WriteJson(ctx, Json.Obj("error", "Не удалось удалить: " + ex.Message), 500);
			return;
		}
		_config.Load();
		Log.Info("Удалена компания " + id + " (в архив: " + archive + ")");
		WriteJson(ctx, Json.Obj("ok", true, "id", id, "archived", archive), 200);
	}

	private void HandleCompanyRaw(HttpListenerContext ctx, SessionInfo si, string id)
	{
		id = Slugify(Uri.UnescapeDataString(id ?? ""));
		string path = Path.Combine(CompaniesDir, id + ".json");
		if (!File.Exists(path))
		{
			WriteJson(ctx, Json.Obj("error", "Компания не найдена: " + id), 404);
			return;
		}
		try
		{
			Dictionary<string, object> raw = Json.ParseObject(File.ReadAllText(path));
			List<object> sources = Json.AsArray(Json.Get(raw, "sources"));
			foreach (object o in sources)
			{
				Dictionary<string, object> src = Json.AsObject(o);
				string key = Json.GetStr(src, "passwordSecret", "");
				src["hasPassword"] = key.Length > 0 && _config.Secret(key).Length > 0;
				src["passwordSecretKey"] = key;
				Dictionary<string, object> ftp = Json.AsObject(Json.Get(src, "ftp"));
				if (ftp.Count > 0)
				{
					string fkey = Json.GetStr(ftp, "passwordSecret", "");
					ftp["hasPassword"] = fkey.Length > 0 && _config.Secret(fkey).Length > 0;
					ftp["passwordSecretKey"] = fkey;
				}
				string sshKey = Json.GetStr(src, "sshPasswordSecret", "");
				if (sshKey.Length > 0)
				{
					src["hasSshPassword"] = _config.Secret(sshKey).Length > 0;
				}
			}
			WriteJson(ctx, raw, 200);
		}
		catch (Exception ex)
		{
			WriteJson(ctx, Json.Obj("error", "Не удалось прочитать файл: " + ex.Message), 500);
		}
	}

	private void HandleVeeamTest(HttpListenerContext ctx)
	{
		Dictionary<string, object> body = ReadJson(ctx);
		string url = Json.GetStr(body, "url", "");
		string user = Json.GetStr(body, "username", "");
		string apiVersion = Json.GetStr(body, "apiVersion", _settings.VeeamApiVersion);
		bool ignoreCert = Json.Bool(Json.Get(body, "ignoreCertErrors"), _settings.VeeamIgnoreCertDefault);
		int timeout = _settings.VeeamTimeoutSeconds;
		string password = Json.GetStr(body, "password", "");
		if (password.Length == 0)
		{
			string secretKey = Json.GetStr(body, "passwordSecret", "");
			if (secretKey.Length > 0)
			{
				password = _config.Secret(secretKey);
			}
		}
		Dictionary<string, object> resp = Json.Obj();
		if (password.Length == 0)
		{
			resp["ok"] = false;
			resp["error"] = "Не задан пароль";
			WriteJson(ctx, resp, 200);
			return;
		}
		if (!VeeamRestCollector.TestConnection(url, user, password, apiVersion, ignoreCert, timeout, out var serverInfo, out var error))
		{
			resp["ok"] = false;
			resp["error"] = error;
			WriteJson(ctx, resp, 200);
			return;
		}
		List<string> jobs = VeeamRestCollector.ListJobNames(url, user, password, apiVersion, ignoreCert, timeout, out var jobsError);
		resp["ok"] = true;
		resp["server"] = serverInfo;
		resp["jobs"] = ToObjectList(jobs);
		if (jobsError.Length > 0)
		{
			resp["jobsError"] = jobsError;
		}
		WriteJson(ctx, resp, 200);
	}

	private Dictionary<string, object> Normalize(Dictionary<string, object> payload, Dictionary<string, object> existing, out List<string> errors)
	{
		errors = new List<string>();
		Dictionary<string, object> result = Json.Obj();
		string rawId = Json.GetStr(payload, "id", "");
		string name = Json.GetStr(payload, "name", "").Trim();
		if (rawId.Length == 0 && existing != null)
		{
			rawId = Json.GetStr(existing, "id", "");
		}
		if (rawId.Length == 0 && name.Length > 0)
		{
			rawId = Slugify(name);
		}
		string id = Slugify(rawId);
		if (id.Length < 2)
		{
			errors.Add("ID компании: нужно минимум 2 символа (a-z, 0-9, дефис)");
		}
		if (name.Length == 0)
		{
			errors.Add("Название компании обязательно");
		}
		result["id"] = id;
		result["name"] = name;
		string shortName = Json.GetStr(payload, "shortName", "").Trim();
		result["shortName"] = ((shortName.Length > 0) ? shortName : name);
		List<object> tags = new List<object>();
		foreach (object t in Json.AsArray(Json.Get(payload, "tags")))
		{
			string s = Json.Str(t).Trim();
			if (s.Length > 0 && !tags.Contains(s))
			{
				tags.Add(s);
			}
		}
		result["tags"] = tags;
		result["timezone"] = Json.GetStr(payload, "timezone", "Europe/Moscow");
		result["enabled"] = Json.Bool(Json.Get(payload, "enabled"), dflt: true);
		string remoteAgent = Json.GetStr(payload, "remoteAgent", "").Trim();
		if (remoteAgent.Length > 0)
		{
			result["remoteAgent"] = remoteAgent;
		}
		List<object> contacts = new List<object>();
		foreach (object o in Json.AsArray(Json.Get(payload, "contacts")))
		{
			Dictionary<string, object> c = Json.AsObject(o);
			string cname = Json.GetStr(c, "name", "").Trim();
			string cphone = Json.GetStr(c, "phone", "").Trim();
			string cemail = Json.GetStr(c, "email", "").Trim();
			if (cname.Length != 0 || cphone.Length != 0 || cemail.Length != 0)
			{
				Dictionary<string, object> nc = Json.Obj();
				nc["role"] = Json.GetStr(c, "role", "admin");
				nc["name"] = cname;
				nc["phone"] = cphone;
				nc["email"] = cemail;
				contacts.Add(nc);
			}
		}
		result["contacts"] = contacts;
		List<object> sources = new List<object>();
		int sourceIndex = 0;
		foreach (object o2 in Json.AsArray(Json.Get(payload, "sources")))
		{
			sourceIndex++;
			Dictionary<string, object> src = Json.AsObject(o2);
			string type = Json.GetStr(src, "type", "").ToLowerInvariant();
			if (type.Length != 0)
			{
				Dictionary<string, object> ns = NormalizeSource(src, type, sourceIndex, errors);
				if (ns != null)
				{
					sources.Add(ns);
				}
			}
		}
		result["sources"] = sources;
		if (sources.Count == 0)
		{
			errors.Add("Добавьте хотя бы один источник проверки");
		}
		Dictionary<string, object> pol = Json.AsObject(Json.Get(payload, "policy"));
		Dictionary<string, object> np = Json.Obj();
		np["freshnessHours"] = Json.Int(Json.Get(pol, "freshnessHours"), 26);
		np["minSizeMb"] = Json.Long(Json.Get(pol, "minSizeMb"), 0L);
		np["consecutiveFailuresToFail"] = Json.Int(Json.Get(pol, "consecutiveFailuresToFail"), 2);
		np["maxSizeDropPercent"] = Json.Int(Json.Get(pol, "maxSizeDropPercent"), 0);
		result["policy"] = np;
		Dictionary<string, object> zbx = Json.AsObject(Json.Get(payload, "zabbix"));
		Dictionary<string, object> nz = Json.Obj();
		nz["enabled"] = Json.Bool(Json.Get(zbx, "enabled"), dflt: true);
		string zhost = Json.GetStr(zbx, "host", "");
		nz["host"] = ((zhost.Length > 0) ? zhost : ("BC-" + id));
		string zdisplay = Json.GetStr(zbx, "displayName", "").Trim();
		nz["displayName"] = ((zdisplay.Length > 0) ? zdisplay : (name + " — бэкапы"));
		Dictionary<string, object> macros = Json.AsObject(Json.Get(zbx, "macros"));
		Dictionary<string, object> nm = Json.Obj();
		foreach (KeyValuePair<string, object> kv in macros)
		{
			string v = Json.Str(kv.Value);
			if (v.Length > 0)
			{
				nm[kv.Key] = v;
			}
		}
		if (nm.Count > 0)
		{
			nz["macros"] = nm;
		}
		result["zabbix"] = nz;
		return result;
	}

	private Dictionary<string, object> NormalizeSource(Dictionary<string, object> src, string type, int index, List<string> errors)
	{
		Dictionary<string, object> ns = Json.Obj();
		ns["type"] = type;
		string sid = Json.GetStr(src, "id", "");
		if (sid.Length == 0)
		{
			sid = type + "-" + index;
		}
		ns["id"] = Slugify(sid);
		ns["enabled"] = Json.Bool(Json.Get(src, "enabled"), dflt: true);
		string comment = Json.GetStr(src, "comment", "");
		if (comment.Length > 0)
		{
			ns["comment"] = comment;
		}
		int interval = Json.Int(Json.Get(src, "intervalMinutes"), 0);
		if (interval > 0)
		{
			ns["intervalMinutes"] = interval;
		}
		switch (type)
		{
		case "veeam-rest":
		{
			string url = Json.GetStr(src, "url", "").Trim();
			string user = Json.GetStr(src, "username", "").Trim();
			if (url.Length == 0)
			{
				errors.Add("Veeam: укажите адрес сервера");
			}
			if (user.Length == 0)
			{
				errors.Add("Veeam: укажите логин");
			}
			ns["url"] = url.TrimEnd('/');
			ns["username"] = user;
			ns["apiVersion"] = Json.GetStr(src, "apiVersion", "1.1-rev1");
			ns["ignoreCertErrors"] = Json.Bool(Json.Get(src, "ignoreCertErrors"), dflt: true);
			List<object> jobs = new List<object>();
			foreach (object j in Json.AsArray(Json.Get(src, "jobs")))
			{
				string js = Json.Str(j).Trim();
				if (js.Length > 0)
				{
					jobs.Add(js);
				}
			}
			ns["jobs"] = jobs;
			List<object> types = new List<object>();
			foreach (object t in Json.AsArray(Json.Get(src, "sessionTypes")))
			{
				string ts = Json.Str(t).Trim();
				if (ts.Length > 0)
				{
					types.Add(ts);
				}
			}
			if (types.Count == 0)
			{
				types.Add("BackupJob");
				types.Add("BackupCopyJob");
				types.Add("ReplicaJob");
			}
			ns["sessionTypes"] = types;
			ns["sessionPageSize"] = Json.Int(Json.Get(src, "sessionPageSize"), 500);
			ns["sessionMaxPages"] = Json.Int(Json.Get(src, "sessionMaxPages"), 20);
			string pwd = Json.GetStr(src, "password", "");
			if (pwd.Length > 0)
			{
				ns["password"] = pwd;
			}
			string secretKey = Json.GetStr(src, "passwordSecret", "");
			if (secretKey.Length > 0)
			{
				ns["passwordSecret"] = secretKey;
			}
			break;
		}
		case "iperius-ftp":
		{
			Dictionary<string, object> ftp = Json.AsObject(Json.Get(src, "ftp"));
			string host = Json.GetStr(ftp, "host", "").Trim();
			if (host.Length == 0)
			{
				errors.Add("FTP: укажите хост");
			}
			Dictionary<string, object> nf = Json.Obj();
			nf["host"] = host;
			nf["port"] = Json.Int(Json.Get(ftp, "port"), 21);
			nf["username"] = Json.GetStr(ftp, "username", "anonymous");
			nf["remotePath"] = Json.GetStr(ftp, "remotePath", "/");
			nf["useTls"] = Json.Bool(Json.Get(ftp, "useTls"), dflt: false);
			nf["pattern"] = Json.GetStr(ftp, "pattern", "*.zip");
			nf["maxFileAgeHours"] = Json.Int(Json.Get(ftp, "maxFileAgeHours"), 26);
			nf["keepLocalCopy"] = Json.Bool(Json.Get(ftp, "keepLocalCopy"), dflt: false);
			string fpwd = Json.GetStr(ftp, "password", "");
			if (fpwd.Length > 0)
			{
				nf["password"] = fpwd;
			}
			string fkey = Json.GetStr(ftp, "passwordSecret", "");
			if (fkey.Length > 0)
			{
				nf["passwordSecret"] = fkey;
			}
			ns["ftp"] = nf;
			break;
		}
		case "zip":
		{
			List<object> paths = ReadPaths(src);
			if (paths.Count == 0)
			{
				errors.Add("ZIP: укажите хотя бы один путь");
			}
			ns["jobName"] = JobName(src, "Архивы ZIP");
			ns["paths"] = paths;
			ns["checkCrc"] = Json.GetStr(src, "checkCrc", "weekly");
			break;
		}
		case "file-age":
		{
			List<object> paths2 = ReadPaths(src);
			if (paths2.Count == 0)
			{
				errors.Add("Файлы: укажите хотя бы один путь");
			}
			ns["jobName"] = JobName(src, "Файлы");
			ns["paths"] = paths2;
			ns["maxAgeHours"] = Json.Int(Json.Get(src, "maxAgeHours"), 26);
			break;
		}
		case "veeam-files":
		{
			List<object> paths3 = ReadPaths(src);
			if (paths3.Count == 0)
			{
				errors.Add("Veeam (файлы): укажите хотя бы один путь");
			}
			ns["jobName"] = JobName(src, "Veeam (файлы)");
			ns["paths"] = paths3;
			ns["maxAgeHours"] = Json.Int(Json.Get(src, "maxAgeHours"), 26);
			break;
		}
		case "http":
		{
			string url2 = Json.GetStr(src, "url", "").Trim();
			if (url2.Length == 0)
			{
				errors.Add("HTTP: укажите URL");
			}
			else if (!url2.StartsWith("http://") && !url2.StartsWith("https://"))
			{
				errors.Add("HTTP: URL должен начинаться с http:// или https://");
			}
			ns["jobName"] = JobName(src, (url2.Length > 0) ? url2 : "HTTP");
			ns["url"] = url2;
			ns["method"] = Json.GetStr(src, "method", "GET").ToUpperInvariant();
			ns["keyword"] = Json.GetStr(src, "keyword", "");
			ns["ignoreCertErrors"] = Json.Bool(Json.Get(src, "ignoreCertErrors"), dflt: false);
			ns["timeoutSeconds"] = Json.Int(Json.Get(src, "timeoutSeconds"), 10);
			ns["slowWarnMs"] = Json.Int(Json.Get(src, "slowWarnMs"), 0);
			ns["slowFailMs"] = Json.Int(Json.Get(src, "slowFailMs"), 0);
			List<object> codes = new List<object>();
			object rawCodes = Json.Get(src, "expectedStatus");
			if (rawCodes is string)
			{
				string[] array = ((string)rawCodes).Split(',');
				foreach (string part in array)
				{
					if (int.TryParse(part.Trim(), out var code) && code > 0)
					{
						codes.Add(code);
					}
				}
			}
			else
			{
				foreach (object o in Json.AsArray(rawCodes))
				{
					int code2 = Json.Int(o);
					if (code2 > 0)
					{
						codes.Add(code2);
					}
				}
			}
			if (codes.Count == 0)
			{
				codes.Add(200);
			}
			ns["expectedStatus"] = codes;
			break;
		}
		case "tcp":
		{
			string host5 = Json.GetStr(src, "host", "").Trim();
			int port = Json.Int(Json.Get(src, "port"), 0);
			if (host5.Length == 0)
			{
				errors.Add("TCP: укажите хост");
			}
			if (port <= 0 || port > 65535)
			{
				errors.Add("TCP: некорректный порт");
			}
			ns["jobName"] = JobName(src, host5 + ":" + port);
			ns["host"] = host5;
			ns["port"] = port;
			ns["timeoutSeconds"] = Json.Int(Json.Get(src, "timeoutSeconds"), 5);
			ns["slowWarnMs"] = Json.Int(Json.Get(src, "slowWarnMs"), 0);
			ns["slowFailMs"] = Json.Int(Json.Get(src, "slowFailMs"), 0);
			break;
		}
		case "ping":
		{
			string host3 = Json.GetStr(src, "host", "").Trim();
			if (host3.Length == 0)
			{
				errors.Add("Ping: укажите хост");
			}
			ns["jobName"] = JobName(src, host3);
			ns["host"] = host3;
			ns["attempts"] = Json.Int(Json.Get(src, "attempts"), 3);
			ns["timeoutMs"] = Json.Int(Json.Get(src, "timeoutMs"), 3000);
			ns["slowWarnMs"] = Json.Int(Json.Get(src, "slowWarnMs"), 0);
			ns["slowFailMs"] = Json.Int(Json.Get(src, "slowFailMs"), 0);
			break;
		}
		case "dns":
		{
			string name = Json.GetStr(src, "name", "").Trim();
			if (name.Length == 0)
			{
				errors.Add("DNS: укажите имя");
			}
			ns["jobName"] = JobName(src, name);
			ns["name"] = name;
			ns["recordType"] = Json.GetStr(src, "recordType", "A").ToUpperInvariant();
			ns["expect"] = Json.GetStr(src, "expect", "");
			ns["timeoutSeconds"] = Json.Int(Json.Get(src, "timeoutSeconds"), 5);
			break;
		}
		case "push":
		{
			string token = Json.GetStr(src, "pushToken", "").Trim();
			if (token.Length == 0)
			{
				errors.Add("Push: укажите токен");
			}
			ns["jobName"] = JobName(src, "Push");
			ns["pushToken"] = token;
			ns["staleMinutes"] = Json.Int(Json.Get(src, "staleMinutes"), 15);
			break;
		}
		case "smart":
		{
			ns["jobName"] = JobName(src, "SMART дисков");
			ns["checkSmartctl"] = Json.Bool(Json.Get(src, "checkSmartctl"), dflt: true);
			ns["minLifePercent"] = Json.Int(Json.Get(src, "minLifePercent"), 20);
			ns["warnTempC"] = Json.Int(Json.Get(src, "warnTempC"), 45);
			ns["maxTempC"] = Json.Int(Json.Get(src, "maxTempC"), 55);
			string dtype = Json.GetStr(src, "deviceType", "").Trim();
			if (dtype.Length > 0)
			{
				ns["deviceType"] = dtype;
			}
			break;
		}
		case "raid":
			ns["jobName"] = JobName(src, "RAID");
			break;
		case "hardware":
		{
			ns["jobName"] = JobName(src, "Железо");
			ns["useIpmi"] = Json.Bool(Json.Get(src, "useIpmi"), dflt: true);
			ns["cpuTempWarn"] = Json.Int(Json.Get(src, "cpuTempWarn"), 75);
			ns["cpuTempFail"] = Json.Int(Json.Get(src, "cpuTempFail"), 90);
			ns["fanMinRpm"] = Json.Int(Json.Get(src, "fanMinRpm"), 600);
			ns["psuFailIsCritical"] = Json.Bool(Json.Get(src, "psuFailIsCritical"), dflt: true);
			string bmc = Json.GetStr(src, "bmcHost", "").Trim();
			ns["bmcHost"] = bmc;
			ns["bmcUser"] = Json.GetStr(src, "bmcUser", "").Trim();
			ns["ipmiInterface"] = Json.GetStr(src, "ipmiInterface", "lanplus");
			ns["bmcTransport"] = Json.GetStr(src, "bmcTransport", "ipmi");
			string pwd2 = Json.GetStr(src, "password", "");
			if (pwd2.Length > 0)
			{
				ns["password"] = pwd2;
			}
			string secretKey2 = Json.GetStr(src, "passwordSecret", "");
			if (secretKey2.Length > 0)
			{
				ns["passwordSecret"] = secretKey2;
			}
			break;
		}
		case "hyperv":
		{
			ns["jobName"] = JobName(src, "ВМ");
			ns["expectedRunning"] = ReadStringList(src, "expectedRunning");
			ns["allowStopped"] = ReadStringList(src, "allowStopped");
			Dictionary<string, object> cp = Json.AsObject(Json.Get(src, "checkpoints"));
			Dictionary<string, object> ncp = Json.Obj();
			ncp["maxAgeDays"] = Json.Int(Json.Get(cp, "maxAgeDays"), 7);
			ncp["maxCount"] = Json.Int(Json.Get(cp, "maxCount"), 3);
			ns["checkpoints"] = ncp;
			ns["uptimeWarnMinutes"] = Json.Int(Json.Get(src, "uptimeWarnMinutes"), 10);
			ns["checkHeartbeat"] = Json.Bool(Json.Get(src, "checkHeartbeat"), dflt: true);
			ns["checkDiskFree"] = Json.Bool(Json.Get(src, "checkDiskFree"), dflt: true);
			ns["diskFreeWarnPercent"] = Json.Int(Json.Get(src, "diskFreeWarnPercent"), 10);
			break;
		}
		case "mikrotik":
		{
			string host4 = Json.GetStr(src, "host", "").Trim();
			if (host4.Length == 0)
			{
				errors.Add("MikroTik: укажите адрес роутера");
			}
			ns["jobName"] = JobName(src, "MikroTik " + host4);
			ns["host"] = host4;
			ns["port"] = Json.Int(Json.Get(src, "port"), 0);
			ns["username"] = Json.GetStr(src, "username", "").Trim();
			ns["useTls"] = Json.Bool(Json.Get(src, "useTls"), dflt: true);
			string tr = Json.GetStr(src, "transport", "auto").ToLowerInvariant();
			if (tr != "rest" && tr != "api" && tr != "snmp" && tr != "mndp")
			{
				tr = "auto";
			}
			ns["transport"] = tr;
			ns["snmpCommunity"] = Json.GetStr(src, "snmpCommunity", "");
			ns["minVersion"] = Json.GetStr(src, "minVersion", "");
			ns["cpuWarnPercent"] = Json.Int(Json.Get(src, "cpuWarnPercent"), 80);
			ns["tempWarnC"] = Json.Int(Json.Get(src, "tempWarnC"), 70);
			ns["uptimeWarnMinutes"] = Json.Int(Json.Get(src, "uptimeWarnMinutes"), 60);
			ns["timeoutSeconds"] = Json.Int(Json.Get(src, "timeoutSeconds"), 10);
			string mpwd = Json.GetStr(src, "password", "");
			if (mpwd.Length > 0)
			{
				ns["password"] = mpwd;
			}
			string mkey = Json.GetStr(src, "passwordSecret", "");
			if (mkey.Length > 0)
			{
				ns["passwordSecret"] = mkey;
			}
			break;
		}
		case "esxi":
		{
			string host2 = Json.GetStr(src, "host", "").Trim();
			if (host2.Length == 0)
			{
				errors.Add("ESXi: укажите адрес сервера");
			}
			ns["jobName"] = JobName(src, "ESXi");
			ns["host"] = host2;
			ns["port"] = Json.Int(Json.Get(src, "port"), 443);
			ns["username"] = Json.GetStr(src, "username", "").Trim();
			ns["ignoreCertErrors"] = Json.Bool(Json.Get(src, "ignoreCertErrors"), dflt: true);
			ns["timeoutSeconds"] = Json.Int(Json.Get(src, "timeoutSeconds"), 40);
			ns["expectedRunning"] = ReadStringList(src, "expectedRunning");
			ns["allowStopped"] = ReadStringList(src, "allowStopped");
			ns["checkDatastores"] = Json.Bool(Json.Get(src, "checkDatastores"), dflt: true);
			ns["datastoreWarnPercent"] = Json.Int(Json.Get(src, "datastoreWarnPercent"), 15);
			ns["datastoreFailPercent"] = Json.Int(Json.Get(src, "datastoreFailPercent"), 5);
			ns["checkSnapshots"] = Json.Bool(Json.Get(src, "checkSnapshots"), dflt: true);
			ns["snapshotMaxAgeDays"] = Json.Int(Json.Get(src, "snapshotMaxAgeDays"), 7);
			ns["checkSmart"] = Json.Bool(Json.Get(src, "checkSmart"), dflt: false);
			ns["sshPort"] = Json.Int(Json.Get(src, "sshPort"), 22);
			ns["sshUser"] = Json.GetStr(src, "sshUser", "").Trim();
			ns["smartWarnTempC"] = Json.Int(Json.Get(src, "smartWarnTempC"), 50);
			ns["smartMaxTempC"] = Json.Int(Json.Get(src, "smartMaxTempC"), 60);
			string spwd = Json.GetStr(src, "sshPassword", "");
			if (spwd.Length > 0)
			{
				ns["sshPassword"] = spwd;
			}
			string skey = Json.GetStr(src, "sshPasswordSecret", "");
			if (skey.Length > 0)
			{
				ns["sshPasswordSecret"] = skey;
			}
			string epwd = Json.GetStr(src, "password", "");
			if (epwd.Length > 0)
			{
				ns["password"] = epwd;
			}
			string ekey = Json.GetStr(src, "passwordSecret", "");
			if (ekey.Length > 0)
			{
				ns["passwordSecret"] = ekey;
			}
			break;
		}
		case "inventory":
			ns["jobName"] = JobName(src, "Инвентарь");
			ns["services"] = ReadStringList(src, "services");
			ns["checkVolumes"] = Json.Bool(Json.Get(src, "checkVolumes"), dflt: true);
			ns["volumeMinFreePercent"] = Json.Int(Json.Get(src, "volumeMinFreePercent"), 10);
			ns["checkDiskFree"] = Json.Int(Json.Get(src, "checkDiskFree"), 5);
			ns["collectSoftware"] = Json.Bool(Json.Get(src, "collectSoftware"), dflt: true);
			ns["softwareMaxItems"] = Json.Int(Json.Get(src, "softwareMaxItems"), 60);
			ns["checkHotfixes"] = Json.Bool(Json.Get(src, "checkHotfixes"), dflt: false);
			ns["hotfixMaxAgeDays"] = Json.Int(Json.Get(src, "hotfixMaxAgeDays"), 60);
			break;
		case "vss":
			ns["jobName"] = JobName(src, "VSS");
			ns["maxShadowAgeHours"] = Json.Int(Json.Get(src, "maxShadowAgeHours"), 48);
			ns["storageWarnPercent"] = Json.Int(Json.Get(src, "storageWarnPercent"), 80);
			ns["storageFailPercent"] = Json.Int(Json.Get(src, "storageFailPercent"), 95);
			break;
		case "eventlog":
		{
			ns["jobName"] = JobName(src, "Журнал");
			List<object> logs = ReadStringList(src, "logs");
			if (logs.Count == 0)
			{
				logs.Add("System");
				logs.Add("Application");
			}
			ns["logs"] = logs;
			ns["ignore"] = ReadStringList(src, "ignore");
			ns["hours"] = Json.Int(Json.Get(src, "hours"), 24);
			ns["includeWarnings"] = Json.Bool(Json.Get(src, "includeWarnings"), dflt: false);
			ns["warnCount"] = Json.Int(Json.Get(src, "warnCount"), 5);
			ns["failCount"] = Json.Int(Json.Get(src, "failCount"), 30);
			ns["maxEvents"] = Json.Int(Json.Get(src, "maxEvents"), 500);
			break;
		}
		case "netconfig":
			ns["jobName"] = JobName(src, "Сеть");
			ns["checkDns"] = Json.Bool(Json.Get(src, "checkDns"), dflt: true);
			ns["checkGateway"] = Json.Bool(Json.Get(src, "checkGateway"), dflt: true);
			ns["minLinkSpeedMbps"] = Json.Int(Json.Get(src, "minLinkSpeedMbps"), 0);
			break;
		case "tasks":
			ns["jobName"] = JobName(src, "Задачи");
			ns["expect"] = ReadStringList(src, "expect");
			ns["ignore"] = ReadStringList(src, "ignore");
			ns["autostartIgnore"] = ReadStringList(src, "autostartIgnore");
			ns["depth"] = Json.Int(Json.Get(src, "depth"), 3);
			ns["maxLastRunAgeHours"] = Json.Int(Json.Get(src, "maxLastRunAgeHours"), 0);
			ns["includeAutostart"] = Json.Bool(Json.Get(src, "includeAutostart"), dflt: true);
			break;
		default:
			errors.Add("Неизвестный тип источника: " + type);
			return null;
		}
		return ns;
	}

	private static string JobName(Dictionary<string, object> src, string dflt)
	{
		string jn = Json.GetStr(src, "jobName", "").Trim();
		if (jn.Length <= 0)
		{
			return dflt;
		}
		return jn;
	}

	private static List<object> ReadPaths(Dictionary<string, object> src)
	{
		List<object> paths = new List<object>();
		foreach (object p in Json.AsArray(Json.Get(src, "paths")))
		{
			string s = Json.Str(p).Trim();
			if (s.Length > 0)
			{
				paths.Add(s);
			}
		}
		return paths;
	}

	private static List<object> ReadStringList(Dictionary<string, object> src, string key)
	{
		List<object> list = new List<object>();
		object raw = Json.Get(src, key);
		if (raw is string)
		{
			string[] array = ((string)raw).Split(',', '\n', ';');
			foreach (string part in array)
			{
				string s = part.Trim();
				if (s.Length > 0 && !list.Contains(s))
				{
					list.Add(s);
				}
			}
			return list;
		}
		foreach (object o in Json.AsArray(raw))
		{
			string s2 = Json.Str(o).Trim();
			if (s2.Length > 0 && !list.Contains(s2))
			{
				list.Add(s2);
			}
		}
		return list;
	}

	private List<string> ExtractSecrets(Dictionary<string, object> company, string companyId, Dictionary<string, object> existing)
	{
		List<string> warnings = new List<string>();
		List<object> sources = Json.AsArray(Json.Get(company, "sources"));
		Dictionary<string, string> existingKeys = CollectExistingSecretKeys(existing);
		int veeamIdx = 0;
		int ftpIdx = 0;
		int mikrotikIdx = 0;
		int esxiIdx = 0;
		foreach (object o in sources)
		{
			Dictionary<string, object> src = Json.AsObject(o);
			switch (Json.GetStr(src, "type", ""))
			{
			case "veeam-rest":
			{
				veeamIdx++;
				string key3 = ((veeamIdx == 1) ? ("veeam." + companyId) : ("veeam." + companyId + "." + veeamIdx));
				ApplySecret(src, "password", "passwordSecret", key3, existingKeys, warnings, "Veeam: пароль не задан — проверка не заработает");
				break;
			}
			case "iperius-ftp":
			{
				ftpIdx++;
				Dictionary<string, object> ftp = Json.AsObject(Json.Get(src, "ftp"));
				if (ftp.Count != 0)
				{
					string key2 = ((ftpIdx == 1) ? ("ftp." + companyId) : ("ftp." + companyId + "." + ftpIdx));
					ApplySecret(ftp, "password", "passwordSecret", key2, existingKeys, warnings, "FTP: пароль не задан");
				}
				break;
			}
			case "hardware":
			{
				string host2 = Json.GetStr(src, "bmcHost", "");
				if (host2.Length != 0)
				{
					ApplySecret(src, "password", "passwordSecret", "bmc." + companyId, existingKeys, warnings, "IPMI: пароль BMC не задан");
				}
				break;
			}
			case "mikrotik":
			{
				string host3 = Json.GetStr(src, "host", "");
				if (host3.Length != 0)
				{
					mikrotikIdx++;
					string key4 = ((mikrotikIdx == 1) ? ("mikrotik." + companyId) : ("mikrotik." + companyId + "." + mikrotikIdx));
					ApplySecret(src, "password", "passwordSecret", key4, existingKeys, warnings, "MikroTik: пароль не задан");
				}
				break;
			}
			case "esxi":
			{
				string host = Json.GetStr(src, "host", "");
				if (host.Length != 0)
				{
					esxiIdx++;
					string key = ((esxiIdx == 1) ? ("esxi." + companyId) : ("esxi." + companyId + "." + esxiIdx));
					ApplySecret(src, "password", "passwordSecret", key, existingKeys, warnings, "ESXi: пароль не задан");
					string sshHost = Json.GetStr(src, "sshUser", "");
					if (sshHost.Length > 0)
					{
						ApplySecret(src, "sshPassword", "sshPasswordSecret", key + ".ssh", existingKeys, warnings, "");
					}
				}
				break;
			}
			}
		}
		return warnings;
	}

	private void ApplySecret(Dictionary<string, object> holder, string plainField, string keyField, string newKey, Dictionary<string, string> existingKeys, List<string> warnings, string warnIfEmpty)
	{
		string plain = Json.GetStr(holder, plainField, "");
		string oldKey = Json.GetStr(holder, keyField, "");
		holder.Remove(plainField);
		string fromExisting;
		if (plain.Length > 0)
		{
			_config.SaveSecret(newKey, plain, protect: true);
			holder[keyField] = newKey;
		}
		else if (oldKey.Length > 0 && _config.Secret(oldKey).Length > 0)
		{
			holder[keyField] = oldKey;
		}
		else if (existingKeys.TryGetValue((oldKey.Length > 0) ? oldKey : newKey, out fromExisting) && fromExisting.Length > 0)
		{
			holder[keyField] = ((oldKey.Length > 0) ? oldKey : newKey);
		}
		else
		{
			holder.Remove(keyField);
			warnings.Add(warnIfEmpty);
		}
	}

	internal void MarkHasPassword(Dictionary<string, object> company)
	{
		foreach (object o in Json.AsArray(Json.Get(company, "sources")))
		{
			Dictionary<string, object> src = Json.AsObject(o);
			string key = Json.GetStr(src, "passwordSecret", "");
			src["hasPassword"] = key.Length > 0 && _config.Secret(key).Length > 0;
			Dictionary<string, object> ftp = Json.AsObject(Json.Get(src, "ftp"));
			if (ftp.Count > 0)
			{
				string fkey = Json.GetStr(ftp, "passwordSecret", "");
				ftp["hasPassword"] = fkey.Length > 0 && _config.Secret(fkey).Length > 0;
			}
		}
	}

	private static void StripRuntimeFields(Dictionary<string, object> company)
	{
		foreach (object o in Json.AsArray(Json.Get(company, "sources")))
		{
			Dictionary<string, object> src = Json.AsObject(o);
			src.Remove("hasPassword");
			src.Remove("passwordSecretKey");
			src.Remove("password");
			src.Remove("hasSshPassword");
			src.Remove("sshPassword");
			Dictionary<string, object> ftp = Json.AsObject(Json.Get(src, "ftp"));
			if (ftp.Count > 0)
			{
				ftp.Remove("hasPassword");
				ftp.Remove("passwordSecretKey");
				ftp.Remove("password");
			}
		}
	}

	private static Dictionary<string, string> CollectExistingSecretKeys(Dictionary<string, object> existing)
	{
		Dictionary<string, string> keys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (existing == null)
		{
			return keys;
		}
		foreach (object o in Json.AsArray(Json.Get(existing, "sources")))
		{
			Dictionary<string, object> src = Json.AsObject(o);
			string k = Json.GetStr(src, "passwordSecret", "");
			if (k.Length > 0)
			{
				keys[k] = k;
			}
			Dictionary<string, object> ftp = Json.AsObject(Json.Get(src, "ftp"));
			string fk = Json.GetStr(ftp, "passwordSecret", "");
			if (fk.Length > 0)
			{
				keys[fk] = fk;
			}
		}
		return keys;
	}

	private string EnsureZabbixHostFor(string companyId)
	{
		if (!_settings.ZabbixApiEnabled || !_settings.ZabbixAutoCreateHosts)
		{
			return "";
		}
		Company c = _config.Find(companyId);
		if (c == null)
		{
			return "компания не загрузилась";
		}
		try
		{
			ZabbixApi api = new ZabbixApi(_settings.ZabbixApiUrl, _settings.ZabbixApiToken, _settings.ZabbixTimeoutSeconds);
			if (!api.IsConfigured)
			{
				return "Zabbix API не настроен";
			}
			Dictionary<string, string> macros = new Dictionary<string, string>();
			macros["{$BC.MAX_AGE_H}"] = c.Policy.FreshnessHours + "h";
			if (c.Policy.MinSizeBytes > 0)
			{
				macros["{$BC.MIN_SIZE}"] = c.Policy.MinSizeBytes.ToString();
			}
			Dictionary<string, string> tags = new Dictionary<string, string>();
			tags["company"] = c.Id;
			string host = (string.IsNullOrEmpty(c.Zabbix.Host) ? ("BC-" + c.Id) : c.Zabbix.Host);
			string display = (string.IsNullOrEmpty(c.Zabbix.DisplayName) ? c.Name : c.Zabbix.DisplayName);
			api.EnsureHost(host, display, _settings.ZabbixHostGroup, _settings.ZabbixTemplateName, macros, tags);
			return "хост " + host + " заведён в Zabbix";
		}
		catch (Exception ex)
		{
			Log.Warn("Zabbix API (" + companyId + "): " + ex.Message);
			return "Zabbix: " + ex.Message;
		}
	}

	private static List<object> ToObjectList(List<string> items)
	{
		List<object> list = new List<object>();
		if (items == null)
		{
			return list;
		}
		foreach (string s in items)
		{
			list.Add(s);
		}
		return list;
	}

	private void HandleCompanyRawSave(HttpListenerContext ctx, SessionInfo si, string id)
	{
		if (!Roles.CanWrite(si.Role))
		{
			WriteJson(ctx, Json.Obj("error", "Недостаточно прав"), 403);
			return;
		}
		id = Slugify(Uri.UnescapeDataString(id ?? ""));
		string path = Path.Combine(CompaniesDir, id + ".json");
		if (!File.Exists(path))
		{
			WriteJson(ctx, Json.Obj("error", "Компания не найдена: " + id), 404);
			return;
		}
		if (!si.CanSeeCompany(id))
		{
			WriteJson(ctx, Json.Obj("error", "Нет доступа к этой компании"), 403);
			return;
		}
		Dictionary<string, object> existing = new Dictionary<string, object>();
		try
		{
			existing = Json.ParseObject(File.ReadAllText(path));
		}
		catch
		{
		}
		Dictionary<string, object> payload = ReadJson(ctx);
		payload["id"] = id;
		Dictionary<string, object> normalized = Normalize(payload, existing, out var errors);
		if (errors.Count > 0)
		{
			WriteJson(ctx, Json.Obj("error", "Проверьте поля", "errors", ToObjectList(errors)), 400);
			return;
		}
		List<string> warnings = ExtractSecrets(normalized, id, existing);
		StripRuntimeFields(normalized);
		ConfigLoader.AtomicWrite(path, Json.WritePretty(normalized));
		_config.Load();
		_audit.Write(si.User, "company.rawUpdate", id, "");
		Dictionary<string, object> resp = Json.Obj();
		resp["ok"] = true;
		resp["id"] = id;
		resp["warnings"] = ToObjectList(warnings);
		WriteJson(ctx, resp, 200);
	}

	private void HandleCompanyRename(HttpListenerContext ctx, SessionInfo si, string oldId)
	{
		if (!Roles.CanWrite(si.Role))
		{
			WriteJson(ctx, Json.Obj("error", "Недостаточно прав"), 403);
			return;
		}
		oldId = Slugify(Uri.UnescapeDataString(oldId ?? ""));
		Dictionary<string, object> body = ReadJson(ctx);
		string newId = Slugify(Json.GetStr(body, "newId", ""));
		bool moveHistory = Json.Bool(Json.Get(body, "moveHistory"), dflt: true);
		bool renameZabbix = Json.Bool(Json.Get(body, "renameZabbix"), dflt: false);
		if (newId.Length < 2)
		{
			WriteJson(ctx, Json.Obj("error", "Новый ID: минимум 2 символа (a-z, 0-9, дефис)"), 400);
			return;
		}
		string oldPath = Path.Combine(CompaniesDir, oldId + ".json");
		string newPath = Path.Combine(CompaniesDir, newId + ".json");
		if (!File.Exists(oldPath))
		{
			WriteJson(ctx, Json.Obj("error", "Компания не найдена: " + oldId), 404);
			return;
		}
		if (File.Exists(newPath))
		{
			WriteJson(ctx, Json.Obj("error", "ID уже занят: " + newId), 409);
			return;
		}
		if (string.Equals(oldId, newId, StringComparison.OrdinalIgnoreCase))
		{
			WriteJson(ctx, Json.Obj("error", "ID не изменился"), 400);
			return;
		}
		Dictionary<string, object> raw;
		try
		{
			raw = Json.ParseObject(File.ReadAllText(oldPath));
		}
		catch (Exception ex)
		{
			WriteJson(ctx, Json.Obj("error", "Не удалось прочитать: " + ex.Message), 500);
			return;
		}
		string oldZabbixHost = Json.GetStr(Json.AsObject(Json.Get(raw, "zabbix")), "host", "");
		raw["id"] = newId;
		List<string> renamedSecrets = new List<string>();
		foreach (object o in Json.AsArray(Json.Get(raw, "sources")))
		{
			Dictionary<string, object> src = Json.AsObject(o);
			string key = Json.GetStr(src, "passwordSecret", "");
			if (key.Length > 0 && key.Contains(oldId))
			{
				string newKey = key.Replace(oldId, newId);
				string value = _config.Secret(key);
				if (value.Length > 0)
				{
					_config.SaveSecret(newKey, value, protect: true);
					renamedSecrets.Add(key + " → " + newKey);
				}
				src["passwordSecret"] = newKey;
			}
			Dictionary<string, object> ftp = Json.AsObject(Json.Get(src, "ftp"));
			string fkey = Json.GetStr(ftp, "passwordSecret", "");
			if (fkey.Length > 0 && fkey.Contains(oldId))
			{
				string newKey2 = fkey.Replace(oldId, newId);
				string value2 = _config.Secret(fkey);
				if (value2.Length > 0)
				{
					_config.SaveSecret(newKey2, value2, protect: true);
					renamedSecrets.Add(fkey + " → " + newKey2);
				}
				ftp["passwordSecret"] = newKey2;
			}
		}
		Dictionary<string, object> zbx = Json.AsObject(Json.Get(raw, "zabbix"));
		if (zbx.Count > 0)
		{
			string host = Json.GetStr(zbx, "host", "");
			if (host.Length == 0 || host == "BC-" + oldId)
			{
				zbx["host"] = "BC-" + newId;
			}
		}
		StripRuntimeFields(raw);
		ConfigLoader.AtomicWrite(newPath, Json.WritePretty(raw));
		File.Delete(oldPath);
		int movedRuns = 0;
		if (moveHistory)
		{
			movedRuns = _store.RenameCompany(oldId, newId);
		}
		_config.Load();
		string zabbixNote = "";
		if (renameZabbix && oldZabbixHost.Length > 0)
		{
			zabbixNote = RenameZabbixHost(oldZabbixHost, "BC-" + newId);
		}
		_audit.Write(si.User, "company.rename", oldId + " → " + newId, "history=" + movedRuns + " secrets=" + renamedSecrets.Count);
		Dictionary<string, object> resp = Json.Obj();
		resp["ok"] = true;
		resp["oldId"] = oldId;
		resp["newId"] = newId;
		resp["movedRuns"] = movedRuns;
		resp["renamedSecrets"] = ToObjectList(renamedSecrets);
		if (zabbixNote.Length > 0)
		{
			resp["zabbix"] = zabbixNote;
		}
		WriteJson(ctx, resp, 200);
	}

	private string RenameZabbixHost(string oldHost, string newHost)
	{
		if (!_settings.ZabbixApiEnabled)
		{
			return "";
		}
		try
		{
			ZabbixApi api = new ZabbixApi(_settings.ZabbixApiUrl, _settings.ZabbixApiToken, _settings.ZabbixTimeoutSeconds);
			if (!api.IsConfigured)
			{
				return "";
			}
			string hostId = api.FindHostId(oldHost);
			if (hostId.Length == 0)
			{
				return "хост " + oldHost + " не найден в Zabbix";
			}
			Dictionary<string, object> p = Json.Obj();
			p["hostid"] = hostId;
			p["host"] = newHost;
			api.Call("host.update", p);
			return "Zabbix: " + oldHost + " → " + newHost;
		}
		catch (Exception ex)
		{
			Log.Warn("Zabbix rename: " + ex.Message);
			return "Zabbix: " + ex.Message;
		}
	}

	private void HandleCompaniesBulk(HttpListenerContext ctx, SessionInfo si)
	{
		if (!Roles.CanWrite(si.Role))
		{
			WriteJson(ctx, Json.Obj("error", "Недостаточно прав"), 403);
			return;
		}
		Dictionary<string, object> body = ReadJson(ctx);
		string action = Json.GetStr(body, "action", "");
		List<string> ids = new List<string>();
		foreach (object o in Json.AsArray(Json.Get(body, "ids")))
		{
			string id = Json.Str(o);
			if (id.Length > 0)
			{
				ids.Add(id);
			}
		}
		if (ids.Count == 0)
		{
			WriteJson(ctx, Json.Obj("error", "Не выбрано ни одной компании"), 400);
			return;
		}
		int changed = 0;
		List<string> errors = new List<string>();
		foreach (string id2 in ids)
		{
			if (!si.CanSeeCompany(id2))
			{
				errors.Add(id2 + ": нет доступа");
				continue;
			}
			string path = Path.Combine(CompaniesDir, Slugify(id2) + ".json");
			if (!File.Exists(path))
			{
				errors.Add(id2 + ": не найдена");
				continue;
			}
			try
			{
				Dictionary<string, object> raw = Json.ParseObject(File.ReadAllText(path));
				switch (action)
				{
				case "enable":
					raw["enabled"] = true;
					break;
				case "disable":
					raw["enabled"] = false;
					break;
				case "delete":
				{
					if (!Roles.CanDeleteCompanies(si.Role))
					{
						errors.Add(id2 + ": нет прав на удаление");
						continue;
					}
					string archiveDir = Path.Combine(CompaniesDir, "_archive");
					Directory.CreateDirectory(archiveDir);
					string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
					File.Move(path, Path.Combine(archiveDir, id2 + "-" + stamp + ".json"));
					changed++;
					continue;
				}
				default:
					errors.Add("неизвестное действие: " + action);
					continue;
				}
				ConfigLoader.AtomicWrite(path, Json.WritePretty(raw));
				changed++;
			}
			catch (Exception ex)
			{
				errors.Add(id2 + ": " + ex.Message);
			}
		}
		_config.Load();
		_audit.Write(si.User, "company.bulk", action, "changed=" + changed + " ids=" + string.Join(",", ids.ToArray()));
		Dictionary<string, object> resp = Json.Obj();
		resp["ok"] = true;
		resp["changed"] = changed;
		resp["errors"] = ToObjectList(errors);
		WriteJson(ctx, resp, 200);
	}

	private void HandleCompanyHistory(HttpListenerContext ctx, SessionInfo si, string id)
	{
		id = Slugify(Uri.UnescapeDataString(id ?? ""));
		if (!si.CanSeeCompany(id))
		{
			WriteJson(ctx, Json.Obj("error", "Нет доступа"), 403);
			return;
		}
		List<object> items = new List<object>();
		foreach (object o in _audit.Read(2000))
		{
			Dictionary<string, object> e = Json.AsObject(o);
			string target = Json.GetStr(e, "target", "");
			string action = Json.GetStr(e, "action", "");
			if (action.StartsWith("company.") && target.IndexOf(id, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				items.Add(e);
				if (items.Count >= 100)
				{
					break;
				}
			}
		}
		Dictionary<string, object> resp = Json.Obj();
		resp["company"] = id;
		resp["items"] = items;
		WriteJson(ctx, resp, 200);
	}

	private void HandleCompaniesExport(HttpListenerContext ctx, SessionInfo si)
	{
		Dictionary<string, object> root = Json.Obj();
		root["exportedAt"] = DateTime.Now;
		root["agentVersion"] = _scheduler.Version;
		List<object> list = new List<object>();
		foreach (Company c in _config.Companies)
		{
			if (!si.CanSeeCompany(c.Id))
			{
				continue;
			}
			string path = Path.Combine(CompaniesDir, c.Id + ".json");
			if (File.Exists(path))
			{
				try
				{
					Dictionary<string, object> raw = Json.ParseObject(File.ReadAllText(path));
					StripRuntimeFields(raw);
					list.Add(raw);
				}
				catch
				{
				}
			}
		}
		root["companies"] = list;
		byte[] bytes = Encoding.UTF8.GetBytes(Json.WritePretty(root));
		ctx.Response.ContentType = "application/json; charset=utf-8";
		ctx.Response.AddHeader("Content-Disposition", "attachment; filename=\"companies-" + DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".json\"");
		ctx.Response.ContentLength64 = bytes.Length;
		ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
		ctx.Response.Close();
	}

	/// <summary>
	/// GET /api/config/export — вся конфигурация одним ZIP.
	/// Параметры: <c>secrets=1</c> — включить секреты, <c>companies=id1,id2</c> — только выбранные.
	/// </summary>
	private void HandleConfigExport(HttpListenerContext ctx, SessionInfo si)
	{
		if (!Roles.CanManageUsers(si.Role))
		{
			WriteJson(ctx, Json.Obj("error", "недостаточно прав"), 403);
			return;
		}
		bool withSecrets = Query(ctx, "secrets") == "1";
		string filter = Query(ctx, "companies").Trim();
		List<string> ids = null;
		if (filter.Length > 0)
		{
			ids = new List<string>();
			foreach (string part in filter.Split(','))
			{
				string id = part.Trim();
				if (id.Length > 0)
				{
					ids.Add(id);
				}
			}
		}
		byte[] zip;
		try
		{
			zip = ConfigBundle.Export(_settings.ConfigDir, withSecrets, ids);
		}
		catch (Exception ex)
		{
			Log.Error("Экспорт конфигурации", ex);
			WriteJson(ctx, Json.Obj("error", "не удалось собрать архив: " + ex.Message), 500);
			return;
		}
		string fileName = "config-" + DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) +
			(withSecrets ? "-with-secrets" : "") + ".zip";
		ctx.Response.ContentType = "application/zip";
		ctx.Response.AddHeader("Content-Disposition", "attachment; filename=\"" + fileName + "\"");
		ctx.Response.ContentLength64 = zip.Length;
		ctx.Response.OutputStream.Write(zip, 0, zip.Length);
		ctx.Response.Close();
		_audit.Write(si.User, "config.export", "config",
			"архив " + (zip.Length / 1024) + " КБ" + (withSecrets ? ", с секретами" : "") + ", IP " + ClientIp(ctx));
		Log.Info("Экспорт конфигурации: " + fileName + " (" + zip.Length + " байт, секреты: " + withSecrets + ")");
	}

	/// <summary>
	/// POST /api/config/import — применить архив конфигурации.
	/// Тело — сам ZIP. Параметры: <c>dryRun=1</c>, <c>secrets=1</c>.
	/// Перед записью текущая конфигурация сохраняется в <c>&lt;dataDir&gt;\backups</c>.
	/// </summary>
	private void HandleConfigImport(HttpListenerContext ctx, SessionInfo si)
	{
		if (!Roles.CanManageUsers(si.Role))
		{
			WriteJson(ctx, Json.Obj("error", "недостаточно прав"), 403);
			return;
		}
		byte[] body;
		try
		{
			using MemoryStream ms = new MemoryStream();
			ctx.Request.InputStream.CopyTo(ms);
			body = ms.ToArray();
		}
		catch (Exception ex)
		{
			WriteJson(ctx, Json.Obj("error", "не удалось прочитать тело запроса: " + ex.Message), 400);
			return;
		}
		if (body.Length == 0)
		{
			WriteJson(ctx, Json.Obj("error", "пустой запрос: ожидается ZIP с конфигурацией"), 400);
			return;
		}
		bool dryRun = Query(ctx, "dryRun") == "1";
		bool allowSecrets = Query(ctx, "secrets") == "1";
		string backupDir = Path.Combine(_settings.DataDir, "backups");

		ConfigBundle.ImportResult res = ConfigBundle.Import(_settings.ConfigDir, body, dryRun, allowSecrets, backupDir);
		if (!res.Ok)
		{
			Log.Warn("Импорт конфигурации отклонён: " + res.Error);
			WriteJson(ctx, res.ToJson(), 400);
			return;
		}
		if (dryRun)
		{
			Log.Info("Импорт конфигурации: проверка без изменений, файлов " + res.Changes.Count);
		}
		else
		{
			Log.Info("Импорт конфигурации: записано файлов " + res.FilesWritten +
				", компаний " + res.CompaniesImported +
				", резервная копия: " + (res.BackupPath.Length > 0 ? res.BackupPath : "нет"));
			_audit.Write(si.User, "config.import", "config",
				"файлов: " + res.FilesWritten + ", компаний: " + res.CompaniesImported +
				(res.SecretsImported ? ", с секретами" : "") + ", IP " + ClientIp(ctx));
			// Перечитываем конфигурацию, чтобы изменения применились без перезапуска.
			try
			{
				_config.Load();
			}
			catch (Exception ex)
			{
				Log.Warn("После импорта конфигурация перечитана с ошибкой: " + ex.Message);
			}
		}
		WriteJson(ctx, res.ToJson(), 200);
	}

	private void HandleDay(HttpListenerContext ctx, SessionInfo si)	{
		string company = QueryParam(ctx, "company");
		string dateText = QueryParam(ctx, "date");
		if (company.Length == 0)
		{
			WriteJson(ctx, Json.Obj("error", "Не указана компания"), 400);
			return;
		}
		if (!si.CanSeeCompany(company))
		{
			WriteJson(ctx, Json.Obj("error", "Нет доступа к этой компании"), 403);
			return;
		}
		if (!DateTime.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
		{
			WriteJson(ctx, Json.Obj("error", "Дата должна быть в формате ГГГГ-ММ-ДД"), 400);
			return;
		}
		Company c = _config.Find(company);
		List<BackupRun> runs = _store.GetDay(company, date, 500);
		HashSet<string> currentJobs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (JobState st in _store.GetStates())
		{
			if (string.Equals(st.CompanyId, company, StringComparison.OrdinalIgnoreCase))
			{
				currentJobs.Add(st.Job);
			}
		}
		Dictionary<string, BackupRun> lastByJob = new Dictionary<string, BackupRun>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, int> runCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		List<string> jobOrder = new List<string>();
		foreach (BackupRun r in runs)
		{
			runCount.TryGetValue(r.Job, out var n);
			runCount[r.Job] = n + 1;
			if (!lastByJob.ContainsKey(r.Job))
			{
				lastByJob[r.Job] = r;
				jobOrder.Add(r.Job);
			}
		}
		int ok = 0;
		int warn = 0;
		int fail = 0;
		int nodata = 0;
		int skip = 0;
		List<object> jobs = new List<object>();
		foreach (string job in jobOrder)
		{
			BackupRun r2 = lastByJob[job];
			bool current = currentJobs.Contains(job);
			if (current)
			{
				switch (r2.Status)
				{
				case BackupStatus.Ok:
					ok++;
					break;
				case BackupStatus.Warn:
					warn++;
					break;
				case BackupStatus.Fail:
					fail++;
					break;
				case BackupStatus.NoData:
					nodata++;
					break;
				default:
					skip++;
					break;
				}
			}
			Dictionary<string, object> d = r2.ToJson();
			d["runs"] = runCount[job];
			d["current"] = current;
			jobs.Add(d);
		}
		jobs.Sort((object a, object b) =>
		{
			Dictionary<string, object> obj = Json.AsObject(a);
			Dictionary<string, object> obj2 = Json.AsObject(b);
			bool flag = Json.Bool(Json.Get(obj, "current"), dflt: true);
			bool flag2 = Json.Bool(Json.Get(obj2, "current"), dflt: true);
			if (flag != flag2)
			{
				if (!flag)
				{
					return 1;
				}
				return -1;
			}
			int num = Priority(Json.GetStr(obj, "status", ""));
			int num2 = Priority(Json.GetStr(obj2, "status", ""));
			return (num != num2) ? num.CompareTo(num2) : string.Compare(Json.GetStr(obj, "job", ""), Json.GetStr(obj2, "job", ""), StringComparison.OrdinalIgnoreCase);
		});
		BackupStatus worst = BackupStatus.Skip;
		if (fail > 0)
		{
			worst = BackupStatus.Fail;
		}
		else if (warn > 0)
		{
			worst = BackupStatus.Warn;
		}
		else if (nodata > 0)
		{
			worst = BackupStatus.NoData;
		}
		else if (ok > 0)
		{
			worst = BackupStatus.Ok;
		}
		Dictionary<string, object> resp = Json.Obj();
		resp["company"] = company;
		resp["companyName"] = ((c != null) ? c.Name : company);
		resp["date"] = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		resp["status"] = StatusNames.Of(worst);
		resp["jobs"] = jobs;
		resp["total"] = jobs.Count;
		int archived = 0;
		foreach (object o in jobs)
		{
			if (!Json.Bool(Json.Get(Json.AsObject(o), "current"), dflt: true))
			{
				archived++;
			}
		}
		resp["archived"] = archived;
		Dictionary<string, object> counts = Json.Obj();
		counts["ok"] = ok;
		counts["warn"] = warn;
		counts["fail"] = fail;
		counts["nodata"] = nodata;
		counts["skip"] = skip;
		resp["counts"] = counts;
		resp["prev"] = NeighborInfo(company, date.AddDays(-1.0));
		resp["next"] = NeighborInfo(company, date.AddDays(1.0));
		WriteJson(ctx, resp, 200);
	}

	private Dictionary<string, object> NeighborInfo(string company, DateTime date)
	{
		List<BackupRun> runs = _store.GetDay(company, date, 300);
		if (runs.Count == 0)
		{
			return null;
		}
		BackupStatus worst = BackupStatus.Skip;
		foreach (BackupRun r in runs)
		{
			worst = StatusNames.Worst(worst, r.Status);
		}
		Dictionary<string, object> d = Json.Obj();
		d["date"] = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		d["status"] = StatusNames.Of(worst);
		d["runs"] = runs.Count;
		return d;
	}

	private void HandleJobHistory(HttpListenerContext ctx, SessionInfo si)
	{
		string company = QueryParam(ctx, "company");
		string job = QueryParam(ctx, "job");
		string dateText = QueryParam(ctx, "date");
		if (company.Length == 0 || job.Length == 0)
		{
			WriteJson(ctx, Json.Obj("error", "Нужны company и job"), 400);
			return;
		}
		if (!si.CanSeeCompany(company))
		{
			WriteJson(ctx, Json.Obj("error", "Нет доступа к этой компании"), 403);
			return;
		}
		if (!DateTime.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var center))
		{
			center = DateTime.Today;
		}
		int days = 30;
		string dq = QueryParam(ctx, "days");
		if (!string.IsNullOrEmpty(dq))
		{
			int.TryParse(dq, out days);
		}
		if (days < 1)
		{
			days = 30;
		}
		if (days > 180)
		{
			days = 180;
		}
		DateTime to = ((center > DateTime.Today) ? DateTime.Today : center);
		DateTime from = to.AddDays(-(days - 1));
		List<BackupRun> runs = _store.GetJournalRange(company, job, from, to, 1000);
		Dictionary<string, BackupRun> lastByDay = new Dictionary<string, BackupRun>(StringComparer.Ordinal);
		Dictionary<string, int> countByDay = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (BackupRun r in runs)
		{
			string key = r.Timestamp.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
			countByDay.TryGetValue(key, out var n);
			countByDay[key] = n + 1;
			if (!lastByDay.ContainsKey(key))
			{
				lastByDay[key] = r;
			}
		}
		List<object> items = new List<object>();
		DateTime cur = to;
		int guard = 0;
		while (cur >= from && guard++ < 400)
		{
			string key2 = cur.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
			Dictionary<string, object> d = Json.Obj();
			d["date"] = key2;
			if (lastByDay.TryGetValue(key2, out var r2))
			{
				countByDay.TryGetValue(key2, out var n2);
				d["status"] = StatusNames.Of(r2.Status);
				d["message"] = r2.Message;
				d["ts"] = r2.Timestamp;
				d["runs"] = n2;
				if (r2.LastSuccess.HasValue)
				{
					d["lastSuccess"] = r2.LastSuccess.Value;
				}
				d["size"] = r2.SizeBytes;
			}
			else
			{
				d["status"] = null;
				d["runs"] = 0;
			}
			items.Add(d);
			cur = cur.AddDays(-1.0);
		}
		int ok = 0;
		int warn = 0;
		int fail = 0;
		int nodata = 0;
		int missing = 0;
		foreach (object o in items)
		{
			Dictionary<string, object> d2 = Json.AsObject(o);
			string st = Json.GetStr(d2, "status", "");
			if (st.Length == 0)
			{
				missing++;
				continue;
			}
			switch (st)
			{
			case "OK":
				ok++;
				break;
			case "WARN":
				warn++;
				break;
			case "FAIL":
				fail++;
				break;
			default:
				nodata++;
				break;
			}
		}
		Company c = _config.Find(company);
		Dictionary<string, object> resp = Json.Obj();
		resp["company"] = company;
		resp["companyName"] = ((c != null) ? c.Name : company);
		resp["job"] = job;
		resp["from"] = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		resp["to"] = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		resp["items"] = items;
		Dictionary<string, object> counts = Json.Obj();
		counts["ok"] = ok;
		counts["warn"] = warn;
		counts["fail"] = fail;
		counts["nodata"] = nodata;
		counts["missing"] = missing;
		resp["counts"] = counts;
		WriteJson(ctx, resp, 200);
	}

	private static int Priority(string status)
	{
		return status switch
		{
			"FAIL" => 0, 
			"WARN" => 1, 
			"NODATA" => 2, 
			"OK" => 3, 
			_ => 4, 
		};
	}

	private void HandleDeployStatus(HttpListenerContext ctx, SessionInfo si)
	{
		if (!DenyDeploy(ctx, si))
		{
			return;
		}
		DeployManager.CredentialInfo cred = _deploy.GetCredential();
		Dictionary<string, object> d = Json.Obj();
		d["ok"] = true;
		Dictionary<string, object> c = Json.Obj();
		c["user"] = cred.User;
		c["hasPassword"] = cred.HasPassword;
		d["credential"] = c;
		List<object> subnets = new List<object>();
		foreach (string s in Scanner.LocalSubnets())
		{
			subnets.Add(s);
		}
		d["subnets"] = subnets;
		List<object> jobs = new List<object>();
		foreach (DeployJob j in _deploy.Jobs(30))
		{
			jobs.Add(j.ToJson());
		}
		d["jobs"] = jobs;
		List<object> agents = new List<object>();
		string currentVersion = _scheduler.Version;
		foreach (AgentRegistry.RemoteAgent a in _agents.All)
		{
			Dictionary<string, object> x = Json.Obj();
			x["id"] = a.Id;
			x["name"] = a.Name;
			x["enabled"] = a.Enabled;
			x["lastIp"] = a.LastIp;
			x["lastSeen"] = a.LastSeen;
			x["lastVersion"] = a.LastVersion;
			x["outdated"] = a.LastVersion.Length > 0 && !string.Equals(a.LastVersion, currentVersion, StringComparison.OrdinalIgnoreCase);
			agents.Add(x);
		}
		d["agents"] = agents;
		d["currentVersion"] = currentVersion;
		d["centralUrl"] = _deploy.GetCentralUrl();
		WriteJson(ctx, d, 200);
	}

	private void HandleDeployCredential(HttpListenerContext ctx, SessionInfo si)
	{
		if (!DenyDeploy(ctx, si))
		{
			return;
		}
		Dictionary<string, object> body = ReadJson(ctx);
		string user = Json.GetStr(body, "username", "").Trim();
		string pass = Json.GetStr(body, "password", "");
		if (user.Length == 0)
		{
			WriteJson(ctx, Json.Obj("ok", false, "error", "не задано имя учётной записи"), 400);
			return;
		}
		if (pass.Length == 0)
		{
			DeployManager.CredentialInfo old = _deploy.GetCredential();
			if (!old.HasPassword)
			{
				WriteJson(ctx, Json.Obj("ok", false, "error", "не задан пароль"), 400);
				return;
			}
			string saved = _config.Secret("deploy.cred");
			Dictionary<string, object> d = Json.ParseObject(saved);
			pass = Json.GetStr(d, "password", "");
		}
		_deploy.SaveCredential(user, pass);
		_audit.Write(si.User, "deploy.credential", user, "учётная запись WinRM сохранена");
		WriteJson(ctx, Json.Obj("ok", true, "user", user), 200);
	}

	private void HandleDeployScan(HttpListenerContext ctx, SessionInfo si)
	{
		if (!DenyDeploy(ctx, si))
		{
			return;
		}
		Dictionary<string, object> body = ReadJson(ctx);
		string spec = Json.GetStr(body, "spec", "").Trim();
		int threads = Json.Int(Json.Get(body, "threads"), 48);
		int timeoutMs = Json.Int(Json.Get(body, "timeoutMs"), 900);
		if (timeoutMs < 200)
		{
			timeoutMs = 200;
		}
		if (timeoutMs > 5000)
		{
			timeoutMs = 5000;
		}
		List<string> addresses = Scanner.Expand(spec, 1024, out var error);
		if (error.Length > 0)
		{
			WriteJson(ctx, Json.Obj("ok", false, "error", error), 400);
			return;
		}
		List<Target> found = Scanner.Scan(addresses, threads, timeoutMs);
		List<Target> alive = new List<Target>();
		foreach (Target t in found)
		{
			if (t.Online)
			{
				alive.Add(t);
			}
		}
		List<object> items = new List<object>();
		foreach (Target t2 in alive)
		{
			items.Add(t2.ToJson());
		}
		_audit.Write(si.User, "deploy.scan", spec, "адресов " + addresses.Count + ", ответили " + alive.Count);
		Dictionary<string, object> d = Json.Obj();
		d["ok"] = true;
		d["checked"] = addresses.Count;
		d["count"] = alive.Count;
		d["targets"] = items;
		WriteJson(ctx, d, 200);
	}

	private void HandleDeployAd(HttpListenerContext ctx, SessionInfo si)
	{
		if (!DenyDeploy(ctx, si))
		{
			return;
		}
		DeployManager.CredentialInfo cred = _deploy.GetCredential();
		if (!cred.HasPassword)
		{
			WriteJson(ctx, Json.Obj("ok", false, "error", "сначала сохраните учётную запись домена"), 400);
			return;
		}
		Dictionary<string, object> body = ReadJson(ctx);
		string domain = Json.GetStr(body, "domain", "").Trim();
		string baseDn = Json.GetStr(body, "baseDn", "").Trim();
		int maxHosts = Json.Int(Json.Get(body, "maxHosts"), 500);
		if (maxHosts < 1)
		{
			maxHosts = 500;
		}
		if (maxHosts > 5000)
		{
			maxHosts = 5000;
		}
		List<Target> found = Scanner.DiscoverAd(domain, baseDn, cred.User, SecretOf("deploy.cred", "password"), maxHosts, out var error);
		if (error.Length > 0)
		{
			WriteJson(ctx, Json.Obj("ok", false, "error", error), 400);
			return;
		}
		List<object> items = new List<object>();
		foreach (Target t in found)
		{
			items.Add(t.ToJson());
		}
		_audit.Write(si.User, "deploy.ad", (domain.Length > 0) ? domain : "(текущий домен)", "компьютеров " + found.Count);
		Dictionary<string, object> d = Json.Obj();
		d["ok"] = true;
		d["count"] = found.Count;
		d["targets"] = items;
		WriteJson(ctx, d, 200);
	}

	private void HandleDeployProbe(HttpListenerContext ctx, SessionInfo si)
	{
		if (!DenyDeploy(ctx, si))
		{
			return;
		}
		Dictionary<string, object> body = ReadJson(ctx);
		List<object> hosts = Json.AsArray(Json.Get(body, "hosts"));
		List<string> addresses = new List<string>();
		foreach (object o in hosts)
		{
			string h = Json.Str(o).Trim();
			if (h.Length > 0 && !addresses.Contains(h))
			{
				addresses.Add(h);
			}
		}
		if (addresses.Count == 0)
		{
			WriteJson(ctx, Json.Obj("ok", false, "error", "не переданы адреса"), 400);
			return;
		}
		if (addresses.Count > 1024)
		{
			WriteJson(ctx, Json.Obj("ok", false, "error", "слишком много адресов"), 400);
			return;
		}
		List<Target> found = Scanner.Scan(addresses, 32, 1200);
		List<object> items = new List<object>();
		foreach (Target t in found)
		{
			items.Add(t.ToJson());
		}
		Dictionary<string, object> d = Json.Obj();
		d["ok"] = true;
		d["count"] = found.Count;
		d["targets"] = items;
		WriteJson(ctx, d, 200);
	}

	private void HandleDeployTest(HttpListenerContext ctx, SessionInfo si)
	{
		if (DenyDeploy(ctx, si))
		{
			Dictionary<string, object> body = ReadJson(ctx);
			string host = Json.GetStr(body, "host", "").Trim();
			int port = Json.Int(Json.Get(body, "port"), 0);
			bool https = Json.Bool(Json.Get(body, "https"));
			if (host.Length == 0)
			{
				WriteJson(ctx, Json.Obj("ok", false, "error", "не задан хост"), 400);
			}
			else
			{
				WriteJson(ctx, _deploy.TestHost(host, port, https), 200);
			}
		}
	}

	private void HandleDeployInstall(HttpListenerContext ctx, SessionInfo si)
	{
		if (!DenyDeploy(ctx, si))
		{
			return;
		}
		DeployManager.CredentialInfo cred = _deploy.GetCredential();
		if (!cred.HasPassword)
		{
			WriteJson(ctx, Json.Obj("ok", false, "error", "сначала сохраните учётную запись WinRM"), 400);
			return;
		}
		Dictionary<string, object> body = ReadJson(ctx);
		List<object> hosts = Json.AsArray(Json.Get(body, "hosts"));
		if (hosts == null || hosts.Count == 0)
		{
			WriteJson(ctx, Json.Obj("ok", false, "error", "не выбрано ни одной машины"), 400);
			return;
		}
		DeployOptions o = new DeployOptions();
		o.Mode = Json.GetStr(body, "mode", "agent");
		o.CentralUrl = Json.GetStr(body, "centralUrl", "");
		o.Https = Json.Bool(Json.Get(body, "https"));
		o.WinRmPort = Json.Int(Json.Get(body, "port"), 0);
		string dir = Json.GetStr(body, "installDir", "").Trim();
		if (dir.Length > 0)
		{
			o.InstallDir = dir;
		}
		string svc = Json.GetStr(body, "serviceName", "").Trim();
		if (svc.Length > 0)
		{
			o.ServiceName = svc;
		}
		List<string> ids = new List<string>();
		List<object> started = new List<object>();
		foreach (object h in hosts)
		{
			string host = Json.Str(h).Trim();
			if (host.Length != 0)
			{
				DeployJob job = _deploy.Enqueue("install", host, o);
				ids.Add(job.Id);
				Dictionary<string, object> x = Json.Obj();
				x["host"] = host;
				x["job"] = job.Id;
				started.Add(x);
			}
		}
		_audit.Write(si.User, "deploy.install", string.Join(", ", ids.ToArray()), "машин: " + ids.Count + ", режим: " + o.Mode);
		Dictionary<string, object> d = Json.Obj();
		d["ok"] = true;
		d["count"] = ids.Count;
		d["jobs"] = started;
		WriteJson(ctx, d, 200);
	}

	private void HandleDeployUninstall(HttpListenerContext ctx, SessionInfo si)
	{
		if (!DenyDeploy(ctx, si))
		{
			return;
		}
		DeployManager.CredentialInfo cred = _deploy.GetCredential();
		if (!cred.HasPassword)
		{
			WriteJson(ctx, Json.Obj("ok", false, "error", "сначала сохраните учётную запись WinRM"), 400);
			return;
		}
		Dictionary<string, object> body = ReadJson(ctx);
		List<object> hosts = Json.AsArray(Json.Get(body, "hosts"));
		if (hosts == null || hosts.Count == 0)
		{
			WriteJson(ctx, Json.Obj("ok", false, "error", "не выбрано ни одной машины"), 400);
			return;
		}
		DeployOptions o = new DeployOptions();
		o.Mode = "uninstall";
		o.Https = Json.Bool(Json.Get(body, "https"));
		o.WinRmPort = Json.Int(Json.Get(body, "port"), 0);
		o.KeepFiles = Json.Bool(Json.Get(body, "keepFiles"));
		o.RemoveAgent = Json.Bool(Json.Get(body, "removeAgent"), dflt: true);
		string dir = Json.GetStr(body, "installDir", "").Trim();
		if (dir.Length > 0)
		{
			o.InstallDir = dir;
		}
		string svc = Json.GetStr(body, "serviceName", "").Trim();
		if (svc.Length > 0)
		{
			o.ServiceName = svc;
		}
		List<string> ids = new List<string>();
		List<object> started = new List<object>();
		foreach (object h in hosts)
		{
			string host = Json.Str(h).Trim();
			if (host.Length != 0)
			{
				DeployJob job = _deploy.Enqueue("uninstall", host, o);
				ids.Add(job.Id);
				Dictionary<string, object> x = Json.Obj();
				x["host"] = host;
				x["job"] = job.Id;
				started.Add(x);
			}
		}
		_audit.Write(si.User, "deploy.uninstall", string.Join(", ", ids.ToArray()), "машин: " + ids.Count);
		Dictionary<string, object> d = Json.Obj();
		d["ok"] = true;
		d["count"] = ids.Count;
		d["jobs"] = started;
		WriteJson(ctx, d, 200);
	}

	private void HandleDeployJobs(HttpListenerContext ctx, SessionInfo si)
	{
		if (!DenyDeploy(ctx, si))
		{
			return;
		}
		int limit = 30;
		string raw = QueryParam(ctx, "limit");
		if (int.TryParse(raw, out var parsed) && parsed > 0 && parsed <= 200)
		{
			limit = parsed;
		}
		List<object> jobs = new List<object>();
		foreach (DeployJob j in _deploy.Jobs(limit))
		{
			jobs.Add(j.ToJson());
		}
		Dictionary<string, object> d = Json.Obj();
		d["ok"] = true;
		d["jobs"] = jobs;
		WriteJson(ctx, d, 200);
	}

	private bool DenyDeploy(HttpListenerContext ctx, SessionInfo si)
	{
		if (si != null && !Roles.CanManageUsers(si.Role))
		{
			WriteJson(ctx, Json.Obj("error", "forbidden"), 403);
			return false;
		}
		if (_deploy == null)
		{
			WriteJson(ctx, Json.Obj("error", "deploy недоступен"), 503);
			return false;
		}
		return true;
	}

	private string SecretOf(string key, string field)
	{
		string raw = _config.Secret(key);
		if (raw.Length == 0)
		{
			return "";
		}
		try
		{
			Dictionary<string, object> d = Json.ParseObject(raw);
			return Json.GetStr(d, field, "");
		}
		catch
		{
			return "";
		}
	}

	private void HandleManageCatalog(HttpListenerContext ctx, SessionInfo si)
	{
		if (!DenyManage(ctx, si))
		{
			return;
		}
		List<object> ops = new List<object>();
		foreach (OpDef d in Catalog.All())
		{
			ops.Add(d.ToJson());
		}
		List<object> groups = new List<object>();
		foreach (string g in Catalog.Groups())
		{
			groups.Add(g);
		}
		List<object> forbidden = new List<object>();
		string[] forbiddenServices = Catalog.ForbiddenServices;
		foreach (string s in forbiddenServices)
		{
			forbidden.Add(s);
		}
		List<object> common = new List<object>();
		string[] commonServices = Catalog.CommonServices;
		foreach (string s2 in commonServices)
		{
			common.Add(s2);
		}
		Dictionary<string, object> d2 = Json.Obj();
		d2["ok"] = true;
		d2["operations"] = ops;
		d2["groups"] = groups;
		d2["forbiddenServices"] = forbidden;
		d2["commonServices"] = common;
		WriteJson(ctx, d2, 200);
	}

	private void HandleManageTargets(HttpListenerContext ctx, SessionInfo si)
	{
		if (!DenyManage(ctx, si))
		{
			return;
		}
		List<object> targets = new List<object>();
		Dictionary<string, object> local = Json.Obj();
		local["id"] = "local";
		local["name"] = Environment.MachineName;
		local["host"] = "localhost";
		local["kind"] = "local";
		local["note"] = "эта машина (Hyper-V, локальные службы)";
		targets.Add(local);
		foreach (AgentRegistry.RemoteAgent a in _agents.All)
		{
			Dictionary<string, object> t = Json.Obj();
			t["id"] = a.Id;
			t["name"] = a.Name;
			t["host"] = ((a.LastIp.Length > 0) ? a.LastIp : "");
			t["kind"] = "agent";
			t["enabled"] = a.Enabled;
			if (a.LastSeen.HasValue)
			{
				t["lastSeen"] = a.LastSeen.Value;
			}
			if (a.LastIp.Length == 0)
			{
				t["note"] = "адрес ещё не известен — агент не выходил на связь";
			}
			targets.Add(t);
		}
		CredentialInfo cred = GetManageCredential();
		Dictionary<string, object> d = Json.Obj();
		d["ok"] = true;
		d["targets"] = targets;
		d["hasCredential"] = cred.HasPassword;
		d["credentialUser"] = cred.User;
		WriteJson(ctx, d, 200);
	}

	private void HandleManageSubmit(HttpListenerContext ctx, SessionInfo si)
	{
		if (!DenyManage(ctx, si))
		{
			return;
		}
		Dictionary<string, object> body = ReadJson(ctx);
		string opId = Json.GetStr(body, "op", "").Trim();
		string target = Json.GetStr(body, "target", "").Trim();
		string confirmText = Json.GetStr(body, "confirmText", "");
		OpDef def = Catalog.Find(opId);
		if (def == null)
		{
			WriteJson(ctx, Json.Obj("ok", false, "error", "неизвестная операция: " + opId), 400);
			return;
		}
		Dictionary<string, string> prms = ReadParams(body);
		OpJob job = _ops.Submit(def, target, prms, si.User, confirmText, out var error);
		if (job == null)
		{
			WriteJson(ctx, Json.Obj("ok", false, "error", error), 400);
			return;
		}
		_audit.Write(si.User, "manage.submit", job.Id, def.Id + " → " + target + " (" + job.Status + ")");
		Dictionary<string, object> d = Json.Obj();
		d["ok"] = true;
		d["job"] = job.ToJson();
		d["needsApproval"] = job.Status == "pending";
		WriteJson(ctx, d, 200);
	}

	private void HandleManageApprove(HttpListenerContext ctx, SessionInfo si)
	{
		if (DenyManage(ctx, si))
		{
			Dictionary<string, object> body = ReadJson(ctx);
			string jobId = Json.GetStr(body, "job", "").Trim();
			if (!_ops.Approve(jobId, si.User, out var error))
			{
				WriteJson(ctx, Json.Obj("ok", false, "error", error), 400);
			}
			else
			{
				_audit.Write(si.User, "manage.approve", jobId, "операция подтверждена");
				OpJob job = _ops.ById(jobId);
				Dictionary<string, object> d = Json.Obj();
				d["ok"] = true;
				d["job"] = job?.ToJson();
				WriteJson(ctx, d, 200);
			}
		}
	}

	private void HandleManageReject(HttpListenerContext ctx, SessionInfo si)
	{
		if (DenyManage(ctx, si))
		{
			Dictionary<string, object> body = ReadJson(ctx);
			string jobId = Json.GetStr(body, "job", "").Trim();
			if (!_ops.Reject(jobId, si.User, out var error))
			{
				WriteJson(ctx, Json.Obj("ok", false, "error", error), 400);
			}
			else
			{
				_audit.Write(si.User, "manage.reject", jobId, "операция отклонена");
				WriteJson(ctx, Json.Obj("ok", true), 200);
			}
		}
	}

	private void HandleManageJobs(HttpListenerContext ctx, SessionInfo si)
	{
		if (!DenyManage(ctx, si))
		{
			return;
		}
		int limit = 50;
		if (int.TryParse(QueryParam(ctx, "limit"), out var parsed) && parsed > 0 && parsed <= 200)
		{
			limit = parsed;
		}
		List<object> jobs = new List<object>();
		foreach (OpJob j in _ops.Jobs(limit))
		{
			jobs.Add(j.ToJson());
		}
		Dictionary<string, object> d = Json.Obj();
		d["ok"] = true;
		d["jobs"] = jobs;
		WriteJson(ctx, d, 200);
	}

	private void HandleManageHistory(HttpListenerContext ctx, SessionInfo si)
	{
		if (DenyManage(ctx, si))
		{
			string month = QueryParam(ctx, "month");
			int limit = 100;
			if (int.TryParse(QueryParam(ctx, "limit"), out var parsed) && parsed > 0 && parsed <= 500)
			{
				limit = parsed;
			}
			Dictionary<string, object> d = Json.Obj();
			d["ok"] = true;
			d["month"] = ((month.Length > 0) ? month : DateTime.Now.ToString("yyyy-MM"));
			d["items"] = _ops.History(month, limit);
			WriteJson(ctx, d, 200);
		}
	}

	private bool DenyManage(HttpListenerContext ctx, SessionInfo si)
	{
		if (si != null && !Roles.CanManageUsers(si.Role))
		{
			WriteJson(ctx, Json.Obj("error", "forbidden"), 403);
			return false;
		}
		if (_ops == null)
		{
			WriteJson(ctx, Json.Obj("error", "управление недоступно"), 503);
			return false;
		}
		return true;
	}

	private static Dictionary<string, string> ReadParams(Dictionary<string, object> body)
	{
		Dictionary<string, string> result = new Dictionary<string, string>();
		Dictionary<string, object> prms = Json.AsObject(Json.Get(body, "params"));
		foreach (KeyValuePair<string, object> kv in prms)
		{
			result[kv.Key] = Json.Str(kv.Value);
		}
		return result;
	}

	private CredentialInfo GetManageCredential()
	{
		CredentialInfo ci = new CredentialInfo();
		string raw = _config.Secret("deploy.cred");
		if (raw.Length == 0)
		{
			return ci;
		}
		try
		{
			Dictionary<string, object> d = Json.ParseObject(raw);
			ci.User = Json.GetStr(d, "user", "");
			ci.HasPassword = Json.GetStr(d, "password", "").Length > 0;
		}
		catch
		{
		}
		return ci;
	}

	private void HandleUptime(HttpListenerContext ctx, SessionInfo si)
	{
		int hours = 24;
		string hs = QueryParam(ctx, "hours");
		if (!string.IsNullOrEmpty(hs))
		{
			int.TryParse(hs, out hours);
		}
		if (hours <= 0)
		{
			hours = 24;
		}
		if (hours > 720)
		{
			hours = 720;
		}
		string companyFilter = QueryParam(ctx, "company");
		List<object> items = new List<object>();
		foreach (JobState st in _store.GetStates())
		{
			if (st.IsUptime && si.CanSeeCompany(st.CompanyId) && (string.IsNullOrEmpty(companyFilter) || string.Equals(st.CompanyId, companyFilter, StringComparison.OrdinalIgnoreCase)))
			{
				Company c = _config.Find(st.CompanyId);
				if (c != null)
				{
					Dictionary<string, object> d = Json.Obj();
					d["company"] = st.CompanyId;
					d["companyName"] = c.Name;
					d["job"] = st.Job;
					d["kind"] = st.Kind;
					d["status"] = StatusNames.Of(st.Status);
					d["message"] = st.Message;
					d["target"] = st.Target;
					d["responseMs"] = st.ResponseMs;
					d["lastCheck"] = st.Updated;
					d["failures"] = st.ConsecutiveFailures;
					d["series"] = _store.GetUptimeSeries(st.CompanyId, st.Job, hours);
					d["summary"] = _store.GetUptimeSummary(st.CompanyId, st.Job, hours);
					items.Add(d);
				}
			}
		}
		items.Sort((object a, object b) =>
		{
			Dictionary<string, object> obj = Json.AsObject(a);
			Dictionary<string, object> obj2 = Json.AsObject(b);
			int num = StatusNames.Weight(StatusNames.Parse(Json.GetStr(obj, "status", "OK")));
			int num2 = StatusNames.Weight(StatusNames.Parse(Json.GetStr(obj2, "status", "OK")));
			return (num != num2) ? num2.CompareTo(num) : string.Compare(Json.GetStr(obj, "job", ""), Json.GetStr(obj2, "job", ""), StringComparison.OrdinalIgnoreCase);
		});
		Dictionary<string, object> resp = Json.Obj();
		resp["hours"] = hours;
		resp["generatedAt"] = DateTime.Now;
		resp["items"] = items;
		WriteJson(ctx, resp, 200);
	}

	private void HandlePush(HttpListenerContext ctx, string token)
	{
		token = (token ?? "").Trim().TrimEnd('/');
		if (token.Length == 0)
		{
			WriteJson(ctx, Json.Obj("error", "no token"), 400);
			return;
		}
		Company found = null;
		Source foundSource = null;
		foreach (Company c in _config.Companies)
		{
			foreach (Source s in c.Sources)
			{
				if (string.Equals(s.Type, "push", StringComparison.OrdinalIgnoreCase))
				{
					string t = s.GetStr("pushToken", "");
					if (t.Length != 0 && string.Equals(t, token, StringComparison.Ordinal))
					{
						found = c;
						foundSource = s;
						break;
					}
				}
			}
			if (found != null)
			{
				break;
			}
		}
		if (found == null)
		{
			WriteJson(ctx, Json.Obj("error", "unknown token"), 404);
			return;
		}
		string body = "";
		try
		{
			using StreamReader sr = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
			body = sr.ReadToEnd();
		}
		catch
		{
		}
		Dictionary<string, object> parsed = Json.Obj();
		try
		{
			parsed = Json.ParseObject(body);
		}
		catch
		{
		}
		string job = foundSource.GetStr("jobName", "Push");
		string status = Json.GetStr(parsed, "status", "OK");
		string message = Json.GetStr(parsed, "msg", "");
		BackupRun run = new BackupRun();
		run.Timestamp = DateTime.Now;
		run.CompanyId = found.Id;
		run.CompanyName = found.Name;
		run.Job = job;
		run.Kind = "push";
		run.SourceId = foundSource.Id;
		run.Status = StatusNames.Parse(status);
		run.LastSuccess = DateTime.UtcNow;
		run.LastAttempt = DateTime.UtcNow;
		run.IsUptime = true;
		run.Message = message;
		run.Target = "push://" + found.Id + "/" + job;
		_store.Record(run);
		Log.Debug("push: " + found.Id + " / " + job + " -> " + status);
		Dictionary<string, object> resp = Json.Obj();
		resp["ok"] = true;
		resp["job"] = job;
		resp["receivedAt"] = DateTime.Now;
		WriteJson(ctx, resp, 200);
	}

	private bool RequireAdmin(HttpListenerContext ctx, SessionInfo si)
	{
		if (Roles.CanManageUsers(si.Role))
		{
			return true;
		}
		WriteJson(ctx, Json.Obj("error", "Требуются права администратора"), 403);
		return false;
	}

	private void HandleUsersList(HttpListenerContext ctx, SessionInfo si)
	{
		if (!RequireAdmin(ctx, si))
		{
			return;
		}
		List<object> users = new List<object>();
		foreach (UserRecord u in _auth.List())
		{
			Dictionary<string, object> d = u.ToJson(includeHash: false);
			d["roleDescription"] = Roles.Describe(u.Role);
			d["locked"] = u.IsLocked;
			users.Add(d);
		}
		Dictionary<string, object> resp = Json.Obj();
		resp["users"] = users;
		resp["roles"] = RolesPayload();
		resp["currentUser"] = si.User;
		WriteJson(ctx, resp, 200);
	}

	private static List<object> RolesPayload()
	{
		List<object> list = new List<object>();
		string[] all = new string[4] { "admin", "operator", "viewer", "client" };
		string[] array = all;
		foreach (string r in array)
		{
			Dictionary<string, object> d = Json.Obj();
			d["id"] = r;
			d["description"] = Roles.Describe(r);
			list.Add(d);
		}
		return list;
	}

	private void HandleUserCreate(HttpListenerContext ctx, SessionInfo si)
	{
		if (!RequireAdmin(ctx, si))
		{
			return;
		}
		Dictionary<string, object> body = ReadJson(ctx);
		string username = Json.GetStr(body, "username", "").Trim();
		string role = Json.GetStr(body, "role", "viewer");
		string password = Json.GetStr(body, "password", "");
		bool mustChange = Json.Bool(Json.Get(body, "mustChangePassword"), dflt: true);
		if (!UserNameRe.IsMatch(username))
		{
			WriteJson(ctx, Json.Obj("error", "Логин: 2-32 символа, только латиница, цифры, точка, дефис, подчёркивание"), 400);
			return;
		}
		if (!Roles.IsValid(role))
		{
			WriteJson(ctx, Json.Obj("error", "Неизвестная роль: " + role), 400);
			return;
		}
		if (_auth.Exists(username))
		{
			WriteJson(ctx, Json.Obj("error", "Пользователь уже существует: " + username), 409);
			return;
		}
		bool generated = false;
		if (password.Length == 0)
		{
			password = Auth.GeneratePassword(14);
			generated = true;
		}
		if (password.Length < 8)
		{
			WriteJson(ctx, Json.Obj("error", "Пароль должен быть не короче 8 символов"), 400);
			return;
		}
		UserRecord u = new UserRecord();
		u.Username = username;
		u.Hash = Auth.Hash(password);
		u.Role = role;
		u.Enabled = Json.Bool(Json.Get(body, "enabled"), dflt: true);
		u.DisplayName = Json.GetStr(body, "displayName", "");
		u.Email = Json.GetStr(body, "email", "");
		u.MustChangePassword = mustChange;
		u.CreatedAt = DateTime.Now;
		foreach (object o in Json.AsArray(Json.Get(body, "companyIds")))
		{
			string id = Json.Str(o);
			if (id.Length > 0)
			{
				u.CompanyIds.Add(id);
			}
		}
		_auth.Add(u);
		_audit.Write(si.User, "user.create", username, "role=" + role);
		Dictionary<string, object> resp = Json.Obj();
		resp["ok"] = true;
		resp["username"] = username;
		if (generated)
		{
			resp["password"] = password;
		}
		resp["generated"] = generated;
		WriteJson(ctx, resp, 201);
	}

	private void HandleUserUpdate(HttpListenerContext ctx, SessionInfo si, string username)
	{
		if (!RequireAdmin(ctx, si))
		{
			return;
		}
		username = Uri.UnescapeDataString(username ?? "");
		if (!_auth.Exists(username))
		{
			WriteJson(ctx, Json.Obj("error", "Пользователь не найден: " + username), 404);
			return;
		}
		Dictionary<string, object> body = ReadJson(ctx);
		if (string.Equals(username, si.User, StringComparison.OrdinalIgnoreCase))
		{
			string newRole = Json.GetStr(body, "role", "");
			bool newEnabled = Json.Bool(Json.Get(body, "enabled"), dflt: true);
			if (newRole.Length > 0 && newRole != "admin")
			{
				WriteJson(ctx, Json.Obj("error", "Нельзя понизить собственную роль"), 400);
				return;
			}
			if (!newEnabled)
			{
				WriteJson(ctx, Json.Obj("error", "Нельзя отключить собственную учётную запись"), 400);
				return;
			}
		}
		if (!_auth.Update(username, body, out var error))
		{
			WriteJson(ctx, Json.Obj("error", error), 400);
			return;
		}
		_audit.Write(si.User, "user.update", username, "role=" + Json.GetStr(body, "role", "?") + " enabled=" + Json.Bool(Json.Get(body, "enabled"), dflt: true));
		WriteJson(ctx, Json.Obj("ok", true, "username", username), 200);
	}

	private void HandleUserDelete(HttpListenerContext ctx, SessionInfo si, string username)
	{
		if (RequireAdmin(ctx, si))
		{
			username = Uri.UnescapeDataString(username ?? "");
			if (string.Equals(username, si.User, StringComparison.OrdinalIgnoreCase))
			{
				WriteJson(ctx, Json.Obj("error", "Нельзя удалить собственную учётную запись"), 400);
				return;
			}
			if (!_auth.Delete(username, out var error))
			{
				WriteJson(ctx, Json.Obj("error", error), 400);
				return;
			}
			_audit.Write(si.User, "user.delete", username, "");
			WriteJson(ctx, Json.Obj("ok", true, "username", username), 200);
		}
	}

	private void HandleUserReset(HttpListenerContext ctx, SessionInfo si, string username)
	{
		if (!RequireAdmin(ctx, si))
		{
			return;
		}
		username = Uri.UnescapeDataString(username ?? "");
		if (!_auth.Exists(username))
		{
			WriteJson(ctx, Json.Obj("error", "Пользователь не найден: " + username), 404);
			return;
		}
		Dictionary<string, object> body = ReadJson(ctx);
		string password = Json.GetStr(body, "password", "");
		bool mustChange = Json.Bool(Json.Get(body, "mustChangePassword"), dflt: true);
		bool generated = false;
		if (password.Length == 0)
		{
			password = Auth.GeneratePassword(14);
			generated = true;
		}
		if (password.Length < 8)
		{
			WriteJson(ctx, Json.Obj("error", "Пароль должен быть не короче 8 символов"), 400);
			return;
		}
		_auth.ResetPassword(username, password, mustChange);
		_audit.Write(si.User, "user.resetPassword", username, "mustChange=" + mustChange);
		Dictionary<string, object> resp = Json.Obj();
		resp["ok"] = true;
		resp["username"] = username;
		if (generated)
		{
			resp["password"] = password;
		}
		resp["generated"] = generated;
		WriteJson(ctx, resp, 200);
	}

	private void HandleAudit(HttpListenerContext ctx, SessionInfo si)
	{
		if (RequireAdmin(ctx, si))
		{
			int limit = 200;
			string lim = QueryParam(ctx, "limit");
			if (!string.IsNullOrEmpty(lim))
			{
				int.TryParse(lim, out limit);
			}
			Dictionary<string, object> resp = Json.Obj();
			resp["items"] = _audit.Read(limit);
			WriteJson(ctx, resp, 200);
		}
	}
}
