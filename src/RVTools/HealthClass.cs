using System;
using System.Reflection;
using log4net;
using Vim25Api;

namespace RVTools;

internal class HealthClass
{
	private static readonly ILog logger = LogManager.GetLogger(typeof(HealthClass));

	private static bool _CheckCDROM = true;

	private static bool _CheckUSB = true;

	private static bool _CheckSnapshot = true;

	private static bool _CheckTools = true;

	private static bool _CheckZombies = true;

	private static bool _CheckInconsistentFolderNames = true;

	private static bool _CheckSecurityIssues = true;

	private static bool _CheckPerformanceTips = true;

	private static bool _CheckGuestStorage = true;

	private static int _CheckGuestStoragePercentage = 10;

	private static bool _CheckHostStorage = true;

	private static int _CheckHostStoragePercentage = 15;

	private static bool _CheckVCPUsPerCore = true;

	private static decimal _CheckVCPUsPerCoreMaxvalue = 4m;

	private static bool _CheckVMsOnStorage = true;

	private static int _CheckVMsOnStorageMaxvalue = 16;

	private static bool _CheckCertificateExpire = true;

	private static int _CheckCertificateExpireValue = 90;

	public static bool CheckCDROM
	{
		get
		{
			return _CheckCDROM;
		}
		set
		{
			if (logger.IsDebugEnabled)
			{
				MethodBase currentMethod = MethodBase.GetCurrentMethod();
				logger.Debug(currentMethod.Name + " " + value);
			}
			_CheckCDROM = value;
		}
	}

	public static bool DefaultCheckCDROM => true;

	public static bool CheckUSB
	{
		get
		{
			return _CheckUSB;
		}
		set
		{
			if (logger.IsDebugEnabled)
			{
				MethodBase currentMethod = MethodBase.GetCurrentMethod();
				logger.Debug(currentMethod.Name + " " + value);
			}
			_CheckUSB = value;
		}
	}

	public static bool DefaultCheckUSB => true;

	public static bool CheckSnapshot
	{
		get
		{
			return _CheckSnapshot;
		}
		set
		{
			if (logger.IsDebugEnabled)
			{
				MethodBase currentMethod = MethodBase.GetCurrentMethod();
				logger.Debug(currentMethod.Name + " " + value);
			}
			_CheckSnapshot = value;
		}
	}

	public static bool DefaultSnapshot => true;

	public static bool CheckTools
	{
		get
		{
			return _CheckTools;
		}
		set
		{
			if (logger.IsDebugEnabled)
			{
				MethodBase currentMethod = MethodBase.GetCurrentMethod();
				logger.Debug(currentMethod.Name + " " + value);
			}
			_CheckTools = value;
		}
	}

	public static bool CheckZombies
	{
		get
		{
			return _CheckZombies;
		}
		set
		{
			if (logger.IsDebugEnabled)
			{
				MethodBase currentMethod = MethodBase.GetCurrentMethod();
				logger.Debug(currentMethod.Name + " " + value);
			}
			_CheckZombies = value;
		}
	}

	public static bool CheckInconsistentFolderNames
	{
		get
		{
			return _CheckInconsistentFolderNames;
		}
		set
		{
			if (logger.IsDebugEnabled)
			{
				MethodBase currentMethod = MethodBase.GetCurrentMethod();
				logger.Debug(currentMethod.Name + " " + value);
			}
			_CheckInconsistentFolderNames = value;
		}
	}

	public static bool CheckSecurityIssues
	{
		get
		{
			return _CheckSecurityIssues;
		}
		set
		{
			if (logger.IsDebugEnabled)
			{
				MethodBase currentMethod = MethodBase.GetCurrentMethod();
				logger.Debug(currentMethod.Name + " " + value);
			}
			_CheckSecurityIssues = value;
		}
	}

	public static bool CheckPerformanceTips
	{
		get
		{
			return _CheckPerformanceTips;
		}
		set
		{
			if (logger.IsDebugEnabled)
			{
				MethodBase currentMethod = MethodBase.GetCurrentMethod();
				logger.Debug(currentMethod.Name + " " + value);
			}
			_CheckPerformanceTips = value;
		}
	}

	public static bool DefaultTools => true;

	public static bool DefaultZombies => true;

	public static bool DefaultInconsistentFolderNames => true;

	public static bool DefaultSecurityIssues => true;

	public static bool DefaultPerformanceTips => true;

	public static bool CheckGuestStorage
	{
		get
		{
			return _CheckGuestStorage;
		}
		set
		{
			if (logger.IsDebugEnabled)
			{
				MethodBase currentMethod = MethodBase.GetCurrentMethod();
				logger.Debug(currentMethod.Name + " " + value);
			}
			_CheckGuestStorage = value;
		}
	}

