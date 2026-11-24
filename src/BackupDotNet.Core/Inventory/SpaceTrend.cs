using System;
using System.Collections.Generic;
using System.IO;

namespace BackupDotNet.Core.Inventory;

/// <summary>
/// История свободного места на дисках хоста и прогноз заполнения.
///
/// Зачем: «осталось 12 ГБ» само по себе ничего не говорит. Важно, за сколько
/// дней они кончатся. На VHDX-дисках ВМ место уходит ступеньками — после
/// бэкапа, после снимка, — поэтому прогноз считается по прямой линии через
/// точки за последний месяц, а не по разнице «вчера-сегодня».
///
/// Хранится одна точка в день на диск: <c>&lt;dataDir&gt;\space-history.json</c>.
/// Больше не нужно — за месяц набирается 30 точек, этого хватает для линии.
/// </summary>
public static class SpaceTrend
{
	private const int KeepDays = 120;

	/// <summary>Меньше трёх точек — линию не построить, прогноза нет.</summary>
	private const int MinSamples = 3;

	/// <summary>Сколько последних точек участвует в расчёте.</summary>
	private const int WindowDays = 30;

	private static readonly object Sync = new object();

	/// <summary>Одна точка: сколько было свободно.</summary>
	private class Sample
	{
		public DateTime At;

		public double FreeGb;

		public double TotalGb;
	}

	private static string FilePath(Settings settings)
	{
		return Path.Combine(settings.DataDir, "space-history.json");
	}

	/// <summary>
	/// Записывает сегодняшние значения, если за сегодня их ещё нет.
	/// Вызывается часто (каждый тик планировщика) — поэтому проверка дешёвая.
	/// </summary>
	public static void RecordIfDue(Settings settings)
	{
		if (settings == null)
		{
			return;
		}
		try
		{
			Dictionary<string, object> host = VmInventory.Host(out string error, force: false);
			if (host == null || host.Count == 0)
			{
				return;
			}
			Record(settings, host);
		}
		catch (Exception ex)
		{
			Log.Warn("История места: " + ex.Message);
		}
	}

	/// <summary>Записывает точку по уже прочитанным данным хоста.</summary>
	public static void Record(Settings settings, Dictionary<string, object> host)
	{
		if (settings == null || host == null)
		{
			return;
		}
		List<object> drives = Json.AsArray(Json.Get(host, "drives"));
		if (drives.Count == 0)
		{
			return;
		}

		lock (Sync)
		{
			try
			{
				Dictionary<string, object> store = LoadStore(settings);
				Dictionary<string, object> byDrive = Json.AsObject(Json.Get(store, "drives"));
				if (byDrive.Count == 0)
				{
					byDrive = Json.Obj();
					store["drives"] = byDrive;
				}

				DateTime today = DateTime.Today;
				bool changed = false;

				foreach (object o in drives)
				{
					Dictionary<string, object> d = Json.AsObject(o);
					string name = Json.GetStr(d, "name", "");
					if (name.Length == 0)
					{
						continue;
					}
					double freeGb = Json.Dbl(Json.Get(d, "freeGb"), 0.0);
					double totalGb = Json.Dbl(Json.Get(d, "totalGb"), 0.0);
					if (totalGb <= 0)
					{
						continue;
					}

					List<Sample> samples = ReadSamples(byDrive, name);

					// Точка за сегодня уже есть — заменяем её, а не добавляем
					// вторую: за день место может меняться, интересно последнее.
					int existing = samples.FindIndex(s => s.At.Date == today);
					if (existing >= 0)
					{
						if (Math.Abs(samples[existing].FreeGb - freeGb) < 0.05)
						{
							continue;
						}
						samples[existing].FreeGb = freeGb;
						samples[existing].TotalGb = totalGb;
					}
					else
					{
						samples.Add(new Sample { At = today, FreeGb = freeGb, TotalGb = totalGb });
					}
					changed = true;

					// Чистим старое и сортируем по времени.
					DateTime cutoff = today.AddDays(-KeepDays);
					samples.RemoveAll(s => s.At < cutoff);
					samples.Sort((a, b) => a.At.CompareTo(b.At));
					byDrive[name] = ToJsonList(samples);
				}

				if (changed)
				{
					Directory.CreateDirectory(settings.DataDir);
					File.WriteAllText(FilePath(settings), Json.WritePretty(store));
				}
			}
			catch (Exception ex)
			{
				Log.Warn("История места, запись: " + ex.Message);
			}
		}
	}

