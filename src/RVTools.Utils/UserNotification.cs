using System;
using System.Windows.Forms;
using RVTools.CustomerExperience;

namespace RVTools.Utils;

internal class UserNotification
{
	private string Context { get; }

	private ITrace Logger { get; }

	private ITelemetryCollector Telemetry { get; }

	public UserNotification(string context, ITrace logger, ITelemetryCollector telemetry)
	{
		Context = context;
		Logger = logger;
		Telemetry = telemetry;
	}

	public void ReportError(string caption, string message, Exception err = null, MessageBoxIcon errorIcon = MessageBoxIcon.Hand)
	{
		Logger.Log((errorIcon == MessageBoxIcon.Exclamation) ? LogLevel.Warning : LogLevel.Error, caption + " : " + message, err);
		Telemetry.AddException(new InvalidOperationException(caption + " : " + message, err));
		if (Environment.UserInteractive)
		{
			MessageBox.Show((err == null) ? message : (message + ": " + err.Message), caption, MessageBoxButtons.OK, errorIcon);
		}
	}

	public void ReportError(string message, Exception err = null, MessageBoxIcon errorIcon = MessageBoxIcon.Hand)
	{
		ReportError("RVTools", message, err, errorIcon);
	}

	public void ReportWarning(string caption, string message, Exception err = null)
	{
		ReportError(caption, message, err, MessageBoxIcon.Exclamation);
	}

	public void ReportWarning(string message, Exception err = null)
	{
		ReportWarning("RVTools", message, err);
	}
}
