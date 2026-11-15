using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using RVTools.Utils;

namespace RVTools.CustomerExperience;

[ExcludeFromCodeCoverage]
public class AppInsightsTelemetryCollector : ITelemetryCollector
{
	private const string OutOfContextName = "None";

	private readonly TelemetryContext _config;

	private readonly SecureString _connectionString;

	private const LogLevel TelemetryLogLevel = LogLevel.Info;

	private IOperationHolder<DependencyTelemetry> _contextOperation;

	private IOperationHolder<DependencyTelemetry> _instanceOperation;

	private IOperationHolder<DependencyTelemetry> _stageOperation;

	private TelemetryClient _telemetryClient;

	private ITrace Trace { get; }

	public DateTime SessionStartTimestamp { get; } = DateTime.UtcNow;

	public DateTime ContextStageStartTimestamp { get; private set; } = DateTime.MinValue;

	public DateTime ContextStartTimestamp { get; private set; } = DateTime.MinValue;

	public DateTime ContextEndTimestamp { get; private set; } = DateTime.MinValue;

	public bool Enabled { get; private set; }

	public string ContextName { get; private set; }

	public string ContextStage { get; private set; }

	private TelemetryClient Client
	{
		get
		{
			if (!Enabled)
			{
				return null;
			}
			if (_telemetryClient != null)
			{
				return _telemetryClient;
			}
			try
			{
				TelemetryConfiguration telemetryConfiguration = TelemetryConfiguration.CreateDefault();
				if (_connectionString != null && _connectionString.Length != 0)
				{
					telemetryConfiguration.ConnectionString = SecureStringToString(_connectionString);
				}
				_telemetryClient = new TelemetryClient(telemetryConfiguration);
				_telemetryClient.Context.Session.Id = _config.SessionId;
				_telemetryClient.Context.User.Id = _config.UserUuid;
				_telemetryClient.Context.Device.Id = _config.InstanceUuid;
				_telemetryClient.Context.Component.Version = _config.AppName + " (" + _config.AppEdition + " Edition) v" + _config.ExecutableVersion;
				_telemetryClient.Context.Device.Model = _config.AppName + " (" + _config.AppEdition + " Edition)";
				_telemetryClient.Context.Device.OperatingSystem = _config.OperatingSystemName;
				AddEvent("SessionStart", "Application", _config.AsDictionary());
				DependencyTelemetry dependencyTelemetry = new DependencyTelemetry
				{
					Name = "Collector",
					Type = "Instance"
				};
				foreach (KeyValuePair<string, string> item in _config.AsDictionary())
				{
					dependencyTelemetry.Properties.Add(item.Key, item.Value);
				}
				_instanceOperation = _telemetryClient.StartOperation(dependencyTelemetry);
				Trace.Log(LogLevel.Info, "Telemetry Started");
			}
			catch (Exception ex)
			{
				Trace.Log(LogLevel.Error, "Telemetry Failed to Start: {0}", ex);
				_telemetryClient = null;
				Enabled = false;
			}
			return _telemetryClient;
		}
	}

	public AppInsightsTelemetryCollector(TelemetryContext config, SecureString connectionString, ITrace log)
	{
		_connectionString = connectionString;
		_config = config;
		Trace = new ContextLogger(log, "Telemetry");
		Trace.Log(LogLevel.Info, "Telemetry Initialized");
	}

	public void EnterContext(string name, IReadOnlyDictionary<string, string> properties = null)
	{
		if (ContextName == name)
		{
			return;
		}
		LeaveContext("Context switching to '" + name + "'");
		if (string.IsNullOrEmpty(name))
		{
			return;
		}
		Trace.Log(LogLevel.Info, "Entering Context '" + name + "'", BuildParams(properties));
		DependencyTelemetry dependencyTelemetry = new DependencyTelemetry
		{
			Name = name,
			Type = "Context"
		};
		dependencyTelemetry.Context.Operation.ParentId = _instanceOperation?.Telemetry.Id;
		dependencyTelemetry.Context.Operation.Name = _instanceOperation?.Telemetry.Name;
		_contextOperation = Client?.StartOperation(dependencyTelemetry);
		if (properties != null)
		{
			foreach (KeyValuePair<string, string> property in properties)
			{
				_contextOperation?.Telemetry.Properties.Add(property.Key, property.Value);
			}
		}
		AddEvent("EnterContext", name, properties);
		ContextName = name;
		ContextEndTimestamp = DateTime.MinValue;
	}

