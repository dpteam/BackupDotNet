using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace BackupDotNet.Core.Support;

/// <summary>
/// Сборка «логпака» для передачи в поддержку или ИИ на анализ.
///
/// Внутри — всё, что нужно для разбора: журналы, состояние, конфигурация,
/// отчёт самодиагностики. Снаружи — ничего лишнего: имена, адреса и секреты
/// заменены плейсхолдерами <b>до</b> упаковки, поэтому даже распакованный
/// архив нельзя использовать против владельца.
///
/// Чего здесь <b>нет</b> и почему:
///  • <c>secrets.json</c> — не включается вообще, даже обезличенный: сама
///    структура ключей может подсказать, что искать;
///  • <c>users.json</c> — вместо него обезличенная сводка (роли, число);
///  • файлы бэкапов — только имена копий и размеры, без содержимого.
/// </summary>
public static class LogPack
{
	/// <summary>Сколько последних журналов включать.</summary>
	private const int MaxLogFiles = 30;

	/// <summary>Предел размера одного файла после обезличивания (МБ).</summary>
	private const int MaxFileMb = 8;

	/// <summary>Собирает архив. Возвращает байты ZIP и манифест.</summary>
	public static byte[] Build(Settings settings, AgentCtx ctx, string prefix, string currentVersion,
		out Dictionary<string, object> manifest)
	{
		Redactor red = new Redactor();
		RegisterKnownEntities(red, settings, ctx);

		List<object> included = new List<object>();
		List<object> skipped = new List<object>();

		using MemoryStream ms = new MemoryStream();
		using (ZipArchive zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
		{
			// --- 1. журналы агента
			string logDir = Log.Dir;
			if (string.IsNullOrEmpty(logDir))
			{
				logDir = Path.Combine(settings.DataDir, "logs");
			}
			foreach (FileInfo f in Log.ListFiles(logDir))
			{
				// Публичный журнал — это уже вычищенная копия основного.
				// Класть в архив обе версии незачем: получится дубль
				// с разной степенью обезличивания, и читать его будет труднее.
				if (Log.IsPublicName(f.Name))
				{
					skipped.Add(Json.Obj("file", f.Name, "reason", "публичная копия — в архиве есть исходный журнал"));
					continue;
				}
				if (included.Count >= MaxLogFiles)
				{
					skipped.Add(Json.Obj("file", f.Name, "reason", "сверх лимита " + MaxLogFiles));
					continue;
				}
				AddTextFile(zip, red, "logs/" + f.Name, SafeRead(f.FullName), included, skipped);
			}

			// --- 2. журналы прогонов и аудит
			AddDirectory(zip, red, Path.Combine(settings.DataDir, "journal"), "journal", "*.ndjson", included, skipped, 3);
			AddDirectory(zip, red, Path.Combine(settings.DataDir, "audit"), "audit", "*.ndjson", included, skipped, 2);

			// --- 3. состояние: задания, очереди, расписания
			AddSingle(zip, red, Path.Combine(settings.DataDir, "state", "jobs.json"), "state/jobs.json", included, skipped);
			AddSingle(zip, red, Path.Combine(settings.DataDir, "backups", "jobs", "index.json"),
				"backups/jobs.json", included, skipped);
			AddSingle(zip, red, Path.Combine(settings.DataDir, "backups", "schedules.json"),
				"backups/schedules.json", included, skipped);

			// --- 4. конфигурация (без секретов)
			AddSingle(zip, red, Path.Combine(settings.ConfigDir, "settings.json"), "config/settings.json", included, skipped);
			AddSingle(zip, red, Path.Combine(settings.ConfigDir, "branding.json"), "config/branding.json", included, skipped);
			AddSingle(zip, red, Path.Combine(settings.ConfigDir, "env.json"), "config/env.json", included, skipped);

			// Компании: файлы целиком, но обезличенные.
			string companiesDir = Path.Combine(settings.ConfigDir, "companies");
			if (Directory.Exists(companiesDir))
			{
				int n = 0;
				foreach (string f in Directory.GetFiles(companiesDir, "*.json"))
				{
					if (++n > 50)
					{
						skipped.Add(Json.Obj("file", Path.GetFileName(f), "reason", "сверх лимита 50 компаний"));
						continue;
					}
					AddTextFile(zip, red, "config/companies/" + Path.GetFileName(f),
						SafeRead(f), included, skipped);
				}
			}

			// --- 5. пользователи: только сводка, без имён
			AddUserSummary(zip, red, settings, included);

			// --- 6. отчёт самодиагностики
			try
			{
				Dictionary<string, object> diag = Diagnostics.Build(ctx, null, prefix);
				AddTextFile(zip, red, "diagnostics.json", Json.WritePretty(diag), included, skipped);
			}
			catch (Exception ex)
			{
				skipped.Add(Json.Obj("file", "diagnostics.json", "reason", ex.Message));
			}

			// --- 7. сводка по копиям бэкапа (без содержимого)
			try
			{
				string dest = string.IsNullOrWhiteSpace(settings.BackupDestination)
					? Path.Combine(settings.DataDir, "backups", "store")
					: settings.BackupDestination;
				List<Backup.BackupCopy> copies = Backup.BackupRetention.Scan(dest);
				List<object> list = new List<object>();
				foreach (Backup.BackupCopy c in copies)
				{
					Dictionary<string, object> d = Json.Obj();
					d["name"] = Path.GetFileName(c.Path);
					d["timestamp"] = c.Timestamp;
					d["sizeBytes"] = c.SizeBytes;
					list.Add(d);
				}
				Dictionary<string, object> root = Json.Obj();
				root["count"] = copies.Count;
				root["copies"] = list;
				AddTextFile(zip, red, "backups/copies.json", Json.WritePretty(root), included, skipped);
			}
			catch (Exception ex)
			{
				skipped.Add(Json.Obj("file", "backups/copies.json", "reason", ex.Message));
			}

			// --- 8. README внутри архива
			AddTextFile(zip, red, "README.txt", BuildReadme(currentVersion), included, skipped);

			// --- 9. манифест
			manifest = Json.Obj();
			manifest["product"] = Branding.Current.ProductName;
			manifest["version"] = currentVersion;
			manifest["createdAt"] = DateTime.Now;
			manifest["includesSecrets"] = false;
			manifest["files"] = included;
			manifest["skipped"] = skipped;
			manifest["redaction"] = red.Stats();
			manifest["note"] = "Имена, адреса и секреты заменены плейсхолдерами до упаковки. " +
				"Одинаковое значение всегда заменяется одинаково — по архиву можно рассуждать, " +
				"но нельзя установить, что это за хост или человек.";

			ZipArchiveEntry e = zip.CreateEntry("manifest.json", CompressionLevel.Optimal);
			using Stream s = e.Open();
			byte[] bytes = new UTF8Encoding(false).GetBytes(Json.WritePretty(manifest));
			s.Write(bytes, 0, bytes.Length);
		}
		return ms.ToArray();
	}

	// ------------------------------------------------------------- наполнение

	/// <summary>
	/// Регистрирует всё, что знаем заранее: имена машин, компании, ВМ.
	/// Без этого в логах остались бы имена, не похожие ни на один шаблон.
	/// </summary>
	private static void RegisterKnownEntities(Redactor red, Settings settings, AgentCtx ctx)
	{
		try
		{
			red.Register(Environment.MachineName, "HOST");
			red.Register(Environment.UserName, "USER");
			red.Register(settings.AgentName, "AGENT");
			red.Register(settings.DataDir, "DATAPATH");
			red.Register(settings.ConfigDir, "CONFIGPATH");
			red.Register(AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\'), "INSTALLPATH");
			red.Register(settings.ZabbixServer, "ZABBIX");
			red.Register(settings.BackupDestination, "BACKUPPATH");

			if (ctx != null && ctx.Config != null)
			{
				foreach (Company c in ctx.Config.Companies)
				{
					red.Register(c.Name, "COMPANY");
					red.Register(c.ShortName, "COMPANY");
					red.Register(c.Id, "COMPANYID");
					red.Register(c.Zabbix.Host, "ZABBIXHOST");
					foreach (Contact ct in c.Contacts)
					{
						red.Register(ct.Name, "PERSON");
						red.Register(ct.Phone, "PHONE");
						red.Register(ct.Email, "EMAIL");
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Debug("Логпак: регистрация сущностей — " + ex.Message);
		}
	}

	private static void AddDirectory(ZipArchive zip, Redactor red, string dir, string zipPrefix,
		string pattern, List<object> included, List<object> skipped, int maxFiles)
	{
		if (!Directory.Exists(dir))
		{
			return;
		}
		try
		{
			List<string> files = new List<string>(Directory.GetFiles(dir, pattern));
			// Свежие первыми: при разборе важнее последние дни.
			files.Sort((a, b) => string.Compare(b, a, StringComparison.OrdinalIgnoreCase));
			int n = 0;
			foreach (string f in files)
			{
				if (++n > maxFiles)
				{
					skipped.Add(Json.Obj("file", Path.GetFileName(f), "reason", "сверх лимита " + maxFiles));
					continue;
				}
				AddTextFile(zip, red, zipPrefix + "/" + Path.GetFileName(f), SafeRead(f), included, skipped);
			}
		}
		catch (Exception ex)
		{
			skipped.Add(Json.Obj("dir", dir, "reason", ex.Message));
		}
	}

	private static void AddSingle(ZipArchive zip, Redactor red, string path, string zipName,
		List<object> included, List<object> skipped)
	{
		if (!File.Exists(path))
		{
			return;
		}
		AddTextFile(zip, red, zipName, SafeRead(path), included, skipped);
	}

	private static void AddTextFile(ZipArchive zip, Redactor red, string zipName, string content,
		List<object> included, List<object> skipped)
	{
		try
		{
			// Имя файла тоже обезличиваем: «config/companies/secretcorp.json»
			// выдаёт идентификатор компании не хуже содержимого.
			string safeName = red.Redact(zipName ?? "file");
			// Плейсхолдеры содержат «<>», а это недопустимые символы в имени
			// внутри ZIP на некоторых распаковщиках — приводим к безопасному виду.
			safeName = safeName.Replace('<', '_').Replace('>', '_');

			string clean = red.Redact(content ?? "");
			// Ограничение размера: журнал в 200 МБ в архиве бесполезен,
			// его никто не прочитает. Берём хвост — там свежие события.
			if (clean.Length > MaxFileMb * 1024 * 1024)
			{
				int cut = clean.Length - MaxFileMb * 1024 * 1024;
				int nl = clean.IndexOf('\n', cut);
				if (nl > 0)
				{
					clean = "... (начало обрезано)\n" + clean.Substring(nl + 1);
				}
				skipped.Add(Json.Obj("file", safeName, "reason", "обрезан до " + MaxFileMb + " МБ"));
			}

			ZipArchiveEntry e = zip.CreateEntry(safeName, CompressionLevel.Optimal);
			using Stream s = e.Open();
			byte[] bytes = new UTF8Encoding(false).GetBytes(clean);
			s.Write(bytes, 0, bytes.Length);

			Dictionary<string, object> d = Json.Obj();
			d["file"] = safeName;
			d["bytes"] = bytes.Length;
			included.Add(d);
		}
		catch (Exception ex)
		{
			skipped.Add(Json.Obj("file", zipName, "reason", ex.Message));
		}
	}

	/// <summary>
	/// Пользователи: только количество и роли. Имена и хеши паролей не нужны
	/// для разбора, а хеши — это учётные данные.
	/// </summary>
	private static void AddUserSummary(ZipArchive zip, Redactor red, Settings settings, List<object> included)
	{
		try
		{
			string path = Path.Combine(settings.ConfigDir, "users.json");
			if (!File.Exists(path))
			{
				return;
			}
			Dictionary<string, object> root = Json.ParseObject(SafeRead(path));
			Dictionary<string, int> roles = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			int total = 0;
			foreach (object o in Json.AsArray(Json.Get(root, "users")))
			{
				Dictionary<string, object> u = Json.AsObject(o);
				string role = Json.GetStr(u, "role", "unknown");
				roles.TryGetValue(role, out int n);
				roles[role] = n + 1;
				total++;
			}
			Dictionary<string, object> summary = Json.Obj();
			summary["total"] = total;
			Dictionary<string, object> r = Json.Obj();
			foreach (KeyValuePair<string, int> kv in roles)
			{
				r[kv.Key] = kv.Value;
			}
			summary["roles"] = r;
			summary["note"] = "Имена, логины и хеши паролей не включаются";

			Dictionary<string, object> wrapper = Json.Obj();
			wrapper["users"] = summary;
			AddTextFile(zip, red, "config/users-summary.json", Json.WritePretty(wrapper), included,
				new List<object>());
		}
		catch (Exception ex)
		{
			Log.Debug("Логпак: сводка пользователей — " + ex.Message);
		}
	}

	private static string BuildReadme(string version)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine(Branding.Current.ProductName + " " + version + " — логпак для поддержки");
		sb.AppendLine("Собран: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
		sb.AppendLine();
		sb.AppendLine("ЧТО ВНУТРИ");
		sb.AppendLine("  logs/          журналы агента (последние " + MaxLogFiles + ")");
		sb.AppendLine("  journal/       журнал прогонов проверок");
		sb.AppendLine("  audit/         журнал действий пользователей");
		sb.AppendLine("  state/         текущее состояние заданий");
		sb.AppendLine("  backups/       задания и расписания бэкапов, список копий");
		sb.AppendLine("  config/        настройки, брендинг, компании");
		sb.AppendLine("  diagnostics.json  отчёт самодиагностики");
		sb.AppendLine("  manifest.json  что вошло, что пропущено, сколько замен");
		sb.AppendLine();
		sb.AppendLine("ЧТО ОБЕЗЛИЧЕНО");
		sb.AppendLine("  Пароли, токены, ключи API       → <SECRET>");
		sb.AppendLine("  IP-адреса                       → <IP> / <IP6>");
		sb.AppendLine("  Почтовые адреса                 → <EMAIL>");
		sb.AppendLine("  Имена машин и домены            → <HOST> / <HOST-N>");
		sb.AppendLine("  Имена компаний, ВМ, людей       → <COMPANY-N> / <PERSON-N>");
		sb.AppendLine("  Пути с именами пользователей    → C:\\Users\\<USER>\\...");
		sb.AppendLine("  Сетевые папки                   → \\\\<HOST>\\<SHARE>");
		sb.AppendLine();
		sb.AppendLine("Замена сквозная: одно и то же значение всегда превращается в один и тот же");
		sb.AppendLine("плейсхолдер. Поэтому по архиву можно рассуждать («HOST-1 не отвечает час»),");
		sb.AppendLine("но нельзя установить, что это за хост.");
		sb.AppendLine();
		sb.AppendLine("ЧЕГО ЗДЕСЬ НЕТ");
		sb.AppendLine("  secrets.json — не включается вообще, даже обезличенный;");
		sb.AppendLine("  users.json   — только сводка по ролям, без имён и хешей;");
		sb.AppendLine("  файлы резервных копий — только имена и размеры.");
		return sb.ToString();
	}

	/// <summary>Читает файл, не падая на занятых и очень больших.</summary>
	private static string SafeRead(string path)
	{
		try
		{
			if (!File.Exists(path))
			{
				return "";
			}
			FileInfo fi = new FileInfo(path);
			// Больше 64 МБ читать в память незачем: такой файл всё равно обрежется.
			if (fi.Length > 64L * 1024 * 1024)
			{
				using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read,
					FileShare.ReadWrite | FileShare.Delete);
				fs.Seek(-32L * 1024 * 1024, SeekOrigin.End);
				using StreamReader sr = new StreamReader(fs, Encoding.UTF8);
				return "(начало файла пропущено: он больше 64 МБ)\n" + sr.ReadToEnd();
			}
			using FileStream fs2 = new FileStream(path, FileMode.Open, FileAccess.Read,
				FileShare.ReadWrite | FileShare.Delete);
			using StreamReader sr2 = new StreamReader(fs2, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
			return sr2.ReadToEnd();
		}
		catch (Exception ex)
		{
			return "(не удалось прочитать: " + ex.GetType().Name + ")";
		}
	}
}
