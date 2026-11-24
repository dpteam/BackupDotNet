using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BackupDotNet.Core.Support;

/// <summary>
/// Обезличивание текста перед передачей наружу.
///
/// Задача: сохранить <b>полезность</b> для разбора и убрать всё, что нельзя
/// отдавать. Поэтому значения заменяются не звёздочками, а <b>устойчивыми
/// плейсхолдерами</b>: одно и то же имя хоста всегда становится одним и тем же
/// <c>&lt;HOST-1&gt;</c>. По такой копии можно рассуждать («HOST-1 не отвечает
/// уже час»), но нельзя узнать, что это за хост.
///
/// Что вычищается:
///  • секреты — пароли, токены, ключи, строки подключения;
///  • адреса — IPv4, IPv6, URL с учётными данными;
///  • почта и имена пользователей;
///  • имена машин, домены, имена виртуальных машин и компаний;
///  • пути с именами пользователей.
///
/// Что <b>остаётся</b>: коды ошибок, типы источников, времена, размеры,
/// тексты исключений без идентификаторов. Именно это и нужно для анализа.
/// </summary>
public class Redactor
{
	/// <summary>Сколько замен каждого вида сделано — попадает в манифест архива.</summary>
	private readonly Dictionary<string, int> _counters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	/// <summary>Известное значение → плейсхолдер. Одно значение — один плейсхолдер.</summary>
	private readonly Dictionary<string, string> _map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	/// <summary>Порядок замены: сначала длинные, чтобы «host.example.org» не съелся как «host».</summary>
	private readonly List<string> _known = new List<string>();

	/// <summary>
	/// Регулярные выражения для того, что нельзя перечислить заранее.
	/// Порядок важен: секреты раньше всего, чтобы не утекли через другие замены.
	/// </summary>
	private static readonly Regex[] SecretPatterns = new Regex[]
	{
		// "password": "..."  /  "token":"..."  /  apiKey = "..."
		new Regex("(?i)(\"(?:password|passwd|pwd|token|secret|apikey|api_key|key)\"\\s*[:=]\\s*)\"[^\"]*\"", RegexOptions.Compiled),
		new Regex("(?i)((?:password|passwd|pwd|token|secret|apikey|api_key|community)\\s*[=:]\\s*)([^\\s,;\"']+)", RegexOptions.Compiled),
		// dpapi:... и прочие base64-секреты
		new Regex("dpapi:[A-Za-z0-9+/=]+", RegexOptions.Compiled),
		// Строки подключения: Server=...;Password=...
		new Regex("(?i)(Password|Pwd)\\s*=\\s*[^;\\s]+", RegexOptions.Compiled),
		// Bearer-токены
		new Regex("(?i)Bearer\\s+[A-Za-z0-9._\\-]+", RegexOptions.Compiled),
	};

	private static readonly Regex EmailRe = new Regex(
		@"\b[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}\b", RegexOptions.Compiled);

	private static readonly Regex Ipv4Re = new Regex(
		@"\b(?:(?:25[0-5]|2[0-4]\d|1?\d?\d)\.){3}(?:25[0-5]|2[0-4]\d|1?\d?\d)\b", RegexOptions.Compiled);

	// IPv6 — только «полные» формы, чтобы не портить версии и времена.
	private static readonly Regex Ipv6Re = new Regex(
		@"\b(?:[A-Fa-f0-9]{1,4}:){7}[A-Fa-f0-9]{1,4}\b|\b(?:[A-Fa-f0-9]{1,4}:){1,7}:(?:[A-Fa-f0-9]{1,4})?\b",
		RegexOptions.Compiled);

	// URL с учётными данными: https://user:pass@host/...
	// Жадный до последней «@» перед путём: пароль может сам содержать «@»
	// и «:», и наивное выражение оставило бы его хвост в тексте.
	private static readonly Regex UrlCredsRe = new Regex(
		@"\b([a-z][a-z0-9+.\-]*://)[^\s/]*@", RegexOptions.Compiled | RegexOptions.IgnoreCase);

