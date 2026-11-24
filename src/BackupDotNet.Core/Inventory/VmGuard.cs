using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Threading;

namespace BackupDotNet.Core.Inventory;

/// <summary>
/// Защита от разрушительных действий с виртуальными машинами.
///
/// Идея простая: одна кнопка «выключить» на проде — это не удобство, а авария.
/// Поэтому машины с «опасными» именами (по шаблону из настроек) нельзя гасить
/// и удалять без явного подтверждения, массовые операции ограничены по размеру,
/// а удаление заменено карантином: машина выключается и переименовывается,
/// настоящая очистка — отдельным шагом через несколько дней.
/// </summary>
public static class VmGuard
{
	/// <summary>Действия, которые может выполнять только администратор.</summary>
	public static bool IsAdminOnly(string action)
	{
		switch ((action ?? "").Trim().ToLowerInvariant())
		{
		case "delete":
		case "purge":
		case "quarantine":
		case "force-delete":
			return true;
		default:
			return false;
		}
	}

	/// <summary>Разрушительное ли действие: гасит машину или теряет данные.</summary>
	public static bool IsDestructive(string action)
	{
		switch ((action ?? "").Trim().ToLowerInvariant())
		{
		case "shutdown":
		case "stop":
		case "reset":
		case "reboot":
		case "save":
		case "delete":
		case "purge":
		case "quarantine":
			return true;
		default:
			return false;
		}
	}

