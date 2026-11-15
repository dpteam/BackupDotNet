using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace BackupDotNet.Core.Backup;

/// <summary>
/// Одна копия бэкапа на диске. Каталог вида <c>&lt;имя&gt;_2026-11-16_00-07-19</c>.
/// </summary>
public class BackupCopy
{
	public string Path = "";

	/// <summary>Имя объекта без даты: имя ВМ, имя базы.</summary>
	public string Name = "";

	public DateTime Timestamp = DateTime.MinValue;

	public long SizeBytes;

	/// <summary>Ключ дня: yyyy-MM-dd.</summary>
	public string DayKey => Timestamp.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

	public string WeekKey => IsoWeekKey(Timestamp);

	public string MonthKey => Timestamp.ToString("yyyy-MM", CultureInfo.InvariantCulture);

	public static string IsoWeekKey(DateTime dt)
	{
		// ISO 8601: неделя начинается с понедельника, первая неделя содержит 4 января.
		IsoWeek week = IsoWeekOf(dt);
		return week.Year.ToString("0000", CultureInfo.InvariantCulture) + "-W" +
			week.Week.ToString("00", CultureInfo.InvariantCulture);
	}

	private struct IsoWeek
	{
		public int Year;
		public int Week;
	}

	private static IsoWeek IsoWeekOf(DateTime dt)
	{
		// Алгоритм без CultureInfo: он не зависит от региональных настроек.
		DateTime day = dt.Date;
		int dayOfWeek = ((int)day.DayOfWeek + 6) % 7; // понедельник = 0
		DateTime thursday = day.AddDays(3 - dayOfWeek);
		int year = thursday.Year;
		DateTime jan1 = new DateTime(year, 1, 1);
		int week = 1 + (int)((thursday - jan1).TotalDays / 7.0);
		return new IsoWeek { Year = year, Week = week };
	}
}

/// <summary>
/// Политика хранения бэкапов.
///
/// Зачем так сложно: простое «хранить N последних» плохо переживает отпуск.
/// Через месяц остаются только свежие копии, и откатиться на «состояние до
/// обновления в прошлом квартале» некуда. GFS (grandfather-father-son) держит
/// цепочку: последние дни подробно, недели и месяцы — редко, но долго.
/// </summary>
public class RetentionPolicy
{
	/// <summary>Сколько последних копий хранить всегда. 0 — не ограничивать.</summary>
	public int KeepCount;

	/// <summary>Копии не старше N дней хранить всегда. 0 — не ограничивать.</summary>
	public int KeepDays;

	/// <summary>Хранить по одной копии за каждый из последних N дней.</summary>
	public int GfsDaily;

	/// <summary>Хранить по одной копии за каждую из последних N недель.</summary>
	public int GfsWeekly;

	/// <summary>Хранить по одной копии за каждый из последних N месяцев.</summary>
	public int GfsMonthly;

	/// <summary>Никогда не удалять самую свежую копию. Выключить нельзя осознанно — см. Apply.</summary>
	public bool ProtectLast = true;

	/// <summary>Сколько копий разрешено удалить за один проход.</summary>
	public int MaxDeletePerRun = 10;

	/// <summary>Политика вообще что-то ограничивает?</summary>
	public bool IsActive => KeepCount > 0 || KeepDays > 0 || GfsDaily > 0 || GfsWeekly > 0 || GfsMonthly > 0;

	public static RetentionPolicy FromJson(Dictionary<string, object> d)
	{
		RetentionPolicy p = new RetentionPolicy();
		if (d == null || d.Count == 0)
		{
			return p;
		}
		p.KeepCount = Json.Int(Json.Get(d, "keepCount"), 0);
		p.KeepDays = Json.Int(Json.Get(d, "keepDays"), 0);
		Dictionary<string, object> gfs = Json.AsObject(Json.Get(d, "gfs"));
		p.GfsDaily = Json.Int(Json.Get(gfs, "daily"), 0);
		p.GfsWeekly = Json.Int(Json.Get(gfs, "weekly"), 0);
		p.GfsMonthly = Json.Int(Json.Get(gfs, "monthly"), 0);
		p.ProtectLast = Json.Bool(Json.Get(d, "protectLast"), dflt: true);
		p.MaxDeletePerRun = Json.Int(Json.Get(d, "maxDeletePerRun"), 10);
		return p;
	}

	public Dictionary<string, object> ToJson()
	{
		Dictionary<string, object> d = Json.Obj();
		d["keepCount"] = KeepCount;
		d["keepDays"] = KeepDays;
		Dictionary<string, object> gfs = Json.Obj();
		gfs["daily"] = GfsDaily;
		gfs["weekly"] = GfsWeekly;
		gfs["monthly"] = GfsMonthly;
		d["gfs"] = gfs;
		d["protectLast"] = ProtectLast;
		d["maxDeletePerRun"] = MaxDeletePerRun;
		d["active"] = IsActive;
		return d;
	}
}

