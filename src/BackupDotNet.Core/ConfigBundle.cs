using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace BackupDotNet.Core;

/// <summary>
/// Перенос конфигурации одним архивом: экспорт и импорт.
///
/// Зачем: инсталляция живёт на нескольких машинах (центральный агент, филиалы,
/// тестовый стенд), и переносить её вручную по десяти файлам — источник ошибок.
/// Здесь один ZIP: настройки, брендинг, пути и все компании.
///
/// Правила, которые соблюдаются сознательно:
///  • <b>секреты не выгружаются по умолчанию</b> — их приходится просить явно;
///  • перед импортом текущая конфигурация <b>сохраняется в архив</b>, так что
///    неудачный импорт всегда можно откатить;
///  • импорт умеет работать «вхолостую» (dry-run) и рассказать, что изменится;
///  • файлы пишутся атомарно, через <see cref="ConfigLoader.AtomicWrite"/>.
/// </summary>
public static class ConfigBundle
{
	/// <summary>Версия формата архива. Меняется, если ломается совместимость.</summary>
	public const int FormatVersion = 1;

	/// <summary>Имена файлов, которые участвуют в переносе.</summary>
	private static readonly string[] SingleFiles =
	{
		"settings.json",
		"branding.json",
		"env.json"
	};

	private const string SecretsName = "secrets.json";

	private const string ManifestName = "manifest.json";

	/// <summary>Результат импорта: что сделано и что при этом было в файлах.</summary>
	public class ImportResult
	{
		public bool Ok;

		public string Error = "";

		public bool DryRun;

		public string BackupPath = "";

		public int FilesWritten;

		public int CompaniesImported;

		public int CompaniesSkipped;

		public bool SecretsImported;

		public string ManifestProduct = "";

		public string ManifestVersion = "";

		public DateTime? ManifestCreatedAt;