	/// <summary>Совпадает ли имя с защищённым шаблоном.</summary>
	public static bool IsProtected(Settings settings, string vmName)
	{
		if (settings == null || vmName == null)
		{
			return false;
		}
		foreach (string pattern in settings.VmProtectedPatterns ?? new List<string>())
		{
			if (!string.IsNullOrWhiteSpace(pattern) && GlobMatch(pattern.Trim(), vmName))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>Шаблоны, которым соответствует имя (для сообщения человеку).</summary>
	public static string MatchedPattern(Settings settings, string vmName)
	{
		if (settings == null || vmName == null)
		{
			return "";
		}
		foreach (string pattern in settings.VmProtectedPatterns ?? new List<string>())
		{
			if (!string.IsNullOrWhiteSpace(pattern) && GlobMatch(pattern.Trim(), vmName))
			{
				return pattern.Trim();
			}
		}
		return "";
	}

	/// <summary>
	/// Можно ли выполнить действие. Возвращает пустую строку, если можно,
	/// иначе — объяснение отказа.
	/// </summary>
	/// <param name="confirmed">
	/// Человек явно подтвердил действие над защищённой машиной (ввёл её имя).
	/// Это НЕ то же самое, что «жёсткое выключение»: одно про питание,
	/// другое про защиту. Смешивать нельзя — кнопка «жёстко выключить»
	/// иначе обходила бы защиту.
	/// </param>
	public static string Check(Settings settings, string vmName, string action, bool confirmed)
	{
		if (settings == null)
		{
			return "";
		}
		if (!IsDestructive(action))
		{
			return "";
		}

		string pattern = MatchedPattern(settings, vmName);
		if (pattern.Length > 0 && settings.VmRequireForceForProtected && !confirmed)
		{
			return "ВМ «" + vmName + "» защищена шаблоном «" + pattern + "». " +
				"Действие «" + ActionName(action) + "» требует явного подтверждения";
		}
		return "";
	}

	/// <summary>
	/// Проверка массовой операции: размер и защищённые машины.
	/// Возвращает пустую строку, если можно.
	/// </summary>
	public static string CheckBatch(Settings settings, List<string> names, string action, bool confirmed)
	{
		if (settings == null || names == null)
		{
			return "";
		}
		int max = settings.VmMaxBatchSize > 0 ? settings.VmMaxBatchSize : 5;
		if (names.Count > max)
		{
			return "за один раз можно обработать не больше " + max + " машин " +
				"(запрошено " + names.Count + "). Измените vm.maxBatchSize в настройках";
		}
		if (!IsDestructive(action) || confirmed || !settings.VmRequireForceForProtected)
		{
			return "";
		}

		List<string> blocked = new List<string>();
		foreach (string name in names)
		{
			string pattern = MatchedPattern(settings, name);
			if (pattern.Length > 0)
			{
				blocked.Add(name + " («" + pattern + "»)");
			}
		}
		if (blocked.Count > 0)
		{
			return "среди выбранных есть защищённые машины: " + string.Join(", ", blocked.ToArray()) +
				". Действие требует явного подтверждения";
		}
		return "";
	}

	/// <summary>Человеческое название действия.</summary>
	public static string ActionName(string action)
	{
		switch ((action ?? "").Trim().ToLowerInvariant())
		{
		case "start":
			return "запуск";
		case "shutdown":
			return "штатное выключение";
		case "stop":
			return "жёсткое выключение";
		case "reboot":
			return "перезагрузка";
		case "reset":
			return "сброс питания";
		case "pause":
			return "пауза";
		case "resume":
			return "возобновление";
		case "save":
			return "сохранение состояния";
		case "delete":
			return "удаление";
		case "purge":
			return "окончательное удаление";
		case "quarantine":
			return "перемещение в карантин";
		case "snapshot.create":
			return "создание снимка";
		case "snapshot.apply":
			return "откат к снимку";
		case "snapshot.delete":
			return "удаление снимка";
		default:
			return action ?? "";
		}
	}

	/// <summary>
	/// Сравнение с шаблоном: <c>*</c> — любое число символов, <c>?</c> — один.
	/// Регистр не важен: имена ВМ в Hyper-V тоже регистронезависимы.
	/// </summary>
	public static bool GlobMatch(string pattern, string text)
	{
		if (string.IsNullOrEmpty(pattern))
		{
			return false;
		}
		return GlobMatchAt(pattern, 0, text ?? "", 0);
	}

	private static bool GlobMatchAt(string p, int pi, string t, int ti)
	{
		while (pi < p.Length)
		{
			char c = p[pi];
			if (c == '*')
			{
				// Схлопываем подряд идущие звёздочки, иначе «**» даёт лишние ветвления.
				while (pi < p.Length && p[pi] == '*')
				{
					pi++;
				}
				if (pi == p.Length)
				{
					return true;
				}
				for (int i = ti; i <= t.Length; i++)
				{
					if (GlobMatchAt(p, pi, t, i))
					{
						return true;
					}
				}
				return false;
			}
			if (ti >= t.Length)
			{
				return false;
			}
			if (c != '?' && char.ToLowerInvariant(c) != char.ToLowerInvariant(t[ti]))
			{
				return false;
			}
			pi++;
			ti++;
		}
		return ti == t.Length;
	}
}

/// <summary>
/// Карантин вместо удаления.
///
/// Удалённую ВМ не вернуть, а «ой, не ту» случается. Поэтому удаление идёт
/// в два шага: машина выключается и переименовывается в
/// <c>QUARANTINE_&lt;имя&gt;_&lt;дата&gt;</c>, а через <c>vm.quarantineDays</c> дней
/// её можно вычистить — уже осознанно, из списка карантина.
/// </summary>
public static class VmQuarantine
{
	private const string Prefix = "QUARANTINE_";

	/// <summary>Запись о машине в карантине.</summary>
	public class Item
	{
		public string OriginalName = "";

		public string QuarantineName = "";

		public string VmId = "";

		public DateTime Since;

		public List<string> Disks = new List<string>();

		public string By = "";

		public int DaysLeft;

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["originalName"] = OriginalName;
			d["quarantineName"] = QuarantineName;
			d["vmId"] = VmId;
			d["since"] = Since;
			d["disks"] = Disks;
			d["by"] = By;
			d["daysLeft"] = DaysLeft;
			return d;
		}
	}

	private static string Dir(Settings settings)
	{
		return Path.Combine(settings.DataDir, "vm-quarantine");
	}

	/// <summary>
	/// Перемещает машину в карантин: выключает и переименовывает.
	/// Файлы дисков остаются на месте — их удалит <see cref="Purge"/>.
	/// </summary>
	public static VmControl.Result Move(Settings settings, string vmName, string user, bool confirmed)
	{
		VmControl.Result r = new VmControl.Result();
		r.Destructive = true;

		if (string.IsNullOrWhiteSpace(vmName))
		{
			r.Message = "не указано имя машины";
			return r;
		}
		string name = vmName.Trim();

		// Проверка защиты — здесь, а не в вызывающем коде: иначе новый
		// обработчик однажды забудет её вызвать, и защищённая машина уедет
		// в карантин (проверено на практике).
		string blocked = VmGuard.Check(settings, name, "quarantine", confirmed);
		if (blocked.Length > 0)
		{
			r.Message = blocked;
			r.Protected = true;
			return r;
		}

		// Уже в карантине — второй раз не переименовываем.
		if (name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
		{
			r.Message = "машина уже в карантине: " + name;
			return r;
		}

		string vmPath = FindVmPath(name);
		if (vmPath.Length == 0)
		{
			r.Message = "виртуальная машина не найдена: " + name;
			return r;
		}

		try
		{
			string id = "";
			using (ManagementObject vm = new ManagementObject(vmPath))
			{
				vm.Get();
				id = Hw.Prop(vm, "Name");
				int state = Hw.PropInt(vm, "EnabledState", -1);

				// Выключаем до переименования: у работающей машины имя меняется,
				// но её гостевая ОС об этом не знает.
				if (state != 3 && state != 4)
				{
					VmControl.Result stop = VmControl.SetPower(name, "stop", true);
					if (!stop.Ok)
					{
						r.Message = "не удалось выключить перед карантином: " + stop.Message;
						return r;
					}
				}
			}

			List<string> disks = DiskPaths(name);
			string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
			string newName = Prefix + SafePart(name) + "_" + stamp;

			string renameError = RenameVm(id, newName);
			if (renameError.Length > 0)
			{
				r.Message = "не удалось переименовать: " + renameError;
				return r;
			}

			Item item = new Item();
			item.OriginalName = name;
			item.QuarantineName = newName;
			item.VmId = id;
			item.Since = DateTime.Now;
			item.Disks = disks;
			item.By = user ?? "";
			Write(settings, item);

			VmInventory.Invalidate();
			r.Ok = true;
			r.Message = "машина «" + name + "» в карантине как «" + newName + "». " +
				"Окончательное удаление — через " + Days(settings) + " дн.";
			Log.Warn("ВМ " + name + ": перемещена в карантин как " + newName + " (" + (user ?? "?") + ")");
			return r;
		}
		catch (Exception ex)
		{
			r.Message = "карантин не выполнен: " + ex.Message;
			Log.Warn("ВМ " + name + ", карантин: " + ex.Message);
			return r;
		}
	}

	/// <summary>Список машин в карантине, свежие сверху.</summary>
	public static List<Item> List(Settings settings)
	{
		List<Item> list = new List<Item>();
		string dir = Dir(settings);
		if (!Directory.Exists(dir))
		{
			return list;
		}
		int days = Days(settings);
		try
		{
			foreach (string file in Directory.GetFiles(dir, "*.json"))
			{
				try
				{
					Dictionary<string, object> obj = Json.ParseObject(File.ReadAllText(file));
					Item item = new Item();
					item.OriginalName = Json.GetStr(obj, "originalName", "");
					item.QuarantineName = Json.GetStr(obj, "quarantineName", "");
					item.VmId = Json.GetStr(obj, "vmId", "");
					item.By = Json.GetStr(obj, "by", "");
					string since = Json.GetStr(obj, "since", "");
					if (DateTime.TryParse(since, out DateTime dt))
					{
						item.Since = dt;
					}
					foreach (object o in Json.AsArray(Json.Get(obj, "disks")))
					{
						string path = Json.Str(o, "");
						if (path.Length > 0)
						{
							item.Disks.Add(path);
						}
					}
					item.DaysLeft = Math.Max(0, days - (int)(DateTime.Now - item.Since).TotalDays);
					list.Add(item);
				}
				catch (Exception ex)
				{
					Log.Warn("Карантин ВМ " + Path.GetFileName(file) + ": " + ex.Message);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Список карантина ВМ: " + ex.Message);
		}
		list.Sort((a, b) => b.Since.CompareTo(a.Since));
		return list;
	}

	/// <summary>Возвращает машину из карантина под прежним именем.</summary>
	public static VmControl.Result Restore(Settings settings, string quarantineName)
	{
		VmControl.Result r = new VmControl.Result();
		string name = (quarantineName ?? "").Trim();
		if (name.Length == 0)
		{
			r.Message = "не указано имя машины в карантине";
			return r;
		}

		Item item = Find(settings, name);
		if (item == null)
		{
			r.Message = "запись о карантине не найдена: " + name;
			return r;
		}

		// Прежнее имя могло быть занято новой машиной — тогда возврат невозможен.
		if (FindVmPath(item.OriginalName).Length > 0)
		{
			r.Message = "имя «" + item.OriginalName + "» уже занято другой машиной";
			return r;
		}

		string error = RenameVm(item.VmId, item.OriginalName);
		if (error.Length > 0)
		{
			r.Message = "не удалось вернуть: " + error;
			return r;
		}

		Delete(settings, item);
		VmInventory.Invalidate();
		r.Ok = true;
		r.Message = "машина возвращена как «" + item.OriginalName + "»";
		Log.Info("ВМ " + item.OriginalName + ": возвращена из карантина");
		return r;
	}

	/// <summary>
	/// Окончательно удаляет машину из карантина вместе с дисками.
	/// Отдельный шаг, не автоматический: файлы удаляются безвозвратно.
	/// </summary>
	public static VmControl.Result Purge(Settings settings, string quarantineName, bool confirmed)
	{
		VmControl.Result r = new VmControl.Result();
		r.Destructive = true;
		string name = (quarantineName ?? "").Trim();
		Item item = Find(settings, name);
		if (item == null)
		{
			r.Message = "запись о карантине не найдена: " + name;
			return r;
		}

		// Проверяем по исходному имени: переименование в карантин не должно
		// становиться лазейкой для удаления защищённой машины.
		string blocked = VmGuard.Check(settings, item.OriginalName, "purge", confirmed);
		if (blocked.Length > 0)
		{
			r.Message = blocked;
			r.Protected = true;
			return r;
		}

		string error = DestroyVm(item.VmId);
		if (error.Length > 0)
		{
			r.Message = "машина не удалена: " + error;
			return r;
		}

		int deletedDisks = 0;
		long freed = 0;
		foreach (string disk in item.Disks)
		{
			try
			{
				if (File.Exists(disk))
				{
					long size = new FileInfo(disk).Length;
					File.Delete(disk);
					deletedDisks++;
					freed += size;
				}
			}
			catch (Exception ex)
			{
				Log.Warn("Карантин ВМ " + name + ", диск " + disk + ": " + ex.Message);
			}
		}

		Delete(settings, item);
		VmInventory.Invalidate();
		r.Ok = true;
		r.Message = "удалено безвозвратно: «" + item.OriginalName + "», дисков " + deletedDisks +
			(freed > 0 ? ", освобождено " + Hw.FormatBytes(freed) : "");
		Log.Warn("ВМ " + item.OriginalName + ": удалена из карантина, дисков " + deletedDisks);
		return r;
	}

	/// <summary>Сколько машин в карантине просрочено (для подсказки в интерфейсе).</summary>
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

	// ------------------------------------------------------------------ файлы

	private static int Days(Settings settings)
	{
		return settings != null && settings.VmQuarantineDays > 0 ? settings.VmQuarantineDays : 7;
	}

	private static Item Find(Settings settings, string quarantineName)
	{
		foreach (Item item in List(settings))
		{
			if (string.Equals(item.QuarantineName, quarantineName, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(item.OriginalName, quarantineName, StringComparison.OrdinalIgnoreCase))
			{
				return item;
			}
		}
		return null;
	}

	private static void Write(Settings settings, Item item)
	{
		string dir = Dir(settings);
		Directory.CreateDirectory(dir);
		Dictionary<string, object> obj = Json.Obj();
		obj["originalName"] = item.OriginalName;
		obj["quarantineName"] = item.QuarantineName;
		obj["vmId"] = item.VmId;
		obj["since"] = item.Since;
		obj["disks"] = item.Disks;
		obj["by"] = item.By;
		File.WriteAllText(Path.Combine(dir, item.QuarantineName + ".json"), Json.WritePretty(obj));
	}

	private static void Delete(Settings settings, Item item)
	{
		try
		{
			string file = Path.Combine(Dir(settings), item.QuarantineName + ".json");
			if (File.Exists(file))
			{
				File.Delete(file);
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Карантин ВМ, запись " + item.QuarantineName + ": " + ex.Message);
		}
	}

	// ------------------------------------------------------------------ WMI

	private static string FindVmPath(string name)
	{
		ManagementObjectCollection coll = Hw.Query("root\\virtualization\\v2",
			"SELECT * FROM Msvm_ComputerSystem");
		if (coll == null)
		{
			return "";
		}
		try
		{
			foreach (ManagementBaseObject o in coll)
			{
				using (o)
				{
					if (string.Equals(Hw.Prop(o, "ElementName"), name, StringComparison.OrdinalIgnoreCase) &&
						o is ManagementObject mo)
					{
						return mo.Path.Path;
					}
				}
			}
		}
		finally
		{
			try
			{
				coll.Dispose();
			}
			catch
			{
			}
		}
		return "";
	}

	/// <summary>Пути дисков машины — их надо помнить, чтобы потом удалить.</summary>
	private static List<string> DiskPaths(string vmName)
	{
		List<string> list = new List<string>();
		foreach (Dictionary<string, object> vm in VmInventory.Vms(out string error, force: true))
		{
			if (!string.Equals(Json.GetStr(vm, "name", ""), vmName, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			// Диски в инвентаре — массив объектов {path, fileName, sizeBytes, type},
			// а не строк. Взять Json.Str от объекта значит получить имя типа.
			foreach (object o in Json.AsArray(Json.Get(vm, "disks")))
			{
				Dictionary<string, object> disk = Json.AsObject(o);
				string path = Json.GetStr(disk, "path", "");
				if (path.Length > 0)
				{
					list.Add(path);
				}
			}
			break;
		}
		return list;
	}

	private static string RenameVm(string vmId, string newName)
	{
		try
		{
			ManagementObjectCollection coll = Hw.Query("root\\virtualization\\v2",
				"SELECT * FROM Msvm_VirtualSystemSettingData WHERE InstanceID='Microsoft:" + vmId +
				"' AND VirtualSystemType='Microsoft:Hyper-V:System:Realized'");
			if (coll == null)
			{
				return "конфигурация машины не читается";
			}
			string xml = null;
			try
			{
				foreach (ManagementBaseObject o in coll)
				{
					using (o)
					{
						if (o is ManagementObject mo)
						{
							mo["ElementName"] = newName;
							xml = mo.GetText(TextFormat.WmiDtd20);
							break;
						}
					}
				}
			}
			finally
			{
				try
				{
					coll.Dispose();
				}
				catch
				{
				}
			}
			if (xml == null)
			{
				return "конфигурация машины не найдена";
			}

			ManagementObject svc = FindOne("SELECT * FROM Msvm_VirtualSystemManagementService");
			if (svc == null)
			{
				return "служба управления Hyper-V недоступна";
			}
			try
			{
				ManagementBaseObject inParams = svc.GetMethodParameters("ModifySystemSettings");
				inParams["SystemSettings"] = xml;
				ManagementBaseObject outParams = svc.InvokeMethod("ModifySystemSettings", inParams, null);
				uint code = Convert.ToUInt32(outParams["ReturnValue"]);
				if (code != 0u && code != 4096u)
				{
					return "Hyper-V вернул код " + code;
				}
				string job = Convert.ToString(outParams["Job"]) ?? "";
				return WaitJob(job, 120);
			}
			finally
			{
				svc.Dispose();
			}
		}
		catch (Exception ex)
		{
			return ex.Message;
		}
	}

	private static string DestroyVm(string vmId)
	{
		try
		{
			string path = "";
			ManagementObjectCollection coll = Hw.Query("root\\virtualization\\v2",
				"SELECT * FROM Msvm_ComputerSystem WHERE Name='" + vmId + "'");
			if (coll != null)
			{
				try
				{
					foreach (ManagementBaseObject o in coll)
					{
						if (o is ManagementObject mo)
						{
							path = mo.Path.Path;
							break;
						}
					}
				}
				finally
				{
					try
					{
						coll.Dispose();
					}
					catch
					{
					}
				}
			}
			if (path.Length == 0)
			{
				return "машина не найдена";
			}

			ManagementObject svc = FindOne("SELECT * FROM Msvm_VirtualSystemManagementService");
			if (svc == null)
			{
				return "служба управления Hyper-V недоступна";
			}
			try
			{
				ManagementBaseObject inParams = svc.GetMethodParameters("DestroySystem");
				inParams["AffectedSystem"] = path;
				ManagementBaseObject outParams = svc.InvokeMethod("DestroySystem", inParams, null);
				uint code = Convert.ToUInt32(outParams["ReturnValue"]);
				if (code != 0u && code != 4096u)
				{
					return "Hyper-V вернул код " + code;
				}
				string job = Convert.ToString(outParams["Job"]) ?? "";
				return WaitJob(job, 180);
			}
			finally
			{
				svc.Dispose();
			}
		}
		catch (Exception ex)
		{
			return ex.Message;
		}
	}

	private static ManagementObject FindOne(string wql)
	{
		ManagementObjectCollection coll = Hw.Query("root\\virtualization\\v2", wql);
		if (coll == null)
		{
			return null;
		}
		try
		{
			foreach (ManagementBaseObject o in coll)
			{
				if (o is ManagementObject mo)
				{
					return mo;
				}
			}
		}
		catch
		{
		}
		try
		{
			coll.Dispose();
		}
		catch
		{
		}
		return null;
	}

	private static string WaitJob(string jobPath, int timeoutSeconds)
	{
		if (string.IsNullOrEmpty(jobPath))
		{
			return "";
		}
		DateTime deadline = DateTime.Now.AddSeconds(Math.Max(5, timeoutSeconds));
		while (DateTime.Now < deadline)
		{
			try
			{
				using ManagementObject job = new ManagementObject(new ManagementPath(jobPath));
				job.Get();
				int state = Hw.PropInt(job, "JobState", -1);
				if (state == 2 || state == 7)
				{
					return "";
				}
				if (state == 8 || state == 9 || state == 10)
				{
					string err = Hw.Prop(job, "ErrorDescription");
					return err.Length > 0 ? err : ("задание завершилось с состоянием " + state);
				}
			}
			catch
			{
				return "";
			}
			Thread.Sleep(500);
		}
		return "превышено время ожидания (" + timeoutSeconds + " с)";
	}

	private static string SafePart(string name)
	{
		char[] bad = Path.GetInvalidFileNameChars();
		System.Text.StringBuilder sb = new System.Text.StringBuilder(name.Length);
		foreach (char c in name)
		{
			sb.Append(Array.IndexOf(bad, c) >= 0 || c == '\\' || c == '/' ? '_' : c);
		}
		string s = sb.ToString().Trim();
		return s.Length > 0 ? s : "vm";
	}
}
