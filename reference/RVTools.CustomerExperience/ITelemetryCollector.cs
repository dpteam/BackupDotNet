using System;
using System.Collections.Generic;

namespace RVTools.CustomerExperience;

public interface ITelemetryCollector
{
	bool Enabled { get; }

	string ContextName { get; }

	string ContextStage { get; }

	void EnterContext(string name, IReadOnlyDictionary<string, string> properties = null);

	void EnterContextStage(string name);

	void LeaveContext(string reason);

	void AddEvent(string eventName, string itemName, IReadOnlyDictionary<string, string> properties = null);

	void AddException(Exception ex, IReadOnlyDictionary<string, string> properties = null);

	void AddMetric(string name, double value);

	void Close(string reason);

	void Enable();

	void Disable();
}
