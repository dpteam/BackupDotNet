using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BackupChecker.Core;

namespace BackupChecker.Web;

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
		if (limit <= 0 || limit > 2000)
		{
			limit = 200;
		}
		List<object> result = new List<object>();
		lock (_sync)
		{
			string month = DateTime.Now.ToString("yyyy-MM", CultureInfo.InvariantCulture);
			string path = Path.Combine(_dir, month + ".ndjson");
			if (!File.Exists(path))
			{
				return result;
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
							result.Add(Json.ParseObject(lines[i]));
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
		return result;
	}
}
