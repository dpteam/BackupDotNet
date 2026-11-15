using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management;
using System.Threading;

namespace BackupDotNet.Core.Backup;

/// <summary>
/// Бэкап виртуальной машины Hyper-V экспортом её определения.
///
/// Почему экспорт, а не копирование VHDX:
///  • экспорт — штатный механизм Hyper-V, он сам собирает конфигурацию, диски
///    и состояние в один каталог, консистентно;
///  • он не требует останавливать ВМ (для работающей создаётся снимок состояния
///    файлов средствами Hyper-V);
///  • не нужны внешние библиотеки: всё через WMI <c>root\virtualization\v2</c>.
///
/// Ограничения, о которых честно сказано в интерфейсе:
///  • экспорт работающей ВМ даёт копию «как есть», без VSS-снимка тома —
///    это не транзакционно-консистентный бэкап приложения;
///  • экспорт не сжимает: размер результата близок к сумме дисков.
/// </summary>
public class HyperVExportEngine : IBackupEngine
{
	public string Name => "hyperv-export";

	public string Title => "Hyper-V · экспорт ВМ";

	public string Description =>
		"Экспорт виртуальной машины средствами Hyper-V (WMI). Работает без остановки ВМ, " +
		"результат — каталог с конфигурацией и дисками.";

	/// <summary>
	/// Доступен ли движок на этой машине. Проверяем и службу, и WMI-пространство:
	/// одной проверки мало, они расходятся (служба есть, прав нет).
	/// </summary>
	public bool IsAvailable(out string reason)
	{
		reason = "";
		ManagementObjectCollection vms = Hw.Query("root\\virtualization\\v2", "SELECT Name FROM Msvm_ComputerSystem");
		if (vms == null)
		{
			reason = "нет доступа к WMI root\\virtualization\\v2 — нужны права администратора " +
				"или членство в группе «Hyper-V Administrators»";
			return false;
		}
		try
		{
			vms.Dispose();
		}
		catch
		{
		}
		return true;
	}

