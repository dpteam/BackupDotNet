using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace BackupDotNet.Core;

/// <summary>
/// Проверка обновлений по локальному каталогу (в том числе по сетевой папке).
///
/// Зачем не интернет: продукт делается для контуров без внешней сети. Обновления
/// раскладываются на файловом сервере, агент читает оттуда маленький манифест
/// и говорит: «есть версия новее». Скачивать и устанавливать он не пытается —
/// это осознанно оставлено человеку.
///
/// Формат <c>latest.json</c> в каталоге обновлений:
/// <code>
/// {
///   "version": "1.1.0",
///   "date": "2026-11-20",
///   "file": "BackupDotNet-1.1.0.zip",
///   "notes": "что нового",
///   "minFrom": "1.0.0"
/// }
/// </code>
/// </summary>
public static class UpdateChecker
{
	/// <summary>Имя файла-манифеста в каталоге обновлений.</summary>
	public const string ManifestName = "latest.json";

	/// <summary>Результат проверки.</summary>
	public class Result
	{
		public bool Enabled;

		public bool Available;

		public string CurrentVersion = "";

		public string LatestVersion = "";

		public string Path = "";

		public string FileName = "";

		public string Notes = "";

		public string Error = "";

		public DateTime? PublishedAt;

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["enabled"] = Enabled;
			d["available"] = Available;
			d["currentVersion"] = CurrentVersion;
			d["latestVersion"] = LatestVersion;
			d["path"] = Path;
			d["fileName"] = FileName;
			d["notes"] = Notes;
			d["error"] = Error;
			d["publishedAt"] = PublishedAt;
			return d;
		}
	}

	/// <summary>
	/// Читает манифест и сравнивает версии. Не бросает исключений:
	/// недоступная сетевая папка — обычная ситуация, а не авария.
	/// </summary>
	public static Result Check(Settings settings, string currentVersion)
	{
		Result r = new Result();
		r.CurrentVersion = currentVersion ?? "";
		r.Enabled = settings != null && settings.UpdatesEnabled;
		r.Path = settings == null ? "" : (settings.UpdatesPath ?? "");

		if (!r.Enabled)
		{
			r.Error = "проверка обновлений выключена (updates.enabled = false)";
			return r;
		}
		if (string.IsNullOrWhiteSpace(r.Path))
		{
			r.Error = "не задан каталог обновлений (updates.path)";
			return r;
		}

		string manifest = Path.Combine(r.Path, ManifestName);
		string text;
		try
		{
			if (!File.Exists(manifest))
			{
				r.Error = "в каталоге нет " + ManifestName + ": " + r.Path;
				return r;
			}
			text = File.ReadAllText(manifest, Encoding.UTF8);
		}
		catch (Exception ex)
		{
			r.Error = "каталог обновлений недоступен: " + ex.Message;
			return r;
		}

		Dictionary<string, object> m;
		try
		{
			m = Json.ParseObject(text);
		}
		catch (Exception ex)
		{
			r.Error = ManifestName + ": некорректный JSON — " + ex.Message;
			return r;
		}

		r.LatestVersion = Json.GetStr(m, "version", "").Trim();
		r.FileName = Json.GetStr(m, "file", "").Trim();
		r.Notes = Json.GetStr(m, "notes", "").Trim();
		if (r.LatestVersion.Length == 0)
		{
			r.Error = ManifestName + ": не указана версия";
			return r;
		}
		string dateText = Json.GetStr(m, "date", "").Trim();
		if (dateText.Length > 0 &&
			DateTime.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
		{
			r.PublishedAt = dt;
		}

		r.Available = Compare(r.LatestVersion, r.CurrentVersion) > 0;
		return r;
	}

	/// <summary>
	/// Сравнивает версии вида «1.2.3» / «1.2.3.4». Возвращает 1, если
	/// <paramref name="a"/> новее, -1 если старее, 0 если равны.
	/// Нечисловые хвосты (суффиксы вроде «-beta») отбрасываются.
	/// </summary>
	public static int Compare(string a, string b)
	{
		int[] pa = Parse(a);
		int[] pb = Parse(b);
		int len = Math.Max(pa.Length, pb.Length);
		for (int i = 0; i < len; i++)
		{
			int x = (i < pa.Length) ? pa[i] : 0;
			int y = (i < pb.Length) ? pb[i] : 0;
			if (x != y)
			{
				return (x > y) ? 1 : -1;
			}
		}
		return 0;
	}

	private static int[] Parse(string v)
	{
		if (string.IsNullOrEmpty(v))
		{
			return new int[0];
		}
		// Отбрасываем суффикс: 1.2.3-beta -> 1.2.3
		int dash = v.IndexOf('-');
		if (dash > 0)
		{
			v = v.Substring(0, dash);
		}
		dash = v.IndexOf('+');
		if (dash > 0)
		{
			v = v.Substring(0, dash);
		}
		string[] parts = v.Split('.');
		List<int> nums = new List<int>();
		foreach (string p in parts)
		{
			if (int.TryParse(p.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
			{
				nums.Add(n);
			}
			else
			{
				break;
			}
		}
		return nums.ToArray();
	}
}
