using System;
using System.Collections.Generic;
using System.Globalization;

namespace BackupDotNet.Core.Backup;

/// <summary>
/// Расписание резервного копирования.
///
/// Сознательно не cron: администратору нужно «каждую ночь в 2:00», а не
/// «0 2 * * *». Формат читается человеком и правится без шпаргалки.
/// </summary>
public class BackupSchedule
{
	/// <summary>Короткий идентификатор расписания.</summary>
	public string Id = "";

	/// <summary>Название для интерфейса: «Ночной бэкап ВМ».</summary>
	public string Title = "";

	public bool Enabled = true;

	/// <summary>Движок: <c>hyperv-export</c>.</summary>
	public string Engine = "hyperv-export";

	/// <summary>Что копируем. Для Hyper-V — имя ВМ; можно список через запятую.</summary>
	public List<string> Targets = new List<string>();

	/// <summary>Каталог назначения. Пусто — из <c>backup.destination</c>.</summary>
	public string Destination = "";

	/// <summary>Дни недели: 1 = понедельник … 7 = воскресенье. Пусто — каждый день.</summary>
	public List<int> DaysOfWeek = new List<int>();

	/// <summary>Время запуска: <c>02:00</c>.</summary>
	public string TimeOfDay = "02:00";

	/// <summary>Окно, в котором разрешён запуск: <c>22:00</c>–<c>06:00</c>. Пусто — без ограничения.</summary>
	public string WindowFrom = "";

	public string WindowTo = "";

	/// <summary>Сколько раз повторять при ошибке.</summary>
	public int Retries;

	/// <summary>Пауза между повторами, минут.</summary>
	public int RetryDelayMinutes = 15;

	/// <summary>Последний запуск (ставится планировщиком).</summary>
	public DateTime? LastRunAt;

	/// <summary>Результат последнего запуска: ok / failed.</summary>
	public string LastResult = "";

	/// <summary>Текст последней ошибки.</summary>
	public string LastError = "";

	/// <summary>Когда планируется следующий запуск (вычисляется).</summary>
	public DateTime? NextRunAt;

	/// <summary>
	/// Вычисляет ближайший момент запуска после <paramref name="after"/>.
	/// Возвращает <c>null</c>, если расписание выключено.
	/// </summary>
	public DateTime? NextRunAfter(DateTime after)
	{
		if (!Enabled)
		{
			return null;
		}
		TimeSpan time;
		if (!TryParseTime(TimeOfDay, out time))
		{
			time = TimeSpan.FromHours(2.0);
		}

		// Ищем в пределах двух недель: этого хватит на любой набор дней.
		DateTime day = after.Date;
		for (int i = 0; i <= 14; i++)
		{
			DateTime candidateDay = day.AddDays(i);
			if (DaysOfWeek.Count > 0 && !DaysOfWeek.Contains(DayNumber(candidateDay.DayOfWeek)))
			{
				continue;
			}
			DateTime candidate = candidateDay.Add(time);
			if (candidate > after)
			{
				return candidate;
			}
		}
		return null;
	}

	/// <summary>
	/// Разрешён ли запуск прямо сейчас с точки зрения окна.
	/// Окно может пересекать полночь (<c>22:00</c>–<c>06:00</c>).
	/// </summary>
	public bool IsInWindow(DateTime now)
	{
		if (string.IsNullOrWhiteSpace(WindowFrom) || string.IsNullOrWhiteSpace(WindowTo))
		{
			return true;
		}
		TimeSpan from;
		TimeSpan to;
		if (!TryParseTime(WindowFrom, out from) || !TryParseTime(WindowTo, out to))
		{
			return true;
		}
		if (from == to)
		{
			return true;
		}
		TimeSpan t = now.TimeOfDay;
		if (from < to)
		{
			return t >= from && t < to;
		}
		return t >= from || t < to;
	}

	/// <summary>Человеческое описание расписания — для интерфейса.</summary>
	public string Describe()
	{
		string days;
		if (DaysOfWeek.Count == 0)
		{
			days = "каждый день";
		}
		else if (DaysOfWeek.Count == 7)
		{
			days = "каждый день";
		}
		else
		{
			List<string> names = new List<string>();
			foreach (int d in DaysOfWeek)
			{
				names.Add(DayName(d));
			}
			days = string.Join(", ", names.ToArray());
		}
		string s = days + " в " + TimeOfDay;
		if (!string.IsNullOrWhiteSpace(WindowFrom) && !string.IsNullOrWhiteSpace(WindowTo))
		{
			s += " (окно " + WindowFrom + "–" + WindowTo + ")";
		}
		return s;
	}

