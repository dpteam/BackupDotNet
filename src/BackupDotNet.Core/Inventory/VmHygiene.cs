using System;
using System.Collections.Generic;
using System.IO;
using System.Management;

namespace BackupDotNet.Core.Inventory;

/// <summary>
/// Гигиена инфраструктуры: то, что копится незаметно и съедает место.
///
/// Что ищем и почему это важно:
///  • <b>диски-сироты</b> — файлы VHDX, не привязанные ни к одной ВМ. Появляются
///    после неудачного удаления ВМ и занимают десятки гигабайт;
///  • <b>снимки</b> — чем дольше живёт снимок, тем больше разница с базовым
///    диском и тем дольше откат; после месяца снимок уже дороже, чем полезен;
///  • <b>выключенные ВМ</b> — не ошибка, но если машина выключена полгода,
///    стоит спросить, нужна ли она.
///
/// Ничего не удаляет: только показывает. Удаление файлов — решение человека,
/// и оно должно быть осознанным.
/// </summary>
public static class VmHygiene
{
	/// <summary>Результат осмотра.</summary>
	public class Report
	{
		public List<object> OrphanDisks = new List<object>();

		public List<object> OldSnapshots = new List<object>();

		public List<object> StoppedVms = new List<object>();

		public List<object> LargeVms = new List<object>();

		public long OrphanBytes;