	public static bool DefaultCheckGuestStorage => true;

	public static int CheckGuestStoragePercentage
	{
		get
		{
			return _CheckGuestStoragePercentage;
		}
		set
		{
			if (logger.IsDebugEnabled)
			{
				MethodBase currentMethod = MethodBase.GetCurrentMethod();
				logger.Debug(currentMethod.Name + " " + value);
			}
			_CheckGuestStoragePercentage = value;
		}
	}

	public static int DefaultCheckGuestStoragePercentage => 10;

	public static bool CheckHostStorage
	{
		get
		{
			return _CheckHostStorage;
		}
		set
		{
			if (logger.IsDebugEnabled)
			{
				MethodBase currentMethod = MethodBase.GetCurrentMethod();
				logger.Debug(currentMethod.Name + " " + value);
			}
			_CheckHostStorage = value;
		}
	}

	public static bool DefaultCheckHostStorage => true;

	public static int CheckHostStoragePercentage
	{
		get
		{
			return _CheckHostStoragePercentage;
		}
		set
		{
			if (logger.IsDebugEnabled)
			{
				MethodBase currentMethod = MethodBase.GetCurrentMethod();
				logger.Debug(currentMethod.Name + " " + value);
			}
			_CheckHostStoragePercentage = value;
		}
	}

	public static int DefaultCheckHostStoragePercentage => 15;

	public static bool CheckVCPUsPerCore
	{
		get
		{
			return _CheckVCPUsPerCore;
		}
		set
		{
			if (logger.IsDebugEnabled)
			{
				MethodBase currentMethod = MethodBase.GetCurrentMethod();
				logger.Debug(currentMethod.Name + " " + value);
			}
			_CheckVCPUsPerCore = value;
		}
	}

	public static bool DefaultCheckVCPUsPerCore => true;

	public static decimal CheckVCPUsPerCoreMaxvalue
	{
		get
		{
			return _CheckVCPUsPerCoreMaxvalue;
		}
		set
		{
			if (logger.IsDebugEnabled)
			{
				MethodBase currentMethod = MethodBase.GetCurrentMethod();
				logger.Debug(currentMethod.Name + " " + value);
			}
			_CheckVCPUsPerCoreMaxvalue = value;
		}
	}

	public static decimal DefaultCheckVCPUsPerCoreMaxvalue => 4m;

	public static bool CheckVMsOnStorage
	{
		get
		{
			return _CheckVMsOnStorage;
		}
		set
		{
			if (logger.IsDebugEnabled)
			{
				MethodBase currentMethod = MethodBase.GetCurrentMethod();
				logger.Debug(currentMethod.Name + " " + value);
			}
			_CheckVMsOnStorage = value;
		}
	}

	public static bool CheckCertificateExpire
	{
		get
		{
			return _CheckCertificateExpire;
		}
		set
		{
			if (logger.IsDebugEnabled)
			{
				MethodBase currentMethod = MethodBase.GetCurrentMethod();
				logger.Debug(currentMethod.Name + " " + value);
			}
			_CheckCertificateExpire = value;
		}
	}

	public static bool DefaultCheckVMsOnStorage => true;

	public static bool DefaultCheckCertificateExpire => true;

	public static int CheckVMsOnStorageMaxvalue
	{
		get
		{
			return _CheckVMsOnStorageMaxvalue;
		}
		set
		{
			if (logger.IsDebugEnabled)
			{
				MethodBase currentMethod = MethodBase.GetCurrentMethod();
				logger.Debug(currentMethod.Name + " " + value);
			}
			_CheckVMsOnStorageMaxvalue = value;
		}
	}

	public static int CheckCertificateExpireValue
	{
		get
		{
			return _CheckCertificateExpireValue;
		}
		set
		{
			if (logger.IsDebugEnabled)
			{
				MethodBase currentMethod = MethodBase.GetCurrentMethod();
				logger.Debug(currentMethod.Name + " " + value);
			}
			_CheckCertificateExpireValue = value;
		}
	}

	public static int DefaultCheckVMsOnStorageMaxvalue => 16;

	public static int DefaultCheckCertificateExpireValue => 90;

	public static string CDROMMessage(VirtualDevice VDS)
	{
		if (!_CheckCDROM)
		{
			return null;
		}
		if (VDS is VirtualCdrom)
		{
			if (logger.IsDebugEnabled)
			{
				MethodBase currentMethod = MethodBase.GetCurrentMethod();
				logger.Debug(currentMethod.Name + " Started for object" + VDS.ToString());
			}
			VirtualCdrom virtualCdrom = (VirtualCdrom)VDS;
			if (virtualCdrom.connectable.connected)
			{
				return "VM has a CDROM device connected! " + virtualCdrom.deviceInfo.label;
			}
		}
		return null;
	}

