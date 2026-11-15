using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BackupDotNet.Core;

namespace BackupDotNet.Web;

public class AuditLog
{
	private readonly string _dir;

	private readonly object _sync = new object();

	public AuditLog(string dataDir)
	{
		_dir = Path.Combine(dataDir, "audit");
		Directory.CreateDirectory(_dir);
	}

	public void Write(string user, string action, string target, string detail)
	{
		try
		{
			Dictionary<string, object> d = Json.Obj();
			d["ts"] = DateTime.UtcNow;
			d["user"] = user;
			d["action"] = action;
			if (!string.IsNullOrEmpty(target))
			{
				d["target"] = target;
			}
			if (!string.IsNullOrEmpty(detail))
			{
				d["detail"] = detail;
			}
			string month = DateTime.Now.ToString("yyyy-MM", CultureInfo.InvariantCulture);
			string path = Path.Combine(_dir, month + ".ndjson");
			lock (_sync)
			{
				File.AppendAllText(path, Json.Write(d) + "\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			}
		}
		catch (Exception ex)
		{
			Log.Warn("audit: " + ex.Message);
		}
	}

	public List<object> Read(int limit)
	{
		return Read(limit, "", "");
	}

	/// <summary>
	/// Читает журнал за последние месяцы, свежие записи первыми.
	/// <paramref name="actionFilter"/> — подстрока действия (например, <c>backup.</c>),
	/// <paramref name="userFilter"/> — имя пользователя.
	/// </summary>
	public List<object> Read(int limit, string actionFilter, string userFilter)
	{
		if (limit <= 0 || limit > 2000)
		{
			limit = 200;
		}
		List<object> result = new List<object>();
		lock (_sync)
		{
			// Журнал разложен по месяцам. Читаем текущий и предыдущий: чаще
			// «кто это сделал» ищут в пределах пары недель, а не за год.
			List<string> months = new List<string>();
			DateTime now = DateTime.Now;
			for (int i = 0; i < 3; i++)
			{
				months.Add(now.AddMonths(-i).ToString("yyyy-MM", CultureInfo.InvariantCulture));
			}

			foreach (string month in months)
			{
				if (result.Count >= limit)
				{
					break;
				}
				string path = Path.Combine(_dir, month + ".ndjson");
				if (!File.Exists(path))
				{
					continue;
				}
				try
				{
					string[] lines = File.ReadAllLines(path);
					int i = lines.Length - 1;
					while (i >= 0 && result.Count < limit)
					{
						if (lines[i].Length >= 3)
						{
							try
							{
								Dictionary<string, object> row = Json.ParseObject(lines[i]);
								if (Matches(row, actionFilter, userFilter))
								{
									result.Add(row);
								}
							}
							catch
							{
							}
						}
						i--;
					}
				}
				catch
				{
				}
			}
		}
		return result;
	}

	private static bool Matches(Dictionary<string, object> row, string actionFilter, string userFilter)
	{
		if (!string.IsNullOrEmpty(actionFilter))
		{
			string action = Json.GetStr(row, "action", "");
			if (action.IndexOf(actionFilter, StringComparison.OrdinalIgnoreCase) < 0)
			{
				return false;
			}
		}
		if (!string.IsNullOrEmpty(userFilter))
		{
			string user = Json.GetStr(row, "user", "");
			if (user.IndexOf(userFilter, StringComparison.OrdinalIgnoreCase) < 0)
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>Список доступных месяцев журнала — для выбора в интерфейсе.</summary>
	public List<string> Months()
	{
		List<string> list = new List<string>();
		try
		{
			foreach (string f in Directory.GetFiles(_dir, "*.ndjson"))
			{
				list.Add(Path.GetFileNameWithoutExtension(f));
			}
		}
		catch
		{
		}
		list.Sort();
		list.Reverse();
		return list;
	}
}
