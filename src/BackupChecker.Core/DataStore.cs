using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace BackupChecker.Core;

public class DataStore
{
	private readonly object _sync = new object();

	private readonly string _statePath;

	private readonly string _journalDir;

	private readonly string _rollupDir;

	private Dictionary<string, JobState> _state = new Dictionary<string, JobState>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, Dictionary<string, DayAgg>> _rollups = new Dictionary<string, Dictionary<string, DayAgg>>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, Dictionary<string, HourAgg>> _uptime = new Dictionary<string, Dictionary<string, HourAgg>>(StringComparer.OrdinalIgnoreCase);

	public DataStore(string dataDir)
	{
		_statePath = Path.Combine(dataDir, "state", "jobs.json");
		_journalDir = Path.Combine(dataDir, "journal");
		_rollupDir = Path.Combine(dataDir, "rollups");
		Directory.CreateDirectory(Path.GetDirectoryName(_statePath));
		Directory.CreateDirectory(_journalDir);
		Directory.CreateDirectory(_rollupDir);
	}

	public void Load()
	{
		lock (_sync)
		{
			_state.Clear();
			if (File.Exists(_statePath))
			{
				try
				{
					Dictionary<string, object> root = Json.ParseObject(File.ReadAllText(_statePath));
					foreach (KeyValuePair<string, object> kv in root)
					{
						_state[kv.Key] = JobState.FromJson(kv.Key, Json.AsObject(kv.Value));
					}
				}
				catch (Exception ex)
				{
					Log.Warn("Не удалось прочитать state/jobs.json: " + ex.Message);
				}
			}
			_rollups.Clear();
			string ym = DateTime.Now.ToString("yyyy-MM", CultureInfo.InvariantCulture);
			LoadRollup(ym);
			LoadRollup(PreviousMonth(ym));
			_uptime.Clear();
			LoadUptime(ym);
			LoadUptime(PreviousMonth(ym));
		}
	}

