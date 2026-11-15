using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace BackupDotNet.Core.Backup;

/// <summary>
/// Очередь заданий резервного копирования.
///
/// Один рабочий поток: параллельные экспорты ВМ конкурируют за диск и сеть,
/// и от этого хуже всем. Очередь переживает перезапуск — незавершённые задания
/// помечаются как прерванные, а не теряются молча.
/// </summary>
public class BackupJobQueue
{
	private const int KeepInMemory = 100;

	private readonly string _dir;

	private readonly string _jobsPath;

	private readonly object _sync = new object();

	private readonly List<BackupJob> _jobs = new List<BackupJob>();

	private readonly Queue<BackupJob> _queue = new Queue<BackupJob>();

	private readonly Dictionary<string, CancellationTokenSource> _cancel =
		new Dictionary<string, CancellationTokenSource>(StringComparer.OrdinalIgnoreCase);

	private Thread _worker;

	private volatile bool _stopping;

	public BackupJobQueue(Settings settings)
	{
		_dir = Path.Combine(settings.DataDir, "backups", "jobs");
		_jobsPath = Path.Combine(_dir, "index.json");
		try
		{
			Directory.CreateDirectory(_dir);
		}
		catch
		{
		}
		Load();
	}

	public void Start()
	{
		_stopping = false;
		_worker = new Thread(Loop);
		_worker.IsBackground = true;
		_worker.Name = "backup-jobs";
		_worker.Start();
	}

	public void Stop()
	{
		_stopping = true;
		try
		{
			_worker?.Join(3000);
		}
		catch
		{
		}
	}

	/// <summary>Ставит задание в очередь. Возвращает созданное задание.</summary>
	public BackupJob Enqueue(string engine, string target, string destination, Dictionary<string, string> options, string user, out string error)
	{
		error = "";
		IBackupEngine e = BackupEngines.Get(engine);
		if (e == null)
		{
			error = "неизвестный движок бэкапа: " + engine;
			return null;
		}
		if (!e.IsAvailable(out string reason))
		{
			error = e.Title + " недоступен: " + reason;
			return null;
		}
		if (string.IsNullOrWhiteSpace(target))
		{
			error = "не указан объект копирования";
			return null;
		}
		if (string.IsNullOrWhiteSpace(destination))
		{
			error = "не указан каталог назначения";
			return null;
		}

		BackupJob job = new BackupJob();
		job.Id = NewId();
		job.Engine = engine;
		job.Target = target.Trim();
		job.Destination = destination.Trim();
		job.RequestedBy = user ?? "";
		job.Status = "pending";
		job.Message = "в очереди";
		job.CreatedAt = DateTime.Now;
		if (options != null)
		{
			foreach (KeyValuePair<string, string> kv in options)
			{
				job.Options[kv.Key] = kv.Value;
			}
		}

		lock (_sync)
		{
			_jobs.Add(job);
			_queue.Enqueue(job);
			Trim();
		}
		Save();
		Log.Info("Бэкап: задание " + job.Id + " — " + e.Title + " «" + job.Target + "» → " + job.Destination);
		return job;
	}

	/// <summary>Просит отменить задание. Движок остановится на ближайшей проверке.</summary>
	public bool Cancel(string id, out string error)
	{
		error = "";
		lock (_sync)
		{
			BackupJob job = Find(id);
			if (job == null)
			{
				error = "задание не найдено";
				return false;
			}
			if (job.IsFinished)
			{
				error = "задание уже завершено";
				return false;
			}
			if (_cancel.TryGetValue(id, out var cts))
			{
				cts.Cancel();
				job.Message = "отмена…";
				return true;
			}
			// Ещё в очереди — снимаем сразу.
			job.Status = "cancelled";
			job.Message = "отменено до запуска";
			job.FinishedAt = DateTime.Now;
			return true;
		}
	}

	public List<BackupJob> List()
	{
		lock (_sync)
		{
			List<BackupJob> copy = new List<BackupJob>(_jobs);
			copy.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
			return copy;
		}
	}

	public BackupJob Get(string id)
	{
		lock (_sync)
		{
			return Find(id);
		}
	}

	private BackupJob Find(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		foreach (BackupJob j in _jobs)
		{
			if (string.Equals(j.Id, id, StringComparison.OrdinalIgnoreCase))
			{
				return j;
			}
		}
		return null;
	}

	// ------------------------------------------------------------------ рабочий поток

	private void Loop()
	{
		while (!_stopping)
		{
			BackupJob job = null;
			lock (_sync)
			{
				while (_queue.Count > 0)
				{
					BackupJob next = _queue.Dequeue();
					if (next.Status == "pending")
					{
						job = next;
						break;
					}
				}
			}
			if (job == null)
			{
				Thread.Sleep(500);
				continue;
			}
			RunOne(job);
		}
	}

