using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;

namespace RVTools.CustomerExperience;

public class TelemetryContext
{
	private struct MEMORYSTATUSEX
	{
		internal uint dwLength;

		internal uint dwMemoryLoad;

		internal ulong ullTotalPhys;

		internal ulong ullAvailPhys;

		internal ulong ullTotalPageFile;

		internal ulong ullAvailPageFile;

		internal ulong ullTotalVirtual;

		internal ulong ullAvailVirtual;

		internal ulong ullAvailExtendedVirtual;
	}

	public string AppName { get; }

	public string AppEdition { get; }

	public string AppEnvironment { get; }

	public string SessionId { get; } = Guid.NewGuid().ToString();

	public string InstanceUuid { get; }

	public string UserUuid { get; }

	public bool IsAdminUser { get; }

	public string CollectorId { get; }

	public string ExecutableName { get; }

	public string ExecutableVersion { get; }

	public string ExecutableTimestamp { get; }

	public string OperatingSystemName { get; }

	public long TotalMemory { get; }

	public long FreeMemory { get; }

	public long TotalDiskSpace { get; }

	public long FreeDiskSpace { get; }

	public string CpuName { get; }

	public int CpuCount { get; }

	public string CommandLineParameters { get; }

	public string Language { get; }

	public bool UserInteractive { get; }

	[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

	[DllImport("kernel32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetPhysicallyInstalledSystemMemory(out long totalMemoryInKilobytes);

	private static string HashString(string input)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(input);
		byte[] source = new SHA256Managed().ComputeHash(bytes);
		return string.Join("", source.Select((byte b) => b.ToString("x2")));
	}

	private static bool IsAdmin()
	{
		SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
		return new WindowsPrincipal(WindowsIdentity.GetCurrent()).UserClaims.Any((Claim x) => x.Value.Contains(sid.Value));
	}

	public TelemetryContext(string appName, string appEdition, string environment, string uniqueUserIdentifier, string collectorId)
	{
		AppName = appName;
		AppEdition = appEdition;
		AppEnvironment = environment;
		Language = CultureInfo.CurrentUICulture.Name;
		string hostName = Dns.GetHostName();
		string text = (from nic in NetworkInterface.GetAllNetworkInterfaces()
			where nic.OperationalStatus == OperationalStatus.Up
			select nic.GetPhysicalAddress().ToString()).FirstOrDefault();
		InstanceUuid = HashString("*" + hostName + "*" + text + "*");
		UserUuid = ((uniqueUserIdentifier == null) ? InstanceUuid : HashString(uniqueUserIdentifier));
		IsAdminUser = IsAdmin();
		CollectorId = collectorId;
		ProcessModule mainModule = Process.GetCurrentProcess().MainModule;
		if (mainModule != null)
		{
			ExecutableName = mainModule.ModuleName;
		}
		string text2 = ((!(Assembly.GetEntryAssembly() != null)) ? (Assembly.GetExecutingAssembly()?.Location ?? ".") : (Assembly.GetEntryAssembly()?.Location ?? "."));
		GetPhysicallyInstalledSystemMemory(out var totalMemoryInKilobytes);
		MEMORYSTATUSEX lpBuffer = new MEMORYSTATUSEX
		{
			dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX))
		};
		GlobalMemoryStatusEx(ref lpBuffer);
		ExecutableVersion = FileVersionInfo.GetVersionInfo(text2).FileVersion;
		ExecutableTimestamp = File.GetLastWriteTime(text2).ToString("yyyy-MM-dd HH:mm:ss");
		OperatingSystemName = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion")?.GetValue("productName").ToString();
		TotalMemory = totalMemoryInKilobytes * 1024;
		FreeMemory = (long)lpBuffer.ullAvailPhys;
		TotalDiskSpace = new DriveInfo(Environment.GetFolderPath(Environment.SpecialFolder.System)).TotalSize;
		FreeDiskSpace = new DriveInfo(Environment.GetFolderPath(Environment.SpecialFolder.System)).AvailableFreeSpace;
		CpuName = Registry.LocalMachine.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0")?.GetValue("ProcessorNameString").ToString();
		CpuCount = Environment.ProcessorCount;
		UserInteractive = Environment.UserInteractive;
	}

	public Dictionary<string, string> AsDictionary()
	{
		return GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).ToDictionary((PropertyInfo property) => property.Name, (PropertyInfo property) => property.GetValue(this, null)?.ToString());
	}
}