		public long SnapshotBytes;

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["orphanDisks"] = OrphanDisks;
			d["oldSnapshots"] = OldSnapshots;
			d["stoppedVms"] = StoppedVms;
			d["largeVms"] = LargeVms;
			d["orphanBytes"] = OrphanBytes;
			d["snapshotBytes"] = SnapshotBytes;
			d["orphanCount"] = OrphanDisks.Count;
			d["snapshotCount"] = OldSnapshots.Count;
			d["stoppedCount"] = StoppedVms.Count;
			return d;
		}
	}

	/// <summary>
	/// Осматривает хост.
	/// </summary>
	/// <param name="snapshotMaxAgeDays">Снимки старше этого срока попадают в отчёт.</param>
	/// <param name="stoppedMaxAgeDays">Выключенные дольше этого срока — в отчёт.</param>
	/// <param name="searchRoots">Где искать файлы-сироты. Пусто — только каталоги ВМ.</param>
	public static Report Inspect(int snapshotMaxAgeDays, int stoppedMaxAgeDays, List<string> searchRoots)
	{
		Report report = new Report();
		if (snapshotMaxAgeDays <= 0)
		{
			snapshotMaxAgeDays = 30;
		}
		if (stoppedMaxAgeDays <= 0)
		{
			stoppedMaxAgeDays = 90;
		}

		// --- 1. собираем всё, что реально используется
		HashSet<string> usedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<string> vmDirs = new List<string>();
		List<Dictionary<string, object>> vms = new List<Dictionary<string, object>>();
		try
		{
			string err;
			vms = VmInventory.Vms(out err, force: true);
		}
		catch (Exception ex)
		{
			Log.Warn("Гигиена: не удалось прочитать список ВМ — " + ex.Message);
		}

		foreach (Dictionary<string, object> vm in vms)
		{
			foreach (object o in Json.AsArray(Json.Get(vm, "disks")))
			{
				string path = Json.GetStr(Json.AsObject(o), "path", "");
				if (path.Length == 0)
				{
					continue;
				}
				usedPaths.Add(path);
				try
				{
					string dir = Path.GetDirectoryName(path);
					if (!string.IsNullOrEmpty(dir) && !ContainsIgnoreCase(vmDirs, dir))
					{
						vmDirs.Add(dir);
					}
				}
				catch
				{
				}
			}
		}

		// Снимок делает активным разностный диск (.avhdx), а базовый .vhdx остаётся
		// его родителем. Без этого базовый файл выглядел бы сиротой — а он нужен.
		AddParentChains(usedPaths);

		// --- 2. снимки старше срока
		ReadSnapshots(report, snapshotMaxAgeDays);

		// --- 3. выключенные ВМ
		DateTime now = DateTime.Now;
		foreach (Dictionary<string, object> vm in vms)
		{
			if (Json.Bool(Json.Get(vm, "running"), dflt: false))
			{
				continue;
			}
			string state = Json.GetStr(vm, "state", "");
			if (state.Length == 0)
			{
				continue;
			}
			// Аптайм у выключенной не считается, поэтому срок «сколько стоит»
			// берём из времени последнего изменения файла конфигурации.
			double daysStopped = -1.0;
			string cfgPath = FindConfigFile(vm);
			if (cfgPath.Length > 0)
			{
				try
				{
					daysStopped = (now - File.GetLastWriteTime(cfgPath)).TotalDays;
				}
				catch
				{
				}
			}
			if (daysStopped >= 0 && daysStopped < stoppedMaxAgeDays)
			{
				continue;
			}
			Dictionary<string, object> s = Json.Obj();
			s["name"] = Json.GetStr(vm, "name", "");
			s["state"] = state;
			s["daysStopped"] = daysStopped >= 0 ? Math.Round(daysStopped) : (double?)null;
			s["diskBytes"] = Json.Long(Json.Get(vm, "diskBytes"), 0L);
			report.StoppedVms.Add(s);
		}

		// --- 4. крупные ВМ (больше 100 ГБ дисков) — просто для сведения
		foreach (Dictionary<string, object> vm in vms)
		{
			long bytes = Json.Long(Json.Get(vm, "diskBytes"), 0L);
			if (bytes < 100L * 1024 * 1024 * 1024)
			{
				continue;
			}
			Dictionary<string, object> l = Json.Obj();
			l["name"] = Json.GetStr(vm, "name", "");
			l["diskBytes"] = bytes;
			report.LargeVms.Add(l);
		}
		report.LargeVms.Sort((a, b) =>
			Json.Long(Json.Get(Json.AsObject(b), "diskBytes"), 0L)
				.CompareTo(Json.Long(Json.Get(Json.AsObject(a), "diskBytes"), 0L)));

		// --- 5. диски-сироты
		// Всегда смотрим каталоги, где реально лежат диски ВМ, плюс всё, что
		// попросили дополнительно. Иначе сирота рядом с ВМ не находится.
		List<string> roots = new List<string>(vmDirs);
		if (searchRoots != null)
		{
			foreach (string r in searchRoots)
			{
				if (!string.IsNullOrWhiteSpace(r))
				{
					roots.Add(r.Trim());
				}
			}
		}

		HashSet<string> scanned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string root in roots)
		{
			if (!scanned.Add(root))
			{
				continue;
			}
			ScanForOrphans(report, root, usedPaths, 0);
		}

		report.OrphanDisks.Sort((a, b) =>
			Json.Long(Json.Get(Json.AsObject(b), "sizeBytes"), 0L)
				.CompareTo(Json.Long(Json.Get(Json.AsObject(a), "sizeBytes"), 0L)));
		return report;
	}

	/// <summary>Поиск без учёта регистра: у <see cref="List{T}"/> нет такой перегрузки.</summary>
	private static bool ContainsIgnoreCase(List<string> list, string value)
	{
		foreach (string s in list)
		{
			if (string.Equals(s, value, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Достраивает цепочку родителей для разностных дисков: снимок делает активным
	/// <c>.avhdx</c>, а базовый <c>.vhdx</c> остаётся его родителем и без этой
	/// достройки выглядел бы сиротой.
	///
	/// Источник — конфигурации снимков: там в настройках хранилища указан базовый
	/// диск. Перечислять <c>Msvm_VirtualHardDiskSettingData</c> бесполезно:
	/// этот класс заполняется только через явный вызов метода.
	/// </summary>
	private static void AddParentChains(HashSet<string> usedPaths)
	{
		ManagementObjectCollection all = Hw.Query("root\\virtualization\\v2",
			"SELECT * FROM Msvm_VirtualSystemSettingData");
		if (all == null)
		{
			return;
		}
		try
		{
			foreach (ManagementBaseObject o in all)
			{
				using (o)
				{
					string type = Hw.Prop(o, "VirtualSystemType");
					bool isSnapshot = type.IndexOf("Snapshot", StringComparison.OrdinalIgnoreCase) >= 0;
					if (!isSnapshot || !(o is ManagementObject mo))
					{
						continue;
					}
					foreach (ManagementObject sasd in mo.GetRelated("Msvm_StorageAllocationSettingData"))
					{
						using (sasd)
						{
							try
							{
								object hr = sasd["HostResource"];
								if (hr is string[] arr)
								{
									foreach (string p in arr)
									{
										if (!string.IsNullOrEmpty(p))
										{
											usedPaths.Add(p);
										}
									}
								}
								else if (hr != null)
								{
									string single = Convert.ToString(hr, System.Globalization.CultureInfo.InvariantCulture);
									if (!string.IsNullOrEmpty(single))
									{
										usedPaths.Add(single);
									}
								}
							}
							catch
							{
							}
						}
					}
				}
			}
		}
		catch
		{
		}
		finally
		{
			try
			{
				all.Dispose();
			}
			catch
			{
			}
		}
	}

	/// <summary>
	/// Ищет файлы образов, которых нет в списке используемых.
	/// Глубина ограничена: в корне диска сканировать всё — плохая идея.
	/// </summary>
	private static void ScanForOrphans(Report report, string root, HashSet<string> usedPaths, int depth)
	{
		if (depth > 4 || string.IsNullOrEmpty(root) || !Directory.Exists(root))
		{
			return;
		}
		try
		{
			foreach (string f in Directory.GetFiles(root, "*.vhd*"))
			{
				// Расширения проверяем точно: маска *.vhd* ловит ещё и служебные
				// файлы отслеживания изменений (.vhdx.rct, .vhdx.mrt), которые
				// сиротами не являются и удалять их нельзя.
				string ext = Path.GetExtension(f).ToLowerInvariant();
				if (ext != ".vhd" && ext != ".vhdx" && ext != ".avhdx")
				{
					continue;
				}
				if (usedPaths.Contains(f))
				{
					continue;
				}
				long size = 0L;
				DateTime modified = DateTime.MinValue;
				try
				{
					FileInfo fi = new FileInfo(f);
					size = fi.Length;
					modified = fi.LastWriteTime;
				}
				catch
				{
				}
				Dictionary<string, object> o = Json.Obj();
				o["path"] = f;
				o["fileName"] = Path.GetFileName(f);
				o["sizeBytes"] = size;
				o["modified"] = modified;
				report.OrphanDisks.Add(o);
				report.OrphanBytes += size;
			}
			foreach (string dir in Directory.GetDirectories(root))
			{
				ScanForOrphans(report, dir, usedPaths, depth + 1);
			}
		}
		catch
		{
		}
	}

	private static void ReadSnapshots(Report report, int maxAgeDays)
	{
		ManagementObjectCollection all = Hw.Query("root\\virtualization\\v2", "SELECT * FROM Msvm_VirtualSystemSettingData");
		if (all == null)
		{
			return;
		}
		DateTime now = DateTime.Now;
		try
		{
			foreach (ManagementBaseObject o in all)
			{
				using (o)
				{
					string type = Hw.Prop(o, "VirtualSystemType");
					if (type.IndexOf("Snapshot", StringComparison.OrdinalIgnoreCase) < 0)
					{
						continue;
					}
					DateTime? created = null;
					try
					{
						created = Hw.ParseWmiDate(o["CreationTime"]);
					}
					catch
					{
					}
					double days = created.HasValue ? (now - created.Value).TotalDays : -1.0;
					if (days >= 0 && days < maxAgeDays)
					{
						continue;
					}
					Dictionary<string, object> s = Json.Obj();
					s["name"] = Hw.Prop(o, "ElementName");
					s["vmId"] = Hw.Prop(o, "ConfigurationID");
					s["created"] = created;
					s["ageDays"] = days >= 0 ? Math.Round(days) : (double?)null;
					s["maxAgeDays"] = maxAgeDays;
					report.OldSnapshots.Add(s);
					// Размер снимка — это разница с базовым диском, её быстро не получить.
					// Показываем хотя бы возраст: он и есть главный сигнал.
				}
			}
		}
		catch
		{
		}
		finally
		{
			try
			{
				all.Dispose();
			}
			catch
			{
			}
		}
	}

	/// <summary>
	/// Ищет файл конфигурации ВМ. Сначала по <c>ConfigurationDataRoot</c> из WMI —
	/// это точный путь; эвристика «рядом с дисками» работает только когда
	/// каталоги ВМ и дисков лежат рядом, а это не всегда так.
	/// </summary>
	private static string FindConfigFile(Dictionary<string, object> vm)
	{
		string configRoot = Json.GetStr(vm, "configRoot", "");
		if (configRoot.Length > 0 && Directory.Exists(configRoot))
		{
			try
			{
				// Hyper-V кладёт конфигурацию в подкаталог Virtual Machines,
				// а не в корень. Ищем на двух уровнях — этого достаточно.
				foreach (string dir in new string[2]
				{
					configRoot,
					Path.Combine(configRoot, "Virtual Machines")
				})
				{
					if (!Directory.Exists(dir))
					{
						continue;
					}
					string[] cfg = Directory.GetFiles(dir, "*.vmcx");
					if (cfg.Length > 0)
					{
						return cfg[0];
					}
					string[] xml = Directory.GetFiles(dir, "*.xml");
					if (xml.Length > 0)
					{
						return xml[0];
					}
				}
			}
			catch
			{
			}
		}

		// Запасной путь: каталог Virtual Machines рядом с дисками.
		foreach (object o in Json.AsArray(Json.Get(vm, "disks")))
		{
			string path = Json.GetStr(Json.AsObject(o), "path", "");
			if (path.Length == 0)
			{
				continue;
			}
			try
			{
				string dir = Path.GetDirectoryName(path);
				if (string.IsNullOrEmpty(dir))
				{
					continue;
				}
				string parent = Path.GetDirectoryName(dir);
				if (string.IsNullOrEmpty(parent))
				{
					continue;
				}
				string vmCfgDir = Path.Combine(parent, "Virtual Machines");
				if (Directory.Exists(vmCfgDir))
				{
					string[] cfg = Directory.GetFiles(vmCfgDir, "*.vmcx");
					if (cfg.Length > 0)
					{
						return cfg[0];
					}
				}
			}
			catch
			{
			}
		}
		return "";
	}
}