	// Windows-путь с именем пользователя: C:\Users\Имя\...
	private static readonly Regex UserPathRe = new Regex(
		@"([A-Za-z]:\\Users\\)([^\\/\s""']+)", RegexOptions.Compiled);

	// UNC-путь. Два варианта: обычный \\server\share и JSON-экранированный
	// \\\\server\\share — в конфигурации пути хранятся вторым способом, и
	// наивное выражение «две обратные косые» его не ловит.
	// Группы сохраняют исходное число косых, чтобы не ломать JSON.
	private static readonly Regex UncRe = new Regex(
		@"(\\{2,})([^\\/\s""']+)(\\{1,})([^\\/\s""'*?]+)", RegexOptions.Compiled);

	// Домен в виде host.example.org / host.local
	private static readonly Regex HostRe = new Regex(
		@"\b(?:[A-Za-z0-9](?:[A-Za-z0-9\-]{0,61}[A-Za-z0-9])?\.)+(?:local|lan|corp|internal|ru|com|org|net|io|by|kz|ua|su)\b",
		RegexOptions.Compiled | RegexOptions.IgnoreCase);

	/// <summary>Добавляет известное значение (имя хоста, компания, ВМ) в список замены.</summary>
	public void Register(string value, string category)
	{
		if (string.IsNullOrWhiteSpace(value) || value.Length < 3)
		{
			return;
		}
		// Слишком общие строки заменять нельзя: «admin» в тексте ошибки — это роль,
		// а не имя пользователя, и замена сделает лог бессмысленным.
		if (IsTooGeneric(value))
		{
			return;
		}
		string v = value.Trim();
		if (_map.ContainsKey(v))
		{
			return;
		}
		string placeholder = MakePlaceholder(category);
		_map[v] = placeholder;
		_known.Add(v);
		// Длинные значения заменяем первыми: иначе «server01.corp.local»
		// превратится в «<HOST-1>.corp.local».
		_known.Sort((a, b) => b.Length.CompareTo(a.Length));
	}

	/// <summary>Счётчики замен для манифеста.</summary>
	public Dictionary<string, object> Stats()
	{
		Dictionary<string, object> d = Json.Obj();
		foreach (KeyValuePair<string, int> kv in _counters)
		{
			// Ключи с подчёркиванием — служебные счётчики плейсхолдеров.
			if (!kv.Key.StartsWith("_", StringComparison.Ordinal))
			{
				d[kv.Key] = kv.Value;
			}
		}
		d["knownValues"] = _map.Count;
		return d;
	}

