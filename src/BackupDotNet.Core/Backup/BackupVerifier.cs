using System;
using System.Collections.Generic;
using System.IO;

namespace BackupDotNet.Core.Backup;

/// <summary>
/// Проверка пригодности копии бэкапа.
///
/// Что проверяется и почему именно это:
///  • <b>наличие обязательных файлов</b> — конфигурация и хотя бы один диск;
///  • <b>подпись формата диска</b> — самая частая реальная поломка это не «битый
///    сектор», а недокопированный или чужой файл. У VHDX первые 8 байт —
///    <c>vhdxfile</c>, у VHD последние 512 — <c>conectix</c>. Если подписи нет,
///    образ не смонтируется ни при каких обстоятельствах;
///  • <b>ненулевой размер</b> — нулевой файл проходит любую проверку «файл есть»;
///  • <b>совпадение с размером на момент создания</b> — ловит обрезанную копию.
///
/// Чего проверка НЕ делает: не монтирует образ и не поднимает ВМ. Монтирование
/// требует полных прав администратора, а пробный импорт ВМ в Hyper-V привязывает
/// диски к исходным путям — это опасно делать автоматически (см. ROADMAP).
/// Поэтому здесь честная структурная проверка, а не имитация восстановления.
/// </summary>
public static class BackupVerifier
{
	/// <summary>Результат проверки одной копии.</summary>
	public class Result
	{
		public string Path = "";

		public string Name = "";

		/// <summary>ok | warn | fail</summary>
		public string Status = "fail";

		public string Summary = "";

		public long SizeBytes;

		public int FileCount;