		public List<object> Changes = new List<object>();

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["ok"] = Ok;
			d["error"] = Error;
			d["dryRun"] = DryRun;
			d["backupPath"] = BackupPath;
			d["filesWritten"] = FilesWritten;
			d["companiesImported"] = CompaniesImported;
			d["companiesSkipped"] = CompaniesSkipped;
			d["secretsImported"] = SecretsImported;
			d["manifestProduct"] = ManifestProduct;
			d["manifestVersion"] = ManifestVersion;
			d["manifestCreatedAt"] = ManifestCreatedAt;
			d["changes"] = Changes;
			return d;
		}
	}

	// ------------------------------------------------------------------ экспорт

	/// <summary>
	/// Собирает ZIP с конфигурацией. Возвращает байты архива.
	/// </summary>
	/// <param name="configDir">Каталог конфигурации.</param>
	/// <param name="includeSecrets">Включить <c>secrets.json</c> (по умолчанию нет).</param>
	/// <param name="companyIds">
	/// Ограничить набор компаний. <c>null</c> — все. Пустой список — ни одной.
	/// </param>
	public static byte[] Export(string configDir, bool includeSecrets, ICollection<string> companyIds)
	{
		using MemoryStream ms = new MemoryStream();
		using (ZipArchive zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
		{
			Dictionary<string, object> manifest = Json.Obj();
			manifest["format"] = FormatVersion;
			manifest["product"] = Branding.Current.ProductName;
			manifest["version"] = AssemblyVersion();
			manifest["createdAt"] = DateTime.Now;
			manifest["machine"] = Safe(() => Environment.MachineName, "");
			manifest["includesSecrets"] = includeSecrets;
			WriteEntry(zip, ManifestName, Json.WritePretty(manifest));

			foreach (string name in SingleFiles)
			{
				AddFile(zip, configDir, name);
			}
			if (includeSecrets)
			{
				AddFile(zip, configDir, SecretsName);
			}

			string companiesDir = Path.Combine(configDir, "companies");
			if (Directory.Exists(companiesDir))
			{
				List<string> files = new List<string>(Directory.GetFiles(companiesDir, "*.json"));
				files.Sort(StringComparer.OrdinalIgnoreCase);
				foreach (string f in files)
				{
					string fileName = Path.GetFileName(f);
					// Файлы, начинающиеся с «_», — заметки, они не являются компаниями.
					if (fileName.StartsWith("_", StringComparison.Ordinal))
					{
						continue;
					}
					if (companyIds != null)
					{
						string id = Path.GetFileNameWithoutExtension(fileName);
						bool wanted = false;
						foreach (string c in companyIds)
						{
							if (string.Equals(c, id, StringComparison.OrdinalIgnoreCase))
							{
								wanted = true;
								break;
							}
						}
						if (!wanted)
						{
							continue;
						}
					}
					AddFile(zip, configDir, "companies/" + fileName);
				}
			}
		}
		return ms.ToArray();
	}

	private static void AddFile(ZipArchive zip, string configDir, string relative)
	{
		string full = Path.Combine(configDir, relative.Replace('/', Path.DirectorySeparatorChar));
		if (!File.Exists(full))
		{
			return;
		}
		try
		{
			WriteEntry(zip, relative, File.ReadAllText(full, Encoding.UTF8));
		}
		catch
		{
			// Нечитаемый файл просто не попадает в архив — экспорт не должен падать.
		}
	}

	private static void WriteEntry(ZipArchive zip, string name, string content)
	{
		ZipArchiveEntry e = zip.CreateEntry(name, CompressionLevel.Optimal);
		using Stream s = e.Open();
		byte[] bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(content ?? "");
		s.Write(bytes, 0, bytes.Length);
	}

	// ------------------------------------------------------------------ импорт

	/// <summary>
	/// Разбирает архив и применяет его к каталогу конфигурации.
	/// </summary>
	/// <param name="configDir">Куда применять.</param>
	/// <param name="archive">Байты ZIP.</param>
	/// <param name="dryRun">Только показать, что изменится.</param>
	/// <param name="allowSecrets">Разрешить импорт <c>secrets.json</c>.</param>
	/// <param name="backupDir">
	/// Куда положить архив с текущей конфигурацией перед изменениями.
	/// Пустая строка — не делать резервную копию.
	/// </param>
	public static ImportResult Import(string configDir, byte[] archive, bool dryRun, bool allowSecrets, string backupDir)
	{
		ImportResult res = new ImportResult();
		res.DryRun = dryRun;

		Dictionary<string, string> entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			using MemoryStream ms = new MemoryStream(archive ?? Array.Empty<byte>());
			using ZipArchive zip = new ZipArchive(ms, ZipArchiveMode.Read);
			foreach (ZipArchiveEntry e in zip.Entries)
			{
				if (e.FullName.EndsWith("/", StringComparison.Ordinal))
				{
					continue;
				}
				// Защита от «zip slip»: только плоские имена и companies/<файл>.
				string name = e.FullName.Replace('\\', '/');
				if (name.Contains(".."))
				{
					res.Error = "архив содержит недопустимый путь: " + name;
					return res;
				}
				if (!IsAllowedEntry(name))
				{
					continue;
				}
				using Stream s = e.Open();
				using StreamReader sr = new StreamReader(s, Encoding.UTF8);
				entries[name] = sr.ReadToEnd();
			}
		}
		catch (Exception ex)
		{
			res.Error = "не удалось прочитать архив: " + ex.Message;
			return res;
		}

		if (entries.Count == 0)
		{
			res.Error = "архив пуст или не содержит конфигурации";
			return res;
		}

		// --- манифест ------------------------------------------------------
		if (entries.TryGetValue(ManifestName, out var manifestText))
		{
			try
			{
				Dictionary<string, object> m = Json.ParseObject(manifestText);
				res.ManifestProduct = Json.GetStr(m, "product", "");
				res.ManifestVersion = Json.GetStr(m, "version", "");
				int fmt = Json.Int(Json.Get(m, "format"), 0);
				if (fmt > FormatVersion)
				{
					res.Error = "архив создан более новой версией (формат " + fmt + ", поддерживается до " + FormatVersion + ")";
					return res;
				}
				if (Json.Get(m, "createdAt") != null)
				{
					res.ManifestCreatedAt = ParseDate(Json.Str(Json.Get(m, "createdAt")));
				}
			}
			catch
			{
				// Манифест битый — не повод отказываться: файлы всё равно проверим по одному.
			}
		}

		// --- что изменится ------------------------------------------------
		List<string> writes = new List<string>();
		foreach (string name in SingleFiles)
		{
			if (!entries.ContainsKey(name))
			{
				continue;
			}
			if (!ValidateJson(name, entries[name], res))
			{
				return res;
			}
			writes.Add(name);
			AddChange(res, name, DescribeChange(configDir, name, entries[name]));
		}

		if (entries.ContainsKey(SecretsName))
		{
			if (!allowSecrets)
			{
				AddChange(res, SecretsName, "пропущен", "секреты не импортируются без явного разрешения");
			}
			else if (!ValidateJson(SecretsName, entries[SecretsName], res))
			{
				return res;
			}
			else
			{
				writes.Add(SecretsName);
				res.SecretsImported = true;
				AddChange(res, SecretsName, "будет заменён", "содержит учётные данные");
			}
		}

		List<KeyValuePair<string, string>> companies = new List<KeyValuePair<string, string>>();
		foreach (KeyValuePair<string, string> kv in entries)
		{
			if (!kv.Key.StartsWith("companies/", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			string fileName = kv.Key.Substring("companies/".Length);
			if (fileName.Length == 0 || !fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			if (!ValidateJson(kv.Key, kv.Value, res))
			{
				return res;
			}
			companies.Add(new KeyValuePair<string, string>(fileName, kv.Value));
		}
		companies.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase));

		foreach (KeyValuePair<string, string> c in companies)
		{
			string target = "companies/" + c.Key;
			writes.Add(target);
			AddChange(res, target, DescribeChange(configDir, target, c.Value));
		}

		if (writes.Count == 0)
		{
			res.Error = "в архиве нет файлов, которые можно применить";
			return res;
		}

		if (dryRun)
		{
			res.Ok = true;
			res.CompaniesImported = companies.Count;
			return res;
		}

		// --- резервная копия ----------------------------------------------
		if (!string.IsNullOrEmpty(backupDir))
		{
			try
			{
				Directory.CreateDirectory(backupDir);
				string path = Path.Combine(backupDir,
					"config-before-import-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture) + ".zip");
				byte[] current = Export(configDir, includeSecrets: true, companyIds: null);
				File.WriteAllBytes(path, current);
				res.BackupPath = path;
			}
			catch (Exception ex)
			{
				res.Error = "не удалось сделать резервную копию конфигурации: " + ex.Message;
				return res;
			}
		}

		// --- запись --------------------------------------------------------
		try
		{
			foreach (string name in writes)
			{
				string full = Path.Combine(configDir, name.Replace('/', Path.DirectorySeparatorChar));
				ConfigLoader.AtomicWrite(full, entries[name]);
				res.FilesWritten++;
			}
		}
		catch (Exception ex)
		{
			res.Error = "ошибка записи: " + ex.Message +
				(res.BackupPath.Length > 0 ? " (прежняя конфигурация в " + res.BackupPath + ")" : "");
			return res;
		}

		res.Ok = true;
		res.CompaniesImported = companies.Count;
		return res;
	}

	private static bool IsAllowedEntry(string name)
	{
		if (string.Equals(name, ManifestName, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		foreach (string f in SingleFiles)
		{
			if (string.Equals(name, f, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		if (string.Equals(name, SecretsName, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (name.StartsWith("companies/", StringComparison.OrdinalIgnoreCase))
		{
			string rest = name.Substring("companies/".Length);
			return rest.Length > 0 && rest.IndexOf('/') < 0;
		}
		return false;
	}

	private static bool ValidateJson(string name, string text, ImportResult res)
	{
		try
		{
			Json.ParseObject(text);
			return true;
		}
		catch (Exception ex)
		{
			res.Error = name + ": некорректный JSON — " + ex.Message;
			return false;
		}
	}

	/// <summary>Описывает, что произойдёт с файлом: новый, идентичный, изменённый.</summary>
	private static string DescribeChange(string configDir, string relative, string newText)
	{
		string full = Path.Combine(configDir, relative.Replace('/', Path.DirectorySeparatorChar));
		if (!File.Exists(full))
		{
			return "новый";
		}
		try
		{
			string old = File.ReadAllText(full, Encoding.UTF8);
			// Сравниваем нормализованный JSON: форматирование не должно давать «изменён».
			string a = Json.Write(Json.ParseObject(old));
			string b = Json.Write(Json.ParseObject(newText));
			return string.Equals(a, b, StringComparison.Ordinal) ? "без изменений" : "будет заменён";
		}
		catch
		{
			return "будет заменён";
		}
	}

	private static void AddChange(ImportResult res, string file, string action, string note = "")
	{
		Dictionary<string, object> c = Json.Obj();
		c["file"] = file;
		c["action"] = action;
		if (!string.IsNullOrEmpty(note))
		{
			c["note"] = note;
		}
		res.Changes.Add(c);
	}

	/// <summary>Разбирает дату из ISO-8601 (как её пишет <see cref="Json"/>).</summary>
	private static DateTime? ParseDate(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return null;
		}
		if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
		{
			return dt;
		}
		return null;
	}

	private static string AssemblyVersion()	{
		try
		{
			Version v = typeof(ConfigBundle).Assembly.GetName().Version;
			return (v == null) ? "1.0.0" : (v.Major + "." + v.Minor + "." + v.Build);
		}
		catch
		{
			return "1.0.0";
		}
	}

	private static T Safe<T>(Func<T> f, T dflt)
	{
		try
		{
			return f();
		}
		catch
		{
			return dflt;
		}
	}
}
