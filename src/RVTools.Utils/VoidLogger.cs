using System;

namespace RVTools.Utils;

public class VoidLogger : ITrace, IDisposable
{
	public string[] LogFiles => new string[0];

	public LogLevel MinLogLevel { get; set; }

	public void Debug(string msg, string[] parameters = null)
	{
	}

	public void Debug(string msg, Exception ex, string[] parameters = null)
	{
	}

	public void Dispose()
	{
	}

	public void Error(string msg, string[] parameters = null)
	{
	}

	public void Error(string msg, Exception ex, string[] parameters = null)
	{
	}

	public void Info(string msg, string[] parameters = null)
	{
	}

	public void Info(string msg, Exception ex, string[] parameters = null)
	{
	}

	public void Warn(string msg, string[] parameters = null)
	{
	}

	public void Warn(string msg, Exception ex, string[] parameters = null)
	{
	}

	public void Log(LogLevel level, string msg, string[] parameters = null)
	{
	}

	public void Log(LogLevel level, string msg, Exception ex, string[] parameters = null)
	{
	}
}
