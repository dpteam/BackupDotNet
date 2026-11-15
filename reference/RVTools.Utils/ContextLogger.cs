using System;

namespace RVTools.Utils;

public class ContextLogger : ITrace, IDisposable
{
	private readonly ITrace _logger;

	private readonly string _contextName;

	public string[] LogFiles => _logger.LogFiles;

	public LogLevel MinLogLevel
	{
		get
		{
			return _logger?.MinLogLevel ?? LogLevel.Info;
		}
		set
		{
			if (_logger != null)
			{
				_logger.MinLogLevel = value;
			}
		}
	}

	public ContextLogger(ITrace logger, string context)
	{
		_logger = logger ?? new VoidLogger();
		_contextName = context ?? "";
	}

	private string ContextMsg(string msg)
	{
		if (_contextName != null && _contextName.Length != 0)
		{
			return "[" + _contextName + "] " + msg;
		}
		return msg;
	}

	public void Debug(string msg, string[] parameters = null)
	{
		_logger.Debug(ContextMsg(msg), parameters);
	}

	public void Debug(string msg, Exception ex, string[] parameters = null)
	{
		_logger.Debug(ContextMsg(msg), ex, parameters);
	}

	public void Dispose()
	{
	}

	public void Error(string msg, string[] parameters = null)
	{
		_logger.Error(ContextMsg(msg), parameters);
	}

	public void Error(string msg, Exception ex, string[] parameters = null)
	{
		_logger.Error(ContextMsg(msg), ex, parameters);
	}

	public void Log(LogLevel level, string msg, string[] parameters = null)
	{
		_logger.Log(level, ContextMsg(msg), parameters);
	}

	public void Log(LogLevel level, string msg, Exception ex, string[] parameters = null)
	{
		_logger.Log(level, ContextMsg(msg), ex, parameters);
	}

	public void Info(string msg, string[] parameters = null)
	{
		_logger.Info(ContextMsg(msg), parameters);
	}

	public void Info(string msg, Exception ex, string[] parameters = null)
	{
		_logger.Info(ContextMsg(msg), ex, parameters);
	}

	public void Warn(string msg, string[] parameters = null)
	{
		_logger.Warn(ContextMsg(msg), parameters);
	}

	public void Warn(string msg, Exception ex, string[] parameters = null)
	{
		_logger.Warn(ContextMsg(msg), ex, parameters);
	}
}
