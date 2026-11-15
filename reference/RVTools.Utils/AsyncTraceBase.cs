using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace RVTools.Utils;

public abstract class AsyncTraceBase : ITrace, IDisposable
{
	protected class LogEntry
	{
		public DateTime Timestamp { get; }

		public string Level { get; }

		public string Message { get; }

		public string TimeStampString => Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fff");

		public string Line => TimeStampString + " [" + Level + "] " + Message.Replace("\n", "\n                                ");

		public LogEntry(string level, string msg, string[] parameters)
		{
			Timestamp = DateTime.UtcNow;
			Level = level;
			Message = msg;
			if (parameters != null && parameters.Any())
			{
				Message = Message + " { " + string.Join(", ", parameters) + "}";
			}
		}
	}

	private readonly CancellationTokenSource _cancellationToken;

	private readonly BlockingCollection<LogEntry> _logEntriesQueue;

	private bool _disposed;

	public JObject Config { get; }

	public LogLevel MinLogLevel { get; set; }

	public virtual string[] LogFiles => Array.Empty<string>();

	protected AsyncTraceBase(JObject config)
	{
		Config = config;
		if (config == null)
		{
			MinLogLevel = LogLevelFromString(Environment.GetCommandLineArgs().FirstOrDefault((string s) => s.ToLower().StartsWith("minloglevel="))?.Substring(12));
		}
		else
		{
			config.TryGetValue("MinLogLevel", out JToken value);
			MinLogLevel = LogLevelFromString(value?.ToString());
		}
		_cancellationToken = new CancellationTokenSource();
		_logEntriesQueue = new BlockingCollection<LogEntry>();
		ThreadPool.QueueUserWorkItem(ThreadProc, this);
	}

	private static void ThreadProc(object stateInfo)
	{
		((AsyncTraceBase)stateInfo).LogThreadProc();
	}

	~AsyncTraceBase()
	{
		Dispose(disposing: true);
	}

	protected static LogLevel LogLevelFromString(string level)
	{
		if (level == null)
		{
			return LogLevel.Info;
		}
		switch (level.ToUpper().Trim())
		{
		case "D":
		case "DEBUG":
			return LogLevel.Debug;
		case "I":
		case "INFO":
		case "INFORMATION":
			return LogLevel.Info;
		case "W":
		case "WARN":
		case "WARNING":
			return LogLevel.Warning;
		case "E":
		case "ERROR":
			return LogLevel.Error;
		default:
			return LogLevel.Info;
		}
	}

	protected static string LogLevelToString(LogLevel level)
	{
		return level switch
		{
			LogLevel.Debug => "DEBUG", 
			LogLevel.Info => "INFO ", 
			LogLevel.Warning => "WARN ", 
			LogLevel.Error => "ERROR", 
			_ => "?????", 
		};
	}

	protected abstract Task WriteOutput(LogEntry logEntry);

	protected virtual Task Flush()
	{
		return Task.CompletedTask;
	}

	private void LogThreadProc()
	{
		foreach (LogEntry item in _logEntriesQueue.GetConsumingEnumerable(_cancellationToken.Token))
		{
			WriteOutput(item);
			if (_logEntriesQueue.Count == 0)
			{
				Flush().Wait();
			}
		}
	}

	private static string ExceptionDetails(Exception ex)
	{
		if (ex == null)
		{
			return string.Empty;
		}
		if (!(ex is AggregateException ex2))
		{
			return "[" + ex.GetType().Name + ": \"" + ex.Message + "\" " + ExceptionDetails(ex.InnerException) + "]";
		}
		string seed = "[" + ex2.GetType().Name + ": \"" + ex2.Message + "\" {";
		seed = ex2.InnerExceptions.Aggregate(seed, (string current, Exception inner) => current + ExceptionDetails(inner));
		return seed + "}";
	}

	private void WriteLine(LogLevel level, string msg, Exception ex, string[] parameters = null)
	{
		if (level < MinLogLevel)
		{
			return;
		}
		if (ex != null)
		{
			msg = msg + " " + ExceptionDetails(ex);
			if (MinLogLevel == LogLevel.Debug)
			{
				msg = msg + "\r\n" + ex.StackTrace;
			}
		}
		if (!_logEntriesQueue.IsAddingCompleted)
		{
			_logEntriesQueue.Add(new LogEntry(LogLevelToString(level), msg, parameters));
		}
	}

	public void Debug(string msg, string[] parameters = null)
	{
		Log(LogLevel.Debug, msg, null, parameters);
	}

	public void Debug(string msg, Exception ex, string[] parameters = null)
	{
		Log(LogLevel.Debug, msg, ex, parameters);
	}

	public void Info(string msg, string[] parameters = null)
	{
		Log(LogLevel.Info, msg, null, parameters);
	}

	public void Info(string msg, Exception ex, string[] parameters = null)
	{
		Log(LogLevel.Info, msg, ex, parameters);
	}

	public void Warn(string msg, string[] parameters = null)
	{
		Log(LogLevel.Warning, msg, null, parameters);
	}

	public void Warn(string msg, Exception ex, string[] parameters = null)
	{
		Log(LogLevel.Warning, msg, ex, parameters);
	}

	public void Error(string msg, string[] parameters = null)
	{
		Log(LogLevel.Error, msg, null, parameters);
	}

	public void Error(string msg, Exception ex, string[] parameters = null)
	{
		Log(LogLevel.Error, msg, ex, parameters);
	}

	public void Log(LogLevel level, string msg, string[] parameters = null)
	{
		WriteLine(level, msg, null, parameters);
	}

	public void Log(LogLevel level, string msg, Exception ex, string[] parameters = null)
	{
		WriteLine(level, msg, ex, parameters);
	}

	public virtual void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposed && disposing)
		{
			_logEntriesQueue.CompleteAdding();
			_disposed = true;
		}
	}
}
