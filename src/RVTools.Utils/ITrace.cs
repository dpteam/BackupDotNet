using System;

namespace RVTools.Utils;

public interface ITrace : IDisposable
{
	LogLevel MinLogLevel { get; set; }

	string[] LogFiles { get; }

	void Debug(string msg, string[] parameters = null);

	void Debug(string msg, Exception ex, string[] parameters = null);

	void Info(string msg, string[] parameters = null);

	void Info(string msg, Exception ex, string[] parameters = null);

	void Warn(string msg, string[] parameters = null);

	void Warn(string msg, Exception ex, string[] parameters = null);

	void Error(string msg, string[] parameters = null);

	void Error(string msg, Exception ex, string[] parameters = null);

	void Log(LogLevel level, string msg, string[] parameters = null);

	void Log(LogLevel level, string msg, Exception ex, string[] parameters = null);
}
