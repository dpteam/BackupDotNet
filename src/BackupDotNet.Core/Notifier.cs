using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Text;

namespace BackupDotNet.Core;

/// <summary>
/// Уведомления о смене состояния заданий: SMTP и Telegram.
///
/// Принципы, ради которых всё так устроено:
///  • <b>уведомляем только о переходах</b>, а не о каждом прогоне. Иначе канал
///    превращается в шум и его отключают — а вместе с ним и настоящие аварии;
///  • <b>подавление повторов</b>: одно и то же событие по одному заданию не чаще
///    <c>minIntervalMinutes</c>;
///  • <b>тихие часы</b>: ночью не будим, кроме восстановления;
///  • <b>очередь на диске</b>: если SMTP или Telegram недоступны, событие не
///    теряется, а досылается позже;
///  • <b>никаких внешних библиотек</b>: SMTP — из <c>System.Net.Mail</c>,
///    Telegram — обычный HTTPS-запрос к Bot API.
/// </summary>
public class Notifier
{
	private readonly Settings _settings;

	private readonly ConfigLoader _config;

	private readonly string _queuePath;

	private readonly object _sync = new object();

	/// <summary>Время последней отправки по ключу «компания|задание|вид».</summary>
	private readonly Dictionary<string, DateTime> _lastSent = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

	private readonly string _throttlePath;

	private DateTime _throttleSaved = DateTime.MinValue;

	/// <summary>Счётчики для страницы настроек и диагностики.</summary>
	public long SentCount;

	public long FailedCount;

	public long SuppressedCount;

	public DateTime? LastSentAt;

	public string LastError = "";

	public Notifier(Settings settings, ConfigLoader config)
	{
		_settings = settings;
		_config = config;
		_queuePath = Path.Combine(settings.DataDir, "state", "notify-queue.ndjson");
		_throttlePath = Path.Combine(settings.DataDir, "state", "notify-throttle.json");
		LoadThrottle();
	}

	/// <summary>Включён ли хотя бы один канал.</summary>
	public bool AnyChannelEnabled
	{
		get
		{
			if (!_settings.NotifyEnabled)
			{
				return false;
			}
			return SmtpConfigured || TelegramConfigured;
		}
	}

	public bool SmtpConfigured
	{
		get
		{
			return _settings.NotifySmtpEnabled &&
				!string.IsNullOrEmpty(_settings.NotifySmtpHost) &&
				!string.IsNullOrEmpty(_settings.NotifySmtpFrom) &&
				_settings.NotifySmtpTo.Count > 0;
		}
	}

	public bool TelegramConfigured
	{
		get
		{
			return _settings.NotifyTelegramEnabled &&
				!string.IsNullOrEmpty(TelegramToken) &&
				_settings.NotifyTelegramChats.Count > 0;
		}
	}

	private string TelegramToken
	{
		get
		{
			if (!string.IsNullOrEmpty(_settings.NotifyTelegramTokenSecret))
			{
				string v = _config?.Secret(_settings.NotifyTelegramTokenSecret) ?? "";
				if (v.Length > 0)
				{
					return v;
				}
			}
			return _settings.NotifyTelegramToken ?? "";
		}
	}

	private string SmtpPassword
	{
		get
		{
			if (!string.IsNullOrEmpty(_settings.NotifySmtpPasswordSecret))
			{
				string v = _config?.Secret(_settings.NotifySmtpPasswordSecret) ?? "";
				if (v.Length > 0)
				{
					return v;
				}
			}
			return _settings.NotifySmtpPassword ?? "";
		}
	}

	// ------------------------------------------------------------------ правила