	public static string USBMessage(VirtualDevice VDS)
	{
		if (!_CheckUSB)
		{
			return null;
		}
		if (VDS is VirtualUSB)
		{
			if (logger.IsDebugEnabled)
			{
				MethodBase currentMethod = MethodBase.GetCurrentMethod();
				logger.Debug(currentMethod.Name + " Started for object" + VDS.ToString());
			}
			VirtualUSB virtualUSB = (VirtualUSB)VDS;
			if (virtualUSB.connected)
			{
				return "VM has a USB device connected! " + virtualUSB.deviceInfo.label;
			}
		}
		return null;
	}

	public static string InconsistentFolderMessage(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		if (!_CheckInconsistentFolderNames)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " return null; _CheckInconsistentFolderNames == false ");
			}
			return null;
		}
		string vmInventoryProperty = VISDK.GetVmInventoryProperty(index, "summary.config.name");
		string vmInventoryProperty2 = VISDK.GetVmInventoryProperty(index, "summary.config.vmPathName");
		char[] separator = new char[2] { ']', '/' };
		try
		{
			string[] array = vmInventoryProperty2.Split(separator, StringSplitOptions.RemoveEmptyEntries);
			if (array.Length == 2)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(methodBase.Name + "Inconsistent Foldername! VMname = " + vmInventoryProperty + " Foldername = /");
				}
				return "Inconsistent Foldername! VMname = " + vmInventoryProperty + " Foldername = /";
			}
			string text = array[1].TrimStart(' ');
			if (text != vmInventoryProperty)
			{
				string text2 = (string)VISDK.vSANFriendlyNames[text];
				if (text2 == null)
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + "Inconsistent Foldername! VMname = " + vmInventoryProperty + " Foldername = " + array[1].TrimStart(' '));
					}
					return "Inconsistent Foldername! VMname = " + vmInventoryProperty + " Foldername = " + text;
				}
				if (text2 != vmInventoryProperty)
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + "Inconsistent Foldername! VMname = " + vmInventoryProperty + " Foldername = " + array[1].TrimStart(' '));
					}
					return "Inconsistent Foldername! VMname = " + vmInventoryProperty + " Foldername = " + text2;
				}
			}
		}
		catch (Exception exception)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + "Health Check Inconsistent Folder Names", exception);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " return null");
		}
		return null;
	}

	public static string SnapshotMessage(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		if (!_CheckSnapshot)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " return null; _CheckSnapshot == false ");
			}
			return null;
		}
		VirtualMachineSnapshotTree[] snapShotTree = VISDK.GetSnapShotTree(index);
		if (snapShotTree != null)
		{
			VirtualMachineSnapshotTree virtualMachineSnapshotTree = snapShotTree[0];
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + "VM has an active snapshot! " + virtualMachineSnapshotTree.name + " created on " + virtualMachineSnapshotTree.createTime.ToLocalTime().ToString());
			}
			return "VM has an active snapshot! " + virtualMachineSnapshotTree.name + " created on " + virtualMachineSnapshotTree.createTime.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
		}
		return null;
	}

	public static string ToolsMessage(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		if (!_CheckTools)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " return null; _CheckTools == false ");
			}
			return null;
		}
		if (VISDK.GetVmInventoryProperty(index, "summary.runtime.powerState") != "poweredOn")
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " return null; Powerstate != poweredOn ");
			}
			return null;
		}
		try
		{
			if (VISDK.GetVmInventoryProperty(index, "guest.toolsStatus").Contains("Ok"))
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(methodBase.Name + " return null; guest.toolStatus = Ok ");
				}
				return null;
			}
		}
		catch
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " return VMware tools not installed!");
			}
			return "VMware tools not installed!";
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " return VMware tools are out of date, not running or not installed!");
		}
		return "VMware tools are out of date, not running or not installed!";
	}

	public static string ConsolidationNeededMessage(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		try
		{
			if (VISDK.GetVmInventoryProperty(index, "runtime.consolidationNeeded").Contains("True"))
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(methodBase.Name + " return Consolidation needed; runtime.consolidationNeeded == true");
				}
				return "Consolidation needed";
			}
		}
		catch
		{
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " return null");
		}
		return null;
	}

	public static string GuestStorageMessage(GuestDiskInfo disk)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for object " + disk.ToString());
		}
		if (!_CheckGuestStorage)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " return null; _CheckGuestStorage == false");
			}
			return null;
		}
		long num = 0L;
		long num2 = 0L;
		try
		{
			num = disk.freeSpace / (disk.capacity / 100);
			num2 = disk.freeSpace / 1048576;
			if (num < _CheckGuestStoragePercentage)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(methodBase.Name + " FreeSpacePercentage < _CheckGuestStoragePercentage " + num + " < " + _CheckGuestStoragePercentage);
				}
				if (logger.IsDebugEnabled)
				{
					logger.Debug(methodBase.Name + " return On " + disk.diskPath + " is " + num + "% disk space available! The threshold value is " + _CheckGuestStoragePercentage + "%. Free MB = " + num2);
				}
				return "On " + disk.diskPath + " is " + num + "% disk space available! The threshold value is " + _CheckGuestStoragePercentage + "%. Free MB = " + num2.ToString("N0");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " No message for " + disk.diskPath + " is " + num + "% disk space available! The threshold value is " + _CheckGuestStoragePercentage + "%");
			}
		}
		catch
		{
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " return null");
		}
		return null;
	}

	public static string HostStorageMessage(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		if (!_CheckHostStorage)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " return null; _CheckHostStorage == false");
			}
			return null;
		}
		if (VISDK.GetDatastoreInventoryProperty(index, "summary.name") == "/storage/archive")
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " return null; summary name == /storage/archive");
			}
			return null;
		}
		long num = 0L;
		long num2 = 0L;
		try
		{
			num = VISDK.GetDatastoreInventoryPropertyInt(index, "summary.freeSpace") / (VISDK.GetDatastoreInventoryPropertyInt(index, "summary.capacity") / 100);
			num2 = VISDK.GetDatastoreInventoryPropertyInt(index, "summary.freeSpace") / 1048576;
			if (num < _CheckHostStoragePercentage)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(methodBase.Name + " FreeSpacePercentage < _CheckHostStoragePercentage " + num + " < " + _CheckHostStoragePercentage);
				}
				if (logger.IsDebugEnabled)
				{
					logger.Debug(methodBase.Name + " Return On " + VISDK.GetDatastoreInventoryProperty(index, "summary.name") + " is " + num + "% disk space available! The threshold value is " + _CheckHostStoragePercentage + "%. Free MB = " + num2);
				}
				return "On " + VISDK.GetDatastoreInventoryProperty(index, "summary.name") + " is " + num + "% disk space available! The threshold value is " + _CheckHostStoragePercentage + "%. Free MB = " + num2.ToString("N0");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " No message for " + VISDK.GetDatastoreInventoryProperty(index, "summary.name") + " is " + num + "% disk space available! The threshold value is " + _CheckHostStoragePercentage + "%");
			}
		}
		catch
		{
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " return null");
		}
		return null;
	}

	public static string VCPUsPerCore(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		if (!_CheckVCPUsPerCore)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " return null; _CheckVCPUsPerCore == false");
			}
			return null;
		}
		decimal num = 0m;
		decimal num2 = VISDK.GetHostInventoryPropertyInt(index, "vCPUs");
		decimal num3 = VISDK.GetHostInventoryPropertyInt(index, "summary.hardware.numCpuCores");
		try
		{
			num = num2 / num3;
		}
		catch
		{
		}
		if (num > _CheckVCPUsPerCoreMaxvalue)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " VCPUsPerCore > _CheckVCPUsPerCoreMaxvalue " + num + " > " + _CheckVCPUsPerCoreMaxvalue);
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " return there are " + num.ToString("F2") + " virtual CPUs active per core on this host. The threshold value is " + _CheckVCPUsPerCoreMaxvalue);
			}
			return "There are " + num.ToString("F2") + " virtual CPUs active per core on this host. The threshold value is " + _CheckVCPUsPerCoreMaxvalue;
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " No message there are " + num.ToString("F2") + " virtual CPUs active per core on this host. The threshold value is " + _CheckVCPUsPerCoreMaxvalue);
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " return null");
		}
		return null;
	}

	public static string VMsOnStorage(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		if (!_CheckVMsOnStorage)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " return null; _CheckVMsOnStorage == false");
			}
			return null;
		}
		int num = (int)VISDK.GetDatastoreInventoryPropertyInt(index, "vm");
		if (num > _CheckVMsOnStorageMaxvalue)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " vms > _CheckVMsOnStorageMaxvalue " + num + " > " + _CheckVMsOnStorageMaxvalue);
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " return there are " + num + " VMs active on this datastore. The threshold value is " + _CheckVMsOnStorageMaxvalue);
			}
			return "There are " + num + " VMs active on this datastore. The threshold value is " + _CheckVMsOnStorageMaxvalue;
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " No message there are " + num + " VMs active on this datastore. The threshold value is " + _CheckVMsOnStorageMaxvalue);
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " return null");
		}
		return null;
	}
}
