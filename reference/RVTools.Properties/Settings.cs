using System.CodeDom.Compiler;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace RVTools.Properties;

[CompilerGenerated]
[GeneratedCode("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.7.0.0")]
internal sealed class Settings : ApplicationSettingsBase
{
	private static Settings defaultInstance = (Settings)SettingsBase.Synchronized(new Settings());

	public static Settings Default => defaultInstance;

	[UserScopedSetting]
	[DebuggerNonUserCode]
	public StringCollection VIHOSTS
	{
		get
		{
			return (StringCollection)this["VIHOSTS"];
		}
		set
		{
			this["VIHOSTS"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("True")]
	public bool HCCDROM
	{
		get
		{
			return (bool)this["HCCDROM"];
		}
		set
		{
			this["HCCDROM"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("True")]
	public bool HCUSB
	{
		get
		{
			return (bool)this["HCUSB"];
		}
		set
		{
			this["HCUSB"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("True")]
	public bool HCSnapshot
	{
		get
		{
			return (bool)this["HCSnapshot"];
		}
		set
		{
			this["HCSnapshot"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("True")]
	public bool HCTools
	{
		get
		{
			return (bool)this["HCTools"];
		}
		set
		{
			this["HCTools"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("True")]
	public bool HCGuestStorage
	{
		get
		{
			return (bool)this["HCGuestStorage"];
		}
		set
		{
			this["HCGuestStorage"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("10")]
	public int HCGuestStoragePercentage
	{
		get
		{
			return (int)this["HCGuestStoragePercentage"];
		}
		set
		{
			this["HCGuestStoragePercentage"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("True")]
	public bool HCHostStorage
	{
		get
		{
			return (bool)this["HCHostStorage"];
		}
		set
		{
			this["HCHostStorage"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("10")]
	public int HCHostStoragePercentage
	{
		get
		{
			return (int)this["HCHostStoragePercentage"];
		}
		set
		{
			this["HCHostStoragePercentage"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("True")]
	public bool HCVMPerCore
	{
		get
		{
			return (bool)this["HCVMPerCore"];
		}
		set
		{
			this["HCVMPerCore"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("4")]
	public decimal HCVMPerCoreMaxvalue
	{
		get
		{
			return (decimal)this["HCVMPerCoreMaxvalue"];
		}
		set
		{
			this["HCVMPerCoreMaxvalue"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("True")]
	public bool HCVMsOnStorage
	{
		get
		{
			return (bool)this["HCVMsOnStorage"];
		}
		set
		{
			this["HCVMsOnStorage"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("16")]
	public int HCVMsOnStorageMaxvalue
	{
		get
		{
			return (int)this["HCVMsOnStorageMaxvalue"];
		}
		set
		{
			this["HCVMsOnStorageMaxvalue"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("True")]
	public bool HCZombies
	{
		get
		{
			return (bool)this["HCZombies"];
		}
		set
		{
			this["HCZombies"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("True")]
	public bool HCInconsistentFolderNames
	{
		get
		{
			return (bool)this["HCInconsistentFolderNames"];
		}
		set
		{
			this["HCInconsistentFolderNames"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("")]
	public string LastHost
	{
		get
		{
			return (string)this["LastHost"];
		}
		set
		{
			this["LastHost"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("True")]
	public bool FVirtualMachine
	{
		get
		{
			return (bool)this["FVirtualMachine"];
		}
		set
		{
			this["FVirtualMachine"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("True")]
	public bool FTemplate
	{
		get
		{
			return (bool)this["FTemplate"];
		}
		set
		{
			this["FTemplate"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("True")]
	public bool FPoweredOn
	{
		get
		{
			return (bool)this["FPoweredOn"];
		}
		set
		{
			this["FPoweredOn"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("True")]
	public bool FPoweredOff
	{
		get
		{
			return (bool)this["FPoweredOff"];
		}
		set
		{
			this["FPoweredOff"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("True")]
	public bool FSuspended
	{
		get
		{
			return (bool)this["FSuspended"];
		}
		set
		{
			this["FSuspended"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("False")]
	public bool FVMNameExactly
	{
		get
		{
			return (bool)this["FVMNameExactly"];
		}
		set
		{
			this["FVMNameExactly"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("False")]
	public bool FVMNameStartsWith
	{
		get
		{
			return (bool)this["FVMNameStartsWith"];
		}
		set
		{
			this["FVMNameStartsWith"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("True")]
	public bool FVMNameContains
	{
		get
		{
			return (bool)this["FVMNameContains"];
		}
		set
		{
			this["FVMNameContains"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("")]
	public string FVMName
	{
		get
		{
			return (string)this["FVMName"];
		}
		set
		{
			this["FVMName"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("False")]
	public bool FNotesExactly
	{
		get
		{
			return (bool)this["FNotesExactly"];
		}
		set
		{
			this["FNotesExactly"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("False")]
	public bool FNotesStartsWith
	{
		get
		{
			return (bool)this["FNotesStartsWith"];
		}
		set
		{
			this["FNotesStartsWith"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("True")]
	public bool FNotesContains
	{
		get
		{
			return (bool)this["FNotesContains"];
		}
		set
		{
			this["FNotesContains"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("")]
	public string FNotes
	{
		get
		{
			return (string)this["FNotes"];
		}
		set
		{
			this["FNotes"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("")]
	public string FDataCenter
	{
		get
		{
			return (string)this["FDataCenter"];
		}
		set
		{
			this["FDataCenter"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("")]
	public string FCluster
	{
		get
		{
			return (string)this["FCluster"];
		}
		set
		{
			this["FCluster"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("")]
	public string FHost
	{
		get
		{
			return (string)this["FHost"];
		}
		set
		{
			this["FHost"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("False")]
	public bool FSaveFilter
	{
		get
		{
			return (bool)this["FSaveFilter"];
		}
		set
		{
			this["FSaveFilter"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("False")]
	public bool vInfoShowFolder
	{
		get
		{
			return (bool)this["vInfoShowFolder"];
		}
		set
		{
			this["vInfoShowFolder"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("False")]
	public bool vInfoShowAppName
	{
		get
		{
			return (bool)this["vInfoShowAppName"];
		}
		set
		{
			this["vInfoShowAppName"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("False")]
	public bool vInfoShowResourcePool
	{
		get
		{
			return (bool)this["vInfoShowResourcePool"];
		}
		set
		{
			this["vInfoShowResourcePool"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("True")]
	public bool UseWindowsCredentials
	{
		get
		{
			return (bool)this["UseWindowsCredentials"];
		}
		set
		{
			this["UseWindowsCredentials"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("False")]
	public bool AutoRefresh
	{
		get
		{
			return (bool)this["AutoRefresh"];
		}
		set
		{
			this["AutoRefresh"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("10")]
	public int AutoRefreshInterval
	{
		get
		{
			return (int)this["AutoRefreshInterval"];
		}
		set
		{
			this["AutoRefreshInterval"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("False")]
	public bool ExcludeAnnotations
	{
		get
		{
			return (bool)this["ExcludeAnnotations"];
		}
		set
		{
			this["ExcludeAnnotations"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("False")]
	public bool ExcludeTags
	{
		get
		{
			return (bool)this["ExcludeTags"];
		}
		set
		{
			this["ExcludeTags"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("False")]
	public bool GetFriendlyNames
	{
		get
		{
			return (bool)this["GetFriendlyNames"];
		}
		set
		{
			this["GetFriendlyNames"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("True")]
	public bool FSRMPlaceholder
	{
		get
		{
			return (bool)this["FSRMPlaceholder"];
		}
		set
		{
			this["FSRMPlaceholder"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("False")]
	public bool GetFileInfo
	{
		get
		{
			return (bool)this["GetFileInfo"];
		}
		set
		{
			this["GetFileInfo"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("True")]
	public bool HCSecurityIssues
	{
		get
		{
			return (bool)this["HCSecurityIssues"];
		}
		set
		{
			this["HCSecurityIssues"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("True")]
	public bool HCPerformanceTips
	{
		get
		{
			return (bool)this["HCPerformanceTips"];
		}
		set
		{
			this["HCPerformanceTips"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("90")]
	public int HCCertificateExpireDays
	{
		get
		{
			return (int)this["HCCertificateExpireDays"];
		}
		set
		{
			this["HCCertificateExpireDays"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("True")]
	public bool HCCertificateExpire
	{
		get
		{
			return (bool)this["HCCertificateExpire"];
		}
		set
		{
			this["HCCertificateExpire"] = value;
		}
	}
}
