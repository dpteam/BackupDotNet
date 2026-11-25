using System;
using System.Collections.Generic;
using System.IO;

using BackupDotNet.Core;
namespace BackupDotNet.Setup;

/// <summary>
/// Состояние мастера между запросами страницы.
///
/// Страница в WebView2 — обычный браузер: она общается с движком по HTTP,
/// поэтому всё, что введено, живёт здесь, а не в памяти страницы. Так мастер
/// переживает перезагрузку страницы и не теряет введённое.
/// </summary>
public sealed class SetupSession
{
	private readonly string _uiDir;

	private readonly object _sync = new object();

	public SetupSession(string uiDir)
	{
		_uiDir = uiDir;
	}

	/// <summary>Каталог, откуда берутся файлы агента.</summary>
	public string PayloadDir
	{
		get
		{
			// Сначала ищем payload рядом с установщиком, потом — файлы прямо
			// рядом (так удобно при отладке).
			string payload = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "payload");
			if (Directory.Exists(payload))
			{
				return payload;
			}
			return AppDomain.CurrentDomain.BaseDirectory;
		}
	}

	/// <summary>Есть ли что устанавливать.</summary>
	public bool PayloadReady
	{
		get
		{
			try
			{
				return File.Exists(Path.Combine(PayloadDir, "BackupDotNet.exe"));
			}
			catch
			{
				return false;
			}
		}
	}

	/// <summary>Версия устанавливаемого агента (из exe), если файлы на месте.</summary>
	public string PayloadVersion
	{
		get
		{
			try
			{
				string exe = Path.Combine(PayloadDir, "BackupDotNet.exe");
				if (!File.Exists(exe))
				{
					return "";
				}
				return System.Diagnostics.FileVersionInfo.GetVersionInfo(exe).FileVersion ?? "";
			}
			catch
			{
				return "";
			}
		}
	}

	public Dictionary<string, object> ToJson()
	{
		lock (_sync)
		{
			Dictionary<string, object> d = Json.Obj();
			d["setupVersion"] = Program.SetupVersion;
			d["payloadDir"] = PayloadDir;
			d["payloadReady"] = PayloadReady;
			d["payloadVersion"] = PayloadVersion;
			return d;
		}
	}
}