	/// <summary>
	/// Прогноз по всем дискам хоста. Диски без истории тоже попадают в ответ,
	/// но с пометкой «мало данных» — иначе интерфейс молчал бы о них.
	/// </summary>
	public static List<Dictionary<string, object>> Forecast(Settings settings)
	{
		List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
		if (settings == null)
		{
			return result;
		}

		try
		{
			Dictionary<string, object> host = VmInventory.Host(out string error, force: false);
			List<object> drives = Json.AsArray(Json.Get(host, "drives"));

			Dictionary<string, object> byDrive;
			lock (Sync)
			{
				byDrive = Json.AsObject(Json.Get(LoadStore(settings), "drives"));
			}

			foreach (object o in drives)
			{
				Dictionary<string, object> d = Json.AsObject(o);
				string name = Json.GetStr(d, "name", "");
				if (name.Length == 0)
				{
					continue;
				}
				double freeGb = Json.Dbl(Json.Get(d, "freeGb"), 0.0);
				double totalGb = Json.Dbl(Json.Get(d, "totalGb"), 0.0);

				List<Sample> samples = ReadSamples(byDrive, name);
				result.Add(BuildForecast(name, freeGb, totalGb, samples));
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Прогноз места: " + ex.Message);
		}
		return result;
	}

	/// <summary>Прогноз по одному диску (или null, если диска нет).</summary>
	public static Dictionary<string, object> ForecastFor(Settings settings, string driveName)
	{
		foreach (Dictionary<string, object> f in Forecast(settings))
		{
			if (string.Equals(Json.GetStr(f, "name", ""), driveName, StringComparison.OrdinalIgnoreCase))
			{
				return f;
			}
		}
		return null;
	}

	// ------------------------------------------------------------------ расчёт

	private static Dictionary<string, object> BuildForecast(string name, double freeGb, double totalGb, List<Sample> samples)
	{
		Dictionary<string, object> r = Json.Obj();
		r["name"] = name;
		r["freeGb"] = Math.Round(freeGb, 1);
		r["totalGb"] = Math.Round(totalGb, 1);
		r["freePercent"] = totalGb > 0 ? Math.Round(freeGb * 100.0 / totalGb, 1) : 0.0;
		r["samples"] = samples.Count;

		// Берём последние WindowDays точек: старые выбросы (разово освободили
		// терабайт) не должны портить линию.
		DateTime cutoff = DateTime.Today.AddDays(-WindowDays);
		List<Sample> window = samples.FindAll(s => s.At >= cutoff);
		if (window.Count < MinSamples)
		{
			r["trend"] = "мало данных";
			r["perDayGb"] = 0.0;
			r["daysToFull"] = null;
			r["warning"] = false;
			r["note"] = "нужно минимум " + MinSamples + " точки за " + WindowDays + " дней (есть " + window.Count + ")";
			return r;
		}

		// Прямая по методу наименьших квадратов: y = a + b·x, где x — дни
		// от первой точки окна.
		DateTime origin = window[0].At;
		double sumX = 0, sumY = 0, sumXY = 0, sumXX = 0;
		int n = window.Count;
		foreach (Sample s in window)
		{
			double x = (s.At - origin).TotalDays;
			double y = s.FreeGb;
			sumX += x;
			sumY += y;
			sumXY += x * y;
			sumXX += x * x;
		}
		double denom = n * sumXX - sumX * sumX;
		double slope = Math.Abs(denom) < 1e-9 ? 0.0 : (n * sumXY - sumX * sumY) / denom;

		r["perDayGb"] = Math.Round(slope, 2);

		string trend;
		if (slope < -0.05)
		{
			trend = "убывает";
		}
		else if (slope > 0.05)
		{
			trend = "растёт";
		}
		else
		{
			trend = "стабильно";
		}
		r["trend"] = trend;

		if (slope < -0.01 && freeGb > 0)
		{
			double days = freeGb / (-slope);
			r["daysToFull"] = Math.Round(days, 0);
			// Месяц — это тот срок, за который ещё можно что-то сделать.
			r["warning"] = days <= 30;
			if (days <= 7)
			{
				r["note"] = "место кончится на этой неделе";
			}
			else if (days <= 30)
			{
				r["note"] = "место кончится примерно через " + Math.Round(days, 0) + " дн.";
			}
			else
			{
				r["note"] = "";
			}
		}
		else
		{
			r["daysToFull"] = null;
			bool low = freeGb < totalGb * 0.1;
			r["warning"] = low;
			r["note"] = low ? "свободно меньше 10%, но место не убывает" : "";
		}

		return r;
	}

	// ------------------------------------------------------------------ файл

	private static Dictionary<string, object> LoadStore(Settings settings)
	{
		try
		{
			string file = FilePath(settings);
			if (!File.Exists(file))
			{
				return Json.Obj();
			}
			Dictionary<string, object> obj = Json.ParseObject(File.ReadAllText(file));
			return obj.Count > 0 ? obj : Json.Obj();
		}
		catch (Exception ex)
		{
			Log.Warn("История места, чтение: " + ex.Message);
			return Json.Obj();
		}
	}

	private static List<Sample> ReadSamples(Dictionary<string, object> byDrive, string driveName)
	{
		List<Sample> list = new List<Sample>();
		if (byDrive == null || !byDrive.ContainsKey(driveName))
		{
			return list;
		}
		foreach (object o in Json.AsArray(byDrive[driveName]))
		{
			Dictionary<string, object> d = Json.AsObject(o);
			string at = Json.GetStr(d, "at", "");
			if (!DateTime.TryParse(at, out DateTime dt))
			{
				continue;
			}
			list.Add(new Sample
			{
				At = dt.Date,
				FreeGb = Json.Dbl(Json.Get(d, "freeGb"), 0.0),
				TotalGb = Json.Dbl(Json.Get(d, "totalGb"), 0.0)
			});
		}
		list.Sort((a, b) => a.At.CompareTo(b.At));
		return list;
	}

	private static List<object> ToJsonList(List<Sample> samples)
	{
		List<object> list = new List<object>();
		foreach (Sample s in samples)
		{
			Dictionary<string, object> d = Json.Obj();
			d["at"] = s.At.ToString("yyyy-MM-dd");
			d["freeGb"] = Math.Round(s.FreeGb, 1);
			d["totalGb"] = Math.Round(s.TotalGb, 1);
			list.Add(d);
		}
		return list;
	}
}