		public List<object> Checks = new List<object>();

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["path"] = Path;
			d["name"] = Name;
			d["status"] = Status;
			d["summary"] = Summary;
			d["sizeBytes"] = SizeBytes;
			d["fileCount"] = FileCount;
			d["checks"] = Checks;
			return d;
		}
	}

	/// <summary>
	/// Проверяет копию. <paramref name="expectedBytes"/> — размер на момент
	/// создания (0 — не сверять).
	/// </summary>
	public static Result Verify(string copyPath, long expectedBytes)
	{
		Result r = new Result();
		r.Path = copyPath;
		r.Name = System.IO.Path.GetFileName(copyPath ?? "");

		if (string.IsNullOrEmpty(copyPath) || !Directory.Exists(copyPath))
		{
			r.Status = "fail";
			r.Summary = "каталог копии не найден";
			Add(r, "каталог", "fail", "не найден: " + copyPath);
			return r;
		}

		string[] files;
		try
		{
			files = Directory.GetFiles(copyPath, "*", SearchOption.AllDirectories);
		}
		catch (Exception ex)
		{
			r.Status = "fail";
			r.Summary = "не удалось прочитать каталог";
			Add(r, "каталог", "fail", ex.Message);
			return r;
		}

		r.FileCount = files.Length;
		if (files.Length == 0)
		{
			r.Status = "fail";
			r.Summary = "каталог пуст";
			Add(r, "файлы", "fail", "в копии нет ни одного файла");
			return r;
		}

		// --- группируем по расширению
		List<string> configs = new List<string>();
		List<string> states = new List<string>();
		List<string> disks = new List<string>();
		long total = 0L;
		foreach (string f in files)
		{
			try
			{
				total += new FileInfo(f).Length;
			}
			catch
			{
			}
			string ext = System.IO.Path.GetExtension(f).ToLowerInvariant();
			if (ext == ".vmcx" || ext == ".xml")
			{
				configs.Add(f);
			}
			else if (ext == ".vmgs")
			{
				states.Add(f);
			}
			else if (ext == ".vhdx" || ext == ".vhd" || ext == ".avhdx")
			{
				disks.Add(f);
			}
		}
		r.SizeBytes = total;

		bool failed = false;
		bool warned = false;

		// --- конфигурация
		if (configs.Count == 0)
		{
			Add(r, "конфигурация", "fail", "нет файла конфигурации (.vmcx/.xml)");
			failed = true;
		}
		else
		{
			Add(r, "конфигурация", "ok", "найдено файлов: " + configs.Count);
		}

		// --- диски
		if (disks.Count == 0)
		{
			Add(r, "диски", "fail", "в копии нет ни одного виртуального диска");
			failed = true;
		}
		else
		{
			foreach (string disk in disks)
			{
				string check = VerifyDiskImage(disk, out long size, out string detail);
				Add(r, "диск " + System.IO.Path.GetFileName(disk), check, detail);
				if (check == "fail")
				{
					failed = true;
				}
				else if (check == "warn")
				{
					warned = true;
				}
			}
		}

		// --- состояние ВМ (для Gen2 обычно есть)
		if (states.Count > 0)
		{
			Add(r, "состояние ВМ", "ok", "файлов .vmgs: " + states.Count);
		}

		// --- сверка размера с моментом создания
		if (expectedBytes > 0 && total > 0)
		{
			if (total == expectedBytes)
			{
				Add(r, "размер", "ok", "совпадает с записанным: " + Fmt.Size(total));
			}
			else if (total < expectedBytes)
			{
				Add(r, "размер", "warn",
					"меньше записанного: " + Fmt.Size(total) + " вместо " + Fmt.Size(expectedBytes) +
					" — возможно, копия обрезана");
				warned = true;
			}
			else
			{
				Add(r, "размер", "ok",
					"больше записанного: " + Fmt.Size(total) + " вместо " + Fmt.Size(expectedBytes));
			}
		}

		r.Status = failed ? "fail" : (warned ? "warn" : "ok");
		r.Summary = failed
			? "копия непригодна к восстановлению"
			: (warned ? "копия пригодна, есть замечания" : "копия пригодна к восстановлению");
		return r;
	}

	/// <summary>
	/// Проверяет подпись образа диска. Читаем только начало и конец файла —
	/// гигабайты в память не тянем.
	/// </summary>
	private static string VerifyDiskImage(string path, out long size, out string detail)
	{
		size = 0L;
		detail = "";
		try
		{
			FileInfo fi = new FileInfo(path);
			size = fi.Length;
			if (size == 0)
			{
				detail = "файл нулевого размера";
				return "fail";
			}
			string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
			using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			if (ext == ".vhdx")
			{
				// Минимальный валидный VHDX — 4 МБ (заголовки, таблицы, метаданные).
				if (size < 4L * 1024 * 1024)
				{
					detail = "слишком мал для VHDX: " + Fmt.Size(size);
					return "fail";
				}
				byte[] sig = new byte[8];
				fs.Read(sig, 0, 8);
				string s = System.Text.Encoding.ASCII.GetString(sig);
				if (s != "vhdxfile")
				{
					detail = "нет подписи vhdxfile (найдено «" + Printable(s) + "»)";
					return "fail";
				}
				// Заголовок по смещению 64 КБ — вторая контрольная точка формата.
				fs.Seek(65536, SeekOrigin.Begin);
				byte[] head = new byte[4];
				fs.Read(head, 0, 4);
				if (System.Text.Encoding.ASCII.GetString(head) != "head")
				{
					detail = "заголовок VHDX не читается, файл повреждён";
					return "fail";
				}
				// Файл VHDX всегда выровнен по размеру блока (1 МБ). Остаток
				// означает обрыв записи: именно так выглядит недокопированная копия.
				// Футер «foot» у экспортированных Hyper-V файлов может отсутствовать,
				// поэтому на него полагаться нельзя — а выравнивание работает всегда.
				if (size % (1024 * 1024) != 0)
				{
					detail = "размер не выровнен по 1 МБ (" + size + " байт) — копия обрезана";
					return "fail";
				}
				detail = "VHDX, " + Fmt.Size(size);
				return "ok";
			}
			if (ext == ".vhd")
			{
				if (size < 512)
				{
					detail = "слишком мал для VHD: " + Fmt.Size(size);
					return "fail";
				}
				if (size % 512 != 0)
				{
					detail = "размер не кратен 512 байтам — копия обрезана";
					return "fail";
				}
				// У VHD подпись в футере — последние 512 байт.
				fs.Seek(-512, SeekOrigin.End);
				byte[] foot = new byte[8];
				fs.Read(foot, 0, 8);
				if (System.Text.Encoding.ASCII.GetString(foot) != "conectix")
				{
					detail = "нет подписи conectix в футере — файл повреждён или обрезан";
					return "fail";
				}
				detail = "VHD, " + Fmt.Size(size);
				return "ok";
			}
			// .avhdx — файл разностного диска, проверяем как VHDX.
			if (ext == ".avhdx")
			{
				byte[] sig = new byte[8];
				fs.Read(sig, 0, 8);
				if (System.Text.Encoding.ASCII.GetString(sig) != "vhdxfile")
				{
					detail = "нет подписи vhdxfile";
					return "fail";
				}
				detail = "разностный VHDX, " + Fmt.Size(size);
				return "warn";
			}
			detail = "неизвестный тип: " + Fmt.Size(size);
			return "warn";
		}
		catch (Exception ex)
		{
			detail = "не удалось прочитать: " + ex.Message;
			return "fail";
		}
	}

	/// <summary>Проверяет все копии в каталоге бэкапов.</summary>
	public static List<Result> VerifyAll(string root)
	{
		List<Result> list = new List<Result>();
		foreach (BackupCopy copy in BackupRetention.Scan(root))
		{
			list.Add(Verify(copy.Path, copy.SizeBytes));
		}
		return list;
	}

	private static void Add(Result r, string name, string status, string detail)
	{
		Dictionary<string, object> c = Json.Obj();
		c["name"] = name;
		c["status"] = status;
		c["detail"] = detail ?? "";
		r.Checks.Add(c);
	}

	/// <summary>Заменяет непечатные байты, чтобы сообщение можно было показать.</summary>
	private static string Printable(string s)
	{
		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		foreach (char ch in s)
		{
			sb.Append((ch >= 32 && ch < 127) ? ch : '.');
		}
		return sb.ToString();
	}
}
