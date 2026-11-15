using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BackupDotNet.Core;

public class Scheduler
{
	private readonly AgentCtx _ctx;

	private readonly DataStore _store;

	private readonly Settings _settings;

	private readonly Dictionary<string, DateTime> _nextRun = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

	private readonly object _sync = new object();

	private readonly ZabbixSender _sender;

	private readonly ZabbixApi _api;

	private Thread _thread;

	private volatile bool _stopping;

	private DateTime _lastHeartbeat = DateTime.MinValue;

	private readonly Dictionary<string, HashSet<string>> _produced = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

	private readonly object _producedSync = new object();

	private readonly object _runSync = new object();

	public string Version = "1.0.0";

	public DateTime StartedAt = DateTime.Now;

	public DateTime? LastRunAt;

	public int LastRunCount;

	/// <summary>Когда завершился последний тик планировщика.</summary>
	public DateTime? LastTickAt;

	/// <summary>Сколько длился последний тик (секунды).</summary>
	public double LastTickDurationSeconds;

	/// <summary>Сколько тиков отработано с момента запуска.</summary>
	public long Ticks;

	/// <summary>Сколько тиков завершились исключением.</summary>
	public long TickErrors;

	/// <summary>Идёт ли планировщик прямо сейчас.</summary>
	public bool IsRunning
	{
		get
		{
			Thread t = _thread;
			return t != null && t.IsAlive && !_stopping;
		}
	}

	/// <summary>Путь к очереди Zabbix (пустая строка, если отправка не настроена).</summary>
	public string ZabbixQueuePath
	{
		get
		{
			return Path.Combine(_settings.DataDir, "state", "zabbix-queue.ndjson");
		}
	}

	/// <summary>Настроена ли отправка в Zabbix.</summary>
	public bool ZabbixEnabled => _sender != null;

	public Scheduler(AgentCtx ctx)
	{
		_ctx = ctx;
		_store = ctx.Store;
		_settings = ctx.Settings;
		if (_settings.ZabbixEnabled && !string.IsNullOrEmpty(_settings.ZabbixServer))
		{
			_sender = new ZabbixSender(_settings.ZabbixServer, _settings.ZabbixPort, _settings.ZabbixTimeoutSeconds, _settings.ZabbixBatchSize, _settings.ZabbixBatchBytes, Path.Combine(_settings.DataDir, "state", "zabbix-queue.ndjson"));
		}
		if (_settings.ZabbixApiEnabled)
		{
			_api = new ZabbixApi(_settings.ZabbixApiUrl, _settings.ZabbixApiToken, _settings.ZabbixTimeoutSeconds);
		}
	}

	public void Start()
	{
		_stopping = false;
		_thread = new Thread(Loop);
		_thread.IsBackground = true;
		_thread.Name = "scheduler";
		_thread.Start();
	}

	public void Stop()
	{
		_stopping = true;
		if (_thread != null)
		{
			try
			{
				_thread.Join(5000);
			}
			catch
			{
			}
		}
	}

	private void Loop()
	{
		Log.Info("Планировщик запущен, тик " + _settings.TickSeconds + " с");
		SleepInterruptible(3000);
		while (!_stopping)
		{
			DateTime tickStart = DateTime.Now;
			try
			{
				List<Company> due = PickDue();
				if (due.Count > 0)
				{
					Log.Info("К проверке: " + due.Count + " источник(ов)");
					RunCompanies(due);
				}
				FlushZabbixQueue();
				SendHeartbeatIfNeeded();
			}
			catch (Exception ex)
			{
				Log.Error("Ошибка в цикле планировщика", ex);
				TickErrors++;
			}
			// Отставание = сколько тик занял сверх ожидаемого.
			// Если оно стабильно растёт, значит проверки не успевают за интервалом.
			LastTickAt = DateTime.Now;
			LastTickDurationSeconds = (LastTickAt.Value - tickStart).TotalSeconds;
			Ticks++;
			SleepInterruptible(Math.Max(5, _settings.TickSeconds) * 1000);
		}
		Log.Info("Планировщик остановлен");
	}

	private void SleepInterruptible(int ms)
	{
		int step = 250;
		for (int elapsed = 0; elapsed < ms; elapsed += step)
		{
			if (_stopping)
			{
				break;
			}
			Thread.Sleep(step);
		}
	}