	public void EnterContextStage(string name)
	{
		if (!(ContextStage == name))
		{
			if (ContextName == null && !string.IsNullOrEmpty(name))
			{
				Trace.Log(LogLevel.Info, "No context started. Creating default context for new stage '" + name + "'");
				EnterContext("None");
			}
			Trace.Log(LogLevel.Info, string.IsNullOrEmpty(name) ? ("Leaving Stage '" + ContextStage + "' on Context '" + ContextName + "'") : ("Entering Stage '" + name + "' on Context '" + ContextName + "'"));
			if (_stageOperation != null)
			{
				Client?.StopOperation(_stageOperation);
				_stageOperation.Dispose();
				_stageOperation = null;
			}
			if (!string.IsNullOrEmpty(name))
			{
				DependencyTelemetry dependencyTelemetry = new DependencyTelemetry
				{
					Name = name,
					Type = "Stage"
				};
				dependencyTelemetry.Context.Operation.ParentId = _contextOperation?.Telemetry.Id;
				dependencyTelemetry.Context.Operation.Name = _contextOperation?.Telemetry.Name;
				_stageOperation = Client?.StartOperation(dependencyTelemetry);
			}
			AddEvent("ContextStageChange", name, new Dictionary<string, string> { 
			{
				"StageDuration",
				(ContextStageStartTimestamp == DateTime.MinValue) ? null : (DateTime.UtcNow - ContextStageStartTimestamp).ToString()
			} });
			ContextStage = name;
			ContextStageStartTimestamp = DateTime.UtcNow;
		}
	}

	public void LeaveContext(string reason)
	{
		if (ContextName != null)
		{
			EnterContextStage(null);
			Trace.Log(LogLevel.Info, "Leaving Context '" + ContextName + "' due to '" + reason + "'");
			AddEvent("LeaveContext", ContextName, new Dictionary<string, string>
			{
				{
					"Duration",
					(ContextStartTimestamp == DateTime.MinValue) ? null : (DateTime.UtcNow - ContextStartTimestamp).ToString()
				},
				{
					"StageDuration",
					(ContextStageStartTimestamp == DateTime.MinValue) ? null : (DateTime.UtcNow - ContextStageStartTimestamp).ToString()
				},
				{ "Reason", reason }
			});
			ContextName = null;
			DateTime contextStageStartTimestamp = (ContextStartTimestamp = DateTime.MinValue);
			ContextStageStartTimestamp = contextStageStartTimestamp;
			ContextEndTimestamp = DateTime.UtcNow;
			if (_contextOperation != null)
			{
				Client?.StopOperation(_contextOperation);
				_contextOperation?.Dispose();
				_contextOperation = null;
			}
		}
	}

	public void AddEvent(string eventName, string itemName, IReadOnlyDictionary<string, string> properties = null)
	{
		Trace.Log(LogLevel.Info, "Event '" + eventName + "': '" + itemName + "'", BuildParams(properties));
		Dictionary<string, string> dictionary = new Dictionary<string, string>
		{
			{ "ContextName", ContextName },
			{ "ContextStage", ContextStage },
			{ "Name", itemName }
		};
		if (properties != null)
		{
			foreach (KeyValuePair<string, string> property in properties)
			{
				dictionary.Add(property.Key, property.Value);
			}
		}
		EventTelemetry eventTelemetry = new EventTelemetry(eventName);
		eventTelemetry.Context.Operation.ParentId = _stageOperation?.Telemetry.Id ?? _contextOperation?.Telemetry.Id ?? _instanceOperation?.Telemetry.Id;
		eventTelemetry.Context.Operation.Name = _stageOperation?.Telemetry.Name ?? _contextOperation?.Telemetry.Name ?? _instanceOperation?.Telemetry.Name;
		foreach (KeyValuePair<string, string> item in dictionary)
		{
			eventTelemetry.Properties.Add(item.Key, item.Value);
		}
		Client?.TrackEvent(eventTelemetry);
	}

