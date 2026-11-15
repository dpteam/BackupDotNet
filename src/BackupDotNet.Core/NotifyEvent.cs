using System;
using System.Collections.Generic;
using System.Globalization;

namespace BackupDotNet.Core;

/// <summary>
/// Событие, о котором стоит уведомить человека.
///
/// Событие создаётся только на <b>смене</b> состояния: переход в FAIL, в WARN,
/// либо восстановление. Повторять одно и то же каждую минуту — верный способ
/// добиться, чтобы уведомления перестали читать.
/// </summary>
public class NotifyEvent
{
	/// <summary>Вид перехода: fail, warn, recover, nodata.</summary>
	public string Kind = "";

	public string CompanyId = "";

	public string CompanyName = "";

	public string Job = "";

	public string SourceKind = "";

	public BackupStatus Previous = BackupStatus.Skip;

	public BackupStatus Current = BackupStatus.Ok;

	public string Message = "";

	public string Target = "";

	public DateTime Timestamp = DateTime.Now;

	/// <summary>Сколько неудач подряд (для «падает третий раз»).</summary>
	public int ConsecutiveFailures;

	/// <summary>Заголовок письма и первая строка сообщения.</summary>
	public string Title
	{
		get
		{
			string subject = (Kind == "recover") ? "восстановлено"
				: (Kind == "warn") ? "предупреждение"
				: (Kind == "nodata") ? "нет данных"
				: "сбой";
			return "[" + SubjectPrefix + "] " + subject + ": " + Job;
		}
	}

	/// <summary>Префикс темы: имя продукта или компании — настраивается.</summary>
	public string SubjectPrefix = "Backup";

	/// <summary>Текст сообщения: сначала главное, потом подробности.</summary>
	public string Body
	{
		get
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.AppendLine(Title);
			sb.AppendLine();
			sb.AppendLine("Компания:  " + CompanyName + " (" + CompanyId + ")");
			sb.AppendLine("Задание:   " + Job);
			if (!string.IsNullOrEmpty(SourceKind))
			{
				sb.AppendLine("Тип:       " + SourceKind);
			}
			if (!string.IsNullOrEmpty(Target))
			{
				sb.AppendLine("Объект:    " + Target);
			}
			sb.AppendLine("Было:      " + StatusNames.Of(Previous));
			sb.AppendLine("Стало:     " + StatusNames.Of(Current));
			if (ConsecutiveFailures > 1)
			{
				sb.AppendLine("Подряд:    " + ConsecutiveFailures + " неудач");
			}
			if (!string.IsNullOrEmpty(Message))
			{
				sb.AppendLine();
				sb.AppendLine(Message);
			}
			sb.AppendLine();
			sb.AppendLine("Время:     " + Timestamp.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture));
			return sb.ToString();
		}
	}

	/// <summary>Короткая строка для Telegram и журнала.</summary>
	public string ShortText
	{
		get
		{
			string icon = (Kind == "recover") ? "✅" : (Kind == "warn") ? "⚠️" : (Kind == "nodata") ? "❔" : "🔴";
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.Append(icon).Append(' ').Append(SubjectPrefix).Append(": ").Append(Job);
			sb.Append('\n').Append(CompanyName);
			if (!string.IsNullOrEmpty(Message))
			{
				sb.Append('\n').Append(Message);
			}
			if (ConsecutiveFailures > 1)
			{
				sb.Append("\nнеудач подряд: ").Append(ConsecutiveFailures);
			}
			sb.Append('\n').Append(Timestamp.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture));
			return sb.ToString();
		}
	}

	/// <summary>Ключ для подавления повторов: одно задание + один вид события.</summary>
	public string ThrottleKey => (CompanyId + "|" + Job + "|" + Kind).ToLowerInvariant();

	public Dictionary<string, object> ToJson()
	{
		Dictionary<string, object> d = Json.Obj();
		d["kind"] = Kind;
		d["companyId"] = CompanyId;
		d["companyName"] = CompanyName;
		d["job"] = Job;
		d["sourceKind"] = SourceKind;
		d["previous"] = (int)Previous;
		d["current"] = (int)Current;
		d["message"] = Message;
		d["target"] = Target;
		d["timestamp"] = Timestamp;
		d["consecutiveFailures"] = ConsecutiveFailures;
		d["title"] = Title;
		return d;
	}

	public static NotifyEvent FromJson(Dictionary<string, object> d)
	{
		NotifyEvent e = new NotifyEvent();
		e.Kind = Json.GetStr(d, "kind", "");
		e.CompanyId = Json.GetStr(d, "companyId", "");
		e.CompanyName = Json.GetStr(d, "companyName", "");
		e.Job = Json.GetStr(d, "job", "");
		e.SourceKind = Json.GetStr(d, "sourceKind", "");
		e.Previous = (BackupStatus)Json.Int(Json.Get(d, "previous"), 0);
		e.Current = (BackupStatus)Json.Int(Json.Get(d, "current"), 0);
		e.Message = Json.GetStr(d, "message", "");
		e.Target = Json.GetStr(d, "target", "");
		e.ConsecutiveFailures = Json.Int(Json.Get(d, "consecutiveFailures"), 0);
		try
		{
			string ts = Json.GetStr(d, "timestamp", "");
			if (ts.Length > 0)
			{
				e.Timestamp = DateTime.Parse(ts, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal).ToLocalTime();
			}
		}
		catch
		{
		}
		return e;
	}
}
