using System;
using System.Collections.Generic;

namespace RVTools.CustomerExperience;

public class VoidTelemetryCollector : ITelemetryCollector
{
	public bool Enabled { get; private set; }

	public string ContextName { get; private set; }

	public string ContextStage { get; private set; }

	public void EnterContext(string name, IReadOnlyDictionary<string, string> properties = null)
	{
		ContextName = name;
	}

	public void EnterContextStage(string name)
	{
		ContextStage = name;
	}

	public void LeaveContext(string reason)
	{
		ContextName = null;
	}

	public void AddEvent(string eventName, string itemName, IReadOnlyDictionary<string, string> properties = null)
	{
	}

	public void AddException(Exception ex, IReadOnlyDictionary<string, string> properties = null)
	{
	}

	public void AddMetric(string name, double value)
	{
	}

	public void Close(string reason)
	{
	}

	public void Enable()
	{
		Enabled = true;
	}

	public void Disable()
	{
		Enabled = false;
	}
}
