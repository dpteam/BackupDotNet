using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace BackupChecker.Core;

public static class Log
{
	private static readonly object Sync = new object();

	private static string _dir;

	private static LogLevel _min = LogLevel.Info;

	private static long _maxBytes = 33554432L;

	private static bool _echo = true;

	private static string _currentDate = "";

	public static void Init(string dir, string level, int maxMb, bool echo)
	{
		lock (Sync)
		{
			_dir = dir;
			_min = ParseLevel(level);
			_maxBytes = (long)Math.Max(1, maxMb) * 1024L * 1024;
			_echo = echo;
			if (!string.IsNullOrEmpty(_dir) && !Directory.Exists(_dir))
			{
				Directory.CreateDirectory(_dir);
			}
		}
	}

	public static LogLevel ParseLevel(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return LogLevel.Info;
		}
		return s.Trim().ToLowerInvariant() switch
		{
			"debug" => LogLevel.Debug, 
			"info" => LogLevel.Info, 
			"warn" => LogLevel.Warn, 
			"error" => LogLevel.Error, 
			_ => LogLevel.Info, 
		};
	}

	public static void Debug(string msg)
	{
		Write(LogLevel.Debug, msg, null);
	}

	public static void Info(string msg)
	{
		Write(LogLevel.Info, msg, null);
	}

	public static void Warn(string msg)
	{
		Write(LogLevel.Warn, msg, null);
	}

	public static void Error(string msg)
	{
		Write(LogLevel.Error, msg, null);
	}

	public static void Error(string msg, Exception ex)
	{
		Write(LogLevel.Error, msg, ex);
	}

	private static void Write(LogLevel lvl, string msg, Exception ex)
	{
		if (lvl < _min)
		{
			return;
		}
		string line = string.Format(CultureInfo.InvariantCulture, "{0:yyyy-MM-dd HH:mm:ss.fff} [{1,-5}] {2}{3}", DateTime.Now, lvl.ToString().ToUpperInvariant(), msg, (ex == null) ? "" : (" | " + ex.GetType().Name + ": " + ex.Message));
		lock (Sync)
		{
			if (_echo && Environment.UserInteractive)
			{
				try
				{
					if (lvl >= LogLevel.Error)
					{
						Console.Error.WriteLine(line);
					}
					else
					{
						Console.WriteLine(line);
					}
				}
				catch
				{
				}
			}
			if (string.IsNullOrEmpty(_dir))
			{
				return;
			}
			try
			{
				string date = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
				if (date != _currentDate)
				{
					_currentDate = date;
				}
				string path = Path.Combine(_dir, "agent-" + _currentDate + ".log");
				FileInfo fi = new FileInfo(path);
				if (fi.Exists && fi.Length > _maxBytes)
				{
					string rolled = Path.Combine(_dir, "agent-" + _currentDate + "-" + DateTime.Now.ToString("HHmmss", CultureInfo.InvariantCulture) + ".log");
					try
					{
						File.Move(path, rolled);
					}
					catch
					{
					}
				}
				File.AppendAllText(path, line + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			}
			catch
			{
			}
		}
	}

	public static void Cleanup(string dir, int keepDays)
	{
		if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir) || keepDays <= 0)
		{
			return;
		}
		try
		{
			DateTime cutoff = DateTime.Now.AddDays(-keepDays);
			string[] files = Directory.GetFiles(dir, "agent-*.log");
			foreach (string f in files)
			{
				try
				{
					if (File.GetLastWriteTime(f) < cutoff)
					{
						File.Delete(f);
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
}