	private List<Company> PickDue()
	{
		List<Company> result = new List<Company>();
		DateTime now = DateTime.Now;
		foreach (Company c in _ctx.Config.Companies)
		{
			if (!c.Enabled || !string.IsNullOrEmpty(c.RemoteAgent))
			{
				continue;
			}
			List<Source> dueSources = new List<Source>();
			foreach (Source s in c.Sources)
			{
				if (!s.Enabled)
				{
					continue;
				}
				string key = c.Id + "|" + s.Id;
				DateTime next;
				lock (_sync)
				{
					if (!_nextRun.TryGetValue(key, out next))
					{
						next = now;
						_nextRun[key] = now;
					}
				}
				if (now >= next)
				{
					dueSources.Add(s);
					int interval = ((s.IntervalMinutes > 0) ? s.IntervalMinutes : _settings.DefaultIntervalMinutes);
					lock (_sync)
					{
						_nextRun[key] = now.AddMinutes(interval);
					}
				}
			}
			if (dueSources.Count > 0)
			{
				Company copy = c;
				copy.Sources = dueSources;
				result.Add(copy);
			}
		}
		return result;
	}

	public void RunCompanies(List<Company> companies)
	{
		lock (_runSync)
		{
			RunCompaniesInner(companies);
		}
	}

	private void RunCompaniesInner(List<Company> companies)
	{
		List<Action> actions = new List<Action>();
		lock (_producedSync)
		{
			_produced.Clear();
		}
		foreach (Company c in companies)
		{
			Company cc = c;
			foreach (Source s in cc.Sources)
			{
				Source ss = s;
				actions.Add(() =>
				{
					RunSource(cc, ss);
				});
			}
		}
		ParallelOptions po = new ParallelOptions();
		po.MaxDegreeOfParallelism = Math.Max(1, _settings.MaxParallelChecks);
		Parallel.ForEach(actions, po, (Action a) =>
		{
			try
			{
				a();
			}
			catch (Exception ex)
			{
				Log.Error("Ошибка выполнения проверки", ex);
			}
		});
		foreach (Company c2 in companies)
		{
			List<string> jobs = new List<string>();
			lock (_producedSync)
			{
				if (_produced.TryGetValue(c2.Id, out var set))
				{
					jobs.AddRange(set);
				}
			}
			if (jobs.Count > 0)
			{
				_store.PruneJobs(c2.Id, jobs);
			}
		}
		LastRunAt = DateTime.Now;
		LastRunCount = actions.Count;
	}

	public void RunAll()
	{
		List<Company> all = new List<Company>();
		foreach (Company c in _ctx.Config.Companies)
		{
			if (c.Enabled)
			{
				if (!string.IsNullOrEmpty(c.RemoteAgent))
				{
					Log.Debug(c.Id + ": пропущена, проверяется агентом " + c.RemoteAgent);
				}
				else
				{
					all.Add(c);
				}
			}
		}
		if (all.Count > 0)
		{
			RunCompanies(all);
		}
	}

	public void RunCompany(string companyId)
	{
		Company c = _ctx.Config.Find(companyId);
		if (c != null)
		{
			if (!string.IsNullOrEmpty(c.RemoteAgent))
			{
				Log.Info(companyId + ": компания на агенте " + c.RemoteAgent + " — запрошен внеочередной прогон");
				return;
			}
			List<Company> one = new List<Company>();
			one.Add(c);
			RunCompanies(one);
		}
	}

	private void Track(string companyId, string job)
	{
		lock (_producedSync)
		{
			if (!_produced.TryGetValue(companyId, out var set))
			{
				set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				_produced[companyId] = set;
			}
			set.Add(job);
		}
	}

	private void RunSource(Company company, Source source)
	{
		ICollector collector = Collectors.Get(source.Type);
		if (collector == null)
		{
			BackupRun bad = new BackupRun();
			bad.Timestamp = DateTime.Now;
			bad.CompanyId = company.Id;
			bad.CompanyName = company.Name;
			bad.Job = source.Id;
			bad.Kind = source.Type;
			bad.SourceId = source.Id;
			bad.Status = BackupStatus.NoData;
			bad.Message = "неизвестный тип источника: " + source.Type;
			_store.Record(bad);
			Track(company.Id, bad.Job);
			Log.Warn(company.Id + ": неизвестный тип источника " + source.Type);
			return;
		}
		List<CollectResult> results;
		try
		{
			results = collector.Collect(company, source, _ctx);
		}
		catch (Exception ex)
		{
			Log.Error("Коллектор " + source.Type + " для " + company.Id + " упал", ex);
			CollectResult cr = new CollectResult();
			cr.CompanyId = company.Id;
			cr.CompanyName = company.Name;
			cr.SourceId = source.Id;
			cr.Job = source.GetStr("jobName", source.Id);
			cr.Kind = source.Type;
			cr.Reachable = false;
			cr.Fail(ex.Message);
			results = new List<CollectResult>();
			results.Add(cr);
		}
		Ingest(company, source, results);
	}

