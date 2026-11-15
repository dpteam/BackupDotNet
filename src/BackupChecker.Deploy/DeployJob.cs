using System;
using System.Collections.Generic;
using BackupChecker.Core;

namespace BackupChecker.Deploy;

public class DeployJob
{
	public string Id = "";

	public string Kind = "";

	public string Host = "";

	public string Phase = "в очереди";

	public string Message = "";

	public int Percent;

	public bool Done;

	public bool Ok;

	public DateTime Started = DateTime.Now;

	public DateTime? Finished;

	public List<string> Log = new List<string>();

	internal readonly object Sync = new object();

	public void Add(string line)
	{
		lock (Sync)
		{
			Log.Add(DateTime.Now.ToString("HH:mm:ss") + "  " + line);
			if (Log.Count > 400)
			{
				Log.RemoveAt(0);
			}
		}
	}

	public void SetPhase(string phase, int percent)
	{
		lock (Sync)
		{
			Phase = phase;
			Percent = percent;
		}
	}

	public void Fail(string message)
	{
		lock (Sync)
		{
			Ok = false;
			Done = true;
			Finished = DateTime.Now;
			Message = message;
			Log.Add(DateTime.Now.ToString("HH:mm:ss") + "  ОШИБКА: " + message);
		}
	}

	public void Success(string message)
	{
		lock (Sync)
		{
			Ok = true;
			Done = true;
			Percent = 100;
			Phase = "готово";
			Finished = DateTime.Now;
			Message = message;
			Log.Add(DateTime.Now.ToString("HH:mm:ss") + "  " + message);
		}
	}

	public Dictionary<string, object> ToJson()
	{
		lock (Sync)
		{
			Dictionary<string, object> d = Json.Obj();
			d["id"] = Id;
			d["kind"] = Kind;
			d["host"] = Host;
			d["phase"] = Phase;
			d["message"] = Message;
			d["percent"] = Percent;
			d["done"] = Done;
			d["ok"] = Ok;
			d["started"] = Started;
			if (Finished.HasValue)
			{
				d["finished"] = Finished.Value;
			}
			List<object> log = new List<object>();
			foreach (string s in Log)
			{
				log.Add(s);
			}
			d["log"] = log;
			return d;
		}
	}
}
