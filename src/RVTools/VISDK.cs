using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RVTools.CustomerExperience;
using RVTools.LoginSSPI.Api;
using RVTools.Utils;
using Vim25Api;
using VMware.Binding.WsTrust;

namespace RVTools;

internal class VISDK
{
	public class _VMFileObject
	{
		public string dataStoreName { get; set; }

		public string pathName { get; set; }

		public string friendlyPathName { get; set; }

		public string fileName { get; set; }

		public string fileType { get; set; }

		public long fileSize { get; set; }
	}

	private static UserSession session;

	private static string _HostidPrevDatacenter = null;

	private static string _HostidPrevCluster = null;

	private static string _HostidPrevHost = null;

	private static string _prevDatacenter = null;

	private static string _prevCluster = null;

	private static string _prevHost = null;

	private static ObjectContent[] _Inventory;

	private static ObjectContent[] _DatastoreInventory;

	private static ObjectContent[] _ClusterInventory;

	private static ObjectContent[] _LicenseInventory;

	private static ArrayList _ResourcePoolInventory = new ArrayList();

	private static ObjectContent[] _HostHierarchy;

	private static ObjectContent[] _CustomFields;

	private static ArrayList _BrowserResult = new ArrayList();

	private static ArrayList _BrowserAllFiles = new ArrayList();

	private static ArrayList _BrowserResultErrors = new ArrayList();

	private static StringCollection _PropertySpecPathSet = new StringCollection();

	private static StringCollection _FolderName = new StringCollection();

	private static StringCollection _ResourcePool = new StringCollection();

	public static Dictionary<string, HostCertificateManagerCertificateInfo> _HostCertificates;

	public static Hashtable vSANFriendlyNames = new Hashtable();

	public static List<Hashtable> cisAssociatedTag = new List<Hashtable>();

	private static readonly ILog logger = LogManager.GetLogger(typeof(VISDK));

	public static readonly ITrace Trace = new Log4NetTrace(logger);

	public static readonly ITelemetryCollector Telemetry = FMain.Telemetry;

	private static UserNotification UserNotification = new UserNotification("FMain", Trace, Telemetry);

	public static bool Connected { get; set; }

	public static int ExitCode { get; set; } = 0;

	public static string Server { get; set; }

	public static string Username { get; set; }

	public static SecureString Password { get; set; }

	public static bool StartedCommandFromCLI { get; set; }

	public static VimPortType _service { get; set; }

	public static int APIVersion { get; private set; }

	public static ConnectionState _state { get; set; }

	public static ServiceContent _sic { get; set; }

	public static ManagedObjectReference _svcRef { get; set; }

	public static ObjectContent[] HostInventory { get; private set; }

	public static ArrayList DistributedSwitchInventory { get; } = new ArrayList();

	public static ArrayList DistributedPortInventory { get; } = new ArrayList();

	public static ObjectContent[] Inventory => _Inventory;

	public static int InventoryCounter => _Inventory.Length;

	public static int DatastoreInventoryCounter => _DatastoreInventory.Length;

	public static int ResourcePoolInventoryCounter => _ResourcePoolInventory.Count;

