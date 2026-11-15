using System;
using System.Collections.Generic;

namespace BackupDotNet.Core.Backup;

/// <summary>
/// Одно задание резервного копирования.
///
/// Задание живёт в очереди и выполняется фоновым потоком: экспорт ВМ занимает
/// минуты и десятки гигабайт, держать на этом HTTP-запрос нельзя.
/// </summary>
public class BackupJob
{
	/// <summary>Короткий идентификатор: по нему задание ищут в интерфейсе.</summary>
	public string Id = "";

	/// <summary>Движок, который выполняет задание: <c>hyperv-export</c> и т.п.</summary>
	public string Engine = "";

	/// <summary>Что копируем: имя ВМ, путь к базе, имя задания.</summary>
	public string Target = "";

	/// <summary>Куда пишем.</summary>
	public string Destination = "";

	/// <summary>pending | running | done | failed | cancelled</summary>
	public string Status = "pending";

	/// <summary>0..100. Для движков без прогресса остаётся 0 до завершения.</summary>
	public int Progress;

	/// <summary>Что происходит сейчас — человеческим языком.</summary>
	public string Message = "";

	/// <summary>Итог: размер результата в байтах.</summary>
	public long SizeBytes;

	/// <summary>Сколько заняло.</summary>
	public long DurationMs;

	public string RequestedBy = "";

	public DateTime CreatedAt = DateTime.Now;

	public DateTime? StartedAt;

	public DateTime? FinishedAt;

	/// <summary>Ошибка, если задание провалилось.</summary>
	public string Error = "";

	/// <summary>Снимок настроек, с которыми запускали: движок мог измениться позже.</summary>
	public Dictionary<string, string> Options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	/// <summary>Признак отмены. Движок обязан проверять его в цикле ожидания.</summary>
	internal volatile bool CancelRequested;

	public bool IsFinished => Status == "done" || Status == "failed" || Status == "cancelled";

	public Dictionary<string, object> ToJson()
	{
		Dictionary<string, object> d = Json.Obj();
		d["id"] = Id;
		d["engine"] = Engine;
		d["target"] = Target;
		d["destination"] = Destination;
		d["status"] = Status;
		d["progress"] = Progress;
		d["message"] = Message;
		d["sizeBytes"] = SizeBytes;
		d["durationMs"] = DurationMs;
		d["requestedBy"] = RequestedBy;
		d["createdAt"] = CreatedAt;
		d["startedAt"] = StartedAt;
		d["finishedAt"] = FinishedAt;
		if (Error.Length > 0)
		{
			d["error"] = Error;
		}
		if (Options.Count > 0)
		{
			Dictionary<string, object> o = Json.Obj();
			foreach (KeyValuePair<string, string> kv in Options)
			{
				o[kv.Key] = kv.Value;
			}
			d["options"] = o;
		}
		return d;
	}

	public static BackupJob FromJson(Dictionary<string, object> d)
	{
		BackupJob j = new BackupJob();
		j.Id = Json.GetStr(d, "id", "");
		j.Engine = Json.GetStr(d, "engine", "");
		j.Target = Json.GetStr(d, "target", "");
		j.Destination = Json.GetStr(d, "destination", "");
		j.Status = Json.GetStr(d, "status", "pending");
		j.Progress = Json.Int(Json.Get(d, "progress"), 0);
		j.Message = Json.GetStr(d, "message", "");
		j.SizeBytes = Json.Long(Json.Get(d, "sizeBytes"), 0L);
		j.DurationMs = Json.Long(Json.Get(d, "durationMs"), 0L);
		j.RequestedBy = Json.GetStr(d, "requestedBy", "");
		j.Error = Json.GetStr(d, "error", "");
		j.CreatedAt = BackupRun.ParseDate(Json.Get(d, "createdAt"));
		object st = Json.Get(d, "startedAt");
		if (st != null)
		{
			j.StartedAt = BackupRun.ParseDate(st);
		}
		object fin = Json.Get(d, "finishedAt");
		if (fin != null)
		{
			j.FinishedAt = BackupRun.ParseDate(fin);
		}
		Dictionary<string, object> o = Json.AsObject(Json.Get(d, "options"));
		foreach (KeyValuePair<string, object> kv in o)
		{
			j.Options[kv.Key] = Json.Str(kv.Value);
		}
		return j;
	}
}
