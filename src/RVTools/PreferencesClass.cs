using System.Reflection;
using log4net;

namespace RVTools;

internal class PreferencesClass
{
	private static readonly ILog logger = LogManager.GetLogger(typeof(PreferencesClass));

	private static bool _vInfoShowFolder = false;

	private static bool _vInfoShowAppName = false;

	private static bool _AutoRefresh = false;

	private static bool _GetFriendlyNames = false;

	private static bool _GetFileInfo = false;

	private static bool _ExcludeAnnotations = false;

	private static bool _ExcludeTags = false;

	private static int _AutoRefreshInterval = 10;

	public static bool vInfoShowFolder
	{
		get
		{
			return _vInfoShowFolder;
		}
		set
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			_vInfoShowFolder = value;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " " + value);
			}
		}
	}

	public static bool vInfoShowAppName
	{
		get
		{
			return _vInfoShowAppName;
		}
		set
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			_vInfoShowAppName = value;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " " + value);
			}
		}
	}

	public static bool GetFriendlyNames
	{
		get
		{
			return _GetFriendlyNames;
		}
		set
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			_GetFriendlyNames = value;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " " + value);
			}
		}
	}

	public static bool GetFileInfo
	{
		get
		{
			return _GetFileInfo;
		}
		set
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			_GetFileInfo = value;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " " + value);
			}
		}
	}

	public static bool ExcludeAnnotations
	{
		get
		{
			return _ExcludeAnnotations;
		}
		set
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			_ExcludeAnnotations = value;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " " + value);
			}
		}
	}

	public static bool ExcludeTags
	{
		get
		{
			return _ExcludeTags;
		}
		set
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			_ExcludeTags = value;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " " + value);
			}
		}
	}

	public static bool AutoRefresh
	{
		get
		{
			return _AutoRefresh;
		}
		set
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			_AutoRefresh = value;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " " + value);
			}
		}
	}

	public static int AutoRefreshInterval
	{
		get
		{
			return _AutoRefreshInterval;
		}
		set
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			_AutoRefreshInterval = value;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " " + value);
			}
		}
	}
}
