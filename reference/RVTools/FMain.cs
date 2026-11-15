using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ClosedXML.Excel;
using log4net;
using log4net.Config;
using RVTools.CustomerExperience;
using RVTools.Properties;
using RVTools.Utils;
using Vim25Api;

namespace RVTools;

public class FMain : Form
{
	public bool myFilter;

	private static Thread BrowseThread;

	private static Thread FileInfoThread;

	private static string ExportDir = null;

	private static string ExportFile = null;

	private static bool ExcludeCustomAnnotations = false;

	private static bool ExcludeTags = false;

	private static bool GetFriendlyNames = false;

	private static bool GetFileInfo = false;

	private static bool DBColumnNames = false;

	private static string CommandToExecute = null;

	private static string InstanceUUID = null;

	private static int KeepAliveCounter = 0;

	private bool isBatch;

	private static FWait PleaseWait;

	private static bool TabPagevInfo_Refresh_data = true;

	private static bool TabPagevCPU_Refresh_data = true;

	private static bool TabPagevMemory_Refresh_data = true;

	private static bool TabPagevDisk_Refresh_data = true;

	private static bool TabPagevPartition_Refresh_data = true;

	private static bool TabPagevNetwork_Refresh_data = true;

	private static bool TabPagevCD_Refresh_data = true;

	private static bool TabPagevUSB_Refresh_data = true;

	private static bool TabPagevSnapshot_Refresh_data = true;

	private static bool TabPagevTools_Refresh_data = true;

	private static bool TabPagevRP_Refresh_data = true;

	private static bool TabPagevSource_Refresh_data = true;

	private static bool TabPagevCluster_Refresh_data = true;

	private static bool TabPagevHost_Refresh_data = true;

	private static bool TabPagevHBA_Refresh_data = true;

	private static bool TabPagevNIC_Refresh_data = true;

	private static bool TabPagevSwitch_Refresh_data = true;

	private static bool TabPagevPort_Refresh_data = true;

	private static bool TabPagedvSwitch_Refresh_data = true;

	private static bool TabPagedvPort_Refresh_data = true;

	private static bool TabPagevSC_VMK_Refresh_data = true;

	private static bool TabPagevDatastore_Refresh_data = true;

	private static bool TabPagevLicense_Refresh_data = true;

	private static bool TabPagevFileInfo_Refresh_data = true;

	private static bool TabPagevMultiPath_Refresh_data = true;

	private static readonly ILog logger = LogManager.GetLogger(typeof(FMain));

	public static readonly ITrace Trace = new Log4NetTrace(logger);

	private static SecureString TelemetryConnectionString = "InstrumentationKey=4cfabc3e-b75a-49bb-9fff-c7bf4b2cabde;IngestionEndpoint=https://eastus2-3.in.applicationinsights.azure.com/;LiveEndpoint=https://eastus2.livediagnostics.monitor.azure.com/;ApplicationId=8a39954a-347b-4c1d-b69d-606098d0e09f".Aggregate(new SecureString(), (SecureString ss, char c) =>
	{
		ss.AppendChar(c);
		return ss;
	});

	private static ITelemetryCollector _telemetryCollector = null;

	private static bool TelemetryEnabled = true;

	private static UserNotification UserNotification = new UserNotification("FMain", Trace, Telemetry);

	private static List<VirtualMachineSnapshotTree> _SnapShots = new List<VirtualMachineSnapshotTree>();

	private IContainer components;

	private StatusStrip statusStrip1;

	private ToolStripStatusLabel StatusUsername;

	private ToolStripStatusLabel StatusServer;

	private ToolStripStatusLabel StatusVCFullname;

	private MenuStrip menuStrip1;

	private ToolStripMenuItem fileToolStripMenuItem;

	private ToolStripMenuItem exitToolStripMenuItem;

	private ToolStripMenuItem viewToolStripMenuItem;

	private ToolStripMenuItem helpToolStripMenuItem;

	private ToolStripMenuItem aboutToolStripMenuItem;

	private ToolStripMenuItem VMToolsToolStripMenuItem;

	private ToolStripMenuItem VMtoolStripMenuItem1;

	private ToolStripMenuItem NetworktoolStripMenuItem1;

	private ToolStripMenuItem toolStripMenuItem1;

	private ToolStripMenuItem toolStripMenuItem2;

	private ToolStripMenuItem toolStripMenuItem3;

	private ToolStripMenuItem CheckForUpdatestoolStripMenuItem;

	private System.Windows.Forms.Timer KeepAliveTimer;

	private ToolStripStatusLabel StatusGridCounter;

	private System.Windows.Forms.Timer DisplayDataGridTimer;

	private ToolStripMenuItem CPUToolStripMenuItem;

	private ToolStripMenuItem MemoryToolStripMenuItem;

	private ToolStripMenuItem SnapshotToolStripMenuItem;

	private ToolStripMenuItem ExportToolStripMenuItem;

	private ToolStripSeparator toolStripSeparator3;

	private ToolStripStatusLabel StatusFilter;

	private ToolStripMenuItem toolStripMenuESX;

	private ToolStripMenuItem vHostToolstripMenuItem;

	private ToolStripMenuItem DatastoreToolStripMenuItem;

	private ToolStripMenuItem toolStripMenuView;

	private ToolStripMenuItem refreshToolStripMenuItem;

	private ToolStripMenuItem healhtToolStripMenuItem;

	private ToolStripMenuItem HealthPropertiesToolStripMenuItem;

	private ToolStripSeparator toolStripMenuItem4;

	private ToolStripMenuItem HealthCheckToolStripMenuItem;

	private ToolStripSeparator toolStripMenuItem5;

	private ToolStripMenuItem documentationToolStripMenuItem;

	private ToolStripMenuItem PartitiontoolStripMenuItem;

	private ToolStripMenuItem vSwitchToolStripMenuItem;

	private ToolStripMenuItem vPortToolStripMenuItem;

	private ToolStripMenuItem ExportAllMenuItem;

	private ToolStripMenuItem toolStripMenuFilter;

	private ToolStripMenuItem NICToolStripMenuItem;

	private ToolStripMenuItem ExportAll2xlsMenuItem;

	private ToolStripMenuItem vConsoleToolStripMenuItem;

	private ToolStripSeparator toolStripSeparator1;

	private ToolStripSeparator toolStripSeparator2;

	private ToolStripMenuItem twittertoolStripMenuItem;

	private ToolStripMenuItem dvSwitchtoolStripMenuItem;

	private ToolStripMenuItem dvPorttoolStripMenuItem;

	private ToolStripMenuItem vHBAToolStripMenuItem;

	private ToolStripMenuItem toolStripMenuEdit;

	private ToolStripMenuItem toolStripPreferences;

	private ToolStripMenuItem vRPToolStripMenuItem;

	private ToolStripMenuItem vClusterStripMenuItem;

	private ToolStripMenuItem multiPathToolStripMenuItem;

	private ToolStripStatusLabel StatusLastRefresh;

	private ToolStripMenuItem licenseToolStripMenuItem;

	private TabPage tabvLicense;

	private Panel panel27;

	private DataGridView dataGridvLicense;

	private TabPage tabvHealth;

	private Panel panel15;

	private DataGridView dataGridvHealth;

	private TabPage tabvMultiPath;

	private Panel panel26;

	private DataGridView dataGridvMultiPath;

	private TabPage tabvDatastore;

	private Panel panel3;

	private DataGridView dataGridvDatastore;

	private TabPage tabvSC_VMK;

	private Panel panel20;

	private DataGridView dataGridvSC_VMK;

	private TabPage tabdvPort;

	private Panel panel22;

	private DataGridView dataGriddvPort;

	private TabPage tabdvSwitch;

	private Panel panel21;

	private DataGridView dataGriddvSwitch;

	private TabPage tabvPort;

	private Panel panel18;

	private DataGridView dataGridvPort;

	private TabPage tabvSwitch;

	private Panel panel17;

	private DataGridView dataGridvSwitch;

	private TabPage tabvNIC;

	private Panel panel19;

	private DataGridView dataGridvNIC;

	private TabPage tabvHBA;

	private Panel panel23;

	private DataGridView dataGridvHBA;

	private TabPage tabvHost;

	private Panel panel14;

	private DataGridView dataGridvHost;

	private TabPage tabvCluster;

	private Panel panel25;

	private DataGridView dataGridvCluster;

	private TabPage tabvRP;

	private Panel panel24;

	private DataGridView dataGridvRP;

	private TabPage tabvTools;

	private Panel panel10;

	private DataGridView dataGridvTools;

	private Panel panel11;

	private Button button1;

	private TabPage tabvSnapshot;

	private Panel panel9;

	private DataGridView dataGridvSnapshot;

	private TabPage tabvCD;

	private Panel panel12;

	private DataGridView dataGridvCD;

	private Panel panel13;

	private Button buttonDisconnectCD;

	private TabPage tabvUSB;

	private Panel panel1;

	private DataGridView dataGridvUSB;

	private Panel panel2;

	private Button buttonDisconnectUSB;

	private TabPage tabvNetwork;

	private Panel panel5;

	private DataGridView dataGridvNetwork;

	private TabPage tabvPartition;

	private Panel panel16;

	private DataGridView dataGridvPartition;

	private TabPage tabvDisk;

	private Panel panel6;

	private DataGridView dataGridvDisk;

	private TabPage tabvMemory;

	private Panel panel8;

	private DataGridView dataGridvMemory;

	private TabPage tabvCPU;

	private Panel panel7;

	private DataGridView dataGridvCPU;

	private TabPage tabvInfo;

	private Panel panel4;

	private DataGridView dataGridvInfo;

	private TabControl TabPages;

	private DataGridViewTextBoxColumn vMultiPathHost;

	private DataGridViewTextBoxColumn vMultiPathCluster;

	private DataGridViewTextBoxColumn vMultiPathDataCenter;

	private DataGridViewTextBoxColumn vMultiPathDatastoreName;

	private DataGridViewTextBoxColumn vMultiPathDiskName;

	private DataGridViewTextBoxColumn vMultiPathDisplayName;

	private DataGridViewTextBoxColumn vMultiPathPolicy;

	private DataGridViewTextBoxColumn vMultiPathOperationalState;

	private DataGridViewTextBoxColumn vMultiPathPath1;

	private DataGridViewTextBoxColumn vMultiPathPath1State;

	private DataGridViewTextBoxColumn vMultiPathPath2;

	private DataGridViewTextBoxColumn vMultiPathPath2State;

	private DataGridViewTextBoxColumn vMultiPathPath3;

	private DataGridViewTextBoxColumn vMultiPathPath3State;

	private DataGridViewTextBoxColumn vMultiPathPath4;

	private DataGridViewTextBoxColumn vMultiPathPath4State;

	private DataGridViewTextBoxColumn vMultiPathPath5;

	private DataGridViewTextBoxColumn vMultiPathPath5State;

	private DataGridViewTextBoxColumn vMultiPathPath6;

	private DataGridViewTextBoxColumn vMultiPathPath6State;

	private DataGridViewTextBoxColumn vMultiPathPath7;

	private DataGridViewTextBoxColumn vMultiPathPath7State;

	private DataGridViewTextBoxColumn vMultiPathPath8;

	private DataGridViewTextBoxColumn vMultiPathPath8State;

	private DataGridViewTextBoxColumn vMultiPathVStorageSupport;

	private DataGridViewTextBoxColumn vMultiPathQueueDepth;

	private DataGridViewTextBoxColumn vMultiPathVendor;

	private DataGridViewTextBoxColumn vMultiPathModel;

	private DataGridViewTextBoxColumn vMultiPathRevision;

	private DataGridViewTextBoxColumn vMultiPathSCSILevel;

	private DataGridViewTextBoxColumn vMultiPathSerialNumber;

	private DataGridViewTextBoxColumn vMultiPathUUID;

	private DataGridViewTextBoxColumn vMultiPathObjectID;

	private DataGridViewTextBoxColumn vMultiPathVISDKServer;

	private DataGridViewTextBoxColumn vMultiPathInstanceUUID;

	private DataGridViewTextBoxColumn vSC_VMKHost;

	private DataGridViewTextBoxColumn vSC_VMKDatacenter;

	private DataGridViewTextBoxColumn vSC_VMKCluster;

	private DataGridViewTextBoxColumn vSC_VMKPortGroup;

	private DataGridViewTextBoxColumn vSC_VMKDevice;

	private DataGridViewTextBoxColumn vSC_VMKMAC;

	private DataGridViewTextBoxColumn vSC_VMKDHCP;

	private DataGridViewTextBoxColumn vSC_VMKIP;

	private DataGridViewTextBoxColumn vSC_VMKIP6;

	private DataGridViewTextBoxColumn vSC_VMKMask;

	private DataGridViewTextBoxColumn vSC_VMKGateway;

	private DataGridViewTextBoxColumn vSC_VMKGateway6;

	private DataGridViewTextBoxColumn vSC_VMKMTU;

	private DataGridViewTextBoxColumn vSC_VMKVISDKServer;

	private DataGridViewTextBoxColumn vSC_VMKInstanceUUID;

	private DataGridViewTextBoxColumn vHBAHostName;

	private DataGridViewTextBoxColumn vHBADatacenter;

	private DataGridViewTextBoxColumn vHBACluster;

	private DataGridViewTextBoxColumn vHBADevice;

	private DataGridViewTextBoxColumn vHBAType;

	private DataGridViewTextBoxColumn vHBAStatus;

	private DataGridViewTextBoxColumn vHBABus;

	private DataGridViewTextBoxColumn vHBAPci;

	private DataGridViewTextBoxColumn vHBADriver;

	private DataGridViewTextBoxColumn vHBAModel;

	private DataGridViewTextBoxColumn vHBAWorldWideName;

	private DataGridViewTextBoxColumn vHBAVISDKServer;

	private DataGridViewTextBoxColumn vHBAInstanceUUID;

	private DataGridViewTextBoxColumn vLicenseName;

	private DataGridViewTextBoxColumn vLicenseKey;

	private DataGridViewTextBoxColumn vLicenseLabels;

	private DataGridViewTextBoxColumn vLicenseCostUnit;

	private DataGridViewTextBoxColumn vLicenseTotal;

	private DataGridViewTextBoxColumn vLicenseUsed;

	private DataGridViewTextBoxColumn vLicenseExpirationDate;

	private DataGridViewTextBoxColumn vLicenseFeatures;

	private DataGridViewTextBoxColumn vLicenseVISDKServer;

	private DataGridViewTextBoxColumn vLicenseInstanceUUID;

	private DataGridViewTextBoxColumn vPortHost;

	private DataGridViewTextBoxColumn vPortDatacenter;

	private DataGridViewTextBoxColumn vPortCluster;

	private DataGridViewTextBoxColumn vPortName;

	private DataGridViewTextBoxColumn vPortSwitch;

	private DataGridViewTextBoxColumn vPortVLAN;

	private DataGridViewTextBoxColumn vPortAllowPromiscuous;

	private DataGridViewTextBoxColumn vPortMacChanges;

	private DataGridViewTextBoxColumn vPortForgedTransmits;

	private DataGridViewTextBoxColumn vPortTrafficShaping;

	private DataGridViewTextBoxColumn vPortAverageBandwidth;

	private DataGridViewTextBoxColumn vPortPeakBandwidth;

	private DataGridViewTextBoxColumn vPortBurstSize;

	private DataGridViewTextBoxColumn vPortPolicy;

	private DataGridViewTextBoxColumn vPortReversePolicy;

	private DataGridViewTextBoxColumn vPortNotifySwitches;

	private DataGridViewTextBoxColumn vPortRollingOrder;

	private DataGridViewTextBoxColumn vPortCsumOffload;

	private DataGridViewTextBoxColumn vPortTCPSegmentation;

	private DataGridViewTextBoxColumn vPortZeroCopyXmit;

	private DataGridViewTextBoxColumn vPortVISDKServer;

	private DataGridViewTextBoxColumn vPortInstanceUUID;

	private DataGridViewTextBoxColumn vSwitchHost;

	private DataGridViewTextBoxColumn vSwitchDatacenter;

	private DataGridViewTextBoxColumn vSwitchCluster;

	private DataGridViewTextBoxColumn vSwitchName;

	private DataGridViewTextBoxColumn vSwitchNumPorts;

	private DataGridViewTextBoxColumn vSwitchFreePorts;

	private DataGridViewTextBoxColumn vSwitchAllowPromiscuous;

	private DataGridViewTextBoxColumn vSwitchMacChanges;

	private DataGridViewTextBoxColumn vSwitchForgedTransmits;

	private DataGridViewTextBoxColumn vSwitchTrafficShaping;

	private DataGridViewTextBoxColumn vSwitchAverageBandwidth;

	private DataGridViewTextBoxColumn vSwitchPeakBandwidth;

	private DataGridViewTextBoxColumn vSwitchBurstSize;

	private DataGridViewTextBoxColumn vSwitchPolicy;

	private DataGridViewTextBoxColumn vSwitchReversePolicy;

	private DataGridViewTextBoxColumn vSwitchNotifySwitches;

	private DataGridViewTextBoxColumn vSwitchRollingOrder;

	private DataGridViewTextBoxColumn vSwitchCsumOffload;

	private DataGridViewTextBoxColumn vSwitchTCPSegmentation;

	private DataGridViewTextBoxColumn vSwitchZeroCopyXmit;

	private DataGridViewTextBoxColumn vSwitchMTU;

	private DataGridViewTextBoxColumn vSwitchVISDKServer;

	private DataGridViewTextBoxColumn vSwitchInstanceUUID;

	private DataGridViewTextBoxColumn vNicHostName;

	private DataGridViewTextBoxColumn vNicDatacenter;

	private DataGridViewTextBoxColumn vNicCluster;

	private DataGridViewTextBoxColumn vNicDevice;

	private DataGridViewTextBoxColumn vNicDriver;

	private DataGridViewTextBoxColumn vNicSpeed;

	private DataGridViewTextBoxColumn vNicDuplex;

	private DataGridViewTextBoxColumn vNicMac;

	private DataGridViewTextBoxColumn vNicSwitch;

	private DataGridViewTextBoxColumn vNicUplinkPort;

	private DataGridViewTextBoxColumn vNicPci;

	private DataGridViewTextBoxColumn vNicWakeOn;

	private DataGridViewTextBoxColumn vNICVISDKServer;

	private DataGridViewTextBoxColumn vNICInstanceUUID;

	private DataGridViewTextBoxColumn dvSwitchName;

	private DataGridViewTextBoxColumn dvSwitchDatacenter;

	private DataGridViewTextBoxColumn dvSwitchProductInfoName;

	private DataGridViewTextBoxColumn dvSwitchProductInfoVendor;

	private DataGridViewTextBoxColumn dvSwitchProductInfoVersion;

	private DataGridViewTextBoxColumn dvSwitchDescription;

	private DataGridViewTextBoxColumn dvSwitchCreateTime;

	private DataGridViewTextBoxColumn dvSwitchHostMembers;

	private DataGridViewTextBoxColumn dvSwitchMaxPorts;

	private DataGridViewTextBoxColumn dvSwitchNumPorts;

	private DataGridViewTextBoxColumn dvSwitchNumVms;

	private DataGridViewTextBoxColumn dvSwitchInTrafficEnabled;

	private DataGridViewTextBoxColumn dvSwitchInTrafficAvg;

	private DataGridViewTextBoxColumn dvSwitchInTrafficPeak;

	private DataGridViewTextBoxColumn dvSwitchInTrafficBurst;

	private DataGridViewTextBoxColumn dvSwitchOutTrafficEnabled;

	private DataGridViewTextBoxColumn dvSwitchOutTrafficAvg;

	private DataGridViewTextBoxColumn dvSwitchOutTrafficPeak;

	private DataGridViewTextBoxColumn dvSwitchOutTrafficBurst;

	private DataGridViewTextBoxColumn dvSwitchCDPProtocol;

	private DataGridViewTextBoxColumn dvSwitchCDPoperation;

	private DataGridViewTextBoxColumn dvSwitchLacpName;

	private DataGridViewTextBoxColumn dvSwitchLacpMode;

	private DataGridViewTextBoxColumn dvSwitchLacpLoadBalance;

	private DataGridViewTextBoxColumn dvSwitchMaxMtu;

	private DataGridViewTextBoxColumn dvSwitchContactInfoContact;

	private DataGridViewTextBoxColumn dvSwitchContactInfoName;

	private DataGridViewTextBoxColumn dvSwitchObjectID;

	private DataGridViewTextBoxColumn dvSwitchVISDKServer;

	private DataGridViewTextBoxColumn dvSwitchInstanceUUID;

	private DataGridViewTextBoxColumn dvPortName;

	private DataGridViewTextBoxColumn dvPortSwitch;

	private DataGridViewTextBoxColumn dvPortType;

	private DataGridViewTextBoxColumn dvPortNumPorts;

	private DataGridViewTextBoxColumn dvPortVLAN;

	private DataGridViewTextBoxColumn dvPortSpeed;

	private DataGridViewTextBoxColumn dvPortFullDuplex;

	private DataGridViewTextBoxColumn dvPortBlocked;

	private DataGridViewTextBoxColumn dvPortAllowPromiscuous;

	private DataGridViewTextBoxColumn dvPortMacChanges;

	private DataGridViewTextBoxColumn dvPortActiveUplink;

	private DataGridViewTextBoxColumn dvPortStandbyUplink;

	private DataGridViewTextBoxColumn dvPortPolicy;

	private DataGridViewTextBoxColumn dvPortForgedTransmits;

	private DataGridViewTextBoxColumn dvPortInTrafficShaping;

	private DataGridViewTextBoxColumn dvPortInTrafficAverage;

	private DataGridViewTextBoxColumn dvPortInTrafficPeak;

	private DataGridViewTextBoxColumn dvPortInTrafficBurst;

	private DataGridViewTextBoxColumn dvPortOutTrafficShaping;

	private DataGridViewTextBoxColumn dvPortOutTrafficAverage;

	private DataGridViewTextBoxColumn dvPortOutTrafficPeak;

	private DataGridViewTextBoxColumn dvPortOutTrafficBurst;

	private DataGridViewTextBoxColumn dvPortReversePolicy;

	private DataGridViewTextBoxColumn dvPortNotifySwitch;

	private DataGridViewTextBoxColumn dvPortRollingOrder;

	private DataGridViewTextBoxColumn dvPortCheckBeacon;

	private DataGridViewTextBoxColumn dvPortLivePortMovingAllowed;

	private DataGridViewTextBoxColumn dvPortCheckDuplex;

	private DataGridViewTextBoxColumn dvPortCheckErrorPercent;

	private DataGridViewTextBoxColumn dvPortCheckSpeed;

	private DataGridViewTextBoxColumn dvPortPercentage;

	private DataGridViewTextBoxColumn dvPortBlockOverrideAllowed;

	private DataGridViewTextBoxColumn dvPortPortConfigResetAtDisconnect;

	private DataGridViewTextBoxColumn dvPortShapingOverrideAllowed;

	private DataGridViewTextBoxColumn dvPortVendorConfigOverrideAllowed;

	private DataGridViewTextBoxColumn dvPortSecurityPolicyOverrideAllowed;

	private DataGridViewTextBoxColumn dvPortUplinkTeamingOverrideAllowed;

	private DataGridViewTextBoxColumn dvPortVlanOverrideAllowed;

	private DataGridViewTextBoxColumn dvPortObjectID;

	private DataGridViewTextBoxColumn dvPortVISDKServer;

	private DataGridViewTextBoxColumn dvPortInstanceUUID;

	private TabPage tabvFileInfo;

	private Panel panel28;

	private DataGridView dataGridvFileInfo;

	private DataGridViewTextBoxColumn vFileInfoFriendlyPath;

	private DataGridViewTextBoxColumn vFileInfoFileName;

	private DataGridViewTextBoxColumn vFileInfoFileType;

	private DataGridViewTextBoxColumn vFileInfoFileSize;

	private DataGridViewTextBoxColumn vFileInfoPath;

	private DataGridViewTextBoxColumn vFileInfoSortColumn;

	private DataGridViewTextBoxColumn vFileInfoSDKServer;

	private DataGridViewTextBoxColumn vFileInfoSDKUUID;

	private DataGridViewTextBoxColumn vClusterName;

	private DataGridViewTextBoxColumn vClusterConfigStatus;

	private DataGridViewTextBoxColumn vClusterOverallStatus;

	private DataGridViewTextBoxColumn vClusterNumHosts;

	private DataGridViewTextBoxColumn vClusterNumEffectiveHosts;

	private DataGridViewTextBoxColumn vClusterTotalCpu;

	private DataGridViewTextBoxColumn vClusterNumCpuCores;

	private DataGridViewTextBoxColumn vClusterNumCpuThreads;

	private DataGridViewTextBoxColumn vClusterEffectiveCpu;

	private DataGridViewTextBoxColumn vClusterTotalMemory;

	private DataGridViewTextBoxColumn vClusterEffectiveMemory;

	private DataGridViewTextBoxColumn vClusterNumVmotions;

	private DataGridViewTextBoxColumn vClusterEnabled;

	private DataGridViewTextBoxColumn vClusterFailoverLevel;

	private DataGridViewTextBoxColumn vClusterAdmissionControlEnabled;

	private DataGridViewTextBoxColumn vClusterHostMonitoring;

	private DataGridViewTextBoxColumn vClusterHBDatastoreCandidatePolicy;

	private DataGridViewTextBoxColumn vClusterIsolationResponse;

	private DataGridViewTextBoxColumn vClusterRestartPriority;

	private DataGridViewTextBoxColumn vClusterClusterSettings;

	private DataGridViewTextBoxColumn vClusterMaxFailures;

	private DataGridViewTextBoxColumn vClusterMaxFailureWindow;

	private DataGridViewTextBoxColumn vClusterFailureInterval;

	private DataGridViewTextBoxColumn vClusterMinUpTime;

	private DataGridViewTextBoxColumn vClusterVmMonitoring;

	private DataGridViewTextBoxColumn vClusterDRSEnabled;

	private DataGridViewTextBoxColumn vClusterDRSDefaultVmBehavior;

	private DataGridViewTextBoxColumn vClusterDRSvmotionRate;

	private DataGridViewTextBoxColumn vClusterDpmEnabled;

	private DataGridViewTextBoxColumn vClusterDefaultDpmBehavior;

	private DataGridViewTextBoxColumn vClusterHostPowerActionRate;

	private DataGridViewTextBoxColumn vClusterObjectID;

	private DataGridViewTextBoxColumn vClusterVISDKServer;

	private DataGridViewTextBoxColumn vClusterInstanceUUID;

	private DataGridViewTextBoxColumn vDatastoreName;

	private DataGridViewTextBoxColumn vDatastoreConfigStatus;

	private DataGridViewTextBoxColumn vDatastoreAddress;

	private DataGridViewTextBoxColumn vDatastoreAccessible;

	private DataGridViewTextBoxColumn vDatastoreType;

	private DataGridViewTextBoxColumn vDatastoreVMsTotal;

	private DataGridViewTextBoxColumn vDatastoreVMs;

	private DataGridViewTextBoxColumn vDatastoreCapacity;

	private DataGridViewTextBoxColumn vDatastoreProvisioned;

	private DataGridViewTextBoxColumn vDatastoreInUse;

	private DataGridViewTextBoxColumn vDatastoreFreeSpace;

	private DataGridViewTextBoxColumn vDataStoreFreePercentage;

	private DataGridViewTextBoxColumn vDatastoreSIOCFlag;

	private DataGridViewTextBoxColumn vDatastoreSIOCThreshold;

	private DataGridViewTextBoxColumn vDatastoreNumberofHosts;

	private DataGridViewTextBoxColumn vDatastoreHosts;

	private DataGridViewTextBoxColumn vDatastoreClusterName;

	private DataGridViewTextBoxColumn vDatastoreClusterCapacity;

	private DataGridViewTextBoxColumn vDatastoreClusterFreeSpace;

	private DataGridViewTextBoxColumn vDatastoreBlockSizeMiB;

	private DataGridViewTextBoxColumn vDatastoreMaxBlocks;

	private DataGridViewTextBoxColumn vDatastoreExtents;

	private DataGridViewTextBoxColumn vDatastoreMajorVersion;

	private DataGridViewTextBoxColumn vDatastoreVersion;

	private DataGridViewTextBoxColumn vDatastoreVMFSUpgradable;

	private DataGridViewTextBoxColumn vDatastoreMultipleHostAccess;

	private DataGridViewTextBoxColumn vDatastoreURL;

	private DataGridViewTextBoxColumn vDatastoreObjectID;

	private DataGridViewTextBoxColumn vDatastoreVISDKServer;

	private DataGridViewTextBoxColumn vDatastoreInstanceUUID;

	private DataGridViewTextBoxColumn vRPName;

	private DataGridViewTextBoxColumn vRPPath;

	private DataGridViewTextBoxColumn vRPstatus;

	private DataGridViewTextBoxColumn vRPVMsTotal;

	private DataGridViewTextBoxColumn vRPVMs;

	private DataGridViewTextBoxColumn vRPnumberOfCPUs;

	private DataGridViewTextBoxColumn vRPCPUlimit;

	private DataGridViewTextBoxColumn vRPCPUoverheadLimit;

	private DataGridViewTextBoxColumn vRPCPUreservation;

	private DataGridViewTextBoxColumn vRPCPUlevel;

	private DataGridViewTextBoxColumn vRPCPUshares;

	private DataGridViewTextBoxColumn vRPCPUexpandableReservation;

	private DataGridViewTextBoxColumn vRPCPUmaxUsage;

	private DataGridViewTextBoxColumn vRPCPUoverallUsage;

	private DataGridViewTextBoxColumn vRPCPUreservationUsed;

	private DataGridViewTextBoxColumn vRPCPUreservationUsedForVm;

	private DataGridViewTextBoxColumn vRPCPUunreservedForPool;

	private DataGridViewTextBoxColumn vRPCPUunreservedForVm;

	private DataGridViewTextBoxColumn vRPconfiguredMemoryMiB;

	private DataGridViewTextBoxColumn vRPMEMlimit;

	private DataGridViewTextBoxColumn vRPMEMoverheadLimit;

	private DataGridViewTextBoxColumn vRPMEMreservation;

	private DataGridViewTextBoxColumn vRPMEMlevel;

	private DataGridViewTextBoxColumn vRPMEMshares;

	private DataGridViewTextBoxColumn vRPMEMexpandableReservation;

	private DataGridViewTextBoxColumn vRPMEMmaxUsage;

	private DataGridViewTextBoxColumn vRPMEMoverallUsage;

	private DataGridViewTextBoxColumn vRPMEMreservationUsed;

	private DataGridViewTextBoxColumn vRPMEMreservationUsedForVm;

	private DataGridViewTextBoxColumn vRPMEMunreservedForPool;

	private DataGridViewTextBoxColumn vRPMEMunreservedForVm;

	private DataGridViewTextBoxColumn vRPoverallCpuDemand;

	private DataGridViewTextBoxColumn vRPoverallCpuUsage;

	private DataGridViewTextBoxColumn vRPstaticCpuEntitlement;

	private DataGridViewTextBoxColumn vRPdistributedCpuEntitlement;

	private DataGridViewTextBoxColumn vRPballoonedMemory;

	private DataGridViewTextBoxColumn vRPcompressedMemory;

	private DataGridViewTextBoxColumn vRPconsumedOverheadMemory;

	private DataGridViewTextBoxColumn vRPdistributedMemoryEntitlement;

	private DataGridViewTextBoxColumn vRPguestMemoryUsage;

	private DataGridViewTextBoxColumn vRPhostMemoryUsage;

	private DataGridViewTextBoxColumn vRPoverheadMemory;

	private DataGridViewTextBoxColumn vRPprivateMemory;

	private DataGridViewTextBoxColumn vRPsharedMemory;

	private DataGridViewTextBoxColumn vRPstaticMemoryEntitlement;

	private DataGridViewTextBoxColumn vRPswappedMemory;

	private DataGridViewTextBoxColumn vRPObjectID;

	private DataGridViewTextBoxColumn vRPVISDKServer;

	private DataGridViewTextBoxColumn vRPInstanceUUID;

	private DataGridViewTextBoxColumn vHealthName;

	private DataGridViewTextBoxColumn vHealthMessage;

	private DataGridViewTextBoxColumn vHealthMessageType;

	private DataGridViewTextBoxColumn vHealthVISDKServer;

	private DataGridViewTextBoxColumn vHealthInstanceUUID;

	private TabPage tabvSource;

	private Panel panel29;

	private DataGridView dataGridvSource;

	private ToolStripMenuItem vSourceStripMenuItem;

	private DataGridViewCheckBoxColumn vToolsUpgrade;

	private DataGridViewTextBoxColumn vToolsVMName;

	private DataGridViewTextBoxColumn vToolsPowerstate;

	private DataGridViewTextBoxColumn vToolsTemplate;

	private DataGridViewTextBoxColumn vToolsSRMPlaceholder;

	private DataGridViewTextBoxColumn vToolsVMVersion;

	private DataGridViewTextBoxColumn vToolsTools;

	private DataGridViewTextBoxColumn vToolsVersion;

	private DataGridViewTextBoxColumn vToolsRequiredVersion;

	private DataGridViewTextBoxColumn vToolsUpgradeable;

	private DataGridViewTextBoxColumn vToolsUpgradePolicy;

	private DataGridViewTextBoxColumn vToolsSyncTime;

	private DataGridViewTextBoxColumn vToolsAppState;

	private DataGridViewTextBoxColumn vToolsAppHeartbeatStatus;

	private DataGridViewTextBoxColumn vToolsKernelCrashed;

	private DataGridViewTextBoxColumn vToolsOperationsReady;

	private DataGridViewTextBoxColumn vToolsStateChangeSupported;

	private DataGridViewTextBoxColumn vToolsInteractiveOperationsReady;

	private DataGridViewTextBoxColumn vToolsNotes;

	private DataGridViewTextBoxColumn vToolsDatacenter;

	private DataGridViewTextBoxColumn vToolsCluster;

	private DataGridViewTextBoxColumn vToolsHost;

	private DataGridViewTextBoxColumn vToolsFolder;

	private DataGridViewTextBoxColumn vToolsOS;

	private DataGridViewTextBoxColumn vToolsguestFullName;

	private DataGridViewTextBoxColumn vToolsVMRef;

	private DataGridViewTextBoxColumn vToolsObjectID;

	private DataGridViewTextBoxColumn vToolsUUID;

	private DataGridViewTextBoxColumn vToolsVISDKServer;

	private DataGridViewTextBoxColumn vToolsInstanceUUID;

	private DataGridViewTextBoxColumn vSnapshotVMName;

	private DataGridViewTextBoxColumn vSnapshotPowerstate;

	private DataGridViewTextBoxColumn vSnapshotName;

	private DataGridViewTextBoxColumn vSnapshotDescription;

	private DataGridViewTextBoxColumn vSnapshotCreateTime;

	private DataGridViewTextBoxColumn vSnapshotFilename;

	private DataGridViewTextBoxColumn vSnapshotSizeVmsn;

	private DataGridViewTextBoxColumn vSnapshotSizeTotal;

	private DataGridViewTextBoxColumn vSnapshotQuiesced;

	private DataGridViewTextBoxColumn vSnapshotState;

	private DataGridViewTextBoxColumn vSnapshotNotes;

	private DataGridViewTextBoxColumn vSnapshotDatacenter;

	private DataGridViewTextBoxColumn vSnapshotCluster;

	private DataGridViewTextBoxColumn vSnapshotHost;

	private DataGridViewTextBoxColumn vSnapshotFolder;

	private DataGridViewTextBoxColumn vSnapshotOS;

	private DataGridViewTextBoxColumn vSnapshotOSTools;

	private DataGridViewTextBoxColumn vSnapshotObjectID;

	private DataGridViewTextBoxColumn vSnapshotUUID;

	private DataGridViewTextBoxColumn vSnapshotVISDKServer;

	private DataGridViewTextBoxColumn vSnapshotInstanceUUID;

	private DataGridViewCheckBoxColumn vCDSelect;

	private DataGridViewTextBoxColumn vCDVMName;

	private DataGridViewTextBoxColumn vCDPowerstate;

	private DataGridViewTextBoxColumn vCDTemplate;

	private DataGridViewTextBoxColumn vCDSRMPlaceholder;

	private DataGridViewTextBoxColumn vCDlabel;

	private DataGridViewTextBoxColumn vCDConnected;

	private DataGridViewTextBoxColumn vCDStartup;

	private DataGridViewTextBoxColumn vCDSummary;

	private DataGridViewTextBoxColumn vCDNotes;

	private DataGridViewTextBoxColumn vCDDatacenter;

	private DataGridViewTextBoxColumn vCDCluster;

	private DataGridViewTextBoxColumn vCDHost;

	private DataGridViewTextBoxColumn vCDFolder;

	private DataGridViewTextBoxColumn vCDOS;

	private DataGridViewTextBoxColumn vCDOSTools;

	private DataGridViewTextBoxColumn vCDVMRef;

	private DataGridViewTextBoxColumn vCDObjectID;

	private DataGridViewTextBoxColumn vCDUUID;

	private DataGridViewTextBoxColumn vCDVISDKServer;

	private DataGridViewTextBoxColumn vCDInstanceUUID;

	private DataGridViewCheckBoxColumn vUSBSelect;

	private DataGridViewTextBoxColumn vUSBVMName;

	private DataGridViewTextBoxColumn vUSBPowerstate;

	private DataGridViewTextBoxColumn vUSBTemplate;

	private DataGridViewTextBoxColumn vUSBSRMPlaceholder;

	private DataGridViewTextBoxColumn vUSBLabel;

	private DataGridViewTextBoxColumn vUSBSummary;

	private DataGridViewTextBoxColumn vUSBConnected;

	private DataGridViewTextBoxColumn vUSBFamily;

	private DataGridViewTextBoxColumn vUSBSpeed;

	private DataGridViewTextBoxColumn vUSBEHCIEnabled;

	private DataGridViewTextBoxColumn vUSBAutoConnect;

	private DataGridViewTextBoxColumn vUSBBusNumber;

	private DataGridViewTextBoxColumn vUSBUnitNumber;

	private DataGridViewTextBoxColumn vUSBNotes;

	private DataGridViewTextBoxColumn vUSBDatacenter;

	private DataGridViewTextBoxColumn vUSBCluster;

	private DataGridViewTextBoxColumn vUSBHost;

	private DataGridViewTextBoxColumn vUSBFolder;

	private DataGridViewTextBoxColumn vUSBOS;

	private DataGridViewTextBoxColumn vUSBOSTools;

	private DataGridViewTextBoxColumn vUSBVMRef;

	private DataGridViewTextBoxColumn vUSBObjectID;

	private DataGridViewTextBoxColumn vUSBUUID;

	private DataGridViewTextBoxColumn vUSBVISDKServer;

	private DataGridViewTextBoxColumn vUSBInstanceUUID;

	private DataGridViewTextBoxColumn vNetworkVMName;

	private DataGridViewTextBoxColumn vNetworkPowerstate;

	private DataGridViewTextBoxColumn vNetworkTemplate;

	private DataGridViewTextBoxColumn vNetworkSRMPlaceholder;

	private DataGridViewTextBoxColumn vNetworkNic;

	private DataGridViewTextBoxColumn vNetworkAdapter;

	private DataGridViewTextBoxColumn vNetworkName;

	private DataGridViewTextBoxColumn vNetworkSwitch;

	private DataGridViewTextBoxColumn vNetworkConnected;

	private DataGridViewTextBoxColumn vNetworkStartsConnected;

	private DataGridViewTextBoxColumn vNetworkMacAddress;

	private DataGridViewTextBoxColumn vNetworkMacAddressType;

	private DataGridViewTextBoxColumn vNetworkIP4Address;

	private DataGridViewTextBoxColumn vNetworkIP6Address;

	private DataGridViewTextBoxColumn vNetworkDirectPathIO;

	private DataGridViewTextBoxColumn vNetworkSortColumn;

	private DataGridViewTextBoxColumn vNetworkNotes;

	private DataGridViewTextBoxColumn vNetworkDatacenter;

	private DataGridViewTextBoxColumn vNetworkCluster;

	private DataGridViewTextBoxColumn vNetworkHost;

	private DataGridViewTextBoxColumn vNetworkFolder;

	private DataGridViewTextBoxColumn vNetworkOS;

	private DataGridViewTextBoxColumn vNetworkOSTools;

	private DataGridViewTextBoxColumn vNetworkObjectID;

	private DataGridViewTextBoxColumn vNetworkUUID;

	private DataGridViewTextBoxColumn vNetworkVISDKServer;

	private DataGridViewTextBoxColumn vNetworkInstanceUUID;

	private DataGridViewTextBoxColumn vPartitionVMName;

	private DataGridViewTextBoxColumn vPartitionPowerstate;

	private DataGridViewTextBoxColumn vPartitionTemplate;

	private DataGridViewTextBoxColumn vPartitionSRMPlaceholder;

	private DataGridViewTextBoxColumn vPartitionDiskKey;

	private DataGridViewTextBoxColumn vPartitionDisk;

	private DataGridViewTextBoxColumn vPartitionCapacityMiB;

	private DataGridViewTextBoxColumn vPartitionConsumedMiB;

	private DataGridViewTextBoxColumn vPartitionFreeMiB;

	private DataGridViewTextBoxColumn vPartitionFreePercentage;

	private DataGridViewTextBoxColumn vPartitionSortColumn;

	private DataGridViewTextBoxColumn vPartitionNotes;

	private DataGridViewTextBoxColumn vPartitionDatacenter;

	private DataGridViewTextBoxColumn vPartitionCluster;

	private DataGridViewTextBoxColumn vPartitionHost;

	private DataGridViewTextBoxColumn vPartitionFolder;

	private DataGridViewTextBoxColumn vPartitionOS;

	private DataGridViewTextBoxColumn vPartitionOSTools;

	private DataGridViewTextBoxColumn vPartitionObjectID;

	private DataGridViewTextBoxColumn vPartitionUUID;

	private DataGridViewTextBoxColumn vPartitionVISDKServer;

	private DataGridViewTextBoxColumn vPartitionInstanceUUID;

	private DataGridViewTextBoxColumn vMemoryVMName;

	private DataGridViewTextBoxColumn vMemoryPowerstate;

	private DataGridViewTextBoxColumn vMemoryTemplate;

	private DataGridViewTextBoxColumn vMemorySRMPlaceholder;

	private DataGridViewTextBoxColumn vMemorySizeMiB;

	private DataGridViewTextBoxColumn vMemoryReservationLockedToMax;

	private DataGridViewTextBoxColumn vMemoryOverhead;

	private DataGridViewTextBoxColumn vMemoryMaxUsage;

	private DataGridViewTextBoxColumn vMemoryHostMemoryUsage;

	private DataGridViewTextBoxColumn vMemoryconsumedOverheadMemory;

	private DataGridViewTextBoxColumn vMemoryprivateMemory;

	private DataGridViewTextBoxColumn vMemorysharedMemory;

	private DataGridViewTextBoxColumn vMemoryswappedMemory;

	private DataGridViewTextBoxColumn vMemoryballoonedMemory;

	private DataGridViewTextBoxColumn vMemoryGuestMemoryUsage;

	private DataGridViewTextBoxColumn vMemorystaticMemoryEntitlement;

	private DataGridViewTextBoxColumn vMemorydistributedMemoryEntitlement;

	private DataGridViewTextBoxColumn vMemorySharesLevel;

	private DataGridViewTextBoxColumn vMemoryShares;

	private DataGridViewTextBoxColumn vMemoryReservation;

	private DataGridViewTextBoxColumn vMemoryReservationLimit;

	private DataGridViewTextBoxColumn vMemoryHotAdd;

	private DataGridViewTextBoxColumn vMemoryNotes;

	private DataGridViewTextBoxColumn vMemoryDatacenter;

	private DataGridViewTextBoxColumn vMemoryCluster;

	private DataGridViewTextBoxColumn vMemoryHost;

	private DataGridViewTextBoxColumn vMemoryFolder;

	private DataGridViewTextBoxColumn vMemoryOS;

	private DataGridViewTextBoxColumn vMemoryOSTools;

	private DataGridViewTextBoxColumn vMemoryObjectID;

	private DataGridViewTextBoxColumn vMemoryUUID;

	private DataGridViewTextBoxColumn vMemoryVISDKServer;

	private DataGridViewTextBoxColumn vMemoryInstanceUUID;

	private DataGridViewTextBoxColumn vCPUVMName;

	private DataGridViewTextBoxColumn vCPUPowerstate;

	private DataGridViewTextBoxColumn vCPUTemplate;

	private DataGridViewTextBoxColumn vCPUSRMPlaceholder;

	private DataGridViewTextBoxColumn vCPUCPUs;

	private DataGridViewTextBoxColumn vCPUSockets;

	private DataGridViewTextBoxColumn vCPUCoresPerSocket;

	private DataGridViewTextBoxColumn vCPUMaxCpuUsage;

	private DataGridViewTextBoxColumn vCPUOverallCpuUsage;

	private DataGridViewTextBoxColumn vCPUSharesLevel;

	private DataGridViewTextBoxColumn vCPUShares;

	private DataGridViewTextBoxColumn vCPUReservation;

	private DataGridViewTextBoxColumn vCPUStaticCpuEntitlement;

	private DataGridViewTextBoxColumn vCPUDistributedCpuEntitlement;

	private DataGridViewTextBoxColumn vCPUReservationLimit;

	private DataGridViewTextBoxColumn vCPUHotAdd;

	private DataGridViewTextBoxColumn vCPUHotRemove;

	private DataGridViewTextBoxColumn vCPUNumaOnCpuHotaddExposed;

	private DataGridViewTextBoxColumn vCPUNotes;

	private DataGridViewTextBoxColumn vCPUDatacenter;

	private DataGridViewTextBoxColumn vCPUCluster;

	private DataGridViewTextBoxColumn vCPUHost;

	private DataGridViewTextBoxColumn vCPUFolder;

	private DataGridViewTextBoxColumn vCPUOS;

	private DataGridViewTextBoxColumn vCPUOSTools;

	private DataGridViewTextBoxColumn vCPUObjectID;

	private DataGridViewTextBoxColumn vCPUUUID;

	private DataGridViewTextBoxColumn vCPUVISDKServer;

	private DataGridViewTextBoxColumn vCPUInstanceUUID;

	private DataGridViewTextBoxColumn vSourceName;

	private DataGridViewTextBoxColumn vSourceOSType;

	private DataGridViewTextBoxColumn vSourceApiType;

	private DataGridViewTextBoxColumn vSourceAPIVersion;

	private DataGridViewTextBoxColumn vSourceVersion;

	private DataGridViewTextBoxColumn vSourcePatchLevel;

	private DataGridViewTextBoxColumn vSourceBuild;

	private DataGridViewTextBoxColumn vSourceFullname;

	private DataGridViewTextBoxColumn vSourceProductName;

	private DataGridViewTextBoxColumn vSourceProductVersion;

	private DataGridViewTextBoxColumn vSourceProductLine;

	private DataGridViewTextBoxColumn vSourceVendor;

	private DataGridViewTextBoxColumn vSourceVISDKServer;

	private DataGridViewTextBoxColumn vSourceVISDKUUID;

	private DataGridViewTextBoxColumn vDiskVMName;

	private DataGridViewTextBoxColumn vDiskPowerstate;

	private DataGridViewTextBoxColumn vDiskTemplate;

	private DataGridViewTextBoxColumn vDiskSRMPlaceholder;

	private DataGridViewTextBoxColumn vDiskDisk;

	private DataGridViewTextBoxColumn vDiskKey;

	private DataGridViewTextBoxColumn vDiskDiskUUID;

	private DataGridViewTextBoxColumn vDiskDiskPath;

	private DataGridViewTextBoxColumn vDiskCapacityMiB;

	private DataGridViewTextBoxColumn vDiskRaw;

	private DataGridViewTextBoxColumn vDiskMode;

	private DataGridViewTextBoxColumn vDiskSharingMode;

	private DataGridViewTextBoxColumn vDiskThinProvisioned;

	private DataGridViewTextBoxColumn vDiskEagerlyScrub;

	private DataGridViewTextBoxColumn vDiskSplit;

	private DataGridViewTextBoxColumn vDiskWriteThrough;

	private DataGridViewTextBoxColumn vDiskLevel;

	private DataGridViewTextBoxColumn vDiskShares;

	private DataGridViewTextBoxColumn vDiskReservation;

	private DataGridViewTextBoxColumn vDiskLimit;

	private DataGridViewTextBoxColumn vDiskController;

	private DataGridViewTextBoxColumn vDiskControllerLabel;

	private DataGridViewTextBoxColumn vDiskSCSICtlrUnitNumber;

	private DataGridViewTextBoxColumn vDiskUnit;

	private DataGridViewTextBoxColumn vDiskControllerSharedBus;

	private DataGridViewTextBoxColumn vDiskPath;

	private DataGridViewTextBoxColumn vDiskRawLunId;

	private DataGridViewTextBoxColumn vDiskRawCompMode;

	private DataGridViewTextBoxColumn vDiskSortColumn;

	private DataGridViewTextBoxColumn vDiskNotes;

	private DataGridViewTextBoxColumn vDiskDatacenter;

	private DataGridViewTextBoxColumn vDiskCluster;

	private DataGridViewTextBoxColumn vDiskHost;

	private DataGridViewTextBoxColumn vDiskFolder;

	private DataGridViewTextBoxColumn vDiskOS;

	private DataGridViewTextBoxColumn vDiskOSTools;

	private DataGridViewTextBoxColumn vDiskObjectID;

	private DataGridViewTextBoxColumn vDIskUUID;

	private DataGridViewTextBoxColumn vDiskVISDKServer;

	private DataGridViewTextBoxColumn vDiskInstanceUUID;

	private DataGridViewTextBoxColumn vInfoVMName;

	private DataGridViewTextBoxColumn vInfoPowerstate;

	private DataGridViewTextBoxColumn vInfoTemplate;

	private DataGridViewTextBoxColumn vInfoSRMPlaceHolder;

	private DataGridViewTextBoxColumn vInfoConfigStatus;

	private DataGridViewTextBoxColumn vInfoGuestHostName;

	private DataGridViewTextBoxColumn vInfoConnectionState;

	private DataGridViewTextBoxColumn vInfoGuestState;

	private DataGridViewTextBoxColumn vInfoHeartbeat;

	private DataGridViewTextBoxColumn vInfoConsolidationNeeded;

	private DataGridViewTextBoxColumn vInfoBootTime;

	private DataGridViewTextBoxColumn vInfoSuspendedToMemory;

	private DataGridViewTextBoxColumn vInfoSuspendTime;

	private DataGridViewTextBoxColumn vInfoSuspendInterval;

	private DataGridViewTextBoxColumn vInfoCreateDate;

	private DataGridViewTextBoxColumn vInfoChangeVersion;

	private DataGridViewTextBoxColumn vInfoCPUs;

	private DataGridViewTextBoxColumn vInfoOverallCpuReadiness;

	private DataGridViewTextBoxColumn vInfoMemory;

	private DataGridViewTextBoxColumn vInfoActiveMemory;

	private DataGridViewTextBoxColumn vInfoNICs;

	private DataGridViewTextBoxColumn vInfoNumVirtualDisks;

	private DataGridViewTextBoxColumn vInfoTotalDiskCapacityMiB;

	private DataGridViewTextBoxColumn vInfoFixedPassthruHotPlugEnabled;

	private DataGridViewTextBoxColumn vInfominRequiredEVCModeKey;

	private DataGridViewTextBoxColumn vInfoLatencySensitivity;

	private DataGridViewTextBoxColumn vInfoOpNotificationTimeout;

	private DataGridViewTextBoxColumn vInfoEnableUUID;

	private DataGridViewTextBoxColumn vINfoCBT;

	private DataGridViewTextBoxColumn vInfoPrimaryIPAddress;

	private DataGridViewTextBoxColumn vInfoNetwork1;

	private DataGridViewTextBoxColumn vInfoNetwork2;

	private DataGridViewTextBoxColumn vInfoNetwork3;

	private DataGridViewTextBoxColumn vInfoNetwork4;

	private DataGridViewTextBoxColumn vInfoNetwork5;

	private DataGridViewTextBoxColumn vInfoNetwork6;

	private DataGridViewTextBoxColumn vInfoNetwork7;

	private DataGridViewTextBoxColumn vInfoNetwork8;

	private DataGridViewTextBoxColumn vInfoNumDisplays;

	private DataGridViewTextBoxColumn vInfoVideoRamKiB;

	private DataGridViewTextBoxColumn vInfoResourcepool;

	private DataGridViewTextBoxColumn vInfoFolderId;

	private DataGridViewTextBoxColumn vInfoFolder;

	private DataGridViewTextBoxColumn vInfovApp;

	private DataGridViewTextBoxColumn vInfoDASProtection;

	private DataGridViewTextBoxColumn vInfoFaultToleranceState;

	private DataGridViewTextBoxColumn vInfoFTInfoRole;

	private DataGridViewTextBoxColumn vInfoFTLatencyStatus;

	private DataGridViewTextBoxColumn vInfoFTLogBandwidth;

	private DataGridViewTextBoxColumn vInfoFTSecondaryLatency;

	private DataGridViewTextBoxColumn vInfoVmFailoverInProgress;

	private DataGridViewTextBoxColumn vInfoProvisioned;

	private DataGridViewTextBoxColumn vInfoInUse;

	private DataGridViewTextBoxColumn vInfoUnshared;

	private DataGridViewTextBoxColumn vInfoHARestartPriority;

	private DataGridViewTextBoxColumn vInfoHAIsolationResponse;

	private DataGridViewTextBoxColumn vInfoVMmonitoring;

	private DataGridViewTextBoxColumn vInfoClusterRule;

	private DataGridViewTextBoxColumn vInfoClusterRuleName;

	private DataGridViewTextBoxColumn vInfoInstallBootRequired;

	private DataGridViewTextBoxColumn vInfoBootDelay;

	private DataGridViewTextBoxColumn vInfoBootRetryDelay;

	private DataGridViewTextBoxColumn vInfoBootRetryEnabled;

	private DataGridViewTextBoxColumn vInfoBootBios;

	private DataGridViewTextBoxColumn vInfoRebootPowerOff;

	private DataGridViewTextBoxColumn vInfoEfiSecureBoot;

	private DataGridViewTextBoxColumn vInfoFirmware;

	private DataGridViewTextBoxColumn VInfoVersion;

	private DataGridViewTextBoxColumn vInfoHWUpgradeStatus;

	private DataGridViewTextBoxColumn vInfoHWUpgradePolicy;

	private DataGridViewTextBoxColumn vInfoHWTarget;

	private DataGridViewTextBoxColumn vInfopath;

	private DataGridViewTextBoxColumn vInfoLogDirectory;

	private DataGridViewTextBoxColumn vInfoSnapshotDirectory;

	private DataGridViewTextBoxColumn vInfoSuspendDirectory;

	private DataGridViewTextBoxColumn vInfoNotes;

	private DataGridViewTextBoxColumn vInfoDataCenter;

	private DataGridViewTextBoxColumn vInfoCluster;

	private DataGridViewTextBoxColumn vInfoHost;

	private DataGridViewTextBoxColumn vInfoOS;

	private DataGridViewTextBoxColumn vInfoOSTools;

	private DataGridViewTextBoxColumn vInfoCustomizationInfo;

	private DataGridViewTextBoxColumn vInfoGuestDetailedData;

	private DataGridViewTextBoxColumn vInfoObjectID;

	private DataGridViewTextBoxColumn vInfoSMBIOSUUID;

	private DataGridViewTextBoxColumn vInfoUUID;

	private DataGridViewTextBoxColumn vInfoVISDKServerType;

	private DataGridViewTextBoxColumn vInfoVISDKAPI;

	private DataGridViewTextBoxColumn vInfoVISDKServer;

	private DataGridViewTextBoxColumn vInfoInstanceUUID;

	private DataGridViewTextBoxColumn vHostName;

	private DataGridViewTextBoxColumn vHostDatacenter;

	private DataGridViewTextBoxColumn vHostCluster;

	private DataGridViewTextBoxColumn vHostConfigStatus;

	private DataGridViewTextBoxColumn vHostComplianceCheckState;

	private DataGridViewTextBoxColumn vHostinMaintenanceMode;

	private DataGridViewTextBoxColumn vHostinQuarantineMode;

	private DataGridViewTextBoxColumn vHostvSANFaultDomainName;

	private DataGridViewTextBoxColumn vHostCpuModel;

	private DataGridViewTextBoxColumn vHostCpuMhz;

	private DataGridViewTextBoxColumn vHostHTAvailable;

	private DataGridViewTextBoxColumn vHostHTActive;

	private DataGridViewTextBoxColumn vHostNumCpu;

	private DataGridViewTextBoxColumn vHostCoresPerCPU;

	private DataGridViewTextBoxColumn vHostNumCpuCores;

	private DataGridViewTextBoxColumn vHostOverallCpuUsage;

	private DataGridViewTextBoxColumn vHostMemorySize;

	private DataGridViewTextBoxColumn vHostMemoryTieringType;

	private DataGridViewTextBoxColumn vHostOverallMemoryUsage;

	private DataGridViewTextBoxColumn vHostServiceConsoleReserved;

	private DataGridViewTextBoxColumn vHostNumNics;

	private DataGridViewTextBoxColumn vHostNumHBAs;

	private DataGridViewTextBoxColumn vHostVMsTotal;

	private DataGridViewTextBoxColumn vHostVMs;

	private DataGridViewTextBoxColumn vHostVMsPerCore;

	private DataGridViewTextBoxColumn vHostvCPUs;

	private DataGridViewTextBoxColumn vHostVCPUsPerCore;

	private DataGridViewTextBoxColumn vHostvRAM;

	private DataGridViewTextBoxColumn vHostVMUsedMemory;

	private DataGridViewTextBoxColumn vHostVMSwappedMemory;

	private DataGridViewTextBoxColumn vHostVMBalloonedMemory;

	private DataGridViewTextBoxColumn vHostVmotionSupported;

	private DataGridViewTextBoxColumn vHostStorageVmotionSupported;

	private DataGridViewTextBoxColumn vHostCurrentEVCModeKey;

	private DataGridViewTextBoxColumn vHostMaxEVCModeKey;

	private DataGridViewTextBoxColumn vHostAssignedLicenses;

	private DataGridViewTextBoxColumn vHostATSHeartbeat;

	private DataGridViewTextBoxColumn vHostATSLocking;

	private DataGridViewTextBoxColumn vHostCurrentPolicy;

	private DataGridViewTextBoxColumn vHostHardwareSupport;

	private DataGridViewTextBoxColumn vHostHostPowerPolicyShortname;

	private DataGridViewTextBoxColumn vHostFullName;

	private DataGridViewTextBoxColumn vHostBootTime;

	private DataGridViewTextBoxColumn vHostDNSServers;

	private DataGridViewTextBoxColumn vHostDHCP;

	private DataGridViewTextBoxColumn vHostDomainName;

	private DataGridViewTextBoxColumn vHostDomainList;

	private DataGridViewTextBoxColumn vHostSearchDomains;

	private DataGridViewTextBoxColumn vHostNTPServers;

	private DataGridViewTextBoxColumn vHostNTPD;

	private DataGridViewTextBoxColumn vHostTZDescription;

	private DataGridViewTextBoxColumn vHostTZName;

	private DataGridViewTextBoxColumn vHostTZOffset;

	private DataGridViewTextBoxColumn vHostVendor;

	private DataGridViewTextBoxColumn vHostModel;

	private DataGridViewTextBoxColumn vHostSerialNumber;

	private DataGridViewTextBoxColumn vHostServiceTag;

	private DataGridViewTextBoxColumn vHostOemSpecificString;

	private DataGridViewTextBoxColumn vHostBiosVendor;

	private DataGridViewTextBoxColumn vHostBiosVersion;

	private DataGridViewTextBoxColumn vHostBiosDate;

	private DataGridViewTextBoxColumn vHostCertificateIssuer;

	private DataGridViewTextBoxColumn vHostCertificateStartDate;

	private DataGridViewTextBoxColumn vHostCertificateExpiry;

	private DataGridViewTextBoxColumn vHostCertificateStatus;

	private DataGridViewTextBoxColumn vHostCertificateSubject;

	private DataGridViewTextBoxColumn vHostObjectID;

	private DataGridViewTextBoxColumn vHostUUID;

	private DataGridViewTextBoxColumn vHostVISDKServer;

	private DataGridViewTextBoxColumn vHostInstanceUUID;

	public static ITelemetryCollector Telemetry
	{
		get
		{
			if (_telemetryCollector != null)
			{
				return _telemetryCollector;
			}
			if (!RvToolsInstanceInfo.ProductImprovementOptIn)
			{
				_telemetryCollector = new VoidTelemetryCollector();
				return _telemetryCollector;
			}
			TelemetryContext config = new TelemetryContext("RVTools Collector", "Standard", "Production", null, string.Empty);
			try
			{
				_telemetryCollector = new AppInsightsTelemetryCollector(config, TelemetryConnectionString, Trace);
			}
			catch (Exception ex)
			{
				Trace.Error("Failed to create telemetry collector", ex);
				_telemetryCollector = new VoidTelemetryCollector();
			}
			if (TelemetryEnabled)
			{
				_telemetryCollector.Enable();
			}
			else
			{
				_telemetryCollector.Disable();
			}
			return _telemetryCollector;
		}
	}

	public static void ProgramImprovementOptInChanged()
	{
		_telemetryCollector = null;
	}

	public FMain()
	{
		XmlConfigurator.Configure();
		InitializeComponent();
	}

	private void FMain_Load(object sender, EventArgs e)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		VISDK.StartedCommandFromCLI = false;
		if (RvToolsInstanceInfo.IsCommandlineArgumentSet("help"))
		{
			MessageBox.Show("Please check the documentation for the commandline options.", "RVTools Commandline options", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Please check the documentation for the commandline options message and close().");
			}
			Close();
		}
		VISDK.Server = RvToolsInstanceInfo.CommandLineArgumentValue("s");
		VISDK.Username = RvToolsInstanceInfo.CommandLineArgumentValue("u");
		string text = RvToolsInstanceInfo.CommandLineArgumentValue("p");
		try
		{
			if (text != null)
			{
				VISDK.Password = VISDK.Decrypt(text);
			}
		}
		catch
		{
		}
		if (RvToolsInstanceInfo.IsCommandlineArgumentSet("c"))
		{
			VISDK.StartedCommandFromCLI = true;
			CommandToExecute = RvToolsInstanceInfo.CommandLineArgumentValue("c");
			ExportDir = RvToolsInstanceInfo.CommandLineArgumentValue("d");
			ExportFile = RvToolsInstanceInfo.CommandLineArgumentValue("f");
			ExcludeCustomAnnotations = RvToolsInstanceInfo.IsCommandlineArgumentSet("ExcludeCustomAnnotations");
			ExcludeTags = RvToolsInstanceInfo.IsCommandlineArgumentSet("ExcludeTags");
			GetFriendlyNames = RvToolsInstanceInfo.IsCommandlineArgumentSet("GetFriendlyNames");
			GetFileInfo = RvToolsInstanceInfo.IsCommandlineArgumentSet("GetFileInfo");
			DBColumnNames = RvToolsInstanceInfo.IsCommandlineArgumentSet("DBColumnNames");
		}
		if (RvToolsInstanceInfo.IsCommandlineArgumentSet("s"))
		{
			if (RvToolsInstanceInfo.IsCommandlineArgumentSet("u"))
			{
				VISDK.Connect(UseWindowsCredentials: false);
			}
			else
			{
				VISDK.Connect(UseWindowsCredentials: true);
			}
		}
		if (!VISDK.Connected && VISDK.StartedCommandFromCLI)
		{
			Close();
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Not connected, close and exit");
			}
			VISDK.ExitCode = -1;
			return;
		}
		if (!VISDK.Connected)
		{
			new FLogin().ShowDialog();
		}
		if (!VISDK.Connected)
		{
			Close();
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Not connected, close and exit");
			}
			return;
		}
		Telemetry.AddEvent("TargetConnected", VISDK._sic.about.apiType, new Dictionary<string, string>
		{
			{
				"Version",
				VISDK._sic.about.apiVersion
			},
			{
				"FullName",
				VISDK._sic.about.fullName
			},
			{
				"ProductName",
				VISDK._sic.about.licenseProductName
			},
			{
				"Build",
				VISDK._sic.about.build
			}
		});
		HealthClass.CheckCDROM = Settings.Default.HCCDROM;
		HealthClass.CheckUSB = Settings.Default.HCUSB;
		HealthClass.CheckSnapshot = Settings.Default.HCSnapshot;
		HealthClass.CheckTools = Settings.Default.HCTools;
		HealthClass.CheckZombies = Settings.Default.HCZombies;
		HealthClass.CheckInconsistentFolderNames = Settings.Default.HCInconsistentFolderNames;
		HealthClass.CheckSecurityIssues = Settings.Default.HCSecurityIssues;
		HealthClass.CheckPerformanceTips = Settings.Default.HCPerformanceTips;
		HealthClass.CheckGuestStorage = Settings.Default.HCGuestStorage;
		HealthClass.CheckGuestStoragePercentage = Settings.Default.HCGuestStoragePercentage;
		HealthClass.CheckHostStorage = Settings.Default.HCHostStorage;
		HealthClass.CheckHostStoragePercentage = Settings.Default.HCHostStoragePercentage;
		HealthClass.CheckVCPUsPerCore = Settings.Default.HCVMPerCore;
		HealthClass.CheckVCPUsPerCoreMaxvalue = Settings.Default.HCVMPerCoreMaxvalue;
		HealthClass.CheckVMsOnStorage = Settings.Default.HCVMsOnStorage;
		HealthClass.CheckVMsOnStorageMaxvalue = Settings.Default.HCVMsOnStorageMaxvalue;
		HealthClass.CheckCertificateExpire = Settings.Default.HCCertificateExpire;
		HealthClass.CheckCertificateExpireValue = Settings.Default.HCCertificateExpireDays;
		if (Settings.Default.FSaveFilter)
		{
			FilterClass.VirtualMachine = Settings.Default.FVirtualMachine;
			FilterClass.Template = Settings.Default.FTemplate;
			FilterClass.SRMPlaceholder = Settings.Default.FSRMPlaceholder;
			FilterClass.PoweredOn = Settings.Default.FPoweredOn;
			FilterClass.PoweredOff = Settings.Default.FPoweredOff;
			FilterClass.Suspended = Settings.Default.FSuspended;
			FilterClass.VMNameExactly = Settings.Default.FVMNameExactly;
			FilterClass.VMNameStartsWith = Settings.Default.FVMNameStartsWith;
			FilterClass.VMNameContains = Settings.Default.FVMNameContains;
			FilterClass.VMName = Settings.Default.FVMName;
			FilterClass.NotesExactly = Settings.Default.FNotesExactly;
			FilterClass.NotesStartsWith = Settings.Default.FNotesStartsWith;
			FilterClass.NotesContains = Settings.Default.FNotesContains;
			FilterClass.Notes = Settings.Default.FNotes;
			FilterClass.DataCenter = Settings.Default.FDataCenter;
			FilterClass.Cluster = Settings.Default.FCluster;
			FilterClass.Host = Settings.Default.FHost;
			FilterClass.SaveFilter = Settings.Default.FSaveFilter;
		}
		PreferencesClass.vInfoShowFolder = Settings.Default.vInfoShowFolder;
		PreferencesClass.vInfoShowAppName = Settings.Default.vInfoShowAppName;
		PreferencesClass.AutoRefresh = Settings.Default.AutoRefresh;
		PreferencesClass.AutoRefreshInterval = Settings.Default.AutoRefreshInterval;
		PreferencesClass.ExcludeAnnotations = Settings.Default.ExcludeAnnotations;
		PreferencesClass.ExcludeTags = Settings.Default.ExcludeTags;
		PreferencesClass.GetFriendlyNames = Settings.Default.GetFriendlyNames;
		PreferencesClass.GetFileInfo = Settings.Default.GetFileInfo;
		if (ExcludeCustomAnnotations && VISDK.StartedCommandFromCLI)
		{
			PreferencesClass.ExcludeAnnotations = true;
		}
		if (!ExcludeCustomAnnotations && VISDK.StartedCommandFromCLI)
		{
			PreferencesClass.ExcludeAnnotations = false;
		}
		if (ExcludeTags && VISDK.StartedCommandFromCLI)
		{
			PreferencesClass.ExcludeTags = true;
		}
		if (!ExcludeTags && VISDK.StartedCommandFromCLI)
		{
			PreferencesClass.ExcludeTags = false;
		}
		if (GetFriendlyNames && VISDK.StartedCommandFromCLI)
		{
			PreferencesClass.GetFriendlyNames = true;
		}
		if (!GetFriendlyNames && VISDK.StartedCommandFromCLI)
		{
			PreferencesClass.GetFriendlyNames = false;
		}
		if (GetFileInfo && VISDK.StartedCommandFromCLI)
		{
			PreferencesClass.GetFileInfo = true;
		}
		if (!GetFileInfo && VISDK.StartedCommandFromCLI)
		{
			PreferencesClass.GetFileInfo = false;
		}
		PleaseWait = new FWait();
		PleaseWait.Show();
		Application.DoEvents();
		QuerySDKInventory();
		StatusLastRefresh.Text = "Last refresh: " + DateTime.Now.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
		PleaseWait.Text = "RVTools: Processing data. Please wait.....";
		PleaseWait.Refresh();
		try
		{
			InstanceUUID = VISDK._sic.about.instanceUuid;
			if (VISDK._sic.about.instanceUuid == null)
			{
				InstanceUUID = VISDK.Server;
			}
		}
		catch
		{
			InstanceUUID = VISDK.Server;
		}
		TabPagevInfo();
		PleaseWait.Close();
		Text = "RVTools (" + VISDK.Server + ")";
		StatusUsername.Text = VISDK.Username;
		StatusServer.Text = VISDK.Server;
		StatusVCFullname.Text = VISDK._sic.about.fullName + "  VI API " + VISDK._sic.about.apiVersion;
		KeepAliveTimer.Enabled = true;
		DisplayDataGridTimer.Enabled = true;
		if (CommandToExecute != null)
		{
			if (CommandToExecute.ToUpper().StartsWith("EXPORT") && CommandToExecute.ToUpper().EndsWith("2CSV"))
			{
				isBatch = true;
				ExportAllorOne2CSVStartedFromCommandLine();
				VISDK.Disconnect();
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Close program");
				}
				Close();
			}
			else if (CommandToExecute.ToUpper().StartsWith("EXPORT") && CommandToExecute.ToUpper().EndsWith("2XLS"))
			{
				isBatch = true;
				ExportAllorOne2XLSStartedFromCommandLine();
				VISDK.Disconnect();
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Close program");
				}
				Close();
			}
			else if (CommandToExecute.ToUpper().StartsWith("EXPORT") && CommandToExecute.ToUpper().EndsWith("2XLSX"))
			{
				isBatch = true;
				ExportAllorOne2XLSStartedFromCommandLine();
				VISDK.Disconnect();
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Close program");
				}
				Close();
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void DisplayDataGridTimer_Tick(object sender, EventArgs e)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		DisplayDataGridTimer.Enabled = false;
		TabPagevInfo();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void KeepAliveTimer_Tick(object sender, EventArgs e)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		try
		{
			ManagedObjectReference managedObjectReference = new ManagedObjectReference();
			managedObjectReference.type = "ServiceInstance";
			managedObjectReference.Value = "ServiceInstance";
			VISDK._service.CurrentTime(managedObjectReference);
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Handled Exception: " + ex.Message);
			}
			VISDK.Disconnect();
			Close();
		}
		if (PreferencesClass.AutoRefresh)
		{
			KeepAliveCounter++;
			if (KeepAliveCounter >= PreferencesClass.AutoRefreshInterval)
			{
				KeepAliveCounter = 0;
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Automatic refresh needed. ");
				}
				if (Cursor == Cursors.WaitCursor)
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(currentMethod.Name + " Already busy! Don't refresh now. ");
					}
					return;
				}
				if (CommandToExecute != null)
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(currentMethod.Name + " Started from batch! No auto refresh. ");
					}
					return;
				}
				Cursor = Cursors.WaitCursor;
				PleaseWait = new FWait();
				PleaseWait.Show();
				Application.DoEvents();
				QuerySDKInventory();
				StatusLastRefresh.Text = "Last refresh: " + DateTime.Now.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
				PleaseWait.Text = "RVTools: Processing data. Please wait.....";
				PleaseWait.Refresh();
				PleaseWait.Close();
				ShowSelectedTab();
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void ExportToolStripMenuItem_Click(object sender, EventArgs e)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		CommandToExecute = "Export2csv";
		SaveFileDialog saveFileDialog = new SaveFileDialog();
		saveFileDialog.Filter = "Excel worksheet (*.xlsx)|*.xlsx|Comma Separated Value (*.csv)|*.csv";
		saveFileDialog.FileName = "RVTools_" + TabPages.SelectedTab.Name;
		saveFileDialog.ValidateNames = true;
		if (saveFileDialog.ShowDialog() != DialogResult.Cancel)
		{
			if (TabPages.SelectedTab.Name == "tabvInfo")
			{
				ExportDataGrid(ref dataGridvInfo, saveFileDialog.FileName);
			}
			if (TabPages.SelectedTab.Name == "tabvCPU")
			{
				ExportDataGrid(ref dataGridvCPU, saveFileDialog.FileName);
			}
			if (TabPages.SelectedTab.Name == "tabvMemory")
			{
				ExportDataGrid(ref dataGridvMemory, saveFileDialog.FileName);
			}
			if (TabPages.SelectedTab.Name == "tabvDisk")
			{
				ExportDataGrid(ref dataGridvDisk, saveFileDialog.FileName);
			}
			if (TabPages.SelectedTab.Name == "tabvPartition")
			{
				ExportDataGrid(ref dataGridvPartition, saveFileDialog.FileName);
			}
			if (TabPages.SelectedTab.Name == "tabvNetwork")
			{
				ExportDataGrid(ref dataGridvNetwork, saveFileDialog.FileName);
			}
			if (TabPages.SelectedTab.Name == "tabvCD")
			{
				ExportDataGrid(ref dataGridvCD, saveFileDialog.FileName);
			}
			if (TabPages.SelectedTab.Name == "tabvUSB")
			{
				ExportDataGrid(ref dataGridvUSB, saveFileDialog.FileName);
			}
			if (TabPages.SelectedTab.Name == "tabvSnapshot")
			{
				ExportDataGrid(ref dataGridvSnapshot, saveFileDialog.FileName);
			}
			if (TabPages.SelectedTab.Name == "tabvTools")
			{
				ExportDataGrid(ref dataGridvTools, saveFileDialog.FileName);
			}
			if (TabPages.SelectedTab.Name == "tabvSource")
			{
				ExportDataGrid(ref dataGridvSource, saveFileDialog.FileName);
			}
			if (TabPages.SelectedTab.Name == "tabvRP")
			{
				ExportDataGrid(ref dataGridvRP, saveFileDialog.FileName);
			}
			if (TabPages.SelectedTab.Name == "tabvCluster")
			{
				ExportDataGrid(ref dataGridvCluster, saveFileDialog.FileName);
			}
			if (TabPages.SelectedTab.Name == "tabvHost")
			{
				ExportDataGrid(ref dataGridvHost, saveFileDialog.FileName);
			}
			if (TabPages.SelectedTab.Name == "tabvHBA")
			{
				ExportDataGrid(ref dataGridvHBA, saveFileDialog.FileName);
			}
			if (TabPages.SelectedTab.Name == "tabvNIC")
			{
				ExportDataGrid(ref dataGridvNIC, saveFileDialog.FileName);
			}
			if (TabPages.SelectedTab.Name == "tabvSwitch")
			{
				ExportDataGrid(ref dataGridvSwitch, saveFileDialog.FileName);
			}
			if (TabPages.SelectedTab.Name == "tabdvSwitch")
			{
				ExportDataGrid(ref dataGriddvSwitch, saveFileDialog.FileName);
			}
			if (TabPages.SelectedTab.Name == "tabvPort")
			{
				ExportDataGrid(ref dataGridvPort, saveFileDialog.FileName);
			}
			if (TabPages.SelectedTab.Name == "tabdvPort")
			{
				ExportDataGrid(ref dataGriddvPort, saveFileDialog.FileName);
			}
			if (TabPages.SelectedTab.Name == "tabvSC_VMK")
			{
				ExportDataGrid(ref dataGridvSC_VMK, saveFileDialog.FileName);
			}
			if (TabPages.SelectedTab.Name == "tabvDatastore")
			{
				ExportDataGrid(ref dataGridvDatastore, saveFileDialog.FileName);
			}
			if (TabPages.SelectedTab.Name == "tabvLicense")
			{
				ExportDataGrid(ref dataGridvLicense, saveFileDialog.FileName);
			}
			if (TabPages.SelectedTab.Name == "tabvMultiPath")
			{
				ExportDataGrid(ref dataGridvMultiPath, saveFileDialog.FileName);
			}
			if (TabPages.SelectedTab.Name == "tabvFileInfo")
			{
				ExportDataGrid(ref dataGridvFileInfo, saveFileDialog.FileName);
			}
			if (TabPages.SelectedTab.Name == "tabvHealth")
			{
				ExportDataGrid(ref dataGridvHealth, saveFileDialog.FileName);
			}
			CommandToExecute = null;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
		}
	}

	private void ExportAllMenuItem_Click(object sender, EventArgs e)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		CommandToExecute = "ExportAll2csv";
		FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
		folderBrowserDialog.ShowNewFolderButton = true;
		folderBrowserDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
		if (folderBrowserDialog.ShowDialog() == DialogResult.Cancel)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
			return;
		}
		try
		{
			Cursor.Hide();
			string text = folderBrowserDialog.SelectedPath + "\\RVTools_export_all_" + DateTime.Now.ToLocalTime().ToString("yyyy-MM-dd_HH.mm.ss");
			Directory.CreateDirectory(text);
			ExportAllorOne2csv(text, null);
		}
		catch (Exception err)
		{
			UserNotification.ReportError("Export All Datagrids failed", err);
		}
		finally
		{
			Cursor.Show();
		}
		CommandToExecute = null;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void ExportAllorOne2CSVStartedFromCommandLine()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		string text = null;
		if (CommandToExecute.ToUpper() == "EXPORTALL2CSV")
		{
			ExportFile = null;
		}
		if (ExportFile != null && !ExportFile.EndsWith(".csv"))
		{
			ExportFile += ".csv";
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Export file: " + ExportFile);
		}
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
		try
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Export All to csv started from commandline");
			}
			if (ExportDir == null)
			{
				folderPath = folderPath + "\\RVTools_export_all_" + DateTime.Now.ToLocalTime().ToString("yyyy-MM-dd_HH.mm.ss");
				Directory.CreateDirectory(folderPath);
			}
			else
			{
				if (!Directory.Exists(ExportDir))
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(currentMethod.Name + " ERROR! Export directory not found! " + ExportDir);
					}
					return;
				}
				folderPath = ExportDir;
			}
			text = ((ExportFile != null) ? ("\\" + ExportFile) : ("\\RVTools_" + CommandToExecute + "_" + DateTime.Now.ToLocalTime().ToString("yyyy-MM-dd_HH.mm.ss") + ".csv"));
			if (CommandToExecute.ToUpper() == "EXPORTALL2CSV")
			{
				text = null;
			}
			if (CommandToExecute.ToUpper() == "EXPORTALL2CSV")
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Export all files " + text);
				}
			}
			else if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Export all files to " + folderPath + text);
			}
			ExportAllorOne2csv(folderPath, text, startedFromCommandline: true);
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Handled Exception: " + ex.Message);
			}
		}
		finally
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + "Export All to csv terminated!");
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void ExportAllorOne2csv(string DirName, string FileName, bool startedFromCommandline = false)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("Export", "CSV", new Dictionary<string, string>
		{
			{
				"Selection",
				CommandToExecute.ToUpper()
			},
			{
				"StartedFromCommandline",
				startedFromCommandline.ToString()
			}
		});
		if (CommandToExecute.ToUpper() == "EXPORTALL2CSV" || CommandToExecute.ToUpper() == "EXPORTVINFO2CSV")
		{
			TabPagevInfo();
			TabPages.SelectTab("tabvInfo");
			if (FileName != null)
			{
				ExportDataGrid2csv(ref dataGridvInfo, DirName + FileName);
			}
			else
			{
				ExportDataGrid2csv(ref dataGridvInfo, DirName + "\\RVTools_" + TabPages.SelectedTab.Name + ".csv");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Datagrid to csv: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute.ToUpper() == "EXPORTALL2CSV" || CommandToExecute.ToUpper() == "EXPORTVCPU2CSV")
		{
			TabPagevCPU();
			TabPages.SelectTab("tabvCPU");
			if (FileName != null)
			{
				ExportDataGrid2csv(ref dataGridvCPU, DirName + FileName);
			}
			else
			{
				ExportDataGrid2csv(ref dataGridvCPU, DirName + "\\RVTools_" + TabPages.SelectedTab.Name + ".csv");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Datagrid to csv: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute.ToUpper() == "EXPORTALL2CSV" || CommandToExecute.ToUpper() == "EXPORTVMEMORY2CSV")
		{
			TabPagevMemory();
			TabPages.SelectTab("tabvMemory");
			if (FileName != null)
			{
				ExportDataGrid2csv(ref dataGridvMemory, DirName + FileName);
			}
			else
			{
				ExportDataGrid2csv(ref dataGridvMemory, DirName + "\\RVTools_" + TabPages.SelectedTab.Name + ".csv");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Datagrid to csv: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute.ToUpper() == "EXPORTALL2CSV" || CommandToExecute.ToUpper() == "EXPORTVDISK2CSV")
		{
			TabPagevDisk();
			TabPages.SelectTab("tabvDisk");
			if (FileName != null)
			{
				ExportDataGrid2csv(ref dataGridvDisk, DirName + FileName);
			}
			else
			{
				ExportDataGrid2csv(ref dataGridvDisk, DirName + "\\RVTools_" + TabPages.SelectedTab.Name + ".csv");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Datagrid to csv: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute.ToUpper() == "EXPORTALL2CSV" || CommandToExecute.ToUpper() == "EXPORTVPARTITION2CSV")
		{
			TabPagevPartition();
			TabPages.SelectTab("tabvPartition");
			if (FileName != null)
			{
				ExportDataGrid2csv(ref dataGridvPartition, DirName + FileName);
			}
			else
			{
				ExportDataGrid2csv(ref dataGridvPartition, DirName + "\\RVTools_" + TabPages.SelectedTab.Name + ".csv");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Datagrid to csv: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute.ToUpper() == "EXPORTALL2CSV" || CommandToExecute.ToUpper() == "EXPORTVNETWORK2CSV")
		{
			TabPagevNetwork();
			TabPages.SelectTab("tabvNetwork");
			if (FileName != null)
			{
				ExportDataGrid2csv(ref dataGridvNetwork, DirName + FileName);
			}
			else
			{
				ExportDataGrid2csv(ref dataGridvNetwork, DirName + "\\RVTools_" + TabPages.SelectedTab.Name + ".csv");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Datagrid to csv: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute.ToUpper() == "EXPORTALL2CSV" || CommandToExecute.ToUpper() == "EXPORTVCD2CSV")
		{
			TabPagevCD();
			TabPages.SelectTab("tabvCD");
			if (FileName != null)
			{
				ExportDataGrid2csv(ref dataGridvCD, DirName + FileName);
			}
			else
			{
				ExportDataGrid2csv(ref dataGridvCD, DirName + "\\RVTools_" + TabPages.SelectedTab.Name + ".csv");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Datagrid to csv: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute.ToUpper() == "EXPORTALL2CSV" || CommandToExecute.ToUpper() == "EXPORTVUSB2CSV")
		{
			TabPagevUSB();
			TabPages.SelectTab("tabvUSB");
			if (FileName != null)
			{
				ExportDataGrid2csv(ref dataGridvUSB, DirName + FileName);
			}
			else
			{
				ExportDataGrid2csv(ref dataGridvUSB, DirName + "\\RVTools_" + TabPages.SelectedTab.Name + ".csv");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Datagrid to csv: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute.ToUpper() == "EXPORTALL2CSV" || CommandToExecute.ToUpper() == "EXPORTVSNAPSHOT2CSV")
		{
			TabPagevSnapshot();
			TabPages.SelectTab("tabvSnapshot");
			if (FileName != null)
			{
				ExportDataGrid2csv(ref dataGridvSnapshot, DirName + FileName);
			}
			else
			{
				ExportDataGrid2csv(ref dataGridvSnapshot, DirName + "\\RVTools_" + TabPages.SelectedTab.Name + ".csv");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Datagrid to csv: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute.ToUpper() == "EXPORTALL2CSV" || CommandToExecute.ToUpper() == "EXPORTVTOOLS2CSV")
		{
			TabPagevTools();
			TabPages.SelectTab("tabvTools");
			if (FileName != null)
			{
				ExportDataGrid2csv(ref dataGridvTools, DirName + FileName);
			}
			else
			{
				ExportDataGrid2csv(ref dataGridvTools, DirName + "\\RVTools_" + TabPages.SelectedTab.Name + ".csv");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Datagrid to csv: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute.ToUpper() == "EXPORTALL2CSV" || CommandToExecute.ToUpper() == "EXPORTVSOURCE2CSV")
		{
			TabPagevSource();
			TabPages.SelectTab("tabvSource");
			if (FileName != null)
			{
				ExportDataGrid2csv(ref dataGridvSource, DirName + FileName);
			}
			else
			{
				ExportDataGrid2csv(ref dataGridvSource, DirName + "\\RVTools_" + TabPages.SelectedTab.Name + ".csv");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Datagrid to csv: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute.ToUpper() == "EXPORTALL2CSV" || CommandToExecute.ToUpper() == "EXPORTVCLUSTER2CSV")
		{
			TabPagevCluster();
			TabPages.SelectTab("tabvCluster");
			if (FileName != null)
			{
				ExportDataGrid2csv(ref dataGridvCluster, DirName + FileName);
			}
			else
			{
				ExportDataGrid2csv(ref dataGridvCluster, DirName + "\\RVTools_" + TabPages.SelectedTab.Name + ".csv");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Datagrid to csv: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute.ToUpper() == "EXPORTALL2CSV" || CommandToExecute.ToUpper() == "EXPORTVRP2CSV")
		{
			TabPagevRP();
			TabPages.SelectTab("tabvRP");
			if (FileName != null)
			{
				ExportDataGrid2csv(ref dataGridvRP, DirName + FileName);
			}
			else
			{
				ExportDataGrid2csv(ref dataGridvRP, DirName + "\\RVTools_" + TabPages.SelectedTab.Name + ".csv");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Datagrid to csv: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute.ToUpper() == "EXPORTALL2CSV" || CommandToExecute.ToUpper() == "EXPORTVHOST2CSV")
		{
			TabPagevHost();
			TabPages.SelectTab("tabvHost");
			if (FileName != null)
			{
				ExportDataGrid2csv(ref dataGridvHost, DirName + FileName);
			}
			else
			{
				ExportDataGrid2csv(ref dataGridvHost, DirName + "\\RVTools_" + TabPages.SelectedTab.Name + ".csv");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Datagrid to csv: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute.ToUpper() == "EXPORTALL2CSV" || CommandToExecute.ToUpper() == "EXPORTVHBA2CSV")
		{
			TabPagevHBA();
			TabPages.SelectTab("tabvHBA");
			if (FileName != null)
			{
				ExportDataGrid2csv(ref dataGridvHBA, DirName + FileName);
			}
			else
			{
				ExportDataGrid2csv(ref dataGridvHBA, DirName + "\\RVTools_" + TabPages.SelectedTab.Name + ".csv");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Datagrid to csv: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute.ToUpper() == "EXPORTALL2CSV" || CommandToExecute.ToUpper() == "EXPORTVNIC2CSV")
		{
			TabPagevNIC();
			TabPages.SelectTab("tabvNIC");
			if (FileName != null)
			{
				ExportDataGrid2csv(ref dataGridvNIC, DirName + FileName);
			}
			else
			{
				ExportDataGrid2csv(ref dataGridvNIC, DirName + "\\RVTools_" + TabPages.SelectedTab.Name + ".csv");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Datagrid to csv: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute.ToUpper() == "EXPORTALL2CSV" || CommandToExecute.ToUpper() == "EXPORTVSWITCH2CSV")
		{
			TabPagevSwitch();
			TabPages.SelectTab("tabvSwitch");
			if (FileName != null)
			{
				ExportDataGrid2csv(ref dataGridvSwitch, DirName + FileName);
			}
			else
			{
				ExportDataGrid2csv(ref dataGridvSwitch, DirName + "\\RVTools_" + TabPages.SelectedTab.Name + ".csv");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Datagrid to csv: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute.ToUpper() == "EXPORTALL2CSV" || CommandToExecute.ToUpper() == "EXPORTVPORT2CSV")
		{
			TabPagevPort();
			TabPages.SelectTab("tabvPort");
			if (FileName != null)
			{
				ExportDataGrid2csv(ref dataGridvPort, DirName + FileName);
			}
			else
			{
				ExportDataGrid2csv(ref dataGridvPort, DirName + "\\RVTools_" + TabPages.SelectedTab.Name + ".csv");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Datagrid to csv: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute.ToUpper() == "EXPORTALL2CSV" || CommandToExecute.ToUpper() == "EXPORTDVSWITCH2CSV")
		{
			TabPagedvSwitch();
			TabPages.SelectTab("tabdvSwitch");
			if (FileName != null)
			{
				ExportDataGrid2csv(ref dataGriddvSwitch, DirName + FileName);
			}
			else
			{
				ExportDataGrid2csv(ref dataGriddvSwitch, DirName + "\\RVTools_" + TabPages.SelectedTab.Name + ".csv");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Datagrid to csv: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute.ToUpper() == "EXPORTALL2CSV" || CommandToExecute.ToUpper() == "EXPORTDVPORT2CSV")
		{
			TabPagedvPort();
			TabPages.SelectTab("tabdvPort");
			if (FileName != null)
			{
				ExportDataGrid2csv(ref dataGriddvPort, DirName + FileName);
			}
			else
			{
				ExportDataGrid2csv(ref dataGriddvPort, DirName + "\\RVTools_" + TabPages.SelectedTab.Name + ".csv");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Datagrid to csv: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute.ToUpper() == "EXPORTALL2CSV" || CommandToExecute.ToUpper() == "EXPORTVSC+VMK2CSV")
		{
			TabPagevSC_VMK();
			TabPages.SelectTab("tabvSC_VMK");
			if (FileName != null)
			{
				ExportDataGrid2csv(ref dataGridvSC_VMK, DirName + FileName);
			}
			else
			{
				ExportDataGrid2csv(ref dataGridvSC_VMK, DirName + "\\RVTools_" + TabPages.SelectedTab.Name + ".csv");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Datagrid to csv: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute.ToUpper() == "EXPORTALL2CSV" || CommandToExecute.ToUpper() == "EXPORTVDATASTORE2CSV")
		{
			TabPagevDatastore();
			TabPages.SelectTab("tabvDatastore");
			if (FileName != null)
			{
				ExportDataGrid2csv(ref dataGridvDatastore, DirName + FileName);
			}
			else
			{
				ExportDataGrid2csv(ref dataGridvDatastore, DirName + "\\RVTools_" + TabPages.SelectedTab.Name + ".csv");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Datagrid to csv: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute.ToUpper() == "EXPORTALL2CSV" || CommandToExecute.ToUpper() == "EXPORTVMULTIPATH2CSV")
		{
			TabPagevMultiPath();
			TabPages.SelectTab("tabvMultiPath");
			if (FileName != null)
			{
				ExportDataGrid2csv(ref dataGridvMultiPath, DirName + FileName);
			}
			else
			{
				ExportDataGrid2csv(ref dataGridvMultiPath, DirName + "\\RVTools_" + TabPages.SelectedTab.Name + ".csv");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Datagrid to csv: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute.ToUpper() == "EXPORTALL2CSV" || CommandToExecute.ToUpper() == "EXPORTVLICENSE2CSV")
		{
			TabPagevLicense();
			TabPages.SelectTab("tabvLicense");
			if (FileName != null)
			{
				ExportDataGrid2csv(ref dataGridvLicense, DirName + FileName);
			}
			else
			{
				ExportDataGrid2csv(ref dataGridvLicense, DirName + "\\RVTools_" + TabPages.SelectedTab.Name + ".csv");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Datagrid to csv: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute.ToUpper() == "EXPORTALL2CSV" || CommandToExecute.ToUpper() == "EXPORTVFILEINFO2CSV")
		{
			TabPagevFileInfo();
			TabPages.SelectTab("tabvFileInfo");
			if (FileName != null)
			{
				ExportDataGrid2csv(ref dataGridvFileInfo, DirName + FileName);
			}
			else
			{
				ExportDataGrid2csv(ref dataGridvFileInfo, DirName + "\\RVTools_" + TabPages.SelectedTab.Name + ".csv");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Datagrid to csv: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute.ToUpper() == "EXPORTALL2CSV" || CommandToExecute.ToUpper() == "EXPORTVHEALTH2CSV")
		{
			TabPagevHealth();
			TabPages.SelectTab("tabvHealth");
			if (FileName != null)
			{
				ExportDataGrid2csv(ref dataGridvHealth, DirName + FileName);
			}
			else
			{
				ExportDataGrid2csv(ref dataGridvHealth, DirName + "\\RVTools_" + TabPages.SelectedTab.Name + ".csv");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Datagrid to csv: " + TabPages.SelectedTab.Name);
			}
		}
		CommandToExecute = null;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void ExportDataGrid(ref DataGridView datagrid, string FileName)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		string text = datagrid.Parent?.Parent?.Text?.ToUpper() ?? "UNKTAB";
		if (FileName.EndsWith(".csv"))
		{
			Telemetry.AddEvent("Export", "CSV", new Dictionary<string, string>
			{
				{
					"Selection",
					"EXPORT" + text + "2CSV"
				},
				{ "StartedFromCommandline", "False" }
			});
			ExportDataGrid2csv(ref datagrid, FileName);
		}
		else
		{
			Telemetry.AddEvent("Export", "Excel", new Dictionary<string, string>
			{
				{
					"Selection",
					"EXPORT" + text + "2XLS"
				},
				{ "StartedFromCommandline", "False" }
			});
			ExportDataGrid2xls(ref datagrid, FileName);
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private static string ToCsv(string val)
	{
		string listSeparator = Thread.CurrentThread.CurrentCulture.TextInfo.ListSeparator;
		if (string.IsNullOrEmpty(val))
		{
			return string.Empty;
		}
		val = val.Replace('\r', ' ');
		val = val.Replace('\n', ' ');
		if (val.StartsWith("\"") && val.EndsWith("\"") && val.Length >= 2)
		{
			if (val.Length != 2)
			{
				return ToCsv(val.Substring(1, val.Length - 2));
			}
			return string.Empty;
		}
		if (val.Contains('"') || val.Contains(listSeparator))
		{
			string text = val.Replace("\"", "\"\"");
			return "\"" + text + "\"";
		}
		return val;
	}

	private void ExportDataGrid2csv(ref DataGridView datagrid, string FileName)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		string listSeparator = Thread.CurrentThread.CurrentCulture.TextInfo.ListSeparator;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Thread.CurrentThread.CurrentCulture.TextInfo.ListSeparator=" + listSeparator);
		}
		try
		{
			StreamWriter streamWriter = new StreamWriter(FileName, append: false, Encoding.Default);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " StreamWriter created for " + FileName);
			}
			string text = "";
			for (int i = 0; i < datagrid.Columns.Count; i++)
			{
				text = ((!DBColumnNames) ? (text.ToString() + datagrid.Columns[i].HeaderText + listSeparator) : (text.ToString() + datagrid.Columns[i].Name + listSeparator));
			}
			text = text.Remove(text.Length - 1, 1);
			streamWriter.WriteLine(text);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Titles created.");
			}
			for (int j = 0; j < datagrid.RowCount; j++)
			{
				List<string> list = new List<string>();
				for (int k = 0; k < datagrid.Rows[j].Cells.Count; k++)
				{
					list.Add(ToCsv(datagrid.Rows[j].Cells[k].Value?.ToString()));
				}
				streamWriter.WriteLine(string.Join(listSeparator, list));
			}
			streamWriter.Close();
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Data written.");
			}
		}
		catch (Exception err)
		{
			UserNotification.ReportError("Export to CSV failed", err);
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void ExportAll2xlsMenuItem_Click(object sender, EventArgs e)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		CommandToExecute = "ExportAll2xls";
		SaveFileDialog saveFileDialog = new SaveFileDialog();
		saveFileDialog.Filter = "Excel worksheet (*.xlsx)|*.xlsx";
		saveFileDialog.FileName = "RVTools_export_all_" + DateTime.Now.ToLocalTime().ToString("yyyy-MM-dd_HH.mm.ss");
		saveFileDialog.ValidateNames = true;
		if (saveFileDialog.ShowDialog() == DialogResult.Cancel)
		{
			return;
		}
		try
		{
			Cursor.Hide();
			XLWorkbook Workbook = new XLWorkbook();
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Workbook created.");
			}
			ExportAllorOne2xls(ref Workbook);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheets created.");
			}
			Workbook.SaveAs(saveFileDialog.FileName);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " XLSX file written: " + saveFileDialog.FileName);
			}
		}
		catch (Exception err)
		{
			UserNotification.ReportError("Export to Excel Failed", err);
		}
		finally
		{
			Cursor.Show();
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void ExportAllorOne2XLSStartedFromCommandLine()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Directory name: " + folderPath);
		}
		string text = null;
		try
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Export to xlsx started from commandline");
			}
			if (ExportFile != null)
			{
				if (!ExportFile.EndsWith(".xlsx"))
				{
					ExportFile += ".xlsx";
				}
				ExportFile = ExportFile.Replace(".xls.xlsx", ".xlsx");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Export file: " + ExportFile);
			}
			if (ExportDir == null)
			{
				text = ((ExportFile != null) ? (folderPath + "\\" + text) : (folderPath + "\\RVTools_export_" + DateTime.Now.ToLocalTime().ToString("yyyy-MM-dd_HH.mm.ss") + ".xlsx"));
			}
			else
			{
				if (!Directory.Exists(ExportDir))
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(currentMethod.Name + " ERROR! Export directory not found! " + ExportDir);
					}
					return;
				}
				text = ((ExportFile != null) ? (ExportDir + "\\" + ExportFile) : (ExportDir + "\\RVTools_export_" + DateTime.Now.ToLocalTime().ToString("yyyy-MM-dd_HH.mm.ss") + ".xlsx"));
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Export dir: " + ExportDir);
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Export path: " + text);
			}
			XLWorkbook Workbook = new XLWorkbook();
			InitializeWorkbook(ref Workbook);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Workbook created.");
			}
			ExportAllorOne2xls(ref Workbook, startedFromCommandline: true);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheets created.");
			}
			Workbook.SaveAs(text);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheet count: " + Workbook.Worksheets.Count);
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " XLS file written: " + text);
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void ExportAllorOne2xls(ref XLWorkbook Workbook, bool startedFromCommandline = false)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("Export", "Excel", new Dictionary<string, string>
		{
			{
				"Selection",
				CommandToExecute.ToUpper()
			},
			{
				"StartedFromCommandline",
				startedFromCommandline.ToString()
			}
		});
		CommandToExecute = CommandToExecute.ToUpper();
		if (CommandToExecute.EndsWith("XLS"))
		{
			CommandToExecute = CommandToExecute.Replace("XLS", "XLSX");
		}
		if (CommandToExecute == "EXPORTALL2XLSX" || CommandToExecute == "EXPORTVINFO2XLSX")
		{
			TabPagevInfo();
			TabPages.SelectTab("tabvInfo");
			IXLWorksheet WorkSheet = Workbook.Worksheets.Add("vInfo");
			DataGrid2Worksheet(ref dataGridvInfo, ref WorkSheet);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheet created, data copied: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute == "EXPORTALL2XLSX" || CommandToExecute == "EXPORTVCPU2XLSX")
		{
			TabPagevCPU();
			TabPages.SelectTab("tabvCPU");
			IXLWorksheet WorkSheet2 = Workbook.Worksheets.Add("vCPU");
			DataGrid2Worksheet(ref dataGridvCPU, ref WorkSheet2);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheet created, data copied: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute == "EXPORTALL2XLSX" || CommandToExecute == "EXPORTVMEMORY2XLSX")
		{
			TabPagevMemory();
			TabPages.SelectTab("tabvMemory");
			IXLWorksheet WorkSheet3 = Workbook.Worksheets.Add("vMemory");
			DataGrid2Worksheet(ref dataGridvMemory, ref WorkSheet3);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheet created, data copied: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute == "EXPORTALL2XLSX" || CommandToExecute == "EXPORTVDISK2XLSX")
		{
			TabPagevDisk();
			TabPages.SelectTab("tabvDisk");
			IXLWorksheet WorkSheet4 = Workbook.Worksheets.Add("vDisk");
			DataGrid2Worksheet(ref dataGridvDisk, ref WorkSheet4);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheet created, data copied: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute == "EXPORTALL2XLSX" || CommandToExecute == "EXPORTVPARTITION2XLSX")
		{
			TabPagevPartition();
			TabPages.SelectTab("tabvPartition");
			IXLWorksheet WorkSheet5 = Workbook.Worksheets.Add("vPartition");
			DataGrid2Worksheet(ref dataGridvPartition, ref WorkSheet5);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheet created, data copied: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute == "EXPORTALL2XLSX" || CommandToExecute == "EXPORTVNETWORK2XLSX")
		{
			TabPagevNetwork();
			TabPages.SelectTab("tabvNetwork");
			IXLWorksheet WorkSheet6 = Workbook.Worksheets.Add("vNetwork");
			DataGrid2Worksheet(ref dataGridvNetwork, ref WorkSheet6);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheet created, data copied: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute == "EXPORTALL2XLSX" || CommandToExecute == "EXPORTVCD2XLSX")
		{
			TabPagevCD();
			TabPages.SelectTab("tabvCD");
			IXLWorksheet WorkSheet7 = Workbook.Worksheets.Add("vCD");
			DataGrid2Worksheet(ref dataGridvCD, ref WorkSheet7);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheet created, data copied: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute == "EXPORTALL2XLSX" || CommandToExecute == "EXPORTVUSB2XLSX")
		{
			TabPagevUSB();
			TabPages.SelectTab("tabvUSB");
			IXLWorksheet WorkSheet8 = Workbook.Worksheets.Add("vUSB");
			DataGrid2Worksheet(ref dataGridvUSB, ref WorkSheet8);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheet created, data copied: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute == "EXPORTALL2XLSX" || CommandToExecute == "EXPORTVSNAPSHOT2XLSX")
		{
			TabPagevSnapshot();
			TabPages.SelectTab("tabvSnapshot");
			IXLWorksheet WorkSheet9 = Workbook.Worksheets.Add("vSnapshot");
			DataGrid2Worksheet(ref dataGridvSnapshot, ref WorkSheet9);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheet created, data copied: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute == "EXPORTALL2XLSX" || CommandToExecute == "EXPORTVTOOLS2XLSX")
		{
			TabPagevTools();
			TabPages.SelectTab("tabvTools");
			IXLWorksheet WorkSheet10 = Workbook.Worksheets.Add("vTools");
			DataGrid2Worksheet(ref dataGridvTools, ref WorkSheet10);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheet created, data copied: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute == "EXPORTALL2XLSX" || CommandToExecute == "EXPORTVSOURCE2XLSX")
		{
			TabPagevSource();
			TabPages.SelectTab("tabvSource");
			IXLWorksheet WorkSheet11 = Workbook.Worksheets.Add("vSource");
			DataGrid2Worksheet(ref dataGridvSource, ref WorkSheet11);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheet created, data copied: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute == "EXPORTALL2XLSX" || CommandToExecute == "EXPORTVRP2XLSX")
		{
			TabPagevRP();
			TabPages.SelectTab("tabvRP");
			IXLWorksheet WorkSheet12 = Workbook.Worksheets.Add("vRP");
			DataGrid2Worksheet(ref dataGridvRP, ref WorkSheet12);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheet created, data copied: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute == "EXPORTALL2XLSX" || CommandToExecute == "EXPORTVCLUSTER2XLSX")
		{
			TabPagevCluster();
			TabPages.SelectTab("tabvCluster");
			IXLWorksheet WorkSheet13 = Workbook.Worksheets.Add("vCluster");
			DataGrid2Worksheet(ref dataGridvCluster, ref WorkSheet13);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheet created, data copied: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute == "EXPORTALL2XLSX" || CommandToExecute == "EXPORTVHOST2XLSX")
		{
			TabPagevHost();
			TabPages.SelectTab("tabvHost");
			IXLWorksheet WorkSheet14 = Workbook.Worksheets.Add("vHost");
			DataGrid2Worksheet(ref dataGridvHost, ref WorkSheet14);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheet created, data copied: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute == "EXPORTALL2XLSX" || CommandToExecute == "EXPORTVHBA2XLSX")
		{
			TabPagevHBA();
			TabPages.SelectTab("tabvHBA");
			IXLWorksheet WorkSheet15 = Workbook.Worksheets.Add("vHBA");
			DataGrid2Worksheet(ref dataGridvHBA, ref WorkSheet15);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheet created, data copied: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute == "EXPORTALL2XLSX" || CommandToExecute == "EXPORTVNIC2XLSX")
		{
			TabPagevNIC();
			TabPages.SelectTab("tabvNIC");
			IXLWorksheet WorkSheet16 = Workbook.Worksheets.Add("vNIC");
			DataGrid2Worksheet(ref dataGridvNIC, ref WorkSheet16);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheet created, data copied: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute == "EXPORTALL2XLSX" || CommandToExecute == "EXPORTVSWITCH2XLSX")
		{
			TabPagevSwitch();
			TabPages.SelectTab("tabvSwitch");
			IXLWorksheet WorkSheet17 = Workbook.Worksheets.Add("vSwitch");
			DataGrid2Worksheet(ref dataGridvSwitch, ref WorkSheet17);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheet created, data copied: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute == "EXPORTALL2XLSX" || CommandToExecute == "EXPORTVPORT2XLSX")
		{
			TabPagevPort();
			TabPages.SelectTab("tabvPort");
			IXLWorksheet WorkSheet18 = Workbook.Worksheets.Add("vPort");
			DataGrid2Worksheet(ref dataGridvPort, ref WorkSheet18);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheet created, data copied: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute == "EXPORTALL2XLSX" || CommandToExecute == "EXPORTDVSWITCH2XLSX")
		{
			TabPagedvSwitch();
			TabPages.SelectTab("tabdvSwitch");
			IXLWorksheet WorkSheet19 = Workbook.Worksheets.Add("dvSwitch");
			DataGrid2Worksheet(ref dataGriddvSwitch, ref WorkSheet19);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheet created, data copied: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute == "EXPORTALL2XLSX" || CommandToExecute == "EXPORTDVPORT2XLSX")
		{
			TabPagedvPort();
			TabPages.SelectTab("tabdvPort");
			IXLWorksheet WorkSheet20 = Workbook.Worksheets.Add("dvPort");
			DataGrid2Worksheet(ref dataGriddvPort, ref WorkSheet20);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheet created, data copied: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute == "EXPORTALL2XLSX" || CommandToExecute == "EXPORTVSC+VMK2XLSX")
		{
			TabPagevSC_VMK();
			TabPages.SelectTab("tabvSC_VMK");
			IXLWorksheet WorkSheet21 = Workbook.Worksheets.Add("vSC_VMK");
			DataGrid2Worksheet(ref dataGridvSC_VMK, ref WorkSheet21);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheet created, data copied: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute == "EXPORTALL2XLSX" || CommandToExecute == "EXPORTVDATASTORE2XLSX")
		{
			TabPagevDatastore();
			TabPages.SelectTab("tabvDatastore");
			IXLWorksheet WorkSheet22 = Workbook.Worksheets.Add("vDatastore");
			DataGrid2Worksheet(ref dataGridvDatastore, ref WorkSheet22);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheet created, data copied: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute == "EXPORTALL2XLSX" || CommandToExecute == "EXPORTVMULTIPATH2XLSX")
		{
			TabPagevMultiPath();
			TabPages.SelectTab("tabvMultiPath");
			IXLWorksheet WorkSheet23 = Workbook.Worksheets.Add("vMultiPath");
			DataGrid2Worksheet(ref dataGridvMultiPath, ref WorkSheet23);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheet created, data copied: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute == "EXPORTALL2XLSX" || CommandToExecute == "EXPORTVLICENSE2XLSX")
		{
			TabPagevLicense();
			TabPages.SelectTab("tabvLicense");
			IXLWorksheet WorkSheet24 = Workbook.Worksheets.Add("vLicense");
			DataGrid2Worksheet(ref dataGridvLicense, ref WorkSheet24);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheet created, data copied: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute == "EXPORTALL2XLSX" || CommandToExecute == "EXPORTVFILEINFO2XLSX")
		{
			TabPagevFileInfo();
			TabPages.SelectTab("tabvFileInfo");
			IXLWorksheet WorkSheet25 = Workbook.Worksheets.Add("vFileInfo");
			DataGrid2Worksheet(ref dataGridvFileInfo, ref WorkSheet25);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheet created, data copied: " + TabPages.SelectedTab.Name);
			}
		}
		if (CommandToExecute == "EXPORTALL2XLSX" || CommandToExecute == "EXPORTVHEALTH2XLSX")
		{
			TabPagevHealth();
			TabPages.SelectTab("tabvHealth");
			IXLWorksheet WorkSheet26 = Workbook.Worksheets.Add("vHealth");
			DataGrid2Worksheet(ref dataGridvHealth, ref WorkSheet26);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Worksheet created, data copied: " + TabPages.SelectedTab.Name);
			}
		}
		CommandToExecute = null;
		IXLWorksheet iXLWorksheet = Workbook.Worksheets.Add("vMetaData");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Worksheet created: " + iXLWorksheet);
		}
		iXLWorksheet.Cell(1, 1).Value = "RVTools major version";
		iXLWorksheet.Cell(1, 2).Value = "RVTools version";
		iXLWorksheet.Cell(1, 3).Value = "xlsx creation datetime";
		iXLWorksheet.Cell(1, 4).Value = "Server";
		string value = Assembly.GetExecutingAssembly().GetName().Version.Major + "." + Assembly.GetExecutingAssembly().GetName().Version.Minor;
		iXLWorksheet.Cell(2, 1).Value = value;
		iXLWorksheet.Cell(2, 2).Value = Assembly.GetExecutingAssembly().GetName().Version.ToString();
		iXLWorksheet.Cell(2, 3).Value = DateTime.Now.ToString();
		iXLWorksheet.Cell(2, 4).Value = VISDK.Server;
		for (int i = 0; i < 4; i++)
		{
			iXLWorksheet.Column(i + 1).AdjustToContents();
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void ExportDataGrid2xls(ref DataGridView datagrid, string FileName)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		XLWorkbook Workbook = new XLWorkbook();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Workbook created");
		}
		InitializeWorkbook(ref Workbook);
		string text = datagrid.Name.Replace("dataGrid", "");
		IXLWorksheet WorkSheet = Workbook.Worksheets.Add(text);
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Worksheet created: " + text);
		}
		DataGrid2Worksheet(ref datagrid, ref WorkSheet);
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Datagrid information copied to worksheet");
		}
		try
		{
			Workbook.SaveAs(FileName);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " XLSX file created: " + FileName);
			}
		}
		catch (Exception err)
		{
			UserNotification.ReportError("Export to Excel failed", err);
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void InitializeWorkbook(ref XLWorkbook Workbook)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Workbook.Properties.Title = "RVTools export";
		Workbook.Properties.Author = "Robware";
		Workbook.Properties.Comments = "https:\\\\www.robware.net\\rvtools";
		Workbook.EventTracking = XLEventTracking.Disabled;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void DataGrid2Worksheet(ref DataGridView datagrid, ref IXLWorksheet WorkSheet)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Datagrid name: " + datagrid.Name);
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Datagrid rows: " + datagrid.RowCount);
		}
		WorkSheet.Style.Font.FontName = "Verdana";
		WorkSheet.Style.Font.FontSize = 9.0;
		int num = 1;
		DateTime now = DateTime.Now;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Start Format columns ");
		}
		for (int i = 0; i < datagrid.Columns.Count; i++)
		{
			string format = datagrid.Columns[i].DefaultCellStyle.Format;
			try
			{
				switch (format)
				{
				case "":
					WorkSheet.Column(i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
					break;
				case "G":
					WorkSheet.Column(i + 1).Style.DateFormat.Format = "yyyy/MM/dd HH:mm:ss";
					WorkSheet.Column(i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
					break;
				case "N0":
					WorkSheet.Column(i + 1).Style.NumberFormat.Format = "#,##0";
					WorkSheet.Column(i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
					break;
				case "N2":
					WorkSheet.Column(i + 1).Style.NumberFormat.Format = "#,##0.00";
					WorkSheet.Column(i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
					break;
				case "N4":
					WorkSheet.Column(i + 1).Style.NumberFormat.Format = "#,##0.0000";
					WorkSheet.Column(i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
					break;
				default:
					WorkSheet.Column(i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
					break;
				}
			}
			catch
			{
			}
		}
		TimeSpan timeSpan = DateTime.Now - now;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Format columns, time it took (ms): " + timeSpan.Milliseconds);
		}
		now = DateTime.Now;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Start write header");
		}
		for (int j = 1; j <= datagrid.ColumnCount; j++)
		{
			if (DBColumnNames)
			{
				WorkSheet.Cell(num, j).Value = datagrid.Columns[j - 1].Name;
			}
			else
			{
				WorkSheet.Cell(num, j).Value = datagrid.Columns[j - 1].HeaderText;
			}
			WorkSheet.Cell(num, j).Style.Font.SetBold();
			WorkSheet.Cell(num, j).Style.Font.FontColor = XLColor.White;
			WorkSheet.Cell(num, j).Style.Fill.BackgroundColor = XLColor.Black;
			WorkSheet.Cell(num, j).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " End write header");
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Start copy data");
		}
		now = DateTime.Now;
		for (int k = 0; k < datagrid.Rows.Count; k++)
		{
			num++;
			for (int l = 0; l < datagrid.Columns.Count; l++)
			{
				WorkSheet.Cell(num, l + 1).Value = datagrid.Rows[k].Cells[l].Value;
				string text = WorkSheet.Cell(num, l + 1).DataType.ToString();
				if (text == "Boolean")
				{
					WorkSheet.Cell(num, l + 1).SetDataType(XLDataType.Text);
				}
				if (text == "DateTime" && datagrid.Columns[l].DefaultCellStyle.Format == "")
				{
					WorkSheet.Cell(num, l + 1).SetDataType(XLDataType.Text);
					WorkSheet.Cell(num, l + 1).Value = "'" + datagrid.Rows[k].Cells[l].Value.ToString();
				}
			}
		}
		timeSpan = DateTime.Now - now;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Copy data, time it took (ms): " + timeSpan.Milliseconds);
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " rows written: " + num);
		}
		if (WorkSheet.Name == "vUSB" || WorkSheet.Name == "vCD" || WorkSheet.Name == "vTools")
		{
			WorkSheet.Column(1).Delete();
		}
		WorkSheet.RangeUsed().SetAutoFilter();
		WorkSheet.SheetView.Freeze(1, 1);
		now = DateTime.Now;
		WorkSheet.ColumnsUsed().AdjustToContents();
		timeSpan = DateTime.Now - now;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Resize columns, time it took (ms): " + timeSpan.Milliseconds);
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void toolStripPreferences_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "Edit.Preferences");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		bool vInfoShowFolder = PreferencesClass.vInfoShowFolder;
		bool vInfoShowAppName = PreferencesClass.vInfoShowAppName;
		bool excludeAnnotations = PreferencesClass.ExcludeAnnotations;
		bool excludeTags = PreferencesClass.ExcludeTags;
		bool getFriendlyNames = PreferencesClass.GetFriendlyNames;
		bool getFileInfo = PreferencesClass.GetFileInfo;
		new FPreferences().ShowDialog();
		if ((!vInfoShowFolder && PreferencesClass.vInfoShowFolder) || (vInfoShowFolder && !PreferencesClass.vInfoShowFolder) || (!vInfoShowAppName && PreferencesClass.vInfoShowAppName) || (vInfoShowAppName && !PreferencesClass.vInfoShowAppName) || (!excludeAnnotations && PreferencesClass.ExcludeAnnotations) || (excludeAnnotations && !PreferencesClass.ExcludeAnnotations) || (!excludeTags && PreferencesClass.ExcludeTags) || (excludeTags && !PreferencesClass.ExcludeTags) || (!getFriendlyNames && PreferencesClass.GetFriendlyNames) || (!getFileInfo && PreferencesClass.GetFileInfo))
		{
			Cursor = Cursors.WaitCursor;
			PleaseWait = new FWait();
			PleaseWait.Show();
			Application.DoEvents();
			QuerySDKInventory();
			PleaseWait.Text = "RVTools: Processing data. Please wait.....";
			PleaseWait.Refresh();
			PleaseWait.Close();
			ShowSelectedTab();
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void exitToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "File.Exit");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		VISDK.Disconnect();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
		Close();
	}

	private void toolStripMenuFilter_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "View.Filter");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		new Filter().ShowDialog();
		TabPagevInfo_Refresh_data = true;
		TabPagevCPU_Refresh_data = true;
		TabPagevMemory_Refresh_data = true;
		TabPagevDisk_Refresh_data = true;
		TabPagevPartition_Refresh_data = true;
		TabPagevNetwork_Refresh_data = true;
		TabPagevCD_Refresh_data = true;
		TabPagevUSB_Refresh_data = true;
		TabPagevSnapshot_Refresh_data = true;
		TabPagevTools_Refresh_data = true;
		TabPagevRP_Refresh_data = true;
		TabPagevSource_Refresh_data = true;
		TabPagevCluster_Refresh_data = true;
		TabPagevHost_Refresh_data = true;
		TabPagevHBA_Refresh_data = true;
		TabPagevNIC_Refresh_data = true;
		TabPagevSwitch_Refresh_data = true;
		TabPagevPort_Refresh_data = true;
		TabPagedvSwitch_Refresh_data = true;
		TabPagedvPort_Refresh_data = true;
		TabPagevSC_VMK_Refresh_data = true;
		TabPagevMultiPath_Refresh_data = true;
		TabPagevDatastore_Refresh_data = true;
		TabPagevLicense_Refresh_data = true;
		TabPagevFileInfo_Refresh_data = true;
		StatusFilter.Visible = FilterClass.ShowFilterIcon();
		ShowSelectedTab();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void VMtoolStripMenuItem1_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "VM.Info");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		TabPages.SelectTab("tabvInfo");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void CPUToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "VM.CPU");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		TabPages.SelectTab("tabvCPU");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void MemoryToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "VM.Memory");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		TabPages.SelectTab("tabvMemory");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void toolStripMenuItem1_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "VM.Disk");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		TabPages.SelectTab("tabvDisk");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void PartiontoolStripMenuItem_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "VM.Partition");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		TabPages.SelectTab("tabvPartition");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void NetworktoolStripMenuItem1_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "VM.Network");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		TabPages.SelectTab("tabvNetwork");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void toolStripMenuItem2_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "VM.CD");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		TabPages.SelectTab("tabvCD");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void toolStripMenuItem3_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "VM.USB");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		TabPages.SelectTab("tabvUSB");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void SnapshotToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "VM.Snapshot");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		TabPages.SelectTab("tabvSnapshot");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void vMToolsToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "VM.VmTools");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		TabPages.SelectTab("tabvTools");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void vSourceStripMenuItem_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "ESX.Source");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		TabPages.SelectTab("tabvSource");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void vClusterStripMenuItem_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "ESX.Cluster");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		TabPages.SelectTab("tabvCluster");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void vRPToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "ESX.ResourcePool");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		TabPages.SelectTab("tabvRP");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void vHostToolstripMenuItem_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "ESX.Host");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		TabPages.SelectTab("tabvHost");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void vHBAToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "ESX.Hba");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		TabPages.SelectTab("tabvHBA");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void NICToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "ESX.Nic");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		TabPages.SelectTab("tabvNIC");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void vSwitchToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "ESX.Switch");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		TabPages.SelectTab("tabvSwitch");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void dvSwitchtoolStripMenuItem_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "ESX.DistributedSwitch");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		TabPages.SelectTab("tabdvSwitch");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void vConsoleToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "ESX.SvcConsoleVmKernel");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		TabPages.SelectTab("tabvSC_VMK");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void vPortToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "ESX.Port");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		TabPages.SelectTab("tabvPort");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void dvPorttoolStripMenuItem_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "ESX.DistributedPort");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		TabPages.SelectTab("tabdvPort");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void DatastoreToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "ESX.Datastore");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		TabPages.SelectTab("tabvDatastore");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void licenseToolStripMenuItem_Click_1(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "ESX.Licence");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		TabPages.SelectTab("tabvLicense");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void multiPathToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "ESX.MultiPath");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		TabPages.SelectTab("tabvMultiPath");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "View.Refresh");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		if (!(Cursor == Cursors.WaitCursor))
		{
			Cursor = Cursors.WaitCursor;
			PleaseWait = new FWait();
			PleaseWait.Show();
			Application.DoEvents();
			QuerySDKInventory();
			StatusLastRefresh.Text = "Last refresh: " + DateTime.Now.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
			PleaseWait.Text = "RVTools: Processing data. Please wait.....";
			PleaseWait.Refresh();
			PleaseWait.Close();
			ShowSelectedTab();
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
		}
	}

	private void HealthPropertiesToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "Health.Properties");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		new FHealthCheckProperties().ShowDialog();
		TabPages.SelectTab("tabvHealth");
		TabPagevHealth();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void HealthCheckToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "Health.Check");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		TabPages.SelectTab("tabvHealth");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "Help.About");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		new AboutBox().ShowDialog();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void CheckForUpdatestoolStripMenu_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "Help.VersionInfo");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		try
		{
			Process.Start("https://www.robware.net/versionInfo");
		}
		catch
		{
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void twittertoolStripMenuItem_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuOption", "Help.Twitter");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		try
		{
			Process.Start("http://www.twitter.com/rvtools");
		}
		catch
		{
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void documentationToolStripMenuItem_Click(object sender, EventArgs e)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		Telemetry.AddEvent("MenuOption", "Help.Documentation");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		try
		{
			Process.Start(Path.GetDirectoryName(Application.ExecutablePath) + "\\rvtools.pdf");
		}
		catch
		{
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void tabControl1_Selected(object sender, TabControlEventArgs e)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		ShowSelectedTab();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void ShowSelectedTab()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		if (TabPages.SelectedTab.Name == "tabvInfo")
		{
			tabvInfo.Show();
			TabPagevInfo();
		}
		else if (TabPages.SelectedTab.Name == "tabvCPU")
		{
			tabvCPU.Show();
			TabPagevCPU();
		}
		else if (TabPages.SelectedTab.Name == "tabvMemory")
		{
			tabvMemory.Show();
			TabPagevMemory();
		}
		else if (TabPages.SelectedTab.Name == "tabvDisk")
		{
			tabvDisk.Show();
			TabPagevDisk();
		}
		else if (TabPages.SelectedTab.Name == "tabvPartition")
		{
			tabvPartition.Show();
			TabPagevPartition();
		}
		else if (TabPages.SelectedTab.Name == "tabvNetwork")
		{
			tabvNetwork.Show();
			TabPagevNetwork();
		}
		else if (TabPages.SelectedTab.Name == "tabvCD")
		{
			tabvCD.Show();
			TabPagevCD();
		}
		else if (TabPages.SelectedTab.Name == "tabvUSB")
		{
			tabvUSB.Show();
			TabPagevUSB();
		}
		else if (TabPages.SelectedTab.Name == "tabvSnapshot")
		{
			tabvSnapshot.Show();
			TabPagevSnapshot();
		}
		else if (TabPages.SelectedTab.Name == "tabvTools")
		{
			tabvTools.Show();
			TabPagevTools();
		}
		else if (TabPages.SelectedTab.Name == "tabvRP")
		{
			tabvRP.Show();
			TabPagevRP();
		}
		else if (TabPages.SelectedTab.Name == "tabvSource")
		{
			tabvSource.Show();
			TabPagevSource();
		}
		else if (TabPages.SelectedTab.Name == "tabvCluster")
		{
			tabvCluster.Show();
			TabPagevCluster();
		}
		else if (TabPages.SelectedTab.Name == "tabvHost")
		{
			tabvHost.Show();
			TabPagevHost();
		}
		else if (TabPages.SelectedTab.Name == "tabvHost")
		{
			tabvHost.Show();
			TabPagevHost();
		}
		else if (TabPages.SelectedTab.Name == "tabvHost")
		{
			tabvHost.Show();
			TabPagevHost();
		}
		else if (TabPages.SelectedTab.Name == "tabvHBA")
		{
			tabvHBA.Show();
			TabPagevHBA();
		}
		else if (TabPages.SelectedTab.Name == "tabvNIC")
		{
			tabvNIC.Show();
			TabPagevNIC();
		}
		else if (TabPages.SelectedTab.Name == "tabvSwitch")
		{
			tabvSwitch.Show();
			TabPagevSwitch();
		}
		else if (TabPages.SelectedTab.Name == "tabdvSwitch")
		{
			tabdvSwitch.Show();
			TabPagedvSwitch();
		}
		else if (TabPages.SelectedTab.Name == "tabvPort")
		{
			tabvPort.Show();
			TabPagevPort();
		}
		else if (TabPages.SelectedTab.Name == "tabdvPort")
		{
			tabdvPort.Show();
			TabPagedvPort();
		}
		else if (TabPages.SelectedTab.Name == "tabvSC_VMK")
		{
			tabvSC_VMK.Show();
			TabPagevSC_VMK();
		}
		else if (TabPages.SelectedTab.Name == "tabvDatastore")
		{
			tabvDatastore.Show();
			TabPagevDatastore();
		}
		else if (TabPages.SelectedTab.Name == "tabvMultiPath")
		{
			tabvMultiPath.Show();
			TabPagevMultiPath();
		}
		else if (TabPages.SelectedTab.Name == "tabvLicense")
		{
			tabvLicense.Show();
			TabPagevLicense();
		}
		else if (TabPages.SelectedTab.Name == "tabvFileInfo")
		{
			tabvFileInfo.Show();
			TabPagevFileInfo();
		}
		else if (TabPages.SelectedTab.Name == "tabvHealth")
		{
			tabvHealth.Show();
			TabPagevHealth();
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void TabPagevInfo()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("ShowTab", "vInfo");
		DateTime utcNow = DateTime.UtcNow;
		if (!TabPagevInfo_Refresh_data)
		{
			StatusGridCounter.Text = dataGridvInfo.RowCount + " rows";
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
			return;
		}
		TabPagevInfo_Refresh_data = false;
		Cursor = Cursors.WaitCursor;
		Application.DoEvents();
		dataGridvInfo.Rows.Clear();
		int num = 0;
		StatusFilter.Visible = FilterClass.ShowFilterIcon();
		if (!PreferencesClass.ExcludeAnnotations)
		{
			SortedList<string, string> customFieldHeaders = VISDK.GetCustomFieldHeaders("VirtualMachine");
			int num2 = dataGridvInfo.Columns["vInfoNotes"].Index;
			try
			{
				foreach (KeyValuePair<string, string> item in customFieldHeaders)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
					dataGridViewTextBoxColumn.Name = "CustomField" + item.Value;
					dataGridViewTextBoxColumn.HeaderText = item.Key;
					dataGridViewTextBoxColumn.Width = 200;
					if (!dataGridvInfo.Columns.Contains("CustomField" + item.Value))
					{
						dataGridvInfo.Columns.Insert(num2 + 1, dataGridViewTextBoxColumn);
					}
					num2++;
				}
			}
			catch (Exception ex)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding CustomFieldHeaders " + ex.Message);
				}
			}
		}
		if (!PreferencesClass.ExcludeTags)
		{
			try
			{
				List<Hashtable> tagColumnNames = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "VirtualMachine");
				int num3 = dataGridvInfo.Columns["vInfoDatacenter"].Index - 1;
				foreach (Hashtable item2 in tagColumnNames)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
					if (!dataGridvInfo.Columns.Contains("vInfo_tags_" + item2["Name"].ToString()))
					{
						dataGridViewTextBoxColumn2.Name = "vInfo_tags_" + item2["Name"].ToString();
						dataGridViewTextBoxColumn2.HeaderText = item2["Name"].ToString();
						dataGridViewTextBoxColumn2.ToolTipText = item2["Desc"].ToString();
						dataGridViewTextBoxColumn2.Width = 200;
						dataGridvInfo.Columns.Insert(num3 + 1, dataGridViewTextBoxColumn2);
						num3++;
					}
				}
			}
			catch (Exception ex2)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding tags " + ex2.Message);
				}
			}
		}
		for (int i = 0; i < 1; i++)
		{
			for (int j = 0; j < VISDK.InventoryCounter; j++)
			{
				if (FilterClass.ShowFilterIcon() && (!FilterClass.DisplayVirtualMachine(j) || !FilterClass.DisplayPowerstate(j) || !FilterClass.DisplayDataCenter(VISDK.GetDatacenterName(VISDK.GetSummaryRuntimeHost(j))) || !FilterClass.DisplayCluster(VISDK.GetClusterName(VISDK.GetSummaryRuntimeHost(j))) || !FilterClass.DisplayHost(VISDK.GetHostName(VISDK.GetSummaryRuntimeHost(j))) || !FilterClass.DisplayVMName(j) || !FilterClass.DisplayNotes(j)))
				{
					continue;
				}
				dataGridvInfo.Rows.Add();
				string vmInventoryProperty = VISDK.GetVmInventoryProperty(j, "summary.config.name");
				if (logger.IsInfoEnabled)
				{
					logger.Info(currentMethod.Name + " Add Row for VM: " + vmInventoryProperty);
				}
				dataGridvInfo.Rows[num].Cells["vInfoVMName"].Value = vmInventoryProperty;
				dataGridvInfo.Rows[num].Cells["vInfoPowerState"].Value = VISDK.GetVmInventoryProperty(j, "summary.runtime.powerState");
				dataGridvInfo.Rows[num].Cells["vInfoTemplate"].Value = VISDK.GetVmInventoryProperty(j, "summary.config.template");
				dataGridvInfo.Rows[num].Cells["vInfoConfigStatus"].Value = VISDK.GetVmInventoryProperty(j, "configStatus");
				dataGridvInfo.Rows[num].Cells["vInfoGuestHostName"].Value = VISDK.GetVmInventoryProperty(j, "guest.hostName");
				dataGridvInfo.Rows[num].Cells["vInfoGuestState"].Value = VISDK.GetVmInventoryProperty(j, "guest.guestState");
				dataGridvInfo.Rows[num].Cells["vInfoHeartbeat"].Value = VISDK.GetVmInventoryProperty(j, "summary.quickStats.guestHeartbeatStatus");
				dataGridvInfo.Rows[num].Cells["vInfoConsolidationNeeded"].Value = VISDK.GetVmInventoryProperty(j, "runtime.consolidationNeeded");
				dataGridvInfo.Rows[num].Cells["vInfoFirmware"].Value = VISDK.GetVmInventoryProperty(j, "config.firmware");
				dataGridvInfo.Rows[num].Cells["vInfoPrimaryIPAddress"].Value = VISDK.GetVmInventoryProperty(j, "guest.ipAddress");
				try
				{
					DateTime minValue = DateTime.MinValue;
					string vmInventoryProperty2 = VISDK.GetVmInventoryProperty(j, "config.changeVersion");
					minValue = Convert.ToDateTime(vmInventoryProperty2);
					if (vmInventoryProperty2 == null)
					{
						dataGridvInfo.Rows[num].Cells["vInfoChangeVersion"].Value = null;
					}
					else
					{
						dataGridvInfo.Rows[num].Cells["vInfoChangeVersion"].Value = minValue.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
					}
				}
				catch
				{
				}
				try
				{
					DateTime minValue2 = DateTime.MinValue;
					string vmInventoryProperty3 = VISDK.GetVmInventoryProperty(j, "config.createDate");
					minValue2 = Convert.ToDateTime(vmInventoryProperty3);
					if (vmInventoryProperty3 == null)
					{
						dataGridvInfo.Rows[num].Cells["vInfoCreateDate"].Value = null;
					}
					else
					{
						dataGridvInfo.Rows[num].Cells["vInfoCreateDate"].Value = minValue2.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
					}
				}
				catch
				{
				}
				dataGridvInfo.Rows[num].Cells["vInfoConnectionState"].Value = VISDK.GetVmInventoryProperty(j, "summary.runtime.connectionState");
				if (VISDK.GetVmInventoryProperty(j, "summary.runtime.powerState") == "suspended")
				{
					try
					{
						DateTime minValue3 = DateTime.MinValue;
						minValue3 = Convert.ToDateTime(VISDK.GetVmInventoryProperty(j, "summary.runtime.suspendTime"));
						dataGridvInfo.Rows[num].Cells["vInfoSuspendTime"].Value = minValue3.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
					}
					catch
					{
						dataGridvInfo.Rows[num].Cells["vInfoSuspendTime"].Value = null;
					}
				}
				if (VISDK.GetVmInventoryProperty(j, "summary.runtime.powerState") == "poweredOn")
				{
					try
					{
						DateTime minValue4 = DateTime.MinValue;
						string vmInventoryProperty4 = VISDK.GetVmInventoryProperty(j, "summary.runtime.bootTime");
						dataGridvInfo.Rows[num].Cells["vInfoBootTime"].Value = null;
						if (vmInventoryProperty4 != null)
						{
							minValue4 = Convert.ToDateTime(vmInventoryProperty4);
							dataGridvInfo.Rows[num].Cells["vInfoBootTime"].Value = minValue4.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
						}
					}
					catch
					{
						dataGridvInfo.Rows[num].Cells["vInfoBootTime"].Value = null;
					}
				}
				dataGridvInfo.Rows[num].Cells["vInfoCPUs"].Value = VISDK.GetInventoryPropertyInt(j, "summary.config.numCpu");
				dataGridvInfo.Rows[num].Cells["vInfoMemory"].Value = VISDK.GetInventoryPropertyInt(j, "summary.config.memorySizeMB");
				bool sRMPlaceholderBool = VISDK.GetSRMPlaceholderBool(j);
				dataGridvInfo.Rows[num].Cells["vInfoSRMPlaceholder"].Value = sRMPlaceholderBool.ToString();
				try
				{
					LatencySensitivity latencySensitivity = VISDK.GetLatencySensitivity(j);
					dataGridvInfo.Rows[num].Cells["vInfoLatencySensitivity"].Value = latencySensitivity.level.ToString();
				}
				catch
				{
					dataGridvInfo.Rows[num].Cells["vInfoLatencySensitivity"].Value = null;
				}
				try
				{
					dataGridvInfo.Rows[num].Cells["vInfoRebootPowerOff"].Value = VISDK.GetVmInventoryProperty(j, "config.rebootPowerOff");
				}
				catch
				{
					dataGridvInfo.Rows[num].Cells["vInfoRebootPowerOff"].Value = null;
				}
				VirtualMachineBootOptions bootOptions = VISDK.GetBootOptions(j);
				try
				{
					dataGridvInfo.Rows[num].Cells["vInfoBootDelay"].Value = bootOptions.bootDelay;
				}
				catch
				{
					dataGridvInfo.Rows[num].Cells["vInfoBootDelay"].Value = null;
				}
				try
				{
					dataGridvInfo.Rows[num].Cells["vInfoBootRetryDelay"].Value = bootOptions.bootRetryDelay;
				}
				catch
				{
					dataGridvInfo.Rows[num].Cells["vInfoBootRetryDelay"].Value = null;
				}
				try
				{
					dataGridvInfo.Rows[num].Cells["vInfoBootRetryEnabled"].Value = bootOptions.bootRetryEnabled;
				}
				catch
				{
					dataGridvInfo.Rows[num].Cells["vInfoBootRetryEnabled"].Value = null;
				}
				try
				{
					dataGridvInfo.Rows[num].Cells["vInfoBootBios"].Value = bootOptions.enterBIOSSetup;
				}
				catch
				{
					dataGridvInfo.Rows[num].Cells["vInfoBootBios"].Value = null;
				}
				try
				{
					dataGridvInfo.Rows[num].Cells["vInfoEfiSecureBoot"].Value = bootOptions.efiSecureBootEnabled;
				}
				catch
				{
					dataGridvInfo.Rows[num].Cells["vInfoEfiSecureBoot"].Value = null;
				}
				ScheduledHardwareUpgradeInfo scheduledHardwareUpgradeInfo = VISDK.GetScheduledHardwareUpgradeInfo(j);
				try
				{
					dataGridvInfo.Rows[num].Cells["vInfoHWUpgradeStatus"].Value = scheduledHardwareUpgradeInfo.scheduledHardwareUpgradeStatus;
				}
				catch
				{
					dataGridvInfo.Rows[num].Cells["vInfoHWUpgradeStatus"].Value = null;
				}
				try
				{
					dataGridvInfo.Rows[num].Cells["vInfoHWUpgradePolicy"].Value = scheduledHardwareUpgradeInfo.upgradePolicy;
				}
				catch
				{
					dataGridvInfo.Rows[num].Cells["vInfoHWUpgradePolicy"].Value = null;
				}
				try
				{
					dataGridvInfo.Rows[num].Cells["vInfoHWTarget"].Value = scheduledHardwareUpgradeInfo.versionKey;
				}
				catch
				{
					dataGridvInfo.Rows[num].Cells["vInfoHWTarget"].Value = null;
				}
				VirtualDevice[] deviceInfo = VISDK.GetDeviceInfo(j);
				int num4 = 0;
				string text = null;
				int num5 = 0;
				while (deviceInfo != null && num5 < deviceInfo.Length)
				{
					if (deviceInfo[num5] is VirtualEthernetCard)
					{
						VirtualEthernetCard virtualEthernetCard = (VirtualEthernetCard)deviceInfo[num5];
						num4++;
						try
						{
							text = ((VirtualEthernetCardNetworkBackingInfo)virtualEthernetCard.backing).deviceName;
						}
						catch
						{
							text = virtualEthernetCard.deviceInfo.summary;
						}
						try
						{
							text = VISDK.GetDSPortNameV2(((VirtualEthernetCardDistributedVirtualPortBackingInfo)virtualEthernetCard.backing).port.portgroupKey);
						}
						catch
						{
						}
						if (num4 == 1)
						{
							dataGridvInfo.Rows[num].Cells["vInfoNetwork1"].Value = text;
						}
						if (num4 == 2)
						{
							dataGridvInfo.Rows[num].Cells["vInfoNetwork2"].Value = text;
						}
						if (num4 == 3)
						{
							dataGridvInfo.Rows[num].Cells["vInfoNetwork3"].Value = text;
						}
						if (num4 == 4)
						{
							dataGridvInfo.Rows[num].Cells["vInfoNetwork4"].Value = text;
						}
						if (num4 == 5)
						{
							dataGridvInfo.Rows[num].Cells["vInfoNetwork5"].Value = text;
						}
						if (num4 == 6)
						{
							dataGridvInfo.Rows[num].Cells["vInfoNetwork6"].Value = text;
						}
						if (num4 == 7)
						{
							dataGridvInfo.Rows[num].Cells["vInfoNetwork7"].Value = text;
						}
						if (num4 == 8)
						{
							dataGridvInfo.Rows[num].Cells["vInfoNetwork8"].Value = text;
						}
					}
					num5++;
				}
				dataGridvInfo.Rows[num].Cells["vInfoNICs"].Value = num4;
				long num6 = 0L;
				try
				{
					int num7 = 0;
					while (deviceInfo != null && num7 < deviceInfo.Length)
					{
						if (deviceInfo[num7] is VirtualDisk)
						{
							VirtualDisk virtualDisk = (VirtualDisk)deviceInfo[num7];
							num6 += virtualDisk.capacityInKB / 1024;
						}
						num7++;
					}
				}
				catch
				{
				}
				dataGridvInfo.Rows[num].Cells["vInfoTotalDiskCapacityMiB"].Value = num6;
				try
				{
					dataGridvInfo.Rows[num].Cells["vInfoProvisioned"].Value = (VISDK.GetInventoryPropertyInt(j, "summary.storage.committed") + VISDK.GetInventoryPropertyInt(j, "summary.storage.uncommitted")) / 1048576;
				}
				catch
				{
					dataGridvInfo.Rows[num].Cells["vInfoProvisioned"].Value = null;
				}
				try
				{
					dataGridvInfo.Rows[num].Cells["vInfoInUse"].Value = VISDK.GetInventoryPropertyInt(j, "summary.storage.committed") / 1048576;
				}
				catch
				{
					dataGridvInfo.Rows[num].Cells["vInfoInUse"].Value = null;
				}
				try
				{
					dataGridvInfo.Rows[num].Cells["vInfoUnshared"].Value = VISDK.GetInventoryPropertyInt(j, "summary.storage.unshared") / 1048576;
				}
				catch
				{
					dataGridvInfo.Rows[num].Cells["vInfoUnshared"].Value = null;
				}
				dataGridvInfo.Rows[num].Cells["vInfoInstallBootRequired"].Value = VISDK.GetVmInventoryProperty(j, "summary.config.installBootRequired");
				dataGridvInfo.Rows[num].Cells["vInfoNumVirtualDisks"].Value = VISDK.GetInventoryPropertyInt(j, "summary.config.numVirtualDisks");
				dataGridvInfo.Rows[num].Cells["vInfoCBT"].Value = VISDK.GetOptionValueVM(j, "ctkEnabled");
				dataGridvInfo.Rows[num].Cells["vInfoEnableUUID"].Value = VISDK.GetOptionValueVM(j, "disk.EnableUUID");
				dataGridvInfo.Rows[num].Cells["vInfoFaultToleranceState"].Value = VISDK.GetVmInventoryProperty(j, "summary.runtime.faultToleranceState");
				dataGridvInfo.Rows[num].Cells["vInfoFTLatencyStatus"].Value = VISDK.GetVmInventoryProperty(j, "summary.quickStats.ftLatencyStatus");
				dataGridvInfo.Rows[num].Cells["vInfoFTLogBandwidth"].Value = VISDK.GetVmInventoryProperty(j, "summary.quickStats.ftLogBandwidth");
				dataGridvInfo.Rows[num].Cells["vInfoFTSecondaryLatency"].Value = VISDK.GetVmInventoryProperty(j, "summary.quickStats.ftSecondaryLatency");
				try
				{
					FaultToleranceConfigInfo fTInfo = VISDK.GetFTInfo(j);
					dataGridvInfo.Rows[num].Cells["vInfoFTInfoRole"].Value = fTInfo.role;
				}
				catch
				{
					dataGridvInfo.Rows[num].Cells["vInfoFTInfoRole"].Value = null;
				}
				string text2 = VISDK.GetVmInventoryProperty(j, "config.version");
				if (text2 != null)
				{
					text2 = text2.Replace("vmx-0", "");
					text2 = text2.Replace("vmx-", "");
					text2 = text2.Replace("vmx", "");
				}
				try
				{
					dataGridvInfo.Rows[num].Cells["vInfoVersion"].Value = Convert.ToInt16(text2);
				}
				catch
				{
					dataGridvInfo.Rows[num].Cells["vInfoVersion"].Value = text2;
				}
				VirtualMachineRuntimeInfoDasProtectionState dASProtectionState = VISDK.GetDASProtectionState(j);
				if (dASProtectionState != null)
				{
					dataGridvInfo.Rows[num].Cells["vInfoDASProtection"].Value = dASProtectionState.dasProtected;
				}
				try
				{
					VirtualMachineFileInfo virtualMachineFileInfo = VISDK.GetVirtualMachineFileInfo(j);
					if (PreferencesClass.GetFriendlyNames)
					{
						dataGridvInfo.Rows[num].Cells["vInfoLogDirectory"].Value = VISDK.GetFriendlyFolder(virtualMachineFileInfo.logDirectory);
						dataGridvInfo.Rows[num].Cells["vInfoSnapshotDirectory"].Value = VISDK.GetFriendlyFolder(virtualMachineFileInfo.snapshotDirectory);
						dataGridvInfo.Rows[num].Cells["vInfoSuspendDirectory"].Value = VISDK.GetFriendlyFolder(virtualMachineFileInfo.suspendDirectory);
						dataGridvInfo.Rows[num].Cells["vInfoPath"].Value = VISDK.GetFriendlyPath(VISDK.GetVmInventoryProperty(j, "summary.config.vmPathName"));
					}
					else
					{
						dataGridvInfo.Rows[num].Cells["vInfoLogDirectory"].Value = virtualMachineFileInfo.logDirectory;
						dataGridvInfo.Rows[num].Cells["vInfoSnapshotDirectory"].Value = virtualMachineFileInfo.snapshotDirectory;
						dataGridvInfo.Rows[num].Cells["vInfoSuspendDirectory"].Value = virtualMachineFileInfo.suspendDirectory;
						dataGridvInfo.Rows[num].Cells["vInfoPath"].Value = VISDK.GetVmInventoryProperty(j, "summary.config.vmPathName");
					}
				}
				catch
				{
				}
				dataGridvInfo.Rows[num].Cells["vInfoOS"].Value = VISDK.GetVmInventoryProperty(j, "summary.config.guestFullName");
				dataGridvInfo.Rows[num].Cells["vInfoOSTools"].Value = VISDK.GetVmInventoryProperty(j, "guest.guestFullName");
				dataGridvInfo.Rows[num].Cells["vInfoHost"].Value = VISDK.GetHostName(VISDK.GetSummaryRuntimeHost(j));
				dataGridvInfo.Rows[num].Cells["vInfoDatacenter"].Value = VISDK.GetDatacenterName(VISDK.GetSummaryRuntimeHost(j));
				string clusterName = VISDK.GetClusterName(VISDK.GetSummaryRuntimeHost(j));
				dataGridvInfo.Rows[num].Cells["vInfoCluster"].Value = clusterName;
				if (!PreferencesClass.ExcludeAnnotations)
				{
					dataGridvInfo.Rows[num].Cells["vInfoNotes"].Value = VISDK.GetSummaryConfigAnnotation(j);
					CustomFieldValue[] customFieldValueForVM = VISDK.GetCustomFieldValueForVM(j);
					for (int num8 = customFieldValueForVM.Length; num8 > 0; num8--)
					{
						CustomFieldStringValue customFieldStringValue = (CustomFieldStringValue)customFieldValueForVM.GetValue(num8 - 1);
						try
						{
							dataGridvInfo.Rows[num].Cells["CustomField" + customFieldStringValue.key].Value = customFieldStringValue.value;
						}
						catch (Exception ex3)
						{
							if (logger.IsDebugEnabled)
							{
								logger.Debug(currentMethod.Name + " Handled Exception: during setting value to custom field " + ex3.Message);
							}
						}
					}
				}
				if (!PreferencesClass.ExcludeTags)
				{
					try
					{
						List<Hashtable> tagColumnNames2 = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "VirtualMachine");
						string summaryVM = VISDK.GetSummaryVM(j);
						foreach (Hashtable item3 in tagColumnNames2)
						{
							string tagColumnValues = VISDK.GetTagColumnValues(ref VISDK.cisAssociatedTag, "VirtualMachine", item3["Name"].ToString(), summaryVM);
							dataGridvInfo.Rows[num].Cells["vInfo_tags_" + item3["Name"].ToString()].Value = tagColumnValues;
						}
					}
					catch (Exception ex4)
					{
						if (logger.IsDebugEnabled)
						{
							logger.Debug(currentMethod.Name + " Handled Exception: during setting tag value " + ex4.Message);
						}
					}
				}
				string vMResourcePool = VISDK.GetVMResourcePool(j);
				dataGridvInfo.Rows[num].Cells["vInfoResourcePool"].Value = vMResourcePool;
				dataGridvInfo.Rows[num].Cells["vInfoFolderId"].Value = VISDK.GetVMFolderID(j);
				if (PreferencesClass.vInfoShowFolder || CommandToExecute != null)
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(currentMethod.Name + " Call GetVMFolder for VM " + vmInventoryProperty);
					}
					string vMFolder = VISDK.GetVMFolder(j);
					dataGridvInfo.Rows[num].Cells["vInfoFolder"].Value = vMFolder;
				}
				if (PreferencesClass.vInfoShowAppName || CommandToExecute != null)
				{
					try
					{
						dataGridvInfo.Rows[num].Cells["vInfovApp"].Value = VISDK.GetvAppInfo(j);
					}
					catch
					{
					}
				}
				dataGridvInfo.Rows[num].Cells["vInfoUUID"].Value = VISDK.GetVmInventoryProperty(j, "config.instanceUuid");
				dataGridvInfo.Rows[num].Cells["vInfoSMBIOSUUID"].Value = VISDK.GetVmInventoryProperty(j, "config.uuid");
				dataGridvInfo.Rows[num].Cells["vInfoObjectID"].Value = VISDK.GetSummaryVM(j);
				dataGridvInfo.Rows[num].Cells["vInfoVISDKServerType"].Value = VISDK._sic.about.fullName;
				dataGridvInfo.Rows[num].Cells["vInfoVISDKAPI"].Value = VISDK._sic.about.apiVersion;
				dataGridvInfo.Rows[num].Cells["vInfoVISDKServer"].Value = VISDK.Server;
				dataGridvInfo.Rows[num].Cells["vInfoInstanceUUID"].Value = InstanceUUID;
				try
				{
					dataGridvInfo.Rows[num].Cells["vInfominRequiredEVCModeKey"].Value = VISDK.GetVmInventoryProperty(j, "runtime.minRequiredEVCModeKey");
				}
				catch
				{
				}
				ClusterDasVmSettings clusterDasVmSettings = null;
				try
				{
					clusterDasVmSettings = VISDK.GetClusterDasDefaultSettings(clusterName, VISDK.GetSummaryVM(j));
				}
				catch
				{
				}
				ClusterDasVmSettings clusterDasVmSettings2 = null;
				try
				{
					clusterDasVmSettings2 = VISDK.GetClusterDasVmSettings(clusterName, VISDK.GetSummaryVM(j));
				}
				catch
				{
				}
				try
				{
					dataGridvInfo.Rows[num].Cells["vInfoHARestartPriority"].Value = clusterDasVmSettings.restartPriority;
					if (clusterDasVmSettings2.restartPriority != "clusterRestartPriority")
					{
						dataGridvInfo.Rows[num].Cells["vInfoHARestartPriority"].Value = clusterDasVmSettings2.restartPriority;
					}
				}
				catch
				{
				}
				try
				{
					dataGridvInfo.Rows[num].Cells["vInfoHAIsolationResponse"].Value = clusterDasVmSettings.isolationResponse;
					if (clusterDasVmSettings2.isolationResponse != "clusterIsolationResponse")
					{
						dataGridvInfo.Rows[num].Cells["vInfoHAIsolationResponse"].Value = clusterDasVmSettings2.isolationResponse;
					}
				}
				catch
				{
				}
				try
				{
					dataGridvInfo.Rows[num].Cells["vInfoVMmonitoring"].Value = clusterDasVmSettings.vmToolsMonitoringSettings.vmMonitoring;
					if (clusterDasVmSettings2.vmToolsMonitoringSettings.vmMonitoring.Length > 0)
					{
						dataGridvInfo.Rows[num].Cells["vInfoVMmonitoring"].Value = clusterDasVmSettings2.vmToolsMonitoringSettings.vmMonitoring;
					}
				}
				catch
				{
				}
				ClusterRuleInfo[] array = null;
				try
				{
					array = VISDK.GetClusterRuleInfo(clusterName, VISDK.GetSummaryVM(j));
				}
				catch
				{
				}
				string text3 = "";
				string text4 = "";
				if (array != null)
				{
					ClusterRuleInfo[] array2 = array;
					foreach (ClusterRuleInfo clusterRuleInfo in array2)
					{
						ManagedObjectReference[] vm;
						if (clusterRuleInfo is ClusterAffinityRuleSpec)
						{
							ClusterAffinityRuleSpec clusterAffinityRuleSpec = (ClusterAffinityRuleSpec)clusterRuleInfo;
							vm = clusterAffinityRuleSpec.vm;
							for (int l = 0; l < vm.Length; l++)
							{
								if (vm[l].Value == VISDK.GetSummaryVM(j))
								{
									if (text3.Length == 0)
									{
										text3 = "Affinity";
										text4 = clusterAffinityRuleSpec.name;
									}
									else
									{
										text3 += ", Affinity";
										text4 = text4 + ", " + clusterAffinityRuleSpec.name;
									}
								}
							}
						}
						if (!(clusterRuleInfo is ClusterAntiAffinityRuleSpec))
						{
							continue;
						}
						ClusterAntiAffinityRuleSpec clusterAntiAffinityRuleSpec = (ClusterAntiAffinityRuleSpec)clusterRuleInfo;
						vm = clusterAntiAffinityRuleSpec.vm;
						for (int l = 0; l < vm.Length; l++)
						{
							if (vm[l].Value == VISDK.GetSummaryVM(j))
							{
								if (text3.Length == 0)
								{
									text3 = "Anti Affinity";
									text4 = clusterAntiAffinityRuleSpec.name;
								}
								else
								{
									text3 += ", Anti Affinity";
									text4 = text4 + ", " + clusterAntiAffinityRuleSpec.name;
								}
							}
						}
					}
				}
				dataGridvInfo.Rows[num].Cells["vInfoClusterRule"].Value = text3;
				dataGridvInfo.Rows[num].Cells["vInfoClusterRuleName"].Value = text4;
				try
				{
					VirtualDevice[] deviceInfo2 = VISDK.GetDeviceInfo(j);
					foreach (VirtualDevice virtualDevice in deviceInfo2)
					{
						if (virtualDevice is VirtualMachineVideoCard)
						{
							VirtualMachineVideoCard virtualMachineVideoCard = (VirtualMachineVideoCard)virtualDevice;
							try
							{
								dataGridvInfo.Rows[num].Cells["vInfoNumDisplays"].Value = virtualMachineVideoCard.numDisplays;
							}
							catch
							{
								dataGridvInfo.Rows[num].Cells["vInfoNumDisplays"].Value = null;
							}
							try
							{
								dataGridvInfo.Rows[num].Cells["vInfoVideoRamKiB"].Value = virtualMachineVideoCard.videoRamSizeInKB;
							}
							catch
							{
								dataGridvInfo.Rows[num].Cells["vInfoVideoRamKiB"].Value = null;
							}
							break;
						}
					}
				}
				catch (Exception ex5)
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(currentMethod.Name + " catch exception: foreach VirtualDevice " + ex5.Message);
					}
				}
				vmInventoryProperty = VISDK.GetVmInventoryProperty(j, "summary.config.name");
				string text5 = VISDK.GetVmInventoryProperty(j, "guest.customizationInfo.customizationStatus")?.Replace("TOOLSDEPLOYPKG_", "");
				string vmInventoryProperty5 = VISDK.GetVmInventoryProperty(j, "guest.customizationInfo.errorMsg");
				dataGridvInfo.Rows[num].Cells["vInfoCustomizationInfo"].Value = (string.IsNullOrWhiteSpace(vmInventoryProperty5) ? text5 : (text5 + ": " + vmInventoryProperty5));
				dataGridvInfo.Rows[num].Cells["vInfoFixedPassthruHotPlugEnabled"].Value = VISDK.GetVmInventoryProperty(j, "config.fixedPassthruHotPlugEnabled");
				dataGridvInfo.Rows[num].Cells["vInfoSuspendedToMemory"].Value = VISDK.GetVmInventoryProperty(j, "runtime.suspendedToMemory");
				dataGridvInfo.Rows[num].Cells["vInfoGuestDetailedData"].Value = VISDK.GetVmInventoryProperty(j, "guest.guestDetailedData");
				dataGridvInfo.Rows[num].Cells["vInfoOpNotificationTimeout"].Value = VISDK.GetVmInventoryProperty(j, "runtime.opNotificationTimeout");
				dataGridvInfo.Rows[num].Cells["vInfoSuspendInterval"].Value = VISDK.GetVmInventoryProperty(j, "runtime.suspendInterval");
				dataGridvInfo.Rows[num].Cells["vInfoVmFailoverInProgress"].Value = VISDK.GetVmInventoryProperty(j, "runtime.vmFailoverInProgress");
				dataGridvInfo.Rows[num].Cells["vInfoActiveMemory"].Value = VISDK.GetVmInventoryProperty(j, "summary.quickStats.activeMemory");
				int num9 = Convert.ToInt32(VISDK.GetVmInventoryProperty(j, "summary.quickStats.overallCpuReadiness") ?? "-1");
				dataGridvInfo.Rows[num].Cells["vInfoOverallCpuReadiness"].Value = ((num9 < 0) ? null : $"{num9}%");
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " END of loop for VM: " + vmInventoryProperty);
				}
				num++;
			}
		}
		dataGridvInfo.AutoResizeColumns();
		string columnName = "vInfoVMname";
		if (dataGridvInfo.SortedColumn != null)
		{
			columnName = dataGridvInfo.SortedColumn.Name;
		}
		dataGridvInfo.Sort(dataGridvInfo.Columns[columnName], ListSortDirection.Ascending);
		StatusGridCounter.Text = num + " rows";
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Number of rows: " + num);
		}
		Cursor = Cursors.Default;
		Telemetry.AddEvent("RenderTab", "vInfo", new Dictionary<string, string>
		{
			{
				"RenderTime",
				DateTime.UtcNow.Subtract(utcNow).ToString()
			},
			{
				"RowCount",
				num.ToString()
			}
		});
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void TabPagevCPU()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("ShowTab", "vCPU");
		DateTime utcNow = DateTime.UtcNow;
		if (!TabPagevCPU_Refresh_data)
		{
			StatusGridCounter.Text = dataGridvCPU.RowCount + " rows";
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
			return;
		}
		TabPagevCPU_Refresh_data = false;
		Application.DoEvents();
		Cursor = Cursors.WaitCursor;
		dataGridvCPU.Rows.Clear();
		int num = 0;
		if (!PreferencesClass.ExcludeAnnotations)
		{
			SortedList<string, string> customFieldHeaders = VISDK.GetCustomFieldHeaders("VirtualMachine");
			int num2 = dataGridvCPU.Columns["vCPUNotes"].Index;
			try
			{
				foreach (KeyValuePair<string, string> item in customFieldHeaders)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
					dataGridViewTextBoxColumn.Name = "CustomField" + item.Value;
					dataGridViewTextBoxColumn.HeaderText = item.Key;
					dataGridViewTextBoxColumn.Width = 200;
					if (!dataGridvCPU.Columns.Contains("CustomField" + item.Value))
					{
						dataGridvCPU.Columns.Insert(num2 + 1, dataGridViewTextBoxColumn);
					}
					num2++;
				}
			}
			catch (Exception ex)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding CustomFieldHeaders " + ex.Message);
				}
			}
		}
		if (!PreferencesClass.ExcludeTags)
		{
			try
			{
				List<Hashtable> tagColumnNames = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "VirtualMachine");
				int num3 = dataGridvCPU.Columns["vCPUDatacenter"].Index - 1;
				foreach (Hashtable item2 in tagColumnNames)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
					if (!dataGridvCPU.Columns.Contains("vCPU_tags_" + item2["Name"].ToString()))
					{
						dataGridViewTextBoxColumn2.Name = "vCPU_tags_" + item2["Name"].ToString();
						dataGridViewTextBoxColumn2.HeaderText = item2["Name"].ToString();
						dataGridViewTextBoxColumn2.ToolTipText = item2["Desc"].ToString();
						dataGridViewTextBoxColumn2.Width = 200;
						dataGridvCPU.Columns.Insert(num3 + 1, dataGridViewTextBoxColumn2);
						num3++;
					}
				}
			}
			catch (Exception ex2)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding tags " + ex2.Message);
				}
			}
		}
		StatusFilter.Visible = FilterClass.ShowFilterIcon();
		for (int i = 0; i < VISDK.InventoryCounter; i++)
		{
			if (FilterClass.ShowFilterIcon() && (!FilterClass.DisplayVirtualMachine(i) || !FilterClass.DisplayPowerstate(i) || !FilterClass.DisplayDataCenter(VISDK.GetDatacenterName(VISDK.GetSummaryRuntimeHost(i))) || !FilterClass.DisplayCluster(VISDK.GetClusterName(VISDK.GetSummaryRuntimeHost(i))) || !FilterClass.DisplayHost(VISDK.GetHostName(VISDK.GetSummaryRuntimeHost(i))) || !FilterClass.DisplayVMName(i) || !FilterClass.DisplayNotes(i)))
			{
				continue;
			}
			dataGridvCPU.Rows.Add();
			string vmInventoryProperty = VISDK.GetVmInventoryProperty(i, "summary.config.name");
			if (logger.IsInfoEnabled)
			{
				logger.Info(currentMethod.Name + " Add Row for CPU: " + vmInventoryProperty);
			}
			dataGridvCPU.Rows[num].Cells["vCPUVMName"].Value = vmInventoryProperty;
			dataGridvCPU.Rows[num].Cells["vCPUPowerState"].Value = VISDK.GetVmInventoryProperty(i, "summary.runtime.powerState");
			dataGridvCPU.Rows[num].Cells["vCPUTemplate"].Value = VISDK.GetVmInventoryProperty(i, "summary.config.template");
			bool sRMPlaceholderBool = VISDK.GetSRMPlaceholderBool(i);
			dataGridvCPU.Rows[num].Cells["vCPUSRMPlaceholder"].Value = sRMPlaceholderBool.ToString();
			dataGridvCPU.Rows[num].Cells["vCPUCPUs"].Value = VISDK.GetInventoryPropertyInt(i, "summary.config.numCpu");
			string optionValueVM = VISDK.GetOptionValueVM(i, "cpuid.coresPerSocket");
			int num4 = 1;
			try
			{
				num4 = Convert.ToInt16(optionValueVM);
			}
			catch
			{
			}
			if (num4 == 0)
			{
				num4 = 1;
			}
			dataGridvCPU.Rows[num].Cells["vCPUCoresPerSocket"].Value = num4;
			try
			{
				int num5 = (int)VISDK.GetInventoryPropertyInt(i, "summary.config.numCpu") / num4;
				dataGridvCPU.Rows[num].Cells["vCPUSockets"].Value = num5;
			}
			catch
			{
			}
			dataGridvCPU.Rows[num].Cells["vCPUMaxCpuUsage"].Value = VISDK.GetInventoryPropertyInt(i, "summary.runtime.maxCpuUsage");
			dataGridvCPU.Rows[num].Cells["vCPUOverallCpuUsage"].Value = VISDK.GetInventoryPropertyInt(i, "summary.quickStats.overallCpuUsage");
			try
			{
				SharesInfo cPUShareInfo = VISDK.GetCPUShareInfo(i);
				dataGridvCPU.Rows[num].Cells["vCPUSharesLevel"].Value = cPUShareInfo.level.ToString();
				dataGridvCPU.Rows[num].Cells["vCPUShares"].Value = cPUShareInfo.shares;
			}
			catch
			{
			}
			try
			{
				VirtualMachineVirtualNumaInfo numaInfo = VISDK.GetNumaInfo(i);
				dataGridvCPU.Rows[num].Cells["vCPUNumaOnCpuHotaddExposed"].Value = numaInfo.vnumaOnCpuHotaddExposed.ToString();
			}
			catch
			{
				dataGridvCPU.Rows[num].Cells["vCPUNumaOnCpuHotaddExposed"].Value = false.ToString();
			}
			dataGridvCPU.Rows[num].Cells["vCPUReservation"].Value = VISDK.GetInventoryPropertyInt(i, "resourceConfig.cpuAllocation.reservation");
			dataGridvCPU.Rows[num].Cells["vCPUReservationLimit"].Value = VISDK.GetInventoryPropertyInt(i, "resourceConfig.cpuAllocation.limit");
			dataGridvCPU.Rows[num].Cells["vCPUStaticCpuEntitlement"].Value = VISDK.GetInventoryPropertyInt(i, "summary.quickStats.staticCpuEntitlement");
			dataGridvCPU.Rows[num].Cells["vCPUdistributedCpuEntitlement"].Value = VISDK.GetInventoryPropertyInt(i, "summary.quickStats.distributedCpuEntitlement");
			dataGridvCPU.Rows[num].Cells["vCPUOS"].Value = VISDK.GetVmInventoryProperty(i, "summary.config.guestFullName");
			dataGridvCPU.Rows[num].Cells["vCPUOSTools"].Value = VISDK.GetVmInventoryProperty(i, "guest.guestFullName");
			dataGridvCPU.Rows[num].Cells["vCPUHost"].Value = VISDK.GetHostName(VISDK.GetSummaryRuntimeHost(i));
			dataGridvCPU.Rows[num].Cells["vCPUDatacenter"].Value = VISDK.GetDatacenterName(VISDK.GetSummaryRuntimeHost(i));
			dataGridvCPU.Rows[num].Cells["vCPUCluster"].Value = VISDK.GetClusterName(VISDK.GetSummaryRuntimeHost(i));
			dataGridvCPU.Rows[num].Cells["vCPUHotAdd"].Value = VISDK.GetVmInventoryProperty(i, "config.cpuHotAddEnabled");
			dataGridvCPU.Rows[num].Cells["vCPUHotRemove"].Value = VISDK.GetVmInventoryProperty(i, "config.cpuHotRemoveEnabled");
			if (!PreferencesClass.ExcludeAnnotations)
			{
				dataGridvCPU.Rows[num].Cells["vCPUNotes"].Value = VISDK.GetSummaryConfigAnnotation(i);
				CustomFieldValue[] customFieldValueForVM = VISDK.GetCustomFieldValueForVM(i);
				for (int num6 = customFieldValueForVM.Length; num6 > 0; num6--)
				{
					CustomFieldStringValue customFieldStringValue = (CustomFieldStringValue)customFieldValueForVM.GetValue(num6 - 1);
					try
					{
						dataGridvCPU.Rows[num].Cells["CustomField" + customFieldStringValue.key].Value = customFieldStringValue.value;
					}
					catch (Exception ex3)
					{
						if (logger.IsDebugEnabled)
						{
							logger.Debug(currentMethod.Name + " Handled Exception: during setting value to custom field " + ex3.Message);
						}
					}
				}
			}
			if (!PreferencesClass.ExcludeTags)
			{
				try
				{
					List<Hashtable> tagColumnNames2 = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "VirtualMachine");
					string summaryVM = VISDK.GetSummaryVM(i);
					foreach (Hashtable item3 in tagColumnNames2)
					{
						string tagColumnValues = VISDK.GetTagColumnValues(ref VISDK.cisAssociatedTag, "VirtualMachine", item3["Name"].ToString(), summaryVM);
						dataGridvCPU.Rows[num].Cells["vCPU_tags_" + item3["Name"].ToString()].Value = tagColumnValues;
					}
				}
				catch (Exception ex4)
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(currentMethod.Name + " Handled Exception: during setting tag value " + ex4.Message);
					}
				}
			}
			if (PreferencesClass.vInfoShowFolder || CommandToExecute != null)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Call GetVMFolder for VM " + vmInventoryProperty);
				}
				string vMFolder = VISDK.GetVMFolder(i);
				dataGridvCPU.Rows[num].Cells["vCPUFolder"].Value = vMFolder;
			}
			dataGridvCPU.Rows[num].Cells["vCPUUUID"].Value = VISDK.GetVmInventoryProperty(i, "config.instanceUuid");
			dataGridvCPU.Rows[num].Cells["vCPUObjectID"].Value = VISDK.GetSummaryVM(i);
			dataGridvCPU.Rows[num].Cells["vCPUInstanceUUID"].Value = InstanceUUID;
			dataGridvCPU.Rows[num].Cells["vCPUVISDKServer"].Value = VISDK.Server;
			num++;
		}
		dataGridvCPU.AutoResizeColumns();
		string columnName = "vCPUVMName";
		if (dataGridvCPU.SortedColumn != null)
		{
			columnName = dataGridvCPU.SortedColumn.Name;
		}
		dataGridvCPU.Sort(dataGridvCPU.Columns[columnName], ListSortDirection.Ascending);
		StatusGridCounter.Text = num + " rows";
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Number of rows: " + num);
		}
		Telemetry.AddEvent("RenderTab", "vCPU", new Dictionary<string, string>
		{
			{
				"RenderTime",
				DateTime.UtcNow.Subtract(utcNow).ToString()
			},
			{
				"RowCount",
				num.ToString()
			}
		});
		Cursor = Cursors.Default;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void TabPagevMemory()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("ShowTab", "vMemory");
		DateTime utcNow = DateTime.UtcNow;
		if (!TabPagevMemory_Refresh_data)
		{
			StatusGridCounter.Text = dataGridvMemory.RowCount + " rows";
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
			return;
		}
		TabPagevMemory_Refresh_data = false;
		Application.DoEvents();
		Cursor = Cursors.WaitCursor;
		dataGridvMemory.Rows.Clear();
		int num = 0;
		if (!PreferencesClass.ExcludeAnnotations)
		{
			SortedList<string, string> customFieldHeaders = VISDK.GetCustomFieldHeaders("VirtualMachine");
			int num2 = dataGridvMemory.Columns["vMemoryNotes"].Index;
			try
			{
				foreach (KeyValuePair<string, string> item in customFieldHeaders)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
					dataGridViewTextBoxColumn.Name = "CustomField" + item.Value;
					dataGridViewTextBoxColumn.HeaderText = item.Key;
					dataGridViewTextBoxColumn.Width = 200;
					if (!dataGridvMemory.Columns.Contains("CustomField" + item.Value))
					{
						dataGridvMemory.Columns.Insert(num2 + 1, dataGridViewTextBoxColumn);
					}
					num2++;
				}
			}
			catch (Exception ex)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding CustomFieldHeaders " + ex.Message);
				}
			}
		}
		if (!PreferencesClass.ExcludeTags)
		{
			try
			{
				List<Hashtable> tagColumnNames = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "VirtualMachine");
				int num3 = dataGridvMemory.Columns["vMemoryDatacenter"].Index - 1;
				foreach (Hashtable item2 in tagColumnNames)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
					if (!dataGridvMemory.Columns.Contains("vMemory_tags_" + item2["Name"].ToString()))
					{
						dataGridViewTextBoxColumn2.Name = "vMemory_tags_" + item2["Name"].ToString();
						dataGridViewTextBoxColumn2.HeaderText = item2["Name"].ToString();
						dataGridViewTextBoxColumn2.ToolTipText = item2["Desc"].ToString();
						dataGridViewTextBoxColumn2.Width = 200;
						dataGridvMemory.Columns.Insert(num3 + 1, dataGridViewTextBoxColumn2);
						num3++;
					}
				}
			}
			catch (Exception ex2)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding tags " + ex2.Message);
				}
			}
		}
		StatusFilter.Visible = FilterClass.ShowFilterIcon();
		for (int i = 0; i < VISDK.InventoryCounter; i++)
		{
			if (FilterClass.ShowFilterIcon() && (!FilterClass.DisplayVirtualMachine(i) || !FilterClass.DisplayPowerstate(i) || !FilterClass.DisplayDataCenter(VISDK.GetDatacenterName(VISDK.GetSummaryRuntimeHost(i))) || !FilterClass.DisplayCluster(VISDK.GetClusterName(VISDK.GetSummaryRuntimeHost(i))) || !FilterClass.DisplayHost(VISDK.GetHostName(VISDK.GetSummaryRuntimeHost(i))) || !FilterClass.DisplayVMName(i) || !FilterClass.DisplayNotes(i)))
			{
				continue;
			}
			dataGridvMemory.Rows.Add();
			string vmInventoryProperty = VISDK.GetVmInventoryProperty(i, "summary.config.name");
			if (logger.IsInfoEnabled)
			{
				logger.Info(currentMethod.Name + " Add Row for Memory: " + vmInventoryProperty);
			}
			dataGridvMemory.Rows[num].Cells["vMemoryVMName"].Value = vmInventoryProperty;
			dataGridvMemory.Rows[num].Cells["vMemoryPowerState"].Value = VISDK.GetVmInventoryProperty(i, "summary.runtime.powerState");
			dataGridvMemory.Rows[num].Cells["vMemoryTemplate"].Value = VISDK.GetVmInventoryProperty(i, "summary.config.template");
			bool sRMPlaceholderBool = VISDK.GetSRMPlaceholderBool(i);
			dataGridvMemory.Rows[num].Cells["vMemorySRMPlaceholder"].Value = sRMPlaceholderBool.ToString();
			dataGridvMemory.Rows[num].Cells["vMemorySizeMiB"].Value = VISDK.GetInventoryPropertyInt(i, "summary.config.memorySizeMB");
			dataGridvMemory.Rows[num].Cells["vMemoryOverhead"].Value = VISDK.GetInventoryPropertyInt(i, "summary.runtime.memoryOverhead") / 1048576;
			dataGridvMemory.Rows[num].Cells["vMemoryMaxUsage"].Value = VISDK.GetInventoryPropertyInt(i, "summary.runtime.maxMemoryUsage");
			dataGridvMemory.Rows[num].Cells["vMemoryGuestMemoryUsage"].Value = VISDK.GetInventoryPropertyInt(i, "summary.quickStats.guestMemoryUsage");
			dataGridvMemory.Rows[num].Cells["vMemoryHostMemoryUsage"].Value = VISDK.GetInventoryPropertyInt(i, "summary.quickStats.hostMemoryUsage");
			dataGridvMemory.Rows[num].Cells["vMemoryballoonedMemory"].Value = VISDK.GetInventoryPropertyInt(i, "summary.quickStats.balloonedMemory");
			dataGridvMemory.Rows[num].Cells["vMemoryconsumedOverheadMemory"].Value = VISDK.GetInventoryPropertyInt(i, "summary.quickStats.consumedOverheadMemory");
			dataGridvMemory.Rows[num].Cells["vMemoryprivateMemory"].Value = VISDK.GetInventoryPropertyInt(i, "summary.quickStats.privateMemory");
			dataGridvMemory.Rows[num].Cells["vMemorysharedMemory"].Value = VISDK.GetInventoryPropertyInt(i, "summary.quickStats.sharedMemory");
			dataGridvMemory.Rows[num].Cells["vMemoryswappedMemory"].Value = VISDK.GetInventoryPropertyInt(i, "summary.quickStats.swappedMemory");
			dataGridvMemory.Rows[num].Cells["vMemorystaticMemoryEntitlement"].Value = VISDK.GetInventoryPropertyInt(i, "summary.quickStats.staticMemoryEntitlement");
			dataGridvMemory.Rows[num].Cells["vMemorydistributedMemoryEntitlement"].Value = VISDK.GetInventoryPropertyInt(i, "summary.quickStats.distributedMemoryEntitlement");
			try
			{
				SharesInfo memoryShareInfo = VISDK.GetMemoryShareInfo(i);
				dataGridvMemory.Rows[num].Cells["vMemorySharesLevel"].Value = memoryShareInfo.level.ToString();
				dataGridvMemory.Rows[num].Cells["vMemoryShares"].Value = memoryShareInfo.shares;
			}
			catch
			{
			}
			try
			{
				dataGridvMemory.Rows[num].Cells["vMemoryReservationLockedToMax"].Value = VISDK.GetVmInventoryProperty(i, "config.memoryReservationLockedToMax");
			}
			catch
			{
			}
			dataGridvMemory.Rows[num].Cells["vMemoryReservation"].Value = VISDK.GetInventoryPropertyInt(i, "resourceConfig.memoryAllocation.reservation");
			dataGridvMemory.Rows[num].Cells["vMemoryReservationLimit"].Value = VISDK.GetInventoryPropertyInt(i, "resourceConfig.memoryAllocation.limit");
			dataGridvMemory.Rows[num].Cells["vMemoryOS"].Value = VISDK.GetVmInventoryProperty(i, "summary.config.guestFullName");
			dataGridvMemory.Rows[num].Cells["vMemoryOSTools"].Value = VISDK.GetVmInventoryProperty(i, "guest.guestFullName");
			dataGridvMemory.Rows[num].Cells["vMemoryHost"].Value = VISDK.GetHostName(VISDK.GetSummaryRuntimeHost(i));
			dataGridvMemory.Rows[num].Cells["vMemoryDatacenter"].Value = VISDK.GetDatacenterName(VISDK.GetSummaryRuntimeHost(i));
			dataGridvMemory.Rows[num].Cells["vMemoryCluster"].Value = VISDK.GetClusterName(VISDK.GetSummaryRuntimeHost(i));
			dataGridvMemory.Rows[num].Cells["vMemoryHotAdd"].Value = VISDK.GetVmInventoryProperty(i, "config.memoryHotAddEnabled");
			if (!PreferencesClass.ExcludeAnnotations)
			{
				dataGridvMemory.Rows[num].Cells["vMemoryNotes"].Value = VISDK.GetSummaryConfigAnnotation(i);
				CustomFieldValue[] customFieldValueForVM = VISDK.GetCustomFieldValueForVM(i);
				for (int num4 = customFieldValueForVM.Length; num4 > 0; num4--)
				{
					CustomFieldStringValue customFieldStringValue = (CustomFieldStringValue)customFieldValueForVM.GetValue(num4 - 1);
					try
					{
						dataGridvMemory.Rows[num].Cells["CustomField" + customFieldStringValue.key].Value = customFieldStringValue.value;
					}
					catch (Exception ex3)
					{
						if (logger.IsDebugEnabled)
						{
							logger.Debug(currentMethod.Name + " Handled Exception: during setting value to custom field " + ex3.Message);
						}
					}
				}
			}
			if (!PreferencesClass.ExcludeTags)
			{
				try
				{
					List<Hashtable> tagColumnNames2 = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "VirtualMachine");
					string summaryVM = VISDK.GetSummaryVM(i);
					foreach (Hashtable item3 in tagColumnNames2)
					{
						string tagColumnValues = VISDK.GetTagColumnValues(ref VISDK.cisAssociatedTag, "VirtualMachine", item3["Name"].ToString(), summaryVM);
						dataGridvMemory.Rows[num].Cells["vMemory_tags_" + item3["Name"].ToString()].Value = tagColumnValues;
					}
				}
				catch (Exception ex4)
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(currentMethod.Name + " Handled Exception: during setting tag value " + ex4.Message);
					}
				}
			}
			if (PreferencesClass.vInfoShowFolder || CommandToExecute != null)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Call GetVMFolder for VM " + vmInventoryProperty);
				}
				string vMFolder = VISDK.GetVMFolder(i);
				dataGridvMemory.Rows[num].Cells["vMemoryFolder"].Value = vMFolder;
			}
			dataGridvMemory.Rows[num].Cells["vMemoryUUID"].Value = VISDK.GetVmInventoryProperty(i, "config.instanceUuid");
			dataGridvMemory.Rows[num].Cells["vMemoryObjectID"].Value = VISDK.GetSummaryVM(i);
			dataGridvMemory.Rows[num].Cells["vMemoryInstanceUUID"].Value = InstanceUUID;
			dataGridvMemory.Rows[num].Cells["vMemoryVISDKServer"].Value = VISDK.Server;
			num++;
		}
		dataGridvMemory.AutoResizeColumns();
		string columnName = "vMemoryVMName";
		if (dataGridvMemory.SortedColumn != null)
		{
			columnName = dataGridvMemory.SortedColumn.Name;
		}
		dataGridvMemory.Sort(dataGridvMemory.Columns[columnName], ListSortDirection.Ascending);
		StatusGridCounter.Text = num + " rows";
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Number of rows: " + num);
		}
		Telemetry.AddEvent("RenderTab", "vMemory", new Dictionary<string, string>
		{
			{
				"RenderTime",
				DateTime.UtcNow.Subtract(utcNow).ToString()
			},
			{
				"RowCount",
				num.ToString()
			}
		});
		Cursor = Cursors.Default;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void TabPagevDisk()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("ShowTab", "vDisk");
		DateTime utcNow = DateTime.UtcNow;
		if (!TabPagevDisk_Refresh_data)
		{
			StatusGridCounter.Text = dataGridvDisk.RowCount + " rows";
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
			return;
		}
		TabPagevDisk_Refresh_data = false;
		Application.DoEvents();
		Cursor = Cursors.WaitCursor;
		dataGridvDisk.Rows.Clear();
		int num = 0;
		if (!PreferencesClass.ExcludeAnnotations)
		{
			SortedList<string, string> customFieldHeaders = VISDK.GetCustomFieldHeaders("VirtualMachine");
			int num2 = dataGridvDisk.Columns["vDiskNotes"].Index;
			try
			{
				foreach (KeyValuePair<string, string> item in customFieldHeaders)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
					dataGridViewTextBoxColumn.Name = "CustomField" + item.Value;
					dataGridViewTextBoxColumn.HeaderText = item.Key;
					dataGridViewTextBoxColumn.Width = 200;
					if (!dataGridvDisk.Columns.Contains("CustomField" + item.Value))
					{
						dataGridvDisk.Columns.Insert(num2 + 1, dataGridViewTextBoxColumn);
					}
					num2++;
				}
			}
			catch (Exception ex)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding CustomFieldHeaders " + ex.Message);
				}
			}
		}
		if (!PreferencesClass.ExcludeTags)
		{
			try
			{
				List<Hashtable> tagColumnNames = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "VirtualMachine");
				int num3 = dataGridvDisk.Columns["vDiskDatacenter"].Index - 1;
				foreach (Hashtable item2 in tagColumnNames)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
					if (!dataGridvDisk.Columns.Contains("vDisk_tags_" + item2["Name"].ToString()))
					{
						dataGridViewTextBoxColumn2.Name = "vDisk_tags_" + item2["Name"].ToString();
						dataGridViewTextBoxColumn2.HeaderText = item2["Name"].ToString();
						dataGridViewTextBoxColumn2.ToolTipText = item2["Desc"].ToString();
						dataGridViewTextBoxColumn2.Width = 200;
						dataGridvDisk.Columns.Insert(num3 + 1, dataGridViewTextBoxColumn2);
						num3++;
					}
				}
			}
			catch (Exception ex2)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding tags " + ex2.Message);
				}
			}
		}
		StatusFilter.Visible = FilterClass.ShowFilterIcon();
		for (int i = 0; i < VISDK.InventoryCounter; i++)
		{
			if (FilterClass.ShowFilterIcon() && (!FilterClass.DisplayVirtualMachine(i) || !FilterClass.DisplayPowerstate(i) || !FilterClass.DisplayDataCenter(VISDK.GetDatacenterName(VISDK.GetSummaryRuntimeHost(i))) || !FilterClass.DisplayCluster(VISDK.GetClusterName(VISDK.GetSummaryRuntimeHost(i))) || !FilterClass.DisplayHost(VISDK.GetHostName(VISDK.GetSummaryRuntimeHost(i))) || !FilterClass.DisplayVMName(i) || !FilterClass.DisplayNotes(i)))
			{
				continue;
			}
			VirtualDevice[] deviceInfo = VISDK.GetDeviceInfo(i);
			int num4 = 0;
			try
			{
				int num5 = 0;
				while (deviceInfo != null && num5 < deviceInfo.Length)
				{
					if (deviceInfo[num5] is VirtualDisk)
					{
						VirtualDisk virtualDisk = (VirtualDisk)deviceInfo[num5];
						dataGridvDisk.Rows.Add();
						string vmInventoryProperty = VISDK.GetVmInventoryProperty(i, "summary.config.name");
						if (logger.IsInfoEnabled)
						{
							logger.Info(currentMethod.Name + " Add Row for Disk: " + vmInventoryProperty);
						}
						dataGridvDisk.Rows[num].Cells["vDiskVMName"].Value = vmInventoryProperty;
						dataGridvDisk.Rows[num].Cells["vDiskPowerState"].Value = VISDK.GetVmInventoryProperty(i, "summary.runtime.powerState");
						dataGridvDisk.Rows[num].Cells["vDiskTemplate"].Value = VISDK.GetVmInventoryProperty(i, "summary.config.template");
						try
						{
							dataGridvDisk.Rows[num].Cells["vDiskKey"].Value = virtualDisk.key.ToString();
						}
						catch
						{
							dataGridvDisk.Rows[num].Cells["vDiskKey"].Value = null;
						}
						dataGridvDisk.Rows[num].Cells["vDiskSortColumn"].Value = vmInventoryProperty + " " + virtualDisk.key;
						bool sRMPlaceholderBool = VISDK.GetSRMPlaceholderBool(i);
						dataGridvDisk.Rows[num].Cells["vDiskSRMPlaceholder"].Value = sRMPlaceholderBool.ToString();
						try
						{
							dataGridvDisk.Rows[num].Cells["vDiskDisk"].Value = virtualDisk.deviceInfo.label;
						}
						catch
						{
							dataGridvDisk.Rows[num].Cells["vDiskDisk"].Value = null;
						}
						try
						{
							dataGridvDisk.Rows[num].Cells["vDiskCapacityMiB"].Value = virtualDisk.capacityInKB / 1024;
						}
						catch
						{
							dataGridvDisk.Rows[num].Cells["vDiskCapacityMiB"].Value = null;
						}
						try
						{
							dataGridvDisk.Rows[num].Cells["vDiskLevel"].Value = virtualDisk.shares.level.ToString();
						}
						catch
						{
							dataGridvDisk.Rows[num].Cells["vDiskLevel"].Value = null;
						}
						try
						{
							dataGridvDisk.Rows[num].Cells["vDiskShares"].Value = virtualDisk.shares.shares;
						}
						catch
						{
							dataGridvDisk.Rows[num].Cells["vDiskShares"].Value = null;
						}
						try
						{
							dataGridvDisk.Rows[num].Cells["vDiskReservation"].Value = virtualDisk.storageIOAllocation.reservation;
						}
						catch
						{
							dataGridvDisk.Rows[num].Cells["vDiskReservation"].Value = null;
						}
						try
						{
							dataGridvDisk.Rows[num].Cells["vDiskLimit"].Value = virtualDisk.storageIOAllocation.limit;
						}
						catch
						{
							dataGridvDisk.Rows[num].Cells["vDiskLimit"].Value = null;
						}
						try
						{
							dataGridvDisk.Rows[num].Cells["vDiskUnit"].Value = virtualDisk.unitNumber;
						}
						catch
						{
							dataGridvDisk.Rows[num].Cells["vDiskUnit"].Value = null;
						}
						int num6 = 0;
						while (deviceInfo != null && num6 < deviceInfo.Length)
						{
							if (deviceInfo[num6].key == virtualDisk.controllerKey)
							{
								VirtualController virtualController = (VirtualController)deviceInfo[num6];
								try
								{
									dataGridvDisk.Rows[num].Cells["vDiskController"].Value = virtualController.deviceInfo.summary;
								}
								catch
								{
									dataGridvDisk.Rows[num].Cells["vDiskController"].Value = null;
								}
								try
								{
									dataGridvDisk.Rows[num].Cells["vDiskControllerLabel"].Value = virtualController.deviceInfo.label;
								}
								catch
								{
									dataGridvDisk.Rows[num].Cells["vDiskControllerLabel"].Value = null;
								}
								try
								{
									VirtualSCSIController virtualSCSIController = (VirtualSCSIController)deviceInfo[num6];
									dataGridvDisk.Rows[num].Cells["vDiskControllerSharedBus"].Value = virtualSCSIController.sharedBus;
								}
								catch
								{
									dataGridvDisk.Rows[num].Cells["vDiskControllerSharedBus"].Value = null;
								}
								try
								{
									VirtualSCSIController virtualSCSIController2 = (VirtualSCSIController)deviceInfo[num6];
									dataGridvDisk.Rows[num].Cells["vDiskSCSICtlrUnitNumber"].Value = virtualSCSIController2.scsiCtlrUnitNumber;
								}
								catch
								{
									dataGridvDisk.Rows[num].Cells["vDiskSCSICtlrUnitNumber"].Value = null;
								}
							}
							num6++;
						}
						try
						{
							if (virtualDisk.backing is VirtualDiskFlatVer2BackingInfo)
							{
								dataGridvDisk.Rows[num].Cells["vDiskRaw"].Value = "False";
								VirtualDiskFlatVer2BackingInfo virtualDiskFlatVer2BackingInfo = (VirtualDiskFlatVer2BackingInfo)virtualDisk.backing;
								try
								{
									dataGridvDisk.Rows[num].Cells["vDiskPath"].Value = VISDK.GetFriendlyPath(virtualDiskFlatVer2BackingInfo.fileName);
								}
								catch
								{
									dataGridvDisk.Rows[num].Cells["vDiskPath"].Value = null;
								}
								if (virtualDiskFlatVer2BackingInfo.thinProvisionedSpecified)
								{
									dataGridvDisk.Rows[num].Cells["vDiskThinProvisioned"].Value = virtualDiskFlatVer2BackingInfo.thinProvisioned.ToString();
								}
								if (virtualDiskFlatVer2BackingInfo.splitSpecified)
								{
									dataGridvDisk.Rows[num].Cells["vDiskSplit"].Value = virtualDiskFlatVer2BackingInfo.split.ToString();
								}
								try
								{
									dataGridvDisk.Rows[num].Cells["vDiskEagerlyScrub"].Value = virtualDiskFlatVer2BackingInfo.eagerlyScrub.ToString();
								}
								catch
								{
									dataGridvDisk.Rows[num].Cells["vDiskEagerlyScrub"].Value = null;
								}
								try
								{
									dataGridvDisk.Rows[num].Cells["vDiskWriteThrough"].Value = virtualDiskFlatVer2BackingInfo.writeThrough.ToString();
								}
								catch
								{
									dataGridvDisk.Rows[num].Cells["vDiskWriteThrough"].Value = null;
								}
								dataGridvDisk.Rows[num].Cells["vDiskMode"].Value = virtualDiskFlatVer2BackingInfo.diskMode;
								dataGridvDisk.Rows[num].Cells["vDiskDiskUUID"].Value = virtualDiskFlatVer2BackingInfo.uuid;
								try
								{
									dataGridvDisk.Rows[num].Cells["vDiskSharingMode"].Value = virtualDiskFlatVer2BackingInfo.sharing;
								}
								catch
								{
									dataGridvDisk.Rows[num].Cells["vDiskSharingMode"].Value = null;
								}
							}
							if (virtualDisk.backing is VirtualDiskRawDiskMappingVer1BackingInfo)
							{
								dataGridvDisk.Rows[num].Cells["vDiskRaw"].Value = "True";
								VirtualDiskRawDiskMappingVer1BackingInfo virtualDiskRawDiskMappingVer1BackingInfo = (VirtualDiskRawDiskMappingVer1BackingInfo)virtualDisk.backing;
								try
								{
									dataGridvDisk.Rows[num].Cells["vDiskMode"].Value = virtualDiskRawDiskMappingVer1BackingInfo.diskMode;
								}
								catch
								{
									dataGridvDisk.Rows[num].Cells["vDiskMode"].Value = null;
								}
								try
								{
									dataGridvDisk.Rows[num].Cells["vDiskPath"].Value = VISDK.GetFriendlyPath(virtualDiskRawDiskMappingVer1BackingInfo.fileName);
								}
								catch
								{
									dataGridvDisk.Rows[num].Cells["vDiskPath"].Value = null;
								}
								dataGridvDisk.Rows[num].Cells["vDiskDiskUUID"].Value = virtualDiskRawDiskMappingVer1BackingInfo.uuid;
								try
								{
									dataGridvDisk.Rows[num].Cells["vDiskRawCompMode"].Value = virtualDiskRawDiskMappingVer1BackingInfo.compatibilityMode;
								}
								catch
								{
									dataGridvDisk.Rows[num].Cells["vDiskRawCompMode"].Value = null;
								}
								try
								{
									dataGridvDisk.Rows[num].Cells["vDiskRawLunId"].Value = virtualDiskRawDiskMappingVer1BackingInfo.lunUuid;
								}
								catch
								{
									dataGridvDisk.Rows[num].Cells["vDiskRawLunId"].Value = null;
								}
							}
						}
						catch
						{
						}
						if (VISDK.APIVersion >= 700)
						{
							string text = null;
							GuestDiskInfo[] guestDiskInfo = VISDK.GetGuestDiskInfo(i);
							foreach (GuestDiskInfo guestDiskInfo2 in guestDiskInfo)
							{
								try
								{
									GuestInfoVirtualDiskMapping[] mappings = guestDiskInfo2.mappings;
									for (int k = 0; k < mappings.Length; k++)
									{
										if (mappings[k].key == virtualDisk.key)
										{
											text = ((text != null) ? (text + ", " + guestDiskInfo2.diskPath) : guestDiskInfo2.diskPath);
										}
									}
									dataGridvDisk.Rows[num].Cells["vDiskDiskPath"].Value = text;
								}
								catch
								{
								}
							}
						}
						dataGridvDisk.Rows[num].Cells["vDiskOS"].Value = VISDK.GetVmInventoryProperty(i, "summary.config.guestFullName");
						dataGridvDisk.Rows[num].Cells["vDiskOSTools"].Value = VISDK.GetVmInventoryProperty(i, "guest.guestFullName");
						dataGridvDisk.Rows[num].Cells["vDiskHost"].Value = VISDK.GetHostName(VISDK.GetSummaryRuntimeHost(i));
						dataGridvDisk.Rows[num].Cells["vDiskDatacenter"].Value = VISDK.GetDatacenterName(VISDK.GetSummaryRuntimeHost(i));
						dataGridvDisk.Rows[num].Cells["vDiskCluster"].Value = VISDK.GetClusterName(VISDK.GetSummaryRuntimeHost(i));
						if (!PreferencesClass.ExcludeAnnotations)
						{
							dataGridvDisk.Rows[num].Cells["vDiskNotes"].Value = VISDK.GetSummaryConfigAnnotation(i);
							CustomFieldValue[] customFieldValueForVM = VISDK.GetCustomFieldValueForVM(i);
							for (int num7 = customFieldValueForVM.Length; num7 > 0; num7--)
							{
								CustomFieldStringValue customFieldStringValue = (CustomFieldStringValue)customFieldValueForVM.GetValue(num7 - 1);
								try
								{
									dataGridvDisk.Rows[num].Cells["CustomField" + customFieldStringValue.key].Value = customFieldStringValue.value;
								}
								catch (Exception ex3)
								{
									if (logger.IsDebugEnabled)
									{
										logger.Debug(currentMethod.Name + " Handled Exception: during setting value to custom field " + ex3.Message);
									}
								}
							}
						}
						if (!PreferencesClass.ExcludeTags)
						{
							try
							{
								List<Hashtable> tagColumnNames2 = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "VirtualMachine");
								string summaryVM = VISDK.GetSummaryVM(i);
								foreach (Hashtable item3 in tagColumnNames2)
								{
									string tagColumnValues = VISDK.GetTagColumnValues(ref VISDK.cisAssociatedTag, "VirtualMachine", item3["Name"].ToString(), summaryVM);
									dataGridvDisk.Rows[num].Cells["vDisk_tags_" + item3["Name"].ToString()].Value = tagColumnValues;
								}
							}
							catch (Exception ex4)
							{
								if (logger.IsDebugEnabled)
								{
									logger.Debug(currentMethod.Name + " Handled Exception: during setting tag value " + ex4.Message);
								}
							}
						}
						if (PreferencesClass.vInfoShowFolder || CommandToExecute != null)
						{
							if (logger.IsDebugEnabled)
							{
								logger.Debug(currentMethod.Name + " Call GetVMFolder for VM " + vmInventoryProperty);
							}
							string vMFolder = VISDK.GetVMFolder(i);
							dataGridvDisk.Rows[num].Cells["vDiskFolder"].Value = vMFolder;
						}
						dataGridvDisk.Rows[num].Cells["vDiskUUID"].Value = VISDK.GetVmInventoryProperty(i, "config.instanceUuid");
						dataGridvDisk.Rows[num].Cells["vDiskObjectID"].Value = VISDK.GetSummaryVM(i);
						dataGridvDisk.Rows[num].Cells["vDiskInstanceUUID"].Value = InstanceUUID;
						dataGridvDisk.Rows[num].Cells["vDiskVISDKServer"].Value = VISDK.Server;
						num++;
						num4++;
					}
					num5++;
				}
			}
			catch
			{
			}
		}
		dataGridvDisk.AutoResizeColumns();
		string columnName = "vDiskSortColumn";
		if (dataGridvDisk.SortedColumn != null)
		{
			columnName = dataGridvDisk.SortedColumn.Name;
		}
		dataGridvDisk.Sort(dataGridvDisk.Columns[columnName], ListSortDirection.Ascending);
		StatusGridCounter.Text = num + " rows";
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Number of rows: " + num);
		}
		Telemetry.AddEvent("RenderTab", "vDisk", new Dictionary<string, string>
		{
			{
				"RenderTime",
				DateTime.UtcNow.Subtract(utcNow).ToString()
			},
			{
				"RowCount",
				num.ToString()
			}
		});
		Cursor = Cursors.Default;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void TabPagevPartition()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("ShowTab", "vPartition");
		DateTime utcNow = DateTime.UtcNow;
		if (!TabPagevPartition_Refresh_data)
		{
			StatusGridCounter.Text = dataGridvPartition.RowCount + " rows";
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
			return;
		}
		TabPagevPartition_Refresh_data = false;
		Application.DoEvents();
		Cursor = Cursors.WaitCursor;
		dataGridvPartition.Rows.Clear();
		int num = 0;
		if (!PreferencesClass.ExcludeAnnotations)
		{
			SortedList<string, string> customFieldHeaders = VISDK.GetCustomFieldHeaders("VirtualMachine");
			int num2 = dataGridvPartition.Columns["vPartitionNotes"].Index;
			try
			{
				foreach (KeyValuePair<string, string> item in customFieldHeaders)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
					dataGridViewTextBoxColumn.Name = "CustomField" + item.Value;
					dataGridViewTextBoxColumn.HeaderText = item.Key;
					dataGridViewTextBoxColumn.Width = 200;
					if (!dataGridvPartition.Columns.Contains("CustomField" + item.Value))
					{
						dataGridvPartition.Columns.Insert(num2 + 1, dataGridViewTextBoxColumn);
					}
					num2++;
				}
			}
			catch (Exception ex)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding CustomFieldHeaders " + ex.Message);
				}
			}
		}
		if (!PreferencesClass.ExcludeTags)
		{
			try
			{
				List<Hashtable> tagColumnNames = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "VirtualMachine");
				int num3 = dataGridvPartition.Columns["vPartitionDatacenter"].Index - 1;
				foreach (Hashtable item2 in tagColumnNames)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
					if (!dataGridvPartition.Columns.Contains("vPartition_tags_" + item2["Name"].ToString()))
					{
						dataGridViewTextBoxColumn2.Name = "vPartition_tags_" + item2["Name"].ToString();
						dataGridViewTextBoxColumn2.HeaderText = item2["Name"].ToString();
						dataGridViewTextBoxColumn2.ToolTipText = item2["Desc"].ToString();
						dataGridViewTextBoxColumn2.Width = 200;
						dataGridvPartition.Columns.Insert(num3 + 1, dataGridViewTextBoxColumn2);
						num3++;
					}
				}
			}
			catch (Exception ex2)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding tags " + ex2.Message);
				}
			}
		}
		StatusFilter.Visible = FilterClass.ShowFilterIcon();
		for (int i = 0; i < 1; i++)
		{
			for (int j = 0; j < VISDK.InventoryCounter; j++)
			{
				if (FilterClass.ShowFilterIcon() && (!FilterClass.DisplayVirtualMachine(j) || !FilterClass.DisplayPowerstate(j) || !FilterClass.DisplayDataCenter(VISDK.GetDatacenterName(VISDK.GetSummaryRuntimeHost(j))) || !FilterClass.DisplayCluster(VISDK.GetClusterName(VISDK.GetSummaryRuntimeHost(j))) || !FilterClass.DisplayHost(VISDK.GetHostName(VISDK.GetSummaryRuntimeHost(j))) || !FilterClass.DisplayVMName(j) || !FilterClass.DisplayNotes(j)))
				{
					continue;
				}
				GuestDiskInfo[] guestDiskInfo = VISDK.GetGuestDiskInfo(j);
				if (guestDiskInfo == null)
				{
					continue;
				}
				for (int k = 0; k < guestDiskInfo.Length; k++)
				{
					if (VISDK.GetVmInventoryProperty(j, "summary.runtime.powerState") != "poweredOn")
					{
						continue;
					}
					dataGridvPartition.Rows.Add();
					string vmInventoryProperty = VISDK.GetVmInventoryProperty(j, "summary.config.name");
					if (logger.IsInfoEnabled)
					{
						logger.Info(currentMethod.Name + " Add Row for Partition: " + vmInventoryProperty);
					}
					dataGridvPartition.Rows[num].Cells["vPartitionVMName"].Value = vmInventoryProperty;
					dataGridvPartition.Rows[num].Cells["vPartitionPowerState"].Value = VISDK.GetVmInventoryProperty(j, "summary.runtime.powerState");
					dataGridvPartition.Rows[num].Cells["vPartitionTemplate"].Value = VISDK.GetVmInventoryProperty(j, "summary.config.template");
					bool sRMPlaceholderBool = VISDK.GetSRMPlaceholderBool(j);
					dataGridvPartition.Rows[num].Cells["vPartitionSRMPlaceholder"].Value = sRMPlaceholderBool.ToString();
					string text = null;
					try
					{
						GuestInfoVirtualDiskMapping[] mappings = guestDiskInfo[k].mappings;
						text = mappings[0].key.ToString();
						dataGridvPartition.Rows[num].Cells["vPartitionDiskKey"].Value = mappings[0].key.ToString();
					}
					catch
					{
					}
					dataGridvPartition.Rows[num].Cells["vPartitionSortColumn"].Value = vmInventoryProperty + " " + text;
					try
					{
						dataGridvPartition.Rows[num].Cells["vPartitionDisk"].Value = guestDiskInfo[k].diskPath.ToString();
					}
					catch
					{
						dataGridvPartition.Rows[num].Cells["vPartitionDisk"].Value = null;
					}
					try
					{
						dataGridvPartition.Rows[num].Cells["vPartitionCapacityMiB"].Value = guestDiskInfo[k].capacity / 1048576;
					}
					catch
					{
						dataGridvPartition.Rows[num].Cells["vPartitionCapacityMiB"].Value = null;
					}
					try
					{
						dataGridvPartition.Rows[num].Cells["vPartitionConsumedMiB"].Value = guestDiskInfo[k].capacity / 1048576 - guestDiskInfo[k].freeSpace / 1048576;
					}
					catch
					{
						dataGridvPartition.Rows[num].Cells["vPartitionConsumedMiB"].Value = null;
					}
					try
					{
						dataGridvPartition.Rows[num].Cells["vPartitionFreeMiB"].Value = guestDiskInfo[k].freeSpace / 1048576;
					}
					catch
					{
						dataGridvPartition.Rows[num].Cells["vPartitionFreeMiB"].Value = null;
					}
					long value = 0L;
					try
					{
						value = guestDiskInfo[k].freeSpace / (guestDiskInfo[k].capacity / 100);
					}
					catch
					{
					}
					try
					{
						dataGridvPartition.Rows[num].Cells["vPartitionFreePercentage"].Value = Convert.ToInt16(value);
					}
					catch
					{
						dataGridvPartition.Rows[num].Cells["vPartitionFreePercentage"].Value = null;
					}
					dataGridvPartition.Rows[num].Cells["vPartitionOS"].Value = VISDK.GetVmInventoryProperty(j, "summary.config.guestFullName");
					dataGridvPartition.Rows[num].Cells["vPartitionOSTools"].Value = VISDK.GetVmInventoryProperty(j, "guest.guestFullName");
					dataGridvPartition.Rows[num].Cells["vPartitionHost"].Value = VISDK.GetHostName(VISDK.GetSummaryRuntimeHost(j));
					dataGridvPartition.Rows[num].Cells["vPartitionDatacenter"].Value = VISDK.GetDatacenterName(VISDK.GetSummaryRuntimeHost(j));
					dataGridvPartition.Rows[num].Cells["vPartitionCluster"].Value = VISDK.GetClusterName(VISDK.GetSummaryRuntimeHost(j));
					if (!PreferencesClass.ExcludeAnnotations)
					{
						dataGridvPartition.Rows[num].Cells["vPartitionNotes"].Value = VISDK.GetSummaryConfigAnnotation(j);
						CustomFieldValue[] customFieldValueForVM = VISDK.GetCustomFieldValueForVM(j);
						for (int num4 = customFieldValueForVM.Length; num4 > 0; num4--)
						{
							CustomFieldStringValue customFieldStringValue = (CustomFieldStringValue)customFieldValueForVM.GetValue(num4 - 1);
							try
							{
								dataGridvPartition.Rows[num].Cells["CustomField" + customFieldStringValue.key].Value = customFieldStringValue.value;
							}
							catch (Exception ex3)
							{
								if (logger.IsDebugEnabled)
								{
									logger.Debug(currentMethod.Name + " Handled Exception: during setting value to custom field " + ex3.Message);
								}
							}
						}
					}
					if (!PreferencesClass.ExcludeTags)
					{
						try
						{
							List<Hashtable> tagColumnNames2 = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "VirtualMachine");
							string summaryVM = VISDK.GetSummaryVM(j);
							foreach (Hashtable item3 in tagColumnNames2)
							{
								string tagColumnValues = VISDK.GetTagColumnValues(ref VISDK.cisAssociatedTag, "VirtualMachine", item3["Name"].ToString(), summaryVM);
								dataGridvPartition.Rows[num].Cells["vPartition_tags_" + item3["Name"].ToString()].Value = tagColumnValues;
							}
						}
						catch (Exception ex4)
						{
							if (logger.IsDebugEnabled)
							{
								logger.Debug(currentMethod.Name + " Handled Exception: during setting tag value " + ex4.Message);
							}
						}
					}
					if (PreferencesClass.vInfoShowFolder || CommandToExecute != null)
					{
						if (logger.IsDebugEnabled)
						{
							logger.Debug(currentMethod.Name + " Call GetVMFolder for VM " + vmInventoryProperty);
						}
						string vMFolder = VISDK.GetVMFolder(j);
						dataGridvPartition.Rows[num].Cells["vPartitionFolder"].Value = vMFolder;
					}
					dataGridvPartition.Rows[num].Cells["vPartitionUUID"].Value = VISDK.GetVmInventoryProperty(j, "config.instanceUuid");
					dataGridvPartition.Rows[num].Cells["vPartitionObjectID"].Value = VISDK.GetSummaryVM(j);
					dataGridvPartition.Rows[num].Cells["vPartitionInstanceUUID"].Value = InstanceUUID;
					dataGridvPartition.Rows[num].Cells["vPartitionVISDKServer"].Value = VISDK.Server;
					num++;
				}
			}
		}
		dataGridvPartition.AutoResizeColumns();
		string columnName = "vPartitionSortColumn";
		if (dataGridvPartition.SortedColumn != null)
		{
			columnName = dataGridvPartition.SortedColumn.Name;
		}
		dataGridvPartition.Sort(dataGridvPartition.Columns[columnName], ListSortDirection.Ascending);
		StatusGridCounter.Text = num + " rows";
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Number of rows: " + num);
		}
		Telemetry.AddEvent("RenderTab", "vPartition", new Dictionary<string, string>
		{
			{
				"RenderTime",
				DateTime.UtcNow.Subtract(utcNow).ToString()
			},
			{
				"RowCount",
				num.ToString()
			}
		});
		Cursor = Cursors.Default;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void TabPagevNetwork()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("ShowTab", "vNetwork");
		DateTime utcNow = DateTime.UtcNow;
		if (!TabPagevNetwork_Refresh_data)
		{
			StatusGridCounter.Text = dataGridvNetwork.RowCount + " rows";
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
			return;
		}
		TabPagevNetwork_Refresh_data = false;
		List<HostPortGroup> portInfo = VISDK.GetPortInfo();
		Application.DoEvents();
		Cursor = Cursors.WaitCursor;
		dataGridvNetwork.Rows.Clear();
		int num = 0;
		if (!PreferencesClass.ExcludeAnnotations)
		{
			SortedList<string, string> customFieldHeaders = VISDK.GetCustomFieldHeaders("VirtualMachine");
			int num2 = dataGridvNetwork.Columns["vNetworkNotes"].Index;
			try
			{
				foreach (KeyValuePair<string, string> item in customFieldHeaders)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
					dataGridViewTextBoxColumn.Name = "CustomField" + item.Value;
					dataGridViewTextBoxColumn.HeaderText = item.Key;
					dataGridViewTextBoxColumn.Width = 200;
					if (!dataGridvNetwork.Columns.Contains("CustomField" + item.Value))
					{
						dataGridvNetwork.Columns.Insert(num2 + 1, dataGridViewTextBoxColumn);
					}
					num2++;
				}
			}
			catch (Exception ex)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding CustomFieldHeaders " + ex.Message);
				}
			}
		}
		if (!PreferencesClass.ExcludeTags)
		{
			try
			{
				List<Hashtable> tagColumnNames = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "VirtualMachine");
				int num3 = dataGridvNetwork.Columns["vNetworkDatacenter"].Index - 1;
				foreach (Hashtable item2 in tagColumnNames)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
					if (!dataGridvNetwork.Columns.Contains("vNetwork_tags_" + item2["Name"].ToString()))
					{
						dataGridViewTextBoxColumn2.Name = "vNetwork_tags_" + item2["Name"].ToString();
						dataGridViewTextBoxColumn2.HeaderText = item2["Name"].ToString();
						dataGridViewTextBoxColumn2.ToolTipText = item2["Desc"].ToString();
						dataGridViewTextBoxColumn2.Width = 200;
						dataGridvNetwork.Columns.Insert(num3 + 1, dataGridViewTextBoxColumn2);
						num3++;
					}
				}
			}
			catch (Exception ex2)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding tags " + ex2.Message);
				}
			}
		}
		StatusFilter.Visible = FilterClass.ShowFilterIcon();
		for (int i = 0; i < VISDK.InventoryCounter; i++)
		{
			if (FilterClass.ShowFilterIcon() && (!FilterClass.DisplayVirtualMachine(i) || !FilterClass.DisplayPowerstate(i) || !FilterClass.DisplayDataCenter(VISDK.GetDatacenterName(VISDK.GetSummaryRuntimeHost(i))) || !FilterClass.DisplayCluster(VISDK.GetClusterName(VISDK.GetSummaryRuntimeHost(i))) || !FilterClass.DisplayHost(VISDK.GetHostName(VISDK.GetSummaryRuntimeHost(i))) || !FilterClass.DisplayVMName(i) || !FilterClass.DisplayNotes(i)))
			{
				continue;
			}
			GuestNicInfo[] guestNicInfo = VISDK.GetGuestNicInfo(i);
			VirtualDevice[] deviceInfo = VISDK.GetDeviceInfo(i);
			int num4 = 0;
			try
			{
				int num5 = 0;
				while (deviceInfo != null && num5 < deviceInfo.Length)
				{
					if (deviceInfo[num5] is VirtualEthernetCard)
					{
						VirtualEthernetCard virtualEthernetCard = (VirtualEthernetCard)deviceInfo[num5];
						num4++;
						dataGridvNetwork.Rows.Add();
						string vmInventoryProperty = VISDK.GetVmInventoryProperty(i, "summary.config.name");
						if (logger.IsInfoEnabled)
						{
							logger.Info(currentMethod.Name + " Add Row for Network: " + vmInventoryProperty);
						}
						dataGridvNetwork.Rows[num].Cells["vNetworkVMName"].Value = vmInventoryProperty;
						dataGridvNetwork.Rows[num].Cells["vNetworkPowerstate"].Value = VISDK.GetVmInventoryProperty(i, "summary.runtime.powerState");
						dataGridvNetwork.Rows[num].Cells["vNetworkTemplate"].Value = VISDK.GetVmInventoryProperty(i, "summary.config.template");
						bool sRMPlaceholderBool = VISDK.GetSRMPlaceholderBool(i);
						dataGridvNetwork.Rows[num].Cells["vNetworkSRMPlaceholder"].Value = sRMPlaceholderBool.ToString();
						dataGridvNetwork.Rows[num].Cells["vNetworkNIC"].Value = virtualEthernetCard.deviceInfo.label;
						dataGridvNetwork.Rows[num].Cells["vNetworkSortColumn"].Value = vmInventoryProperty + " " + virtualEthernetCard.deviceInfo.label;
						dataGridvNetwork.Rows[num].Cells["vNetworkAdapter"].Value = virtualEthernetCard.ToString();
						dataGridvNetwork.Rows[num].Cells["vNetworkDirectPathIO"].Value = virtualEthernetCard.uptCompatibilityEnabled.ToString();
						if (virtualEthernetCard is VirtualE1000)
						{
							dataGridvNetwork.Rows[num].Cells["vNetworkAdapter"].Value = "E1000";
						}
						if (virtualEthernetCard is VirtualE1000e)
						{
							dataGridvNetwork.Rows[num].Cells["vNetworkAdapter"].Value = "E1000e";
						}
						if (virtualEthernetCard is VirtualPCNet32)
						{
							dataGridvNetwork.Rows[num].Cells["vNetworkAdapter"].Value = "PCNet32";
						}
						if (virtualEthernetCard is VirtualVmxnet)
						{
							dataGridvNetwork.Rows[num].Cells["vNetworkAdapter"].Value = "Vmxnet";
						}
						if (virtualEthernetCard is VirtualVmxnet2)
						{
							dataGridvNetwork.Rows[num].Cells["vNetworkAdapter"].Value = "Vmxnet2";
						}
						if (virtualEthernetCard is VirtualVmxnet3)
						{
							dataGridvNetwork.Rows[num].Cells["vNetworkAdapter"].Value = "Vmxnet3";
						}
						dataGridvNetwork.Rows[num].Cells["vNetworkName"].Value = virtualEthernetCard.deviceInfo.summary;
						if (logger.IsInfoEnabled)
						{
							logger.Info(currentMethod.Name + " vNetworkName = VNIC.deviceInfo.summary = " + virtualEthernetCard.deviceInfo.summary);
						}
						try
						{
							VirtualEthernetCardNetworkBackingInfo virtualEthernetCardNetworkBackingInfo = (VirtualEthernetCardNetworkBackingInfo)virtualEthernetCard.backing;
							dataGridvNetwork.Rows[num].Cells["vNetworkName"].Value = virtualEthernetCardNetworkBackingInfo.deviceName;
							if (logger.IsInfoEnabled)
							{
								logger.Info(currentMethod.Name + " vNetworkName = BackingInfo.deviceName = " + virtualEthernetCardNetworkBackingInfo.deviceName);
							}
							foreach (HostPortGroup item3 in portInfo)
							{
								if (virtualEthernetCardNetworkBackingInfo.deviceName == item3.spec.name)
								{
									dataGridvNetwork.Rows[num].Cells["vNetworkSwitch"].Value = item3.spec.vswitchName;
								}
							}
						}
						catch
						{
						}
						try
						{
							VirtualEthernetCardDistributedVirtualPortBackingInfo virtualEthernetCardDistributedVirtualPortBackingInfo = (VirtualEthernetCardDistributedVirtualPortBackingInfo)virtualEthernetCard.backing;
							dataGridvNetwork.Rows[num].Cells["vNetworkSwitch"].Value = VISDK.GetDSSwitchNameV2(virtualEthernetCardDistributedVirtualPortBackingInfo.port.switchUuid);
							dataGridvNetwork.Rows[num].Cells["vNetworkName"].Value = VISDK.GetDSPortNameV2(virtualEthernetCardDistributedVirtualPortBackingInfo.port.portgroupKey);
						}
						catch
						{
						}
						dataGridvNetwork.Rows[num].Cells["vNetworkConnected"].Value = virtualEthernetCard.connectable.connected;
						dataGridvNetwork.Rows[num].Cells["vNetworkStartsConnected"].Value = virtualEthernetCard.connectable.startConnected;
						dataGridvNetwork.Rows[num].Cells["vNetworkMacAddress"].Value = virtualEthernetCard.macAddress;
						dataGridvNetwork.Rows[num].Cells["vNetworkMacAddressType"].Value = virtualEthernetCard.addressType;
						dataGridvNetwork.Rows[num].Cells["vNetworkIP4Address"].Value = "unknown";
						dataGridvNetwork.Rows[num].Cells["vNetworkIP6Address"].Value = "unknown";
						string text = null;
						string text2 = null;
						try
						{
							GuestNicInfo[] array = guestNicInfo;
							foreach (GuestNicInfo guestNicInfo2 in array)
							{
								if (virtualEthernetCard.key != guestNicInfo2.deviceConfigId)
								{
									continue;
								}
								string[] ipAddress = guestNicInfo2.ipAddress;
								foreach (string text3 in ipAddress)
								{
									if (text3.Contains(":"))
									{
										text2 = ((text2 != null) ? (text2 + ", " + text3) : text3);
									}
									else
									{
										text = ((text != null) ? (text + ", " + text3) : text3);
									}
								}
							}
						}
						catch
						{
						}
						if (text == null)
						{
							dataGridvNetwork.Rows[num].Cells["vNetworkIP4Address"].Value = null;
						}
						else
						{
							dataGridvNetwork.Rows[num].Cells["vNetworkIP4Address"].Value = text;
						}
						if (text2 == null)
						{
							dataGridvNetwork.Rows[num].Cells["vNetworkIP6Address"].Value = null;
						}
						else
						{
							dataGridvNetwork.Rows[num].Cells["vNetworkIP6Address"].Value = text2;
						}
						dataGridvNetwork.Rows[num].Cells["vNetworkOS"].Value = VISDK.GetVmInventoryProperty(i, "summary.config.guestFullName");
						dataGridvNetwork.Rows[num].Cells["vNetworkOSTools"].Value = VISDK.GetVmInventoryProperty(i, "guest.guestFullName");
						dataGridvNetwork.Rows[num].Cells["vNetworkHost"].Value = VISDK.GetHostName(VISDK.GetSummaryRuntimeHost(i));
						dataGridvNetwork.Rows[num].Cells["vNetworkDatacenter"].Value = VISDK.GetDatacenterName(VISDK.GetSummaryRuntimeHost(i));
						dataGridvNetwork.Rows[num].Cells["vNetworkCluster"].Value = VISDK.GetClusterName(VISDK.GetSummaryRuntimeHost(i));
						if (PreferencesClass.vInfoShowFolder || CommandToExecute != null)
						{
							if (logger.IsDebugEnabled)
							{
								logger.Debug(currentMethod.Name + " Call GetVMFolder for VM " + vmInventoryProperty);
							}
							string vMFolder = VISDK.GetVMFolder(i);
							dataGridvNetwork.Rows[num].Cells["vNetworkFolder"].Value = vMFolder;
						}
						if (!PreferencesClass.ExcludeAnnotations)
						{
							dataGridvNetwork.Rows[num].Cells["vNetworkNotes"].Value = VISDK.GetSummaryConfigAnnotation(i);
							CustomFieldValue[] customFieldValueForVM = VISDK.GetCustomFieldValueForVM(i);
							for (int num6 = customFieldValueForVM.Length; num6 > 0; num6--)
							{
								CustomFieldStringValue customFieldStringValue = (CustomFieldStringValue)customFieldValueForVM.GetValue(num6 - 1);
								try
								{
									dataGridvNetwork.Rows[num].Cells["CustomField" + customFieldStringValue.key].Value = customFieldStringValue.value;
								}
								catch (Exception ex3)
								{
									if (logger.IsDebugEnabled)
									{
										logger.Debug(currentMethod.Name + " Handled Exception: during setting value to custom field " + ex3.Message);
									}
								}
							}
						}
						if (!PreferencesClass.ExcludeTags)
						{
							try
							{
								List<Hashtable> tagColumnNames2 = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "VirtualMachine");
								string summaryVM = VISDK.GetSummaryVM(i);
								foreach (Hashtable item4 in tagColumnNames2)
								{
									string tagColumnValues = VISDK.GetTagColumnValues(ref VISDK.cisAssociatedTag, "VirtualMachine", item4["Name"].ToString(), summaryVM);
									dataGridvNetwork.Rows[num].Cells["vNetwork_tags_" + item4["Name"].ToString()].Value = tagColumnValues;
								}
							}
							catch (Exception ex4)
							{
								if (logger.IsDebugEnabled)
								{
									logger.Debug(currentMethod.Name + " Handled Exception: during setting tag value " + ex4.Message);
								}
							}
						}
						dataGridvNetwork.Rows[num].Cells["vNetworkUUID"].Value = VISDK.GetVmInventoryProperty(i, "config.instanceUuid");
						dataGridvNetwork.Rows[num].Cells["vNetworkObjectID"].Value = VISDK.GetSummaryVM(i);
						dataGridvNetwork.Rows[num].Cells["vNetworkInstanceUUID"].Value = InstanceUUID;
						dataGridvNetwork.Rows[num].Cells["vNetworkVISDKServer"].Value = VISDK.Server;
						num++;
					}
					num5++;
				}
			}
			catch
			{
			}
		}
		dataGridvNetwork.AutoResizeColumns();
		string columnName = "vNetworkSortColumn";
		if (dataGridvNetwork.SortedColumn != null)
		{
			columnName = dataGridvNetwork.SortedColumn.Name;
		}
		dataGridvNetwork.Sort(dataGridvNetwork.Columns[columnName], ListSortDirection.Ascending);
		StatusGridCounter.Text = num + " rows";
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Number of rows: " + num);
		}
		Telemetry.AddEvent("RenderTab", "vNetwork", new Dictionary<string, string>
		{
			{
				"RenderTime",
				DateTime.UtcNow.Subtract(utcNow).ToString()
			},
			{
				"RowCount",
				num.ToString()
			}
		});
		Cursor = Cursors.Default;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void TabPagevCD()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("ShowTab", "vCD");
		DateTime utcNow = DateTime.UtcNow;
		if (!TabPagevCD_Refresh_data)
		{
			StatusGridCounter.Text = dataGridvCD.RowCount + " rows";
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
			return;
		}
		TabPagevCD_Refresh_data = false;
		Application.DoEvents();
		Cursor = Cursors.WaitCursor;
		dataGridvCD.Rows.Clear();
		int num = 0;
		if (!PreferencesClass.ExcludeAnnotations)
		{
			SortedList<string, string> customFieldHeaders = VISDK.GetCustomFieldHeaders("VirtualMachine");
			int num2 = dataGridvCD.Columns["vCDNotes"].Index;
			try
			{
				foreach (KeyValuePair<string, string> item in customFieldHeaders)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
					dataGridViewTextBoxColumn.Name = "CustomField" + item.Value;
					dataGridViewTextBoxColumn.HeaderText = item.Key;
					dataGridViewTextBoxColumn.Width = 200;
					if (!dataGridvCD.Columns.Contains("CustomField" + item.Value))
					{
						dataGridvCD.Columns.Insert(num2 + 1, dataGridViewTextBoxColumn);
					}
					num2++;
				}
			}
			catch (Exception ex)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding CustomFieldHeaders " + ex.Message);
				}
			}
		}
		if (!PreferencesClass.ExcludeTags)
		{
			try
			{
				List<Hashtable> tagColumnNames = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "VirtualMachine");
				int num3 = dataGridvCD.Columns["vCDDatacenter"].Index - 1;
				foreach (Hashtable item2 in tagColumnNames)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
					if (!dataGridvCD.Columns.Contains("vCD_tags_" + item2["Name"].ToString()))
					{
						dataGridViewTextBoxColumn2.Name = "vCD_tags_" + item2["Name"].ToString();
						dataGridViewTextBoxColumn2.HeaderText = item2["Name"].ToString();
						dataGridViewTextBoxColumn2.ToolTipText = item2["Desc"].ToString();
						dataGridViewTextBoxColumn2.Width = 200;
						dataGridvCD.Columns.Insert(num3 + 1, dataGridViewTextBoxColumn2);
						num3++;
					}
				}
			}
			catch (Exception ex2)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding tags " + ex2.Message);
				}
			}
		}
		StatusFilter.Visible = FilterClass.ShowFilterIcon();
		for (int i = 0; i < VISDK.InventoryCounter; i++)
		{
			if (FilterClass.ShowFilterIcon() && (!FilterClass.DisplayVirtualMachine(i) || !FilterClass.DisplayPowerstate(i) || !FilterClass.DisplayDataCenter(VISDK.GetDatacenterName(VISDK.GetSummaryRuntimeHost(i))) || !FilterClass.DisplayCluster(VISDK.GetClusterName(VISDK.GetSummaryRuntimeHost(i))) || !FilterClass.DisplayHost(VISDK.GetHostName(VISDK.GetSummaryRuntimeHost(i))) || !FilterClass.DisplayVMName(i) || !FilterClass.DisplayNotes(i)))
			{
				continue;
			}
			VirtualDevice[] deviceInfo = VISDK.GetDeviceInfo(i);
			int num4 = 0;
			while (deviceInfo != null && num4 < deviceInfo.Length)
			{
				if (deviceInfo[num4] is VirtualCdrom)
				{
					VirtualCdrom virtualCdrom = (VirtualCdrom)deviceInfo[num4];
					dataGridvCD.Rows.Add();
					string vmInventoryProperty = VISDK.GetVmInventoryProperty(i, "summary.config.name");
					if (logger.IsInfoEnabled)
					{
						logger.Info(currentMethod.Name + " Add Row for CD: " + vmInventoryProperty);
					}
					dataGridvCD.Rows[num].Cells["vCDVMName"].Value = vmInventoryProperty;
					dataGridvCD.Rows[num].Cells["vCDPowerstate"].Value = VISDK.GetVmInventoryProperty(i, "summary.runtime.powerState");
					dataGridvCD.Rows[num].Cells["vCDTemplate"].Value = VISDK.GetVmInventoryProperty(i, "summary.config.template");
					bool sRMPlaceholderBool = VISDK.GetSRMPlaceholderBool(i);
					dataGridvCD.Rows[num].Cells["vCDSRMPlaceholder"].Value = sRMPlaceholderBool.ToString();
					dataGridvCD.Rows[num].Cells["vCDLabel"].Value = virtualCdrom.deviceInfo.label;
					dataGridvCD.Rows[num].Cells["vCDConnected"].Value = virtualCdrom.connectable.connected.ToString();
					dataGridvCD.Rows[num].Cells["vCDStartup"].Value = virtualCdrom.connectable.startConnected.ToString();
					dataGridvCD.Rows[num].Cells["vCDSummary"].Value = virtualCdrom.deviceInfo.summary.ToString();
					dataGridvCD.Rows[num].Cells["vCDOS"].Value = VISDK.GetVmInventoryProperty(i, "summary.config.guestFullName");
					dataGridvCD.Rows[num].Cells["vCDOSTools"].Value = VISDK.GetVmInventoryProperty(i, "guest.guestFullName");
					dataGridvCD.Rows[num].Cells["vCDVMRef"].Value = i.ToString();
					dataGridvCD.Rows[num].Cells["vCDHost"].Value = VISDK.GetHostName(VISDK.GetSummaryRuntimeHost(i));
					dataGridvCD.Rows[num].Cells["vCDDatacenter"].Value = VISDK.GetDatacenterName(VISDK.GetSummaryRuntimeHost(i));
					dataGridvCD.Rows[num].Cells["vCDCluster"].Value = VISDK.GetClusterName(VISDK.GetSummaryRuntimeHost(i));
					dataGridvCD.Rows[num].Cells[0].ReadOnly = true;
					if (dataGridvCD.Rows[num].Cells["vCDPowerstate"].Value.ToString() == VirtualMachinePowerState.poweredOn.ToString() && dataGridvCD.Rows[num].Cells["vCDConnected"].Value.ToString() == "True")
					{
						dataGridvCD.Rows[num].Cells[0].ReadOnly = false;
					}
					if (!PreferencesClass.ExcludeAnnotations)
					{
						dataGridvCD.Rows[num].Cells["vCDNotes"].Value = VISDK.GetSummaryConfigAnnotation(i);
						CustomFieldValue[] customFieldValueForVM = VISDK.GetCustomFieldValueForVM(i);
						for (int num5 = customFieldValueForVM.Length; num5 > 0; num5--)
						{
							CustomFieldStringValue customFieldStringValue = (CustomFieldStringValue)customFieldValueForVM.GetValue(num5 - 1);
							try
							{
								dataGridvCD.Rows[num].Cells["CustomField" + customFieldStringValue.key].Value = customFieldStringValue.value;
							}
							catch (Exception ex3)
							{
								if (logger.IsDebugEnabled)
								{
									logger.Debug(currentMethod.Name + " Handled Exception: during setting tag value " + ex3.Message);
								}
							}
						}
					}
					if (!PreferencesClass.ExcludeTags)
					{
						try
						{
							List<Hashtable> tagColumnNames2 = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "VirtualMachine");
							string summaryVM = VISDK.GetSummaryVM(i);
							foreach (Hashtable item3 in tagColumnNames2)
							{
								string tagColumnValues = VISDK.GetTagColumnValues(ref VISDK.cisAssociatedTag, "VirtualMachine", item3["Name"].ToString(), summaryVM);
								dataGridvCD.Rows[num].Cells["vCD_tags_" + item3["Name"].ToString()].Value = tagColumnValues;
							}
						}
						catch (Exception ex4)
						{
							if (logger.IsDebugEnabled)
							{
								logger.Debug(currentMethod.Name + " Handled Exception: during setting value to custom field " + ex4.Message);
							}
						}
					}
					if (PreferencesClass.vInfoShowFolder || CommandToExecute != null)
					{
						if (logger.IsDebugEnabled)
						{
							logger.Debug(currentMethod.Name + " Call GetVMFolder for VM " + vmInventoryProperty);
						}
						string vMFolder = VISDK.GetVMFolder(i);
						dataGridvCD.Rows[num].Cells["vCDFolder"].Value = vMFolder;
					}
					dataGridvCD.Rows[num].Cells["vCDUUID"].Value = VISDK.GetVmInventoryProperty(i, "config.instanceUuid");
					dataGridvCD.Rows[num].Cells["vCDObjectID"].Value = VISDK.GetSummaryVM(i);
					dataGridvCD.Rows[num].Cells["vCDInstanceUUID"].Value = InstanceUUID;
					dataGridvCD.Rows[num].Cells["vCDVISDKServer"].Value = VISDK.Server;
					num++;
				}
				num4++;
			}
		}
		dataGridvCD.AutoResizeColumns();
		string columnName = "vCDVMName";
		if (dataGridvCD.SortedColumn != null)
		{
			columnName = dataGridvCD.SortedColumn.Name;
		}
		dataGridvCD.Sort(dataGridvCD.Columns[columnName], ListSortDirection.Ascending);
		StatusGridCounter.Text = num + " rows";
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Number of rows: " + num);
		}
		Telemetry.AddEvent("RenderTab", "vCD", new Dictionary<string, string>
		{
			{
				"RenderTime",
				DateTime.UtcNow.Subtract(utcNow).ToString()
			},
			{
				"RowCount",
				num.ToString()
			}
		});
		Cursor = Cursors.Default;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void dataGridvCD_CellContentClick(object sender, DataGridViewCellEventArgs e)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		try
		{
			if (e.ColumnIndex == 0 && dataGridvCD.Rows[e.RowIndex].Cells[e.ColumnIndex].ReadOnly)
			{
				UserNotification.ReportWarning("Due to the powerstate and/or connected value it's not possible to select this row!");
			}
		}
		catch
		{
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void buttonDisconnectCD_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuAction", "DisconnectCD");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		for (int i = 0; i < dataGridvCD.RowCount; i++)
		{
			if (!((string)dataGridvCD.Rows[i].Cells[0].Value == "1"))
			{
				continue;
			}
			string text = dataGridvCD.Rows[i].Cells["vCDLabel"].Value.ToString();
			int num = Convert.ToInt16(dataGridvCD.Rows[i].Cells["vCDVMRef"].Value.ToString());
			ManagedObjectReference managedObjectReference = null;
			managedObjectReference = VISDK.Inventory[num].obj;
			VirtualDevice[] deviceInfo = VISDK.GetDeviceInfo(num);
			for (int j = 0; j < deviceInfo.Length; j++)
			{
				if (!(deviceInfo[j].deviceInfo.label == text))
				{
					continue;
				}
				VirtualDeviceConfigSpec[] array = new VirtualDeviceConfigSpec[1]
				{
					new VirtualDeviceConfigSpec()
				};
				array[0].device = deviceInfo[j];
				array[0].device.connectable.connected = false;
				array[0].operation = VirtualDeviceConfigSpecOperation.edit;
				array[0].operationSpecified = true;
				VirtualMachineConfigSpec virtualMachineConfigSpec = new VirtualMachineConfigSpec();
				virtualMachineConfigSpec.deviceChange = array;
				try
				{
					VISDK._service.ReconfigVM_Task(managedObjectReference, virtualMachineConfigSpec);
					MessageBox.Show("ReconfigVM_Task(s) Started!", "RVTools", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				}
				catch (Exception ex)
				{
					if (ex.Message == "fault.RestrictedVersion.summary")
					{
						UserNotification.ReportWarning("VMware does not allow any type of remote management to manage the free version of ESXi.");
					}
				}
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void TabPagevUSB()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("ShowTab", "vUSB");
		DateTime utcNow = DateTime.UtcNow;
		if (!TabPagevUSB_Refresh_data)
		{
			StatusGridCounter.Text = dataGridvUSB.RowCount + " rows";
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
			return;
		}
		TabPagevUSB_Refresh_data = false;
		Application.DoEvents();
		Cursor = Cursors.WaitCursor;
		dataGridvUSB.Rows.Clear();
		int num = 0;
		if (!PreferencesClass.ExcludeAnnotations)
		{
			SortedList<string, string> customFieldHeaders = VISDK.GetCustomFieldHeaders("VirtualMachine");
			int num2 = dataGridvUSB.Columns["vUSBNotes"].Index;
			try
			{
				foreach (KeyValuePair<string, string> item in customFieldHeaders)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
					dataGridViewTextBoxColumn.Name = "CustomField" + item.Value;
					dataGridViewTextBoxColumn.HeaderText = item.Key;
					dataGridViewTextBoxColumn.Width = 200;
					if (!dataGridvUSB.Columns.Contains("CustomField" + item.Value))
					{
						dataGridvUSB.Columns.Insert(num2 + 1, dataGridViewTextBoxColumn);
					}
					num2++;
				}
			}
			catch (Exception ex)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding CustomFieldHeaders " + ex.Message);
				}
			}
		}
		if (!PreferencesClass.ExcludeTags)
		{
			try
			{
				List<Hashtable> tagColumnNames = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "VirtualMachine");
				int num3 = dataGridvUSB.Columns["vUSBDatacenter"].Index - 1;
				foreach (Hashtable item2 in tagColumnNames)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
					if (!dataGridvUSB.Columns.Contains("vUSB_tags_" + item2["Name"].ToString()))
					{
						dataGridViewTextBoxColumn2.Name = "vUSB_tags_" + item2["Name"].ToString();
						dataGridViewTextBoxColumn2.HeaderText = item2["Name"].ToString();
						dataGridViewTextBoxColumn2.ToolTipText = item2["Desc"].ToString();
						dataGridViewTextBoxColumn2.Width = 200;
						dataGridvUSB.Columns.Insert(num3 + 1, dataGridViewTextBoxColumn2);
						num3++;
					}
				}
			}
			catch (Exception ex2)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding tags " + ex2.Message);
				}
			}
		}
		StatusFilter.Visible = FilterClass.ShowFilterIcon();
		for (int i = 0; i < VISDK.InventoryCounter; i++)
		{
			if (FilterClass.ShowFilterIcon() && (!FilterClass.DisplayVirtualMachine(i) || !FilterClass.DisplayPowerstate(i) || !FilterClass.DisplayDataCenter(VISDK.GetDatacenterName(VISDK.GetSummaryRuntimeHost(i))) || !FilterClass.DisplayCluster(VISDK.GetClusterName(VISDK.GetSummaryRuntimeHost(i))) || !FilterClass.DisplayHost(VISDK.GetHostName(VISDK.GetSummaryRuntimeHost(i))) || !FilterClass.DisplayVMName(i) || !FilterClass.DisplayNotes(i)))
			{
				continue;
			}
			VirtualDevice[] deviceInfo = VISDK.GetDeviceInfo(i);
			VirtualUSBController virtualUSBController = null;
			int num4 = 0;
			while (deviceInfo != null && num4 < deviceInfo.Length)
			{
				if (deviceInfo[num4] is VirtualUSB)
				{
					VirtualUSB virtualUSB = (VirtualUSB)deviceInfo[num4];
					try
					{
						VirtualDevice[] array = deviceInfo;
						foreach (VirtualDevice virtualDevice in array)
						{
							if (virtualDevice.key == virtualUSB.controllerKey)
							{
								virtualUSBController = (VirtualUSBController)virtualDevice;
							}
						}
					}
					catch
					{
					}
					dataGridvUSB.Rows.Add();
					string vmInventoryProperty = VISDK.GetVmInventoryProperty(i, "summary.config.name");
					if (logger.IsInfoEnabled)
					{
						logger.Info(currentMethod.Name + " Add Row for USB: " + vmInventoryProperty);
					}
					dataGridvUSB.Rows[num].Cells["vUSBVMname"].Value = vmInventoryProperty;
					dataGridvUSB.Rows[num].Cells["vUSBPowerstate"].Value = VISDK.GetVmInventoryProperty(i, "summary.runtime.powerState");
					dataGridvUSB.Rows[num].Cells["vUSBTemplate"].Value = VISDK.GetVmInventoryProperty(i, "summary.config.template");
					bool sRMPlaceholderBool = VISDK.GetSRMPlaceholderBool(i);
					dataGridvUSB.Rows[num].Cells["vUSBSRMPlaceholder"].Value = sRMPlaceholderBool.ToString();
					dataGridvUSB.Rows[num].Cells["vUSBLabel"].Value = virtualUSB.deviceInfo.label;
					dataGridvUSB.Rows[num].Cells["vUSBConnected"].Value = virtualUSB.connected.ToString();
					dataGridvUSB.Rows[num].Cells["vUSBSummary"].Value = virtualUSB.deviceInfo.summary;
					try
					{
						dataGridvUSB.Rows[num].Cells["vUSBEHCIEnabled"].Value = virtualUSBController.ehciEnabled.ToString();
					}
					catch
					{
					}
					try
					{
						dataGridvUSB.Rows[num].Cells["vUSBBusNumber"].Value = virtualUSBController.busNumber;
					}
					catch
					{
					}
					try
					{
						dataGridvUSB.Rows[num].Cells["vUSBUnitNumber"].Value = virtualUSBController.unitNumber;
					}
					catch
					{
					}
					try
					{
						dataGridvUSB.Rows[num].Cells["vUSBAutoConnect"].Value = virtualUSBController.autoConnectDevices.ToString();
					}
					catch
					{
					}
					string text = null;
					try
					{
						string[] speed = virtualUSB.speed;
						foreach (string text2 in speed)
						{
							text = ((text != null) ? (text + ", " + text2) : text2);
						}
					}
					catch
					{
					}
					dataGridvUSB.Rows[num].Cells["vUSBSpeed"].Value = text;
					string text3 = null;
					try
					{
						string[] speed = virtualUSB.family;
						foreach (string text4 in speed)
						{
							text3 = ((text3 != null) ? (text3 + ", " + text4) : text4);
						}
					}
					catch
					{
					}
					dataGridvUSB.Rows[num].Cells["vUSBFamily"].Value = text3;
					dataGridvUSB.Rows[num].Cells["vUSBOS"].Value = VISDK.GetVmInventoryProperty(i, "summary.config.guestFullName");
					dataGridvUSB.Rows[num].Cells["vUSBOSTools"].Value = VISDK.GetVmInventoryProperty(i, "guest.guestFullName");
					dataGridvUSB.Rows[num].Cells["vUSBVMRef"].Value = i.ToString();
					dataGridvUSB.Rows[num].Cells["vUSBHost"].Value = VISDK.GetHostName(VISDK.GetSummaryRuntimeHost(i));
					dataGridvUSB.Rows[num].Cells["vUSBDatacenter"].Value = VISDK.GetDatacenterName(VISDK.GetSummaryRuntimeHost(i));
					dataGridvUSB.Rows[num].Cells["vUSBCluster"].Value = VISDK.GetClusterName(VISDK.GetSummaryRuntimeHost(i));
					dataGridvUSB.Rows[num].Cells[0].ReadOnly = true;
					if (dataGridvUSB.Rows[num].Cells["vUSBPowerstate"].Value.ToString() == VirtualMachinePowerState.poweredOn.ToString() && dataGridvUSB.Rows[num].Cells["vUSBConnected"].Value.ToString() == "True")
					{
						dataGridvUSB.Rows[num].Cells[0].ReadOnly = false;
					}
					if (!PreferencesClass.ExcludeAnnotations)
					{
						dataGridvUSB.Rows[num].Cells["vUSBNotes"].Value = VISDK.GetSummaryConfigAnnotation(i);
						CustomFieldValue[] customFieldValueForVM = VISDK.GetCustomFieldValueForVM(i);
						for (int num5 = customFieldValueForVM.Length; num5 > 0; num5--)
						{
							CustomFieldStringValue customFieldStringValue = (CustomFieldStringValue)customFieldValueForVM.GetValue(num5 - 1);
							try
							{
								dataGridvUSB.Rows[num].Cells["CustomField" + customFieldStringValue.key].Value = customFieldStringValue.value;
							}
							catch (Exception ex3)
							{
								if (logger.IsDebugEnabled)
								{
									logger.Debug(currentMethod.Name + " Handled Exception: during setting value to custom field " + ex3.Message);
								}
							}
						}
					}
					if (!PreferencesClass.ExcludeTags)
					{
						try
						{
							List<Hashtable> tagColumnNames2 = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "VirtualMachine");
							string summaryVM = VISDK.GetSummaryVM(i);
							foreach (Hashtable item3 in tagColumnNames2)
							{
								string tagColumnValues = VISDK.GetTagColumnValues(ref VISDK.cisAssociatedTag, "VirtualMachine", item3["Name"].ToString(), summaryVM);
								dataGridvUSB.Rows[num].Cells["vUSB_tags_" + item3["Name"].ToString()].Value = tagColumnValues;
							}
						}
						catch (Exception ex4)
						{
							if (logger.IsDebugEnabled)
							{
								logger.Debug(currentMethod.Name + " Handled Exception: during setting tag value " + ex4.Message);
							}
						}
					}
					if (PreferencesClass.vInfoShowFolder || CommandToExecute != null)
					{
						if (logger.IsDebugEnabled)
						{
							logger.Debug(currentMethod.Name + " Call GetVMFolder for VM " + vmInventoryProperty);
						}
						string vMFolder = VISDK.GetVMFolder(i);
						dataGridvUSB.Rows[num].Cells["vUSBFolder"].Value = vMFolder;
					}
					dataGridvUSB.Rows[num].Cells["vUSBUUID"].Value = VISDK.GetVmInventoryProperty(i, "config.instanceUuid");
					dataGridvUSB.Rows[num].Cells["vUSBObjectID"].Value = VISDK.GetSummaryVM(i);
					dataGridvUSB.Rows[num].Cells["vUSBInstanceUUID"].Value = InstanceUUID;
					dataGridvUSB.Rows[num].Cells["vUSBVISDKServer"].Value = VISDK.Server;
					num++;
				}
				num4++;
			}
		}
		dataGridvUSB.AutoResizeColumns();
		string columnName = "vUSBVMName";
		if (dataGridvUSB.SortedColumn != null)
		{
			columnName = dataGridvUSB.SortedColumn.Name;
		}
		dataGridvUSB.Sort(dataGridvUSB.Columns[columnName], ListSortDirection.Ascending);
		StatusGridCounter.Text = num + " rows";
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Number of rows: " + num);
		}
		Telemetry.AddEvent("RenderTab", "vUSB", new Dictionary<string, string>
		{
			{
				"RenderTime",
				DateTime.UtcNow.Subtract(utcNow).ToString()
			},
			{
				"RowCount",
				num.ToString()
			}
		});
		Cursor = Cursors.Default;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void dataGridvUSB_CellContentClick(object sender, DataGridViewCellEventArgs e)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		try
		{
			if (e.ColumnIndex == 0 && dataGridvUSB.Rows[e.RowIndex].Cells[e.ColumnIndex].ReadOnly)
			{
				UserNotification.ReportWarning("Due to the powerstate and/or connected value it's not possible to select this row!");
			}
		}
		catch
		{
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void buttonDisconnectUSB_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuAction", "DisconnectUSB");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		for (int i = 0; i < dataGridvUSB.RowCount; i++)
		{
			if (!((string)dataGridvUSB.Rows[i].Cells[0].Value == "1"))
			{
				continue;
			}
			string text = dataGridvUSB.Rows[i].Cells["vUSBLabel"].Value.ToString();
			int num = Convert.ToInt16(dataGridvUSB.Rows[i].Cells["vUSBVMRef"].Value.ToString());
			ManagedObjectReference managedObjectReference = null;
			managedObjectReference = VISDK.Inventory[num].obj;
			VirtualDevice[] deviceInfo = VISDK.GetDeviceInfo(num);
			for (int j = 0; j < deviceInfo.Length; j++)
			{
				if (!(deviceInfo[j].deviceInfo.label == text))
				{
					continue;
				}
				VirtualDeviceConfigSpec[] array = new VirtualDeviceConfigSpec[1]
				{
					new VirtualDeviceConfigSpec()
				};
				array[0].device = deviceInfo[j];
				array[0].operation = VirtualDeviceConfigSpecOperation.remove;
				array[0].operationSpecified = true;
				VirtualMachineConfigSpec virtualMachineConfigSpec = new VirtualMachineConfigSpec();
				virtualMachineConfigSpec.deviceChange = array;
				try
				{
					VISDK._service.ReconfigVM_Task(managedObjectReference, virtualMachineConfigSpec);
					MessageBox.Show("ReconfigVM_Task(s) Started!", "RVTools", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				}
				catch (Exception ex)
				{
					if (ex.Message == "fault.RestrictedVersion.summary")
					{
						UserNotification.ReportWarning("VMware does not allow any type of remote management to manage the free version of ESXi.");
					}
				}
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void TabPagevSnapshot()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("ShowTab", "vSnapshot");
		DateTime utcNow = DateTime.UtcNow;
		if (!TabPagevSnapshot_Refresh_data)
		{
			StatusGridCounter.Text = dataGridvSnapshot.RowCount + " rows";
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
			return;
		}
		TabPagevSnapshot_Refresh_data = false;
		Application.DoEvents();
		Cursor = Cursors.WaitCursor;
		dataGridvSnapshot.Rows.Clear();
		int num = 0;
		if (!PreferencesClass.ExcludeAnnotations)
		{
			SortedList<string, string> customFieldHeaders = VISDK.GetCustomFieldHeaders("VirtualMachine");
			int num2 = dataGridvSnapshot.Columns["vSnapshotNotes"].Index;
			try
			{
				foreach (KeyValuePair<string, string> item2 in customFieldHeaders)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
					dataGridViewTextBoxColumn.Name = "CustomField" + item2.Value;
					dataGridViewTextBoxColumn.HeaderText = item2.Key;
					dataGridViewTextBoxColumn.Width = 200;
					if (!dataGridvSnapshot.Columns.Contains("CustomField" + item2.Value))
					{
						dataGridvSnapshot.Columns.Insert(num2 + 1, dataGridViewTextBoxColumn);
					}
					num2++;
				}
			}
			catch (Exception ex)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding CustomFieldHeaders " + ex.Message);
				}
			}
		}
		if (!PreferencesClass.ExcludeTags)
		{
			try
			{
				List<Hashtable> tagColumnNames = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "VirtualMachine");
				int num3 = dataGridvSnapshot.Columns["vSnapshotDatacenter"].Index - 1;
				foreach (Hashtable item3 in tagColumnNames)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
					if (!dataGridvSnapshot.Columns.Contains("vSnapshot_tags_" + item3["Name"].ToString()))
					{
						dataGridViewTextBoxColumn2.Name = "vSnapshot_tags_" + item3["Name"].ToString();
						dataGridViewTextBoxColumn2.HeaderText = item3["Name"].ToString();
						dataGridViewTextBoxColumn2.ToolTipText = item3["Desc"].ToString();
						dataGridViewTextBoxColumn2.Width = 200;
						dataGridvSnapshot.Columns.Insert(num3 + 1, dataGridViewTextBoxColumn2);
						num3++;
					}
				}
			}
			catch (Exception ex2)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding tags " + ex2.Message);
				}
			}
		}
		StatusFilter.Visible = FilterClass.ShowFilterIcon();
		for (int i = 0; i < VISDK.InventoryCounter; i++)
		{
			if (FilterClass.ShowFilterIcon() && (!FilterClass.DisplayVirtualMachine(i) || !FilterClass.DisplayPowerstate(i) || !FilterClass.DisplayDataCenter(VISDK.GetDatacenterName(VISDK.GetSummaryRuntimeHost(i))) || !FilterClass.DisplayCluster(VISDK.GetClusterName(VISDK.GetSummaryRuntimeHost(i))) || !FilterClass.DisplayHost(VISDK.GetHostName(VISDK.GetSummaryRuntimeHost(i))) || !FilterClass.DisplayVMName(i) || !FilterClass.DisplayNotes(i)))
			{
				continue;
			}
			VirtualMachineSnapshotTree[] snapShotTree = VISDK.GetSnapShotTree(i);
			_SnapShots.Clear();
			if (snapShotTree != null)
			{
				VirtualMachineSnapshotTree[] array = snapShotTree;
				foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree in array)
				{
					_SnapShots.Add(virtualMachineSnapshotTree);
					if (virtualMachineSnapshotTree.childSnapshotList == null)
					{
						continue;
					}
					VirtualMachineSnapshotTree[] childSnapshotList = virtualMachineSnapshotTree.childSnapshotList;
					foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree2 in childSnapshotList)
					{
						_SnapShots.Add(virtualMachineSnapshotTree2);
						if (virtualMachineSnapshotTree2.childSnapshotList == null)
						{
							continue;
						}
						VirtualMachineSnapshotTree[] childSnapshotList2 = virtualMachineSnapshotTree2.childSnapshotList;
						foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree3 in childSnapshotList2)
						{
							_SnapShots.Add(virtualMachineSnapshotTree3);
							if (virtualMachineSnapshotTree3.childSnapshotList == null)
							{
								continue;
							}
							VirtualMachineSnapshotTree[] childSnapshotList3 = virtualMachineSnapshotTree3.childSnapshotList;
							foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree4 in childSnapshotList3)
							{
								_SnapShots.Add(virtualMachineSnapshotTree4);
								if (virtualMachineSnapshotTree4.childSnapshotList == null)
								{
									continue;
								}
								VirtualMachineSnapshotTree[] childSnapshotList4 = virtualMachineSnapshotTree4.childSnapshotList;
								foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree5 in childSnapshotList4)
								{
									_SnapShots.Add(virtualMachineSnapshotTree5);
									if (virtualMachineSnapshotTree5.childSnapshotList == null)
									{
										continue;
									}
									VirtualMachineSnapshotTree[] childSnapshotList5 = virtualMachineSnapshotTree5.childSnapshotList;
									foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree6 in childSnapshotList5)
									{
										_SnapShots.Add(virtualMachineSnapshotTree6);
										if (virtualMachineSnapshotTree6.childSnapshotList == null)
										{
											continue;
										}
										VirtualMachineSnapshotTree[] childSnapshotList6 = virtualMachineSnapshotTree6.childSnapshotList;
										foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree7 in childSnapshotList6)
										{
											_SnapShots.Add(virtualMachineSnapshotTree7);
											if (virtualMachineSnapshotTree7.childSnapshotList == null)
											{
												continue;
											}
											VirtualMachineSnapshotTree[] childSnapshotList7 = virtualMachineSnapshotTree7.childSnapshotList;
											foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree8 in childSnapshotList7)
											{
												_SnapShots.Add(virtualMachineSnapshotTree8);
												if (virtualMachineSnapshotTree8.childSnapshotList == null)
												{
													continue;
												}
												VirtualMachineSnapshotTree[] childSnapshotList8 = virtualMachineSnapshotTree8.childSnapshotList;
												foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree9 in childSnapshotList8)
												{
													_SnapShots.Add(virtualMachineSnapshotTree9);
													if (virtualMachineSnapshotTree9.childSnapshotList == null)
													{
														continue;
													}
													VirtualMachineSnapshotTree[] childSnapshotList9 = virtualMachineSnapshotTree9.childSnapshotList;
													foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree10 in childSnapshotList9)
													{
														_SnapShots.Add(virtualMachineSnapshotTree10);
														if (virtualMachineSnapshotTree10.childSnapshotList == null)
														{
															continue;
														}
														VirtualMachineSnapshotTree[] childSnapshotList10 = virtualMachineSnapshotTree10.childSnapshotList;
														foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree11 in childSnapshotList10)
														{
															_SnapShots.Add(virtualMachineSnapshotTree11);
															if (virtualMachineSnapshotTree11.childSnapshotList == null)
															{
																continue;
															}
															VirtualMachineSnapshotTree[] childSnapshotList11 = virtualMachineSnapshotTree11.childSnapshotList;
															foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree12 in childSnapshotList11)
															{
																_SnapShots.Add(virtualMachineSnapshotTree12);
																if (virtualMachineSnapshotTree12.childSnapshotList == null)
																{
																	continue;
																}
																VirtualMachineSnapshotTree[] childSnapshotList12 = virtualMachineSnapshotTree12.childSnapshotList;
																foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree13 in childSnapshotList12)
																{
																	_SnapShots.Add(virtualMachineSnapshotTree13);
																	if (virtualMachineSnapshotTree13.childSnapshotList == null)
																	{
																		continue;
																	}
																	VirtualMachineSnapshotTree[] childSnapshotList13 = virtualMachineSnapshotTree13.childSnapshotList;
																	foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree14 in childSnapshotList13)
																	{
																		_SnapShots.Add(virtualMachineSnapshotTree14);
																		if (virtualMachineSnapshotTree14.childSnapshotList == null)
																		{
																			continue;
																		}
																		VirtualMachineSnapshotTree[] childSnapshotList14 = virtualMachineSnapshotTree14.childSnapshotList;
																		foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree15 in childSnapshotList14)
																		{
																			_SnapShots.Add(virtualMachineSnapshotTree15);
																			if (virtualMachineSnapshotTree15.childSnapshotList == null)
																			{
																				continue;
																			}
																			VirtualMachineSnapshotTree[] childSnapshotList15 = virtualMachineSnapshotTree15.childSnapshotList;
																			foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree16 in childSnapshotList15)
																			{
																				_SnapShots.Add(virtualMachineSnapshotTree16);
																				if (virtualMachineSnapshotTree16.childSnapshotList == null)
																				{
																					continue;
																				}
																				VirtualMachineSnapshotTree[] childSnapshotList16 = virtualMachineSnapshotTree16.childSnapshotList;
																				foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree17 in childSnapshotList16)
																				{
																					_SnapShots.Add(virtualMachineSnapshotTree17);
																					if (virtualMachineSnapshotTree17.childSnapshotList == null)
																					{
																						continue;
																					}
																					VirtualMachineSnapshotTree[] childSnapshotList17 = virtualMachineSnapshotTree17.childSnapshotList;
																					foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree18 in childSnapshotList17)
																					{
																						_SnapShots.Add(virtualMachineSnapshotTree18);
																						if (virtualMachineSnapshotTree18.childSnapshotList == null)
																						{
																							continue;
																						}
																						VirtualMachineSnapshotTree[] childSnapshotList18 = virtualMachineSnapshotTree18.childSnapshotList;
																						foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree19 in childSnapshotList18)
																						{
																							_SnapShots.Add(virtualMachineSnapshotTree19);
																							if (virtualMachineSnapshotTree19.childSnapshotList == null)
																							{
																								continue;
																							}
																							VirtualMachineSnapshotTree[] childSnapshotList19 = virtualMachineSnapshotTree19.childSnapshotList;
																							foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree20 in childSnapshotList19)
																							{
																								_SnapShots.Add(virtualMachineSnapshotTree20);
																								if (virtualMachineSnapshotTree20.childSnapshotList == null)
																								{
																									continue;
																								}
																								VirtualMachineSnapshotTree[] childSnapshotList20 = virtualMachineSnapshotTree20.childSnapshotList;
																								foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree21 in childSnapshotList20)
																								{
																									_SnapShots.Add(virtualMachineSnapshotTree21);
																									if (virtualMachineSnapshotTree21.childSnapshotList == null)
																									{
																										continue;
																									}
																									VirtualMachineSnapshotTree[] childSnapshotList21 = virtualMachineSnapshotTree21.childSnapshotList;
																									foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree22 in childSnapshotList21)
																									{
																										_SnapShots.Add(virtualMachineSnapshotTree22);
																										if (virtualMachineSnapshotTree22.childSnapshotList == null)
																										{
																											continue;
																										}
																										VirtualMachineSnapshotTree[] childSnapshotList22 = virtualMachineSnapshotTree22.childSnapshotList;
																										foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree23 in childSnapshotList22)
																										{
																											_SnapShots.Add(virtualMachineSnapshotTree23);
																											if (virtualMachineSnapshotTree23.childSnapshotList == null)
																											{
																												continue;
																											}
																											VirtualMachineSnapshotTree[] childSnapshotList23 = virtualMachineSnapshotTree23.childSnapshotList;
																											foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree24 in childSnapshotList23)
																											{
																												_SnapShots.Add(virtualMachineSnapshotTree24);
																												if (virtualMachineSnapshotTree24.childSnapshotList == null)
																												{
																													continue;
																												}
																												VirtualMachineSnapshotTree[] childSnapshotList24 = virtualMachineSnapshotTree24.childSnapshotList;
																												foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree25 in childSnapshotList24)
																												{
																													_SnapShots.Add(virtualMachineSnapshotTree25);
																													if (virtualMachineSnapshotTree25.childSnapshotList == null)
																													{
																														continue;
																													}
																													VirtualMachineSnapshotTree[] childSnapshotList25 = virtualMachineSnapshotTree25.childSnapshotList;
																													foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree26 in childSnapshotList25)
																													{
																														_SnapShots.Add(virtualMachineSnapshotTree26);
																														if (virtualMachineSnapshotTree26.childSnapshotList == null)
																														{
																															continue;
																														}
																														VirtualMachineSnapshotTree[] childSnapshotList26 = virtualMachineSnapshotTree26.childSnapshotList;
																														foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree27 in childSnapshotList26)
																														{
																															_SnapShots.Add(virtualMachineSnapshotTree27);
																															if (virtualMachineSnapshotTree27.childSnapshotList == null)
																															{
																																continue;
																															}
																															VirtualMachineSnapshotTree[] childSnapshotList27 = virtualMachineSnapshotTree27.childSnapshotList;
																															foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree28 in childSnapshotList27)
																															{
																																_SnapShots.Add(virtualMachineSnapshotTree28);
																																if (virtualMachineSnapshotTree28.childSnapshotList == null)
																																{
																																	continue;
																																}
																																VirtualMachineSnapshotTree[] childSnapshotList28 = virtualMachineSnapshotTree28.childSnapshotList;
																																foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree29 in childSnapshotList28)
																																{
																																	_SnapShots.Add(virtualMachineSnapshotTree29);
																																	if (virtualMachineSnapshotTree29.childSnapshotList == null)
																																	{
																																		continue;
																																	}
																																	VirtualMachineSnapshotTree[] childSnapshotList29 = virtualMachineSnapshotTree29.childSnapshotList;
																																	foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree30 in childSnapshotList29)
																																	{
																																		_SnapShots.Add(virtualMachineSnapshotTree30);
																																		if (virtualMachineSnapshotTree30.childSnapshotList == null)
																																		{
																																			continue;
																																		}
																																		VirtualMachineSnapshotTree[] childSnapshotList30 = virtualMachineSnapshotTree30.childSnapshotList;
																																		foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree31 in childSnapshotList30)
																																		{
																																			_SnapShots.Add(virtualMachineSnapshotTree31);
																																			if (virtualMachineSnapshotTree31.childSnapshotList == null)
																																			{
																																				continue;
																																			}
																																			VirtualMachineSnapshotTree[] childSnapshotList31 = virtualMachineSnapshotTree31.childSnapshotList;
																																			foreach (VirtualMachineSnapshotTree virtualMachineSnapshotTree32 in childSnapshotList31)
																																			{
																																				_SnapShots.Add(virtualMachineSnapshotTree32);
																																				if (virtualMachineSnapshotTree32.childSnapshotList != null)
																																				{
																																					VirtualMachineSnapshotTree[] childSnapshotList32 = virtualMachineSnapshotTree32.childSnapshotList;
																																					foreach (VirtualMachineSnapshotTree item in childSnapshotList32)
																																					{
																																						_SnapShots.Add(item);
																																					}
																																				}
																																			}
																																		}
																																	}
																																}
																															}
																														}
																													}
																												}
																											}
																										}
																									}
																								}
																							}
																						}
																					}
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			foreach (VirtualMachineSnapshotTree snapShot in _SnapShots)
			{
				dataGridvSnapshot.Rows.Add();
				string vmInventoryProperty = VISDK.GetVmInventoryProperty(i, "summary.config.name");
				if (logger.IsInfoEnabled)
				{
					logger.Info(currentMethod.Name + " Add Row for Snapshot: " + vmInventoryProperty);
				}
				dataGridvSnapshot.Rows[num].Cells["vSnapshotVMName"].Value = vmInventoryProperty;
				dataGridvSnapshot.Rows[num].Cells["vSnapshotPowerState"].Value = VISDK.GetVmInventoryProperty(i, "summary.runtime.powerState");
				dataGridvSnapshot.Rows[num].Cells["vSnapshotName"].Value = snapShot.name;
				dataGridvSnapshot.Rows[num].Cells["vSnapshotDescription"].Value = snapShot.description;
				try
				{
					dataGridvSnapshot.Rows[num].Cells["vSnapshotCreateTime"].Value = snapShot.createTime.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
				}
				catch
				{
					dataGridvSnapshot.Rows[num].Cells["vSnapshotCreateTime"].Value = null;
				}
				dataGridvSnapshot.Rows[num].Cells["vSnapshotQuiesced"].Value = snapShot.quiesced;
				dataGridvSnapshot.Rows[num].Cells["vSnapshotState"].Value = snapShot.state.ToString();
				dataGridvSnapshot.Rows[num].Cells["vSnapshotOS"].Value = VISDK.GetVmInventoryProperty(i, "summary.config.guestFullName");
				dataGridvSnapshot.Rows[num].Cells["vSnapshotOSTools"].Value = VISDK.GetVmInventoryProperty(i, "guest.guestFullName");
				dataGridvSnapshot.Rows[num].Cells["vSnapshotHost"].Value = VISDK.GetHostName(VISDK.GetSummaryRuntimeHost(i));
				dataGridvSnapshot.Rows[num].Cells["vSnapshotDatacenter"].Value = VISDK.GetDatacenterName(VISDK.GetSummaryRuntimeHost(i));
				dataGridvSnapshot.Rows[num].Cells["vSnapshotCluster"].Value = VISDK.GetClusterName(VISDK.GetSummaryRuntimeHost(i));
				try
				{
					decimal num32 = VISDK.GetSnapshotTotalSize(i) / 1024;
					num32 /= 1024m;
					dataGridvSnapshot.Rows[num].Cells["vSnapshotSizeTotal"].Value = num32;
				}
				catch
				{
					dataGridvSnapshot.Rows[num].Cells["vSnapshotSizeTotal"].Value = 0;
				}
				VirtualMachineFileLayoutExFileInfo snapshotFileInfo = VISDK.GetSnapshotFileInfo(i, snapShot.snapshot.Value);
				if (snapshotFileInfo != null)
				{
					try
					{
						decimal num33 = snapshotFileInfo.size / 1024;
						num33 /= 1024m;
						dataGridvSnapshot.Rows[num].Cells["vSnapshotSizeVMSN"].Value = num33;
					}
					catch
					{
						dataGridvSnapshot.Rows[num].Cells["vSnapshotSizeVMSN"].Value = null;
					}
					dataGridvSnapshot.Rows[num].Cells["vSnapshotFilename"].Value = VISDK.GetFriendlyPath(snapshotFileInfo.name);
				}
				if (!PreferencesClass.ExcludeAnnotations)
				{
					dataGridvSnapshot.Rows[num].Cells["vSnapshotNotes"].Value = VISDK.GetSummaryConfigAnnotation(i);
					CustomFieldValue[] customFieldValueForVM = VISDK.GetCustomFieldValueForVM(i);
					for (int num34 = customFieldValueForVM.Length; num34 > 0; num34--)
					{
						CustomFieldStringValue customFieldStringValue = (CustomFieldStringValue)customFieldValueForVM.GetValue(num34 - 1);
						try
						{
							dataGridvSnapshot.Rows[num].Cells["CustomField" + customFieldStringValue.key].Value = customFieldStringValue.value;
						}
						catch (Exception ex3)
						{
							if (logger.IsDebugEnabled)
							{
								logger.Debug(currentMethod.Name + " Handled Exception: during setting value to custom field " + ex3.Message);
							}
						}
					}
				}
				if (!PreferencesClass.ExcludeTags)
				{
					try
					{
						List<Hashtable> tagColumnNames2 = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "VirtualMachine");
						string summaryVM = VISDK.GetSummaryVM(i);
						foreach (Hashtable item4 in tagColumnNames2)
						{
							string tagColumnValues = VISDK.GetTagColumnValues(ref VISDK.cisAssociatedTag, "VirtualMachine", item4["Name"].ToString(), summaryVM);
							dataGridvSnapshot.Rows[num].Cells["vSnapshot_tags_" + item4["Name"].ToString()].Value = tagColumnValues;
						}
					}
					catch (Exception ex4)
					{
						if (logger.IsDebugEnabled)
						{
							logger.Debug(currentMethod.Name + " Handled Exception: during setting tag value " + ex4.Message);
						}
					}
				}
				if (PreferencesClass.vInfoShowFolder || CommandToExecute != null)
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(currentMethod.Name + " Call GetVMFolder for VM " + vmInventoryProperty);
					}
					string vMFolder = VISDK.GetVMFolder(i);
					dataGridvSnapshot.Rows[num].Cells["vSnapshotFolder"].Value = vMFolder;
				}
				dataGridvSnapshot.Rows[num].Cells["vSnapshotUUID"].Value = VISDK.GetVmInventoryProperty(i, "config.instanceUuid");
				dataGridvSnapshot.Rows[num].Cells["vSnapshotObjectID"].Value = VISDK.GetSummaryVM(i);
				dataGridvSnapshot.Rows[num].Cells["vSnapshotInstanceUUID"].Value = InstanceUUID;
				dataGridvSnapshot.Rows[num].Cells["vSnapshotVISDKServer"].Value = VISDK.Server;
				num++;
			}
		}
		if (VISDK.APIVersion >= 400)
		{
			dataGridvSnapshot.Columns["vSnapshotSizeVMSN"].Visible = true;
			dataGridvSnapshot.Columns["vSnapshotSizeTotal"].Visible = true;
			dataGridvSnapshot.Columns["vSnapshotFilename"].Visible = true;
		}
		else
		{
			dataGridvSnapshot.Columns["vSnapshotSizeVMSN"].Visible = true;
			dataGridvSnapshot.Columns["vSnapshotSizeTotal"].Visible = true;
			dataGridvSnapshot.Columns["vSnapshotFilename"].Visible = true;
		}
		dataGridvSnapshot.AutoResizeColumns();
		string columnName = "vSnapshotVMName";
		if (dataGridvSnapshot.SortedColumn != null)
		{
			columnName = dataGridvSnapshot.SortedColumn.Name;
		}
		dataGridvSnapshot.Sort(dataGridvSnapshot.Columns[columnName], ListSortDirection.Ascending);
		StatusGridCounter.Text = num + " rows";
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Number of rows: " + num);
		}
		Telemetry.AddEvent("RenderTab", "vSnapshot", new Dictionary<string, string>
		{
			{
				"RenderTime",
				DateTime.UtcNow.Subtract(utcNow).ToString()
			},
			{
				"RowCount",
				num.ToString()
			}
		});
		Cursor = Cursors.Default;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void TabPagevTools()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("ShowTab", "vTools");
		DateTime utcNow = DateTime.UtcNow;
		if (!TabPagevTools_Refresh_data)
		{
			StatusGridCounter.Text = dataGridvTools.RowCount + " rows";
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
			return;
		}
		TabPagevTools_Refresh_data = false;
		Application.DoEvents();
		Cursor = Cursors.WaitCursor;
		dataGridvTools.Rows.Clear();
		int num = 0;
		if (!PreferencesClass.ExcludeAnnotations)
		{
			SortedList<string, string> customFieldHeaders = VISDK.GetCustomFieldHeaders("VirtualMachine");
			int num2 = dataGridvTools.Columns["vToolsNotes"].Index;
			try
			{
				foreach (KeyValuePair<string, string> item in customFieldHeaders)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
					dataGridViewTextBoxColumn.Name = "CustomField" + item.Value;
					dataGridViewTextBoxColumn.HeaderText = item.Key;
					dataGridViewTextBoxColumn.Width = 200;
					if (!dataGridvTools.Columns.Contains("CustomField" + item.Value))
					{
						dataGridvTools.Columns.Insert(num2 + 1, dataGridViewTextBoxColumn);
					}
					num2++;
				}
			}
			catch (Exception ex)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding CustomFieldHeaders " + ex.Message);
				}
			}
		}
		if (!PreferencesClass.ExcludeTags)
		{
			try
			{
				List<Hashtable> tagColumnNames = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "VirtualMachine");
				int num3 = dataGridvTools.Columns["vToolsDatacenter"].Index - 1;
				foreach (Hashtable item2 in tagColumnNames)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
					if (!dataGridvTools.Columns.Contains("vTools_tags_" + item2["Name"].ToString()))
					{
						dataGridViewTextBoxColumn2.Name = "vTools_tags_" + item2["Name"].ToString();
						dataGridViewTextBoxColumn2.HeaderText = item2["Name"].ToString();
						dataGridViewTextBoxColumn2.ToolTipText = item2["Desc"].ToString();
						dataGridViewTextBoxColumn2.Width = 200;
						dataGridvTools.Columns.Insert(num3 + 1, dataGridViewTextBoxColumn2);
						num3++;
					}
				}
			}
			catch (Exception ex2)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding tags " + ex2.Message);
				}
			}
		}
		StatusFilter.Visible = FilterClass.ShowFilterIcon();
		for (int i = 0; i < VISDK.InventoryCounter; i++)
		{
			if (FilterClass.ShowFilterIcon() && (!FilterClass.DisplayVirtualMachine(i) || !FilterClass.DisplayPowerstate(i) || !FilterClass.DisplayDataCenter(VISDK.GetDatacenterName(VISDK.GetSummaryRuntimeHost(i))) || !FilterClass.DisplayCluster(VISDK.GetClusterName(VISDK.GetSummaryRuntimeHost(i))) || !FilterClass.DisplayHost(VISDK.GetHostName(VISDK.GetSummaryRuntimeHost(i))) || !FilterClass.DisplayVMName(i) || !FilterClass.DisplayNotes(i)))
			{
				continue;
			}
			dataGridvTools.Rows.Add();
			string vmInventoryProperty = VISDK.GetVmInventoryProperty(i, "summary.config.name");
			if (logger.IsInfoEnabled)
			{
				logger.Info(currentMethod.Name + " Add Row for Tools: " + vmInventoryProperty);
			}
			dataGridvTools.Rows[num].Cells["vToolsVMName"].Value = vmInventoryProperty;
			string text = VISDK.GetVmInventoryProperty(i, "config.version");
			try
			{
				text = text.Replace("vmx-0", "");
				text = text.Replace("vmx-", "");
				text = text.Replace("vmx", "");
			}
			catch
			{
			}
			try
			{
				dataGridvTools.Rows[num].Cells["vToolsVMVersion"].Value = Convert.ToInt16(text);
			}
			catch
			{
				dataGridvTools.Rows[num].Cells["vToolsVMVersion"].Value = text;
			}
			dataGridvTools.Rows[num].Cells["vToolsTools"].Value = VISDK.GetVmInventoryProperty(i, "guest.toolsStatus");
			if (VISDK.GetVmInventoryProperty(i, "guest.toolsVersion") != null)
			{
				dataGridvTools.Rows[num].Cells["vToolsVersion"].Value = VISDK.GetInventoryPropertyInt(i, "guest.toolsVersion");
			}
			try
			{
				dataGridvTools.Rows[num].Cells["vToolsRequiredVersion"].Value = Convert.ToInt32(VISDK.GetOptionValueVM(i, "vmware.tools.requiredversion"));
			}
			catch
			{
			}
			if (VISDK.DisabledMethod(i, "UpgradeTools_Task"))
			{
				dataGridvTools.Rows[num].Cells["vToolsUpgradeable"].Value = "No";
				dataGridvTools.Rows[num].Cells[0].ReadOnly = true;
			}
			else
			{
				dataGridvTools.Rows[num].Cells["vToolsUpgradeable"].Value = "Yes";
			}
			dataGridvTools.Rows[num].Cells["vToolsTemplate"].Value = VISDK.GetVmInventoryProperty(i, "summary.config.template");
			bool sRMPlaceholderBool = VISDK.GetSRMPlaceholderBool(i);
			dataGridvTools.Rows[num].Cells["vToolsSRMPlaceholder"].Value = sRMPlaceholderBool.ToString();
			dataGridvTools.Rows[num].Cells["vToolsSyncTime"].Value = VISDK.GetVmInventoryProperty(i, "config.tools.syncTimeWithHost");
			dataGridvTools.Rows[num].Cells["vToolsUpgradePolicy"].Value = VISDK.GetVmInventoryProperty(i, "config.tools.toolsUpgradePolicy");
			dataGridvTools.Rows[num].Cells["vToolsPowerstate"].Value = VISDK.GetVmInventoryProperty(i, "summary.runtime.powerState");
			dataGridvTools.Rows[num].Cells["vToolsHost"].Value = VISDK.GetHostName(VISDK.GetSummaryRuntimeHost(i));
			dataGridvTools.Rows[num].Cells["vToolsDatacenter"].Value = VISDK.GetDatacenterName(VISDK.GetSummaryRuntimeHost(i));
			dataGridvTools.Rows[num].Cells["vToolsCluster"].Value = VISDK.GetClusterName(VISDK.GetSummaryRuntimeHost(i));
			dataGridvTools.Rows[num].Cells["vToolsOS"].Value = VISDK.GetVmInventoryProperty(i, "summary.config.guestFullName");
			dataGridvTools.Rows[num].Cells["vToolsguestFullName"].Value = VISDK.GetVmInventoryProperty(i, "guest.guestFullName");
			if (!PreferencesClass.ExcludeAnnotations)
			{
				dataGridvTools.Rows[num].Cells["vToolsNotes"].Value = VISDK.GetSummaryConfigAnnotation(i);
				CustomFieldValue[] customFieldValueForVM = VISDK.GetCustomFieldValueForVM(i);
				for (int num4 = customFieldValueForVM.Length; num4 > 0; num4--)
				{
					CustomFieldStringValue customFieldStringValue = (CustomFieldStringValue)customFieldValueForVM.GetValue(num4 - 1);
					try
					{
						dataGridvTools.Rows[num].Cells["CustomField" + customFieldStringValue.key].Value = customFieldStringValue.value;
					}
					catch (Exception ex3)
					{
						if (logger.IsDebugEnabled)
						{
							logger.Debug(currentMethod.Name + " Handled Exception: during setting value to custom field " + ex3.Message);
						}
					}
				}
			}
			if (!PreferencesClass.ExcludeTags)
			{
				try
				{
					List<Hashtable> tagColumnNames2 = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "VirtualMachine");
					string summaryVM = VISDK.GetSummaryVM(i);
					foreach (Hashtable item3 in tagColumnNames2)
					{
						string tagColumnValues = VISDK.GetTagColumnValues(ref VISDK.cisAssociatedTag, "VirtualMachine", item3["Name"].ToString(), summaryVM);
						dataGridvTools.Rows[num].Cells["vTools_tags_" + item3["Name"].ToString()].Value = tagColumnValues;
					}
				}
				catch (Exception ex4)
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(currentMethod.Name + " Handled Exception: during setting tag value " + ex4.Message);
					}
				}
			}
			if (PreferencesClass.vInfoShowFolder || CommandToExecute != null)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Call GetVMFolder for VM " + vmInventoryProperty);
				}
				string vMFolder = VISDK.GetVMFolder(i);
				dataGridvTools.Rows[num].Cells["vToolsFolder"].Value = vMFolder;
			}
			dataGridvTools.Rows[num].Cells["vToolsVMRef"].Value = i.ToString();
			dataGridvTools.Rows[num].Cells["vToolsUUID"].Value = VISDK.GetVmInventoryProperty(i, "config.instanceUuid");
			dataGridvTools.Rows[num].Cells["vToolsObjectID"].Value = VISDK.GetSummaryVM(i);
			dataGridvTools.Rows[num].Cells["vToolsInstanceUUID"].Value = InstanceUUID;
			dataGridvTools.Rows[num].Cells["vToolsVISDKServer"].Value = VISDK.Server;
			dataGridvTools.Rows[num].Cells["vToolsAppHeartbeatStatus"].Value = VISDK.GetVmInventoryProperty(i, "guest.appHeartbeatStatus");
			dataGridvTools.Rows[num].Cells["vToolsAppState"].Value = VISDK.GetVmInventoryProperty(i, "guest.appState");
			dataGridvTools.Rows[num].Cells["vToolsKernelCrashed"].Value = VISDK.GetVmInventoryProperty(i, "guest.guestKernelCrashed");
			dataGridvTools.Rows[num].Cells["vToolsOperationsReady"].Value = VISDK.GetVmInventoryProperty(i, "guest.guestOperationsReady");
			dataGridvTools.Rows[num].Cells["vToolsStateChangeSupported"].Value = VISDK.GetVmInventoryProperty(i, "guest.guestStateChangeSupported");
			dataGridvTools.Rows[num].Cells["vToolsInteractiveOperationsReady"].Value = VISDK.GetVmInventoryProperty(i, "guest.interactiveGuestOperationsReady");
			num++;
		}
		dataGridvTools.AutoResizeColumns();
		string columnName = "vToolsVMName";
		if (dataGridvTools.SortedColumn != null)
		{
			columnName = dataGridvTools.SortedColumn.Name;
		}
		dataGridvTools.Sort(dataGridvTools.Columns[columnName], ListSortDirection.Ascending);
		StatusGridCounter.Text = num + " rows";
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Number of rows: " + num);
		}
		Telemetry.AddEvent("RenderTab", "vTools", new Dictionary<string, string>
		{
			{
				"RenderTime",
				DateTime.UtcNow.Subtract(utcNow).ToString()
			},
			{
				"RowCount",
				num.ToString()
			}
		});
		Cursor = Cursors.Default;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void dataGridvTools_CellContentClick(object sender, DataGridViewCellEventArgs e)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		try
		{
			if (e.ColumnIndex == 0 && dataGridvTools.Rows[e.RowIndex].Cells[e.ColumnIndex].ReadOnly)
			{
				UserNotification.ReportWarning("The VMware Tools are not upgradeable for this virtual machine!");
			}
		}
		catch
		{
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void btVMUpgrade_Click(object sender, EventArgs e)
	{
		Telemetry.AddEvent("MenuAction", "UpgradeVmTools");
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		if (7 == 7 && MessageBox.Show("When a new version of the VMware tools is installed the VM will be rebooted! Are you sure you want to continue?", "RVTools warning!", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.No)
		{
			return;
		}
		for (int i = 0; i < dataGridvTools.RowCount; i++)
		{
			if (!((string)dataGridvTools.Rows[i].Cells[0].Value == "1"))
			{
				continue;
			}
			string text = dataGridvTools.Rows[i].Cells[1].Value.ToString();
			int num = Convert.ToInt16(dataGridvTools.Rows[i].Cells["vToolsVMRef"].Value.ToString());
			ManagedObjectReference managedObjectReference = null;
			managedObjectReference = VISDK.Inventory[num].obj;
			try
			{
				VISDK._service.UpgradeTools_Task(managedObjectReference, null);
				MessageBox.Show("Initiated Tools Upgrade for " + text, "RVTools", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			}
			catch (Exception ex)
			{
				if (ex.Message == "fault.RestrictedVersion.summary")
				{
					UserNotification.ReportWarning("VMware does not allow any type of remote management to manage the free version of ESXi.");
				}
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void TabPagevRP()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("ShowTab", "vRP");
		DateTime utcNow = DateTime.UtcNow;
		if (!TabPagevRP_Refresh_data)
		{
			StatusGridCounter.Text = dataGridvRP.RowCount + " rows";
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
			return;
		}
		TabPagevRP_Refresh_data = false;
		Application.DoEvents();
		Cursor = Cursors.WaitCursor;
		dataGridvRP.Rows.Clear();
		int num = 0;
		if (!PreferencesClass.ExcludeTags)
		{
			try
			{
				List<Hashtable> tagColumnNames = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "ResourcePool");
				int num2 = dataGridvRP.Columns["vRPVISDKServer"].Index - 1;
				foreach (Hashtable item in tagColumnNames)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
					if (!dataGridvRP.Columns.Contains("vRP_tags_" + item["Name"].ToString()))
					{
						dataGridViewTextBoxColumn.Name = "vRP_tags_" + item["Name"].ToString();
						dataGridViewTextBoxColumn.HeaderText = item["Name"].ToString();
						dataGridViewTextBoxColumn.ToolTipText = item["Desc"].ToString();
						dataGridViewTextBoxColumn.Width = 200;
						dataGridvRP.Columns.Insert(num2 + 1, dataGridViewTextBoxColumn);
						num2++;
					}
				}
			}
			catch (Exception ex)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding tags " + ex.Message);
				}
			}
		}
		StatusFilter.Visible = FilterClass.ShowFilterIcon();
		for (int i = 0; i < VISDK.ResourcePoolInventoryCounter; i++)
		{
			dataGridvRP.Rows.Add();
			string resourcePoolInventoryProperty = VISDK.GetResourcePoolInventoryProperty(i, "name");
			if (logger.IsInfoEnabled)
			{
				logger.Info(currentMethod.Name + " Add Row for RP: " + resourcePoolInventoryProperty);
			}
			dataGridvRP.Rows[num].Cells["vRPName"].Value = resourcePoolInventoryProperty;
			dataGridvRP.Rows[num].Cells["vRPPath"].Value = VISDK.GetResourcePoolPath(i);
			dataGridvRP.Rows[num].Cells["vRPVMs"].Value = VISDK.GetResourcePoolNumberOfVMs(i);
			dataGridvRP.Rows[num].Cells["vRPVMsTotal"].Value = VISDK.GetResourcePoolNumberOfVMsTotal(i);
			dataGridvRP.Rows[num].Cells["vRPnumberOfCPUs"].Value = VISDK.GetResourcePoolNumberOfvCPU(i);
			dataGridvRP.Rows[num].Cells["vRPObjectID"].Value = VISDK.GetResourcePoolObjectID(i);
			ResourcePoolSummary resourcePoolSummary = VISDK.GetResourcePoolSummary(i);
			try
			{
				dataGridvRP.Rows[num].Cells["vRPstatus"].Value = resourcePoolSummary.runtime.overallStatus;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPstatus"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPCPUexpandableReservation"].Value = resourcePoolSummary.config.cpuAllocation.expandableReservation;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPCPUexpandableReservation"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPCPUlimit"].Value = resourcePoolSummary.config.cpuAllocation.limit;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPCPUlimit"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPCPUoverheadLimit"].Value = resourcePoolSummary.config.cpuAllocation.overheadLimit;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPCPUoverheadLimit"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPCPUreservation"].Value = resourcePoolSummary.config.cpuAllocation.reservation;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPCPUreservation"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPCPUlevel"].Value = resourcePoolSummary.config.cpuAllocation.shares.level.ToString();
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPCPUlevel"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPCPUshares"].Value = resourcePoolSummary.config.cpuAllocation.shares.shares;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPCPUshares"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPCPUmaxUsage"].Value = resourcePoolSummary.runtime.cpu.maxUsage;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPCPUmaxUsage"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPCPUoverallUsage"].Value = resourcePoolSummary.runtime.cpu.overallUsage;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPCPUoverallUsage"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPCPUreservationUsed"].Value = resourcePoolSummary.runtime.cpu.reservationUsed;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPCPUreservationUsed"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPCPUreservationUsedForVm"].Value = resourcePoolSummary.runtime.cpu.reservationUsedForVm;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPCPUreservationUsedForVm"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPCPUunreservedForPool"].Value = resourcePoolSummary.runtime.cpu.unreservedForPool;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPCPUunreservedForPool"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPCPUunreservedForVm"].Value = resourcePoolSummary.runtime.cpu.unreservedForVm;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPCPUunreservedForVm"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPoverallCpuDemand"].Value = resourcePoolSummary.quickStats.overallCpuDemand;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPoverallCpuDemand"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPoverallCpuUsage"].Value = resourcePoolSummary.quickStats.overallCpuUsage;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPoverallCpuUsage"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPstaticCpuEntitlement"].Value = resourcePoolSummary.quickStats.staticCpuEntitlement;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPstaticCpuEntitlement"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPdistributedCpuEntitlement"].Value = resourcePoolSummary.quickStats.distributedCpuEntitlement;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPdistributedCpuEntitlement"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPMEMexpandableReservation"].Value = resourcePoolSummary.config.memoryAllocation.expandableReservation;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPMEMexpandableReservation"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPMEMlimit"].Value = resourcePoolSummary.config.memoryAllocation.limit;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPMEMlimit"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPMEMoverheadLimit"].Value = resourcePoolSummary.config.memoryAllocation.overheadLimit;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPMEMoverheadLimit"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPMEMreservation"].Value = resourcePoolSummary.config.memoryAllocation.reservation;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPMEMreservation"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPMEMlevel"].Value = resourcePoolSummary.config.memoryAllocation.shares.level.ToString();
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPMEMlevel"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPMEMshares"].Value = resourcePoolSummary.config.memoryAllocation.shares.shares;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPMEMshares"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPMEMmaxUsage"].Value = resourcePoolSummary.runtime.memory.maxUsage / 1048576;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPMEMmaxUsage"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPMEMoverallUsage"].Value = resourcePoolSummary.runtime.memory.overallUsage / 1048576;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPMEMoverallUsage"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPMEMreservationUsed"].Value = resourcePoolSummary.runtime.memory.reservationUsed / 1048576;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPMEMreservationUsed"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPMEMreservationUsedForVm"].Value = resourcePoolSummary.runtime.memory.reservationUsedForVm / 1048576;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPMEMreservationUsedForVm"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPMEMunreservedForPool"].Value = resourcePoolSummary.runtime.memory.unreservedForPool / 1048576;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPMEMunreservedForPool"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPMEMunreservedForVm"].Value = resourcePoolSummary.runtime.memory.unreservedForVm / 1048576;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPMEMunreservedForVm"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPballoonedMemory"].Value = resourcePoolSummary.quickStats.balloonedMemory;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPballoonedMemory"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPcompressedMemory"].Value = resourcePoolSummary.quickStats.compressedMemory;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPcompressedMemory"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPconsumedOverheadMemory"].Value = resourcePoolSummary.quickStats.consumedOverheadMemory;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPconsumedOverheadMemory"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPdistributedMemoryEntitlement"].Value = resourcePoolSummary.quickStats.distributedMemoryEntitlement;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPdistributedMemoryEntitlement"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPguestMemoryUsage"].Value = resourcePoolSummary.quickStats.guestMemoryUsage;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPguestMemoryUsage"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPhostMemoryUsage"].Value = resourcePoolSummary.quickStats.hostMemoryUsage;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPhostMemoryUsage"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPoverheadMemory"].Value = resourcePoolSummary.quickStats.overheadMemory;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPoverheadMemory"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPprivateMemory"].Value = resourcePoolSummary.quickStats.privateMemory;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPprivateMemory"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPsharedMemory"].Value = resourcePoolSummary.quickStats.sharedMemory;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPsharedMemory"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPstaticMemoryEntitlement"].Value = resourcePoolSummary.quickStats.staticMemoryEntitlement;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPstaticMemoryEntitlement"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPswappedMemory"].Value = resourcePoolSummary.quickStats.swappedMemory;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPswappedMemory"].Value = null;
			}
			try
			{
				dataGridvRP.Rows[num].Cells["vRPconfiguredMemoryMiB"].Value = resourcePoolSummary.configuredMemoryMB;
			}
			catch
			{
				dataGridvRP.Rows[num].Cells["vRPconfiguredMemoryMiB"].Value = null;
			}
			if (!PreferencesClass.ExcludeTags)
			{
				try
				{
					List<Hashtable> tagColumnNames2 = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "ResourcePool");
					string resourcePoolObjectID = VISDK.GetResourcePoolObjectID(num);
					foreach (Hashtable item2 in tagColumnNames2)
					{
						string tagColumnValues = VISDK.GetTagColumnValues(ref VISDK.cisAssociatedTag, "ResourcePool", item2["Name"].ToString(), resourcePoolObjectID);
						dataGridvRP.Rows[num].Cells["vRP_tags_" + item2["Name"].ToString()].Value = tagColumnValues;
					}
				}
				catch (Exception ex2)
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(currentMethod.Name + " Handled Exception: during setting tag value " + ex2.Message);
					}
				}
			}
			dataGridvRP.Rows[num].Cells["vRPInstanceUUID"].Value = InstanceUUID;
			dataGridvRP.Rows[num].Cells["vRPVISDKServer"].Value = VISDK.Server;
			num++;
		}
		dataGridvRP.AutoResizeColumns();
		string columnName = "vRPName";
		if (dataGridvRP.SortedColumn != null)
		{
			columnName = dataGridvRP.SortedColumn.Name;
		}
		dataGridvRP.Sort(dataGridvRP.Columns[columnName], ListSortDirection.Ascending);
		StatusGridCounter.Text = num + " rows";
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Number of rows: " + num);
		}
		Telemetry.AddEvent("RenderTab", "vRP", new Dictionary<string, string>
		{
			{
				"RenderTime",
				DateTime.UtcNow.Subtract(utcNow).ToString()
			},
			{
				"RowCount",
				num.ToString()
			}
		});
		Cursor = Cursors.Default;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void TabPagevSource()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("ShowTab", "vSource");
		DateTime utcNow = DateTime.UtcNow;
		if (!TabPagevSource_Refresh_data)
		{
			StatusGridCounter.Text = dataGridvSource.RowCount + " rows";
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
			return;
		}
		TabPagevSource_Refresh_data = false;
		Application.DoEvents();
		Cursor = Cursors.WaitCursor;
		dataGridvSource.Rows.Clear();
		int num = 0;
		StatusFilter.Visible = FilterClass.ShowFilterIcon();
		dataGridvSource.Rows.Add();
		dataGridvSource.Rows[num].Cells["vSourceName"].Value = VISDK._sic.about.name;
		try
		{
			dataGridvSource.Rows[num].Cells["vSourceApiType"].Value = VISDK._sic.about.apiType;
		}
		catch
		{
			dataGridvSource.Rows[num].Cells["vSourceApiType"].Value = null;
		}
		try
		{
			dataGridvSource.Rows[num].Cells["vSourceAPIVersion"].Value = VISDK._sic.about.apiVersion;
		}
		catch
		{
			dataGridvSource.Rows[num].Cells["vSourceAPIVersion"].Value = null;
		}
		try
		{
			dataGridvSource.Rows[num].Cells["vSourceVersion"].Value = VISDK._sic.about.version;
		}
		catch
		{
			dataGridvSource.Rows[num].Cells["vSourceVersion"].Value = null;
		}
		try
		{
			dataGridvSource.Rows[num].Cells["vSourcePatchLevel"].Value = VISDK._sic.about.patchLevel;
		}
		catch
		{
			dataGridvSource.Rows[num].Cells["vSourcePatchLevel"].Value = null;
		}
		try
		{
			dataGridvSource.Rows[num].Cells["vSourceBuild"].Value = VISDK._sic.about.build;
		}
		catch
		{
			dataGridvSource.Rows[num].Cells["vSourceBuild"].Value = null;
		}
		try
		{
			dataGridvSource.Rows[num].Cells["vSourceFullname"].Value = VISDK._sic.about.fullName;
		}
		catch
		{
			dataGridvSource.Rows[num].Cells["vSourceFullname"].Value = null;
		}
		try
		{
			dataGridvSource.Rows[num].Cells["vSourceProductName"].Value = VISDK._sic.about.licenseProductName;
		}
		catch
		{
			dataGridvSource.Rows[num].Cells["vSourceProductName"].Value = null;
		}
		try
		{
			dataGridvSource.Rows[num].Cells["vSourceProductVersion"].Value = VISDK._sic.about.licenseProductVersion;
		}
		catch
		{
			dataGridvSource.Rows[num].Cells["vSourceProductVersion"].Value = null;
		}
		try
		{
			dataGridvSource.Rows[num].Cells["vSourceOSType"].Value = VISDK._sic.about.osType;
		}
		catch
		{
			dataGridvSource.Rows[num].Cells["vSourceOSType"].Value = null;
		}
		try
		{
			dataGridvSource.Rows[num].Cells["vSourceProductLine"].Value = VISDK._sic.about.productLineId;
		}
		catch
		{
			dataGridvSource.Rows[num].Cells["vSourceProductLine"].Value = null;
		}
		try
		{
			dataGridvSource.Rows[num].Cells["vSourceVendor"].Value = VISDK._sic.about.vendor;
		}
		catch
		{
			dataGridvSource.Rows[num].Cells["vSourceVendor"].Value = null;
		}
		dataGridvSource.Rows[num].Cells["vSourceVISDKServer"].Value = VISDK.Server;
		try
		{
			dataGridvSource.Rows[num].Cells["vSourceVISDKUUID"].Value = VISDK._sic.about.instanceUuid;
		}
		catch
		{
			dataGridvSource.Rows[num].Cells["vSourceVISDKUUID"].Value = null;
		}
		num++;
		dataGridvSource.AutoResizeColumns();
		string columnName = "vSourceName";
		if (dataGridvSource.SortedColumn != null)
		{
			columnName = dataGridvSource.SortedColumn.Name;
		}
		dataGridvSource.Sort(dataGridvSource.Columns[columnName], ListSortDirection.Ascending);
		StatusGridCounter.Text = num + " rows";
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Number of rows: " + num);
		}
		Telemetry.AddEvent("RenderTab", "vSource", new Dictionary<string, string>
		{
			{
				"RenderTime",
				DateTime.UtcNow.Subtract(utcNow).ToString()
			},
			{
				"RowCount",
				num.ToString()
			}
		});
		Cursor = Cursors.Default;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void TabPagevCluster()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("ShowTab", "vCluster");
		DateTime utcNow = DateTime.UtcNow;
		if (!TabPagevCluster_Refresh_data)
		{
			StatusGridCounter.Text = dataGridvCluster.RowCount + " rows";
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
			return;
		}
		TabPagevCluster_Refresh_data = false;
		Application.DoEvents();
		Cursor = Cursors.WaitCursor;
		dataGridvCluster.Rows.Clear();
		int num = 0;
		if (!PreferencesClass.ExcludeAnnotations)
		{
			SortedList<string, string> customFieldHeaders = VISDK.GetCustomFieldHeaders("ClusterComputeResource");
			int num2 = dataGridvCluster.Columns["vClusterObjectID"].Index;
			try
			{
				foreach (KeyValuePair<string, string> item in customFieldHeaders)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
					dataGridViewTextBoxColumn.Name = "CustomField" + item.Value;
					dataGridViewTextBoxColumn.HeaderText = item.Key;
					dataGridViewTextBoxColumn.Width = 200;
					if (!dataGridvCluster.Columns.Contains("CustomField" + item.Value))
					{
						dataGridvCluster.Columns.Insert(num2 + 1, dataGridViewTextBoxColumn);
					}
					num2++;
				}
			}
			catch (Exception ex)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding CustomFieldHeaders " + ex.Message);
				}
			}
		}
		if (!PreferencesClass.ExcludeTags)
		{
			try
			{
				List<Hashtable> tagColumnNames = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "ClusterComputeResource");
				int num3 = dataGridvCluster.Columns["vClusterVISDKServer"].Index - 1;
				foreach (Hashtable item2 in tagColumnNames)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
					if (!dataGridvCluster.Columns.Contains("vCluster_tags_" + item2["Name"].ToString()))
					{
						dataGridViewTextBoxColumn2.Name = "vCluster_tags_" + item2["Name"].ToString();
						dataGridViewTextBoxColumn2.HeaderText = item2["Name"].ToString();
						dataGridViewTextBoxColumn2.ToolTipText = item2["Desc"].ToString();
						dataGridViewTextBoxColumn2.Width = 200;
						dataGridvCluster.Columns.Insert(num3 + 1, dataGridViewTextBoxColumn2);
						num3++;
					}
				}
			}
			catch (Exception ex2)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding tags " + ex2.Message);
				}
			}
		}
		StatusFilter.Visible = FilterClass.ShowFilterIcon();
		for (int i = 0; i < VISDK.ClusterInventoryCounter; i++)
		{
			string clusterInventoryProperty = VISDK.GetClusterInventoryProperty(i, "name");
			if (FilterClass.ShowFilterIcon() && (!FilterClass.DisplayDataCenter(VISDK.GetDatacenterName(VISDK.GetHostInventoryProperty(i, "parent"))) || !FilterClass.DisplayCluster(clusterInventoryProperty)))
			{
				continue;
			}
			dataGridvCluster.Rows.Add();
			if (logger.IsInfoEnabled)
			{
				logger.Info(currentMethod.Name + " Add Row for Cluster: " + clusterInventoryProperty);
			}
			dataGridvCluster.Rows[num].Cells["vClusterName"].Value = clusterInventoryProperty;
			dataGridvCluster.Rows[num].Cells["vClusterConfigStatus"].Value = VISDK.GetClusterInventoryProperty(i, "configStatus");
			try
			{
				ClusterComputeResourceSummary clusterSummary = VISDK.GetClusterSummary(i);
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterOverallStatus"].Value = clusterSummary.overallStatus.ToString();
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterOverallStatus"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterNumHosts"].Value = clusterSummary.numHosts;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterNumHosts"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterNumEffectiveHosts"].Value = clusterSummary.numEffectiveHosts;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterNumEffectiveHosts"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterTotalCpu"].Value = clusterSummary.totalCpu;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterTotalCpu"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterNumCpuCores"].Value = clusterSummary.numCpuCores;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterNumCpuCores"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterNumCpuThreads"].Value = clusterSummary.numCpuThreads;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterNumCpuThreads"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterEffectiveCpu"].Value = clusterSummary.effectiveCpu;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterEffectiveCpu"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterTotalMemory"].Value = clusterSummary.totalMemory / 1048576;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterTotalMemory"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterEffectiveMemory"].Value = clusterSummary.effectiveMemory;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterEffectiveMemory"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterNumVmotions"].Value = clusterSummary.numVmotions;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterNumVmotions"].Value = null;
				}
				ClusterConfigInfoEx clusterConfigEx = VISDK.GetClusterConfigEx(i);
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterAdmissionControlEnabled"].Value = clusterConfigEx.dasConfig.admissionControlEnabled;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterAdmissionControlEnabled"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterHostMonitoring"].Value = clusterConfigEx.dasConfig.hostMonitoring;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterHostMonitoring"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterIsolationResponse"].Value = clusterConfigEx.dasConfig.defaultVmSettings.isolationResponse;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterIsolationResponse"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterRestartPriority"].Value = clusterConfigEx.dasConfig.defaultVmSettings.restartPriority;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterRestartPriority"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterClusterSettings"].Value = clusterConfigEx.dasConfig.defaultVmSettings.vmToolsMonitoringSettings.clusterSettings;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterClusterSettings"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterFailureInterval"].Value = clusterConfigEx.dasConfig.defaultVmSettings.vmToolsMonitoringSettings.failureInterval;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterFailureInterval"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterFailureInterval"].Value = clusterConfigEx.dasConfig.defaultVmSettings.vmToolsMonitoringSettings.failureInterval;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterFailureInterval"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterMaxFailureWindow"].Value = clusterConfigEx.dasConfig.defaultVmSettings.vmToolsMonitoringSettings.maxFailureWindow;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterMaxFailureWindow"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterMaxFailures"].Value = clusterConfigEx.dasConfig.defaultVmSettings.vmToolsMonitoringSettings.maxFailures;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterMaxFailures"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterMinUpTime"].Value = clusterConfigEx.dasConfig.defaultVmSettings.vmToolsMonitoringSettings.minUpTime;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterMinUpTime"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterVmMonitoring"].Value = clusterConfigEx.dasConfig.defaultVmSettings.vmToolsMonitoringSettings.vmMonitoring;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterVmMonitoring"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterEnabled"].Value = clusterConfigEx.dasConfig.enabled;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterEnabled"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterFailoverLevel"].Value = clusterConfigEx.dasConfig.failoverLevel;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterFailoverLevel"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterHBDatastoreCandidatePolicy"].Value = clusterConfigEx.dasConfig.hBDatastoreCandidatePolicy;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterHBDatastoreCandidatePolicy"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterDpmEnabled"].Value = clusterConfigEx.dpmConfigInfo.enabled;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterDpmEnabled"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterDefaultDpmBehavior"].Value = clusterConfigEx.dpmConfigInfo.defaultDpmBehavior;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterDefaultDpmBehavior"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterHostPowerActionRate"].Value = clusterConfigEx.dpmConfigInfo.hostPowerActionRate;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterHostPowerActionRate"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterDRSEnabled"].Value = clusterConfigEx.drsConfig.enabled;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterDRSEnabled"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterDRSDefaultVmBehavior"].Value = clusterConfigEx.drsConfig.defaultVmBehavior;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterDRSDefaultVmBehavior"].Value = null;
				}
				try
				{
					dataGridvCluster.Rows[num].Cells["vClusterDRSVmotionRate"].Value = clusterConfigEx.drsConfig.vmotionRate;
				}
				catch
				{
					dataGridvCluster.Rows[num].Cells["vClusterDRSVmotionRate"].Value = null;
				}
			}
			catch
			{
			}
			dataGridvCluster.Rows[num].Cells["vClusterObjectID"].Value = VISDK.GetClusterObjectID(i);
			if (!PreferencesClass.ExcludeAnnotations)
			{
				CustomFieldValue[] customFieldValueForCluster = VISDK.GetCustomFieldValueForCluster(i);
				for (int num4 = customFieldValueForCluster.Length; num4 > 0; num4--)
				{
					CustomFieldStringValue customFieldStringValue = (CustomFieldStringValue)customFieldValueForCluster.GetValue(num4 - 1);
					try
					{
						dataGridvCluster.Rows[num].Cells["CustomField" + customFieldStringValue.key].Value = customFieldStringValue.value;
					}
					catch (Exception ex3)
					{
						if (logger.IsDebugEnabled)
						{
							logger.Debug(currentMethod.Name + " Handled Exception: during setting value to custom field " + ex3.Message);
						}
					}
				}
			}
			if (!PreferencesClass.ExcludeTags)
			{
				try
				{
					List<Hashtable> tagColumnNames2 = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "ClusterComputeResource");
					string clusterObjectID = VISDK.GetClusterObjectID(i);
					foreach (Hashtable item3 in tagColumnNames2)
					{
						string tagColumnValues = VISDK.GetTagColumnValues(ref VISDK.cisAssociatedTag, "ClusterComputeResource", item3["Name"].ToString(), clusterObjectID);
						dataGridvCluster.Rows[num].Cells["vCluster_tags_" + item3["Name"].ToString()].Value = tagColumnValues;
					}
				}
				catch (Exception ex4)
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(currentMethod.Name + " Handled Exception: during setting tag value " + ex4.Message);
					}
				}
			}
			dataGridvCluster.Rows[num].Cells["vClusterInstanceUUID"].Value = InstanceUUID;
			dataGridvCluster.Rows[num].Cells["vClusterVISDKServer"].Value = VISDK.Server;
			num++;
		}
		dataGridvCluster.AutoResizeColumns();
		string columnName = "vClusterName";
		if (dataGridvCluster.SortedColumn != null)
		{
			columnName = dataGridvCluster.SortedColumn.Name;
		}
		dataGridvCluster.Sort(dataGridvCluster.Columns[columnName], ListSortDirection.Ascending);
		StatusGridCounter.Text = num + " rows";
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Number of rows: " + num);
		}
		Telemetry.AddEvent("RenderTab", "vCluster", new Dictionary<string, string>
		{
			{
				"RenderTime",
				DateTime.UtcNow.Subtract(utcNow).ToString()
			},
			{
				"RowCount",
				num.ToString()
			}
		});
		Cursor = Cursors.Default;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void TabPagevHost()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("ShowTab", "vHost");
		DateTime utcNow = DateTime.UtcNow;
		if (!TabPagevHost_Refresh_data)
		{
			StatusGridCounter.Text = dataGridvHost.RowCount + " rows";
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
			return;
		}
		TabPagevHost_Refresh_data = false;
		Application.DoEvents();
		Cursor = Cursors.WaitCursor;
		dataGridvHost.Rows.Clear();
		int num = 0;
		if (!PreferencesClass.ExcludeAnnotations)
		{
			SortedList<string, string> customFieldHeaders = VISDK.GetCustomFieldHeaders("HostSystem");
			int num2 = dataGridvHost.Columns["vHostObjectID"].Index;
			try
			{
				foreach (KeyValuePair<string, string> item in customFieldHeaders)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
					dataGridViewTextBoxColumn.Name = "CustomField" + item.Value;
					dataGridViewTextBoxColumn.HeaderText = item.Key;
					dataGridViewTextBoxColumn.Width = 200;
					if (!dataGridvHost.Columns.Contains("CustomField" + item.Value))
					{
						dataGridvHost.Columns.Insert(num2 + 1, dataGridViewTextBoxColumn);
					}
					num2++;
				}
			}
			catch (Exception ex)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding CustomFieldHeaders " + ex.Message);
				}
			}
		}
		if (!PreferencesClass.ExcludeTags)
		{
			try
			{
				List<Hashtable> tagColumnNames = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "HostSystem");
				int num3 = dataGridvHost.Columns["vHostVISDKServer"].Index - 1;
				foreach (Hashtable item2 in tagColumnNames)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
					if (!dataGridvHost.Columns.Contains("vHost_tags_" + item2["Name"].ToString()))
					{
						dataGridViewTextBoxColumn2.Name = "vHost_tags_" + item2["Name"].ToString();
						dataGridViewTextBoxColumn2.HeaderText = item2["Name"].ToString();
						dataGridViewTextBoxColumn2.ToolTipText = item2["Desc"].ToString();
						dataGridViewTextBoxColumn2.Width = 200;
						dataGridvHost.Columns.Insert(num3 + 1, dataGridViewTextBoxColumn2);
						num3++;
					}
				}
			}
			catch (Exception ex2)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding tags " + ex2.Message);
				}
			}
		}
		StatusFilter.Visible = FilterClass.ShowFilterIcon();
		for (int i = 0; i < VISDK.HostInventoryCounter; i++)
		{
			if (FilterClass.ShowFilterIcon() && (!FilterClass.DisplayDataCenter(VISDK.GetDatacenterName(VISDK.GetHostInventoryProperty(i, "parent"))) || !FilterClass.DisplayCluster(VISDK.GetClusterName(VISDK.GetHostInventoryProperty(i, "parent"))) || !FilterClass.DisplayHost(VISDK.GetHostInventoryProperty(i, "name"))))
			{
				continue;
			}
			dataGridvHost.Rows.Add();
			string hostInventoryProperty = VISDK.GetHostInventoryProperty(i, "name");
			if (logger.IsInfoEnabled)
			{
				logger.Info(currentMethod.Name + " Add Row for Host: " + hostInventoryProperty);
			}
			dataGridvHost.Rows[num].Cells["vHostName"].Value = hostInventoryProperty;
			dataGridvHost.Rows[num].Cells["vHostDatacenter"].Value = VISDK.GetDatacenterName(VISDK.GetHostInventoryProperty(i, "parent"));
			dataGridvHost.Rows[num].Cells["vHostCluster"].Value = VISDK.GetClusterName(VISDK.GetHostInventoryProperty(i, "parent"));
			dataGridvHost.Rows[num].Cells["vHostConfigStatus"].Value = VISDK.GetHostInventoryProperty(i, "configStatus");
			dataGridvHost.Rows[num].Cells["vHostComplianceCheckState"].Value = VISDK.GetHostInventoryProperty(i, "complianceCheckState");
			try
			{
				dataGridvHost.Rows[num].Cells["vHostinMaintenanceMode"].Value = VISDK.GetHostInventoryProperty(i, "runtime.inMaintenanceMode");
			}
			catch
			{
				dataGridvHost.Rows[num].Cells["vHostinMaintenanceMode"].Value = null;
			}
			try
			{
				dataGridvHost.Rows[num].Cells["vHostinQuarantineMode"].Value = VISDK.GetHostInventoryProperty(i, "runtime.inQuarantineMode");
			}
			catch
			{
				dataGridvHost.Rows[num].Cells["vHostinQuarantineMode"].Value = null;
			}
			dataGridvHost.Rows[num].Cells["vHostFullName"].Value = VISDK.GetHostInventoryProperty(i, "summary.config.product");
			dataGridvHost.Rows[num].Cells["vHostCpuModel"].Value = VISDK.GetHostInventoryProperty(i, "summary.hardware.cpuModel");
			dataGridvHost.Rows[num].Cells["vHostObjectID"].Value = VISDK.HostInventory[i].obj.Value.ToString();
			long hostInventoryPropertyInt = VISDK.GetHostInventoryPropertyInt(i, "summary.hardware.numCpuPkgs");
			long hostInventoryPropertyInt2 = VISDK.GetHostInventoryPropertyInt(i, "summary.hardware.numCpuCores");
			long num4 = 0L;
			try
			{
				num4 = hostInventoryPropertyInt2 / hostInventoryPropertyInt;
			}
			catch
			{
			}
			dataGridvHost.Rows[num].Cells["vHostNumCpu"].Value = hostInventoryPropertyInt;
			dataGridvHost.Rows[num].Cells["vHostCoresPerCPU"].Value = num4;
			dataGridvHost.Rows[num].Cells["vHostNumCpuCores"].Value = hostInventoryPropertyInt2;
			dataGridvHost.Rows[num].Cells["vHostCpuMhz"].Value = VISDK.GetHostInventoryPropertyInt(i, "summary.hardware.cpuMhz");
			long hostInventoryPropertyInt3 = VISDK.GetHostInventoryPropertyInt(i, "summary.hardware.numCpuCores");
			long hostInventoryPropertyInt4 = VISDK.GetHostInventoryPropertyInt(i, "summary.hardware.cpuMhz");
			long hostInventoryPropertyInt5 = VISDK.GetHostInventoryPropertyInt(i, "summary.quickStats.overallCpuUsage");
			try
			{
				dataGridvHost.Rows[num].Cells["vHostOverallCpuUsage"].Value = hostInventoryPropertyInt5 * 100 / (hostInventoryPropertyInt3 * hostInventoryPropertyInt4);
			}
			catch
			{
				dataGridvHost.Rows[num].Cells["vHostOverallCpuUsage"].Value = 0;
			}
			dataGridvHost.Rows[num].Cells["vHostMemorySize"].Value = VISDK.GetHostInventoryPropertyInt(i, "summary.hardware.memorySize") / 1048576;
			long num5 = VISDK.GetHostInventoryPropertyInt(i, "summary.hardware.memorySize") / 1048576;
			long hostInventoryPropertyInt6 = VISDK.GetHostInventoryPropertyInt(i, "summary.quickStats.overallMemoryUsage");
			try
			{
				dataGridvHost.Rows[num].Cells["vHostOverallMemoryUsage"].Value = hostInventoryPropertyInt6 * 100 / num5;
			}
			catch
			{
				dataGridvHost.Rows[num].Cells["vHostOverallMemoryUsage"].Value = 0;
			}
			dataGridvHost.Rows[num].Cells["vHostMemoryTieringType"].Value = VISDK.GetHostInventoryProperty(i, "hardware.memoryTieringType");
			dataGridvHost.Rows[num].Cells["vHostServiceConsoleReserved"].Value = VISDK.GetHostInventoryPropertyInt(i, "config.consoleReservation.serviceConsoleReserved") / 1048576;
			dataGridvHost.Rows[num].Cells["vHostNumNics"].Value = VISDK.GetHostInventoryPropertyInt(i, "summary.hardware.numNics");
			dataGridvHost.Rows[num].Cells["vHostNumHBAs"].Value = VISDK.GetHostInventoryPropertyInt(i, "summary.hardware.numHBAs");
			dataGridvHost.Rows[num].Cells["vHostVMs"].Value = VISDK.GetHostInventoryPropertyInt(i, "vm");
			dataGridvHost.Rows[num].Cells["vHostVMsTotal"].Value = VISDK.GetHostInventoryPropertyInt(i, "vmTotal");
			try
			{
				DateTime dateTime = Convert.ToDateTime(VISDK.GetHostInventoryProperty(i, "summary.runtime.bootTime")).ToLocalTime();
				dataGridvHost.Rows[num].Cells["vHostBootTime"].Value = dateTime.ToString("yyyy/MM/dd HH:mm:ss");
			}
			catch
			{
				dataGridvHost.Rows[num].Cells["vHostBootTime"].Value = null;
			}
			decimal num6 = 0m;
			try
			{
				decimal num7 = VISDK.GetHostInventoryPropertyInt(i, "vm");
				decimal num8 = VISDK.GetHostInventoryPropertyInt(i, "summary.hardware.numCpuCores");
				num6 = num7 / num8;
				dataGridvHost.Rows[num].Cells["vHostVMsPerCore"].Value = num6;
			}
			catch
			{
			}
			decimal num9 = 0m;
			try
			{
				decimal num10 = VISDK.GetHostInventoryPropertyInt(i, "vCPUs");
				decimal num11 = VISDK.GetHostInventoryPropertyInt(i, "summary.hardware.numCpuCores");
				num9 = num10 / num11;
				dataGridvHost.Rows[num].Cells["vHostvCPUs"].Value = num10;
				dataGridvHost.Rows[num].Cells["vHostVCPUsPerCore"].Value = num9;
			}
			catch
			{
			}
			try
			{
				decimal num12 = VISDK.GetHostInventoryPropertyInt(i, "vRAM");
				dataGridvHost.Rows[num].Cells["vHostvRAM"].Value = num12;
			}
			catch
			{
			}
			try
			{
				decimal num13 = VISDK.GetHostInventoryPropertyInt(i, "VMUsedMemory");
				dataGridvHost.Rows[num].Cells["vHostVMUsedMemory"].Value = num13;
			}
			catch
			{
			}
			try
			{
				decimal num14 = VISDK.GetHostInventoryPropertyInt(i, "VMSwappedMemory");
				dataGridvHost.Rows[num].Cells["vHostVMSwappedMemory"].Value = num14;
			}
			catch
			{
			}
			try
			{
				decimal num15 = VISDK.GetHostInventoryPropertyInt(i, "VMBalloonedMemory");
				dataGridvHost.Rows[num].Cells["vHostVMBalloonedMemory"].Value = num15;
			}
			catch
			{
			}
			try
			{
				HostSystemIdentificationInfo[] hostSystemIdentificationInfo = VISDK.GetHostSystemIdentificationInfo(i);
				foreach (HostSystemIdentificationInfo hostSystemIdentificationInfo2 in hostSystemIdentificationInfo)
				{
					if (hostSystemIdentificationInfo2.identifierType.key == "ServiceTag")
					{
						dataGridvHost.Rows[num].Cells["vHostServiceTag"].Value = hostSystemIdentificationInfo2.identifierValue;
					}
					if (hostSystemIdentificationInfo2.identifierType.key == "OemSpecificString")
					{
						dataGridvHost.Rows[num].Cells["vHostOemSpecificString"].Value = hostSystemIdentificationInfo2.identifierValue;
					}
				}
			}
			catch
			{
			}
			try
			{
				DynamicProperty[] propSet = VISDK.GetHostVSanSystem(VISDK.GetHostConfigManager(i).vsanSystem)[0].propSet;
				for (int k = 0; k < propSet.Length; k++)
				{
					if (propSet[k].name == "config")
					{
						VsanHostConfigInfo vsanHostConfigInfo = (VsanHostConfigInfo)propSet[k].val;
						dataGridvHost.Rows[num].Cells["vHostvSANFaultDomainName"].Value = vsanHostConfigInfo.faultDomainInfo.name;
						break;
					}
				}
			}
			catch (Exception ex3)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled exception in GetHostConfigManager section: " + ex3);
				}
			}
			try
			{
				DynamicProperty[] propSet2 = VISDK.GetHostAdvancedOptions(VISDK.GetHostConfigManager(i).advancedOption)[0].propSet;
				for (int l = 0; l < propSet2.Length; l++)
				{
					if (!(propSet2[l].name == "setting"))
					{
						continue;
					}
					OptionValue[] array = (OptionValue[])propSet2[l].val;
					foreach (OptionValue optionValue in array)
					{
						if (optionValue.key == "VMFS3.UseATSForHBOnVMFS5")
						{
							dataGridvHost.Rows[num].Cells["vHostATSHeartbeat"].Value = optionValue.value;
						}
						if (optionValue.key == "VMFS3.HardwareAcceleratedLocking")
						{
							dataGridvHost.Rows[num].Cells["vHostATSLocking"].Value = optionValue.value;
						}
					}
					break;
				}
			}
			catch (Exception ex4)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled exception in GetHostConfigManager section: " + ex4);
				}
			}
			try
			{
				HostCertificateManagerCertificateInfo hostCertificateManagerCertificateInfo = VISDK._HostCertificates[VISDK.HostInventory[i].obj.Value];
				dataGridvHost.Rows[num].Cells["vHostCertificateIssuer"].Value = hostCertificateManagerCertificateInfo.issuer;
				dataGridvHost.Rows[num].Cells["vHostCertificateExpiry"].Value = hostCertificateManagerCertificateInfo.notAfter;
				dataGridvHost.Rows[num].Cells["vHostCertificateStartDate"].Value = hostCertificateManagerCertificateInfo.notBefore;
				dataGridvHost.Rows[num].Cells["vHostCertificateStatus"].Value = hostCertificateManagerCertificateInfo.status;
				dataGridvHost.Rows[num].Cells["vHostCertificateSubject"].Value = hostCertificateManagerCertificateInfo.subject;
			}
			catch (Exception ex5)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled exception in GetHostConfigManager section: " + ex5);
				}
			}
			try
			{
				HostCapability hostCapability = VISDK.GetHostCapability(i);
				dataGridvHost.Rows[num].Cells["vHostVmotionSupported"].Value = hostCapability.vmotionSupported;
				dataGridvHost.Rows[num].Cells["vHostStorageVmotionSupported"].Value = hostCapability.storageVMotionSupported;
			}
			catch (Exception ex6)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled exception in GetHostCapability section: " + ex6);
				}
			}
			try
			{
				HostHyperThreadScheduleInfo hostHyperThreadScheduleInfo = VISDK.GetHostHyperThreadScheduleInfo(i);
				dataGridvHost.Rows[num].Cells["vHostHTAvailable"].Value = hostHyperThreadScheduleInfo.available;
				dataGridvHost.Rows[num].Cells["vHostHTActive"].Value = hostHyperThreadScheduleInfo.active;
			}
			catch (Exception ex7)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled exception in GetHostHyperThreadScheduleInfo section: " + ex7);
				}
			}
			try
			{
				HostDnsConfig hostDnsConfig = VISDK.GetHostDnsConfig(i);
				string text = null;
				try
				{
					for (int m = 0; m < hostDnsConfig.address.Length; m++)
					{
						text = ((text == null) ? hostDnsConfig.address[m] : (text + ", " + hostDnsConfig.address[m]));
					}
				}
				catch (Exception ex8)
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(currentMethod.Name + " Handled exception in Get DNS servers section: " + ex8);
					}
				}
				dataGridvHost.Rows[num].Cells["vHostDNSServers"].Value = text;
				dataGridvHost.Rows[num].Cells["vHostDHCP"].Value = hostDnsConfig.dhcp;
				dataGridvHost.Rows[num].Cells["vHostDomainName"].Value = hostDnsConfig.domainName;
				string text2 = null;
				try
				{
					for (int n = 0; n < hostDnsConfig.searchDomain.Length; n++)
					{
						text2 = ((text2 == null) ? hostDnsConfig.searchDomain[n] : (text2 + ", " + hostDnsConfig.searchDomain[n]));
					}
				}
				catch (Exception ex9)
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(currentMethod.Name + " Handled exception in Get DNS servers section: " + ex9);
					}
				}
				dataGridvHost.Rows[num].Cells["vHostSearchDomains"].Value = text2;
			}
			catch (Exception ex10)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled exception in GetHostDnsInfo section: " + ex10);
				}
			}
			dataGridvHost.Rows[num].Cells["vHostDomainList"].Value = VISDK.GetHostInventoryProperty(i, "config.domainList");
			try
			{
				HostDateTimeInfo hostDateTimeInfo = VISDK.GetHostDateTimeInfo(i);
				string text3 = null;
				if (hostDateTimeInfo != null && hostDateTimeInfo.ntpConfig.server != null)
				{
					for (int num16 = 0; num16 < hostDateTimeInfo.ntpConfig.server.Length; num16++)
					{
						text3 = ((text3 == null) ? hostDateTimeInfo.ntpConfig.server[num16] : (text3 + ", " + hostDateTimeInfo.ntpConfig.server[num16]));
					}
				}
				dataGridvHost.Rows[num].Cells["vHostNTPServers"].Value = text3;
				HostService[] hostServices = VISDK.GetHostServices(i);
				foreach (HostService hostService in hostServices)
				{
					if (hostService.key == "ntpd")
					{
						dataGridvHost.Rows[num].Cells["vHostNTPD"].Value = hostService.running;
						break;
					}
				}
				dataGridvHost.Rows[num].Cells["vHostTZDescription"].Value = hostDateTimeInfo.timeZone.description;
				dataGridvHost.Rows[num].Cells["vHostTZName"].Value = hostDateTimeInfo.timeZone.name;
				dataGridvHost.Rows[num].Cells["vHostTZOffset"].Value = hostDateTimeInfo.timeZone.gmtOffset;
			}
			catch (Exception ex11)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled exception in HostdateTimeInfo section: " + ex11);
				}
			}
			try
			{
				dataGridvHost.Rows[num].Cells["vHostCurrentEVCModeKey"].Value = VISDK.GetHostInventoryProperty(i, "summary.currentEVCModeKey");
				dataGridvHost.Rows[num].Cells["vHostMaxEVCModeKey"].Value = VISDK.GetHostInventoryProperty(i, "summary.maxEVCModeKey");
			}
			catch
			{
			}
			try
			{
				PowerSystemInfo powerSystemInfo = VISDK.GetPowerSystemInfo(i);
				dataGridvHost.Rows[num].Cells["vHostHostPowerPolicyShortname"].Value = powerSystemInfo.currentPolicy.shortName;
			}
			catch (Exception ex12)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled exception in powerSystemInfo section: " + ex12);
				}
			}
			try
			{
				HostCpuPowerManagementInfo hostCpuPowerManagementInfo = VISDK.GetHostCpuPowerManagementInfo(i);
				dataGridvHost.Rows[num].Cells["vHostCurrentPolicy"].Value = hostCpuPowerManagementInfo.currentPolicy;
				dataGridvHost.Rows[num].Cells["vHosthardwareSupport"].Value = hostCpuPowerManagementInfo.hardwareSupport;
			}
			catch (Exception ex13)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled exception in HostCpuPowerManagementInfo section: " + ex13);
				}
			}
			try
			{
				HostBIOSInfo hostBiosInfo = VISDK.GetHostBiosInfo(i);
				dataGridvHost.Rows[num].Cells["vHostBiosVersion"].Value = hostBiosInfo.biosVersion;
				dataGridvHost.Rows[num].Cells["vHostBiosDate"].Value = hostBiosInfo.releaseDate.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
				dataGridvHost.Rows[num].Cells["vHostBiosVendor"].Value = hostBiosInfo.vendor;
			}
			catch (Exception ex14)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled exception in HostBiosInfo section: " + ex14);
				}
			}
			try
			{
				HostSystemInfo hostSystemInfo = VISDK.GetHostSystemInfo(i);
				dataGridvHost.Rows[num].Cells["vHostVendor"].Value = hostSystemInfo.vendor;
				dataGridvHost.Rows[num].Cells["vHostModel"].Value = hostSystemInfo.model;
				dataGridvHost.Rows[num].Cells["vHostSerialNumber"].Value = hostSystemInfo.serialNumber;
				dataGridvHost.Rows[num].Cells["vHostUUID"].Value = hostSystemInfo.uuid;
			}
			catch (Exception ex15)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled exception in SystemInfo section: " + ex15);
				}
			}
			if (!PreferencesClass.ExcludeAnnotations)
			{
				CustomFieldValue[] customFieldValueForHost = VISDK.GetCustomFieldValueForHost(i);
				for (int num17 = customFieldValueForHost.Length; num17 > 0; num17--)
				{
					CustomFieldStringValue customFieldStringValue = (CustomFieldStringValue)customFieldValueForHost.GetValue(num17 - 1);
					try
					{
						dataGridvHost.Rows[num].Cells["CustomField" + customFieldStringValue.key].Value = customFieldStringValue.value;
					}
					catch (Exception ex16)
					{
						if (logger.IsDebugEnabled)
						{
							logger.Debug(currentMethod.Name + " Handled Exception: during setting value to custom field " + ex16.Message);
						}
					}
				}
			}
			if (!PreferencesClass.ExcludeTags)
			{
				try
				{
					List<Hashtable> tagColumnNames2 = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "HostSystem");
					string id = VISDK.HostInventory[i].obj.Value.ToString();
					foreach (Hashtable item3 in tagColumnNames2)
					{
						string tagColumnValues = VISDK.GetTagColumnValues(ref VISDK.cisAssociatedTag, "HostSystem", item3["Name"].ToString(), id);
						dataGridvHost.Rows[num].Cells["vHost_tags_" + item3["Name"].ToString()].Value = tagColumnValues;
					}
				}
				catch (Exception ex17)
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(currentMethod.Name + " Handled Exception: during setting tag value " + ex17.Message);
					}
				}
			}
			dataGridvHost.Rows[num].Cells["vHostInstanceUUID"].Value = InstanceUUID;
			dataGridvHost.Rows[num].Cells["vHostVISDKServer"].Value = VISDK.Server;
			try
			{
				QueryAssignedLicensesRequest request = new QueryAssignedLicensesRequest(VISDK.GetLicenseAssignmentManager(), VISDK.HostInventory[i].obj.Value.ToString());
				LicenseAssignmentManagerLicenseAssignment[] returnval = VISDK._service.QueryAssignedLicenses(request).returnval;
				int num18 = 0;
				LicenseAssignmentManagerLicenseAssignment[] array2 = returnval;
				foreach (LicenseAssignmentManagerLicenseAssignment licenseAssignmentManagerLicenseAssignment in array2)
				{
					if (num18 == 0)
					{
						dataGridvHost.Rows[num].Cells["vHostAssignedLicenses"].Value = licenseAssignmentManagerLicenseAssignment.assignedLicense.licenseKey;
					}
					else
					{
						dataGridvHost.Rows[num].Cells["vHostAssignedLicenses"].Value = dataGridvHost.Rows[num].Cells["vHostAssignedLicenses"].Value?.ToString() + ", " + licenseAssignmentManagerLicenseAssignment.assignedLicense.licenseKey;
					}
					num18++;
				}
			}
			catch
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handeled Exception GetAssignedLicenses");
				}
			}
			num++;
		}
		dataGridvHost.AutoResizeColumns();
		string columnName = "vHostName";
		if (dataGridvHost.SortedColumn != null)
		{
			columnName = dataGridvHost.SortedColumn.Name;
		}
		dataGridvHost.Sort(dataGridvHost.Columns[columnName], ListSortDirection.Ascending);
		StatusGridCounter.Text = num + " rows";
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Number of rows: " + num);
		}
		Telemetry.AddEvent("RenderTab", "vHost", new Dictionary<string, string>
		{
			{
				"RenderTime",
				DateTime.UtcNow.Subtract(utcNow).ToString()
			},
			{
				"RowCount",
				num.ToString()
			}
		});
		Cursor = Cursors.Default;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void TabPagevHBA()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("ShowTab", "vHBA");
		DateTime utcNow = DateTime.UtcNow;
		if (!TabPagevHBA_Refresh_data)
		{
			StatusGridCounter.Text = dataGridvHBA.RowCount + " rows";
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
			return;
		}
		TabPagevHBA_Refresh_data = false;
		Application.DoEvents();
		Cursor = Cursors.WaitCursor;
		dataGridvHBA.Rows.Clear();
		int num = 0;
		StatusFilter.Visible = FilterClass.ShowFilterIcon();
		for (int i = 0; i < VISDK.HostInventoryCounter; i++)
		{
			if (FilterClass.ShowFilterIcon() && (!FilterClass.DisplayDataCenter(VISDK.GetDatacenterName(VISDK.GetHostInventoryProperty(i, "parent"))) || !FilterClass.DisplayCluster(VISDK.GetClusterName(VISDK.GetHostInventoryProperty(i, "parent"))) || !FilterClass.DisplayHost(VISDK.GetHostInventoryProperty(i, "name"))))
			{
				continue;
			}
			HostHostBusAdapter[] hostHBAInfo = VISDK.GetHostHBAInfo(i);
			if (hostHBAInfo == null)
			{
				continue;
			}
			HostHostBusAdapter[] array = hostHBAInfo;
			foreach (HostHostBusAdapter hostHostBusAdapter in array)
			{
				dataGridvHBA.Rows.Add();
				string hostInventoryProperty = VISDK.GetHostInventoryProperty(i, "name");
				if (logger.IsInfoEnabled)
				{
					logger.Info(currentMethod.Name + " Add Row for HBA: " + hostInventoryProperty);
				}
				dataGridvHBA.Rows[num].Cells["vHBAHostName"].Value = hostInventoryProperty;
				dataGridvHBA.Rows[num].Cells["vHBADatacenter"].Value = VISDK.GetDatacenterName(VISDK.GetHostInventoryProperty(i, "parent"));
				dataGridvHBA.Rows[num].Cells["vHBACluster"].Value = VISDK.GetClusterName(VISDK.GetHostInventoryProperty(i, "parent"));
				try
				{
					dataGridvHBA.Rows[num].Cells["vHBABus"].Value = hostHostBusAdapter.bus;
				}
				catch
				{
				}
				try
				{
					dataGridvHBA.Rows[num].Cells["vHBADevice"].Value = hostHostBusAdapter.device;
				}
				catch
				{
				}
				try
				{
					dataGridvHBA.Rows[num].Cells["vHBADriver"].Value = hostHostBusAdapter.driver;
				}
				catch
				{
				}
				try
				{
					dataGridvHBA.Rows[num].Cells["vHBAModel"].Value = hostHostBusAdapter.model;
				}
				catch
				{
				}
				try
				{
					dataGridvHBA.Rows[num].Cells["vHBAPci"].Value = hostHostBusAdapter.pci;
				}
				catch
				{
				}
				try
				{
					dataGridvHBA.Rows[num].Cells["vHBAStatus"].Value = hostHostBusAdapter.status;
				}
				catch
				{
				}
				if (hostHostBusAdapter is HostBlockHba)
				{
					dataGridvHBA.Rows[num].Cells["vHBAType"].Value = "Block SCSI";
				}
				if (hostHostBusAdapter is HostInternetScsiHba)
				{
					dataGridvHBA.Rows[num].Cells["vHBAType"].Value = "ISCSI";
				}
				if (hostHostBusAdapter is HostParallelScsiHba)
				{
					dataGridvHBA.Rows[num].Cells["vHBAType"].Value = "SCSI";
				}
				if (hostHostBusAdapter is HostFibreChannelHba)
				{
					HostFibreChannelHba hostFibreChannelHba = (HostFibreChannelHba)hostHostBusAdapter;
					dataGridvHBA.Rows[num].Cells["vHBAType"].Value = "Fibre Channel";
					try
					{
						string text = hostFibreChannelHba.nodeWorldWideName.ToString("X");
						string text2 = hostFibreChannelHba.portWorldWideName.ToString("X");
						dataGridvHBA.Rows[num].Cells["vHBAWorldWideName"].Value = text + " " + text2;
					}
					catch
					{
					}
				}
				dataGridvHBA.Rows[num].Cells["vHBAInstanceUUID"].Value = InstanceUUID;
				dataGridvHBA.Rows[num].Cells["vHBAVISDKServer"].Value = VISDK.Server;
				num++;
			}
		}
		dataGridvHBA.AutoResizeColumns();
		string columnName = "vHBAHostName";
		if (dataGridvHBA.SortedColumn != null)
		{
			columnName = dataGridvHBA.SortedColumn.Name;
		}
		dataGridvHBA.Sort(dataGridvHBA.Columns[columnName], ListSortDirection.Ascending);
		StatusGridCounter.Text = num + " rows";
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Number of rows: " + num);
		}
		Telemetry.AddEvent("RenderTab", "vHBA", new Dictionary<string, string>
		{
			{
				"RenderTime",
				DateTime.UtcNow.Subtract(utcNow).ToString()
			},
			{
				"RowCount",
				num.ToString()
			}
		});
		Cursor = Cursors.Default;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void TabPagevNIC()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("ShowTab", "vNic");
		DateTime utcNow = DateTime.UtcNow;
		if (!TabPagevNIC_Refresh_data)
		{
			StatusGridCounter.Text = dataGridvNIC.RowCount + " rows";
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
			return;
		}
		TabPagevNIC_Refresh_data = false;
		Application.DoEvents();
		Cursor = Cursors.WaitCursor;
		dataGridvNIC.Rows.Clear();
		int num = 0;
		StatusFilter.Visible = FilterClass.ShowFilterIcon();
		for (int i = 0; i < VISDK.HostInventoryCounter; i++)
		{
			if (FilterClass.ShowFilterIcon() && (!FilterClass.DisplayDataCenter(VISDK.GetDatacenterName(VISDK.GetHostInventoryProperty(i, "parent"))) || !FilterClass.DisplayCluster(VISDK.GetClusterName(VISDK.GetHostInventoryProperty(i, "parent"))) || !FilterClass.DisplayHost(VISDK.GetHostInventoryProperty(i, "name"))))
			{
				continue;
			}
			PhysicalNic[] hostNicInfo = VISDK.GetHostNicInfo(i);
			if (hostNicInfo == null)
			{
				continue;
			}
			HostVirtualSwitch[] array = null;
			HostProxySwitch[] array2 = null;
			try
			{
				array = VISDK.GetHostNetworkSwitchInfo(i);
				array2 = VISDK.GetHostNetworkProxySwitchInfo(i);
			}
			catch
			{
			}
			PhysicalNic[] array3 = hostNicInfo;
			foreach (PhysicalNic physicalNic in array3)
			{
				dataGridvNIC.Rows.Add();
				string hostInventoryProperty = VISDK.GetHostInventoryProperty(i, "name");
				if (logger.IsInfoEnabled)
				{
					logger.Info(currentMethod.Name + " Add Row for NIC: " + hostInventoryProperty);
				}
				dataGridvNIC.Rows[num].Cells["vNicHostName"].Value = hostInventoryProperty;
				dataGridvNIC.Rows[num].Cells["vNicDatacenter"].Value = VISDK.GetDatacenterName(VISDK.GetHostInventoryProperty(i, "parent"));
				dataGridvNIC.Rows[num].Cells["vNicCluster"].Value = VISDK.GetClusterName(VISDK.GetHostInventoryProperty(i, "parent"));
				dataGridvNIC.Rows[num].Cells["vNicDevice"].Value = physicalNic.device;
				dataGridvNIC.Rows[num].Cells["vNicDriver"].Value = physicalNic.driver;
				try
				{
					dataGridvNIC.Rows[num].Cells["vNicSpeed"].Value = physicalNic.linkSpeed.speedMb;
					dataGridvNIC.Rows[num].Cells["vNicDuplex"].Value = physicalNic.linkSpeed.duplex.ToString();
				}
				catch
				{
					dataGridvNIC.Rows[num].Cells["vNicSpeed"].Value = null;
					dataGridvNIC.Rows[num].Cells["vNicDuplex"].Value = "Link is down!";
				}
				dataGridvNIC.Rows[num].Cells["vNicMac"].Value = physicalNic.mac;
				dataGridvNIC.Rows[num].Cells["vNicSwitch"].Value = null;
				try
				{
					HostProxySwitch[] array4 = array2;
					foreach (HostProxySwitch hostProxySwitch in array4)
					{
						try
						{
							if (logger.IsDebugEnabled)
							{
								logger.Debug(currentMethod.Name + " vNicdSwitch: Hostname / switch " + hostInventoryProperty + " / " + hostProxySwitch.dvsName);
							}
							for (int l = 0; l < hostProxySwitch.pnic.Length; l++)
							{
								string text = hostProxySwitch.pnic[l];
								if (logger.IsDebugEnabled)
								{
									logger.Debug(currentMethod.Name + " vdNicSwitch: nic / NIC.key " + text + " / " + physicalNic.key);
								}
								if (text == physicalNic.key)
								{
									dataGridvNIC.Rows[num].Cells["vNicSwitch"].Value = hostProxySwitch.dvsName;
									break;
								}
							}
						}
						catch
						{
							if (logger.IsDebugEnabled)
							{
								logger.Debug(currentMethod.Name + " Handled exception in foreach (HostProxySwitch Switch in dSwitches) section: 1");
							}
						}
					}
				}
				catch
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(currentMethod.Name + " Handled exception in foreach (HostProxySwitch Switch in dSwitches) section: 2");
					}
				}
				try
				{
					HostProxySwitch[] array4 = array2;
					foreach (HostProxySwitch hostProxySwitch2 in array4)
					{
						if (logger.IsDebugEnabled)
						{
							logger.Debug(currentMethod.Name + " DistributedVirtualSwitchHostMemberPnicBacking: Hostname / switch " + hostInventoryProperty + " / " + hostProxySwitch2.dvsName);
						}
						DistributedVirtualSwitchHostMemberPnicBacking distributedVirtualSwitchHostMemberPnicBacking;
						try
						{
							distributedVirtualSwitchHostMemberPnicBacking = (DistributedVirtualSwitchHostMemberPnicBacking)hostProxySwitch2.spec.backing;
						}
						catch
						{
							if (logger.IsDebugEnabled)
							{
								logger.Debug(currentMethod.Name + " Handled exception in foreach (DistributedVirtualSwitchHostMemberPnicBacking) section: 1");
							}
							continue;
						}
						try
						{
							for (int m = 0; m < distributedVirtualSwitchHostMemberPnicBacking.pnicSpec.Length; m++)
							{
								if (!(distributedVirtualSwitchHostMemberPnicBacking.pnicSpec[m].pnicDevice == physicalNic.device))
								{
									continue;
								}
								string uplinkPortKey = distributedVirtualSwitchHostMemberPnicBacking.pnicSpec[m].uplinkPortKey;
								if (hostProxySwitch2.hostLag == null)
								{
									for (int n = 0; n < hostProxySwitch2.uplinkPort.Length; n++)
									{
										if (hostProxySwitch2.uplinkPort[n].key.ToString() == uplinkPortKey)
										{
											dataGridvNIC.Rows[num].Cells["vNicUplinkPort"].Value = hostProxySwitch2.uplinkPort[n].value;
											break;
										}
									}
								}
								if (hostProxySwitch2.hostLag == null)
								{
									continue;
								}
								for (int num2 = 0; num2 < hostProxySwitch2.hostLag.Length; num2++)
								{
									for (int num3 = 0; num3 < hostProxySwitch2.hostLag[num2].uplinkPort.Length; num3++)
									{
										if (hostProxySwitch2.hostLag[num2].uplinkPort[num3].key.ToString() == uplinkPortKey)
										{
											dataGridvNIC.Rows[num].Cells["vNicUplinkPort"].Value = hostProxySwitch2.hostLag[num2].uplinkPort[num3].value;
											break;
										}
									}
									for (int num4 = 0; num4 < hostProxySwitch2.uplinkPort.Length; num4++)
									{
										if (hostProxySwitch2.uplinkPort[num4].key.ToString() == uplinkPortKey)
										{
											dataGridvNIC.Rows[num].Cells["vNicUplinkPort"].Value = hostProxySwitch2.uplinkPort[num4].value;
											break;
										}
									}
								}
							}
						}
						catch
						{
							if (logger.IsDebugEnabled)
							{
								logger.Debug(currentMethod.Name + " Handled exception in foreach (DistributedVirtualSwitchHostMemberPnicBacking) section: 2");
							}
						}
					}
				}
				catch
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(currentMethod.Name + " Handled exception in foreach (DistributedVirtualSwitchHostMemberPnicBacking) section: 3");
					}
				}
				try
				{
					HostVirtualSwitch[] array5 = array;
					foreach (HostVirtualSwitch hostVirtualSwitch in array5)
					{
						if (logger.IsDebugEnabled)
						{
							logger.Debug(currentMethod.Name + " vNicSwitch: Hostname / switch " + hostInventoryProperty + " / " + hostVirtualSwitch.name);
						}
						try
						{
							for (int num5 = 0; num5 < hostVirtualSwitch.pnic.Length; num5++)
							{
								string text2 = hostVirtualSwitch.pnic[num5];
								if (logger.IsDebugEnabled)
								{
									logger.Debug(currentMethod.Name + " vNicSwitch: nic / NIC.key " + text2 + " / " + physicalNic.key);
								}
								if (text2 == physicalNic.key)
								{
									dataGridvNIC.Rows[num].Cells["vNicSwitch"].Value = hostVirtualSwitch.name;
									break;
								}
							}
						}
						catch
						{
							if (logger.IsDebugEnabled)
							{
								logger.Debug(currentMethod.Name + " Handled exception in foreach (HostVirtualSwitch Switch in vSwitches) section: pnic = null");
							}
						}
					}
				}
				catch (Exception ex)
				{
					if (logger.IsInfoEnabled)
					{
						logger.Info(currentMethod.Name + " Handled exception in foreach (HostVirtualSwitch Switch in vSwitches) section: " + ex.Message);
					}
				}
				dataGridvNIC.Rows[num].Cells["vNicPci"].Value = physicalNic.pci;
				dataGridvNIC.Rows[num].Cells["vNicWakeOn"].Value = physicalNic.wakeOnLanSupported;
				dataGridvNIC.Rows[num].Cells["vNICInstanceUUID"].Value = InstanceUUID;
				dataGridvNIC.Rows[num].Cells["vNICVISDKServer"].Value = VISDK.Server;
				num++;
			}
		}
		dataGridvNIC.AutoResizeColumns();
		string columnName = "vNicHostName";
		if (dataGridvNIC.SortedColumn != null)
		{
			columnName = dataGridvNIC.SortedColumn.Name;
		}
		dataGridvNIC.Sort(dataGridvNIC.Columns[columnName], ListSortDirection.Ascending);
		StatusGridCounter.Text = num + " rows";
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Number of rows: " + num);
		}
		Telemetry.AddEvent("RenderTab", "vNic", new Dictionary<string, string>
		{
			{
				"RenderTime",
				DateTime.UtcNow.Subtract(utcNow).ToString()
			},
			{
				"RowCount",
				num.ToString()
			}
		});
		Cursor = Cursors.Default;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void TabPagevSwitch()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("ShowTab", "vSwitch");
		DateTime utcNow = DateTime.UtcNow;
		if (!TabPagevSwitch_Refresh_data)
		{
			StatusGridCounter.Text = dataGridvSwitch.RowCount + " rows";
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
			return;
		}
		TabPagevSwitch_Refresh_data = false;
		Application.DoEvents();
		Cursor = Cursors.WaitCursor;
		dataGridvSwitch.Rows.Clear();
		int num = 0;
		StatusFilter.Visible = FilterClass.ShowFilterIcon();
		for (int i = 0; i < VISDK.HostInventoryCounter; i++)
		{
			if (FilterClass.ShowFilterIcon() && (!FilterClass.DisplayDataCenter(VISDK.GetDatacenterName(VISDK.GetHostInventoryProperty(i, "parent"))) || !FilterClass.DisplayCluster(VISDK.GetClusterName(VISDK.GetHostInventoryProperty(i, "parent"))) || !FilterClass.DisplayHost(VISDK.GetHostInventoryProperty(i, "name"))))
			{
				continue;
			}
			HostVirtualSwitch[] switchInfo = VISDK.GetSwitchInfo(i);
			if (switchInfo == null)
			{
				continue;
			}
			HostVirtualSwitch[] array = switchInfo;
			foreach (HostVirtualSwitch hostVirtualSwitch in array)
			{
				dataGridvSwitch.Rows.Add();
				try
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchName"].Value = hostVirtualSwitch.name;
					if (logger.IsInfoEnabled)
					{
						logger.Info(currentMethod.Name + " Add Row for Switch: " + hostVirtualSwitch.name);
					}
				}
				catch
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchName"].Value = null;
				}
				try
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchMTU"].Value = hostVirtualSwitch.mtu;
				}
				catch
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchMTU"].Value = null;
				}
				try
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchNumPorts"].Value = hostVirtualSwitch.numPorts;
				}
				catch
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchNumPorts"].Value = null;
				}
				try
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchFreePorts"].Value = hostVirtualSwitch.numPortsAvailable;
				}
				catch
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchFreePorts"].Value = null;
				}
				try
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchPolicy"].Value = hostVirtualSwitch.spec.policy.nicTeaming.policy;
				}
				catch
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchPolicy"].Value = null;
				}
				try
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchNotifySwitches"].Value = hostVirtualSwitch.spec.policy.nicTeaming.notifySwitches;
				}
				catch
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchNotifySwitches"].Value = null;
				}
				try
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchReversePolicy"].Value = hostVirtualSwitch.spec.policy.nicTeaming.reversePolicy;
				}
				catch
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchReversePolicy"].Value = null;
				}
				try
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchRollingOrder"].Value = hostVirtualSwitch.spec.policy.nicTeaming.rollingOrder;
				}
				catch
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchRollingOrder"].Value = null;
				}
				try
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchCSUMOffload"].Value = hostVirtualSwitch.spec.policy.offloadPolicy.csumOffload;
				}
				catch
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchCSUMOffload"].Value = null;
				}
				try
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchTCPSegmentation"].Value = hostVirtualSwitch.spec.policy.offloadPolicy.tcpSegmentation;
				}
				catch
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchTCPSegmentation"].Value = null;
				}
				try
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchZeroCopyXmit"].Value = hostVirtualSwitch.spec.policy.offloadPolicy.zeroCopyXmit;
				}
				catch
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchZeroCopyXmit"].Value = null;
				}
				try
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchAllowPromiscuous"].Value = hostVirtualSwitch.spec.policy.security.allowPromiscuous;
				}
				catch
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchAllowPromiscuous"].Value = null;
				}
				try
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchForgedTransmits"].Value = hostVirtualSwitch.spec.policy.security.forgedTransmits;
				}
				catch
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchForgedTransmits"].Value = null;
				}
				try
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchMacChanges"].Value = hostVirtualSwitch.spec.policy.security.macChanges;
				}
				catch
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchMacChanges"].Value = null;
				}
				try
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchTrafficShaping"].Value = hostVirtualSwitch.spec.policy.shapingPolicy.enabled;
				}
				catch
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchTrafficShaping"].Value = null;
				}
				try
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchAveragebandwidth"].Value = hostVirtualSwitch.spec.policy.shapingPolicy.averageBandwidth;
				}
				catch
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchAveragebandwidth"].Value = null;
				}
				try
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchBurstSize"].Value = hostVirtualSwitch.spec.policy.shapingPolicy.burstSize;
				}
				catch
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchBurstSize"].Value = null;
				}
				try
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchPeakBandwidth"].Value = hostVirtualSwitch.spec.policy.shapingPolicy.peakBandwidth;
				}
				catch
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchPeakBandwidth"].Value = null;
				}
				try
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchHost"].Value = VISDK.GetHostInventoryProperty(i, "name");
				}
				catch
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchHost"].Value = null;
				}
				try
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchDatacenter"].Value = VISDK.GetDatacenterName(VISDK.GetHostInventoryProperty(i, "parent"));
				}
				catch
				{
					dataGridvSwitch.Rows[num].Cells["vSwitchDatacenter"].Value = null;
				}
				try
				{
					dataGridvSwitch.Rows[num].Cells["vswitchCluster"].Value = VISDK.GetClusterName(VISDK.GetHostInventoryProperty(i, "parent"));
				}
				catch
				{
					dataGridvSwitch.Rows[num].Cells["vswitchCluster"].Value = null;
				}
				dataGridvSwitch.Rows[num].Cells["vSwitchInstanceUUID"].Value = InstanceUUID;
				dataGridvSwitch.Rows[num].Cells["vSwitchVISDKServer"].Value = VISDK.Server;
				num++;
			}
		}
		dataGridvSwitch.AutoResizeColumns();
		string columnName = "vSwitchHost";
		if (dataGridvSwitch.SortedColumn != null)
		{
			columnName = dataGridvSwitch.SortedColumn.Name;
		}
		dataGridvSwitch.Sort(dataGridvSwitch.Columns[columnName], ListSortDirection.Ascending);
		StatusGridCounter.Text = num + " rows";
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Number of rows: " + num);
		}
		Telemetry.AddEvent("RenderTab", "vSwitch", new Dictionary<string, string>
		{
			{
				"RenderTime",
				DateTime.UtcNow.Subtract(utcNow).ToString()
			},
			{
				"RowCount",
				num.ToString()
			}
		});
		Cursor = Cursors.Default;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void TabPagevPort()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("ShowTab", "vPort");
		DateTime utcNow = DateTime.UtcNow;
		if (!TabPagevPort_Refresh_data)
		{
			StatusGridCounter.Text = dataGridvPort.RowCount + " rows";
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
			return;
		}
		TabPagevPort_Refresh_data = false;
		Application.DoEvents();
		Cursor = Cursors.WaitCursor;
		dataGridvPort.Rows.Clear();
		int num = 0;
		StatusFilter.Visible = FilterClass.ShowFilterIcon();
		for (int i = 0; i < VISDK.HostInventoryCounter; i++)
		{
			if (FilterClass.ShowFilterIcon() && (!FilterClass.DisplayDataCenter(VISDK.GetDatacenterName(VISDK.GetHostInventoryProperty(i, "parent"))) || !FilterClass.DisplayCluster(VISDK.GetClusterName(VISDK.GetHostInventoryProperty(i, "parent"))) || !FilterClass.DisplayHost(VISDK.GetHostInventoryProperty(i, "name"))))
			{
				continue;
			}
			HostPortGroup[] array = null;
			try
			{
				array = VISDK.GetPortInfo(i);
			}
			catch (Exception ex)
			{
				if (logger.IsInfoEnabled)
				{
					logger.Info(currentMethod.Name + " Handled exception in GetPortInfo section: " + ex.Message);
				}
				array = null;
			}
			if (array == null)
			{
				continue;
			}
			HostPortGroup[] array2 = array;
			foreach (HostPortGroup hostPortGroup in array2)
			{
				dataGridvPort.Rows.Add();
				try
				{
					dataGridvPort.Rows[num].Cells["vPortName"].Value = hostPortGroup.spec.name;
					if (logger.IsInfoEnabled)
					{
						logger.Info(currentMethod.Name + " Add Row for Port: " + hostPortGroup.spec.name);
					}
				}
				catch
				{
					dataGridvPort.Rows[num].Cells["vPortName"].Value = null;
				}
				try
				{
					dataGridvPort.Rows[num].Cells["vPortSwitch"].Value = hostPortGroup.spec.vswitchName;
				}
				catch
				{
					dataGridvPort.Rows[num].Cells["vPortSwitch"].Value = null;
				}
				try
				{
					dataGridvPort.Rows[num].Cells["vPortVLAN"].Value = hostPortGroup.spec.vlanId;
				}
				catch
				{
					dataGridvPort.Rows[num].Cells["vPortVLAN"].Value = null;
				}
				try
				{
					dataGridvPort.Rows[num].Cells["vPortAllowPromiscuous"].Value = hostPortGroup.computedPolicy.security.allowPromiscuous;
				}
				catch
				{
					dataGridvPort.Rows[num].Cells["vPortAllowPromiscuous"].Value = null;
				}
				try
				{
					dataGridvPort.Rows[num].Cells["vPortForgedTransmits"].Value = hostPortGroup.computedPolicy.security.forgedTransmits;
				}
				catch
				{
					dataGridvPort.Rows[num].Cells["vPortForgedTransmits"].Value = null;
				}
				try
				{
					dataGridvPort.Rows[num].Cells["vPortMacChanges"].Value = hostPortGroup.computedPolicy.security.macChanges;
				}
				catch
				{
					dataGridvPort.Rows[num].Cells["vPortMacChanges"].Value = null;
				}
				try
				{
					dataGridvPort.Rows[num].Cells["vPortTrafficShaping"].Value = hostPortGroup.computedPolicy.shapingPolicy.enabled;
				}
				catch
				{
					dataGridvPort.Rows[num].Cells["vPortTrafficShaping"].Value = null;
				}
				try
				{
					dataGridvPort.Rows[num].Cells["vPortAveragebandwidth"].Value = hostPortGroup.computedPolicy.shapingPolicy.averageBandwidth;
				}
				catch
				{
					dataGridvPort.Rows[num].Cells["vPortAveragebandwidth"].Value = null;
				}
				try
				{
					dataGridvPort.Rows[num].Cells["vPortBurstSize"].Value = hostPortGroup.computedPolicy.shapingPolicy.burstSize;
				}
				catch
				{
					dataGridvPort.Rows[num].Cells["vPortBurstSize"].Value = null;
				}
				try
				{
					dataGridvPort.Rows[num].Cells["vPortPeakBandwidth"].Value = hostPortGroup.computedPolicy.shapingPolicy.peakBandwidth;
				}
				catch
				{
					dataGridvPort.Rows[num].Cells["vPortPeakBandwidth"].Value = hostPortGroup.computedPolicy.shapingPolicy.peakBandwidth;
				}
				try
				{
					dataGridvPort.Rows[num].Cells["vPortPolicy"].Value = hostPortGroup.computedPolicy.nicTeaming.policy;
				}
				catch
				{
					dataGridvPort.Rows[num].Cells["vPortPolicy"].Value = null;
				}
				try
				{
					dataGridvPort.Rows[num].Cells["vPortNotifySwitches"].Value = hostPortGroup.computedPolicy.nicTeaming.notifySwitches;
				}
				catch
				{
					dataGridvPort.Rows[num].Cells["vPortNotifySwitches"].Value = null;
				}
				try
				{
					dataGridvPort.Rows[num].Cells["vPortReversePolicy"].Value = hostPortGroup.computedPolicy.nicTeaming.reversePolicy;
				}
				catch
				{
					dataGridvPort.Rows[num].Cells["vPortReversePolicy"].Value = null;
				}
				try
				{
					dataGridvPort.Rows[num].Cells["vPortRollingOrder"].Value = hostPortGroup.computedPolicy.nicTeaming.rollingOrder;
				}
				catch
				{
					dataGridvPort.Rows[num].Cells["vPortRollingOrder"].Value = null;
				}
				try
				{
					dataGridvPort.Rows[num].Cells["vPortCSUMOffload"].Value = hostPortGroup.computedPolicy.offloadPolicy.csumOffload;
				}
				catch
				{
					dataGridvPort.Rows[num].Cells["vPortCSUMOffload"].Value = null;
				}
				try
				{
					dataGridvPort.Rows[num].Cells["vPortTCPSegmentation"].Value = hostPortGroup.computedPolicy.offloadPolicy.tcpSegmentation;
				}
				catch
				{
					dataGridvPort.Rows[num].Cells["vPortTCPSegmentation"].Value = null;
				}
				try
				{
					dataGridvPort.Rows[num].Cells["vPortZeroCopyXmit"].Value = hostPortGroup.computedPolicy.offloadPolicy.zeroCopyXmit;
				}
				catch
				{
					dataGridvPort.Rows[num].Cells["vPortZeroCopyXmit"].Value = null;
				}
				try
				{
					dataGridvPort.Rows[num].Cells["vPortHost"].Value = VISDK.GetHostInventoryProperty(i, "name");
				}
				catch
				{
					dataGridvPort.Rows[num].Cells["vPortHost"].Value = null;
				}
				try
				{
					dataGridvPort.Rows[num].Cells["vPortDatacenter"].Value = VISDK.GetDatacenterName(VISDK.GetHostInventoryProperty(i, "parent"));
				}
				catch
				{
					dataGridvPort.Rows[num].Cells["vPortDatacenter"].Value = null;
				}
				try
				{
					dataGridvPort.Rows[num].Cells["vPortCluster"].Value = VISDK.GetClusterName(VISDK.GetHostInventoryProperty(i, "parent"));
				}
				catch
				{
					dataGridvPort.Rows[num].Cells["vPortCluster"].Value = null;
				}
				dataGridvPort.Rows[num].Cells["vPortInstanceUUID"].Value = InstanceUUID;
				dataGridvPort.Rows[num].Cells["vPortVISDKServer"].Value = VISDK.Server;
				num++;
			}
		}
		dataGridvPort.AutoResizeColumns();
		string columnName = "vPortHost";
		if (dataGridvPort.SortedColumn != null)
		{
			columnName = dataGridvPort.SortedColumn.Name;
		}
		dataGridvPort.Sort(dataGridvPort.Columns[columnName], ListSortDirection.Ascending);
		StatusGridCounter.Text = num + " rows";
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Number of rows: " + num);
		}
		Telemetry.AddEvent("RenderTab", "vPort", new Dictionary<string, string>
		{
			{
				"RenderTime",
				DateTime.UtcNow.Subtract(utcNow).ToString()
			},
			{
				"RowCount",
				num.ToString()
			}
		});
		Cursor = Cursors.Default;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void TabPagedvSwitch()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("ShowTab", "vSwitch");
		DateTime utcNow = DateTime.UtcNow;
		if (!TabPagedvSwitch_Refresh_data)
		{
			StatusGridCounter.Text = dataGriddvSwitch.RowCount + " rows";
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
			return;
		}
		TabPagedvSwitch_Refresh_data = false;
		if (!PreferencesClass.ExcludeAnnotations)
		{
			SortedList<string, string> customFieldHeaders = VISDK.GetCustomFieldHeaders("VmwareDistributedVirtualSwitch");
			int num = dataGriddvSwitch.Columns["dvSwitchObjectID"].Index;
			try
			{
				foreach (KeyValuePair<string, string> item in customFieldHeaders)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
					dataGridViewTextBoxColumn.Name = "CustomField" + item.Value;
					dataGridViewTextBoxColumn.HeaderText = item.Key;
					dataGridViewTextBoxColumn.Width = 200;
					if (!dataGriddvSwitch.Columns.Contains("CustomField" + item.Value))
					{
						dataGriddvSwitch.Columns.Insert(num + 1, dataGridViewTextBoxColumn);
					}
					num++;
				}
			}
			catch (Exception ex)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding CustomFieldHeaders " + ex.Message);
				}
			}
		}
		if (!PreferencesClass.ExcludeTags)
		{
			try
			{
				List<Hashtable> tagColumnNames = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "VmwareDistributedVirtualSwitch");
				int num2 = dataGriddvSwitch.Columns["dvSwitchVISDKServer"].Index - 1;
				foreach (Hashtable item2 in tagColumnNames)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
					if (!dataGriddvSwitch.Columns.Contains("dvSwitch_tags_" + item2["Name"].ToString()))
					{
						dataGridViewTextBoxColumn2.Name = "dvSwitch_tags_" + item2["Name"].ToString();
						dataGridViewTextBoxColumn2.HeaderText = item2["Name"].ToString();
						dataGridViewTextBoxColumn2.ToolTipText = item2["Desc"].ToString();
						dataGridViewTextBoxColumn2.Width = 200;
						dataGriddvSwitch.Columns.Insert(num2 + 1, dataGridViewTextBoxColumn2);
						num2++;
					}
				}
			}
			catch (Exception ex2)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding tags " + ex2.Message);
				}
			}
		}
		Application.DoEvents();
		Cursor = Cursors.WaitCursor;
		dataGriddvSwitch.Rows.Clear();
		StatusFilter.Visible = FilterClass.ShowFilterIcon();
		DVSConfigInfo dVSConfigInfo = null;
		VMwareDVSConfigInfo vMwareDVSConfigInfo = null;
		DVSSummary dVSSummary = null;
		for (int i = 0; i < VISDK.DistributedSwitchInventory.Count; i++)
		{
			try
			{
				dVSConfigInfo = VISDK.GetdvSwitchInfo(i);
			}
			catch
			{
				continue;
			}
			try
			{
				dVSSummary = VISDK.GetdvSwitchSummary(i);
			}
			catch
			{
				continue;
			}
			try
			{
				vMwareDVSConfigInfo = VISDK.GetdvVMwareSwitchInfo(i);
			}
			catch
			{
			}
			dataGriddvSwitch.Rows.Add();
			try
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchName"].Value = dVSSummary.name;
				if (logger.IsInfoEnabled)
				{
					logger.Info(currentMethod.Name + " Add Row for dvSwitch: " + dVSSummary.name);
				}
			}
			catch
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchName"].Value = null;
			}
			try
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchDatacenter"].Value = VISDK.GetDatacenterName(dVSSummary.hostMember[0].Value);
			}
			catch
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchDatacenter"].Value = null;
			}
			try
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchProductInfoName"].Value = dVSSummary.productInfo.name;
			}
			catch
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchProductInfoName"].Value = null;
			}
			try
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchProductInfoVendor"].Value = dVSSummary.productInfo.vendor;
			}
			catch
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchProductInfoVendor"].Value = null;
			}
			try
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchProductInfoVersion"].Value = dVSSummary.productInfo.version;
			}
			catch
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchProductInfoVersion"].Value = null;
			}
			try
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchDescription"].Value = dVSSummary.description;
			}
			catch
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchDescription"].Value = null;
			}
			try
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchContactInfoContact"].Value = dVSSummary.contact.contact;
			}
			catch
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchContactInfoContact"].Value = null;
			}
			try
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchContactInfoName"].Value = dVSSummary.contact.name;
			}
			catch
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchContactInfoName"].Value = null;
			}
			try
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchHostMembers"].Value = null;
				ManagedObjectReference[] hostMember = dVSSummary.hostMember;
				string text = null;
				ManagedObjectReference[] array = hostMember;
				foreach (ManagedObjectReference managedObjectReference in array)
				{
					if (text != null)
					{
						text += ", ";
					}
					text += VISDK.GetHostName(managedObjectReference.Value);
				}
				dataGriddvSwitch.Rows[i].Cells["dvSwitchHostMembers"].Value = text;
			}
			catch
			{
			}
			try
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchNumPorts"].Value = dVSSummary.numPorts;
			}
			catch
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchNumPorts"].Value = null;
			}
			try
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchNumVms"].Value = dVSSummary.vm.Length;
			}
			catch
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchNumVms"].Value = null;
			}
			try
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchCreateTime"].Value = dVSConfigInfo.createTime.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
			}
			catch
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchCreateTime"].Value = null;
			}
			try
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchInTrafficEnabled"].Value = dVSConfigInfo.defaultPortConfig.inShapingPolicy.enabled.value;
			}
			catch
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchInTrafficEnabled"].Value = null;
			}
			try
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchInTrafficAvg"].Value = dVSConfigInfo.defaultPortConfig.inShapingPolicy.averageBandwidth.value / 1000;
			}
			catch
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchInTrafficAvg"].Value = null;
			}
			try
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchInTrafficPeak"].Value = dVSConfigInfo.defaultPortConfig.inShapingPolicy.peakBandwidth.value / 1000;
			}
			catch
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchInTrafficPeak"].Value = null;
			}
			try
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchInTrafficBurst"].Value = dVSConfigInfo.defaultPortConfig.inShapingPolicy.burstSize.value / 1024;
			}
			catch
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchInTrafficBurst"].Value = null;
			}
			try
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchOutTrafficEnabled"].Value = dVSConfigInfo.defaultPortConfig.outShapingPolicy.enabled.value;
			}
			catch
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchOutTrafficEnabled"].Value = null;
			}
			try
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchOutTrafficAvg"].Value = dVSConfigInfo.defaultPortConfig.outShapingPolicy.averageBandwidth.value / 1000;
			}
			catch
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchOutTrafficAvg"].Value = null;
			}
			try
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchOutTrafficPeak"].Value = dVSConfigInfo.defaultPortConfig.outShapingPolicy.peakBandwidth.value / 1000;
			}
			catch
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchOutTrafficPeak"].Value = null;
			}
			try
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchOutTrafficBurst"].Value = dVSConfigInfo.defaultPortConfig.outShapingPolicy.burstSize.value / 1024;
			}
			catch
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchOutTrafficBurst"].Value = null;
			}
			try
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchMaxPorts"].Value = dVSConfigInfo.maxPorts;
			}
			catch
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchMaxPorts"].Value = null;
			}
			try
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchCDPProtocol"].Value = vMwareDVSConfigInfo.linkDiscoveryProtocolConfig.protocol;
			}
			catch
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchCDPProtocol"].Value = null;
			}
			try
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchCDPOperation"].Value = vMwareDVSConfigInfo.linkDiscoveryProtocolConfig.operation;
			}
			catch
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchCDPOperation"].Value = null;
			}
			try
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchMaxMtu"].Value = vMwareDVSConfigInfo.maxMtu;
			}
			catch
			{
				dataGriddvSwitch.Rows[i].Cells["dvSwitchMaxMtu"].Value = null;
			}
			string text2 = "";
			string text3 = "";
			string text4 = "";
			int num3 = 0;
			try
			{
				VMwareDvsLacpGroupConfig[] lacpGroupConfig = vMwareDVSConfigInfo.lacpGroupConfig;
				foreach (VMwareDvsLacpGroupConfig vMwareDvsLacpGroupConfig in lacpGroupConfig)
				{
					num3++;
					if (num3 == 1)
					{
						text2 = vMwareDvsLacpGroupConfig.name;
						text3 = vMwareDvsLacpGroupConfig.mode;
						text4 = vMwareDvsLacpGroupConfig.loadbalanceAlgorithm;
					}
					else
					{
						text2 = text2 + ", " + vMwareDvsLacpGroupConfig.name;
						text3 = text3 + ", " + vMwareDvsLacpGroupConfig.mode;
						text4 = text4 + ", " + vMwareDvsLacpGroupConfig.loadbalanceAlgorithm;
					}
				}
			}
			catch
			{
			}
			dataGriddvSwitch.Rows[i].Cells["dvSwitchLacpName"].Value = text2;
			dataGriddvSwitch.Rows[i].Cells["dvSwitchLacpMode"].Value = text3;
			dataGriddvSwitch.Rows[i].Cells["dvSwitchLacpLoadBalance"].Value = text4;
			if (!PreferencesClass.ExcludeAnnotations)
			{
				CustomFieldValue[] customFieldValueForDistributedSwitch = VISDK.GetCustomFieldValueForDistributedSwitch(i);
				for (int num4 = customFieldValueForDistributedSwitch.Length; num4 > 0; num4--)
				{
					CustomFieldStringValue customFieldStringValue = (CustomFieldStringValue)customFieldValueForDistributedSwitch.GetValue(num4 - 1);
					try
					{
						dataGriddvSwitch.Rows[i].Cells["CustomField" + customFieldStringValue.key].Value = customFieldStringValue.value;
					}
					catch (Exception ex3)
					{
						if (logger.IsDebugEnabled)
						{
							logger.Debug(currentMethod.Name + " Handled Exception: during setting value to custom field " + ex3.Message);
						}
					}
				}
			}
			if (!PreferencesClass.ExcludeTags)
			{
				try
				{
					dataGriddvSwitch.Rows[i].Cells["dvSwitchObjectID"].Value = VISDK.GetdvSwitchObjectID(i);
					List<Hashtable> tagColumnNames2 = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "VmwareDistributedVirtualSwitch");
					string id = VISDK.GetdvSwitchObjectID(i);
					foreach (Hashtable item3 in tagColumnNames2)
					{
						string tagColumnValues = VISDK.GetTagColumnValues(ref VISDK.cisAssociatedTag, "VmwareDistributedVirtualSwitch", item3["Name"].ToString(), id);
						dataGriddvSwitch.Rows[i].Cells["dvSwitch_tags_" + item3["Name"].ToString()].Value = tagColumnValues;
					}
				}
				catch (Exception ex4)
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(currentMethod.Name + " Handled Exception: during setting tag value " + ex4.Message);
					}
				}
			}
			dataGriddvSwitch.Rows[i].Cells["dvSwitchInstanceUUID"].Value = InstanceUUID;
			dataGriddvSwitch.Rows[i].Cells["dvSwitchVISDKServer"].Value = VISDK.Server;
		}
		dataGriddvSwitch.AutoResizeColumns();
		string columnName = "dvSwitchName";
		if (dataGriddvSwitch.SortedColumn != null)
		{
			columnName = dataGriddvSwitch.SortedColumn.Name;
		}
		dataGriddvSwitch.Sort(dataGriddvSwitch.Columns[columnName], ListSortDirection.Ascending);
		StatusGridCounter.Text = dataGriddvSwitch.RowCount + " rows";
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Number of rows: " + dataGriddvSwitch.RowCount);
		}
		Telemetry.AddEvent("RenderTab", "dvSwitch", new Dictionary<string, string>
		{
			{
				"RenderTime",
				DateTime.UtcNow.Subtract(utcNow).ToString()
			},
			{
				"RowCount",
				dataGriddvSwitch.RowCount.ToString()
			}
		});
		Cursor = Cursors.Default;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void TabPagedvPort()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("ShowTab", "svPort");
		DateTime utcNow = DateTime.UtcNow;
		if (!TabPagedvPort_Refresh_data)
		{
			StatusGridCounter.Text = dataGriddvPort.RowCount + " rows";
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
			return;
		}
		TabPagedvPort_Refresh_data = false;
		if (!PreferencesClass.ExcludeTags)
		{
			try
			{
				List<Hashtable> tagColumnNames = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "DistributedVirtualPortgroup");
				int num = dataGriddvPort.Columns["dvPortVISDKServer"].Index - 1;
				foreach (Hashtable item in tagColumnNames)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
					if (!dataGriddvPort.Columns.Contains("dvPort_tags_" + item["Name"].ToString()))
					{
						dataGridViewTextBoxColumn.Name = "dvPort_tags_" + item["Name"].ToString();
						dataGridViewTextBoxColumn.HeaderText = item["Name"].ToString();
						dataGridViewTextBoxColumn.ToolTipText = item["Desc"].ToString();
						dataGridViewTextBoxColumn.Width = 200;
						dataGriddvPort.Columns.Insert(num + 1, dataGridViewTextBoxColumn);
						num++;
					}
				}
			}
			catch (Exception ex)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding tags " + ex.Message);
				}
			}
		}
		Application.DoEvents();
		Cursor = Cursors.WaitCursor;
		dataGriddvPort.Rows.Clear();
		int num2 = 0;
		StatusFilter.Visible = FilterClass.ShowFilterIcon();
		DVPortgroupConfigInfo dVPortgroupConfigInfo = null;
		NetworkSummary networkSummary = null;
		for (int i = 0; i < VISDK.DistributedPortInventory.Count; i++)
		{
			try
			{
				dVPortgroupConfigInfo = VISDK.GetdvPortInfo(i);
			}
			catch
			{
				continue;
			}
			try
			{
				networkSummary = VISDK.GetdvPortSummary(i);
			}
			catch
			{
				continue;
			}
			dataGriddvPort.Rows.Add();
			try
			{
				dataGriddvPort.Rows[num2].Cells["dvPortName"].Value = networkSummary.name;
				if (logger.IsInfoEnabled)
				{
					logger.Info(currentMethod.Name + " Add Row for dvPort: " + networkSummary.name);
				}
			}
			catch
			{
				dataGriddvPort.Rows[num2].Cells["dvPortName"].Value = null;
			}
			try
			{
				dataGriddvPort.Rows[num2].Cells["dvPortSwitch"].Value = VISDK.GetdvSwitchName(dVPortgroupConfigInfo.distributedVirtualSwitch.Value);
			}
			catch
			{
				dataGriddvPort.Rows[num2].Cells["dvPortSwitch"].Value = null;
			}
			try
			{
				dataGriddvPort.Rows[num2].Cells["dvPortBlocked"].Value = dVPortgroupConfigInfo.defaultPortConfig.blocked.value;
			}
			catch
			{
				dataGriddvPort.Rows[num2].Cells["dvPortBlocked"].Value = null;
			}
			try
			{
				dataGriddvPort.Rows[num2].Cells["dvPortInTrafficShaping"].Value = dVPortgroupConfigInfo.defaultPortConfig.inShapingPolicy.enabled.value;
			}
			catch
			{
				dataGriddvPort.Rows[num2].Cells["dvPortInTrafficShaping"].Value = null;
			}
			try
			{
				dataGriddvPort.Rows[num2].Cells["dvPortInTrafficAverage"].Value = dVPortgroupConfigInfo.defaultPortConfig.inShapingPolicy.averageBandwidth.value / 1000;
			}
			catch
			{
				dataGriddvPort.Rows[num2].Cells["dvPortInTrafficAverage"].Value = null;
			}
			try
			{
				dataGriddvPort.Rows[num2].Cells["dvPortInTrafficPeak"].Value = dVPortgroupConfigInfo.defaultPortConfig.inShapingPolicy.peakBandwidth.value / 1000;
			}
			catch
			{
				dataGriddvPort.Rows[num2].Cells["dvPortInTrafficPeak"].Value = null;
			}
			try
			{
				dataGriddvPort.Rows[num2].Cells["dvPortInTrafficBurst"].Value = dVPortgroupConfigInfo.defaultPortConfig.inShapingPolicy.burstSize.value / 1024;
			}
			catch
			{
				dataGriddvPort.Rows[num2].Cells["dvPortInTrafficBurst"].Value = null;
			}
			try
			{
				dataGriddvPort.Rows[num2].Cells["dvPortOutTrafficShaping"].Value = dVPortgroupConfigInfo.defaultPortConfig.outShapingPolicy.enabled.value;
			}
			catch
			{
				dataGriddvPort.Rows[num2].Cells["dvPortOutTrafficShaping"].Value = null;
			}
			try
			{
				dataGriddvPort.Rows[num2].Cells["dvPortOutTrafficAverage"].Value = dVPortgroupConfigInfo.defaultPortConfig.outShapingPolicy.averageBandwidth.value / 1000;
			}
			catch
			{
				dataGriddvPort.Rows[num2].Cells["dvPortOutTrafficAverage"].Value = null;
			}
			try
			{
				dataGriddvPort.Rows[num2].Cells["dvPortOutTrafficPeak"].Value = dVPortgroupConfigInfo.defaultPortConfig.outShapingPolicy.peakBandwidth.value / 1000;
			}
			catch
			{
				dataGriddvPort.Rows[num2].Cells["dvPortOutTrafficPeak"].Value = null;
			}
			try
			{
				dataGriddvPort.Rows[num2].Cells["dvPortOutTrafficBurst"].Value = dVPortgroupConfigInfo.defaultPortConfig.outShapingPolicy.burstSize.value / 1024;
			}
			catch
			{
				dataGriddvPort.Rows[num2].Cells["dvPortOutTrafficBurst"].Value = null;
			}
			try
			{
				dataGriddvPort.Rows[num2].Cells["dvPortNumPorts"].Value = dVPortgroupConfigInfo.numPorts;
			}
			catch
			{
				dataGriddvPort.Rows[num2].Cells["dvPortNumPorts"].Value = null;
			}
			try
			{
				dataGriddvPort.Rows[num2].Cells["dvPortType"].Value = dVPortgroupConfigInfo.type;
			}
			catch
			{
				dataGriddvPort.Rows[num2].Cells["dvPortType"].Value = null;
			}
			try
			{
				dataGriddvPort.Rows[num2].Cells["dvPortBlockOverrideAllowed"].Value = dVPortgroupConfigInfo.policy.blockOverrideAllowed;
			}
			catch
			{
				dataGriddvPort.Rows[num2].Cells["dvPortBlockOverrideAllowed"].Value = null;
			}
			try
			{
				dataGriddvPort.Rows[num2].Cells["dvPortLivePortMovingAllowed"].Value = dVPortgroupConfigInfo.policy.livePortMovingAllowed;
			}
			catch
			{
				dataGriddvPort.Rows[num2].Cells["dvPortLivePortMovingAllowed"].Value = null;
			}
			try
			{
				dataGriddvPort.Rows[num2].Cells["dvPortPortConfigResetAtDisconnect"].Value = dVPortgroupConfigInfo.policy.portConfigResetAtDisconnect;
			}
			catch
			{
				dataGriddvPort.Rows[num2].Cells["dvPortPortConfigResetAtDisconnect"].Value = null;
			}
			try
			{
				dataGriddvPort.Rows[num2].Cells["dvPortShapingOverrideAllowed"].Value = dVPortgroupConfigInfo.policy.shapingOverrideAllowed;
			}
			catch
			{
				dataGriddvPort.Rows[num2].Cells["dvPortShapingOverrideAllowed"].Value = null;
			}
			try
			{
				dataGriddvPort.Rows[num2].Cells["dvPortVendorConfigOverrideAllowed"].Value = dVPortgroupConfigInfo.policy.vendorConfigOverrideAllowed;
			}
			catch
			{
				dataGriddvPort.Rows[num2].Cells["dvPortVendorConfigOverrideAllowed"].Value = null;
			}
			try
			{
				VMwareDVSPortSetting vMwareDVSPortSetting = (VMwareDVSPortSetting)dVPortgroupConfigInfo.defaultPortConfig;
				try
				{
					dataGriddvPort.Rows[num2].Cells["dvPortAllowPromiscuous"].Value = vMwareDVSPortSetting.securityPolicy.allowPromiscuous.value;
				}
				catch
				{
					dataGriddvPort.Rows[num2].Cells["dvPortAllowPromiscuous"].Value = null;
				}
				try
				{
					dataGriddvPort.Rows[num2].Cells["dvPortMacChanges"].Value = vMwareDVSPortSetting.securityPolicy.macChanges.value;
				}
				catch
				{
					dataGriddvPort.Rows[num2].Cells["dvPortMacChanges"].Value = null;
				}
				try
				{
					dataGriddvPort.Rows[num2].Cells["dvPortForgedTransmits"].Value = vMwareDVSPortSetting.securityPolicy.forgedTransmits.value;
				}
				catch
				{
					dataGriddvPort.Rows[num2].Cells["dvPortForgedTransmits"].Value = null;
				}
				try
				{
					dataGriddvPort.Rows[num2].Cells["dvPortCheckBeacon"].Value = vMwareDVSPortSetting.uplinkTeamingPolicy.failureCriteria.checkBeacon.value;
				}
				catch
				{
					dataGriddvPort.Rows[num2].Cells["dvPortCheckBeacon"].Value = null;
				}
				try
				{
					dataGriddvPort.Rows[num2].Cells["dvPortCheckDuplex"].Value = vMwareDVSPortSetting.uplinkTeamingPolicy.failureCriteria.checkDuplex.value;
				}
				catch
				{
					dataGriddvPort.Rows[num2].Cells["dvPortCheckDuplex"].Value = null;
				}
				try
				{
					dataGriddvPort.Rows[num2].Cells["dvPortCheckErrorPercent"].Value = vMwareDVSPortSetting.uplinkTeamingPolicy.failureCriteria.checkErrorPercent.value;
				}
				catch
				{
					dataGriddvPort.Rows[num2].Cells["dvPortCheckErrorPercent"].Value = null;
				}
				try
				{
					dataGriddvPort.Rows[num2].Cells["dvPortCheckSpeed"].Value = vMwareDVSPortSetting.uplinkTeamingPolicy.failureCriteria.checkSpeed.value;
				}
				catch
				{
					dataGriddvPort.Rows[num2].Cells["dvPortCheckSpeed"].Value = null;
				}
				try
				{
					dataGriddvPort.Rows[num2].Cells["dvPortFullDuplex"].Value = vMwareDVSPortSetting.uplinkTeamingPolicy.failureCriteria.fullDuplex.value;
				}
				catch
				{
					dataGriddvPort.Rows[num2].Cells["dvPortFullDuplex"].Value = null;
				}
				try
				{
					dataGriddvPort.Rows[num2].Cells["dvPortPercentage"].Value = vMwareDVSPortSetting.uplinkTeamingPolicy.failureCriteria.percentage.value;
				}
				catch
				{
					dataGriddvPort.Rows[num2].Cells["dvPortPercentage"].Value = null;
				}
				try
				{
					dataGriddvPort.Rows[num2].Cells["dvPortSpeed"].Value = vMwareDVSPortSetting.uplinkTeamingPolicy.failureCriteria.speed.value;
				}
				catch
				{
					dataGriddvPort.Rows[num2].Cells["dvPortSpeed"].Value = null;
				}
				try
				{
					dataGriddvPort.Rows[num2].Cells["dvPortNotifySwitch"].Value = vMwareDVSPortSetting.uplinkTeamingPolicy.notifySwitches.value;
				}
				catch
				{
					dataGriddvPort.Rows[num2].Cells["dvPortNotifySwitch"].Value = null;
				}
				try
				{
					dataGriddvPort.Rows[num2].Cells["dvPortPolicy"].Value = vMwareDVSPortSetting.uplinkTeamingPolicy.policy.value;
				}
				catch
				{
					dataGriddvPort.Rows[num2].Cells["dvPortPolicy"].Value = null;
				}
				try
				{
					dataGriddvPort.Rows[num2].Cells["dvPortReversePolicy"].Value = vMwareDVSPortSetting.uplinkTeamingPolicy.reversePolicy.value;
				}
				catch
				{
					dataGriddvPort.Rows[num2].Cells["dvPortReversePolicy"].Value = null;
				}
				try
				{
					dataGriddvPort.Rows[num2].Cells["dvPortRollingOrder"].Value = vMwareDVSPortSetting.uplinkTeamingPolicy.rollingOrder.value;
				}
				catch
				{
					dataGriddvPort.Rows[num2].Cells["dvPortRollingOrder"].Value = null;
				}
				string text = null;
				try
				{
					int num3 = 0;
					string[] activeUplinkPort = vMwareDVSPortSetting.uplinkTeamingPolicy.uplinkPortOrder.activeUplinkPort;
					foreach (string text2 in activeUplinkPort)
					{
						num3++;
						if (num3 > 1)
						{
							text += ", ";
						}
						text += text2;
					}
				}
				catch
				{
				}
				try
				{
					dataGriddvPort.Rows[num2].Cells["dvPortActiveUplink"].Value = text;
				}
				catch
				{
					dataGriddvPort.Rows[num2].Cells["dvPortActiveUplink"].Value = null;
				}
				string text3 = null;
				try
				{
					int num4 = 0;
					string[] activeUplinkPort = vMwareDVSPortSetting.uplinkTeamingPolicy.uplinkPortOrder.standbyUplinkPort;
					foreach (string text4 in activeUplinkPort)
					{
						num4++;
						if (num4 > 1)
						{
							text3 += ", ";
						}
						text3 += text4;
					}
				}
				catch
				{
				}
				try
				{
					dataGriddvPort.Rows[num2].Cells["dvPortStandbyUplink"].Value = text3;
				}
				catch
				{
					dataGriddvPort.Rows[num2].Cells["dvPortStandbyUplink"].Value = null;
				}
				try
				{
					VmwareDistributedVirtualSwitchVlanIdSpec vmwareDistributedVirtualSwitchVlanIdSpec = (VmwareDistributedVirtualSwitchVlanIdSpec)vMwareDVSPortSetting.vlan;
					try
					{
						dataGriddvPort.Rows[num2].Cells["dvPortVLAN"].Value = vmwareDistributedVirtualSwitchVlanIdSpec.vlanId;
					}
					catch
					{
						dataGriddvPort.Rows[num2].Cells["dvPortVLAN"].Value = null;
					}
				}
				catch
				{
				}
				try
				{
					VMwareDVSPortgroupPolicy vMwareDVSPortgroupPolicy = (VMwareDVSPortgroupPolicy)dVPortgroupConfigInfo.policy;
					try
					{
						dataGriddvPort.Rows[num2].Cells["dvPortSecurityPolicyOverrideAllowed"].Value = vMwareDVSPortgroupPolicy.securityPolicyOverrideAllowed;
					}
					catch
					{
						dataGriddvPort.Rows[num2].Cells["dvPortSecurityPolicyOverrideAllowed"].Value = null;
					}
					try
					{
						dataGriddvPort.Rows[num2].Cells["dvPortUplinkTeamingOverrideAllowed"].Value = vMwareDVSPortgroupPolicy.uplinkTeamingOverrideAllowed;
					}
					catch
					{
						dataGriddvPort.Rows[num2].Cells["dvPortUplinkTeamingOverrideAllowed"].Value = null;
					}
					try
					{
						dataGriddvPort.Rows[num2].Cells["dvPortVlanOverrideAllowed"].Value = vMwareDVSPortgroupPolicy.vlanOverrideAllowed;
					}
					catch
					{
						dataGriddvPort.Rows[num2].Cells["dvPortVlanOverrideAllowed"].Value = null;
					}
				}
				catch
				{
				}
			}
			catch
			{
			}
			if (!PreferencesClass.ExcludeTags)
			{
				try
				{
					dataGriddvPort.Rows[i].Cells["dvPortObjectID"].Value = VISDK.GetdvPortObjectID(i);
					List<Hashtable> tagColumnNames2 = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "DistributedVirtualPortgroup");
					string id = VISDK.GetdvPortObjectID(i);
					foreach (Hashtable item2 in tagColumnNames2)
					{
						string tagColumnValues = VISDK.GetTagColumnValues(ref VISDK.cisAssociatedTag, "DistributedVirtualPortgroup", item2["Name"].ToString(), id);
						dataGriddvPort.Rows[num2].Cells["dvPort_tags_" + item2["Name"].ToString()].Value = tagColumnValues;
					}
				}
				catch (Exception ex2)
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(currentMethod.Name + " Handled Exception: during setting tag value " + ex2.Message);
					}
				}
			}
			dataGriddvPort.Rows[num2].Cells["dvPortInstanceUUID"].Value = InstanceUUID;
			dataGriddvPort.Rows[num2].Cells["dvPortVISDKServer"].Value = VISDK.Server;
			num2++;
		}
		dataGriddvPort.AutoResizeColumns();
		string columnName = "dvPortName";
		if (dataGriddvPort.SortedColumn != null)
		{
			columnName = dataGriddvPort.SortedColumn.Name;
		}
		dataGriddvPort.Sort(dataGriddvPort.Columns[columnName], ListSortDirection.Ascending);
		StatusGridCounter.Text = num2 + " rows";
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Number of rows: " + num2);
		}
		Telemetry.AddEvent("RenderTab", "dvPort", new Dictionary<string, string>
		{
			{
				"RenderTime",
				DateTime.UtcNow.Subtract(utcNow).ToString()
			},
			{
				"RowCount",
				num2.ToString()
			}
		});
		Cursor = Cursors.Default;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void TabPagevSC_VMK()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("ShowTab", "vSC_VMK");
		DateTime utcNow = DateTime.UtcNow;
		if (!TabPagevSC_VMK_Refresh_data)
		{
			StatusGridCounter.Text = dataGridvSC_VMK.RowCount + " rows";
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
			return;
		}
		TabPagevSC_VMK_Refresh_data = false;
		Application.DoEvents();
		Cursor = Cursors.WaitCursor;
		dataGridvSC_VMK.Rows.Clear();
		int num = 0;
		StatusFilter.Visible = FilterClass.ShowFilterIcon();
		for (int i = 0; i < VISDK.HostInventoryCounter; i++)
		{
			if (FilterClass.ShowFilterIcon() && (!FilterClass.DisplayDataCenter(VISDK.GetDatacenterName(VISDK.GetHostInventoryProperty(i, "parent"))) || !FilterClass.DisplayCluster(VISDK.GetClusterName(VISDK.GetHostInventoryProperty(i, "parent"))) || !FilterClass.DisplayHost(VISDK.GetHostInventoryProperty(i, "name"))))
			{
				continue;
			}
			HostVirtualNic[] consoleInfo = VISDK.GetConsoleInfo(i);
			HostIpRouteConfig consoleGateway = VISDK.GetConsoleGateway(i);
			if (consoleInfo == null)
			{
				continue;
			}
			HostVirtualNic[] array = consoleInfo;
			foreach (HostVirtualNic hostVirtualNic in array)
			{
				dataGridvSC_VMK.Rows.Add();
				string hostInventoryProperty = VISDK.GetHostInventoryProperty(i, "name");
				if (logger.IsInfoEnabled)
				{
					logger.Info(currentMethod.Name + " Add Row for SC_VMK: " + hostInventoryProperty);
				}
				dataGridvSC_VMK.Rows[num].Cells["vSC_VMKHost"].Value = hostInventoryProperty;
				dataGridvSC_VMK.Rows[num].Cells["vSC_VMKDatacenter"].Value = VISDK.GetDatacenterName(VISDK.GetHostInventoryProperty(i, "parent"));
				dataGridvSC_VMK.Rows[num].Cells["vSC_VMKCluster"].Value = VISDK.GetClusterName(VISDK.GetHostInventoryProperty(i, "parent"));
				try
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKPortGroup"].Value = hostVirtualNic.portgroup;
				}
				catch
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKPortGroup"].Value = null;
				}
				try
				{
					if (hostVirtualNic.portgroup == "")
					{
						try
						{
							dataGridvSC_VMK.Rows[num].Cells["vSC_VMKPortGroup"].Value = VISDK.GetDSPortNameV2(hostVirtualNic.spec.distributedVirtualPort.portgroupKey);
						}
						catch
						{
							dataGridvSC_VMK.Rows[num].Cells["vSC_VMKPortGroup"].Value = null;
						}
					}
				}
				catch
				{
				}
				try
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKDevice"].Value = hostVirtualNic.device;
				}
				catch
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKDevice"].Value = null;
				}
				try
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKMAC"].Value = hostVirtualNic.spec.mac;
				}
				catch
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKMAC"].Value = null;
				}
				try
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKDHCP"].Value = hostVirtualNic.spec.ip.dhcp;
				}
				catch
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKDHCP"].Value = null;
				}
				try
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKIP"].Value = hostVirtualNic.spec.ip.ipAddress;
				}
				catch
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKIP"].Value = null;
				}
				try
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKIP6"].Value = hostVirtualNic.spec.ip.ipV6Config.ipV6Address[0].ipAddress;
				}
				catch
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKIP6"].Value = null;
				}
				try
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKGateway"].Value = consoleGateway.defaultGateway;
				}
				catch
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKGateway"].Value = null;
				}
				try
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKGateway6"].Value = consoleGateway.ipV6DefaultGateway;
				}
				catch
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKGateway6"].Value = null;
				}
				try
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKMask"].Value = hostVirtualNic.spec.ip.subnetMask;
				}
				catch
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKMask"].Value = null;
				}
				try
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKMTU"].Value = hostVirtualNic.spec.mtu;
				}
				catch
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKMTU"].Value = null;
				}
				dataGridvSC_VMK.Rows[num].Cells["vSC_VMKInstanceUUID"].Value = InstanceUUID;
				dataGridvSC_VMK.Rows[num].Cells["vSC_VMKVISDKServer"].Value = VISDK.Server;
				num++;
			}
			HostVirtualNic[] vMKernelInfo = VISDK.GetVMKernelInfo(i);
			HostIpRouteConfig vMKernelGateway = VISDK.GetVMKernelGateway(i);
			if (vMKernelInfo == null)
			{
				continue;
			}
			array = vMKernelInfo;
			foreach (HostVirtualNic hostVirtualNic2 in array)
			{
				dataGridvSC_VMK.Rows.Add();
				string hostInventoryProperty2 = VISDK.GetHostInventoryProperty(i, "name");
				if (logger.IsInfoEnabled)
				{
					logger.Info(currentMethod.Name + " Add Row for SC_VMK: " + hostInventoryProperty2);
				}
				dataGridvSC_VMK.Rows[num].Cells["vSC_VMKHost"].Value = VISDK.GetHostInventoryProperty(i, "name");
				dataGridvSC_VMK.Rows[num].Cells["vSC_VMKDatacenter"].Value = VISDK.GetDatacenterName(VISDK.GetHostInventoryProperty(i, "parent"));
				dataGridvSC_VMK.Rows[num].Cells["vSC_VMKCluster"].Value = VISDK.GetClusterName(VISDK.GetHostInventoryProperty(i, "parent"));
				try
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKPortGroup"].Value = hostVirtualNic2.portgroup;
				}
				catch
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKPortGroup"].Value = null;
				}
				try
				{
					if (hostVirtualNic2.portgroup == "")
					{
						try
						{
							dataGridvSC_VMK.Rows[num].Cells["vSC_VMKPortGroup"].Value = VISDK.GetDSPortNameV2(hostVirtualNic2.spec.distributedVirtualPort.portgroupKey);
						}
						catch
						{
							dataGridvSC_VMK.Rows[num].Cells["vSC_VMKPortGroup"].Value = null;
						}
					}
				}
				catch
				{
				}
				try
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKDevice"].Value = hostVirtualNic2.device;
				}
				catch
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKDevice"].Value = null;
				}
				try
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKMAC"].Value = hostVirtualNic2.spec.mac;
				}
				catch
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKMAC"].Value = null;
				}
				try
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKDHCP"].Value = hostVirtualNic2.spec.ip.dhcp;
				}
				catch
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKDHCP"].Value = null;
				}
				try
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKIP"].Value = hostVirtualNic2.spec.ip.ipAddress;
				}
				catch
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKIP"].Value = null;
				}
				try
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKIP6"].Value = hostVirtualNic2.spec.ip.ipV6Config.ipV6Address[0].ipAddress;
				}
				catch
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKIP6"].Value = null;
				}
				try
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKGateway"].Value = vMKernelGateway.defaultGateway;
				}
				catch
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKGateway"].Value = null;
				}
				try
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKGateway6"].Value = vMKernelGateway.ipV6DefaultGateway;
				}
				catch
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKGateway6"].Value = null;
				}
				try
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKMask"].Value = hostVirtualNic2.spec.ip.subnetMask;
				}
				catch
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKMask"].Value = null;
				}
				try
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKMTU"].Value = hostVirtualNic2.spec.mtu;
				}
				catch
				{
					dataGridvSC_VMK.Rows[num].Cells["vSC_VMKMTU"].Value = null;
				}
				dataGridvSC_VMK.Rows[num].Cells["vSC_VMKInstanceUUID"].Value = InstanceUUID;
				dataGridvSC_VMK.Rows[num].Cells["vSC_VMKVISDKServer"].Value = VISDK.Server;
				num++;
			}
		}
		dataGridvSC_VMK.AutoResizeColumns();
		string columnName = "vSC_VMKHost";
		if (dataGridvSC_VMK.SortedColumn != null)
		{
			columnName = dataGridvSC_VMK.SortedColumn.Name;
		}
		dataGridvSC_VMK.Sort(dataGridvSC_VMK.Columns[columnName], ListSortDirection.Ascending);
		StatusGridCounter.Text = num + " rows";
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Number of rows: " + num);
		}
		Telemetry.AddEvent("RenderTab", "vSC_VMK", new Dictionary<string, string>
		{
			{
				"RenderTime",
				DateTime.UtcNow.Subtract(utcNow).ToString()
			},
			{
				"RowCount",
				num.ToString()
			}
		});
		Cursor = Cursors.Default;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void TabPagevDatastore()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("ShowTab", "vDatastore");
		DateTime utcNow = DateTime.UtcNow;
		if (!TabPagevDatastore_Refresh_data)
		{
			StatusGridCounter.Text = dataGridvDatastore.RowCount + " rows";
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
			return;
		}
		TabPagevDatastore_Refresh_data = false;
		Application.DoEvents();
		Cursor = Cursors.WaitCursor;
		dataGridvDatastore.Rows.Clear();
		int num = 0;
		StatusFilter.Visible = false;
		if (!PreferencesClass.ExcludeAnnotations)
		{
			SortedList<string, string> customFieldHeaders = VISDK.GetCustomFieldHeaders("Datastore");
			int num2 = dataGridvDatastore.Columns["vDatastoreObjectID"].Index;
			try
			{
				foreach (KeyValuePair<string, string> item in customFieldHeaders)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
					dataGridViewTextBoxColumn.Name = "CustomField" + item.Value;
					dataGridViewTextBoxColumn.HeaderText = item.Key;
					dataGridViewTextBoxColumn.Width = 200;
					if (!dataGridvDatastore.Columns.Contains("CustomField" + item.Value))
					{
						dataGridvDatastore.Columns.Insert(num2 + 1, dataGridViewTextBoxColumn);
					}
					num2++;
				}
			}
			catch (Exception ex)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding CustomFieldHeaders " + ex.Message);
				}
			}
		}
		if (!PreferencesClass.ExcludeTags)
		{
			try
			{
				List<Hashtable> tagColumnNames = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "Datastore");
				int num3 = dataGridvDatastore.Columns["vDatastoreVISDKServer"].Index - 1;
				foreach (Hashtable item2 in tagColumnNames)
				{
					DataGridViewTextBoxColumn dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
					if (!dataGridvDatastore.Columns.Contains("vDatastore_tags_" + item2["Name"].ToString()))
					{
						dataGridViewTextBoxColumn2.Name = "vDatastore_tags_" + item2["Name"].ToString();
						dataGridViewTextBoxColumn2.HeaderText = item2["Name"].ToString();
						dataGridViewTextBoxColumn2.ToolTipText = item2["Desc"].ToString();
						dataGridViewTextBoxColumn2.Width = 200;
						dataGridvDatastore.Columns.Insert(num3 + 1, dataGridViewTextBoxColumn2);
						num3++;
					}
				}
			}
			catch (Exception ex2)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception: during adding tags " + ex2.Message);
				}
			}
		}
		for (int i = 0; i < VISDK.DatastoreInventoryCounter; i++)
		{
			dataGridvDatastore.Rows.Add();
			string datastoreInventoryProperty = VISDK.GetDatastoreInventoryProperty(i, "summary.name");
			if (logger.IsInfoEnabled)
			{
				logger.Info(currentMethod.Name + " Add Row for Datastore: " + datastoreInventoryProperty);
			}
			dataGridvDatastore.Rows[num].Cells["vDatastoreName"].Value = datastoreInventoryProperty;
			dataGridvDatastore.Rows[num].Cells["vDatastoreConfigStatus"].Value = VISDK.GetDatastoreInventoryProperty(i, "configStatus");
			dataGridvDatastore.Rows[num].Cells["vDatastoreAccessible"].Value = VISDK.GetDatastoreInventoryProperty(i, "summary.accessible");
			dataGridvDatastore.Rows[num].Cells["vDatastoreType"].Value = VISDK.GetDatastoreInventoryProperty(i, "summary.type");
			dataGridvDatastore.Rows[num].Cells["vDatastoreVMs"].Value = VISDK.GetDatastoreInventoryPropertyInt(i, "vm");
			dataGridvDatastore.Rows[num].Cells["vDatastoreVMsTotal"].Value = VISDK.GetDatastoreInventoryPropertyInt(i, "vmTotal");
			try
			{
				dataGridvDatastore.Rows[num].Cells["vDatastoreCapacity"].Value = VISDK.GetDatastoreInventoryPropertyInt(i, "summary.capacity") / 1048576;
			}
			catch
			{
				dataGridvDatastore.Rows[num].Cells["vDatastoreCapacity"].Value = null;
			}
			try
			{
				dataGridvDatastore.Rows[num].Cells["vDatastoreFreeSpace"].Value = VISDK.GetDatastoreInventoryPropertyInt(i, "summary.freeSpace") / 1048576;
			}
			catch
			{
				dataGridvDatastore.Rows[num].Cells["vDatastoreFreeSpace"].Value = null;
			}
			long value = 0L;
			try
			{
				value = VISDK.GetDatastoreInventoryPropertyInt(i, "summary.freeSpace") / (VISDK.GetDatastoreInventoryPropertyInt(i, "summary.capacity") / 100);
			}
			catch
			{
			}
			try
			{
				dataGridvDatastore.Rows[num].Cells["vDatastoreFreePercentage"].Value = Convert.ToInt16(value);
			}
			catch
			{
				dataGridvDatastore.Rows[num].Cells["vDatastoreFreePercentage"].Value = null;
			}
			try
			{
				dataGridvDatastore.Rows[num].Cells["vDatastoreNumberOfHosts"].Value = VISDK.GetVMFSInfoInt(i, "NumberOfHosts");
			}
			catch
			{
				dataGridvDatastore.Rows[num].Cells["vDatastoreNumberOfHosts"].Value = null;
			}
			try
			{
				dataGridvDatastore.Rows[num].Cells["vDatastoreHosts"].Value = VISDK.GetVMFSInfo(i, "hosts");
			}
			catch
			{
				dataGridvDatastore.Rows[num].Cells["vDatastoreHosts"].Value = null;
			}
			try
			{
				dataGridvDatastore.Rows[num].Cells["vDatastoreBlockSizeMiB"].Value = VISDK.GetVMFSInfoInt(i, "blockSizeMb");
			}
			catch
			{
				dataGridvDatastore.Rows[num].Cells["vDatastoreBlockSizeMiB"].Value = null;
			}
			try
			{
				dataGridvDatastore.Rows[num].Cells["vDatastoreMaxBlocks"].Value = VISDK.GetVMFSInfoInt(i, "maxBlocks");
			}
			catch
			{
				dataGridvDatastore.Rows[num].Cells["vDatastoreMaxBlocks"].Value = null;
			}
			try
			{
				dataGridvDatastore.Rows[num].Cells["vDatastoreExtents"].Value = VISDK.GetVMFSInfoInt(i, "extents");
			}
			catch
			{
				dataGridvDatastore.Rows[num].Cells["vDatastoreExtents"].Value = null;
			}
			try
			{
				dataGridvDatastore.Rows[num].Cells["vDatastoreAddress"].Value = VISDK.GetVMFSInfo(i, "lunid");
			}
			catch
			{
				dataGridvDatastore.Rows[num].Cells["vDatastoreAddress"].Value = null;
			}
			if (VISDK.GetDatastoreInventoryProperty(i, "summary.type") == "NFS")
			{
				try
				{
					NasDatastoreInfo nasDatastoreInfo = (NasDatastoreInfo)VISDK.GetDataStoreInfo(i);
					string value2 = nasDatastoreInfo.nas.remoteHost + " " + nasDatastoreInfo.nas.remotePath;
					dataGridvDatastore.Rows[num].Cells["vDatastoreAddress"].Value = value2;
				}
				catch
				{
				}
			}
			try
			{
				dataGridvDatastore.Rows[num].Cells["vDatastoreMajorVersion"].Value = VISDK.GetVMFSInfoInt(i, "majorVersion");
			}
			catch
			{
				dataGridvDatastore.Rows[num].Cells["vDatastoreMajorVersion"].Value = null;
			}
			try
			{
				dataGridvDatastore.Rows[num].Cells["vDatastoreVersion"].Value = VISDK.GetVMFSInfo(i, "version");
			}
			catch
			{
				dataGridvDatastore.Rows[num].Cells["vDatastoreVersion"].Value = null;
			}
			try
			{
				dataGridvDatastore.Rows[num].Cells["vDatastoreVMFSUpgradable"].Value = VISDK.GetVMFSInfo(i, "vmfsUpgradable");
			}
			catch
			{
				dataGridvDatastore.Rows[num].Cells["vDatastoreVMFSUpgradable"].Value = null;
			}
			long Uncommitted = 0L;
			long num4 = 0L;
			try
			{
				num4 = VISDK.GetDatastoreInventoryPropertyInt(i, "summary.capacity");
				num4 -= VISDK.GetDatastoreInventoryPropertyInt(i, "summary.freeSpace");
				dataGridvDatastore.Rows[num].Cells["vDatastoreInUse"].Value = num4 / 1048576;
			}
			catch
			{
				dataGridvDatastore.Rows[num].Cells["vDatastoreInUse"].Value = null;
			}
			try
			{
				VISDK.GetDatastoreProvisionedSpace(i, ref Uncommitted);
				long num5 = num4 + Uncommitted;
				dataGridvDatastore.Rows[num].Cells["vDatastoreProvisioned"].Value = num5 / 1048576;
			}
			catch
			{
				dataGridvDatastore.Rows[num].Cells["vDatastoreProvisioned"].Value = null;
			}
			try
			{
				StorageIORMInfo sIOCInfo = VISDK.GetSIOCInfo(i);
				dataGridvDatastore.Rows[num].Cells["vDatastoreSIOCFlag"].Value = sIOCInfo.enabled;
				dataGridvDatastore.Rows[num].Cells["vDatastoreSIOCThreshold"].Value = sIOCInfo.congestionThreshold;
			}
			catch
			{
			}
			try
			{
				ManagedObjectReference storagePodMor = VISDK.GetStoragePodMor(i);
				if (storagePodMor.type == "StoragePod")
				{
					DynamicProperty[] propSet = VISDK.GetStoragePodInfo(storagePodMor)[0].propSet;
					for (int j = 0; j < propSet.Length; j++)
					{
						if (propSet[j].name == "summary")
						{
							StoragePodSummary storagePodSummary = (StoragePodSummary)propSet[j].val;
							dataGridvDatastore.Rows[num].Cells["vDatastoreClusterName"].Value = storagePodSummary.name;
							dataGridvDatastore.Rows[num].Cells["vDatastoreClusterCapacity"].Value = storagePodSummary.capacity / 1048576;
							dataGridvDatastore.Rows[num].Cells["vDatastoreClusterFreeSpace"].Value = storagePodSummary.freeSpace / 1048576;
							break;
						}
					}
				}
			}
			catch
			{
			}
			dataGridvDatastore.Rows[num].Cells["vDatastoreMultipleHostAccess"].Value = VISDK.GetDatastoreInventoryProperty(i, "summary.multipleHostAccess");
			dataGridvDatastore.Rows[num].Cells["vDatastoreURL"].Value = VISDK.GetDatastoreInventoryProperty(i, "summary.url");
			if (!PreferencesClass.ExcludeAnnotations)
			{
				CustomFieldValue[] customFieldValueForDatastore = VISDK.GetCustomFieldValueForDatastore(i);
				for (int num6 = customFieldValueForDatastore.Length; num6 > 0; num6--)
				{
					CustomFieldStringValue customFieldStringValue = (CustomFieldStringValue)customFieldValueForDatastore.GetValue(num6 - 1);
					try
					{
						dataGridvDatastore.Rows[num].Cells["CustomField" + customFieldStringValue.key].Value = customFieldStringValue.value;
					}
					catch (Exception ex3)
					{
						if (logger.IsDebugEnabled)
						{
							logger.Debug(currentMethod.Name + " Handled Exception: during setting value to custom field " + ex3.Message);
						}
					}
				}
			}
			if (!PreferencesClass.ExcludeTags)
			{
				try
				{
					dataGridvDatastore.Rows[num].Cells["vDatastoreObjectID"].Value = VISDK.GetDatastoreObjectID(i);
					List<Hashtable> tagColumnNames2 = VISDK.GetTagColumnNames(ref VISDK.cisAssociatedTag, "Datastore");
					string datastoreObjectID = VISDK.GetDatastoreObjectID(i);
					foreach (Hashtable item3 in tagColumnNames2)
					{
						string tagColumnValues = VISDK.GetTagColumnValues(ref VISDK.cisAssociatedTag, "Datastore", item3["Name"].ToString(), datastoreObjectID);
						dataGridvDatastore.Rows[num].Cells["vDatastore_tags_" + item3["Name"].ToString()].Value = tagColumnValues;
					}
				}
				catch (Exception ex4)
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(currentMethod.Name + " Handled Exception: during setting tag value " + ex4.Message);
					}
				}
			}
			dataGridvDatastore.Rows[num].Cells["vDatastoreInstanceUUID"].Value = InstanceUUID;
			dataGridvDatastore.Rows[num].Cells["vDatastoreVISDKServer"].Value = VISDK.Server;
			num++;
		}
		dataGridvDatastore.AutoResizeColumns();
		string columnName = "vDatastoreName";
		if (dataGridvDatastore.SortedColumn != null)
		{
			columnName = dataGridvDatastore.SortedColumn.Name;
		}
		dataGridvDatastore.Sort(dataGridvDatastore.Columns[columnName], ListSortDirection.Ascending);
		StatusGridCounter.Text = num + " rows";
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Number of rows: " + num);
		}
		Telemetry.AddEvent("RenderTab", "vDatastore", new Dictionary<string, string>
		{
			{
				"RenderTime",
				DateTime.UtcNow.Subtract(utcNow).ToString()
			},
			{
				"RowCount",
				num.ToString()
			}
		});
		Cursor = Cursors.Default;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void TabPagevMultiPath()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("ShowTab", "vMultiPath");
		DateTime utcNow = DateTime.UtcNow;
		if (!TabPagevMultiPath_Refresh_data)
		{
			StatusGridCounter.Text = dataGridvMultiPath.RowCount + " rows";
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
			return;
		}
		TabPagevMultiPath_Refresh_data = false;
		Application.DoEvents();
		Cursor = Cursors.WaitCursor;
		dataGridvMultiPath.Rows.Clear();
		int num = 0;
		StatusFilter.Visible = false;
		for (int i = 0; i < VISDK.HostInventoryCounter; i++)
		{
			if (FilterClass.ShowFilterIcon() && (!FilterClass.DisplayDataCenter(VISDK.GetDatacenterName(VISDK.GetHostInventoryProperty(i, "parent"))) || !FilterClass.DisplayCluster(VISDK.GetClusterName(VISDK.GetHostInventoryProperty(i, "parent"))) || !FilterClass.DisplayHost(VISDK.GetHostInventoryProperty(i, "name"))))
			{
				continue;
			}
			ManagedObjectReference[] array = null;
			try
			{
				array = VISDK.GetHostDatastoreMORs(i);
			}
			catch
			{
			}
			ScsiLun[] array2 = null;
			try
			{
				array2 = VISDK.GetHostScsiLun(i);
			}
			catch
			{
			}
			HostMultipathInfoLogicalUnit[] array3 = null;
			try
			{
				array3 = VISDK.GetHostMultipathInfo(i).lun;
			}
			catch
			{
			}
			HostHostBusAdapter[] array4 = null;
			try
			{
				array4 = VISDK.GetHostBusAdapter(i);
			}
			catch
			{
			}
			if (array == null)
			{
				continue;
			}
			ManagedObjectReference[] array5 = array;
			foreach (ManagedObjectReference managedObjectReference in array5)
			{
				VmfsDatastoreInfo vmfsDatastoreInfo = null;
				vmfsDatastoreInfo = VISDK.GetHostDatastoreInfo(managedObjectReference);
				if (vmfsDatastoreInfo == null)
				{
					continue;
				}
				dataGridvMultiPath.Rows.Add();
				string hostInventoryProperty = VISDK.GetHostInventoryProperty(i, "name");
				if (logger.IsInfoEnabled)
				{
					logger.Info(currentMethod.Name + " Add Row for datastore: " + managedObjectReference.Value);
				}
				dataGridvMultiPath.Rows[num].Cells["vMultiPathHost"].Value = hostInventoryProperty;
				dataGridvMultiPath.Rows[num].Cells["vMultiPathDatacenter"].Value = VISDK.GetDatacenterName(VISDK.GetHostInventoryProperty(i, "parent"));
				dataGridvMultiPath.Rows[num].Cells["vMultiPathCluster"].Value = VISDK.GetClusterName(VISDK.GetHostInventoryProperty(i, "parent"));
				try
				{
					dataGridvMultiPath.Rows[num].Cells["vMultiPathDatastoreName"].Value = vmfsDatastoreInfo.name;
				}
				catch
				{
				}
				try
				{
					dataGridvMultiPath.Rows[num].Cells["vMultiPathDiskName"].Value = vmfsDatastoreInfo.vmfs.extent[0].diskName;
				}
				catch
				{
				}
				if (array2 != null)
				{
					ScsiLun[] array6 = array2;
					foreach (ScsiLun scsiLun in array6)
					{
						if (!(scsiLun.canonicalName == vmfsDatastoreInfo.vmfs.extent[0].diskName))
						{
							continue;
						}
						try
						{
							dataGridvMultiPath.Rows[num].Cells["vMultiPathDisplayName"].Value = scsiLun.displayName;
						}
						catch
						{
						}
						try
						{
							dataGridvMultiPath.Rows[num].Cells["vMultiPathModel"].Value = scsiLun.model;
						}
						catch
						{
						}
						try
						{
							dataGridvMultiPath.Rows[num].Cells["vMultiPathOperationalState"].Value = scsiLun.operationalState[0];
						}
						catch
						{
						}
						try
						{
							dataGridvMultiPath.Rows[num].Cells["vMultiPathRevision"].Value = scsiLun.revision;
						}
						catch
						{
						}
						try
						{
							dataGridvMultiPath.Rows[num].Cells["vMultiPathSCSILevel"].Value = scsiLun.scsiLevel;
						}
						catch
						{
						}
						try
						{
							dataGridvMultiPath.Rows[num].Cells["vMultiPathUUID"].Value = scsiLun.uuid;
						}
						catch
						{
						}
						try
						{
							dataGridvMultiPath.Rows[num].Cells["vMultiPathVendor"].Value = scsiLun.vendor;
						}
						catch
						{
						}
						try
						{
							dataGridvMultiPath.Rows[num].Cells["vMultiPathVStorageSupport"].Value = scsiLun.vStorageSupport;
						}
						catch
						{
						}
						try
						{
							dataGridvMultiPath.Rows[num].Cells["vMultiPathQueueDepth"].Value = scsiLun.queueDepth;
						}
						catch
						{
						}
						try
						{
							dataGridvMultiPath.Rows[num].Cells["vMultiPathSerialNumber"].Value = scsiLun.serialNumber;
						}
						catch
						{
						}
						if (array3 == null)
						{
							continue;
						}
						HostMultipathInfoLogicalUnit[] array7 = array3;
						foreach (HostMultipathInfoLogicalUnit hostMultipathInfoLogicalUnit in array7)
						{
							if (!(scsiLun.key == hostMultipathInfoLogicalUnit.lun))
							{
								continue;
							}
							try
							{
								dataGridvMultiPath.Rows[num].Cells["vMultiPathPolicy"].Value = hostMultipathInfoLogicalUnit.policy.policy;
							}
							catch
							{
							}
							HostHostBusAdapter[] array8;
							if (hostMultipathInfoLogicalUnit.path.Length >= 1)
							{
								try
								{
									dataGridvMultiPath.Rows[num].Cells["vMultiPathPath1State"].Value = hostMultipathInfoLogicalUnit.path[0].state;
								}
								catch
								{
								}
								if (array4 != null)
								{
									array8 = array4;
									foreach (HostHostBusAdapter hostHostBusAdapter in array8)
									{
										if (hostMultipathInfoLogicalUnit.path[0].adapter == hostHostBusAdapter.key)
										{
											try
											{
												dataGridvMultiPath.Rows[num].Cells["vMultiPathPath1"].Value = hostHostBusAdapter.device;
											}
											catch
											{
											}
										}
									}
								}
							}
							if (hostMultipathInfoLogicalUnit.path.Length >= 2)
							{
								try
								{
									dataGridvMultiPath.Rows[num].Cells["vMultiPathPath2State"].Value = hostMultipathInfoLogicalUnit.path[1].state;
								}
								catch
								{
								}
								if (array4 != null)
								{
									array8 = array4;
									foreach (HostHostBusAdapter hostHostBusAdapter2 in array8)
									{
										if (hostMultipathInfoLogicalUnit.path[1].adapter == hostHostBusAdapter2.key)
										{
											try
											{
												dataGridvMultiPath.Rows[num].Cells["vMultiPathPath2"].Value = hostHostBusAdapter2.device;
											}
											catch
											{
											}
										}
									}
								}
							}
							if (hostMultipathInfoLogicalUnit.path.Length >= 3)
							{
								try
								{
									dataGridvMultiPath.Rows[num].Cells["vMultiPathPath3State"].Value = hostMultipathInfoLogicalUnit.path[2].state;
								}
								catch
								{
								}
								if (array4 != null)
								{
									array8 = array4;
									foreach (HostHostBusAdapter hostHostBusAdapter3 in array8)
									{
										if (hostMultipathInfoLogicalUnit.path[2].adapter == hostHostBusAdapter3.key)
										{
											try
											{
												dataGridvMultiPath.Rows[num].Cells["vMultiPathPath3"].Value = hostHostBusAdapter3.device;
											}
											catch
											{
											}
										}
									}
								}
							}
							if (hostMultipathInfoLogicalUnit.path.Length >= 4)
							{
								try
								{
									dataGridvMultiPath.Rows[num].Cells["vMultiPathPath4State"].Value = hostMultipathInfoLogicalUnit.path[3].state;
								}
								catch
								{
								}
								if (array4 != null)
								{
									array8 = array4;
									foreach (HostHostBusAdapter hostHostBusAdapter4 in array8)
									{
										if (hostMultipathInfoLogicalUnit.path[3].adapter == hostHostBusAdapter4.key)
										{
											try
											{
												dataGridvMultiPath.Rows[num].Cells["vMultiPathPath4"].Value = hostHostBusAdapter4.device;
											}
											catch
											{
											}
										}
									}
								}
							}
							if (hostMultipathInfoLogicalUnit.path.Length >= 5)
							{
								try
								{
									dataGridvMultiPath.Rows[num].Cells["vMultiPathPath5State"].Value = hostMultipathInfoLogicalUnit.path[4].state;
								}
								catch
								{
								}
								if (array4 != null)
								{
									array8 = array4;
									foreach (HostHostBusAdapter hostHostBusAdapter5 in array8)
									{
										if (hostMultipathInfoLogicalUnit.path[4].adapter == hostHostBusAdapter5.key)
										{
											try
											{
												dataGridvMultiPath.Rows[num].Cells["vMultiPathPath5"].Value = hostHostBusAdapter5.device;
											}
											catch
											{
											}
										}
									}
								}
							}
							if (hostMultipathInfoLogicalUnit.path.Length >= 6)
							{
								try
								{
									dataGridvMultiPath.Rows[num].Cells["vMultiPathPath6State"].Value = hostMultipathInfoLogicalUnit.path[5].state;
								}
								catch
								{
								}
								if (array4 != null)
								{
									array8 = array4;
									foreach (HostHostBusAdapter hostHostBusAdapter6 in array8)
									{
										if (hostMultipathInfoLogicalUnit.path[5].adapter == hostHostBusAdapter6.key)
										{
											try
											{
												dataGridvMultiPath.Rows[num].Cells["vMultiPathPath6"].Value = hostHostBusAdapter6.device;
											}
											catch
											{
											}
										}
									}
								}
							}
							if (hostMultipathInfoLogicalUnit.path.Length >= 7)
							{
								try
								{
									dataGridvMultiPath.Rows[num].Cells["vMultiPathPath7State"].Value = hostMultipathInfoLogicalUnit.path[6].state;
								}
								catch
								{
								}
								if (array4 != null)
								{
									array8 = array4;
									foreach (HostHostBusAdapter hostHostBusAdapter7 in array8)
									{
										if (hostMultipathInfoLogicalUnit.path[6].adapter == hostHostBusAdapter7.key)
										{
											try
											{
												dataGridvMultiPath.Rows[num].Cells["vMultiPathPath7"].Value = hostHostBusAdapter7.device;
											}
											catch
											{
											}
										}
									}
								}
							}
							if (hostMultipathInfoLogicalUnit.path.Length < 8)
							{
								continue;
							}
							try
							{
								dataGridvMultiPath.Rows[num].Cells["vMultiPathPath8State"].Value = hostMultipathInfoLogicalUnit.path[7].state;
							}
							catch
							{
							}
							if (array4 == null)
							{
								continue;
							}
							array8 = array4;
							foreach (HostHostBusAdapter hostHostBusAdapter8 in array8)
							{
								if (hostMultipathInfoLogicalUnit.path[7].adapter == hostHostBusAdapter8.key)
								{
									try
									{
										dataGridvMultiPath.Rows[num].Cells["vMultiPathPath8"].Value = hostHostBusAdapter8.device;
									}
									catch
									{
									}
								}
							}
						}
					}
				}
				try
				{
					dataGridvMultiPath.Rows[num].Cells["vMultiPathObjectID"].Value = managedObjectReference.Value;
				}
				catch
				{
				}
				dataGridvMultiPath.Rows[num].Cells["vMultiPathInstanceUUID"].Value = InstanceUUID;
				dataGridvMultiPath.Rows[num].Cells["vMultiPathVISDKServer"].Value = VISDK.Server;
				num++;
			}
		}
		dataGridvMultiPath.AutoResizeColumns();
		string columnName = "vMultiPathHost";
		if (dataGridvMultiPath.SortedColumn != null)
		{
			columnName = dataGridvMultiPath.SortedColumn.Name;
		}
		dataGridvMultiPath.Sort(dataGridvMultiPath.Columns[columnName], ListSortDirection.Ascending);
		StatusGridCounter.Text = num + " rows";
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Number of rows: " + num);
		}
		Telemetry.AddEvent("RenderTab", "vMultiPath", new Dictionary<string, string>
		{
			{
				"RenderTime",
				DateTime.UtcNow.Subtract(utcNow).ToString()
			},
			{
				"RowCount",
				num.ToString()
			}
		});
		Cursor = Cursors.Default;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void TabPagevLicense()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("ShowTab", "vLicense");
		DateTime utcNow = DateTime.UtcNow;
		if (!TabPagevLicense_Refresh_data)
		{
			StatusGridCounter.Text = dataGridvLicense.RowCount + " rows";
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
			return;
		}
		TabPagevLicense_Refresh_data = false;
		Application.DoEvents();
		Cursor = Cursors.WaitCursor;
		dataGridvLicense.Rows.Clear();
		int num = 0;
		StatusFilter.Visible = false;
		LicenseManagerLicenseInfo[] array = null;
		try
		{
			array = VISDK.GetLicenseInfo();
		}
		catch
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Handeled Exception GetLicenseInfo()");
			}
		}
		if (array != null)
		{
			LicenseManagerLicenseInfo[] array2 = array;
			foreach (LicenseManagerLicenseInfo licenseManagerLicenseInfo in array2)
			{
				if (licenseManagerLicenseInfo.name == "Product Evaluation")
				{
					continue;
				}
				dataGridvLicense.Rows.Add();
				dataGridvLicense.Rows[num].Cells["vLicenseName"].Value = licenseManagerLicenseInfo.name;
				dataGridvLicense.Rows[num].Cells["vLicenseKey"].Value = licenseManagerLicenseInfo.licenseKey;
				if (licenseManagerLicenseInfo.total == 0)
				{
					dataGridvLicense.Rows[num].Cells["vLicenseTotal"].Value = "Unlimited";
				}
				else
				{
					dataGridvLicense.Rows[num].Cells["vLicenseTotal"].Value = licenseManagerLicenseInfo.total;
				}
				dataGridvLicense.Rows[num].Cells["vLicenseUsed"].Value = licenseManagerLicenseInfo.used;
				dataGridvLicense.Rows[num].Cells["vLicenseCostUnit"].Value = licenseManagerLicenseInfo.costUnit;
				string text = null;
				try
				{
					for (int j = 0; j < licenseManagerLicenseInfo.labels.Length; j++)
					{
						if (text != null)
						{
							text += ", ";
						}
						text += licenseManagerLicenseInfo.labels[j].value.ToString();
					}
				}
				catch
				{
				}
				dataGridvLicense.Rows[num].Cells["vLicenseLabels"].Value = text;
				DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0);
				DateTime dateTime2 = new DateTime(1970, 1, 1, 0, 0, 0);
				string text2 = null;
				if (licenseManagerLicenseInfo.properties != null)
				{
					for (int k = 0; k < licenseManagerLicenseInfo.properties.Length; k++)
					{
						if (licenseManagerLicenseInfo.properties[k].key == "feature")
						{
							try
							{
								if (text2 != null)
								{
									text2 += ", ";
								}
								KeyValue keyValue = (KeyValue)licenseManagerLicenseInfo.properties[k].value;
								text2 += keyValue.value.ToString();
							}
							catch
							{
								if (logger.IsDebugEnabled)
								{
									logger.Debug(currentMethod.Name + " Handeled Exception in features");
								}
							}
						}
						if (licenseManagerLicenseInfo.properties[k].key == "expirationHours")
						{
							string s = licenseManagerLicenseInfo.properties[k].value.ToString();
							try
							{
								int.Parse(s);
							}
							catch
							{
								if (logger.IsDebugEnabled)
								{
									logger.Debug(currentMethod.Name + " Handeled Exception parse ExpirationHours");
								}
							}
						}
						if (licenseManagerLicenseInfo.properties[k].key == "expirationMinutes")
						{
							string s2 = licenseManagerLicenseInfo.properties[k].value.ToString();
							try
							{
								int.Parse(s2);
							}
							catch
							{
								if (logger.IsDebugEnabled)
								{
									logger.Debug(currentMethod.Name + " Handeled Exception parse ExpirationMinutes");
								}
							}
						}
						if (!(licenseManagerLicenseInfo.properties[k].key == "expirationDate"))
						{
							continue;
						}
						string s3 = licenseManagerLicenseInfo.properties[k].value.ToString();
						try
						{
							dateTime = DateTime.Parse(s3);
						}
						catch
						{
							if (logger.IsDebugEnabled)
							{
								logger.Debug(currentMethod.Name + " Handeled Exception parse ExpirationDate");
							}
						}
					}
				}
				if (dateTime == dateTime2)
				{
					dataGridvLicense.Rows[num].Cells["vLicenseExpirationDate"].Value = "Never";
				}
				if (dateTime != dateTime2 && dateTime < DateTime.Now)
				{
					dataGridvLicense.Rows[num].Cells["vLicenseExpirationDate"].Value = "Expired";
				}
				if (dateTime != dateTime2 && dateTime >= DateTime.Now)
				{
					dataGridvLicense.Rows[num].Cells["vLicenseExpirationDate"].Value = dateTime.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
				}
				dataGridvLicense.Rows[num].Cells["vLicenseFeatures"].Value = text2;
				dataGridvLicense.Rows[num].Cells["vLicenseInstanceUUID"].Value = InstanceUUID;
				dataGridvLicense.Rows[num].Cells["vLicenseVISDKServer"].Value = VISDK.Server;
				num++;
			}
		}
		dataGridvLicense.AutoResizeColumns();
		string columnName = "vLicenseName";
		if (dataGridvLicense.SortedColumn != null)
		{
			columnName = dataGridvLicense.SortedColumn.Name;
		}
		dataGridvLicense.Sort(dataGridvLicense.Columns[columnName], ListSortDirection.Ascending);
		StatusGridCounter.Text = num + " rows";
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Number of rows: " + num);
		}
		Telemetry.AddEvent("RenderTab", "vLicence", new Dictionary<string, string>
		{
			{
				"RenderTime",
				DateTime.UtcNow.Subtract(utcNow).ToString()
			},
			{
				"RowCount",
				num.ToString()
			}
		});
		Cursor = Cursors.Default;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void TabPagevHealth()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("ShowTab", "vHealth");
		DateTime utcNow = DateTime.UtcNow;
		Application.DoEvents();
		Cursor = Cursors.WaitCursor;
		if (HealthClass.CheckZombies && BrowseThread.ThreadState != System.Threading.ThreadState.Stopped)
		{
			FWait fWait = new FWait();
			fWait.Show();
			Application.DoEvents();
			fWait.Text = "RVTools: Wait for Search Datastores Thread. This may take a while! ";
			fWait.Refresh();
			while (BrowseThread.ThreadState != System.Threading.ThreadState.Stopped)
			{
				if (fWait.Text.Length > 150)
				{
					fWait.Text = "RVTools: Wait for Search Datastores Thread. This may take a while! ";
				}
				fWait.Text += ".";
				fWait.Refresh();
				Application.DoEvents();
				Thread.Sleep(1000);
			}
			fWait.Close();
		}
		PleaseWait.Close();
		Application.DoEvents();
		Cursor = Cursors.WaitCursor;
		dataGridvHealth.Rows.Clear();
		int num = 0;
		StatusFilter.Visible = FilterClass.ShowFilterIcon();
		for (int i = 0; i < VISDK.InventoryCounter; i++)
		{
			if (FilterClass.ShowFilterIcon() && (!FilterClass.DisplayVirtualMachine(i) || !FilterClass.DisplayPowerstate(i) || !FilterClass.DisplayDataCenter(VISDK.GetDatacenterName(VISDK.GetSummaryRuntimeHost(i))) || !FilterClass.DisplayCluster(VISDK.GetClusterName(VISDK.GetSummaryRuntimeHost(i))) || !FilterClass.DisplayHost(VISDK.GetHostName(VISDK.GetSummaryRuntimeHost(i))) || !FilterClass.DisplayVMName(i) || !FilterClass.DisplayNotes(i)))
			{
				continue;
			}
			string text = null;
			VirtualDevice[] deviceInfo = VISDK.GetDeviceInfo(i);
			int num2 = 0;
			while (deviceInfo != null && num2 < deviceInfo.Length)
			{
				text = HealthClass.CDROMMessage(deviceInfo[num2]);
				if (text != null)
				{
					dataGridvHealth.Rows.Add();
					dataGridvHealth.Rows[num].Cells["vHealthName"].Value = VISDK.GetVmInventoryProperty(i, "summary.config.name");
					dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = text;
					dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "CDROM";
					dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
					dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
					num++;
				}
				num2++;
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Execute in-memory performance tip code: " + VISDK.GetVmInventoryProperty(i, "summary.config.name"));
			}
			try
			{
				long inventoryPropertyInt = VISDK.GetInventoryPropertyInt(i, "summary.config.numCpu");
				string vmInventoryProperty = VISDK.GetVmInventoryProperty(i, "config.cpuHotAddEnabled");
				string vmInventoryProperty2 = VISDK.GetVmInventoryProperty(i, "config.memoryHotAddEnabled");
				string vmInventoryProperty3 = VISDK.GetVmInventoryProperty(i, "summary.runtime.powerState");
				string optionValueVM = VISDK.GetOptionValueVM(i, "cpuid.coresPerSocket");
				int num3 = 1;
				try
				{
					num3 = Convert.ToInt16(optionValueVM);
				}
				catch
				{
				}
				if (num3 == 0)
				{
					num3 = 1;
				}
				if (inventoryPropertyInt >= 4 && vmInventoryProperty3 == "poweredOn" && HealthClass.CheckPerformanceTips && (vmInventoryProperty == "True" || vmInventoryProperty2 == "True") && num3 == 1)
				{
					bool flag = false;
					try
					{
						flag = VISDK.GetNumaInfo(i).vnumaOnCpuHotaddExposed;
					}
					catch
					{
					}
					if (!flag)
					{
						dataGridvHealth.Rows.Add();
						dataGridvHealth.Rows[num].Cells["vHealthName"].Value = VISDK.GetVmInventoryProperty(i, "summary.config.name");
						dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = "In-Memory VM performance improvement possible! Please check documentation";
						dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "Performance tip";
						dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
						dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
						num++;
					}
				}
			}
			catch (Exception ex)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception in-memory performance tip: " + ex.Message);
				}
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Execute disk i/o performance tip code: " + VISDK.GetVmInventoryProperty(i, "summary.config.name"));
			}
			try
			{
				int num4 = 0;
				int num5 = 0;
				long num6 = 0L;
				string vmInventoryProperty4 = VISDK.GetVmInventoryProperty(i, "summary.runtime.powerState");
				int num7 = 0;
				while (deviceInfo != null && num7 < deviceInfo.Length)
				{
					if (deviceInfo[num7] is VirtualDisk)
					{
						VirtualDisk virtualDisk = (VirtualDisk)deviceInfo[num7];
						num4++;
						num6 += virtualDisk.capacityInKB;
					}
					if (deviceInfo[num7] is ParaVirtualSCSIController && ((ParaVirtualSCSIController)deviceInfo[num7]).device.Length >= 0)
					{
						num5++;
					}
					num7++;
				}
				if (vmInventoryProperty4 == "poweredOn" && HealthClass.CheckPerformanceTips && num4 > 3 && num6 >= 750000000 && num5 < 2)
				{
					dataGridvHealth.Rows.Add();
					dataGridvHealth.Rows[num].Cells["vHealthName"].Value = VISDK.GetVmInventoryProperty(i, "summary.config.name");
					dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = "Disk I/O performance improvement possible! Please check documentation";
					dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "Performance tip";
					dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
					dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
					num++;
				}
			}
			catch (Exception ex2)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Handled Exception disk i/o performance tip: " + ex2.Message);
				}
			}
			text = HealthClass.InconsistentFolderMessage(i);
			if (text != null)
			{
				dataGridvHealth.Rows.Add();
				dataGridvHealth.Rows[num].Cells["vHealthName"].Value = VISDK.GetVmInventoryProperty(i, "summary.config.name");
				dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = text;
				dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "Foldername";
				dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
				dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
				num++;
			}
			VirtualDevice[] deviceInfo2 = VISDK.GetDeviceInfo(i);
			int num8 = 0;
			while (deviceInfo2 != null && num8 < deviceInfo2.Length)
			{
				text = HealthClass.USBMessage(deviceInfo2[num8]);
				if (text != null)
				{
					dataGridvHealth.Rows.Add();
					dataGridvHealth.Rows[num].Cells["vHealthName"].Value = VISDK.GetVmInventoryProperty(i, "summary.config.name");
					dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = text;
					dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "USB";
					dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
					dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
					num++;
				}
				num8++;
			}
			text = HealthClass.SnapshotMessage(i);
			if (text != null)
			{
				dataGridvHealth.Rows.Add();
				dataGridvHealth.Rows[num].Cells["vHealthName"].Value = VISDK.GetVmInventoryProperty(i, "summary.config.name");
				dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = text;
				dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "Snapshot";
				dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
				dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
				num++;
			}
			text = HealthClass.ConsolidationNeededMessage(i);
			if (text != null)
			{
				dataGridvHealth.Rows.Add();
				dataGridvHealth.Rows[num].Cells["vHealthName"].Value = VISDK.GetVmInventoryProperty(i, "summary.config.name");
				dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = text;
				dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "Snapshot";
				dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
				dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
				num++;
			}
			text = HealthClass.ToolsMessage(i);
			if (text != null)
			{
				dataGridvHealth.Rows.Add();
				dataGridvHealth.Rows[num].Cells["vHealthName"].Value = VISDK.GetVmInventoryProperty(i, "summary.config.name");
				dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = text;
				dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "VM Tools";
				dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
				dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
				num++;
			}
			GuestDiskInfo[] guestDiskInfo = VISDK.GetGuestDiskInfo(i);
			if (guestDiskInfo != null)
			{
				for (int j = 0; j < guestDiskInfo.Length; j++)
				{
					text = HealthClass.GuestStorageMessage(guestDiskInfo[j]);
					if (text != null)
					{
						dataGridvHealth.Rows.Add();
						dataGridvHealth.Rows[num].Cells["vHealthName"].Value = VISDK.GetVmInventoryProperty(i, "summary.config.name");
						dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = text;
						dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "Storage";
						dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
						dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
						num++;
					}
				}
			}
			Event[] vMConfigEvents = VISDK.GetVMConfigEvents(i);
			if (vMConfigEvents != null)
			{
				Event[] array = vMConfigEvents;
				foreach (Event obj3 in array)
				{
					dataGridvHealth.Rows.Add();
					dataGridvHealth.Rows[num].Cells["vHealthName"].Value = VISDK.GetVmInventoryProperty(i, "summary.config.name");
					dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = obj3.fullFormattedMessage;
					dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "VM Config";
					dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
					dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
					num++;
				}
			}
		}
		for (int l = 0; l < VISDK.DatastoreInventoryCounter; l++)
		{
			string text2 = HealthClass.HostStorageMessage(l);
			if (text2 != null)
			{
				dataGridvHealth.Rows.Add();
				dataGridvHealth.Rows[num].Cells["vHealthName"].Value = VISDK.GetDatastoreInventoryProperty(l, "summary.name");
				dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = text2;
				dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "Storage";
				dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
				dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
				num++;
			}
		}
		for (int m = 0; m < VISDK.HostInventoryCounter; m++)
		{
			string text3 = HealthClass.VCPUsPerCore(m);
			if (text3 != null)
			{
				dataGridvHealth.Rows.Add();
				dataGridvHealth.Rows[num].Cells["vHealthName"].Value = VISDK.GetHostInventoryProperty(m, "name");
				dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = text3;
				dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "CPU";
				dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
				dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
				num++;
			}
			if (HealthClass.CheckCertificateExpire)
			{
				try
				{
					int num9 = (int)(VISDK._HostCertificates[VISDK.HostInventory[m].obj.Value].notAfter - DateTime.UtcNow).TotalDays;
					if (num9 <= 0)
					{
						dataGridvHealth.Rows.Add();
						dataGridvHealth.Rows[num].Cells["vHealthName"].Value = VISDK.GetHostInventoryProperty(m, "name");
						dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = "Host certificate is expired.";
						dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "Certificate";
						dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
						dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
						num++;
					}
					else if (num9 <= HealthClass.CheckCertificateExpireValue)
					{
						dataGridvHealth.Rows.Add();
						dataGridvHealth.Rows[num].Cells["vHealthName"].Value = VISDK.GetHostInventoryProperty(m, "name");
						dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = $"Host certificate will expire in {num9} days.";
						dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "Certificate";
						dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
						dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
						num++;
					}
				}
				catch
				{
				}
			}
			try
			{
				HostService[] hostServices = VISDK.GetHostServices(m);
				foreach (HostService hostService in hostServices)
				{
					if (hostService.label == "ESXi Shell" && hostService.running && HealthClass.CheckSecurityIssues)
					{
						dataGridvHealth.Rows.Add();
						dataGridvHealth.Rows[num].Cells["vHealthName"].Value = VISDK.GetHostInventoryProperty(m, "name");
						dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = "ESXi Shell service is running!";
						dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "Security";
						dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
						dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
						num++;
					}
					if (hostService.label == "SSH" && hostService.running && HealthClass.CheckSecurityIssues)
					{
						dataGridvHealth.Rows.Add();
						dataGridvHealth.Rows[num].Cells["vHealthName"].Value = VISDK.GetHostInventoryProperty(m, "name");
						dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = "SSH service is running!";
						dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "Security";
						dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
						dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
						num++;
					}
				}
			}
			catch
			{
			}
		}
		for (int n = 0; n < VISDK.DatastoreInventoryCounter; n++)
		{
			string text4 = HealthClass.VMsOnStorage(n);
			if (text4 != null)
			{
				dataGridvHealth.Rows.Add();
				dataGridvHealth.Rows[num].Cells["vHealthName"].Value = VISDK.GetDatastoreInventoryProperty(n, "summary.name");
				dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = text4;
				dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "Storage";
				dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
				dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
				num++;
			}
			Event[] datastoreConfigEvents = VISDK.GetDatastoreConfigEvents(n);
			if (datastoreConfigEvents != null)
			{
				Event[] array = datastoreConfigEvents;
				foreach (Event obj6 in array)
				{
					dataGridvHealth.Rows.Add();
					dataGridvHealth.Rows[num].Cells["vHealthName"].Value = VISDK.GetDatastoreInventoryProperty(n, "summary.name");
					dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = obj6.fullFormattedMessage;
					dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "Storage config";
					dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
					dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
					num++;
				}
			}
		}
		for (int num10 = 0; num10 < VISDK.ClusterInventoryCounter; num10++)
		{
			Event[] clusterConfigEvents = VISDK.GetClusterConfigEvents(num10);
			if (clusterConfigEvents != null)
			{
				Event[] array = clusterConfigEvents;
				foreach (Event obj7 in array)
				{
					dataGridvHealth.Rows.Add();
					dataGridvHealth.Rows[num].Cells["vHealthName"].Value = VISDK.GetClusterInventoryProperty(num10, "name");
					dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = obj7.fullFormattedMessage;
					dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "Cluster config";
					dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
					dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
					num++;
				}
			}
		}
		for (int num11 = 0; num11 < VISDK.HostInventoryCounter; num11++)
		{
			Event[] hostConfigEvents = VISDK.GetHostConfigEvents(num11);
			if (hostConfigEvents != null)
			{
				Event[] array = hostConfigEvents;
				foreach (Event obj8 in array)
				{
					dataGridvHealth.Rows.Add();
					dataGridvHealth.Rows[num].Cells["vHealthName"].Value = VISDK.GetHostInventoryProperty(num11, "name");
					dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = obj8.fullFormattedMessage;
					dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "Host config";
					dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
					dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
					num++;
				}
			}
			string text5 = null;
			try
			{
				HostDateTimeInfo hostDateTimeInfo = VISDK.GetHostDateTimeInfo(num11);
				if (hostDateTimeInfo != null && hostDateTimeInfo.ntpConfig.server != null)
				{
					for (int num12 = 0; num12 < hostDateTimeInfo.ntpConfig.server.Length; num12++)
					{
						text5 = ((text5 == null) ? hostDateTimeInfo.ntpConfig.server[num12] : (text5 + ", " + hostDateTimeInfo.ntpConfig.server[num12]));
					}
				}
			}
			catch
			{
			}
			if (text5 == null)
			{
				dataGridvHealth.Rows.Add();
				dataGridvHealth.Rows[num].Cells["vHealthName"].Value = VISDK.GetHostInventoryProperty(num11, "name");
				dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = "NTP Server value is null!";
				dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "NTP";
				dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
				dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
				num++;
			}
			try
			{
				HostService[] hostServices = VISDK.GetHostServices(num11);
				foreach (HostService hostService2 in hostServices)
				{
					if (hostService2.key == "ntpd")
					{
						if (!hostService2.running)
						{
							dataGridvHealth.Rows.Add();
							dataGridvHealth.Rows[num].Cells["vHealthName"].Value = VISDK.GetHostInventoryProperty(num11, "name");
							dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = "NTPD service is not running!";
							dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "NTPD";
							dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
							dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
							num++;
						}
						break;
					}
				}
			}
			catch
			{
			}
		}
		for (int num13 = 0; num13 < VISDK.HostInventoryCounter; num13++)
		{
			if (FilterClass.ShowFilterIcon() && (!FilterClass.DisplayDataCenter(VISDK.GetDatacenterName(VISDK.GetHostInventoryProperty(num13, "parent"))) || !FilterClass.DisplayCluster(VISDK.GetClusterName(VISDK.GetHostInventoryProperty(num13, "parent"))) || !FilterClass.DisplayHost(VISDK.GetHostInventoryProperty(num13, "name"))))
			{
				continue;
			}
			ManagedObjectReference[] array2 = null;
			try
			{
				array2 = VISDK.GetHostDatastoreMORs(num13);
			}
			catch
			{
			}
			ScsiLun[] array3 = null;
			try
			{
				array3 = VISDK.GetHostScsiLun(num13);
			}
			catch
			{
			}
			HostMultipathInfoLogicalUnit[] array4 = null;
			try
			{
				array4 = VISDK.GetHostMultipathInfo(num13).lun;
			}
			catch
			{
			}
			try
			{
				VISDK.GetHostBusAdapter(num13);
			}
			catch
			{
			}
			if (array2 == null)
			{
				continue;
			}
			ManagedObjectReference[] array5 = array2;
			foreach (ManagedObjectReference mor in array5)
			{
				VmfsDatastoreInfo vmfsDatastoreInfo = null;
				vmfsDatastoreInfo = VISDK.GetHostDatastoreInfo(mor);
				if (vmfsDatastoreInfo == null)
				{
					continue;
				}
				string hostInventoryProperty = VISDK.GetHostInventoryProperty(num13, "name");
				if (array3 == null)
				{
					continue;
				}
				ScsiLun[] array6 = array3;
				foreach (ScsiLun scsiLun in array6)
				{
					if (!(scsiLun.canonicalName == vmfsDatastoreInfo.vmfs.extent[0].diskName))
					{
						continue;
					}
					if (scsiLun.operationalState[0] == "degraded")
					{
						dataGridvHealth.Rows.Add();
						dataGridvHealth.Rows[num].Cells["vHealthName"].Value = hostInventoryProperty + ", " + vmfsDatastoreInfo.name;
						dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = "One or more paths to the LUN are down, but I/O is still possible. Further path failures may result in lost connectivity.";
						dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "Storage";
						dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
						dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
						num++;
					}
					if (scsiLun.operationalState[0] == "error")
					{
						dataGridvHealth.Rows.Add();
						dataGridvHealth.Rows[num].Cells["vHealthName"].Value = hostInventoryProperty + ", " + vmfsDatastoreInfo.name;
						dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = "The LUN is dead and/or not reachable.";
						dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "Storage";
						dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
						dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
						num++;
					}
					if (scsiLun.operationalState[0] == "lostCommunication")
					{
						dataGridvHealth.Rows.Add();
						dataGridvHealth.Rows[num].Cells["vHealthName"].Value = hostInventoryProperty + ", " + vmfsDatastoreInfo.name;
						dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = "No more paths are available to the LUN.";
						dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "Storage";
						dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
						dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
						num++;
					}
					if (scsiLun.operationalState[0] == "off")
					{
						dataGridvHealth.Rows.Add();
						dataGridvHealth.Rows[num].Cells["vHealthName"].Value = hostInventoryProperty + ", " + vmfsDatastoreInfo.name;
						dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = "The LUN is off.";
						dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "Storage";
						dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
						dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
						num++;
					}
					if (scsiLun.operationalState[0] == "quiesced")
					{
						dataGridvHealth.Rows.Add();
						dataGridvHealth.Rows[num].Cells["vHealthName"].Value = hostInventoryProperty + ", " + vmfsDatastoreInfo.name;
						dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = "The LUN is inactive.";
						dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "Storage";
						dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
						dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
						num++;
					}
					if (scsiLun.operationalState[0] == "timeout")
					{
						dataGridvHealth.Rows.Add();
						dataGridvHealth.Rows[num].Cells["vHealthName"].Value = hostInventoryProperty + ", " + vmfsDatastoreInfo.name;
						dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = "LUN: All Paths have been down for the timeout condition determined by a user-configurable host advanced option.";
						dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "Storage";
						dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
						dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
						num++;
					}
					if (scsiLun.operationalState[0] == "unknownState")
					{
						dataGridvHealth.Rows.Add();
						dataGridvHealth.Rows[num].Cells["vHealthName"].Value = hostInventoryProperty + ", " + vmfsDatastoreInfo.name;
						dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = "The LUN state is unknown.";
						dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "Storage";
						dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
						dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
						num++;
					}
					if (array4 == null)
					{
						continue;
					}
					HostMultipathInfoLogicalUnit[] array7 = array4;
					foreach (HostMultipathInfoLogicalUnit hostMultipathInfoLogicalUnit in array7)
					{
						if (!(scsiLun.key == hostMultipathInfoLogicalUnit.lun))
						{
							continue;
						}
						int num16 = 0;
						HostMultipathInfoPath[] path = hostMultipathInfoLogicalUnit.path;
						foreach (HostMultipathInfoPath obj15 in path)
						{
							if (obj15.state == "dead")
							{
								dataGridvHealth.Rows.Add();
								dataGridvHealth.Rows[num].Cells["vHealthName"].Value = hostInventoryProperty + ", " + vmfsDatastoreInfo.name;
								dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = "Path " + (num16 + 1) + " cannot be used for I/O.";
								dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "Storage";
								dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
								dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
								num++;
							}
							if (obj15.state == "unknown")
							{
								dataGridvHealth.Rows.Add();
								dataGridvHealth.Rows[num].Cells["vHealthName"].Value = hostInventoryProperty + ", " + vmfsDatastoreInfo.name;
								dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = "Path " + (num16 + 1) + " is in unknown error state.";
								dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "Storage";
								dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
								dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
								num++;
							}
							num16++;
						}
					}
				}
			}
		}
		ArrayList browserResultErrors = VISDK.GetBrowserResultErrors();
		if (browserResultErrors != null && HealthClass.CheckZombies)
		{
			for (int num18 = 0; num18 < browserResultErrors.Count; num18++)
			{
				string text6 = browserResultErrors[num18].ToString();
				dataGridvHealth.Rows.Add();
				dataGridvHealth.Rows[num].Cells["vHealthName"].Value = "SearchDatastore";
				dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = "Handled exception for datastore: " + text6;
				dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "Storage";
				dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
				dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
				num++;
			}
		}
		ArrayList browserResult = VISDK.GetBrowserResult();
		if (browserResult != null && HealthClass.CheckZombies)
		{
			for (int num19 = 0; num19 < browserResult.Count; num19++)
			{
				string text7 = browserResult[num19].ToString();
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " VMPath BrowserResult, " + num19 + text7);
				}
				if (text7.EndsWith(".vmx") || text7.EndsWith(".vmtx"))
				{
					int num20 = 0;
					for (num20 = 0; num20 < VISDK.InventoryCounter && !(VISDK.GetVmInventoryProperty(num20, "summary.config.vmPathName") == text7); num20++)
					{
					}
					if (num20 == VISDK.InventoryCounter)
					{
						dataGridvHealth.Rows.Add();
						dataGridvHealth.Rows[num].Cells["vHealthName"].Value = VISDK.GetFriendlyPath(text7);
						if (text7.EndsWith(".vmx"))
						{
							dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = "Possibly a Zombie VM! Please check.";
						}
						else
						{
							dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = "Possibly a Zombie Template! Please check.";
						}
						dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "Zombie";
						dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
						dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
						num++;
					}
				}
				if (!text7.EndsWith(".vmdk"))
				{
					continue;
				}
				int num21 = 0;
				for (num21 = 0; num21 < VISDK.InventoryCounter; num21++)
				{
					VirtualMachineFileLayoutExFileInfo[] virtualMachineFileLayoutExFileInfo = VISDK.GetVirtualMachineFileLayoutExFileInfo(num21);
					string text8 = null;
					if (virtualMachineFileLayoutExFileInfo != null)
					{
						VirtualMachineFileLayoutExFileInfo[] array8 = virtualMachineFileLayoutExFileInfo;
						for (int k = 0; k < array8.Length; k++)
						{
							text8 = array8[k].name;
							if (text8 == text7)
							{
								break;
							}
						}
					}
					if (text8 == text7)
					{
						break;
					}
				}
				if (num21 == VISDK.InventoryCounter)
				{
					dataGridvHealth.Rows.Add();
					dataGridvHealth.Rows[num].Cells["vHealthName"].Value = VISDK.GetFriendlyPath(text7);
					dataGridvHealth.Rows[num].Cells["vHealthMessage"].Value = "Possibly a Zombie vmdk file! Please check.";
					dataGridvHealth.Rows[num].Cells["vHealthMessageType"].Value = "Zombie";
					dataGridvHealth.Rows[num].Cells["vHealthInstanceUUID"].Value = InstanceUUID;
					dataGridvHealth.Rows[num].Cells["vHealthVISDKServer"].Value = VISDK.Server;
					num++;
				}
			}
		}
		string columnName = "vHealthName";
		if (dataGridvHealth.SortedColumn != null)
		{
			columnName = dataGridvHealth.SortedColumn.Name;
		}
		dataGridvHealth.Sort(dataGridvHealth.Columns[columnName], ListSortDirection.Ascending);
		StatusGridCounter.Text = num + " rows";
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Number of rows: " + num);
		}
		Telemetry.AddEvent("RenderTab", "vHealth", new Dictionary<string, string>
		{
			{
				"RenderTime",
				DateTime.UtcNow.Subtract(utcNow).ToString()
			},
			{
				"RowCount",
				num.ToString()
			}
		});
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
		Cursor = Cursors.Default;
	}

	private void TabPagevFileInfo()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Telemetry.AddEvent("ShowTab", "vFileInfo");
		DateTime utcNow = DateTime.UtcNow;
		if (!TabPagevFileInfo_Refresh_data)
		{
			StatusGridCounter.Text = dataGridvFileInfo.RowCount + " rows";
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Exit");
			}
			return;
		}
		TabPagevFileInfo_Refresh_data = false;
		if (dataGridvFileInfo.Rows.Count > 1)
		{
			Cursor = Cursors.Default;
			return;
		}
		Application.DoEvents();
		Cursor = Cursors.WaitCursor;
		if (PreferencesClass.GetFileInfo && FileInfoThread.ThreadState != System.Threading.ThreadState.Stopped)
		{
			FWait fWait = new FWait();
			fWait.Show();
			Application.DoEvents();
			fWait.Text = "RVTools: Wait for GetFileInfo thread his this can take a very long time! ";
			fWait.Refresh();
			while (FileInfoThread.ThreadState != System.Threading.ThreadState.Stopped)
			{
				if (fWait.Text.Length > 150)
				{
					fWait.Text = "RVTools: Wait for GetFileInfo thread his this can take a very long time! ";
				}
				fWait.Text += ".";
				fWait.Refresh();
				Application.DoEvents();
				Thread.Sleep(1000);
			}
			fWait.Close();
		}
		PleaseWait.Close();
		Application.DoEvents();
		Cursor = Cursors.WaitCursor;
		dataGridvFileInfo.Rows.Clear();
		int num = 0;
		StatusFilter.Visible = FilterClass.ShowFilterIcon();
		if (!PreferencesClass.GetFileInfo && dataGridvFileInfo.Rows.Count == 0)
		{
			dataGridvFileInfo.Rows.Add();
			dataGridvFileInfo.Rows[num].Cells["vFileInfoFriendlyPath"].Value = "This tab page is empty when GetFileInfo option is not set in preferences or when using the CLI the switch -GetFileInfo is not passed. ";
		}
		ArrayList arrayList = null;
		try
		{
			arrayList = VISDK.GetBrowserAllFiles();
		}
		catch
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " Handeled Exception GetBrowserAllFiles");
			}
		}
		if (arrayList != null)
		{
			foreach (VISDK._VMFileObject item in arrayList)
			{
				try
				{
					dataGridvFileInfo.Rows.Add();
					dataGridvFileInfo.Rows[num].Cells["vFileInfoFriendlyPath"].Value = item.friendlyPathName;
					dataGridvFileInfo.Rows[num].Cells["vFileInfoFileName"].Value = item.fileName;
					dataGridvFileInfo.Rows[num].Cells["vFileInfoFileType"].Value = item.fileType;
					dataGridvFileInfo.Rows[num].Cells["vFileInfoFileSize"].Value = item.fileSize;
					dataGridvFileInfo.Rows[num].Cells["vFileInfoPath"].Value = item.pathName;
					dataGridvFileInfo.Rows[num].Cells["vFileInfoSDKServer"].Value = VISDK.Server;
					dataGridvFileInfo.Rows[num].Cells["vFileInfoSDKUUID"].Value = InstanceUUID;
					dataGridvFileInfo.Rows[num].Cells["vFileInfoSortColumn"].Value = item.friendlyPathName + item.fileName;
					num++;
				}
				catch
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(currentMethod.Name + " Handeled Exception vFileInfo rows add");
					}
				}
			}
		}
		dataGridvFileInfo.AutoResizeColumns();
		string columnName = "vFileInfoSortColumn";
		if (dataGridvFileInfo.SortedColumn != null)
		{
			columnName = dataGridvFileInfo.SortedColumn.Name;
		}
		dataGridvFileInfo.Sort(dataGridvFileInfo.Columns[columnName], ListSortDirection.Ascending);
		StatusGridCounter.Text = num + " rows";
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Number of rows: " + num);
		}
		Telemetry.AddEvent("RenderTab", "vFileinfo", new Dictionary<string, string>
		{
			{
				"RenderTime",
				DateTime.UtcNow.Subtract(utcNow).ToString()
			},
			{
				"RowCount",
				num.ToString()
			}
		});
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
		Cursor = Cursors.Default;
	}

	private void FMain_FormClosing(object sender, FormClosingEventArgs e)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		VISDK.Disconnect();
		Telemetry.Close(e.CloseReason.ToString());
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private static void QuerySDKInventory()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		DateTime utcNow = DateTime.UtcNow;
		TabPagevInfo_Refresh_data = true;
		TabPagevCPU_Refresh_data = true;
		TabPagevMemory_Refresh_data = true;
		TabPagevDisk_Refresh_data = true;
		TabPagevPartition_Refresh_data = true;
		TabPagevNetwork_Refresh_data = true;
		TabPagevCD_Refresh_data = true;
		TabPagevUSB_Refresh_data = true;
		TabPagevSnapshot_Refresh_data = true;
		TabPagevTools_Refresh_data = true;
		TabPagevRP_Refresh_data = true;
		TabPagevSource_Refresh_data = true;
		TabPagevCluster_Refresh_data = true;
		TabPagevHost_Refresh_data = true;
		TabPagevHBA_Refresh_data = true;
		TabPagevNIC_Refresh_data = true;
		TabPagevSwitch_Refresh_data = true;
		TabPagevPort_Refresh_data = true;
		TabPagedvSwitch_Refresh_data = true;
		TabPagedvPort_Refresh_data = true;
		TabPagevSC_VMK_Refresh_data = true;
		TabPagevDatastore_Refresh_data = true;
		TabPagevLicense_Refresh_data = true;
		TabPagevFileInfo_Refresh_data = true;
		TabPagevMultiPath_Refresh_data = true;
		VISDK.PropertySpecPathSet_clear();
		VISDK.PropertySpecPathSet_add("summary.config.name");
		VISDK.PropertySpecPathSet_add("summary.vm");
		VISDK.PropertySpecPathSet_add("summary.runtime.powerState");
		VISDK.PropertySpecPathSet_add("summary.runtime.bootTime");
		VISDK.PropertySpecPathSet_add("summary.runtime.connectionState");
		VISDK.PropertySpecPathSet_add("summary.runtime.suspendTime");
		VISDK.PropertySpecPathSet_add("summary.config.template");
		VISDK.PropertySpecPathSet_add("summary.config.numCpu");
		VISDK.PropertySpecPathSet_add("summary.config.memorySizeMB");
		VISDK.PropertySpecPathSet_add("summary.config.numEthernetCards");
		VISDK.PropertySpecPathSet_add("summary.config.numVirtualDisks");
		VISDK.PropertySpecPathSet_add("summary.config.vmPathName");
		VISDK.PropertySpecPathSet_add("summary.config.annotation");
		VISDK.PropertySpecPathSet_add("summary.runtime.host");
		VISDK.PropertySpecPathSet_add("summary.config.guestFullName");
		VISDK.PropertySpecPathSet_add("summary.runtime.maxCpuUsage");
		VISDK.PropertySpecPathSet_add("summary.quickStats.overallCpuUsage");
		VISDK.PropertySpecPathSet_add("resourceConfig.cpuAllocation.shares");
		VISDK.PropertySpecPathSet_add("resourceConfig.cpuAllocation.reservation");
		VISDK.PropertySpecPathSet_add("resourceConfig.cpuAllocation.limit");
		VISDK.PropertySpecPathSet_add("summary.quickStats.guestMemoryUsage");
		VISDK.PropertySpecPathSet_add("summary.quickStats.hostMemoryUsage");
		VISDK.PropertySpecPathSet_add("summary.runtime.maxMemoryUsage");
		VISDK.PropertySpecPathSet_add("summary.runtime.memoryOverhead");
		VISDK.PropertySpecPathSet_add("summary.runtime.suspendTime");
		VISDK.PropertySpecPathSet_add("resourceConfig.memoryAllocation.shares");
		VISDK.PropertySpecPathSet_add("resourceConfig.memoryAllocation.reservation");
		VISDK.PropertySpecPathSet_add("resourceConfig.memoryAllocation.limit");
		VISDK.PropertySpecPathSet_add("guest.disk");
		VISDK.PropertySpecPathSet_add("snapshot.rootSnapshotList");
		VISDK.PropertySpecPathSet_add("guest.net");
		VISDK.PropertySpecPathSet_add("guest.ipAddress");
		VISDK.PropertySpecPathSet_add("config.files");
		VISDK.PropertySpecPathSet_add("config.hardware");
		VISDK.PropertySpecPathSet_add("config.version");
		VISDK.PropertySpecPathSet_add("config.uuid");
		VISDK.PropertySpecPathSet_add("config.instanceUuid");
		VISDK.PropertySpecPathSet_add("config.extraConfig");
		VISDK.PropertySpecPathSet_add("config.changeVersion");
		VISDK.PropertySpecPathSet_add("configStatus");
		VISDK.PropertySpecPathSet_add("guest.hostName");
		VISDK.PropertySpecPathSet_add("guest.toolsStatus");
		VISDK.PropertySpecPathSet_add("guest.toolsVersion");
		VISDK.PropertySpecPathSet_add("guest.guestFullName");
		VISDK.PropertySpecPathSet_add("guest.guestState");
		VISDK.PropertySpecPathSet_add("disabledMethod");
		VISDK.PropertySpecPathSet_add("customValue");
		VISDK.PropertySpecPathSet_add("parent");
		VISDK.PropertySpecPathSet_add("resourcePool");
		VISDK.PropertySpecPathSet_add("runtime.suspendInterval");
		if (VISDK.APIVersion >= 250)
		{
			VISDK.PropertySpecPathSet_add("config.tools.syncTimeWithHost");
			VISDK.PropertySpecPathSet_add("config.tools.toolsUpgradePolicy");
			VISDK.PropertySpecPathSet_add("config.bootOptions");
		}
		if (VISDK.APIVersion >= 400)
		{
			VISDK.PropertySpecPathSet_add("layoutEx.file");
			VISDK.PropertySpecPathSet_add("layoutEx.snapshot");
			VISDK.PropertySpecPathSet_add("summary.quickStats.guestHeartbeatStatus");
			VISDK.PropertySpecPathSet_add("summary.quickStats.balloonedMemory");
			VISDK.PropertySpecPathSet_add("summary.quickStats.consumedOverheadMemory");
			VISDK.PropertySpecPathSet_add("summary.quickStats.privateMemory");
			VISDK.PropertySpecPathSet_add("summary.quickStats.sharedMemory");
			VISDK.PropertySpecPathSet_add("summary.quickStats.swappedMemory");
			VISDK.PropertySpecPathSet_add("summary.quickStats.staticMemoryEntitlement");
			VISDK.PropertySpecPathSet_add("summary.quickStats.distributedMemoryEntitlement");
			VISDK.PropertySpecPathSet_add("summary.quickStats.staticCpuEntitlement");
			VISDK.PropertySpecPathSet_add("summary.quickStats.distributedCpuEntitlement");
			VISDK.PropertySpecPathSet_add("summary.quickStats.ftLatencyStatus");
			VISDK.PropertySpecPathSet_add("summary.quickStats.ftLogBandwidth");
			VISDK.PropertySpecPathSet_add("summary.quickStats.ftSecondaryLatency");
			VISDK.PropertySpecPathSet_add("summary.storage.committed");
			VISDK.PropertySpecPathSet_add("summary.storage.uncommitted");
			VISDK.PropertySpecPathSet_add("summary.storage.unshared");
			VISDK.PropertySpecPathSet_add("summary.config.installBootRequired");
			VISDK.PropertySpecPathSet_add("summary.runtime.faultToleranceState");
			VISDK.PropertySpecPathSet_add("guest.toolsVersionStatus");
			VISDK.PropertySpecPathSet_add("storage");
			VISDK.PropertySpecPathSet_add("config.cpuHotAddEnabled");
			VISDK.PropertySpecPathSet_add("config.cpuHotRemoveEnabled");
			VISDK.PropertySpecPathSet_add("config.memoryHotAddEnabled");
			VISDK.PropertySpecPathSet_add("config.ftInfo");
		}
		if (VISDK.APIVersion >= 410)
		{
			VISDK.PropertySpecPathSet_add("parentVApp");
			VISDK.PropertySpecPathSet_add("guest.appHeartbeatStatus");
			VISDK.PropertySpecPathSet_add("runtime.minRequiredEVCModeKey");
		}
		if (VISDK.APIVersion >= 500)
		{
			VISDK.PropertySpecPathSet_add("runtime.consolidationNeeded");
			VISDK.PropertySpecPathSet_add("summary.runtime.dasVmProtection");
			VISDK.PropertySpecPathSet_add("config.firmware");
			VISDK.PropertySpecPathSet_add("guest.guestOperationsReady");
			VISDK.PropertySpecPathSet_add("guest.interactiveGuestOperationsReady");
			VISDK.PropertySpecPathSet_add("config.memoryReservationLockedToMax");
			VISDK.PropertySpecPathSet_add("config.managedBy");
		}
		if (VISDK.APIVersion >= 510)
		{
			VISDK.PropertySpecPathSet_add("config.scheduledHardwareUpgradeInfo");
			VISDK.PropertySpecPathSet_add("config.latencySensitivity");
		}
		if (VISDK.APIVersion >= 550)
		{
			VISDK.PropertySpecPathSet_add("guest.appState");
		}
		if (VISDK.APIVersion >= 600)
		{
			VISDK.PropertySpecPathSet_add("guest.guestKernelCrashed");
			VISDK.PropertySpecPathSet_add("guest.guestStateChangeSupported");
		}
		if (VISDK.APIVersion >= 670)
		{
			VISDK.PropertySpecPathSet_add("config.createDate");
		}
		if (VISDK.APIVersion >= 700)
		{
			VISDK.PropertySpecPathSet_add("summary.quickStats.overallCpuReadiness");
		}
		if (VISDK.APIVersion >= 702)
		{
			VISDK.PropertySpecPathSet_add("guest.customizationInfo.customizationStatus");
			VISDK.PropertySpecPathSet_add("guest.customizationInfo.errorMsg");
			VISDK.PropertySpecPathSet_add("runtime.suspendedToMemory");
			VISDK.PropertySpecPathSet_add("runtime.vmFailoverInProgress");
		}
		if (VISDK.APIVersion >= 703)
		{
			VISDK.PropertySpecPathSet_add("summary.quickStats.activeMemory");
		}
		if (VISDK.APIVersion >= 800)
		{
			VISDK.PropertySpecPathSet_add("config.numaInfo");
			VISDK.PropertySpecPathSet_add("config.rebootPowerOff");
		}
		if (VISDK.APIVersion >= 801)
		{
			VISDK.PropertySpecPathSet_add("config.fixedPassthruHotPlugEnabled");
			VISDK.PropertySpecPathSet_add("runtime.opNotificationTimeout");
		}
		if (VISDK.APIVersion >= 802)
		{
			VISDK.PropertySpecPathSet_add("guest.guestDetailedData");
		}
		Thread thread = new Thread(VISDK.KeepALive);
		thread.IsBackground = true;
		thread.Start();
		Thread thread2 = new Thread(VISDK.QueryCISTags);
		thread2.IsBackground = true;
		thread2.Start();
		Thread thread3 = new Thread(VISDK.QueryResourcePoolInventory);
		thread3.IsBackground = true;
		thread3.Start();
		Thread thread4 = new Thread(VISDK.QueryClusterInventory);
		thread4.IsBackground = true;
		thread4.Start();
		Thread thread5 = new Thread(VISDK.QueryDatastoreInventory);
		thread5.IsBackground = true;
		thread5.Start();
		Thread thread6 = new Thread(VISDK.QueryInventory);
		thread6.IsBackground = true;
		thread6.Start();
		Thread thread7 = new Thread(VISDK.QueryLicenseInformation, 4096000);
		thread7.IsBackground = true;
		thread7.Start();
		Thread thread8 = new Thread(VISDK.QueryCustomFields);
		thread8.IsBackground = true;
		thread8.Start();
		Thread thread9 = new Thread(VISDK.QueryHostInventory);
		thread9.IsBackground = true;
		thread9.Start();
		Thread thread10 = new Thread(VISDK.QueryHostHierarchy);
		thread10.IsBackground = true;
		thread10.Start();
		PleaseWait.Text = "RVTools: Wait for VM Inventory thread.";
		PleaseWait.Refresh();
		while (thread6.ThreadState != System.Threading.ThreadState.Stopped)
		{
			PleaseWait.Text += ".";
			PleaseWait.Refresh();
			Thread.Sleep(1000);
		}
		Thread thread11 = new Thread(VISDK.QueryFolderHierarchy);
		thread11.IsBackground = true;
		thread11.Start();
		PleaseWait.Text = "RVTools: Wait for Host Inventory thread.";
		PleaseWait.Refresh();
		while (thread9.ThreadState != System.Threading.ThreadState.Stopped)
		{
			PleaseWait.Text += ".";
			PleaseWait.Refresh();
			Thread.Sleep(1000);
		}
		Thread thread12 = new Thread(VISDK.QueryDistributedSwitchInventory);
		thread12.IsBackground = true;
		thread12.Start();
		PleaseWait.Text = "RVTools: Wait for Host Hierarchy thread.";
		PleaseWait.Refresh();
		while (thread10.ThreadState != System.Threading.ThreadState.Stopped)
		{
			PleaseWait.Text += ".";
			PleaseWait.Refresh();
			Thread.Sleep(1000);
		}
		PleaseWait.Text = "RVTools: Wait for Distributed Switch Inventory thread.";
		PleaseWait.Refresh();
		while (thread12.ThreadState != System.Threading.ThreadState.Stopped)
		{
			PleaseWait.Text += ".";
			PleaseWait.Refresh();
			Thread.Sleep(1000);
		}
		if (PreferencesClass.vInfoShowFolder || CommandToExecute != null)
		{
			PleaseWait.Text = "RVTools: Wait for Folder Hierachy thread.";
			PleaseWait.Refresh();
			while (thread11.ThreadState != System.Threading.ThreadState.Stopped)
			{
				PleaseWait.Text += ".";
				PleaseWait.Refresh();
				Thread.Sleep(1000);
			}
		}
		PleaseWait.Text = "RVTools: Wait for Custom Fields thread.";
		PleaseWait.Refresh();
		while (thread8.ThreadState != System.Threading.ThreadState.Stopped)
		{
			PleaseWait.Text += ".";
			PleaseWait.Refresh();
			Thread.Sleep(1000);
		}
		PleaseWait.Text = "RVTools: Wait for Query License information thread.";
		PleaseWait.Refresh();
		while (thread7.ThreadState != System.Threading.ThreadState.Stopped)
		{
			PleaseWait.Text += ".";
			PleaseWait.Refresh();
			Thread.Sleep(1000);
		}
		PleaseWait.Text = "RVTools: Wait for Datastore thread.";
		PleaseWait.Refresh();
		while (thread5.ThreadState != System.Threading.ThreadState.Stopped)
		{
			PleaseWait.Text += ".";
			PleaseWait.Refresh();
			Thread.Sleep(1000);
		}
		Thread SearchDatastoreFriendlyNames = null;
		VISDK.ClearBrowserResultFriendlyNames();
		SearchDatastoreFriendlyNames = new Thread(VISDK.SearchDatastoreFriendlyNamesThread);
		SearchDatastoreFriendlyNames.IsBackground = true;
		SearchDatastoreFriendlyNames.Start();
		PleaseWait.Text = "RVTools: Wait for Resource pool thread.";
		PleaseWait.Refresh();
		while (thread3.ThreadState != System.Threading.ThreadState.Stopped)
		{
			PleaseWait.Text += ".";
			PleaseWait.Refresh();
			Thread.Sleep(1000);
		}
		PleaseWait.Text = "RVTools: Wait for CIS REST API thread.";
		PleaseWait.Refresh();
		while (thread2.ThreadState != System.Threading.ThreadState.Stopped)
		{
			PleaseWait.Text += ".";
			PleaseWait.Refresh();
			Thread.Sleep(1000);
		}
		if (PreferencesClass.GetFriendlyNames)
		{
			PleaseWait.Text = "RVTools: Wait for \"Get Friendly vSAN Names\" thread.";
			PleaseWait.Refresh();
			while (SearchDatastoreFriendlyNames.ThreadState != System.Threading.ThreadState.Stopped)
			{
				PleaseWait.Text += ".";
				PleaseWait.Refresh();
				Thread.Sleep(1000);
			}
		}
		VISDK.ClearBrowserResult();
		BrowseThread = new Thread(() =>
		{
			VISDK.SearchDatastoreThread(SearchDatastoreFriendlyNames);
		});
		BrowseThread.IsBackground = true;
		BrowseThread.Start();
		if (PreferencesClass.GetFileInfo && VISDK.GetCounterAllFiles() == 0)
		{
			VISDK.ClearBrowserFileInfoResult();
			FileInfoThread = new Thread(() =>
			{
				VISDK.SearchDatastoreThreadForFileInfo(BrowseThread);
			});
			FileInfoThread.IsBackground = true;
			FileInfoThread.Start();
		}
		Telemetry.AddEvent("CollectionCompleted", "", new Dictionary<string, string> { 
		{
			"Duration",
			DateTime.UtcNow.Subtract(utcNow).ToString()
		} });
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void FMain_Resize(object sender, EventArgs e)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		StatusUsername.Width = statusStrip1.Width * 15 / 100;
		StatusServer.Width = statusStrip1.Width * 15 / 100;
		StatusVCFullname.Width = statusStrip1.Width * 30 / 100;
		StatusGridCounter.Width = statusStrip1.Width * 10 / 100;
		StatusLastRefresh.Width = statusStrip1.Width * 20 / 100;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		this.components = new System.ComponentModel.Container();
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RVTools.FMain));
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle12 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle13 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle14 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle15 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle16 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle17 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle18 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle19 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle20 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle21 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle22 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle23 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle24 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle25 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle26 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle27 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle28 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle29 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle30 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle31 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle32 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle33 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle34 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle35 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle36 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle37 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle38 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle39 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle40 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle41 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle42 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle43 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle44 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle45 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle46 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle47 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle48 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle49 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle50 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle51 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle52 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle53 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle54 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle55 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle56 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle57 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle58 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle59 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle60 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle61 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle62 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle63 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle64 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle65 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle66 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle67 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle68 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle69 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle70 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle71 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle72 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle73 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle74 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle75 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle76 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle77 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle78 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle79 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle80 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle81 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle82 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle83 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle84 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle85 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle86 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle87 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle88 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle89 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle90 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle91 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle92 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle93 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle94 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle95 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle96 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle97 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle98 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle99 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle100 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle101 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle102 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle103 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle104 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle105 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle106 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle107 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle108 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle109 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle110 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle111 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle112 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle113 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle114 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle115 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle116 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle117 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle118 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle119 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle120 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle121 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle122 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle123 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle124 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle125 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle126 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle127 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle128 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle129 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle130 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle131 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle132 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle133 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle134 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle135 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle136 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle137 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle138 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle139 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle140 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle141 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle142 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle143 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle144 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle145 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle146 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle147 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle148 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle149 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle150 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle151 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle152 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle153 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle154 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle155 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle156 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle157 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle158 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle159 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle160 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle161 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle162 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle163 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle164 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle165 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle166 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle167 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle168 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle169 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle170 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle171 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle172 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle173 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle174 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle175 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle176 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle177 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle178 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle179 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle180 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle181 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle182 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle183 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle184 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle185 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle186 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle187 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle188 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle189 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle190 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle191 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle192 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle193 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle194 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle195 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle196 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle197 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle198 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle199 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle200 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle201 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle202 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle203 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle204 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle205 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle206 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle207 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle208 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle209 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle210 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle211 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle212 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle213 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle214 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle215 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle216 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle217 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle218 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle219 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle220 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle221 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle222 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle223 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle224 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle225 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle226 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle227 = new System.Windows.Forms.DataGridViewCellStyle();
		this.statusStrip1 = new System.Windows.Forms.StatusStrip();
		this.StatusUsername = new System.Windows.Forms.ToolStripStatusLabel();
		this.StatusServer = new System.Windows.Forms.ToolStripStatusLabel();
		this.StatusVCFullname = new System.Windows.Forms.ToolStripStatusLabel();
		this.StatusGridCounter = new System.Windows.Forms.ToolStripStatusLabel();
		this.StatusLastRefresh = new System.Windows.Forms.ToolStripStatusLabel();
		this.StatusFilter = new System.Windows.Forms.ToolStripStatusLabel();
		this.menuStrip1 = new System.Windows.Forms.MenuStrip();
		this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.ExportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
		this.ExportAll2xlsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.ExportAllMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
		this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripMenuEdit = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripPreferences = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripMenuView = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripMenuFilter = new System.Windows.Forms.ToolStripMenuItem();
		this.refreshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.VMtoolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
		this.CPUToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.MemoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
		this.PartitiontoolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.NetworktoolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
		this.SnapshotToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.VMToolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripMenuESX = new System.Windows.Forms.ToolStripMenuItem();
		this.vSourceStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.vRPToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.vClusterStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.vHostToolstripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.vHBAToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.NICToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.vSwitchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.vPortToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.dvSwitchtoolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.dvPorttoolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.vConsoleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.DatastoreToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.multiPathToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.licenseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.healhtToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.HealthPropertiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
		this.HealthCheckToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
		this.CheckForUpdatestoolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.twittertoolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripSeparator();
		this.documentationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.KeepAliveTimer = new System.Windows.Forms.Timer(this.components);
		this.DisplayDataGridTimer = new System.Windows.Forms.Timer(this.components);
		this.tabvLicense = new System.Windows.Forms.TabPage();
		this.panel27 = new System.Windows.Forms.Panel();
		this.dataGridvLicense = new System.Windows.Forms.DataGridView();
		this.vLicenseName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vLicenseKey = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vLicenseLabels = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vLicenseCostUnit = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vLicenseTotal = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vLicenseUsed = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vLicenseExpirationDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vLicenseFeatures = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vLicenseVISDKServer = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vLicenseInstanceUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.tabvHealth = new System.Windows.Forms.TabPage();
		this.panel15 = new System.Windows.Forms.Panel();
		this.dataGridvHealth = new System.Windows.Forms.DataGridView();
		this.vHealthName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHealthMessage = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHealthMessageType = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHealthVISDKServer = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHealthInstanceUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.tabvMultiPath = new System.Windows.Forms.TabPage();
		this.panel26 = new System.Windows.Forms.Panel();
		this.dataGridvMultiPath = new System.Windows.Forms.DataGridView();
		this.vMultiPathHost = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathCluster = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathDataCenter = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathDatastoreName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathDiskName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathDisplayName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathPolicy = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathOperationalState = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathPath1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathPath1State = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathPath2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathPath2State = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathPath3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathPath3State = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathPath4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathPath4State = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathPath5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathPath5State = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathPath6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathPath6State = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathPath7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathPath7State = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathPath8 = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathPath8State = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathVStorageSupport = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathQueueDepth = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathVendor = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathModel = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathRevision = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathSCSILevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathSerialNumber = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathObjectID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathVISDKServer = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMultiPathInstanceUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.tabvDatastore = new System.Windows.Forms.TabPage();
		this.panel3 = new System.Windows.Forms.Panel();
		this.dataGridvDatastore = new System.Windows.Forms.DataGridView();
		this.vDatastoreName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreConfigStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreAddress = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreAccessible = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreType = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreVMsTotal = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreVMs = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreCapacity = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreProvisioned = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreInUse = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreFreeSpace = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDataStoreFreePercentage = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreSIOCFlag = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreSIOCThreshold = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreNumberofHosts = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreHosts = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreClusterName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreClusterCapacity = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreClusterFreeSpace = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreBlockSizeMiB = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreMaxBlocks = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreExtents = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreMajorVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreVMFSUpgradable = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreMultipleHostAccess = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreURL = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreObjectID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreVISDKServer = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDatastoreInstanceUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.tabvSC_VMK = new System.Windows.Forms.TabPage();
		this.panel20 = new System.Windows.Forms.Panel();
		this.dataGridvSC_VMK = new System.Windows.Forms.DataGridView();
		this.vSC_VMKHost = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSC_VMKDatacenter = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSC_VMKCluster = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSC_VMKPortGroup = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSC_VMKDevice = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSC_VMKMAC = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSC_VMKDHCP = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSC_VMKIP = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSC_VMKIP6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSC_VMKMask = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSC_VMKGateway = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSC_VMKGateway6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSC_VMKMTU = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSC_VMKVISDKServer = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSC_VMKInstanceUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.tabdvPort = new System.Windows.Forms.TabPage();
		this.panel22 = new System.Windows.Forms.Panel();
		this.dataGriddvPort = new System.Windows.Forms.DataGridView();
		this.dvPortName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortSwitch = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortType = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortNumPorts = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortVLAN = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortSpeed = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortFullDuplex = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortBlocked = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortAllowPromiscuous = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortMacChanges = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortActiveUplink = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortStandbyUplink = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortPolicy = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortForgedTransmits = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortInTrafficShaping = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortInTrafficAverage = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortInTrafficPeak = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortInTrafficBurst = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortOutTrafficShaping = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortOutTrafficAverage = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortOutTrafficPeak = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortOutTrafficBurst = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortReversePolicy = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortNotifySwitch = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortRollingOrder = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortCheckBeacon = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortLivePortMovingAllowed = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortCheckDuplex = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortCheckErrorPercent = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortCheckSpeed = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortPercentage = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortBlockOverrideAllowed = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortPortConfigResetAtDisconnect = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortShapingOverrideAllowed = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortVendorConfigOverrideAllowed = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortSecurityPolicyOverrideAllowed = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortUplinkTeamingOverrideAllowed = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortVlanOverrideAllowed = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortObjectID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortVISDKServer = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvPortInstanceUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.tabdvSwitch = new System.Windows.Forms.TabPage();
		this.panel21 = new System.Windows.Forms.Panel();
		this.dataGriddvSwitch = new System.Windows.Forms.DataGridView();
		this.dvSwitchName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchDatacenter = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchProductInfoName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchProductInfoVendor = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchProductInfoVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchCreateTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchHostMembers = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchMaxPorts = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchNumPorts = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchNumVms = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchInTrafficEnabled = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchInTrafficAvg = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchInTrafficPeak = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchInTrafficBurst = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchOutTrafficEnabled = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchOutTrafficAvg = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchOutTrafficPeak = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchOutTrafficBurst = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchCDPProtocol = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchCDPoperation = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchLacpName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchLacpMode = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchLacpLoadBalance = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchMaxMtu = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchContactInfoContact = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchContactInfoName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchObjectID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchVISDKServer = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dvSwitchInstanceUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.tabvPort = new System.Windows.Forms.TabPage();
		this.panel18 = new System.Windows.Forms.Panel();
		this.dataGridvPort = new System.Windows.Forms.DataGridView();
		this.vPortHost = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPortDatacenter = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPortCluster = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPortName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPortSwitch = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPortVLAN = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPortAllowPromiscuous = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPortMacChanges = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPortForgedTransmits = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPortTrafficShaping = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPortAverageBandwidth = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPortPeakBandwidth = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPortBurstSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPortPolicy = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPortReversePolicy = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPortNotifySwitches = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPortRollingOrder = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPortCsumOffload = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPortTCPSegmentation = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPortZeroCopyXmit = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPortVISDKServer = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPortInstanceUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.tabvSwitch = new System.Windows.Forms.TabPage();
		this.panel17 = new System.Windows.Forms.Panel();
		this.dataGridvSwitch = new System.Windows.Forms.DataGridView();
		this.vSwitchHost = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSwitchDatacenter = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSwitchCluster = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSwitchName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSwitchNumPorts = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSwitchFreePorts = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSwitchAllowPromiscuous = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSwitchMacChanges = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSwitchForgedTransmits = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSwitchTrafficShaping = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSwitchAverageBandwidth = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSwitchPeakBandwidth = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSwitchBurstSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSwitchPolicy = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSwitchReversePolicy = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSwitchNotifySwitches = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSwitchRollingOrder = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSwitchCsumOffload = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSwitchTCPSegmentation = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSwitchZeroCopyXmit = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSwitchMTU = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSwitchVISDKServer = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSwitchInstanceUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.tabvNIC = new System.Windows.Forms.TabPage();
		this.panel19 = new System.Windows.Forms.Panel();
		this.dataGridvNIC = new System.Windows.Forms.DataGridView();
		this.vNicHostName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNicDatacenter = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNicCluster = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNicDevice = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNicDriver = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNicSpeed = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNicDuplex = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNicMac = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNicSwitch = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNicUplinkPort = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNicPci = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNicWakeOn = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNICVISDKServer = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNICInstanceUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.tabvHBA = new System.Windows.Forms.TabPage();
		this.panel23 = new System.Windows.Forms.Panel();
		this.dataGridvHBA = new System.Windows.Forms.DataGridView();
		this.vHBAHostName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHBADatacenter = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHBACluster = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHBADevice = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHBAType = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHBAStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHBABus = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHBAPci = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHBADriver = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHBAModel = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHBAWorldWideName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHBAVISDKServer = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHBAInstanceUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.tabvHost = new System.Windows.Forms.TabPage();
		this.panel14 = new System.Windows.Forms.Panel();
		this.dataGridvHost = new System.Windows.Forms.DataGridView();
		this.tabvCluster = new System.Windows.Forms.TabPage();
		this.panel25 = new System.Windows.Forms.Panel();
		this.dataGridvCluster = new System.Windows.Forms.DataGridView();
		this.vClusterName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterConfigStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterOverallStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterNumHosts = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterNumEffectiveHosts = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterTotalCpu = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterNumCpuCores = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterNumCpuThreads = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterEffectiveCpu = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterTotalMemory = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterEffectiveMemory = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterNumVmotions = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterEnabled = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterFailoverLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterAdmissionControlEnabled = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterHostMonitoring = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterHBDatastoreCandidatePolicy = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterIsolationResponse = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterRestartPriority = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterClusterSettings = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterMaxFailures = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterMaxFailureWindow = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterFailureInterval = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterMinUpTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterVmMonitoring = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterDRSEnabled = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterDRSDefaultVmBehavior = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterDRSvmotionRate = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterDpmEnabled = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterDefaultDpmBehavior = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterHostPowerActionRate = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterObjectID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterVISDKServer = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vClusterInstanceUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.tabvRP = new System.Windows.Forms.TabPage();
		this.panel24 = new System.Windows.Forms.Panel();
		this.dataGridvRP = new System.Windows.Forms.DataGridView();
		this.vRPName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPPath = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPstatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPVMsTotal = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPVMs = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPnumberOfCPUs = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPCPUlimit = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPCPUoverheadLimit = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPCPUreservation = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPCPUlevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPCPUshares = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPCPUexpandableReservation = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPCPUmaxUsage = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPCPUoverallUsage = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPCPUreservationUsed = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPCPUreservationUsedForVm = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPCPUunreservedForPool = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPCPUunreservedForVm = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPconfiguredMemoryMiB = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPMEMlimit = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPMEMoverheadLimit = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPMEMreservation = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPMEMlevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPMEMshares = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPMEMexpandableReservation = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPMEMmaxUsage = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPMEMoverallUsage = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPMEMreservationUsed = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPMEMreservationUsedForVm = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPMEMunreservedForPool = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPMEMunreservedForVm = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPoverallCpuDemand = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPoverallCpuUsage = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPstaticCpuEntitlement = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPdistributedCpuEntitlement = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPballoonedMemory = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPcompressedMemory = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPconsumedOverheadMemory = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPdistributedMemoryEntitlement = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPguestMemoryUsage = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPhostMemoryUsage = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPoverheadMemory = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPprivateMemory = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPsharedMemory = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPstaticMemoryEntitlement = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPswappedMemory = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPObjectID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPVISDKServer = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vRPInstanceUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.tabvTools = new System.Windows.Forms.TabPage();
		this.panel10 = new System.Windows.Forms.Panel();
		this.dataGridvTools = new System.Windows.Forms.DataGridView();
		this.vToolsUpgrade = new System.Windows.Forms.DataGridViewCheckBoxColumn();
		this.vToolsVMName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsPowerstate = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsTemplate = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsSRMPlaceholder = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsVMVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsTools = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsRequiredVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsUpgradeable = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsUpgradePolicy = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsSyncTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsAppState = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsAppHeartbeatStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsKernelCrashed = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsOperationsReady = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsStateChangeSupported = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsInteractiveOperationsReady = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsNotes = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsDatacenter = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsCluster = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsHost = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsFolder = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsOS = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsguestFullName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsVMRef = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsObjectID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsVISDKServer = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vToolsInstanceUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.panel11 = new System.Windows.Forms.Panel();
		this.button1 = new System.Windows.Forms.Button();
		this.tabvSnapshot = new System.Windows.Forms.TabPage();
		this.panel9 = new System.Windows.Forms.Panel();
		this.dataGridvSnapshot = new System.Windows.Forms.DataGridView();
		this.vSnapshotVMName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSnapshotPowerstate = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSnapshotName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSnapshotDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSnapshotCreateTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSnapshotFilename = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSnapshotSizeVmsn = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSnapshotSizeTotal = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSnapshotQuiesced = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSnapshotState = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSnapshotNotes = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSnapshotDatacenter = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSnapshotCluster = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSnapshotHost = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSnapshotFolder = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSnapshotOS = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSnapshotOSTools = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSnapshotObjectID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSnapshotUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSnapshotVISDKServer = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSnapshotInstanceUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.tabvCD = new System.Windows.Forms.TabPage();
		this.panel12 = new System.Windows.Forms.Panel();
		this.dataGridvCD = new System.Windows.Forms.DataGridView();
		this.vCDSelect = new System.Windows.Forms.DataGridViewCheckBoxColumn();
		this.vCDVMName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCDPowerstate = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCDTemplate = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCDSRMPlaceholder = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCDlabel = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCDConnected = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCDStartup = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCDSummary = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCDNotes = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCDDatacenter = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCDCluster = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCDHost = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCDFolder = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCDOS = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCDOSTools = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCDVMRef = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCDObjectID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCDUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCDVISDKServer = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCDInstanceUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.panel13 = new System.Windows.Forms.Panel();
		this.buttonDisconnectCD = new System.Windows.Forms.Button();
		this.tabvUSB = new System.Windows.Forms.TabPage();
		this.panel1 = new System.Windows.Forms.Panel();
		this.dataGridvUSB = new System.Windows.Forms.DataGridView();
		this.vUSBSelect = new System.Windows.Forms.DataGridViewCheckBoxColumn();
		this.vUSBVMName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vUSBPowerstate = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vUSBTemplate = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vUSBSRMPlaceholder = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vUSBLabel = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vUSBSummary = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vUSBConnected = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vUSBFamily = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vUSBSpeed = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vUSBEHCIEnabled = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vUSBAutoConnect = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vUSBBusNumber = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vUSBUnitNumber = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vUSBNotes = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vUSBDatacenter = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vUSBCluster = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vUSBHost = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vUSBFolder = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vUSBOS = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vUSBOSTools = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vUSBVMRef = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vUSBObjectID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vUSBUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vUSBVISDKServer = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vUSBInstanceUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.panel2 = new System.Windows.Forms.Panel();
		this.buttonDisconnectUSB = new System.Windows.Forms.Button();
		this.tabvNetwork = new System.Windows.Forms.TabPage();
		this.panel5 = new System.Windows.Forms.Panel();
		this.dataGridvNetwork = new System.Windows.Forms.DataGridView();
		this.vNetworkVMName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNetworkPowerstate = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNetworkTemplate = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNetworkSRMPlaceholder = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNetworkNic = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNetworkAdapter = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNetworkName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNetworkSwitch = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNetworkConnected = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNetworkStartsConnected = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNetworkMacAddress = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNetworkMacAddressType = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNetworkIP4Address = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNetworkIP6Address = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNetworkDirectPathIO = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNetworkSortColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNetworkNotes = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNetworkDatacenter = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNetworkCluster = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNetworkHost = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNetworkFolder = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNetworkOS = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNetworkOSTools = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNetworkObjectID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNetworkUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNetworkVISDKServer = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vNetworkInstanceUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.tabvPartition = new System.Windows.Forms.TabPage();
		this.panel16 = new System.Windows.Forms.Panel();
		this.dataGridvPartition = new System.Windows.Forms.DataGridView();
		this.vPartitionVMName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPartitionPowerstate = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPartitionTemplate = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPartitionSRMPlaceholder = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPartitionDiskKey = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPartitionDisk = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPartitionCapacityMiB = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPartitionConsumedMiB = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPartitionFreeMiB = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPartitionFreePercentage = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPartitionSortColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPartitionNotes = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPartitionDatacenter = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPartitionCluster = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPartitionHost = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPartitionFolder = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPartitionOS = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPartitionOSTools = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPartitionObjectID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPartitionUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPartitionVISDKServer = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vPartitionInstanceUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.tabvDisk = new System.Windows.Forms.TabPage();
		this.panel6 = new System.Windows.Forms.Panel();
		this.dataGridvDisk = new System.Windows.Forms.DataGridView();
		this.vDiskVMName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskPowerstate = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskTemplate = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskSRMPlaceholder = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskDisk = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskKey = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskDiskUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskDiskPath = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskCapacityMiB = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskRaw = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskMode = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskSharingMode = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskThinProvisioned = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskEagerlyScrub = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskSplit = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskWriteThrough = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskShares = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskReservation = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskLimit = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskController = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskControllerLabel = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskSCSICtlrUnitNumber = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskUnit = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskControllerSharedBus = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskPath = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskRawLunId = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskRawCompMode = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskSortColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskNotes = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskDatacenter = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskCluster = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskHost = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskFolder = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskOS = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskOSTools = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskObjectID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDIskUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskVISDKServer = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vDiskInstanceUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.tabvMemory = new System.Windows.Forms.TabPage();
		this.panel8 = new System.Windows.Forms.Panel();
		this.dataGridvMemory = new System.Windows.Forms.DataGridView();
		this.vMemoryVMName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemoryPowerstate = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemoryTemplate = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemorySRMPlaceholder = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemorySizeMiB = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemoryReservationLockedToMax = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemoryOverhead = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemoryMaxUsage = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemoryHostMemoryUsage = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemoryconsumedOverheadMemory = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemoryprivateMemory = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemorysharedMemory = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemoryswappedMemory = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemoryballoonedMemory = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemoryGuestMemoryUsage = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemorystaticMemoryEntitlement = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemorydistributedMemoryEntitlement = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemorySharesLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemoryShares = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemoryReservation = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemoryReservationLimit = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemoryHotAdd = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemoryNotes = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemoryDatacenter = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemoryCluster = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemoryHost = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemoryFolder = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemoryOS = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemoryOSTools = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemoryObjectID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemoryUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemoryVISDKServer = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vMemoryInstanceUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.tabvCPU = new System.Windows.Forms.TabPage();
		this.panel7 = new System.Windows.Forms.Panel();
		this.dataGridvCPU = new System.Windows.Forms.DataGridView();
		this.vCPUVMName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUPowerstate = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUTemplate = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUSRMPlaceholder = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUCPUs = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUSockets = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUCoresPerSocket = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUMaxCpuUsage = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUOverallCpuUsage = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUSharesLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUShares = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUReservation = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUStaticCpuEntitlement = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUDistributedCpuEntitlement = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUReservationLimit = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUHotAdd = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUHotRemove = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUNumaOnCpuHotaddExposed = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUNotes = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUDatacenter = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUCluster = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUHost = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUFolder = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUOS = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUOSTools = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUObjectID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUVISDKServer = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vCPUInstanceUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.tabvInfo = new System.Windows.Forms.TabPage();
		this.panel4 = new System.Windows.Forms.Panel();
		this.dataGridvInfo = new System.Windows.Forms.DataGridView();
		this.vInfoVMName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoPowerstate = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoTemplate = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoSRMPlaceHolder = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoConfigStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoGuestHostName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoConnectionState = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoGuestState = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoHeartbeat = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoConsolidationNeeded = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoBootTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoSuspendedToMemory = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoSuspendTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoSuspendInterval = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoCreateDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoChangeVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoCPUs = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoOverallCpuReadiness = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoMemory = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoActiveMemory = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoNICs = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoNumVirtualDisks = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoTotalDiskCapacityMiB = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoFixedPassthruHotPlugEnabled = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfominRequiredEVCModeKey = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoLatencySensitivity = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoOpNotificationTimeout = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoEnableUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vINfoCBT = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoPrimaryIPAddress = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoNetwork1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoNetwork2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoNetwork3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoNetwork4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoNetwork5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoNetwork6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoNetwork7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoNetwork8 = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoNumDisplays = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoVideoRamKiB = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoResourcepool = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoFolderId = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoFolder = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfovApp = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoDASProtection = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoFaultToleranceState = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoFTInfoRole = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoFTLatencyStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoFTLogBandwidth = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoFTSecondaryLatency = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoVmFailoverInProgress = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoProvisioned = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoInUse = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoUnshared = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoHARestartPriority = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoHAIsolationResponse = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoVMmonitoring = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoClusterRule = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoClusterRuleName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoInstallBootRequired = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoBootDelay = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoBootRetryDelay = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoBootRetryEnabled = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoBootBios = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoRebootPowerOff = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoEfiSecureBoot = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoFirmware = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.VInfoVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoHWUpgradeStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoHWUpgradePolicy = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoHWTarget = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfopath = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoLogDirectory = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoSnapshotDirectory = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoSuspendDirectory = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoNotes = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoDataCenter = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoCluster = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoHost = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoOS = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoOSTools = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoCustomizationInfo = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoGuestDetailedData = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoObjectID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoSMBIOSUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoVISDKServerType = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoVISDKAPI = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoVISDKServer = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vInfoInstanceUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.TabPages = new System.Windows.Forms.TabControl();
		this.tabvSource = new System.Windows.Forms.TabPage();
		this.panel29 = new System.Windows.Forms.Panel();
		this.dataGridvSource = new System.Windows.Forms.DataGridView();
		this.vSourceName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSourceOSType = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSourceApiType = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSourceAPIVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSourceVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSourcePatchLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSourceBuild = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSourceFullname = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSourceProductName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSourceProductVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSourceProductLine = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSourceVendor = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSourceVISDKServer = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vSourceVISDKUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.tabvFileInfo = new System.Windows.Forms.TabPage();
		this.panel28 = new System.Windows.Forms.Panel();
		this.dataGridvFileInfo = new System.Windows.Forms.DataGridView();
		this.vFileInfoFriendlyPath = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vFileInfoFileName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vFileInfoFileType = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vFileInfoFileSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vFileInfoPath = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vFileInfoSortColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vFileInfoSDKServer = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vFileInfoSDKUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostDatacenter = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostCluster = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostConfigStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostComplianceCheckState = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostinMaintenanceMode = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostinQuarantineMode = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostvSANFaultDomainName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostCpuModel = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostCpuMhz = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostHTAvailable = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostHTActive = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostNumCpu = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostCoresPerCPU = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostNumCpuCores = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostOverallCpuUsage = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostMemorySize = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostMemoryTieringType = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostOverallMemoryUsage = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostServiceConsoleReserved = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostNumNics = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostNumHBAs = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostVMsTotal = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostVMs = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostVMsPerCore = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostvCPUs = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostVCPUsPerCore = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostvRAM = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostVMUsedMemory = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostVMSwappedMemory = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostVMBalloonedMemory = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostVmotionSupported = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostStorageVmotionSupported = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostCurrentEVCModeKey = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostMaxEVCModeKey = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostAssignedLicenses = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostATSHeartbeat = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostATSLocking = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostCurrentPolicy = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostHardwareSupport = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostHostPowerPolicyShortname = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostFullName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostBootTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostDNSServers = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostDHCP = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostDomainName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostDomainList = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostSearchDomains = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostNTPServers = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostNTPD = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostTZDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostTZName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostTZOffset = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostVendor = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostModel = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostSerialNumber = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostServiceTag = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostOemSpecificString = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostBiosVendor = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostBiosVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostBiosDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostCertificateIssuer = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostCertificateStartDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostCertificateExpiry = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostCertificateStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostCertificateSubject = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostObjectID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostVISDKServer = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.vHostInstanceUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.statusStrip1.SuspendLayout();
		this.menuStrip1.SuspendLayout();
		this.tabvLicense.SuspendLayout();
		this.panel27.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dataGridvLicense).BeginInit();
		this.tabvHealth.SuspendLayout();
		this.panel15.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dataGridvHealth).BeginInit();
		this.tabvMultiPath.SuspendLayout();
		this.panel26.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dataGridvMultiPath).BeginInit();
		this.tabvDatastore.SuspendLayout();
		this.panel3.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dataGridvDatastore).BeginInit();
		this.tabvSC_VMK.SuspendLayout();
		this.panel20.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dataGridvSC_VMK).BeginInit();
		this.tabdvPort.SuspendLayout();
		this.panel22.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dataGriddvPort).BeginInit();
		this.tabdvSwitch.SuspendLayout();
		this.panel21.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dataGriddvSwitch).BeginInit();
		this.tabvPort.SuspendLayout();
		this.panel18.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dataGridvPort).BeginInit();
		this.tabvSwitch.SuspendLayout();
		this.panel17.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dataGridvSwitch).BeginInit();
		this.tabvNIC.SuspendLayout();
		this.panel19.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dataGridvNIC).BeginInit();
		this.tabvHBA.SuspendLayout();
		this.panel23.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dataGridvHBA).BeginInit();
		this.tabvHost.SuspendLayout();
		this.panel14.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dataGridvHost).BeginInit();
		this.tabvCluster.SuspendLayout();
		this.panel25.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dataGridvCluster).BeginInit();
		this.tabvRP.SuspendLayout();
		this.panel24.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dataGridvRP).BeginInit();
		this.tabvTools.SuspendLayout();
		this.panel10.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dataGridvTools).BeginInit();
		this.panel11.SuspendLayout();
		this.tabvSnapshot.SuspendLayout();
		this.panel9.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dataGridvSnapshot).BeginInit();
		this.tabvCD.SuspendLayout();
		this.panel12.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dataGridvCD).BeginInit();
		this.panel13.SuspendLayout();
		this.tabvUSB.SuspendLayout();
		this.panel1.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dataGridvUSB).BeginInit();
		this.panel2.SuspendLayout();
		this.tabvNetwork.SuspendLayout();
		this.panel5.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dataGridvNetwork).BeginInit();
		this.tabvPartition.SuspendLayout();
		this.panel16.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dataGridvPartition).BeginInit();
		this.tabvDisk.SuspendLayout();
		this.panel6.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dataGridvDisk).BeginInit();
		this.tabvMemory.SuspendLayout();
		this.panel8.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dataGridvMemory).BeginInit();
		this.tabvCPU.SuspendLayout();
		this.panel7.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dataGridvCPU).BeginInit();
		this.tabvInfo.SuspendLayout();
		this.panel4.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dataGridvInfo).BeginInit();
		this.TabPages.SuspendLayout();
		this.tabvSource.SuspendLayout();
		this.panel29.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dataGridvSource).BeginInit();
		this.tabvFileInfo.SuspendLayout();
		this.panel28.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dataGridvFileInfo).BeginInit();
		base.SuspendLayout();
		this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
		this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[6] { this.StatusUsername, this.StatusServer, this.StatusVCFullname, this.StatusGridCounter, this.StatusLastRefresh, this.StatusFilter });
		this.statusStrip1.Location = new System.Drawing.Point(13, 462);
		this.statusStrip1.Name = "statusStrip1";
		this.statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 19, 0);
		this.statusStrip1.Size = new System.Drawing.Size(1733, 22);
		this.statusStrip1.TabIndex = 0;
		this.statusStrip1.Text = "statusStrip1";
		this.StatusUsername.AutoSize = false;
		this.StatusUsername.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
		this.StatusUsername.Image = (System.Drawing.Image)resources.GetObject("StatusUsername.Image");
		this.StatusUsername.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
		this.StatusUsername.Name = "StatusUsername";
		this.StatusUsername.Size = new System.Drawing.Size(200, 16);
		this.StatusUsername.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		this.StatusServer.AutoSize = false;
		this.StatusServer.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
		this.StatusServer.Image = (System.Drawing.Image)resources.GetObject("StatusServer.Image");
		this.StatusServer.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
		this.StatusServer.Name = "StatusServer";
		this.StatusServer.Size = new System.Drawing.Size(200, 16);
		this.StatusServer.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		this.StatusVCFullname.AutoSize = false;
		this.StatusVCFullname.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
		this.StatusVCFullname.Image = (System.Drawing.Image)resources.GetObject("StatusVCFullname.Image");
		this.StatusVCFullname.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
		this.StatusVCFullname.Name = "StatusVCFullname";
		this.StatusVCFullname.Size = new System.Drawing.Size(350, 16);
		this.StatusVCFullname.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		this.StatusGridCounter.AutoSize = false;
		this.StatusGridCounter.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
		this.StatusGridCounter.Name = "StatusGridCounter";
		this.StatusGridCounter.Size = new System.Drawing.Size(75, 16);
		this.StatusGridCounter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		this.StatusLastRefresh.AutoSize = false;
		this.StatusLastRefresh.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
		this.StatusLastRefresh.Name = "StatusLastRefresh";
		this.StatusLastRefresh.Size = new System.Drawing.Size(200, 16);
		this.StatusLastRefresh.Text = "Last refresh: ";
		this.StatusLastRefresh.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		this.StatusFilter.AutoSize = false;
		this.StatusFilter.Image = RVTools.Properties.Resources.filter;
		this.StatusFilter.Name = "StatusFilter";
		this.StatusFilter.Size = new System.Drawing.Size(29, 16);
		this.StatusFilter.Visible = false;
		this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
		this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[7] { this.fileToolStripMenuItem, this.toolStripMenuEdit, this.toolStripMenuView, this.viewToolStripMenuItem, this.toolStripMenuESX, this.healhtToolStripMenuItem, this.helpToolStripMenuItem });
		this.menuStrip1.Location = new System.Drawing.Point(13, 6);
		this.menuStrip1.Name = "menuStrip1";
		this.menuStrip1.Padding = new System.Windows.Forms.Padding(0, 0, 0, 12);
		this.menuStrip1.Size = new System.Drawing.Size(1733, 36);
		this.menuStrip1.TabIndex = 2;
		this.menuStrip1.Text = "menuStrip1";
		this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[6] { this.ExportToolStripMenuItem, this.toolStripSeparator1, this.ExportAll2xlsMenuItem, this.ExportAllMenuItem, this.toolStripSeparator3, this.exitToolStripMenuItem });
		this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
		this.fileToolStripMenuItem.Size = new System.Drawing.Size(46, 24);
		this.fileToolStripMenuItem.Text = "&File";
		this.ExportToolStripMenuItem.Image = RVTools.Properties.Resources.export;
		this.ExportToolStripMenuItem.Name = "ExportToolStripMenuItem";
		this.ExportToolStripMenuItem.Size = new System.Drawing.Size(211, 26);
		this.ExportToolStripMenuItem.Text = "Export";
		this.ExportToolStripMenuItem.Click += new System.EventHandler(this.ExportToolStripMenuItem_Click);
		this.toolStripSeparator1.Name = "toolStripSeparator1";
		this.toolStripSeparator1.Size = new System.Drawing.Size(208, 6);
		this.ExportAll2xlsMenuItem.Image = RVTools.Properties.Resources.excel;
		this.ExportAll2xlsMenuItem.Name = "ExportAll2xlsMenuItem";
		this.ExportAll2xlsMenuItem.Size = new System.Drawing.Size(211, 26);
		this.ExportAll2xlsMenuItem.Text = "Export all to Excel";
		this.ExportAll2xlsMenuItem.Click += new System.EventHandler(this.ExportAll2xlsMenuItem_Click);
		this.ExportAllMenuItem.Image = RVTools.Properties.Resources.csv;
		this.ExportAllMenuItem.Name = "ExportAllMenuItem";
		this.ExportAllMenuItem.Size = new System.Drawing.Size(211, 26);
		this.ExportAllMenuItem.Text = "Export all to csv";
		this.ExportAllMenuItem.Click += new System.EventHandler(this.ExportAllMenuItem_Click);
		this.toolStripSeparator3.Name = "toolStripSeparator3";
		this.toolStripSeparator3.Size = new System.Drawing.Size(208, 6);
		this.exitToolStripMenuItem.Image = (System.Drawing.Image)resources.GetObject("exitToolStripMenuItem.Image");
		this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
		this.exitToolStripMenuItem.Size = new System.Drawing.Size(211, 26);
		this.exitToolStripMenuItem.Text = "E&xit";
		this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
		this.toolStripMenuEdit.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[1] { this.toolStripPreferences });
		this.toolStripMenuEdit.Name = "toolStripMenuEdit";
		this.toolStripMenuEdit.Size = new System.Drawing.Size(49, 24);
		this.toolStripMenuEdit.Text = "&Edit";
		this.toolStripPreferences.Image = RVTools.Properties.Resources.Preferences;
		this.toolStripPreferences.Name = "toolStripPreferences";
		this.toolStripPreferences.Size = new System.Drawing.Size(168, 26);
		this.toolStripPreferences.Text = "Preferences";
		this.toolStripPreferences.Click += new System.EventHandler(this.toolStripPreferences_Click);
		this.toolStripMenuView.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[2] { this.toolStripMenuFilter, this.refreshToolStripMenuItem });
		this.toolStripMenuView.Name = "toolStripMenuView";
		this.toolStripMenuView.Size = new System.Drawing.Size(55, 24);
		this.toolStripMenuView.Text = "&View";
		this.toolStripMenuFilter.Image = RVTools.Properties.Resources.filter;
		this.toolStripMenuFilter.Name = "toolStripMenuFilter";
		this.toolStripMenuFilter.ShortcutKeys = System.Windows.Forms.Keys.F | System.Windows.Forms.Keys.Control;
		this.toolStripMenuFilter.Size = new System.Drawing.Size(174, 26);
		this.toolStripMenuFilter.Text = "Filter";
		this.toolStripMenuFilter.Click += new System.EventHandler(this.toolStripMenuFilter_Click);
		this.refreshToolStripMenuItem.Image = RVTools.Properties.Resources.arrow_refresh;
		this.refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
		this.refreshToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
		this.refreshToolStripMenuItem.Size = new System.Drawing.Size(174, 26);
		this.refreshToolStripMenuItem.Text = "Refresh";
		this.refreshToolStripMenuItem.Click += new System.EventHandler(this.refreshToolStripMenuItem_Click);
		this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[10] { this.VMtoolStripMenuItem1, this.CPUToolStripMenuItem, this.MemoryToolStripMenuItem, this.toolStripMenuItem1, this.PartitiontoolStripMenuItem, this.NetworktoolStripMenuItem1, this.toolStripMenuItem2, this.toolStripMenuItem3, this.SnapshotToolStripMenuItem, this.VMToolsToolStripMenuItem });
		this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
		this.viewToolStripMenuItem.Size = new System.Drawing.Size(45, 24);
		this.viewToolStripMenuItem.Text = "V&M";
		this.VMtoolStripMenuItem1.Image = RVTools.Properties.Resources.vInfo;
		this.VMtoolStripMenuItem1.Name = "VMtoolStripMenuItem1";
		this.VMtoolStripMenuItem1.Size = new System.Drawing.Size(153, 26);
		this.VMtoolStripMenuItem1.Text = "Info";
		this.VMtoolStripMenuItem1.Click += new System.EventHandler(this.VMtoolStripMenuItem1_Click);
		this.CPUToolStripMenuItem.Image = RVTools.Properties.Resources.vCPU;
		this.CPUToolStripMenuItem.Name = "CPUToolStripMenuItem";
		this.CPUToolStripMenuItem.Size = new System.Drawing.Size(153, 26);
		this.CPUToolStripMenuItem.Text = "CPU";
		this.CPUToolStripMenuItem.Click += new System.EventHandler(this.CPUToolStripMenuItem_Click);
		this.MemoryToolStripMenuItem.Image = RVTools.Properties.Resources.vMemory;
		this.MemoryToolStripMenuItem.Name = "MemoryToolStripMenuItem";
		this.MemoryToolStripMenuItem.Size = new System.Drawing.Size(153, 26);
		this.MemoryToolStripMenuItem.Text = "Memory";
		this.MemoryToolStripMenuItem.Click += new System.EventHandler(this.MemoryToolStripMenuItem_Click);
		this.toolStripMenuItem1.Image = RVTools.Properties.Resources.drive;
		this.toolStripMenuItem1.Name = "toolStripMenuItem1";
		this.toolStripMenuItem1.Size = new System.Drawing.Size(153, 26);
		this.toolStripMenuItem1.Text = "Disk";
		this.toolStripMenuItem1.Click += new System.EventHandler(this.toolStripMenuItem1_Click);
		this.PartitiontoolStripMenuItem.Image = RVTools.Properties.Resources.partition1;
		this.PartitiontoolStripMenuItem.Name = "PartitiontoolStripMenuItem";
		this.PartitiontoolStripMenuItem.Size = new System.Drawing.Size(153, 26);
		this.PartitiontoolStripMenuItem.Text = "Partition";
		this.PartitiontoolStripMenuItem.Click += new System.EventHandler(this.PartiontoolStripMenuItem_Click);
		this.NetworktoolStripMenuItem1.Image = RVTools.Properties.Resources.vNic;
		this.NetworktoolStripMenuItem1.Name = "NetworktoolStripMenuItem1";
		this.NetworktoolStripMenuItem1.Size = new System.Drawing.Size(153, 26);
		this.NetworktoolStripMenuItem1.Text = "Network";
		this.NetworktoolStripMenuItem1.Click += new System.EventHandler(this.NetworktoolStripMenuItem1_Click);
		this.toolStripMenuItem2.Image = RVTools.Properties.Resources.vCD;
		this.toolStripMenuItem2.Name = "toolStripMenuItem2";
		this.toolStripMenuItem2.Size = new System.Drawing.Size(153, 26);
		this.toolStripMenuItem2.Text = "CD";
		this.toolStripMenuItem2.Click += new System.EventHandler(this.toolStripMenuItem2_Click);
		this.toolStripMenuItem3.Image = RVTools.Properties.Resources.vUSB1;
		this.toolStripMenuItem3.Name = "toolStripMenuItem3";
		this.toolStripMenuItem3.Size = new System.Drawing.Size(153, 26);
		this.toolStripMenuItem3.Text = "USB";
		this.toolStripMenuItem3.Click += new System.EventHandler(this.toolStripMenuItem3_Click);
		this.SnapshotToolStripMenuItem.Image = RVTools.Properties.Resources.snapshot;
		this.SnapshotToolStripMenuItem.Name = "SnapshotToolStripMenuItem";
		this.SnapshotToolStripMenuItem.Size = new System.Drawing.Size(153, 26);
		this.SnapshotToolStripMenuItem.Text = "Snapshot";
		this.SnapshotToolStripMenuItem.Click += new System.EventHandler(this.SnapshotToolStripMenuItem_Click);
		this.VMToolsToolStripMenuItem.Image = RVTools.Properties.Resources.vTools;
		this.VMToolsToolStripMenuItem.Name = "VMToolsToolStripMenuItem";
		this.VMToolsToolStripMenuItem.Size = new System.Drawing.Size(153, 26);
		this.VMToolsToolStripMenuItem.Text = "VM Tools";
		this.VMToolsToolStripMenuItem.Click += new System.EventHandler(this.vMToolsToolStripMenuItem_Click);
		this.toolStripMenuESX.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[14]
		{
			this.vSourceStripMenuItem, this.vRPToolStripMenuItem, this.vClusterStripMenuItem, this.vHostToolstripMenuItem, this.vHBAToolStripMenuItem, this.NICToolStripMenuItem, this.vSwitchToolStripMenuItem, this.vPortToolStripMenuItem, this.dvSwitchtoolStripMenuItem, this.dvPorttoolStripMenuItem,
			this.vConsoleToolStripMenuItem, this.DatastoreToolStripMenuItem, this.multiPathToolStripMenuItem, this.licenseToolStripMenuItem
		});
		this.toolStripMenuESX.Name = "toolStripMenuESX";
		this.toolStripMenuESX.Size = new System.Drawing.Size(48, 24);
		this.toolStripMenuESX.Text = "E&SX";
		this.vSourceStripMenuItem.Image = RVTools.Properties.Resources.application_go;
		this.vSourceStripMenuItem.Name = "vSourceStripMenuItem";
		this.vSourceStripMenuItem.Size = new System.Drawing.Size(291, 26);
		this.vSourceStripMenuItem.Text = "vSource";
		this.vSourceStripMenuItem.Click += new System.EventHandler(this.vSourceStripMenuItem_Click);
		this.vRPToolStripMenuItem.Image = RVTools.Properties.Resources.resourcePool;
		this.vRPToolStripMenuItem.Name = "vRPToolStripMenuItem";
		this.vRPToolStripMenuItem.Size = new System.Drawing.Size(291, 26);
		this.vRPToolStripMenuItem.Text = "Resource Pool";
		this.vRPToolStripMenuItem.Click += new System.EventHandler(this.vRPToolStripMenuItem_Click);
		this.vClusterStripMenuItem.Image = RVTools.Properties.Resources.cluster;
		this.vClusterStripMenuItem.Name = "vClusterStripMenuItem";
		this.vClusterStripMenuItem.Size = new System.Drawing.Size(291, 26);
		this.vClusterStripMenuItem.Text = "Cluster";
		this.vClusterStripMenuItem.Click += new System.EventHandler(this.vClusterStripMenuItem_Click);
		this.vHostToolstripMenuItem.Image = RVTools.Properties.Resources.vHost;
		this.vHostToolstripMenuItem.Name = "vHostToolstripMenuItem";
		this.vHostToolstripMenuItem.Size = new System.Drawing.Size(291, 26);
		this.vHostToolstripMenuItem.Text = "Host";
		this.vHostToolstripMenuItem.Click += new System.EventHandler(this.vHostToolstripMenuItem_Click);
		this.vHBAToolStripMenuItem.Image = RVTools.Properties.Resources.vNic;
		this.vHBAToolStripMenuItem.Name = "vHBAToolStripMenuItem";
		this.vHBAToolStripMenuItem.Size = new System.Drawing.Size(291, 26);
		this.vHBAToolStripMenuItem.Text = "HBA";
		this.vHBAToolStripMenuItem.Click += new System.EventHandler(this.vHBAToolStripMenuItem_Click);
		this.NICToolStripMenuItem.Image = RVTools.Properties.Resources.vNic;
		this.NICToolStripMenuItem.Name = "NICToolStripMenuItem";
		this.NICToolStripMenuItem.Size = new System.Drawing.Size(291, 26);
		this.NICToolStripMenuItem.Text = "NIC";
		this.NICToolStripMenuItem.Click += new System.EventHandler(this.NICToolStripMenuItem_Click);
		this.vSwitchToolStripMenuItem.Image = RVTools.Properties.Resources.vSwitch;
		this.vSwitchToolStripMenuItem.Name = "vSwitchToolStripMenuItem";
		this.vSwitchToolStripMenuItem.Size = new System.Drawing.Size(291, 26);
		this.vSwitchToolStripMenuItem.Text = "Switch";
		this.vSwitchToolStripMenuItem.Click += new System.EventHandler(this.vSwitchToolStripMenuItem_Click);
		this.vPortToolStripMenuItem.Image = RVTools.Properties.Resources.Portgroup;
		this.vPortToolStripMenuItem.Name = "vPortToolStripMenuItem";
		this.vPortToolStripMenuItem.Size = new System.Drawing.Size(291, 26);
		this.vPortToolStripMenuItem.Text = "Port";
		this.vPortToolStripMenuItem.Click += new System.EventHandler(this.vPortToolStripMenuItem_Click);
		this.dvSwitchtoolStripMenuItem.Image = RVTools.Properties.Resources.vSwitch;
		this.dvSwitchtoolStripMenuItem.Name = "dvSwitchtoolStripMenuItem";
		this.dvSwitchtoolStripMenuItem.Size = new System.Drawing.Size(291, 26);
		this.dvSwitchtoolStripMenuItem.Text = "Distributed Switch";
		this.dvSwitchtoolStripMenuItem.Click += new System.EventHandler(this.dvSwitchtoolStripMenuItem_Click);
		this.dvPorttoolStripMenuItem.Image = RVTools.Properties.Resources.Portgroup;
		this.dvPorttoolStripMenuItem.Name = "dvPorttoolStripMenuItem";
		this.dvPorttoolStripMenuItem.Size = new System.Drawing.Size(291, 26);
		this.dvPorttoolStripMenuItem.Text = "Distributed Port";
		this.dvPorttoolStripMenuItem.Click += new System.EventHandler(this.dvPorttoolStripMenuItem_Click);
		this.vConsoleToolStripMenuItem.Image = RVTools.Properties.Resources.console;
		this.vConsoleToolStripMenuItem.Name = "vConsoleToolStripMenuItem";
		this.vConsoleToolStripMenuItem.Size = new System.Drawing.Size(291, 26);
		this.vConsoleToolStripMenuItem.Text = "Service console and VMKernel";
		this.vConsoleToolStripMenuItem.Click += new System.EventHandler(this.vConsoleToolStripMenuItem_Click);
		this.DatastoreToolStripMenuItem.Image = (System.Drawing.Image)resources.GetObject("DatastoreToolStripMenuItem.Image");
		this.DatastoreToolStripMenuItem.Name = "DatastoreToolStripMenuItem";
		this.DatastoreToolStripMenuItem.Size = new System.Drawing.Size(291, 26);
		this.DatastoreToolStripMenuItem.Text = "Datastore";
		this.DatastoreToolStripMenuItem.Click += new System.EventHandler(this.DatastoreToolStripMenuItem_Click);
		this.multiPathToolStripMenuItem.Image = RVTools.Properties.Resources.vDatastore;
		this.multiPathToolStripMenuItem.Name = "multiPathToolStripMenuItem";
		this.multiPathToolStripMenuItem.Size = new System.Drawing.Size(291, 26);
		this.multiPathToolStripMenuItem.Text = "MultiPath";
		this.multiPathToolStripMenuItem.Click += new System.EventHandler(this.multiPathToolStripMenuItem_Click);
		this.licenseToolStripMenuItem.Image = RVTools.Properties.Resources.license;
		this.licenseToolStripMenuItem.Name = "licenseToolStripMenuItem";
		this.licenseToolStripMenuItem.Size = new System.Drawing.Size(291, 26);
		this.licenseToolStripMenuItem.Text = "License";
		this.licenseToolStripMenuItem.Click += new System.EventHandler(this.licenseToolStripMenuItem_Click_1);
		this.healhtToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[3] { this.HealthPropertiesToolStripMenuItem, this.toolStripMenuItem4, this.HealthCheckToolStripMenuItem });
		this.healhtToolStripMenuItem.Name = "healhtToolStripMenuItem";
		this.healhtToolStripMenuItem.Size = new System.Drawing.Size(67, 24);
		this.healhtToolStripMenuItem.Text = "Hea&lth";
		this.HealthPropertiesToolStripMenuItem.Image = RVTools.Properties.Resources.Health_Properties;
		this.HealthPropertiesToolStripMenuItem.Name = "HealthPropertiesToolStripMenuItem";
		this.HealthPropertiesToolStripMenuItem.Size = new System.Drawing.Size(159, 26);
		this.HealthPropertiesToolStripMenuItem.Text = "&Properties";
		this.HealthPropertiesToolStripMenuItem.Click += new System.EventHandler(this.HealthPropertiesToolStripMenuItem_Click);
		this.toolStripMenuItem4.Name = "toolStripMenuItem4";
		this.toolStripMenuItem4.Size = new System.Drawing.Size(156, 6);
		this.HealthCheckToolStripMenuItem.Image = RVTools.Properties.Resources.Health_Check;
		this.HealthCheckToolStripMenuItem.Name = "HealthCheckToolStripMenuItem";
		this.HealthCheckToolStripMenuItem.Size = new System.Drawing.Size(159, 26);
		this.HealthCheckToolStripMenuItem.Text = "&Check";
		this.HealthCheckToolStripMenuItem.Click += new System.EventHandler(this.HealthCheckToolStripMenuItem_Click);
		this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[6] { this.aboutToolStripMenuItem, this.toolStripSeparator2, this.CheckForUpdatestoolStripMenuItem, this.twittertoolStripMenuItem, this.toolStripMenuItem5, this.documentationToolStripMenuItem });
		this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
		this.helpToolStripMenuItem.Size = new System.Drawing.Size(55, 24);
		this.helpToolStripMenuItem.Text = "&Help";
		this.aboutToolStripMenuItem.Image = (System.Drawing.Image)resources.GetObject("aboutToolStripMenuItem.Image");
		this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
		this.aboutToolStripMenuItem.Size = new System.Drawing.Size(261, 26);
		this.aboutToolStripMenuItem.Text = "&About";
		this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
		this.toolStripSeparator2.Name = "toolStripSeparator2";
		this.toolStripSeparator2.Size = new System.Drawing.Size(258, 6);
		this.CheckForUpdatestoolStripMenuItem.Image = RVTools.Properties.Resources.world_go;
		this.CheckForUpdatestoolStripMenuItem.Name = "CheckForUpdatestoolStripMenuItem";
		this.CheckForUpdatestoolStripMenuItem.Size = new System.Drawing.Size(261, 26);
		this.CheckForUpdatestoolStripMenuItem.Text = "Check online version info";
		this.CheckForUpdatestoolStripMenuItem.Click += new System.EventHandler(this.CheckForUpdatestoolStripMenu_Click);
		this.twittertoolStripMenuItem.Image = RVTools.Properties.Resources.twitter;
		this.twittertoolStripMenuItem.Name = "twittertoolStripMenuItem";
		this.twittertoolStripMenuItem.Size = new System.Drawing.Size(261, 26);
		this.twittertoolStripMenuItem.Text = "Follow RVTools on twitter";
		this.twittertoolStripMenuItem.Click += new System.EventHandler(this.twittertoolStripMenuItem_Click);
		this.toolStripMenuItem5.Name = "toolStripMenuItem5";
		this.toolStripMenuItem5.Size = new System.Drawing.Size(258, 6);
		this.documentationToolStripMenuItem.Image = RVTools.Properties.Resources.AdobePDF;
		this.documentationToolStripMenuItem.Name = "documentationToolStripMenuItem";
		this.documentationToolStripMenuItem.Size = new System.Drawing.Size(261, 26);
		this.documentationToolStripMenuItem.Text = "Documentation";
		this.documentationToolStripMenuItem.Click += new System.EventHandler(this.documentationToolStripMenuItem_Click);
		this.KeepAliveTimer.Interval = 60000;
		this.KeepAliveTimer.Tick += new System.EventHandler(this.KeepAliveTimer_Tick);
		this.DisplayDataGridTimer.Tick += new System.EventHandler(this.DisplayDataGridTimer_Tick);
		this.tabvLicense.Controls.Add(this.panel27);
		this.tabvLicense.Location = new System.Drawing.Point(4, 25);
		this.tabvLicense.Margin = new System.Windows.Forms.Padding(4);
		this.tabvLicense.Name = "tabvLicense";
		this.tabvLicense.Size = new System.Drawing.Size(1725, 391);
		this.tabvLicense.TabIndex = 33;
		this.tabvLicense.Text = "vLicense";
		this.tabvLicense.UseVisualStyleBackColor = true;
		this.panel27.Controls.Add(this.dataGridvLicense);
		this.panel27.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel27.Location = new System.Drawing.Point(0, 0);
		this.panel27.Margin = new System.Windows.Forms.Padding(4);
		this.panel27.Name = "panel27";
		this.panel27.Padding = new System.Windows.Forms.Padding(20, 18, 20, 18);
		this.panel27.Size = new System.Drawing.Size(1725, 391);
		this.panel27.TabIndex = 6;
		this.dataGridvLicense.AllowUserToAddRows = false;
		this.dataGridvLicense.AllowUserToDeleteRows = false;
		this.dataGridvLicense.AllowUserToOrderColumns = true;
		this.dataGridvLicense.AllowUserToResizeRows = false;
		dataGridViewCellStyle.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
		this.dataGridvLicense.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle;
		this.dataGridvLicense.BackgroundColor = System.Drawing.SystemColors.Window;
		this.dataGridvLicense.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
		this.dataGridvLicense.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGridvLicense.Columns.AddRange(this.vLicenseName, this.vLicenseKey, this.vLicenseLabels, this.vLicenseCostUnit, this.vLicenseTotal, this.vLicenseUsed, this.vLicenseExpirationDate, this.vLicenseFeatures, this.vLicenseVISDKServer, this.vLicenseInstanceUUID);
		this.dataGridvLicense.Dock = System.Windows.Forms.DockStyle.Fill;
		this.dataGridvLicense.Location = new System.Drawing.Point(20, 18);
		this.dataGridvLicense.Margin = new System.Windows.Forms.Padding(4);
		this.dataGridvLicense.Name = "dataGridvLicense";
		this.dataGridvLicense.RowHeadersVisible = false;
		this.dataGridvLicense.RowHeadersWidth = 51;
		this.dataGridvLicense.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this.dataGridvLicense.Size = new System.Drawing.Size(1685, 355);
		this.dataGridvLicense.TabIndex = 4;
		this.vLicenseName.Frozen = true;
		this.vLicenseName.HeaderText = "Name";
		this.vLicenseName.MinimumWidth = 6;
		this.vLicenseName.Name = "vLicenseName";
		this.vLicenseName.ReadOnly = true;
		this.vLicenseName.ToolTipText = "License name";
		this.vLicenseName.Width = 200;
		this.vLicenseKey.HeaderText = "Key";
		this.vLicenseKey.MinimumWidth = 6;
		this.vLicenseKey.Name = "vLicenseKey";
		this.vLicenseKey.ReadOnly = true;
		this.vLicenseKey.ToolTipText = "License key";
		this.vLicenseKey.Width = 200;
		this.vLicenseLabels.HeaderText = "Labels";
		this.vLicenseLabels.MinimumWidth = 6;
		this.vLicenseLabels.Name = "vLicenseLabels";
		this.vLicenseLabels.ReadOnly = true;
		this.vLicenseLabels.ToolTipText = "Labels";
		this.vLicenseLabels.Width = 125;
		this.vLicenseCostUnit.HeaderText = "Cost Unit";
		this.vLicenseCostUnit.MinimumWidth = 6;
		this.vLicenseCostUnit.Name = "vLicenseCostUnit";
		this.vLicenseCostUnit.ReadOnly = true;
		this.vLicenseCostUnit.ToolTipText = "Cost Unit";
		this.vLicenseCostUnit.Width = 125;
		dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle2.Format = "N0";
		dataGridViewCellStyle2.NullValue = null;
		this.vLicenseTotal.DefaultCellStyle = dataGridViewCellStyle2;
		this.vLicenseTotal.HeaderText = "Total";
		this.vLicenseTotal.MinimumWidth = 6;
		this.vLicenseTotal.Name = "vLicenseTotal";
		this.vLicenseTotal.ReadOnly = true;
		this.vLicenseTotal.ToolTipText = "Total licenses";
		this.vLicenseTotal.Width = 125;
		dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle3.Format = "N0";
		dataGridViewCellStyle3.NullValue = null;
		this.vLicenseUsed.DefaultCellStyle = dataGridViewCellStyle3;
		this.vLicenseUsed.HeaderText = "Used";
		this.vLicenseUsed.MinimumWidth = 6;
		this.vLicenseUsed.Name = "vLicenseUsed";
		this.vLicenseUsed.ReadOnly = true;
		this.vLicenseUsed.ToolTipText = "Licenses used";
		this.vLicenseUsed.Width = 125;
		dataGridViewCellStyle4.Format = "G";
		dataGridViewCellStyle4.NullValue = null;
		this.vLicenseExpirationDate.DefaultCellStyle = dataGridViewCellStyle4;
		this.vLicenseExpirationDate.HeaderText = "Expiration Date";
		this.vLicenseExpirationDate.MinimumWidth = 6;
		this.vLicenseExpirationDate.Name = "vLicenseExpirationDate";
		this.vLicenseExpirationDate.ReadOnly = true;
		this.vLicenseExpirationDate.Width = 150;
		this.vLicenseFeatures.HeaderText = "Features";
		this.vLicenseFeatures.MinimumWidth = 6;
		this.vLicenseFeatures.Name = "vLicenseFeatures";
		this.vLicenseFeatures.ReadOnly = true;
		this.vLicenseFeatures.Width = 125;
		this.vLicenseVISDKServer.HeaderText = "VI SDK Server";
		this.vLicenseVISDKServer.MinimumWidth = 6;
		this.vLicenseVISDKServer.Name = "vLicenseVISDKServer";
		this.vLicenseVISDKServer.ReadOnly = true;
		this.vLicenseVISDKServer.ToolTipText = "VI SDK Server, where RVTools gets it's information from";
		this.vLicenseVISDKServer.Width = 150;
		this.vLicenseInstanceUUID.HeaderText = "VI SDK UUID";
		this.vLicenseInstanceUUID.MinimumWidth = 6;
		this.vLicenseInstanceUUID.Name = "vLicenseInstanceUUID";
		this.vLicenseInstanceUUID.ReadOnly = true;
		this.vLicenseInstanceUUID.ToolTipText = "A globally unique identifier associated with this service instance";
		this.vLicenseInstanceUUID.Width = 150;
		this.tabvHealth.Controls.Add(this.panel15);
		this.tabvHealth.Location = new System.Drawing.Point(4, 25);
		this.tabvHealth.Margin = new System.Windows.Forms.Padding(4);
		this.tabvHealth.Name = "tabvHealth";
		this.tabvHealth.Size = new System.Drawing.Size(1725, 391);
		this.tabvHealth.TabIndex = 20;
		this.tabvHealth.Text = "vHealth";
		this.tabvHealth.ToolTipText = "Health Checks";
		this.tabvHealth.UseVisualStyleBackColor = true;
		this.panel15.Controls.Add(this.dataGridvHealth);
		this.panel15.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel15.Location = new System.Drawing.Point(0, 0);
		this.panel15.Margin = new System.Windows.Forms.Padding(4);
		this.panel15.Name = "panel15";
		this.panel15.Padding = new System.Windows.Forms.Padding(20, 18, 20, 18);
		this.panel15.Size = new System.Drawing.Size(1725, 391);
		this.panel15.TabIndex = 5;
		this.dataGridvHealth.AllowUserToAddRows = false;
		this.dataGridvHealth.AllowUserToDeleteRows = false;
		this.dataGridvHealth.AllowUserToOrderColumns = true;
		this.dataGridvHealth.AllowUserToResizeRows = false;
		dataGridViewCellStyle5.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
		this.dataGridvHealth.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle5;
		this.dataGridvHealth.BackgroundColor = System.Drawing.SystemColors.Window;
		this.dataGridvHealth.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
		this.dataGridvHealth.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGridvHealth.Columns.AddRange(this.vHealthName, this.vHealthMessage, this.vHealthMessageType, this.vHealthVISDKServer, this.vHealthInstanceUUID);
		this.dataGridvHealth.Dock = System.Windows.Forms.DockStyle.Fill;
		this.dataGridvHealth.Location = new System.Drawing.Point(20, 18);
		this.dataGridvHealth.Margin = new System.Windows.Forms.Padding(4);
		this.dataGridvHealth.Name = "dataGridvHealth";
		this.dataGridvHealth.RowHeadersVisible = false;
		this.dataGridvHealth.RowHeadersWidth = 51;
		this.dataGridvHealth.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this.dataGridvHealth.Size = new System.Drawing.Size(1685, 355);
		this.dataGridvHealth.TabIndex = 2;
		this.vHealthName.Frozen = true;
		this.vHealthName.HeaderText = "Name";
		this.vHealthName.MinimumWidth = 6;
		this.vHealthName.Name = "vHealthName";
		this.vHealthName.ReadOnly = true;
		this.vHealthName.ToolTipText = "Name";
		this.vHealthName.Width = 250;
		this.vHealthMessage.HeaderText = "Message";
		this.vHealthMessage.MinimumWidth = 6;
		this.vHealthMessage.Name = "vHealthMessage";
		this.vHealthMessage.ReadOnly = true;
		this.vHealthMessage.ToolTipText = "Health Check Message";
		this.vHealthMessage.Width = 550;
		this.vHealthMessageType.HeaderText = "Message type";
		this.vHealthMessageType.MinimumWidth = 6;
		this.vHealthMessageType.Name = "vHealthMessageType";
		this.vHealthMessageType.ReadOnly = true;
		this.vHealthMessageType.ToolTipText = "Message type";
		this.vHealthMessageType.Width = 122;
		this.vHealthVISDKServer.HeaderText = "VI SDK Server";
		this.vHealthVISDKServer.MinimumWidth = 6;
		this.vHealthVISDKServer.Name = "vHealthVISDKServer";
		this.vHealthVISDKServer.ReadOnly = true;
		this.vHealthVISDKServer.ToolTipText = "VI SDK Server, where RVTools gets it's information from";
		this.vHealthVISDKServer.Width = 150;
		this.vHealthInstanceUUID.HeaderText = "VI SDK UUID";
		this.vHealthInstanceUUID.MinimumWidth = 6;
		this.vHealthInstanceUUID.Name = "vHealthInstanceUUID";
		this.vHealthInstanceUUID.ReadOnly = true;
		this.vHealthInstanceUUID.ToolTipText = "A globally unique identifier associated with this service instance";
		this.vHealthInstanceUUID.Width = 150;
		this.tabvMultiPath.Controls.Add(this.panel26);
		this.tabvMultiPath.Location = new System.Drawing.Point(4, 25);
		this.tabvMultiPath.Margin = new System.Windows.Forms.Padding(4);
		this.tabvMultiPath.Name = "tabvMultiPath";
		this.tabvMultiPath.Size = new System.Drawing.Size(1725, 391);
		this.tabvMultiPath.TabIndex = 31;
		this.tabvMultiPath.Text = "vMultiPath";
		this.tabvMultiPath.ToolTipText = "Storage multi path information";
		this.tabvMultiPath.UseVisualStyleBackColor = true;
		this.panel26.Controls.Add(this.dataGridvMultiPath);
		this.panel26.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel26.Location = new System.Drawing.Point(0, 0);
		this.panel26.Margin = new System.Windows.Forms.Padding(4);
		this.panel26.Name = "panel26";
		this.panel26.Padding = new System.Windows.Forms.Padding(20, 18, 20, 18);
		this.panel26.Size = new System.Drawing.Size(1725, 391);
		this.panel26.TabIndex = 4;
		this.dataGridvMultiPath.AllowUserToAddRows = false;
		this.dataGridvMultiPath.AllowUserToDeleteRows = false;
		this.dataGridvMultiPath.AllowUserToOrderColumns = true;
		this.dataGridvMultiPath.AllowUserToResizeRows = false;
		dataGridViewCellStyle6.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
		this.dataGridvMultiPath.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle6;
		this.dataGridvMultiPath.BackgroundColor = System.Drawing.SystemColors.Window;
		this.dataGridvMultiPath.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
		this.dataGridvMultiPath.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGridvMultiPath.Columns.AddRange(this.vMultiPathHost, this.vMultiPathCluster, this.vMultiPathDataCenter, this.vMultiPathDatastoreName, this.vMultiPathDiskName, this.vMultiPathDisplayName, this.vMultiPathPolicy, this.vMultiPathOperationalState, this.vMultiPathPath1, this.vMultiPathPath1State, this.vMultiPathPath2, this.vMultiPathPath2State, this.vMultiPathPath3, this.vMultiPathPath3State, this.vMultiPathPath4, this.vMultiPathPath4State, this.vMultiPathPath5, this.vMultiPathPath5State, this.vMultiPathPath6, this.vMultiPathPath6State, this.vMultiPathPath7, this.vMultiPathPath7State, this.vMultiPathPath8, this.vMultiPathPath8State, this.vMultiPathVStorageSupport, this.vMultiPathQueueDepth, this.vMultiPathVendor, this.vMultiPathModel, this.vMultiPathRevision, this.vMultiPathSCSILevel, this.vMultiPathSerialNumber, this.vMultiPathUUID, this.vMultiPathObjectID, this.vMultiPathVISDKServer, this.vMultiPathInstanceUUID);
		this.dataGridvMultiPath.Dock = System.Windows.Forms.DockStyle.Fill;
		this.dataGridvMultiPath.Location = new System.Drawing.Point(20, 18);
		this.dataGridvMultiPath.Margin = new System.Windows.Forms.Padding(4);
		this.dataGridvMultiPath.Name = "dataGridvMultiPath";
		this.dataGridvMultiPath.RowHeadersVisible = false;
		this.dataGridvMultiPath.RowHeadersWidth = 51;
		this.dataGridvMultiPath.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this.dataGridvMultiPath.Size = new System.Drawing.Size(1685, 355);
		this.dataGridvMultiPath.TabIndex = 2;
		this.vMultiPathHost.Frozen = true;
		this.vMultiPathHost.HeaderText = "Host";
		this.vMultiPathHost.MinimumWidth = 6;
		this.vMultiPathHost.Name = "vMultiPathHost";
		this.vMultiPathHost.ReadOnly = true;
		this.vMultiPathHost.ToolTipText = "The host on which the vm is running";
		this.vMultiPathHost.Width = 125;
		this.vMultiPathCluster.Frozen = true;
		this.vMultiPathCluster.HeaderText = "Cluster";
		this.vMultiPathCluster.MinimumWidth = 6;
		this.vMultiPathCluster.Name = "vMultiPathCluster";
		this.vMultiPathCluster.ReadOnly = true;
		this.vMultiPathCluster.ToolTipText = "Cluster name";
		this.vMultiPathCluster.Width = 125;
		this.vMultiPathDataCenter.Frozen = true;
		this.vMultiPathDataCenter.HeaderText = "Datacenter";
		this.vMultiPathDataCenter.MinimumWidth = 6;
		this.vMultiPathDataCenter.Name = "vMultiPathDataCenter";
		this.vMultiPathDataCenter.ReadOnly = true;
		this.vMultiPathDataCenter.ToolTipText = "Datacenter name";
		this.vMultiPathDataCenter.Width = 125;
		this.vMultiPathDatastoreName.Frozen = true;
		this.vMultiPathDatastoreName.HeaderText = "Datastore";
		this.vMultiPathDatastoreName.MinimumWidth = 6;
		this.vMultiPathDatastoreName.Name = "vMultiPathDatastoreName";
		this.vMultiPathDatastoreName.ReadOnly = true;
		this.vMultiPathDatastoreName.ToolTipText = "Datastore name";
		this.vMultiPathDatastoreName.Width = 125;
		this.vMultiPathDiskName.HeaderText = "Disk";
		this.vMultiPathDiskName.MinimumWidth = 6;
		this.vMultiPathDiskName.Name = "vMultiPathDiskName";
		this.vMultiPathDiskName.ReadOnly = true;
		this.vMultiPathDiskName.Width = 125;
		this.vMultiPathDisplayName.HeaderText = "Display name";
		this.vMultiPathDisplayName.MinimumWidth = 6;
		this.vMultiPathDisplayName.Name = "vMultiPathDisplayName";
		this.vMultiPathDisplayName.ReadOnly = true;
		this.vMultiPathDisplayName.ToolTipText = "User configurable display name of the SCSI logical unit";
		this.vMultiPathDisplayName.Width = 125;
		this.vMultiPathPolicy.HeaderText = "Policy";
		this.vMultiPathPolicy.MinimumWidth = 6;
		this.vMultiPathPolicy.Name = "vMultiPathPolicy";
		this.vMultiPathPolicy.ReadOnly = true;
		this.vMultiPathPolicy.Width = 125;
		this.vMultiPathOperationalState.HeaderText = "Oper. State";
		this.vMultiPathOperationalState.MinimumWidth = 6;
		this.vMultiPathOperationalState.Name = "vMultiPathOperationalState";
		this.vMultiPathOperationalState.ReadOnly = true;
		this.vMultiPathOperationalState.ToolTipText = "The operational states of the LUN.";
		this.vMultiPathOperationalState.Width = 125;
		this.vMultiPathPath1.HeaderText = "Path 1";
		this.vMultiPathPath1.MinimumWidth = 6;
		this.vMultiPathPath1.Name = "vMultiPathPath1";
		this.vMultiPathPath1.ReadOnly = true;
		this.vMultiPathPath1.Width = 125;
		this.vMultiPathPath1State.HeaderText = "Path 1 state";
		this.vMultiPathPath1State.MinimumWidth = 6;
		this.vMultiPathPath1State.Name = "vMultiPathPath1State";
		this.vMultiPathPath1State.ReadOnly = true;
		this.vMultiPathPath1State.Width = 125;
		this.vMultiPathPath2.HeaderText = "Path 2";
		this.vMultiPathPath2.MinimumWidth = 6;
		this.vMultiPathPath2.Name = "vMultiPathPath2";
		this.vMultiPathPath2.ReadOnly = true;
		this.vMultiPathPath2.Width = 125;
		this.vMultiPathPath2State.HeaderText = "Path 2 state";
		this.vMultiPathPath2State.MinimumWidth = 6;
		this.vMultiPathPath2State.Name = "vMultiPathPath2State";
		this.vMultiPathPath2State.ReadOnly = true;
		this.vMultiPathPath2State.Width = 125;
		this.vMultiPathPath3.HeaderText = "Path 3";
		this.vMultiPathPath3.MinimumWidth = 6;
		this.vMultiPathPath3.Name = "vMultiPathPath3";
		this.vMultiPathPath3.ReadOnly = true;
		this.vMultiPathPath3.Width = 125;
		this.vMultiPathPath3State.HeaderText = "Path 3 state";
		this.vMultiPathPath3State.MinimumWidth = 6;
		this.vMultiPathPath3State.Name = "vMultiPathPath3State";
		this.vMultiPathPath3State.ReadOnly = true;
		this.vMultiPathPath3State.Width = 125;
		this.vMultiPathPath4.HeaderText = "Path 4";
		this.vMultiPathPath4.MinimumWidth = 6;
		this.vMultiPathPath4.Name = "vMultiPathPath4";
		this.vMultiPathPath4.ReadOnly = true;
		this.vMultiPathPath4.Width = 125;
		this.vMultiPathPath4State.HeaderText = "Path 4 state";
		this.vMultiPathPath4State.MinimumWidth = 6;
		this.vMultiPathPath4State.Name = "vMultiPathPath4State";
		this.vMultiPathPath4State.ReadOnly = true;
		this.vMultiPathPath4State.Width = 125;
		this.vMultiPathPath5.HeaderText = "Path 5";
		this.vMultiPathPath5.MinimumWidth = 6;
		this.vMultiPathPath5.Name = "vMultiPathPath5";
		this.vMultiPathPath5.ReadOnly = true;
		this.vMultiPathPath5.Width = 125;
		this.vMultiPathPath5State.HeaderText = "Path 5 state";
		this.vMultiPathPath5State.MinimumWidth = 6;
		this.vMultiPathPath5State.Name = "vMultiPathPath5State";
		this.vMultiPathPath5State.ReadOnly = true;
		this.vMultiPathPath5State.Width = 125;
		this.vMultiPathPath6.HeaderText = "Path 6";
		this.vMultiPathPath6.MinimumWidth = 6;
		this.vMultiPathPath6.Name = "vMultiPathPath6";
		this.vMultiPathPath6.ReadOnly = true;
		this.vMultiPathPath6.Width = 125;
		this.vMultiPathPath6State.HeaderText = "Path 6 state";
		this.vMultiPathPath6State.MinimumWidth = 6;
		this.vMultiPathPath6State.Name = "vMultiPathPath6State";
		this.vMultiPathPath6State.ReadOnly = true;
		this.vMultiPathPath6State.Width = 125;
		this.vMultiPathPath7.HeaderText = "Path 7";
		this.vMultiPathPath7.MinimumWidth = 6;
		this.vMultiPathPath7.Name = "vMultiPathPath7";
		this.vMultiPathPath7.ReadOnly = true;
		this.vMultiPathPath7.Width = 125;
		this.vMultiPathPath7State.HeaderText = "Path 7 state";
		this.vMultiPathPath7State.MinimumWidth = 6;
		this.vMultiPathPath7State.Name = "vMultiPathPath7State";
		this.vMultiPathPath7State.ReadOnly = true;
		this.vMultiPathPath7State.Width = 125;
		this.vMultiPathPath8.HeaderText = "Path 8";
		this.vMultiPathPath8.MinimumWidth = 6;
		this.vMultiPathPath8.Name = "vMultiPathPath8";
		this.vMultiPathPath8.ReadOnly = true;
		this.vMultiPathPath8.Width = 125;
		this.vMultiPathPath8State.HeaderText = "Path 8 state";
		this.vMultiPathPath8State.MinimumWidth = 6;
		this.vMultiPathPath8State.Name = "vMultiPathPath8State";
		this.vMultiPathPath8State.ReadOnly = true;
		this.vMultiPathPath8State.Width = 125;
		this.vMultiPathVStorageSupport.HeaderText = "vStorage";
		this.vMultiPathVStorageSupport.MinimumWidth = 6;
		this.vMultiPathVStorageSupport.Name = "vMultiPathVStorageSupport";
		this.vMultiPathVStorageSupport.ReadOnly = true;
		this.vMultiPathVStorageSupport.ToolTipText = "vStorage hardware acceleration support status";
		this.vMultiPathVStorageSupport.Width = 125;
		this.vMultiPathQueueDepth.HeaderText = "Queue depth";
		this.vMultiPathQueueDepth.MinimumWidth = 6;
		this.vMultiPathQueueDepth.Name = "vMultiPathQueueDepth";
		this.vMultiPathQueueDepth.ReadOnly = true;
		this.vMultiPathQueueDepth.Width = 120;
		this.vMultiPathVendor.HeaderText = "Vendor";
		this.vMultiPathVendor.MinimumWidth = 6;
		this.vMultiPathVendor.Name = "vMultiPathVendor";
		this.vMultiPathVendor.ReadOnly = true;
		this.vMultiPathVendor.ToolTipText = "The vendor of the SCSI device.";
		this.vMultiPathVendor.Width = 125;
		this.vMultiPathModel.HeaderText = "Model";
		this.vMultiPathModel.MinimumWidth = 6;
		this.vMultiPathModel.Name = "vMultiPathModel";
		this.vMultiPathModel.ReadOnly = true;
		this.vMultiPathModel.ToolTipText = "The model number of the SCSI device";
		this.vMultiPathModel.Width = 125;
		this.vMultiPathRevision.HeaderText = "Revision";
		this.vMultiPathRevision.MinimumWidth = 6;
		this.vMultiPathRevision.Name = "vMultiPathRevision";
		this.vMultiPathRevision.ReadOnly = true;
		this.vMultiPathRevision.ToolTipText = "The revision of the SCSI device";
		this.vMultiPathRevision.Width = 125;
		this.vMultiPathSCSILevel.HeaderText = "Level";
		this.vMultiPathSCSILevel.MinimumWidth = 6;
		this.vMultiPathSCSILevel.Name = "vMultiPathSCSILevel";
		this.vMultiPathSCSILevel.ReadOnly = true;
		this.vMultiPathSCSILevel.ToolTipText = "The SCSI level of the SCSI device";
		this.vMultiPathSCSILevel.Width = 125;
		this.vMultiPathSerialNumber.HeaderText = "Serial #";
		this.vMultiPathSerialNumber.MinimumWidth = 6;
		this.vMultiPathSerialNumber.Name = "vMultiPathSerialNumber";
		this.vMultiPathSerialNumber.ReadOnly = true;
		this.vMultiPathSerialNumber.Width = 125;
		this.vMultiPathUUID.HeaderText = "UUID";
		this.vMultiPathUUID.MinimumWidth = 6;
		this.vMultiPathUUID.Name = "vMultiPathUUID";
		this.vMultiPathUUID.ReadOnly = true;
		this.vMultiPathUUID.ToolTipText = "Universally unique identifier for the LUN used to identify ScsiLun across multiple servers";
		this.vMultiPathUUID.Width = 125;
		this.vMultiPathObjectID.HeaderText = "Object ID";
		this.vMultiPathObjectID.MinimumWidth = 6;
		this.vMultiPathObjectID.Name = "vMultiPathObjectID";
		this.vMultiPathObjectID.ReadOnly = true;
		this.vMultiPathObjectID.Width = 125;
		this.vMultiPathVISDKServer.HeaderText = "VI SDK Server";
		this.vMultiPathVISDKServer.MinimumWidth = 6;
		this.vMultiPathVISDKServer.Name = "vMultiPathVISDKServer";
		this.vMultiPathVISDKServer.ReadOnly = true;
		this.vMultiPathVISDKServer.ToolTipText = "VI SDK Server, where RVTools gets it's information from";
		this.vMultiPathVISDKServer.Width = 150;
		this.vMultiPathInstanceUUID.HeaderText = "VI SDK UUID";
		this.vMultiPathInstanceUUID.MinimumWidth = 6;
		this.vMultiPathInstanceUUID.Name = "vMultiPathInstanceUUID";
		this.vMultiPathInstanceUUID.ReadOnly = true;
		this.vMultiPathInstanceUUID.ToolTipText = "A globally unique identifier associated with this service instance";
		this.vMultiPathInstanceUUID.Width = 125;
		this.tabvDatastore.Controls.Add(this.panel3);
		this.tabvDatastore.Location = new System.Drawing.Point(4, 25);
		this.tabvDatastore.Margin = new System.Windows.Forms.Padding(4);
		this.tabvDatastore.Name = "tabvDatastore";
		this.tabvDatastore.Size = new System.Drawing.Size(1725, 391);
		this.tabvDatastore.TabIndex = 18;
		this.tabvDatastore.Text = "vDatastore";
		this.tabvDatastore.ToolTipText = "Datastore Information";
		this.tabvDatastore.UseVisualStyleBackColor = true;
		this.panel3.Controls.Add(this.dataGridvDatastore);
		this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel3.Location = new System.Drawing.Point(0, 0);
		this.panel3.Margin = new System.Windows.Forms.Padding(4);
		this.panel3.Name = "panel3";
		this.panel3.Padding = new System.Windows.Forms.Padding(20, 18, 20, 18);
		this.panel3.Size = new System.Drawing.Size(1725, 391);
		this.panel3.TabIndex = 4;
		this.dataGridvDatastore.AllowUserToAddRows = false;
		this.dataGridvDatastore.AllowUserToDeleteRows = false;
		this.dataGridvDatastore.AllowUserToOrderColumns = true;
		this.dataGridvDatastore.AllowUserToResizeRows = false;
		dataGridViewCellStyle7.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
		this.dataGridvDatastore.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle7;
		this.dataGridvDatastore.BackgroundColor = System.Drawing.SystemColors.Window;
		this.dataGridvDatastore.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
		this.dataGridvDatastore.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGridvDatastore.Columns.AddRange(this.vDatastoreName, this.vDatastoreConfigStatus, this.vDatastoreAddress, this.vDatastoreAccessible, this.vDatastoreType, this.vDatastoreVMsTotal, this.vDatastoreVMs, this.vDatastoreCapacity, this.vDatastoreProvisioned, this.vDatastoreInUse, this.vDatastoreFreeSpace, this.vDataStoreFreePercentage, this.vDatastoreSIOCFlag, this.vDatastoreSIOCThreshold, this.vDatastoreNumberofHosts, this.vDatastoreHosts, this.vDatastoreClusterName, this.vDatastoreClusterCapacity, this.vDatastoreClusterFreeSpace, this.vDatastoreBlockSizeMiB, this.vDatastoreMaxBlocks, this.vDatastoreExtents, this.vDatastoreMajorVersion, this.vDatastoreVersion, this.vDatastoreVMFSUpgradable, this.vDatastoreMultipleHostAccess, this.vDatastoreURL, this.vDatastoreObjectID, this.vDatastoreVISDKServer, this.vDatastoreInstanceUUID);
		this.dataGridvDatastore.Dock = System.Windows.Forms.DockStyle.Fill;
		this.dataGridvDatastore.Location = new System.Drawing.Point(20, 18);
		this.dataGridvDatastore.Margin = new System.Windows.Forms.Padding(4);
		this.dataGridvDatastore.Name = "dataGridvDatastore";
		this.dataGridvDatastore.RowHeadersVisible = false;
		this.dataGridvDatastore.RowHeadersWidth = 51;
		this.dataGridvDatastore.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this.dataGridvDatastore.Size = new System.Drawing.Size(1685, 355);
		this.dataGridvDatastore.TabIndex = 2;
		this.vDatastoreName.Frozen = true;
		this.vDatastoreName.HeaderText = "Name";
		this.vDatastoreName.MinimumWidth = 6;
		this.vDatastoreName.Name = "vDatastoreName";
		this.vDatastoreName.ReadOnly = true;
		this.vDatastoreName.ToolTipText = "Datastore name";
		this.vDatastoreName.Width = 125;
		this.vDatastoreConfigStatus.HeaderText = "Config status";
		this.vDatastoreConfigStatus.MinimumWidth = 6;
		this.vDatastoreConfigStatus.Name = "vDatastoreConfigStatus";
		this.vDatastoreConfigStatus.ReadOnly = true;
		this.vDatastoreConfigStatus.ToolTipText = "Config status";
		this.vDatastoreConfigStatus.Width = 125;
		this.vDatastoreAddress.HeaderText = "Address";
		this.vDatastoreAddress.MinimumWidth = 6;
		this.vDatastoreAddress.Name = "vDatastoreAddress";
		this.vDatastoreAddress.ReadOnly = true;
		this.vDatastoreAddress.ToolTipText = "The full device's address";
		this.vDatastoreAddress.Width = 125;
		this.vDatastoreAccessible.HeaderText = "Accessible";
		this.vDatastoreAccessible.MinimumWidth = 6;
		this.vDatastoreAccessible.Name = "vDatastoreAccessible";
		this.vDatastoreAccessible.ReadOnly = true;
		this.vDatastoreAccessible.ToolTipText = "The connectivity status of this datastore";
		this.vDatastoreAccessible.Width = 125;
		this.vDatastoreType.HeaderText = "Type";
		this.vDatastoreType.MinimumWidth = 6;
		this.vDatastoreType.Name = "vDatastoreType";
		this.vDatastoreType.ReadOnly = true;
		this.vDatastoreType.ToolTipText = "Type of file system volume";
		this.vDatastoreType.Width = 125;
		dataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle8.Format = "N0";
		dataGridViewCellStyle8.NullValue = null;
		this.vDatastoreVMsTotal.DefaultCellStyle = dataGridViewCellStyle8;
		this.vDatastoreVMsTotal.HeaderText = "# VMs total";
		this.vDatastoreVMsTotal.MinimumWidth = 6;
		this.vDatastoreVMsTotal.Name = "vDatastoreVMsTotal";
		this.vDatastoreVMsTotal.ReadOnly = true;
		this.vDatastoreVMsTotal.ToolTipText = "Total number of VMs on this datastore";
		this.vDatastoreVMsTotal.Width = 125;
		dataGridViewCellStyle9.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle9.Format = "N0";
		dataGridViewCellStyle9.NullValue = null;
		this.vDatastoreVMs.DefaultCellStyle = dataGridViewCellStyle9;
		this.vDatastoreVMs.HeaderText = "# VMs";
		this.vDatastoreVMs.MinimumWidth = 6;
		this.vDatastoreVMs.Name = "vDatastoreVMs";
		this.vDatastoreVMs.ReadOnly = true;
		this.vDatastoreVMs.ToolTipText = "Number of running VMs on this datastore";
		this.vDatastoreVMs.Width = 125;
		dataGridViewCellStyle10.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle10.Format = "N0";
		dataGridViewCellStyle10.NullValue = null;
		this.vDatastoreCapacity.DefaultCellStyle = dataGridViewCellStyle10;
		this.vDatastoreCapacity.HeaderText = "Capacity MiB";
		this.vDatastoreCapacity.MinimumWidth = 6;
		this.vDatastoreCapacity.Name = "vDatastoreCapacity";
		this.vDatastoreCapacity.ReadOnly = true;
		this.vDatastoreCapacity.ToolTipText = "Maximum capacity of this datastore";
		this.vDatastoreCapacity.Width = 125;
		dataGridViewCellStyle11.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle11.Format = "N0";
		dataGridViewCellStyle11.NullValue = null;
		this.vDatastoreProvisioned.DefaultCellStyle = dataGridViewCellStyle11;
		this.vDatastoreProvisioned.HeaderText = "Provisioned MiB";
		this.vDatastoreProvisioned.MinimumWidth = 6;
		this.vDatastoreProvisioned.Name = "vDatastoreProvisioned";
		this.vDatastoreProvisioned.ReadOnly = true;
		this.vDatastoreProvisioned.ToolTipText = "Total storage space, in MiB, potentially used.";
		this.vDatastoreProvisioned.Width = 150;
		dataGridViewCellStyle12.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle12.Format = "N0";
		dataGridViewCellStyle12.NullValue = null;
		this.vDatastoreInUse.DefaultCellStyle = dataGridViewCellStyle12;
		this.vDatastoreInUse.HeaderText = "In Use MiB";
		this.vDatastoreInUse.MinimumWidth = 6;
		this.vDatastoreInUse.Name = "vDatastoreInUse";
		this.vDatastoreInUse.ReadOnly = true;
		this.vDatastoreInUse.ToolTipText = "Total Storage space, in MiB, on this datastore that is actually being used.";
		this.vDatastoreInUse.Width = 125;
		dataGridViewCellStyle13.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle13.Format = "N0";
		dataGridViewCellStyle13.NullValue = null;
		this.vDatastoreFreeSpace.DefaultCellStyle = dataGridViewCellStyle13;
		this.vDatastoreFreeSpace.HeaderText = "Free MiB";
		this.vDatastoreFreeSpace.MinimumWidth = 6;
		this.vDatastoreFreeSpace.Name = "vDatastoreFreeSpace";
		this.vDatastoreFreeSpace.ReadOnly = true;
		this.vDatastoreFreeSpace.ToolTipText = "Available space of this datastore";
		this.vDatastoreFreeSpace.Width = 125;
		dataGridViewCellStyle14.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle14.Format = "N0";
		dataGridViewCellStyle14.NullValue = null;
		this.vDataStoreFreePercentage.DefaultCellStyle = dataGridViewCellStyle14;
		this.vDataStoreFreePercentage.HeaderText = "Free %";
		this.vDataStoreFreePercentage.MinimumWidth = 6;
		this.vDataStoreFreePercentage.Name = "vDataStoreFreePercentage";
		this.vDataStoreFreePercentage.ReadOnly = true;
		this.vDataStoreFreePercentage.ToolTipText = "Available space of this datastore in %";
		this.vDataStoreFreePercentage.Width = 125;
		this.vDatastoreSIOCFlag.HeaderText = "SIOC enabled";
		this.vDatastoreSIOCFlag.MinimumWidth = 6;
		this.vDatastoreSIOCFlag.Name = "vDatastoreSIOCFlag";
		this.vDatastoreSIOCFlag.ReadOnly = true;
		this.vDatastoreSIOCFlag.ToolTipText = "Flag indicating whether or not the service is enabled. ";
		this.vDatastoreSIOCFlag.Width = 125;
		dataGridViewCellStyle15.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle15.Format = "N0";
		dataGridViewCellStyle15.NullValue = null;
		this.vDatastoreSIOCThreshold.DefaultCellStyle = dataGridViewCellStyle15;
		this.vDatastoreSIOCThreshold.HeaderText = "SIOC Threshold";
		this.vDatastoreSIOCThreshold.MinimumWidth = 6;
		this.vDatastoreSIOCThreshold.Name = "vDatastoreSIOCThreshold";
		this.vDatastoreSIOCThreshold.ReadOnly = true;
		this.vDatastoreSIOCThreshold.ToolTipText = "The latency beyond which the storage array is considered congested. ";
		this.vDatastoreSIOCThreshold.Width = 150;
		dataGridViewCellStyle16.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle16.Format = "N0";
		dataGridViewCellStyle16.NullValue = null;
		this.vDatastoreNumberofHosts.DefaultCellStyle = dataGridViewCellStyle16;
		this.vDatastoreNumberofHosts.HeaderText = "# Hosts";
		this.vDatastoreNumberofHosts.MinimumWidth = 6;
		this.vDatastoreNumberofHosts.Name = "vDatastoreNumberofHosts";
		this.vDatastoreNumberofHosts.ReadOnly = true;
		this.vDatastoreNumberofHosts.ToolTipText = "Number of Hosts";
		this.vDatastoreNumberofHosts.Width = 125;
		this.vDatastoreHosts.HeaderText = "Hosts";
		this.vDatastoreHosts.MinimumWidth = 6;
		this.vDatastoreHosts.Name = "vDatastoreHosts";
		this.vDatastoreHosts.ReadOnly = true;
		this.vDatastoreHosts.ToolTipText = "Host names";
		this.vDatastoreHosts.Width = 125;
		this.vDatastoreClusterName.HeaderText = "Cluster name";
		this.vDatastoreClusterName.MinimumWidth = 6;
		this.vDatastoreClusterName.Name = "vDatastoreClusterName";
		this.vDatastoreClusterName.ReadOnly = true;
		this.vDatastoreClusterName.ToolTipText = "Datastore cluster name";
		this.vDatastoreClusterName.Width = 125;
		dataGridViewCellStyle17.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle17.Format = "N0";
		dataGridViewCellStyle17.NullValue = null;
		this.vDatastoreClusterCapacity.DefaultCellStyle = dataGridViewCellStyle17;
		this.vDatastoreClusterCapacity.HeaderText = "Cluster capacity MiB";
		this.vDatastoreClusterCapacity.MinimumWidth = 6;
		this.vDatastoreClusterCapacity.Name = "vDatastoreClusterCapacity";
		this.vDatastoreClusterCapacity.ReadOnly = true;
		this.vDatastoreClusterCapacity.ToolTipText = "Total capacity of this storage pod, in MiB";
		this.vDatastoreClusterCapacity.Width = 175;
		dataGridViewCellStyle18.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle18.Format = "N0";
		dataGridViewCellStyle18.NullValue = null;
		this.vDatastoreClusterFreeSpace.DefaultCellStyle = dataGridViewCellStyle18;
		this.vDatastoreClusterFreeSpace.HeaderText = "Cluster free space MiB";
		this.vDatastoreClusterFreeSpace.MinimumWidth = 6;
		this.vDatastoreClusterFreeSpace.Name = "vDatastoreClusterFreeSpace";
		this.vDatastoreClusterFreeSpace.ReadOnly = true;
		this.vDatastoreClusterFreeSpace.ToolTipText = "Total free space on this storage pod, in MiB.";
		this.vDatastoreClusterFreeSpace.Width = 175;
		dataGridViewCellStyle19.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle19.Format = "N0";
		dataGridViewCellStyle19.NullValue = null;
		this.vDatastoreBlockSizeMiB.DefaultCellStyle = dataGridViewCellStyle19;
		this.vDatastoreBlockSizeMiB.HeaderText = "Block size";
		this.vDatastoreBlockSizeMiB.MinimumWidth = 6;
		this.vDatastoreBlockSizeMiB.Name = "vDatastoreBlockSizeMiB";
		this.vDatastoreBlockSizeMiB.ReadOnly = true;
		this.vDatastoreBlockSizeMiB.ToolTipText = "VMFS block size";
		this.vDatastoreBlockSizeMiB.Width = 125;
		dataGridViewCellStyle20.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle20.Format = "N0";
		dataGridViewCellStyle20.NullValue = null;
		this.vDatastoreMaxBlocks.DefaultCellStyle = dataGridViewCellStyle20;
		this.vDatastoreMaxBlocks.HeaderText = "Max Blocks";
		this.vDatastoreMaxBlocks.MinimumWidth = 6;
		this.vDatastoreMaxBlocks.Name = "vDatastoreMaxBlocks";
		this.vDatastoreMaxBlocks.ReadOnly = true;
		this.vDatastoreMaxBlocks.ToolTipText = "VMFS maximum number of blocks.";
		this.vDatastoreMaxBlocks.Width = 125;
		dataGridViewCellStyle21.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle21.Format = "N0";
		dataGridViewCellStyle21.NullValue = null;
		this.vDatastoreExtents.DefaultCellStyle = dataGridViewCellStyle21;
		this.vDatastoreExtents.HeaderText = "# Extents";
		this.vDatastoreExtents.MinimumWidth = 6;
		this.vDatastoreExtents.Name = "vDatastoreExtents";
		this.vDatastoreExtents.ReadOnly = true;
		this.vDatastoreExtents.ToolTipText = "VMFS number of extents";
		this.vDatastoreExtents.Width = 125;
		dataGridViewCellStyle22.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle22.Format = "N0";
		dataGridViewCellStyle22.NullValue = null;
		this.vDatastoreMajorVersion.DefaultCellStyle = dataGridViewCellStyle22;
		this.vDatastoreMajorVersion.HeaderText = "Major Version";
		this.vDatastoreMajorVersion.MinimumWidth = 6;
		this.vDatastoreMajorVersion.Name = "vDatastoreMajorVersion";
		this.vDatastoreMajorVersion.ReadOnly = true;
		this.vDatastoreMajorVersion.ToolTipText = "Major version number of VMFS.";
		this.vDatastoreMajorVersion.Width = 125;
		dataGridViewCellStyle23.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		this.vDatastoreVersion.DefaultCellStyle = dataGridViewCellStyle23;
		this.vDatastoreVersion.HeaderText = "Version";
		this.vDatastoreVersion.MinimumWidth = 6;
		this.vDatastoreVersion.Name = "vDatastoreVersion";
		this.vDatastoreVersion.ReadOnly = true;
		this.vDatastoreVersion.ToolTipText = "VMFS version string";
		this.vDatastoreVersion.Width = 125;
		this.vDatastoreVMFSUpgradable.HeaderText = "VMFS Upgradeable";
		this.vDatastoreVMFSUpgradable.MinimumWidth = 6;
		this.vDatastoreVMFSUpgradable.Name = "vDatastoreVMFSUpgradable";
		this.vDatastoreVMFSUpgradable.ReadOnly = true;
		this.vDatastoreVMFSUpgradable.ToolTipText = "Can the filesystem be upgraded to a newer version?";
		this.vDatastoreVMFSUpgradable.Width = 200;
		this.vDatastoreMultipleHostAccess.HeaderText = "MHA";
		this.vDatastoreMultipleHostAccess.MinimumWidth = 6;
		this.vDatastoreMultipleHostAccess.Name = "vDatastoreMultipleHostAccess";
		this.vDatastoreMultipleHostAccess.ReadOnly = true;
		this.vDatastoreMultipleHostAccess.ToolTipText = "Multiple Host Access";
		this.vDatastoreMultipleHostAccess.Width = 125;
		this.vDatastoreURL.HeaderText = "URL";
		this.vDatastoreURL.MinimumWidth = 6;
		this.vDatastoreURL.Name = "vDatastoreURL";
		this.vDatastoreURL.ReadOnly = true;
		this.vDatastoreURL.ToolTipText = "The unique locator for the datastore";
		this.vDatastoreURL.Width = 125;
		this.vDatastoreObjectID.HeaderText = "Object ID";
		this.vDatastoreObjectID.MinimumWidth = 6;
		this.vDatastoreObjectID.Name = "vDatastoreObjectID";
		this.vDatastoreObjectID.ReadOnly = true;
		this.vDatastoreObjectID.ToolTipText = "Datastore object id";
		this.vDatastoreObjectID.Width = 125;
		this.vDatastoreVISDKServer.HeaderText = "VI SDK Server";
		this.vDatastoreVISDKServer.MinimumWidth = 6;
		this.vDatastoreVISDKServer.Name = "vDatastoreVISDKServer";
		this.vDatastoreVISDKServer.ReadOnly = true;
		this.vDatastoreVISDKServer.ToolTipText = "VI SDK Server, where RVTools gets it's information from";
		this.vDatastoreVISDKServer.Width = 150;
		this.vDatastoreInstanceUUID.HeaderText = "VI SDK UUID";
		this.vDatastoreInstanceUUID.MinimumWidth = 6;
		this.vDatastoreInstanceUUID.Name = "vDatastoreInstanceUUID";
		this.vDatastoreInstanceUUID.ReadOnly = true;
		this.vDatastoreInstanceUUID.ToolTipText = "A globally unique identifier associated with this service instance";
		this.vDatastoreInstanceUUID.Width = 125;
		this.tabvSC_VMK.Controls.Add(this.panel20);
		this.tabvSC_VMK.Location = new System.Drawing.Point(4, 25);
		this.tabvSC_VMK.Margin = new System.Windows.Forms.Padding(4);
		this.tabvSC_VMK.Name = "tabvSC_VMK";
		this.tabvSC_VMK.Size = new System.Drawing.Size(1725, 391);
		this.tabvSC_VMK.TabIndex = 25;
		this.tabvSC_VMK.Text = "vSC+VMK";
		this.tabvSC_VMK.ToolTipText = "Service console and VMKernel information";
		this.tabvSC_VMK.UseVisualStyleBackColor = true;
		this.panel20.Controls.Add(this.dataGridvSC_VMK);
		this.panel20.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel20.Location = new System.Drawing.Point(0, 0);
		this.panel20.Margin = new System.Windows.Forms.Padding(4);
		this.panel20.Name = "panel20";
		this.panel20.Padding = new System.Windows.Forms.Padding(20, 18, 20, 18);
		this.panel20.Size = new System.Drawing.Size(1725, 391);
		this.panel20.TabIndex = 6;
		this.dataGridvSC_VMK.AllowUserToAddRows = false;
		this.dataGridvSC_VMK.AllowUserToDeleteRows = false;
		this.dataGridvSC_VMK.AllowUserToOrderColumns = true;
		this.dataGridvSC_VMK.AllowUserToResizeRows = false;
		dataGridViewCellStyle24.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
		this.dataGridvSC_VMK.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle24;
		this.dataGridvSC_VMK.BackgroundColor = System.Drawing.SystemColors.Window;
		this.dataGridvSC_VMK.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
		this.dataGridvSC_VMK.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGridvSC_VMK.Columns.AddRange(this.vSC_VMKHost, this.vSC_VMKDatacenter, this.vSC_VMKCluster, this.vSC_VMKPortGroup, this.vSC_VMKDevice, this.vSC_VMKMAC, this.vSC_VMKDHCP, this.vSC_VMKIP, this.vSC_VMKIP6, this.vSC_VMKMask, this.vSC_VMKGateway, this.vSC_VMKGateway6, this.vSC_VMKMTU, this.vSC_VMKVISDKServer, this.vSC_VMKInstanceUUID);
		this.dataGridvSC_VMK.Dock = System.Windows.Forms.DockStyle.Fill;
		this.dataGridvSC_VMK.Location = new System.Drawing.Point(20, 18);
		this.dataGridvSC_VMK.Margin = new System.Windows.Forms.Padding(4);
		this.dataGridvSC_VMK.Name = "dataGridvSC_VMK";
		this.dataGridvSC_VMK.RowHeadersVisible = false;
		this.dataGridvSC_VMK.RowHeadersWidth = 51;
		this.dataGridvSC_VMK.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this.dataGridvSC_VMK.Size = new System.Drawing.Size(1685, 355);
		this.dataGridvSC_VMK.TabIndex = 3;
		this.vSC_VMKHost.Frozen = true;
		this.vSC_VMKHost.HeaderText = "Host";
		this.vSC_VMKHost.MinimumWidth = 6;
		this.vSC_VMKHost.Name = "vSC_VMKHost";
		this.vSC_VMKHost.ReadOnly = true;
		this.vSC_VMKHost.ToolTipText = "Host name";
		this.vSC_VMKHost.Width = 125;
		this.vSC_VMKDatacenter.HeaderText = "Datacenter";
		this.vSC_VMKDatacenter.MinimumWidth = 6;
		this.vSC_VMKDatacenter.Name = "vSC_VMKDatacenter";
		this.vSC_VMKDatacenter.ReadOnly = true;
		this.vSC_VMKDatacenter.ToolTipText = "Datacenter name";
		this.vSC_VMKDatacenter.Width = 125;
		this.vSC_VMKCluster.HeaderText = "Cluster";
		this.vSC_VMKCluster.MinimumWidth = 6;
		this.vSC_VMKCluster.Name = "vSC_VMKCluster";
		this.vSC_VMKCluster.ReadOnly = true;
		this.vSC_VMKCluster.ToolTipText = "Cluster name";
		this.vSC_VMKCluster.Width = 125;
		this.vSC_VMKPortGroup.HeaderText = "Port Group";
		this.vSC_VMKPortGroup.MinimumWidth = 6;
		this.vSC_VMKPortGroup.Name = "vSC_VMKPortGroup";
		this.vSC_VMKPortGroup.ReadOnly = true;
		this.vSC_VMKPortGroup.ToolTipText = "The name of the port group";
		this.vSC_VMKPortGroup.Width = 125;
		this.vSC_VMKDevice.HeaderText = "Device";
		this.vSC_VMKDevice.MinimumWidth = 6;
		this.vSC_VMKDevice.Name = "vSC_VMKDevice";
		this.vSC_VMKDevice.ReadOnly = true;
		this.vSC_VMKDevice.ToolTipText = "Device name";
		this.vSC_VMKDevice.Width = 125;
		this.vSC_VMKMAC.HeaderText = "Mac Address";
		this.vSC_VMKMAC.MinimumWidth = 6;
		this.vSC_VMKMAC.Name = "vSC_VMKMAC";
		this.vSC_VMKMAC.ReadOnly = true;
		this.vSC_VMKMAC.ToolTipText = "Mac Address";
		this.vSC_VMKMAC.Width = 125;
		this.vSC_VMKDHCP.HeaderText = "DHCP";
		this.vSC_VMKDHCP.MinimumWidth = 6;
		this.vSC_VMKDHCP.Name = "vSC_VMKDHCP";
		this.vSC_VMKDHCP.ReadOnly = true;
		this.vSC_VMKDHCP.ToolTipText = "DHCP enabled?";
		this.vSC_VMKDHCP.Width = 125;
		this.vSC_VMKIP.HeaderText = "IP Address";
		this.vSC_VMKIP.MinimumWidth = 6;
		this.vSC_VMKIP.Name = "vSC_VMKIP";
		this.vSC_VMKIP.ReadOnly = true;
		this.vSC_VMKIP.ToolTipText = "The IP address";
		this.vSC_VMKIP.Width = 125;
		this.vSC_VMKIP6.HeaderText = "IP 6 Address";
		this.vSC_VMKIP6.MinimumWidth = 6;
		this.vSC_VMKIP6.Name = "vSC_VMKIP6";
		this.vSC_VMKIP6.ReadOnly = true;
		this.vSC_VMKIP6.ToolTipText = "The IP 6 address";
		this.vSC_VMKIP6.Width = 125;
		this.vSC_VMKMask.HeaderText = "Subnet mask";
		this.vSC_VMKMask.MinimumWidth = 6;
		this.vSC_VMKMask.Name = "vSC_VMKMask";
		this.vSC_VMKMask.ReadOnly = true;
		this.vSC_VMKMask.ToolTipText = "The subnet mask";
		this.vSC_VMKMask.Width = 125;
		this.vSC_VMKGateway.HeaderText = "Gateway";
		this.vSC_VMKGateway.MinimumWidth = 6;
		this.vSC_VMKGateway.Name = "vSC_VMKGateway";
		this.vSC_VMKGateway.ReadOnly = true;
		this.vSC_VMKGateway.ToolTipText = "Default Gateway";
		this.vSC_VMKGateway.Width = 125;
		this.vSC_VMKGateway6.HeaderText = "IP 6 Gateway";
		this.vSC_VMKGateway6.MinimumWidth = 6;
		this.vSC_VMKGateway6.Name = "vSC_VMKGateway6";
		this.vSC_VMKGateway6.ReadOnly = true;
		this.vSC_VMKGateway6.ToolTipText = "Default IP 6 Gateway";
		this.vSC_VMKGateway6.Width = 125;
		dataGridViewCellStyle25.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle25.Format = "N0";
		dataGridViewCellStyle25.NullValue = null;
		this.vSC_VMKMTU.DefaultCellStyle = dataGridViewCellStyle25;
		this.vSC_VMKMTU.HeaderText = "MTU";
		this.vSC_VMKMTU.MinimumWidth = 6;
		this.vSC_VMKMTU.Name = "vSC_VMKMTU";
		this.vSC_VMKMTU.ReadOnly = true;
		this.vSC_VMKMTU.ToolTipText = "Maximum transmission unit for packets size in bytes for the virtual NIC";
		this.vSC_VMKMTU.Width = 125;
		this.vSC_VMKVISDKServer.HeaderText = "VI SDK Server";
		this.vSC_VMKVISDKServer.MinimumWidth = 6;
		this.vSC_VMKVISDKServer.Name = "vSC_VMKVISDKServer";
		this.vSC_VMKVISDKServer.ReadOnly = true;
		this.vSC_VMKVISDKServer.ToolTipText = "VI SDK Server, where RVTools gets it's information from";
		this.vSC_VMKVISDKServer.Width = 150;
		this.vSC_VMKInstanceUUID.HeaderText = "VI SDK UUID";
		this.vSC_VMKInstanceUUID.MinimumWidth = 6;
		this.vSC_VMKInstanceUUID.Name = "vSC_VMKInstanceUUID";
		this.vSC_VMKInstanceUUID.ReadOnly = true;
		this.vSC_VMKInstanceUUID.ToolTipText = "A globally unique identifier associated with this service instance";
		this.vSC_VMKInstanceUUID.Width = 125;
		this.tabdvPort.Controls.Add(this.panel22);
		this.tabdvPort.Location = new System.Drawing.Point(4, 25);
		this.tabdvPort.Margin = new System.Windows.Forms.Padding(4);
		this.tabdvPort.Name = "tabdvPort";
		this.tabdvPort.Size = new System.Drawing.Size(1725, 391);
		this.tabdvPort.TabIndex = 27;
		this.tabdvPort.Text = "dvPort";
		this.tabdvPort.ToolTipText = "Distributed Port Information";
		this.tabdvPort.UseVisualStyleBackColor = true;
		this.panel22.Controls.Add(this.dataGriddvPort);
		this.panel22.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel22.Location = new System.Drawing.Point(0, 0);
		this.panel22.Margin = new System.Windows.Forms.Padding(4);
		this.panel22.Name = "panel22";
		this.panel22.Padding = new System.Windows.Forms.Padding(20, 18, 20, 18);
		this.panel22.Size = new System.Drawing.Size(1725, 391);
		this.panel22.TabIndex = 8;
		this.dataGriddvPort.AllowUserToAddRows = false;
		this.dataGriddvPort.AllowUserToDeleteRows = false;
		this.dataGriddvPort.AllowUserToOrderColumns = true;
		this.dataGriddvPort.AllowUserToResizeRows = false;
		dataGridViewCellStyle26.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
		this.dataGriddvPort.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle26;
		this.dataGriddvPort.BackgroundColor = System.Drawing.SystemColors.Window;
		this.dataGriddvPort.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
		this.dataGriddvPort.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGriddvPort.Columns.AddRange(this.dvPortName, this.dvPortSwitch, this.dvPortType, this.dvPortNumPorts, this.dvPortVLAN, this.dvPortSpeed, this.dvPortFullDuplex, this.dvPortBlocked, this.dvPortAllowPromiscuous, this.dvPortMacChanges, this.dvPortActiveUplink, this.dvPortStandbyUplink, this.dvPortPolicy, this.dvPortForgedTransmits, this.dvPortInTrafficShaping, this.dvPortInTrafficAverage, this.dvPortInTrafficPeak, this.dvPortInTrafficBurst, this.dvPortOutTrafficShaping, this.dvPortOutTrafficAverage, this.dvPortOutTrafficPeak, this.dvPortOutTrafficBurst, this.dvPortReversePolicy, this.dvPortNotifySwitch, this.dvPortRollingOrder, this.dvPortCheckBeacon, this.dvPortLivePortMovingAllowed, this.dvPortCheckDuplex, this.dvPortCheckErrorPercent, this.dvPortCheckSpeed, this.dvPortPercentage, this.dvPortBlockOverrideAllowed, this.dvPortPortConfigResetAtDisconnect, this.dvPortShapingOverrideAllowed, this.dvPortVendorConfigOverrideAllowed, this.dvPortSecurityPolicyOverrideAllowed, this.dvPortUplinkTeamingOverrideAllowed, this.dvPortVlanOverrideAllowed, this.dvPortObjectID, this.dvPortVISDKServer, this.dvPortInstanceUUID);
		this.dataGriddvPort.Dock = System.Windows.Forms.DockStyle.Fill;
		this.dataGriddvPort.Location = new System.Drawing.Point(20, 18);
		this.dataGriddvPort.Margin = new System.Windows.Forms.Padding(4);
		this.dataGriddvPort.Name = "dataGriddvPort";
		this.dataGriddvPort.RowHeadersVisible = false;
		this.dataGriddvPort.RowHeadersWidth = 51;
		this.dataGriddvPort.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this.dataGriddvPort.Size = new System.Drawing.Size(1685, 355);
		this.dataGriddvPort.TabIndex = 2;
		this.dvPortName.Frozen = true;
		this.dvPortName.HeaderText = "Port";
		this.dvPortName.MinimumWidth = 6;
		this.dvPortName.Name = "dvPortName";
		this.dvPortName.ReadOnly = true;
		this.dvPortName.ToolTipText = "Distributed Port name";
		this.dvPortName.Width = 125;
		this.dvPortSwitch.Frozen = true;
		this.dvPortSwitch.HeaderText = "Switch";
		this.dvPortSwitch.MinimumWidth = 6;
		this.dvPortSwitch.Name = "dvPortSwitch";
		this.dvPortSwitch.ReadOnly = true;
		this.dvPortSwitch.Width = 125;
		this.dvPortType.HeaderText = "Type";
		this.dvPortType.MinimumWidth = 6;
		this.dvPortType.Name = "dvPortType";
		this.dvPortType.ReadOnly = true;
		this.dvPortType.ToolTipText = "The type of portgroup";
		this.dvPortType.Width = 125;
		dataGridViewCellStyle27.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle27.Format = "N0";
		dataGridViewCellStyle27.NullValue = null;
		this.dvPortNumPorts.DefaultCellStyle = dataGridViewCellStyle27;
		this.dvPortNumPorts.HeaderText = "# Ports";
		this.dvPortNumPorts.MinimumWidth = 6;
		this.dvPortNumPorts.Name = "dvPortNumPorts";
		this.dvPortNumPorts.ReadOnly = true;
		this.dvPortNumPorts.ToolTipText = "Number of ports in the portgroup";
		this.dvPortNumPorts.Width = 125;
		dataGridViewCellStyle28.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle28.Format = "N0";
		dataGridViewCellStyle28.NullValue = null;
		this.dvPortVLAN.DefaultCellStyle = dataGridViewCellStyle28;
		this.dvPortVLAN.HeaderText = "VLAN";
		this.dvPortVLAN.MinimumWidth = 6;
		this.dvPortVLAN.Name = "dvPortVLAN";
		this.dvPortVLAN.ReadOnly = true;
		this.dvPortVLAN.ToolTipText = "VLAN ID";
		this.dvPortVLAN.Width = 125;
		dataGridViewCellStyle29.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle29.Format = "N0";
		dataGridViewCellStyle29.NullValue = null;
		this.dvPortSpeed.DefaultCellStyle = dataGridViewCellStyle29;
		this.dvPortSpeed.HeaderText = "Speed";
		this.dvPortSpeed.MinimumWidth = 6;
		this.dvPortSpeed.Name = "dvPortSpeed";
		this.dvPortSpeed.ReadOnly = true;
		this.dvPortSpeed.ToolTipText = "Speed in megabits per second";
		this.dvPortSpeed.Width = 125;
		this.dvPortFullDuplex.HeaderText = "Full Duplex";
		this.dvPortFullDuplex.MinimumWidth = 6;
		this.dvPortFullDuplex.Name = "dvPortFullDuplex";
		this.dvPortFullDuplex.ReadOnly = true;
		this.dvPortFullDuplex.Width = 150;
		this.dvPortBlocked.HeaderText = "Blocked";
		this.dvPortBlocked.MinimumWidth = 6;
		this.dvPortBlocked.Name = "dvPortBlocked";
		this.dvPortBlocked.ReadOnly = true;
		this.dvPortBlocked.ToolTipText = "Whether this port is blocked, i.e. packet forwarding is stopped.";
		this.dvPortBlocked.Width = 125;
		this.dvPortAllowPromiscuous.HeaderText = "Allow Promiscuous";
		this.dvPortAllowPromiscuous.MinimumWidth = 6;
		this.dvPortAllowPromiscuous.Name = "dvPortAllowPromiscuous";
		this.dvPortAllowPromiscuous.ReadOnly = true;
		this.dvPortAllowPromiscuous.ToolTipText = "Flag to indicate whether or not all traffic is seen on the port";
		this.dvPortAllowPromiscuous.Width = 150;
		this.dvPortMacChanges.HeaderText = "Mac Changes";
		this.dvPortMacChanges.MinimumWidth = 6;
		this.dvPortMacChanges.Name = "dvPortMacChanges";
		this.dvPortMacChanges.ReadOnly = true;
		this.dvPortMacChanges.ToolTipText = "Flag to indicate whether or not the virtual network adapter should be allowed to send network traffic with a different MAC address than that of the virtual network adapter";
		this.dvPortMacChanges.Width = 125;
		this.dvPortActiveUplink.HeaderText = "Active Uplink";
		this.dvPortActiveUplink.MinimumWidth = 6;
		this.dvPortActiveUplink.Name = "dvPortActiveUplink";
		this.dvPortActiveUplink.ReadOnly = true;
		this.dvPortActiveUplink.ToolTipText = "List of active uplink ports used for load balancing";
		this.dvPortActiveUplink.Width = 150;
		this.dvPortStandbyUplink.HeaderText = "Standby Uplink";
		this.dvPortStandbyUplink.MinimumWidth = 6;
		this.dvPortStandbyUplink.Name = "dvPortStandbyUplink";
		this.dvPortStandbyUplink.ReadOnly = true;
		this.dvPortStandbyUplink.ToolTipText = "Standby uplink ports used for failover";
		this.dvPortStandbyUplink.Width = 150;
		this.dvPortPolicy.HeaderText = "Policy";
		this.dvPortPolicy.MinimumWidth = 6;
		this.dvPortPolicy.Name = "dvPortPolicy";
		this.dvPortPolicy.ToolTipText = "Network adapter teaming policy";
		this.dvPortPolicy.Width = 125;
		this.dvPortForgedTransmits.HeaderText = "Forged Transmits";
		this.dvPortForgedTransmits.MinimumWidth = 6;
		this.dvPortForgedTransmits.Name = "dvPortForgedTransmits";
		this.dvPortForgedTransmits.ReadOnly = true;
		this.dvPortForgedTransmits.ToolTipText = "Flag to indicate whether or not the Media Access Control (MAC) address can be changed";
		this.dvPortForgedTransmits.Width = 150;
		this.dvPortInTrafficShaping.HeaderText = "In Traffic Shaping";
		this.dvPortInTrafficShaping.MinimumWidth = 6;
		this.dvPortInTrafficShaping.Name = "dvPortInTrafficShaping";
		this.dvPortInTrafficShaping.ReadOnly = true;
		this.dvPortInTrafficShaping.ToolTipText = "Flag to indicate whether or not traffic shaper is enabled on the port";
		this.dvPortInTrafficShaping.Width = 150;
		dataGridViewCellStyle30.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle30.Format = "N0";
		dataGridViewCellStyle30.NullValue = null;
		this.dvPortInTrafficAverage.DefaultCellStyle = dataGridViewCellStyle30;
		this.dvPortInTrafficAverage.HeaderText = "In Avg";
		this.dvPortInTrafficAverage.MinimumWidth = 6;
		this.dvPortInTrafficAverage.Name = "dvPortInTrafficAverage";
		this.dvPortInTrafficAverage.ReadOnly = true;
		this.dvPortInTrafficAverage.ToolTipText = "The average bandwidth in Kbits per second if shaping is enabled on the port";
		this.dvPortInTrafficAverage.Width = 125;
		dataGridViewCellStyle31.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle31.Format = "N0";
		dataGridViewCellStyle31.NullValue = null;
		this.dvPortInTrafficPeak.DefaultCellStyle = dataGridViewCellStyle31;
		this.dvPortInTrafficPeak.HeaderText = "In Peak";
		this.dvPortInTrafficPeak.MinimumWidth = 6;
		this.dvPortInTrafficPeak.Name = "dvPortInTrafficPeak";
		this.dvPortInTrafficPeak.ReadOnly = true;
		this.dvPortInTrafficPeak.ToolTipText = "The peak bandwidth during bursts in Kbits per second if traffic shaping is enabled on the port";
		this.dvPortInTrafficPeak.Width = 125;
		dataGridViewCellStyle32.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle32.Format = "N0";
		dataGridViewCellStyle32.NullValue = null;
		this.dvPortInTrafficBurst.DefaultCellStyle = dataGridViewCellStyle32;
		this.dvPortInTrafficBurst.HeaderText = "In Burst";
		this.dvPortInTrafficBurst.MinimumWidth = 6;
		this.dvPortInTrafficBurst.Name = "dvPortInTrafficBurst";
		this.dvPortInTrafficBurst.ReadOnly = true;
		this.dvPortInTrafficBurst.ToolTipText = "The maximum burst size allowed in Kbytes if shaping is enabled on the port";
		this.dvPortInTrafficBurst.Width = 125;
		this.dvPortOutTrafficShaping.HeaderText = "Out Traffic Shaping";
		this.dvPortOutTrafficShaping.MinimumWidth = 6;
		this.dvPortOutTrafficShaping.Name = "dvPortOutTrafficShaping";
		this.dvPortOutTrafficShaping.ReadOnly = true;
		this.dvPortOutTrafficShaping.ToolTipText = "Flag to indicate whether or not traffic shaper is enabled on the port";
		this.dvPortOutTrafficShaping.Width = 150;
		dataGridViewCellStyle33.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle33.Format = "N0";
		dataGridViewCellStyle33.NullValue = null;
		this.dvPortOutTrafficAverage.DefaultCellStyle = dataGridViewCellStyle33;
		this.dvPortOutTrafficAverage.HeaderText = "Out Avg";
		this.dvPortOutTrafficAverage.MinimumWidth = 6;
		this.dvPortOutTrafficAverage.Name = "dvPortOutTrafficAverage";
		this.dvPortOutTrafficAverage.ReadOnly = true;
		this.dvPortOutTrafficAverage.ToolTipText = "The average bandwidth in Kbits per second if shaping is enabled on the port";
		this.dvPortOutTrafficAverage.Width = 125;
		dataGridViewCellStyle34.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle34.Format = "N0";
		dataGridViewCellStyle34.NullValue = null;
		this.dvPortOutTrafficPeak.DefaultCellStyle = dataGridViewCellStyle34;
		this.dvPortOutTrafficPeak.HeaderText = "Out Peak";
		this.dvPortOutTrafficPeak.MinimumWidth = 6;
		this.dvPortOutTrafficPeak.Name = "dvPortOutTrafficPeak";
		this.dvPortOutTrafficPeak.ReadOnly = true;
		this.dvPortOutTrafficPeak.ToolTipText = "The peak bandwidth during bursts in Kbits per second if traffic shaping is enabled on the port";
		this.dvPortOutTrafficPeak.Width = 125;
		dataGridViewCellStyle35.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle35.Format = "N0";
		dataGridViewCellStyle35.NullValue = null;
		this.dvPortOutTrafficBurst.DefaultCellStyle = dataGridViewCellStyle35;
		this.dvPortOutTrafficBurst.HeaderText = "Out Burst";
		this.dvPortOutTrafficBurst.MinimumWidth = 6;
		this.dvPortOutTrafficBurst.Name = "dvPortOutTrafficBurst";
		this.dvPortOutTrafficBurst.ReadOnly = true;
		this.dvPortOutTrafficBurst.ToolTipText = "The maximum burst size allowed in Kbytes if shaping is enabled on the port";
		this.dvPortOutTrafficBurst.Width = 125;
		this.dvPortReversePolicy.HeaderText = "Reverse Policy";
		this.dvPortReversePolicy.MinimumWidth = 6;
		this.dvPortReversePolicy.Name = "dvPortReversePolicy";
		this.dvPortReversePolicy.ReadOnly = true;
		this.dvPortReversePolicy.ToolTipText = "Flag to indicate whether or not the teaming policy is applied to inbound frames as well";
		this.dvPortReversePolicy.Width = 150;
		this.dvPortNotifySwitch.HeaderText = "Notify Switch";
		this.dvPortNotifySwitch.MinimumWidth = 6;
		this.dvPortNotifySwitch.Name = "dvPortNotifySwitch";
		this.dvPortNotifySwitch.ToolTipText = "Flag to specify whether or not to notify the physical switch if a link fails";
		this.dvPortNotifySwitch.Width = 150;
		this.dvPortRollingOrder.HeaderText = "Rolling Order";
		this.dvPortRollingOrder.MinimumWidth = 6;
		this.dvPortRollingOrder.Name = "dvPortRollingOrder";
		this.dvPortRollingOrder.ReadOnly = true;
		this.dvPortRollingOrder.ToolTipText = "Flag to indicate whether or not to use a rolling policy when restoring links";
		this.dvPortRollingOrder.Width = 150;
		this.dvPortCheckBeacon.HeaderText = "Check Beacon";
		this.dvPortCheckBeacon.MinimumWidth = 6;
		this.dvPortCheckBeacon.Name = "dvPortCheckBeacon";
		this.dvPortCheckBeacon.ReadOnly = true;
		this.dvPortCheckBeacon.ToolTipText = "Flag to indicate whether or not to enable this property to enable beacon probing as a method to validate the link status of a physical network adapter";
		this.dvPortCheckBeacon.Width = 150;
		this.dvPortLivePortMovingAllowed.HeaderText = "Live Port Moving";
		this.dvPortLivePortMovingAllowed.MinimumWidth = 6;
		this.dvPortLivePortMovingAllowed.Name = "dvPortLivePortMovingAllowed";
		this.dvPortLivePortMovingAllowed.ReadOnly = true;
		this.dvPortLivePortMovingAllowed.ToolTipText = "Allow a live port to be move in and out of the portgroup";
		this.dvPortLivePortMovingAllowed.Width = 180;
		this.dvPortCheckDuplex.HeaderText = "Check Duplex";
		this.dvPortCheckDuplex.MinimumWidth = 6;
		this.dvPortCheckDuplex.Name = "dvPortCheckDuplex";
		this.dvPortCheckDuplex.ReadOnly = true;
		this.dvPortCheckDuplex.ToolTipText = "Flag to indicate whether or not to use the link duplex reported by the driver as link selection criteria";
		this.dvPortCheckDuplex.Width = 150;
		this.dvPortCheckErrorPercent.HeaderText = "Check Error %";
		this.dvPortCheckErrorPercent.MinimumWidth = 6;
		this.dvPortCheckErrorPercent.Name = "dvPortCheckErrorPercent";
		this.dvPortCheckErrorPercent.ReadOnly = true;
		this.dvPortCheckErrorPercent.ToolTipText = "Flag to indicate whether or not to use link error percentage to detect failure";
		this.dvPortCheckErrorPercent.Width = 150;
		this.dvPortCheckSpeed.HeaderText = "Check Speed";
		this.dvPortCheckSpeed.MinimumWidth = 6;
		this.dvPortCheckSpeed.Name = "dvPortCheckSpeed";
		this.dvPortCheckSpeed.ReadOnly = true;
		this.dvPortCheckSpeed.ToolTipText = "Use link speed as the criteria to detect a failure";
		this.dvPortCheckSpeed.Width = 150;
		dataGridViewCellStyle36.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle36.Format = "N0";
		dataGridViewCellStyle36.NullValue = null;
		this.dvPortPercentage.DefaultCellStyle = dataGridViewCellStyle36;
		this.dvPortPercentage.HeaderText = "Percentage";
		this.dvPortPercentage.MinimumWidth = 6;
		this.dvPortPercentage.Name = "dvPortPercentage";
		this.dvPortPercentage.ReadOnly = true;
		this.dvPortPercentage.Width = 150;
		this.dvPortBlockOverrideAllowed.HeaderText = "Block Override";
		this.dvPortBlockOverrideAllowed.MinimumWidth = 6;
		this.dvPortBlockOverrideAllowed.Name = "dvPortBlockOverrideAllowed";
		this.dvPortBlockOverrideAllowed.ReadOnly = true;
		this.dvPortBlockOverrideAllowed.ToolTipText = "Allow the setting of blocked of an individual port to override the setting in defaultPortConfig of a portgroup";
		this.dvPortBlockOverrideAllowed.Width = 150;
		this.dvPortPortConfigResetAtDisconnect.HeaderText = "Config Reset";
		this.dvPortPortConfigResetAtDisconnect.MinimumWidth = 6;
		this.dvPortPortConfigResetAtDisconnect.Name = "dvPortPortConfigResetAtDisconnect";
		this.dvPortPortConfigResetAtDisconnect.ReadOnly = true;
		this.dvPortPortConfigResetAtDisconnect.ToolTipText = "If true, reset the port network setting back to the portgroup setting (thus removing the per-port setting) when the port is disconnected from the connectee";
		this.dvPortPortConfigResetAtDisconnect.Width = 150;
		this.dvPortShapingOverrideAllowed.HeaderText = "Shaping Override";
		this.dvPortShapingOverrideAllowed.MinimumWidth = 6;
		this.dvPortShapingOverrideAllowed.Name = "dvPortShapingOverrideAllowed";
		this.dvPortShapingOverrideAllowed.ReadOnly = true;
		this.dvPortShapingOverrideAllowed.ToolTipText = "Shaping override allowed?";
		this.dvPortShapingOverrideAllowed.Width = 150;
		this.dvPortVendorConfigOverrideAllowed.HeaderText = "Vendor Config Override";
		this.dvPortVendorConfigOverrideAllowed.MinimumWidth = 6;
		this.dvPortVendorConfigOverrideAllowed.Name = "dvPortVendorConfigOverrideAllowed";
		this.dvPortVendorConfigOverrideAllowed.ReadOnly = true;
		this.dvPortVendorConfigOverrideAllowed.ToolTipText = "Vendor Config override allowed?";
		this.dvPortVendorConfigOverrideAllowed.Width = 250;
		this.dvPortSecurityPolicyOverrideAllowed.HeaderText = "Sec. Policy Override";
		this.dvPortSecurityPolicyOverrideAllowed.MinimumWidth = 6;
		this.dvPortSecurityPolicyOverrideAllowed.Name = "dvPortSecurityPolicyOverrideAllowed";
		this.dvPortSecurityPolicyOverrideAllowed.ReadOnly = true;
		this.dvPortSecurityPolicyOverrideAllowed.ToolTipText = "Security Policy override allowed?";
		this.dvPortSecurityPolicyOverrideAllowed.Width = 200;
		this.dvPortUplinkTeamingOverrideAllowed.HeaderText = "Teaming Override";
		this.dvPortUplinkTeamingOverrideAllowed.MinimumWidth = 6;
		this.dvPortUplinkTeamingOverrideAllowed.Name = "dvPortUplinkTeamingOverrideAllowed";
		this.dvPortUplinkTeamingOverrideAllowed.ReadOnly = true;
		this.dvPortUplinkTeamingOverrideAllowed.ToolTipText = "Teaming override allowed?";
		this.dvPortUplinkTeamingOverrideAllowed.Width = 150;
		this.dvPortVlanOverrideAllowed.HeaderText = "Vlan Override";
		this.dvPortVlanOverrideAllowed.MinimumWidth = 6;
		this.dvPortVlanOverrideAllowed.Name = "dvPortVlanOverrideAllowed";
		this.dvPortVlanOverrideAllowed.ReadOnly = true;
		this.dvPortVlanOverrideAllowed.ToolTipText = "Vlan override allowed?";
		this.dvPortVlanOverrideAllowed.Width = 150;
		this.dvPortObjectID.HeaderText = "Object ID";
		this.dvPortObjectID.MinimumWidth = 6;
		this.dvPortObjectID.Name = "dvPortObjectID";
		this.dvPortObjectID.ReadOnly = true;
		this.dvPortObjectID.ToolTipText = "Distributed Port object id";
		this.dvPortObjectID.Width = 125;
		this.dvPortVISDKServer.HeaderText = "VI SDK Server";
		this.dvPortVISDKServer.MinimumWidth = 6;
		this.dvPortVISDKServer.Name = "dvPortVISDKServer";
		this.dvPortVISDKServer.ReadOnly = true;
		this.dvPortVISDKServer.ToolTipText = "VI SDK Server, where RVTools gets it's information from";
		this.dvPortVISDKServer.Width = 150;
		this.dvPortInstanceUUID.HeaderText = "VI SDK UUID";
		this.dvPortInstanceUUID.MinimumWidth = 6;
		this.dvPortInstanceUUID.Name = "dvPortInstanceUUID";
		this.dvPortInstanceUUID.ReadOnly = true;
		this.dvPortInstanceUUID.ToolTipText = "A globally unique identifier associated with this service instance";
		this.dvPortInstanceUUID.Width = 125;
		this.tabdvSwitch.Controls.Add(this.panel21);
		this.tabdvSwitch.Location = new System.Drawing.Point(4, 25);
		this.tabdvSwitch.Margin = new System.Windows.Forms.Padding(4);
		this.tabdvSwitch.Name = "tabdvSwitch";
		this.tabdvSwitch.Size = new System.Drawing.Size(1725, 391);
		this.tabdvSwitch.TabIndex = 26;
		this.tabdvSwitch.Text = "dvSwitch";
		this.tabdvSwitch.ToolTipText = "Distributed Switch Information";
		this.tabdvSwitch.UseVisualStyleBackColor = true;
		this.panel21.Controls.Add(this.dataGriddvSwitch);
		this.panel21.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel21.Location = new System.Drawing.Point(0, 0);
		this.panel21.Margin = new System.Windows.Forms.Padding(4);
		this.panel21.Name = "panel21";
		this.panel21.Padding = new System.Windows.Forms.Padding(20, 18, 20, 18);
		this.panel21.Size = new System.Drawing.Size(1725, 391);
		this.panel21.TabIndex = 7;
		this.dataGriddvSwitch.AllowUserToAddRows = false;
		this.dataGriddvSwitch.AllowUserToDeleteRows = false;
		this.dataGriddvSwitch.AllowUserToOrderColumns = true;
		this.dataGriddvSwitch.AllowUserToResizeRows = false;
		dataGridViewCellStyle37.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
		this.dataGriddvSwitch.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle37;
		this.dataGriddvSwitch.BackgroundColor = System.Drawing.SystemColors.Window;
		this.dataGriddvSwitch.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
		this.dataGriddvSwitch.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGriddvSwitch.Columns.AddRange(this.dvSwitchName, this.dvSwitchDatacenter, this.dvSwitchProductInfoName, this.dvSwitchProductInfoVendor, this.dvSwitchProductInfoVersion, this.dvSwitchDescription, this.dvSwitchCreateTime, this.dvSwitchHostMembers, this.dvSwitchMaxPorts, this.dvSwitchNumPorts, this.dvSwitchNumVms, this.dvSwitchInTrafficEnabled, this.dvSwitchInTrafficAvg, this.dvSwitchInTrafficPeak, this.dvSwitchInTrafficBurst, this.dvSwitchOutTrafficEnabled, this.dvSwitchOutTrafficAvg, this.dvSwitchOutTrafficPeak, this.dvSwitchOutTrafficBurst, this.dvSwitchCDPProtocol, this.dvSwitchCDPoperation, this.dvSwitchLacpName, this.dvSwitchLacpMode, this.dvSwitchLacpLoadBalance, this.dvSwitchMaxMtu, this.dvSwitchContactInfoContact, this.dvSwitchContactInfoName, this.dvSwitchObjectID, this.dvSwitchVISDKServer, this.dvSwitchInstanceUUID);
		this.dataGriddvSwitch.Dock = System.Windows.Forms.DockStyle.Fill;
		this.dataGriddvSwitch.Location = new System.Drawing.Point(20, 18);
		this.dataGriddvSwitch.Margin = new System.Windows.Forms.Padding(4);
		this.dataGriddvSwitch.Name = "dataGriddvSwitch";
		this.dataGriddvSwitch.RowHeadersVisible = false;
		this.dataGriddvSwitch.RowHeadersWidth = 51;
		this.dataGriddvSwitch.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this.dataGriddvSwitch.Size = new System.Drawing.Size(1685, 355);
		this.dataGriddvSwitch.TabIndex = 2;
		this.dvSwitchName.Frozen = true;
		this.dvSwitchName.HeaderText = "Switch";
		this.dvSwitchName.MinimumWidth = 6;
		this.dvSwitchName.Name = "dvSwitchName";
		this.dvSwitchName.ReadOnly = true;
		this.dvSwitchName.ToolTipText = "Distributed Switch name";
		this.dvSwitchName.Width = 125;
		this.dvSwitchDatacenter.HeaderText = "Datacenter";
		this.dvSwitchDatacenter.MinimumWidth = 6;
		this.dvSwitchDatacenter.Name = "dvSwitchDatacenter";
		this.dvSwitchDatacenter.ReadOnly = true;
		this.dvSwitchDatacenter.Width = 125;
		this.dvSwitchProductInfoName.HeaderText = "Name";
		this.dvSwitchProductInfoName.MinimumWidth = 6;
		this.dvSwitchProductInfoName.Name = "dvSwitchProductInfoName";
		this.dvSwitchProductInfoName.ReadOnly = true;
		this.dvSwitchProductInfoName.ToolTipText = "Short form of the product name";
		this.dvSwitchProductInfoName.Width = 125;
		this.dvSwitchProductInfoVendor.HeaderText = "Vendor";
		this.dvSwitchProductInfoVendor.MinimumWidth = 6;
		this.dvSwitchProductInfoVendor.Name = "dvSwitchProductInfoVendor";
		this.dvSwitchProductInfoVendor.ReadOnly = true;
		this.dvSwitchProductInfoVendor.ToolTipText = "Name of the vendor of this product";
		this.dvSwitchProductInfoVendor.Width = 125;
		this.dvSwitchProductInfoVersion.HeaderText = "Version";
		this.dvSwitchProductInfoVersion.MinimumWidth = 6;
		this.dvSwitchProductInfoVersion.Name = "dvSwitchProductInfoVersion";
		this.dvSwitchProductInfoVersion.ReadOnly = true;
		this.dvSwitchProductInfoVersion.ToolTipText = "Version number";
		this.dvSwitchProductInfoVersion.Width = 125;
		this.dvSwitchDescription.HeaderText = "Description";
		this.dvSwitchDescription.MinimumWidth = 6;
		this.dvSwitchDescription.Name = "dvSwitchDescription";
		this.dvSwitchDescription.ReadOnly = true;
		this.dvSwitchDescription.ToolTipText = "A description string of the switch";
		this.dvSwitchDescription.Width = 125;
		dataGridViewCellStyle38.Format = "G";
		dataGridViewCellStyle38.NullValue = null;
		this.dvSwitchCreateTime.DefaultCellStyle = dataGridViewCellStyle38;
		this.dvSwitchCreateTime.HeaderText = "Created";
		this.dvSwitchCreateTime.MinimumWidth = 6;
		this.dvSwitchCreateTime.Name = "dvSwitchCreateTime";
		this.dvSwitchCreateTime.ReadOnly = true;
		this.dvSwitchCreateTime.ToolTipText = "The create time of the switch";
		this.dvSwitchCreateTime.Width = 125;
		this.dvSwitchHostMembers.HeaderText = "Host members";
		this.dvSwitchHostMembers.MinimumWidth = 6;
		this.dvSwitchHostMembers.Name = "dvSwitchHostMembers";
		this.dvSwitchHostMembers.ReadOnly = true;
		this.dvSwitchHostMembers.ToolTipText = "The name of the hosts that join the switch";
		this.dvSwitchHostMembers.Width = 125;
		dataGridViewCellStyle39.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle39.Format = "N0";
		dataGridViewCellStyle39.NullValue = null;
		this.dvSwitchMaxPorts.DefaultCellStyle = dataGridViewCellStyle39;
		this.dvSwitchMaxPorts.HeaderText = "Max Ports";
		this.dvSwitchMaxPorts.MinimumWidth = 6;
		this.dvSwitchMaxPorts.Name = "dvSwitchMaxPorts";
		this.dvSwitchMaxPorts.ToolTipText = "The maximum number of ports allowed in the switch, not including conflict ports";
		this.dvSwitchMaxPorts.Width = 125;
		dataGridViewCellStyle40.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle40.Format = "N0";
		dataGridViewCellStyle40.NullValue = null;
		this.dvSwitchNumPorts.DefaultCellStyle = dataGridViewCellStyle40;
		this.dvSwitchNumPorts.HeaderText = "# Ports";
		this.dvSwitchNumPorts.MinimumWidth = 6;
		this.dvSwitchNumPorts.Name = "dvSwitchNumPorts";
		this.dvSwitchNumPorts.ReadOnly = true;
		this.dvSwitchNumPorts.ToolTipText = "Current number of ports, not including conflict ports";
		this.dvSwitchNumPorts.Width = 125;
		dataGridViewCellStyle41.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle41.Format = "N0";
		dataGridViewCellStyle41.NullValue = null;
		this.dvSwitchNumVms.DefaultCellStyle = dataGridViewCellStyle41;
		this.dvSwitchNumVms.HeaderText = "# VMs";
		this.dvSwitchNumVms.MinimumWidth = 6;
		this.dvSwitchNumVms.Name = "dvSwitchNumVms";
		this.dvSwitchNumVms.ReadOnly = true;
		this.dvSwitchNumVms.ToolTipText = "The number of Virtual Machines with vnics that connects to the switch";
		this.dvSwitchNumVms.Width = 125;
		this.dvSwitchInTrafficEnabled.HeaderText = "In Traffic Shaping";
		this.dvSwitchInTrafficEnabled.MinimumWidth = 6;
		this.dvSwitchInTrafficEnabled.Name = "dvSwitchInTrafficEnabled";
		this.dvSwitchInTrafficEnabled.ReadOnly = true;
		this.dvSwitchInTrafficEnabled.ToolTipText = "The flag to indicate whether or not input traffic shaper is enabled on the port";
		this.dvSwitchInTrafficEnabled.Width = 150;
		dataGridViewCellStyle42.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle42.Format = "N0";
		dataGridViewCellStyle42.NullValue = null;
		this.dvSwitchInTrafficAvg.DefaultCellStyle = dataGridViewCellStyle42;
		this.dvSwitchInTrafficAvg.HeaderText = "In Avg";
		this.dvSwitchInTrafficAvg.MinimumWidth = 6;
		this.dvSwitchInTrafficAvg.Name = "dvSwitchInTrafficAvg";
		this.dvSwitchInTrafficAvg.ReadOnly = true;
		this.dvSwitchInTrafficAvg.ToolTipText = "The average bandwidth in Kbits per second if input shaping is enabled on the port. ";
		this.dvSwitchInTrafficAvg.Width = 125;
		dataGridViewCellStyle43.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle43.Format = "N0";
		dataGridViewCellStyle43.NullValue = null;
		this.dvSwitchInTrafficPeak.DefaultCellStyle = dataGridViewCellStyle43;
		this.dvSwitchInTrafficPeak.HeaderText = "In Peak";
		this.dvSwitchInTrafficPeak.MinimumWidth = 6;
		this.dvSwitchInTrafficPeak.Name = "dvSwitchInTrafficPeak";
		this.dvSwitchInTrafficPeak.ReadOnly = true;
		this.dvSwitchInTrafficPeak.ToolTipText = "The peak bandwidth during bursts in Kbits per second if traffic input shaping is enabled on the port";
		this.dvSwitchInTrafficPeak.Width = 125;
		dataGridViewCellStyle44.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle44.Format = "N0";
		dataGridViewCellStyle44.NullValue = null;
		this.dvSwitchInTrafficBurst.DefaultCellStyle = dataGridViewCellStyle44;
		this.dvSwitchInTrafficBurst.HeaderText = "In Burst";
		this.dvSwitchInTrafficBurst.MinimumWidth = 6;
		this.dvSwitchInTrafficBurst.Name = "dvSwitchInTrafficBurst";
		this.dvSwitchInTrafficBurst.ReadOnly = true;
		this.dvSwitchInTrafficBurst.ToolTipText = "The maximum burst size allowed in Kbytes if input shaping is enabled on the port";
		this.dvSwitchInTrafficBurst.Width = 125;
		this.dvSwitchOutTrafficEnabled.HeaderText = "Out Traffic Shaping";
		this.dvSwitchOutTrafficEnabled.MinimumWidth = 6;
		this.dvSwitchOutTrafficEnabled.Name = "dvSwitchOutTrafficEnabled";
		this.dvSwitchOutTrafficEnabled.ReadOnly = true;
		this.dvSwitchOutTrafficEnabled.ToolTipText = "The flag to indicate whether or not output traffic shaper is enabled on the port";
		this.dvSwitchOutTrafficEnabled.Width = 150;
		dataGridViewCellStyle45.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle45.Format = "N0";
		dataGridViewCellStyle45.NullValue = null;
		this.dvSwitchOutTrafficAvg.DefaultCellStyle = dataGridViewCellStyle45;
		this.dvSwitchOutTrafficAvg.HeaderText = "Out Avg";
		this.dvSwitchOutTrafficAvg.MinimumWidth = 6;
		this.dvSwitchOutTrafficAvg.Name = "dvSwitchOutTrafficAvg";
		this.dvSwitchOutTrafficAvg.ReadOnly = true;
		this.dvSwitchOutTrafficAvg.ToolTipText = "The average bandwidth in Kbits per second if output shaping is enabled on the port.";
		this.dvSwitchOutTrafficAvg.Width = 125;
		dataGridViewCellStyle46.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle46.Format = "N0";
		dataGridViewCellStyle46.NullValue = null;
		this.dvSwitchOutTrafficPeak.DefaultCellStyle = dataGridViewCellStyle46;
		this.dvSwitchOutTrafficPeak.HeaderText = "Out Peak";
		this.dvSwitchOutTrafficPeak.MinimumWidth = 6;
		this.dvSwitchOutTrafficPeak.Name = "dvSwitchOutTrafficPeak";
		this.dvSwitchOutTrafficPeak.ReadOnly = true;
		this.dvSwitchOutTrafficPeak.ToolTipText = "The peak bandwidth during bursts in Kbits per second if traffic output shaping is enabled on the port";
		this.dvSwitchOutTrafficPeak.Width = 125;
		dataGridViewCellStyle47.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle47.Format = "N0";
		dataGridViewCellStyle47.NullValue = null;
		this.dvSwitchOutTrafficBurst.DefaultCellStyle = dataGridViewCellStyle47;
		this.dvSwitchOutTrafficBurst.HeaderText = "Out Burst";
		this.dvSwitchOutTrafficBurst.MinimumWidth = 6;
		this.dvSwitchOutTrafficBurst.Name = "dvSwitchOutTrafficBurst";
		this.dvSwitchOutTrafficBurst.ReadOnly = true;
		this.dvSwitchOutTrafficBurst.ToolTipText = "The maximum burst size allowed in Kbytes if output shaping is enabled on the port";
		this.dvSwitchOutTrafficBurst.Width = 125;
		this.dvSwitchCDPProtocol.HeaderText = "CDP Type";
		this.dvSwitchCDPProtocol.MinimumWidth = 6;
		this.dvSwitchCDPProtocol.Name = "dvSwitchCDPProtocol";
		this.dvSwitchCDPProtocol.ReadOnly = true;
		this.dvSwitchCDPProtocol.ToolTipText = "The discovery protocol type";
		this.dvSwitchCDPProtocol.Width = 125;
		this.dvSwitchCDPoperation.HeaderText = "CDP Operation";
		this.dvSwitchCDPoperation.MinimumWidth = 6;
		this.dvSwitchCDPoperation.Name = "dvSwitchCDPoperation";
		this.dvSwitchCDPoperation.ReadOnly = true;
		this.dvSwitchCDPoperation.ToolTipText = "Whether to advertise or listen";
		this.dvSwitchCDPoperation.Width = 150;
		this.dvSwitchLacpName.HeaderText = "LACP Name";
		this.dvSwitchLacpName.MinimumWidth = 6;
		this.dvSwitchLacpName.Name = "dvSwitchLacpName";
		this.dvSwitchLacpName.ReadOnly = true;
		this.dvSwitchLacpName.ToolTipText = "LACP display name";
		this.dvSwitchLacpName.Width = 125;
		this.dvSwitchLacpMode.HeaderText = "LACP Mode";
		this.dvSwitchLacpMode.MinimumWidth = 6;
		this.dvSwitchLacpMode.Name = "dvSwitchLacpMode";
		this.dvSwitchLacpMode.ReadOnly = true;
		this.dvSwitchLacpMode.ToolTipText = "The mode of link Aggregation Control Protocol";
		this.dvSwitchLacpMode.Width = 125;
		this.dvSwitchLacpLoadBalance.HeaderText = "LACP Load Balance Alg.";
		this.dvSwitchLacpLoadBalance.MinimumWidth = 6;
		this.dvSwitchLacpLoadBalance.Name = "dvSwitchLacpLoadBalance";
		this.dvSwitchLacpLoadBalance.ReadOnly = true;
		this.dvSwitchLacpLoadBalance.ToolTipText = "Load balance policy";
		this.dvSwitchLacpLoadBalance.Width = 250;
		dataGridViewCellStyle48.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle48.Format = "N0";
		dataGridViewCellStyle48.NullValue = null;
		this.dvSwitchMaxMtu.DefaultCellStyle = dataGridViewCellStyle48;
		this.dvSwitchMaxMtu.HeaderText = "Max MTU";
		this.dvSwitchMaxMtu.MinimumWidth = 6;
		this.dvSwitchMaxMtu.Name = "dvSwitchMaxMtu";
		this.dvSwitchMaxMtu.ReadOnly = true;
		this.dvSwitchMaxMtu.ToolTipText = "The max MTU in the switch";
		this.dvSwitchMaxMtu.Width = 125;
		this.dvSwitchContactInfoContact.HeaderText = "Contact";
		this.dvSwitchContactInfoContact.MinimumWidth = 6;
		this.dvSwitchContactInfoContact.Name = "dvSwitchContactInfoContact";
		this.dvSwitchContactInfoContact.ReadOnly = true;
		this.dvSwitchContactInfoContact.ToolTipText = "The contact information for the person";
		this.dvSwitchContactInfoContact.Width = 125;
		this.dvSwitchContactInfoName.HeaderText = "Admin Name";
		this.dvSwitchContactInfoName.MinimumWidth = 6;
		this.dvSwitchContactInfoName.Name = "dvSwitchContactInfoName";
		this.dvSwitchContactInfoName.ReadOnly = true;
		this.dvSwitchContactInfoName.ToolTipText = "The name of the person that is responsible for the switch";
		this.dvSwitchContactInfoName.Width = 125;
		this.dvSwitchObjectID.HeaderText = "Object ID";
		this.dvSwitchObjectID.MinimumWidth = 6;
		this.dvSwitchObjectID.Name = "dvSwitchObjectID";
		this.dvSwitchObjectID.ReadOnly = true;
		this.dvSwitchObjectID.ToolTipText = "Distributed Switch object id";
		this.dvSwitchObjectID.Width = 125;
		this.dvSwitchVISDKServer.HeaderText = "VI SDK Server";
		this.dvSwitchVISDKServer.MinimumWidth = 6;
		this.dvSwitchVISDKServer.Name = "dvSwitchVISDKServer";
		this.dvSwitchVISDKServer.ReadOnly = true;
		this.dvSwitchVISDKServer.ToolTipText = "VI SDK Server, where RVTools gets it's information from";
		this.dvSwitchVISDKServer.Width = 150;
		this.dvSwitchInstanceUUID.HeaderText = "VI SDK UUID";
		this.dvSwitchInstanceUUID.MinimumWidth = 6;
		this.dvSwitchInstanceUUID.Name = "dvSwitchInstanceUUID";
		this.dvSwitchInstanceUUID.ReadOnly = true;
		this.dvSwitchInstanceUUID.ToolTipText = "A globally unique identifier associated with this service instance";
		this.dvSwitchInstanceUUID.Width = 125;
		this.tabvPort.Controls.Add(this.panel18);
		this.tabvPort.Location = new System.Drawing.Point(4, 25);
		this.tabvPort.Margin = new System.Windows.Forms.Padding(4);
		this.tabvPort.Name = "tabvPort";
		this.tabvPort.Size = new System.Drawing.Size(1725, 391);
		this.tabvPort.TabIndex = 23;
		this.tabvPort.Text = "vPort";
		this.tabvPort.ToolTipText = "Port Group Information";
		this.tabvPort.UseVisualStyleBackColor = true;
		this.panel18.Controls.Add(this.dataGridvPort);
		this.panel18.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel18.Location = new System.Drawing.Point(0, 0);
		this.panel18.Margin = new System.Windows.Forms.Padding(4);
		this.panel18.Name = "panel18";
		this.panel18.Padding = new System.Windows.Forms.Padding(20, 18, 20, 18);
		this.panel18.Size = new System.Drawing.Size(1725, 391);
		this.panel18.TabIndex = 7;
		this.dataGridvPort.AllowUserToAddRows = false;
		this.dataGridvPort.AllowUserToDeleteRows = false;
		this.dataGridvPort.AllowUserToOrderColumns = true;
		this.dataGridvPort.AllowUserToResizeRows = false;
		dataGridViewCellStyle49.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
		this.dataGridvPort.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle49;
		this.dataGridvPort.BackgroundColor = System.Drawing.SystemColors.Window;
		this.dataGridvPort.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
		this.dataGridvPort.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGridvPort.Columns.AddRange(this.vPortHost, this.vPortDatacenter, this.vPortCluster, this.vPortName, this.vPortSwitch, this.vPortVLAN, this.vPortAllowPromiscuous, this.vPortMacChanges, this.vPortForgedTransmits, this.vPortTrafficShaping, this.vPortAverageBandwidth, this.vPortPeakBandwidth, this.vPortBurstSize, this.vPortPolicy, this.vPortReversePolicy, this.vPortNotifySwitches, this.vPortRollingOrder, this.vPortCsumOffload, this.vPortTCPSegmentation, this.vPortZeroCopyXmit, this.vPortVISDKServer, this.vPortInstanceUUID);
		this.dataGridvPort.Dock = System.Windows.Forms.DockStyle.Fill;
		this.dataGridvPort.Location = new System.Drawing.Point(20, 18);
		this.dataGridvPort.Margin = new System.Windows.Forms.Padding(4);
		this.dataGridvPort.Name = "dataGridvPort";
		this.dataGridvPort.RowHeadersVisible = false;
		this.dataGridvPort.RowHeadersWidth = 51;
		this.dataGridvPort.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this.dataGridvPort.Size = new System.Drawing.Size(1685, 355);
		this.dataGridvPort.TabIndex = 2;
		this.vPortHost.Frozen = true;
		this.vPortHost.HeaderText = "Host";
		this.vPortHost.MinimumWidth = 6;
		this.vPortHost.Name = "vPortHost";
		this.vPortHost.ReadOnly = true;
		this.vPortHost.ToolTipText = "Host name";
		this.vPortHost.Width = 125;
		this.vPortDatacenter.HeaderText = "Datacenter";
		this.vPortDatacenter.MinimumWidth = 6;
		this.vPortDatacenter.Name = "vPortDatacenter";
		this.vPortDatacenter.ReadOnly = true;
		this.vPortDatacenter.ToolTipText = "Datacenter name";
		this.vPortDatacenter.Width = 125;
		this.vPortCluster.HeaderText = "Cluster";
		this.vPortCluster.MinimumWidth = 6;
		this.vPortCluster.Name = "vPortCluster";
		this.vPortCluster.ReadOnly = true;
		this.vPortCluster.ToolTipText = "Cluster name";
		this.vPortCluster.Width = 125;
		this.vPortName.HeaderText = "Port Group";
		this.vPortName.MinimumWidth = 6;
		this.vPortName.Name = "vPortName";
		this.vPortName.ReadOnly = true;
		this.vPortName.ToolTipText = "The name of the port group";
		this.vPortName.Width = 125;
		this.vPortSwitch.HeaderText = "Switch";
		this.vPortSwitch.MinimumWidth = 6;
		this.vPortSwitch.Name = "vPortSwitch";
		this.vPortSwitch.ReadOnly = true;
		this.vPortSwitch.ToolTipText = "Switch Name";
		this.vPortSwitch.Width = 125;
		dataGridViewCellStyle50.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		this.vPortVLAN.DefaultCellStyle = dataGridViewCellStyle50;
		this.vPortVLAN.HeaderText = "VLAN";
		this.vPortVLAN.MinimumWidth = 6;
		this.vPortVLAN.Name = "vPortVLAN";
		this.vPortVLAN.ReadOnly = true;
		this.vPortVLAN.ToolTipText = "VLAN ID";
		this.vPortVLAN.Width = 125;
		this.vPortAllowPromiscuous.HeaderText = "Promiscuous Mode";
		this.vPortAllowPromiscuous.MinimumWidth = 6;
		this.vPortAllowPromiscuous.Name = "vPortAllowPromiscuous";
		this.vPortAllowPromiscuous.ReadOnly = true;
		this.vPortAllowPromiscuous.ToolTipText = "Security: Allow Promiscuous Mode";
		this.vPortAllowPromiscuous.Width = 200;
		this.vPortMacChanges.HeaderText = "Mac Changes";
		this.vPortMacChanges.MinimumWidth = 6;
		this.vPortMacChanges.Name = "vPortMacChanges";
		this.vPortMacChanges.ReadOnly = true;
		this.vPortMacChanges.ToolTipText = "Security: Allow MAC address Changes";
		this.vPortMacChanges.Width = 200;
		this.vPortForgedTransmits.HeaderText = "Forged Transmits";
		this.vPortForgedTransmits.MinimumWidth = 6;
		this.vPortForgedTransmits.Name = "vPortForgedTransmits";
		this.vPortForgedTransmits.ReadOnly = true;
		this.vPortForgedTransmits.ToolTipText = "Security: Allow Forged Transmits";
		this.vPortForgedTransmits.Width = 200;
		this.vPortTrafficShaping.HeaderText = "Traffic Shaping";
		this.vPortTrafficShaping.MinimumWidth = 6;
		this.vPortTrafficShaping.Name = "vPortTrafficShaping";
		this.vPortTrafficShaping.ReadOnly = true;
		this.vPortTrafficShaping.ToolTipText = "Traffic Shaping: Flag to indicate whether or not traffic shaper is enabled on the port";
		this.vPortTrafficShaping.Width = 200;
		dataGridViewCellStyle51.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle51.Format = "N0";
		dataGridViewCellStyle51.NullValue = null;
		this.vPortAverageBandwidth.DefaultCellStyle = dataGridViewCellStyle51;
		this.vPortAverageBandwidth.HeaderText = "Width";
		this.vPortAverageBandwidth.MinimumWidth = 6;
		this.vPortAverageBandwidth.Name = "vPortAverageBandwidth";
		this.vPortAverageBandwidth.ReadOnly = true;
		this.vPortAverageBandwidth.ToolTipText = "Traffic Shaping: Average Bandwidth in Kbps";
		this.vPortAverageBandwidth.Width = 125;
		dataGridViewCellStyle52.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle52.Format = "N0";
		dataGridViewCellStyle52.NullValue = null;
		this.vPortPeakBandwidth.DefaultCellStyle = dataGridViewCellStyle52;
		this.vPortPeakBandwidth.HeaderText = "Peak";
		this.vPortPeakBandwidth.MinimumWidth = 6;
		this.vPortPeakBandwidth.Name = "vPortPeakBandwidth";
		this.vPortPeakBandwidth.ReadOnly = true;
		this.vPortPeakBandwidth.ToolTipText = "Traffic Shapping: Peak Bandwidth in Kbps";
		this.vPortPeakBandwidth.Width = 125;
		dataGridViewCellStyle53.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle53.Format = "N0";
		dataGridViewCellStyle53.NullValue = null;
		this.vPortBurstSize.DefaultCellStyle = dataGridViewCellStyle53;
		this.vPortBurstSize.HeaderText = "Burst";
		this.vPortBurstSize.MinimumWidth = 6;
		this.vPortBurstSize.Name = "vPortBurstSize";
		this.vPortBurstSize.ReadOnly = true;
		this.vPortBurstSize.ToolTipText = "Traffic Shaping: Burst size in KB";
		this.vPortBurstSize.Width = 125;
		this.vPortPolicy.HeaderText = "Policy";
		this.vPortPolicy.MinimumWidth = 6;
		this.vPortPolicy.Name = "vPortPolicy";
		this.vPortPolicy.ReadOnly = true;
		this.vPortPolicy.ToolTipText = "NIC Teaming: Network adapter teaming policy includes failover and load balancing";
		this.vPortPolicy.Width = 125;
		this.vPortReversePolicy.HeaderText = "Reverse Policy";
		this.vPortReversePolicy.MinimumWidth = 6;
		this.vPortReversePolicy.Name = "vPortReversePolicy";
		this.vPortReversePolicy.ReadOnly = true;
		this.vPortReversePolicy.ToolTipText = "NIC Teaming: The flag to indicate whether or not the teaming policy is applied to inbound frames as well";
		this.vPortReversePolicy.Width = 200;
		this.vPortNotifySwitches.HeaderText = "Notify Switch";
		this.vPortNotifySwitches.MinimumWidth = 6;
		this.vPortNotifySwitches.Name = "vPortNotifySwitches";
		this.vPortNotifySwitches.ReadOnly = true;
		this.vPortNotifySwitches.ToolTipText = "NIC Teaming: Notify Switches";
		this.vPortNotifySwitches.Width = 200;
		this.vPortRollingOrder.HeaderText = "Rolling Order";
		this.vPortRollingOrder.MinimumWidth = 6;
		this.vPortRollingOrder.Name = "vPortRollingOrder";
		this.vPortRollingOrder.ReadOnly = true;
		this.vPortRollingOrder.ToolTipText = "NIC Teaming: Flag to indicate whether or not to use a rolling policy when restoring links";
		this.vPortRollingOrder.Width = 200;
		this.vPortCsumOffload.HeaderText = "Offload";
		this.vPortCsumOffload.MinimumWidth = 6;
		this.vPortCsumOffload.Name = "vPortCsumOffload";
		this.vPortCsumOffload.ReadOnly = true;
		this.vPortCsumOffload.ToolTipText = "Flag to indicate wheter or not checksum offloading is supported";
		this.vPortCsumOffload.Width = 125;
		this.vPortTCPSegmentation.HeaderText = "TSO";
		this.vPortTCPSegmentation.MinimumWidth = 6;
		this.vPortTCPSegmentation.Name = "vPortTCPSegmentation";
		this.vPortTCPSegmentation.ReadOnly = true;
		this.vPortTCPSegmentation.ToolTipText = "Flag to indicate whether or not TCP segmentation offloading (TSO) is supported.";
		this.vPortTCPSegmentation.Width = 125;
		this.vPortZeroCopyXmit.HeaderText = "Zero Copy Xmit";
		this.vPortZeroCopyXmit.MinimumWidth = 6;
		this.vPortZeroCopyXmit.Name = "vPortZeroCopyXmit";
		this.vPortZeroCopyXmit.ReadOnly = true;
		this.vPortZeroCopyXmit.Width = 200;
		this.vPortVISDKServer.HeaderText = "VI SDK Server";
		this.vPortVISDKServer.MinimumWidth = 6;
		this.vPortVISDKServer.Name = "vPortVISDKServer";
		this.vPortVISDKServer.ReadOnly = true;
		this.vPortVISDKServer.ToolTipText = "VI SDK Server, where RVTools gets it's information from";
		this.vPortVISDKServer.Width = 150;
		this.vPortInstanceUUID.HeaderText = "VI SDK UUID";
		this.vPortInstanceUUID.MinimumWidth = 6;
		this.vPortInstanceUUID.Name = "vPortInstanceUUID";
		this.vPortInstanceUUID.ReadOnly = true;
		this.vPortInstanceUUID.ToolTipText = "A globally unique identifier associated with this service instance";
		this.vPortInstanceUUID.Width = 125;
		this.tabvSwitch.Controls.Add(this.panel17);
		this.tabvSwitch.Location = new System.Drawing.Point(4, 25);
		this.tabvSwitch.Margin = new System.Windows.Forms.Padding(4);
		this.tabvSwitch.Name = "tabvSwitch";
		this.tabvSwitch.Size = new System.Drawing.Size(1725, 391);
		this.tabvSwitch.TabIndex = 22;
		this.tabvSwitch.Text = "vSwitch";
		this.tabvSwitch.ToolTipText = "Virtual Switch Information";
		this.tabvSwitch.UseVisualStyleBackColor = true;
		this.panel17.Controls.Add(this.dataGridvSwitch);
		this.panel17.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel17.Location = new System.Drawing.Point(0, 0);
		this.panel17.Margin = new System.Windows.Forms.Padding(4);
		this.panel17.Name = "panel17";
		this.panel17.Padding = new System.Windows.Forms.Padding(20, 18, 20, 18);
		this.panel17.Size = new System.Drawing.Size(1725, 391);
		this.panel17.TabIndex = 6;
		this.dataGridvSwitch.AllowUserToAddRows = false;
		this.dataGridvSwitch.AllowUserToDeleteRows = false;
		this.dataGridvSwitch.AllowUserToOrderColumns = true;
		this.dataGridvSwitch.AllowUserToResizeRows = false;
		dataGridViewCellStyle54.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
		this.dataGridvSwitch.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle54;
		this.dataGridvSwitch.BackgroundColor = System.Drawing.SystemColors.Window;
		this.dataGridvSwitch.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
		this.dataGridvSwitch.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGridvSwitch.Columns.AddRange(this.vSwitchHost, this.vSwitchDatacenter, this.vSwitchCluster, this.vSwitchName, this.vSwitchNumPorts, this.vSwitchFreePorts, this.vSwitchAllowPromiscuous, this.vSwitchMacChanges, this.vSwitchForgedTransmits, this.vSwitchTrafficShaping, this.vSwitchAverageBandwidth, this.vSwitchPeakBandwidth, this.vSwitchBurstSize, this.vSwitchPolicy, this.vSwitchReversePolicy, this.vSwitchNotifySwitches, this.vSwitchRollingOrder, this.vSwitchCsumOffload, this.vSwitchTCPSegmentation, this.vSwitchZeroCopyXmit, this.vSwitchMTU, this.vSwitchVISDKServer, this.vSwitchInstanceUUID);
		this.dataGridvSwitch.Dock = System.Windows.Forms.DockStyle.Fill;
		this.dataGridvSwitch.Location = new System.Drawing.Point(20, 18);
		this.dataGridvSwitch.Margin = new System.Windows.Forms.Padding(4);
		this.dataGridvSwitch.Name = "dataGridvSwitch";
		this.dataGridvSwitch.RowHeadersVisible = false;
		this.dataGridvSwitch.RowHeadersWidth = 51;
		this.dataGridvSwitch.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this.dataGridvSwitch.Size = new System.Drawing.Size(1685, 355);
		this.dataGridvSwitch.TabIndex = 2;
		this.vSwitchHost.Frozen = true;
		this.vSwitchHost.HeaderText = "Host";
		this.vSwitchHost.MinimumWidth = 6;
		this.vSwitchHost.Name = "vSwitchHost";
		this.vSwitchHost.ReadOnly = true;
		this.vSwitchHost.ToolTipText = "Host name";
		this.vSwitchHost.Width = 125;
		this.vSwitchDatacenter.HeaderText = "Datacenter";
		this.vSwitchDatacenter.MinimumWidth = 6;
		this.vSwitchDatacenter.Name = "vSwitchDatacenter";
		this.vSwitchDatacenter.ReadOnly = true;
		this.vSwitchDatacenter.ToolTipText = "Datacenter name";
		this.vSwitchDatacenter.Width = 125;
		this.vSwitchCluster.HeaderText = "Cluster";
		this.vSwitchCluster.MinimumWidth = 6;
		this.vSwitchCluster.Name = "vSwitchCluster";
		this.vSwitchCluster.ReadOnly = true;
		this.vSwitchCluster.ToolTipText = "Cluster name";
		this.vSwitchCluster.Width = 125;
		this.vSwitchName.HeaderText = "Switch";
		this.vSwitchName.MinimumWidth = 6;
		this.vSwitchName.Name = "vSwitchName";
		this.vSwitchName.ReadOnly = true;
		this.vSwitchName.ToolTipText = "Switch name";
		this.vSwitchName.Width = 125;
		dataGridViewCellStyle55.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle55.Format = "N0";
		dataGridViewCellStyle55.NullValue = null;
		this.vSwitchNumPorts.DefaultCellStyle = dataGridViewCellStyle55;
		this.vSwitchNumPorts.HeaderText = "# Ports";
		this.vSwitchNumPorts.MinimumWidth = 6;
		this.vSwitchNumPorts.Name = "vSwitchNumPorts";
		this.vSwitchNumPorts.ReadOnly = true;
		this.vSwitchNumPorts.ToolTipText = "The number of ports that this virtual switch is configured to use";
		this.vSwitchNumPorts.Width = 125;
		dataGridViewCellStyle56.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle56.Format = "N0";
		dataGridViewCellStyle56.NullValue = null;
		this.vSwitchFreePorts.DefaultCellStyle = dataGridViewCellStyle56;
		this.vSwitchFreePorts.HeaderText = "Free Ports";
		this.vSwitchFreePorts.MinimumWidth = 6;
		this.vSwitchFreePorts.Name = "vSwitchFreePorts";
		this.vSwitchFreePorts.ReadOnly = true;
		this.vSwitchFreePorts.ToolTipText = "The number of ports that are available on this virtual switch";
		this.vSwitchFreePorts.Width = 125;
		this.vSwitchAllowPromiscuous.HeaderText = "Promiscuous Mode";
		this.vSwitchAllowPromiscuous.MinimumWidth = 6;
		this.vSwitchAllowPromiscuous.Name = "vSwitchAllowPromiscuous";
		this.vSwitchAllowPromiscuous.ReadOnly = true;
		this.vSwitchAllowPromiscuous.ToolTipText = "Security: Allow Promiscuous Mode";
		this.vSwitchAllowPromiscuous.Width = 200;
		this.vSwitchMacChanges.HeaderText = "Mac Changes";
		this.vSwitchMacChanges.MinimumWidth = 6;
		this.vSwitchMacChanges.Name = "vSwitchMacChanges";
		this.vSwitchMacChanges.ReadOnly = true;
		this.vSwitchMacChanges.ToolTipText = "Security: Allow MAC address Changes";
		this.vSwitchMacChanges.Width = 200;
		this.vSwitchForgedTransmits.HeaderText = "Forged Transmits";
		this.vSwitchForgedTransmits.MinimumWidth = 6;
		this.vSwitchForgedTransmits.Name = "vSwitchForgedTransmits";
		this.vSwitchForgedTransmits.ReadOnly = true;
		this.vSwitchForgedTransmits.ToolTipText = "Security: Allow Forged Transmits";
		this.vSwitchForgedTransmits.Width = 200;
		this.vSwitchTrafficShaping.HeaderText = "Traffic Shaping";
		this.vSwitchTrafficShaping.MinimumWidth = 6;
		this.vSwitchTrafficShaping.Name = "vSwitchTrafficShaping";
		this.vSwitchTrafficShaping.ReadOnly = true;
		this.vSwitchTrafficShaping.ToolTipText = "Flag to indicate whether or not traffic shaper is enabled on the port";
		this.vSwitchTrafficShaping.Width = 200;
		dataGridViewCellStyle57.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle57.Format = "N0";
		dataGridViewCellStyle57.NullValue = null;
		this.vSwitchAverageBandwidth.DefaultCellStyle = dataGridViewCellStyle57;
		this.vSwitchAverageBandwidth.HeaderText = "Width";
		this.vSwitchAverageBandwidth.MinimumWidth = 6;
		this.vSwitchAverageBandwidth.Name = "vSwitchAverageBandwidth";
		this.vSwitchAverageBandwidth.ReadOnly = true;
		this.vSwitchAverageBandwidth.ToolTipText = "Traffic Shaping: Average Bandwidth in Kbps";
		this.vSwitchAverageBandwidth.Width = 125;
		dataGridViewCellStyle58.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle58.Format = "N0";
		dataGridViewCellStyle58.NullValue = null;
		this.vSwitchPeakBandwidth.DefaultCellStyle = dataGridViewCellStyle58;
		this.vSwitchPeakBandwidth.HeaderText = "Peak";
		this.vSwitchPeakBandwidth.MinimumWidth = 6;
		this.vSwitchPeakBandwidth.Name = "vSwitchPeakBandwidth";
		this.vSwitchPeakBandwidth.ReadOnly = true;
		this.vSwitchPeakBandwidth.ToolTipText = "Traffic Shapping: Peak Bandwidth in Kbps";
		this.vSwitchPeakBandwidth.Width = 125;
		dataGridViewCellStyle59.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle59.Format = "N0";
		dataGridViewCellStyle59.NullValue = null;
		this.vSwitchBurstSize.DefaultCellStyle = dataGridViewCellStyle59;
		this.vSwitchBurstSize.HeaderText = "Burst";
		this.vSwitchBurstSize.MinimumWidth = 6;
		this.vSwitchBurstSize.Name = "vSwitchBurstSize";
		this.vSwitchBurstSize.ReadOnly = true;
		this.vSwitchBurstSize.ToolTipText = "Traffic Shaping: Burst size in KB";
		this.vSwitchBurstSize.Width = 125;
		this.vSwitchPolicy.HeaderText = "Policy";
		this.vSwitchPolicy.MinimumWidth = 6;
		this.vSwitchPolicy.Name = "vSwitchPolicy";
		this.vSwitchPolicy.ReadOnly = true;
		this.vSwitchPolicy.ToolTipText = "NIC Teaming: Network adapter teaming policy includes failover and load balancing";
		this.vSwitchPolicy.Width = 200;
		this.vSwitchReversePolicy.HeaderText = "Reverse Policy";
		this.vSwitchReversePolicy.MinimumWidth = 6;
		this.vSwitchReversePolicy.Name = "vSwitchReversePolicy";
		this.vSwitchReversePolicy.ReadOnly = true;
		this.vSwitchReversePolicy.ToolTipText = "NIC Teaming: The flag to indicate whether or not the teaming policy is applied to inbound frames as well";
		this.vSwitchReversePolicy.Width = 150;
		this.vSwitchNotifySwitches.HeaderText = "Notify Switch";
		this.vSwitchNotifySwitches.MinimumWidth = 6;
		this.vSwitchNotifySwitches.Name = "vSwitchNotifySwitches";
		this.vSwitchNotifySwitches.ReadOnly = true;
		this.vSwitchNotifySwitches.ToolTipText = "NIC Teaming: Notify Switches";
		this.vSwitchNotifySwitches.Width = 150;
		this.vSwitchRollingOrder.HeaderText = "Rolling Order";
		this.vSwitchRollingOrder.MinimumWidth = 6;
		this.vSwitchRollingOrder.Name = "vSwitchRollingOrder";
		this.vSwitchRollingOrder.ReadOnly = true;
		this.vSwitchRollingOrder.ToolTipText = "NIC Teaming: Flag to indicate whether or not to use a rolling policy when restoring links";
		this.vSwitchRollingOrder.Width = 150;
		this.vSwitchCsumOffload.HeaderText = "Offload";
		this.vSwitchCsumOffload.MinimumWidth = 6;
		this.vSwitchCsumOffload.Name = "vSwitchCsumOffload";
		this.vSwitchCsumOffload.ReadOnly = true;
		this.vSwitchCsumOffload.ToolTipText = "Flag to indicate whether or not checksum offloading is supported.";
		this.vSwitchCsumOffload.Width = 150;
		this.vSwitchTCPSegmentation.HeaderText = "TSO";
		this.vSwitchTCPSegmentation.MinimumWidth = 6;
		this.vSwitchTCPSegmentation.Name = "vSwitchTCPSegmentation";
		this.vSwitchTCPSegmentation.ReadOnly = true;
		this.vSwitchTCPSegmentation.ToolTipText = "Flag to indicate whether or not TCP segmentation offloading (TSO) is supported.";
		this.vSwitchTCPSegmentation.Width = 125;
		this.vSwitchZeroCopyXmit.HeaderText = "Zero Copy Xmit";
		this.vSwitchZeroCopyXmit.MinimumWidth = 6;
		this.vSwitchZeroCopyXmit.Name = "vSwitchZeroCopyXmit";
		this.vSwitchZeroCopyXmit.ReadOnly = true;
		this.vSwitchZeroCopyXmit.ToolTipText = "Flag to indicate whether or not zero copy transmits are supported";
		this.vSwitchZeroCopyXmit.Width = 200;
		dataGridViewCellStyle60.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle60.Format = "N0";
		dataGridViewCellStyle60.NullValue = null;
		this.vSwitchMTU.DefaultCellStyle = dataGridViewCellStyle60;
		this.vSwitchMTU.HeaderText = "MTU";
		this.vSwitchMTU.MinimumWidth = 6;
		this.vSwitchMTU.Name = "vSwitchMTU";
		this.vSwitchMTU.ReadOnly = true;
		this.vSwitchMTU.ToolTipText = "The maximum transmission unit (MTU) of the virtual switch in bytes";
		this.vSwitchMTU.Width = 125;
		this.vSwitchVISDKServer.HeaderText = "VI SDK Server";
		this.vSwitchVISDKServer.MinimumWidth = 6;
		this.vSwitchVISDKServer.Name = "vSwitchVISDKServer";
		this.vSwitchVISDKServer.ReadOnly = true;
		this.vSwitchVISDKServer.ToolTipText = "VI SDK Server, where RVTools gets it's information from";
		this.vSwitchVISDKServer.Width = 150;
		this.vSwitchInstanceUUID.HeaderText = "VI SDK UUID";
		this.vSwitchInstanceUUID.MinimumWidth = 6;
		this.vSwitchInstanceUUID.Name = "vSwitchInstanceUUID";
		this.vSwitchInstanceUUID.ReadOnly = true;
		this.vSwitchInstanceUUID.ToolTipText = "A globally unique identifier associated with this service instance";
		this.vSwitchInstanceUUID.Width = 125;
		this.tabvNIC.Controls.Add(this.panel19);
		this.tabvNIC.Location = new System.Drawing.Point(4, 25);
		this.tabvNIC.Margin = new System.Windows.Forms.Padding(4);
		this.tabvNIC.Name = "tabvNIC";
		this.tabvNIC.Size = new System.Drawing.Size(1725, 391);
		this.tabvNIC.TabIndex = 24;
		this.tabvNIC.Text = "vNIC";
		this.tabvNIC.ToolTipText = "Physical Nic Info";
		this.tabvNIC.UseVisualStyleBackColor = true;
		this.panel19.Controls.Add(this.dataGridvNIC);
		this.panel19.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel19.Location = new System.Drawing.Point(0, 0);
		this.panel19.Margin = new System.Windows.Forms.Padding(4);
		this.panel19.Name = "panel19";
		this.panel19.Padding = new System.Windows.Forms.Padding(20, 18, 20, 18);
		this.panel19.Size = new System.Drawing.Size(1725, 391);
		this.panel19.TabIndex = 6;
		this.dataGridvNIC.AllowUserToAddRows = false;
		this.dataGridvNIC.AllowUserToDeleteRows = false;
		this.dataGridvNIC.AllowUserToOrderColumns = true;
		this.dataGridvNIC.AllowUserToResizeRows = false;
		dataGridViewCellStyle61.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
		this.dataGridvNIC.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle61;
		this.dataGridvNIC.BackgroundColor = System.Drawing.SystemColors.Window;
		this.dataGridvNIC.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
		this.dataGridvNIC.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGridvNIC.Columns.AddRange(this.vNicHostName, this.vNicDatacenter, this.vNicCluster, this.vNicDevice, this.vNicDriver, this.vNicSpeed, this.vNicDuplex, this.vNicMac, this.vNicSwitch, this.vNicUplinkPort, this.vNicPci, this.vNicWakeOn, this.vNICVISDKServer, this.vNICInstanceUUID);
		this.dataGridvNIC.Dock = System.Windows.Forms.DockStyle.Fill;
		this.dataGridvNIC.Location = new System.Drawing.Point(20, 18);
		this.dataGridvNIC.Margin = new System.Windows.Forms.Padding(4);
		this.dataGridvNIC.Name = "dataGridvNIC";
		this.dataGridvNIC.RowHeadersVisible = false;
		this.dataGridvNIC.RowHeadersWidth = 51;
		this.dataGridvNIC.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this.dataGridvNIC.Size = new System.Drawing.Size(1685, 355);
		this.dataGridvNIC.TabIndex = 4;
		this.vNicHostName.Frozen = true;
		this.vNicHostName.HeaderText = "Host";
		this.vNicHostName.MinimumWidth = 6;
		this.vNicHostName.Name = "vNicHostName";
		this.vNicHostName.ReadOnly = true;
		this.vNicHostName.ToolTipText = "Host name";
		this.vNicHostName.Width = 125;
		this.vNicDatacenter.HeaderText = "Datacenter";
		this.vNicDatacenter.MinimumWidth = 6;
		this.vNicDatacenter.Name = "vNicDatacenter";
		this.vNicDatacenter.ReadOnly = true;
		this.vNicDatacenter.ToolTipText = "Datacenter name";
		this.vNicDatacenter.Width = 125;
		this.vNicCluster.HeaderText = "Cluster";
		this.vNicCluster.MinimumWidth = 6;
		this.vNicCluster.Name = "vNicCluster";
		this.vNicCluster.ReadOnly = true;
		this.vNicCluster.ToolTipText = "Cluster name";
		this.vNicCluster.Width = 125;
		this.vNicDevice.HeaderText = "Network Device";
		this.vNicDevice.MinimumWidth = 6;
		this.vNicDevice.Name = "vNicDevice";
		this.vNicDevice.ReadOnly = true;
		this.vNicDevice.ToolTipText = "The device name of the physical network adapter";
		this.vNicDevice.Width = 150;
		this.vNicDriver.HeaderText = "Driver";
		this.vNicDriver.MinimumWidth = 6;
		this.vNicDriver.Name = "vNicDriver";
		this.vNicDriver.ReadOnly = true;
		this.vNicDriver.ToolTipText = "The name of the driver";
		this.vNicDriver.Width = 125;
		dataGridViewCellStyle62.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle62.Format = "N0";
		dataGridViewCellStyle62.NullValue = null;
		this.vNicSpeed.DefaultCellStyle = dataGridViewCellStyle62;
		this.vNicSpeed.HeaderText = "Speed";
		this.vNicSpeed.MinimumWidth = 6;
		this.vNicSpeed.Name = "vNicSpeed";
		this.vNicSpeed.ReadOnly = true;
		this.vNicSpeed.ToolTipText = "The current link state of the physical network adapter";
		this.vNicSpeed.Width = 125;
		this.vNicDuplex.HeaderText = "Duplex";
		this.vNicDuplex.MinimumWidth = 6;
		this.vNicDuplex.Name = "vNicDuplex";
		this.vNicDuplex.ReadOnly = true;
		this.vNicDuplex.Width = 125;
		this.vNicMac.HeaderText = "MAC";
		this.vNicMac.MinimumWidth = 6;
		this.vNicMac.Name = "vNicMac";
		this.vNicMac.ReadOnly = true;
		this.vNicMac.ToolTipText = "MAC address of the physical network adapter";
		this.vNicMac.Width = 125;
		this.vNicSwitch.HeaderText = "Switch";
		this.vNicSwitch.MinimumWidth = 6;
		this.vNicSwitch.Name = "vNicSwitch";
		this.vNicSwitch.ReadOnly = true;
		this.vNicSwitch.ToolTipText = "Name of virtual or distributed switch where nic is connected to.";
		this.vNicSwitch.Width = 125;
		this.vNicUplinkPort.HeaderText = "Uplink port";
		this.vNicUplinkPort.MinimumWidth = 6;
		this.vNicUplinkPort.Name = "vNicUplinkPort";
		this.vNicUplinkPort.ReadOnly = true;
		this.vNicUplinkPort.ToolTipText = "Name of Uplink Port";
		this.vNicUplinkPort.Width = 200;
		this.vNicPci.HeaderText = "PCI";
		this.vNicPci.MinimumWidth = 6;
		this.vNicPci.Name = "vNicPci";
		this.vNicPci.ReadOnly = true;
		this.vNicPci.ToolTipText = "Device hash of the PCI device corresponding to this physical network adapter";
		this.vNicPci.Width = 125;
		this.vNicWakeOn.HeaderText = "WakeOn";
		this.vNicWakeOn.MinimumWidth = 6;
		this.vNicWakeOn.Name = "vNicWakeOn";
		this.vNicWakeOn.ReadOnly = true;
		this.vNicWakeOn.ToolTipText = "Flag indicating whether the NIC is wake-on-LAN capable";
		this.vNicWakeOn.Width = 125;
		this.vNICVISDKServer.HeaderText = "VI SDK Server";
		this.vNICVISDKServer.MinimumWidth = 6;
		this.vNICVISDKServer.Name = "vNICVISDKServer";
		this.vNICVISDKServer.ReadOnly = true;
		this.vNICVISDKServer.ToolTipText = "VI SDK Server, where RVTools gets it's information from";
		this.vNICVISDKServer.Width = 150;
		this.vNICInstanceUUID.HeaderText = "VI SDK UUID";
		this.vNICInstanceUUID.MinimumWidth = 6;
		this.vNICInstanceUUID.Name = "vNICInstanceUUID";
		this.vNICInstanceUUID.ReadOnly = true;
		this.vNICInstanceUUID.ToolTipText = "A globally unique identifier associated with this service instance";
		this.vNICInstanceUUID.Width = 125;
		this.tabvHBA.Controls.Add(this.panel23);
		this.tabvHBA.Location = new System.Drawing.Point(4, 25);
		this.tabvHBA.Margin = new System.Windows.Forms.Padding(4);
		this.tabvHBA.Name = "tabvHBA";
		this.tabvHBA.Size = new System.Drawing.Size(1725, 391);
		this.tabvHBA.TabIndex = 28;
		this.tabvHBA.Text = "vHBA";
		this.tabvHBA.ToolTipText = "HBA Information";
		this.tabvHBA.UseVisualStyleBackColor = true;
		this.panel23.Controls.Add(this.dataGridvHBA);
		this.panel23.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel23.Location = new System.Drawing.Point(0, 0);
		this.panel23.Margin = new System.Windows.Forms.Padding(4);
		this.panel23.Name = "panel23";
		this.panel23.Padding = new System.Windows.Forms.Padding(20, 18, 20, 18);
		this.panel23.Size = new System.Drawing.Size(1725, 391);
		this.panel23.TabIndex = 7;
		this.dataGridvHBA.AllowUserToAddRows = false;
		this.dataGridvHBA.AllowUserToDeleteRows = false;
		this.dataGridvHBA.AllowUserToOrderColumns = true;
		this.dataGridvHBA.AllowUserToResizeRows = false;
		dataGridViewCellStyle63.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
		this.dataGridvHBA.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle63;
		this.dataGridvHBA.BackgroundColor = System.Drawing.SystemColors.Window;
		this.dataGridvHBA.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
		this.dataGridvHBA.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGridvHBA.Columns.AddRange(this.vHBAHostName, this.vHBADatacenter, this.vHBACluster, this.vHBADevice, this.vHBAType, this.vHBAStatus, this.vHBABus, this.vHBAPci, this.vHBADriver, this.vHBAModel, this.vHBAWorldWideName, this.vHBAVISDKServer, this.vHBAInstanceUUID);
		this.dataGridvHBA.Dock = System.Windows.Forms.DockStyle.Fill;
		this.dataGridvHBA.Location = new System.Drawing.Point(20, 18);
		this.dataGridvHBA.Margin = new System.Windows.Forms.Padding(4);
		this.dataGridvHBA.Name = "dataGridvHBA";
		this.dataGridvHBA.RowHeadersVisible = false;
		this.dataGridvHBA.RowHeadersWidth = 51;
		this.dataGridvHBA.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this.dataGridvHBA.Size = new System.Drawing.Size(1685, 355);
		this.dataGridvHBA.TabIndex = 4;
		this.vHBAHostName.HeaderText = "Host";
		this.vHBAHostName.MinimumWidth = 6;
		this.vHBAHostName.Name = "vHBAHostName";
		this.vHBAHostName.ReadOnly = true;
		this.vHBAHostName.ToolTipText = "Host name";
		this.vHBAHostName.Width = 125;
		this.vHBADatacenter.HeaderText = "Datacenter";
		this.vHBADatacenter.MinimumWidth = 6;
		this.vHBADatacenter.Name = "vHBADatacenter";
		this.vHBADatacenter.ReadOnly = true;
		this.vHBADatacenter.ToolTipText = "Datacenter name";
		this.vHBADatacenter.Width = 125;
		this.vHBACluster.HeaderText = "Cluster";
		this.vHBACluster.MinimumWidth = 6;
		this.vHBACluster.Name = "vHBACluster";
		this.vHBACluster.ReadOnly = true;
		this.vHBACluster.ToolTipText = "Cluster name";
		this.vHBACluster.Width = 125;
		this.vHBADevice.HeaderText = "Device";
		this.vHBADevice.MinimumWidth = 6;
		this.vHBADevice.Name = "vHBADevice";
		this.vHBADevice.ReadOnly = true;
		this.vHBADevice.ToolTipText = "The device name of host bus adapter";
		this.vHBADevice.Width = 125;
		this.vHBAType.HeaderText = "Type";
		this.vHBAType.MinimumWidth = 6;
		this.vHBAType.Name = "vHBAType";
		this.vHBAType.ReadOnly = true;
		this.vHBAType.ToolTipText = "Object type";
		this.vHBAType.Width = 125;
		this.vHBAStatus.HeaderText = "Status";
		this.vHBAStatus.MinimumWidth = 6;
		this.vHBAStatus.Name = "vHBAStatus";
		this.vHBAStatus.ReadOnly = true;
		this.vHBAStatus.ToolTipText = "The operational status of the adapter";
		this.vHBAStatus.Width = 125;
		this.vHBABus.HeaderText = "Bus";
		this.vHBABus.MinimumWidth = 6;
		this.vHBABus.Name = "vHBABus";
		this.vHBABus.ReadOnly = true;
		this.vHBABus.ToolTipText = "The host bus number";
		this.vHBABus.Width = 125;
		this.vHBAPci.HeaderText = "Pci";
		this.vHBAPci.MinimumWidth = 6;
		this.vHBAPci.Name = "vHBAPci";
		this.vHBAPci.ReadOnly = true;
		this.vHBAPci.ToolTipText = "The Peripheral Connect Interface";
		this.vHBAPci.Width = 125;
		this.vHBADriver.HeaderText = "Driver";
		this.vHBADriver.MinimumWidth = 6;
		this.vHBADriver.Name = "vHBADriver";
		this.vHBADriver.ReadOnly = true;
		this.vHBADriver.ToolTipText = "The name of the driver";
		this.vHBADriver.Width = 125;
		this.vHBAModel.HeaderText = "Model";
		this.vHBAModel.MinimumWidth = 6;
		this.vHBAModel.Name = "vHBAModel";
		this.vHBAModel.ReadOnly = true;
		this.vHBAModel.ToolTipText = "The model name of the host bus adapter";
		this.vHBAModel.Width = 125;
		this.vHBAWorldWideName.HeaderText = "WWN";
		this.vHBAWorldWideName.MinimumWidth = 6;
		this.vHBAWorldWideName.Name = "vHBAWorldWideName";
		this.vHBAWorldWideName.ReadOnly = true;
		this.vHBAWorldWideName.ToolTipText = "The world wide name for the adapter";
		this.vHBAWorldWideName.Width = 125;
		this.vHBAVISDKServer.HeaderText = "VI SDK Server";
		this.vHBAVISDKServer.MinimumWidth = 6;
		this.vHBAVISDKServer.Name = "vHBAVISDKServer";
		this.vHBAVISDKServer.ReadOnly = true;
		this.vHBAVISDKServer.ToolTipText = "VI SDK Server, where RVTools gets it's information from";
		this.vHBAVISDKServer.Width = 150;
		this.vHBAInstanceUUID.HeaderText = "VI SDK UUID";
		this.vHBAInstanceUUID.MinimumWidth = 6;
		this.vHBAInstanceUUID.Name = "vHBAInstanceUUID";
		this.vHBAInstanceUUID.ReadOnly = true;
		this.vHBAInstanceUUID.ToolTipText = "A globally unique identifier associated with this service instance";
		this.vHBAInstanceUUID.Width = 125;
		this.tabvHost.Controls.Add(this.panel14);
		this.tabvHost.Location = new System.Drawing.Point(4, 25);
		this.tabvHost.Margin = new System.Windows.Forms.Padding(4);
		this.tabvHost.Name = "tabvHost";
		this.tabvHost.Size = new System.Drawing.Size(1725, 391);
		this.tabvHost.TabIndex = 19;
		this.tabvHost.Text = "vHost";
		this.tabvHost.ToolTipText = "Host Information";
		this.tabvHost.UseVisualStyleBackColor = true;
		this.panel14.Controls.Add(this.dataGridvHost);
		this.panel14.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel14.Location = new System.Drawing.Point(0, 0);
		this.panel14.Margin = new System.Windows.Forms.Padding(4);
		this.panel14.Name = "panel14";
		this.panel14.Padding = new System.Windows.Forms.Padding(20, 18, 20, 18);
		this.panel14.Size = new System.Drawing.Size(1725, 391);
		this.panel14.TabIndex = 5;
		this.dataGridvHost.AllowUserToAddRows = false;
		this.dataGridvHost.AllowUserToDeleteRows = false;
		this.dataGridvHost.AllowUserToOrderColumns = true;
		this.dataGridvHost.AllowUserToResizeRows = false;
		dataGridViewCellStyle64.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
		this.dataGridvHost.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle64;
		this.dataGridvHost.BackgroundColor = System.Drawing.SystemColors.Window;
		this.dataGridvHost.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
		this.dataGridvHost.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGridvHost.Columns.AddRange(this.vHostName, this.vHostDatacenter, this.vHostCluster, this.vHostConfigStatus, this.vHostComplianceCheckState, this.vHostinMaintenanceMode, this.vHostinQuarantineMode, this.vHostvSANFaultDomainName, this.vHostCpuModel, this.vHostCpuMhz, this.vHostHTAvailable, this.vHostHTActive, this.vHostNumCpu, this.vHostCoresPerCPU, this.vHostNumCpuCores, this.vHostOverallCpuUsage, this.vHostMemorySize, this.vHostMemoryTieringType, this.vHostOverallMemoryUsage, this.vHostServiceConsoleReserved, this.vHostNumNics, this.vHostNumHBAs, this.vHostVMsTotal, this.vHostVMs, this.vHostVMsPerCore, this.vHostvCPUs, this.vHostVCPUsPerCore, this.vHostvRAM, this.vHostVMUsedMemory, this.vHostVMSwappedMemory, this.vHostVMBalloonedMemory, this.vHostVmotionSupported, this.vHostStorageVmotionSupported, this.vHostCurrentEVCModeKey, this.vHostMaxEVCModeKey, this.vHostAssignedLicenses, this.vHostATSHeartbeat, this.vHostATSLocking, this.vHostCurrentPolicy, this.vHostHardwareSupport, this.vHostHostPowerPolicyShortname, this.vHostFullName, this.vHostBootTime, this.vHostDNSServers, this.vHostDHCP, this.vHostDomainName, this.vHostDomainList, this.vHostSearchDomains, this.vHostNTPServers, this.vHostNTPD, this.vHostTZDescription, this.vHostTZName, this.vHostTZOffset, this.vHostVendor, this.vHostModel, this.vHostSerialNumber, this.vHostServiceTag, this.vHostOemSpecificString, this.vHostBiosVendor, this.vHostBiosVersion, this.vHostBiosDate, this.vHostCertificateIssuer, this.vHostCertificateStartDate, this.vHostCertificateExpiry, this.vHostCertificateStatus, this.vHostCertificateSubject, this.vHostObjectID, this.vHostUUID, this.vHostVISDKServer, this.vHostInstanceUUID);
		this.dataGridvHost.Dock = System.Windows.Forms.DockStyle.Fill;
		this.dataGridvHost.Location = new System.Drawing.Point(20, 18);
		this.dataGridvHost.Margin = new System.Windows.Forms.Padding(4);
		this.dataGridvHost.Name = "dataGridvHost";
		this.dataGridvHost.RowHeadersVisible = false;
		this.dataGridvHost.RowHeadersWidth = 51;
		this.dataGridvHost.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this.dataGridvHost.Size = new System.Drawing.Size(1685, 355);
		this.dataGridvHost.TabIndex = 2;
		this.tabvCluster.Controls.Add(this.panel25);
		this.tabvCluster.Location = new System.Drawing.Point(4, 25);
		this.tabvCluster.Margin = new System.Windows.Forms.Padding(4);
		this.tabvCluster.Name = "tabvCluster";
		this.tabvCluster.Size = new System.Drawing.Size(1725, 391);
		this.tabvCluster.TabIndex = 30;
		this.tabvCluster.Text = "vCluster";
		this.tabvCluster.ToolTipText = "Cluster information";
		this.tabvCluster.UseVisualStyleBackColor = true;
		this.panel25.Controls.Add(this.dataGridvCluster);
		this.panel25.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel25.Location = new System.Drawing.Point(0, 0);
		this.panel25.Margin = new System.Windows.Forms.Padding(4);
		this.panel25.Name = "panel25";
		this.panel25.Padding = new System.Windows.Forms.Padding(20, 18, 20, 18);
		this.panel25.Size = new System.Drawing.Size(1725, 391);
		this.panel25.TabIndex = 5;
		this.dataGridvCluster.AllowUserToAddRows = false;
		this.dataGridvCluster.AllowUserToDeleteRows = false;
		this.dataGridvCluster.AllowUserToOrderColumns = true;
		this.dataGridvCluster.AllowUserToResizeRows = false;
		dataGridViewCellStyle65.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
		this.dataGridvCluster.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle65;
		this.dataGridvCluster.BackgroundColor = System.Drawing.SystemColors.Window;
		this.dataGridvCluster.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
		this.dataGridvCluster.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGridvCluster.Columns.AddRange(this.vClusterName, this.vClusterConfigStatus, this.vClusterOverallStatus, this.vClusterNumHosts, this.vClusterNumEffectiveHosts, this.vClusterTotalCpu, this.vClusterNumCpuCores, this.vClusterNumCpuThreads, this.vClusterEffectiveCpu, this.vClusterTotalMemory, this.vClusterEffectiveMemory, this.vClusterNumVmotions, this.vClusterEnabled, this.vClusterFailoverLevel, this.vClusterAdmissionControlEnabled, this.vClusterHostMonitoring, this.vClusterHBDatastoreCandidatePolicy, this.vClusterIsolationResponse, this.vClusterRestartPriority, this.vClusterClusterSettings, this.vClusterMaxFailures, this.vClusterMaxFailureWindow, this.vClusterFailureInterval, this.vClusterMinUpTime, this.vClusterVmMonitoring, this.vClusterDRSEnabled, this.vClusterDRSDefaultVmBehavior, this.vClusterDRSvmotionRate, this.vClusterDpmEnabled, this.vClusterDefaultDpmBehavior, this.vClusterHostPowerActionRate, this.vClusterObjectID, this.vClusterVISDKServer, this.vClusterInstanceUUID);
		this.dataGridvCluster.Dock = System.Windows.Forms.DockStyle.Fill;
		this.dataGridvCluster.Location = new System.Drawing.Point(20, 18);
		this.dataGridvCluster.Margin = new System.Windows.Forms.Padding(4);
		this.dataGridvCluster.Name = "dataGridvCluster";
		this.dataGridvCluster.RowHeadersVisible = false;
		this.dataGridvCluster.RowHeadersWidth = 51;
		this.dataGridvCluster.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this.dataGridvCluster.Size = new System.Drawing.Size(1685, 355);
		this.dataGridvCluster.TabIndex = 2;
		this.vClusterName.Frozen = true;
		this.vClusterName.HeaderText = "Name";
		this.vClusterName.MinimumWidth = 6;
		this.vClusterName.Name = "vClusterName";
		this.vClusterName.ReadOnly = true;
		this.vClusterName.ToolTipText = "Cluster name";
		this.vClusterName.Width = 125;
		this.vClusterConfigStatus.HeaderText = "Config status";
		this.vClusterConfigStatus.MinimumWidth = 6;
		this.vClusterConfigStatus.Name = "vClusterConfigStatus";
		this.vClusterConfigStatus.ReadOnly = true;
		this.vClusterConfigStatus.ToolTipText = "Config status";
		this.vClusterConfigStatus.Width = 125;
		this.vClusterOverallStatus.HeaderText = "OverallStatus";
		this.vClusterOverallStatus.MinimumWidth = 6;
		this.vClusterOverallStatus.Name = "vClusterOverallStatus";
		this.vClusterOverallStatus.ReadOnly = true;
		this.vClusterOverallStatus.ToolTipText = "Overall alarm status";
		this.vClusterOverallStatus.Width = 125;
		dataGridViewCellStyle66.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle66.Format = "N0";
		dataGridViewCellStyle66.NullValue = null;
		this.vClusterNumHosts.DefaultCellStyle = dataGridViewCellStyle66;
		this.vClusterNumHosts.HeaderText = "NumHosts";
		this.vClusterNumHosts.MinimumWidth = 6;
		this.vClusterNumHosts.Name = "vClusterNumHosts";
		this.vClusterNumHosts.ReadOnly = true;
		this.vClusterNumHosts.ToolTipText = "Total number of hosts.";
		this.vClusterNumHosts.Width = 125;
		dataGridViewCellStyle67.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle67.Format = "N0";
		dataGridViewCellStyle67.NullValue = null;
		this.vClusterNumEffectiveHosts.DefaultCellStyle = dataGridViewCellStyle67;
		this.vClusterNumEffectiveHosts.HeaderText = "numEffectiveHosts";
		this.vClusterNumEffectiveHosts.MinimumWidth = 6;
		this.vClusterNumEffectiveHosts.Name = "vClusterNumEffectiveHosts";
		this.vClusterNumEffectiveHosts.ReadOnly = true;
		this.vClusterNumEffectiveHosts.ToolTipText = "Total number of effective hosts.";
		this.vClusterNumEffectiveHosts.Width = 150;
		dataGridViewCellStyle68.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle68.Format = "N0";
		dataGridViewCellStyle68.NullValue = null;
		this.vClusterTotalCpu.DefaultCellStyle = dataGridViewCellStyle68;
		this.vClusterTotalCpu.HeaderText = "TotalCpu";
		this.vClusterTotalCpu.MinimumWidth = 6;
		this.vClusterTotalCpu.Name = "vClusterTotalCpu";
		this.vClusterTotalCpu.ReadOnly = true;
		this.vClusterTotalCpu.ToolTipText = "Aggregated CPU resources of all hosts, in MHz";
		this.vClusterTotalCpu.Width = 125;
		dataGridViewCellStyle69.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle69.Format = "N0";
		dataGridViewCellStyle69.NullValue = null;
		this.vClusterNumCpuCores.DefaultCellStyle = dataGridViewCellStyle69;
		this.vClusterNumCpuCores.HeaderText = "NumCpuCores";
		this.vClusterNumCpuCores.MinimumWidth = 6;
		this.vClusterNumCpuCores.Name = "vClusterNumCpuCores";
		this.vClusterNumCpuCores.ReadOnly = true;
		this.vClusterNumCpuCores.ToolTipText = "Number of physical CPU cores. Physical CPU cores are the processors contained by a CPU package. ";
		this.vClusterNumCpuCores.Width = 150;
		dataGridViewCellStyle70.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle70.Format = "N0";
		dataGridViewCellStyle70.NullValue = null;
		this.vClusterNumCpuThreads.DefaultCellStyle = dataGridViewCellStyle70;
		this.vClusterNumCpuThreads.HeaderText = "NumCpuThreads";
		this.vClusterNumCpuThreads.MinimumWidth = 6;
		this.vClusterNumCpuThreads.Name = "vClusterNumCpuThreads";
		this.vClusterNumCpuThreads.ReadOnly = true;
		this.vClusterNumCpuThreads.ToolTipText = "Aggregated number of CPU threads";
		this.vClusterNumCpuThreads.Width = 150;
		dataGridViewCellStyle71.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle71.Format = "N0";
		dataGridViewCellStyle71.NullValue = null;
		this.vClusterEffectiveCpu.DefaultCellStyle = dataGridViewCellStyle71;
		this.vClusterEffectiveCpu.HeaderText = "Effective Cpu";
		this.vClusterEffectiveCpu.MinimumWidth = 6;
		this.vClusterEffectiveCpu.Name = "vClusterEffectiveCpu";
		this.vClusterEffectiveCpu.ReadOnly = true;
		this.vClusterEffectiveCpu.ToolTipText = "Effective CPU resources (in MHz) available to run virtual machines.";
		this.vClusterEffectiveCpu.Width = 150;
		dataGridViewCellStyle72.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle72.Format = "N0";
		dataGridViewCellStyle72.NullValue = null;
		this.vClusterTotalMemory.DefaultCellStyle = dataGridViewCellStyle72;
		this.vClusterTotalMemory.HeaderText = "TotalMemory";
		this.vClusterTotalMemory.MinimumWidth = 6;
		this.vClusterTotalMemory.Name = "vClusterTotalMemory";
		this.vClusterTotalMemory.ReadOnly = true;
		this.vClusterTotalMemory.ToolTipText = "Aggregated memory resources of all hosts, in MiB";
		this.vClusterTotalMemory.Width = 150;
		dataGridViewCellStyle73.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle73.Format = "N0";
		dataGridViewCellStyle73.NullValue = null;
		this.vClusterEffectiveMemory.DefaultCellStyle = dataGridViewCellStyle73;
		this.vClusterEffectiveMemory.HeaderText = "Effective Memory";
		this.vClusterEffectiveMemory.MinimumWidth = 6;
		this.vClusterEffectiveMemory.Name = "vClusterEffectiveMemory";
		this.vClusterEffectiveMemory.ReadOnly = true;
		this.vClusterEffectiveMemory.ToolTipText = "Effective memory resources (in MiB) available to run virtual machines.";
		this.vClusterEffectiveMemory.Width = 150;
		dataGridViewCellStyle74.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle74.Format = "N0";
		dataGridViewCellStyle74.NullValue = null;
		this.vClusterNumVmotions.DefaultCellStyle = dataGridViewCellStyle74;
		this.vClusterNumVmotions.HeaderText = "Num VMotions";
		this.vClusterNumVmotions.MinimumWidth = 6;
		this.vClusterNumVmotions.Name = "vClusterNumVmotions";
		this.vClusterNumVmotions.ReadOnly = true;
		this.vClusterNumVmotions.ToolTipText = "Total number of migrations with VMotion that have been done internal to this cluster";
		this.vClusterNumVmotions.Width = 150;
		this.vClusterEnabled.HeaderText = "HA enabled";
		this.vClusterEnabled.MinimumWidth = 6;
		this.vClusterEnabled.Name = "vClusterEnabled";
		this.vClusterEnabled.ReadOnly = true;
		this.vClusterEnabled.ToolTipText = "Flag to indicate whether or not vSphere HA feature is enabled. ";
		this.vClusterEnabled.Width = 150;
		dataGridViewCellStyle75.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle75.Format = "N0";
		dataGridViewCellStyle75.NullValue = null;
		this.vClusterFailoverLevel.DefaultCellStyle = dataGridViewCellStyle75;
		this.vClusterFailoverLevel.HeaderText = "Failover Level";
		this.vClusterFailoverLevel.MinimumWidth = 6;
		this.vClusterFailoverLevel.Name = "vClusterFailoverLevel";
		this.vClusterFailoverLevel.ReadOnly = true;
		this.vClusterFailoverLevel.ToolTipText = "Configured failover level";
		this.vClusterFailoverLevel.Width = 125;
		this.vClusterAdmissionControlEnabled.HeaderText = "AdmissionControlEnabled";
		this.vClusterAdmissionControlEnabled.MinimumWidth = 6;
		this.vClusterAdmissionControlEnabled.Name = "vClusterAdmissionControlEnabled";
		this.vClusterAdmissionControlEnabled.ReadOnly = true;
		this.vClusterAdmissionControlEnabled.ToolTipText = "Flag that determines whether strict admission control is enabled";
		this.vClusterAdmissionControlEnabled.Width = 200;
		this.vClusterHostMonitoring.HeaderText = "Host monitoring";
		this.vClusterHostMonitoring.MinimumWidth = 6;
		this.vClusterHostMonitoring.Name = "vClusterHostMonitoring";
		this.vClusterHostMonitoring.ReadOnly = true;
		this.vClusterHostMonitoring.ToolTipText = "Determines whether HA restarts virtual machines after a host fails.";
		this.vClusterHostMonitoring.Width = 150;
		this.vClusterHBDatastoreCandidatePolicy.HeaderText = "HB Datastore Candidate Policy";
		this.vClusterHBDatastoreCandidatePolicy.MinimumWidth = 6;
		this.vClusterHBDatastoreCandidatePolicy.Name = "vClusterHBDatastoreCandidatePolicy";
		this.vClusterHBDatastoreCandidatePolicy.ReadOnly = true;
		this.vClusterHBDatastoreCandidatePolicy.ToolTipText = "The policy on what datastores will be used by vCenter Server to choose heartbeat datastores.";
		this.vClusterHBDatastoreCandidatePolicy.Width = 250;
		this.vClusterIsolationResponse.HeaderText = "Isolation Response";
		this.vClusterIsolationResponse.MinimumWidth = 6;
		this.vClusterIsolationResponse.Name = "vClusterIsolationResponse";
		this.vClusterIsolationResponse.ReadOnly = true;
		this.vClusterIsolationResponse.ToolTipText = "Indicates whether or not the virtual machine should be powered off if a host determines that it is isolated from the rest of the compute resource. ";
		this.vClusterIsolationResponse.Width = 150;
		this.vClusterRestartPriority.HeaderText = "Restart Priority";
		this.vClusterRestartPriority.MinimumWidth = 6;
		this.vClusterRestartPriority.Name = "vClusterRestartPriority";
		this.vClusterRestartPriority.ReadOnly = true;
		this.vClusterRestartPriority.ToolTipText = "Restart priority for a virtual machine. ";
		this.vClusterRestartPriority.Width = 150;
		this.vClusterClusterSettings.HeaderText = "Cluster Settings";
		this.vClusterClusterSettings.MinimumWidth = 6;
		this.vClusterClusterSettings.Name = "vClusterClusterSettings";
		this.vClusterClusterSettings.ReadOnly = true;
		this.vClusterClusterSettings.ToolTipText = "Flag indicating whether to use the cluster settings or the per VM settings. ";
		this.vClusterClusterSettings.Width = 150;
		dataGridViewCellStyle76.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle76.Format = "N0";
		dataGridViewCellStyle76.NullValue = null;
		this.vClusterMaxFailures.DefaultCellStyle = dataGridViewCellStyle76;
		this.vClusterMaxFailures.HeaderText = "Max Failures";
		this.vClusterMaxFailures.MinimumWidth = 6;
		this.vClusterMaxFailures.Name = "vClusterMaxFailures";
		this.vClusterMaxFailures.ReadOnly = true;
		this.vClusterMaxFailures.ToolTipText = "Maximum number of failures and automated resets allowed";
		this.vClusterMaxFailures.Width = 150;
		dataGridViewCellStyle77.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle77.Format = "N0";
		dataGridViewCellStyle77.NullValue = null;
		this.vClusterMaxFailureWindow.DefaultCellStyle = dataGridViewCellStyle77;
		this.vClusterMaxFailureWindow.HeaderText = "Max Failure Window";
		this.vClusterMaxFailureWindow.MinimumWidth = 6;
		this.vClusterMaxFailureWindow.Name = "vClusterMaxFailureWindow";
		this.vClusterMaxFailureWindow.ReadOnly = true;
		this.vClusterMaxFailureWindow.ToolTipText = "The number of seconds for the window during which up to maxFailures resets can occur before automated responses stop. ";
		this.vClusterMaxFailureWindow.Width = 200;
		dataGridViewCellStyle78.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle78.Format = "N0";
		dataGridViewCellStyle78.NullValue = null;
		this.vClusterFailureInterval.DefaultCellStyle = dataGridViewCellStyle78;
		this.vClusterFailureInterval.HeaderText = "Failure Interval";
		this.vClusterFailureInterval.MinimumWidth = 6;
		this.vClusterFailureInterval.Name = "vClusterFailureInterval";
		this.vClusterFailureInterval.ReadOnly = true;
		this.vClusterFailureInterval.ToolTipText = "If no heartbeat has been received for at least the specified number of seconds, the virtual machine is declared as failed. ";
		this.vClusterFailureInterval.Width = 150;
		dataGridViewCellStyle79.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle79.Format = "N0";
		dataGridViewCellStyle79.NullValue = null;
		this.vClusterMinUpTime.DefaultCellStyle = dataGridViewCellStyle79;
		this.vClusterMinUpTime.HeaderText = "Min Up Time";
		this.vClusterMinUpTime.MinimumWidth = 6;
		this.vClusterMinUpTime.Name = "vClusterMinUpTime";
		this.vClusterMinUpTime.ReadOnly = true;
		this.vClusterMinUpTime.ToolTipText = "The number of seconds for the virtual machine's heartbeats to stabilize after the virtual machine has been powered on";
		this.vClusterMinUpTime.Width = 150;
		this.vClusterVmMonitoring.HeaderText = "VM Monitoring";
		this.vClusterVmMonitoring.MinimumWidth = 6;
		this.vClusterVmMonitoring.Name = "vClusterVmMonitoring";
		this.vClusterVmMonitoring.ReadOnly = true;
		this.vClusterVmMonitoring.ToolTipText = "Indicates the type of virtual machine monitoring.";
		this.vClusterVmMonitoring.Width = 150;
		this.vClusterDRSEnabled.HeaderText = "DRS enabled";
		this.vClusterDRSEnabled.MinimumWidth = 6;
		this.vClusterDRSEnabled.Name = "vClusterDRSEnabled";
		this.vClusterDRSEnabled.ReadOnly = true;
		this.vClusterDRSEnabled.ToolTipText = "Flag indicating whether or not DRS is enabled";
		this.vClusterDRSEnabled.Width = 150;
		this.vClusterDRSDefaultVmBehavior.HeaderText = "DRS default VM behavior";
		this.vClusterDRSDefaultVmBehavior.MinimumWidth = 6;
		this.vClusterDRSDefaultVmBehavior.Name = "vClusterDRSDefaultVmBehavior";
		this.vClusterDRSDefaultVmBehavior.ReadOnly = true;
		this.vClusterDRSDefaultVmBehavior.ToolTipText = "Default DRS behavior for virtual machines";
		this.vClusterDRSDefaultVmBehavior.Width = 250;
		dataGridViewCellStyle80.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle80.Format = "N0";
		dataGridViewCellStyle80.NullValue = null;
		this.vClusterDRSvmotionRate.DefaultCellStyle = dataGridViewCellStyle80;
		this.vClusterDRSvmotionRate.HeaderText = "DRS vmotion rate";
		this.vClusterDRSvmotionRate.MinimumWidth = 6;
		this.vClusterDRSvmotionRate.Name = "vClusterDRSvmotionRate";
		this.vClusterDRSvmotionRate.ReadOnly = true;
		this.vClusterDRSvmotionRate.ToolTipText = "Threshold for generated ClusterRecommendations";
		this.vClusterDRSvmotionRate.Width = 200;
		this.vClusterDpmEnabled.HeaderText = "DPM enabled";
		this.vClusterDpmEnabled.MinimumWidth = 6;
		this.vClusterDpmEnabled.Name = "vClusterDpmEnabled";
		this.vClusterDpmEnabled.ReadOnly = true;
		this.vClusterDpmEnabled.ToolTipText = "Flag indicating whether or not the service is enabled. This service can not be enabled, unless DRS is enabled as well. ";
		this.vClusterDpmEnabled.Width = 150;
		this.vClusterDefaultDpmBehavior.HeaderText = "DPM default behavior";
		this.vClusterDefaultDpmBehavior.MinimumWidth = 6;
		this.vClusterDefaultDpmBehavior.Name = "vClusterDefaultDpmBehavior";
		this.vClusterDefaultDpmBehavior.ReadOnly = true;
		this.vClusterDefaultDpmBehavior.ToolTipText = "Specifies the default VMware DPM behavior for hosts.";
		this.vClusterDefaultDpmBehavior.Width = 250;
		dataGridViewCellStyle81.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle81.Format = "N0";
		dataGridViewCellStyle81.NullValue = null;
		this.vClusterHostPowerActionRate.DefaultCellStyle = dataGridViewCellStyle81;
		this.vClusterHostPowerActionRate.HeaderText = "DPM Host Power Action Rate";
		this.vClusterHostPowerActionRate.MinimumWidth = 6;
		this.vClusterHostPowerActionRate.Name = "vClusterHostPowerActionRate";
		this.vClusterHostPowerActionRate.ReadOnly = true;
		this.vClusterHostPowerActionRate.ToolTipText = "DPM generates only those recommendations that are above the specified rating. Ratings vary from 1 to 5.";
		this.vClusterHostPowerActionRate.Width = 300;
		this.vClusterObjectID.HeaderText = "Object ID";
		this.vClusterObjectID.MinimumWidth = 6;
		this.vClusterObjectID.Name = "vClusterObjectID";
		this.vClusterObjectID.ReadOnly = true;
		this.vClusterObjectID.ToolTipText = "Cluster object id";
		this.vClusterObjectID.Width = 125;
		this.vClusterVISDKServer.HeaderText = "VI SDK Server";
		this.vClusterVISDKServer.MinimumWidth = 6;
		this.vClusterVISDKServer.Name = "vClusterVISDKServer";
		this.vClusterVISDKServer.ReadOnly = true;
		this.vClusterVISDKServer.ToolTipText = "VI SDK Server, where RVTools gets it's information from";
		this.vClusterVISDKServer.Width = 150;
		this.vClusterInstanceUUID.HeaderText = "VI SDK UUID";
		this.vClusterInstanceUUID.MinimumWidth = 6;
		this.vClusterInstanceUUID.Name = "vClusterInstanceUUID";
		this.vClusterInstanceUUID.ReadOnly = true;
		this.vClusterInstanceUUID.ToolTipText = "A globally unique identifier associated with this service instance";
		this.vClusterInstanceUUID.Width = 125;
		this.tabvRP.Controls.Add(this.panel24);
		this.tabvRP.Location = new System.Drawing.Point(4, 25);
		this.tabvRP.Margin = new System.Windows.Forms.Padding(4);
		this.tabvRP.Name = "tabvRP";
		this.tabvRP.Size = new System.Drawing.Size(1725, 391);
		this.tabvRP.TabIndex = 29;
		this.tabvRP.Text = "vRP";
		this.tabvRP.ToolTipText = "Resourcepool info";
		this.tabvRP.UseVisualStyleBackColor = true;
		this.panel24.Controls.Add(this.dataGridvRP);
		this.panel24.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel24.Location = new System.Drawing.Point(0, 0);
		this.panel24.Margin = new System.Windows.Forms.Padding(4);
		this.panel24.Name = "panel24";
		this.panel24.Padding = new System.Windows.Forms.Padding(20, 18, 20, 18);
		this.panel24.Size = new System.Drawing.Size(1725, 391);
		this.panel24.TabIndex = 4;
		this.dataGridvRP.AllowUserToAddRows = false;
		this.dataGridvRP.AllowUserToDeleteRows = false;
		this.dataGridvRP.AllowUserToOrderColumns = true;
		this.dataGridvRP.AllowUserToResizeRows = false;
		dataGridViewCellStyle82.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
		this.dataGridvRP.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle82;
		this.dataGridvRP.BackgroundColor = System.Drawing.SystemColors.Window;
		this.dataGridvRP.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
		this.dataGridvRP.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGridvRP.Columns.AddRange(this.vRPName, this.vRPPath, this.vRPstatus, this.vRPVMsTotal, this.vRPVMs, this.vRPnumberOfCPUs, this.vRPCPUlimit, this.vRPCPUoverheadLimit, this.vRPCPUreservation, this.vRPCPUlevel, this.vRPCPUshares, this.vRPCPUexpandableReservation, this.vRPCPUmaxUsage, this.vRPCPUoverallUsage, this.vRPCPUreservationUsed, this.vRPCPUreservationUsedForVm, this.vRPCPUunreservedForPool, this.vRPCPUunreservedForVm, this.vRPconfiguredMemoryMiB, this.vRPMEMlimit, this.vRPMEMoverheadLimit, this.vRPMEMreservation, this.vRPMEMlevel, this.vRPMEMshares, this.vRPMEMexpandableReservation, this.vRPMEMmaxUsage, this.vRPMEMoverallUsage, this.vRPMEMreservationUsed, this.vRPMEMreservationUsedForVm, this.vRPMEMunreservedForPool, this.vRPMEMunreservedForVm, this.vRPoverallCpuDemand, this.vRPoverallCpuUsage, this.vRPstaticCpuEntitlement, this.vRPdistributedCpuEntitlement, this.vRPballoonedMemory, this.vRPcompressedMemory, this.vRPconsumedOverheadMemory, this.vRPdistributedMemoryEntitlement, this.vRPguestMemoryUsage, this.vRPhostMemoryUsage, this.vRPoverheadMemory, this.vRPprivateMemory, this.vRPsharedMemory, this.vRPstaticMemoryEntitlement, this.vRPswappedMemory, this.vRPObjectID, this.vRPVISDKServer, this.vRPInstanceUUID);
		this.dataGridvRP.Dock = System.Windows.Forms.DockStyle.Fill;
		this.dataGridvRP.Location = new System.Drawing.Point(20, 18);
		this.dataGridvRP.Margin = new System.Windows.Forms.Padding(4);
		this.dataGridvRP.Name = "dataGridvRP";
		this.dataGridvRP.RowHeadersVisible = false;
		this.dataGridvRP.RowHeadersWidth = 51;
		this.dataGridvRP.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this.dataGridvRP.Size = new System.Drawing.Size(1685, 355);
		this.dataGridvRP.TabIndex = 2;
		this.vRPName.Frozen = true;
		this.vRPName.HeaderText = "Resource Pool name";
		this.vRPName.MinimumWidth = 6;
		this.vRPName.Name = "vRPName";
		this.vRPName.ReadOnly = true;
		this.vRPName.ToolTipText = "Resource Pool name";
		this.vRPName.Width = 250;
		this.vRPPath.HeaderText = "Resource Pool path";
		this.vRPPath.MinimumWidth = 6;
		this.vRPPath.Name = "vRPPath";
		this.vRPPath.ReadOnly = true;
		this.vRPPath.ToolTipText = "Resource Pool path";
		this.vRPPath.Width = 250;
		this.vRPstatus.HeaderText = "Status";
		this.vRPstatus.MinimumWidth = 6;
		this.vRPstatus.Name = "vRPstatus";
		this.vRPstatus.ReadOnly = true;
		this.vRPstatus.ToolTipText = "Overall health of the tree";
		this.vRPstatus.Width = 125;
		dataGridViewCellStyle83.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle83.Format = "N0";
		dataGridViewCellStyle83.NullValue = null;
		this.vRPVMsTotal.DefaultCellStyle = dataGridViewCellStyle83;
		this.vRPVMsTotal.HeaderText = "# VMs total";
		this.vRPVMsTotal.MinimumWidth = 6;
		this.vRPVMsTotal.Name = "vRPVMsTotal";
		this.vRPVMsTotal.ReadOnly = true;
		this.vRPVMsTotal.ToolTipText = "Total number of VMs in this resource pool";
		this.vRPVMsTotal.Width = 125;
		dataGridViewCellStyle84.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle84.Format = "N0";
		dataGridViewCellStyle84.NullValue = null;
		this.vRPVMs.DefaultCellStyle = dataGridViewCellStyle84;
		this.vRPVMs.HeaderText = "# VMs";
		this.vRPVMs.MinimumWidth = 6;
		this.vRPVMs.Name = "vRPVMs";
		this.vRPVMs.ReadOnly = true;
		this.vRPVMs.ToolTipText = "Number of running VMs in this resource pool";
		this.vRPVMs.Width = 125;
		dataGridViewCellStyle85.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle85.Format = "N0";
		dataGridViewCellStyle85.NullValue = null;
		this.vRPnumberOfCPUs.DefaultCellStyle = dataGridViewCellStyle85;
		this.vRPnumberOfCPUs.HeaderText = "# vCPUs";
		this.vRPnumberOfCPUs.MinimumWidth = 6;
		this.vRPnumberOfCPUs.Name = "vRPnumberOfCPUs";
		this.vRPnumberOfCPUs.ReadOnly = true;
		this.vRPnumberOfCPUs.ToolTipText = "Number of virtual cores running in this resource pool";
		this.vRPnumberOfCPUs.Width = 125;
		dataGridViewCellStyle86.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle86.Format = "N0";
		dataGridViewCellStyle86.NullValue = null;
		this.vRPCPUlimit.DefaultCellStyle = dataGridViewCellStyle86;
		this.vRPCPUlimit.HeaderText = "CPU limit";
		this.vRPCPUlimit.MinimumWidth = 6;
		this.vRPCPUlimit.Name = "vRPCPUlimit";
		this.vRPCPUlimit.ReadOnly = true;
		this.vRPCPUlimit.ToolTipText = "The utilization of a virtual machine/resource pool will not exceed this limit, even if there are available resources";
		this.vRPCPUlimit.Width = 150;
		dataGridViewCellStyle87.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle87.Format = "N0";
		dataGridViewCellStyle87.NullValue = null;
		this.vRPCPUoverheadLimit.DefaultCellStyle = dataGridViewCellStyle87;
		this.vRPCPUoverheadLimit.HeaderText = "CPU overheadLimit";
		this.vRPCPUoverheadLimit.MinimumWidth = 6;
		this.vRPCPUoverheadLimit.Name = "vRPCPUoverheadLimit";
		this.vRPCPUoverheadLimit.ReadOnly = true;
		this.vRPCPUoverheadLimit.ToolTipText = "The maximum allowed overhead memory";
		this.vRPCPUoverheadLimit.Width = 200;
		dataGridViewCellStyle88.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle88.Format = "N0";
		dataGridViewCellStyle88.NullValue = null;
		this.vRPCPUreservation.DefaultCellStyle = dataGridViewCellStyle88;
		this.vRPCPUreservation.HeaderText = "CPU reservation";
		this.vRPCPUreservation.MinimumWidth = 6;
		this.vRPCPUreservation.Name = "vRPCPUreservation";
		this.vRPCPUreservation.ReadOnly = true;
		this.vRPCPUreservation.ToolTipText = "Amount of resource that is guaranteed available to the virtual machine or resource pool";
		this.vRPCPUreservation.Width = 200;
		this.vRPCPUlevel.HeaderText = "CPU level";
		this.vRPCPUlevel.MinimumWidth = 6;
		this.vRPCPUlevel.Name = "vRPCPUlevel";
		this.vRPCPUlevel.ReadOnly = true;
		this.vRPCPUlevel.ToolTipText = "The allocation level";
		this.vRPCPUlevel.Width = 125;
		dataGridViewCellStyle89.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle89.Format = "N0";
		dataGridViewCellStyle89.NullValue = null;
		this.vRPCPUshares.DefaultCellStyle = dataGridViewCellStyle89;
		this.vRPCPUshares.HeaderText = "CPU shares";
		this.vRPCPUshares.MinimumWidth = 6;
		this.vRPCPUshares.Name = "vRPCPUshares";
		this.vRPCPUshares.ReadOnly = true;
		this.vRPCPUshares.ToolTipText = "The number of shares allocated";
		this.vRPCPUshares.Width = 125;
		this.vRPCPUexpandableReservation.HeaderText = "CPU expandableReservation";
		this.vRPCPUexpandableReservation.MinimumWidth = 6;
		this.vRPCPUexpandableReservation.Name = "vRPCPUexpandableReservation";
		this.vRPCPUexpandableReservation.ReadOnly = true;
		this.vRPCPUexpandableReservation.ToolTipText = "Expandable reservation flag";
		this.vRPCPUexpandableReservation.Width = 250;
		dataGridViewCellStyle90.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle90.Format = "N0";
		dataGridViewCellStyle90.NullValue = null;
		this.vRPCPUmaxUsage.DefaultCellStyle = dataGridViewCellStyle90;
		this.vRPCPUmaxUsage.HeaderText = "CPU maxUsage";
		this.vRPCPUmaxUsage.MinimumWidth = 6;
		this.vRPCPUmaxUsage.Name = "vRPCPUmaxUsage";
		this.vRPCPUmaxUsage.ReadOnly = true;
		this.vRPCPUmaxUsage.ToolTipText = "Current upper-bound on usage";
		this.vRPCPUmaxUsage.Width = 200;
		dataGridViewCellStyle91.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle91.Format = "N0";
		dataGridViewCellStyle91.NullValue = null;
		this.vRPCPUoverallUsage.DefaultCellStyle = dataGridViewCellStyle91;
		this.vRPCPUoverallUsage.HeaderText = "CPU overallUsage";
		this.vRPCPUoverallUsage.MinimumWidth = 6;
		this.vRPCPUoverallUsage.Name = "vRPCPUoverallUsage";
		this.vRPCPUoverallUsage.ReadOnly = true;
		this.vRPCPUoverallUsage.ToolTipText = "Close to real-time resource usage of all running child virtual machines, including virtual machines in child resource pools";
		this.vRPCPUoverallUsage.Width = 200;
		dataGridViewCellStyle92.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle92.Format = "N0";
		dataGridViewCellStyle92.NullValue = null;
		this.vRPCPUreservationUsed.DefaultCellStyle = dataGridViewCellStyle92;
		this.vRPCPUreservationUsed.HeaderText = "CPU reservationUsed";
		this.vRPCPUreservationUsed.MinimumWidth = 6;
		this.vRPCPUreservationUsed.Name = "vRPCPUreservationUsed";
		this.vRPCPUreservationUsed.ReadOnly = true;
		this.vRPCPUreservationUsed.ToolTipText = "Total amount of resources that have been used";
		this.vRPCPUreservationUsed.Width = 200;
		dataGridViewCellStyle93.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle93.Format = "N0";
		dataGridViewCellStyle93.NullValue = null;
		this.vRPCPUreservationUsedForVm.DefaultCellStyle = dataGridViewCellStyle93;
		this.vRPCPUreservationUsedForVm.HeaderText = "CPU reservationUsedForVm";
		this.vRPCPUreservationUsedForVm.MinimumWidth = 6;
		this.vRPCPUreservationUsedForVm.Name = "vRPCPUreservationUsedForVm";
		this.vRPCPUreservationUsedForVm.ReadOnly = true;
		this.vRPCPUreservationUsedForVm.ToolTipText = "Total amount of resources that have been used to satisfy the reservation requirements of running virtual machines in this resource pool or any of its child resource pools";
		this.vRPCPUreservationUsedForVm.Width = 300;
		dataGridViewCellStyle94.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle94.Format = "N0";
		dataGridViewCellStyle94.NullValue = null;
		this.vRPCPUunreservedForPool.DefaultCellStyle = dataGridViewCellStyle94;
		this.vRPCPUunreservedForPool.HeaderText = "CPU unreservedForPool";
		this.vRPCPUunreservedForPool.MinimumWidth = 6;
		this.vRPCPUunreservedForPool.Name = "vRPCPUunreservedForPool";
		this.vRPCPUunreservedForPool.ReadOnly = true;
		this.vRPCPUunreservedForPool.ToolTipText = "Total amount of resources available to satisfy a reservation for a child resource pool";
		this.vRPCPUunreservedForPool.Width = 300;
		dataGridViewCellStyle95.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle95.Format = "N0";
		dataGridViewCellStyle95.NullValue = null;
		this.vRPCPUunreservedForVm.DefaultCellStyle = dataGridViewCellStyle95;
		this.vRPCPUunreservedForVm.HeaderText = "CPU unreservedForVm";
		this.vRPCPUunreservedForVm.MinimumWidth = 6;
		this.vRPCPUunreservedForVm.Name = "vRPCPUunreservedForVm";
		this.vRPCPUunreservedForVm.ReadOnly = true;
		this.vRPCPUunreservedForVm.ToolTipText = "Total amount of resources available to satisfy a reservation for a child virtual machine";
		this.vRPCPUunreservedForVm.Width = 300;
		dataGridViewCellStyle96.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle96.Format = "N0";
		dataGridViewCellStyle96.NullValue = null;
		this.vRPconfiguredMemoryMiB.DefaultCellStyle = dataGridViewCellStyle96;
		this.vRPconfiguredMemoryMiB.HeaderText = "Mem Configured";
		this.vRPconfiguredMemoryMiB.MinimumWidth = 6;
		this.vRPconfiguredMemoryMiB.Name = "vRPconfiguredMemoryMiB";
		this.vRPconfiguredMemoryMiB.ReadOnly = true;
		this.vRPconfiguredMemoryMiB.ToolTipText = "Total configured memory of all virtual machines in the resource pool, in MiB.";
		this.vRPconfiguredMemoryMiB.Width = 200;
		dataGridViewCellStyle97.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle97.Format = "N0";
		dataGridViewCellStyle97.NullValue = null;
		this.vRPMEMlimit.DefaultCellStyle = dataGridViewCellStyle97;
		this.vRPMEMlimit.HeaderText = "Mem limit";
		this.vRPMEMlimit.MinimumWidth = 6;
		this.vRPMEMlimit.Name = "vRPMEMlimit";
		this.vRPMEMlimit.ReadOnly = true;
		this.vRPMEMlimit.ToolTipText = "The utilization of a virtual machine/resource pool will not exceed this limit";
		this.vRPMEMlimit.Width = 150;
		dataGridViewCellStyle98.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle98.Format = "N0";
		dataGridViewCellStyle98.NullValue = null;
		this.vRPMEMoverheadLimit.DefaultCellStyle = dataGridViewCellStyle98;
		this.vRPMEMoverheadLimit.HeaderText = "Mem overheadLimit";
		this.vRPMEMoverheadLimit.MinimumWidth = 6;
		this.vRPMEMoverheadLimit.Name = "vRPMEMoverheadLimit";
		this.vRPMEMoverheadLimit.ReadOnly = true;
		this.vRPMEMoverheadLimit.ToolTipText = "The maximum allowed overhead memory";
		this.vRPMEMoverheadLimit.Width = 200;
		dataGridViewCellStyle99.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle99.Format = "N0";
		dataGridViewCellStyle99.NullValue = null;
		this.vRPMEMreservation.DefaultCellStyle = dataGridViewCellStyle99;
		this.vRPMEMreservation.HeaderText = "Mem reservation";
		this.vRPMEMreservation.MinimumWidth = 6;
		this.vRPMEMreservation.Name = "vRPMEMreservation";
		this.vRPMEMreservation.ReadOnly = true;
		this.vRPMEMreservation.ToolTipText = "Amount of resource that is guaranteed available to the virtual machine or resource pool";
		this.vRPMEMreservation.Width = 150;
		this.vRPMEMlevel.HeaderText = "Mem level";
		this.vRPMEMlevel.MinimumWidth = 6;
		this.vRPMEMlevel.Name = "vRPMEMlevel";
		this.vRPMEMlevel.ReadOnly = true;
		this.vRPMEMlevel.ToolTipText = "The allocation level";
		this.vRPMEMlevel.Width = 125;
		dataGridViewCellStyle100.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle100.Format = "N0";
		dataGridViewCellStyle100.NullValue = null;
		this.vRPMEMshares.DefaultCellStyle = dataGridViewCellStyle100;
		this.vRPMEMshares.HeaderText = "Mem shares";
		this.vRPMEMshares.MinimumWidth = 6;
		this.vRPMEMshares.Name = "vRPMEMshares";
		this.vRPMEMshares.ReadOnly = true;
		this.vRPMEMshares.ToolTipText = "The number of shares allocated";
		this.vRPMEMshares.Width = 125;
		this.vRPMEMexpandableReservation.HeaderText = "Mem expandableReservation";
		this.vRPMEMexpandableReservation.MinimumWidth = 6;
		this.vRPMEMexpandableReservation.Name = "vRPMEMexpandableReservation";
		this.vRPMEMexpandableReservation.ReadOnly = true;
		this.vRPMEMexpandableReservation.ToolTipText = "Expandable reservation flag";
		this.vRPMEMexpandableReservation.Width = 300;
		dataGridViewCellStyle101.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle101.Format = "N0";
		dataGridViewCellStyle101.NullValue = null;
		this.vRPMEMmaxUsage.DefaultCellStyle = dataGridViewCellStyle101;
		this.vRPMEMmaxUsage.HeaderText = "Mem maxUsage";
		this.vRPMEMmaxUsage.MinimumWidth = 6;
		this.vRPMEMmaxUsage.Name = "vRPMEMmaxUsage";
		this.vRPMEMmaxUsage.ReadOnly = true;
		this.vRPMEMmaxUsage.ToolTipText = "Current upper-bound on usage in MiB";
		this.vRPMEMmaxUsage.Width = 200;
		dataGridViewCellStyle102.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle102.Format = "N0";
		dataGridViewCellStyle102.NullValue = null;
		this.vRPMEMoverallUsage.DefaultCellStyle = dataGridViewCellStyle102;
		this.vRPMEMoverallUsage.HeaderText = "Mem overallUsage";
		this.vRPMEMoverallUsage.MinimumWidth = 6;
		this.vRPMEMoverallUsage.Name = "vRPMEMoverallUsage";
		this.vRPMEMoverallUsage.ReadOnly = true;
		this.vRPMEMoverallUsage.ToolTipText = "Close to real-time resource usage of all running child virtual machines, including virtual machines in child resource pools in MiB";
		this.vRPMEMoverallUsage.Width = 200;
		dataGridViewCellStyle103.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle103.Format = "N0";
		dataGridViewCellStyle103.NullValue = null;
		this.vRPMEMreservationUsed.DefaultCellStyle = dataGridViewCellStyle103;
		this.vRPMEMreservationUsed.HeaderText = "Mem reservationUsed";
		this.vRPMEMreservationUsed.MinimumWidth = 6;
		this.vRPMEMreservationUsed.Name = "vRPMEMreservationUsed";
		this.vRPMEMreservationUsed.ReadOnly = true;
		this.vRPMEMreservationUsed.ToolTipText = "Total amount of resources that have been used in MiB";
		this.vRPMEMreservationUsed.Width = 250;
		dataGridViewCellStyle104.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle104.Format = "N0";
		dataGridViewCellStyle104.NullValue = null;
		this.vRPMEMreservationUsedForVm.DefaultCellStyle = dataGridViewCellStyle104;
		this.vRPMEMreservationUsedForVm.HeaderText = "Mem reservationUsedForVm";
		this.vRPMEMreservationUsedForVm.MinimumWidth = 6;
		this.vRPMEMreservationUsedForVm.Name = "vRPMEMreservationUsedForVm";
		this.vRPMEMreservationUsedForVm.ReadOnly = true;
		this.vRPMEMreservationUsedForVm.ToolTipText = "Total amount of resources that have been used to satisfy the reservation requirements in MiB";
		this.vRPMEMreservationUsedForVm.Width = 300;
		dataGridViewCellStyle105.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle105.Format = "N0";
		dataGridViewCellStyle105.NullValue = null;
		this.vRPMEMunreservedForPool.DefaultCellStyle = dataGridViewCellStyle105;
		this.vRPMEMunreservedForPool.HeaderText = "Mem unreservedForPool";
		this.vRPMEMunreservedForPool.MinimumWidth = 6;
		this.vRPMEMunreservedForPool.Name = "vRPMEMunreservedForPool";
		this.vRPMEMunreservedForPool.ReadOnly = true;
		this.vRPMEMunreservedForPool.ToolTipText = "Total amount of resources available to satisfy a reservation for a child resource pool in MiB";
		this.vRPMEMunreservedForPool.Width = 300;
		dataGridViewCellStyle106.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle106.Format = "N0";
		dataGridViewCellStyle106.NullValue = null;
		this.vRPMEMunreservedForVm.DefaultCellStyle = dataGridViewCellStyle106;
		this.vRPMEMunreservedForVm.HeaderText = "Mem unreservedForVm";
		this.vRPMEMunreservedForVm.MinimumWidth = 6;
		this.vRPMEMunreservedForVm.Name = "vRPMEMunreservedForVm";
		this.vRPMEMunreservedForVm.ReadOnly = true;
		this.vRPMEMunreservedForVm.ToolTipText = "Total amount of resources available to satisfy a reservation for a child virtual machine in MiB";
		this.vRPMEMunreservedForVm.Width = 300;
		dataGridViewCellStyle107.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle107.Format = "N0";
		dataGridViewCellStyle107.NullValue = null;
		this.vRPoverallCpuDemand.DefaultCellStyle = dataGridViewCellStyle107;
		this.vRPoverallCpuDemand.HeaderText = "QS overallCpuDemand";
		this.vRPoverallCpuDemand.MinimumWidth = 6;
		this.vRPoverallCpuDemand.Name = "vRPoverallCpuDemand";
		this.vRPoverallCpuDemand.ReadOnly = true;
		this.vRPoverallCpuDemand.ToolTipText = "Basic CPU performance statistics, in MHz for all VMs in this resource pool";
		this.vRPoverallCpuDemand.Width = 300;
		dataGridViewCellStyle108.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle108.Format = "N0";
		dataGridViewCellStyle108.NullValue = null;
		this.vRPoverallCpuUsage.DefaultCellStyle = dataGridViewCellStyle108;
		this.vRPoverallCpuUsage.HeaderText = "QS overallCpuUsage";
		this.vRPoverallCpuUsage.MinimumWidth = 6;
		this.vRPoverallCpuUsage.Name = "vRPoverallCpuUsage";
		this.vRPoverallCpuUsage.ReadOnly = true;
		this.vRPoverallCpuUsage.ToolTipText = "Basic CPU performance statistics, in MHz for all VMs in this resource pool";
		this.vRPoverallCpuUsage.Width = 300;
		dataGridViewCellStyle109.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle109.Format = "N0";
		dataGridViewCellStyle109.NullValue = null;
		this.vRPstaticCpuEntitlement.DefaultCellStyle = dataGridViewCellStyle109;
		this.vRPstaticCpuEntitlement.HeaderText = "QS staticCpuEntitlement";
		this.vRPstaticCpuEntitlement.MinimumWidth = 6;
		this.vRPstaticCpuEntitlement.Name = "vRPstaticCpuEntitlement";
		this.vRPstaticCpuEntitlement.ReadOnly = true;
		this.vRPstaticCpuEntitlement.ToolTipText = "The static CPU resource entitlement for all virtual machines in this resource pool";
		this.vRPstaticCpuEntitlement.Width = 300;
		dataGridViewCellStyle110.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle110.Format = "N0";
		dataGridViewCellStyle110.NullValue = null;
		this.vRPdistributedCpuEntitlement.DefaultCellStyle = dataGridViewCellStyle110;
		this.vRPdistributedCpuEntitlement.HeaderText = "QS distributedCpuEntitlement";
		this.vRPdistributedCpuEntitlement.MinimumWidth = 6;
		this.vRPdistributedCpuEntitlement.Name = "vRPdistributedCpuEntitlement";
		this.vRPdistributedCpuEntitlement.ReadOnly = true;
		this.vRPdistributedCpuEntitlement.ToolTipText = "This is the amount of CPU resource, in MHz, that all VMs in this resource pool are entitled to, as calculated by DRS";
		this.vRPdistributedCpuEntitlement.Width = 300;
		dataGridViewCellStyle111.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle111.Format = "N0";
		dataGridViewCellStyle111.NullValue = null;
		this.vRPballoonedMemory.DefaultCellStyle = dataGridViewCellStyle111;
		this.vRPballoonedMemory.HeaderText = "QS balloonedMemory";
		this.vRPballoonedMemory.MinimumWidth = 6;
		this.vRPballoonedMemory.Name = "vRPballoonedMemory";
		this.vRPballoonedMemory.ReadOnly = true;
		this.vRPballoonedMemory.ToolTipText = "The size of the balloon driver in a virtual machine, in MiB, in this resource pool";
		this.vRPballoonedMemory.Width = 300;
		dataGridViewCellStyle112.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle112.Format = "N0";
		dataGridViewCellStyle112.NullValue = null;
		this.vRPcompressedMemory.DefaultCellStyle = dataGridViewCellStyle112;
		this.vRPcompressedMemory.HeaderText = "QS compressedMemory";
		this.vRPcompressedMemory.MinimumWidth = 6;
		this.vRPcompressedMemory.Name = "vRPcompressedMemory";
		this.vRPcompressedMemory.ReadOnly = true;
		this.vRPcompressedMemory.ToolTipText = "The amount of compressed memory currently consumed by VM, in KiB, in this resource pool";
		this.vRPcompressedMemory.Width = 300;
		dataGridViewCellStyle113.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle113.Format = "N0";
		dataGridViewCellStyle113.NullValue = null;
		this.vRPconsumedOverheadMemory.DefaultCellStyle = dataGridViewCellStyle113;
		this.vRPconsumedOverheadMemory.HeaderText = "QS consumedOverheadMemory";
		this.vRPconsumedOverheadMemory.MinimumWidth = 6;
		this.vRPconsumedOverheadMemory.Name = "vRPconsumedOverheadMemory";
		this.vRPconsumedOverheadMemory.ReadOnly = true;
		this.vRPconsumedOverheadMemory.ToolTipText = "The amount of overhead memory, in MiB, currently being consumed";
		this.vRPconsumedOverheadMemory.Width = 300;
		dataGridViewCellStyle114.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle114.Format = "N0";
		dataGridViewCellStyle114.NullValue = null;
		this.vRPdistributedMemoryEntitlement.DefaultCellStyle = dataGridViewCellStyle114;
		this.vRPdistributedMemoryEntitlement.HeaderText = "QS distributedMemoryEntitlement";
		this.vRPdistributedMemoryEntitlement.MinimumWidth = 6;
		this.vRPdistributedMemoryEntitlement.Name = "vRPdistributedMemoryEntitlement";
		this.vRPdistributedMemoryEntitlement.ReadOnly = true;
		this.vRPdistributedMemoryEntitlement.ToolTipText = "This is the amount of memory resource, in MiB, that the VMs in this resource pool are entitled to";
		this.vRPdistributedMemoryEntitlement.Width = 300;
		dataGridViewCellStyle115.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle115.Format = "N0";
		dataGridViewCellStyle115.NullValue = null;
		this.vRPguestMemoryUsage.DefaultCellStyle = dataGridViewCellStyle115;
		this.vRPguestMemoryUsage.HeaderText = "QS guestMemoryUsage";
		this.vRPguestMemoryUsage.MinimumWidth = 6;
		this.vRPguestMemoryUsage.Name = "vRPguestMemoryUsage";
		this.vRPguestMemoryUsage.ReadOnly = true;
		this.vRPguestMemoryUsage.ToolTipText = "Guest memory utilization statistics, in MiB";
		this.vRPguestMemoryUsage.Width = 300;
		dataGridViewCellStyle116.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle116.Format = "N0";
		dataGridViewCellStyle116.NullValue = null;
		this.vRPhostMemoryUsage.DefaultCellStyle = dataGridViewCellStyle116;
		this.vRPhostMemoryUsage.HeaderText = "QS hostMemoryUsage";
		this.vRPhostMemoryUsage.MinimumWidth = 6;
		this.vRPhostMemoryUsage.Name = "vRPhostMemoryUsage";
		this.vRPhostMemoryUsage.ReadOnly = true;
		this.vRPhostMemoryUsage.ToolTipText = "Host memory utilization statistics, in MiB";
		this.vRPhostMemoryUsage.Width = 300;
		dataGridViewCellStyle117.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle117.Format = "N0";
		dataGridViewCellStyle117.NullValue = null;
		this.vRPoverheadMemory.DefaultCellStyle = dataGridViewCellStyle117;
		this.vRPoverheadMemory.HeaderText = "QS overheadMemory";
		this.vRPoverheadMemory.MinimumWidth = 6;
		this.vRPoverheadMemory.Name = "vRPoverheadMemory";
		this.vRPoverheadMemory.ReadOnly = true;
		this.vRPoverheadMemory.ToolTipText = "The amount of memory resource (in MiB) that will be used by a virtual machine above its guest memory requirements";
		this.vRPoverheadMemory.Width = 300;
		dataGridViewCellStyle118.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle118.Format = "N0";
		dataGridViewCellStyle118.NullValue = null;
		this.vRPprivateMemory.DefaultCellStyle = dataGridViewCellStyle118;
		this.vRPprivateMemory.HeaderText = "QS privateMemory";
		this.vRPprivateMemory.MinimumWidth = 6;
		this.vRPprivateMemory.Name = "vRPprivateMemory";
		this.vRPprivateMemory.ReadOnly = true;
		this.vRPprivateMemory.ToolTipText = "The portion of memory, in MiB, that is granted to a virtual machine from non-shared host memory";
		this.vRPprivateMemory.Width = 200;
		dataGridViewCellStyle119.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle119.Format = "N0";
		dataGridViewCellStyle119.NullValue = null;
		this.vRPsharedMemory.DefaultCellStyle = dataGridViewCellStyle119;
		this.vRPsharedMemory.HeaderText = "QS sharedMemory";
		this.vRPsharedMemory.MinimumWidth = 6;
		this.vRPsharedMemory.Name = "vRPsharedMemory";
		this.vRPsharedMemory.ReadOnly = true;
		this.vRPsharedMemory.ToolTipText = "The portion of memory, in MiB, that is granted to a virtual machine from host memory that is shared between VMs. ";
		this.vRPsharedMemory.Width = 200;
		dataGridViewCellStyle120.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle120.Format = "N0";
		dataGridViewCellStyle120.NullValue = null;
		this.vRPstaticMemoryEntitlement.DefaultCellStyle = dataGridViewCellStyle120;
		this.vRPstaticMemoryEntitlement.HeaderText = "QS staticMemoryEntitlement";
		this.vRPstaticMemoryEntitlement.MinimumWidth = 6;
		this.vRPstaticMemoryEntitlement.Name = "vRPstaticMemoryEntitlement";
		this.vRPstaticMemoryEntitlement.ReadOnly = true;
		this.vRPstaticMemoryEntitlement.ToolTipText = "The static memory resource entitlement";
		this.vRPstaticMemoryEntitlement.Width = 300;
		dataGridViewCellStyle121.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle121.Format = "N0";
		dataGridViewCellStyle121.NullValue = null;
		this.vRPswappedMemory.DefaultCellStyle = dataGridViewCellStyle121;
		this.vRPswappedMemory.HeaderText = "QS swappedMemory";
		this.vRPswappedMemory.MinimumWidth = 6;
		this.vRPswappedMemory.Name = "vRPswappedMemory";
		this.vRPswappedMemory.ReadOnly = true;
		this.vRPswappedMemory.ToolTipText = "The portion of memory, in MiB, that is granted to a virtual machine from the host's swap space";
		this.vRPswappedMemory.Width = 250;
		this.vRPObjectID.HeaderText = "Object ID";
		this.vRPObjectID.MinimumWidth = 6;
		this.vRPObjectID.Name = "vRPObjectID";
		this.vRPObjectID.ReadOnly = true;
		this.vRPObjectID.ToolTipText = "Resource pool object id";
		this.vRPObjectID.Width = 125;
		this.vRPVISDKServer.HeaderText = "VI SDK Server";
		this.vRPVISDKServer.MinimumWidth = 6;
		this.vRPVISDKServer.Name = "vRPVISDKServer";
		this.vRPVISDKServer.ReadOnly = true;
		this.vRPVISDKServer.ToolTipText = "VI SDK Server, where RVTools gets it's information from";
		this.vRPVISDKServer.Width = 200;
		this.vRPInstanceUUID.HeaderText = "VI SDK UUID";
		this.vRPInstanceUUID.MinimumWidth = 6;
		this.vRPInstanceUUID.Name = "vRPInstanceUUID";
		this.vRPInstanceUUID.ReadOnly = true;
		this.vRPInstanceUUID.ToolTipText = "A globally unique identifier associated with this service instance";
		this.vRPInstanceUUID.Width = 200;
		this.tabvTools.Controls.Add(this.panel10);
		this.tabvTools.Controls.Add(this.panel11);
		this.tabvTools.Location = new System.Drawing.Point(4, 25);
		this.tabvTools.Margin = new System.Windows.Forms.Padding(4);
		this.tabvTools.Name = "tabvTools";
		this.tabvTools.Size = new System.Drawing.Size(1725, 391);
		this.tabvTools.TabIndex = 17;
		this.tabvTools.Text = "vTools";
		this.tabvTools.ToolTipText = "VMware Tools Information";
		this.tabvTools.UseVisualStyleBackColor = true;
		this.panel10.Controls.Add(this.dataGridvTools);
		this.panel10.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel10.Location = new System.Drawing.Point(0, 0);
		this.panel10.Margin = new System.Windows.Forms.Padding(0);
		this.panel10.Name = "panel10";
		this.panel10.Padding = new System.Windows.Forms.Padding(20, 18, 20, 6);
		this.panel10.Size = new System.Drawing.Size(1725, 328);
		this.panel10.TabIndex = 3;
		this.dataGridvTools.AllowUserToAddRows = false;
		this.dataGridvTools.AllowUserToDeleteRows = false;
		this.dataGridvTools.AllowUserToOrderColumns = true;
		this.dataGridvTools.AllowUserToResizeRows = false;
		dataGridViewCellStyle122.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
		this.dataGridvTools.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle122;
		this.dataGridvTools.BackgroundColor = System.Drawing.SystemColors.Window;
		this.dataGridvTools.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
		this.dataGridvTools.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGridvTools.Columns.AddRange(this.vToolsUpgrade, this.vToolsVMName, this.vToolsPowerstate, this.vToolsTemplate, this.vToolsSRMPlaceholder, this.vToolsVMVersion, this.vToolsTools, this.vToolsVersion, this.vToolsRequiredVersion, this.vToolsUpgradeable, this.vToolsUpgradePolicy, this.vToolsSyncTime, this.vToolsAppState, this.vToolsAppHeartbeatStatus, this.vToolsKernelCrashed, this.vToolsOperationsReady, this.vToolsStateChangeSupported, this.vToolsInteractiveOperationsReady, this.vToolsNotes, this.vToolsDatacenter, this.vToolsCluster, this.vToolsHost, this.vToolsFolder, this.vToolsOS, this.vToolsguestFullName, this.vToolsVMRef, this.vToolsObjectID, this.vToolsUUID, this.vToolsVISDKServer, this.vToolsInstanceUUID);
		this.dataGridvTools.Dock = System.Windows.Forms.DockStyle.Fill;
		this.dataGridvTools.Location = new System.Drawing.Point(20, 18);
		this.dataGridvTools.Margin = new System.Windows.Forms.Padding(4);
		this.dataGridvTools.Name = "dataGridvTools";
		this.dataGridvTools.RowHeadersVisible = false;
		this.dataGridvTools.RowHeadersWidth = 51;
		this.dataGridvTools.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this.dataGridvTools.Size = new System.Drawing.Size(1685, 304);
		this.dataGridvTools.TabIndex = 0;
		this.dataGridvTools.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridvTools_CellContentClick);
		this.vToolsUpgrade.FalseValue = "0";
		this.vToolsUpgrade.Frozen = true;
		this.vToolsUpgrade.HeaderText = "Upgrade";
		this.vToolsUpgrade.MinimumWidth = 6;
		this.vToolsUpgrade.Name = "vToolsUpgrade";
		this.vToolsUpgrade.ToolTipText = "Only selectable when Upgradeable is True!";
		this.vToolsUpgrade.TrueValue = "1";
		this.vToolsUpgrade.Width = 54;
		this.vToolsVMName.Frozen = true;
		this.vToolsVMName.HeaderText = "VM";
		this.vToolsVMName.MinimumWidth = 6;
		this.vToolsVMName.Name = "vToolsVMName";
		this.vToolsVMName.ReadOnly = true;
		this.vToolsVMName.ToolTipText = "Virtual Machine Name";
		this.vToolsVMName.Width = 48;
		this.vToolsPowerstate.HeaderText = "Powerstate";
		this.vToolsPowerstate.MinimumWidth = 6;
		this.vToolsPowerstate.Name = "vToolsPowerstate";
		this.vToolsPowerstate.ReadOnly = true;
		this.vToolsPowerstate.ToolTipText = "Power state";
		this.vToolsPowerstate.Width = 85;
		this.vToolsTemplate.HeaderText = "Template";
		this.vToolsTemplate.MinimumWidth = 6;
		this.vToolsTemplate.Name = "vToolsTemplate";
		this.vToolsTemplate.ReadOnly = true;
		this.vToolsTemplate.ToolTipText = "Is VM a template?";
		this.vToolsTemplate.Width = 76;
		this.vToolsSRMPlaceholder.HeaderText = "SRM Placeholder";
		this.vToolsSRMPlaceholder.MinimumWidth = 6;
		this.vToolsSRMPlaceholder.Name = "vToolsSRMPlaceholder";
		this.vToolsSRMPlaceholder.ReadOnly = true;
		this.vToolsSRMPlaceholder.ToolTipText = "Is VM a SRM Placeholder?";
		this.vToolsSRMPlaceholder.Width = 200;
		dataGridViewCellStyle123.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle123.Format = "N0";
		dataGridViewCellStyle123.NullValue = null;
		this.vToolsVMVersion.DefaultCellStyle = dataGridViewCellStyle123;
		this.vToolsVMVersion.HeaderText = "VM Version";
		this.vToolsVMVersion.MinimumWidth = 6;
		this.vToolsVMVersion.Name = "vToolsVMVersion";
		this.vToolsVMVersion.ReadOnly = true;
		this.vToolsVMVersion.ToolTipText = "Virtual machine hardware version.";
		this.vToolsVMVersion.Width = 125;
		this.vToolsTools.HeaderText = "Tools";
		this.vToolsTools.MinimumWidth = 6;
		this.vToolsTools.Name = "vToolsTools";
		this.vToolsTools.ReadOnly = true;
		this.vToolsTools.ToolTipText = "Tools Status";
		this.vToolsTools.Width = 58;
		dataGridViewCellStyle124.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle124.Format = "N0";
		dataGridViewCellStyle124.NullValue = null;
		this.vToolsVersion.DefaultCellStyle = dataGridViewCellStyle124;
		this.vToolsVersion.HeaderText = "Tools Version";
		this.vToolsVersion.MinimumWidth = 6;
		this.vToolsVersion.Name = "vToolsVersion";
		this.vToolsVersion.ReadOnly = true;
		this.vToolsVersion.Width = 96;
		dataGridViewCellStyle125.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle125.Format = "N0";
		dataGridViewCellStyle125.NullValue = null;
		this.vToolsRequiredVersion.DefaultCellStyle = dataGridViewCellStyle125;
		this.vToolsRequiredVersion.HeaderText = "Required Version";
		this.vToolsRequiredVersion.MinimumWidth = 6;
		this.vToolsRequiredVersion.Name = "vToolsRequiredVersion";
		this.vToolsRequiredVersion.ReadOnly = true;
		this.vToolsRequiredVersion.Width = 150;
		this.vToolsUpgradeable.HeaderText = "Upgradeable";
		this.vToolsUpgradeable.MinimumWidth = 6;
		this.vToolsUpgradeable.Name = "vToolsUpgradeable";
		this.vToolsUpgradeable.ReadOnly = true;
		this.vToolsUpgradeable.ToolTipText = "Can we automatically upgrade?";
		this.vToolsUpgradeable.Width = 93;
		this.vToolsUpgradePolicy.HeaderText = "Upgrade Policy";
		this.vToolsUpgradePolicy.MinimumWidth = 6;
		this.vToolsUpgradePolicy.Name = "vToolsUpgradePolicy";
		this.vToolsUpgradePolicy.ReadOnly = true;
		this.vToolsUpgradePolicy.ToolTipText = "Tools upgrade policy setting for the virtual machine.";
		this.vToolsUpgradePolicy.Width = 200;
		this.vToolsSyncTime.HeaderText = "Sync time";
		this.vToolsSyncTime.MinimumWidth = 6;
		this.vToolsSyncTime.Name = "vToolsSyncTime";
		this.vToolsSyncTime.ReadOnly = true;
		this.vToolsSyncTime.ToolTipText = "Indicates whether or not the tools program will sync time with the host time. ";
		this.vToolsSyncTime.Width = 125;
		this.vToolsAppState.HeaderText = "App status";
		this.vToolsAppState.MinimumWidth = 6;
		this.vToolsAppState.Name = "vToolsAppState";
		this.vToolsAppState.ReadOnly = true;
		this.vToolsAppState.ToolTipText = "Application state";
		this.vToolsAppState.Width = 125;
		this.vToolsAppHeartbeatStatus.HeaderText = "Heartbeat status";
		this.vToolsAppHeartbeatStatus.MinimumWidth = 6;
		this.vToolsAppHeartbeatStatus.Name = "vToolsAppHeartbeatStatus";
		this.vToolsAppHeartbeatStatus.ReadOnly = true;
		this.vToolsAppHeartbeatStatus.ToolTipText = "Application heartbeat status";
		this.vToolsAppHeartbeatStatus.Width = 200;
		this.vToolsKernelCrashed.HeaderText = "Kernel Crash state";
		this.vToolsKernelCrashed.MinimumWidth = 6;
		this.vToolsKernelCrashed.Name = "vToolsKernelCrashed";
		this.vToolsKernelCrashed.ReadOnly = true;
		this.vToolsKernelCrashed.ToolTipText = "Guest operating system's kernel crash state";
		this.vToolsKernelCrashed.Width = 200;
		this.vToolsOperationsReady.HeaderText = "Operation Ready";
		this.vToolsOperationsReady.MinimumWidth = 6;
		this.vToolsOperationsReady.Name = "vToolsOperationsReady";
		this.vToolsOperationsReady.ReadOnly = true;
		this.vToolsOperationsReady.ToolTipText = "Guest Operations availability";
		this.vToolsOperationsReady.Width = 200;
		this.vToolsStateChangeSupported.HeaderText = "State change support";
		this.vToolsStateChangeSupported.MinimumWidth = 6;
		this.vToolsStateChangeSupported.Name = "vToolsStateChangeSupported";
		this.vToolsStateChangeSupported.ReadOnly = true;
		this.vToolsStateChangeSupported.ToolTipText = "State change support";
		this.vToolsStateChangeSupported.Width = 200;
		this.vToolsInteractiveOperationsReady.HeaderText = "Interactive Guest";
		this.vToolsInteractiveOperationsReady.MinimumWidth = 6;
		this.vToolsInteractiveOperationsReady.Name = "vToolsInteractiveOperationsReady";
		this.vToolsInteractiveOperationsReady.ReadOnly = true;
		this.vToolsInteractiveOperationsReady.ToolTipText = "Interactive Guest Operations availability";
		this.vToolsInteractiveOperationsReady.Width = 200;
		this.vToolsNotes.HeaderText = "Annotation";
		this.vToolsNotes.MinimumWidth = 6;
		this.vToolsNotes.Name = "vToolsNotes";
		this.vToolsNotes.ReadOnly = true;
		this.vToolsNotes.ToolTipText = "Description for the virtual machine";
		this.vToolsNotes.Width = 60;
		this.vToolsDatacenter.HeaderText = "Datacenter";
		this.vToolsDatacenter.MinimumWidth = 6;
		this.vToolsDatacenter.Name = "vToolsDatacenter";
		this.vToolsDatacenter.ReadOnly = true;
		this.vToolsDatacenter.ToolTipText = "Datacenter name";
		this.vToolsDatacenter.Width = 125;
		this.vToolsCluster.HeaderText = "Cluster";
		this.vToolsCluster.MinimumWidth = 6;
		this.vToolsCluster.Name = "vToolsCluster";
		this.vToolsCluster.ReadOnly = true;
		this.vToolsCluster.ToolTipText = "Cluster name";
		this.vToolsCluster.Width = 125;
		this.vToolsHost.HeaderText = "Host";
		this.vToolsHost.MinimumWidth = 6;
		this.vToolsHost.Name = "vToolsHost";
		this.vToolsHost.ReadOnly = true;
		this.vToolsHost.ToolTipText = "The host on which the vm is running";
		this.vToolsHost.Width = 54;
		this.vToolsFolder.HeaderText = "Folder";
		this.vToolsFolder.MinimumWidth = 6;
		this.vToolsFolder.Name = "vToolsFolder";
		this.vToolsFolder.ReadOnly = true;
		this.vToolsFolder.ToolTipText = "VM folder name";
		this.vToolsFolder.Width = 125;
		this.vToolsOS.HeaderText = "OS according to the configuration file";
		this.vToolsOS.MinimumWidth = 6;
		this.vToolsOS.Name = "vToolsOS";
		this.vToolsOS.ReadOnly = true;
		this.vToolsOS.ToolTipText = "Guest operating system according to the Configuration file.";
		this.vToolsOS.Width = 300;
		this.vToolsguestFullName.HeaderText = "OS according to the VMware Tools";
		this.vToolsguestFullName.MinimumWidth = 6;
		this.vToolsguestFullName.Name = "vToolsguestFullName";
		this.vToolsguestFullName.ReadOnly = true;
		this.vToolsguestFullName.ToolTipText = "Guest operating system according to the VMware Tools.";
		this.vToolsguestFullName.Width = 300;
		this.vToolsVMRef.HeaderText = "VMRef";
		this.vToolsVMRef.MinimumWidth = 6;
		this.vToolsVMRef.Name = "vToolsVMRef";
		this.vToolsVMRef.ReadOnly = true;
		this.vToolsVMRef.ToolTipText = "Internal use";
		this.vToolsVMRef.Width = 65;
		this.vToolsObjectID.HeaderText = "VM ID";
		this.vToolsObjectID.MinimumWidth = 6;
		this.vToolsObjectID.Name = "vToolsObjectID";
		this.vToolsObjectID.ReadOnly = true;
		this.vToolsObjectID.ToolTipText = "VM Object ID";
		this.vToolsObjectID.Width = 125;
		this.vToolsUUID.HeaderText = "VM UUID";
		this.vToolsUUID.MinimumWidth = 6;
		this.vToolsUUID.Name = "vToolsUUID";
		this.vToolsUUID.ReadOnly = true;
		this.vToolsUUID.ToolTipText = "VirtualCenter-specific 128-bit UUID of a virtual machine";
		this.vToolsUUID.Width = 125;
		this.vToolsVISDKServer.HeaderText = "VI SDK Server";
		this.vToolsVISDKServer.MinimumWidth = 6;
		this.vToolsVISDKServer.Name = "vToolsVISDKServer";
		this.vToolsVISDKServer.ReadOnly = true;
		this.vToolsVISDKServer.ToolTipText = "VI SDK Server, where RVTools gets it's information from";
		this.vToolsVISDKServer.Width = 150;
		this.vToolsInstanceUUID.HeaderText = "VI SDK UUID";
		this.vToolsInstanceUUID.MinimumWidth = 6;
		this.vToolsInstanceUUID.Name = "vToolsInstanceUUID";
		this.vToolsInstanceUUID.ReadOnly = true;
		this.vToolsInstanceUUID.ToolTipText = "A globally unique identifier associated with this service instance";
		this.vToolsInstanceUUID.Width = 125;
		this.panel11.Controls.Add(this.button1);
		this.panel11.Dock = System.Windows.Forms.DockStyle.Bottom;
		this.panel11.Location = new System.Drawing.Point(0, 328);
		this.panel11.Margin = new System.Windows.Forms.Padding(4);
		this.panel11.Name = "panel11";
		this.panel11.Padding = new System.Windows.Forms.Padding(20, 15, 20, 15);
		this.panel11.Size = new System.Drawing.Size(1725, 63);
		this.panel11.TabIndex = 2;
		this.button1.Dock = System.Windows.Forms.DockStyle.Fill;
		this.button1.Location = new System.Drawing.Point(20, 15);
		this.button1.Margin = new System.Windows.Forms.Padding(4);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(1685, 33);
		this.button1.TabIndex = 0;
		this.button1.Text = "Upgrade VMware Tools";
		this.button1.UseVisualStyleBackColor = true;
		this.button1.Click += new System.EventHandler(this.btVMUpgrade_Click);
		this.tabvSnapshot.Controls.Add(this.panel9);
		this.tabvSnapshot.Location = new System.Drawing.Point(4, 25);
		this.tabvSnapshot.Margin = new System.Windows.Forms.Padding(4);
		this.tabvSnapshot.Name = "tabvSnapshot";
		this.tabvSnapshot.Size = new System.Drawing.Size(1725, 391);
		this.tabvSnapshot.TabIndex = 16;
		this.tabvSnapshot.Text = "vSnapshot";
		this.tabvSnapshot.ToolTipText = "Virtual Snapshot Information";
		this.tabvSnapshot.UseVisualStyleBackColor = true;
		this.panel9.Controls.Add(this.dataGridvSnapshot);
		this.panel9.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel9.Location = new System.Drawing.Point(0, 0);
		this.panel9.Margin = new System.Windows.Forms.Padding(4);
		this.panel9.Name = "panel9";
		this.panel9.Padding = new System.Windows.Forms.Padding(20, 18, 20, 18);
		this.panel9.Size = new System.Drawing.Size(1725, 391);
		this.panel9.TabIndex = 3;
		this.dataGridvSnapshot.AllowUserToAddRows = false;
		this.dataGridvSnapshot.AllowUserToDeleteRows = false;
		this.dataGridvSnapshot.AllowUserToOrderColumns = true;
		this.dataGridvSnapshot.AllowUserToResizeRows = false;
		dataGridViewCellStyle126.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
		this.dataGridvSnapshot.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle126;
		this.dataGridvSnapshot.BackgroundColor = System.Drawing.SystemColors.Window;
		this.dataGridvSnapshot.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
		this.dataGridvSnapshot.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGridvSnapshot.Columns.AddRange(this.vSnapshotVMName, this.vSnapshotPowerstate, this.vSnapshotName, this.vSnapshotDescription, this.vSnapshotCreateTime, this.vSnapshotFilename, this.vSnapshotSizeVmsn, this.vSnapshotSizeTotal, this.vSnapshotQuiesced, this.vSnapshotState, this.vSnapshotNotes, this.vSnapshotDatacenter, this.vSnapshotCluster, this.vSnapshotHost, this.vSnapshotFolder, this.vSnapshotOS, this.vSnapshotOSTools, this.vSnapshotObjectID, this.vSnapshotUUID, this.vSnapshotVISDKServer, this.vSnapshotInstanceUUID);
		this.dataGridvSnapshot.Dock = System.Windows.Forms.DockStyle.Fill;
		this.dataGridvSnapshot.Location = new System.Drawing.Point(20, 18);
		this.dataGridvSnapshot.Margin = new System.Windows.Forms.Padding(4);
		this.dataGridvSnapshot.Name = "dataGridvSnapshot";
		this.dataGridvSnapshot.RowHeadersVisible = false;
		this.dataGridvSnapshot.RowHeadersWidth = 51;
		this.dataGridvSnapshot.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this.dataGridvSnapshot.Size = new System.Drawing.Size(1685, 355);
		this.dataGridvSnapshot.TabIndex = 2;
		this.vSnapshotVMName.Frozen = true;
		this.vSnapshotVMName.HeaderText = "VM";
		this.vSnapshotVMName.MinimumWidth = 6;
		this.vSnapshotVMName.Name = "vSnapshotVMName";
		this.vSnapshotVMName.ReadOnly = true;
		this.vSnapshotVMName.ToolTipText = "Virtual Machine Name";
		this.vSnapshotVMName.Width = 48;
		this.vSnapshotPowerstate.HeaderText = "Powerstate";
		this.vSnapshotPowerstate.MinimumWidth = 6;
		this.vSnapshotPowerstate.Name = "vSnapshotPowerstate";
		this.vSnapshotPowerstate.ReadOnly = true;
		this.vSnapshotPowerstate.ToolTipText = "Power State";
		this.vSnapshotPowerstate.Width = 125;
		this.vSnapshotName.HeaderText = "Name";
		this.vSnapshotName.MinimumWidth = 6;
		this.vSnapshotName.Name = "vSnapshotName";
		this.vSnapshotName.ReadOnly = true;
		this.vSnapshotName.ToolTipText = "Name of the snapshot. ";
		this.vSnapshotName.Width = 60;
		this.vSnapshotDescription.HeaderText = "Description";
		this.vSnapshotDescription.MinimumWidth = 6;
		this.vSnapshotDescription.Name = "vSnapshotDescription";
		this.vSnapshotDescription.ReadOnly = true;
		this.vSnapshotDescription.ToolTipText = "Description of the snapshot.";
		this.vSnapshotDescription.Width = 85;
		dataGridViewCellStyle127.Format = "G";
		dataGridViewCellStyle127.NullValue = null;
		this.vSnapshotCreateTime.DefaultCellStyle = dataGridViewCellStyle127;
		this.vSnapshotCreateTime.HeaderText = "Date / time";
		this.vSnapshotCreateTime.MinimumWidth = 6;
		this.vSnapshotCreateTime.Name = "vSnapshotCreateTime";
		this.vSnapshotCreateTime.ReadOnly = true;
		this.vSnapshotCreateTime.ToolTipText = "The date and time the snapshot was taken.";
		this.vSnapshotCreateTime.Width = 85;
		this.vSnapshotFilename.HeaderText = "Filename";
		this.vSnapshotFilename.MinimumWidth = 6;
		this.vSnapshotFilename.Name = "vSnapshotFilename";
		this.vSnapshotFilename.ReadOnly = true;
		this.vSnapshotFilename.ToolTipText = "Filename";
		this.vSnapshotFilename.Width = 125;
		dataGridViewCellStyle128.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle128.Format = "N4";
		dataGridViewCellStyle128.NullValue = null;
		this.vSnapshotSizeVmsn.DefaultCellStyle = dataGridViewCellStyle128;
		this.vSnapshotSizeVmsn.HeaderText = "Size MiB (vmsn)";
		this.vSnapshotSizeVmsn.MinimumWidth = 6;
		this.vSnapshotSizeVmsn.Name = "vSnapshotSizeVmsn";
		this.vSnapshotSizeVmsn.ReadOnly = true;
		this.vSnapshotSizeVmsn.ToolTipText = "Size of memory state at the time of the snapshot in MiBs";
		this.vSnapshotSizeVmsn.Width = 150;
		dataGridViewCellStyle129.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle129.Format = "N4";
		dataGridViewCellStyle129.NullValue = null;
		this.vSnapshotSizeTotal.DefaultCellStyle = dataGridViewCellStyle129;
		this.vSnapshotSizeTotal.HeaderText = "Size MiB (total)";
		this.vSnapshotSizeTotal.MinimumWidth = 6;
		this.vSnapshotSizeTotal.Name = "vSnapshotSizeTotal";
		this.vSnapshotSizeTotal.ReadOnly = true;
		this.vSnapshotSizeTotal.ToolTipText = "Total size of all snapshot files for this VM in MiB";
		this.vSnapshotSizeTotal.Width = 150;
		this.vSnapshotQuiesced.HeaderText = "Quiesced";
		this.vSnapshotQuiesced.MinimumWidth = 6;
		this.vSnapshotQuiesced.Name = "vSnapshotQuiesced";
		this.vSnapshotQuiesced.ReadOnly = true;
		this.vSnapshotQuiesced.ToolTipText = "Flag to indicate whether or not the snapshot was created with the \"quiesce\" option, ensuring a consistent state of the file system.";
		this.vSnapshotQuiesced.Width = 77;
		this.vSnapshotState.HeaderText = "State";
		this.vSnapshotState.MinimumWidth = 6;
		this.vSnapshotState.Name = "vSnapshotState";
		this.vSnapshotState.ReadOnly = true;
		this.vSnapshotState.ToolTipText = "The power state of the virtual machine when this snapshot was taken.";
		this.vSnapshotState.Width = 57;
		this.vSnapshotNotes.HeaderText = "Annotation";
		this.vSnapshotNotes.MinimumWidth = 6;
		this.vSnapshotNotes.Name = "vSnapshotNotes";
		this.vSnapshotNotes.ReadOnly = true;
		this.vSnapshotNotes.ToolTipText = "Description for the virtual machine";
		this.vSnapshotNotes.Width = 60;
		this.vSnapshotDatacenter.HeaderText = "Datacenter";
		this.vSnapshotDatacenter.MinimumWidth = 6;
		this.vSnapshotDatacenter.Name = "vSnapshotDatacenter";
		this.vSnapshotDatacenter.ReadOnly = true;
		this.vSnapshotDatacenter.ToolTipText = "Datacenter name";
		this.vSnapshotDatacenter.Width = 125;
		this.vSnapshotCluster.HeaderText = "Cluster";
		this.vSnapshotCluster.MinimumWidth = 6;
		this.vSnapshotCluster.Name = "vSnapshotCluster";
		this.vSnapshotCluster.ReadOnly = true;
		this.vSnapshotCluster.ToolTipText = "Cluster name";
		this.vSnapshotCluster.Width = 125;
		this.vSnapshotHost.HeaderText = "Host";
		this.vSnapshotHost.MinimumWidth = 6;
		this.vSnapshotHost.Name = "vSnapshotHost";
		this.vSnapshotHost.ReadOnly = true;
		this.vSnapshotHost.ToolTipText = "The host on which the vm is running";
		this.vSnapshotHost.Width = 54;
		this.vSnapshotFolder.HeaderText = "Folder";
		this.vSnapshotFolder.MinimumWidth = 6;
		this.vSnapshotFolder.Name = "vSnapshotFolder";
		this.vSnapshotFolder.ReadOnly = true;
		this.vSnapshotFolder.ToolTipText = "VM folder name";
		this.vSnapshotFolder.Width = 125;
		this.vSnapshotOS.HeaderText = "OS according to the configuration file";
		this.vSnapshotOS.MinimumWidth = 6;
		this.vSnapshotOS.Name = "vSnapshotOS";
		this.vSnapshotOS.ReadOnly = true;
		this.vSnapshotOS.ToolTipText = "Guest operating system according to the Configuration file.";
		this.vSnapshotOS.Width = 300;
		this.vSnapshotOSTools.HeaderText = "OS according to the VMware Tools";
		this.vSnapshotOSTools.MinimumWidth = 6;
		this.vSnapshotOSTools.Name = "vSnapshotOSTools";
		this.vSnapshotOSTools.ReadOnly = true;
		this.vSnapshotOSTools.ToolTipText = "Guest operating system according to VMware tools";
		this.vSnapshotOSTools.Width = 300;
		this.vSnapshotObjectID.HeaderText = "VM ID";
		this.vSnapshotObjectID.MinimumWidth = 6;
		this.vSnapshotObjectID.Name = "vSnapshotObjectID";
		this.vSnapshotObjectID.ReadOnly = true;
		this.vSnapshotObjectID.ToolTipText = "VM Object ID";
		this.vSnapshotObjectID.Width = 125;
		this.vSnapshotUUID.HeaderText = "VM UUID";
		this.vSnapshotUUID.MinimumWidth = 6;
		this.vSnapshotUUID.Name = "vSnapshotUUID";
		this.vSnapshotUUID.ReadOnly = true;
		this.vSnapshotUUID.ToolTipText = "VirtualCenter-specific 128-bit UUID of a virtual machine";
		this.vSnapshotUUID.Width = 125;
		this.vSnapshotVISDKServer.HeaderText = "VI SDK Server";
		this.vSnapshotVISDKServer.MinimumWidth = 6;
		this.vSnapshotVISDKServer.Name = "vSnapshotVISDKServer";
		this.vSnapshotVISDKServer.ReadOnly = true;
		this.vSnapshotVISDKServer.ToolTipText = "VI SDK Server, where RVTools gets it's information from";
		this.vSnapshotVISDKServer.Width = 150;
		this.vSnapshotInstanceUUID.HeaderText = "VI SDK UUID";
		this.vSnapshotInstanceUUID.MinimumWidth = 6;
		this.vSnapshotInstanceUUID.Name = "vSnapshotInstanceUUID";
		this.vSnapshotInstanceUUID.ReadOnly = true;
		this.vSnapshotInstanceUUID.ToolTipText = "A globally unique identifier associated with this service instance";
		this.vSnapshotInstanceUUID.Width = 125;
		this.tabvCD.Controls.Add(this.panel12);
		this.tabvCD.Controls.Add(this.panel13);
		this.tabvCD.Location = new System.Drawing.Point(4, 25);
		this.tabvCD.Margin = new System.Windows.Forms.Padding(4);
		this.tabvCD.Name = "tabvCD";
		this.tabvCD.Size = new System.Drawing.Size(1725, 391);
		this.tabvCD.TabIndex = 15;
		this.tabvCD.Text = "vCD";
		this.tabvCD.ToolTipText = "Virtual CD Information";
		this.tabvCD.UseVisualStyleBackColor = true;
		this.panel12.Controls.Add(this.dataGridvCD);
		this.panel12.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel12.Location = new System.Drawing.Point(0, 0);
		this.panel12.Margin = new System.Windows.Forms.Padding(4);
		this.panel12.Name = "panel12";
		this.panel12.Padding = new System.Windows.Forms.Padding(20, 18, 20, 6);
		this.panel12.Size = new System.Drawing.Size(1725, 328);
		this.panel12.TabIndex = 6;
		this.dataGridvCD.AllowUserToAddRows = false;
		this.dataGridvCD.AllowUserToDeleteRows = false;
		this.dataGridvCD.AllowUserToOrderColumns = true;
		this.dataGridvCD.AllowUserToResizeRows = false;
		dataGridViewCellStyle130.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
		this.dataGridvCD.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle130;
		this.dataGridvCD.BackgroundColor = System.Drawing.SystemColors.Window;
		this.dataGridvCD.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
		this.dataGridvCD.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGridvCD.Columns.AddRange(this.vCDSelect, this.vCDVMName, this.vCDPowerstate, this.vCDTemplate, this.vCDSRMPlaceholder, this.vCDlabel, this.vCDConnected, this.vCDStartup, this.vCDSummary, this.vCDNotes, this.vCDDatacenter, this.vCDCluster, this.vCDHost, this.vCDFolder, this.vCDOS, this.vCDOSTools, this.vCDVMRef, this.vCDObjectID, this.vCDUUID, this.vCDVISDKServer, this.vCDInstanceUUID);
		this.dataGridvCD.Dock = System.Windows.Forms.DockStyle.Fill;
		this.dataGridvCD.Location = new System.Drawing.Point(20, 18);
		this.dataGridvCD.Margin = new System.Windows.Forms.Padding(4);
		this.dataGridvCD.Name = "dataGridvCD";
		this.dataGridvCD.RowHeadersVisible = false;
		this.dataGridvCD.RowHeadersWidth = 51;
		this.dataGridvCD.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this.dataGridvCD.Size = new System.Drawing.Size(1685, 304);
		this.dataGridvCD.TabIndex = 3;
		this.dataGridvCD.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridvCD_CellContentClick);
		this.vCDSelect.FalseValue = "0";
		this.vCDSelect.Frozen = true;
		this.vCDSelect.HeaderText = "Select";
		this.vCDSelect.MinimumWidth = 6;
		this.vCDSelect.Name = "vCDSelect";
		this.vCDSelect.ToolTipText = "Select to Disconnect";
		this.vCDSelect.TrueValue = "1";
		this.vCDSelect.Width = 43;
		this.vCDVMName.Frozen = true;
		this.vCDVMName.HeaderText = "VM";
		this.vCDVMName.MinimumWidth = 6;
		this.vCDVMName.Name = "vCDVMName";
		this.vCDVMName.ReadOnly = true;
		this.vCDVMName.ToolTipText = "Virtual Machine Name";
		this.vCDVMName.Width = 48;
		this.vCDPowerstate.HeaderText = "Powerstate";
		this.vCDPowerstate.MinimumWidth = 6;
		this.vCDPowerstate.Name = "vCDPowerstate";
		this.vCDPowerstate.ReadOnly = true;
		this.vCDPowerstate.Width = 85;
		this.vCDTemplate.HeaderText = "Template";
		this.vCDTemplate.MinimumWidth = 6;
		this.vCDTemplate.Name = "vCDTemplate";
		this.vCDTemplate.ReadOnly = true;
		this.vCDTemplate.ToolTipText = "Is VM a template?";
		this.vCDTemplate.Width = 125;
		this.vCDSRMPlaceholder.HeaderText = "SRM Placeholder";
		this.vCDSRMPlaceholder.MinimumWidth = 6;
		this.vCDSRMPlaceholder.Name = "vCDSRMPlaceholder";
		this.vCDSRMPlaceholder.ReadOnly = true;
		this.vCDSRMPlaceholder.ToolTipText = "Is VM a SRM Placeholder?";
		this.vCDSRMPlaceholder.Width = 200;
		this.vCDlabel.HeaderText = "Device Node";
		this.vCDlabel.MinimumWidth = 6;
		this.vCDlabel.Name = "vCDlabel";
		this.vCDlabel.ReadOnly = true;
		this.vCDlabel.ToolTipText = "Device Node";
		this.vCDlabel.Width = 125;
		this.vCDConnected.HeaderText = "Connected";
		this.vCDConnected.MinimumWidth = 6;
		this.vCDConnected.Name = "vCDConnected";
		this.vCDConnected.ReadOnly = true;
		this.vCDConnected.ToolTipText = "Is CD connected?";
		this.vCDConnected.Width = 84;
		this.vCDStartup.HeaderText = "Starts Connected";
		this.vCDStartup.MinimumWidth = 6;
		this.vCDStartup.Name = "vCDStartup";
		this.vCDStartup.ReadOnly = true;
		this.vCDStartup.ToolTipText = "Connect CD at power on?";
		this.vCDStartup.Width = 150;
		this.vCDSummary.HeaderText = "Device Type";
		this.vCDSummary.MinimumWidth = 6;
		this.vCDSummary.Name = "vCDSummary";
		this.vCDSummary.ReadOnly = true;
		this.vCDSummary.ToolTipText = "Device Type";
		this.vCDSummary.Width = 125;
		this.vCDNotes.HeaderText = "Annotation";
		this.vCDNotes.MinimumWidth = 6;
		this.vCDNotes.Name = "vCDNotes";
		this.vCDNotes.ReadOnly = true;
		this.vCDNotes.ToolTipText = "Description for the virtual machine";
		this.vCDNotes.Width = 60;
		this.vCDDatacenter.HeaderText = "Datacenter";
		this.vCDDatacenter.MinimumWidth = 6;
		this.vCDDatacenter.Name = "vCDDatacenter";
		this.vCDDatacenter.ReadOnly = true;
		this.vCDDatacenter.ToolTipText = "Datacenter name";
		this.vCDDatacenter.Width = 125;
		this.vCDCluster.HeaderText = "Cluster";
		this.vCDCluster.MinimumWidth = 6;
		this.vCDCluster.Name = "vCDCluster";
		this.vCDCluster.ReadOnly = true;
		this.vCDCluster.ToolTipText = "Cluster name";
		this.vCDCluster.Width = 125;
		this.vCDHost.HeaderText = "Host";
		this.vCDHost.MinimumWidth = 6;
		this.vCDHost.Name = "vCDHost";
		this.vCDHost.ReadOnly = true;
		this.vCDHost.ToolTipText = "The host on which the vm is running";
		this.vCDHost.Width = 54;
		this.vCDFolder.HeaderText = "Folder";
		this.vCDFolder.MinimumWidth = 6;
		this.vCDFolder.Name = "vCDFolder";
		this.vCDFolder.ReadOnly = true;
		this.vCDFolder.ToolTipText = "VM folder name";
		this.vCDFolder.Width = 125;
		this.vCDOS.HeaderText = "OS according to the configuration file";
		this.vCDOS.MinimumWidth = 6;
		this.vCDOS.Name = "vCDOS";
		this.vCDOS.ReadOnly = true;
		this.vCDOS.ToolTipText = "Guest operating system according to the Configuration file.";
		this.vCDOS.Width = 300;
		this.vCDOSTools.HeaderText = "OS according to the VMware Tools";
		this.vCDOSTools.MinimumWidth = 6;
		this.vCDOSTools.Name = "vCDOSTools";
		this.vCDOSTools.ReadOnly = true;
		this.vCDOSTools.ToolTipText = "Guest operating system according to VMware tools";
		this.vCDOSTools.Width = 300;
		this.vCDVMRef.HeaderText = "VMRef";
		this.vCDVMRef.MinimumWidth = 6;
		this.vCDVMRef.Name = "vCDVMRef";
		this.vCDVMRef.ReadOnly = true;
		this.vCDVMRef.ToolTipText = "Internal use";
		this.vCDVMRef.Width = 65;
		this.vCDObjectID.HeaderText = "VM ID";
		this.vCDObjectID.MinimumWidth = 6;
		this.vCDObjectID.Name = "vCDObjectID";
		this.vCDObjectID.ReadOnly = true;
		this.vCDObjectID.ToolTipText = "VM Object ID";
		this.vCDObjectID.Width = 125;
		this.vCDUUID.HeaderText = "VM UUID";
		this.vCDUUID.MinimumWidth = 6;
		this.vCDUUID.Name = "vCDUUID";
		this.vCDUUID.ReadOnly = true;
		this.vCDUUID.ToolTipText = "VirtualCenter-specific 128-bit UUID of a virtual machine";
		this.vCDUUID.Width = 125;
		this.vCDVISDKServer.HeaderText = "VI SDK Server";
		this.vCDVISDKServer.MinimumWidth = 6;
		this.vCDVISDKServer.Name = "vCDVISDKServer";
		this.vCDVISDKServer.ReadOnly = true;
		this.vCDVISDKServer.ToolTipText = "VI SDK Server, where RVTools gets it's information from";
		this.vCDVISDKServer.Width = 150;
		this.vCDInstanceUUID.HeaderText = "VI SDK UUID";
		this.vCDInstanceUUID.MinimumWidth = 6;
		this.vCDInstanceUUID.Name = "vCDInstanceUUID";
		this.vCDInstanceUUID.ReadOnly = true;
		this.vCDInstanceUUID.ToolTipText = "A globally unique identifier associated with this service instance";
		this.vCDInstanceUUID.Width = 125;
		this.panel13.Controls.Add(this.buttonDisconnectCD);
		this.panel13.Dock = System.Windows.Forms.DockStyle.Bottom;
		this.panel13.Location = new System.Drawing.Point(0, 328);
		this.panel13.Margin = new System.Windows.Forms.Padding(4);
		this.panel13.Name = "panel13";
		this.panel13.Padding = new System.Windows.Forms.Padding(20, 15, 20, 15);
		this.panel13.Size = new System.Drawing.Size(1725, 63);
		this.panel13.TabIndex = 5;
		this.buttonDisconnectCD.Dock = System.Windows.Forms.DockStyle.Fill;
		this.buttonDisconnectCD.Location = new System.Drawing.Point(20, 15);
		this.buttonDisconnectCD.Margin = new System.Windows.Forms.Padding(4);
		this.buttonDisconnectCD.Name = "buttonDisconnectCD";
		this.buttonDisconnectCD.Size = new System.Drawing.Size(1685, 33);
		this.buttonDisconnectCD.TabIndex = 0;
		this.buttonDisconnectCD.Text = "Disconnect CD";
		this.buttonDisconnectCD.UseVisualStyleBackColor = true;
		this.buttonDisconnectCD.Click += new System.EventHandler(this.buttonDisconnectCD_Click);
		this.tabvUSB.Controls.Add(this.panel1);
		this.tabvUSB.Controls.Add(this.panel2);
		this.tabvUSB.Location = new System.Drawing.Point(4, 25);
		this.tabvUSB.Margin = new System.Windows.Forms.Padding(4);
		this.tabvUSB.Name = "tabvUSB";
		this.tabvUSB.Size = new System.Drawing.Size(1725, 391);
		this.tabvUSB.TabIndex = 14;
		this.tabvUSB.Text = "vUSB";
		this.tabvUSB.ToolTipText = "Virtual USB Information";
		this.tabvUSB.UseVisualStyleBackColor = true;
		this.panel1.Controls.Add(this.dataGridvUSB);
		this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel1.Location = new System.Drawing.Point(0, 0);
		this.panel1.Margin = new System.Windows.Forms.Padding(4);
		this.panel1.Name = "panel1";
		this.panel1.Padding = new System.Windows.Forms.Padding(20, 18, 20, 6);
		this.panel1.Size = new System.Drawing.Size(1725, 328);
		this.panel1.TabIndex = 7;
		this.dataGridvUSB.AllowUserToAddRows = false;
		this.dataGridvUSB.AllowUserToDeleteRows = false;
		this.dataGridvUSB.AllowUserToOrderColumns = true;
		this.dataGridvUSB.AllowUserToResizeRows = false;
		dataGridViewCellStyle131.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
		this.dataGridvUSB.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle131;
		this.dataGridvUSB.BackgroundColor = System.Drawing.SystemColors.Window;
		this.dataGridvUSB.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
		this.dataGridvUSB.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGridvUSB.Columns.AddRange(this.vUSBSelect, this.vUSBVMName, this.vUSBPowerstate, this.vUSBTemplate, this.vUSBSRMPlaceholder, this.vUSBLabel, this.vUSBSummary, this.vUSBConnected, this.vUSBFamily, this.vUSBSpeed, this.vUSBEHCIEnabled, this.vUSBAutoConnect, this.vUSBBusNumber, this.vUSBUnitNumber, this.vUSBNotes, this.vUSBDatacenter, this.vUSBCluster, this.vUSBHost, this.vUSBFolder, this.vUSBOS, this.vUSBOSTools, this.vUSBVMRef, this.vUSBObjectID, this.vUSBUUID, this.vUSBVISDKServer, this.vUSBInstanceUUID);
		this.dataGridvUSB.Dock = System.Windows.Forms.DockStyle.Fill;
		this.dataGridvUSB.Location = new System.Drawing.Point(20, 18);
		this.dataGridvUSB.Margin = new System.Windows.Forms.Padding(4);
		this.dataGridvUSB.Name = "dataGridvUSB";
		this.dataGridvUSB.RowHeadersVisible = false;
		this.dataGridvUSB.RowHeadersWidth = 51;
		this.dataGridvUSB.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this.dataGridvUSB.Size = new System.Drawing.Size(1685, 304);
		this.dataGridvUSB.TabIndex = 4;
		this.dataGridvUSB.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridvUSB_CellContentClick);
		this.vUSBSelect.FalseValue = "0";
		this.vUSBSelect.Frozen = true;
		this.vUSBSelect.HeaderText = "Select";
		this.vUSBSelect.MinimumWidth = 6;
		this.vUSBSelect.Name = "vUSBSelect";
		this.vUSBSelect.ToolTipText = "Select to Disconnect";
		this.vUSBSelect.TrueValue = "1";
		this.vUSBSelect.Width = 43;
		this.vUSBVMName.Frozen = true;
		this.vUSBVMName.HeaderText = "VM";
		this.vUSBVMName.MinimumWidth = 6;
		this.vUSBVMName.Name = "vUSBVMName";
		this.vUSBVMName.ReadOnly = true;
		this.vUSBVMName.ToolTipText = "Virtual Machine Name";
		this.vUSBVMName.Width = 48;
		this.vUSBPowerstate.HeaderText = "Powerstate";
		this.vUSBPowerstate.MinimumWidth = 6;
		this.vUSBPowerstate.Name = "vUSBPowerstate";
		this.vUSBPowerstate.ReadOnly = true;
		this.vUSBPowerstate.ToolTipText = "Power State";
		this.vUSBPowerstate.Width = 85;
		this.vUSBTemplate.HeaderText = "Template";
		this.vUSBTemplate.MinimumWidth = 6;
		this.vUSBTemplate.Name = "vUSBTemplate";
		this.vUSBTemplate.ReadOnly = true;
		this.vUSBTemplate.ToolTipText = "Is VM a template?";
		this.vUSBTemplate.Width = 125;
		this.vUSBSRMPlaceholder.HeaderText = "SRM Placeholder";
		this.vUSBSRMPlaceholder.MinimumWidth = 6;
		this.vUSBSRMPlaceholder.Name = "vUSBSRMPlaceholder";
		this.vUSBSRMPlaceholder.ReadOnly = true;
		this.vUSBSRMPlaceholder.ToolTipText = "Is VM a SRM Placeholder?";
		this.vUSBSRMPlaceholder.Width = 200;
		this.vUSBLabel.HeaderText = "Device Node";
		this.vUSBLabel.MinimumWidth = 6;
		this.vUSBLabel.Name = "vUSBLabel";
		this.vUSBLabel.ReadOnly = true;
		this.vUSBLabel.ToolTipText = "Device Node";
		this.vUSBLabel.Width = 125;
		this.vUSBSummary.HeaderText = "Device Type";
		this.vUSBSummary.MinimumWidth = 6;
		this.vUSBSummary.Name = "vUSBSummary";
		this.vUSBSummary.ReadOnly = true;
		this.vUSBSummary.ToolTipText = "Device Type";
		this.vUSBSummary.Width = 125;
		this.vUSBConnected.HeaderText = "Connected";
		this.vUSBConnected.MinimumWidth = 6;
		this.vUSBConnected.Name = "vUSBConnected";
		this.vUSBConnected.ReadOnly = true;
		this.vUSBConnected.ToolTipText = "Is USB device connected?";
		this.vUSBConnected.Width = 84;
		this.vUSBFamily.HeaderText = "Family";
		this.vUSBFamily.MinimumWidth = 6;
		this.vUSBFamily.Name = "vUSBFamily";
		this.vUSBFamily.ReadOnly = true;
		this.vUSBFamily.ToolTipText = "Device class families";
		this.vUSBFamily.Width = 125;
		this.vUSBSpeed.HeaderText = "Speed";
		this.vUSBSpeed.MinimumWidth = 6;
		this.vUSBSpeed.Name = "vUSBSpeed";
		this.vUSBSpeed.ReadOnly = true;
		this.vUSBSpeed.ToolTipText = "Device speeds detected by server";
		this.vUSBSpeed.Width = 125;
		this.vUSBEHCIEnabled.HeaderText = "EHCI enabled";
		this.vUSBEHCIEnabled.MinimumWidth = 6;
		this.vUSBEHCIEnabled.Name = "vUSBEHCIEnabled";
		this.vUSBEHCIEnabled.ReadOnly = true;
		this.vUSBEHCIEnabled.ToolTipText = "Flag to indicate whether or not enhanced host controller interface (USB 2.0) is enabled on this controller.";
		this.vUSBEHCIEnabled.Width = 125;
		this.vUSBAutoConnect.HeaderText = "Auto connect";
		this.vUSBAutoConnect.MinimumWidth = 6;
		this.vUSBAutoConnect.Name = "vUSBAutoConnect";
		this.vUSBAutoConnect.ReadOnly = true;
		this.vUSBAutoConnect.ToolTipText = "Flag to indicate whether or not the ability to hot plug devices is enabled on controller";
		this.vUSBAutoConnect.Width = 125;
		dataGridViewCellStyle132.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle132.Format = "N0";
		dataGridViewCellStyle132.NullValue = null;
		this.vUSBBusNumber.DefaultCellStyle = dataGridViewCellStyle132;
		this.vUSBBusNumber.HeaderText = "Bus number";
		this.vUSBBusNumber.MinimumWidth = 6;
		this.vUSBBusNumber.Name = "vUSBBusNumber";
		this.vUSBBusNumber.ReadOnly = true;
		this.vUSBBusNumber.Width = 125;
		dataGridViewCellStyle133.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle133.Format = "N0";
		dataGridViewCellStyle133.NullValue = null;
		this.vUSBUnitNumber.DefaultCellStyle = dataGridViewCellStyle133;
		this.vUSBUnitNumber.HeaderText = "Unit number";
		this.vUSBUnitNumber.MinimumWidth = 6;
		this.vUSBUnitNumber.Name = "vUSBUnitNumber";
		this.vUSBUnitNumber.ReadOnly = true;
		this.vUSBUnitNumber.Width = 125;
		this.vUSBNotes.HeaderText = "Annotation";
		this.vUSBNotes.MinimumWidth = 6;
		this.vUSBNotes.Name = "vUSBNotes";
		this.vUSBNotes.ReadOnly = true;
		this.vUSBNotes.ToolTipText = "Description for the virtual machine";
		this.vUSBNotes.Width = 60;
		this.vUSBDatacenter.HeaderText = "Datacenter";
		this.vUSBDatacenter.MinimumWidth = 6;
		this.vUSBDatacenter.Name = "vUSBDatacenter";
		this.vUSBDatacenter.ReadOnly = true;
		this.vUSBDatacenter.ToolTipText = "Datacenter name";
		this.vUSBDatacenter.Width = 125;
		this.vUSBCluster.HeaderText = "Cluster";
		this.vUSBCluster.MinimumWidth = 6;
		this.vUSBCluster.Name = "vUSBCluster";
		this.vUSBCluster.ReadOnly = true;
		this.vUSBCluster.ToolTipText = "Cluster name";
		this.vUSBCluster.Width = 125;
		this.vUSBHost.HeaderText = "Host";
		this.vUSBHost.MinimumWidth = 6;
		this.vUSBHost.Name = "vUSBHost";
		this.vUSBHost.ReadOnly = true;
		this.vUSBHost.ToolTipText = "The host on which the vm is running";
		this.vUSBHost.Width = 54;
		this.vUSBFolder.HeaderText = "Folder";
		this.vUSBFolder.MinimumWidth = 6;
		this.vUSBFolder.Name = "vUSBFolder";
		this.vUSBFolder.ReadOnly = true;
		this.vUSBFolder.ToolTipText = "VM folder name";
		this.vUSBFolder.Width = 125;
		this.vUSBOS.HeaderText = "OS according to the configuration file";
		this.vUSBOS.MinimumWidth = 6;
		this.vUSBOS.Name = "vUSBOS";
		this.vUSBOS.ReadOnly = true;
		this.vUSBOS.ToolTipText = "Guest operating system according to the Configuration file.";
		this.vUSBOS.Width = 300;
		this.vUSBOSTools.HeaderText = "OS according to the VMware tools";
		this.vUSBOSTools.MinimumWidth = 6;
		this.vUSBOSTools.Name = "vUSBOSTools";
		this.vUSBOSTools.ReadOnly = true;
		this.vUSBOSTools.ToolTipText = "Guest operating system according to VMware tools";
		this.vUSBOSTools.Width = 300;
		this.vUSBVMRef.HeaderText = "VMRef";
		this.vUSBVMRef.MinimumWidth = 6;
		this.vUSBVMRef.Name = "vUSBVMRef";
		this.vUSBVMRef.ReadOnly = true;
		this.vUSBVMRef.ToolTipText = "Internal use";
		this.vUSBVMRef.Width = 65;
		this.vUSBObjectID.HeaderText = "VM ID";
		this.vUSBObjectID.MinimumWidth = 6;
		this.vUSBObjectID.Name = "vUSBObjectID";
		this.vUSBObjectID.ReadOnly = true;
		this.vUSBObjectID.ToolTipText = "VM Object ID";
		this.vUSBObjectID.Width = 125;
		this.vUSBUUID.HeaderText = "VM UUID";
		this.vUSBUUID.MinimumWidth = 6;
		this.vUSBUUID.Name = "vUSBUUID";
		this.vUSBUUID.ReadOnly = true;
		this.vUSBUUID.ToolTipText = "VirtualCenter-specific 128-bit UUID of a virtual machine";
		this.vUSBUUID.Width = 125;
		this.vUSBVISDKServer.HeaderText = "VI SDK Server";
		this.vUSBVISDKServer.MinimumWidth = 6;
		this.vUSBVISDKServer.Name = "vUSBVISDKServer";
		this.vUSBVISDKServer.ReadOnly = true;
		this.vUSBVISDKServer.ToolTipText = "VI SDK Server, where RVTools gets it's information from";
		this.vUSBVISDKServer.Width = 150;
		this.vUSBInstanceUUID.HeaderText = "VI SDK UUID";
		this.vUSBInstanceUUID.MinimumWidth = 6;
		this.vUSBInstanceUUID.Name = "vUSBInstanceUUID";
		this.vUSBInstanceUUID.ReadOnly = true;
		this.vUSBInstanceUUID.ToolTipText = "A globally unique identifier associated with this service instance";
		this.vUSBInstanceUUID.Width = 125;
		this.panel2.Controls.Add(this.buttonDisconnectUSB);
		this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
		this.panel2.Location = new System.Drawing.Point(0, 328);
		this.panel2.Margin = new System.Windows.Forms.Padding(4);
		this.panel2.Name = "panel2";
		this.panel2.Padding = new System.Windows.Forms.Padding(20, 15, 20, 15);
		this.panel2.Size = new System.Drawing.Size(1725, 63);
		this.panel2.TabIndex = 6;
		this.buttonDisconnectUSB.Dock = System.Windows.Forms.DockStyle.Fill;
		this.buttonDisconnectUSB.Location = new System.Drawing.Point(20, 15);
		this.buttonDisconnectUSB.Margin = new System.Windows.Forms.Padding(4);
		this.buttonDisconnectUSB.Name = "buttonDisconnectUSB";
		this.buttonDisconnectUSB.Size = new System.Drawing.Size(1685, 33);
		this.buttonDisconnectUSB.TabIndex = 0;
		this.buttonDisconnectUSB.Text = "Remove USB Host Device from Virtual Machine";
		this.buttonDisconnectUSB.UseVisualStyleBackColor = true;
		this.buttonDisconnectUSB.Click += new System.EventHandler(this.buttonDisconnectUSB_Click);
		this.tabvNetwork.Controls.Add(this.panel5);
		this.tabvNetwork.Location = new System.Drawing.Point(4, 25);
		this.tabvNetwork.Margin = new System.Windows.Forms.Padding(4);
		this.tabvNetwork.Name = "tabvNetwork";
		this.tabvNetwork.Size = new System.Drawing.Size(1725, 391);
		this.tabvNetwork.TabIndex = 13;
		this.tabvNetwork.Text = "vNetwork";
		this.tabvNetwork.ToolTipText = "Virtual Network Information";
		this.tabvNetwork.UseVisualStyleBackColor = true;
		this.panel5.Controls.Add(this.dataGridvNetwork);
		this.panel5.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel5.Location = new System.Drawing.Point(0, 0);
		this.panel5.Margin = new System.Windows.Forms.Padding(4);
		this.panel5.Name = "panel5";
		this.panel5.Padding = new System.Windows.Forms.Padding(20, 18, 20, 18);
		this.panel5.Size = new System.Drawing.Size(1725, 391);
		this.panel5.TabIndex = 1;
		this.dataGridvNetwork.AllowUserToAddRows = false;
		this.dataGridvNetwork.AllowUserToDeleteRows = false;
		this.dataGridvNetwork.AllowUserToOrderColumns = true;
		this.dataGridvNetwork.AllowUserToResizeRows = false;
		dataGridViewCellStyle134.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
		this.dataGridvNetwork.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle134;
		this.dataGridvNetwork.BackgroundColor = System.Drawing.SystemColors.Window;
		this.dataGridvNetwork.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
		this.dataGridvNetwork.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGridvNetwork.Columns.AddRange(this.vNetworkVMName, this.vNetworkPowerstate, this.vNetworkTemplate, this.vNetworkSRMPlaceholder, this.vNetworkNic, this.vNetworkAdapter, this.vNetworkName, this.vNetworkSwitch, this.vNetworkConnected, this.vNetworkStartsConnected, this.vNetworkMacAddress, this.vNetworkMacAddressType, this.vNetworkIP4Address, this.vNetworkIP6Address, this.vNetworkDirectPathIO, this.vNetworkSortColumn, this.vNetworkNotes, this.vNetworkDatacenter, this.vNetworkCluster, this.vNetworkHost, this.vNetworkFolder, this.vNetworkOS, this.vNetworkOSTools, this.vNetworkObjectID, this.vNetworkUUID, this.vNetworkVISDKServer, this.vNetworkInstanceUUID);
		this.dataGridvNetwork.Dock = System.Windows.Forms.DockStyle.Fill;
		this.dataGridvNetwork.Location = new System.Drawing.Point(20, 18);
		this.dataGridvNetwork.Margin = new System.Windows.Forms.Padding(4);
		this.dataGridvNetwork.Name = "dataGridvNetwork";
		this.dataGridvNetwork.RowHeadersVisible = false;
		this.dataGridvNetwork.RowHeadersWidth = 51;
		this.dataGridvNetwork.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this.dataGridvNetwork.Size = new System.Drawing.Size(1685, 355);
		this.dataGridvNetwork.TabIndex = 3;
		this.vNetworkVMName.Frozen = true;
		this.vNetworkVMName.HeaderText = "VM";
		this.vNetworkVMName.MinimumWidth = 6;
		this.vNetworkVMName.Name = "vNetworkVMName";
		this.vNetworkVMName.ReadOnly = true;
		this.vNetworkVMName.ToolTipText = "Name of Virtual machine";
		this.vNetworkVMName.Width = 48;
		this.vNetworkPowerstate.HeaderText = "Powerstate";
		this.vNetworkPowerstate.MinimumWidth = 6;
		this.vNetworkPowerstate.Name = "vNetworkPowerstate";
		this.vNetworkPowerstate.ReadOnly = true;
		this.vNetworkPowerstate.ToolTipText = "Power State";
		this.vNetworkPowerstate.Width = 85;
		this.vNetworkTemplate.HeaderText = "Template";
		this.vNetworkTemplate.MinimumWidth = 6;
		this.vNetworkTemplate.Name = "vNetworkTemplate";
		this.vNetworkTemplate.ReadOnly = true;
		this.vNetworkTemplate.ToolTipText = "Is VM a template?";
		this.vNetworkTemplate.Width = 125;
		this.vNetworkSRMPlaceholder.HeaderText = "SRM Placeholder";
		this.vNetworkSRMPlaceholder.MinimumWidth = 6;
		this.vNetworkSRMPlaceholder.Name = "vNetworkSRMPlaceholder";
		this.vNetworkSRMPlaceholder.ReadOnly = true;
		this.vNetworkSRMPlaceholder.ToolTipText = "Is VM a SRM Placeholder?";
		this.vNetworkSRMPlaceholder.Width = 200;
		this.vNetworkNic.HeaderText = "NIC label";
		this.vNetworkNic.MinimumWidth = 6;
		this.vNetworkNic.Name = "vNetworkNic";
		this.vNetworkNic.ReadOnly = true;
		this.vNetworkNic.ToolTipText = "NIC label";
		this.vNetworkNic.Width = 125;
		this.vNetworkAdapter.HeaderText = "Adapter";
		this.vNetworkAdapter.MinimumWidth = 6;
		this.vNetworkAdapter.Name = "vNetworkAdapter";
		this.vNetworkAdapter.ReadOnly = true;
		this.vNetworkAdapter.ToolTipText = "Adapter type";
		this.vNetworkAdapter.Width = 125;
		this.vNetworkName.HeaderText = "Network";
		this.vNetworkName.MinimumWidth = 6;
		this.vNetworkName.Name = "vNetworkName";
		this.vNetworkName.ReadOnly = true;
		this.vNetworkName.ToolTipText = "Network Name";
		this.vNetworkName.Width = 72;
		this.vNetworkSwitch.HeaderText = "Switch";
		this.vNetworkSwitch.MinimumWidth = 6;
		this.vNetworkSwitch.Name = "vNetworkSwitch";
		this.vNetworkSwitch.ReadOnly = true;
		this.vNetworkSwitch.ToolTipText = "Name of Switch or Distributed Switch.";
		this.vNetworkSwitch.Width = 125;
		this.vNetworkConnected.HeaderText = "Connected";
		this.vNetworkConnected.MinimumWidth = 6;
		this.vNetworkConnected.Name = "vNetworkConnected";
		this.vNetworkConnected.ReadOnly = true;
		this.vNetworkConnected.ToolTipText = "Is Network Connected?";
		this.vNetworkConnected.Width = 84;
		this.vNetworkStartsConnected.HeaderText = "Starts Connected";
		this.vNetworkStartsConnected.MinimumWidth = 6;
		this.vNetworkStartsConnected.Name = "vNetworkStartsConnected";
		this.vNetworkStartsConnected.ReadOnly = true;
		this.vNetworkStartsConnected.ToolTipText = "Specifies whether or not to connect the device when the virtual machine starts.";
		this.vNetworkStartsConnected.Width = 150;
		this.vNetworkMacAddress.HeaderText = "Mac Address";
		this.vNetworkMacAddress.MinimumWidth = 6;
		this.vNetworkMacAddress.Name = "vNetworkMacAddress";
		this.vNetworkMacAddress.ReadOnly = true;
		this.vNetworkMacAddress.ToolTipText = "Mac Address";
		this.vNetworkMacAddress.Width = 94;
		this.vNetworkMacAddressType.HeaderText = "Type";
		this.vNetworkMacAddressType.MinimumWidth = 6;
		this.vNetworkMacAddressType.Name = "vNetworkMacAddressType";
		this.vNetworkMacAddressType.ReadOnly = true;
		this.vNetworkMacAddressType.ToolTipText = "MAC address type";
		this.vNetworkMacAddressType.Width = 125;
		this.vNetworkIP4Address.HeaderText = "IPv4 Address";
		this.vNetworkIP4Address.MinimumWidth = 6;
		this.vNetworkIP4Address.Name = "vNetworkIP4Address";
		this.vNetworkIP4Address.ReadOnly = true;
		this.vNetworkIP4Address.ToolTipText = "IPv4 Address";
		this.vNetworkIP4Address.Width = 125;
		this.vNetworkIP6Address.HeaderText = "IPv6 Address";
		this.vNetworkIP6Address.MinimumWidth = 6;
		this.vNetworkIP6Address.Name = "vNetworkIP6Address";
		this.vNetworkIP6Address.ReadOnly = true;
		this.vNetworkIP6Address.ToolTipText = "IPv6 Address";
		this.vNetworkIP6Address.Width = 125;
		this.vNetworkDirectPathIO.HeaderText = "Direct Path IO";
		this.vNetworkDirectPathIO.MinimumWidth = 6;
		this.vNetworkDirectPathIO.Name = "vNetworkDirectPathIO";
		this.vNetworkDirectPathIO.ReadOnly = true;
		this.vNetworkDirectPathIO.ToolTipText = "Network Adapter DirectPath I/O Parameter";
		this.vNetworkDirectPathIO.Width = 125;
		this.vNetworkSortColumn.HeaderText = "Internal Sort Column";
		this.vNetworkSortColumn.MinimumWidth = 6;
		this.vNetworkSortColumn.Name = "vNetworkSortColumn";
		this.vNetworkSortColumn.ReadOnly = true;
		this.vNetworkSortColumn.ToolTipText = "Sort Column used internally to sort the vNetwork data";
		this.vNetworkSortColumn.Width = 250;
		this.vNetworkNotes.HeaderText = "Annotation";
		this.vNetworkNotes.MinimumWidth = 6;
		this.vNetworkNotes.Name = "vNetworkNotes";
		this.vNetworkNotes.ReadOnly = true;
		this.vNetworkNotes.ToolTipText = "Description for the virtual machine";
		this.vNetworkNotes.Width = 60;
		this.vNetworkDatacenter.HeaderText = "Datacenter";
		this.vNetworkDatacenter.MinimumWidth = 6;
		this.vNetworkDatacenter.Name = "vNetworkDatacenter";
		this.vNetworkDatacenter.ReadOnly = true;
		this.vNetworkDatacenter.ToolTipText = "Datacenter name";
		this.vNetworkDatacenter.Width = 125;
		this.vNetworkCluster.HeaderText = "Cluster";
		this.vNetworkCluster.MinimumWidth = 6;
		this.vNetworkCluster.Name = "vNetworkCluster";
		this.vNetworkCluster.ReadOnly = true;
		this.vNetworkCluster.ToolTipText = "Cluster name";
		this.vNetworkCluster.Width = 125;
		this.vNetworkHost.HeaderText = "Host";
		this.vNetworkHost.MinimumWidth = 6;
		this.vNetworkHost.Name = "vNetworkHost";
		this.vNetworkHost.ReadOnly = true;
		this.vNetworkHost.ToolTipText = "The host on which the vm is running";
		this.vNetworkHost.Width = 54;
		this.vNetworkFolder.HeaderText = "Folder";
		this.vNetworkFolder.MinimumWidth = 6;
		this.vNetworkFolder.Name = "vNetworkFolder";
		this.vNetworkFolder.ReadOnly = true;
		this.vNetworkFolder.ToolTipText = "VM folder name";
		this.vNetworkFolder.Width = 125;
		this.vNetworkOS.HeaderText = "OS according to the configuration file";
		this.vNetworkOS.MinimumWidth = 6;
		this.vNetworkOS.Name = "vNetworkOS";
		this.vNetworkOS.ReadOnly = true;
		this.vNetworkOS.ToolTipText = "Guest operating system according to the Configuration file.";
		this.vNetworkOS.Width = 300;
		this.vNetworkOSTools.HeaderText = "OS according to the VMware Tools";
		this.vNetworkOSTools.MinimumWidth = 6;
		this.vNetworkOSTools.Name = "vNetworkOSTools";
		this.vNetworkOSTools.ReadOnly = true;
		this.vNetworkOSTools.ToolTipText = "Guest operating system according to VMware tools";
		this.vNetworkOSTools.Width = 300;
		this.vNetworkObjectID.HeaderText = "VM ID";
		this.vNetworkObjectID.MinimumWidth = 6;
		this.vNetworkObjectID.Name = "vNetworkObjectID";
		this.vNetworkObjectID.ReadOnly = true;
		this.vNetworkObjectID.ToolTipText = "VM Object ID";
		this.vNetworkObjectID.Width = 125;
		this.vNetworkUUID.HeaderText = "VM UUID";
		this.vNetworkUUID.MinimumWidth = 6;
		this.vNetworkUUID.Name = "vNetworkUUID";
		this.vNetworkUUID.ReadOnly = true;
		this.vNetworkUUID.ToolTipText = "VirtualCenter-specific 128-bit UUID of a virtual machine";
		this.vNetworkUUID.Width = 125;
		this.vNetworkVISDKServer.HeaderText = "VI SDK Server";
		this.vNetworkVISDKServer.MinimumWidth = 6;
		this.vNetworkVISDKServer.Name = "vNetworkVISDKServer";
		this.vNetworkVISDKServer.ReadOnly = true;
		this.vNetworkVISDKServer.ToolTipText = "VI SDK Server, where RVTools gets it's information from";
		this.vNetworkVISDKServer.Width = 150;
		this.vNetworkInstanceUUID.HeaderText = "VI SDK UUID";
		this.vNetworkInstanceUUID.MinimumWidth = 6;
		this.vNetworkInstanceUUID.Name = "vNetworkInstanceUUID";
		this.vNetworkInstanceUUID.ReadOnly = true;
		this.vNetworkInstanceUUID.ToolTipText = "A globally unique identifier associated with this service instance";
		this.vNetworkInstanceUUID.Width = 125;
		this.tabvPartition.Controls.Add(this.panel16);
		this.tabvPartition.Location = new System.Drawing.Point(4, 25);
		this.tabvPartition.Margin = new System.Windows.Forms.Padding(4);
		this.tabvPartition.Name = "tabvPartition";
		this.tabvPartition.Size = new System.Drawing.Size(1725, 391);
		this.tabvPartition.TabIndex = 21;
		this.tabvPartition.Text = "vPartition";
		this.tabvPartition.ToolTipText = "Partion Information";
		this.tabvPartition.UseVisualStyleBackColor = true;
		this.panel16.Controls.Add(this.dataGridvPartition);
		this.panel16.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel16.Location = new System.Drawing.Point(0, 0);
		this.panel16.Margin = new System.Windows.Forms.Padding(4);
		this.panel16.Name = "panel16";
		this.panel16.Padding = new System.Windows.Forms.Padding(20, 18, 20, 18);
		this.panel16.Size = new System.Drawing.Size(1725, 391);
		this.panel16.TabIndex = 2;
		this.dataGridvPartition.AllowUserToAddRows = false;
		this.dataGridvPartition.AllowUserToDeleteRows = false;
		this.dataGridvPartition.AllowUserToOrderColumns = true;
		this.dataGridvPartition.AllowUserToResizeRows = false;
		dataGridViewCellStyle135.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
		this.dataGridvPartition.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle135;
		this.dataGridvPartition.BackgroundColor = System.Drawing.SystemColors.Window;
		this.dataGridvPartition.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
		this.dataGridvPartition.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGridvPartition.Columns.AddRange(this.vPartitionVMName, this.vPartitionPowerstate, this.vPartitionTemplate, this.vPartitionSRMPlaceholder, this.vPartitionDiskKey, this.vPartitionDisk, this.vPartitionCapacityMiB, this.vPartitionConsumedMiB, this.vPartitionFreeMiB, this.vPartitionFreePercentage, this.vPartitionSortColumn, this.vPartitionNotes, this.vPartitionDatacenter, this.vPartitionCluster, this.vPartitionHost, this.vPartitionFolder, this.vPartitionOS, this.vPartitionOSTools, this.vPartitionObjectID, this.vPartitionUUID, this.vPartitionVISDKServer, this.vPartitionInstanceUUID);
		this.dataGridvPartition.Dock = System.Windows.Forms.DockStyle.Fill;
		this.dataGridvPartition.Location = new System.Drawing.Point(20, 18);
		this.dataGridvPartition.Margin = new System.Windows.Forms.Padding(4);
		this.dataGridvPartition.Name = "dataGridvPartition";
		this.dataGridvPartition.RowHeadersVisible = false;
		this.dataGridvPartition.RowHeadersWidth = 51;
		this.dataGridvPartition.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this.dataGridvPartition.Size = new System.Drawing.Size(1685, 355);
		this.dataGridvPartition.TabIndex = 3;
		this.vPartitionVMName.Frozen = true;
		this.vPartitionVMName.HeaderText = "VM";
		this.vPartitionVMName.MinimumWidth = 6;
		this.vPartitionVMName.Name = "vPartitionVMName";
		this.vPartitionVMName.ReadOnly = true;
		this.vPartitionVMName.ToolTipText = "Virtual Machine Name";
		this.vPartitionVMName.Width = 125;
		this.vPartitionPowerstate.HeaderText = "Powerstate";
		this.vPartitionPowerstate.MinimumWidth = 6;
		this.vPartitionPowerstate.Name = "vPartitionPowerstate";
		this.vPartitionPowerstate.ReadOnly = true;
		this.vPartitionPowerstate.ToolTipText = "Power State";
		this.vPartitionPowerstate.Width = 125;
		this.vPartitionTemplate.HeaderText = "Template";
		this.vPartitionTemplate.MinimumWidth = 6;
		this.vPartitionTemplate.Name = "vPartitionTemplate";
		this.vPartitionTemplate.ReadOnly = true;
		this.vPartitionTemplate.ToolTipText = "Is VM a template?";
		this.vPartitionTemplate.Width = 125;
		this.vPartitionSRMPlaceholder.HeaderText = "SRM Placeholder";
		this.vPartitionSRMPlaceholder.MinimumWidth = 6;
		this.vPartitionSRMPlaceholder.Name = "vPartitionSRMPlaceholder";
		this.vPartitionSRMPlaceholder.ReadOnly = true;
		this.vPartitionSRMPlaceholder.ToolTipText = "Is VM a SRM Placeholder?";
		this.vPartitionSRMPlaceholder.Width = 200;
		this.vPartitionDiskKey.HeaderText = "Disk Key";
		this.vPartitionDiskKey.MinimumWidth = 6;
		this.vPartitionDiskKey.Name = "vPartitionDiskKey";
		this.vPartitionDiskKey.ReadOnly = true;
		this.vPartitionDiskKey.ToolTipText = "Disk Key";
		this.vPartitionDiskKey.Width = 125;
		this.vPartitionDisk.HeaderText = "Disk";
		this.vPartitionDisk.MinimumWidth = 6;
		this.vPartitionDisk.Name = "vPartitionDisk";
		this.vPartitionDisk.ReadOnly = true;
		this.vPartitionDisk.ToolTipText = "Disk name";
		this.vPartitionDisk.Width = 125;
		dataGridViewCellStyle136.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle136.Format = "N0";
		dataGridViewCellStyle136.NullValue = null;
		this.vPartitionCapacityMiB.DefaultCellStyle = dataGridViewCellStyle136;
		this.vPartitionCapacityMiB.HeaderText = "Capacity MiB";
		this.vPartitionCapacityMiB.MinimumWidth = 6;
		this.vPartitionCapacityMiB.Name = "vPartitionCapacityMiB";
		this.vPartitionCapacityMiB.ReadOnly = true;
		this.vPartitionCapacityMiB.ToolTipText = "Disk Capacity in MiB";
		this.vPartitionCapacityMiB.Width = 150;
		dataGridViewCellStyle137.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle137.Format = "N0";
		dataGridViewCellStyle137.NullValue = null;
		this.vPartitionConsumedMiB.DefaultCellStyle = dataGridViewCellStyle137;
		this.vPartitionConsumedMiB.HeaderText = "Consumed MiB";
		this.vPartitionConsumedMiB.MinimumWidth = 6;
		this.vPartitionConsumedMiB.Name = "vPartitionConsumedMiB";
		this.vPartitionConsumedMiB.ReadOnly = true;
		this.vPartitionConsumedMiB.ToolTipText = "Comsumed capacity in MiB";
		this.vPartitionConsumedMiB.Width = 150;
		dataGridViewCellStyle138.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle138.Format = "N0";
		dataGridViewCellStyle138.NullValue = null;
		this.vPartitionFreeMiB.DefaultCellStyle = dataGridViewCellStyle138;
		this.vPartitionFreeMiB.HeaderText = "Free MiB";
		this.vPartitionFreeMiB.MinimumWidth = 6;
		this.vPartitionFreeMiB.Name = "vPartitionFreeMiB";
		this.vPartitionFreeMiB.ReadOnly = true;
		this.vPartitionFreeMiB.ToolTipText = "Free Disk Space in MiB";
		this.vPartitionFreeMiB.Width = 125;
		dataGridViewCellStyle139.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle139.Format = "N0";
		dataGridViewCellStyle139.NullValue = null;
		this.vPartitionFreePercentage.DefaultCellStyle = dataGridViewCellStyle139;
		this.vPartitionFreePercentage.HeaderText = "Free %";
		this.vPartitionFreePercentage.MinimumWidth = 6;
		this.vPartitionFreePercentage.Name = "vPartitionFreePercentage";
		this.vPartitionFreePercentage.ReadOnly = true;
		this.vPartitionFreePercentage.ToolTipText = "Free Disk Space Percentage";
		this.vPartitionFreePercentage.Width = 125;
		this.vPartitionSortColumn.HeaderText = "Internal Sort Column";
		this.vPartitionSortColumn.MinimumWidth = 6;
		this.vPartitionSortColumn.Name = "vPartitionSortColumn";
		this.vPartitionSortColumn.ReadOnly = true;
		this.vPartitionSortColumn.ToolTipText = "Sort column used internally to sort the vPartion data";
		this.vPartitionSortColumn.Width = 250;
		this.vPartitionNotes.HeaderText = "Annotation";
		this.vPartitionNotes.MinimumWidth = 6;
		this.vPartitionNotes.Name = "vPartitionNotes";
		this.vPartitionNotes.ReadOnly = true;
		this.vPartitionNotes.ToolTipText = "Description for the virtual machine";
		this.vPartitionNotes.Width = 150;
		this.vPartitionDatacenter.HeaderText = "Datacenter";
		this.vPartitionDatacenter.MinimumWidth = 6;
		this.vPartitionDatacenter.Name = "vPartitionDatacenter";
		this.vPartitionDatacenter.ReadOnly = true;
		this.vPartitionDatacenter.ToolTipText = "Datacenter name";
		this.vPartitionDatacenter.Width = 150;
		this.vPartitionCluster.HeaderText = "Cluster";
		this.vPartitionCluster.MinimumWidth = 6;
		this.vPartitionCluster.Name = "vPartitionCluster";
		this.vPartitionCluster.ReadOnly = true;
		this.vPartitionCluster.ToolTipText = "Cluster name";
		this.vPartitionCluster.Width = 125;
		this.vPartitionHost.HeaderText = "Host";
		this.vPartitionHost.MinimumWidth = 6;
		this.vPartitionHost.Name = "vPartitionHost";
		this.vPartitionHost.ReadOnly = true;
		this.vPartitionHost.ToolTipText = "The host on which the vm is running";
		this.vPartitionHost.Width = 125;
		this.vPartitionFolder.HeaderText = "Folder";
		this.vPartitionFolder.MinimumWidth = 6;
		this.vPartitionFolder.Name = "vPartitionFolder";
		this.vPartitionFolder.ReadOnly = true;
		this.vPartitionFolder.ToolTipText = "VM folder name";
		this.vPartitionFolder.Width = 125;
		this.vPartitionOS.HeaderText = "OS according to the configuration file";
		this.vPartitionOS.MinimumWidth = 6;
		this.vPartitionOS.Name = "vPartitionOS";
		this.vPartitionOS.ReadOnly = true;
		this.vPartitionOS.ToolTipText = "Guest operating system according to the Configuration file.";
		this.vPartitionOS.Width = 300;
		this.vPartitionOSTools.HeaderText = "OS according to the VMware Tools";
		this.vPartitionOSTools.MinimumWidth = 6;
		this.vPartitionOSTools.Name = "vPartitionOSTools";
		this.vPartitionOSTools.ReadOnly = true;
		this.vPartitionOSTools.ToolTipText = "Guest operating system according to VMware tools";
		this.vPartitionOSTools.Width = 300;
		this.vPartitionObjectID.HeaderText = "VM ID";
		this.vPartitionObjectID.MinimumWidth = 6;
		this.vPartitionObjectID.Name = "vPartitionObjectID";
		this.vPartitionObjectID.ReadOnly = true;
		this.vPartitionObjectID.ToolTipText = "VM Object ID";
		this.vPartitionObjectID.Width = 125;
		this.vPartitionUUID.HeaderText = "VM UUID";
		this.vPartitionUUID.MinimumWidth = 6;
		this.vPartitionUUID.Name = "vPartitionUUID";
		this.vPartitionUUID.ReadOnly = true;
		this.vPartitionUUID.ToolTipText = "VirtualCenter-specific 128-bit UUID of a virtual machine";
		this.vPartitionUUID.Width = 150;
		this.vPartitionVISDKServer.HeaderText = "VI SDK Server";
		this.vPartitionVISDKServer.MinimumWidth = 6;
		this.vPartitionVISDKServer.Name = "vPartitionVISDKServer";
		this.vPartitionVISDKServer.ReadOnly = true;
		this.vPartitionVISDKServer.ToolTipText = "VI SDK Server, where RVTools gets it's information from";
		this.vPartitionVISDKServer.Width = 150;
		this.vPartitionInstanceUUID.HeaderText = "VI SDK UUID";
		this.vPartitionInstanceUUID.MinimumWidth = 6;
		this.vPartitionInstanceUUID.Name = "vPartitionInstanceUUID";
		this.vPartitionInstanceUUID.ReadOnly = true;
		this.vPartitionInstanceUUID.ToolTipText = "A globally unique identifier associated with this service instance";
		this.vPartitionInstanceUUID.Width = 150;
		this.tabvDisk.Controls.Add(this.panel6);
		this.tabvDisk.Location = new System.Drawing.Point(4, 25);
		this.tabvDisk.Margin = new System.Windows.Forms.Padding(4);
		this.tabvDisk.Name = "tabvDisk";
		this.tabvDisk.Size = new System.Drawing.Size(1725, 391);
		this.tabvDisk.TabIndex = 11;
		this.tabvDisk.Text = "vDisk";
		this.tabvDisk.ToolTipText = "Virtual Disk Information";
		this.tabvDisk.UseVisualStyleBackColor = true;
		this.panel6.Controls.Add(this.dataGridvDisk);
		this.panel6.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel6.Location = new System.Drawing.Point(0, 0);
		this.panel6.Margin = new System.Windows.Forms.Padding(4);
		this.panel6.Name = "panel6";
		this.panel6.Padding = new System.Windows.Forms.Padding(20, 18, 20, 18);
		this.panel6.Size = new System.Drawing.Size(1725, 391);
		this.panel6.TabIndex = 1;
		this.dataGridvDisk.AllowUserToAddRows = false;
		this.dataGridvDisk.AllowUserToDeleteRows = false;
		this.dataGridvDisk.AllowUserToOrderColumns = true;
		this.dataGridvDisk.AllowUserToResizeRows = false;
		dataGridViewCellStyle140.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
		this.dataGridvDisk.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle140;
		this.dataGridvDisk.BackgroundColor = System.Drawing.SystemColors.Window;
		this.dataGridvDisk.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
		this.dataGridvDisk.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGridvDisk.Columns.AddRange(this.vDiskVMName, this.vDiskPowerstate, this.vDiskTemplate, this.vDiskSRMPlaceholder, this.vDiskDisk, this.vDiskKey, this.vDiskDiskUUID, this.vDiskDiskPath, this.vDiskCapacityMiB, this.vDiskRaw, this.vDiskMode, this.vDiskSharingMode, this.vDiskThinProvisioned, this.vDiskEagerlyScrub, this.vDiskSplit, this.vDiskWriteThrough, this.vDiskLevel, this.vDiskShares, this.vDiskReservation, this.vDiskLimit, this.vDiskController, this.vDiskControllerLabel, this.vDiskSCSICtlrUnitNumber, this.vDiskUnit, this.vDiskControllerSharedBus, this.vDiskPath, this.vDiskRawLunId, this.vDiskRawCompMode, this.vDiskSortColumn, this.vDiskNotes, this.vDiskDatacenter, this.vDiskCluster, this.vDiskHost, this.vDiskFolder, this.vDiskOS, this.vDiskOSTools, this.vDiskObjectID, this.vDIskUUID, this.vDiskVISDKServer, this.vDiskInstanceUUID);
		this.dataGridvDisk.Dock = System.Windows.Forms.DockStyle.Fill;
		this.dataGridvDisk.Location = new System.Drawing.Point(20, 18);
		this.dataGridvDisk.Margin = new System.Windows.Forms.Padding(4);
		this.dataGridvDisk.Name = "dataGridvDisk";
		this.dataGridvDisk.RowHeadersVisible = false;
		this.dataGridvDisk.RowHeadersWidth = 51;
		this.dataGridvDisk.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this.dataGridvDisk.Size = new System.Drawing.Size(1685, 355);
		this.dataGridvDisk.TabIndex = 3;
		this.vDiskVMName.Frozen = true;
		this.vDiskVMName.HeaderText = "VM";
		this.vDiskVMName.MinimumWidth = 6;
		this.vDiskVMName.Name = "vDiskVMName";
		this.vDiskVMName.ReadOnly = true;
		this.vDiskVMName.ToolTipText = "Virtual Machine Name";
		this.vDiskVMName.Width = 48;
		this.vDiskPowerstate.HeaderText = "Powerstate";
		this.vDiskPowerstate.MinimumWidth = 6;
		this.vDiskPowerstate.Name = "vDiskPowerstate";
		this.vDiskPowerstate.ReadOnly = true;
		this.vDiskPowerstate.ToolTipText = "Power State";
		this.vDiskPowerstate.Width = 125;
		this.vDiskTemplate.HeaderText = "Template";
		this.vDiskTemplate.MinimumWidth = 6;
		this.vDiskTemplate.Name = "vDiskTemplate";
		this.vDiskTemplate.ReadOnly = true;
		this.vDiskTemplate.ToolTipText = "Is VM a template?";
		this.vDiskTemplate.Width = 125;
		this.vDiskSRMPlaceholder.HeaderText = "SRM Placeholder";
		this.vDiskSRMPlaceholder.MinimumWidth = 6;
		this.vDiskSRMPlaceholder.Name = "vDiskSRMPlaceholder";
		this.vDiskSRMPlaceholder.ReadOnly = true;
		this.vDiskSRMPlaceholder.ToolTipText = "Is VM a SRM Placeholder?";
		this.vDiskSRMPlaceholder.Width = 200;
		this.vDiskDisk.HeaderText = "Disk";
		this.vDiskDisk.MinimumWidth = 6;
		this.vDiskDisk.Name = "vDiskDisk";
		this.vDiskDisk.ReadOnly = true;
		this.vDiskDisk.ToolTipText = "Disk name";
		this.vDiskDisk.Width = 53;
		this.vDiskKey.HeaderText = "Disk Key";
		this.vDiskKey.MinimumWidth = 6;
		this.vDiskKey.Name = "vDiskKey";
		this.vDiskKey.ReadOnly = true;
		this.vDiskKey.ToolTipText = "Disk Key";
		this.vDiskKey.Width = 125;
		this.vDiskDiskUUID.HeaderText = "Disk UUID";
		this.vDiskDiskUUID.MinimumWidth = 6;
		this.vDiskDiskUUID.Name = "vDiskDiskUUID";
		this.vDiskDiskUUID.ReadOnly = true;
		this.vDiskDiskUUID.ToolTipText = "Disk UUID for the virtual disk, if available.";
		this.vDiskDiskUUID.Width = 125;
		this.vDiskDiskPath.HeaderText = "Disk Path";
		this.vDiskDiskPath.MinimumWidth = 6;
		this.vDiskDiskPath.Name = "vDiskDiskPath";
		this.vDiskDiskPath.ReadOnly = true;
		this.vDiskDiskPath.ToolTipText = "Name of the virtual disk in the guest operating system. For example: C:\\ ";
		this.vDiskDiskPath.Width = 125;
		dataGridViewCellStyle141.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle141.Format = "N0";
		dataGridViewCellStyle141.NullValue = null;
		this.vDiskCapacityMiB.DefaultCellStyle = dataGridViewCellStyle141;
		this.vDiskCapacityMiB.HeaderText = "Capacity MiB";
		this.vDiskCapacityMiB.MinimumWidth = 6;
		this.vDiskCapacityMiB.Name = "vDiskCapacityMiB";
		this.vDiskCapacityMiB.ReadOnly = true;
		this.vDiskCapacityMiB.ToolTipText = "Disk Capacity in MiB";
		this.vDiskCapacityMiB.Width = 92;
		this.vDiskRaw.HeaderText = "Raw";
		this.vDiskRaw.MinimumWidth = 6;
		this.vDiskRaw.Name = "vDiskRaw";
		this.vDiskRaw.ReadOnly = true;
		this.vDiskRaw.Width = 125;
		this.vDiskMode.HeaderText = "Disk Mode";
		this.vDiskMode.MinimumWidth = 6;
		this.vDiskMode.Name = "vDiskMode";
		this.vDiskMode.ReadOnly = true;
		this.vDiskMode.ToolTipText = "The disk persistence mode.";
		this.vDiskMode.Width = 125;
		this.vDiskSharingMode.HeaderText = "Sharing mode";
		this.vDiskSharingMode.MinimumWidth = 6;
		this.vDiskSharingMode.Name = "vDiskSharingMode";
		this.vDiskSharingMode.ReadOnly = true;
		this.vDiskSharingMode.ToolTipText = "The sharing mode of the virtual disk";
		this.vDiskSharingMode.Width = 200;
		this.vDiskThinProvisioned.HeaderText = "Thin";
		this.vDiskThinProvisioned.MinimumWidth = 6;
		this.vDiskThinProvisioned.Name = "vDiskThinProvisioned";
		this.vDiskThinProvisioned.ReadOnly = true;
		this.vDiskThinProvisioned.ToolTipText = "Thin Provisioned?";
		this.vDiskThinProvisioned.Width = 53;
		this.vDiskEagerlyScrub.HeaderText = "Eagerly Scrub";
		this.vDiskEagerlyScrub.MinimumWidth = 6;
		this.vDiskEagerlyScrub.Name = "vDiskEagerlyScrub";
		this.vDiskEagerlyScrub.ReadOnly = true;
		this.vDiskEagerlyScrub.ToolTipText = "Flag to indicate to the underlying filesystem whether the virtual disk backing file should be scrubbed completely at this time";
		this.vDiskEagerlyScrub.Width = 125;
		this.vDiskSplit.HeaderText = "Split";
		this.vDiskSplit.MinimumWidth = 6;
		this.vDiskSplit.Name = "vDiskSplit";
		this.vDiskSplit.ReadOnly = true;
		this.vDiskSplit.ToolTipText = "Split?";
		this.vDiskSplit.Width = 53;
		this.vDiskWriteThrough.HeaderText = "Write Through";
		this.vDiskWriteThrough.MinimumWidth = 6;
		this.vDiskWriteThrough.Name = "vDiskWriteThrough";
		this.vDiskWriteThrough.ReadOnly = true;
		this.vDiskWriteThrough.ToolTipText = "Flag to indicate whether writes should go directly to the file system or should be buffered";
		this.vDiskWriteThrough.Width = 125;
		this.vDiskLevel.HeaderText = "Level";
		this.vDiskLevel.MinimumWidth = 6;
		this.vDiskLevel.Name = "vDiskLevel";
		this.vDiskLevel.ReadOnly = true;
		this.vDiskLevel.ToolTipText = "Share Level";
		this.vDiskLevel.Width = 125;
		dataGridViewCellStyle142.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle142.Format = "N0";
		dataGridViewCellStyle142.NullValue = null;
		this.vDiskShares.DefaultCellStyle = dataGridViewCellStyle142;
		this.vDiskShares.HeaderText = "Shares";
		this.vDiskShares.MinimumWidth = 6;
		this.vDiskShares.Name = "vDiskShares";
		this.vDiskShares.ReadOnly = true;
		this.vDiskShares.ToolTipText = "Number of Shares";
		this.vDiskShares.Width = 125;
		dataGridViewCellStyle143.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle143.Format = "N0";
		dataGridViewCellStyle143.NullValue = null;
		this.vDiskReservation.DefaultCellStyle = dataGridViewCellStyle143;
		this.vDiskReservation.HeaderText = "Reservation";
		this.vDiskReservation.MinimumWidth = 6;
		this.vDiskReservation.Name = "vDiskReservation";
		this.vDiskReservation.ReadOnly = true;
		this.vDiskReservation.Width = 125;
		dataGridViewCellStyle144.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle144.Format = "N0";
		dataGridViewCellStyle144.NullValue = null;
		this.vDiskLimit.DefaultCellStyle = dataGridViewCellStyle144;
		this.vDiskLimit.HeaderText = "Limit";
		this.vDiskLimit.MinimumWidth = 6;
		this.vDiskLimit.Name = "vDiskLimit";
		this.vDiskLimit.ReadOnly = true;
		this.vDiskLimit.Width = 125;
		this.vDiskController.HeaderText = "Controller";
		this.vDiskController.MinimumWidth = 6;
		this.vDiskController.Name = "vDiskController";
		this.vDiskController.ReadOnly = true;
		this.vDiskController.ToolTipText = "SCSI Controller";
		this.vDiskController.Width = 125;
		this.vDiskControllerLabel.HeaderText = "Label";
		this.vDiskControllerLabel.MinimumWidth = 6;
		this.vDiskControllerLabel.Name = "vDiskControllerLabel";
		this.vDiskControllerLabel.ReadOnly = true;
		this.vDiskControllerLabel.ToolTipText = "Display label";
		this.vDiskControllerLabel.Width = 125;
		dataGridViewCellStyle145.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle145.Format = "N0";
		dataGridViewCellStyle145.NullValue = null;
		this.vDiskSCSICtlrUnitNumber.DefaultCellStyle = dataGridViewCellStyle145;
		this.vDiskSCSICtlrUnitNumber.HeaderText = "SCSI Unit #";
		this.vDiskSCSICtlrUnitNumber.MinimumWidth = 6;
		this.vDiskSCSICtlrUnitNumber.Name = "vDiskSCSICtlrUnitNumber";
		this.vDiskSCSICtlrUnitNumber.ReadOnly = true;
		this.vDiskSCSICtlrUnitNumber.ToolTipText = "The unit number of the SCSI controller";
		this.vDiskSCSICtlrUnitNumber.Width = 125;
		dataGridViewCellStyle146.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle146.Format = "N0";
		dataGridViewCellStyle146.NullValue = null;
		this.vDiskUnit.DefaultCellStyle = dataGridViewCellStyle146;
		this.vDiskUnit.HeaderText = "Unit #";
		this.vDiskUnit.MinimumWidth = 6;
		this.vDiskUnit.Name = "vDiskUnit";
		this.vDiskUnit.ReadOnly = true;
		this.vDiskUnit.ToolTipText = "Unit number of this device";
		this.vDiskUnit.Width = 125;
		this.vDiskControllerSharedBus.HeaderText = "Shared Bus";
		this.vDiskControllerSharedBus.MinimumWidth = 6;
		this.vDiskControllerSharedBus.Name = "vDiskControllerSharedBus";
		this.vDiskControllerSharedBus.ReadOnly = true;
		this.vDiskControllerSharedBus.ToolTipText = "Mode for sharing the SCSI bus";
		this.vDiskControllerSharedBus.Width = 125;
		this.vDiskPath.HeaderText = "Path";
		this.vDiskPath.MinimumWidth = 6;
		this.vDiskPath.Name = "vDiskPath";
		this.vDiskPath.ReadOnly = true;
		this.vDiskPath.ToolTipText = "vmdk file name";
		this.vDiskPath.Width = 125;
		this.vDiskRawLunId.HeaderText = "Raw LUN ID";
		this.vDiskRawLunId.MinimumWidth = 6;
		this.vDiskRawLunId.Name = "vDiskRawLunId";
		this.vDiskRawLunId.ReadOnly = true;
		this.vDiskRawLunId.ToolTipText = "Unique identifier of the LUN accessed by the raw disk mapping";
		this.vDiskRawLunId.Width = 125;
		this.vDiskRawCompMode.HeaderText = "Raw Comp. Mode";
		this.vDiskRawCompMode.MinimumWidth = 6;
		this.vDiskRawCompMode.Name = "vDiskRawCompMode";
		this.vDiskRawCompMode.ReadOnly = true;
		this.vDiskRawCompMode.ToolTipText = "The compatibility mode of the raw disk mapping (RDM)";
		this.vDiskRawCompMode.Width = 150;
		this.vDiskSortColumn.HeaderText = "Internal Sort Column";
		this.vDiskSortColumn.MinimumWidth = 6;
		this.vDiskSortColumn.Name = "vDiskSortColumn";
		this.vDiskSortColumn.ReadOnly = true;
		this.vDiskSortColumn.ToolTipText = "Sort Column is used internally for sorting the vDisk data";
		this.vDiskSortColumn.Width = 250;
		this.vDiskNotes.HeaderText = "Annotation";
		this.vDiskNotes.MinimumWidth = 6;
		this.vDiskNotes.Name = "vDiskNotes";
		this.vDiskNotes.ReadOnly = true;
		this.vDiskNotes.ToolTipText = "Description for the virtual machine";
		this.vDiskNotes.Width = 60;
		this.vDiskDatacenter.HeaderText = "Datacenter";
		this.vDiskDatacenter.MinimumWidth = 6;
		this.vDiskDatacenter.Name = "vDiskDatacenter";
		this.vDiskDatacenter.ReadOnly = true;
		this.vDiskDatacenter.ToolTipText = "Datacenter name";
		this.vDiskDatacenter.Width = 125;
		this.vDiskCluster.HeaderText = "Cluster";
		this.vDiskCluster.MinimumWidth = 6;
		this.vDiskCluster.Name = "vDiskCluster";
		this.vDiskCluster.ReadOnly = true;
		this.vDiskCluster.ToolTipText = "Cluster name";
		this.vDiskCluster.Width = 125;
		this.vDiskHost.HeaderText = "Host";
		this.vDiskHost.MinimumWidth = 6;
		this.vDiskHost.Name = "vDiskHost";
		this.vDiskHost.ReadOnly = true;
		this.vDiskHost.ToolTipText = "The host on which the vm is running";
		this.vDiskHost.Width = 54;
		this.vDiskFolder.HeaderText = "Folder";
		this.vDiskFolder.MinimumWidth = 6;
		this.vDiskFolder.Name = "vDiskFolder";
		this.vDiskFolder.ReadOnly = true;
		this.vDiskFolder.ToolTipText = "VM folder name";
		this.vDiskFolder.Width = 125;
		this.vDiskOS.HeaderText = "OS according to the configuration file";
		this.vDiskOS.MinimumWidth = 6;
		this.vDiskOS.Name = "vDiskOS";
		this.vDiskOS.ReadOnly = true;
		this.vDiskOS.ToolTipText = "OS according to the configuration file";
		this.vDiskOS.Width = 300;
		this.vDiskOSTools.HeaderText = "OS according to the VMware Tools";
		this.vDiskOSTools.MinimumWidth = 6;
		this.vDiskOSTools.Name = "vDiskOSTools";
		this.vDiskOSTools.ReadOnly = true;
		this.vDiskOSTools.Width = 300;
		this.vDiskObjectID.HeaderText = "VM ID";
		this.vDiskObjectID.MinimumWidth = 6;
		this.vDiskObjectID.Name = "vDiskObjectID";
		this.vDiskObjectID.ReadOnly = true;
		this.vDiskObjectID.ToolTipText = "VM Object ID";
		this.vDiskObjectID.Width = 125;
		this.vDIskUUID.HeaderText = "VM UUID";
		this.vDIskUUID.MinimumWidth = 6;
		this.vDIskUUID.Name = "vDIskUUID";
		this.vDIskUUID.ReadOnly = true;
		this.vDIskUUID.ToolTipText = "VirtualCenter-specific 128-bit UUID of a virtual machine";
		this.vDIskUUID.Width = 125;
		this.vDiskVISDKServer.HeaderText = "VI SDK Server";
		this.vDiskVISDKServer.MinimumWidth = 6;
		this.vDiskVISDKServer.Name = "vDiskVISDKServer";
		this.vDiskVISDKServer.ReadOnly = true;
		this.vDiskVISDKServer.ToolTipText = "VI SDK Server, where RVTools gets it's information from";
		this.vDiskVISDKServer.Width = 150;
		this.vDiskInstanceUUID.HeaderText = "VI SDK UUID";
		this.vDiskInstanceUUID.MinimumWidth = 6;
		this.vDiskInstanceUUID.Name = "vDiskInstanceUUID";
		this.vDiskInstanceUUID.ReadOnly = true;
		this.vDiskInstanceUUID.ToolTipText = "A globally unique identifier associated with this service instance";
		this.vDiskInstanceUUID.Width = 125;
		this.tabvMemory.Controls.Add(this.panel8);
		this.tabvMemory.Location = new System.Drawing.Point(4, 25);
		this.tabvMemory.Margin = new System.Windows.Forms.Padding(4);
		this.tabvMemory.Name = "tabvMemory";
		this.tabvMemory.Size = new System.Drawing.Size(1725, 391);
		this.tabvMemory.TabIndex = 10;
		this.tabvMemory.Text = "vMemory";
		this.tabvMemory.ToolTipText = "Virtual Memory Information";
		this.tabvMemory.UseVisualStyleBackColor = true;
		this.panel8.Controls.Add(this.dataGridvMemory);
		this.panel8.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel8.Location = new System.Drawing.Point(0, 0);
		this.panel8.Margin = new System.Windows.Forms.Padding(4);
		this.panel8.Name = "panel8";
		this.panel8.Padding = new System.Windows.Forms.Padding(20, 18, 20, 18);
		this.panel8.Size = new System.Drawing.Size(1725, 391);
		this.panel8.TabIndex = 3;
		this.dataGridvMemory.AllowUserToAddRows = false;
		this.dataGridvMemory.AllowUserToDeleteRows = false;
		this.dataGridvMemory.AllowUserToOrderColumns = true;
		this.dataGridvMemory.AllowUserToResizeRows = false;
		dataGridViewCellStyle147.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
		this.dataGridvMemory.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle147;
		this.dataGridvMemory.BackgroundColor = System.Drawing.SystemColors.Window;
		this.dataGridvMemory.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
		this.dataGridvMemory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGridvMemory.Columns.AddRange(this.vMemoryVMName, this.vMemoryPowerstate, this.vMemoryTemplate, this.vMemorySRMPlaceholder, this.vMemorySizeMiB, this.vMemoryReservationLockedToMax, this.vMemoryOverhead, this.vMemoryMaxUsage, this.vMemoryHostMemoryUsage, this.vMemoryconsumedOverheadMemory, this.vMemoryprivateMemory, this.vMemorysharedMemory, this.vMemoryswappedMemory, this.vMemoryballoonedMemory, this.vMemoryGuestMemoryUsage, this.vMemorystaticMemoryEntitlement, this.vMemorydistributedMemoryEntitlement, this.vMemorySharesLevel, this.vMemoryShares, this.vMemoryReservation, this.vMemoryReservationLimit, this.vMemoryHotAdd, this.vMemoryNotes, this.vMemoryDatacenter, this.vMemoryCluster, this.vMemoryHost, this.vMemoryFolder, this.vMemoryOS, this.vMemoryOSTools, this.vMemoryObjectID, this.vMemoryUUID, this.vMemoryVISDKServer, this.vMemoryInstanceUUID);
		this.dataGridvMemory.Dock = System.Windows.Forms.DockStyle.Fill;
		this.dataGridvMemory.Location = new System.Drawing.Point(20, 18);
		this.dataGridvMemory.Margin = new System.Windows.Forms.Padding(4);
		this.dataGridvMemory.Name = "dataGridvMemory";
		this.dataGridvMemory.RowHeadersVisible = false;
		this.dataGridvMemory.RowHeadersWidth = 51;
		this.dataGridvMemory.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this.dataGridvMemory.Size = new System.Drawing.Size(1685, 355);
		this.dataGridvMemory.TabIndex = 2;
		this.vMemoryVMName.Frozen = true;
		this.vMemoryVMName.HeaderText = "VM";
		this.vMemoryVMName.MinimumWidth = 6;
		this.vMemoryVMName.Name = "vMemoryVMName";
		this.vMemoryVMName.ReadOnly = true;
		this.vMemoryVMName.ToolTipText = "Virtual Machine Name";
		this.vMemoryVMName.Width = 48;
		this.vMemoryPowerstate.HeaderText = "Powerstate";
		this.vMemoryPowerstate.MinimumWidth = 6;
		this.vMemoryPowerstate.Name = "vMemoryPowerstate";
		this.vMemoryPowerstate.ReadOnly = true;
		this.vMemoryPowerstate.ToolTipText = "Power State";
		this.vMemoryPowerstate.Width = 125;
		this.vMemoryTemplate.HeaderText = "Template";
		this.vMemoryTemplate.MinimumWidth = 6;
		this.vMemoryTemplate.Name = "vMemoryTemplate";
		this.vMemoryTemplate.ReadOnly = true;
		this.vMemoryTemplate.ToolTipText = "Is VM a template?";
		this.vMemoryTemplate.Width = 125;
		this.vMemorySRMPlaceholder.HeaderText = "SRM Placeholder";
		this.vMemorySRMPlaceholder.MinimumWidth = 6;
		this.vMemorySRMPlaceholder.Name = "vMemorySRMPlaceholder";
		this.vMemorySRMPlaceholder.ReadOnly = true;
		this.vMemorySRMPlaceholder.ToolTipText = "Is VM a SRM Placeholder?";
		this.vMemorySRMPlaceholder.Width = 200;
		dataGridViewCellStyle148.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle148.Format = "N0";
		dataGridViewCellStyle148.NullValue = null;
		this.vMemorySizeMiB.DefaultCellStyle = dataGridViewCellStyle148;
		this.vMemorySizeMiB.HeaderText = "Size MiB";
		this.vMemorySizeMiB.MinimumWidth = 6;
		this.vMemorySizeMiB.Name = "vMemorySizeMiB";
		this.vMemorySizeMiB.ReadOnly = true;
		this.vMemorySizeMiB.ToolTipText = "Memory size in MiB";
		this.vMemorySizeMiB.Width = 71;
		this.vMemoryReservationLockedToMax.HeaderText = "Memory Reservation Locked To Max";
		this.vMemoryReservationLockedToMax.MinimumWidth = 6;
		this.vMemoryReservationLockedToMax.Name = "vMemoryReservationLockedToMax";
		this.vMemoryReservationLockedToMax.ReadOnly = true;
		this.vMemoryReservationLockedToMax.ToolTipText = "If set true, memory resource reservation for this virtual machine will always be equal to the virtual machine's memory size";
		this.vMemoryReservationLockedToMax.Width = 400;
		dataGridViewCellStyle149.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle149.Format = "N0";
		dataGridViewCellStyle149.NullValue = null;
		this.vMemoryOverhead.DefaultCellStyle = dataGridViewCellStyle149;
		this.vMemoryOverhead.HeaderText = "Overhead";
		this.vMemoryOverhead.MinimumWidth = 6;
		this.vMemoryOverhead.Name = "vMemoryOverhead";
		this.vMemoryOverhead.ReadOnly = true;
		this.vMemoryOverhead.ToolTipText = "Memory overhead in MiB";
		this.vMemoryOverhead.Width = 125;
		dataGridViewCellStyle150.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle150.Format = "N0";
		dataGridViewCellStyle150.NullValue = null;
		this.vMemoryMaxUsage.DefaultCellStyle = dataGridViewCellStyle150;
		this.vMemoryMaxUsage.HeaderText = "Max";
		this.vMemoryMaxUsage.MinimumWidth = 6;
		this.vMemoryMaxUsage.Name = "vMemoryMaxUsage";
		this.vMemoryMaxUsage.ReadOnly = true;
		this.vMemoryMaxUsage.ToolTipText = "Current upper-bound on memory usage";
		this.vMemoryMaxUsage.Width = 52;
		dataGridViewCellStyle151.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle151.Format = "N0";
		dataGridViewCellStyle151.NullValue = null;
		this.vMemoryHostMemoryUsage.DefaultCellStyle = dataGridViewCellStyle151;
		this.vMemoryHostMemoryUsage.HeaderText = "Consumed";
		this.vMemoryHostMemoryUsage.MinimumWidth = 6;
		this.vMemoryHostMemoryUsage.Name = "vMemoryHostMemoryUsage";
		this.vMemoryHostMemoryUsage.ReadOnly = true;
		this.vMemoryHostMemoryUsage.ToolTipText = "Host memory: Actual consumption of physical memory that has been allocated to the virtual machine.";
		this.vMemoryHostMemoryUsage.Width = 83;
		dataGridViewCellStyle152.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle152.Format = "N0";
		dataGridViewCellStyle152.NullValue = null;
		this.vMemoryconsumedOverheadMemory.DefaultCellStyle = dataGridViewCellStyle152;
		this.vMemoryconsumedOverheadMemory.HeaderText = "Consumed Overhead";
		this.vMemoryconsumedOverheadMemory.MinimumWidth = 6;
		this.vMemoryconsumedOverheadMemory.Name = "vMemoryconsumedOverheadMemory";
		this.vMemoryconsumedOverheadMemory.ReadOnly = true;
		this.vMemoryconsumedOverheadMemory.ToolTipText = "Host memory: Amount of consumed memory in MiB, being used for virtualization purposes.";
		this.vMemoryconsumedOverheadMemory.Width = 150;
		dataGridViewCellStyle153.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle153.Format = "N0";
		dataGridViewCellStyle153.NullValue = null;
		this.vMemoryprivateMemory.DefaultCellStyle = dataGridViewCellStyle153;
		this.vMemoryprivateMemory.HeaderText = "Private";
		this.vMemoryprivateMemory.MinimumWidth = 6;
		this.vMemoryprivateMemory.Name = "vMemoryprivateMemory";
		this.vMemoryprivateMemory.ReadOnly = true;
		this.vMemoryprivateMemory.ToolTipText = "Guest memory: Amount of memory in MiB, backed by host memory and not being shared.";
		this.vMemoryprivateMemory.Width = 125;
		dataGridViewCellStyle154.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle154.Format = "N0";
		dataGridViewCellStyle154.NullValue = null;
		this.vMemorysharedMemory.DefaultCellStyle = dataGridViewCellStyle154;
		this.vMemorysharedMemory.HeaderText = "Shared";
		this.vMemorysharedMemory.MinimumWidth = 6;
		this.vMemorysharedMemory.Name = "vMemorysharedMemory";
		this.vMemorysharedMemory.ReadOnly = true;
		this.vMemorysharedMemory.ToolTipText = "Guest memory: Amount of memory in MiB, being shared.";
		this.vMemorysharedMemory.Width = 125;
		dataGridViewCellStyle155.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle155.Format = "N0";
		dataGridViewCellStyle155.NullValue = null;
		this.vMemoryswappedMemory.DefaultCellStyle = dataGridViewCellStyle155;
		this.vMemoryswappedMemory.HeaderText = "Swapped";
		this.vMemoryswappedMemory.MinimumWidth = 6;
		this.vMemoryswappedMemory.Name = "vMemoryswappedMemory";
		this.vMemoryswappedMemory.ReadOnly = true;
		this.vMemoryswappedMemory.ToolTipText = "Guest memory: Amount of memory in MiB, reclaimed by swapping.";
		this.vMemoryswappedMemory.Width = 125;
		dataGridViewCellStyle156.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle156.Format = "N0";
		dataGridViewCellStyle156.NullValue = null;
		this.vMemoryballoonedMemory.DefaultCellStyle = dataGridViewCellStyle156;
		this.vMemoryballoonedMemory.HeaderText = "Ballooned";
		this.vMemoryballoonedMemory.MinimumWidth = 6;
		this.vMemoryballoonedMemory.Name = "vMemoryballoonedMemory";
		this.vMemoryballoonedMemory.ReadOnly = true;
		this.vMemoryballoonedMemory.ToolTipText = "Guest memory: Amount of memory in MiB, reclaimed by ballooning.";
		this.vMemoryballoonedMemory.Width = 125;
		dataGridViewCellStyle157.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle157.Format = "N0";
		dataGridViewCellStyle157.NullValue = null;
		this.vMemoryGuestMemoryUsage.DefaultCellStyle = dataGridViewCellStyle157;
		this.vMemoryGuestMemoryUsage.HeaderText = "Active";
		this.vMemoryGuestMemoryUsage.MinimumWidth = 6;
		this.vMemoryGuestMemoryUsage.Name = "vMemoryGuestMemoryUsage";
		this.vMemoryGuestMemoryUsage.ReadOnly = true;
		this.vMemoryGuestMemoryUsage.ToolTipText = "Guest memory: Amount of memory in MiB, recently accessed.";
		this.vMemoryGuestMemoryUsage.Width = 89;
		dataGridViewCellStyle158.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle158.Format = "N0";
		dataGridViewCellStyle158.NullValue = null;
		this.vMemorystaticMemoryEntitlement.DefaultCellStyle = dataGridViewCellStyle158;
		this.vMemorystaticMemoryEntitlement.HeaderText = "Entitlement";
		this.vMemorystaticMemoryEntitlement.MinimumWidth = 6;
		this.vMemorystaticMemoryEntitlement.Name = "vMemorystaticMemoryEntitlement";
		this.vMemorystaticMemoryEntitlement.ReadOnly = true;
		this.vMemorystaticMemoryEntitlement.ToolTipText = "The static memory resource entitlement for a virtual machine";
		this.vMemorystaticMemoryEntitlement.Width = 125;
		dataGridViewCellStyle159.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle159.Format = "N0";
		dataGridViewCellStyle159.NullValue = null;
		this.vMemorydistributedMemoryEntitlement.DefaultCellStyle = dataGridViewCellStyle159;
		this.vMemorydistributedMemoryEntitlement.HeaderText = "DRS Entitlement";
		this.vMemorydistributedMemoryEntitlement.MinimumWidth = 6;
		this.vMemorydistributedMemoryEntitlement.Name = "vMemorydistributedMemoryEntitlement";
		this.vMemorydistributedMemoryEntitlement.ReadOnly = true;
		this.vMemorydistributedMemoryEntitlement.ToolTipText = "This is the amount of memory, in MiB, that this VM is entitled to, as calculated by DRS";
		this.vMemorydistributedMemoryEntitlement.Width = 150;
		this.vMemorySharesLevel.HeaderText = "Level";
		this.vMemorySharesLevel.MinimumWidth = 6;
		this.vMemorySharesLevel.Name = "vMemorySharesLevel";
		this.vMemorySharesLevel.ReadOnly = true;
		this.vMemorySharesLevel.ToolTipText = "Share level";
		this.vMemorySharesLevel.Width = 58;
		dataGridViewCellStyle160.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle160.Format = "N0";
		dataGridViewCellStyle160.NullValue = null;
		this.vMemoryShares.DefaultCellStyle = dataGridViewCellStyle160;
		this.vMemoryShares.HeaderText = "Shares";
		this.vMemoryShares.MinimumWidth = 6;
		this.vMemoryShares.Name = "vMemoryShares";
		this.vMemoryShares.ReadOnly = true;
		this.vMemoryShares.ToolTipText = "Number of shares";
		this.vMemoryShares.Width = 65;
		dataGridViewCellStyle161.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle161.Format = "N0";
		dataGridViewCellStyle161.NullValue = null;
		this.vMemoryReservation.DefaultCellStyle = dataGridViewCellStyle161;
		this.vMemoryReservation.HeaderText = "Reservation";
		this.vMemoryReservation.MinimumWidth = 6;
		this.vMemoryReservation.Name = "vMemoryReservation";
		this.vMemoryReservation.ReadOnly = true;
		this.vMemoryReservation.ToolTipText = "Memory reservation";
		this.vMemoryReservation.Width = 89;
		dataGridViewCellStyle162.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle162.Format = "N0";
		dataGridViewCellStyle162.NullValue = null;
		this.vMemoryReservationLimit.DefaultCellStyle = dataGridViewCellStyle162;
		this.vMemoryReservationLimit.HeaderText = "Limit";
		this.vMemoryReservationLimit.MinimumWidth = 6;
		this.vMemoryReservationLimit.Name = "vMemoryReservationLimit";
		this.vMemoryReservationLimit.ReadOnly = true;
		this.vMemoryReservationLimit.ToolTipText = "Memory limit";
		this.vMemoryReservationLimit.Width = 53;
		this.vMemoryHotAdd.HeaderText = "Hot Add";
		this.vMemoryHotAdd.MinimumWidth = 6;
		this.vMemoryHotAdd.Name = "vMemoryHotAdd";
		this.vMemoryHotAdd.ReadOnly = true;
		this.vMemoryHotAdd.ToolTipText = "Whether memory can be added while this virtual machine is running";
		this.vMemoryHotAdd.Width = 125;
		this.vMemoryNotes.HeaderText = "Annotation";
		this.vMemoryNotes.MinimumWidth = 6;
		this.vMemoryNotes.Name = "vMemoryNotes";
		this.vMemoryNotes.ReadOnly = true;
		this.vMemoryNotes.ToolTipText = "Description for the virtual machine";
		this.vMemoryNotes.Width = 60;
		this.vMemoryDatacenter.HeaderText = "Datacenter";
		this.vMemoryDatacenter.MinimumWidth = 6;
		this.vMemoryDatacenter.Name = "vMemoryDatacenter";
		this.vMemoryDatacenter.ReadOnly = true;
		this.vMemoryDatacenter.ToolTipText = "Datacenter name";
		this.vMemoryDatacenter.Width = 125;
		this.vMemoryCluster.HeaderText = "Cluster";
		this.vMemoryCluster.MinimumWidth = 6;
		this.vMemoryCluster.Name = "vMemoryCluster";
		this.vMemoryCluster.ReadOnly = true;
		this.vMemoryCluster.ToolTipText = "Cluster name";
		this.vMemoryCluster.Width = 125;
		this.vMemoryHost.HeaderText = "Host";
		this.vMemoryHost.MinimumWidth = 6;
		this.vMemoryHost.Name = "vMemoryHost";
		this.vMemoryHost.ReadOnly = true;
		this.vMemoryHost.ToolTipText = "The host on which the vm is running";
		this.vMemoryHost.Width = 54;
		this.vMemoryFolder.HeaderText = "Folder";
		this.vMemoryFolder.MinimumWidth = 6;
		this.vMemoryFolder.Name = "vMemoryFolder";
		this.vMemoryFolder.ReadOnly = true;
		this.vMemoryFolder.ToolTipText = "VM folder name";
		this.vMemoryFolder.Width = 125;
		this.vMemoryOS.HeaderText = "OS according to the configuration file";
		this.vMemoryOS.MinimumWidth = 6;
		this.vMemoryOS.Name = "vMemoryOS";
		this.vMemoryOS.ReadOnly = true;
		this.vMemoryOS.ToolTipText = "Guest operating system according to the Configuration file.";
		this.vMemoryOS.Width = 300;
		this.vMemoryOSTools.HeaderText = "OS according to the VMware Tools";
		this.vMemoryOSTools.MinimumWidth = 6;
		this.vMemoryOSTools.Name = "vMemoryOSTools";
		this.vMemoryOSTools.ReadOnly = true;
		this.vMemoryOSTools.ToolTipText = "Guest operating system according to VMware tools";
		this.vMemoryOSTools.Width = 300;
		this.vMemoryObjectID.HeaderText = "VM ID";
		this.vMemoryObjectID.MinimumWidth = 6;
		this.vMemoryObjectID.Name = "vMemoryObjectID";
		this.vMemoryObjectID.ReadOnly = true;
		this.vMemoryObjectID.ToolTipText = "VM Object ID";
		this.vMemoryObjectID.Width = 125;
		this.vMemoryUUID.HeaderText = "VM UUID";
		this.vMemoryUUID.MinimumWidth = 6;
		this.vMemoryUUID.Name = "vMemoryUUID";
		this.vMemoryUUID.ReadOnly = true;
		this.vMemoryUUID.ToolTipText = "VirtualCenter-specific 128-bit UUID of a virtual machine";
		this.vMemoryUUID.Width = 125;
		this.vMemoryVISDKServer.HeaderText = "VI SDK Server";
		this.vMemoryVISDKServer.MinimumWidth = 6;
		this.vMemoryVISDKServer.Name = "vMemoryVISDKServer";
		this.vMemoryVISDKServer.ReadOnly = true;
		this.vMemoryVISDKServer.ToolTipText = "VI SDK Server, where RVTools gets it's information from";
		this.vMemoryVISDKServer.Width = 150;
		this.vMemoryInstanceUUID.HeaderText = "VI SDK UUID";
		this.vMemoryInstanceUUID.MinimumWidth = 6;
		this.vMemoryInstanceUUID.Name = "vMemoryInstanceUUID";
		this.vMemoryInstanceUUID.ReadOnly = true;
		this.vMemoryInstanceUUID.ToolTipText = "A globally unique identifier associated with this service instance";
		this.vMemoryInstanceUUID.Width = 125;
		this.tabvCPU.Controls.Add(this.panel7);
		this.tabvCPU.Location = new System.Drawing.Point(4, 25);
		this.tabvCPU.Margin = new System.Windows.Forms.Padding(4);
		this.tabvCPU.Name = "tabvCPU";
		this.tabvCPU.Size = new System.Drawing.Size(1725, 391);
		this.tabvCPU.TabIndex = 9;
		this.tabvCPU.Text = "vCPU";
		this.tabvCPU.ToolTipText = "Virtual CPU Information";
		this.tabvCPU.UseVisualStyleBackColor = true;
		this.panel7.Controls.Add(this.dataGridvCPU);
		this.panel7.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel7.Location = new System.Drawing.Point(0, 0);
		this.panel7.Margin = new System.Windows.Forms.Padding(4);
		this.panel7.Name = "panel7";
		this.panel7.Padding = new System.Windows.Forms.Padding(20, 18, 20, 18);
		this.panel7.Size = new System.Drawing.Size(1725, 391);
		this.panel7.TabIndex = 3;
		this.dataGridvCPU.AllowUserToAddRows = false;
		this.dataGridvCPU.AllowUserToDeleteRows = false;
		this.dataGridvCPU.AllowUserToOrderColumns = true;
		this.dataGridvCPU.AllowUserToResizeRows = false;
		dataGridViewCellStyle163.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
		this.dataGridvCPU.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle163;
		this.dataGridvCPU.BackgroundColor = System.Drawing.SystemColors.Window;
		this.dataGridvCPU.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
		this.dataGridvCPU.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGridvCPU.Columns.AddRange(this.vCPUVMName, this.vCPUPowerstate, this.vCPUTemplate, this.vCPUSRMPlaceholder, this.vCPUCPUs, this.vCPUSockets, this.vCPUCoresPerSocket, this.vCPUMaxCpuUsage, this.vCPUOverallCpuUsage, this.vCPUSharesLevel, this.vCPUShares, this.vCPUReservation, this.vCPUStaticCpuEntitlement, this.vCPUDistributedCpuEntitlement, this.vCPUReservationLimit, this.vCPUHotAdd, this.vCPUHotRemove, this.vCPUNumaOnCpuHotaddExposed, this.vCPUNotes, this.vCPUDatacenter, this.vCPUCluster, this.vCPUHost, this.vCPUFolder, this.vCPUOS, this.vCPUOSTools, this.vCPUObjectID, this.vCPUUUID, this.vCPUVISDKServer, this.vCPUInstanceUUID);
		this.dataGridvCPU.Dock = System.Windows.Forms.DockStyle.Fill;
		this.dataGridvCPU.Location = new System.Drawing.Point(20, 18);
		this.dataGridvCPU.Margin = new System.Windows.Forms.Padding(4);
		this.dataGridvCPU.Name = "dataGridvCPU";
		this.dataGridvCPU.RowHeadersVisible = false;
		this.dataGridvCPU.RowHeadersWidth = 51;
		this.dataGridvCPU.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this.dataGridvCPU.Size = new System.Drawing.Size(1685, 355);
		this.dataGridvCPU.TabIndex = 2;
		this.vCPUVMName.Frozen = true;
		this.vCPUVMName.HeaderText = "VM";
		this.vCPUVMName.MinimumWidth = 6;
		this.vCPUVMName.Name = "vCPUVMName";
		this.vCPUVMName.ReadOnly = true;
		this.vCPUVMName.ToolTipText = "Virtual Machine Name";
		this.vCPUVMName.Width = 48;
		this.vCPUPowerstate.HeaderText = "Powerstate";
		this.vCPUPowerstate.MinimumWidth = 6;
		this.vCPUPowerstate.Name = "vCPUPowerstate";
		this.vCPUPowerstate.ReadOnly = true;
		this.vCPUPowerstate.ToolTipText = "Power State";
		this.vCPUPowerstate.Width = 125;
		this.vCPUTemplate.HeaderText = "Template";
		this.vCPUTemplate.MinimumWidth = 6;
		this.vCPUTemplate.Name = "vCPUTemplate";
		this.vCPUTemplate.ReadOnly = true;
		this.vCPUTemplate.ToolTipText = "Is VM a template?";
		this.vCPUTemplate.Width = 125;
		this.vCPUSRMPlaceholder.HeaderText = "SRM Placeholder";
		this.vCPUSRMPlaceholder.MinimumWidth = 6;
		this.vCPUSRMPlaceholder.Name = "vCPUSRMPlaceholder";
		this.vCPUSRMPlaceholder.ReadOnly = true;
		this.vCPUSRMPlaceholder.ToolTipText = "Is VM a SRM Placeholder?";
		this.vCPUSRMPlaceholder.Width = 200;
		dataGridViewCellStyle164.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle164.Format = "N0";
		dataGridViewCellStyle164.NullValue = null;
		this.vCPUCPUs.DefaultCellStyle = dataGridViewCellStyle164;
		this.vCPUCPUs.HeaderText = "CPUs";
		this.vCPUCPUs.MinimumWidth = 6;
		this.vCPUCPUs.Name = "vCPUCPUs";
		this.vCPUCPUs.ReadOnly = true;
		this.vCPUCPUs.ToolTipText = "Total number of cores";
		this.vCPUCPUs.Width = 61;
		dataGridViewCellStyle165.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle165.Format = "N0";
		dataGridViewCellStyle165.NullValue = null;
		this.vCPUSockets.DefaultCellStyle = dataGridViewCellStyle165;
		this.vCPUSockets.HeaderText = "Sockets";
		this.vCPUSockets.MinimumWidth = 6;
		this.vCPUSockets.Name = "vCPUSockets";
		this.vCPUSockets.ReadOnly = true;
		this.vCPUSockets.ToolTipText = "Number of virtual sockets";
		this.vCPUSockets.Width = 125;
		dataGridViewCellStyle166.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle166.Format = "N0";
		dataGridViewCellStyle166.NullValue = null;
		this.vCPUCoresPerSocket.DefaultCellStyle = dataGridViewCellStyle166;
		this.vCPUCoresPerSocket.HeaderText = "Cores p/s";
		this.vCPUCoresPerSocket.MinimumWidth = 6;
		this.vCPUCoresPerSocket.Name = "vCPUCoresPerSocket";
		this.vCPUCoresPerSocket.ReadOnly = true;
		this.vCPUCoresPerSocket.ToolTipText = "Number of cores per socket";
		this.vCPUCoresPerSocket.Width = 125;
		dataGridViewCellStyle167.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle167.Format = "N0";
		dataGridViewCellStyle167.NullValue = null;
		this.vCPUMaxCpuUsage.DefaultCellStyle = dataGridViewCellStyle167;
		this.vCPUMaxCpuUsage.HeaderText = "Max";
		this.vCPUMaxCpuUsage.MinimumWidth = 6;
		this.vCPUMaxCpuUsage.Name = "vCPUMaxCpuUsage";
		this.vCPUMaxCpuUsage.ReadOnly = true;
		this.vCPUMaxCpuUsage.ToolTipText = "Max CPU Usage";
		this.vCPUMaxCpuUsage.Width = 52;
		dataGridViewCellStyle168.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle168.Format = "N0";
		dataGridViewCellStyle168.NullValue = null;
		this.vCPUOverallCpuUsage.DefaultCellStyle = dataGridViewCellStyle168;
		this.vCPUOverallCpuUsage.HeaderText = "Overall";
		this.vCPUOverallCpuUsage.MinimumWidth = 6;
		this.vCPUOverallCpuUsage.Name = "vCPUOverallCpuUsage";
		this.vCPUOverallCpuUsage.ReadOnly = true;
		this.vCPUOverallCpuUsage.ToolTipText = "Overall CPU Usage";
		this.vCPUOverallCpuUsage.Width = 65;
		this.vCPUSharesLevel.HeaderText = "Level";
		this.vCPUSharesLevel.MinimumWidth = 6;
		this.vCPUSharesLevel.Name = "vCPUSharesLevel";
		this.vCPUSharesLevel.ReadOnly = true;
		this.vCPUSharesLevel.ToolTipText = "Shares level";
		this.vCPUSharesLevel.Width = 58;
		dataGridViewCellStyle169.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle169.Format = "N0";
		dataGridViewCellStyle169.NullValue = null;
		this.vCPUShares.DefaultCellStyle = dataGridViewCellStyle169;
		this.vCPUShares.HeaderText = "Shares";
		this.vCPUShares.MinimumWidth = 6;
		this.vCPUShares.Name = "vCPUShares";
		this.vCPUShares.ReadOnly = true;
		this.vCPUShares.ToolTipText = "Number of Shares";
		this.vCPUShares.Width = 65;
		dataGridViewCellStyle170.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle170.Format = "N0";
		dataGridViewCellStyle170.NullValue = null;
		this.vCPUReservation.DefaultCellStyle = dataGridViewCellStyle170;
		this.vCPUReservation.HeaderText = "Reservation";
		this.vCPUReservation.MinimumWidth = 6;
		this.vCPUReservation.Name = "vCPUReservation";
		this.vCPUReservation.ReadOnly = true;
		this.vCPUReservation.ToolTipText = "CPU Reservation";
		this.vCPUReservation.Width = 89;
		dataGridViewCellStyle171.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle171.Format = "N0";
		dataGridViewCellStyle171.NullValue = null;
		this.vCPUStaticCpuEntitlement.DefaultCellStyle = dataGridViewCellStyle171;
		this.vCPUStaticCpuEntitlement.HeaderText = "Entitlement";
		this.vCPUStaticCpuEntitlement.MinimumWidth = 6;
		this.vCPUStaticCpuEntitlement.Name = "vCPUStaticCpuEntitlement";
		this.vCPUStaticCpuEntitlement.ReadOnly = true;
		this.vCPUStaticCpuEntitlement.ToolTipText = "The static CPU resource entitlement for a virtual machine";
		this.vCPUStaticCpuEntitlement.Width = 125;
		dataGridViewCellStyle172.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle172.Format = "N0";
		dataGridViewCellStyle172.NullValue = null;
		this.vCPUDistributedCpuEntitlement.DefaultCellStyle = dataGridViewCellStyle172;
		this.vCPUDistributedCpuEntitlement.HeaderText = "DRS Entitlement";
		this.vCPUDistributedCpuEntitlement.MinimumWidth = 6;
		this.vCPUDistributedCpuEntitlement.Name = "vCPUDistributedCpuEntitlement";
		this.vCPUDistributedCpuEntitlement.ReadOnly = true;
		this.vCPUDistributedCpuEntitlement.ToolTipText = "This is the amount of CPU resource, in MHz, that this VM is entitled to, as calculated by DRS";
		this.vCPUDistributedCpuEntitlement.Width = 150;
		dataGridViewCellStyle173.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle173.Format = "N0";
		dataGridViewCellStyle173.NullValue = null;
		this.vCPUReservationLimit.DefaultCellStyle = dataGridViewCellStyle173;
		this.vCPUReservationLimit.HeaderText = "Limit";
		this.vCPUReservationLimit.MinimumWidth = 6;
		this.vCPUReservationLimit.Name = "vCPUReservationLimit";
		this.vCPUReservationLimit.ReadOnly = true;
		this.vCPUReservationLimit.ToolTipText = "CPU Reservation Limit";
		this.vCPUReservationLimit.Width = 53;
		this.vCPUHotAdd.HeaderText = "Hot Add";
		this.vCPUHotAdd.MinimumWidth = 6;
		this.vCPUHotAdd.Name = "vCPUHotAdd";
		this.vCPUHotAdd.ReadOnly = true;
		this.vCPUHotAdd.ToolTipText = "Whether virtual processors can be added while this virtual machine is running.";
		this.vCPUHotAdd.Width = 125;
		this.vCPUHotRemove.HeaderText = "Hot Remove";
		this.vCPUHotRemove.MinimumWidth = 6;
		this.vCPUHotRemove.Name = "vCPUHotRemove";
		this.vCPUHotRemove.ReadOnly = true;
		this.vCPUHotRemove.ToolTipText = "Whether virtual processors can be removed while this virtual machine is running";
		this.vCPUHotRemove.Width = 125;
		this.vCPUNumaOnCpuHotaddExposed.HeaderText = "Numa Hotadd Exposed";
		this.vCPUNumaOnCpuHotaddExposed.MinimumWidth = 6;
		this.vCPUNumaOnCpuHotaddExposed.Name = "vCPUNumaOnCpuHotaddExposed";
		this.vCPUNumaOnCpuHotaddExposed.ReadOnly = true;
		this.vCPUNumaOnCpuHotaddExposed.ToolTipText = "Whether virtual NUMA topology is exposed when CPU hotadd is enabled. ";
		this.vCPUNumaOnCpuHotaddExposed.Width = 250;
		this.vCPUNotes.HeaderText = "Annotation";
		this.vCPUNotes.MinimumWidth = 6;
		this.vCPUNotes.Name = "vCPUNotes";
		this.vCPUNotes.ReadOnly = true;
		this.vCPUNotes.ToolTipText = "Description for the virtual machine";
		this.vCPUNotes.Width = 60;
		this.vCPUDatacenter.HeaderText = "Datacenter";
		this.vCPUDatacenter.MinimumWidth = 6;
		this.vCPUDatacenter.Name = "vCPUDatacenter";
		this.vCPUDatacenter.ReadOnly = true;
		this.vCPUDatacenter.ToolTipText = "Datacenter name";
		this.vCPUDatacenter.Width = 125;
		this.vCPUCluster.HeaderText = "Cluster";
		this.vCPUCluster.MinimumWidth = 6;
		this.vCPUCluster.Name = "vCPUCluster";
		this.vCPUCluster.ReadOnly = true;
		this.vCPUCluster.ToolTipText = "Cluster name";
		this.vCPUCluster.Width = 125;
		this.vCPUHost.HeaderText = "Host";
		this.vCPUHost.MinimumWidth = 6;
		this.vCPUHost.Name = "vCPUHost";
		this.vCPUHost.ReadOnly = true;
		this.vCPUHost.ToolTipText = "The host on which the vm is running";
		this.vCPUHost.Width = 54;
		this.vCPUFolder.HeaderText = "Folder";
		this.vCPUFolder.MinimumWidth = 6;
		this.vCPUFolder.Name = "vCPUFolder";
		this.vCPUFolder.ReadOnly = true;
		this.vCPUFolder.ToolTipText = "VM folder name";
		this.vCPUFolder.Width = 125;
		this.vCPUOS.HeaderText = "OS according to the configuration file";
		this.vCPUOS.MinimumWidth = 6;
		this.vCPUOS.Name = "vCPUOS";
		this.vCPUOS.ReadOnly = true;
		this.vCPUOS.ToolTipText = "Guest operating system according to the Configuration file.";
		this.vCPUOS.Width = 300;
		this.vCPUOSTools.HeaderText = "OS according to the VMware Tools";
		this.vCPUOSTools.MinimumWidth = 6;
		this.vCPUOSTools.Name = "vCPUOSTools";
		this.vCPUOSTools.ReadOnly = true;
		this.vCPUOSTools.ToolTipText = "Guest operating system according to VMware tools";
		this.vCPUOSTools.Width = 300;
		this.vCPUObjectID.HeaderText = "VM ID";
		this.vCPUObjectID.MinimumWidth = 6;
		this.vCPUObjectID.Name = "vCPUObjectID";
		this.vCPUObjectID.ReadOnly = true;
		this.vCPUObjectID.ToolTipText = "VM Object ID";
		this.vCPUObjectID.Width = 125;
		this.vCPUUUID.HeaderText = "VM UUID";
		this.vCPUUUID.MinimumWidth = 6;
		this.vCPUUUID.Name = "vCPUUUID";
		this.vCPUUUID.ReadOnly = true;
		this.vCPUUUID.ToolTipText = "VirtualCenter-specific 128-bit UUID of a virtual machine";
		this.vCPUUUID.Width = 125;
		this.vCPUVISDKServer.HeaderText = "VI SDK Server";
		this.vCPUVISDKServer.MinimumWidth = 6;
		this.vCPUVISDKServer.Name = "vCPUVISDKServer";
		this.vCPUVISDKServer.ReadOnly = true;
		this.vCPUVISDKServer.ToolTipText = "VI SDK Server, where RVTools gets it's information from";
		this.vCPUVISDKServer.Width = 150;
		this.vCPUInstanceUUID.HeaderText = "VI SDK UUID";
		this.vCPUInstanceUUID.MinimumWidth = 6;
		this.vCPUInstanceUUID.Name = "vCPUInstanceUUID";
		this.vCPUInstanceUUID.ReadOnly = true;
		this.vCPUInstanceUUID.ToolTipText = "A globally unique identifier associated with this service instance";
		this.vCPUInstanceUUID.Width = 125;
		this.tabvInfo.Controls.Add(this.panel4);
		this.tabvInfo.Location = new System.Drawing.Point(4, 25);
		this.tabvInfo.Margin = new System.Windows.Forms.Padding(4);
		this.tabvInfo.Name = "tabvInfo";
		this.tabvInfo.Size = new System.Drawing.Size(1725, 391);
		this.tabvInfo.TabIndex = 8;
		this.tabvInfo.Text = "vInfo";
		this.tabvInfo.ToolTipText = "Virtual Machine Information";
		this.tabvInfo.UseVisualStyleBackColor = true;
		this.panel4.Controls.Add(this.dataGridvInfo);
		this.panel4.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel4.Location = new System.Drawing.Point(0, 0);
		this.panel4.Margin = new System.Windows.Forms.Padding(4);
		this.panel4.Name = "panel4";
		this.panel4.Padding = new System.Windows.Forms.Padding(20, 18, 20, 18);
		this.panel4.Size = new System.Drawing.Size(1725, 391);
		this.panel4.TabIndex = 3;
		this.dataGridvInfo.AllowUserToAddRows = false;
		this.dataGridvInfo.AllowUserToDeleteRows = false;
		this.dataGridvInfo.AllowUserToOrderColumns = true;
		this.dataGridvInfo.AllowUserToResizeRows = false;
		dataGridViewCellStyle174.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
		this.dataGridvInfo.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle174;
		this.dataGridvInfo.BackgroundColor = System.Drawing.SystemColors.Window;
		this.dataGridvInfo.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
		this.dataGridvInfo.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGridvInfo.Columns.AddRange(this.vInfoVMName, this.vInfoPowerstate, this.vInfoTemplate, this.vInfoSRMPlaceHolder, this.vInfoConfigStatus, this.vInfoGuestHostName, this.vInfoConnectionState, this.vInfoGuestState, this.vInfoHeartbeat, this.vInfoConsolidationNeeded, this.vInfoBootTime, this.vInfoSuspendedToMemory, this.vInfoSuspendTime, this.vInfoSuspendInterval, this.vInfoCreateDate, this.vInfoChangeVersion, this.vInfoCPUs, this.vInfoOverallCpuReadiness, this.vInfoMemory, this.vInfoActiveMemory, this.vInfoNICs, this.vInfoNumVirtualDisks, this.vInfoTotalDiskCapacityMiB, this.vInfoFixedPassthruHotPlugEnabled, this.vInfominRequiredEVCModeKey, this.vInfoLatencySensitivity, this.vInfoOpNotificationTimeout, this.vInfoEnableUUID, this.vINfoCBT, this.vInfoPrimaryIPAddress, this.vInfoNetwork1, this.vInfoNetwork2, this.vInfoNetwork3, this.vInfoNetwork4, this.vInfoNetwork5, this.vInfoNetwork6, this.vInfoNetwork7, this.vInfoNetwork8, this.vInfoNumDisplays, this.vInfoVideoRamKiB, this.vInfoResourcepool, this.vInfoFolderId, this.vInfoFolder, this.vInfovApp, this.vInfoDASProtection, this.vInfoFaultToleranceState, this.vInfoFTInfoRole, this.vInfoFTLatencyStatus, this.vInfoFTLogBandwidth, this.vInfoFTSecondaryLatency, this.vInfoVmFailoverInProgress, this.vInfoProvisioned, this.vInfoInUse, this.vInfoUnshared, this.vInfoHARestartPriority, this.vInfoHAIsolationResponse, this.vInfoVMmonitoring, this.vInfoClusterRule, this.vInfoClusterRuleName, this.vInfoInstallBootRequired, this.vInfoBootDelay, this.vInfoBootRetryDelay, this.vInfoBootRetryEnabled, this.vInfoBootBios, this.vInfoRebootPowerOff, this.vInfoEfiSecureBoot, this.vInfoFirmware, this.VInfoVersion, this.vInfoHWUpgradeStatus, this.vInfoHWUpgradePolicy, this.vInfoHWTarget, this.vInfopath, this.vInfoLogDirectory, this.vInfoSnapshotDirectory, this.vInfoSuspendDirectory, this.vInfoNotes, this.vInfoDataCenter, this.vInfoCluster, this.vInfoHost, this.vInfoOS, this.vInfoOSTools, this.vInfoCustomizationInfo, this.vInfoGuestDetailedData, this.vInfoObjectID, this.vInfoSMBIOSUUID, this.vInfoUUID, this.vInfoVISDKServerType, this.vInfoVISDKAPI, this.vInfoVISDKServer, this.vInfoInstanceUUID);
		this.dataGridvInfo.Dock = System.Windows.Forms.DockStyle.Fill;
		this.dataGridvInfo.Location = new System.Drawing.Point(20, 18);
		this.dataGridvInfo.Margin = new System.Windows.Forms.Padding(4);
		this.dataGridvInfo.Name = "dataGridvInfo";
		this.dataGridvInfo.RowHeadersVisible = false;
		this.dataGridvInfo.RowHeadersWidth = 51;
		this.dataGridvInfo.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this.dataGridvInfo.Size = new System.Drawing.Size(1685, 355);
		this.dataGridvInfo.TabIndex = 2;
		this.vInfoVMName.Frozen = true;
		this.vInfoVMName.HeaderText = "VM";
		this.vInfoVMName.MinimumWidth = 6;
		this.vInfoVMName.Name = "vInfoVMName";
		this.vInfoVMName.ReadOnly = true;
		this.vInfoVMName.ToolTipText = "Virtual Machine Name";
		this.vInfoVMName.Width = 200;
		this.vInfoPowerstate.HeaderText = "Powerstate";
		this.vInfoPowerstate.MinimumWidth = 6;
		this.vInfoPowerstate.Name = "vInfoPowerstate";
		this.vInfoPowerstate.ReadOnly = true;
		this.vInfoPowerstate.ToolTipText = "Power State";
		this.vInfoPowerstate.Width = 125;
		this.vInfoTemplate.HeaderText = "Template";
		this.vInfoTemplate.MinimumWidth = 6;
		this.vInfoTemplate.Name = "vInfoTemplate";
		this.vInfoTemplate.ReadOnly = true;
		this.vInfoTemplate.ToolTipText = "Is VM a template?";
		this.vInfoTemplate.Width = 125;
		this.vInfoSRMPlaceHolder.HeaderText = "SRM Placeholder";
		this.vInfoSRMPlaceHolder.MinimumWidth = 6;
		this.vInfoSRMPlaceHolder.Name = "vInfoSRMPlaceHolder";
		this.vInfoSRMPlaceHolder.ReadOnly = true;
		this.vInfoSRMPlaceHolder.ToolTipText = "Is VM SRM Placeholder?";
		this.vInfoSRMPlaceHolder.Width = 200;
		this.vInfoConfigStatus.HeaderText = "Config status";
		this.vInfoConfigStatus.MinimumWidth = 6;
		this.vInfoConfigStatus.Name = "vInfoConfigStatus";
		this.vInfoConfigStatus.ReadOnly = true;
		this.vInfoConfigStatus.ToolTipText = "Config status";
		this.vInfoConfigStatus.Width = 125;
		this.vInfoGuestHostName.HeaderText = "DNS Name";
		this.vInfoGuestHostName.MinimumWidth = 6;
		this.vInfoGuestHostName.Name = "vInfoGuestHostName";
		this.vInfoGuestHostName.ReadOnly = true;
		this.vInfoGuestHostName.ToolTipText = "DNS Name of the guest operating system ";
		this.vInfoGuestHostName.Width = 125;
		this.vInfoConnectionState.HeaderText = "Connection state";
		this.vInfoConnectionState.MinimumWidth = 6;
		this.vInfoConnectionState.Name = "vInfoConnectionState";
		this.vInfoConnectionState.ReadOnly = true;
		this.vInfoConnectionState.ToolTipText = "Indicates whether or not the virtual machine is available for management";
		this.vInfoConnectionState.Width = 150;
		this.vInfoGuestState.HeaderText = "Guest state";
		this.vInfoGuestState.MinimumWidth = 6;
		this.vInfoGuestState.Name = "vInfoGuestState";
		this.vInfoGuestState.ReadOnly = true;
		this.vInfoGuestState.ToolTipText = "Operation mode of guest operating system";
		this.vInfoGuestState.Width = 125;
		this.vInfoHeartbeat.HeaderText = "Heartbeat";
		this.vInfoHeartbeat.MinimumWidth = 6;
		this.vInfoHeartbeat.Name = "vInfoHeartbeat";
		this.vInfoHeartbeat.ReadOnly = true;
		this.vInfoHeartbeat.ToolTipText = "The heartbeat status";
		this.vInfoHeartbeat.Width = 125;
		this.vInfoConsolidationNeeded.HeaderText = "Consolidation Needed";
		this.vInfoConsolidationNeeded.MinimumWidth = 6;
		this.vInfoConsolidationNeeded.Name = "vInfoConsolidationNeeded";
		this.vInfoConsolidationNeeded.ReadOnly = true;
		this.vInfoConsolidationNeeded.ToolTipText = "Whether any disk of the virtual machine requires consolidation switch.";
		this.vInfoConsolidationNeeded.Width = 200;
		dataGridViewCellStyle175.Format = "G";
		dataGridViewCellStyle175.NullValue = null;
		this.vInfoBootTime.DefaultCellStyle = dataGridViewCellStyle175;
		this.vInfoBootTime.HeaderText = "PowerOn";
		this.vInfoBootTime.MinimumWidth = 6;
		this.vInfoBootTime.Name = "vInfoBootTime";
		this.vInfoBootTime.ReadOnly = true;
		this.vInfoBootTime.ToolTipText = "The timestamp when the virtual machine was most recently powered on. ";
		this.vInfoBootTime.Width = 125;
		this.vInfoSuspendedToMemory.HeaderText = "Suspended To Memory";
		this.vInfoSuspendedToMemory.MinimumWidth = 6;
		this.vInfoSuspendedToMemory.Name = "vInfoSuspendedToMemory";
		this.vInfoSuspendedToMemory.ReadOnly = true;
		this.vInfoSuspendedToMemory.ToolTipText = "Whether the virtual machine is suspended to memory, or not.";
		this.vInfoSuspendedToMemory.Width = 400;
		dataGridViewCellStyle176.Format = "G";
		dataGridViewCellStyle176.NullValue = null;
		this.vInfoSuspendTime.DefaultCellStyle = dataGridViewCellStyle176;
		this.vInfoSuspendTime.HeaderText = "Suspend time";
		this.vInfoSuspendTime.MinimumWidth = 6;
		this.vInfoSuspendTime.Name = "vInfoSuspendTime";
		this.vInfoSuspendTime.ReadOnly = true;
		this.vInfoSuspendTime.ToolTipText = "The timestamp when the virtual machine was most recently suspended";
		this.vInfoSuspendTime.Width = 150;
		this.vInfoSuspendInterval.HeaderText = "Suspend Interval";
		this.vInfoSuspendInterval.MinimumWidth = 6;
		this.vInfoSuspendInterval.Name = "vInfoSuspendInterval";
		this.vInfoSuspendInterval.ReadOnly = true;
		this.vInfoSuspendInterval.ToolTipText = "The total time the virtual machine has been suspended since it was initially powered on";
		this.vInfoSuspendInterval.Width = 300;
		dataGridViewCellStyle177.Format = "G";
		dataGridViewCellStyle177.NullValue = null;
		this.vInfoCreateDate.DefaultCellStyle = dataGridViewCellStyle177;
		this.vInfoCreateDate.HeaderText = "Creation date";
		this.vInfoCreateDate.MinimumWidth = 6;
		this.vInfoCreateDate.Name = "vInfoCreateDate";
		this.vInfoCreateDate.ToolTipText = "Creation date of a virtual machine";
		this.vInfoCreateDate.Width = 125;
		dataGridViewCellStyle178.Format = "G";
		dataGridViewCellStyle178.NullValue = null;
		this.vInfoChangeVersion.DefaultCellStyle = dataGridViewCellStyle178;
		this.vInfoChangeVersion.HeaderText = "Change Version";
		this.vInfoChangeVersion.MinimumWidth = 6;
		this.vInfoChangeVersion.Name = "vInfoChangeVersion";
		this.vInfoChangeVersion.ReadOnly = true;
		this.vInfoChangeVersion.ToolTipText = "Unique identifier for a given version of the configuration";
		this.vInfoChangeVersion.Width = 200;
		dataGridViewCellStyle179.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle179.Format = "N0";
		dataGridViewCellStyle179.NullValue = null;
		this.vInfoCPUs.DefaultCellStyle = dataGridViewCellStyle179;
		this.vInfoCPUs.HeaderText = "CPUs";
		this.vInfoCPUs.MinimumWidth = 6;
		this.vInfoCPUs.Name = "vInfoCPUs";
		this.vInfoCPUs.ReadOnly = true;
		this.vInfoCPUs.ToolTipText = "Number of CPUs";
		this.vInfoCPUs.Width = 125;
		dataGridViewCellStyle180.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle180.NullValue = null;
		this.vInfoOverallCpuReadiness.DefaultCellStyle = dataGridViewCellStyle180;
		this.vInfoOverallCpuReadiness.HeaderText = "Overall Cpu Readiness";
		this.vInfoOverallCpuReadiness.MinimumWidth = 6;
		this.vInfoOverallCpuReadiness.Name = "vInfoOverallCpuReadiness";
		this.vInfoOverallCpuReadiness.ReadOnly = true;
		this.vInfoOverallCpuReadiness.ToolTipText = "Percentage of time that the virtual machine was ready, but could not get scheduled to run on the physical CPU";
		this.vInfoOverallCpuReadiness.Width = 400;
		dataGridViewCellStyle181.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle181.Format = "N0";
		dataGridViewCellStyle181.NullValue = null;
		this.vInfoMemory.DefaultCellStyle = dataGridViewCellStyle181;
		this.vInfoMemory.HeaderText = "Memory";
		this.vInfoMemory.MinimumWidth = 6;
		this.vInfoMemory.Name = "vInfoMemory";
		this.vInfoMemory.ReadOnly = true;
		this.vInfoMemory.ToolTipText = "Memory Size in MiB's";
		this.vInfoMemory.Width = 125;
		dataGridViewCellStyle182.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle182.Format = "N0";
		dataGridViewCellStyle182.NullValue = null;
		this.vInfoActiveMemory.DefaultCellStyle = dataGridViewCellStyle182;
		this.vInfoActiveMemory.HeaderText = "Active Memory";
		this.vInfoActiveMemory.MinimumWidth = 6;
		this.vInfoActiveMemory.Name = "vInfoActiveMemory";
		this.vInfoActiveMemory.ReadOnly = true;
		this.vInfoActiveMemory.ToolTipText = "The amount of memory that was recently touched by the VM, in MiB.";
		this.vInfoActiveMemory.Width = 300;
		dataGridViewCellStyle183.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle183.Format = "N0";
		dataGridViewCellStyle183.NullValue = null;
		this.vInfoNICs.DefaultCellStyle = dataGridViewCellStyle183;
		this.vInfoNICs.HeaderText = "NICs";
		this.vInfoNICs.MinimumWidth = 6;
		this.vInfoNICs.Name = "vInfoNICs";
		this.vInfoNICs.ReadOnly = true;
		this.vInfoNICs.ToolTipText = "Number of NICs";
		this.vInfoNICs.Width = 125;
		dataGridViewCellStyle184.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle184.Format = "N0";
		dataGridViewCellStyle184.NullValue = null;
		this.vInfoNumVirtualDisks.DefaultCellStyle = dataGridViewCellStyle184;
		this.vInfoNumVirtualDisks.HeaderText = "Disks";
		this.vInfoNumVirtualDisks.MinimumWidth = 6;
		this.vInfoNumVirtualDisks.Name = "vInfoNumVirtualDisks";
		this.vInfoNumVirtualDisks.ReadOnly = true;
		this.vInfoNumVirtualDisks.ToolTipText = "Number of virtual disks attached to the virtual machine. ";
		this.vInfoNumVirtualDisks.Width = 125;
		dataGridViewCellStyle185.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle185.Format = "N0";
		dataGridViewCellStyle185.NullValue = null;
		this.vInfoTotalDiskCapacityMiB.DefaultCellStyle = dataGridViewCellStyle185;
		this.vInfoTotalDiskCapacityMiB.HeaderText = "Total disk capacity MiB";
		this.vInfoTotalDiskCapacityMiB.MinimumWidth = 6;
		this.vInfoTotalDiskCapacityMiB.Name = "vInfoTotalDiskCapacityMiB";
		this.vInfoTotalDiskCapacityMiB.ReadOnly = true;
		this.vInfoTotalDiskCapacityMiB.ToolTipText = "Total disk capacity in MiB";
		this.vInfoTotalDiskCapacityMiB.Width = 300;
		this.vInfoFixedPassthruHotPlugEnabled.HeaderText = "Fixed Passthru HotPlug";
		this.vInfoFixedPassthruHotPlugEnabled.MinimumWidth = 6;
		this.vInfoFixedPassthruHotPlugEnabled.Name = "vInfoFixedPassthruHotPlugEnabled";
		this.vInfoFixedPassthruHotPlugEnabled.ReadOnly = true;
		this.vInfoFixedPassthruHotPlugEnabled.ToolTipText = "Indicates whether support to add and remove fixed passthrough devices when the VM is running is enabled.";
		this.vInfoFixedPassthruHotPlugEnabled.Width = 300;
		dataGridViewCellStyle186.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle186.Format = "N0";
		dataGridViewCellStyle186.NullValue = null;
		this.vInfominRequiredEVCModeKey.DefaultCellStyle = dataGridViewCellStyle186;
		this.vInfominRequiredEVCModeKey.HeaderText = "min Required EVC Mode Key";
		this.vInfominRequiredEVCModeKey.MinimumWidth = 6;
		this.vInfominRequiredEVCModeKey.Name = "vInfominRequiredEVCModeKey";
		this.vInfominRequiredEVCModeKey.ReadOnly = true;
		this.vInfominRequiredEVCModeKey.ToolTipText = "  this identifies the least-featured EVC mode (among those for the appropriate CPU vendor) that could admit the virtual machine\t";
		this.vInfominRequiredEVCModeKey.Width = 300;
		this.vInfoLatencySensitivity.HeaderText = "Latency Sensitivity";
		this.vInfoLatencySensitivity.MinimumWidth = 6;
		this.vInfoLatencySensitivity.Name = "vInfoLatencySensitivity";
		this.vInfoLatencySensitivity.ReadOnly = true;
		this.vInfoLatencySensitivity.ToolTipText = "The latency-sensitivity setting of the virtual machine";
		this.vInfoLatencySensitivity.Width = 150;
		dataGridViewCellStyle187.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle187.Format = "N0";
		dataGridViewCellStyle187.NullValue = null;
		this.vInfoOpNotificationTimeout.DefaultCellStyle = dataGridViewCellStyle187;
		this.vInfoOpNotificationTimeout.HeaderText = "Op Notification Timeout";
		this.vInfoOpNotificationTimeout.MinimumWidth = 6;
		this.vInfoOpNotificationTimeout.Name = "vInfoOpNotificationTimeout";
		this.vInfoOpNotificationTimeout.ReadOnly = true;
		this.vInfoOpNotificationTimeout.ToolTipText = "Operation notification timeout in seconds. Specifies the maximum time duration the application may take to prepare for the operation after it has been notified";
		this.vInfoOpNotificationTimeout.Width = 400;
		this.vInfoEnableUUID.HeaderText = "EnableUUID";
		this.vInfoEnableUUID.MinimumWidth = 6;
		this.vInfoEnableUUID.Name = "vInfoEnableUUID";
		this.vInfoEnableUUID.ReadOnly = true;
		this.vInfoEnableUUID.ToolTipText = "disk UUID enabled?";
		this.vInfoEnableUUID.Width = 125;
		this.vINfoCBT.HeaderText = "CBT";
		this.vINfoCBT.MinimumWidth = 6;
		this.vINfoCBT.Name = "vINfoCBT";
		this.vINfoCBT.ReadOnly = true;
		this.vINfoCBT.ToolTipText = "Change Block Tracking enabled?";
		this.vINfoCBT.Width = 125;
		this.vInfoPrimaryIPAddress.HeaderText = "Primary IP Address";
		this.vInfoPrimaryIPAddress.MinimumWidth = 6;
		this.vInfoPrimaryIPAddress.Name = "vInfoPrimaryIPAddress";
		this.vInfoPrimaryIPAddress.ReadOnly = true;
		this.vInfoPrimaryIPAddress.ToolTipText = "Primary IP address assigned to the guest operating system, if known";
		this.vInfoPrimaryIPAddress.Width = 250;
		this.vInfoNetwork1.HeaderText = "Network #1";
		this.vInfoNetwork1.MinimumWidth = 6;
		this.vInfoNetwork1.Name = "vInfoNetwork1";
		this.vInfoNetwork1.ReadOnly = true;
		this.vInfoNetwork1.ToolTipText = "Network 1 label";
		this.vInfoNetwork1.Width = 125;
		this.vInfoNetwork2.HeaderText = "Network #2";
		this.vInfoNetwork2.MinimumWidth = 6;
		this.vInfoNetwork2.Name = "vInfoNetwork2";
		this.vInfoNetwork2.ReadOnly = true;
		this.vInfoNetwork2.ToolTipText = "Network 2 label";
		this.vInfoNetwork2.Width = 125;
		this.vInfoNetwork3.HeaderText = "Network #3";
		this.vInfoNetwork3.MinimumWidth = 6;
		this.vInfoNetwork3.Name = "vInfoNetwork3";
		this.vInfoNetwork3.ReadOnly = true;
		this.vInfoNetwork3.ToolTipText = "Network 3 label";
		this.vInfoNetwork3.Width = 125;
		this.vInfoNetwork4.HeaderText = "Network #4";
		this.vInfoNetwork4.MinimumWidth = 6;
		this.vInfoNetwork4.Name = "vInfoNetwork4";
		this.vInfoNetwork4.ReadOnly = true;
		this.vInfoNetwork4.ToolTipText = "Network 4 label";
		this.vInfoNetwork4.Width = 125;
		this.vInfoNetwork5.HeaderText = "Network #5";
		this.vInfoNetwork5.MinimumWidth = 6;
		this.vInfoNetwork5.Name = "vInfoNetwork5";
		this.vInfoNetwork5.ReadOnly = true;
		this.vInfoNetwork5.ToolTipText = "Network 5 label";
		this.vInfoNetwork5.Width = 125;
		this.vInfoNetwork6.HeaderText = "Network #6";
		this.vInfoNetwork6.MinimumWidth = 6;
		this.vInfoNetwork6.Name = "vInfoNetwork6";
		this.vInfoNetwork6.ReadOnly = true;
		this.vInfoNetwork6.ToolTipText = "Network 6 label";
		this.vInfoNetwork6.Width = 125;
		this.vInfoNetwork7.HeaderText = "Network #7";
		this.vInfoNetwork7.MinimumWidth = 6;
		this.vInfoNetwork7.Name = "vInfoNetwork7";
		this.vInfoNetwork7.ReadOnly = true;
		this.vInfoNetwork7.ToolTipText = "Network 7 label";
		this.vInfoNetwork7.Width = 125;
		this.vInfoNetwork8.HeaderText = "Network #8";
		this.vInfoNetwork8.MinimumWidth = 6;
		this.vInfoNetwork8.Name = "vInfoNetwork8";
		this.vInfoNetwork8.ReadOnly = true;
		this.vInfoNetwork8.ToolTipText = "Network 8 label";
		this.vInfoNetwork8.Width = 125;
		dataGridViewCellStyle188.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle188.Format = "N0";
		dataGridViewCellStyle188.NullValue = null;
		this.vInfoNumDisplays.DefaultCellStyle = dataGridViewCellStyle188;
		this.vInfoNumDisplays.HeaderText = "Num Monitors";
		this.vInfoNumDisplays.MinimumWidth = 6;
		this.vInfoNumDisplays.Name = "vInfoNumDisplays";
		this.vInfoNumDisplays.ReadOnly = true;
		this.vInfoNumDisplays.ToolTipText = "Number of supported monitors";
		this.vInfoNumDisplays.Width = 150;
		dataGridViewCellStyle189.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle189.Format = "N0";
		dataGridViewCellStyle189.NullValue = null;
		this.vInfoVideoRamKiB.DefaultCellStyle = dataGridViewCellStyle189;
		this.vInfoVideoRamKiB.HeaderText = "Video Ram KiB";
		this.vInfoVideoRamKiB.MinimumWidth = 6;
		this.vInfoVideoRamKiB.Name = "vInfoVideoRamKiB";
		this.vInfoVideoRamKiB.ReadOnly = true;
		this.vInfoVideoRamKiB.ToolTipText = "The size of the framebuffer for a virtual machine";
		this.vInfoVideoRamKiB.Width = 150;
		this.vInfoResourcepool.HeaderText = "Resource pool";
		this.vInfoResourcepool.MinimumWidth = 6;
		this.vInfoResourcepool.Name = "vInfoResourcepool";
		this.vInfoResourcepool.ReadOnly = true;
		this.vInfoResourcepool.ToolTipText = "The current resource pool that specifies resource allocation for this virtual machine";
		this.vInfoResourcepool.Width = 150;
		this.vInfoFolderId.HeaderText = "Folder ID";
		this.vInfoFolderId.MinimumWidth = 6;
		this.vInfoFolderId.Name = "vInfoFolderId";
		this.vInfoFolderId.ReadOnly = true;
		this.vInfoFolderId.ToolTipText = "Folder ID";
		this.vInfoFolderId.Width = 125;
		this.vInfoFolder.HeaderText = "Folder";
		this.vInfoFolder.MinimumWidth = 6;
		this.vInfoFolder.Name = "vInfoFolder";
		this.vInfoFolder.ReadOnly = true;
		this.vInfoFolder.ToolTipText = "VM folder name";
		this.vInfoFolder.Width = 125;
		this.vInfovApp.HeaderText = "vApp";
		this.vInfovApp.MinimumWidth = 6;
		this.vInfovApp.Name = "vInfovApp";
		this.vInfovApp.ReadOnly = true;
		this.vInfovApp.ToolTipText = "vApp name";
		this.vInfovApp.Width = 125;
		this.vInfoDASProtection.HeaderText = "DAS protection";
		this.vInfoDASProtection.MinimumWidth = 6;
		this.vInfoDASProtection.Name = "vInfoDASProtection";
		this.vInfoDASProtection.ReadOnly = true;
		this.vInfoDASProtection.ToolTipText = "The vSphere HA protection state for a virtual machine.";
		this.vInfoDASProtection.Width = 150;
		this.vInfoFaultToleranceState.HeaderText = "FT State";
		this.vInfoFaultToleranceState.MinimumWidth = 6;
		this.vInfoFaultToleranceState.Name = "vInfoFaultToleranceState";
		this.vInfoFaultToleranceState.ReadOnly = true;
		this.vInfoFaultToleranceState.ToolTipText = "The fault tolerance state of the virtual machine. ";
		this.vInfoFaultToleranceState.Width = 125;
		dataGridViewCellStyle190.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle190.Format = "N0";
		dataGridViewCellStyle190.NullValue = null;
		this.vInfoFTInfoRole.DefaultCellStyle = dataGridViewCellStyle190;
		this.vInfoFTInfoRole.HeaderText = "FT Role";
		this.vInfoFTInfoRole.MinimumWidth = 6;
		this.vInfoFTInfoRole.Name = "vInfoFTInfoRole";
		this.vInfoFTInfoRole.ReadOnly = true;
		this.vInfoFTInfoRole.ToolTipText = "The index of the current VM in instanceUuids array starting from 1, so 1 means that it is the primary VM";
		this.vInfoFTInfoRole.Width = 125;
		this.vInfoFTLatencyStatus.HeaderText = "FT Latency";
		this.vInfoFTLatencyStatus.MinimumWidth = 6;
		this.vInfoFTLatencyStatus.Name = "vInfoFTLatencyStatus";
		this.vInfoFTLatencyStatus.ReadOnly = true;
		this.vInfoFTLatencyStatus.ToolTipText = "The latency status of the fault tolerance";
		this.vInfoFTLatencyStatus.Width = 125;
		dataGridViewCellStyle191.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle191.Format = "N0";
		dataGridViewCellStyle191.NullValue = null;
		this.vInfoFTLogBandwidth.DefaultCellStyle = dataGridViewCellStyle191;
		this.vInfoFTLogBandwidth.HeaderText = "FT Bandwidth";
		this.vInfoFTLogBandwidth.MinimumWidth = 6;
		this.vInfoFTLogBandwidth.Name = "vInfoFTLogBandwidth";
		this.vInfoFTLogBandwidth.ReadOnly = true;
		this.vInfoFTLogBandwidth.ToolTipText = "The network bandwidth used for logging between the primary and secondary fault tolerance VMs";
		this.vInfoFTLogBandwidth.Width = 150;
		dataGridViewCellStyle192.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle192.Format = "N0";
		dataGridViewCellStyle192.NullValue = null;
		this.vInfoFTSecondaryLatency.DefaultCellStyle = dataGridViewCellStyle192;
		this.vInfoFTSecondaryLatency.HeaderText = "FT Sec. Latency";
		this.vInfoFTSecondaryLatency.MinimumWidth = 6;
		this.vInfoFTSecondaryLatency.Name = "vInfoFTSecondaryLatency";
		this.vInfoFTSecondaryLatency.ReadOnly = true;
		this.vInfoFTSecondaryLatency.ToolTipText = "The amount of time in wallclock that the VCPU of the secondary fault tolerance VM is behind the VCPU of the primary VM. The unit is millisecond.";
		this.vInfoFTSecondaryLatency.Width = 150;
		this.vInfoVmFailoverInProgress.HeaderText = "Vm Failover In Progress";
		this.vInfoVmFailoverInProgress.MinimumWidth = 6;
		this.vInfoVmFailoverInProgress.Name = "vInfoVmFailoverInProgress";
		this.vInfoVmFailoverInProgress.ReadOnly = true;
		this.vInfoVmFailoverInProgress.ToolTipText = "Represents if the vm is currently being failed over by FDM";
		this.vInfoVmFailoverInProgress.Width = 400;
		dataGridViewCellStyle193.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle193.Format = "N0";
		dataGridViewCellStyle193.NullValue = null;
		this.vInfoProvisioned.DefaultCellStyle = dataGridViewCellStyle193;
		this.vInfoProvisioned.HeaderText = "Provisioned MiB";
		this.vInfoProvisioned.MinimumWidth = 6;
		this.vInfoProvisioned.Name = "vInfoProvisioned";
		this.vInfoProvisioned.ReadOnly = true;
		this.vInfoProvisioned.ToolTipText = "Total provisioned storage space, in Mebibytes, committed to this virtual machine across all datastores";
		this.vInfoProvisioned.Width = 150;
		dataGridViewCellStyle194.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle194.Format = "N0";
		dataGridViewCellStyle194.NullValue = null;
		this.vInfoInUse.DefaultCellStyle = dataGridViewCellStyle194;
		this.vInfoInUse.HeaderText = "In Use MiB";
		this.vInfoInUse.MinimumWidth = 6;
		this.vInfoInUse.Name = "vInfoInUse";
		this.vInfoInUse.ReadOnly = true;
		this.vInfoInUse.ToolTipText = "Storage in use, space in Mebibytes, used by this virtual machine on all datastores";
		this.vInfoInUse.Width = 150;
		dataGridViewCellStyle195.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle195.Format = "N0";
		dataGridViewCellStyle195.NullValue = null;
		this.vInfoUnshared.DefaultCellStyle = dataGridViewCellStyle195;
		this.vInfoUnshared.HeaderText = "Unshared MiB";
		this.vInfoUnshared.MinimumWidth = 6;
		this.vInfoUnshared.Name = "vInfoUnshared";
		this.vInfoUnshared.ReadOnly = true;
		this.vInfoUnshared.ToolTipText = "Total storage space, in Mebibytes, occupied by the virtual machine across all datastores, that is not shared with any other virtual machine";
		this.vInfoUnshared.Width = 150;
		this.vInfoHARestartPriority.HeaderText = "HA Restart Priority";
		this.vInfoHARestartPriority.MinimumWidth = 6;
		this.vInfoHARestartPriority.Name = "vInfoHARestartPriority";
		this.vInfoHARestartPriority.ReadOnly = true;
		this.vInfoHARestartPriority.ToolTipText = "Restart priority for a virtual machine. ";
		this.vInfoHARestartPriority.Width = 250;
		this.vInfoHAIsolationResponse.HeaderText = "HA Isolation Response";
		this.vInfoHAIsolationResponse.MinimumWidth = 6;
		this.vInfoHAIsolationResponse.Name = "vInfoHAIsolationResponse";
		this.vInfoHAIsolationResponse.ReadOnly = true;
		this.vInfoHAIsolationResponse.ToolTipText = "Indicates whether or not the virtual machine should be powered off if a host determines that it is isolated from the rest of the compute resource. ";
		this.vInfoHAIsolationResponse.Width = 250;
		this.vInfoVMmonitoring.HeaderText = "HA VM Monitoring";
		this.vInfoVMmonitoring.MinimumWidth = 6;
		this.vInfoVMmonitoring.Name = "vInfoVMmonitoring";
		this.vInfoVMmonitoring.ReadOnly = true;
		this.vInfoVMmonitoring.ToolTipText = "HA VM Monitoring status";
		this.vInfoVMmonitoring.Width = 250;
		this.vInfoClusterRule.HeaderText = "Cluster rule(s)";
		this.vInfoClusterRule.MinimumWidth = 6;
		this.vInfoClusterRule.Name = "vInfoClusterRule";
		this.vInfoClusterRule.ReadOnly = true;
		this.vInfoClusterRule.Width = 150;
		this.vInfoClusterRuleName.HeaderText = "Cluster rule name(s)";
		this.vInfoClusterRuleName.MinimumWidth = 6;
		this.vInfoClusterRuleName.Name = "vInfoClusterRuleName";
		this.vInfoClusterRuleName.ReadOnly = true;
		this.vInfoClusterRuleName.Width = 250;
		this.vInfoInstallBootRequired.HeaderText = "Boot Required";
		this.vInfoInstallBootRequired.MinimumWidth = 6;
		this.vInfoInstallBootRequired.Name = "vInfoInstallBootRequired";
		this.vInfoInstallBootRequired.ReadOnly = true;
		this.vInfoInstallBootRequired.ToolTipText = "Whether the VM requires a reboot to finish installation. False if no vApp meta-data is configured. ";
		this.vInfoInstallBootRequired.Width = 150;
		dataGridViewCellStyle196.Alignment = System.Windows.Forms.DataGridViewContentAlignment.TopRight;
		dataGridViewCellStyle196.Format = "N0";
		dataGridViewCellStyle196.NullValue = null;
		this.vInfoBootDelay.DefaultCellStyle = dataGridViewCellStyle196;
		this.vInfoBootDelay.HeaderText = "Boot delay";
		this.vInfoBootDelay.MinimumWidth = 6;
		this.vInfoBootDelay.Name = "vInfoBootDelay";
		this.vInfoBootDelay.ReadOnly = true;
		this.vInfoBootDelay.ToolTipText = "Delay in milliseconds before starting the boot sequence";
		this.vInfoBootDelay.Width = 125;
		dataGridViewCellStyle197.Alignment = System.Windows.Forms.DataGridViewContentAlignment.TopRight;
		dataGridViewCellStyle197.Format = "N0";
		dataGridViewCellStyle197.NullValue = null;
		this.vInfoBootRetryDelay.DefaultCellStyle = dataGridViewCellStyle197;
		this.vInfoBootRetryDelay.HeaderText = "Boot retry delay";
		this.vInfoBootRetryDelay.MinimumWidth = 6;
		this.vInfoBootRetryDelay.Name = "vInfoBootRetryDelay";
		this.vInfoBootRetryDelay.ReadOnly = true;
		this.vInfoBootRetryDelay.ToolTipText = "Delay in milliseconds before a boot retry";
		this.vInfoBootRetryDelay.Width = 200;
		this.vInfoBootRetryEnabled.HeaderText = "Boot retry enabled";
		this.vInfoBootRetryEnabled.MinimumWidth = 6;
		this.vInfoBootRetryEnabled.Name = "vInfoBootRetryEnabled";
		this.vInfoBootRetryEnabled.ReadOnly = true;
		this.vInfoBootRetryEnabled.ToolTipText = "If set to true, a virtual machine that fails to boot will try again after the bootRetryDelay time period has expired";
		this.vInfoBootRetryEnabled.Width = 200;
		this.vInfoBootBios.HeaderText = "Boot BIOS setup";
		this.vInfoBootBios.MinimumWidth = 6;
		this.vInfoBootBios.Name = "vInfoBootBios";
		this.vInfoBootBios.ReadOnly = true;
		this.vInfoBootBios.ToolTipText = "If set to true, the virtual machine automatically enters BIOS setup the next time it boots";
		this.vInfoBootBios.Width = 200;
		this.vInfoRebootPowerOff.HeaderText = "Reboot PowerOff";
		this.vInfoRebootPowerOff.MinimumWidth = 6;
		this.vInfoRebootPowerOff.Name = "vInfoRebootPowerOff";
		this.vInfoRebootPowerOff.ReadOnly = true;
		this.vInfoRebootPowerOff.ToolTipText = "Whether the next reboot will result in a power off";
		this.vInfoRebootPowerOff.Width = 200;
		this.vInfoEfiSecureBoot.HeaderText = "EFI Secure boot";
		this.vInfoEfiSecureBoot.MinimumWidth = 6;
		this.vInfoEfiSecureBoot.Name = "vInfoEfiSecureBoot";
		this.vInfoEfiSecureBoot.ReadOnly = true;
		this.vInfoEfiSecureBoot.ToolTipText = "If set to true, the virtual machine's firmware will perform signature checks of any EFI images loaded during startup";
		this.vInfoEfiSecureBoot.Width = 200;
		this.vInfoFirmware.HeaderText = "Firmware";
		this.vInfoFirmware.MinimumWidth = 6;
		this.vInfoFirmware.Name = "vInfoFirmware";
		this.vInfoFirmware.ReadOnly = true;
		this.vInfoFirmware.ToolTipText = "Information about firmware type for this Virtual Machine";
		this.vInfoFirmware.Width = 125;
		dataGridViewCellStyle198.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle198.Format = "N0";
		dataGridViewCellStyle198.NullValue = null;
		this.VInfoVersion.DefaultCellStyle = dataGridViewCellStyle198;
		this.VInfoVersion.HeaderText = "HW version";
		this.VInfoVersion.MinimumWidth = 6;
		this.VInfoVersion.Name = "VInfoVersion";
		this.VInfoVersion.ReadOnly = true;
		this.VInfoVersion.ToolTipText = "Virtual machine hardware version.";
		this.VInfoVersion.Width = 150;
		this.vInfoHWUpgradeStatus.HeaderText = "HW upgrade status";
		this.vInfoHWUpgradeStatus.MinimumWidth = 6;
		this.vInfoHWUpgradeStatus.Name = "vInfoHWUpgradeStatus";
		this.vInfoHWUpgradeStatus.ReadOnly = true;
		this.vInfoHWUpgradeStatus.ToolTipText = "Status for last attempt to run scheduled hardware upgrade.";
		this.vInfoHWUpgradeStatus.Width = 250;
		this.vInfoHWUpgradePolicy.HeaderText = "HW upgrade policy";
		this.vInfoHWUpgradePolicy.MinimumWidth = 6;
		this.vInfoHWUpgradePolicy.Name = "vInfoHWUpgradePolicy";
		this.vInfoHWUpgradePolicy.ReadOnly = true;
		this.vInfoHWUpgradePolicy.ToolTipText = "Scheduled hardware upgrade policy setting for the virtual machine.";
		this.vInfoHWUpgradePolicy.Width = 250;
		this.vInfoHWTarget.HeaderText = "HW target";
		this.vInfoHWTarget.MinimumWidth = 6;
		this.vInfoHWTarget.Name = "vInfoHWTarget";
		this.vInfoHWTarget.ReadOnly = true;
		this.vInfoHWTarget.ToolTipText = "Key for target hardware version to be used on next scheduled upgrade";
		this.vInfoHWTarget.Width = 150;
		this.vInfopath.HeaderText = "Path";
		this.vInfopath.MinimumWidth = 6;
		this.vInfopath.Name = "vInfopath";
		this.vInfopath.ReadOnly = true;
		this.vInfopath.ToolTipText = "Path name to the configuration file for the virtual machine ";
		this.vInfopath.Width = 125;
		this.vInfoLogDirectory.HeaderText = "Log directory";
		this.vInfoLogDirectory.MinimumWidth = 6;
		this.vInfoLogDirectory.Name = "vInfoLogDirectory";
		this.vInfoLogDirectory.ReadOnly = true;
		this.vInfoLogDirectory.ToolTipText = "Directory to store the log files for the virtual machine. If not specified, this defaults to the same directory as the configuration file, ";
		this.vInfoLogDirectory.Width = 150;
		this.vInfoSnapshotDirectory.HeaderText = "Snapshot directory";
		this.vInfoSnapshotDirectory.MinimumWidth = 6;
		this.vInfoSnapshotDirectory.Name = "vInfoSnapshotDirectory";
		this.vInfoSnapshotDirectory.ReadOnly = true;
		this.vInfoSnapshotDirectory.ToolTipText = "Path name of the directory that holds suspend and snapshot files belonging to the virtual machine\t";
		this.vInfoSnapshotDirectory.Width = 200;
		this.vInfoSuspendDirectory.HeaderText = "Suspend directory";
		this.vInfoSuspendDirectory.MinimumWidth = 6;
		this.vInfoSuspendDirectory.Name = "vInfoSuspendDirectory";
		this.vInfoSuspendDirectory.ReadOnly = true;
		this.vInfoSuspendDirectory.ToolTipText = "Some products allow the suspend directory to be different than the snapshot directory. On products where this is not possible, setting of this property is ignored";
		this.vInfoSuspendDirectory.Width = 200;
		this.vInfoNotes.HeaderText = "Annotation";
		this.vInfoNotes.MinimumWidth = 6;
		this.vInfoNotes.Name = "vInfoNotes";
		this.vInfoNotes.ReadOnly = true;
		this.vInfoNotes.ToolTipText = "Description for the virtual machine";
		this.vInfoNotes.Width = 200;
		this.vInfoDataCenter.HeaderText = "Datacenter";
		this.vInfoDataCenter.MinimumWidth = 6;
		this.vInfoDataCenter.Name = "vInfoDataCenter";
		this.vInfoDataCenter.ReadOnly = true;
		this.vInfoDataCenter.ToolTipText = "Datacenter name";
		this.vInfoDataCenter.Width = 125;
		this.vInfoCluster.HeaderText = "Cluster";
		this.vInfoCluster.MinimumWidth = 6;
		this.vInfoCluster.Name = "vInfoCluster";
		this.vInfoCluster.ReadOnly = true;
		this.vInfoCluster.ToolTipText = "Cluster name";
		this.vInfoCluster.Width = 125;
		this.vInfoHost.HeaderText = "Host";
		this.vInfoHost.MinimumWidth = 6;
		this.vInfoHost.Name = "vInfoHost";
		this.vInfoHost.ReadOnly = true;
		this.vInfoHost.ToolTipText = "The host on which the vm is running";
		this.vInfoHost.Width = 125;
		this.vInfoOS.HeaderText = "OS according to the configuration file";
		this.vInfoOS.MinimumWidth = 6;
		this.vInfoOS.Name = "vInfoOS";
		this.vInfoOS.ReadOnly = true;
		this.vInfoOS.ToolTipText = "Guest operating system according to the Configuration file.";
		this.vInfoOS.Width = 400;
		this.vInfoOSTools.HeaderText = "OS according to the VMware Tools";
		this.vInfoOSTools.MinimumWidth = 6;
		this.vInfoOSTools.Name = "vInfoOSTools";
		this.vInfoOSTools.ReadOnly = true;
		this.vInfoOSTools.ToolTipText = "Guest operating system according to VMware tools";
		this.vInfoOSTools.Width = 400;
		this.vInfoCustomizationInfo.HeaderText = "Customization Info";
		this.vInfoCustomizationInfo.MinimumWidth = 6;
		this.vInfoCustomizationInfo.Name = "vInfoCustomizationInfo";
		this.vInfoCustomizationInfo.ReadOnly = true;
		this.vInfoCustomizationInfo.ToolTipText = "Guest OS Customization status info";
		this.vInfoCustomizationInfo.Width = 300;
		this.vInfoGuestDetailedData.HeaderText = "Guest Detailed Data";
		this.vInfoGuestDetailedData.MinimumWidth = 6;
		this.vInfoGuestDetailedData.Name = "vInfoGuestDetailedData";
		this.vInfoGuestDetailedData.ReadOnly = true;
		this.vInfoGuestDetailedData.ToolTipText = "Guest OS Detailed data";
		this.vInfoGuestDetailedData.Width = 400;
		this.vInfoObjectID.HeaderText = "VM ID";
		this.vInfoObjectID.MinimumWidth = 6;
		this.vInfoObjectID.Name = "vInfoObjectID";
		this.vInfoObjectID.ReadOnly = true;
		this.vInfoObjectID.ToolTipText = "VM Object ID";
		this.vInfoObjectID.Width = 125;
		this.vInfoSMBIOSUUID.HeaderText = "SMBIOS UUID";
		this.vInfoSMBIOSUUID.MinimumWidth = 6;
		this.vInfoSMBIOSUUID.Name = "vInfoSMBIOSUUID";
		this.vInfoSMBIOSUUID.ReadOnly = true;
		this.vInfoSMBIOSUUID.ToolTipText = "128-bit SMBIOS UUID of a virtual machine represented as a hexadecimal string";
		this.vInfoSMBIOSUUID.Width = 200;
		this.vInfoUUID.HeaderText = "VM UUID";
		this.vInfoUUID.MinimumWidth = 6;
		this.vInfoUUID.Name = "vInfoUUID";
		this.vInfoUUID.ReadOnly = true;
		this.vInfoUUID.ToolTipText = "VirtualCenter-specific 128-bit UUID of a virtual machine";
		this.vInfoUUID.Width = 150;
		this.vInfoVISDKServerType.HeaderText = "VI SDK Server type";
		this.vInfoVISDKServerType.MinimumWidth = 6;
		this.vInfoVISDKServerType.Name = "vInfoVISDKServerType";
		this.vInfoVISDKServerType.ReadOnly = true;
		this.vInfoVISDKServerType.ToolTipText = "VI SDK Server type";
		this.vInfoVISDKServerType.Width = 200;
		this.vInfoVISDKAPI.HeaderText = "VI SDK API Version";
		this.vInfoVISDKAPI.MinimumWidth = 6;
		this.vInfoVISDKAPI.Name = "vInfoVISDKAPI";
		this.vInfoVISDKAPI.ReadOnly = true;
		this.vInfoVISDKAPI.ToolTipText = "VI SDK API Version";
		this.vInfoVISDKAPI.Width = 200;
		this.vInfoVISDKServer.HeaderText = "VI SDK Server";
		this.vInfoVISDKServer.MinimumWidth = 6;
		this.vInfoVISDKServer.Name = "vInfoVISDKServer";
		this.vInfoVISDKServer.ReadOnly = true;
		this.vInfoVISDKServer.ToolTipText = "VI SDK Server, where RVTools gets it's information from";
		this.vInfoVISDKServer.Width = 200;
		this.vInfoInstanceUUID.HeaderText = "VI SDK UUID";
		this.vInfoInstanceUUID.MinimumWidth = 6;
		this.vInfoInstanceUUID.Name = "vInfoInstanceUUID";
		this.vInfoInstanceUUID.ReadOnly = true;
		this.vInfoInstanceUUID.ToolTipText = "A globally unique identifier associated with this service instance";
		this.vInfoInstanceUUID.Width = 200;
		this.TabPages.Controls.Add(this.tabvInfo);
		this.TabPages.Controls.Add(this.tabvCPU);
		this.TabPages.Controls.Add(this.tabvMemory);
		this.TabPages.Controls.Add(this.tabvDisk);
		this.TabPages.Controls.Add(this.tabvPartition);
		this.TabPages.Controls.Add(this.tabvNetwork);
		this.TabPages.Controls.Add(this.tabvCD);
		this.TabPages.Controls.Add(this.tabvUSB);
		this.TabPages.Controls.Add(this.tabvSnapshot);
		this.TabPages.Controls.Add(this.tabvTools);
		this.TabPages.Controls.Add(this.tabvSource);
		this.TabPages.Controls.Add(this.tabvRP);
		this.TabPages.Controls.Add(this.tabvCluster);
		this.TabPages.Controls.Add(this.tabvHost);
		this.TabPages.Controls.Add(this.tabvHBA);
		this.TabPages.Controls.Add(this.tabvNIC);
		this.TabPages.Controls.Add(this.tabvSwitch);
		this.TabPages.Controls.Add(this.tabvPort);
		this.TabPages.Controls.Add(this.tabdvSwitch);
		this.TabPages.Controls.Add(this.tabdvPort);
		this.TabPages.Controls.Add(this.tabvSC_VMK);
		this.TabPages.Controls.Add(this.tabvDatastore);
		this.TabPages.Controls.Add(this.tabvMultiPath);
		this.TabPages.Controls.Add(this.tabvLicense);
		this.TabPages.Controls.Add(this.tabvFileInfo);
		this.TabPages.Controls.Add(this.tabvHealth);
		this.TabPages.Dock = System.Windows.Forms.DockStyle.Fill;
		this.TabPages.Location = new System.Drawing.Point(13, 42);
		this.TabPages.Margin = new System.Windows.Forms.Padding(4);
		this.TabPages.Name = "TabPages";
		this.TabPages.SelectedIndex = 0;
		this.TabPages.ShowToolTips = true;
		this.TabPages.Size = new System.Drawing.Size(1733, 420);
		this.TabPages.TabIndex = 1;
		this.TabPages.Selected += new System.Windows.Forms.TabControlEventHandler(this.tabControl1_Selected);
		this.tabvSource.Controls.Add(this.panel29);
		this.tabvSource.Location = new System.Drawing.Point(4, 25);
		this.tabvSource.Margin = new System.Windows.Forms.Padding(4);
		this.tabvSource.Name = "tabvSource";
		this.tabvSource.Size = new System.Drawing.Size(1725, 391);
		this.tabvSource.TabIndex = 35;
		this.tabvSource.Text = "vSource";
		this.tabvSource.ToolTipText = "Source information";
		this.tabvSource.UseVisualStyleBackColor = true;
		this.panel29.Controls.Add(this.dataGridvSource);
		this.panel29.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel29.Location = new System.Drawing.Point(0, 0);
		this.panel29.Margin = new System.Windows.Forms.Padding(0);
		this.panel29.Name = "panel29";
		this.panel29.Padding = new System.Windows.Forms.Padding(20, 18, 20, 18);
		this.panel29.Size = new System.Drawing.Size(1725, 391);
		this.panel29.TabIndex = 4;
		this.dataGridvSource.AllowUserToAddRows = false;
		this.dataGridvSource.AllowUserToDeleteRows = false;
		this.dataGridvSource.AllowUserToOrderColumns = true;
		this.dataGridvSource.AllowUserToResizeRows = false;
		dataGridViewCellStyle199.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
		this.dataGridvSource.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle199;
		this.dataGridvSource.BackgroundColor = System.Drawing.SystemColors.Window;
		this.dataGridvSource.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
		this.dataGridvSource.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGridvSource.Columns.AddRange(this.vSourceName, this.vSourceOSType, this.vSourceApiType, this.vSourceAPIVersion, this.vSourceVersion, this.vSourcePatchLevel, this.vSourceBuild, this.vSourceFullname, this.vSourceProductName, this.vSourceProductVersion, this.vSourceProductLine, this.vSourceVendor, this.vSourceVISDKServer, this.vSourceVISDKUUID);
		this.dataGridvSource.Dock = System.Windows.Forms.DockStyle.Fill;
		this.dataGridvSource.Location = new System.Drawing.Point(20, 18);
		this.dataGridvSource.Margin = new System.Windows.Forms.Padding(4);
		this.dataGridvSource.Name = "dataGridvSource";
		this.dataGridvSource.RowHeadersVisible = false;
		this.dataGridvSource.RowHeadersWidth = 51;
		this.dataGridvSource.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this.dataGridvSource.Size = new System.Drawing.Size(1685, 355);
		this.dataGridvSource.TabIndex = 0;
		this.vSourceName.Frozen = true;
		this.vSourceName.HeaderText = "Name";
		this.vSourceName.MinimumWidth = 6;
		this.vSourceName.Name = "vSourceName";
		this.vSourceName.ReadOnly = true;
		this.vSourceName.ToolTipText = "Short form of the product name";
		this.vSourceName.Width = 125;
		this.vSourceOSType.HeaderText = "OS type";
		this.vSourceOSType.MinimumWidth = 6;
		this.vSourceOSType.Name = "vSourceOSType";
		this.vSourceOSType.ReadOnly = true;
		this.vSourceOSType.ToolTipText = "Operating system type and architecture";
		this.vSourceOSType.Width = 125;
		this.vSourceApiType.HeaderText = "API type";
		this.vSourceApiType.MinimumWidth = 6;
		this.vSourceApiType.Name = "vSourceApiType";
		this.vSourceApiType.ReadOnly = true;
		this.vSourceApiType.ToolTipText = "API type";
		this.vSourceApiType.Width = 125;
		this.vSourceAPIVersion.HeaderText = "API version";
		this.vSourceAPIVersion.MinimumWidth = 6;
		this.vSourceAPIVersion.Name = "vSourceAPIVersion";
		this.vSourceAPIVersion.ReadOnly = true;
		this.vSourceAPIVersion.ToolTipText = "The version of the API as a dot-separated string";
		this.vSourceAPIVersion.Width = 125;
		this.vSourceVersion.HeaderText = "Version";
		this.vSourceVersion.MinimumWidth = 6;
		this.vSourceVersion.Name = "vSourceVersion";
		this.vSourceVersion.ReadOnly = true;
		this.vSourceVersion.ToolTipText = "Dot-separated version string. For example, \"1.2\".";
		this.vSourceVersion.Width = 125;
		this.vSourcePatchLevel.HeaderText = "Patch level";
		this.vSourcePatchLevel.MinimumWidth = 6;
		this.vSourcePatchLevel.Name = "vSourcePatchLevel";
		this.vSourcePatchLevel.ReadOnly = true;
		this.vSourcePatchLevel.ToolTipText = "Patch level for the server.";
		this.vSourcePatchLevel.Width = 125;
		this.vSourceBuild.HeaderText = "Build";
		this.vSourceBuild.MinimumWidth = 6;
		this.vSourceBuild.Name = "vSourceBuild";
		this.vSourceBuild.ReadOnly = true;
		this.vSourceBuild.ToolTipText = "Build string for the server on which this call is made";
		this.vSourceBuild.Width = 125;
		this.vSourceFullname.HeaderText = "Fullname";
		this.vSourceFullname.MinimumWidth = 6;
		this.vSourceFullname.Name = "vSourceFullname";
		this.vSourceFullname.ReadOnly = true;
		this.vSourceFullname.ToolTipText = "The complete product name, including the version information";
		this.vSourceFullname.Width = 125;
		this.vSourceProductName.HeaderText = "Product name";
		this.vSourceProductName.MinimumWidth = 6;
		this.vSourceProductName.Name = "vSourceProductName";
		this.vSourceProductName.ReadOnly = true;
		this.vSourceProductName.ToolTipText = "The license product name";
		this.vSourceProductName.Width = 150;
		this.vSourceProductVersion.HeaderText = "Product version";
		this.vSourceProductVersion.MinimumWidth = 6;
		this.vSourceProductVersion.Name = "vSourceProductVersion";
		this.vSourceProductVersion.ReadOnly = true;
		this.vSourceProductVersion.ToolTipText = "The license product version";
		this.vSourceProductVersion.Width = 150;
		this.vSourceProductLine.HeaderText = "Product line";
		this.vSourceProductLine.MinimumWidth = 6;
		this.vSourceProductLine.Name = "vSourceProductLine";
		this.vSourceProductLine.ReadOnly = true;
		this.vSourceProductLine.ToolTipText = "The product ID is a unique identifier for a product line";
		this.vSourceProductLine.Width = 125;
		this.vSourceVendor.HeaderText = "Vendor";
		this.vSourceVendor.MinimumWidth = 6;
		this.vSourceVendor.Name = "vSourceVendor";
		this.vSourceVendor.ReadOnly = true;
		this.vSourceVendor.ToolTipText = "Name of the vendor of this product";
		this.vSourceVendor.Width = 125;
		this.vSourceVISDKServer.HeaderText = "VI SDK Server";
		this.vSourceVISDKServer.MinimumWidth = 6;
		this.vSourceVISDKServer.Name = "vSourceVISDKServer";
		this.vSourceVISDKServer.ReadOnly = true;
		this.vSourceVISDKServer.ToolTipText = "VI SDK Server, where RVTools gets it's information from";
		this.vSourceVISDKServer.Width = 200;
		this.vSourceVISDKUUID.HeaderText = "VI SDK UUID";
		this.vSourceVISDKUUID.MinimumWidth = 6;
		this.vSourceVISDKUUID.Name = "vSourceVISDKUUID";
		this.vSourceVISDKUUID.ReadOnly = true;
		this.vSourceVISDKUUID.ToolTipText = "A globally unique identifier associated with this service instance";
		this.vSourceVISDKUUID.Width = 125;
		this.tabvFileInfo.Controls.Add(this.panel28);
		this.tabvFileInfo.Location = new System.Drawing.Point(4, 25);
		this.tabvFileInfo.Margin = new System.Windows.Forms.Padding(4);
		this.tabvFileInfo.Name = "tabvFileInfo";
		this.tabvFileInfo.Size = new System.Drawing.Size(1725, 391);
		this.tabvFileInfo.TabIndex = 34;
		this.tabvFileInfo.Text = "vFileInfo";
		this.tabvFileInfo.ToolTipText = "VM File Info";
		this.tabvFileInfo.UseVisualStyleBackColor = true;
		this.panel28.Controls.Add(this.dataGridvFileInfo);
		this.panel28.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel28.Location = new System.Drawing.Point(0, 0);
		this.panel28.Margin = new System.Windows.Forms.Padding(4);
		this.panel28.Name = "panel28";
		this.panel28.Padding = new System.Windows.Forms.Padding(20, 18, 20, 18);
		this.panel28.Size = new System.Drawing.Size(1725, 391);
		this.panel28.TabIndex = 6;
		this.dataGridvFileInfo.AllowUserToAddRows = false;
		this.dataGridvFileInfo.AllowUserToDeleteRows = false;
		this.dataGridvFileInfo.AllowUserToOrderColumns = true;
		this.dataGridvFileInfo.AllowUserToResizeRows = false;
		dataGridViewCellStyle200.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);
		this.dataGridvFileInfo.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle200;
		this.dataGridvFileInfo.BackgroundColor = System.Drawing.SystemColors.Window;
		this.dataGridvFileInfo.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
		this.dataGridvFileInfo.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dataGridvFileInfo.Columns.AddRange(this.vFileInfoFriendlyPath, this.vFileInfoFileName, this.vFileInfoFileType, this.vFileInfoFileSize, this.vFileInfoPath, this.vFileInfoSortColumn, this.vFileInfoSDKServer, this.vFileInfoSDKUUID);
		this.dataGridvFileInfo.Dock = System.Windows.Forms.DockStyle.Fill;
		this.dataGridvFileInfo.Location = new System.Drawing.Point(20, 18);
		this.dataGridvFileInfo.Margin = new System.Windows.Forms.Padding(4);
		this.dataGridvFileInfo.Name = "dataGridvFileInfo";
		this.dataGridvFileInfo.RowHeadersVisible = false;
		this.dataGridvFileInfo.RowHeadersWidth = 51;
		this.dataGridvFileInfo.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this.dataGridvFileInfo.Size = new System.Drawing.Size(1685, 355);
		this.dataGridvFileInfo.TabIndex = 2;
		this.vFileInfoFriendlyPath.Frozen = true;
		this.vFileInfoFriendlyPath.HeaderText = "Friendly Path Name";
		this.vFileInfoFriendlyPath.MinimumWidth = 6;
		this.vFileInfoFriendlyPath.Name = "vFileInfoFriendlyPath";
		this.vFileInfoFriendlyPath.ReadOnly = true;
		this.vFileInfoFriendlyPath.ToolTipText = "Path (friendly name)";
		this.vFileInfoFriendlyPath.Width = 250;
		this.vFileInfoFileName.HeaderText = "File Name";
		this.vFileInfoFileName.MinimumWidth = 6;
		this.vFileInfoFileName.Name = "vFileInfoFileName";
		this.vFileInfoFileName.ReadOnly = true;
		this.vFileInfoFileName.ToolTipText = "File Name";
		this.vFileInfoFileName.Width = 200;
		this.vFileInfoFileType.HeaderText = "File Type";
		this.vFileInfoFileType.MinimumWidth = 6;
		this.vFileInfoFileType.Name = "vFileInfoFileType";
		this.vFileInfoFileType.ReadOnly = true;
		this.vFileInfoFileType.ToolTipText = "File Type";
		this.vFileInfoFileType.Width = 122;
		dataGridViewCellStyle201.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle201.Format = "N0";
		dataGridViewCellStyle201.NullValue = null;
		this.vFileInfoFileSize.DefaultCellStyle = dataGridViewCellStyle201;
		this.vFileInfoFileSize.HeaderText = "File Size in bytes";
		this.vFileInfoFileSize.MinimumWidth = 6;
		this.vFileInfoFileSize.Name = "vFileInfoFileSize";
		this.vFileInfoFileSize.ReadOnly = true;
		this.vFileInfoFileSize.Width = 250;
		this.vFileInfoPath.HeaderText = "Path";
		this.vFileInfoPath.MinimumWidth = 6;
		this.vFileInfoPath.Name = "vFileInfoPath";
		this.vFileInfoPath.ReadOnly = true;
		this.vFileInfoPath.Width = 125;
		this.vFileInfoSortColumn.HeaderText = "Internal Sort Column";
		this.vFileInfoSortColumn.MinimumWidth = 6;
		this.vFileInfoSortColumn.Name = "vFileInfoSortColumn";
		this.vFileInfoSortColumn.ReadOnly = true;
		this.vFileInfoSortColumn.ToolTipText = "Internal sort column";
		this.vFileInfoSortColumn.Width = 250;
		this.vFileInfoSDKServer.HeaderText = "VI SDK Server";
		this.vFileInfoSDKServer.MinimumWidth = 6;
		this.vFileInfoSDKServer.Name = "vFileInfoSDKServer";
		this.vFileInfoSDKServer.ReadOnly = true;
		this.vFileInfoSDKServer.ToolTipText = "VI SDK Server, where RVTools gets it's information from";
		this.vFileInfoSDKServer.Width = 150;
		this.vFileInfoSDKUUID.HeaderText = "VI SDK UUID";
		this.vFileInfoSDKUUID.MinimumWidth = 6;
		this.vFileInfoSDKUUID.Name = "vFileInfoSDKUUID";
		this.vFileInfoSDKUUID.ReadOnly = true;
		this.vFileInfoSDKUUID.ToolTipText = "A globally unique identifier associated with this service instance";
		this.vFileInfoSDKUUID.Width = 150;
		this.vHostName.Frozen = true;
		this.vHostName.HeaderText = "Host";
		this.vHostName.MinimumWidth = 6;
		this.vHostName.Name = "vHostName";
		this.vHostName.ReadOnly = true;
		this.vHostName.ToolTipText = "Host name";
		this.vHostName.Width = 125;
		this.vHostDatacenter.HeaderText = "Datacenter";
		this.vHostDatacenter.MinimumWidth = 6;
		this.vHostDatacenter.Name = "vHostDatacenter";
		this.vHostDatacenter.ReadOnly = true;
		this.vHostDatacenter.ToolTipText = "Datacenter name";
		this.vHostDatacenter.Width = 125;
		this.vHostCluster.HeaderText = "Cluster";
		this.vHostCluster.MinimumWidth = 6;
		this.vHostCluster.Name = "vHostCluster";
		this.vHostCluster.ReadOnly = true;
		this.vHostCluster.ToolTipText = "Cluster name";
		this.vHostCluster.Width = 125;
		this.vHostConfigStatus.HeaderText = "Config status";
		this.vHostConfigStatus.MinimumWidth = 6;
		this.vHostConfigStatus.Name = "vHostConfigStatus";
		this.vHostConfigStatus.ReadOnly = true;
		this.vHostConfigStatus.ToolTipText = "Config status";
		this.vHostConfigStatus.Width = 125;
		this.vHostComplianceCheckState.HeaderText = "Compliance Check State";
		this.vHostComplianceCheckState.MinimumWidth = 6;
		this.vHostComplianceCheckState.Name = "vHostComplianceCheckState";
		this.vHostComplianceCheckState.ReadOnly = true;
		this.vHostComplianceCheckState.ToolTipText = "The host profile compliance check state";
		this.vHostComplianceCheckState.Width = 300;
		this.vHostinMaintenanceMode.HeaderText = "in Maintenance Mode";
		this.vHostinMaintenanceMode.MinimumWidth = 6;
		this.vHostinMaintenanceMode.Name = "vHostinMaintenanceMode";
		this.vHostinMaintenanceMode.ReadOnly = true;
		this.vHostinMaintenanceMode.ToolTipText = "The flag to indicate whether or not the host is in maintenance mode.";
		this.vHostinMaintenanceMode.Width = 200;
		this.vHostinQuarantineMode.HeaderText = "in Quarantine Mode";
		this.vHostinQuarantineMode.MinimumWidth = 6;
		this.vHostinQuarantineMode.Name = "vHostinQuarantineMode";
		this.vHostinQuarantineMode.ReadOnly = true;
		this.vHostinQuarantineMode.ToolTipText = "The flag to indicate whether or not the host is in quarantine mode";
		this.vHostinQuarantineMode.Width = 200;
		this.vHostvSANFaultDomainName.HeaderText = "vSAN Fault Domain Name";
		this.vHostvSANFaultDomainName.MinimumWidth = 6;
		this.vHostvSANFaultDomainName.Name = "vHostvSANFaultDomainName";
		this.vHostvSANFaultDomainName.ReadOnly = true;
		this.vHostvSANFaultDomainName.ToolTipText = "vSAN Host Fault Domain Name";
		this.vHostvSANFaultDomainName.Width = 300;
		this.vHostCpuModel.HeaderText = "CPU Model";
		this.vHostCpuModel.MinimumWidth = 6;
		this.vHostCpuModel.Name = "vHostCpuModel";
		this.vHostCpuModel.ReadOnly = true;
		this.vHostCpuModel.ToolTipText = "The CPU model";
		this.vHostCpuModel.Width = 150;
		dataGridViewCellStyle202.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle202.Format = "N0";
		dataGridViewCellStyle202.NullValue = null;
		this.vHostCpuMhz.DefaultCellStyle = dataGridViewCellStyle202;
		this.vHostCpuMhz.HeaderText = "Speed";
		this.vHostCpuMhz.MinimumWidth = 6;
		this.vHostCpuMhz.Name = "vHostCpuMhz";
		this.vHostCpuMhz.ReadOnly = true;
		this.vHostCpuMhz.ToolTipText = "The speed of the CPU cores.";
		this.vHostCpuMhz.Width = 125;
		this.vHostHTAvailable.HeaderText = "HT Available";
		this.vHostHTAvailable.MinimumWidth = 6;
		this.vHostHTAvailable.Name = "vHostHTAvailable";
		this.vHostHTAvailable.ReadOnly = true;
		this.vHostHTAvailable.ToolTipText = "The flag to indicate whether or not hyperthreading optimization is available on the system";
		this.vHostHTAvailable.Width = 125;
		this.vHostHTActive.HeaderText = "HT Active";
		this.vHostHTActive.MinimumWidth = 6;
		this.vHostHTActive.Name = "vHostHTActive";
		this.vHostHTActive.ReadOnly = true;
		this.vHostHTActive.ToolTipText = "The flag to indicate whether or not the CPU scheduler is currently treating hyperthreads as schedulable resources.";
		this.vHostHTActive.Width = 125;
		dataGridViewCellStyle203.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle203.Format = "N0";
		dataGridViewCellStyle203.NullValue = null;
		this.vHostNumCpu.DefaultCellStyle = dataGridViewCellStyle203;
		this.vHostNumCpu.HeaderText = "# CPU";
		this.vHostNumCpu.MinimumWidth = 6;
		this.vHostNumCpu.Name = "vHostNumCpu";
		this.vHostNumCpu.ReadOnly = true;
		this.vHostNumCpu.ToolTipText = "Total number of CPUs";
		this.vHostNumCpu.Width = 150;
		dataGridViewCellStyle204.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle204.Format = "N0";
		dataGridViewCellStyle204.NullValue = null;
		this.vHostCoresPerCPU.DefaultCellStyle = dataGridViewCellStyle204;
		this.vHostCoresPerCPU.FillWeight = 150f;
		this.vHostCoresPerCPU.HeaderText = "Cores per CPU";
		this.vHostCoresPerCPU.MinimumWidth = 6;
		this.vHostCoresPerCPU.Name = "vHostCoresPerCPU";
		this.vHostCoresPerCPU.ReadOnly = true;
		this.vHostCoresPerCPU.ToolTipText = "Cores per CPU";
		this.vHostCoresPerCPU.Width = 150;
		dataGridViewCellStyle205.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle205.Format = "N0";
		dataGridViewCellStyle205.NullValue = null;
		this.vHostNumCpuCores.DefaultCellStyle = dataGridViewCellStyle205;
		this.vHostNumCpuCores.HeaderText = "# Cores";
		this.vHostNumCpuCores.MinimumWidth = 6;
		this.vHostNumCpuCores.Name = "vHostNumCpuCores";
		this.vHostNumCpuCores.ReadOnly = true;
		this.vHostNumCpuCores.ToolTipText = "Total number of cores";
		this.vHostNumCpuCores.Width = 150;
		dataGridViewCellStyle206.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle206.Format = "N0";
		dataGridViewCellStyle206.NullValue = null;
		this.vHostOverallCpuUsage.DefaultCellStyle = dataGridViewCellStyle206;
		this.vHostOverallCpuUsage.HeaderText = "CPU usage %";
		this.vHostOverallCpuUsage.MinimumWidth = 6;
		this.vHostOverallCpuUsage.Name = "vHostOverallCpuUsage";
		this.vHostOverallCpuUsage.ReadOnly = true;
		this.vHostOverallCpuUsage.ToolTipText = "CPU usage across all cores on the host in %";
		this.vHostOverallCpuUsage.Width = 125;
		dataGridViewCellStyle207.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle207.Format = "N0";
		dataGridViewCellStyle207.NullValue = null;
		this.vHostMemorySize.DefaultCellStyle = dataGridViewCellStyle207;
		this.vHostMemorySize.HeaderText = "# Memory";
		this.vHostMemorySize.MinimumWidth = 6;
		this.vHostMemorySize.Name = "vHostMemorySize";
		this.vHostMemorySize.ReadOnly = true;
		this.vHostMemorySize.ToolTipText = "Total amount of physical memory on the host in MiB.";
		this.vHostMemorySize.Width = 125;
		this.vHostMemoryTieringType.HeaderText = "Memory Tiering Type";
		this.vHostMemoryTieringType.MinimumWidth = 6;
		this.vHostMemoryTieringType.Name = "vHostMemoryTieringType";
		this.vHostMemoryTieringType.ReadOnly = true;
		this.vHostMemoryTieringType.ToolTipText = "Type of memory tiering configured on this host";
		this.vHostMemoryTieringType.Width = 300;
		dataGridViewCellStyle208.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle208.Format = "N0";
		dataGridViewCellStyle208.NullValue = null;
		this.vHostOverallMemoryUsage.DefaultCellStyle = dataGridViewCellStyle208;
		this.vHostOverallMemoryUsage.HeaderText = "Memory usage %";
		this.vHostOverallMemoryUsage.MinimumWidth = 6;
		this.vHostOverallMemoryUsage.Name = "vHostOverallMemoryUsage";
		this.vHostOverallMemoryUsage.ReadOnly = true;
		this.vHostOverallMemoryUsage.ToolTipText = "Physical memory usage on the host in %";
		this.vHostOverallMemoryUsage.Width = 150;
		dataGridViewCellStyle209.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle209.Format = "N0";
		dataGridViewCellStyle209.NullValue = null;
		this.vHostServiceConsoleReserved.DefaultCellStyle = dataGridViewCellStyle209;
		this.vHostServiceConsoleReserved.HeaderText = "Console";
		this.vHostServiceConsoleReserved.MinimumWidth = 6;
		this.vHostServiceConsoleReserved.Name = "vHostServiceConsoleReserved";
		this.vHostServiceConsoleReserved.ReadOnly = true;
		this.vHostServiceConsoleReserved.ToolTipText = "The amount of memory that is currently reserved for the service console";
		this.vHostServiceConsoleReserved.Width = 125;
		dataGridViewCellStyle210.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle210.Format = "N0";
		dataGridViewCellStyle210.NullValue = null;
		this.vHostNumNics.DefaultCellStyle = dataGridViewCellStyle210;
		this.vHostNumNics.HeaderText = "# NICs";
		this.vHostNumNics.MinimumWidth = 6;
		this.vHostNumNics.Name = "vHostNumNics";
		this.vHostNumNics.ReadOnly = true;
		this.vHostNumNics.ToolTipText = "The number of network adapters";
		this.vHostNumNics.Width = 125;
		dataGridViewCellStyle211.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle211.Format = "N0";
		dataGridViewCellStyle211.NullValue = null;
		this.vHostNumHBAs.DefaultCellStyle = dataGridViewCellStyle211;
		this.vHostNumHBAs.HeaderText = "# HBAs";
		this.vHostNumHBAs.MinimumWidth = 6;
		this.vHostNumHBAs.Name = "vHostNumHBAs";
		this.vHostNumHBAs.ReadOnly = true;
		this.vHostNumHBAs.ToolTipText = "The number of host bus adapters ";
		this.vHostNumHBAs.Width = 125;
		dataGridViewCellStyle212.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle212.Format = "N0";
		dataGridViewCellStyle212.NullValue = null;
		this.vHostVMsTotal.DefaultCellStyle = dataGridViewCellStyle212;
		this.vHostVMsTotal.HeaderText = "# VMs total";
		this.vHostVMsTotal.MinimumWidth = 6;
		this.vHostVMsTotal.Name = "vHostVMsTotal";
		this.vHostVMsTotal.ReadOnly = true;
		this.vHostVMsTotal.ToolTipText = "Total number of VMs on Host.";
		this.vHostVMsTotal.Width = 125;
		dataGridViewCellStyle213.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle213.Format = "N0";
		dataGridViewCellStyle213.NullValue = null;
		this.vHostVMs.DefaultCellStyle = dataGridViewCellStyle213;
		this.vHostVMs.HeaderText = "# VMs";
		this.vHostVMs.MinimumWidth = 6;
		this.vHostVMs.Name = "vHostVMs";
		this.vHostVMs.ReadOnly = true;
		this.vHostVMs.ToolTipText = "Number of VMs running on Host.";
		this.vHostVMs.Width = 125;
		dataGridViewCellStyle214.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle214.Format = "N2";
		dataGridViewCellStyle214.NullValue = null;
		this.vHostVMsPerCore.DefaultCellStyle = dataGridViewCellStyle214;
		this.vHostVMsPerCore.HeaderText = "VMs per Core";
		this.vHostVMsPerCore.MinimumWidth = 6;
		this.vHostVMsPerCore.Name = "vHostVMsPerCore";
		this.vHostVMsPerCore.ReadOnly = true;
		this.vHostVMsPerCore.ToolTipText = "Number of running VMs per core";
		this.vHostVMsPerCore.Width = 125;
		dataGridViewCellStyle215.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle215.Format = "N0";
		dataGridViewCellStyle215.NullValue = null;
		this.vHostvCPUs.DefaultCellStyle = dataGridViewCellStyle215;
		this.vHostvCPUs.HeaderText = "# vCPUs";
		this.vHostvCPUs.MinimumWidth = 6;
		this.vHostvCPUs.Name = "vHostvCPUs";
		this.vHostvCPUs.ReadOnly = true;
		this.vHostvCPUs.ToolTipText = "Total number of running virtual CPUs";
		this.vHostvCPUs.Width = 125;
		dataGridViewCellStyle216.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle216.Format = "N2";
		dataGridViewCellStyle216.NullValue = null;
		this.vHostVCPUsPerCore.DefaultCellStyle = dataGridViewCellStyle216;
		this.vHostVCPUsPerCore.HeaderText = "vCPUs per Core";
		this.vHostVCPUsPerCore.MinimumWidth = 6;
		this.vHostVCPUsPerCore.Name = "vHostVCPUsPerCore";
		this.vHostVCPUsPerCore.ReadOnly = true;
		this.vHostVCPUsPerCore.ToolTipText = "Number of running virtual CPUs per Core";
		this.vHostVCPUsPerCore.Width = 150;
		dataGridViewCellStyle217.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle217.Format = "N0";
		dataGridViewCellStyle217.NullValue = null;
		this.vHostvRAM.DefaultCellStyle = dataGridViewCellStyle217;
		this.vHostvRAM.HeaderText = "vRAM";
		this.vHostvRAM.MinimumWidth = 6;
		this.vHostvRAM.Name = "vHostvRAM";
		this.vHostvRAM.ReadOnly = true;
		this.vHostvRAM.ToolTipText = "vRAM is total amount of virtual RAM allocated to all running VMs";
		this.vHostvRAM.Width = 125;
		dataGridViewCellStyle218.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle218.Format = "N0";
		dataGridViewCellStyle218.NullValue = null;
		this.vHostVMUsedMemory.DefaultCellStyle = dataGridViewCellStyle218;
		this.vHostVMUsedMemory.HeaderText = "VM Used memory";
		this.vHostVMUsedMemory.MinimumWidth = 6;
		this.vHostVMUsedMemory.Name = "vHostVMUsedMemory";
		this.vHostVMUsedMemory.ReadOnly = true;
		this.vHostVMUsedMemory.ToolTipText = "Guest memory: Total amount of memory in MiB, recently accessed";
		this.vHostVMUsedMemory.Width = 200;
		dataGridViewCellStyle219.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle219.Format = "N0";
		dataGridViewCellStyle219.NullValue = null;
		this.vHostVMSwappedMemory.DefaultCellStyle = dataGridViewCellStyle219;
		this.vHostVMSwappedMemory.HeaderText = "VM Memory Swapped";
		this.vHostVMSwappedMemory.MinimumWidth = 6;
		this.vHostVMSwappedMemory.Name = "vHostVMSwappedMemory";
		this.vHostVMSwappedMemory.ReadOnly = true;
		this.vHostVMSwappedMemory.ToolTipText = "Guest memory: Total amount of memory in MiB, reclaimed by swapping";
		this.vHostVMSwappedMemory.Width = 200;
		dataGridViewCellStyle220.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		dataGridViewCellStyle220.Format = "N0";
		dataGridViewCellStyle220.NullValue = null;
		this.vHostVMBalloonedMemory.DefaultCellStyle = dataGridViewCellStyle220;
		this.vHostVMBalloonedMemory.HeaderText = "VM Memory Ballooned";
		this.vHostVMBalloonedMemory.MinimumWidth = 6;
		this.vHostVMBalloonedMemory.Name = "vHostVMBalloonedMemory";
		this.vHostVMBalloonedMemory.ReadOnly = true;
		this.vHostVMBalloonedMemory.ToolTipText = "Guest memory: Total amount of memory in MiB, reclaimed by ballooning";
		this.vHostVMBalloonedMemory.Width = 200;
		this.vHostVmotionSupported.HeaderText = "VMotion support";
		this.vHostVmotionSupported.MinimumWidth = 6;
		this.vHostVmotionSupported.Name = "vHostVmotionSupported";
		this.vHostVmotionSupported.ReadOnly = true;
		this.vHostVmotionSupported.ToolTipText = "Flag indicating whether you can perform VMotion";
		this.vHostVmotionSupported.Width = 150;
		this.vHostStorageVmotionSupported.HeaderText = "Storage VMotion support";
		this.vHostStorageVmotionSupported.MinimumWidth = 6;
		this.vHostStorageVmotionSupported.Name = "vHostStorageVmotionSupported";
		this.vHostStorageVmotionSupported.ReadOnly = true;
		this.vHostStorageVmotionSupported.ToolTipText = "Indicates whether the storage of a powered-on virtual machine may be relocated";
		this.vHostStorageVmotionSupported.Width = 200;
		this.vHostCurrentEVCModeKey.HeaderText = "Current EVC";
		this.vHostCurrentEVCModeKey.MinimumWidth = 6;
		this.vHostCurrentEVCModeKey.Name = "vHostCurrentEVCModeKey";
		this.vHostCurrentEVCModeKey.ReadOnly = true;
		this.vHostCurrentEVCModeKey.ToolTipText = "The Enhanced VMotion Compatibility mode that is currently in effect for this host";
		this.vHostCurrentEVCModeKey.Width = 125;
		this.vHostMaxEVCModeKey.HeaderText = "Max EVC";
		this.vHostMaxEVCModeKey.MinimumWidth = 6;
		this.vHostMaxEVCModeKey.Name = "vHostMaxEVCModeKey";
		this.vHostMaxEVCModeKey.ReadOnly = true;
		this.vHostMaxEVCModeKey.ToolTipText = "The most capable Enhanced VMotion Compatibility mode supported by the host hardware and software";
		this.vHostMaxEVCModeKey.Width = 125;
		this.vHostAssignedLicenses.HeaderText = "Assigned License(s)";
		this.vHostAssignedLicenses.MinimumWidth = 6;
		this.vHostAssignedLicenses.Name = "vHostAssignedLicenses";
		this.vHostAssignedLicenses.ReadOnly = true;
		this.vHostAssignedLicenses.Width = 200;
		dataGridViewCellStyle221.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		this.vHostATSHeartbeat.DefaultCellStyle = dataGridViewCellStyle221;
		this.vHostATSHeartbeat.HeaderText = "ATS Heartbeat";
		this.vHostATSHeartbeat.MinimumWidth = 6;
		this.vHostATSHeartbeat.Name = "vHostATSHeartbeat";
		this.vHostATSHeartbeat.ReadOnly = true;
		this.vHostATSHeartbeat.ToolTipText = "ATS Heartbeat. 0 = disabled 1 = enabled";
		this.vHostATSHeartbeat.Width = 150;
		dataGridViewCellStyle222.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
		this.vHostATSLocking.DefaultCellStyle = dataGridViewCellStyle222;
		this.vHostATSLocking.HeaderText = "ATS Locking";
		this.vHostATSLocking.MinimumWidth = 6;
		this.vHostATSLocking.Name = "vHostATSLocking";
		this.vHostATSLocking.ReadOnly = true;
		this.vHostATSLocking.ToolTipText = "ATS locking. 0 = disabled 1 = enabled";
		this.vHostATSLocking.Width = 125;
		this.vHostCurrentPolicy.HeaderText = "Current CPU power man. policy";
		this.vHostCurrentPolicy.MinimumWidth = 6;
		this.vHostCurrentPolicy.Name = "vHostCurrentPolicy";
		this.vHostCurrentPolicy.ReadOnly = true;
		this.vHostCurrentPolicy.ToolTipText = "Information about current CPU power management policy. ";
		this.vHostCurrentPolicy.Width = 250;
		this.vHostHardwareSupport.HeaderText = "Supported CPU power man.";
		this.vHostHardwareSupport.MinimumWidth = 6;
		this.vHostHardwareSupport.Name = "vHostHardwareSupport";
		this.vHostHardwareSupport.ReadOnly = true;
		this.vHostHardwareSupport.ToolTipText = "Information about supported CPU power management. ";
		this.vHostHardwareSupport.Width = 250;
		this.vHostHostPowerPolicyShortname.HeaderText = "Host Power Policy";
		this.vHostHostPowerPolicyShortname.MinimumWidth = 6;
		this.vHostHostPowerPolicyShortname.Name = "vHostHostPowerPolicyShortname";
		this.vHostHostPowerPolicyShortname.ReadOnly = true;
		this.vHostHostPowerPolicyShortname.ToolTipText = "Power Policy Short Name";
		this.vHostHostPowerPolicyShortname.Width = 200;
		this.vHostFullName.HeaderText = "ESX Version";
		this.vHostFullName.MinimumWidth = 6;
		this.vHostFullName.Name = "vHostFullName";
		this.vHostFullName.ReadOnly = true;
		this.vHostFullName.ToolTipText = "The complete product name, including the version information";
		this.vHostFullName.Width = 125;
		dataGridViewCellStyle223.Format = "G";
		dataGridViewCellStyle223.NullValue = null;
		this.vHostBootTime.DefaultCellStyle = dataGridViewCellStyle223;
		this.vHostBootTime.HeaderText = "Boot time";
		this.vHostBootTime.MinimumWidth = 6;
		this.vHostBootTime.Name = "vHostBootTime";
		this.vHostBootTime.ReadOnly = true;
		this.vHostBootTime.ToolTipText = "The time when the host was booted.";
		this.vHostBootTime.Width = 125;
		this.vHostDNSServers.HeaderText = "DNS Servers";
		this.vHostDNSServers.MinimumWidth = 6;
		this.vHostDNSServers.Name = "vHostDNSServers";
		this.vHostDNSServers.ReadOnly = true;
		this.vHostDNSServers.ToolTipText = "The IP addresses of the DNS servers, placed in order of preference. ";
		this.vHostDNSServers.Width = 125;
		this.vHostDHCP.HeaderText = "DHCP";
		this.vHostDHCP.MinimumWidth = 6;
		this.vHostDHCP.Name = "vHostDHCP";
		this.vHostDHCP.ReadOnly = true;
		this.vHostDHCP.ToolTipText = "The flag to indicate whether or not DHCP is used";
		this.vHostDHCP.Width = 125;
		this.vHostDomainName.HeaderText = "Domain";
		this.vHostDomainName.MinimumWidth = 6;
		this.vHostDomainName.Name = "vHostDomainName";
		this.vHostDomainName.ReadOnly = true;
		this.vHostDomainName.ToolTipText = "The domain name portion of the DNS name";
		this.vHostDomainName.Width = 125;
		this.vHostDomainList.HeaderText = "Domain List";
		this.vHostDomainList.MinimumWidth = 6;
		this.vHostDomainList.Name = "vHostDomainList";
		this.vHostDomainList.ReadOnly = true;
		this.vHostDomainList.ToolTipText = "List of Windows domains available for user searches, if the underlying system supports windows domain membership";
		this.vHostDomainList.Width = 300;
		this.vHostSearchDomains.HeaderText = "DNS Search Order";
		this.vHostSearchDomains.MinimumWidth = 6;
		this.vHostSearchDomains.Name = "vHostSearchDomains";
		this.vHostSearchDomains.ReadOnly = true;
		this.vHostSearchDomains.ToolTipText = "The domain in which to search for hosts, placed in order of preference.";
		this.vHostSearchDomains.Width = 150;
		this.vHostNTPServers.HeaderText = "NTP Server(s)";
		this.vHostNTPServers.MinimumWidth = 6;
		this.vHostNTPServers.Name = "vHostNTPServers";
		this.vHostNTPServers.ReadOnly = true;
		this.vHostNTPServers.ToolTipText = "List of time servers";
		this.vHostNTPServers.Width = 125;
		this.vHostNTPD.HeaderText = "NTPD running";
		this.vHostNTPD.MinimumWidth = 6;
		this.vHostNTPD.Name = "vHostNTPD";
		this.vHostNTPD.ReadOnly = true;
		this.vHostNTPD.ToolTipText = "NTPD  running status";
		this.vHostNTPD.Width = 150;
		this.vHostTZDescription.HeaderText = "Time Zone";
		this.vHostTZDescription.MinimumWidth = 6;
		this.vHostTZDescription.Name = "vHostTZDescription";
		this.vHostTZDescription.ReadOnly = true;
		this.vHostTZDescription.ToolTipText = "Description of the time zone";
		this.vHostTZDescription.Width = 125;
		this.vHostTZName.HeaderText = "Time Zone Name";
		this.vHostTZName.MinimumWidth = 6;
		this.vHostTZName.Name = "vHostTZName";
		this.vHostTZName.ReadOnly = true;
		this.vHostTZName.ToolTipText = "The time zone name";
		this.vHostTZName.Width = 150;
		this.vHostTZOffset.HeaderText = "GMT Offset";
		this.vHostTZOffset.MinimumWidth = 6;
		this.vHostTZOffset.Name = "vHostTZOffset";
		this.vHostTZOffset.ReadOnly = true;
		this.vHostTZOffset.ToolTipText = "The GMT offset in seconds";
		this.vHostTZOffset.Width = 125;
		this.vHostVendor.HeaderText = "Vendor";
		this.vHostVendor.MinimumWidth = 6;
		this.vHostVendor.Name = "vHostVendor";
		this.vHostVendor.ReadOnly = true;
		this.vHostVendor.ToolTipText = "Hardware vendor identification";
		this.vHostVendor.Width = 125;
		this.vHostModel.HeaderText = "Model";
		this.vHostModel.MinimumWidth = 6;
		this.vHostModel.Name = "vHostModel";
		this.vHostModel.ReadOnly = true;
		this.vHostModel.ToolTipText = "System model identification";
		this.vHostModel.Width = 125;
		this.vHostSerialNumber.HeaderText = "Serial number";
		this.vHostSerialNumber.MinimumWidth = 6;
		this.vHostSerialNumber.Name = "vHostSerialNumber";
		this.vHostSerialNumber.ReadOnly = true;
		this.vHostSerialNumber.ToolTipText = "Serial number";
		this.vHostSerialNumber.Width = 200;
		this.vHostServiceTag.HeaderText = "Service tag";
		this.vHostServiceTag.MinimumWidth = 6;
		this.vHostServiceTag.Name = "vHostServiceTag";
		this.vHostServiceTag.ReadOnly = true;
		this.vHostServiceTag.ToolTipText = "Service tag of the system";
		this.vHostServiceTag.Width = 125;
		this.vHostOemSpecificString.HeaderText = "OEM specific string";
		this.vHostOemSpecificString.MinimumWidth = 6;
		this.vHostOemSpecificString.Name = "vHostOemSpecificString";
		this.vHostOemSpecificString.ReadOnly = true;
		this.vHostOemSpecificString.ToolTipText = "OEM specific string";
		this.vHostOemSpecificString.Width = 150;
		this.vHostBiosVendor.HeaderText = "BIOS Vendor";
		this.vHostBiosVendor.MinimumWidth = 6;
		this.vHostBiosVendor.Name = "vHostBiosVendor";
		this.vHostBiosVendor.ReadOnly = true;
		this.vHostBiosVendor.ToolTipText = "The vendor for the BIOS";
		this.vHostBiosVendor.Width = 150;
		dataGridViewCellStyle224.NullValue = null;
		this.vHostBiosVersion.DefaultCellStyle = dataGridViewCellStyle224;
		this.vHostBiosVersion.HeaderText = "BIOS Version";
		this.vHostBiosVersion.MinimumWidth = 6;
		this.vHostBiosVersion.Name = "vHostBiosVersion";
		this.vHostBiosVersion.ReadOnly = true;
		this.vHostBiosVersion.ToolTipText = "The current BIOS version of the physical chassis ";
		this.vHostBiosVersion.Width = 125;
		dataGridViewCellStyle225.Format = "G";
		dataGridViewCellStyle225.NullValue = null;
		this.vHostBiosDate.DefaultCellStyle = dataGridViewCellStyle225;
		this.vHostBiosDate.HeaderText = "BIOS Date";
		this.vHostBiosDate.MinimumWidth = 6;
		this.vHostBiosDate.Name = "vHostBiosDate";
		this.vHostBiosDate.ReadOnly = true;
		this.vHostBiosDate.ToolTipText = "The release date for the BIOS";
		this.vHostBiosDate.Width = 125;
		this.vHostCertificateIssuer.HeaderText = "Certificate Issuer";
		this.vHostCertificateIssuer.MinimumWidth = 6;
		this.vHostCertificateIssuer.Name = "vHostCertificateIssuer";
		this.vHostCertificateIssuer.ReadOnly = true;
		this.vHostCertificateIssuer.ToolTipText = "The issuer of the certificate.";
		this.vHostCertificateIssuer.Width = 300;
		dataGridViewCellStyle226.Format = "G";
		dataGridViewCellStyle226.NullValue = null;
		this.vHostCertificateStartDate.DefaultCellStyle = dataGridViewCellStyle226;
		this.vHostCertificateStartDate.HeaderText = "Certificate Start Date";
		this.vHostCertificateStartDate.MinimumWidth = 6;
		this.vHostCertificateStartDate.Name = "vHostCertificateStartDate";
		this.vHostCertificateStartDate.ReadOnly = true;
		this.vHostCertificateStartDate.ToolTipText = "The validity of the certificate.";
		this.vHostCertificateStartDate.Width = 300;
		dataGridViewCellStyle227.Format = "G";
		dataGridViewCellStyle227.NullValue = null;
		this.vHostCertificateExpiry.DefaultCellStyle = dataGridViewCellStyle227;
		this.vHostCertificateExpiry.HeaderText = "Certificate Expiry Date";
		this.vHostCertificateExpiry.MinimumWidth = 6;
		this.vHostCertificateExpiry.Name = "vHostCertificateExpiry";
		this.vHostCertificateExpiry.ReadOnly = true;
		this.vHostCertificateExpiry.ToolTipText = "The validity of the certificate.";
		this.vHostCertificateExpiry.Width = 300;
		this.vHostCertificateStatus.HeaderText = "Certificate Status";
		this.vHostCertificateStatus.MinimumWidth = 6;
		this.vHostCertificateStatus.Name = "vHostCertificateStatus";
		this.vHostCertificateStatus.ReadOnly = true;
		this.vHostCertificateStatus.ToolTipText = "The status of the certificate in vCenter Server.";
		this.vHostCertificateStatus.Width = 300;
		this.vHostCertificateSubject.HeaderText = "Certificate Subject";
		this.vHostCertificateSubject.MinimumWidth = 6;
		this.vHostCertificateSubject.Name = "vHostCertificateSubject";
		this.vHostCertificateSubject.ReadOnly = true;
		this.vHostCertificateSubject.ToolTipText = "The subject of the certificate.";
		this.vHostCertificateSubject.Width = 300;
		this.vHostObjectID.HeaderText = "Object ID";
		this.vHostObjectID.MinimumWidth = 6;
		this.vHostObjectID.Name = "vHostObjectID";
		this.vHostObjectID.ReadOnly = true;
		this.vHostObjectID.ToolTipText = "Host object id";
		this.vHostObjectID.Width = 125;
		this.vHostUUID.HeaderText = "UUID";
		this.vHostUUID.MinimumWidth = 6;
		this.vHostUUID.Name = "vHostUUID";
		this.vHostUUID.ReadOnly = true;
		this.vHostUUID.ToolTipText = "Host UUID";
		this.vHostUUID.Width = 125;
		this.vHostVISDKServer.HeaderText = "VI SDK Server";
		this.vHostVISDKServer.MinimumWidth = 6;
		this.vHostVISDKServer.Name = "vHostVISDKServer";
		this.vHostVISDKServer.ReadOnly = true;
		this.vHostVISDKServer.ToolTipText = "VI SDK Server, where RVTools gets it's information from";
		this.vHostVISDKServer.Width = 150;
		this.vHostInstanceUUID.HeaderText = "VI SDK UUID";
		this.vHostInstanceUUID.MinimumWidth = 6;
		this.vHostInstanceUUID.Name = "vHostInstanceUUID";
		this.vHostInstanceUUID.ReadOnly = true;
		this.vHostInstanceUUID.ToolTipText = "A globally unique identifier associated with this service instance";
		this.vHostInstanceUUID.Width = 125;
		base.AutoScaleDimensions = new System.Drawing.SizeF(8f, 16f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(1759, 484);
		base.Controls.Add(this.TabPages);
		base.Controls.Add(this.statusStrip1);
		base.Controls.Add(this.menuStrip1);
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.MainMenuStrip = this.menuStrip1;
		base.Margin = new System.Windows.Forms.Padding(4);
		base.Name = "FMain";
		base.Padding = new System.Windows.Forms.Padding(13, 6, 13, 0);
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "RVTools";
		base.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FMain_FormClosing);
		base.Load += new System.EventHandler(this.FMain_Load);
		base.Resize += new System.EventHandler(this.FMain_Resize);
		this.statusStrip1.ResumeLayout(false);
		this.statusStrip1.PerformLayout();
		this.menuStrip1.ResumeLayout(false);
		this.menuStrip1.PerformLayout();
		this.tabvLicense.ResumeLayout(false);
		this.panel27.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dataGridvLicense).EndInit();
		this.tabvHealth.ResumeLayout(false);
		this.panel15.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dataGridvHealth).EndInit();
		this.tabvMultiPath.ResumeLayout(false);
		this.panel26.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dataGridvMultiPath).EndInit();
		this.tabvDatastore.ResumeLayout(false);
		this.panel3.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dataGridvDatastore).EndInit();
		this.tabvSC_VMK.ResumeLayout(false);
		this.panel20.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dataGridvSC_VMK).EndInit();
		this.tabdvPort.ResumeLayout(false);
		this.panel22.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dataGriddvPort).EndInit();
		this.tabdvSwitch.ResumeLayout(false);
		this.panel21.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dataGriddvSwitch).EndInit();
		this.tabvPort.ResumeLayout(false);
		this.panel18.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dataGridvPort).EndInit();
		this.tabvSwitch.ResumeLayout(false);
		this.panel17.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dataGridvSwitch).EndInit();
		this.tabvNIC.ResumeLayout(false);
		this.panel19.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dataGridvNIC).EndInit();
		this.tabvHBA.ResumeLayout(false);
		this.panel23.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dataGridvHBA).EndInit();
		this.tabvHost.ResumeLayout(false);
		this.panel14.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dataGridvHost).EndInit();
		this.tabvCluster.ResumeLayout(false);
		this.panel25.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dataGridvCluster).EndInit();
		this.tabvRP.ResumeLayout(false);
		this.panel24.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dataGridvRP).EndInit();
		this.tabvTools.ResumeLayout(false);
		this.panel10.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dataGridvTools).EndInit();
		this.panel11.ResumeLayout(false);
		this.tabvSnapshot.ResumeLayout(false);
		this.panel9.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dataGridvSnapshot).EndInit();
		this.tabvCD.ResumeLayout(false);
		this.panel12.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dataGridvCD).EndInit();
		this.panel13.ResumeLayout(false);
		this.tabvUSB.ResumeLayout(false);
		this.panel1.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dataGridvUSB).EndInit();
		this.panel2.ResumeLayout(false);
		this.tabvNetwork.ResumeLayout(false);
		this.panel5.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dataGridvNetwork).EndInit();
		this.tabvPartition.ResumeLayout(false);
		this.panel16.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dataGridvPartition).EndInit();
		this.tabvDisk.ResumeLayout(false);
		this.panel6.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dataGridvDisk).EndInit();
		this.tabvMemory.ResumeLayout(false);
		this.panel8.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dataGridvMemory).EndInit();
		this.tabvCPU.ResumeLayout(false);
		this.panel7.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dataGridvCPU).EndInit();
		this.tabvInfo.ResumeLayout(false);
		this.panel4.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dataGridvInfo).EndInit();
		this.TabPages.ResumeLayout(false);
		this.tabvSource.ResumeLayout(false);
		this.panel29.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dataGridvSource).EndInit();
		this.tabvFileInfo.ResumeLayout(false);
		this.panel28.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dataGridvFileInfo).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
