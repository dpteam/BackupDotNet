using System;
using System.Collections.Generic;
using System.IO;

namespace BackupDotNet.Core.Inventory;

/// <summary>
/// Безопасное удаление файлов-сирот: не «удалить», а «убрать в отстойник».
///
/// Сирота — это VHDX, не привязанный ни к одной ВМ. Но «не привязан» бывает
/// по разным причинам: машину удалили (тогда файл мусор), либо конфигурация
/// лежит на отключённом диске, либо ВМ на паузе в переносе. Разница в 33 ГБ
/// не стоит риска, поэтому файл сначала переезжает в
/// <c>&lt;dataDir&gt;\orphan-trash</c> и только через N дней удаляется.
///
/// Отдельная защита: файл, который числится в базовой цепочке хоть одного
/// снимка, в отстойник не попадает никогда — иначе сломается откат.
/// </summary>
public static class OrphanTrash
{
	private const string TrashDirName = "orphan-trash";

	/// <summary>Запись об убранном файле.</summary>
	public class Item
	{
		public string FileName = "";

		public string OriginalPath = "";

		public long SizeBytes;

		public DateTime Since;

		public string By = "";

		public int DaysLeft;

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["fileName"] = FileName;
			d["originalPath"] = OriginalPath;
			d["sizeBytes"] = SizeBytes;
			d["since"] = Since;
			d["by"] = By;
			d["daysLeft"] = DaysLeft;
			return d;
		}
	}

	public static string Dir(Settings settings)
	{
		return Path.Combine(settings.DataDir, TrashDirName);
	}

	private static string MetaDir(Settings settings)
	{
		return Path.Combine(settings.DataDir, "orphan-trash-meta");
	}

	/// <summary>
	/// Переносит файл в отстойник. Не удаляет: файл можно вернуть.
	/// </summary>
	/// <param name="path">Полный путь к файлу-сироте.</param>
	public static VmControl.Result Move(Settings settings, string path, string user, bool confirmed)
	{
		VmControl.Result r = new VmControl.Result();
		r.Destructive = true;

		string full = (path ?? "").Trim();
		if (full.Length == 0)
		{
			r.Message = "не указан файл";
			return r;
		}

		try
		{
			full = Path.GetFullPath(full);
		}
		catch (Exception ex)
		{
			r.Message = "некорректный путь: " + ex.Message;
			return r;
		}

		if (!File.Exists(full))
		{
			r.Message = "файл не найден: " + full;
			return r;
		}

		// Только образы дисков: остальное к сиротам не относится.
		string ext = Path.GetExtension(full);
		if (!ext.Equals(".vhdx", StringComparison.OrdinalIgnoreCase) &&
			!ext.Equals(".vhd", StringComparison.OrdinalIgnoreCase))
		{
			r.Message = "это не образ диска: " + ext;
			return r;
		}

		// Файл, на который ссылается хоть одна ВМ или цепочка снимков,
		// трогать нельзя: это не сирота, а база.
		string usedBy = UsedByAnyVm(full);
		if (usedBy.Length > 0)
		{
			r.Message = "файл используется: " + usedBy;
			r.Protected = true;
			return r;
		}

		// Защита по имени: тот же механизм, что и для ВМ.
		string blocked = VmGuard.Check(settings, Path.GetFileNameWithoutExtension(full), "delete", confirmed);
		if (blocked.Length > 0)
		{
			r.Message = blocked;
			r.Protected = true;
			return r;
		}

		try
		{
			string trash = Dir(settings);
			Directory.CreateDirectory(trash);
			Directory.CreateDirectory(MetaDir(settings));

			long size = new FileInfo(full).Length;
			string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
			string baseName = Path.GetFileNameWithoutExtension(full) + "_" + stamp;
			string target = Path.Combine(trash, baseName + ext);
			// Два файла с одинаковым именем в одну секунду дали бы одно имя
			// и File.Move упал бы на существующем файле.
			int n = 2;
			while (File.Exists(target))
			{
				target = Path.Combine(trash, baseName + "_" + n + ext);
				n++;
			}

			File.Move(full, target);

			Item item = new Item();
			item.FileName = Path.GetFileName(target);
			item.OriginalPath = full;
			item.SizeBytes = size;
			item.Since = DateTime.Now;
			item.By = user ?? "";
			WriteMeta(settings, item);

			r.Ok = true;
			r.Message = "файл убран в отстойник: " + Path.GetFileName(target) +
				" (" + Hw.FormatBytes(size) + "). Удаление — через " + Days(settings) + " дн.";
			Log.Warn("Сирота " + full + ": убрана в отстойник (" + (user ?? "?") + ")");
			return r;
		}
		catch (Exception ex)
		{
			r.Message = "не удалось убрать файл: " + ex.Message;
			Log.Warn("Сирота " + full + ", отстойник: " + ex.Message);
			return r;
		}
	}

	/// <summary>Что лежит в отстойнике.</summary>
	public static List<Item> List(Settings settings)
	{
		List<Item> list = new List<Item>();
		string meta = MetaDir(settings);
		if (!System.IO.Directory.Exists(meta))
		{
			return list;
		}
		int days = Days(settings);
		try
		{
			foreach (string file in System.IO.Directory.GetFiles(meta, "*.json"))
			{
				try
				{
					Dictionary<string, object> obj = Json.ParseObject(File.ReadAllText(file));
					Item item = new Item();
					item.FileName = Json.GetStr(obj, "fileName", "");
					item.OriginalPath = Json.GetStr(obj, "originalPath", "");
					item.SizeBytes = Json.Long(Json.Get(obj, "sizeBytes"), 0L);
					item.By = Json.GetStr(obj, "by", "");
					if (DateTime.TryParse(Json.GetStr(obj, "since", ""), out DateTime since))
					{
						item.Since = since;
					}
					item.DaysLeft = Math.Max(0, days - (int)(DateTime.Now - item.Since).TotalDays);
					list.Add(item);
				}
				catch (Exception ex)
				{
					Log.Warn("Отстойник, запись " + Path.GetFileName(file) + ": " + ex.Message);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Список отстойника: " + ex.Message);
		}
		list.Sort((a, b) => b.Since.CompareTo(a.Since));
		return list;
	}

	/// <summary>Возвращает файл на прежнее место.</summary>
	public static VmControl.Result Restore(Settings settings, string fileName)
	{
		VmControl.Result r = new VmControl.Result();
		Item item = Find(settings, fileName);
		if (item == null)
		{
			r.Message = "запись не найдена: " + fileName;
			return r;
		}

		try
		{
			string source = Path.Combine(Dir(settings), item.FileName);
			if (!File.Exists(source))
			{
				r.Message = "файл в отстойнике уже отсутствует";
				return r;
			}
			if (File.Exists(item.OriginalPath))
			{
				r.Message = "прежнее место занято: " + item.OriginalPath;
				return r;
			}
			string dir = Path.GetDirectoryName(item.OriginalPath);
			if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
			{
				System.IO.Directory.CreateDirectory(dir);
			}
			File.Move(source, item.OriginalPath);
			DeleteMeta(settings, item);

			r.Ok = true;
			r.Message = "файл возвращён: " + item.OriginalPath;
			Log.Info("Отстойник: файл возвращён — " + item.OriginalPath);
			return r;
		}
		catch (Exception ex)
		{
			r.Message = "не удалось вернуть файл: " + ex.Message;
			return r;
		}
	}

	/// <summary>Удаляет файл из отстойника безвозвратно.</summary>
	public static VmControl.Result Purge(Settings settings, string fileName, bool confirmed)
	{
		VmControl.Result r = new VmControl.Result();
		r.Destructive = true;
		Item item = Find(settings, fileName);
		if (item == null)
		{
			r.Message = "запись не найдена: " + fileName;
			return r;
		}

		string blocked = VmGuard.Check(settings, Path.GetFileNameWithoutExtension(item.OriginalPath), "purge", confirmed);
		if (blocked.Length > 0)
		{
			r.Message = blocked;
			r.Protected = true;
			return r;
		}

		try
		{
			string file = Path.Combine(Dir(settings), item.FileName);
			long freed = 0L;
			if (File.Exists(file))
			{
				freed = new FileInfo(file).Length;
				File.Delete(file);
			}
			DeleteMeta(settings, item);

			r.Ok = true;
			r.Message = "удалено безвозвратно: " + item.FileName +
				(freed > 0 ? ", освобождено " + Hw.FormatBytes(freed) : "");
			Log.Warn("Отстойник: файл удалён — " + item.OriginalPath);
			return r;
		}
		catch (Exception ex)
		{
			r.Message = "не удалось удалить: " + ex.Message;
			return r;
		}
	}

	/// <summary>Сколько записей просрочено.</summary>
	public static int ExpiredCount(Settings settings)
	{
		int n = 0;
		foreach (Item item in List(settings))
		{
			if (item.DaysLeft <= 0)
			{
				n++;
			}
		}
		return n;
	}

	// ------------------------------------------------------------------ проверки

	/// <summary>
	/// Ссылается ли на файл хоть одна ВМ или цепочка её снимков.
	/// Возвращает имя машины или пустую строку.
	/// </summary>
	private static string UsedByAnyVm(string path)
	{
		try
		{
			foreach (Dictionary<string, object> vm in VmInventory.Vms(out string error, force: false))
			{
				string name = Json.GetStr(vm, "name", "");
				foreach (object o in Json.AsArray(Json.Get(vm, "disks")))
				{
					Dictionary<string, object> disk = Json.AsObject(o);
					string diskPath = Json.GetStr(disk, "path", "");
					if (diskPath.Length > 0 &&
						string.Equals(Path.GetFullPath(diskPath), path, StringComparison.OrdinalIgnoreCase))
					{
						return "диск ВМ «" + name + "»";
					}
				}
			}
		}
		catch (Exception ex)
		{
			// Не смогли проверить — значит нельзя трогать.
			return "не удалось проверить привязку (" + ex.Message + ")";
		}
		return "";
	}

	// ------------------------------------------------------------------ файлы

	private static int Days(Settings settings)
	{
		return settings != null && settings.VmQuarantineDays > 0 ? settings.VmQuarantineDays : 7;
	}

	private static Item Find(Settings settings, string fileName)
	{
		foreach (Item item in List(settings))
		{
			if (string.Equals(item.FileName, fileName, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(Path.GetFileName(item.OriginalPath), fileName, StringComparison.OrdinalIgnoreCase))
			{
				return item;
			}
		}
		return null;
	}

	private static void WriteMeta(Settings settings, Item item)
	{
		Dictionary<string, object> obj = Json.Obj();
		obj["fileName"] = item.FileName;
		obj["originalPath"] = item.OriginalPath;
		obj["sizeBytes"] = item.SizeBytes;
		obj["since"] = item.Since;
		obj["by"] = item.By;
		File.WriteAllText(Path.Combine(MetaDir(settings), item.FileName + ".json"), Json.WritePretty(obj));
	}

	private static void DeleteMeta(Settings settings, Item item)
	{
		try
		{
			string file = Path.Combine(MetaDir(settings), item.FileName + ".json");
			if (File.Exists(file))
			{
				File.Delete(file);
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Отстойник, метаданные " + item.FileName + ": " + ex.Message);
		}
	}
}
