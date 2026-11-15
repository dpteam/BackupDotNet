using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace BackupDotNet.Core.Backup;

/// <summary>
/// Планировщик резервного копирования.
///
/// Работает как отдельный поток рядом с очередью заданий: сам ничего не копирует,
/// только решает «пора» и ставит задание в очередь. Разделение важно — очередь
/// умеет отмену и прогресс, а расписание не должно в это вмешиваться.
///
/// Защиты, без которых планировщик бэкапов опасен:
///  • <b>окно запуска</b> — не начинать копирование в рабочее время;
///  • <b>повтор с задержкой</b> — не молотить упавшую ВМ каждую минуту;
///  • <b>не больше одного задания на расписание</b> одновременно;
///  • <b>пропуск, если время ушло</b> — если агент был выключен сутки, не надо
///    наверстывать все пропущенные запуски подряд.
/// </summary>
public class BackupScheduler
{
	private readonly Settings _settings;

	private readonly BackupJobQueue _queue;

	private readonly string _path;

	private readonly object _sync = new object();

	private readonly List<BackupSchedule> _schedules = new List<BackupSchedule>();

	private Thread _thread;

	private volatile bool _stopping;

	/// <summary>Допустимое опоздание: если проспали больше — запуск пропускается.</summary>
	private const int LateToleranceMinutes = 180;

	/// <summary>Сколько минут между проверками расписания.</summary>
	private const int PollSeconds = 30;

	public BackupScheduler(Settings settings, BackupJobQueue queue)
	{
		_settings = settings;
		_queue = queue;
		_path = Path.Combine(settings.DataDir, "backups", "schedules.json");
		Load();
	}

	public void Start()
	{
		_stopping = false;
		_thread = new Thread(Loop);
		_thread.IsBackground = true;
		_thread.Name = "backup-schedule";
		_thread.Start();
		RecalcNextRuns();
	}

	public void Stop()
	{
		_stopping = true;
		try
		{
			_thread?.Join(3000);
		}
		catch
		{
		}
	}

	/// <summary>Копия списка расписаний для интерфейса.</summary>
	public List<BackupSchedule> List()
	{
		lock (_sync)
		{
			return new List<BackupSchedule>(_schedules);
		}
	}

	public BackupSchedule Get(string id)
	{
		lock (_sync)
		{
			return Find(id);
		}
	}

	/// <summary>Создаёт или заменяет расписание. Возвращает список проблем.</summary>
	public List<string> Save(BackupSchedule s)
	{
		List<string> problems = s.Validate();
		if (problems.Count > 0)
		{
			return problems;
		}
		lock (_sync)
		{
			int idx = _schedules.FindIndex(x => string.Equals(x.Id, s.Id, StringComparison.OrdinalIgnoreCase));
			if (idx >= 0)
			{
				// Сохраняем историю запусков: она относится к расписанию, а не к форме.
				s.LastRunAt = _schedules[idx].LastRunAt;
				s.LastResult = _schedules[idx].LastResult;
				s.LastError = _schedules[idx].LastError;
				_schedules[idx] = s;
			}
			else
			{
				_schedules.Add(s);
			}
			s.NextRunAt = s.NextRunAfter(DateTime.Now);
		}
		SaveFile();
		Log.Info("Расписание бэкапа сохранено: " + s.Id + " — " + s.Describe());
		return problems;
	}

