using System.Diagnostics.CodeAnalysis;

namespace RVTools.CustomerExperience;

[ExcludeFromCodeCoverage]
public static class TelemetryEvent
{
	internal const string SessionStart = "SessionStart";

	internal const string SessionEnd = "SessionEnd";

	internal const string SessionError = "SessionError";

	internal const string EnterContext = "EnterContext";

	internal const string LeaveContext = "LeaveContext";

	internal const string ContextStageChange = "ContextStageChange";

	public const string UserDataEntered = "UserDataEntered";

	public const string UserButtonPressed = "UserButtonPressed";

	public const string UserOptionSelection = "UserOptionSelection";

	public const string GenericCollectionStats = "GenericCollectionStats";

	public const string CollectionWarning = "CollectionWarning";
}