	/// <summary>
	/// Решает, нужно ли уведомлять о переходе, и создаёт событие.
	/// Возвращает <c>null</c>, если перехода нет или он не интересен.
	/// </summary>
	public NotifyEvent BuildEvent(Company company, bool hadPrev, BackupStatus was, string prevTarget, BackupRun run)
	{
		if (!AnyChannelEnabled)
		{
			return null;
		}
		// prev отсутствует — «задание видим впервые». Это не повод молчать:
		// первый же провал — самая важная новость. Но и «восстановлением» он быть не может.
		bool firstSeen = !hadPrev;
		BackupStatus now = run.Status;
		if (!firstSeen && was == now)
		{
			return null;
		}

		string kind = null;
		if (now == BackupStatus.Fail && _settings.NotifyOnFail)
		{
			kind = "fail";
		}
		else if (now == BackupStatus.Warn && _settings.NotifyOnWarn)
		{
			kind = "warn";
		}
		else if (now == BackupStatus.NoData && _settings.NotifyOnNoData)
		{
			kind = "nodata";
		}
		else if (!firstSeen && now == BackupStatus.Ok && _settings.NotifyOnRecover &&
			(was == BackupStatus.Fail || was == BackupStatus.Warn || was == BackupStatus.NoData))
		{
			kind = "recover";
		}
		if (kind == null)
		{
			return null;
		}

		NotifyEvent e = new NotifyEvent();
		e.Kind = kind;
		e.CompanyId = company.Id;
		e.CompanyName = company.Name;
		e.Job = run.Job;
		e.SourceKind = run.Kind;
		e.Previous = was;
		e.Current = now;
		e.Message = run.Message;
		e.Target = prevTarget ?? "";
		e.Timestamp = run.Timestamp;
		e.ConsecutiveFailures = run.ConsecutiveFailures;
		e.SubjectPrefix = string.IsNullOrEmpty(_settings.AgentName) ? Branding.Current.ProductName : _settings.AgentName;
		if (firstSeen)
		{
			e.Message = string.IsNullOrEmpty(e.Message)
				? "задание появилось впервые и сразу в состоянии " + StatusNames.Of(now)
				: e.Message + " (задание появилось впервые)";
		}
		return e;
	}

	/// <summary>Проверяет подавление повторов и тихие часы. Не отправляет.</summary>
	public bool ShouldSend(NotifyEvent e, out string reason)
	{
		reason = "";
		int minInterval = Math.Max(0, _settings.NotifyMinIntervalMinutes);
		if (minInterval > 0)
		{
			lock (_sync)
			{
				if (_lastSent.TryGetValue(e.ThrottleKey, out var last))
				{
					double mins = (DateTime.Now - last).TotalMinutes;
					if (mins < (double)minInterval)
					{
						reason = "повтор через " + (int)mins + " из " + minInterval + " мин";
						return false;
					}
				}
			}
		}

		// В тихие часы не будим. Восстановление — исключение: хорошие новости можно.
		if (e.Kind != "recover" && InQuietHours(_settings.NotifyQuietFrom, _settings.NotifyQuietTo))
		{
			reason = "тихие часы " + _settings.NotifyQuietFrom + "–" + _settings.NotifyQuietTo;
			return false;
		}
		return true;
	}

	/// <summary>
	/// Отправляет событие. Возвращает пустую строку при успехе,
	/// иначе — описание проблемы (и ставит событие в очередь на досылку).
	/// </summary>
	public string Send(NotifyEvent e)
	{
		string suppress;
		if (!ShouldSend(e, out suppress))
		{
			SuppressedCount++;
			Log.Debug("Уведомление подавлено (" + suppress + "): " + e.Title);
			return "";
		}

		List<string> errors = new List<string>();

		if (SmtpConfigured)
		{
			string err = SendSmtp(e);
			if (err.Length > 0)
			{
				errors.Add("SMTP: " + err);
			}
		}
		if (TelegramConfigured)
		{
			string err = SendTelegram(e);
			if (err.Length > 0)
			{
				errors.Add("Telegram: " + err);
			}
		}

		if (errors.Count > 0)
		{
			FailedCount++;
			LastError = string.Join("; ", errors.ToArray());
			Log.Warn("Уведомление не отправлено: " + LastError);
			Enqueue(e);
			return LastError;
		}

		lock (_sync)
		{
			_lastSent[e.ThrottleKey] = DateTime.Now;
		}
		SaveThrottle();
		SentCount++;
		LastSentAt = DateTime.Now;
		LastError = "";
		Log.Info("Уведомление отправлено: " + e.Title);
		return "";
	}

	// ------------------------------------------------------------------ SMTP