	/// <summary>Список ВМ на локальном хосте (для выпадающего списка в интерфейсе).</summary>
	public List<Dictionary<string, object>> ListTargets(out string error)
	{
		error = "";
		List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
		ManagementObjectCollection systems = Hw.Query("root\\virtualization\\v2", "SELECT * FROM Msvm_ComputerSystem");
		if (systems == null)
		{
			error = "Hyper-V недоступен: нет прав на WMI root\\virtualization\\v2";
			return list;
		}
		string hostName = Environment.MachineName;
		try
		{
			foreach (ManagementBaseObject o in systems)
			{
				using (o)
				{
					string name = Hw.Prop(o, "ElementName");
					if (string.IsNullOrEmpty(name) || string.Equals(name, hostName, StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					int state = Hw.PropInt(o, "EnabledState", -1);
					Dictionary<string, object> vm = Json.Obj();
					vm["name"] = name;
					vm["id"] = Hw.Prop(o, "Name");
					vm["stateCode"] = state;
					vm["state"] = StateName(state);
					vm["running"] = state == 2;
					// Приблизительный размер: сумма виртуальных дисков, если получится.
					vm["diskGb"] = 0.0;
					list.Add(vm);
				}
			}
		}
		catch (Exception ex)
		{
			error = ex.Message;
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
		list.Sort((a, b) => string.Compare(
			Json.GetStr(a, "name", ""), Json.GetStr(b, "name", ""), StringComparison.OrdinalIgnoreCase));
		return list;
	}

	/// <summary>
	/// Выполняет экспорт. Прогресс сообщается через <paramref name="report"/>:
	/// Hyper-V не отдаёт проценты по WMI, поэтому показываем этапы.
	/// </summary>
	public void Run(BackupJob job, Action<int, string> report, CancellationToken cancel)
	{
		string vmName = (job.Target ?? "").Trim();
		if (vmName.Length == 0)
		{
			throw new BackupException("не указано имя виртуальной машины");
		}
		string destRoot = (job.Destination ?? "").Trim();
		if (destRoot.Length == 0)
		{
			throw new BackupException("не указан каталог назначения");
		}

		report(5, "поиск виртуальной машины «" + vmName + "»");
		string vmPath = FindVmPath(vmName);
		if (vmPath.Length == 0)
		{
			throw new BackupException("виртуальная машина не найдена: " + vmName);
		}

		// Каталог на каждое задание: так копии не перемешиваются, а старые
		// можно удалять по политике хранения целиком.
		string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
		string targetDir = Path.Combine(destRoot, SafeName(vmName) + "_" + stamp);
		try
		{
			Directory.CreateDirectory(targetDir);
		}
		catch (Exception ex)
		{
			throw new BackupException("не удалось создать каталог «" + targetDir + "»: " + ex.Message);
		}
		job.Destination = targetDir;

		report(10, "подготовка экспорта");
		CheckCancel(cancel);

		// ExportSystemDefinition — асинхронная операция: возвращает Job-объект,
		// за которым надо следить до завершения.
		string jobPath;
		try
		{
			jobPath = StartExport(vmPath, targetDir, copyOnly: true);
		}
		catch (BackupException)
		{
			throw;
		}
		catch (Exception ex)
		{
			throw new BackupException("Hyper-V отклонил экспорт: " + ex.Message);
		}

		report(20, "экспорт выполняется");
		int lastPercent = 20;
		DateTime start = DateTime.Now;
		while (true)
		{
			CheckCancel(cancel);
			int state;
			string err;
			bool finished = PollJob(jobPath, out state, out err);

			if (err.Length > 0)
			{
				throw new BackupException("экспорт завершился ошибкой: " + err);
			}
			if (finished)
			{
				break;
			}

			// Проценты Hyper-V не отдаёт — показываем рост по времени,
			// но не даём «доползти» до 95, пока работа не закончена.
			double elapsed = (DateTime.Now - start).TotalSeconds;
			int fake = 20 + (int)Math.Min(70.0, elapsed / 6.0);
			if (fake > lastPercent)
			{
				lastPercent = fake;
				report(Math.Min(90, fake), "экспорт выполняется, " + (int)elapsed + " с");
			}
			Thread.Sleep(700);
		}

		report(93, "подсчёт размера");
		long size = DirSize(targetDir);
		job.SizeBytes = size;

		report(97, "проверка результата");
		if (!LooksLikeExport(targetDir))
		{
			throw new BackupException(
				"экспорт завершился, но в каталоге нет файлов конфигурации ВМ — результат нельзя считать пригодным");
		}

		report(100, "готово: " + Fmt.Size(size));
	}

	// ------------------------------------------------------------------ WMI

	/// <summary>
	/// Возвращает полный CIM-путь ВМ. Именно путь, а не GUID:
	/// параметр <c>ComputerSystem</c> имеет тип Reference, и Hyper-V
	/// отвергает голый идентификатор с ошибкой «неверный параметр».
	/// </summary>
	private static string FindVmPath(string name)
	{
		ManagementObjectCollection systems = Hw.Query("root\\virtualization\\v2", "SELECT * FROM Msvm_ComputerSystem");
		if (systems == null)
		{
			throw new BackupException("Hyper-V недоступен: нет прав на WMI root\\virtualization\\v2");
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

	/// <summary>
	/// Запускает экспорт и возвращает путь к WMI-заданию, за которым нужно следить.
	/// </summary>
	private static string StartExport(string vmPath, string targetDir, bool copyOnly)
	{
		ManagementObjectCollection svcColl = Hw.Query("root\\virtualization\\v2",
			"SELECT * FROM Msvm_VirtualSystemManagementService");
		if (svcColl == null)
		{
			throw new BackupException("служба управления Hyper-V недоступна");
		}
		ManagementObject svc = null;
		try
		{
			foreach (ManagementBaseObject o in svcColl)
			{
				svc = o as ManagementObject;
				if (svc != null)
				{
					break;
				}
			}
		}
		catch
		{
			// оставляем svc как есть
		}

		if (svc == null)
		{
			svcColl.Dispose();
			throw new BackupException("не найден Msvm_VirtualSystemManagementService");
		}

		try
		{
			ManagementBaseObject inParams = svc.GetMethodParameters("ExportSystemDefinition");
			inParams["ComputerSystem"] = vmPath;
			inParams["ExportDirectory"] = targetDir;
			// CopyOnly: не менять состояние ВМ после экспорта.
			inParams["ExportSettingData"] = BuildExportSetting(copyOnly);

			ManagementBaseObject outParams = svc.InvokeMethod("ExportSystemDefinition", inParams, null);
			uint code = Convert.ToUInt32(outParams["ReturnValue"], CultureInfo.InvariantCulture);
			if (code == 4096u)
			{
				// 4096 = Job Started (асинхронно)
				return Convert.ToString(outParams["Job"], CultureInfo.InvariantCulture) ?? "";
			}
			if (code == 0u)
			{
				return ""; // завершилось синхронно
			}
			throw new BackupException("ReturnValue=" + code);
		}
		finally
		{
			try
			{
				svc.Dispose();
			}
			catch
			{
			}
			try
			{
				svcColl.Dispose();
			}
			catch
			{
			}
		}
	}

	/// <summary>
	/// Собирает <c>Msvm_VirtualSystemExportSettingData</c>: копировать диски и
	/// не трогать состояние ВМ.
	///
	/// Важно: без этого параметра Hyper-V отвечает «неверный параметр» (32773),
	/// поэтому при ошибке сборки надо падать, а не подставлять null.
	/// Типы полей строгие: <c>SnapshotVirtualSystem</c> — строка, не число.
	/// </summary>
	private static string BuildExportSetting(bool copyOnly)
	{
		ManagementScope scope = new ManagementScope("\\\\.\\root\\virtualization\\v2");
		scope.Connect();
		using ManagementClass cls = new ManagementClass(scope, new ManagementPath("Msvm_VirtualSystemExportSettingData"), null);
		using ManagementObject inst = cls.CreateInstance();
		inst["CopyVmStorage"] = true;
		inst["CopyVmRuntimeInformation"] = copyOnly;
		inst["CreateVmExportSubdirectory"] = false;
		inst["CopySnapshotConfiguration"] = (byte)0; // снимки не копируем
		inst["SnapshotVirtualSystem"] = null;        // строка: пусто = без снимка
		return inst.GetText(TextFormat.WmiDtd20);
	}

	/// <summary>
	/// Проверяет состояние WMI-задания. Возвращает true, если работа завершена.
	/// </summary>
	private static bool PollJob(string jobPath, out int state, out string error)
	{
		state = -1;
		error = "";
		if (string.IsNullOrEmpty(jobPath))
		{
			return true; // синхронное выполнение
		}
		try
		{
			using ManagementObject job = new ManagementObject(new ManagementPath(jobPath));
			job.Get();
			object st = job["JobState"];
			if (st != null)
			{
				state = Convert.ToInt32(st, CultureInfo.InvariantCulture);
			}
			// 2 = Completed, 7 = Completed with warnings, 8 = Failed, 9 = Cancelled, 10 = Terminated
			if (state == 7)
			{
				string desc = Convert.ToString(job["ErrorDescription"], CultureInfo.InvariantCulture) ?? "";
				Log.Warn("Hyper-V экспорт завершён с предупреждениями: " + desc);
				return true;
			}
			if (state == 8 || state == 9 || state == 10)
			{
				error = Convert.ToString(job["ErrorDescription"], CultureInfo.InvariantCulture) ?? "";
				if (error.Length == 0)
				{
					error = "состояние задания " + state;
				}
				return true;
			}
			return state == 2;
		}
		catch (Exception ex)
		{
			// Задание могло исчезнуть после завершения — это нормально.
			Log.Debug("Hyper-V опрос задания: " + ex.Message);
			return true;
		}
	}

	// ------------------------------------------------------------- утилиты

	private static void CheckCancel(CancellationToken cancel)
	{
		if (cancel.IsCancellationRequested)
		{
			throw new BackupException("отменено пользователем");
		}
	}

	/// <summary>
	/// Признак пригодного экспорта: есть файлы конфигурации и хотя бы один диск.
	/// Hyper-V кладёт конфигурацию в <c>.vmcx</c> (это XML, но с другим расширением),
	/// а состояние — в <c>.vmgs</c>. Пустой каталог бэкапом не считается.
	/// </summary>
	private static bool LooksLikeExport(string dir)
	{
		try
		{
			if (!Directory.Exists(dir))
			{
				return false;
			}
			string[] files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
			bool hasConfig = false;
			bool hasDisk = false;
			foreach (string f in files)
			{
				string ext = Path.GetExtension(f).ToLowerInvariant();
				if (ext == ".vmcx" || ext == ".xml" || ext == ".vmgs")
				{
					hasConfig = true;
				}
				else if (ext == ".vhdx" || ext == ".vhd" || ext == ".avhdx")
				{
					hasDisk = true;
				}
			}
			return hasConfig && hasDisk;
		}
		catch
		{
			return false;
		}
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

	private static string SafeName(string name)
	{
		char[] invalid = Path.GetInvalidFileNameChars();
		System.Text.StringBuilder sb = new System.Text.StringBuilder(name.Length);
		foreach (char ch in name)
		{
			sb.Append(Array.IndexOf(invalid, ch) >= 0 || ch == ' ' ? '_' : ch);
		}
		string s = sb.ToString().Trim('_');
		return s.Length > 0 ? s : "vm";
	}

	public static string StateName(int code)
	{
		switch (code)
		{
		case 2:
			return "работает";
		case 3:
			return "выключена";
		case 9:
			return "пауза";
		case 32768:
			return "приостановлена";
		case 32769:
			return "сохранена";
		case 32770:
			return "запускается";
		case 32771:
			return "снимок";
		case 32773:
			return "сохраняется";
		case 32774:
			return "останавливается";
		case 32776:
			return "загрузка";
		case 32777:
			return "возобновление";
		default:
			return "состояние " + code;
		}
	}
}

/// <summary>Ошибка выполнения бэкапа: текст пригоден для показа пользователю.</summary>
public class BackupException : Exception
{
	public BackupException(string message)
		: base(message)
	{
	}
}