	public void AddException(Exception ex, IReadOnlyDictionary<string, string> properties = null)
	{
		Trace.Log(LogLevel.Info, "Error: '" + ex.GetType().Name + "' - '" + ex?.Message + "' ", ex, BuildParams(properties));
		ExceptionTelemetry exceptionTelemetry = new ExceptionTelemetry(ex);
		exceptionTelemetry.Context.Operation.ParentId = _stageOperation?.Telemetry.Id ?? _contextOperation?.Telemetry.Id ?? _instanceOperation?.Telemetry.Id;
		exceptionTelemetry.Context.Operation.Name = _stageOperation?.Telemetry.Name ?? _contextOperation?.Telemetry.Name ?? _instanceOperation?.Telemetry.Name;
		exceptionTelemetry.Properties.Add("ContextName", ContextName);
		exceptionTelemetry.Properties.Add("ContextStage", ContextStage);
		if (properties != null)
		{
			foreach (KeyValuePair<string, string> property in properties)
			{
				exceptionTelemetry.Properties.Add(property.Key, property.Value);
			}
		}
		Client?.TrackException(exceptionTelemetry);
	}

	public void AddMetric(string name, double value)
	{
		Trace.Log(LogLevel.Info, $"Metric '{name}': {value}");
		MetricTelemetry metricTelemetry = new MetricTelemetry(name, value);
		metricTelemetry.Context.Operation.ParentId = _stageOperation?.Telemetry.Id ?? _contextOperation?.Telemetry.Id ?? _instanceOperation?.Telemetry.Id;
		metricTelemetry.Context.Operation.Name = _stageOperation?.Telemetry.Name ?? _contextOperation?.Telemetry.Name ?? _instanceOperation?.Telemetry.Name;
		metricTelemetry.Properties.Add("ContextName", ContextName);
		metricTelemetry.Properties.Add("ContextStage", ContextStage);
		Client?.TrackMetric(metricTelemetry);
	}

	public void Close(string reason)
	{
		if (_telemetryClient != null)
		{
			LeaveContext("Telemetry Closing due to " + reason);
			AddEvent("SessionEnd", "Application", new Dictionary<string, string>
			{
				{ "Reason", reason },
				{
					"SessionDuration",
					(DateTime.UtcNow - SessionStartTimestamp).ToString()
				}
			});
			if (_instanceOperation != null)
			{
				_telemetryClient.StopOperation(_instanceOperation);
				_instanceOperation.Dispose();
				_instanceOperation = null;
			}
			TelemetryClient telemetryClient = _telemetryClient;
			_telemetryClient = null;
			telemetryClient.FlushAsync(CancellationToken.None).Wait();
			Trace.Log(LogLevel.Info, "Telemetry Closed. (" + reason + ")");
		}
	}

	public void Enable()
	{
		if (!Enabled)
		{
			Trace.Log(LogLevel.Info, "Enabling Telemetry.");
			Enabled = true;
			_ = Client;
		}
	}

	public void Disable()
	{
		if (Enabled)
		{
			Trace.Log(LogLevel.Info, "Disabling Telemetry.");
			Enabled = false;
		}
	}

	private string[] BuildParams(IReadOnlyDictionary<string, string> properties)
	{
		List<string> list = properties?.Select((KeyValuePair<string, string> p) => p.Key + "=\"" + p.Value + "\"").ToList() ?? new List<string>();
		if (ContextName != null)
		{
			list.Add("ContextName=" + ContextName);
		}
		if (ContextStage != null)
		{
			list.Add("ContextStage=" + ContextStage);
		}
		return list.ToArray();
	}

	private static string SecureStringToString(SecureString secret)
	{
		IntPtr intPtr = IntPtr.Zero;
		try
		{
			intPtr = Marshal.SecureStringToGlobalAllocUnicode(secret);
			return Marshal.PtrToStringUni(intPtr);
		}
		finally
		{
			Marshal.ZeroFreeGlobalAllocUnicode(intPtr);
		}
	}

	~AppInsightsTelemetryCollector()
	{
		Close("Finalizer");
	}
}