	private string SendSmtp(NotifyEvent e)
	{
		try
		{
			using MailMessage msg = new MailMessage();
			msg.From = new MailAddress(_settings.NotifySmtpFrom);
			foreach (string to in _settings.NotifySmtpTo)
			{
				string addr = (to ?? "").Trim();
				if (addr.Length == 0)
				{
					continue;
				}
				try
				{
					msg.To.Add(new MailAddress(addr));
				}
				catch
				{
					Log.Warn("Уведомления: некорректный адрес «" + addr + "» — пропущен");
				}
			}
			if (msg.To.Count == 0)
			{
				return "не задан ни один корректный адрес получателя";
			}
			msg.Subject = e.Title;
			msg.Body = e.Body;
			msg.SubjectEncoding = Encoding.UTF8;
			msg.BodyEncoding = Encoding.UTF8;
			msg.IsBodyHtml = false;
			msg.Headers.Add("X-Mailer", Branding.Current.ProductName);

			using SmtpClient client = new SmtpClient(_settings.NotifySmtpHost, _settings.NotifySmtpPort);
			client.EnableSsl = _settings.NotifySmtpUseSsl;
			client.Timeout = Math.Max(5, _settings.NotifySmtpTimeoutSeconds) * 1000;
			string pwd = SmtpPassword;
			if (!string.IsNullOrEmpty(_settings.NotifySmtpUser))
			{
				client.Credentials = new NetworkCredential(_settings.NotifySmtpUser, pwd);
			}
			else
			{
				client.UseDefaultCredentials = false;
			}
			client.Send(msg);
			return "";
		}
		catch (Exception ex)
		{
			return ex.Message;
		}
	}

	// -------------------------------------------------------------- Telegram

	private string SendTelegram(NotifyEvent e)
	{
		string token = TelegramToken;
		if (token.Length == 0)
		{
			return "не задан токен бота";
		}
		List<string> errors = new List<string>();
		foreach (string chat in _settings.NotifyTelegramChats)
		{
			string id = (chat ?? "").Trim();
			if (id.Length == 0)
			{
				continue;
			}
			string err = TelegramSendMessage(token, id, e.ShortText);
			if (err.Length > 0)
			{
				errors.Add(id + ": " + err);
			}
		}
		return string.Join("; ", errors.ToArray());
	}

	/// <summary>Один вызов Bot API. Отдельный метод — его же использует кнопка «Проверить».</summary>
	public string TelegramSendMessage(string token, string chatId, string text)
	{
		try
		{
			string url = "https://api.telegram.org/bot" + token + "/sendMessage";
			string body = "chat_id=" + Uri.EscapeDataString(chatId) +
				"&text=" + Uri.EscapeDataString(text) +
				"&disable_web_page_preview=true";
			string response = HttpPost(url, body, Math.Max(5, _settings.NotifySmtpTimeoutSeconds) * 1000);
			// Bot API отвечает {"ok":true,...} либо {"ok":false,"description":"..."}
			Dictionary<string, object> d = Json.ParseObject(response);
			if (Json.Bool(Json.Get(d, "ok"), dflt: false))
			{
				return "";
			}
			string desc = Json.GetStr(d, "description", "");
			return (desc.Length > 0) ? desc : "неожиданный ответ Telegram";
		}
		catch (Exception ex)
		{
			return ex.Message;
		}
	}

