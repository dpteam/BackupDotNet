using System.Reflection;
using log4net;
using Vim25Api;

namespace RVTools;

internal class FilterClass
{
	private static readonly ILog logger = LogManager.GetLogger(typeof(FilterClass));

	private static bool _VirtualMachine = true;

	private static bool _Template = true;

	private static bool _SRMPlaceholder = true;

	private static bool _PoweredOn = true;

	private static bool _PoweredOff = true;

	private static bool _Suspended = true;

	private static bool _VMNameExactly = false;

	private static bool _VMNameStartsWith = false;

	private static bool _VMNameContains = true;

	private static string _VMName = "";

	private static bool _NotesExactly = false;

	private static bool _NotesStartsWith = false;

	private static bool _NotesContains = true;

	private static string _Notes = "";

	private static string _DataCenter = "ALL";

	private static string _Cluster = "ALL";

	private static string _Host = "ALL";

	private static bool _SaveFilter = false;

	public static string DataCenter
	{
		get
		{
			return _DataCenter;
		}
		set
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			_DataCenter = value;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " " + value.ToString());
			}
		}
	}

	public static string Cluster
	{
		get
		{
			return _Cluster;
		}
		set
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			_Cluster = value;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " " + value.ToString());
			}
		}
	}

	public static string Host
	{
		get
		{
			return _Host;
		}
		set
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			_Host = value;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " " + value.ToString());
			}
		}
	}

	public static string VMName
	{
		get
		{
			return _VMName;
		}
		set
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			_VMName = value;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " " + value.ToString());
			}
		}
	}

	public static string DefaultDataCenter => "ALL";

	public static string DefaultCluster => "ALL";

	public static string DefaultHost => "ALL";

	public static bool VirtualMachine
	{
		get
		{
			return _VirtualMachine;
		}
		set
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			_VirtualMachine = value;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " " + value);
			}
		}
	}

	public static bool DefaultVirtualMachine => true;

	public static bool Template
	{
		get
		{
			return _Template;
		}
		set
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			_Template = value;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " " + value);
			}
		}
	}

	public static bool SRMPlaceholder
	{
		get
		{
			return _SRMPlaceholder;
		}
		set
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			_SRMPlaceholder = value;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " " + value);
			}
		}
	}

	public static bool DefaultTemplate => true;

	public static bool DefaultSRMPlaceholder => true;

	public static bool PoweredOn
	{
		get
		{
			return _PoweredOn;
		}
		set
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			_PoweredOn = value;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " " + value);
			}
		}
	}

	public static bool DefaultPoweredOn => true;

	public static bool PoweredOff
	{
		get
		{
			return _PoweredOff;
		}
		set
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			_PoweredOff = value;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " " + value);
			}
		}
	}

	public static bool DefaultPoweredOff => true;

	public static bool Suspended
	{
		get
		{
			return _Suspended;
		}
		set
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			_Suspended = value;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " " + value);
			}
		}
	}

	public static bool DefaultSuspended => true;

	public static string DefaultVMName => "";

	public static bool VMNameExactly
	{
		get
		{
			return _VMNameExactly;
		}
		set
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			_VMNameExactly = value;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " " + value);
			}
		}
	}

	public static bool DefaultVMNameExactly => false;

	public static bool VMNameStartsWith
	{
		get
		{
			return _VMNameStartsWith;
		}
		set
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			_VMNameStartsWith = value;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " " + value);
			}
		}
	}

	public static bool DefaultVMNameStartsWith => false;

	public static bool VMNameContains
	{
		get
		{
			return _VMNameContains;
		}
		set
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			_VMNameContains = value;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " " + value);
			}
		}
	}

	public static bool DefaultVMNameContains => true;

	public static string Notes
	{
		get
		{
			return _Notes;
		}
		set
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			_Notes = value;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " " + value.ToString());
			}
		}
	}

	public static string DefaultNotes => "";

	public static bool NotesExactly
	{
		get
		{
			return _NotesExactly;
		}
		set
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			_NotesExactly = value;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " " + value);
			}
		}
	}

	public static bool DefaultNotesExactly => false;

	public static bool NotesStartsWith
	{
		get
		{
			return _NotesStartsWith;
		}
		set
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			_NotesStartsWith = value;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " " + value);
			}
		}
	}

	public static bool DefaultNotesStartsWith => false;

	public static bool NotesContains
	{
		get
		{
			return _NotesContains;
		}
		set
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			_NotesContains = value;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " " + value);
			}
		}
	}

	public static bool DefaultNotesContains => true;

	public static bool SaveFilter
	{
		get
		{
			return _SaveFilter;
		}
		set
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			_SaveFilter = value;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " " + value);
			}
		}
	}

	public static bool DefaultSaveFilter => false;

	public static bool DisplayVirtualMachine(int index)
	{
		string vmInventoryProperty = VISDK.GetVmInventoryProperty(index, "summary.config.template");
		bool sRMPlaceholderBool = VISDK.GetSRMPlaceholderBool(index);
		if (vmInventoryProperty == "True" && !_Template)
		{
			return false;
		}
		if (sRMPlaceholderBool && !_SRMPlaceholder)
		{
			return false;
		}
		if (sRMPlaceholderBool && _SRMPlaceholder)
		{
			return true;
		}
		if (vmInventoryProperty == "False" && !_VirtualMachine)
		{
			return false;
		}
		return true;
	}

	public static bool DisplayPowerstate(int index)
	{
		if (VISDK.GetVmInventoryProperty(index, "summary.config.template") == "True")
		{
			return true;
		}
		string vmInventoryProperty = VISDK.GetVmInventoryProperty(index, "summary.runtime.powerState");
		if (vmInventoryProperty == "poweredOn" && !_PoweredOn)
		{
			return false;
		}
		if (vmInventoryProperty == "poweredOff" && !_PoweredOff)
		{
			return false;
		}
		if (vmInventoryProperty == "suspended" && !_Suspended)
		{
			return false;
		}
		return true;
	}

	public static bool DisplayDataCenter(string dc)
	{
		if (dc == _DataCenter || _DataCenter == "ALL")
		{
			return true;
		}
		return false;
	}

	public static bool DisplayCluster(string cluster)
	{
		if (cluster == _Cluster || _Cluster == "ALL")
		{
			return true;
		}
		return false;
	}

	public static bool DisplayHost(string host)
	{
		if (host == _Host || _Host == "ALL")
		{
			return true;
		}
		return false;
	}

	public static bool DisplayVMName(int index)
	{
		if (_VMName == "")
		{
			return true;
		}
		string vmInventoryProperty = VISDK.GetVmInventoryProperty(index, "summary.config.name");
		if (_VMNameExactly && _VMName.ToUpper() == vmInventoryProperty.ToUpper())
		{
			return true;
		}
		if (_VMNameStartsWith && vmInventoryProperty.ToUpper().StartsWith(_VMName.ToUpper()))
		{
			return true;
		}
		if (_VMNameContains && vmInventoryProperty.ToUpper().Contains(_VMName.ToUpper()))
		{
			return true;
		}
		return false;
	}

	public static bool DisplayNotes(int index)
	{
		try
		{
			if (_Notes == "")
			{
				return true;
			}
			string vmInventoryProperty = VISDK.GetVmInventoryProperty(index, "summary.config.annotation");
			if (_NotesExactly && _Notes.ToUpper() == vmInventoryProperty.ToUpper())
			{
				return true;
			}
			if (_NotesStartsWith && vmInventoryProperty.ToUpper().StartsWith(_Notes.ToUpper()))
			{
				return true;
			}
			if (_NotesContains && vmInventoryProperty.ToUpper().Contains(_Notes.ToUpper()))
			{
				return true;
			}
			CustomFieldValue[] array = null;
			try
			{
				array = VISDK.GetCustomFieldValueForVM(index);
			}
			catch
			{
				return true;
			}
			for (int i = 0; i < array.Length; i++)
			{
				vmInventoryProperty = ((CustomFieldStringValue)array.GetValue(i)).value;
				if (_NotesExactly && _Notes.ToUpper() == vmInventoryProperty.ToUpper())
				{
					return true;
				}
				if (_NotesStartsWith && vmInventoryProperty.ToUpper().StartsWith(_Notes.ToUpper()))
				{
					return true;
				}
				if (_NotesContains && vmInventoryProperty.ToUpper().Contains(_Notes.ToUpper()))
				{
					return true;
				}
			}
		}
		catch
		{
			return false;
		}
		return false;
	}

	public static bool ShowFilterIcon()
	{
		if (_VirtualMachine != DefaultVirtualMachine)
		{
			return true;
		}
		if (_Template != DefaultTemplate)
		{
			return true;
		}
		if (_SRMPlaceholder != DefaultSRMPlaceholder)
		{
			return true;
		}
		if (_PoweredOn != DefaultPoweredOn)
		{
			return true;
		}
		if (_PoweredOff != DefaultPoweredOff)
		{
			return true;
		}
		if (_Suspended != DefaultSuspended)
		{
			return true;
		}
		if (_DataCenter != DefaultDataCenter)
		{
			return true;
		}
		if (_Cluster != DefaultCluster)
		{
			return true;
		}
		if (_Host != DefaultHost)
		{
			return true;
		}
		if (_VMName != DefaultVMName)
		{
			return true;
		}
		if (_Notes != DefaultNotes)
		{
			return true;
		}
		return false;
	}
}
