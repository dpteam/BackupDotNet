using System;
using System.Collections.Generic;
using System.IO;

namespace BackupDotNet.Core.Inventory;

/// <summary>
/// Шаблоны конфигурации ВМ: сохранённые наборы параметров в
/// <c>&lt;dataDir&gt;\vm-templates\*.json</c>.
///
/// Шаблон — это не копия машины, а «заготовка мастера»: имя и размер диска
/// при создании всё равно задаются заново, иначе двадцать машин получили бы
/// одно имя.
/// </summary>
public static class VmTemplates
{
	private static readonly object Sync = new object();

	/// <summary>Имя шаблона — это имя файла, поэтому проверяется строго.</summary>
	public static string ValidateName(string name)
	{
		string n = (name ?? "").Trim();
		if (n.Length == 0)
		{
			return "не указано имя шаблона";
		}
		if (n.Length > 60)
		{
			return "имя шаблона длиннее 60 символов";
		}
		if (n.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
		{
			return "в имени шаблона есть недопустимые символы";
		}
		return "";
	}

	public static string Directory(string dataDir)
	{
		return Path.Combine(dataDir, "vm-templates");
	}

	/// <summary>Список шаблонов, свежие сверху.</summary>
	public static List<Dictionary<string, object>> List(string dataDir)
	{
		List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
		string dir = Directory(dataDir);
		if (!System.IO.Directory.Exists(dir))
		{
			return list;
		}

		try
		{
			foreach (string file in System.IO.Directory.GetFiles(dir, "*.json"))
			{
				try
				{
					string text = File.ReadAllText(file);
					Dictionary<string, object> obj = Json.ParseObject(text);
					if (obj.Count == 0)
					{
						continue;
					}
					obj["name"] = Path.GetFileNameWithoutExtension(file);
					obj["file"] = Path.GetFileName(file);
					try
					{
						obj["modified"] = File.GetLastWriteTime(file);
					}
					catch
					{
					}
					list.Add(obj);
				}
				catch (Exception ex)
				{
					Log.Warn("Шаблон ВМ " + Path.GetFileName(file) + ": " + ex.Message);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Список шаблонов ВМ: " + ex.Message);
		}

		list.Sort((a, b) =>
		{
			string x = Json.GetStr(a, "name", "");
			string y = Json.GetStr(b, "name", "");
			return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
		});
		return list;
	}

	/// <summary>Читает шаблон по имени. Пустой словарь — не найден.</summary>
	public static Dictionary<string, object> Get(string dataDir, string name)
	{
		string err = ValidateName(name);
		if (err.Length > 0)
		{
			return Json.Obj();
		}
		string file = Path.Combine(Directory(dataDir), name.Trim() + ".json");
		if (!File.Exists(file))
		{
			return Json.Obj();
		}
		try
		{
			Dictionary<string, object> obj = Json.ParseObject(File.ReadAllText(file));
			obj["name"] = name.Trim();
			return obj;
		}
		catch (Exception ex)
		{
			Log.Warn("Шаблон ВМ " + name + ": " + ex.Message);
			return Json.Obj();
		}
	}

	/// <summary>Сохраняет шаблон. Возвращает текст ошибки или пустую строку.</summary>
	public static string Save(string dataDir, string name, Dictionary<string, object> body)
	{
		string err = ValidateName(name);
		if (err.Length > 0)
		{
			return err;
		}

		try
		{
			lock (Sync)
			{
				string dir = Directory(dataDir);
				if (!System.IO.Directory.Exists(dir))
				{
					System.IO.Directory.CreateDirectory(dir);
				}

				Dictionary<string, object> data = Json.Obj();
				// Имя в шаблоне не храним: оно станет именем файла и может
				// разойтись с содержимым.
				data["generation"] = Json.Long(Json.Get(body, "generation"), 2L);
				data["cpu"] = Json.Long(Json.Get(body, "cpu"), 2L);
				data["memoryMb"] = Json.Long(Json.Get(body, "memoryMb"), 2048L);
				data["dynamicMemory"] = Json.Bool(Json.Get(body, "dynamicMemory"), dflt: false);
				data["memoryMinMb"] = Json.Long(Json.Get(body, "memoryMinMb"), 0L);
				data["memoryMaxMb"] = Json.Long(Json.Get(body, "memoryMaxMb"), 0L);
				data["diskGb"] = Json.Long(Json.Get(body, "diskGb"), 40L);
				data["switchName"] = Json.GetStr(body, "switchName", "");
				data["autoStart"] = Json.Bool(Json.Get(body, "autoStart"), dflt: true);
				data["notes"] = Json.GetStr(body, "notes", "");
				data["savedAt"] = DateTime.Now;

				string file = Path.Combine(dir, name.Trim() + ".json");
				File.WriteAllText(file, Json.WritePretty(data));
				Log.Info("Шаблон ВМ сохранён: " + name.Trim());
			}
			return "";
		}
		catch (Exception ex)
		{
			return ex.Message;
		}
	}

	/// <summary>Удаляет шаблон.</summary>
	public static string Delete(string dataDir, string name)
	{
		string err = ValidateName(name);
		if (err.Length > 0)
		{
			return err;
		}
		try
		{
			string file = Path.Combine(Directory(dataDir), name.Trim() + ".json");
			if (!File.Exists(file))
			{
				return "шаблон не найден";
			}
			File.Delete(file);
			Log.Info("Шаблон ВМ удалён: " + name.Trim());
			return "";
		}
		catch (Exception ex)
		{
			return ex.Message;
		}
	}

	/// <summary>Заполняет спецификацию создания из шаблона.</summary>
	public static void Apply(Dictionary<string, object> template, VmCreator.Spec spec)
	{
		if (template == null || template.Count == 0 || spec == null)
		{
			return;
		}
		spec.Generation = (int)Json.Long(Json.Get(template, "generation"), spec.Generation);
		spec.Cpu = (int)Json.Long(Json.Get(template, "cpu"), spec.Cpu);
		spec.MemoryMb = (int)Json.Long(Json.Get(template, "memoryMb"), spec.MemoryMb);
		spec.DynamicMemory = Json.Bool(Json.Get(template, "dynamicMemory"), spec.DynamicMemory);
		spec.MemoryMinMb = (int)Json.Long(Json.Get(template, "memoryMinMb"), spec.MemoryMinMb);
		spec.MemoryMaxMb = (int)Json.Long(Json.Get(template, "memoryMaxMb"), spec.MemoryMaxMb);
		spec.DiskGb = (int)Json.Long(Json.Get(template, "diskGb"), spec.DiskGb);
		spec.SwitchName = Json.GetStr(template, "switchName", spec.SwitchName);
		spec.AutoStart = Json.Bool(Json.Get(template, "autoStart"), spec.AutoStart);
		spec.Notes = Json.GetStr(template, "notes", spec.Notes);
	}
}