	public void Ingest(Company company, Source source, List<CollectResult> results)
	{
		List<ZabbixValue> values = new List<ZabbixValue>();
		List<Dictionary<string, object>> discovery = new List<Dictionary<string, object>>();
		int total = 0;
		int failed = 0;
		BackupStatus worst = BackupStatus.Skip;
		foreach (CollectResult cr in results)
		{
			JobState prev = _store.GetState(company.Id, cr.Job);
			BackupRun run = PolicyEngine.Evaluate(company, source, cr, prev);
			_store.Record(run);
			Track(company.Id, run.Job);
			total++;
			if (run.Status == BackupStatus.Fail)
			{
				failed++;
			}
			worst = StatusNames.Worst(worst, run.Status);
			string job = run.Job;
			string host = ZabbixHost(company);
			long clock = ToUnix(run.Timestamp);
			string key = "bc.job.status[" + job + "]";
			int status = (int)run.Status;
			values.Add(new ZabbixValue(host, key, status.ToString(), clock));
			if (run.LastSuccess.HasValue)
			{
				long ageSec = (long)(DateTime.UtcNow - PolicyEngine.ToUtc(run.LastSuccess.Value)).TotalSeconds;
				if (ageSec < 0)
				{
					ageSec = 0L;
				}
				values.Add(new ZabbixValue(host, "bc.job.age[" + job + "]", ageSec.ToString(), clock));
			}
			if (run.SizeBytes > 0)
			{
				values.Add(new ZabbixValue(host, "bc.job.size[" + job + "]", run.SizeBytes.ToString(), clock));
			}
			values.Add(new ZabbixValue(host, "bc.job.duration[" + job + "]", run.DurationSeconds.ToString(), clock));
			values.Add(new ZabbixValue(host, "bc.job.failures[" + job + "]", run.ConsecutiveFailures.ToString(), clock));
			values.Add(new ZabbixValue(host, "bc.job.message[" + job + "]", Truncate(run.Message, 500), clock));
			if (run.ResponseMs > 0)
			{
				values.Add(new ZabbixValue(host, "bc.job.responseMs[" + job + "]", run.ResponseMs.ToString(), clock));
			}
			Dictionary<string, object> disc = Json.Obj();
			disc["{#JOB}"] = job;
			disc["{#KIND}"] = run.Kind;
			discovery.Add(disc);
			Log.Info(string.Format("{0} / {1}: {2}{3}", company.Id, job, StatusNames.Of(run.Status), string.IsNullOrEmpty(run.Message) ? "" : (" — " + run.Message)));
		}
		if (_sender != null && results.Count > 0)
		{
			string host2 = ZabbixHost(company);
			long clock2 = ToUnix(DateTime.Now);
			List<JobState> companyStates = new List<JobState>();
			foreach (JobState st in _store.GetStates())
			{
				if (string.Equals(st.CompanyId, company.Id, StringComparison.OrdinalIgnoreCase))
				{
					companyStates.Add(st);
				}
			}
			List<object> fullDiscovery = new List<object>();
			HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (JobState st2 in companyStates)
			{
				if (seen.Add(st2.Job))
				{
					Dictionary<string, object> d = Json.Obj();
					d["{#JOB}"] = st2.Job;
					d["{#KIND}"] = st2.Kind;
					d["{#UPTIME}"] = (st2.IsUptime ? "1" : "0");
					fullDiscovery.Add(d);
				}
			}
			if (fullDiscovery.Count > 0)
			{
				Dictionary<string, object> discObj = Json.Obj();
				discObj["data"] = fullDiscovery;
				values.Add(new ZabbixValue(host2, "bc.discovery", Json.Write(discObj), clock2));
			}
			int companyTotal = companyStates.Count;
			int companyFailed = 0;
			BackupStatus companyWorst = BackupStatus.Skip;
			foreach (JobState st3 in companyStates)
			{
				if (st3.Status == BackupStatus.Fail)
				{
					companyFailed++;
				}
				companyWorst = StatusNames.Worst(companyWorst, st3.Status);
			}
			if (companyTotal == 0)
			{
				companyTotal = total;
				companyFailed = failed;
				companyWorst = worst;
			}
			int num = (int)companyWorst;
			values.Add(new ZabbixValue(host2, "bc.company.status", num.ToString(), clock2));
			values.Add(new ZabbixValue(host2, "bc.company.jobs.failed", companyFailed.ToString(), clock2));
			values.Add(new ZabbixValue(host2, "bc.company.jobs.total", companyTotal.ToString(), clock2));
			values.Add(new ZabbixValue(host2, "bc.jobs.failed", companyFailed.ToString(), clock2));
			values.Add(new ZabbixValue(host2, "bc.jobs.total", companyTotal.ToString(), clock2));
			string err = _sender.Send(values);
			if (!string.IsNullOrEmpty(err))
			{
				Log.Warn("Zabbix: " + err);
			}
			else
			{
				Log.Debug("Zabbix: отправлено " + values.Count + " значений для " + host2);
			}
		}
		EnsureZabbixHost(company);
	}

