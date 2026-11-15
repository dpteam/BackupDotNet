using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace HyperVBackUp.Engine;

public class CSV
{
	[DllImport("ResUtils.dll", CharSet = CharSet.Auto)]
	private static extern bool ClusterIsPathOnSharedVolume(string lpszPathName);

	[DllImport("ResUtils.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern bool ClusterGetVolumePathName(string lpszFileName, StringBuilder lpszVolumePathName, int cchBufferLength);

	[DllImport("ResUtils.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern bool ClusterGetVolumeNameForVolumeMountPoint(string lpszVolumeMountPoint, StringBuilder lpszVolumeName, int cchBufferLength);

	[DllImport("ResUtils.dll", CharSet = CharSet.Auto)]
	private static extern int ClusterPrepareSharedVolumeForBackup(string lpszFileName, StringBuilder lpszVolumePathName, ref int lpcchVolumePathName, StringBuilder lpszVolumeName, ref int lpcchVolumeName);

	[DllImport("ResUtils.dll", CharSet = CharSet.Auto)]
	private static extern int ClusterClearBackupStateForSharedVolume(string lpszVolumePathName);

	public static bool IsSupported()
	{
		if (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major >= 6)
		{
			return Environment.OSVersion.Version.Minor >= 1;
		}
		return false;
	}

	public static bool IsPathOnSharedVolume(string path)
	{
		return ClusterIsPathOnSharedVolume(path);
	}

	public static string GetVolumeNameForMountPoint(string volumeMountPoint)
	{
		StringBuilder stringBuilder = new StringBuilder(2048);
		if (!ClusterGetVolumeNameForVolumeMountPoint(volumeMountPoint, stringBuilder, stringBuilder.Capacity))
		{
			throw new Win32Exception(Marshal.GetLastWin32Error());
		}
		return stringBuilder.ToString();
	}

	public static string GetVolumePath(string path)
	{
		StringBuilder stringBuilder = new StringBuilder(2048);
		if (!ClusterGetVolumePathName(path, stringBuilder, stringBuilder.Capacity))
		{
			throw new Win32Exception(Marshal.GetLastWin32Error());
		}
		return stringBuilder.ToString();
	}

	public static void ClusterPrepareSharedVolumeForBackup(string path, out string volumePath, out string volumeName)
	{
		StringBuilder stringBuilder = new StringBuilder(2048);
		StringBuilder stringBuilder2 = new StringBuilder(2048);
		int lpcchVolumePathName = stringBuilder.Capacity;
		int lpcchVolumeName = stringBuilder2.Capacity;
		int num = ClusterPrepareSharedVolumeForBackup(path, stringBuilder, ref lpcchVolumePathName, stringBuilder2, ref lpcchVolumeName);
		if (num != 0)
		{
			throw new Win32Exception(num);
		}
		volumePath = stringBuilder.ToString();
		volumeName = stringBuilder2.ToString();
	}
}