	public Dictionary<string, object> ToJson()
	{
		Dictionary<string, object> d = Json.Obj();
		d["id"] = Id;
		d["title"] = Title;
		d["enabled"] = Enabled;
		d["engine"] = Engine;
		List<object> targets = new List<object>();
		foreach (string t in Targets)
		{
			targets.Add(t);
		}
		d["targets"] = targets;
		d["destination"] = Destination;
		List<object> days = new List<object>();
		foreach (int x in DaysOfWeek)
		{
			days.Add(x);
		}
		d["daysOfWeek"] = days;
		d["timeOfDay"] = TimeOfDay;
		d["windowFrom"] = WindowFrom;
		d["windowTo"] = WindowTo;
		d["retries"] = Retries;
		d["retryDelayMinutes"] = RetryDelayMinutes;
		d["lastRunAt"] = LastRunAt;
		d["lastResult"] = LastResult;
		d["lastError"] = LastError;
		d["nextRunAt"] = NextRunAt;
		d["describe"] = Describe();
		return d;
	}

	public static BackupSchedule FromJson(Dictionary<string, object> d)
	{
		BackupSchedule s = new BackupSchedule();
		s.Id = Json.GetStr(d, "id", "");
		s.Title = Json.GetStr(d, "title", s.Id);
		s.Enabled = Json.Bool(Json.Get(d, "enabled"), dflt: true);
		s.Engine = Json.GetStr(d, "engine", "hyperv-export");
		foreach (object o in Json.AsArray(Json.Get(d, "targets")))
		{
			string t = Json.Str(o).Trim();
			if (t.Length > 0)
			{
				s.Targets.Add(t);
			}
		}
		s.Destination = Json.GetStr(d, "destination", "");
		foreach (object o in Json.AsArray(Json.Get(d, "daysOfWeek")))
		{
			int v = Json.Int(o, 0);
			if (v >= 1 && v <= 7 && !s.DaysOfWeek.Contains(v))
			{
				s.DaysOfWeek.Add(v);
			}
		}
		s.DaysOfWeek.Sort();
		s.TimeOfDay = Json.GetStr(d, "timeOfDay", s.TimeOfDay);
		s.WindowFrom = Json.GetStr(d, "windowFrom", "");
		s.WindowTo = Json.GetStr(d, "windowTo", "");
		s.Retries = Json.Int(Json.Get(d, "retries"), 0);
		s.RetryDelayMinutes = Json.Int(Json.Get(d, "retryDelayMinutes"), 15);
		object last = Json.Get(d, "lastRunAt");
		if (last != null)
		{
			s.LastRunAt = BackupRun.ParseDate(last);
		}
		s.LastResult = Json.GetStr(d, "lastResult", "");
		s.LastError = Json.GetStr(d, "lastError", "");
		return s;
	}

	// ------------------------------------------------------------- утилиты

	private static int DayNumber(DayOfWeek d)
	{
		// .NET: воскресенье = 0. Нам нужно 1 = понедельник … 7 = воскресенье.
		return d == DayOfWeek.Sunday ? 7 : (int)d;
	}

	public static string DayName(int n)
	{
		switch (n)
		{
		case 1:
			return "Пн";
		case 2:
			return "Вт";
		case 3:
			return "Ср";
		case 4:
			return "Чт";
		case 5:
			return "Пт";
		case 6:
			return "Сб";
		case 7:
			return "Вс";
		default:
			return "?";
		}
	}

	public static bool TryParseTime(string s, out TimeSpan t)
	{
		t = TimeSpan.Zero;
		if (string.IsNullOrWhiteSpace(s))
		{
			return false;
		}
		return TimeSpan.TryParseExact(s.Trim(), new string[2] { @"h\:mm", @"hh\:mm" },
			CultureInfo.InvariantCulture, out t);
	}

	/// <summary>Проверяет корректность и возвращает список проблем.</summary>
	public List<string> Validate()
	{
		List<string> problems = new List<string>();
		if (string.IsNullOrWhiteSpace(Id))
		{
			problems.Add("не задан id расписания");
		}
		if (string.IsNullOrWhiteSpace(Engine))
		{
			problems.Add("не выбран движок");
		}
		else if (BackupEngines.Get(Engine) == null)
		{
			problems.Add("неизвестный движок: " + Engine);
		}
		if (Targets.Count == 0)
		{
			problems.Add("не выбран ни один объект копирования");
		}
		TimeSpan t;
		if (!TryParseTime(TimeOfDay, out t))
		{
			problems.Add("время запуска должно быть в формате ЧЧ:ММ");
		}
		if (!string.IsNullOrWhiteSpace(WindowFrom) && !TryParseTime(WindowFrom, out t))
		{
			problems.Add("начало окна должно быть в формате ЧЧ:ММ");
		}
		if (!string.IsNullOrWhiteSpace(WindowTo) && !TryParseTime(WindowTo, out t))
		{
			problems.Add("конец окна должен быть в формате ЧЧ:ММ");
		}
		if (Retries < 0 || Retries > 10)
		{
			problems.Add("число повторов — от 0 до 10");
		}
		if (RetryDelayMinutes < 1 || RetryDelayMinutes > 1440)
		{
			problems.Add("пауза между повторами — от 1 до 1440 минут");
		}
		return problems;
	}
}