	private void LoadUptime(string ym)
	{
		string path = Path.Combine(_rollupDir, "uptime-" + ym + ".json");
		if (!File.Exists(path))
		{
			return;
		}
		try
		{
			Dictionary<string, object> root = Json.ParseObject(File.ReadAllText(path));
			foreach (KeyValuePair<string, object> job in root)
			{
				Dictionary<string, object> hours = Json.AsObject(job.Value);
				Dictionary<string, HourAgg> target = new Dictionary<string, HourAgg>(StringComparer.OrdinalIgnoreCase);
				foreach (KeyValuePair<string, object> h in hours)
				{
					target[h.Key] = HourAgg.FromJson(Json.AsObject(h.Value));
				}
				_uptime[job.Key] = target;
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Не удалось прочитать uptime-" + ym + ": " + ex.Message);
		}
	}

	private static string PreviousMonth(string ym)
	{
		if (!DateTime.TryParseExact(ym + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
		{
			return ym;
		}
		return d.AddMonths(-1).ToString("yyyy-MM", CultureInfo.InvariantCulture);
	}

	private void LoadRollup(string ym)
	{
		string path = Path.Combine(_rollupDir, ym + ".json");
		if (!File.Exists(path))
		{
			return;
		}
		try
		{
			Dictionary<string, object> root = Json.ParseObject(File.ReadAllText(path));
			foreach (KeyValuePair<string, object> comp in root)
			{
				Dictionary<string, object> days = Json.AsObject(comp.Value);
				Dictionary<string, DayAgg> target = new Dictionary<string, DayAgg>(StringComparer.OrdinalIgnoreCase);
				foreach (KeyValuePair<string, object> day in days)
				{
					target[day.Key] = DayAgg.FromJson(Json.AsObject(day.Value));
				}
				_rollups[comp.Key] = target;
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Не удалось прочитать rollup " + ym + ": " + ex.Message);
		}
	}

	public void Record(BackupRun run)
	{
		lock (_sync)
		{
			string key = JobState.MakeKey(run.CompanyId, run.Job);
			if (!_state.TryGetValue(key, out var st))
			{
				st = new JobState();
				st.Key = key;
				st.CompanyId = run.CompanyId;
				st.Job = run.Job;
			}
			st.CompanyName = run.CompanyName;
			st.Kind = run.Kind;
			st.Status = run.Status;
			st.LastSuccess = run.LastSuccess;
			st.LastAttempt = run.LastAttempt;
			st.Updated = run.Timestamp;
			st.SizeBytes = run.SizeBytes;
			st.DurationSeconds = run.DurationSeconds;
			st.ConsecutiveFailures = run.ConsecutiveFailures;
			st.Message = run.Message;
			st.Target = run.Target;
			st.ResponseMs = run.ResponseMs;
			st.IsUptime = run.IsUptime;
			st.Details = run.Details;
			_state[key] = st;
			AppendJournal(run);
			AddToRollup(run);
			SaveStateLocked();
			SaveRollupLocked(run.Timestamp.ToString("yyyy-MM", CultureInfo.InvariantCulture));
			if (run.IsUptime)
			{
				AddToUptime(run);
				SaveUptimeLocked(run.Timestamp.ToString("yyyy-MM", CultureInfo.InvariantCulture));
			}
		}
	}

	private void AddToUptime(BackupRun run)
	{
		string jobKey = JobState.MakeKey(run.CompanyId, run.Job);
		string hour = run.Timestamp.ToString("yyyy-MM-dd'T'HH", CultureInfo.InvariantCulture);
		if (!_uptime.TryGetValue(jobKey, out var hours))
		{
			hours = new Dictionary<string, HourAgg>(StringComparer.OrdinalIgnoreCase);
			_uptime[jobKey] = hours;
		}
		if (!hours.TryGetValue(hour, out var agg))
		{
			agg = (hours[hour] = new HourAgg());
		}
		agg.Add(run.Status, run.ResponseMs);
	}

	private void SaveUptimeLocked(string ym)
	{
		try
		{
			Dictionary<string, object> root = Json.Obj();
			foreach (KeyValuePair<string, Dictionary<string, HourAgg>> job in _uptime)
			{
				Dictionary<string, object> hours = Json.Obj();
				bool any = false;
				foreach (KeyValuePair<string, HourAgg> h in job.Value)
				{
					if (h.Key.StartsWith(ym, StringComparison.Ordinal))
					{
						hours[h.Key] = h.Value.ToJson();
						any = true;
					}
				}
				if (any)
				{
					root[job.Key] = hours;
				}
			}
			ConfigLoader.AtomicWrite(Path.Combine(_rollupDir, "uptime-" + ym + ".json"), Json.WritePretty(root));
		}
		catch (Exception ex)
		{
			Log.Error("Не удалось сохранить почасовые свёртки", ex);
		}
	}

	private void AppendJournal(BackupRun run)
	{
		try
		{
			string ym = run.Timestamp.ToString("yyyy-MM", CultureInfo.InvariantCulture);
			string path = Path.Combine(_journalDir, ym + ".ndjson");
			File.AppendAllText(path, Json.Write(run.ToJson()) + "\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		}
		catch (Exception ex)
		{
			Log.Error("Не удалось записать журнал", ex);
		}
	}

	private void AddToRollup(BackupRun run)
	{
		string day = run.Timestamp.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		if (!_rollups.TryGetValue(run.CompanyId, out var comp))
		{
			comp = new Dictionary<string, DayAgg>(StringComparer.OrdinalIgnoreCase);
			_rollups[run.CompanyId] = comp;
		}
		if (!comp.TryGetValue(day, out var agg))
		{
			agg = (comp[day] = new DayAgg());
		}
		agg.Add(run.Status);
	}

	private void SaveStateLocked()
	{
		try
		{
			Dictionary<string, object> root = Json.Obj();
			foreach (KeyValuePair<string, JobState> kv in _state)
			{
				root[kv.Key] = kv.Value.ToJson();
			}
			ConfigLoader.AtomicWrite(_statePath, Json.WritePretty(root));
		}
		catch (Exception ex)
		{
			Log.Error("Не удалось сохранить состояние", ex);
		}
	}

	private void SaveRollupLocked(string ym)
	{
		try
		{
			Dictionary<string, object> root = Json.Obj();
			foreach (KeyValuePair<string, Dictionary<string, DayAgg>> comp in _rollups)
			{
				Dictionary<string, object> days = Json.Obj();
				bool any = false;
				foreach (KeyValuePair<string, DayAgg> d in comp.Value)
				{
					if (d.Key.StartsWith(ym, StringComparison.Ordinal))
					{
						days[d.Key] = d.Value.ToJson();
						any = true;
					}
				}
				if (any)
				{
					root[comp.Key] = days;
				}
			}
			ConfigLoader.AtomicWrite(Path.Combine(_rollupDir, ym + ".json"), Json.WritePretty(root));
		}
		catch (Exception ex)
		{
			Log.Error("Не удалось сохранить свёртку", ex);
		}
	}

	public List<JobState> GetStates()
	{
		lock (_sync)
		{
			return new List<JobState>(_state.Values);
		}
	}

	public int PruneOrphans(ICollection<string> knownCompanyIds)
	{
		lock (_sync)
		{
			HashSet<string> known = new HashSet<string>(knownCompanyIds, StringComparer.OrdinalIgnoreCase);
			List<string> remove = new List<string>();
			foreach (KeyValuePair<string, JobState> kv in _state)
			{
				if (!known.Contains(kv.Value.CompanyId))
				{
					remove.Add(kv.Key);
				}
			}
			foreach (string key in remove)
			{
				_state.Remove(key);
			}
			if (remove.Count > 0)
			{
				Log.Info("Удалено состояний несуществующих компаний: " + remove.Count);
				SaveStateLocked();
			}
			return remove.Count;
		}
	}

	public int PruneJobs(string companyId, ICollection<string> knownJobs)
	{
		lock (_sync)
		{
			HashSet<string> known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (string j in knownJobs)
			{
				known.Add(JobState.MakeKey(companyId, j));
			}
			List<string> remove = new List<string>();
			foreach (KeyValuePair<string, JobState> kv in _state)
			{
				if (string.Equals(kv.Value.CompanyId, companyId, StringComparison.OrdinalIgnoreCase) && !known.Contains(kv.Key))
				{
					remove.Add(kv.Key);
				}
			}
			foreach (string key in remove)
			{
				_state.Remove(key);
			}
			if (remove.Count > 0)
			{
				Log.Info("Удалено устаревших заданий " + companyId + ": " + remove.Count);
				SaveStateLocked();
			}
			return remove.Count;
		}
	}

	public JobState GetState(string companyId, string job)
	{
		lock (_sync)
		{
			if (_state.TryGetValue(JobState.MakeKey(companyId, job), out var st))
			{
				return st;
			}
			return null;
		}
	}

	public List<object> GetUptimeSeries(string companyId, string job, int hours)
	{
		if (hours <= 0)
		{
			hours = 24;
		}
		if (hours > 2160)
		{
			hours = 2160;
		}
		string jobKey = JobState.MakeKey(companyId, job);
		List<object> result = new List<object>();
		lock (_sync)
		{
			if (!_uptime.TryGetValue(jobKey, out var data))
			{
				data = new Dictionary<string, HourAgg>(StringComparer.OrdinalIgnoreCase);
			}
			DateTime start = DateTime.Now.AddHours(-hours + 1);
			start = new DateTime(start.Year, start.Month, start.Day, start.Hour, 0, 0);
			for (int i = 0; i < hours; i++)
			{
				string key = start.AddHours(i).ToString("yyyy-MM-dd'T'HH", CultureInfo.InvariantCulture);
				if (data.TryGetValue(key, out var agg) && agg.Total > 0)
				{
					Dictionary<string, object> d = Json.Obj();
					d["hour"] = key;
					d["status"] = StatusNames.Of(agg.Worst);
					d["ok"] = agg.Ok;
					d["warn"] = agg.Warn;
					d["fail"] = agg.Fail;
					d["avgMs"] = agg.AvgMs;
					d["uptime"] = agg.Uptime;
					result.Add(d);
				}
				else
				{
					result.Add(null);
				}
			}
			return result;
		}
	}

	public Dictionary<string, object> GetUptimeSummary(string companyId, string job, int hours)
	{
		string jobKey = JobState.MakeKey(companyId, job);
		int ok = 0;
		int warn = 0;
		int fail = 0;
		long totalMs = 0L;
		int counted = 0;
		lock (_sync)
		{
			if (_uptime.TryGetValue(jobKey, out var data))
			{
				DateTime start = DateTime.Now.AddHours(-hours);
				foreach (KeyValuePair<string, HourAgg> kv in data)
				{
					if (DateTime.TryParseExact(kv.Key, "yyyy-MM-dd'T'HH", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) && !(t < start))
					{
						ok += kv.Value.Ok;
						warn += kv.Value.Warn;
						fail += kv.Value.Fail;
						if (kv.Value.Counted > 0)
						{
							totalMs += kv.Value.TotalMs;
							counted += kv.Value.Counted;
						}
					}
				}
			}
		}
		int total = ok + warn + fail;
		Dictionary<string, object> r = Json.Obj();
		r["ok"] = ok;
		r["warn"] = warn;
		r["fail"] = fail;
		r["checks"] = total;
		r["avgMs"] = ((counted > 0) ? (totalMs / counted) : 0);
		r["uptime"] = ((total > 0) ? Math.Round((double)(ok + warn) * 100.0 / (double)total, 2) : 0.0);
		return r;
	}

	public int RenameCompany(string oldId, string newId)
	{
		int moved = 0;
		lock (_sync)
		{
			List<string> keys = new List<string>();
			foreach (KeyValuePair<string, JobState> kv in _state)
			{
				if (string.Equals(kv.Value.CompanyId, oldId, StringComparison.OrdinalIgnoreCase))
				{
					keys.Add(kv.Key);
				}
			}
			foreach (string oldKey in keys)
			{
				JobState st = _state[oldKey];
				_state.Remove(oldKey);
				st.CompanyId = newId;
				st.Key = JobState.MakeKey(newId, st.Job);
				_state[st.Key] = st;
				moved++;
			}
			if (_rollups.TryGetValue(oldId, out var days))
			{
				_rollups.Remove(oldId);
				_rollups[newId] = days;
			}
			List<string> upKeys = new List<string>();
			foreach (string k in _uptime.Keys)
			{
				if (k.StartsWith(oldId + "|", StringComparison.OrdinalIgnoreCase))
				{
					upKeys.Add(k);
				}
			}
			foreach (string k2 in upKeys)
			{
				Dictionary<string, HourAgg> hours = _uptime[k2];
				_uptime.Remove(k2);
				string newKey = newId + k2.Substring(oldId.Length);
				_uptime[newKey] = hours;
			}
			SaveStateLocked();
			SaveRollupLocked(DateTime.Now.ToString("yyyy-MM", CultureInfo.InvariantCulture));
			SaveUptimeLocked(DateTime.Now.ToString("yyyy-MM", CultureInfo.InvariantCulture));
		}
		Log.Info("Переименование " + oldId + " → " + newId + ": перенесено заданий " + moved);
		return moved;
	}

	public Dictionary<string, Dictionary<string, DayAgg>> GetMonth(string ym)
	{
		lock (_sync)
		{
			_rollups.ContainsKey(GetAnyCompanyKey());
			Dictionary<string, Dictionary<string, DayAgg>> copy = new Dictionary<string, Dictionary<string, DayAgg>>(StringComparer.OrdinalIgnoreCase);
			foreach (KeyValuePair<string, Dictionary<string, DayAgg>> comp in _rollups)
			{
				Dictionary<string, DayAgg> days = new Dictionary<string, DayAgg>(StringComparer.OrdinalIgnoreCase);
				foreach (KeyValuePair<string, DayAgg> d in comp.Value)
				{
					if (d.Key.StartsWith(ym, StringComparison.Ordinal))
					{
						days[d.Key] = d.Value;
					}
				}
				if (days.Count > 0)
				{
					copy[comp.Key] = days;
				}
			}
			return copy;
		}
	}

	private string GetAnyCompanyKey()
	{
		using (Dictionary<string, Dictionary<string, DayAgg>>.Enumerator enumerator = _rollups.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				return enumerator.Current.Key;
			}
		}
		return "";
	}

	public List<BackupRun> GetJournal(string companyId, int limit)
	{
		List<BackupRun> result = new List<BackupRun>();
		lock (_sync)
		{
			string ym = DateTime.Now.ToString("yyyy-MM", CultureInfo.InvariantCulture);
			List<string> files = new List<string>();
			string cur = Path.Combine(_journalDir, ym + ".ndjson");
			if (File.Exists(cur))
			{
				files.Add(cur);
			}
			string prev = Path.Combine(_journalDir, PreviousMonth(ym) + ".ndjson");
			if (File.Exists(prev))
			{
				files.Insert(0, prev);
			}
			for (int fi = files.Count - 1; fi >= 0 && result.Count < limit; fi--)
			{
				string[] lines;
				try
				{
					lines = File.ReadAllLines(files[fi]);
				}
				catch
				{
					continue;
				}
				int i = lines.Length - 1;
				while (i >= 0 && result.Count < limit)
				{
					string line = lines[i];
					if (line.Length >= 3)
					{
						try
						{
							BackupRun r = BackupRun.FromJson(Json.ParseObject(line));
							if (string.IsNullOrEmpty(companyId) || string.Equals(r.CompanyId, companyId, StringComparison.OrdinalIgnoreCase))
							{
								result.Add(r);
							}
						}
						catch
						{
						}
					}
					i--;
				}
			}
		}
		return result;
	}

	public List<BackupRun> GetHistory(string companyId, string job, int limit)
	{
		List<BackupRun> all = GetJournal(companyId, 500);
		List<BackupRun> result = new List<BackupRun>();
		foreach (BackupRun r in all)
		{
			if (job == null || string.Equals(r.Job, job, StringComparison.OrdinalIgnoreCase))
			{
				result.Add(r);
				if (result.Count >= limit)
				{
					break;
				}
			}
		}
		return result;
	}

	public List<BackupRun> GetDay(string companyId, DateTime date, int limit)
	{
		List<BackupRun> result = new List<BackupRun>();
		if (limit <= 0)
		{
			limit = 500;
		}
		string ym = date.ToString("yyyy-MM", CultureInfo.InvariantCulture);
		string path = Path.Combine(_journalDir, ym + ".ndjson");
		if (!File.Exists(path))
		{
			return result;
		}
		string[] lines;
		try
		{
			lines = File.ReadAllLines(path);
		}
		catch
		{
			return result;
		}
		lock (_sync)
		{
			int i = lines.Length - 1;
			while (i >= 0 && result.Count < limit)
			{
				string line = lines[i];
				if (line.Length >= 3)
				{
					try
					{
						BackupRun r = BackupRun.FromJson(Json.ParseObject(line));
						if (!(r.Timestamp.Date != date.Date) && (string.IsNullOrEmpty(companyId) || string.Equals(r.CompanyId, companyId, StringComparison.OrdinalIgnoreCase)))
						{
							result.Add(r);
						}
					}
					catch
					{
					}
				}
				i--;
			}
		}
		return result;
	}

	public List<BackupRun> GetJournalRange(string companyId, DateTime from, DateTime to, int limit)
	{
		return GetJournalRange(companyId, null, from, to, limit);
	}

	public List<BackupRun> GetJournalRange(string companyId, string job, DateTime from, DateTime to, int limit)
	{
		List<BackupRun> result = new List<BackupRun>();
		if (limit <= 0)
		{
			limit = 500;
		}
		List<string> months = new List<string>();
		DateTime cur = new DateTime(from.Year, from.Month, 1);
		DateTime end = new DateTime(to.Year, to.Month, 1);
		int guard = 0;
		while (cur <= end && guard++ < 120)
		{
			months.Add(cur.ToString("yyyy-MM", CultureInfo.InvariantCulture));
			cur = cur.AddMonths(1);
		}
		for (int mi = months.Count - 1; mi >= 0 && result.Count < limit; mi--)
		{
			string path = Path.Combine(_journalDir, months[mi] + ".ndjson");
			if (!File.Exists(path))
			{
				continue;
			}
			string[] lines;
			try
			{
				lines = File.ReadAllLines(path);
			}
			catch
			{
				continue;
			}
			lock (_sync)
			{
				int i = lines.Length - 1;
				while (i >= 0 && result.Count < limit)
				{
					string line = lines[i];
					if (line.Length >= 3)
					{
						try
						{
							BackupRun r = BackupRun.FromJson(Json.ParseObject(line));
							if (!(r.Timestamp.Date < from.Date) && !(r.Timestamp.Date > to.Date) && (string.IsNullOrEmpty(companyId) || string.Equals(r.CompanyId, companyId, StringComparison.OrdinalIgnoreCase)) && (string.IsNullOrEmpty(job) || string.Equals(r.Job, job, StringComparison.OrdinalIgnoreCase)))
							{
								result.Add(r);
							}
						}
						catch
						{
						}
					}
					i--;
				}
			}
		}
		return result;
	}
}