/// <summary>Применение политики хранения к каталогу бэкапов.</summary>
public static class BackupRetention
{
	/// <summary>
	/// Читает копии из каталога. Понимает имена вида <c>&lt;имя&gt;_yyyy-MM-dd_HH-mm-ss</c>;
	/// всё остальное пропускает — в каталоге бэкапов может лежать что угодно.
	/// </summary>
	public static List<BackupCopy> Scan(string root)
	{
		List<BackupCopy> list = new List<BackupCopy>();
		if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
		{
			return list;
		}
		try
		{
			foreach (string dir in Directory.GetDirectories(root))
			{
				string name = Path.GetFileName(dir);
				if (name.StartsWith("_", StringComparison.Ordinal))
				{
					continue; // служебные каталоги
				}
				DateTime ts;
				string objectName;
				if (!TryParseCopyName(name, out objectName, out ts))
				{
					continue;
				}
				BackupCopy c = new BackupCopy();
				c.Path = dir;
				c.Name = objectName;
				c.Timestamp = ts;
				c.SizeBytes = DirSize(dir);
				list.Add(c);
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Хранение бэкапов: не удалось прочитать «" + root + "» — " + ex.Message);
		}
		list.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp)); // свежие сверху
		return list;
	}

	/// <summary>
	/// Применяет политику. <paramref name="dryRun"/> — только показать, что было бы удалено.
	/// Возвращает отчёт: удалённые, оставленные с причиной, освобождённое место.
	/// </summary>
	public static Dictionary<string, object> Apply(string root, RetentionPolicy policy, bool dryRun)
	{
		Dictionary<string, object> report = Json.Obj();
		List<object> deleted = new List<object>();
		List<object> kept = new List<object>();
		long freed = 0L;
		int skippedByLimit = 0;

		if (policy == null || !policy.IsActive)
		{
			report["active"] = false;
			report["deleted"] = deleted;
			report["kept"] = kept;
			report["freedBytes"] = 0L;
			report["note"] = "политика хранения не настроена — ничего не удаляется";
			return report;
		}

		List<BackupCopy> copies = Scan(root);
		report["active"] = true;
		report["dryRun"] = dryRun;
		report["total"] = copies.Count;

		if (copies.Count == 0)
		{
			report["deleted"] = deleted;
			report["kept"] = kept;
			report["freedBytes"] = 0L;
			return report;
		}

		// Каждая копия получает причину, по которой её оставляют. Порядок проверок
		// важен: сначала «всегда хранить», потом «удалить».
		Dictionary<string, string> keepReason = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		// 1. Самая свежая копия защищена всегда.
		if (policy.ProtectLast)
		{
			keepReason[copies[0].Path] = "последняя копия";
		}

		// 2. Последние N копий.
		if (policy.KeepCount > 0)
		{
			int n = Math.Min(policy.KeepCount, copies.Count);
			for (int i = 0; i < n; i++)
			{
				if (!keepReason.ContainsKey(copies[i].Path))
				{
					keepReason[copies[i].Path] = "из последних " + policy.KeepCount;
				}
			}
		}

		// 3. Всё, что не старше N дней.
		if (policy.KeepDays > 0)
		{
			DateTime cutoff = DateTime.Now.AddDays(-policy.KeepDays);
			foreach (BackupCopy c in copies)
			{
				if (c.Timestamp >= cutoff && !keepReason.ContainsKey(c.Path))
				{
					keepReason[c.Path] = "моложе " + policy.KeepDays + " дн";
				}
			}
		}

		// 4. GFS: по одной копии за период. Список уже отсортирован по убыванию
		//    времени, поэтому первая встреченная копия периода — самая свежая в нём.
		ApplyGfs(copies, policy.GfsDaily, c => c.DayKey, "день", keepReason);
		ApplyGfs(copies, policy.GfsWeekly, c => c.WeekKey, "неделя", keepReason);
		ApplyGfs(copies, policy.GfsMonthly, c => c.MonthKey, "месяц", keepReason);

		// 5. Что осталось — кандидаты на удаление.
		int deletionsLeft = Math.Max(1, policy.MaxDeletePerRun);
		foreach (BackupCopy c in copies)
		{
			if (keepReason.TryGetValue(c.Path, out var reason))
			{
				Dictionary<string, object> k = Json.Obj();
				k["path"] = c.Path;
				k["name"] = Path.GetFileName(c.Path);
				k["timestamp"] = c.Timestamp;
				k["sizeBytes"] = c.SizeBytes;
				k["reason"] = reason;
				kept.Add(k);
				continue;
			}

			Dictionary<string, object> d = Json.Obj();
			d["path"] = c.Path;
			d["name"] = Path.GetFileName(c.Path);
			d["timestamp"] = c.Timestamp;
			d["sizeBytes"] = c.SizeBytes;

			if (deletionsLeft <= 0)
			{
				skippedByLimit++;
				d["action"] = "отложено";
				d["reason"] = "лимит удалений за проход (" + policy.MaxDeletePerRun + ")";
				kept.Add(d);
				continue;
			}

			if (dryRun)
			{
				d["action"] = "будет удалено";
				deleted.Add(d);
				freed += c.SizeBytes;
				deletionsLeft--;
				continue;
			}

			try
			{
				Directory.Delete(c.Path, recursive: true);
				d["action"] = "удалено";
				deleted.Add(d);
				freed += c.SizeBytes;
				deletionsLeft--;
				Log.Info("Хранение бэкапов: удалена копия " + Path.GetFileName(c.Path) +
					" (" + Fmt.Size(c.SizeBytes) + ")");
			}
			catch (Exception ex)
			{
				d["action"] = "ошибка";
				d["reason"] = ex.Message;
				kept.Add(d);
				Log.Warn("Хранение бэкапов: не удалось удалить " + c.Path + " — " + ex.Message);
			}
		}

		report["deleted"] = deleted;
		report["kept"] = kept;
		report["deletedCount"] = deleted.Count;
		report["keptCount"] = kept.Count;
		report["skippedByLimit"] = skippedByLimit;
		report["freedBytes"] = freed;
		if (!dryRun && deleted.Count > 0)
		{
			Log.Info("Хранение бэкапов: удалено " + deleted.Count + ", освобождено " + Fmt.Size(freed));
		}
		return report;
	}

	private static void ApplyGfs(List<BackupCopy> copies, int count, Func<BackupCopy, string> keyOf, string period, Dictionary<string, string> keepReason)
	{
		if (count <= 0)
		{
			return;
		}
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		int taken = 0;
		foreach (BackupCopy c in copies)
		{
			if (taken >= count)
			{
				break;
			}
			string key = keyOf(c);
			if (!seen.Add(key))
			{
				continue; // в этом периоде уже есть более свежая копия
			}
			taken++;
			if (!keepReason.ContainsKey(c.Path))
			{
				keepReason[c.Path] = "GFS: " + period + " " + key;
			}
		}
	}

	/// <summary>
	/// Разбирает имя каталога копии: <c>&lt;имя&gt;_yyyy-MM-dd_HH-mm-ss</c>,
	/// допускается суффикс <c>-N</c> (защита от совпадения имён в одну секунду).
	///
	/// Порядок важен: сначала пробуем разобрать как есть, и только потом
	/// отрезаем суффикс. Дефис есть и внутри даты (<c>00-29-00</c>), поэтому
	/// «отрезать по последнему дефису» — неверный путь.
	/// </summary>
	public static bool TryParseCopyName(string dirName, out string objectName, out DateTime timestamp)
	{
		objectName = "";
		timestamp = DateTime.MinValue;
		if (string.IsNullOrEmpty(dirName))
		{
			return false;
		}
		if (TryParseDirect(dirName, out objectName, out timestamp))
		{
			return true;
		}

		// Вторая попытка: отрезать «-N» в конце и разобрать остаток.
		int dash = dirName.LastIndexOf('-');
		if (dash <= 0 || dash + 1 >= dirName.Length)
		{
			return false;
		}
		string tail = dirName.Substring(dash + 1);
		foreach (char ch in tail)
		{
			if (ch < '0' || ch > '9')
			{
				return false;
			}
		}
		return TryParseDirect(dirName.Substring(0, dash), out objectName, out timestamp);
	}

	private static bool TryParseDirect(string dirName, out string objectName, out DateTime timestamp)
	{
		objectName = "";
		timestamp = DateTime.MinValue;
		const int stampLen = 19; // yyyy-MM-dd_HH-mm-ss
		if (dirName.Length <= stampLen + 1)
		{
			return false;
		}
		int sep = dirName.Length - stampLen - 1;
		if (dirName[sep] != '_')
		{
			return false;
		}
		string stamp = dirName.Substring(sep + 1);
		if (!DateTime.TryParseExact(stamp, "yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture,
			DateTimeStyles.None, out timestamp))
		{
			return false;
		}
		objectName = dirName.Substring(0, sep);
		return objectName.Length > 0;
	}

	private static long DirSize(string dir)
	{
		long total = 0L;
		try
		{
			foreach (string f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
			{
				try
				{
					total += new FileInfo(f).Length;
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		return total;
	}
}
