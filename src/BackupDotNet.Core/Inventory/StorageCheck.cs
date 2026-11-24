using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace BackupDotNet.Core.Inventory;

/// <summary>
/// Хранилища: доступность, свободное место, возможность записи.
///
/// Зачем отдельно от проверки дисков хоста: бэкап пишется не «на C:», а на
/// конкретную сетевую папку. И папка может быть недоступна по десятку причин —
/// упал сервер, кончились права, закрыли шару, — при этом сам агент жив
/// и молчит, пока бэкап не упадёт ночью.
///
/// Проверка честная: для сети делается реальная запись маленького файла
/// и чтение его обратно. «Папка видна» и «в неё можно писать» — разные вещи.
/// </summary>
public static class StorageCheck
{
	/// <summary>Проверенная цель.</summary>
	public class Target
	{
		public string Path = "";

		public string Label = "";

		/// <summary>UNC или локальный.</summary>
		public string Kind = "";

		public bool Ok;

		public string Error = "";

		public double TotalGb;

		public double FreeGb;

		public double FreePercent;

		/// <summary>Сколько заняла проверка доступа, миллисекунды.</summary>
		public long LatencyMs;

		/// <summary>Проверялась ли запись; null — не проверяли.</summary>
		public bool? Writable;

		/// <summary>Замечания, не мешающие работе (мало места и т. п.).</summary>
		public List<string> Warnings = new List<string>();

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["path"] = Path;
			d["label"] = Label;
			d["kind"] = Kind;
			d["ok"] = Ok;
			d["totalGb"] = Math.Round(TotalGb, 1);
			d["freeGb"] = Math.Round(FreeGb, 1);
			d["freePercent"] = Math.Round(FreePercent, 1);
			d["latencyMs"] = LatencyMs;
			if (Writable.HasValue)
			{
				d["writable"] = Writable.Value;
			}
			if (Error.Length > 0)
			{
				d["error"] = Error;
			}
			if (Warnings.Count > 0)
			{
				d["warnings"] = Warnings;
			}
			return d;
		}
	}

	/// <summary>iSCSI-сессия.</summary>
	public class IscsiSession
	{
		public string Target = "";

		public string Initiator = "";

		public string State = "";

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["target"] = Target;
			d["initiator"] = Initiator;
			d["state"] = State;
			return d;
		}
	}

	// ------------------------------------------------------------------ проверка

	/// <summary>
	/// Проверяет одну цель: доступность, место, при необходимости запись.
	/// </summary>
	/// <param name="path">Путь: UNC (<c>\\server\share</c>) или локальный.</param>
	/// <param name="writeTest">Писать ли пробный файл.</param>
	/// <param name="timeoutSeconds">Сколько ждать ответа от сети.</param>
	public static Target Check(string path, bool writeTest, int timeoutSeconds)
	{
		Target t = new Target();
		t.Path = (path ?? "").Trim();
		if (t.Path.Length == 0)
		{
			t.Error = "не указан путь";
			return t;
		}

		t.Kind = t.Path.StartsWith("\\\\", StringComparison.Ordinal) ? "UNC" : "локальный";
		if (timeoutSeconds <= 0)
		{
			timeoutSeconds = 15;
		}

		DateTime started = DateTime.Now;

		// Directory.Exists на мёртвом UNC висит десятками секунд, поэтому
		// проверка идёт в отдельной задаче с таймаутом.
		bool exists = false;
		bool timedOut = false;
		try
		{
			Task<bool> task = Task.Run(() => Directory.Exists(t.Path));
			if (task.Wait(TimeSpan.FromSeconds(timeoutSeconds)))
			{
				exists = task.Result;
			}
			else
			{
				timedOut = true;
			}
		}
		catch (Exception ex)
		{
			t.Error = ex.Message;
			t.LatencyMs = (long)(DateTime.Now - started).TotalMilliseconds;
			return t;
		}

		t.LatencyMs = (long)(DateTime.Now - started).TotalMilliseconds;

		if (timedOut)
		{
			t.Error = "нет ответа за " + timeoutSeconds + " с — сервер не отвечает или путь недоступен";
			return t;
		}
		if (!exists)
		{
			t.Error = "путь не найден или нет доступа";
			return t;
		}

		// Место: для UNC DriveInfo не работает, нужен Win32.
		if (!TryGetSpace(t.Path, out ulong free, out ulong total))
		{
			// Место узнать не удалось — это не повод считать цель недоступной.
			t.Ok = true;
			t.Warnings.Add("не удалось узнать свободное место");
		}
		else
		{
			t.TotalGb = total / 1073741824.0;
			t.FreeGb = free / 1073741824.0;
			t.FreePercent = total > 0 ? free * 100.0 / total : 0.0;
			t.Ok = true;

			if (t.FreePercent < 5)
			{
				t.Warnings.Add("свободно меньше 5% — бэкап может не поместиться");
			}
			else if (t.FreePercent < 15)
			{
				t.Warnings.Add("свободно меньше 15%");
			}
		}

		if (writeTest)
		{
			string writeError = TryWrite(t.Path);
			t.Writable = writeError.Length == 0;
			if (writeError.Length > 0)
			{
				t.Ok = false;
				t.Error = "нет записи: " + writeError;
			}
		}

		return t;
	}

	/// <summary>
	/// Проверяет все цели из настроек плюс каталог бэкапов.
	/// </summary>
	public static List<Target> CheckAll(Settings settings, bool writeTest)
	{
		List<Target> list = new List<Target>();
		if (settings == null)
		{
			return list;
		}

		List<string> paths = new List<string>();
		List<string> labels = new List<string>();

		// Каталог бэкапов — первым: он важнее прочих.
		string backupDir = (settings.BackupDestination ?? "").Trim();
		if (backupDir.Length > 0)
		{
			paths.Add(backupDir);
			labels.Add("каталог бэкапов");
		}

		foreach (StorageTarget st in settings.StorageTargets ?? new List<StorageTarget>())
		{
			if (st == null || string.IsNullOrWhiteSpace(st.Path))
			{
				continue;
			}
			string p = st.Path.Trim();
			bool duplicate = false;
			foreach (string existing in paths)
			{
				if (string.Equals(existing, p, StringComparison.OrdinalIgnoreCase))
				{
					duplicate = true;
					break;
				}
			}
			if (duplicate)
			{
				continue;
			}
			paths.Add(p);
			labels.Add(st.Label ?? "");
		}

		for (int i = 0; i < paths.Count; i++)
		{
			Target t = Check(paths[i], writeTest, 15);
			t.Label = labels[i];
			list.Add(t);
		}
		return list;
	}

	// ------------------------------------------------------------------ запись

	/// <summary>
	/// Пробная запись: создать файл, прочитать его обратно, удалить.
	/// Возвращает текст ошибки или пустую строку.
	/// </summary>
	private static string TryWrite(string dir)
	{
		string file = Path.Combine(dir, ".bnd-write-test-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".tmp");
		try
		{
			byte[] payload = System.Text.Encoding.UTF8.GetBytes("BackupDotNet write test " + DateTime.Now.ToString("o"));
			File.WriteAllBytes(file, payload);

			byte[] read = File.ReadAllBytes(file);
			if (read.Length != payload.Length)
			{
				return "файл записан не полностью (" + read.Length + " из " + payload.Length + " байт)";
			}
			return "";
		}
		catch (UnauthorizedAccessException)
		{
			return "нет прав на запись";
		}
		catch (IOException ex)
		{
			return ExplainIoError(ex);
		}
		catch (Exception ex)
		{
			return ex.Message;
		}
		finally
		{
			try
			{
				if (File.Exists(file))
				{
					File.Delete(file);
				}
			}
			catch
			{
				// Не смогли убрать за собой — не беда, но и не повод падать.
			}
		}
	}

	private static string ExplainIoError(IOException ex)
	{
		string msg = ex.Message ?? "";
		if (msg.IndexOf("not enough space", StringComparison.OrdinalIgnoreCase) >= 0 ||
			msg.IndexOf("недостаточно места", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "на диске нет места";
		}
		if (msg.IndexOf("network", StringComparison.OrdinalIgnoreCase) >= 0 ||
			msg.IndexOf("сетев", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "обрыв сетевого соединения";
		}
		return msg;
	}

	/// <summary>
	/// Свободное и общее место. Для UNC <see cref="DriveInfo"/> не работает,
	/// поэтому вызывается Win32 напрямую — он есть в любой Windows.
	/// </summary>
	private static bool TryGetSpace(string path, out ulong free, out ulong total)
	{
		free = 0;
		total = 0;
		try
		{
			if (GetDiskFreeSpaceEx(path, out ulong freeBytes, out ulong totalBytes, out ulong totalFree))
			{
				free = freeBytes;
				total = totalBytes;
				return total > 0;
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	private static extern bool GetDiskFreeSpaceEx(string directory, out ulong freeBytesAvailable,
		out ulong totalBytes, out ulong totalFreeBytes);

	// ------------------------------------------------------------------ iSCSI

	/// <summary>
	/// Активные iSCSI-сессии. Если инициатор не настроен — пустой список,
	/// и это нормальное состояние, а не ошибка.
	/// </summary>
	public static List<IscsiSession> IscsiSessions(out string note)
	{
		note = "";
		List<IscsiSession> list = new List<IscsiSession>();
		try
		{
			ManagementObjectCollection coll = Hw.Query("root\\wmi", "SELECT * FROM MSiSCSIInitiator_SessionClass");
			if (coll == null)
			{
				note = "iSCSI-инициатор недоступен";
				return list;
			}
			try
			{
				foreach (ManagementBaseObject o in coll)
				{
					using (o)
					{
						IscsiSession s = new IscsiSession();
						s.Target = Hw.Prop(o, "TargetName");
						s.Initiator = Hw.Prop(o, "InitiatorName");
						s.State = Hw.Prop(o, "SessionState");
						if (s.Target.Length > 0 || s.Initiator.Length > 0)
						{
							list.Add(s);
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

			if (list.Count == 0)
			{
				note = "iSCSI-сессий нет";
			}
		}
		catch (Exception ex)
		{
			note = "iSCSI: " + ex.Message;
		}
		return list;
	}
}