	private static string HttpPost(string url, string body, int timeoutMs)
	{
		ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
		HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
		req.Method = "POST";
		req.ContentType = "application/x-www-form-urlencoded";
		req.Timeout = timeoutMs;
		req.ReadWriteTimeout = timeoutMs;
		req.UserAgent = Branding.Current.ProductName + "-Notifier";
		req.KeepAlive = false;
		byte[] data = Encoding.UTF8.GetBytes(body);
		req.ContentLength = data.Length;
		using (Stream s = req.GetRequestStream())
		{
			s.Write(data, 0, data.Length);
		}
		try
		{
			using HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
			using StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8);
			return sr.ReadToEnd();
		}
		catch (WebException ex)
		{
			// Ошибки Bot API приходят с кодом 400 и телом с описанием — читаем его.
			if (ex.Response is HttpWebResponse err)
			{
				try
				{
					using StreamReader sr = new StreamReader(err.GetResponseStream(), Encoding.UTF8);
					string text = sr.ReadToEnd();
					if (text.Length > 0)
					{
						return text;
					}
				}
				catch
				{
				}
			}
			throw;
		}
	}

	// ------------------------------------------------------- подавление повторов

	/// <summary>
	/// Загружает отметки о последних отправках. Нужно, чтобы перезапуск агента
	/// не превращался в повторную рассылку уже известных проблем.
	/// </summary>
	private void LoadThrottle()
	{
		try
		{
			if (!File.Exists(_throttlePath))
			{
				return;
			}
			Dictionary<string, object> root = Json.ParseObject(File.ReadAllText(_throttlePath, Encoding.UTF8));
			DateTime cutoff = DateTime.Now.AddDays(-7);
			foreach (KeyValuePair<string, object> kv in root)
			{
				try
				{
					DateTime when = DateTime.Parse(Json.Str(kv.Value), CultureInfo.InvariantCulture,
						DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal).ToLocalTime();
					if (when > cutoff)
					{
						_lastSent[kv.Key] = when;
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}

	/// <summary>Сохраняет отметки. Не чаще раза в 30 секунд — незачем чаще.</summary>
	private void SaveThrottle()
	{
		if ((DateTime.Now - _throttleSaved).TotalSeconds < 30.0)
		{
			return;
		}
		_throttleSaved = DateTime.Now;
		try
		{
			Dictionary<string, object> root = Json.Obj();
			DateTime cutoff = DateTime.Now.AddDays(-7);
			lock (_sync)
			{
				foreach (KeyValuePair<string, DateTime> kv in _lastSent)
				{
					if (kv.Value > cutoff)
					{
						root[kv.Key] = kv.Value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
					}
				}
			}
			string dir = Path.GetDirectoryName(_throttlePath);
			if (!string.IsNullOrEmpty(dir))
			{
				Directory.CreateDirectory(dir);
			}
			ConfigLoader.AtomicWrite(_throttlePath, Json.WritePretty(root));
		}
		catch
		{
		}
	}

	// ------------------------------------------------------------------ очередь
	private void Enqueue(NotifyEvent e)
	{
		try
		{
			string dir = Path.GetDirectoryName(_queuePath);
			if (!string.IsNullOrEmpty(dir))
			{
				Directory.CreateDirectory(dir);
			}
			File.AppendAllText(_queuePath, Json.Write(e.ToJson()) + "\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		}
		catch (Exception ex)
		{
			Log.Warn("Уведомления: не удалось поставить в очередь — " + ex.Message);
		}
	}

	/// <summary>
	/// Пытается дослать накопленные события. Возвращает число отправленных.
	/// Ограничение: не больше 50 за раз, чтобы не блокировать планировщик.
	/// </summary>
	public int FlushQueue()
	{
		if (!AnyChannelEnabled || !File.Exists(_queuePath))
		{
			return 0;
		}
		string[] lines;
		try
		{
			lines = File.ReadAllLines(_queuePath);
		}
		catch
		{
			return 0;
		}
		if (lines.Length == 0)
		{
			try
			{
				File.Delete(_queuePath);
			}
			catch
			{
			}
			return 0;
		}

		List<string> remaining = new List<string>();
		int sent = 0;
		int attempts = 0;
		foreach (string line in lines)
		{
			if (string.IsNullOrWhiteSpace(line))
			{
				continue;
			}
			if (attempts >= 50)
			{
				remaining.Add(line);
				continue;
			}
			attempts++;
			NotifyEvent e;
			try
			{
				e = NotifyEvent.FromJson(Json.ParseObject(line));
			}
			catch
			{
				continue; // битую строку выбрасываем
			}
			string err = "";
			if (SmtpConfigured)
			{
				err = SendSmtp(e);
			}
			if (err.Length == 0 && TelegramConfigured)
			{
				err = SendTelegram(e);
			}
			if (err.Length == 0)
			{
				sent++;
				SentCount++;
				LastSentAt = DateTime.Now;
			}
			else
			{
				remaining.Add(line);
			}
		}

		try
		{
			if (remaining.Count == 0)
			{
				File.Delete(_queuePath);
			}
			else
			{
				File.WriteAllLines(_queuePath, remaining.ToArray(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			}
		}
		catch
		{
		}
		if (sent > 0)
		{
			Log.Info("Уведомления: дослано из очереди " + sent);
		}
		return sent;
	}

	/// <summary>Сколько событий ждёт отправки.</summary>
	public int QueueLength
	{
		get
		{
			try
			{
				if (!File.Exists(_queuePath))
				{
					return 0;
				}
				int n = 0;
				foreach (string line in File.ReadAllLines(_queuePath))
				{
					if (!string.IsNullOrWhiteSpace(line))
					{
						n++;
					}
				}
				return n;
			}
			catch
			{
				return 0;
			}
		}
	}

	// ------------------------------------------------------------------ тесты

	/// <summary>
	/// Пробное уведомление: проверяет настройки, не дожидаясь реальной аварии.
	/// Возвращает пустую строку при успехе.
	/// </summary>
	public string SendTest()
	{
		if (!_settings.NotifyEnabled)
		{
			return "уведомления выключены (notifications.enabled = false)";
		}
		if (!SmtpConfigured && !TelegramConfigured)
		{
			return "ни один канал не настроен полностью";
		}

		NotifyEvent e = new NotifyEvent();
		e.Kind = "test";
		e.CompanyId = "test";
		e.CompanyName = "Проверка настроек";
		e.Job = "Тестовое уведомление";
		e.SourceKind = "test";
		e.Previous = BackupStatus.Ok;
		e.Current = BackupStatus.Fail;
		e.Message = "Это проверка канала уведомлений. Если вы её видите — всё настроено верно.";
		e.Timestamp = DateTime.Now;
		e.SubjectPrefix = string.IsNullOrEmpty(_settings.AgentName) ? Branding.Current.ProductName : _settings.AgentName;

		List<string> errors = new List<string>();
		if (SmtpConfigured)
		{
			string err = SendSmtp(e);
			if (err.Length > 0)
			{
				errors.Add("SMTP: " + err);
			}
		}
		if (TelegramConfigured)
		{
			string err = SendTelegram(e);
			if (err.Length > 0)
			{
				errors.Add("Telegram: " + err);
			}
		}
		if (errors.Count > 0)
		{
			LastError = string.Join("; ", errors.ToArray());
			return LastError;
		}
		LastError = "";
		Log.Info("Уведомления: тестовое сообщение отправлено");
		return "";
	}

	/// <summary>Состояние для страницы настроек и диагностики.</summary>
	public Dictionary<string, object> Status()
	{
		Dictionary<string, object> d = Json.Obj();
		d["enabled"] = _settings.NotifyEnabled;
		d["smtp"] = SmtpConfigured;
		d["telegram"] = TelegramConfigured;
		d["smtpHost"] = _settings.NotifySmtpHost;
		d["smtpPort"] = _settings.NotifySmtpPort;
		d["smtpFrom"] = _settings.NotifySmtpFrom;
		d["smtpTo"] = ToObjectList(_settings.NotifySmtpTo);
		d["telegramChats"] = ToObjectList(_settings.NotifyTelegramChats);
		d["onFail"] = _settings.NotifyOnFail;
		d["onWarn"] = _settings.NotifyOnWarn;
		d["onRecover"] = _settings.NotifyOnRecover;
		d["onNoData"] = _settings.NotifyOnNoData;
		d["minIntervalMinutes"] = _settings.NotifyMinIntervalMinutes;
		d["quietFrom"] = _settings.NotifyQuietFrom;
		d["quietTo"] = _settings.NotifyQuietTo;
		d["queueLength"] = QueueLength;
		d["sent"] = SentCount;
		d["failed"] = FailedCount;
		d["suppressed"] = SuppressedCount;
		d["lastSentAt"] = LastSentAt;
		d["lastError"] = LastError;
		return d;
	}

	private static List<object> ToObjectList(List<string> items)
	{
		List<object> list = new List<object>();
		if (items != null)
		{
			foreach (string s in items)
			{
				list.Add(s);
			}
		}
		return list;
	}

	/// <summary>
	/// Попадает ли текущее время в тихие часы. Поддерживает переход через полночь
	/// (<c>22:00–07:00</c>) и пустые значения (тихие часы выключены).
	/// </summary>
	public static bool InQuietHours(string from, string to)
	{
		TimeSpan start;
		TimeSpan end;
		if (!TryParseTime(from, out start) || !TryParseTime(to, out end))
		{
			return false;
		}
		if (start == end)
		{
			return false;
		}
		TimeSpan now = DateTime.Now.TimeOfDay;
		if (start < end)
		{
			return now >= start && now < end;
		}
		// через полночь
		return now >= start || now < end;
	}

	private static bool TryParseTime(string s, out TimeSpan t)
	{
		t = TimeSpan.Zero;
		if (string.IsNullOrWhiteSpace(s))
		{
			return false;
		}
		return TimeSpan.TryParseExact(s.Trim(), new string[2] { @"h\:mm", @"hh\:mm" },
			CultureInfo.InvariantCulture, out t);
	}
}