	/// <summary>Обезличивает текст. Идемпотентно: повторный прогон ничего не ломает.</summary>
	public string Redact(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		string s = text;

		// 1. Секреты — первыми. Если пароль похож на IP, он должен стать
		//    <SECRET>, а не <IP-1>: утечка важнее читаемости.
		foreach (Regex re in SecretPatterns)
		{
			s = re.Replace(s, m =>
			{
				Count("secrets");
				// Оставляем ключ, прячем значение: "password":"<SECRET>"
				if (m.Groups.Count > 1 && m.Groups[1].Success)
				{
					return m.Groups[1].Value + "\"<SECRET>\"";
				}
				return "<SECRET>";
			});
		}

		// 2. URL с учётными данными
		s = UrlCredsRe.Replace(s, m =>
		{
			Count("urlCredentials");
			return m.Groups[1].Value + "<CREDENTIALS>@";
		});

		// 3. Почта — до хостов и известных значений, иначе «admin@server.local»
		//    распадётся на части и превратится в «admin@<HOST>».
		s = EmailRe.Replace(s, m =>
		{
			Count("emails");
			return "<EMAIL>";
		});

		// 4. Доменные имена — до известных значений. Если сначала заменить
		//    компанию «secretcorp», от «fileserver.secretcorp.local» останется
		//    «fileserver.<COMPANYID-1>.local», и шаблон хоста уже не сработает.
		s = HostRe.Replace(s, m =>
		{
			// localhost — не идентификатор, он полезен для анализа.
			if (string.Equals(m.Value, "localhost", StringComparison.OrdinalIgnoreCase))
			{
				return m.Value;
			}
			Count("hosts");
			return "<HOST>";
		});

		// 5. Известные значения (компании, ВМ, люди, пути установки)
		foreach (string known in _known)
		{
			if (s.IndexOf(known, StringComparison.OrdinalIgnoreCase) < 0)
			{
				continue;
			}
			Regex re = new Regex(Regex.Escape(known), RegexOptions.IgnoreCase);
			s = re.Replace(s, _map[known]);
			Count("known");
		}

		// 6. Пути с именами пользователей
		s = UserPathRe.Replace(s, m =>
		{
			Count("userPaths");
			return m.Groups[1].Value + "<USER>";
		});

		// 7. UNC-пути (и обычные, и JSON-экранированные)
		s = UncRe.Replace(s, m =>
		{
			Count("uncPaths");
			// Сохраняем исходное число обратных косых: иначе JSON перестанет быть JSON.
			return m.Groups[1].Value + "<HOST>" + m.Groups[3].Value + "<SHARE>";
		});

		// 8. IPv6 раньше IPv4: полная форма IPv6 содержит точки только в ::ffff:x.x.x.x
		s = Ipv6Re.Replace(s, m =>
		{
			Count("ipv6");
			return "<IP6>";
		});

		// 9. IPv4
		s = Ipv4Re.Replace(s, m =>
		{
			// Версии вроде 1.0.0.0 не трогаем: это не адрес.
			if (LooksLikeVersion(m.Value))
			{
				return m.Value;
			}
			Count("ipv4");
			return "<IP>";
		});

		return s;
	}

	// ------------------------------------------------------------- утилиты

	private void Count(string category)
	{
		_counters.TryGetValue(category, out int n);
		_counters[category] = n + 1;
	}

	private string MakePlaceholder(string category)
	{
		string cat = string.IsNullOrEmpty(category) ? "VALUE" : category.ToUpperInvariant();
		_counters.TryGetValue("_" + cat, out int n);
		_counters["_" + cat] = n + 1;
		return "<" + cat + "-" + (n + 1).ToString(CultureInfo.InvariantCulture) + ">";
	}

	/// <summary>
	/// Слишком общие строки не заменяем: «admin», «root», «test» встречаются
	/// в текстах ошибок как роли и типы, и их замена убивает смысл лога.
	/// </summary>
	private static bool IsTooGeneric(string value)
	{
		string v = value.Trim().ToLowerInvariant();
		switch (v)
		{
		case "admin":
		case "root":
		case "user":
		case "test":
		case "guest":
		case "system":
		case "local":
		case "localhost":
		case "operator":
		case "viewer":
		case "client":
		case "backup":
		case "default":
			return true;
		}
		return v.Length < 3;
	}

	/// <summary>
	/// «1.0.0.0» — это версия сборки, а не IP-адрес. Но ошибиться здесь дороже,
	/// чем перестраховаться: пропущенный адрес — утечка, испорченная версия —
	/// косметика. Поэтому версией считаем только явный шаблон с двумя нулями
	/// в конце и маленьким первым числом.
	/// </summary>
	private static bool LooksLikeVersion(string ip)
	{
		string[] parts = ip.Split('.');
		if (parts.Length != 4)
		{
			return false;
		}
		int a;
		if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out a))
		{
			return false;
		}
		// 10.0.0.0 и 192.168.0.0 — это сети, их нельзя путать с версиями.
		if (a > 4)
		{
			return false;
		}
		return parts[2] == "0" && parts[3] == "0";
	}
}
