using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management;
using System.Threading;
using BackupDotNet.Core.Backup;

namespace BackupDotNet.Core.Inventory;

/// <summary>
/// Управление виртуальными машинами Hyper-V: питание и снимки.
///
/// Всё делается через WMI <c>root\virtualization\v2</c>, без модуля Hyper-V
/// и без внешних библиотек. Каждая операция возвращает результат с текстом,
/// пригодным для показа человеку: «ВМ уже работает» полезнее, чем код 32775.
///
/// Опасные операции (жёсткое выключение, откат снимка) помечены отдельно —
/// интерфейс обязан спрашивать подтверждение, а не выполнять их по одному клику.
/// </summary>
public static class VmControl
{
	/// <summary>
	/// Коды запрашиваемого состояния Hyper-V.
	///
	/// Значения проверены на живой ВМ: пауза — это 9 (Quiesce), а не 32768;
	/// возобновление — обычный Enable (2), а не 32769. С «документными»
	/// 32768/32769 Hyper-V отвечает «недопустимое состояние».
	/// </summary>
	public static class PowerState
	{
		public const ushort Enable = 2;

		public const ushort Disable = 3;

		public const ushort ShutDown = 4;

		public const ushort Quiesce = 9;

		public const ushort Reboot = 10;

		public const ushort Reset = 11;

		public const ushort Save = 32773;
	}

	/// <summary>Результат операции.</summary>
	public class Result
	{
		public bool Ok;

		public string Message = "";

		public string Detail = "";

