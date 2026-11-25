using System;
using System.Collections.Generic;
using System.IO;

using BackupDotNet.Core;
namespace BackupDotNet.Setup;

/// <summary>
/// Поиск установленного WebView2 Runtime.
///
/// Загрузчик WebView2 (WebView2Loader.dll) в комплект не входит: это внешний
/// бинарник, а в проекте разрешено только то, что уже есть в Windows. Поэтому
/// ищем сам рантайм — он ставится вместе с Edge и живёт в
/// <c>%ProgramFiles(x86)%\Microsoft\EdgeWebView\Application\&lt;версия&gt;\EBWebView\x64\</c>.
///
/// Рантайм экспортирует <c>CreateWebViewEnvironmentWithOptionsInternal</c>,
/// которого достаточно для создания окружения.
/// </summary>
public static class WebView2Runtime
{
	/// <summary>Найденный рантайм.</summary>
	public class Found
	{
		public string Version = "";

		/// <summary>Полный путь к EmbeddedBrowserWebView.dll.</summary>
		public string LibraryPath = "";

		/// <summary>Каталог версии — из него берётся msedgewebview2.exe.</summary>
		public string Folder = "";

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["version"] = Version;
			d["libraryPath"] = LibraryPath;
			d["folder"] = Folder;
			return d;
		}
	}

	/// <summary>
	/// Ищет самый свежий установленный рантайм. Возвращает null, если его нет.
	/// </summary>
	public static Found Locate()
	{
		foreach (string root in CandidateRoots())
		{
			Found f = ScanRoot(root);
			if (f != null)
			{
				return f;
			}
		}
		return null;
	}

	/// <summary>Все места, где может лежать рантайм.</summary>
	private static List<string> CandidateRoots()
	{
		List<string> roots = new List<string>();

		// Основное место: Program Files (x86) — так ставит установщик Edge.
		string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
		if (!string.IsNullOrEmpty(pf86))
		{
			roots.Add(Path.Combine(pf86, "Microsoft", "EdgeWebView", "Application"));
		}

		// Рядом с Edge — на случай нестандартной установки.
		string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
		if (!string.IsNullOrEmpty(pf))
		{
			roots.Add(Path.Combine(pf, "Microsoft", "EdgeWebView", "Application"));
		}

		// Пользовательская установка (не требует прав администратора).
		string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		if (!string.IsNullOrEmpty(local))
		{
			roots.Add(Path.Combine(local, "Microsoft", "EdgeWebView", "Application"));
		}

		return roots;
	}

	private static Found ScanRoot(string root)
	{
		if (!Directory.Exists(root))
		{
			return null;
		}

		Found best = null;
		Version bestVer = null;

		try
		{
			foreach (string dir in Directory.GetDirectories(root))
			{
				string name = Path.GetFileName(dir);
				if (!Version.TryParse(name, out Version v))
				{
					// Папки вроде SetupMetrics — не рантайм.
					continue;
				}

				// 64-битный рантайм: наш установщик тоже x64.
				string lib = Path.Combine(dir, "EBWebView", "x64", "EmbeddedBrowserWebView.dll");
				if (!File.Exists(lib))
				{
					continue;
				}

				if (bestVer == null || v > bestVer)
				{
					bestVer = v;
					best = new Found { Version = name, LibraryPath = lib, Folder = dir };
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Поиск WebView2: " + ex.Message);
		}

		return best;
	}

	/// <summary>
	/// Есть ли Edge — нужен как запасной вариант, если WebView2 не заведётся.
	/// </summary>
	public static string FindEdge()
	{
		string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
		string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

		foreach (string baseDir in new[] { pf86, pf })
		{
			if (string.IsNullOrEmpty(baseDir))
			{
				continue;
			}
			string root = Path.Combine(baseDir, "Microsoft", "Edge", "Application");
			if (!Directory.Exists(root))
			{
				continue;
			}
			try
			{
				// Самая свежая версия: папки вида 158.0.4370.33.
				string best = null;
				Version bestVer = null;
				foreach (string dir in Directory.GetDirectories(root))
				{
					if (!Version.TryParse(Path.GetFileName(dir), out Version v))
					{
						continue;
					}
					string exe = Path.Combine(dir, "msedge.exe");
					if (File.Exists(exe) && (bestVer == null || v > bestVer))
					{
						bestVer = v;
						best = exe;
					}
				}
				if (best != null)
				{
					return best;
				}
			}
			catch
			{
			}
		}
		return "";
	}
}
