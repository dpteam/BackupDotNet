using System;
using System.Collections.Generic;
using System.Globalization;

namespace BackupDotNet.Manage;

public static class Catalog
{
	public static readonly string[] ForbiddenServices = new string[27]
	{
		"RpcSs", "RpcEptMapper", "DcomLaunch", "Power", "PlugPlay", "Winmgmt", "EventLog", "Schedule", "LanmanServer", "LanmanWorkstation",
		"SamSs", "KeyIso", "Netlogon", "NTDS", "ADWS", "Kdc", "LSM", "CryptSvc", "TrustedInstaller", "MSDTC",
		"BFE", "MpsSvc", "TermService", "UmRdpService", "SessionEnv", "BackupDotNetService", "BackupDotNetRemote"
	};

	public static readonly string[] CommonServices = new string[14]
	{
		"Spooler", "VSS", "SQLSERVERAGENT", "MSSQLSERVER", "SQLBrowser", "VeeamBackupSvc", "VeeamTransportSvc", "VeeamMountSvc", "WinRM", "W32Time",
		"SNMP", "LanmanServer2", "wuauserv", "BITS"
	};

	public static List<OpDef> All()
	{
		List<OpDef> list = new List<OpDef>();
		list.Add(new OpDef
		{
			Id = "host.info",
			Title = "Сведения о машине",
			Group = "Диагностика",
			Risk = "low",
			Transport = "winrm",
			Description = "Имя, ОС, версия, uptime, свободное место, время последней загрузки.",
			Params = new List<OpParam>()
		});
		list.Add(new OpDef
		{
			Id = "service.list",
			Title = "Список служб",
			Group = "Диагностика",
			Risk = "low",
			Transport = "winrm",
			Description = "Запущенные службы и службы с автоматическим запуском, которые остановлены.",
			Params = new List<OpParam>()
		});
		list.Add(new OpDef
		{
			Id = "vm.list",
			Title = "Список виртуальных машин",
			Group = "Диагностика",
			Risk = "low",
			Transport = "hyperv",
			Description = "Виртуальные машины Hyper-V на этой машине: состояние, память, vCPU.",
			Params = new List<OpParam>()
		});
		list.Add(new OpDef
		{
			Id = "service.restart",
			Title = "Перезапустить службу",
			Group = "Службы",
			Risk = "medium",
			Transport = "winrm",
			Description = "Остановить и запустить службу. Критические службы запрещены.",
			Rollback = "Служба будет запущена повторно; если не поднялась — проверить журнал событий.",
			Params = new List<OpParam>
			{
				new OpParam
				{
					Name = "service",
					Label = "Служба",
					Type = "text",
					Required = true,
					Hint = "короткое имя, например Spooler или VSS"
				}
			}
		});
		list.Add(new OpDef
		{
			Id = "service.start",
			Title = "Запустить службу",
			Group = "Службы",
			Risk = "low",
			Transport = "winrm",
			Description = "Запустить остановленную службу.",
			Params = new List<OpParam>
			{
				new OpParam
				{
					Name = "service",
					Label = "Служба",
					Type = "text",
					Required = true
				}
			}
		});
		list.Add(new OpDef
		{
			Id = "service.stop",
			Title = "Остановить службу",
			Group = "Службы",
			Risk = "medium",
			Transport = "winrm",
			Description = "Остановить службу. Критические службы запрещены.",
			Rollback = "Запустить службу обратно операцией «Запустить службу».",
			Params = new List<OpParam>
			{
				new OpParam
				{
					Name = "service",
					Label = "Служба",
					Type = "text",
					Required = true
				}
			}
		});
		list.Add(new OpDef
		{
			Id = "vm.start",
			Title = "Запустить ВМ",
			Group = "Виртуальные машины",
			Risk = "medium",
			Transport = "hyperv",
			Description = "Включить виртуальную машину Hyper-V на этой машине.",
			Rollback = "Операция «Остановить ВМ».",
			Params = new List<OpParam>
			{
				new OpParam
				{
					Name = "vm",
					Label = "Имя ВМ",
					Type = "text",
					Required = true
				}
			}
		});
		list.Add(new OpDef
		{
			Id = "vm.stop",
			Title = "Остановить ВМ",
			Group = "Виртуальные машины",
			Risk = "medium",
			Transport = "hyperv",
			Description = "Корректно выключить ВМ (гостевая ОС получает сигнал завершения).",
			Rollback = "Операция «Запустить ВМ».",
			Params = new List<OpParam>
			{
				new OpParam
				{
					Name = "vm",
					Label = "Имя ВМ",
					Type = "text",
					Required = true
				}
			}
		});
		list.Add(new OpDef
		{
			Id = "vm.snapshot",
			Title = "Создать снимок ВМ",
			Group = "Виртуальные машины",
			Risk = "low",
			Transport = "hyperv",
			Description = "Контрольная точка Hyper-V. Занимает место на диске хоста.",
			Rollback = "Удалить снимок через Hyper-V Manager (удаление снимка необратимо).",
			Params = new List<OpParam>
			{
				new OpParam
				{
					Name = "vm",
					Label = "Имя ВМ",
					Type = "text",
					Required = true
				}
			}
		});
		list.Add(new OpDef
		{
			Id = "host.reboot",
			Title = "Перезагрузить сервер",
			Group = "Хост",
			Risk = "high",
			Transport = "winrm",
			NeedsConfirmText = true,
			Description = "Перезагрузка с задержкой 60 секунд. Требует ввода имени машины.",
			Rollback = "Откат невозможен. Перед запуском убедитесь, что бэкап завершён.",
			Params = new List<OpParam>
			{
				new OpParam
				{
					Name = "reason",
					Label = "Причина",
					Type = "text",
					Required = true,
					Hint = "попадёт в журнал Windows как комментарий к выключению"
				}
			}
		});
		return list;
	}

