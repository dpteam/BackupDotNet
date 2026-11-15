using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using BackupChecker.Core;

namespace BackupChecker.Manage;

public class OpQueue
{
	private const int KeepInMemory = 200;

	private readonly string _opsDir;

	private readonly Settings _settings;

	private readonly ConfigLoader _config;

	private readonly object _sync = new object();

	private readonly object _journalSync = new object();

	private readonly List<OpJob> _jobs = new List<OpJob>();

	private readonly Queue<OpJob> _queue = new Queue<OpJob>();

	private Thread _worker;

	private volatile bool _stopping;

	public OpQueue(Settings settings, ConfigLoader config)
	{
		_settings = settings;
		_config = config;
		_opsDir = Path.Combine(settings.DataDir, "ops");
		try
		{
			Directory.CreateDirectory(_opsDir);
		}
		catch
		{
		}
		_worker = new Thread(Loop);
		_worker.IsBackground = true;
		_worker.Name = "ops";
		_worker.Start();
	}

	public void Stop()
	{
		_stopping = true;
	}

	public OpJob Submit(OpDef def, string target, Dictionary<string, string> prms, string user, string confirmText, out string error)
	{
		error = "";
		if (def == null)
		{
			error = "неизвестная операция";
			return null;
		}
		List<string> problems = Catalog.Validate(def, prms);
		if (problems.Count > 0)
		{
			error = string.Join("; ", problems.ToArray());
			return null;
		}
		if (string.IsNullOrEmpty(target))
		{
			error = "не задана цель";
			return null;
		}
		if (def.NeedsConfirmText && !string.Equals((confirmText ?? "").Trim(), target, StringComparison.OrdinalIgnoreCase))
		{
			error = "для этой операции введите имя машины (" + target + ") в поле подтверждения";
			return null;
		}
		OpJob job = new OpJob();
		job.Id = "op-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 4);
		job.Op = def.Id;
		job.OpTitle = def.Title;
		job.Target = target;
		job.Risk = def.Risk;
		job.RequestedBy = user ?? "";
		job.Params = new Dictionary<string, string>();
		if (prms != null)
		{
			foreach (KeyValuePair<string, string> kv in prms)
			{
				job.Params[kv.Key] = kv.Value ?? "";
			}
		}
		lock (_sync)
		{
			_jobs.Insert(0, job);
			while (_jobs.Count > 200)
			{
				_jobs.RemoveAt(_jobs.Count - 1);
			}
		}
		if (string.Equals(def.Risk, "low", StringComparison.OrdinalIgnoreCase))
		{
			job.ApprovedBy = job.RequestedBy;
			Enqueue(job, def);
		}
		else
		{
			job.Status = "pending";
		}
		return job;
	}

	public bool Approve(string jobId, string user, out string error)
	{
		error = "";
		OpJob job = ById(jobId);
		if (job == null)
		{
			error = "заявка не найдена";
			return false;
		}
		OpDef def = Catalog.Find(job.Op);
		if (def == null)
		{
			error = "операция из каталога исчезла";
			return false;
		}
		lock (job.Sync)
		{
			if (job.Status != "pending")
			{
				error = "заявка уже обработана (статус " + job.Status + ")";
				return false;
			}
			job.ApprovedBy = user ?? "";
			job.Status = "queued";
		}
		Enqueue(job, def);
		return true;
	}

	public bool Reject(string jobId, string user, out string error)
	{
		error = "";
		OpJob job = ById(jobId);
		if (job == null)
		{
			error = "заявка не найдена";
			return false;
		}
		lock (job.Sync)
		{
			if (job.Status != "pending")
			{
				error = "заявка уже обработана (статус " + job.Status + ")";
				return false;
			}
			job.Status = "rejected";
			job.ApprovedBy = user ?? "";
			job.FinishedAt = DateTime.Now;
			job.Detail = "отклонена";
		}
		WriteJournal(job);
		return true;
	}

	private void Enqueue(OpJob job, OpDef def)
	{
		job.Status = "queued";
		lock (_sync)
		{
			_queue.Enqueue(job);
		}
	}

	public List<OpJob> Jobs(int limit)
	{
		lock (_sync)
		{
			List<OpJob> result = new List<OpJob>();
			foreach (OpJob j in _jobs)
			{
				result.Add(j);
				if (result.Count >= limit)
				{
					break;
				}
			}
			return result;
		}
	}

	public OpJob ById(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		lock (_sync)
		{
			foreach (OpJob j in _jobs)
			{
				if (string.Equals(j.Id, id, StringComparison.OrdinalIgnoreCase))
				{
					return j;
				}
			}
		}
		return null;
	}

	public List<Dictionary<string, object>> History(string month, int limit)
	{
		List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
		if (string.IsNullOrEmpty(month))
		{
			month = DateTime.Now.ToString("yyyy-MM");
		}
		string path = Path.Combine(_opsDir, month + ".ndjson");
		if (!File.Exists(path))
		{
			return result;
		}
		try
		{
			string[] lines = File.ReadAllLines(path, Encoding.UTF8);
			int i = lines.Length - 1;
			while (i >= 0 && result.Count < limit)
			{
				string line = lines[i].Trim();
				if (line.Length != 0)
				{
					try
					{
						Dictionary<string, object> d = Json.ParseObject(line);
						if (d.Count > 0)
						{
							result.Add(d);
						}
					}
					catch
					{
					}
				}
				i--;
			}
		}
		catch (Exception ex)
		{
			Log.Warn("ops ndjson: " + ex.Message);
		}
		return result;
	}