		/// <summary>Операция опасная — интерфейс должен требовать подтверждение.</summary>
		public bool Destructive;

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["ok"] = Ok;
			d["message"] = Message;
			// Интерфейс показывает поле error при неуспешном HTTP-коде:
			// без него пользователь увидел бы только «HTTP 400».
			if (!Ok)
			{
				d["error"] = Message;
			}
			d["detail"] = Detail;
			d["destructive"] = Destructive;
			return d;
		}
	}

	/// <summary>Снимок виртуальной машины.</summary>
	public class Snapshot
	{
		public string Name = "";

		public string Id = "";

		public string Path = "";

		public DateTime? CreatedAt;

		public string VmName = "";

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["name"] = Name;
			d["id"] = Id;
			d["path"] = Path;
			d["createdAt"] = CreatedAt;
			d["vmName"] = VmName;
			if (CreatedAt.HasValue)
			{
				d["ageDays"] = Math.Round((DateTime.Now - CreatedAt.Value).TotalDays, 1);
			}
			return d;
		}
	}

	// ------------------------------------------------------------------ питание

	/// <summary>
	/// Меняет состояние ВМ.
	/// </summary>
	/// <param name="vmName">Имя виртуальной машины.</param>
	/// <param name="action">start | stop | shutdown | reboot | reset | pause | resume | save</param>
	/// <param name="force">Для stop: жёсткое выключение без ожидания.</param>
	public static Result SetPower(string vmName, string action, bool force)
	{
		Result r = new Result();
		string act = (action ?? "").Trim().ToLowerInvariant();

		ushort requested;
		string human;
		switch (act)
		{
		case "start":
			requested = PowerState.Enable;
			human = "запуск";
			break;
		case "shutdown":
			requested = PowerState.ShutDown;
			human = "штатное выключение";
			r.Destructive = true;
			break;
		case "stop":
			// «stop» без force — сначала пробуем штатно, чтобы не терять данные.
			requested = force ? PowerState.Disable : PowerState.ShutDown;
			human = force ? "жёсткое выключение" : "выключение";
			r.Destructive = true;
			break;
		case "reboot":
			requested = PowerState.Reboot;
			human = "перезагрузка";
			r.Destructive = true;
			break;
		case "reset":
			requested = PowerState.Reset;
			human = "сброс питания";
			r.Destructive = true;
			break;
		case "pause":
			requested = PowerState.Quiesce;
			human = "пауза";
			break;
		case "resume":
			requested = PowerState.Enable;
			human = "возобновление";
			break;
		case "save":
			requested = PowerState.Save;
			human = "сохранение состояния";
			r.Destructive = true;
			break;
		default:
			r.Message = "неизвестное действие: " + act;
			return r;
		}

		string vmPath = FindVmPath(vmName);
		if (vmPath.Length == 0)
		{
			r.Message = "виртуальная машина не найдена: " + vmName;
			return r;
		}

		try
		{
			using ManagementObject vm = new ManagementObject(vmPath);
			vm.Get();
			int current = Hw.PropInt(vm, "EnabledState", -1);

			// Не дёргаем ВМ зря: «старт уже работающей» — не ошибка, но и не успех.
			if (requested == PowerState.Enable && current == 2)
			{
				r.Ok = true;
				r.Message = "уже работает";
				return r;
			}
			if ((requested == PowerState.Disable || requested == PowerState.ShutDown) && current == 3)
			{
				r.Ok = true;
				r.Message = "уже выключена";
				return r;
			}
			if (requested == PowerState.Quiesce && current == 9)
			{
				r.Ok = true;
				r.Message = "уже на паузе";
				return r;
			}

			// Переходное состояние: Hyper-V откажет с «недопустимое состояние».
			// Лучше подождать, чем показывать человеку непонятный код.
			if (current == 32770 || current == 32773 || current == 32774 || current == 32777)
			{
				if (!WaitStable(vm, 30))
				{
					r.Message = human + " невозможно: виртуальная машина в переходном состоянии, " +
						"подождите завершения";
					return r;
				}
				current = Hw.PropInt(vm, "EnabledState", -1);
			}

			ManagementBaseObject inParams = vm.GetMethodParameters("RequestStateChange");
			inParams["RequestedState"] = requested;
			// TimeoutPeriod сознательно не задаём: свойство типа DateTime не
			// принимает значение ни строкой, ни DateTime — .NET Management API
			// отвечает «несоответствие типа». Метод работает и без него,
			// а ожидание асинхронного задания мы делаем сами в WaitJob.

			ManagementBaseObject outParams = vm.InvokeMethod("RequestStateChange", inParams, null);
			uint code = Convert.ToUInt32(outParams["ReturnValue"], CultureInfo.InvariantCulture);

			// 0 — выполнено, 4096 — запущено асинхронно, 32768/32769 — уже в нужном
			// состоянии, 32775 — переход уже идёт.
			if (code == 0u || code == 4096u)
			{
				r.Ok = true;
				r.Message = human + " запущено";
				string job = Convert.ToString(outParams["Job"], CultureInfo.InvariantCulture) ?? "";
				if (job.Length > 0)
				{
					string jobError = WaitJob(job, 120);
					if (jobError.Length > 0)
					{
						r.Ok = false;
						r.Message = human + ": " + ExplainText(jobError);
					}
					else
					{
						r.Message = human + " выполнено";
					}
				}

				// Дожидаемся фактического состояния: ответ «выполнено» при ВМ,
				// которая ещё в переходе, вводит в заблуждение.
				ushort target = TargetState(requested);
				if (r.Ok && target != 0 && WaitEnabledState(vmName, target, 60))
				{
					r.Message = human + " выполнено: " + HyperVExportEngine.StateName(target);
				}
				Log.Info("ВМ " + vmName + ": " + human + " (код " + code + ")");
				return r;
			}
			if (code == 32768u || code == 32769u)
			{
				r.Ok = true;
				r.Message = "уже в нужном состоянии";
				return r;
			}
			if (code == 32774u || code == 32775u)
			{
				// 32774 «система используется», 32775 «недопустимое состояние».
				// На практике это значит: операция невозможна, пока ВМ в текущем
				// состоянии. Для паузы — машина ещё не загрузилась; для старта —
				// уже идёт другой переход.
				r.Message = human + " невозможно: виртуальная машина в состоянии «" +
					HyperVExportEngine.StateName(current) + "»";
				return r;
			}
			r.Message = human + ": Hyper-V вернул код " + code + " (" + ExplainStateCode(code) + ")";
			return r;
		}
		catch (Exception ex)
		{
			r.Message = human + ": " + ExplainException(ex, human);
			Log.Warn("ВМ " + vmName + ", " + human + ": " + ex.Message);
			return r;
		}
	}

	/// <summary>
	/// Переводит исключение WMI в понятную фразу. Коды интеграции Hyper-V
	/// выглядят как «0x800710DF» и ничего не говорят человеку.
	/// </summary>
	private static string ExplainException(Exception ex, string human)
	{
		return ExplainText(ex.Message ?? "");
	}

	/// <summary>
	/// Переводит текст ошибки Hyper-V в понятную фразу. Используется и для
	/// исключений, и для описаний ошибок из WMI-заданий.
	/// </summary>
	private static string ExplainText(string msg)
	{
		if (string.IsNullOrEmpty(msg))
		{
			return "неизвестная ошибка";
		}
		if (msg.IndexOf("0x800710DF", StringComparison.OrdinalIgnoreCase) >= 0 ||
			msg.IndexOf("не готов", StringComparison.OrdinalIgnoreCase) >= 0 ||
			msg.IndexOf("not ready", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "гостевая ОС не отвечает — штатное завершение невозможно. " +
				"Используйте принудительное выключение.";
		}
		if (msg.IndexOf("0x80070522", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "нужны права администратора Hyper-V";
		}
		if (msg.IndexOf("0x80070005", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "доступ запрещён: нужны права администратора Hyper-V";
		}
		return msg;
	}

	// ------------------------------------------------------------------ снимки

	/// <summary>Список снимков ВМ, свежие сверху.</summary>
	public static List<Snapshot> ListSnapshots(string vmName)
	{
		List<Snapshot> list = new List<Snapshot>();
		string vmPath = FindVmPath(vmName);
		if (vmPath.Length == 0)
		{
			return list;
		}

		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			using ManagementObject vm = new ManagementObject(vmPath);
			vm.Get();
			foreach (ManagementObject snap in vm.GetRelated("Msvm_VirtualSystemSettingData"))
			{
				using (snap)
				{
					string type = Hw.Prop(snap, "VirtualSystemType");
					if (type.IndexOf("Snapshot", StringComparison.OrdinalIgnoreCase) < 0)
					{
						continue;
					}
					// GetRelated возвращает один и тот же снимок дважды: как
					// настройки снимка и как настройки его «родителя». Без
					// дедупликации в интерфейсе появляются двойники.
					string instanceId = Hw.Prop(snap, "InstanceID");
					if (instanceId.Length > 0 && !seen.Add(instanceId))
					{
						continue;
					}
					Snapshot s = new Snapshot();
					s.Name = Hw.Prop(snap, "ElementName");
					s.Id = instanceId;
					s.Path = snap.Path.Path;
					s.VmName = vmName;
					try
					{
						s.CreatedAt = Hw.ParseWmiDate(snap["CreationTime"]);
					}
					catch
					{
					}
					list.Add(s);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("ВМ " + vmName + ", список снимков: " + ex.Message);
		}

		list.Sort((a, b) =>
		{
			DateTime x = a.CreatedAt ?? DateTime.MinValue;
			DateTime y = b.CreatedAt ?? DateTime.MinValue;
			return y.CompareTo(x);
		});
		return list;
	}

	/// <summary>Создаёт снимок.</summary>
	public static Result CreateSnapshot(string vmName, string snapshotName)
	{
		Result r = new Result();
		string vmPath = FindVmPath(vmName);
		if (vmPath.Length == 0)
		{
			r.Message = "виртуальная машина не найдена: " + vmName;
			return r;
		}

		ManagementObject svc = null;
		try
		{
			svc = GetSnapshotService();
			if (svc == null)
			{
				r.Message = "служба снимков Hyper-V недоступна";
				return r;
			}

			string wantedName = (snapshotName ?? "").Trim();

			ManagementBaseObject inParams = svc.GetMethodParameters("CreateSnapshot");
			inParams["AffectedSystem"] = vmPath;
			// SnapshotSettings с ElementName Hyper-V принимает, но имя игнорирует:
			// снимок всё равно называется «ВМ - (дата время)». Поэтому создаём
			// снимок как есть, а затем переименовываем через ModifySystemSettings.
			inParams["SnapshotSettings"] = null;
			inParams["SnapshotType"] = (ushort)2; // Full — с памятью, полный снимок

			ManagementBaseObject outParams = svc.InvokeMethod("CreateSnapshot", inParams, null);
			uint code = Convert.ToUInt32(outParams["ReturnValue"], CultureInfo.InvariantCulture);

			if (code != 0u && code != 4096u)
			{
				r.Message = "снимок не создан: Hyper-V вернул код " + code;
				return r;
			}

			string job = Convert.ToString(outParams["Job"], CultureInfo.InvariantCulture) ?? "";
			if (job.Length > 0)
			{
				string jobError = WaitJob(job, 300);
				if (jobError.Length > 0)
				{
					r.Message = "снимок не создан: " + ExplainText(jobError);
					return r;
				}
			}

			string resulting = Convert.ToString(outParams["ResultingSnapshot"], CultureInfo.InvariantCulture) ?? "";
			VmsInvalidate();

			string renameNote = "";
			if (wantedName.Length > 0)
			{
				if (resulting.Length == 0)
				{
					// Задание могло не вернуть ссылку — берём самый свежий снимок.
					resulting = NewestSnapshotPath(vmName);
				}
				string renameError = resulting.Length > 0
					? RenameSnapshot(resulting, wantedName)
					: "не удалось найти созданный снимок";
				if (renameError.Length > 0)
				{
					renameNote = " (имя не применено: " + renameError + ")";
				}
			}

			r.Ok = true;
			r.Message = "снимок создан" + (wantedName.Length > 0 ? ": " + wantedName : "") + renameNote;
			Log.Info("ВМ " + vmName + ": " + r.Message);
			return r;
		}
		catch (Exception ex)
		{
			r.Message = "снимок не создан: " + ex.Message;
			Log.Warn("ВМ " + vmName + ", создание снимка: " + ex.Message);
			return r;
		}
		finally
		{
			try
			{
				svc?.Dispose();
			}
			catch
			{
			}
		}
	}

	/// <summary>
	/// Откатывает ВМ к снимку. Состояние машины после отката — как в момент снимка.
	/// </summary>
	public static Result ApplySnapshot(string vmName, string snapshotIdOrName)
	{
		Result r = new Result();
		r.Destructive = true;
		string snapPath = FindSnapshotPath(vmName, snapshotIdOrName);
		if (snapPath.Length == 0)
		{
			r.Message = "снимок не найден: " + snapshotIdOrName;
			return r;
		}

		ManagementObject svc = null;
		try
		{
			svc = GetSnapshotService();
			if (svc == null)
			{
				r.Message = "служба снимков Hyper-V недоступна";
				return r;
			}

			ManagementBaseObject inParams = svc.GetMethodParameters("ApplySnapshot");
			inParams["Snapshot"] = snapPath;
			ManagementBaseObject outParams = svc.InvokeMethod("ApplySnapshot", inParams, null);
			uint code = Convert.ToUInt32(outParams["ReturnValue"], CultureInfo.InvariantCulture);
			if (code != 0u && code != 4096u)
			{
				r.Message = "откат не выполнен: Hyper-V вернул код " + code;
				return r;
			}
			string job = Convert.ToString(outParams["Job"], CultureInfo.InvariantCulture) ?? "";
			if (job.Length > 0)
			{
				string jobError = WaitJob(job, 600);
				if (jobError.Length > 0)
				{
					r.Message = "откат не выполнен: " + ExplainText(jobError);
					return r;
				}
			}
			VmsInvalidate();
			r.Ok = true;
			r.Message = "откат к снимку выполнен";
			Log.Warn("ВМ " + vmName + ": откат к снимку " + snapshotIdOrName);
			return r;
		}
		catch (Exception ex)
		{
			r.Message = "откат не выполнен: " + ex.Message;
			Log.Warn("ВМ " + vmName + ", откат: " + ex.Message);
			return r;
		}
		finally
		{
			try
			{
				svc?.Dispose();
			}
			catch
			{
			}
		}
	}

	/// <summary>Удаляет снимок (сам снимок, не данные ВМ).</summary>
	public static Result DeleteSnapshot(string vmName, string snapshotIdOrName)
	{
		Result r = new Result();
		string snapPath = FindSnapshotPath(vmName, snapshotIdOrName);
		if (snapPath.Length == 0)
		{
			r.Message = "снимок не найден: " + snapshotIdOrName;
			return r;
		}

		ManagementObject svc = null;
		try
		{
			svc = GetSnapshotService();
			if (svc == null)
			{
				r.Message = "служба снимков Hyper-V недоступна";
				return r;
			}

			ManagementBaseObject inParams = svc.GetMethodParameters("DestroySnapshot");
			inParams["AffectedSnapshot"] = snapPath;
			ManagementBaseObject outParams = svc.InvokeMethod("DestroySnapshot", inParams, null);
			uint code = Convert.ToUInt32(outParams["ReturnValue"], CultureInfo.InvariantCulture);
			if (code != 0u && code != 4096u)
			{
				r.Message = "снимок не удалён: Hyper-V вернул код " + code;
				return r;
			}
			string job = Convert.ToString(outParams["Job"], CultureInfo.InvariantCulture) ?? "";
			if (job.Length > 0)
			{
				string jobError = WaitJob(job, 600);
				if (jobError.Length > 0)
				{
					r.Message = "снимок не удалён: " + ExplainText(jobError);
					return r;
				}
			}
			VmsInvalidate();
			r.Ok = true;
			r.Message = "снимок удалён";
			Log.Info("ВМ " + vmName + ": снимок " + snapshotIdOrName + " удалён");
			return r;
		}
		catch (Exception ex)
		{
			r.Message = "снимок не удалён: " + ex.Message;
			return r;
		}
		finally
		{
			try
			{
				svc?.Dispose();
			}
			catch
			{
			}
		}
	}

	// ------------------------------------------------------------------ WMI

	private static string FindVmPath(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return "";
		}
		ManagementObjectCollection systems = Hw.Query("root\\virtualization\\v2", "SELECT * FROM Msvm_ComputerSystem");
		if (systems == null)
		{
			return "";
		}
		try
		{
			foreach (ManagementBaseObject o in systems)
			{
				using (o)
				{
					if (!string.Equals(Hw.Prop(o, "ElementName"), name, StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					if (o is ManagementObject mo)
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
				systems.Dispose();
			}
			catch
			{
			}
		}
		return "";
	}

	/// <summary>Ищет снимок по InstanceID или по имени.</summary>
	private static string FindSnapshotPath(string vmName, string idOrName)
	{
		if (string.IsNullOrWhiteSpace(idOrName))
		{
			return "";
		}
		foreach (Snapshot s in ListSnapshots(vmName))
		{
			if (string.Equals(s.Id, idOrName, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(s.Name, idOrName, StringComparison.OrdinalIgnoreCase))
			{
				return s.Path;
			}
		}
		return "";
	}

	private static ManagementObject GetSnapshotService()
	{
		ManagementObjectCollection coll = Hw.Query("root\\virtualization\\v2",
			"SELECT * FROM Msvm_VirtualSystemSnapshotService");
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
		// Ничего не нашли — коллекцию надо освободить.
		try
		{
			coll.Dispose();
		}
		catch
		{
		}
		return null;
	}

	/// <summary>Путь к самому свежему снимку ВМ (страховка, если WMI не вернул ссылку).</summary>
	private static string NewestSnapshotPath(string vmName)
	{
		List<Snapshot> list = ListSnapshots(vmName);
		return list.Count > 0 ? list[0].Path : "";
	}

	/// <summary>
	/// Переименовывает снимок. Hyper-V не применяет имя из SnapshotSettings при
	/// создании, поэтому имя задаётся отдельным вызовом ModifySystemSettings.
	/// Возвращает пустую строку при успехе.
	/// </summary>
	private static string RenameSnapshot(string snapshotPath, string name)
	{
		ManagementObject svc = null;
		try
		{
			ManagementObjectCollection coll = Hw.Query("root\\virtualization\\v2",
				"SELECT * FROM Msvm_VirtualSystemManagementService");
			if (coll == null)
			{
				return "служба управления Hyper-V недоступна";
			}
			try
			{
				foreach (ManagementBaseObject o in coll)
				{
					if (o is ManagementObject mo)
					{
						svc = mo;
						break;
					}
				}
			}
			finally
			{
				if (svc == null)
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

			if (svc == null)
			{
				return "служба управления Hyper-V недоступна";
			}

			using ManagementObject snap = new ManagementObject(snapshotPath);
			snap.Get();
			snap["ElementName"] = name;
			string xml = snap.GetText(TextFormat.WmiDtd20);

			ManagementBaseObject inParams = svc.GetMethodParameters("ModifySystemSettings");
			inParams["SystemSettings"] = xml;
			ManagementBaseObject outParams = svc.InvokeMethod("ModifySystemSettings", inParams, null);
			uint code = Convert.ToUInt32(outParams["ReturnValue"], CultureInfo.InvariantCulture);
			if (code != 0u && code != 4096u)
			{
				return "Hyper-V вернул код " + code;
			}
			string job = Convert.ToString(outParams["Job"], CultureInfo.InvariantCulture) ?? "";
			if (job.Length > 0)
			{
				string jobError = WaitJob(job, 120);
				if (jobError.Length > 0)
				{
					return ExplainText(jobError);
				}
			}
			return "";
		}
		catch (Exception ex)
		{
			return ex.Message;
		}
		finally
		{
			try
			{
				svc?.Dispose();
			}
			catch
			{
			}
		}
	}

	/// <summary>
	/// Ждёт завершения WMI-задания. Возвращает пустую строку при успехе.
	/// </summary>
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
				// Задание исчезло — считаем, что отработало.
				return "";
			}
			Thread.Sleep(500);
		}
		return "превышено время ожидания (" + timeoutSeconds + " с)";
	}

	/// <summary>Целевое состояние Hyper-V для запрошенного перехода.</summary>
	private static ushort TargetState(ushort requested)
	{
		switch (requested)
		{
		case PowerState.Enable:
			return 2;
		case PowerState.Disable:
			return 3;
		case PowerState.ShutDown:
			return 3;
		case PowerState.Quiesce:
			return 9;
		case PowerState.Reboot:
			return 2;
		case PowerState.Reset:
			return 2;
		case PowerState.Save:
			return 32769;
		default:
			return 0;
		}
	}

	/// <summary>
	/// Ждёт, пока ВМ придёт в нужное состояние. Переходы в Hyper-V асинхронные:
	/// возврат «запущено» ещё не значит, что машина уже работает.
	/// </summary>
	private static bool WaitEnabledState(string vmName, ushort target, int timeoutSeconds)
	{
		DateTime deadline = DateTime.Now.AddSeconds(Math.Max(5, timeoutSeconds));
		while (DateTime.Now < deadline)
		{
			try
			{
				string vmPath = FindVmPath(vmName);
				if (vmPath.Length > 0)
				{
					using ManagementObject vm = new ManagementObject(vmPath);
					vm.Get();
					if (Hw.PropInt(vm, "EnabledState", -1) == target)
					{
						return true;
					}
				}
			}
			catch
			{
				return false;
			}
			Thread.Sleep(500);
		}
		return false;
	}

	/// <summary>
	/// Ждёт, пока ВМ выйдет из переходного состояния. Возвращает false по таймауту.
	/// </summary>
	private static bool WaitStable(ManagementObject vm, int timeoutSeconds)	{
		DateTime deadline = DateTime.Now.AddSeconds(Math.Max(5, timeoutSeconds));
		while (DateTime.Now < deadline)
		{
			try
			{
				vm.Get();
				int state = Hw.PropInt(vm, "EnabledState", -1);
				// Переходные: 32770 стартует, 32773 сохраняется, 32774 останавливается,
				// 32777 возобновляется.
				if (state != 32770 && state != 32773 && state != 32774 && state != 32777)
				{
					return true;
				}
			}
			catch
			{
				return true;
			}
			Thread.Sleep(500);
		}
		return false;
	}

	/// <summary>Расшифровка кодов возврата Hyper-V, которые встречаются на практике.</summary>
	private static string ExplainStateCode(uint code)
	{
		switch (code)
		{
		case 32768u:
			return "операция не удалась";
		case 32769u:
			return "доступ запрещён";
		case 32770u:
			return "не поддерживается";
		case 32771u:
			return "состояние неизвестно";
		case 32772u:
			return "таймаут";
		case 32773u:
			return "неверный параметр";
		case 32774u:
			return "система используется";
		case 32775u:
			return "недопустимое состояние для операции";
		case 32776u:
			return "неверный тип данных";
		case 32777u:
			return "система недоступна";
		case 32778u:
			return "недостаточно памяти";
		default:
			return "код " + code;
		}
	}

	private static void VmsInvalidate()
	{
		try
		{
			VmInventory.Invalidate();
		}
		catch
		{
		}
	}
}
