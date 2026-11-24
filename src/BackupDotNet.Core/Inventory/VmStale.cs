using System;
using System.Collections.Generic;

namespace BackupDotNet.Core.Inventory;

/// <summary>
/// Машины, о которых забыли: работают, место занимают, но резервной копии
/// давно нет (или не было никогда).
///
/// Это не ошибка мониторинга — бэкапы мог делать кто-то другой. Но если
/// про машину не помнит ни журнал бэкапов, ни расписание, то в момент аварии
/// окажется, что восстанавливать нечего. Поэтому показываем, а не чиним.
/// </summary>
public static class VmStale
{
	/// <summary>Строка отчёта по одной машине.</summary>
	public class Item
	{
		public string Name = "";

		public string State = "";

		public bool Running;

		public long DiskBytes;

		/// <summary>Когда последний успешный бэкап. null — не было ни одного.</summary>
		public DateTime? LastBackup;

		public string Reason = "";

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["name"] = Name;
			d["state"] = State;
			d["running"] = Running;
			d["diskBytes"] = DiskBytes;
			d["lastBackup"] = LastBackup;
			if (LastBackup.HasValue)
			{
				d["ageDays"] = Math.Round((DateTime.Now - LastBackup.Value).TotalDays, 1);
			}
			d["reason"] = Reason;
			return d;
		}
	}

	/// <summary>
	/// Ищет машины без свежей резервной копии.
	/// </summary>
	/// <param name="vms">Список ВМ из инвентаря.</param>
	/// <param name="backupTargets">
	/// Цели успешных бэкапов: имя машины → время последнего успеха.
	/// Регистр не важен.
	/// </param>
	/// <param name="maxAgeDays">Старше этого срока бэкап считается устаревшим.</param>
	public static List<Item> Find(
		List<Dictionary<string, object>> vms,
		Dictionary<string, DateTime> backupTargets,
		int maxAgeDays)
	{
		List<Item> list = new List<Item>();
		if (vms == null)
		{
			return list;
		}
		if (maxAgeDays <= 0)
		{
			maxAgeDays = 30;
		}

		Dictionary<string, DateTime> targets = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
		if (backupTargets != null)
		{
			foreach (KeyValuePair<string, DateTime> kv in backupTargets)
			{
				if (!string.IsNullOrWhiteSpace(kv.Key))
				{
					targets[kv.Key.Trim()] = kv.Value;
				}
			}
		}

		foreach (Dictionary<string, object> vm in vms)
		{
			string name = Json.GetStr(vm, "name", "");
			if (name.Length == 0)
			{
				continue;
			}

			// Машины в карантине не считаем: их судьба уже решена.
			if (name.StartsWith("QUARANTINE_", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			targets.TryGetValue(name, out DateTime last);

			Item item = new Item();
			item.Name = name;
			item.State = Json.GetStr(vm, "state", "");
			item.Running = Json.Bool(Json.Get(vm, "running"), dflt: false);
			item.DiskBytes = Json.Long(Json.Get(vm, "diskBytes"), 0L);
			item.LastBackup = last == default(DateTime) ? (DateTime?)null : last;

			if (item.LastBackup == null)
			{
				item.Reason = "резервной копии не было ни разу";
				list.Add(item);
				continue;
			}

			double age = (DateTime.Now - item.LastBackup.Value).TotalDays;
			if (age > maxAgeDays)
			{
				item.Reason = "последняя копия " + Math.Round(age, 0) + " дн. назад";
				list.Add(item);
			}
		}

		// Сначала самые «забытые» и самые крупные: с них и начинать.
		list.Sort((a, b) =>
		{
			DateTime x = a.LastBackup ?? DateTime.MinValue;
			DateTime y = b.LastBackup ?? DateTime.MinValue;
			int cmp = x.CompareTo(y);
			if (cmp != 0)
			{
				return cmp;
			}
			return b.DiskBytes.CompareTo(a.DiskBytes);
		});
		return list;
	}
}