	public static int HostInventoryCounter
	{
		get
		{
			try
			{
				return HostInventory.Length;
			}
			catch (Exception ex)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug("get_HostInventoryCounter Handled Exception: " + ex.Message);
				}
				return 0;
			}
		}
	}

	public static int ClusterInventoryCounter => _ClusterInventory.Length;

	public static void PropertySpecPathSet_clear()
	{
		_PropertySpecPathSet.Clear();
	}

	public static void PropertySpecPathSet_add(string val)
	{
		_PropertySpecPathSet.Add(val);
	}

	public static void ClearBrowserResult()
	{
		_BrowserResult.Clear();
		_BrowserResultErrors.Clear();
	}

	public static void ClearBrowserFileInfoResult()
	{
		_BrowserAllFiles.Clear();
	}

	public static void ClearBrowserResultFriendlyNames()
	{
		vSANFriendlyNames.Clear();
	}

	public static ArrayList GetBrowserResult()
	{
		return _BrowserResult;
	}

	public static ArrayList GetBrowserAllFiles()
	{
		return _BrowserAllFiles;
	}

	public static int GetCounterAllFiles()
	{
		try
		{
			return _BrowserAllFiles.Count;
		}
		catch
		{
			return 0;
		}
	}

	public static ArrayList GetBrowserResultErrors()
	{
		return _BrowserResultErrors;
	}

	public static string GetFriendlyFolder(string vmFolder)
	{
		string result = vmFolder;
		try
		{
			string text = vmFolder.Split(' ')[1];
			text = text.Replace("/", "");
			string text2 = (string)vSANFriendlyNames[text];
			if (text2 != null)
			{
				result = vmFolder.Replace(text, text2);
			}
		}
		catch
		{
		}
		return result;
	}

	public static string GetFriendlyPath(string vmPath)
	{
		string result = vmPath;
		try
		{
			string text = vmPath.Split(' ')[1].Split('/')[0];
			string text2 = (string)vSANFriendlyNames[text];
			if (text2 != null)
			{
				result = vmPath.Replace(text, text2);
			}
		}
		catch
		{
		}
		return result;
	}

	public static byte[] HexStringToByteArray(string hex)
	{
		try
		{
			return (from x in Enumerable.Range(0, hex.Length)
				where x % 2 == 0
				select Convert.ToByte(hex.Substring(x, 2), 16)).ToArray();
		}
		catch
		{
			throw;
		}
	}

	public static SecureString Decrypt(string cipherText)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " cipherText = " + cipherText);
		}
		if (!cipherText.StartsWith("_RVToolsV3PWD"))
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " It is no longer allowed to use plaintext passwords for the RVTools command line interface");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Please encrypt your password with the provided PowerShell script RVToolsPasswordEncryption.ps1");
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Exit ");
			}
			return null;
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Decrypt password encrypted by PowerShell");
		}
		cipherText = cipherText.Replace("_RVToolsV3PWD", "");
		cipherText = cipherText.Replace(Environment.NewLine, "");
		try
		{
			SecureString result = ByteArrayToSecureString(ProtectedData.Unprotect(HexStringToByteArray(cipherText), null, DataProtectionScope.CurrentUser));
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Exit ");
			}
			return result;
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Error while decrypting a password encrypted by PowerShell: " + ex.Message);
			}
			return null;
		}
	}

	private static SecureString ByteArrayToSecureString(byte[] bytes)
	{
		SecureString secureString = new SecureString();
		for (int i = 0; i < bytes.Length; i += 2)
		{
			char c = (char)(bytes[i] + (bytes[i + 1] << 8));
			secureString.AppendChar(c);
		}
		secureString.MakeReadOnly();
		return secureString;
	}

	public static long GetInventoryPropertyInt(int index, string Property)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index and property " + index + ", " + Property);
		}
		try
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Exit");
			}
			return Convert.ToInt64(GetVmInventoryProperty(index, Property));
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
			return 0L;
		}
	}

	public static int GetAPIVersion()
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		string text = _sic.about.apiVersion;
		if (text == null)
		{
			text = _sic.about.version;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " apiVersion (string) = null use about.version = " + text);
			}
		}
		int num = 0;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " apiVersion (string) = " + text);
		}
		try
		{
			num = Convert.ToInt16(Regex.Replace(text, "[^0-9]", ""));
			if (num < 100)
			{
				num *= 10;
			}
			if (num > 1000)
			{
				num /= 10;
			}
		}
		catch
		{
			if (text.StartsWith("3"))
			{
				num = 300;
			}
			if (text.StartsWith("4"))
			{
				num = 400;
			}
			if (text.StartsWith("5"))
			{
				num = 500;
			}
			if (text.StartsWith("6"))
			{
				num = 600;
			}
			if (text.StartsWith("7"))
			{
				num = 700;
			}
			if (text.StartsWith("8"))
			{
				num = 800;
			}
			if (text.StartsWith("9"))
			{
				num = 900;
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " apiVersion (int) = " + num);
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit ");
		}
		return num;
	}

	public static string GetVmInventoryProperty(int index, string property)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for property " + property);
		}
		string text = null;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			foreach (DynamicProperty dynamicProperty in propSet)
			{
				if (dynamicProperty.name == property)
				{
					text = dynamicProperty.val.ToString();
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + text);
		}
		return text;
	}

	public static string GetSummaryConfigAnnotation(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		string text = null;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			int num = 0;
			if (string.Compare("summary.config.annotation", propSet[propSet.Length / 2].name) > 0)
			{
				num = propSet.Length / 2;
			}
			for (int i = num; i < propSet.Length; i++)
			{
				if (propSet[i].name == "summary.config.annotation")
				{
					text = propSet[i].val.ToString();
					text = Regex.Replace(text, "[\u0001-\u001f]", " ");
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + text);
		}
		return text;
	}

	public static string GetSummaryVM(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		string text = null;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			int num = 0;
			if (string.Compare("summary.vm", propSet[propSet.Length / 2].name) > 0)
			{
				num = propSet.Length / 2;
			}
			for (int i = num; i < propSet.Length; i++)
			{
				if (propSet[i].name == "summary.vm")
				{
					text = ((ManagedObjectReference)propSet[i].val).Value;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + text);
		}
		return text;
	}

	public static string GetOptionValueVM(int index, string key)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		string text = null;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			int num = 0;
			if (string.Compare("config.extraConfig", propSet[propSet.Length / 2].name) > 0)
			{
				num = propSet.Length / 2;
			}
			for (int i = num; i < propSet.Length; i++)
			{
				if (!(propSet[i].name == "config.extraConfig"))
				{
					continue;
				}
				OptionValue[] array = (OptionValue[])propSet[i].val;
				foreach (OptionValue optionValue in array)
				{
					if (optionValue.key == key)
					{
						return optionValue.value.ToString();
					}
				}
				break;
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + text);
		}
		return text;
	}

	public static string GetSummaryRuntimeHost(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		string text = null;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			int num = 0;
			if (string.Compare("summary.runtime.host", propSet[propSet.Length / 2].name) > 0)
			{
				num = propSet.Length / 2;
			}
			for (int i = num; i < propSet.Length; i++)
			{
				if (propSet[i].name == "summary.runtime.host")
				{
					text = ((ManagedObjectReference)propSet[i].val).Value;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + text);
		}
		return text;
	}

	public static string GetHostName(string hostid)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for hostid " + hostid);
		}
		string text = null;
		try
		{
			if (_HostidPrevHost == hostid)
			{
				return _prevHost;
			}
			ObjectContent[] hostInventory = HostInventory;
			foreach (ObjectContent objectContent in hostInventory)
			{
				if (!(objectContent.obj.Value == hostid))
				{
					continue;
				}
				DynamicProperty[] propSet = objectContent.propSet;
				int num = 0;
				if (string.Compare("name", propSet[propSet.Length / 2].name) > 0)
				{
					num = propSet.Length / 2;
				}
				for (int j = num; j < propSet.Length; j++)
				{
					if (propSet[j].name == "name")
					{
						text = propSet[j].val.ToString();
						break;
					}
				}
			}
			_HostidPrevHost = hostid;
			_prevHost = text;
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + text);
		}
		return text;
	}

	public static string GetVMFolderID(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		string result = null;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			int num = 0;
			if (string.Compare("parent", propSet[propSet.Length / 2].name) > 0)
			{
				num = propSet.Length / 2;
			}
			for (int i = num; i < propSet.Length; i++)
			{
				if (propSet[i].name == "parent")
				{
					result = ((ManagedObjectReference)propSet[i].val).Value;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		return result;
	}

	public static string GetVMFolder(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		string text = null;
		string text2 = null;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			ManagedObjectReference managedObjectReference = null;
			int num = 0;
			if (string.Compare("parent", propSet[propSet.Length / 2].name) > 0)
			{
				num = propSet.Length / 2;
			}
			for (int i = num; i < propSet.Length; i++)
			{
				if (propSet[i].name == "parent")
				{
					managedObjectReference = (ManagedObjectReference)propSet[i].val;
					break;
				}
			}
			if (managedObjectReference == null)
			{
				return null;
			}
			text2 = managedObjectReference.Value + "=";
			for (int j = 0; j < _FolderName.Count; j++)
			{
				string text3 = _FolderName[j];
				if (text3.StartsWith(text2))
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " Exit " + text3);
					}
					return text3.Remove(0, text2.Length);
				}
			}
			text = text2;
			StringCollection stringCollection = new StringCollection();
			bool flag = true;
			int num2 = 0;
			do
			{
				ObjectContent[] vMParent = GetVMParent(managedObjectReference);
				for (int k = 0; k < vMParent.Length; k++)
				{
					DynamicProperty[] propSet2 = vMParent[k].propSet;
					flag = false;
					for (int l = 0; l < propSet2.Length; l++)
					{
						if (propSet2[l].name == "name")
						{
							stringCollection.Add(propSet2[l].val.ToString());
						}
						if (propSet2[l].name == "parent")
						{
							managedObjectReference = (ManagedObjectReference)propSet2[l].val;
							flag = true;
						}
					}
				}
				num2++;
				if (num2 > 9)
				{
					flag = false;
				}
			}
			while (flag);
			for (int num3 = stringCollection.Count - 1; num3 >= 0; num3--)
			{
				if (!(stringCollection[num3] == "vm") && !(stringCollection[num3] == "Datacenters"))
				{
					text = text + "/" + stringCollection[num3];
				}
			}
			_FolderName.Add(text);
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		try
		{
			text = text.Remove(0, text2.Length);
		}
		catch (Exception ex2)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex2.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + text);
		}
		return text;
	}

	public static ObjectContent[] GetVMParent(ManagedObjectReference mor)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		ObjectContent[] result = null;
		try
		{
			ManagedObjectReference managedObjectReference = new ManagedObjectReference();
			managedObjectReference.type = mor.type;
			managedObjectReference.Value = mor.Value;
			result = GetObjectProperties(_sic.propertyCollector, managedObjectReference, new string[2] { "name", "parent" });
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit ObjectContent[]");
		}
		return result;
	}

	public static ObjectContent[] GetHostAdvancedOptions(ManagedObjectReference mor)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		ObjectContent[] result = null;
		try
		{
			ManagedObjectReference managedObjectReference = new ManagedObjectReference();
			managedObjectReference.type = mor.type;
			managedObjectReference.Value = mor.Value;
			result = GetObjectProperties(_sic.propertyCollector, managedObjectReference, new string[1] { "setting" });
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit GetHostAdvancedOptions[]");
		}
		return result;
	}

	public static ObjectContent[] GetHostVSanSystem(ManagedObjectReference mor)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		ObjectContent[] result = null;
		try
		{
			ManagedObjectReference managedObjectReference = new ManagedObjectReference();
			managedObjectReference.type = mor.type;
			managedObjectReference.Value = mor.Value;
			result = GetObjectProperties(_sic.propertyCollector, managedObjectReference, new string[1] { "config" });
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit method.Name");
		}
		return result;
	}

	public static HostCertificateManagerCertificateInfo GetHostCertificateInfo(ManagedObjectReference mor)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		HostCertificateManagerCertificateInfo result = null;
		try
		{
			ManagedObjectReference managedObjectReference = new ManagedObjectReference();
			managedObjectReference.type = mor.type;
			managedObjectReference.Value = mor.Value;
			result = (HostCertificateManagerCertificateInfo)(GetObjectProperties(_sic.propertyCollector, managedObjectReference, new string[1] { "certificateInfo" })?.FirstOrDefault()?.propSet?.FirstOrDefault()?.val);
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit method.Name");
		}
		return result;
	}

	public static string GetVMResourcePool(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		string text = null;
		string text2 = null;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			ManagedObjectReference managedObjectReference = null;
			int num = 0;
			if (string.Compare("resourcePool", propSet[propSet.Length / 2].name) > 0)
			{
				num = propSet.Length / 2;
			}
			for (int i = num; i < propSet.Length; i++)
			{
				if (propSet[i].name == "resourcePool")
				{
					managedObjectReference = (ManagedObjectReference)propSet[i].val;
					break;
				}
			}
			if (managedObjectReference == null)
			{
				return null;
			}
			text2 = managedObjectReference.Value + "=";
			for (int j = 0; j < _ResourcePool.Count; j++)
			{
				string text3 = _ResourcePool[j];
				if (text3.StartsWith(text2))
				{
					return text3.Remove(0, text2.Length);
				}
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Exit null");
			}
			return null;
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + text.Remove(0, text2.Length));
		}
		return text.Remove(0, text2.Length);
	}

	public static ObjectContent[] GetResourcePoolParent(ManagedObjectReference mor)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		ObjectContent[] result = null;
		try
		{
			ManagedObjectReference managedObjectReference = new ManagedObjectReference();
			managedObjectReference.type = mor.type;
			managedObjectReference.Value = mor.Value;
			result = GetObjectProperties(_sic.propertyCollector, managedObjectReference, new string[2] { "name", "parent" });
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit ObjectContent[]");
		}
		return result;
	}

	public static string GetDatacenterName(string hostid)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for hostid " + hostid);
		}
		string text = null;
		try
		{
			if (_HostidPrevDatacenter == hostid)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(methodBase.Name + " Exit " + _prevDatacenter);
				}
				return _prevDatacenter;
			}
			string id = hostid;
			ObjectContent objectContent = null;
			DynamicProperty[] array = null;
			while (true)
			{
				objectContent = GetParent(id);
				if (objectContent == null)
				{
					break;
				}
				array = objectContent.propSet;
				if (objectContent.obj.type == "Datacenter")
				{
					break;
				}
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i].name == "parent")
					{
						id = ((ManagedObjectReference)array[i].val).Value;
					}
				}
			}
			if (objectContent == null)
			{
				text = null;
			}
			else
			{
				for (int j = 0; j < array.Length; j++)
				{
					if (array[j].name == "name")
					{
						text = array[j].val.ToString();
						break;
					}
				}
			}
			_HostidPrevDatacenter = hostid;
			_prevDatacenter = text;
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + text);
		}
		return text;
	}

	public static string GetClusterName(string hostid)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for hostid " + hostid);
		}
		if (_HostidPrevCluster == hostid)
		{
			return _prevCluster;
		}
		string text = null;
		string id = hostid;
		ObjectContent objectContent = null;
		DynamicProperty[] array = null;
		while (true)
		{
			objectContent = GetParent(id);
			if (objectContent == null)
			{
				break;
			}
			array = objectContent.propSet;
			if (objectContent.obj.type == "ClusterComputeResource")
			{
				break;
			}
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].name == "parent")
				{
					id = ((ManagedObjectReference)array[i].val).Value;
				}
			}
		}
		if (objectContent == null)
		{
			text = null;
		}
		else
		{
			for (int j = 0; j < array.Length; j++)
			{
				if (array[j].name == "name")
				{
					text = array[j].val.ToString();
					break;
				}
			}
		}
		_HostidPrevCluster = hostid;
		_prevCluster = text;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + text);
		}
		return text;
	}

	public static ObjectContent GetParent(string id)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for id " + id);
		}
		ObjectContent result = null;
		try
		{
			ObjectContent[] hostHierarchy = _HostHierarchy;
			foreach (ObjectContent objectContent in hostHierarchy)
			{
				if (!(objectContent.obj.Value == id))
				{
					continue;
				}
				DynamicProperty[] propSet = objectContent.propSet;
				for (int j = 0; j < propSet.Length; j++)
				{
					if (propSet[j].name == "parent")
					{
						result = objectContent;
					}
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit ObjectContent");
		}
		return result;
	}

	public static long GetDatastoreInventoryPropertyInt(int index, string Property)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index  and property" + index + ", " + Property);
		}
		try
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Exit");
			}
			return Convert.ToInt64(GetDatastoreInventoryProperty(index, Property));
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Exit");
			}
			return 0L;
		}
	}

	public static string GetDatastoreInventoryProperty(int index, string Property)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index and property " + index + ", " + Property);
		}
		string text = null;
		try
		{
			DynamicProperty[] propSet = _DatastoreInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (!(propSet[i].name == Property) && (!(propSet[i].name == "vm") || !(Property == "vmTotal")))
				{
					continue;
				}
				if (Property == "vm")
				{
					int num = 0;
					ManagedObjectReference[] array = (ManagedObjectReference[])propSet[i].val;
					for (int j = 0; j < array.Length; j++)
					{
						if (isVirtualMachineAndRunning(array[j].Value))
						{
							num++;
						}
					}
					text = num.ToString();
				}
				else if (Property == "vmTotal")
				{
					int num2 = 0;
					ManagedObjectReference[] array = (ManagedObjectReference[])propSet[i].val;
					for (int j = 0; j < array.Length; j++)
					{
						if (isVirtualMachine(array[j].Value))
						{
							num2++;
						}
					}
					text = num2.ToString();
				}
				else
				{
					text = propSet[i].val.ToString();
				}
				break;
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + text);
		}
		return text;
	}

	public static long GetVMFSInfoInt(int index, string Property)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index and property " + index + ", " + Property);
		}
		try
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Exit");
			}
			return Convert.ToInt64(GetVMFSInfo(index, Property));
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Exit 0");
			}
			return 0L;
		}
	}

	public static StorageIORMInfo GetSIOCInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		StorageIORMInfo result = null;
		try
		{
			DynamicProperty[] propSet = _DatastoreInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "iormConfiguration")
				{
					result = (StorageIORMInfo)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit StorageIORMInfo");
		}
		return result;
	}

	public static ManagedObjectReference GetStoragePodMor(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		ManagedObjectReference result = null;
		try
		{
			DynamicProperty[] propSet = _DatastoreInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "parent")
				{
					result = (ManagedObjectReference)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit GetStoragePodMor");
		}
		return result;
	}

	public static ObjectContent[] GetStoragePodInfo(ManagedObjectReference mor)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		ObjectContent[] result = null;
		try
		{
			ManagedObjectReference managedObjectReference = new ManagedObjectReference();
			managedObjectReference.type = mor.type;
			managedObjectReference.Value = mor.Value;
			result = GetObjectProperties(_sic.propertyCollector, managedObjectReference, new string[1] { "summary" });
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit GetStoragePodInfo[]");
		}
		return result;
	}

	public static DVSConfigInfo GetdvSwitchInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		DVSConfigInfo result = null;
		try
		{
			DynamicProperty[] propSet = ((ObjectContent[])DistributedSwitchInventory[index])[0].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config")
				{
					result = (DVSConfigInfo)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit DVSConfigInfo");
		}
		return result;
	}

	public static string GetdvSwitchObjectID(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		string result = null;
		try
		{
			result = ((ObjectContent[])DistributedSwitchInventory[index])[0].obj.Value;
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit GetdvSwitchObjectID");
		}
		return result;
	}

	public static string GetdvSwitchName(string name)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for name " + name);
		}
		string text = null;
		try
		{
			int num = 0;
			foreach (ObjectContent[] item in DistributedSwitchInventory)
			{
				if (item[0].obj.Value == name)
				{
					text = GetdvSwitchSummary(num).name;
					break;
				}
				num++;
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + text);
		}
		return text;
	}

	public static VMwareDVSConfigInfo GetdvVMwareSwitchInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		VMwareDVSConfigInfo result = null;
		try
		{
			DynamicProperty[] propSet = ((ObjectContent[])DistributedSwitchInventory[index])[0].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config")
				{
					result = (VMwareDVSConfigInfo)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit VMwareDVSConfigInfo");
		}
		return result;
	}

	public static DVSSummary GetdvSwitchSummary(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		DVSSummary result = null;
		try
		{
			DynamicProperty[] propSet = ((ObjectContent[])DistributedSwitchInventory[index])[0].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "summary")
				{
					result = (DVSSummary)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit DVSSummary");
		}
		return result;
	}

	public static DVPortgroupConfigInfo GetdvPortInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		DVPortgroupConfigInfo result = null;
		try
		{
			DynamicProperty[] propSet = ((ObjectContent[])DistributedPortInventory[index])[0].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config")
				{
					result = (DVPortgroupConfigInfo)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit DVPortgroupConfigInfo");
		}
		return result;
	}

	public static string GetdvPortObjectID(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		string result = null;
		try
		{
			result = ((ObjectContent[])DistributedPortInventory[index])[0].obj.Value;
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit DVPortgroupConfigInfo");
		}
		return result;
	}

	public static NetworkSummary GetdvPortSummary(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		NetworkSummary result = null;
		try
		{
			DynamicProperty[] propSet = ((ObjectContent[])DistributedPortInventory[index])[0].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "summary")
				{
					result = (NetworkSummary)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit NetworkSummary");
		}
		return result;
	}

	public static void GetDatastoreProvisionedSpace(int index, ref long Uncommitted)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		try
		{
			DynamicProperty[] propSet = _DatastoreInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (!(propSet[i].name == "vm"))
				{
					continue;
				}
				ManagedObjectReference[] array = (ManagedObjectReference[])propSet[i].val;
				for (int j = 0; j < array.Length; j++)
				{
					VirtualMachineUsageOnDatastore[] virtualMachineUsageOnDatastore = GetVirtualMachineUsageOnDatastore(GetIndexforThisVM(array[j].Value));
					foreach (VirtualMachineUsageOnDatastore virtualMachineUsageOnDatastore2 in virtualMachineUsageOnDatastore)
					{
						if (virtualMachineUsageOnDatastore2.datastore.Value == _DatastoreInventory[index].obj.Value)
						{
							Uncommitted += virtualMachineUsageOnDatastore2.uncommitted;
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + Uncommitted);
		}
	}

	public static string GetDatastoreObjectID(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		string text = "";
		try
		{
			text = _DatastoreInventory[index].obj.Value;
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + text);
		}
		return text;
	}

	public static DatastoreInfo GetDataStoreInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		DatastoreInfo result = null;
		try
		{
			DynamicProperty[] propSet = _DatastoreInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "info")
				{
					result = (DatastoreInfo)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit GetDatastoreInfo");
		}
		return result;
	}

	public static void SearchDatastoreFriendlyNamesThread()
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Start SearchDatastoreFriendlyNamesThread");
			}
		}
		Hashtable hashtable = Hashtable.Synchronized(vSANFriendlyNames);
		ArrayList arrayList = ArrayList.Synchronized(_BrowserResultErrors);
		try
		{
			for (int i = 0; i < DatastoreInventoryCounter; i++)
			{
				try
				{
					if (GetDatastoreInventoryProperty(i, "summary.type") != "vsan")
					{
						continue;
					}
				}
				catch
				{
				}
				string datastoreInventoryProperty = GetDatastoreInventoryProperty(i, "summary.name");
				try
				{
					ManagedObjectReference vMFSBrowser = GetVMFSBrowser(i);
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " Start SearchDatastoreFriendlyNames_Task for " + datastoreInventoryProperty);
					}
					FileQueryFlags fileQueryFlags = new FileQueryFlags();
					fileQueryFlags.fileSize = true;
					HostDatastoreBrowserSearchSpec hostDatastoreBrowserSearchSpec = new HostDatastoreBrowserSearchSpec();
					hostDatastoreBrowserSearchSpec.details = fileQueryFlags;
					hostDatastoreBrowserSearchSpec.details.fileOwner = false;
					hostDatastoreBrowserSearchSpec.details.fileOwnerSpecified = true;
					hostDatastoreBrowserSearchSpec.matchPattern = new string[1] { "*" };
					HostDatastoreBrowserSearchResults hostDatastoreBrowserSearchResults = null;
					ManagedObjectReference managedObjectReference = _service.SearchDatastore_Task(vMFSBrowser, "[" + datastoreInventoryProperty + "]", hostDatastoreBrowserSearchSpec);
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " Started SearchDatastoreFriendlyNames_Task for " + datastoreInventoryProperty);
					}
					if (managedObjectReference != null)
					{
						if (logger.IsDebugEnabled)
						{
							logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " Wait for values SearchDatastoreFriendlyNames_Task for " + datastoreInventoryProperty);
						}
						object[] array = WaitForValues(managedObjectReference, new string[2] { "info.state", "info.error" }, new string[1] { "state" }, new object[1][] { new object[2]
						{
							TaskInfoState.success,
							TaskInfoState.error
						} });
						if (logger.IsDebugEnabled)
						{
							logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " End wait for values SearchDatastoreFriendlyNames_Task for " + datastoreInventoryProperty + " TaskInfoState = " + array[0].ToString());
						}
						if (array[0].Equals(TaskInfoState.success))
						{
							hostDatastoreBrowserSearchResults = (HostDatastoreBrowserSearchResults)((TaskInfo)GetObjectProperties(_sic.propertyCollector, managedObjectReference, new string[1] { "info" })[0].propSet[0].val).result;
						}
						if (logger.IsDebugEnabled)
						{
							logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " End wait for SearchDatastoreFriendlyNames_Task for " + datastoreInventoryProperty);
						}
					}
					if (hostDatastoreBrowserSearchResults == null && logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " SearchResult is null for " + datastoreInventoryProperty);
					}
					if (hostDatastoreBrowserSearchResults == null)
					{
						continue;
					}
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " Loop for all search results in datastore " + datastoreInventoryProperty);
					}
					_ = "[" + datastoreInventoryProperty + "]";
					Vim25Api.FileInfo[] file = hostDatastoreBrowserSearchResults.file;
					foreach (Vim25Api.FileInfo fileInfo in file)
					{
						if (fileInfo.GetType().Name == "FolderFileInfo" && fileInfo.friendlyName != null)
						{
							hashtable.Add(fileInfo.path, fileInfo.friendlyName);
						}
					}
				}
				catch (Exception ex)
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
					}
					arrayList.Add(datastoreInventoryProperty + ", " + ex);
				}
			}
		}
		catch (Exception ex2)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex2.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit SearchDatastoreFriendlyNames");
		}
	}

	public static void SearchDatastoreThreadForFileInfo(Thread SearchDatastoreThread)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Start SearchDatastoreThreadForFileInfo");
			}
		}
		while (true)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Wait for SearchDatastoreThread thread");
			}
			if (SearchDatastoreThread.ThreadState == ThreadState.Stopped)
			{
				break;
			}
			Thread.Sleep(1000);
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " SearchDatastoreThread thread is finished");
		}
		try
		{
			for (int i = 0; i < DatastoreInventoryCounter; i++)
			{
				string datastoreInventoryProperty = GetDatastoreInventoryProperty(i, "summary.name");
				try
				{
					ManagedObjectReference vMFSBrowser = GetVMFSBrowser(i);
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " Start searchDatastoreSubFolders_Task for " + datastoreInventoryProperty);
					}
					FileQueryFlags fileQueryFlags = new FileQueryFlags();
					fileQueryFlags.fileSize = true;
					HostDatastoreBrowserSearchSpec hostDatastoreBrowserSearchSpec = new HostDatastoreBrowserSearchSpec();
					hostDatastoreBrowserSearchSpec.details = fileQueryFlags;
					hostDatastoreBrowserSearchSpec.details.fileOwner = true;
					hostDatastoreBrowserSearchSpec.details.fileOwnerSpecified = true;
					hostDatastoreBrowserSearchSpec.details.fileSize = true;
					hostDatastoreBrowserSearchSpec.details.fileType = true;
					hostDatastoreBrowserSearchSpec.matchPattern = new string[1] { "*" };
					HostDatastoreBrowserSearchResults[] array = null;
					ManagedObjectReference managedObjectReference = _service.SearchDatastoreSubFolders_Task(vMFSBrowser, "[" + datastoreInventoryProperty + "]", hostDatastoreBrowserSearchSpec);
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " Started searchDatastoreSubFolders_Task for " + datastoreInventoryProperty);
					}
					if (managedObjectReference != null)
					{
						if (logger.IsDebugEnabled)
						{
							logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " Wait for values searchDatastoreSubFolders_Task for " + datastoreInventoryProperty);
						}
						object[] array2 = WaitForValues(managedObjectReference, new string[2] { "info.state", "info.error" }, new string[1] { "state" }, new object[1][] { new object[2]
						{
							TaskInfoState.success,
							TaskInfoState.error
						} });
						if (logger.IsDebugEnabled)
						{
							logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " End wait for values searchDatastoreSubFolders_Task for " + datastoreInventoryProperty + " TaskInfoState = " + array2[0].ToString());
						}
						if (array2[0].Equals(TaskInfoState.success))
						{
							array = (HostDatastoreBrowserSearchResults[])((TaskInfo)GetObjectProperties(_sic.propertyCollector, managedObjectReference, new string[1] { "info" })[0].propSet[0].val).result;
						}
						if (logger.IsDebugEnabled)
						{
							logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " End wait for searchDatastoreSubFolders_Task for " + datastoreInventoryProperty);
						}
					}
					if (array == null && logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " SearchResult is null for " + datastoreInventoryProperty);
					}
					if (array == null)
					{
						continue;
					}
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " Loop for all search fileInfo results in datastore " + datastoreInventoryProperty);
					}
					_ = "[" + datastoreInventoryProperty + "]";
					HostDatastoreBrowserSearchResults[] array3 = array;
					foreach (HostDatastoreBrowserSearchResults hostDatastoreBrowserSearchResults in array3)
					{
						string text = null;
						text = GetFriendlyPath(hostDatastoreBrowserSearchResults.folderPath);
						if (hostDatastoreBrowserSearchResults.file == null)
						{
							continue;
						}
						Vim25Api.FileInfo[] file = hostDatastoreBrowserSearchResults.file;
						foreach (Vim25Api.FileInfo fileInfo in file)
						{
							string text2 = fileInfo.GetType().ToString();
							text2 = text2.Replace("Vim25Api.", "");
							if (!(text2 == "FolderFileInfo"))
							{
								_VMFileObject vMFileObject = new _VMFileObject();
								vMFileObject.dataStoreName = datastoreInventoryProperty;
								vMFileObject.pathName = hostDatastoreBrowserSearchResults.folderPath;
								vMFileObject.friendlyPathName = text;
								vMFileObject.fileName = fileInfo.path;
								vMFileObject.fileType = text2;
								vMFileObject.fileSize = fileInfo.fileSize;
								_BrowserAllFiles.Add(vMFileObject);
							}
						}
					}
				}
				catch (Exception ex)
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
					}
				}
			}
		}
		catch (Exception ex2)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex2.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit SearchDatastoreThreadForFileInfo");
		}
	}

	public static void SearchDatastoreThread(Thread SearchDatastoreFriendlyNames)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Start SearchDatastoreThread");
			}
		}
		while (true)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Wait for SearchDatastoreFriendlyNames thread");
			}
			if (SearchDatastoreFriendlyNames.ThreadState == ThreadState.Stopped)
			{
				break;
			}
			Thread.Sleep(1000);
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " SearchDatastoreFriendlyNames thread is finished");
		}
		try
		{
			for (int i = 0; i < DatastoreInventoryCounter; i++)
			{
				string datastoreInventoryProperty = GetDatastoreInventoryProperty(i, "summary.name");
				try
				{
					ManagedObjectReference vMFSBrowser = GetVMFSBrowser(i);
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " Start searchDatastoreSubFolders_Task for " + datastoreInventoryProperty);
					}
					FileQueryFlags fileQueryFlags = new FileQueryFlags();
					fileQueryFlags.fileSize = true;
					HostDatastoreBrowserSearchSpec hostDatastoreBrowserSearchSpec = new HostDatastoreBrowserSearchSpec();
					hostDatastoreBrowserSearchSpec.details = fileQueryFlags;
					hostDatastoreBrowserSearchSpec.details.fileOwner = true;
					hostDatastoreBrowserSearchSpec.details.fileOwnerSpecified = true;
					hostDatastoreBrowserSearchSpec.details.fileSize = false;
					hostDatastoreBrowserSearchSpec.matchPattern = new string[3] { "*.vmx", "*.vmtx", "*.vmdk" };
					HostDatastoreBrowserSearchResults[] array = null;
					ManagedObjectReference managedObjectReference = _service.SearchDatastoreSubFolders_Task(vMFSBrowser, "[" + datastoreInventoryProperty + "]", hostDatastoreBrowserSearchSpec);
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " Started searchDatastoreSubFolders_Task for " + datastoreInventoryProperty);
					}
					if (managedObjectReference != null)
					{
						if (logger.IsDebugEnabled)
						{
							logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " Wait for values searchDatastoreSubFolders_Task for " + datastoreInventoryProperty);
						}
						object[] array2 = WaitForValues(managedObjectReference, new string[2] { "info.state", "info.error" }, new string[1] { "state" }, new object[1][] { new object[2]
						{
							TaskInfoState.success,
							TaskInfoState.error
						} });
						if (logger.IsDebugEnabled)
						{
							logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " End wait for values searchDatastoreSubFolders_Task for " + datastoreInventoryProperty + " TaskInfoState = " + array2[0].ToString());
						}
						if (array2[0].Equals(TaskInfoState.success))
						{
							array = (HostDatastoreBrowserSearchResults[])((TaskInfo)GetObjectProperties(_sic.propertyCollector, managedObjectReference, new string[1] { "info" })[0].propSet[0].val).result;
						}
						if (logger.IsDebugEnabled)
						{
							logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " End wait for searchDatastoreSubFolders_Task for " + datastoreInventoryProperty);
						}
					}
					if (array == null && logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " SearchResult is null for " + datastoreInventoryProperty);
					}
					if (array == null)
					{
						continue;
					}
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " Loop for all search results in datastore " + datastoreInventoryProperty);
					}
					_ = "[" + datastoreInventoryProperty + "]";
					HostDatastoreBrowserSearchResults[] array3 = array;
					foreach (HostDatastoreBrowserSearchResults hostDatastoreBrowserSearchResults in array3)
					{
						if (hostDatastoreBrowserSearchResults.file == null)
						{
							continue;
						}
						Vim25Api.FileInfo[] file = hostDatastoreBrowserSearchResults.file;
						foreach (Vim25Api.FileInfo fileInfo in file)
						{
							string folderPath = hostDatastoreBrowserSearchResults.folderPath;
							folderPath = ((!folderPath.EndsWith("/")) ? (hostDatastoreBrowserSearchResults.folderPath + "/" + fileInfo.path) : (hostDatastoreBrowserSearchResults.folderPath + fileInfo.path));
							if (logger.IsDebugEnabled)
							{
								logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " searchResult = " + folderPath);
							}
							folderPath = folderPath.Replace("]/", "] ");
							if (logger.IsDebugEnabled)
							{
								logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " vmpath = " + folderPath);
							}
							if (fileInfo.path.StartsWith("cos."))
							{
								if (logger.IsDebugEnabled)
								{
									logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " vmpath = " + folderPath + " --> skipped");
								}
								continue;
							}
							if (fileInfo.path.Contains("esxconsole"))
							{
								if (logger.IsDebugEnabled)
								{
									logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " vmpath = " + folderPath + " --> skipped");
								}
								continue;
							}
							if (hostDatastoreBrowserSearchResults.folderPath.Contains(".snapshot"))
							{
								if (logger.IsDebugEnabled)
								{
									logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " vmpath = " + folderPath + " --> skipped");
								}
								continue;
							}
							if (hostDatastoreBrowserSearchResults.folderPath.Contains("VeeamBackup"))
							{
								if (logger.IsDebugEnabled)
								{
									logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " vmpath = " + folderPath + " --> skipped");
								}
								continue;
							}
							if (hostDatastoreBrowserSearchResults.folderPath.Contains("_replica"))
							{
								if (logger.IsDebugEnabled)
								{
									logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " vmpath = " + folderPath + " --> skipped");
								}
								continue;
							}
							if (hostDatastoreBrowserSearchResults.folderPath.Contains("hbrdisk.RDID"))
							{
								if (logger.IsDebugEnabled)
								{
									logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " vmpath = " + folderPath + " --> skipped");
								}
								continue;
							}
							if (folderPath.EndsWith("-flat.vmdk"))
							{
								if (logger.IsDebugEnabled)
								{
									logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " vmpath = " + folderPath + " --> skipped");
								}
								continue;
							}
							if (folderPath.EndsWith(".vmdk") && folderPath.Contains("-flat"))
							{
								if (logger.IsDebugEnabled)
								{
									logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " vmpath = " + folderPath + " --> skipped");
								}
								continue;
							}
							if (folderPath.EndsWith("-delta.vmdk"))
							{
								if (logger.IsDebugEnabled)
								{
									logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " vmpath = " + folderPath + " --> skipped");
								}
								continue;
							}
							if (folderPath.EndsWith("-sesparse.vmdk"))
							{
								if (logger.IsDebugEnabled)
								{
									logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " vmpath = " + folderPath + " --> skipped");
								}
								continue;
							}
							if (folderPath.EndsWith("-digest.vmdk"))
							{
								if (logger.IsDebugEnabled)
								{
									logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " vmpath = " + folderPath + " --> skipped");
								}
								continue;
							}
							if (folderPath.StartsWith("contentlib-") || folderPath.Contains("] contentlib-"))
							{
								if (logger.IsDebugEnabled)
								{
									logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " vmpath = " + folderPath + " --> skipped");
								}
								continue;
							}
							if (folderPath.EndsWith("-ctk.vmdk"))
							{
								if (logger.IsDebugEnabled)
								{
									logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " vmpath = " + folderPath + " --> skipped");
								}
								continue;
							}
							if (folderPath.EndsWith("-rdmp.vmdk"))
							{
								folderPath = folderPath.Replace("-rdmp.vmdk", ".vmdk");
							}
							if ((folderPath.Contains("/vCLS (") && folderPath.EndsWith(").vmdk")) || (folderPath.Contains("/vCLS-") && folderPath.EndsWith(".vmdk")))
							{
								if (logger.IsDebugEnabled)
								{
									logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " vmpath = " + folderPath + " --> skipped");
								}
								continue;
							}
							if ((folderPath.Contains("/vCLS (") && folderPath.EndsWith(").vmx")) || (folderPath.Contains("/vCLS-") && folderPath.EndsWith(".vmx")))
							{
								if (logger.IsDebugEnabled)
								{
									logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " vmpath = " + folderPath + " --> skipped");
								}
								continue;
							}
							_BrowserResult.Add(folderPath);
							if (logger.IsDebugEnabled)
							{
								logger.Debug(methodBase.Name + " " + managedObjectReference.Value + " vmpath = " + folderPath + " --> saved");
							}
						}
					}
				}
				catch (Exception ex)
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
					}
					_BrowserResultErrors.Add(datastoreInventoryProperty + ", " + ex);
				}
			}
		}
		catch (Exception ex2)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex2.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit SearchDatastoreThread");
		}
	}

	public static ManagedObjectReference GetVMFSBrowser(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		ManagedObjectReference result = null;
		try
		{
			DynamicProperty[] propSet = _DatastoreInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "browser")
				{
					result = (ManagedObjectReference)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit mor");
		}
		return result;
	}

	public static string GetVMFSInfo(int index, string Property)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for property " + Property);
		}
		string text = null;
		try
		{
			DynamicProperty[] propSet = _DatastoreInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "host")
				{
					DatastoreHostMount[] array = (DatastoreHostMount[])propSet[i].val;
					if (Property == "NumberOfHosts")
					{
						text = array.Length.ToString();
						break;
					}
					if (Property == "hosts")
					{
						text = null;
						DatastoreHostMount[] array2 = array;
						foreach (DatastoreHostMount datastoreHostMount in array2)
						{
							if (text != null)
							{
								text += ", ";
							}
							text += GetHostName(datastoreHostMount.key.Value);
						}
					}
				}
				if (!(propSet[i].name == "info"))
				{
					continue;
				}
				try
				{
					if (propSet[i].val is VmfsDatastoreInfo)
					{
						VmfsDatastoreInfo vmfsDatastoreInfo = (VmfsDatastoreInfo)propSet[i].val;
						switch (Property)
						{
						case "blockSizeMb":
							text = vmfsDatastoreInfo.vmfs.blockSizeMb.ToString();
							goto end_IL_027c;
						case "maxBlocks":
							text = vmfsDatastoreInfo.vmfs.maxBlocks.ToString();
							goto end_IL_027c;
						case "majorVersion":
							text = vmfsDatastoreInfo.vmfs.majorVersion.ToString();
							goto end_IL_027c;
						case "version":
							text = vmfsDatastoreInfo.vmfs.version.ToString();
							goto end_IL_027c;
						case "extents":
							text = vmfsDatastoreInfo.vmfs.extent.Length.ToString();
							goto end_IL_027c;
						case "lunid":
							text = vmfsDatastoreInfo.vmfs.extent[0].diskName;
							goto end_IL_027c;
						case "vmfsUpgradable":
							text = vmfsDatastoreInfo.vmfs.vmfsUpgradable.ToString();
							goto end_IL_027c;
						}
					}
					if (propSet[i].val is NasDatastoreInfo)
					{
						_ = (NasDatastoreInfo)propSet[i].val;
					}
				}
				catch (Exception ex)
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
					}
				}
				continue;
				end_IL_027c:
				break;
			}
		}
		catch (Exception ex2)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex2.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + text);
		}
		return text;
	}

	public static HostDateTimeInfo GetHostDateTimeInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		HostDateTimeInfo result = null;
		try
		{
			DynamicProperty[] propSet = HostInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config.dateTimeInfo")
				{
					result = (HostDateTimeInfo)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit HostdateTimeInfo");
		}
		return result;
	}

	public static HostBIOSInfo GetHostBiosInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		HostBIOSInfo result = null;
		try
		{
			DynamicProperty[] propSet = HostInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "hardware.biosInfo")
				{
					result = (HostBIOSInfo)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit HostBIOSInfo");
		}
		return result;
	}

	public static HostCpuPowerManagementInfo GetHostCpuPowerManagementInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		HostCpuPowerManagementInfo result = null;
		try
		{
			DynamicProperty[] propSet = HostInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "hardware.cpuPowerManagementInfo")
				{
					result = (HostCpuPowerManagementInfo)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit HostCpuPowerManagementInfo");
		}
		return result;
	}

	public static PowerSystemInfo GetPowerSystemInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		PowerSystemInfo result = null;
		try
		{
			DynamicProperty[] propSet = HostInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config.powerSystemInfo")
				{
					result = (PowerSystemInfo)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit powerSystemInfo");
		}
		return result;
	}

	public static HostSystemInfo GetHostSystemInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		HostSystemInfo result = null;
		try
		{
			DynamicProperty[] propSet = HostInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "hardware.systemInfo")
				{
					result = (HostSystemInfo)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit HostSystemInfo");
		}
		return result;
	}

	public static HostDnsConfig GetHostDnsConfig(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		HostDnsConfig result = null;
		try
		{
			DynamicProperty[] propSet = HostInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config.network.dnsConfig")
				{
					result = (HostDnsConfig)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit HostDnsConfig");
		}
		return result;
	}

	public static HostHyperThreadScheduleInfo GetHostHyperThreadScheduleInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		HostHyperThreadScheduleInfo result = null;
		try
		{
			DynamicProperty[] propSet = HostInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config.hyperThread")
				{
					result = (HostHyperThreadScheduleInfo)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit HostHyperThreadScheduleInfo");
		}
		return result;
	}

	public static HostCapability GetHostCapability(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		HostCapability result = null;
		try
		{
			DynamicProperty[] propSet = HostInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "capability")
				{
					result = (HostCapability)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit HostCapability");
		}
		return result;
	}

	public static HostSystemIdentificationInfo[] GetHostSystemIdentificationInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		HostSystemIdentificationInfo[] result = null;
		try
		{
			DynamicProperty[] propSet = HostInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "summary.hardware.otherIdentifyingInfo")
				{
					result = (HostSystemIdentificationInfo[])propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit HostSystemIdentificationInfo[]");
		}
		return result;
	}

	public static HostConfigManager GetHostConfigManager(ObjectContent host)
	{
		return (HostConfigManager)(host?.propSet?.FirstOrDefault((DynamicProperty e) => e.name == "configManager")?.val);
	}

	public static HostConfigManager GetHostConfigManager(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		HostConfigManager result = null;
		try
		{
			result = GetHostConfigManager(HostInventory[index]);
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit HostConfigManager");
		}
		return result;
	}

	public static Event[] GetClusterConfigEvents(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		Event[] result = null;
		try
		{
			DynamicProperty[] propSet = _ClusterInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "configIssue")
				{
					result = (Event[])propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit Event[]");
		}
		return result;
	}

	public static Event[] GetHostConfigEvents(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		Event[] result = null;
		try
		{
			DynamicProperty[] propSet = HostInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "configIssue")
				{
					result = (Event[])propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit Event[]");
		}
		return result;
	}

	public static Event[] GetVMConfigEvents(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		Event[] result = null;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "configIssue")
				{
					result = (Event[])propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit Event[]");
		}
		return result;
	}

	public static Event[] GetDatastoreConfigEvents(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		Event[] result = null;
		try
		{
			DynamicProperty[] propSet = _DatastoreInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "configIssue")
				{
					result = (Event[])propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit Event[]");
		}
		return result;
	}

	public static HostService[] GetHostServices(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		HostService[] result = null;
		try
		{
			DynamicProperty[] propSet = HostInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config.service")
				{
					result = ((HostServiceInfo)propSet[i].val).service;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit HostSystemIdentificationInfo[]");
		}
		return result;
	}

	public static long GetHostInventoryPropertyInt(int index, string Property)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index and property " + index + ", " + Property);
		}
		try
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Exit ");
			}
			return Convert.ToInt64(GetHostInventoryProperty(index, Property));
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Exit 0");
			}
			return 0L;
		}
	}

	public static string GetResourcePoolObjectID(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		string text = null;
		try
		{
			text = ((ObjectContent)_ResourcePoolInventory[index]).obj.Value;
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + text);
		}
		return text;
	}

	public static string GetClusterObjectID(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		string text = null;
		try
		{
			text = _ClusterInventory[index].obj.Value;
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + text);
		}
		return text;
	}

	public static string GetResourcePoolInventoryProperty(int index, string Property)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index and property " + index + ", " + Property);
		}
		string text = null;
		try
		{
			DynamicProperty[] propSet = ((ObjectContent)_ResourcePoolInventory[index]).propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == Property)
				{
					text = propSet[i].val.ToString();
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + text);
		}
		return text;
	}

	public static string GetResourcePoolPath(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		string text = null;
		try
		{
			int num = _ResourcePool[index].IndexOf("=");
			text = _ResourcePool[index].Remove(0, num + 1);
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + text);
		}
		return text;
	}

	public static ResourcePoolSummary GetResourcePoolSummary(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		ResourcePoolSummary result = null;
		try
		{
			DynamicProperty[] propSet = ((ObjectContent)_ResourcePoolInventory[index]).propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "summary")
				{
					result = (ResourcePoolSummary)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit ResourcePoolSummary");
		}
		return result;
	}

	public static int GetResourcePoolNumberOfVMs(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		int result = 0;
		try
		{
			DynamicProperty[] propSet = ((ObjectContent)_ResourcePoolInventory[index]).propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (!(propSet[i].name == "vm"))
				{
					continue;
				}
				int num = 0;
				ManagedObjectReference[] array = (ManagedObjectReference[])propSet[i].val;
				for (int j = 0; j < array.Length; j++)
				{
					if (isVirtualMachineAndRunning(array[j].Value))
					{
						num++;
					}
				}
				result = num;
				break;
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + result);
		}
		return result;
	}

	public static int GetResourcePoolNumberOfVMsTotal(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		int result = 0;
		try
		{
			DynamicProperty[] propSet = ((ObjectContent)_ResourcePoolInventory[index]).propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (!(propSet[i].name == "vm"))
				{
					continue;
				}
				int num = 0;
				ManagedObjectReference[] array = (ManagedObjectReference[])propSet[i].val;
				for (int j = 0; j < array.Length; j++)
				{
					if (isVirtualMachine(array[j].Value))
					{
						num++;
					}
				}
				result = num;
				break;
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + result);
		}
		return result;
	}

	public static int GetResourcePoolNumberOfvCPU(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		int result = 0;
		try
		{
			DynamicProperty[] propSet = ((ObjectContent)_ResourcePoolInventory[index]).propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "vm")
				{
					int num = 0;
					ManagedObjectReference[] array = (ManagedObjectReference[])propSet[i].val;
					foreach (ManagedObjectReference managedObjectReference in array)
					{
						num += GetNumberofVCPUs(managedObjectReference.Value);
					}
					result = num;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + result);
		}
		return result;
	}

	public static string GetClusterInventoryProperty(int index, string Property)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index and property " + index + ", " + Property);
		}
		string text = null;
		try
		{
			DynamicProperty[] propSet = _ClusterInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == Property)
				{
					text = propSet[i].val.ToString();
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + text);
		}
		return text;
	}

	public static ComputeResourceConfigInfo GetClusterConfigurationEx(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		ComputeResourceConfigInfo result = null;
		try
		{
			DynamicProperty[] propSet = _ClusterInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "configurationEx")
				{
					result = (ComputeResourceConfigInfo)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit ComputeResourceConfigInfo");
		}
		return result;
	}

	public static ClusterComputeResourceSummary GetClusterSummary(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		ClusterComputeResourceSummary result = null;
		try
		{
			DynamicProperty[] propSet = _ClusterInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "summary")
				{
					result = (ClusterComputeResourceSummary)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit ClusterComputeResourceSummary");
		}
		return result;
	}

	public static ClusterConfigInfo GetClusterConfig(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		ClusterConfigInfo result = null;
		try
		{
			DynamicProperty[] propSet = _ClusterInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "configuration")
				{
					result = (ClusterConfigInfo)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit ClusterConfigInfoEx");
		}
		return result;
	}

	public static ClusterConfigInfoEx GetClusterConfigEx(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		ClusterConfigInfoEx result = null;
		try
		{
			DynamicProperty[] propSet = _ClusterInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "configurationEx")
				{
					result = (ClusterConfigInfoEx)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit ClusterConfigInfoEx");
		}
		return result;
	}

	public static ClusterRuleInfo[] GetClusterRuleInfoFix(string ClusterName, string VMName)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for Clustername and VM name " + ClusterName + ", " + VMName);
		}
		ClusterRuleInfo[] result = null;
		try
		{
			for (int i = 0; i < _ClusterInventory.Length; i++)
			{
				DynamicProperty[] propSet = _ClusterInventory[i].propSet;
				for (int j = 0; j < propSet.Length; j++)
				{
					if (!(propSet[j].name == "name") || !(propSet[j].val.ToString() == ClusterName))
					{
						continue;
					}
					for (int k = 0; k < propSet.Length; k++)
					{
						if (propSet[k].name == "configuration")
						{
							ClusterRuleInfo[] rule = ((ClusterConfigInfo)propSet[k].val).rule;
							if (logger.IsDebugEnabled)
							{
								logger.Debug(methodBase.Name + " Exit ClusterRuleInfo[]");
							}
							return rule;
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
		return result;
	}

	public static ClusterRuleInfo[] GetClusterRuleInfo(string ClusterName, string VMName)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for Clustername and VM name " + ClusterName + ", " + VMName);
		}
		ClusterRuleInfo[] result = null;
		try
		{
			for (int i = 0; i < _ClusterInventory.Length; i++)
			{
				DynamicProperty[] propSet = _ClusterInventory[i].propSet;
				for (int j = 0; j < propSet.Length; j++)
				{
					if (!(propSet[j].name == "name") || !(propSet[j].val.ToString() == ClusterName))
					{
						continue;
					}
					for (int k = 0; k < propSet.Length; k++)
					{
						if (propSet[k].name == "configurationEx")
						{
							ClusterRuleInfo[] rule = ((ClusterConfigInfoEx)propSet[k].val).rule;
							if (logger.IsDebugEnabled)
							{
								logger.Debug(methodBase.Name + " Exit ClusterRuleInfo[]");
							}
							return rule;
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
		return result;
	}

	public static ClusterDasVmSettings GetClusterDasDefaultSettings(string ClusterName, string VMName)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for Clustername and VM name " + ClusterName + ", " + VMName);
		}
		ClusterDasVmSettings result = null;
		try
		{
			for (int i = 0; i < _ClusterInventory.Length; i++)
			{
				DynamicProperty[] propSet = _ClusterInventory[i].propSet;
				for (int j = 0; j < propSet.Length; j++)
				{
					if (!(propSet[j].name == "name") || !(propSet[j].val.ToString() == ClusterName))
					{
						continue;
					}
					for (int k = 0; k < propSet.Length; k++)
					{
						if (propSet[k].name == "configurationEx")
						{
							ClusterConfigInfoEx clusterConfigInfoEx = (ClusterConfigInfoEx)propSet[k].val;
							if (logger.IsDebugEnabled)
							{
								logger.Debug(methodBase.Name + " Exit ConfigEx.dasConfig.defaultVmSettings");
							}
							return clusterConfigInfoEx.dasConfig.defaultVmSettings;
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
		return result;
	}

	public static ClusterDasVmSettings GetClusterDasVmSettings(string ClusterName, string VMName)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for Clustername and VM name " + ClusterName + ", " + VMName);
		}
		ClusterDasVmSettings result = null;
		try
		{
			for (int i = 0; i < _ClusterInventory.Length; i++)
			{
				DynamicProperty[] propSet = _ClusterInventory[i].propSet;
				for (int j = 0; j < propSet.Length; j++)
				{
					if (!(propSet[j].name == "name") || !(propSet[j].val.ToString() == ClusterName))
					{
						continue;
					}
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " Clustername: " + ClusterName);
					}
					for (int k = 0; k < propSet.Length; k++)
					{
						if (!(propSet[k].name == "configurationEx"))
						{
							continue;
						}
						if (logger.IsDebugEnabled)
						{
							logger.Debug(methodBase.Name + " Get ClusterConfigInfoEx");
						}
						ClusterConfigInfoEx clusterConfigInfoEx = (ClusterConfigInfoEx)propSet[k].val;
						ClusterDasVmConfigInfo[] array = null;
						try
						{
							if (logger.IsDebugEnabled)
							{
								logger.Debug(methodBase.Name + " Get ClusterDasVmConfigInfo");
							}
							array = clusterConfigInfoEx.dasVmConfig;
							_ = array.LongLength;
						}
						catch
						{
							if (logger.IsDebugEnabled)
							{
								logger.Debug(methodBase.Name + " NO ClusterDasVmConfigInfo");
							}
							if (logger.IsDebugEnabled)
							{
								logger.Debug(methodBase.Name + " Exit null");
							}
							return null;
						}
						if (logger.IsDebugEnabled)
						{
							logger.Debug(methodBase.Name + " OK ClusterDasVmConfigInfo");
						}
						for (int l = 0; l < array.Length; l++)
						{
							if (array[l].key.Value.ToString() == VMName)
							{
								if (logger.IsDebugEnabled)
								{
									logger.Debug(methodBase.Name + " Exit VmConfigInfo[z].dasSettings");
								}
								return array[l].dasSettings;
							}
						}
						if (logger.IsDebugEnabled)
						{
							logger.Debug(methodBase.Name + " Exit ConfigEx.dasConfig.defaultVmSettings");
						}
						return null;
					}
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
		return result;
	}

	public static string GetHostInventoryProperty(int index, string property)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index and property " + index + ", " + property);
		}
		string text = null;
		try
		{
			DynamicProperty[] propSet = HostInventory[index].propSet;
			DynamicProperty[] array = propSet;
			foreach (DynamicProperty dynamicProperty in array)
			{
				if (dynamicProperty.name != property && property != "vmTotal" && property != "vCPUs" && property != "vRAM" && property != "VMUsedMemory" && property != "VMSwappedMemory" && property != "VMBalloonedMemory")
				{
					continue;
				}
				switch (property)
				{
				case "summary.config.product":
					text = ((AboutInfo)dynamicProperty.val).fullName;
					break;
				case "parent":
					text = ((ManagedObjectReference)dynamicProperty.val).Value;
					break;
				case "vm":
					text = ((ManagedObjectReference[])dynamicProperty.val).Count((ManagedObjectReference mor) => isVirtualMachineAndRunning(mor.Value)).ToString();
					break;
				case "vmTotal":
				{
					int num3 = 0;
					for (num3 = 0; num3 < propSet.Length; num3++)
					{
						if (dynamicProperty.name == "vm")
						{
							break;
						}
					}
					text = ((ManagedObjectReference[])propSet[num3 - 1].val).Count((ManagedObjectReference mor) => isVirtualMachine(mor.Value)).ToString();
					break;
				}
				case "vCPUs":
				{
					int num2 = 0;
					for (num2 = 0; num2 < propSet.Length; num2++)
					{
						if (dynamicProperty.name == "vm")
						{
							break;
						}
					}
					text = ((ManagedObjectReference[])propSet[num2 - 1].val).Aggregate(0, (int current, ManagedObjectReference mor) => current + GetNumberofVCPUs(mor.Value)).ToString();
					break;
				}
				case "vRAM":
				{
					int num5 = 0;
					for (num5 = 0; num5 < propSet.Length; num5++)
					{
						if (dynamicProperty.name == "vm")
						{
							break;
						}
					}
					text = ((ManagedObjectReference[])propSet[num5 - 1].val).Aggregate(0, (int current, ManagedObjectReference mor) => current + GetTotalvRAM(mor.Value)).ToString();
					break;
				}
				case "VMUsedMemory":
				{
					int num6 = 0;
					for (num6 = 0; num6 < propSet.Length; num6++)
					{
						if (dynamicProperty.name == "vm")
						{
							break;
						}
					}
					text = ((ManagedObjectReference[])propSet[num6 - 1].val).Aggregate(0, (int current, ManagedObjectReference mor) => current + GetVMUsedMemory(mor.Value)).ToString();
					break;
				}
				case "VMSwappedMemory":
				{
					int num = 0;
					for (num = 0; num < propSet.Length; num++)
					{
						if (dynamicProperty.name == "vm")
						{
							break;
						}
					}
					text = ((ManagedObjectReference[])propSet[num - 1].val).Aggregate(0, (int current, ManagedObjectReference mor) => current + GetVMSwappedMemory(mor.Value)).ToString();
					break;
				}
				case "VMBalloonedMemory":
				{
					int num4 = 0;
					for (num4 = 0; num4 < propSet.Length; num4++)
					{
						if (dynamicProperty.name == "vm")
						{
							break;
						}
					}
					text = ((ManagedObjectReference[])propSet[num4 - 1].val).Aggregate(0, (int current, ManagedObjectReference mor) => current + GetVMBalloonedMemory(mor.Value)).ToString();
					break;
				}
				default:
					text = ((dynamicProperty.val is IEnumerable && !(dynamicProperty.val is string)) ? string.Join(", ", (dynamicProperty.val as IEnumerable<object>).Select((object v) => v.ToString())) : dynamicProperty.val.ToString());
					break;
				}
				break;
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + text);
		}
		return text;
	}

	public static int GetIndexforThisVM(string vm)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for vm " + vm);
		}
		int result = 0;
		try
		{
			for (int i = 0; i < _Inventory.Length; i++)
			{
				if (_Inventory[i].obj.Value == vm)
				{
					result = i;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + result);
		}
		return result;
	}

	public static bool isVirtualMachineAndRunning(string vm)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for vm " + vm);
		}
		try
		{
			for (int i = 0; i < _Inventory.Length; i++)
			{
				if (_Inventory[i].obj.Value != vm)
				{
					continue;
				}
				if (GetVmInventoryProperty(i, "summary.config.template") == "True")
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " Exit false");
					}
					return false;
				}
				if (!(GetVmInventoryProperty(i, "summary.runtime.powerState") == "poweredOn"))
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " Exit false");
					}
					return false;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit true");
		}
		return true;
	}

	public static bool isVirtualMachine(string vm)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for vm " + vm);
		}
		try
		{
			for (int i = 0; i < _Inventory.Length; i++)
			{
				if (_Inventory[i].obj.Value == vm && GetVmInventoryProperty(i, "summary.config.template") == "True")
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " Exit false");
					}
					return false;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit true");
		}
		return true;
	}

	public static int GetNumberofVCPUs(string vm)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for vm " + vm);
		}
		try
		{
			for (int i = 0; i < _Inventory.Length; i++)
			{
				if (!(_Inventory[i].obj.Value == vm))
				{
					continue;
				}
				if (GetVmInventoryProperty(i, "summary.config.template") == "True")
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " Exit 0");
					}
					return 0;
				}
				if (GetVmInventoryProperty(i, "summary.runtime.powerState") != "poweredOn")
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " Exit 0");
					}
					return 0;
				}
				int result = (int)GetInventoryPropertyInt(i, "summary.config.numCpu");
				if (logger.IsDebugEnabled)
				{
					logger.Debug(methodBase.Name + " Exit " + result);
				}
				return result;
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit 0");
		}
		return 0;
	}

	public static int GetTotalvRAM(string vm)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for vm " + vm);
		}
		try
		{
			for (int i = 0; i < _Inventory.Length; i++)
			{
				if (!(_Inventory[i].obj.Value == vm))
				{
					continue;
				}
				if (GetVmInventoryProperty(i, "summary.config.template") == "True")
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " Exit 0");
					}
					return 0;
				}
				if (GetVmInventoryProperty(i, "summary.runtime.powerState") != "poweredOn")
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " Exit 0");
					}
					return 0;
				}
				int result = (int)GetInventoryPropertyInt(i, "summary.config.memorySizeMB");
				if (logger.IsDebugEnabled)
				{
					logger.Debug(methodBase.Name + " Exit " + result);
				}
				return result;
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit 0");
		}
		return 0;
	}

	public static int GetVMUsedMemory(string vm)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for vm " + vm);
		}
		try
		{
			for (int i = 0; i < _Inventory.Length; i++)
			{
				if (!(_Inventory[i].obj.Value == vm))
				{
					continue;
				}
				if (GetVmInventoryProperty(i, "summary.config.template") == "True")
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " Exit 0");
					}
					return 0;
				}
				if (GetVmInventoryProperty(i, "summary.runtime.powerState") != "poweredOn")
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " Exit 0");
					}
					return 0;
				}
				int result = (int)GetInventoryPropertyInt(i, "summary.quickStats.guestMemoryUsage");
				if (logger.IsDebugEnabled)
				{
					logger.Debug(methodBase.Name + " Exit " + result);
				}
				return result;
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit 0");
		}
		return 0;
	}

	public static int GetVMSwappedMemory(string vm)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for vm " + vm);
		}
		try
		{
			for (int i = 0; i < _Inventory.Length; i++)
			{
				if (!(_Inventory[i].obj.Value == vm))
				{
					continue;
				}
				if (GetVmInventoryProperty(i, "summary.config.template") == "True")
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " Exit 0");
					}
					return 0;
				}
				if (GetVmInventoryProperty(i, "summary.runtime.powerState") != "poweredOn")
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " Exit 0");
					}
					return 0;
				}
				int result = (int)GetInventoryPropertyInt(i, "summary.quickStats.swappedMemory");
				if (logger.IsDebugEnabled)
				{
					logger.Debug(methodBase.Name + " Exit " + result);
				}
				return result;
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit 0");
		}
		return 0;
	}

	public static int GetVMBalloonedMemory(string vm)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for vm " + vm);
		}
		try
		{
			for (int i = 0; i < _Inventory.Length; i++)
			{
				if (!(_Inventory[i].obj.Value == vm))
				{
					continue;
				}
				if (GetVmInventoryProperty(i, "summary.config.template") == "True")
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " Exit 0");
					}
					return 0;
				}
				if (GetVmInventoryProperty(i, "summary.runtime.powerState") != "poweredOn")
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " Exit 0");
					}
					return 0;
				}
				int result = (int)GetInventoryPropertyInt(i, "summary.quickStats.balloonedMemory");
				if (logger.IsDebugEnabled)
				{
					logger.Debug(methodBase.Name + " Exit " + result);
				}
				return result;
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit 0");
		}
		return 0;
	}

	public static GuestNicInfo[] GetGuestNicInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		GuestNicInfo[] result = null;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "guest.net")
				{
					result = (GuestNicInfo[])propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit GuestNicInfo[]");
		}
		return result;
	}

	public static ManagedObjectReference GetLicenseAssignmentManager()
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		ManagedObjectReference result = null;
		try
		{
			DynamicProperty[] propSet = _LicenseInventory[0].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "licenseAssignmentManager")
				{
					result = (ManagedObjectReference)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit LicenseManagerLicenseInfo[]");
		}
		return result;
	}

	public static LicenseManagerLicenseInfo[] GetLicenseInfo()
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		LicenseManagerLicenseInfo[] result = null;
		try
		{
			DynamicProperty[] propSet = _LicenseInventory[0].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "licenses")
				{
					result = (LicenseManagerLicenseInfo[])propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit LicenseManagerLicenseInfo[]");
		}
		return result;
	}

	public static VirtualMachineFileLayoutExFileInfo[] GetVirtualMachineFileLayoutExFileInfo(int index)
	{
		MethodBase methodBase = null;
		VirtualMachineFileLayoutExFileInfo[] result = null;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "layoutEx.file")
				{
					result = (VirtualMachineFileLayoutExFileInfo[])propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		return result;
	}

	public static GuestDiskInfo[] GetGuestDiskInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		GuestDiskInfo[] result = null;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "guest.disk")
				{
					result = (GuestDiskInfo[])propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit GuestDiskInfo[]");
		}
		return result;
	}

	public static string GetvAppInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		string text = null;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (!(propSet[i].name == "parentVApp"))
				{
					continue;
				}
				ObjectContent[] vMParent = GetVMParent((ManagedObjectReference)propSet[i].val);
				for (int j = 0; j < vMParent.Length; j++)
				{
					DynamicProperty[] propSet2 = vMParent[j].propSet;
					for (int k = 0; k < propSet2.Length; k++)
					{
						if (propSet2[k].name == "name")
						{
							text = propSet2[k].val.ToString();
						}
					}
				}
				break;
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + text);
		}
		return text;
	}

	public static bool DisabledMethod(int index, string Method)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index and method " + index + ", " + Method);
		}
		bool result = false;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (!(propSet[i].name == "disabledMethod"))
				{
					continue;
				}
				string[] array = (string[])propSet[i].val;
				for (int j = 0; j < array.Length; j++)
				{
					if (array[j] == Method)
					{
						result = true;
						break;
					}
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + result);
		}
		return result;
	}

	public static SortedList<string, string> GetCustomFieldHeaders(string ManagedObjectType)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		SortedList<string, string> sortedList = new SortedList<string, string>();
		try
		{
			DynamicProperty[] propSet = _CustomFields[0].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (!(propSet[i].name == "field"))
				{
					continue;
				}
				CustomFieldDef[] array = (CustomFieldDef[])propSet[i].val;
				foreach (CustomFieldDef customFieldDef in array)
				{
					if (customFieldDef.managedObjectType == ManagedObjectType || customFieldDef.managedObjectType == null)
					{
						sortedList.Add(customFieldDef.name, customFieldDef.key.ToString());
					}
				}
				break;
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit SortedList");
		}
		return sortedList;
	}

	public static CustomFieldValue[] GetCustomFieldValueForVM(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		CustomFieldValue[] result = null;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "customValue")
				{
					result = (CustomFieldValue[])propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit CustomFieldValue[]");
		}
		return result;
	}

	public static CustomFieldValue[] GetCustomFieldValueForCluster(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		CustomFieldValue[] result = null;
		try
		{
			DynamicProperty[] propSet = _ClusterInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "customValue")
				{
					result = (CustomFieldValue[])propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit CustomFieldValue[]");
		}
		return result;
	}

	public static CustomFieldValue[] GetCustomFieldValueForHost(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		CustomFieldValue[] result = null;
		try
		{
			DynamicProperty[] propSet = HostInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "customValue")
				{
					result = (CustomFieldValue[])propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit CustomFieldValue[]");
		}
		return result;
	}

	public static CustomFieldValue[] GetCustomFieldValueForDistributedSwitch(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		CustomFieldValue[] result = null;
		try
		{
			DynamicProperty[] propSet = ((ObjectContent[])DistributedSwitchInventory[index])[0].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "customValue")
				{
					result = (CustomFieldValue[])propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit CustomFieldValue[]");
		}
		return result;
	}

	public static CustomFieldValue[] GetCustomFieldValueForDatastore(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		CustomFieldValue[] result = null;
		try
		{
			DynamicProperty[] propSet = _DatastoreInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "customValue")
				{
					result = (CustomFieldValue[])propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit CustomFieldValue[]");
		}
		return result;
	}

	public static SharesInfo GetCPUShareInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		SharesInfo result = null;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "resourceConfig.cpuAllocation.shares")
				{
					result = (SharesInfo)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit SharesInfo");
		}
		return result;
	}

	public static VirtualMachineVirtualNumaInfo GetNumaInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		VirtualMachineVirtualNumaInfo result = null;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config.numaInfo")
				{
					result = (VirtualMachineVirtualNumaInfo)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit SharesInfo");
		}
		return result;
	}

	public static SharesInfo GetMemoryShareInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		SharesInfo result = null;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "resourceConfig.memoryAllocation.shares")
				{
					result = (SharesInfo)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit SharesInfo");
		}
		return result;
	}

	public static VirtualMachineSnapshotTree[] GetSnapShotTree(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		VirtualMachineSnapshotTree[] result = null;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "snapshot.rootSnapshotList")
				{
					result = (VirtualMachineSnapshotTree[])propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit VirtualMachineSnapshotTree[]");
		}
		return result;
	}

	public static VirtualMachineSnapshotTree[] GetChildSnapShotTree(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		VirtualMachineSnapshotTree[] array = null;
		try
		{
			array = GetSnapShotTree(index);
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit Snapshots[0].childSnapshotList");
		}
		return array[0].childSnapshotList;
	}

	public static string GetCustomFieldDef(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		string text = "";
		try
		{
			CustomFieldDef[] array = (CustomFieldDef[])_CustomFields[0].propSet[0].val;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].key == index)
				{
					text = array[i].name;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + text);
		}
		return text;
	}

	public static HostVirtualSwitch[] GetSwitchInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		HostVirtualSwitch[] result = null;
		try
		{
			DynamicProperty[] propSet = HostInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config.network.vswitch")
				{
					result = (HostVirtualSwitch[])propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit HostVirtualSwitch[]");
		}
		return result;
	}

	public static HostHostBusAdapter[] GetHostHBAInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		HostHostBusAdapter[] result = null;
		try
		{
			DynamicProperty[] propSet = HostInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config.storageDevice.hostBusAdapter")
				{
					result = (HostHostBusAdapter[])propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit HostHostBusAdapter[]");
		}
		return result;
	}

	public static ManagedObjectReference[] GetHostDatastoreMORs(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		ManagedObjectReference[] result = null;
		try
		{
			DynamicProperty[] propSet = HostInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "datastore")
				{
					result = (ManagedObjectReference[])propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit ManagedObjectReference[]");
		}
		return result;
	}

	public static VmfsDatastoreInfo GetHostDatastoreInfo(ManagedObjectReference mor)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for mor " + mor.Value);
		}
		VmfsDatastoreInfo result = null;
		try
		{
			for (int i = 0; i < _DatastoreInventory.Length; i++)
			{
				if (!(_DatastoreInventory[i].obj.Value == mor.Value))
				{
					continue;
				}
				DynamicProperty[] propSet = _DatastoreInventory[i].propSet;
				for (int j = 0; j < propSet.Length; j++)
				{
					if (propSet[j].name == "info")
					{
						if (propSet[j].val is VmfsDatastoreInfo)
						{
							result = (VmfsDatastoreInfo)propSet[j].val;
						}
						break;
					}
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit VmfsDatastoreInfo");
		}
		return result;
	}

	public static ScsiLun[] GetHostScsiLun(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		ScsiLun[] result = null;
		try
		{
			DynamicProperty[] propSet = HostInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config.storageDevice.scsiLun")
				{
					result = (ScsiLun[])propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit ScsiLun[]");
		}
		return result;
	}

	public static HostMultipathInfo GetHostMultipathInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		HostMultipathInfo result = null;
		try
		{
			DynamicProperty[] propSet = HostInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config.storageDevice.multipathInfo")
				{
					result = (HostMultipathInfo)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit HostMultipathInfo");
		}
		return result;
	}

	public static HostHostBusAdapter[] GetHostBusAdapter(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		HostHostBusAdapter[] result = null;
		try
		{
			DynamicProperty[] propSet = HostInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config.storageDevice.hostBusAdapter")
				{
					result = (HostHostBusAdapter[])propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit HostHostBusAdapter[]");
		}
		return result;
	}

	public static PhysicalNic[] GetHostNicInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		PhysicalNic[] result = null;
		try
		{
			DynamicProperty[] propSet = HostInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config.network.pnic")
				{
					result = (PhysicalNic[])propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit PhysicalNic[]");
		}
		return result;
	}

	public static HostVirtualSwitch[] GetHostNetworkSwitchInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		HostVirtualSwitch[] result = null;
		try
		{
			DynamicProperty[] propSet = HostInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config.network.vswitch")
				{
					result = (HostVirtualSwitch[])propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit HostVirtualSwitch[]");
		}
		return result;
	}

	public static HostProxySwitch[] GetHostNetworkProxySwitchInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		HostProxySwitch[] result = null;
		try
		{
			DynamicProperty[] propSet = HostInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config.network.proxySwitch")
				{
					result = (HostProxySwitch[])propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit HostProxySwitch[]");
		}
		return result;
	}

	public static string GetDSSwitchNameV2(string switchUuid)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for switchUuid " + switchUuid);
		}
		string text = null;
		try
		{
			for (int i = 0; i < DistributedSwitchInventory.Count; i++)
			{
				DynamicProperty[] propSet = ((ObjectContent[])DistributedSwitchInventory[i])[0].propSet;
				for (int j = 0; j < propSet.Length; j++)
				{
					if (propSet[j].name == "config")
					{
						DVSConfigInfo dVSConfigInfo = (DVSConfigInfo)propSet[j].val;
						if (switchUuid == dVSConfigInfo.uuid)
						{
							text = dVSConfigInfo.name;
							break;
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + text);
		}
		return text;
	}

	public static string GetDSPortNameV2(string PortKey)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for PortKey " + PortKey);
		}
		string text = null;
		try
		{
			for (int i = 0; i < DistributedPortInventory.Count; i++)
			{
				DynamicProperty[] propSet = ((ObjectContent[])DistributedPortInventory[i])[0].propSet;
				for (int j = 0; j < propSet.Length; j++)
				{
					if (propSet[j].name == "config")
					{
						DVPortgroupConfigInfo dVPortgroupConfigInfo = (DVPortgroupConfigInfo)propSet[j].val;
						if (PortKey == dVPortgroupConfigInfo.key)
						{
							text = dVPortgroupConfigInfo.name;
							break;
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + text);
		}
		return text;
	}

	public static List<HostPortGroup> GetPortInfo()
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		List<HostPortGroup> list = new List<HostPortGroup>();
		for (int i = 0; i < HostInventory.Length; i++)
		{
			DynamicProperty[] propSet = HostInventory[i].propSet;
			for (int j = 0; j < propSet.Length; j++)
			{
				if (propSet[j].name == "config.network.portgroup")
				{
					HostPortGroup[] array = (HostPortGroup[])propSet[j].val;
					foreach (HostPortGroup item in array)
					{
						list.Add(item);
					}
				}
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit List<HostPortGroup>");
		}
		return list;
	}

	public static HostPortGroup[] GetPortInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		HostPortGroup[] result = null;
		try
		{
			DynamicProperty[] propSet = HostInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config.network.portgroup")
				{
					result = (HostPortGroup[])propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit HostPortGroup[]");
		}
		return result;
	}

	public static HostVirtualNic[] GetConsoleInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		HostVirtualNic[] result = null;
		try
		{
			DynamicProperty[] propSet = HostInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config.network.consoleVnic")
				{
					result = (HostVirtualNic[])propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit HostVirtualNic[]");
		}
		return result;
	}

	public static HostIpRouteConfig GetConsoleGateway(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		HostIpRouteConfig result = null;
		try
		{
			DynamicProperty[] propSet = HostInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config.network.consoleIpRouteConfig")
				{
					result = (HostIpRouteConfig)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit HostIpRouteConfig");
		}
		return result;
	}

	public static HostVirtualNic[] GetVMKernelInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		HostVirtualNic[] result = null;
		try
		{
			DynamicProperty[] propSet = HostInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config.network.vnic")
				{
					result = (HostVirtualNic[])propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit HostVirtualNic[]");
		}
		return result;
	}

	public static HostIpRouteConfig GetVMKernelGateway(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		HostIpRouteConfig result = null;
		try
		{
			DynamicProperty[] propSet = HostInventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config.network.ipRouteConfig")
				{
					result = (HostIpRouteConfig)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit HostIpRouteConfig");
		}
		return result;
	}

	public static VirtualMachineUsageOnDatastore[] GetVirtualMachineUsageOnDatastore(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		VirtualMachineUsageOnDatastore[] result = null;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "storage")
				{
					result = ((VirtualMachineStorageInfo)propSet[i].val).perDatastoreUsage;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit VirtualMachineUsageOnDatastore[]");
		}
		return result;
	}

	public static VirtualMachineRuntimeInfoDasProtectionState GetDASProtectionState(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		VirtualMachineRuntimeInfoDasProtectionState result = null;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "summary.runtime.dasVmProtection")
				{
					result = (VirtualMachineRuntimeInfoDasProtectionState)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit VirtualMachineRuntimeInfoDasProtectionState");
		}
		return result;
	}

	public static FaultToleranceConfigInfo GetFTInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		FaultToleranceConfigInfo result = null;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config.ftInfo")
				{
					result = (FaultToleranceConfigInfo)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit GetFTInfo");
		}
		return result;
	}

	public static VirtualMachineFileInfo GetVirtualMachineFileInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		VirtualMachineFileInfo result = null;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config.files")
				{
					result = (VirtualMachineFileInfo)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit GetVirtualMachineFileInfo");
		}
		return result;
	}

	public static VirtualDevice[] GetDeviceInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		VirtualDevice[] result = null;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config.hardware")
				{
					result = ((VirtualHardware)propSet[i].val).device;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit VirtualDevice[]");
		}
		return result;
	}

	public static VirtualMachineBootOptions GetBootOptions(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		VirtualMachineBootOptions result = null;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config.bootOptions")
				{
					result = (VirtualMachineBootOptions)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit VirtualMachineBootOptions");
		}
		return result;
	}

	public static ScheduledHardwareUpgradeInfo GetScheduledHardwareUpgradeInfo(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		ScheduledHardwareUpgradeInfo result = null;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config.scheduledHardwareUpgradeInfo")
				{
					result = (ScheduledHardwareUpgradeInfo)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit ScheduledHardwareUpgradeInfo");
		}
		return result;
	}

	public static LatencySensitivity GetLatencySensitivity(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		LatencySensitivity result = null;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config.latencySensitivity")
				{
					result = (LatencySensitivity)propSet[i].val;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit GetlatencySensitivity");
		}
		return result;
	}

	public static bool GetSRMPlaceholderBool(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		bool result = false;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (propSet[i].name == "config.managedBy")
				{
					if (((ManagedByInfo)propSet[i].val).extensionKey == "com.vmware.vcDr")
					{
						result = true;
					}
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit GetManagedByInfo");
		}
		return result;
	}

	public static VirtualMachineFileLayoutExFileInfo GetSnapshotFileInfo(int index, string key)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index and key" + index + ", " + key);
		}
		VirtualMachineFileLayoutExFileInfo result = null;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (!(propSet[i].name == "layoutEx.snapshot"))
				{
					continue;
				}
				try
				{
					VirtualMachineFileLayoutExSnapshotLayout[] array = (VirtualMachineFileLayoutExSnapshotLayout[])propSet[i].val;
					foreach (VirtualMachineFileLayoutExSnapshotLayout virtualMachineFileLayoutExSnapshotLayout in array)
					{
						if (!(virtualMachineFileLayoutExSnapshotLayout.key.Value == key))
						{
							continue;
						}
						for (int k = 0; k < propSet.Length; k++)
						{
							if (!(propSet[k].name == "layoutEx.file"))
							{
								continue;
							}
							VirtualMachineFileLayoutExFileInfo[] array2 = (VirtualMachineFileLayoutExFileInfo[])propSet[k].val;
							for (int l = 0; l < array2.Length; l++)
							{
								if (array2[l].key == virtualMachineFileLayoutExSnapshotLayout.dataKey)
								{
									result = array2[l];
									break;
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
					}
				}
			}
		}
		catch (Exception ex2)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex2.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit VirtualMachineFileLayoutExFileInfo");
		}
		return result;
	}

	public static long GetSnapshotTotalSize(int index)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for index " + index);
		}
		long num = 0L;
		try
		{
			DynamicProperty[] propSet = _Inventory[index].propSet;
			for (int i = 0; i < propSet.Length; i++)
			{
				if (!(propSet[i].name == "layoutEx.file"))
				{
					continue;
				}
				try
				{
					VirtualMachineFileLayoutExFileInfo[] array = (VirtualMachineFileLayoutExFileInfo[])propSet[i].val;
					Regex regex = new Regex(".*[-][0-9]{6}.vmdk");
					VirtualMachineFileLayoutExFileInfo[] array2 = array;
					foreach (VirtualMachineFileLayoutExFileInfo virtualMachineFileLayoutExFileInfo in array2)
					{
						if (virtualMachineFileLayoutExFileInfo.type == "snapshotData")
						{
							num += virtualMachineFileLayoutExFileInfo.size;
						}
						if (virtualMachineFileLayoutExFileInfo.type == "snapshotList")
						{
							num += virtualMachineFileLayoutExFileInfo.size;
						}
						if (virtualMachineFileLayoutExFileInfo.name.EndsWith("-delta.vmdk"))
						{
							num += virtualMachineFileLayoutExFileInfo.size;
						}
						if (virtualMachineFileLayoutExFileInfo.type == "snapshotMemory")
						{
							num += virtualMachineFileLayoutExFileInfo.size;
						}
						if (regex.IsMatch(virtualMachineFileLayoutExFileInfo.name))
						{
							num += virtualMachineFileLayoutExFileInfo.size;
						}
					}
				}
				catch (Exception ex)
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
					}
				}
			}
		}
		catch (Exception ex2)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex2.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit " + num);
		}
		return num;
	}

	public static void QueryDistributedSwitchInventory()
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		StringCollection stringCollection = new StringCollection();
		StringCollection stringCollection2 = new StringCollection();
		try
		{
			for (int i = 0; i < HostInventoryCounter; i++)
			{
				DynamicProperty[] propSet = HostInventory[i].propSet;
				for (int j = 0; j < propSet.Length; j++)
				{
					if (!(propSet[j].name == "network"))
					{
						continue;
					}
					ManagedObjectReference[] array = (ManagedObjectReference[])propSet[j].val;
					foreach (ManagedObjectReference managedObjectReference in array)
					{
						if (logger.IsDebugEnabled)
						{
							logger.Debug(methodBase.Name + " DistributedVirtualPortGroups mor.Value " + managedObjectReference.Value);
						}
						if (logger.IsDebugEnabled)
						{
							logger.Debug(methodBase.Name + " DistributedVirtualPortGroups mor.Type " + managedObjectReference.type);
						}
						if (!(managedObjectReference.type != "DistributedVirtualPortgroup") && !stringCollection.Contains(managedObjectReference.Value))
						{
							stringCollection.Add(managedObjectReference.Value);
							if (logger.IsDebugEnabled)
							{
								logger.Debug(methodBase.Name + " DistributedVirtualPortGroups.Add " + managedObjectReference.Value);
							}
						}
					}
					break;
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception (a): " + ex.Message);
			}
		}
		try
		{
			DistributedPortInventory.Clear();
			StringEnumerator enumerator = stringCollection.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					string current = enumerator.Current;
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " portname in DistributedVirtualPortGroups " + current);
					}
					ManagedObjectReference managedObjectReference2 = new ManagedObjectReference();
					managedObjectReference2.type = "DistributedVirtualPortgroup";
					managedObjectReference2.Value = current;
					ObjectContent[] objectProperties = GetObjectProperties(_sic.propertyCollector, managedObjectReference2, new string[5] { "name", "config", "summary", "host", "customValue" });
					DistributedPortInventory.Add(objectProperties);
				}
			}
			finally
			{
				if (enumerator is IDisposable disposable)
				{
					disposable.Dispose();
				}
			}
		}
		catch (Exception ex2)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception (b): " + ex2.Message);
			}
		}
		try
		{
			stringCollection2.Clear();
			StringEnumerator enumerator = stringCollection.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					string current2 = enumerator.Current;
					ManagedObjectReference managedObjectReference3 = new ManagedObjectReference();
					managedObjectReference3.type = "DistributedVirtualPortgroup";
					managedObjectReference3.Value = current2;
					ManagedObjectReference managedObjectReference4 = (ManagedObjectReference)GetObjectProperties(_sic.propertyCollector, managedObjectReference3, new string[1] { "config.distributedVirtualSwitch" })[0].propSet[0].val;
					if (!stringCollection2.Contains(managedObjectReference4.Value))
					{
						stringCollection2.Add(managedObjectReference4.Value);
						if (logger.IsDebugEnabled)
						{
							logger.Debug(methodBase.Name + " DistributedVirtualSwitches.Add " + managedObjectReference4.Value);
						}
					}
				}
			}
			finally
			{
				if (enumerator is IDisposable disposable2)
				{
					disposable2.Dispose();
				}
			}
		}
		catch (Exception ex3)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception (c): " + ex3.Message);
			}
		}
		try
		{
			DistributedSwitchInventory.Clear();
			StringEnumerator enumerator = stringCollection2.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					string current3 = enumerator.Current;
					ManagedObjectReference managedObjectReference5 = new ManagedObjectReference();
					managedObjectReference5.type = "DistributedVirtualSwitch";
					managedObjectReference5.Value = current3;
					ObjectContent[] objectProperties2 = GetObjectProperties(_sic.propertyCollector, managedObjectReference5, new string[5] { "name", "config", "portgroup", "summary", "customValue" });
					DistributedSwitchInventory.Add(objectProperties2);
				}
			}
			finally
			{
				if (enumerator is IDisposable disposable3)
				{
					disposable3.Dispose();
				}
			}
		}
		catch (Exception ex4)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception (d): " + ex4.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
	}

	public static void QueryInventory()
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		try
		{
			PropertySpec propertySpec = new PropertySpec();
			propertySpec.all = false;
			propertySpec.type = "VirtualMachine";
			propertySpec.pathSet = new string[_PropertySpecPathSet.Count];
			_PropertySpecPathSet.CopyTo(propertySpec.pathSet, 0);
			ObjectSpec objectSpec = new ObjectSpec();
			objectSpec.obj = _sic.rootFolder;
			objectSpec.skip = true;
			TraversalSpec traversalSpec = new TraversalSpec();
			traversalSpec.name = "recurseFolders";
			traversalSpec.type = "Folder";
			traversalSpec.path = "childEntity";
			traversalSpec.skip = false;
			SelectionSpec selectionSpec = new SelectionSpec();
			selectionSpec.name = "recurseFolders";
			TraversalSpec traversalSpec2 = new TraversalSpec();
			traversalSpec2.type = "Datacenter";
			traversalSpec2.path = "vmFolder";
			traversalSpec2.skip = true;
			traversalSpec2.selectSet = new SelectionSpec[1] { selectionSpec };
			traversalSpec.selectSet = new SelectionSpec[2] { selectionSpec, traversalSpec2 };
			TraversalSpec vMTraversalSpec = getVMTraversalSpec();
			objectSpec.selectSet = new SelectionSpec[1] { vMTraversalSpec };
			PropertyFilterSpec propertyFilterSpec = new PropertyFilterSpec();
			propertyFilterSpec.propSet = new PropertySpec[1] { propertySpec };
			propertyFilterSpec.objectSet = new ObjectSpec[1] { objectSpec };
			PropertyFilterSpec[] specs = new PropertyFilterSpec[1] { propertyFilterSpec };
			ManagedObjectReference managedObjectReference = new ManagedObjectReference();
			managedObjectReference.type = "ServiceInstance";
			managedObjectReference.Value = "ServiceInstance";
			ManagedObjectReference propertyCollector = _service.RetrieveServiceContent(managedObjectReference).propertyCollector;
			if (logger.IsDebugEnabled)
			{
				_Inventory = RetrievePropertiesEx(propertyCollector, specs, 1);
			}
			else
			{
				_Inventory = RetrievePropertiesEx(propertyCollector, specs);
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
	}

	public static ObjectContent[] RetrievePropertiesEx(ManagedObjectReference propertyCollector, PropertyFilterSpec[] specs, int maxObjects = 0)
	{
		string type = specs[0].propSet[0].type;
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " " + type + " Started");
		}
		List<ObjectContent> list = new List<ObjectContent>();
		try
		{
			RetrieveResult returnval = _service.RetrievePropertiesEx(new RetrievePropertiesExRequest(propertyCollector, specs, new RetrieveOptions
			{
				maxObjects = maxObjects,
				maxObjectsSpecified = (maxObjects != 0)
			})).returnval;
			if (returnval != null)
			{
				string token = returnval.token;
				if (logger.IsDebugEnabled)
				{
					logger.Debug(methodBase.Name + " " + type + " result #objects: " + returnval.objects.Length + " result token: " + token);
				}
				list.AddRange(returnval.objects.AsEnumerable());
				while (!string.IsNullOrEmpty(token))
				{
					returnval = _service.ContinueRetrievePropertiesEx(propertyCollector, token);
					if (returnval != null)
					{
						token = returnval.token;
						if (logger.IsDebugEnabled)
						{
							logger.Debug(methodBase.Name + " " + type + " result token: " + token);
						}
						list.AddRange(returnval.objects.AsEnumerable());
					}
				}
			}
		}
		catch (Exception ex)
		{
			logger.Error("RetrievePropertiesEx for " + type + " failed", ex);
			if (type == "VirtualMachine")
			{
				UserNotification.ReportWarning("RVTools warning", "Not all " + type + " inventory items are collected! Please, read documentation about a possible deserialize error!", ex);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " " + type + " Exit");
		}
		return list.ToArray();
	}

	public static TraversalSpec getVMTraversalSpec()
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		TraversalSpec traversalSpec = new TraversalSpec();
		try
		{
			TraversalSpec traversalSpec2 = new TraversalSpec();
			traversalSpec2.name = "vAppToVM";
			traversalSpec2.type = "VirtualApp";
			traversalSpec2.path = "vm";
			TraversalSpec traversalSpec3 = new TraversalSpec();
			traversalSpec3.name = "vAppToVApp";
			traversalSpec3.type = "VirtualApp";
			traversalSpec3.path = "resourcePool";
			SelectionSpec selectionSpec = new SelectionSpec();
			selectionSpec.name = "vAppToVApp";
			SelectionSpec selectionSpec2 = new SelectionSpec();
			selectionSpec2.name = "vAppToVM";
			SelectionSpec[] selectSet = new SelectionSpec[2] { selectionSpec, selectionSpec2 };
			traversalSpec3.selectSet = selectSet;
			SelectionSpec selectionSpec3 = new SelectionSpec();
			selectionSpec3.name = "VisitFolders";
			TraversalSpec traversalSpec4 = new TraversalSpec();
			traversalSpec4.name = "DataCenterToVMFolder";
			traversalSpec4.type = "Datacenter";
			traversalSpec4.path = "vmFolder";
			traversalSpec4.skip = false;
			SelectionSpec[] selectSet2 = new SelectionSpec[1] { selectionSpec3 };
			traversalSpec4.selectSet = selectSet2;
			traversalSpec.name = "VisitFolders";
			traversalSpec.type = "Folder";
			traversalSpec.path = "childEntity";
			traversalSpec.skip = false;
			SelectionSpec[] selectSet3 = new SelectionSpec[2] { selectionSpec3, traversalSpec4 };
			if (APIVersion >= 400)
			{
				selectSet3 = new SelectionSpec[4] { selectionSpec3, traversalSpec4, traversalSpec2, traversalSpec3 };
			}
			traversalSpec.selectSet = selectSet3;
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit TraversalSpec");
		}
		return traversalSpec;
	}

	public static void QueryHostInventory()
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		try
		{
			TraversalSpec traversalSpec = new TraversalSpec();
			traversalSpec.type = "ComputeResource";
			traversalSpec.skip = true;
			traversalSpec.path = "host";
			traversalSpec.selectSet = new SelectionSpec[1]
			{
				new SelectionSpec()
			};
			traversalSpec.selectSet[0].name = "traverseChild";
			TraversalSpec traversalSpec2 = new TraversalSpec();
			traversalSpec2.type = "ClusterComputeResource";
			traversalSpec2.skip = true;
			traversalSpec2.path = "host";
			traversalSpec2.selectSet = new SelectionSpec[1]
			{
				new SelectionSpec()
			};
			traversalSpec2.selectSet[0].name = "traverseChild";
			TraversalSpec traversalSpec3 = new TraversalSpec();
			traversalSpec3.type = "Datacenter";
			traversalSpec3.skip = true;
			traversalSpec3.path = "hostFolder";
			traversalSpec3.selectSet = new SelectionSpec[1]
			{
				new SelectionSpec()
			};
			traversalSpec3.selectSet[0].name = "traverseChild";
			TraversalSpec traversalSpec4 = new TraversalSpec();
			traversalSpec4.type = "Folder";
			traversalSpec4.skip = false;
			traversalSpec4.path = "childEntity";
			traversalSpec4.name = "traverseChild";
			traversalSpec4.selectSet = new SelectionSpec[4]
			{
				new SelectionSpec(),
				traversalSpec,
				traversalSpec2,
				traversalSpec3
			};
			traversalSpec4.selectSet[0].name = traversalSpec4.name;
			ObjectSpec objectSpec = new ObjectSpec();
			objectSpec.obj = _sic.rootFolder;
			objectSpec.skip = true;
			objectSpec.selectSet = new SelectionSpec[1] { traversalSpec4 };
			PropertySpec propertySpec = new PropertySpec();
			propertySpec.type = "HostSystem";
			propertySpec.all = false;
			propertySpec.allSpecified = false;
			propertySpec.pathSet = new string[38]
			{
				"name", "capability", "config.service", "customValue", "configStatus", "configIssue", "datastore", "parent", "vm", "config.storageDevice.multipathInfo",
				"config.storageDevice.scsiLun", "config.network.vnic", "config.storageDevice.hostBusAdapter", "config.network.ipRouteConfig", "config.network.consoleVnic", "config.network.consoleIpRouteConfig", "config.network.dnsConfig", "config.network.vswitch", "summary.runtime.bootTime", "summary.config.product",
				"summary.hardware.cpuModel", "summary.hardware.cpuMhz", "summary.hardware.memorySize", "summary.hardware.numCpuPkgs", "summary.hardware.numCpuCores", "summary.hardware.numHBAs", "summary.hardware.numNics", "summary.quickStats.overallCpuUsage", "summary.quickStats.overallMemoryUsage", "summary.hardware.otherIdentifyingInfo",
				"config.consoleReservation.serviceConsoleReserved", "config.network.vswitch", "config.network.portgroup", "config.dateTimeInfo", "config.hyperThread", "hardware.biosInfo", "hardware.systemInfo", "config.network.pnic"
			};
			if (APIVersion >= 400)
			{
				propertySpec.pathSet = new string[43]
				{
					"name", "capability", "config.service", "customValue", "configStatus", "configIssue", "datastore", "parent", "vm", "config.storageDevice.multipathInfo",
					"config.storageDevice.scsiLun", "config.network.vnic", "config.storageDevice.hostBusAdapter", "config.network.ipRouteConfig", "config.network.consoleVnic", "config.network.consoleIpRouteConfig", "config.network.dnsConfig", "config.network.vswitch", "config.network.proxySwitch", "summary.runtime.bootTime",
					"summary.config.product", "summary.currentEVCModeKey", "summary.maxEVCModeKey", "summary.hardware.cpuModel", "summary.hardware.cpuMhz", "summary.hardware.memorySize", "summary.hardware.numCpuPkgs", "summary.hardware.numCpuCores", "summary.hardware.numHBAs", "summary.hardware.numNics",
					"summary.quickStats.overallCpuUsage", "summary.quickStats.overallMemoryUsage", "summary.hardware.otherIdentifyingInfo", "config.consoleReservation.serviceConsoleReserved", "config.network.vswitch", "config.network.portgroup", "config.dateTimeInfo", "config.hyperThread", "hardware.biosInfo", "hardware.systemInfo",
					"hardware.cpuPowerManagementInfo", "config.network.pnic", "network"
				};
			}
			if (APIVersion >= 410)
			{
				propertySpec.pathSet = new string[46]
				{
					"name", "capability", "configManager", "runtime.inMaintenanceMode", "config.service", "config.powerSystemInfo", "customValue", "configStatus", "configIssue", "datastore",
					"parent", "vm", "config.storageDevice.multipathInfo", "config.storageDevice.scsiLun", "config.network.vnic", "config.storageDevice.hostBusAdapter", "config.network.ipRouteConfig", "config.network.consoleVnic", "config.network.consoleIpRouteConfig", "config.network.dnsConfig",
					"config.network.vswitch", "config.network.proxySwitch", "summary.runtime.bootTime", "summary.config.product", "summary.currentEVCModeKey", "summary.maxEVCModeKey", "summary.hardware.cpuModel", "summary.hardware.cpuMhz", "summary.hardware.memorySize", "summary.hardware.numCpuPkgs",
					"summary.hardware.numCpuCores", "summary.hardware.numHBAs", "summary.hardware.numNics", "summary.quickStats.overallCpuUsage", "summary.quickStats.overallMemoryUsage", "summary.hardware.otherIdentifyingInfo", "config.consoleReservation.serviceConsoleReserved", "config.network.vswitch", "config.network.portgroup", "config.dateTimeInfo",
					"config.hyperThread", "hardware.biosInfo", "hardware.systemInfo", "hardware.cpuPowerManagementInfo", "config.network.pnic", "network"
				};
			}
			if (APIVersion >= 650)
			{
				propertySpec.pathSet = new string[48]
				{
					"name", "capability", "configManager", "runtime.inMaintenanceMode", "runtime.inQuarantineMode", "config.service", "config.powerSystemInfo", "customValue", "configStatus", "configIssue",
					"datastore", "parent", "vm", "config.storageDevice.multipathInfo", "config.storageDevice.scsiLun", "config.network.vnic", "config.storageDevice.hostBusAdapter", "config.network.ipRouteConfig", "config.network.consoleVnic", "config.network.consoleIpRouteConfig",
					"config.network.dnsConfig", "config.network.vswitch", "config.network.proxySwitch", "summary.runtime.bootTime", "summary.config.product", "summary.currentEVCModeKey", "summary.maxEVCModeKey", "summary.hardware.cpuModel", "summary.hardware.cpuMhz", "summary.hardware.memorySize",
					"summary.hardware.numCpuPkgs", "summary.hardware.numCpuCores", "summary.hardware.numHBAs", "summary.hardware.numNics", "summary.quickStats.overallCpuUsage", "summary.quickStats.overallMemoryUsage", "summary.hardware.otherIdentifyingInfo", "config.consoleReservation.serviceConsoleReserved", "config.network.vswitch", "config.network.portgroup",
					"config.dateTimeInfo", "config.hyperThread", "hardware.biosInfo", "hardware.systemInfo", "hardware.cpuPowerManagementInfo", "config.network.pnic", "network", "config.domainList"
				};
			}
			if (APIVersion >= 670)
			{
				propertySpec.pathSet = new string[49]
				{
					"name", "capability", "configManager", "runtime.inMaintenanceMode", "runtime.inQuarantineMode", "config.service", "config.powerSystemInfo", "customValue", "configStatus", "configIssue",
					"datastore", "parent", "vm", "config.storageDevice.multipathInfo", "config.storageDevice.scsiLun", "config.network.vnic", "config.storageDevice.hostBusAdapter", "config.network.ipRouteConfig", "config.network.consoleVnic", "config.network.consoleIpRouteConfig",
					"config.network.dnsConfig", "config.network.vswitch", "config.network.proxySwitch", "summary.runtime.bootTime", "summary.config.product", "summary.currentEVCModeKey", "summary.maxEVCModeKey", "summary.hardware.cpuModel", "summary.hardware.cpuMhz", "summary.hardware.memorySize",
					"summary.hardware.numCpuPkgs", "summary.hardware.numCpuCores", "summary.hardware.numHBAs", "summary.hardware.numNics", "summary.quickStats.overallCpuUsage", "summary.quickStats.overallMemoryUsage", "summary.hardware.otherIdentifyingInfo", "config.consoleReservation.serviceConsoleReserved", "config.network.vswitch", "config.network.portgroup",
					"config.dateTimeInfo", "config.hyperThread", "hardware.biosInfo", "hardware.systemInfo", "hardware.cpuPowerManagementInfo", "config.network.pnic", "network", "config.domainList", "complianceCheckState"
				};
			}
			if (APIVersion >= 703)
			{
				propertySpec.pathSet = new string[50]
				{
					"name", "capability", "configManager", "runtime.inMaintenanceMode", "runtime.inQuarantineMode", "config.service", "config.powerSystemInfo", "customValue", "configStatus", "configIssue",
					"datastore", "parent", "vm", "config.storageDevice.multipathInfo", "config.storageDevice.scsiLun", "config.network.vnic", "config.storageDevice.hostBusAdapter", "config.network.ipRouteConfig", "config.network.consoleVnic", "config.network.consoleIpRouteConfig",
					"config.network.dnsConfig", "config.network.vswitch", "config.network.proxySwitch", "summary.runtime.bootTime", "summary.config.product", "summary.currentEVCModeKey", "summary.maxEVCModeKey", "summary.hardware.cpuModel", "summary.hardware.cpuMhz", "summary.hardware.memorySize",
					"summary.hardware.numCpuPkgs", "summary.hardware.numCpuCores", "summary.hardware.numHBAs", "summary.hardware.numNics", "summary.quickStats.overallCpuUsage", "summary.quickStats.overallMemoryUsage", "summary.hardware.otherIdentifyingInfo", "config.consoleReservation.serviceConsoleReserved", "config.network.vswitch", "config.network.portgroup",
					"config.dateTimeInfo", "config.hyperThread", "hardware.biosInfo", "hardware.systemInfo", "hardware.cpuPowerManagementInfo", "config.network.pnic", "network", "config.domainList", "complianceCheckState", "hardware.memoryTieringType"
				};
			}
			PropertySpec[] propSet = new PropertySpec[1] { propertySpec };
			PropertyFilterSpec propertyFilterSpec = new PropertyFilterSpec();
			propertyFilterSpec.objectSet = new ObjectSpec[1] { objectSpec };
			propertyFilterSpec.propSet = propSet;
			PropertyFilterSpec[] specs = new PropertyFilterSpec[1] { propertyFilterSpec };
			ManagedObjectReference managedObjectReference = new ManagedObjectReference();
			managedObjectReference.type = "ServiceInstance";
			managedObjectReference.Value = "ServiceInstance";
			HostInventory = RetrievePropertiesEx(_service.RetrieveServiceContent(managedObjectReference).propertyCollector, specs);
			Dictionary<string, uint> dictionary = new Dictionary<string, uint>();
			ObjectContent[] hostInventory = HostInventory;
			for (int i = 0; i < hostInventory.Length; i++)
			{
				string text = ((AboutInfo)(hostInventory[i].propSet.FirstOrDefault((DynamicProperty h) => h.name == "summary.config.product")?.val))?.fullName;
				if (text != null)
				{
					dictionary.TryGetValue(text, out var value);
					value = (dictionary[text] = value + 1);
				}
			}
			foreach (KeyValuePair<string, uint> item in dictionary)
			{
				Telemetry.AddEvent("EsxVersion", item.Key, new Dictionary<string, string> { 
				{
					"Count",
					item.Value.ToString()
				} });
			}
			_HostCertificates = HostInventory.ToDictionary((ObjectContent kvp) => kvp.obj.Value, (ObjectContent kvp) => GetHostCertificateInfo(GetHostConfigManager(kvp).certificateManager));
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
	}

	public static void QueryClusterInventory()
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		try
		{
			TraversalSpec traversalSpec = new TraversalSpec();
			traversalSpec.type = "ComputeResource";
			traversalSpec.skip = true;
			traversalSpec.path = "host";
			traversalSpec.selectSet = new SelectionSpec[1]
			{
				new SelectionSpec()
			};
			traversalSpec.selectSet[0].name = "traverseChild";
			TraversalSpec traversalSpec2 = new TraversalSpec();
			traversalSpec2.type = "ClusterComputeResource";
			traversalSpec2.skip = true;
			traversalSpec2.path = "host";
			traversalSpec2.selectSet = new SelectionSpec[1]
			{
				new SelectionSpec()
			};
			traversalSpec2.selectSet[0].name = "traverseChild";
			TraversalSpec traversalSpec3 = new TraversalSpec();
			traversalSpec3.type = "Datacenter";
			traversalSpec3.skip = true;
			traversalSpec3.path = "hostFolder";
			traversalSpec3.selectSet = new SelectionSpec[1]
			{
				new SelectionSpec()
			};
			traversalSpec3.selectSet[0].name = "traverseChild";
			TraversalSpec traversalSpec4 = new TraversalSpec();
			traversalSpec4.type = "Folder";
			traversalSpec4.skip = false;
			traversalSpec4.path = "childEntity";
			traversalSpec4.name = "traverseChild";
			traversalSpec4.selectSet = new SelectionSpec[4]
			{
				new SelectionSpec(),
				traversalSpec,
				traversalSpec2,
				traversalSpec3
			};
			traversalSpec4.selectSet[0].name = traversalSpec4.name;
			ObjectSpec objectSpec = new ObjectSpec();
			objectSpec.obj = _sic.rootFolder;
			objectSpec.skip = true;
			objectSpec.selectSet = new SelectionSpec[1] { traversalSpec4 };
			PropertySpec propertySpec = new PropertySpec();
			propertySpec.type = "ClusterComputeResource";
			propertySpec.all = false;
			propertySpec.allSpecified = false;
			propertySpec.pathSet = new string[7] { "name", "configStatus", "configIssue", "configuration", "configurationEx", "summary", "customValue" };
			PropertySpec[] propSet = new PropertySpec[1] { propertySpec };
			PropertyFilterSpec propertyFilterSpec = new PropertyFilterSpec();
			propertyFilterSpec.objectSet = new ObjectSpec[1] { objectSpec };
			propertyFilterSpec.propSet = propSet;
			PropertyFilterSpec[] specs = new PropertyFilterSpec[1] { propertyFilterSpec };
			ManagedObjectReference managedObjectReference = new ManagedObjectReference();
			managedObjectReference.type = "ServiceInstance";
			managedObjectReference.Value = "ServiceInstance";
			_ClusterInventory = RetrievePropertiesEx(_service.RetrieveServiceContent(managedObjectReference).propertyCollector, specs);
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
	}

	public static void QueryDatastoreInventory()
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		try
		{
			TraversalSpec traversalSpec = new TraversalSpec();
			traversalSpec.type = "Datacenter";
			traversalSpec.skip = false;
			traversalSpec.path = "datastore";
			traversalSpec.selectSet = new SelectionSpec[1]
			{
				new SelectionSpec()
			};
			traversalSpec.selectSet[0].name = "traverseChild";
			TraversalSpec traversalSpec2 = new TraversalSpec();
			traversalSpec2.type = "Folder";
			traversalSpec2.skip = false;
			traversalSpec2.path = "childEntity";
			traversalSpec2.name = "traverseChild";
			traversalSpec2.selectSet = new SelectionSpec[2]
			{
				new SelectionSpec(),
				traversalSpec
			};
			traversalSpec2.selectSet[0].name = traversalSpec2.name;
			ObjectSpec objectSpec = new ObjectSpec();
			objectSpec.obj = _sic.rootFolder;
			objectSpec.skip = true;
			objectSpec.selectSet = new SelectionSpec[1] { traversalSpec2 };
			PropertySpec propertySpec = new PropertySpec();
			propertySpec.type = "Datastore";
			propertySpec.all = false;
			propertySpec.allSpecified = false;
			propertySpec.pathSet = new string[11]
			{
				"vm", "browser", "host", "info", "summary.accessible", "summary.capacity", "summary.freeSpace", "summary.multipleHostAccess", "summary.name", "summary.type",
				"summary.url"
			};
			if (APIVersion >= 400)
			{
				propertySpec.pathSet = new string[15]
				{
					"vm", "customValue", "browser", "host", "configStatus", "configIssue", "info", "name", "summary.accessible", "summary.capacity",
					"summary.freeSpace", "summary.multipleHostAccess", "summary.name", "summary.type", "summary.url"
				};
			}
			if (APIVersion >= 410)
			{
				propertySpec.pathSet = new string[16]
				{
					"vm", "customValue", "browser", "host", "configStatus", "configIssue", "info", "name", "iormConfiguration", "summary.accessible",
					"summary.capacity", "summary.freeSpace", "summary.multipleHostAccess", "summary.name", "summary.type", "summary.url"
				};
			}
			if (APIVersion >= 500)
			{
				propertySpec.pathSet = new string[17]
				{
					"vm", "customValue", "browser", "host", "parent", "configStatus", "configIssue", "info", "name", "iormConfiguration",
					"summary.accessible", "summary.capacity", "summary.freeSpace", "summary.multipleHostAccess", "summary.name", "summary.type", "summary.url"
				};
			}
			PropertySpec[] propSet = new PropertySpec[1] { propertySpec };
			PropertyFilterSpec propertyFilterSpec = new PropertyFilterSpec();
			propertyFilterSpec.objectSet = new ObjectSpec[1] { objectSpec };
			propertyFilterSpec.propSet = propSet;
			PropertyFilterSpec[] specs = new PropertyFilterSpec[1] { propertyFilterSpec };
			ManagedObjectReference managedObjectReference = new ManagedObjectReference();
			managedObjectReference.type = "ServiceInstance";
			managedObjectReference.Value = "ServiceInstance";
			_DatastoreInventory = RetrievePropertiesEx(_service.RetrieveServiceContent(managedObjectReference).propertyCollector, specs);
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
	}

	public static void KeepALive()
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		while (true)
		{
			Thread.Sleep(60000);
			try
			{
				ManagedObjectReference managedObjectReference = new ManagedObjectReference();
				managedObjectReference.type = "ServiceInstance";
				managedObjectReference.Value = "ServiceInstance";
				DateTime dateTime = _service.CurrentTime(managedObjectReference);
				if (logger.IsDebugEnabled)
				{
					logger.Debug(methodBase.Name + " KeepALive succeeded " + dateTime.ToLocalTime().ToString());
				}
			}
			catch (Exception ex)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
				}
			}
		}
	}

	public static void QueryCISTags()
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		List<Hashtable> list = new List<Hashtable>();
		List<Hashtable> list2 = new List<Hashtable>();
		cisAssociatedTag.Clear();
		string text = null;
		try
		{
			text = GetCISSessionId();
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: GetSession: " + ex.Message);
			}
			return;
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + "CISSession: " + text);
		}
		try
		{
			foreach (string item in JsonConvert.DeserializeObject<List<string>>(JObject.Parse(GetListOfCategoryIDs(text)).GetValue("value").ToString()))
			{
				JObject jObject = JObject.Parse(GetCategoryName(text, item));
				string text2 = (string?)jObject["value"]["name"];
				string text3 = (string?)jObject["value"]["description"];
				Hashtable hashtable = new Hashtable();
				hashtable["id"] = item;
				hashtable["name"] = text2;
				hashtable["desc"] = text3;
				list.Add(hashtable);
				if (logger.IsDebugEnabled)
				{
					logger.Debug(methodBase.Name + " Catagory: " + item + ", " + text2 + ", " + text3);
				}
			}
		}
		catch (Exception ex2)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: Get list of Category IDs (Tag Keys): " + ex2.Message);
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Exit");
			}
			return;
		}
		try
		{
			List<string> list3 = JsonConvert.DeserializeObject<List<string>>(JObject.Parse(GetListOfTags(text)).GetValue("value").ToString());
			foreach (string item2 in list3)
			{
				JObject jObject2 = JObject.Parse(GetTagName(text, item2));
				string text4 = (string?)jObject2["value"]["name"];
				string value = (string?)jObject2["value"]["description"];
				string text5 = (string?)jObject2["value"]["category_id"];
				string text6 = null;
				foreach (Hashtable item3 in list)
				{
					string text7 = item3["id"].ToString();
					string text8 = item3["name"].ToString();
					string text9 = item3["desc"].ToString();
					if (text7 == text5)
					{
						text6 = text8;
						value = text9;
					}
				}
				Hashtable hashtable2 = new Hashtable();
				hashtable2["id"] = item2;
				hashtable2["name"] = text4;
				hashtable2["description"] = value;
				hashtable2["category"] = text6;
				list2.Add(hashtable2);
				if (logger.IsDebugEnabled)
				{
					logger.Debug(methodBase.Name + " cisTag.Add: " + item2 + " " + text4 + " catagory_name " + text6);
				}
			}
			foreach (string item4 in list3)
			{
				foreach (JToken item5 in (IEnumerable<JToken>)JObject.Parse(GetAssociatedObjects(text, item4)).GetValue("value"))
				{
					string text10 = (string?)item5["id"];
					string text11 = (string?)item5["type"];
					string text12 = null;
					string value2 = null;
					string value3 = null;
					string text13 = null;
					foreach (Hashtable item6 in list2)
					{
						string text14 = item6["id"].ToString();
						string text15 = item6["name"].ToString();
						string text16 = item6["category"].ToString();
						string text17 = item6["description"].ToString();
						if (text14 == item4)
						{
							text12 = text15;
							value2 = text14;
							value3 = text17;
							text13 = text16;
						}
					}
					Hashtable hashtable3 = new Hashtable();
					hashtable3["id"] = text10;
					hashtable3["type"] = text11;
					hashtable3["tag_id"] = value2;
					hashtable3["tag_value"] = text12;
					hashtable3["tag_desc"] = value3;
					hashtable3["tag_cat"] = text13;
					cisAssociatedTag.Add(hashtable3);
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " cisAssociatedTag.Add TagID = " + item4 + " id = " + text10 + " type = " + text11 + " tag = " + text12 + " cat = " + text13);
					}
				}
			}
		}
		catch (Exception ex3)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: Get list of Tag IDs(Tag Values): " + ex3.Message);
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Exit");
			}
			return;
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
	}

	public static string GetTagColumnValues(ref List<Hashtable> associatedTags, string type, string columnName, string id)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started type = " + type + " columnName = " + columnName + " id = " + id);
		}
		string text = null;
		foreach (Hashtable associatedTag in associatedTags)
		{
			if (associatedTag["type"].ToString() == type && associatedTag["tag_cat"].ToString() == columnName && associatedTag["id"].ToString() == id)
			{
				text = ((text != null) ? (text + ", " + associatedTag["tag_value"].ToString()) : associatedTag["tag_value"].ToString());
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit column value = " + text);
		}
		return text;
	}

	public static List<Hashtable> GetTagColumnNames(ref List<Hashtable> associatedTags, string type)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started type =" + type);
		}
		List<Hashtable> list = new List<Hashtable>();
		foreach (Hashtable associatedTag in associatedTags)
		{
			if (!(associatedTag["type"].ToString() == type))
			{
				continue;
			}
			Hashtable hashtable = new Hashtable();
			hashtable["Name"] = associatedTag["tag_cat"].ToString();
			hashtable["Desc"] = associatedTag["tag_desc"].ToString();
			bool flag = false;
			foreach (Hashtable item in list)
			{
				if (item["Name"].ToString() == associatedTag["tag_cat"].ToString())
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				list.Add(hashtable);
				if (logger.IsDebugEnabled)
				{
					logger.Debug(methodBase.Name + " columnNames.Add" + associatedTag["tag_cat"].ToString());
				}
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
		return list;
	}

	public static string GetCISSessionId()
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		string text = null;
		string url = "https://" + Server + "/rest/com/vmware/cis/session";
		try
		{
			text = (string?)JObject.Parse(CallCISRestAPI(null, url, "POST"))["value"];
		}
		catch (Exception ex)
		{
			throw ex;
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
		return text;
	}

	public static string GetListOfCategoryIDs(string cisSession)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started cisSession = " + cisSession);
		}
		string url = "https://" + Server + "/rest/com/vmware/cis/tagging/category";
		string text = null;
		if (cisSession == null)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Exit cisSession = null");
			}
			return null;
		}
		try
		{
			text = CallCISRestAPI(cisSession, url, "GET");
		}
		catch (Exception ex)
		{
			throw ex;
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
		return text;
	}

	public static string GetCategoryName(string cisSession, string catid)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started cisSession = " + cisSession + " catid = " + catid);
		}
		string url = "https://" + Server + "/rest/com/vmware/cis/tagging/category/id:" + catid;
		string text = null;
		if (cisSession == null)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Exit cisSession = null");
			}
			return null;
		}
		try
		{
			text = CallCISRestAPI(cisSession, url, "GET");
		}
		catch (Exception ex)
		{
			throw ex;
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
		return text;
	}

	public static string GetListOfTags(string cisSession)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started cisSession = " + cisSession);
		}
		string url = "https://" + Server + "/rest/com/vmware/cis/tagging/tag";
		string text = null;
		if (cisSession == null)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Exit cisSession = null");
			}
			return null;
		}
		try
		{
			text = CallCISRestAPI(cisSession, url, "GET");
		}
		catch (Exception ex)
		{
			throw ex;
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
		return text;
	}

	public static string GetTagName(string cisSession, string tagID)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started tagID = " + tagID);
		}
		string url = "https://" + Server + "/rest/com/vmware/cis/tagging/tag/id:" + tagID;
		string text = null;
		if (cisSession == null)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Exit cisSession = null");
			}
			return null;
		}
		try
		{
			text = CallCISRestAPI(cisSession, url, "GET");
		}
		catch (Exception ex)
		{
			throw ex;
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
		return text;
	}

	public static string GetAssociatedObjects(string cisSession, string tagID)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for cissession" + cisSession + " tag-id " + tagID);
		}
		string url = "https://" + Server + "/rest/com/vmware/cis/tagging/tag-association/id:" + tagID + "?~action=list-attached-objects";
		string text = null;
		if (cisSession == null)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Exit cisSession = null");
			}
			return null;
		}
		try
		{
			text = CallCISRestAPI(cisSession, url, "POST");
		}
		catch (Exception ex)
		{
			throw ex;
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
		return text;
	}

	public static string CallCISRestAPI(string cisSession, string url, string requestMethod)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for url " + requestMethod + " " + url);
		}
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
		if (Password != null && Password.Length != 0)
		{
			httpWebRequest.Credentials = new NetworkCredential(Username, Password);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " use _Username and Password");
			}
		}
		else
		{
			httpWebRequest.UseDefaultCredentials = true;
			httpWebRequest.ImpersonationLevel = TokenImpersonationLevel.Impersonation;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " use default credentials");
			}
		}
		httpWebRequest.ServerCertificateValidationCallback = (RemoteCertificateValidationCallback)Delegate.Combine(httpWebRequest.ServerCertificateValidationCallback, new RemoteCertificateValidationCallback(ValidateServerCertificate));
		httpWebRequest.Method = requestMethod;
		httpWebRequest.Headers.Add("vmware-api-session-id", cisSession);
		httpWebRequest.UserAgent = "RVTools";
		httpWebRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
		string result = string.Empty;
		HttpWebResponse httpWebResponse = null;
		try
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " try response = (HttpWebResponse)request.GetResponse(); ");
			}
			httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " http response code " + httpWebResponse.StatusCode);
			}
			using Stream stream = httpWebResponse.GetResponseStream();
			using StreamReader streamReader = new StreamReader(stream);
			result = streamReader.ReadToEnd();
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " GetResponse error " + ex.Message);
			}
			throw ex;
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
		return result;
	}

	public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
		return true;
	}

	public static void QueryResourcePoolInventory()
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		try
		{
			TraversalSpec traversalSpec = new TraversalSpec();
			traversalSpec.name = "rpToVm";
			traversalSpec.type = "ResourcePool";
			traversalSpec.path = "vm";
			traversalSpec.skip = false;
			traversalSpec.selectSet = new SelectionSpec[1]
			{
				new SelectionSpec()
			};
			traversalSpec.selectSet[0].name = "visitFolders";
			TraversalSpec traversalSpec2 = new TraversalSpec();
			traversalSpec2.name = "rpToRp";
			traversalSpec2.type = "ResourcePool";
			traversalSpec2.path = "resourcePool";
			traversalSpec2.skip = false;
			traversalSpec2.selectSet = new SelectionSpec[1]
			{
				new SelectionSpec()
			};
			traversalSpec2.selectSet[0].name = "visitFolders";
			TraversalSpec traversalSpec3 = new TraversalSpec();
			traversalSpec3.name = "crToRp";
			traversalSpec3.type = "ComputeResource";
			traversalSpec3.path = "resourcePool";
			traversalSpec3.skip = false;
			traversalSpec3.selectSet = new SelectionSpec[1]
			{
				new SelectionSpec()
			};
			traversalSpec3.selectSet[0].name = "visitFolders";
			TraversalSpec traversalSpec4 = new TraversalSpec();
			traversalSpec4.name = "crToH";
			traversalSpec4.type = "ComputeResource";
			traversalSpec4.path = "host";
			traversalSpec4.skip = false;
			traversalSpec4.selectSet = new SelectionSpec[1]
			{
				new SelectionSpec()
			};
			traversalSpec4.selectSet[0].name = "visitFolders";
			TraversalSpec traversalSpec5 = new TraversalSpec();
			traversalSpec5.name = "dcToHf";
			traversalSpec5.type = "Datacenter";
			traversalSpec5.path = "hostFolder";
			traversalSpec5.skip = false;
			traversalSpec5.selectSet = new SelectionSpec[1]
			{
				new SelectionSpec()
			};
			traversalSpec5.selectSet[0].name = "visitFolders";
			TraversalSpec traversalSpec6 = new TraversalSpec();
			traversalSpec6.name = "dcToVmf";
			traversalSpec6.type = "Datacenter";
			traversalSpec6.path = "vmFolder";
			traversalSpec6.skip = false;
			traversalSpec6.selectSet = new SelectionSpec[1]
			{
				new SelectionSpec()
			};
			traversalSpec6.selectSet[0].name = "visitFolders";
			TraversalSpec traversalSpec7 = new TraversalSpec();
			traversalSpec7.name = "HToVm";
			traversalSpec7.type = "HostSystem";
			traversalSpec7.path = "vm";
			traversalSpec7.skip = false;
			traversalSpec7.selectSet = new SelectionSpec[1]
			{
				new SelectionSpec()
			};
			traversalSpec7.selectSet[0].name = "visitFolders";
			TraversalSpec traversalSpec8 = new TraversalSpec();
			traversalSpec8.name = "visitFolders";
			traversalSpec8.type = "Folder";
			traversalSpec8.path = "childEntity";
			traversalSpec8.skip = false;
			traversalSpec8.selectSet = new SelectionSpec[4]
			{
				new SelectionSpec(),
				traversalSpec5,
				traversalSpec3,
				traversalSpec2
			};
			traversalSpec8.selectSet[0].name = traversalSpec8.name;
			ObjectSpec objectSpec = new ObjectSpec();
			objectSpec.obj = _sic.rootFolder;
			objectSpec.skip = true;
			objectSpec.selectSet = new SelectionSpec[1] { traversalSpec8 };
			PropertySpec propertySpec = new PropertySpec();
			propertySpec.type = "ResourcePool";
			propertySpec.all = false;
			propertySpec.allSpecified = true;
			propertySpec.pathSet = new string[6] { "name", "owner", "parent", "resourcePool", "summary", "vm" };
			PropertySpec[] propSet = new PropertySpec[1] { propertySpec };
			PropertyFilterSpec propertyFilterSpec = new PropertyFilterSpec();
			propertyFilterSpec.objectSet = new ObjectSpec[1] { objectSpec };
			propertyFilterSpec.propSet = propSet;
			PropertyFilterSpec[] specs = new PropertyFilterSpec[1] { propertyFilterSpec };
			ManagedObjectReference managedObjectReference = new ManagedObjectReference();
			managedObjectReference.type = "ServiceInstance";
			managedObjectReference.Value = "ServiceInstance";
			ObjectContent[] array = RetrievePropertiesEx(_service.RetrieveServiceContent(managedObjectReference).propertyCollector, specs);
			_ResourcePoolInventory.Clear();
			for (int i = 0; i < array.Length; i++)
			{
				_ResourcePoolInventory.Add(array[i]);
			}
			for (int j = 0; j < _ResourcePoolInventory.Count; j++)
			{
				DynamicProperty[] propSet2 = ((ObjectContent)_ResourcePoolInventory[j]).propSet;
				foreach (DynamicProperty dynamicProperty in propSet2)
				{
					if (!(dynamicProperty.name == "resourcePool"))
					{
						continue;
					}
					ManagedObjectReference[] array2 = (ManagedObjectReference[])dynamicProperty.val;
					for (int l = 0; l < array2.Length; l++)
					{
						ObjectContent[] resourcePoolChilds = GetResourcePoolChilds(array2[l]);
						foreach (ObjectContent value in resourcePoolChilds)
						{
							_ResourcePoolInventory.Add(value);
						}
					}
				}
			}
			StringCollection stringCollection = new StringCollection();
			for (int n = 0; n < _ResourcePoolInventory.Count; n++)
			{
				ObjectContent objectContent = (ObjectContent)_ResourcePoolInventory[n];
				DynamicProperty[] propSet3 = objectContent.propSet;
				stringCollection.Clear();
				DynamicProperty[] propSet2 = propSet3;
				foreach (DynamicProperty dynamicProperty2 in propSet2)
				{
					if (dynamicProperty2.name == "name")
					{
						stringCollection.Add(dynamicProperty2.val.ToString());
					}
				}
				propSet2 = propSet3;
				foreach (DynamicProperty dynamicProperty3 in propSet2)
				{
					if (!(dynamicProperty3.name == "parent"))
					{
						continue;
					}
					ManagedObjectReference mor = (ManagedObjectReference)dynamicProperty3.val;
					bool flag = true;
					int num = 0;
					do
					{
						ObjectContent[] resourcePoolChilds = GetResourcePoolParent(mor);
						for (int l = 0; l < resourcePoolChilds.Length; l++)
						{
							DynamicProperty[] propSet4 = resourcePoolChilds[l].propSet;
							flag = false;
							for (int num2 = 0; num2 < propSet4.Length; num2++)
							{
								if (propSet4[num2].name == "name")
								{
									stringCollection.Add(propSet4[num2].val.ToString());
								}
								if (propSet4[num2].name == "parent")
								{
									mor = (ManagedObjectReference)propSet4[num2].val;
									flag = true;
								}
							}
						}
						num++;
						if (num > 9)
						{
							flag = false;
						}
					}
					while (flag);
				}
				stringCollection.Add(objectContent.obj.Value + "=");
				string text = null;
				for (int num3 = stringCollection.Count - 1; num3 >= 0; num3--)
				{
					if (!(stringCollection[num3] == "host") && !(stringCollection[num3] == "Datacenters"))
					{
						text = text + "/" + stringCollection[num3];
					}
				}
				text = text.TrimStart('/');
				_ResourcePool.Add(text);
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
	}

	public static ObjectContent[] GetResourcePoolChilds(ManagedObjectReference mor)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started for mor " + mor.Value);
		}
		ObjectContent[] result = null;
		try
		{
			ManagedObjectReference managedObjectReference = new ManagedObjectReference();
			managedObjectReference.type = mor.type;
			managedObjectReference.Value = mor.Value;
			result = GetObjectProperties(_sic.propertyCollector, managedObjectReference, new string[6] { "name", "owner", "parent", "resourcePool", "summary", "vm" });
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit ObjectContent[]");
		}
		return result;
	}

	public static void QueryHostHierarchy()
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		try
		{
			TraversalSpec traversalSpec = new TraversalSpec();
			traversalSpec.type = "ComputeResource";
			traversalSpec.skip = false;
			traversalSpec.path = "host";
			traversalSpec.selectSet = new SelectionSpec[1]
			{
				new SelectionSpec()
			};
			traversalSpec.selectSet[0].name = "traverseChild";
			TraversalSpec traversalSpec2 = new TraversalSpec();
			traversalSpec2.type = "ClusterComputeResource";
			traversalSpec2.skip = false;
			traversalSpec2.path = "host";
			traversalSpec2.selectSet = new SelectionSpec[1]
			{
				new SelectionSpec()
			};
			traversalSpec2.selectSet[0].name = "traverseChild";
			TraversalSpec traversalSpec3 = new TraversalSpec();
			traversalSpec3.type = "Datacenter";
			traversalSpec3.skip = false;
			traversalSpec3.path = "hostFolder";
			traversalSpec3.selectSet = new SelectionSpec[1]
			{
				new SelectionSpec()
			};
			traversalSpec3.selectSet[0].name = "traverseChild";
			TraversalSpec traversalSpec4 = new TraversalSpec();
			traversalSpec4.type = "Folder";
			traversalSpec4.skip = false;
			traversalSpec4.path = "childEntity";
			traversalSpec4.name = "traverseChild";
			traversalSpec4.selectSet = new SelectionSpec[4]
			{
				new SelectionSpec(),
				traversalSpec,
				traversalSpec2,
				traversalSpec3
			};
			traversalSpec4.selectSet[0].name = traversalSpec4.name;
			ObjectSpec objectSpec = new ObjectSpec();
			objectSpec.obj = _sic.rootFolder;
			objectSpec.skip = true;
			objectSpec.selectSet = new SelectionSpec[1] { traversalSpec4 };
			PropertySpec propertySpec = new PropertySpec();
			propertySpec.type = "ManagedEntity";
			propertySpec.all = true;
			propertySpec.allSpecified = false;
			propertySpec.pathSet = new string[2] { "name", "parent" };
			PropertySpec[] propSet = new PropertySpec[1] { propertySpec };
			PropertyFilterSpec propertyFilterSpec = new PropertyFilterSpec();
			propertyFilterSpec.objectSet = new ObjectSpec[1] { objectSpec };
			propertyFilterSpec.propSet = propSet;
			PropertyFilterSpec[] specs = new PropertyFilterSpec[1] { propertyFilterSpec };
			ManagedObjectReference managedObjectReference = new ManagedObjectReference();
			managedObjectReference.type = "ServiceInstance";
			managedObjectReference.Value = "ServiceInstance";
			_HostHierarchy = RetrievePropertiesEx(_service.RetrieveServiceContent(managedObjectReference).propertyCollector, specs);
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
	}

	public static void QueryFolderHierarchy()
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		try
		{
			for (int i = 0; i < InventoryCounter; i++)
			{
				GetVMFolder(i);
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
	}

	public static object[] WaitForValues(ManagedObjectReference objmor, string[] filterProps, string[] endWaitProps, object[][] expectedVals)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Start " + objmor.Value);
		}
		object[] array = new object[filterProps.Length];
		try
		{
			string version = "";
			object[] array2 = new object[endWaitProps.Length];
			PropertyFilterSpec propertyFilterSpec = new PropertyFilterSpec();
			propertyFilterSpec.objectSet = new ObjectSpec[1]
			{
				new ObjectSpec()
			};
			propertyFilterSpec.objectSet[0].obj = objmor;
			propertyFilterSpec.propSet = new PropertySpec[1]
			{
				new PropertySpec()
			};
			propertyFilterSpec.propSet[0].pathSet = filterProps;
			propertyFilterSpec.propSet[0].type = objmor.type;
			propertyFilterSpec.objectSet[0].selectSet = null;
			propertyFilterSpec.objectSet[0].skip = false;
			propertyFilterSpec.objectSet[0].skipSpecified = true;
			ManagedObjectReference managedObjectReference = _service.CreateFilter(_sic.propertyCollector, propertyFilterSpec, partialUpdates: true);
			bool flag = false;
			UpdateSet updateSet = null;
			PropertyFilterUpdate[] array3 = null;
			ObjectUpdate[] array4 = null;
			ObjectUpdate objectUpdate = null;
			PropertyChange[] array5 = null;
			PropertyChange propertyChange = null;
			while (!flag)
			{
				updateSet = _service.WaitForUpdates(_sic.propertyCollector, version);
				version = updateSet.version;
				if (updateSet == null || updateSet.filterSet == null)
				{
					Thread.Sleep(100);
					continue;
				}
				array3 = updateSet.filterSet;
				for (int i = 0; i < array3.Length; i++)
				{
					array4 = array3[i].objectSet;
					objectUpdate = null;
					array5 = null;
					for (int j = 0; j < array4.Length; j++)
					{
						objectUpdate = array4[j];
						if (objectUpdate.kind == ObjectUpdateKind.modify || objectUpdate.kind == ObjectUpdateKind.enter || objectUpdate.kind == ObjectUpdateKind.leave)
						{
							array5 = objectUpdate.changeSet;
							for (int k = 0; k < array5.Length; k++)
							{
								propertyChange = array5[k];
								UpdateValues(endWaitProps, array2, propertyChange);
								UpdateValues(filterProps, array, propertyChange);
							}
						}
					}
				}
				object obj = null;
				for (int l = 0; l < array2.Length; l++)
				{
					if (flag)
					{
						break;
					}
					for (int m = 0; m < expectedVals[l].Length; m++)
					{
						if (flag)
						{
							break;
						}
						obj = expectedVals[l][m];
						if (logger.IsDebugEnabled)
						{
							logger.Debug(methodBase.Name + " " + objmor.Value + " Expected value: " + obj.ToString() + " current value: " + array2[l].ToString());
						}
						flag = obj.Equals(array2[l]) | flag;
						if (logger.IsDebugEnabled)
						{
							logger.Debug(methodBase.Name + " " + objmor.Value + " Reached: " + flag);
						}
					}
				}
			}
			_service.DestroyPropertyFilter(managedObjectReference);
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " " + objmor.Value + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " " + objmor.Value + " Exit object[]");
		}
		return array;
	}

	protected static void UpdateValues(string[] props, object[] vals, PropertyChange propchg)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		try
		{
			for (int i = 0; i < props.Length; i++)
			{
				if (propchg.name.LastIndexOf(props[i]) >= 0)
				{
					if (propchg.op == PropertyChangeOp.remove)
					{
						vals[i] = "";
					}
					else
					{
						vals[i] = propchg.val;
					}
				}
			}
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
	}

	protected static ObjectContent[] GetObjectProperties(ManagedObjectReference collector, ManagedObjectReference mobj, string[] properties)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " " + mobj.Value.ToString() + " Started");
		}
		PropertyFilterSpec propertyFilterSpec = new PropertyFilterSpec();
		try
		{
			if (mobj == null)
			{
				return null;
			}
			if (collector == null)
			{
				return null;
			}
			propertyFilterSpec.propSet = new PropertySpec[1]
			{
				new PropertySpec()
			};
			propertyFilterSpec.propSet[0].all = properties == null || properties.Length == 0;
			propertyFilterSpec.propSet[0].allSpecified = propertyFilterSpec.propSet[0].all;
			propertyFilterSpec.propSet[0].type = mobj.type;
			propertyFilterSpec.propSet[0].pathSet = properties;
			propertyFilterSpec.objectSet = new ObjectSpec[1]
			{
				new ObjectSpec()
			};
			propertyFilterSpec.objectSet[0].obj = mobj;
			propertyFilterSpec.objectSet[0].skip = false;
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " " + mobj.Value.ToString() + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " " + mobj.Value.ToString() + " Exit ObjectContent[]");
		}
		return RetrievePropertiesEx(collector, new PropertyFilterSpec[1] { propertyFilterSpec });
	}

	public static void QueryCustomFields()
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		try
		{
			PropertySpec propertySpec = new PropertySpec();
			propertySpec.type = "CustomFieldsManager";
			propertySpec.pathSet = new string[1] { "field" };
			ObjectSpec objectSpec = new ObjectSpec();
			objectSpec.obj = _sic.customFieldsManager;
			PropertyFilterSpec propertyFilterSpec = new PropertyFilterSpec();
			propertyFilterSpec.propSet = new PropertySpec[1] { propertySpec };
			propertyFilterSpec.objectSet = new ObjectSpec[1] { objectSpec };
			PropertyFilterSpec[] specs = new PropertyFilterSpec[1] { propertyFilterSpec };
			ManagedObjectReference managedObjectReference = new ManagedObjectReference();
			managedObjectReference.type = "ServiceInstance";
			managedObjectReference.Value = "ServiceInstance";
			_CustomFields = RetrievePropertiesEx(_service.RetrieveServiceContent(managedObjectReference).propertyCollector, specs);
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
	}

	public static void QueryLicenseInformation()
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		try
		{
			PropertySpec propertySpec = new PropertySpec();
			propertySpec.type = "LicenseManager";
			propertySpec.pathSet = new string[2] { "licenses", "licenseAssignmentManager" };
			ObjectSpec objectSpec = new ObjectSpec();
			objectSpec.obj = _sic.licenseManager;
			PropertyFilterSpec propertyFilterSpec = new PropertyFilterSpec();
			propertyFilterSpec.propSet = new PropertySpec[1] { propertySpec };
			propertyFilterSpec.objectSet = new ObjectSpec[1] { objectSpec };
			PropertyFilterSpec[] specs = new PropertyFilterSpec[1] { propertyFilterSpec };
			ManagedObjectReference managedObjectReference = new ManagedObjectReference();
			managedObjectReference.type = "ServiceInstance";
			managedObjectReference.Value = "ServiceInstance";
			_LicenseInventory = RetrievePropertiesEx(_service.RetrieveServiceContent(managedObjectReference).propertyCollector, specs);
		}
		catch (Exception ex)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
	}

	public static void Connect(bool UseWindowsCredentials)
	{
		ServicePointManager.ServerCertificateValidationCallback = TrustAllCertificatePolicy.Validate;
		ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		try
		{
			Connected = false;
			if (_service != null)
			{
				Disconnect();
			}
			string text = "https://" + Server + "/sdk";
			string text2 = null;
			try
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(methodBase.Name + " Check if server can be found");
				}
				IPHostEntry hostEntry = Dns.GetHostEntry(Server);
				text2 = "host/" + hostEntry.HostName;
				if (logger.IsDebugEnabled)
				{
					logger.Debug(methodBase.Name + " Server found: " + text2);
				}
			}
			catch (Exception ex)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(methodBase.Name + " Server not found " + ex.Message);
				}
				text2 = "host/" + Server;
			}
			_service = GetVimService(text);
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " >> RVTools version " + Assembly.GetExecutingAssembly().GetName().Version.ToString());
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " >> Computer name " + Environment.MachineName);
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " >> OS Version " + Environment.OSVersion.VersionString);
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " >> Service URL " + text);
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " >> Use Windows Credentials " + UseWindowsCredentials);
			}
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " >> User Name " + Username);
			}
			ServicePointManager.ServerCertificateValidationCallback = CertificateValidation;
			_svcRef = new ManagedObjectReference();
			_svcRef.type = "ServiceInstance";
			_svcRef.Value = "ServiceInstance";
			try
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(methodBase.Name + " RetrieveServiceContent");
				}
				_sic = _service.RetrieveServiceContent(_svcRef);
				if (logger.IsDebugEnabled)
				{
					logger.Debug(methodBase.Name + " RetrieveServiceContent retrieved");
				}
			}
			catch (Exception ex2)
			{
				Connected = false;
				if (ex2.Message.Contains("Unsupported namespace"))
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + "RetrieveServiceContent: Unsupported namespace");
					}
					if (!StartedCommandFromCLI)
					{
						UserNotification.ReportError("Sorry but this application only supports:\n\n- ESX Server 3.5\n- VirtualCenter 2.5\n- ESX Server 3i\n- ESX Server 4i\n- vSphere 4\n- vSphere 5\n- vSphere 6", ex2, MessageBoxIcon.Exclamation);
					}
				}
				else
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " RetrieveServiceContent: " + ex2.Message);
					}
					if (!StartedCommandFromCLI)
					{
						UserNotification.ReportError("Could not connect to " + Server, ex2);
					}
				}
				return;
			}
			string[] array = new string[3] { "Negotiate", "Kerberos", "NTLM" };
			if (UseWindowsCredentials)
			{
				string[] array2 = array;
				foreach (string text3 in array2)
				{
					try
					{
						ClientConnection clientConnection = new ClientConnection();
						if (logger.IsDebugEnabled)
						{
							logger.Debug(methodBase.Name + " ConnectBySspi: " + text3 + " " + Server + " " + text2);
						}
						clientConnection.ConnectBySspi(text3, Server, text2);
						if (logger.IsDebugEnabled)
						{
							logger.Debug(methodBase.Name + " Connected " + text2);
						}
						session = clientConnection._session;
						_sic = clientConnection.Sic;
						_service = clientConnection.VimService;
						Connected = true;
						if (logger.IsDebugEnabled)
						{
							logger.Debug(methodBase.Name + " Connected = true, break foreach loop" + text2);
						}
					}
					catch (Exception ex3)
					{
						if (logger.IsDebugEnabled)
						{
							logger.Debug(methodBase.Name + " ConnectBySspi " + ex3.Message);
						}
						continue;
					}
					break;
				}
				array2 = array;
				foreach (string text4 in array2)
				{
					try
					{
						ClientConnection clientConnection2 = new ClientConnection();
						if (logger.IsDebugEnabled)
						{
							logger.Debug(methodBase.Name + " ConnectBySspiCustomException: " + text4 + " " + Server + " " + text2);
						}
						clientConnection2.ConnectBySspiCustomException(text4, Server, text2);
						if (logger.IsDebugEnabled)
						{
							logger.Debug(methodBase.Name + " Connected " + text2);
						}
						session = clientConnection2._session;
						_sic = clientConnection2.Sic;
						_service = clientConnection2.VimService;
						Connected = true;
						if (logger.IsDebugEnabled)
						{
							logger.Debug(methodBase.Name + " Connected = true, break foreach loop" + text2);
						}
					}
					catch (Exception ex4)
					{
						if (logger.IsDebugEnabled)
						{
							logger.Debug(methodBase.Name + " ConnectBySspiCustomException " + ex4.Message);
						}
						continue;
					}
					break;
				}
				if (!Connected)
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " SSPILogin failed:");
					}
					if (!StartedCommandFromCLI)
					{
						UserNotification.ReportError("Could not logon with your Windows credentials. Please enter your username and password. Sorry for this.");
					}
					return;
				}
			}
			else
			{
				if (string.IsNullOrWhiteSpace(Username))
				{
					UserNotification.ReportError("Can not complete login due to no username specified");
					return;
				}
				try
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " User name and password are entered. Use these to logon");
					}
					session = _service.Login(_sic.sessionManager, Username, new NetworkCredential(null, Password).Password, null);
				}
				catch (Exception ex5)
				{
					Connected = false;
					if (logger.IsDebugEnabled)
					{
						logger.Debug(methodBase.Name + " Login failed: " + ex5.Message);
					}
					if (!StartedCommandFromCLI)
					{
						UserNotification.ReportError("Login Failed", ex5);
					}
					return;
				}
			}
			Connected = true;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + "Connected to SDK web service");
			}
			APIVersion = GetAPIVersion();
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " >> API Version " + APIVersion);
			}
		}
		catch (Exception ex6)
		{
			Connected = false;
			if (logger.IsDebugEnabled)
			{
				logger.Debug(methodBase.Name + " Not connected to SDK, Unhandled exception:" + ex6.Message);
			}
			if (!StartedCommandFromCLI)
			{
				UserNotification.ReportError("Not connected to SDK", ex6);
			}
			return;
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
	}

	private static VimPortType GetVimService(string url, string username = null, string password = null, X509Certificate2 signingCertificate = null, XmlElement rawToken = null)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		CustomBinding wcfBinding = SamlTokenHelper.GetWcfBinding();
		wcfBinding.Name = "RVTools";
		TimeSpan receiveTimeout = (wcfBinding.SendTimeout = new TimeSpan(0, 25, 0));
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " >> Service SendTimeout    " + wcfBinding.SendTimeout.ToString());
		}
		wcfBinding.ReceiveTimeout = receiveTimeout;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " >> Service ReceiveTimeout " + wcfBinding.ReceiveTimeout.ToString());
		}
		EndpointAddress remoteAddress = new EndpointAddress(url);
		ChannelFactory<VimPortType> channelFactory = new ChannelFactory<VimPortType>(wcfBinding, remoteAddress);
		channelFactory.Endpoint.Behaviors.Add(new WsTrustBehavior(rawToken));
		SamlTokenHelper.SetCredentials(username, password, signingCertificate, channelFactory.Credentials);
		VimPortType result = channelFactory.CreateChannel();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
		return result;
	}

	private static bool CertificateValidation(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors errors)
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit true");
		}
		return true;
	}

	public static void Disconnect()
	{
		MethodBase methodBase = null;
		if (logger.IsDebugEnabled)
		{
			methodBase = MethodBase.GetCurrentMethod();
			logger.Debug(methodBase.Name + " Started");
		}
		if (_service != null)
		{
			try
			{
				_service.Logout(_sic.sessionManager);
			}
			catch (Exception ex)
			{
				if (logger.IsDebugEnabled)
				{
					logger.Debug(methodBase.Name + " Handled Exception: " + ex.Message);
				}
			}
			_service = null;
			_sic = null;
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(methodBase.Name + " Exit");
		}
	}
}
