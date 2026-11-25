using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;

using BackupDotNet.Core;
namespace BackupDotNet.Setup;

/// <summary>
/// Что мастеру нужно знать о машине до начала установки.
///
/// Собирается один раз при старте: это «факты», а не догадки. Если чего-то
/// нет (нет прав, занят порт, мало места) — мастер обязан сказать это до
/// первого изменения, а не после.
/// </summary>
public static class EnvProbe
{
	/// <summary>Сведения о среде.</summary>
	public static Dictionary<string, object> Collect()
	{
		Dictionary<string, object> d = Json.Obj();

		d["machineName"] = Environment.MachineName;
		d["userName"] = Environment.UserName;
		d["is64"] = Environment.Is64BitOperatingSystem;
		d["osVersion"] = Environment.OSVersion.VersionString;
		d["osBuild"] = Environment.OSVersion.Version.Build;
		d["isAdmin"] = IsAdmin();
		d["isDomainJoined"] = IsDomainJoined();
		d["dotnet"] = ClrVersion();
		d["processorCount"] = Environment.ProcessorCount;

		// WebView2: от него зависит, откроется ли окно мастера.
		WebView2Runtime.Found runtime = WebView2Runtime.Locate();
		Dictionary<string, object> webview = Json.Obj();
		webview["found"] = runtime != null;
		if (runtime != null)
		{
			webview["version"] = runtime.Version;
		}
		else
		{
			webview["note"] = "WebView2 Runtime не найден; мастер откроется в браузере";
		}
		d["webview2"] = webview;

		string edge = WebView2Runtime.FindEdge();
		Dictionary<string, object> edgeInfo = Json.Obj();
		edgeInfo["found"] = edge.Length > 0;
		if (edge.Length > 0)
		{
			edgeInfo["path"] = edge;
		}
		d["edge"] = edgeInfo;

		// Куда ставим по умолчанию.
		Dictionary<string, object> defaults = Json.Obj();
		defaults["installDir"] = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "DPT.Backup");
		defaults["dataDir"] = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DPT.Backup");
		defaults["serviceName"] = "DPTBackupService";
		defaults["port"] = 8080;
		d["defaults"] = defaults;

		// Что уже стоит на машине.
		d["existing"] = DetectExisting();

		// Диски: мастер предупредит, если места мало.
		d["drives"] = Drives();

		return d;
	}

	/// <summary>Права администратора: без них службу не создать.</summary>
	public static bool IsAdmin()
	{
		try
		{
			using WindowsIdentity identity = WindowsIdentity.GetCurrent();
			WindowsPrincipal principal = new WindowsPrincipal(identity);
			return principal.IsInRole(WindowsBuiltInRole.Administrator);
		}
		catch
		{
			return false;
		}
	}

	/// <summary>В домене ли машина: от этого зависит способ установки через AD.</summary>
	public static bool IsDomainJoined()
	{
		try
		{
			return System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties()
				.DomainName.Length > 0;
		}
		catch
		{
			return false;
		}
	}

	private static string ClrVersion()
	{
		try
		{
			return Environment.Version.ToString();
		}
		catch
		{
			return "";
		}
	}

	/// <summary>
	/// Есть ли уже установленный агент. Проверяем службу и каталог: одного
	/// признака мало — службу могли удалить, файлы оставить.
	/// </summary>
	private static Dictionary<string, object> DetectExisting()
	{
		Dictionary<string, object> d = Json.Obj();
		bool found = false;

		string[] serviceNames = { "DPTBackupService", "BackupDotNetService" };
		List<string> services = new List<string>();
		foreach (string name in serviceNames)
		{
			if (ServiceExists(name))
			{
				services.Add(name);
				found = true;
			}
		}
		d["services"] = services;

		string[] dirs =
		{
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "DPT.Backup"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "BackupDotNet"),
			@"C:\BackupDotNet"
		};
		List<string> folders = new List<string>();
		foreach (string dir in dirs)
		{
			try
			{
				if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, "BackupDotNet.exe")))
				{
					folders.Add(dir);
					found = true;
				}
			}
			catch
			{
			}
		}
		d["folders"] = folders;
		d["found"] = found;
		return d;
	}

	private static bool ServiceExists(string name)
	{
		try
		{
			using System.ServiceProcess.ServiceController sc = new System.ServiceProcess.ServiceController(name);
			// Обращение к Status заставляет службу ответить, если она есть.
			System.ServiceProcess.ServiceControllerStatus _ = sc.Status;
			return true;
		}
		catch (InvalidOperationException)
		{
			// Служба не найдена — это нормальный случай.
			return false;
		}
		catch
		{
			return false;
		}
	}

	private static List<object> Drives()
	{
		List<object> list = new List<object>();
		try
		{
			foreach (DriveInfo di in DriveInfo.GetDrives())
			{
				try
				{
					if (!di.IsReady || di.DriveType != DriveType.Fixed)
					{
						continue;
					}
					Dictionary<string, object> d = Json.Obj();
					d["name"] = di.Name;
					d["freeGb"] = Math.Round(di.AvailableFreeSpace / 1073741824.0, 1);
					d["totalGb"] = Math.Round(di.TotalSize / 1073741824.0, 1);
					list.Add(d);
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		return list;
	}
}