	public bool Delete(string id, out string error)
	{
		error = "";
		lock (_sync)
		{
			int idx = _schedules.FindIndex(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
			if (idx < 0)
			{
				error = "расписание не найдено";
				return false;
			}
			_schedules.RemoveAt(idx);
		}
		SaveFile();
		Log.Info("Расписание бэкапа удалено: " + id);
		return true;
	}

	/// <summary>Запускает расписание немедленно, не дожидаясь времени.</summary>
	public bool RunNow(string id, string user, out string error)
	{
		error = "";
		BackupSchedule s = Get(id);
		if (s == null)
		{
			error = "расписание не найдено";
			return false;
		}
		return Enqueue(s, user, out error, forced: true);
	}

	// ------------------------------------------------------------------ поток

	private void Loop()
	{
		Log.Info("Планировщик бэкапов запущен, проверка каждые " + PollSeconds + " с");
		Thread.Sleep(5000); // даём агенту подняться
		while (!_stopping)
		{
			try
			{
				Tick();
			}
			catch (Exception ex)
			{
				Log.Error("Планировщик бэкапов: ошибка в цикле", ex);
			}
			for (int i = 0; i < PollSeconds * 4 && !_stopping; i++)
			{
				Thread.Sleep(250);
			}
		}
		Log.Info("Планировщик бэкапов остановлен");
	}

	private void Tick()
	{
		List<BackupSchedule> due = new List<BackupSchedule>();
		DateTime now = DateTime.Now;

		lock (_sync)
		{
			foreach (BackupSchedule s in _schedules)
			{
				if (!s.Enabled)
				{
					continue;
				}

				// Опорная точка — сохранённый NextRunAt, а не пересчёт от «сейчас».
				// Пересчёт каждый тик давал окно шириной в пару минут: если тик
				// попадал чуть позже, запуск молча пропускался.
				if (!s.NextRunAt.HasValue)
				{
					s.NextRunAt = s.NextRunAfter(now);
					continue;
				}

				DateTime planned = s.NextRunAt.Value;
				if (planned > now)
				{
					continue; // ещё не время
				}

				// Пропуск: если проспали больше допустимого (агент был выключен),
				// не навёрстываем — переносим на следующее окно по расписанию.
				double late = (now - planned).TotalMinutes;
				if (late > LateToleranceMinutes)
				{
					Log.Warn("Расписание " + s.Id + ": запуск пропущен (опоздание " +
						(int)late + " мин). Следующий — по расписанию.");
					s.NextRunAt = s.NextRunAfter(now);
					continue;
				}

				// Окно: не начинать вне разрешённого времени. Откладываем
				// не на «через 15 минут», а на ближайший момент внутри окна.
				if (!s.IsInWindow(now))
				{
					Log.Info("Расписание " + s.Id + ": время пришло, но вне окна " +
						s.WindowFrom + "–" + s.WindowTo + " — отложено");
					s.NextRunAt = s.NextRunAfter(now);
					continue;
				}

				due.Add(s);
			}
		}

		foreach (BackupSchedule s in due)
		{
			string error;
			if (Enqueue(s, "scheduler", out error, forced: false))
			{
				lock (_sync)
				{
					s.LastRunAt = now;
					s.LastResult = "ok";
					s.LastError = "";
					s.NextRunAt = s.NextRunAfter(now.AddMinutes(1));
				}
				SaveFile();
			}
			else
			{
				lock (_sync)
				{
					s.LastResult = "failed";
					s.LastError = error;
					// Повтор: сдвигаем на паузу, если она настроена.
					int delay = Math.Max(1, s.RetryDelayMinutes);
					s.NextRunAt = now.AddMinutes(delay);
				}
				SaveFile();
				Log.Warn("Расписание " + s.Id + ": не удалось поставить задание — " + error);
			}
		}
	}

	/// <summary>
	/// Ставит задания по расписанию. На каждый объект — своё задание:
	/// так одна упавшая ВМ не блокирует копирование остальных.
	/// </summary>
	private bool Enqueue(BackupSchedule s, string user, out string error, bool forced)
	{
		error = "";
		string dest = s.Destination;
		if (string.IsNullOrWhiteSpace(dest))
		{
			dest = string.IsNullOrWhiteSpace(_settings.BackupDestination)
				? Path.Combine(_settings.DataDir, "backups", "store")
				: _settings.BackupDestination;
		}
		int queued = 0;
		List<string> failures = new List<string>();
		foreach (string target in s.Targets)
		{
			string err;
			BackupJob job = _queue.Enqueue(s.Engine, target, dest, null, user, out err);
			if (job == null)
			{
				failures.Add(target + ": " + err);
			}
			else
			{
				queued++;
			}
		}
		if (queued == 0)
		{
			error = (failures.Count > 0) ? string.Join("; ", failures.ToArray()) : "не удалось поставить ни одного задания";
			return false;
		}
		if (failures.Count > 0)
		{
			Log.Warn("Расписание " + s.Id + ": часть объектов не поставлена — " +
				string.Join("; ", failures.ToArray()));
		}
		Log.Info("Расписание " + s.Id + " (" + (forced ? "вручную" : "по времени") +
			"): поставлено заданий " + queued);
		return true;
	}

	// ------------------------------------------------------------- хранение

	private void RecalcNextRuns()
	{
		DateTime now = DateTime.Now;
		lock (_sync)
		{
			foreach (BackupSchedule s in _schedules)
			{
				s.NextRunAt = s.NextRunAfter(now);
			}
		}
		SaveFile();
	}

	private void Load()
	{
		try
		{
			if (!File.Exists(_path))
			{
				return;
			}
			Dictionary<string, object> root = Json.ParseObject(File.ReadAllText(_path, Encoding.UTF8));
			foreach (object o in Json.AsArray(Json.Get(root, "schedules")))
			{
				BackupSchedule s = BackupSchedule.FromJson(Json.AsObject(o));
				if (!string.IsNullOrEmpty(s.Id))
				{
					_schedules.Add(s);
				}
			}
			Log.Debug("Расписания бэкапов: загружено " + _schedules.Count);
		}
		catch (Exception ex)
		{
			Log.Warn("Расписания бэкапов: не удалось прочитать — " + ex.Message);
		}
	}

	private void SaveFile()
	{
		try
		{
			Dictionary<string, object> root = Json.Obj();
			List<object> list = new List<object>();
			lock (_sync)
			{
				foreach (BackupSchedule s in _schedules)
				{
					list.Add(s.ToJson());
				}
			}
			root["schedules"] = list;
			string dir = Path.GetDirectoryName(_path);
			if (!string.IsNullOrEmpty(dir))
			{
				Directory.CreateDirectory(dir);
			}
			ConfigLoader.AtomicWrite(_path, Json.WritePretty(root));
		}
		catch (Exception ex)
		{
			Log.Warn("Расписания бэкапов: не удалось сохранить — " + ex.Message);
		}
	}

	private BackupSchedule Find(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		return _schedules.Find(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
	}
}