	private void Loop()
	{
		while (!_stopping)
		{
			OpJob job = null;
			lock (_sync)
			{
				if (_queue.Count > 0)
				{
					job = _queue.Dequeue();
				}
			}
			if (job == null)
			{
				Thread.Sleep(300);
				continue;
			}
			try
			{
				Execute(job);
			}
			catch (Exception ex)
			{
				lock (job.Sync)
				{
					job.Status = "fail";
					job.Error = "непредвиденная ошибка: " + ex.Message;
					job.FinishedAt = DateTime.Now;
				}
				WriteJournal(job);
				Log.Error("операция " + job.Op, ex);
			}
		}
	}

	private void Execute(OpJob job)
	{
		OpDef def = Catalog.Find(job.Op);
		lock (job.Sync)
		{
			job.Status = "running";
			job.Detail = "выполняется";
		}
		DateTime started = DateTime.Now;
		OpResult res;
		try
		{
			res = Dispatch(def, job);
		}
		catch (Exception ex)
		{
			res = OpResult.Fail(ex.Message);
		}
		lock (job.Sync)
		{
			job.DurationMs = (long)(DateTime.Now - started).TotalMilliseconds;
			job.FinishedAt = DateTime.Now;
			if (res != null && res.Ok)
			{
				job.Status = "ok";
				job.Output = res.Output ?? "";
				job.Detail = "выполнено";
			}
			else
			{
				job.Status = "fail";
				job.Output = ((res == null) ? "" : (res.Output ?? ""));
				job.Error = ((res == null) ? "нет результата" : (res.Error ?? ""));
				job.Detail = "ошибка";
			}
		}
		WriteJournal(job);
		Log.Info("операция " + job.Op + " на " + job.Target + ": " + job.Status + ((job.Error.Length > 0) ? (" — " + job.Error) : ""));
	}

	private OpResult Dispatch(OpDef def, OpJob job)
	{
		if (def == null)
		{
			return OpResult.Fail("неизвестная операция");
		}
		if (string.Equals(def.Transport, "hyperv", StringComparison.OrdinalIgnoreCase))
		{
			if (job.Op == "vm.list")
			{
				return Runners.VmList();
			}
			string vm = Catalog.Get(job.Params, "vm");
			if (job.Op == "vm.start")
			{
				return Runners.VmPower(vm, start: true);
			}
			if (job.Op == "vm.stop")
			{
				return Runners.VmPower(vm, start: false);
			}
			if (job.Op == "vm.snapshot")
			{
				return Runners.VmSnapshot(vm);
			}
			return OpResult.Fail("операция " + job.Op + " не реализована");
		}
		string script = BuildScript(job, out var timeout);
		if (script.Length == 0)
		{
			return OpResult.Fail("операция " + job.Op + " не реализована");
		}
		if (Runners.IsLocal(job.Target))
		{
			return Runners.RunLocal(script, timeout);
		}
		if (!LoadCredential(out var user, out var password))
		{
			return OpResult.Fail("не заданы учётные данные для WinRM (страница «Развёртывание» → «Доступ к машинам»)");
		}
		return Runners.RunWinRm(job.Target, 0, https: false, user, password, script, timeout);
	}

	private static string BuildScript(OpJob job, out int timeout)
	{
		timeout = 120;
		switch (job.Op)
		{
		case "host.info":
			return Runners.ScriptHostInfo();
		case "service.list":
			timeout = 180;
			return Runners.ScriptServiceList();
		case "service.restart":
			timeout = 180;
			return Runners.ScriptServiceAction("restart", Catalog.Get(job.Params, "service"));
		case "service.start":
			timeout = 180;
			return Runners.ScriptServiceAction("start", Catalog.Get(job.Params, "service"));
		case "service.stop":
			timeout = 180;
			return Runners.ScriptServiceAction("stop", Catalog.Get(job.Params, "service"));
		case "host.reboot":
			timeout = 60;
			return Runners.ScriptReboot(Catalog.Get(job.Params, "reason"), 60);
		default:
			return "";
		}
	}

	private bool LoadCredential(out string user, out string password)
	{
		user = "";
		password = "";
		string raw = _config.Secret("deploy.cred");
		if (raw.Length == 0)
		{
			return false;
		}
		try
		{
			Dictionary<string, object> d = Json.ParseObject(raw);
			user = Json.GetStr(d, "user", "");
			password = Json.GetStr(d, "password", "");
		}
		catch
		{
			return false;
		}
		return user.Length > 0;
	}

	private void WriteJournal(OpJob job)
	{
		try
		{
			Dictionary<string, object> d = job.ToJson();
			d["ts"] = DateTime.UtcNow;
			string path = Path.Combine(_opsDir, DateTime.Now.ToString("yyyy-MM") + ".ndjson");
			string line = Json.Write(d) + "\n";
			lock (_journalSync)
			{
				File.AppendAllText(path, line, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			}
		}
		catch (Exception ex)
		{
			Log.Warn("не удалось записать журнал операций: " + ex.Message);
		}
	}
}
