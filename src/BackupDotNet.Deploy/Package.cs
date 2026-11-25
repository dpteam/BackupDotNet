using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using BackupDotNet.Core;

namespace BackupDotNet.Deploy;

public static class Package
{
	private static readonly string[] ToolFiles = new string[3] { "plink.exe", "ipmitool.exe", "smartctl.exe" };

	/// <summary>
	/// Сборки, без которых BackupDotNet.exe не запускается. Список явный,
	/// а не «все dll из каталога»: в каталоге могут оказаться чужие файлы,
	/// и тащить их на каждую машину незачем.
	/// </summary>
	private static readonly string[] AgentAssemblies = new string[]
	{
		"BackupDotNet.Core.dll",
		"BackupDotNet.Web.dll",
		"BackupDotNet.Deploy.dll",
		"BackupDotNet.Manage.dll"
	};

	public static string Build(string rootDir, Settings settings, string agentId, string centralUrl, string token, string hostName, out long sizeBytes, out string error)
	{
		sizeBytes = 0L;
		error = "";
		try
		{
			string exe = ((Assembly.GetEntryAssembly() != null) ? Assembly.GetEntryAssembly().Location : Path.Combine(rootDir, "BackupDotNet.exe"));
			if (!File.Exists(exe))
			{
				error = "не найден BackupDotNet.exe (" + exe + ")";
				return "";
			}
			string zipPath = Path.Combine(Path.GetTempPath(), "bc-deploy-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".zip");
			if (File.Exists(zipPath))
			{
				File.Delete(zipPath);
			}
			using (FileStream fs = new FileStream(zipPath, FileMode.CreateNew, FileAccess.Write))
			{
				using ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Create);
				AddFile(zip, exe, "BackupDotNet.exe");

				// Все зависимости рядом с exe: без них он не запускается, а
				// служба падает сразу после старта с «не удалось запустить
				// службу». Раньше в пакет клался только сам exe — 47 КБ
				// вместо 730 КБ, и агент не мог загрузить ни ядро, ни веб.
				string exeDir = Path.GetDirectoryName(exe);
				foreach (string name in AgentAssemblies)
				{
					string path = Path.Combine(exeDir, name);
					if (File.Exists(path))
					{
						AddFile(zip, path, name);
					}
				}
				// .config читается CLR до запуска: без него отличаются
				// настройки привязок и кодировка консоли.
				string cfg = exe + ".config";
				if (File.Exists(cfg))
				{
					AddFile(zip, cfg, "BackupDotNet.exe.config");
				}

				AddTree(zip, settings.PublicDir, "public");
				AddTree(zip, Path.Combine(rootDir, "templates"), "templates");
				string toolsDir = Path.Combine(rootDir, "tools");
				string[] toolFiles = ToolFiles;
				foreach (string name in toolFiles)
				{
					string path = Path.Combine(toolsDir, name);
					if (File.Exists(path))
					{
						AddFile(zip, path, "tools/" + name);
					}
				}
				AddText(zip, "config/settings.json", SettingsJson(hostName));
				AddText(zip, "config/remote.json", RemoteJson(agentId, centralUrl, token));
				string brandingPath = Path.Combine(rootDir, "config", "branding.json");
				if (File.Exists(brandingPath))
				{
					AddFile(zip, brandingPath, "config/branding.json");
				}
				AddText(zip, "config/env.json", EnvJson());
			}
			FileInfo fi = new FileInfo(zipPath);
			sizeBytes = fi.Length;
			return zipPath;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return "";
		}
	}

	private static string EnvJson()
	{
		// Только имя удалённой службы. Каталоги здесь задавать нельзя:
		// env.json перебивает settings.json, и агент начинал писать в
		// ProgramData сервера — на той же машине это прямой конфликт
		// с серверной службой (два процесса на один каталог данных).
		Branding b = Branding.Current;
		Dictionary<string, object> brand = Json.Obj();
		brand["serviceNameRemote"] = b.ServiceNameRemote;
		Dictionary<string, object> root = Json.Obj();
		root["brand"] = brand;
		return Json.WritePretty(root);
	}

	private static string SettingsJson(string hostName)
	{
		// Каталог данных отдельный от серверного: агент живёт на другой машине
		// и не должен писать в ProgramData сервера, если это та же машина.
		Branding b = Branding.Current;
		string dataDir = string.IsNullOrWhiteSpace(b.DataDir)
			? @"C:\ProgramData\BackupDotNetAgent"
			: b.DataDir.TrimEnd('\\') + "Agent";

		StringBuilder sb = new StringBuilder();
		sb.AppendLine("{");
		sb.AppendLine("  \"agent\": {");
		sb.AppendLine("    \"name\": " + Q(hostName) + ",");
		sb.AppendLine("    \"dataDir\": " + Q(dataDir) + ",");
		sb.AppendLine("    \"timezone\": \"Europe/Moscow\",");
		sb.AppendLine("    \"language\": \"ru\"");
		sb.AppendLine("  },");
		sb.AppendLine("  \"scheduler\": { \"tickSeconds\": 60, \"jitterSeconds\": 30 },");
		sb.AppendLine("  \"logging\": { \"level\": \"info\", \"keepDays\": 30 },");
		sb.AppendLine("  \"zabbix\": { \"enabled\": false }");
		sb.AppendLine("}");
		return sb.ToString();
	}

	private static string RemoteJson(string agentId, string centralUrl, string token)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("{");
		sb.AppendLine("  \"centralUrl\": " + Q((centralUrl ?? "").TrimEnd('/')) + ",");
		sb.AppendLine("  \"token\": " + Q(token) + ",");
		sb.AppendLine("  \"agentId\": " + Q(agentId) + ",");
		sb.AppendLine("  \"intervalSeconds\": 300,");
		sb.AppendLine("  \"timeoutSeconds\": 60,");
		sb.AppendLine("  \"ignoreCertErrors\": true");
		sb.AppendLine("}");
		return sb.ToString();
	}

	private static string Q(string s)
	{
		if (s == null)
		{
			s = "";
		}
		return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
	}

	private static void AddText(ZipArchive zip, string entryName, string content)
	{
		ZipArchiveEntry e = zip.CreateEntry(entryName, CompressionLevel.Optimal);
		using Stream s = e.Open();
		byte[] bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(content);
		s.Write(bytes, 0, bytes.Length);
	}

	private static void AddFile(ZipArchive zip, string path, string entryName)
	{
		zip.CreateEntryFromFile(path, entryName, CompressionLevel.Optimal);
	}

	private static void AddTree(ZipArchive zip, string dir, string prefix)
	{
		if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
		{
			return;
		}
		string[] files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
		foreach (string file in files)
		{
			string rel = file.Substring(dir.Length).TrimStart('\\', '/').Replace('\\', '/');
			if (!rel.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase))
			{
				AddFile(zip, file, prefix + "/" + rel);
			}
		}
	}
}
