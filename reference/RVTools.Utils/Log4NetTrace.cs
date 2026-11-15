using System;
using System.Linq;
using log4net;
using log4net.Core;

namespace RVTools.Utils;

public class Log4NetTrace : ITrace, IDisposable
{
	private readonly ILog _logger;

	public LogLevel MinLogLevel { get; set; }

	public string[] LogFiles => _logger?.Logger?.Repository?.GetCurrentLoggers()?.Select((ILogger x) => x.Name).ToArray() ?? Array.Empty<string>();

	public Log4NetTrace(ILog logger)
	{
		_logger = logger ?? LogManager.GetLogger("RVTools");
	}

	public void Dispose()
	{
	}

	private static string FormattedMessage(string msg, string[] parameters)
	{
		if (parameters == null || !parameters.Any())
		{
			return msg;
		}
		if (string.IsNullOrEmpty(msg))
		{
			return string.Join(", ", parameters);
		}
		return msg + " - " + string.Join(", ", parameters);
	}

	public void Debug(string msg, string[] parameters = null)
	{
		Debug(msg, null, parameters);
	}

	public void Debug(string msg, Exception ex, string[] parameters = null)
	{
		if (_logger.IsDebugEnabled)
		{
			_logger.Debug(FormattedMessage(msg, parameters), ex);
		}
	}

	public void Info(string msg, string[] parameters = null)
	{
		Info(msg, null, parameters);
	}

	public void Info(string msg, Exception ex, string[] parameters = null)
	{
		if (_logger.IsInfoEnabled)
		{
			_logger.Info(FormattedMessage(msg, parameters), ex);
		}
	}

	public void Warn(string msg, string[] parameters = null)
	{
		Warn(msg, null, parameters);
	}

	public void Warn(string msg, Exception ex, string[] parameters = null)
	{
		if (_logger.IsWarnEnabled)
		{
			_logger.Warn(FormattedMessage(msg, parameters), ex);
		}
	}

	public void Error(string msg, string[] parameters = null)
	{
		Error(msg, null, parameters);
	}

	public void Error(string msg, Exception ex, string[] parameters = null)
	{
		if (_logger.IsErrorEnabled)
		{
			_logger.Error(FormattedMessage(msg, parameters), ex);
		}
	}

	public void Log(LogLevel level, string msg, string[] parameters = null)
	{
		Log(level, msg, null, parameters);
	}

	public void Log(LogLevel level, string msg, Exception ex, string[] parameters = null)
	{
		switch (level)
		{
		case LogLevel.Debug:
			Debug(msg, ex, parameters);
			break;
		case LogLevel.Info:
			Info(msg, ex, parameters);
			break;
		case LogLevel.Warning:
			Warn(msg, ex, parameters);
			break;
		default:
			Error(msg, ex, parameters);
			break;
		}
	}
}
