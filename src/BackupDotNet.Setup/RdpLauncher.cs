using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using BackupDotNet.Core;

namespace BackupDotNet.Setup;

/// <summary>
/// Подключение по RDP к целевой машине, когда удалённые способы недоступны.
///
/// Что делает: готовит <c>.rdp</c>-файл с нужным адресом и запускает
/// <c>mstsc</c>. Пароль при этом вводит человек в стандартном окне Windows.
///
/// Почему не подставляем учётные данные автоматически: Windows не даёт
/// программынй вход по RDP с паролем — и правильно делает, иначе любой
/// установщик мог бы молча зайти на чужую машину. Единственный обход —
/// <c>cmdkey</c>, который кладёт пароль в хранилище учётных данных навсегда.
/// Установщик этого не делает: цена удобства тут выше, чем польза.
/// </summary>
public static class RdpLauncher
{
	/// <summary>Результат подготовки подключения.</summary>
	public class Outcome
	{
		public bool Ok;

		public string Message = "";

		public string RdpPath = "";

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["ok"] = Ok;
			d["message"] = Message;
			if (RdpPath.Length > 0)
			{
				d["rdpPath"] = RdpPath;
			}
			if (!Ok)
			{
				d["error"] = Message;
			}
			return d;
		}
	}

	/// <summary>
	/// Готовит .rdp-файл и (если <paramref name="launch"/>) запускает mstsc.
	/// </summary>
	/// <param name="host">Целевая машина.</param>
	/// <param name="user">Имя пользователя: попадёт в файл, пароль — нет.</param>
	/// <param name="adminSession">
	/// Подключаться к консольной сессии администратора (<c>/admin</c>).
	/// </param>
	/// <param name="launch">Запускать ли mstsc сразу.</param>
	public static Outcome Prepare(string host, string user, bool adminSession, bool launch)
	{
		Outcome o = new Outcome();
		string h = (host ?? "").Trim().TrimStart('\\');

		if (h.Length == 0)
		{
			o.Message = "не указан компьютер";
			return o;
		}

		// Локальный адрес: RDP к самому себе бессмысленен.
		if (h.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
			h.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
			h.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase))
		{
			o.Message = "это эта же машина — RDP не нужен, запустите скрипт локально";
			return o;
		}

		try
		{
			string dir = Path.Combine(Path.GetTempPath(), "bnd-rdp");
			Directory.CreateDirectory(dir);
			string file = Path.Combine(dir, "connect-" + SafeName(h) + ".rdp");

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("screen mode id:i:2");          // полноэкранный
			sb.AppendLine("use multimon:i:0");
			sb.AppendLine("desktopwidth:i:1920");
			sb.AppendLine("desktopheight:i:1080");
			sb.AppendLine("session bpp:i:32");
			sb.AppendLine("full address:s:" + h);
			if (!string.IsNullOrWhiteSpace(user))
			{
				sb.AppendLine("username:s:" + user.Trim());
			}
			// Пароль не пишем: его введёт человек.
			sb.AppendLine("prompt for credentials:i:1");
			sb.AppendLine("promptcredentialonce:i:1");
			sb.AppendLine("authentication level:i:0");
			sb.AppendLine("enablecredsspsupport:i:1");
			sb.AppendLine("redirectclipboard:i:1");       // буфер обмена — ради скрипта
			sb.AppendLine("redirectprinters:i:0");
			sb.AppendLine("redirectcomports:i:0");
			sb.AppendLine("redirectsmartcards:i:0");
			sb.AppendLine("audiomode:i:2");
			sb.AppendLine("administrative session:i:" + (adminSession ? "1" : "0"));
			sb.AppendLine("negotiate security layer:i:1");
			sb.AppendLine("bandwidthautodetect:i:1");

			File.WriteAllText(file, sb.ToString(), Encoding.Unicode);
			o.RdpPath = file;

			if (launch)
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = "mstsc.exe",
					Arguments = "\"" + file + "\"",
					UseShellExecute = true
				});
				o.Ok = true;
				o.Message = "Открываю подключение к " + h +
					". Пароль введите в окне Windows, затем вставьте скрипт (он уже в буфере).";
				Log.Info("RDP: запущено подключение к " + h);
			}
			else
			{
				o.Ok = true;
				o.Message = "Файл подключения готов: " + file;
			}
			return o;
		}
		catch (Exception ex)
		{
			o.Message = "не удалось подготовить RDP: " + ex.Message;
			Log.Warn("RDP: " + ex.Message);
			return o;
		}
	}

	/// <summary>Имя файла без недопустимых символов.</summary>
	private static string SafeName(string host)
	{
		StringBuilder sb = new StringBuilder(host.Length);
		foreach (char c in host)
		{
			sb.Append(char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_' ? c : '_');
		}
		string s = sb.ToString();
		return s.Length > 0 ? s : "host";
	}
}