	public static OpDef Find(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		foreach (OpDef d in All())
		{
			if (string.Equals(d.Id, id, StringComparison.OrdinalIgnoreCase))
			{
				return d;
			}
		}
		return null;
	}

	public static List<string> Groups()
	{
		List<string> g = new List<string>();
		foreach (OpDef d in All())
		{
			if (!g.Contains(d.Group))
			{
				g.Add(d.Group);
			}
		}
		return g;
	}

	public static List<string> Validate(OpDef def, Dictionary<string, string> given)
	{
		List<string> errors = new List<string>();
		if (def == null)
		{
			errors.Add("неизвестная операция");
			return errors;
		}
		if (given == null)
		{
			given = new Dictionary<string, string>();
		}
		foreach (OpParam p in def.Params)
		{
			string v = Get(given, p.Name);
			if (v.Length == 0)
			{
				if (p.Required)
				{
					errors.Add("не задан параметр «" + p.Label + "»");
				}
			}
			else if (p.Type == "int")
			{
				if (!int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var _))
				{
					errors.Add("«" + p.Label + "» должно быть числом");
				}
			}
			else
			{
				if (!(p.Type == "choice") || p.Choices.Count <= 0)
				{
					continue;
				}
				bool ok = false;
				foreach (string c in p.Choices)
				{
					if (string.Equals(c, v, StringComparison.OrdinalIgnoreCase))
					{
						ok = true;
						break;
					}
				}
				if (!ok)
				{
					errors.Add("«" + p.Label + "»: допустимые значения — " + string.Join(", ", p.Choices.ToArray()));
				}
			}
		}
		string svc = Get(given, "service");
		if (svc.Length > 0)
		{
			string clean = svc.Trim();
			string text = clean;
			foreach (char c2 in text)
			{
				if (!char.IsLetterOrDigit(c2) && c2 != '.' && c2 != '-' && c2 != '_')
				{
					errors.Add("в имени службы недопустимый символ: «" + c2 + "»");
					break;
				}
			}
			string[] forbiddenServices = ForbiddenServices;
			foreach (string bad in forbiddenServices)
			{
				if (string.Equals(bad, clean, StringComparison.OrdinalIgnoreCase))
				{
					errors.Add("служба " + bad + " в списке запрещённых: её остановка кладёт сервер, домен или сам агент");
					break;
				}
			}
		}
		string[] array = new string[2] { "vm", "reason" };
		foreach (string key in array)
		{
			string v2 = Get(given, key);
			if (v2.Length != 0 && (v2.IndexOf('\n') >= 0 || v2.IndexOf('\r') >= 0))
			{
				errors.Add("параметр «" + key + "» не должен содержать перевод строки");
			}
		}
		return errors;
	}

	public static string Get(Dictionary<string, string> d, string key)
	{
		if (d == null)
		{
			return "";
		}
		if (d.TryGetValue(key, out var v))
		{
			return (v ?? "").Trim();
		}
		foreach (KeyValuePair<string, string> kv in d)
		{
			if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
			{
				return (kv.Value ?? "").Trim();
			}
		}
		return "";
	}

	public static bool IsHighRisk(OpDef def)
	{
		if (def != null)
		{
			return string.Equals(def.Risk, "high", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}
}