	private void RunOne(BackupJob job)
	{
		IBackupEngine engine = BackupEngines.Get(job.Engine);
		CancellationTokenSource cts = new CancellationTokenSource();
		lock (_sync)
		{
			_cancel[job.Id] = cts;
			job.Status = "running";
			job.StartedAt = DateTime.Now;
			job.Message = "запуск";
		}
		Save();

		DateTime started = DateTime.Now;
		try
		{
			if (engine == null)
			{
				throw new BackupException("движок «" + job.Engine + "» не найден");
			}
			engine.Run(job, (percent, message) =>
			{
				lock (_sync)
				{
					if (percent >= 0)
					{
						job.Progress = Math.Min(100, percent);
					}
					if (!string.IsNullOrEmpty(message))
					{
						job.Message = message;
					}
				}
			}, cts.Token);

			lock (_sync)
			{
				job.Status = "done";
				job.Progress = 100;
				job.FinishedAt = DateTime.Now;
				job.DurationMs = (long)(job.FinishedAt.Value - started).TotalMilliseconds;
				if (string.IsNullOrEmpty(job.Message) || job.Message.StartsWith("готово", StringComparison.OrdinalIgnoreCase) == false)
				{
					job.Message = "готово: " + Fmt.Size(job.SizeBytes);
				}
			}
			Log.Info("Бэкап: задание " + job.Id + " завершено за " +
				Math.Round((DateTime.Now - started).TotalSeconds) + " с, размер " + Fmt.Size(job.SizeBytes));
		}
		catch (BackupException ex)
		{
			lock (_sync)
			{
				job.Status = cts.IsCancellationRequested ? "cancelled" : "failed";
				job.Error = ex.Message;
				job.Message = cts.IsCancellationRequested ? "отменено" : ex.Message;
				job.FinishedAt = DateTime.Now;
				job.DurationMs = (long)(job.FinishedAt.Value - started).TotalMilliseconds;
			}
			Log.Warn("Бэкап: задание " + job.Id + " — " + ex.Message);
		}
		catch (Exception ex)
		{
			lock (_sync)
			{
				job.Status = "failed";
				job.Error = ex.Message;
				job.Message = ex.Message;
				job.FinishedAt = DateTime.Now;
				job.DurationMs = (long)(job.FinishedAt.Value - started).TotalMilliseconds;
			}
			Log.Error("Бэкап: задание " + job.Id + " упало", ex);
		}
		finally
		{
			lock (_sync)
			{
				_cancel.Remove(job.Id);
			}
			try
			{
				cts.Dispose();
			}
			catch
			{
			}
			Save();
		}
	}

	// ------------------------------------------------------------------ хранение

	private void Trim()
	{
		if (_jobs.Count <= KeepInMemory)
		{
			return;
		}
		// Оставляем завершённые, начиная с самых новых.
		List<BackupJob> finished = _jobs.FindAll(j => j.IsFinished);
		finished.Sort((a, b) => a.CreatedAt.CompareTo(b.CreatedAt));
		int toRemove = _jobs.Count - KeepInMemory;
		for (int i = 0; i < finished.Count && toRemove > 0; i++)
		{
			_jobs.Remove(finished[i]);
			toRemove--;
		}
	}

	private void Load()
	{
		try
		{
			if (!File.Exists(_jobsPath))
			{
				return;
			}
			Dictionary<string, object> root = Json.ParseObject(File.ReadAllText(_jobsPath, Encoding.UTF8));
			foreach (object o in Json.AsArray(Json.Get(root, "jobs")))
			{
				BackupJob j = BackupJob.FromJson(Json.AsObject(o));
				if (string.IsNullOrEmpty(j.Id))
				{
					continue;
				}
				// Незавершённые после перезапуска не возобновляем: экспорт ВМ
				// нельзя продолжить с середины. Честно помечаем как прерванные.
				if (!j.IsFinished)
				{
					j.Status = "failed";
					j.Error = "прервано перезапуском агента";
					j.Message = j.Error;
					j.FinishedAt = DateTime.Now;
				}
				_jobs.Add(j);
			}
			Log.Debug("Бэкап: загружено заданий " + _jobs.Count);
		}
		catch (Exception ex)
		{
			Log.Warn("Бэкап: не удалось прочитать историю заданий — " + ex.Message);
		}
	}

	private void Save()
	{
		try
		{
			Dictionary<string, object> root = Json.Obj();
			List<object> list = new List<object>();
			lock (_sync)
			{
				List<BackupJob> copy = new List<BackupJob>(_jobs);
				copy.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
				foreach (BackupJob j in copy)
				{
					list.Add(j.ToJson());
				}
			}
			root["jobs"] = list;
			ConfigLoader.AtomicWrite(_jobsPath, Json.WritePretty(root));
		}
		catch (Exception ex)
		{
			Log.Warn("Бэкап: не удалось сохранить историю заданий — " + ex.Message);
		}
	}

	private static string NewId()
	{
		return DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "-" +
			Guid.NewGuid().ToString("N").Substring(0, 6);
	}
}