	private void EnsureZabbixHost(Company company)
	{
		if (_api == null || !_api.IsConfigured || !_settings.ZabbixAutoCreateHosts)
		{
			return;
		}
		string host = ZabbixHost(company);
		if (string.IsNullOrEmpty(host))
		{
			return;
		}
		try
		{
			Dictionary<string, string> macros = new Dictionary<string, string>();
			macros["{$BC.MAX_AGE_H}"] = company.Policy.FreshnessHours + "h";
			if (company.Policy.MinSizeBytes > 0)
			{
				macros["{$BC.MIN_SIZE}"] = company.Policy.MinSizeBytes.ToString();
			}
			if (company.Zabbix.Macros != null)
			{
				foreach (KeyValuePair<string, string> kv in company.Zabbix.Macros)
				{
					macros[kv.Key] = kv.Value;
				}
			}
			Dictionary<string, string> tags = new Dictionary<string, string>();
			tags["company"] = company.Id;
			_api.EnsureHost(host, string.IsNullOrEmpty(company.Zabbix.DisplayName) ? company.Name : company.Zabbix.DisplayName, _settings.ZabbixHostGroup, _settings.ZabbixTemplateName, macros, tags);
		}
		catch (Exception ex)
		{
			Log.Warn("Zabbix API (" + company.Id + "): " + ex.Message);
		}
	}

	private string ZabbixHost(Company company)
	{
		if (!string.IsNullOrEmpty(company.Zabbix.Host))
		{
			return company.Zabbix.Host;
		}
		return "BC-" + company.Id;
	}

	private void FlushZabbixQueue()
	{
		if (_sender != null)
		{
			int sent = _sender.FlushQueue();
			if (sent > 0)
			{
				Log.Info("Zabbix: дослано из очереди " + sent + " значений");
			}
		}
	}

	private void SendHeartbeatIfNeeded()
	{
		if (_sender == null || (DateTime.Now - _lastHeartbeat).TotalMinutes < 5.0)
		{
			return;
		}
		_lastHeartbeat = DateTime.Now;
		string agentHost = "BC-AGENT";
		long clock = ToUnix(DateTime.Now);
		List<ZabbixValue> values = new List<ZabbixValue>();
		values.Add(new ZabbixValue(agentHost, "bc.agent.heartbeat", clock.ToString(), clock));
		values.Add(new ZabbixValue(agentHost, "bc.agent.version", Version, clock));
		List<JobState> states = _store.GetStates();
		int failed = 0;
		foreach (JobState s in states)
		{
			if (s.Status == BackupStatus.Fail)
			{
				failed++;
			}
		}
		values.Add(new ZabbixValue(agentHost, "bc.jobs.total", states.Count.ToString(), clock));
		values.Add(new ZabbixValue(agentHost, "bc.jobs.failed", failed.ToString(), clock));
		string err = _sender.Send(values);
		if (!string.IsNullOrEmpty(err))
		{
			Log.Warn("Zabbix heartbeat: " + err);
		}
	}

	public static long ToUnix(DateTime dt)
	{
		return (long)(dt.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
	}

	private static string Truncate(string s, int n)
	{
		if (string.IsNullOrEmpty(s))
		{
			return "";
		}
		if (s.Length > n)
		{
			return s.Substring(0, n);
		}
		return s;
	}
}
