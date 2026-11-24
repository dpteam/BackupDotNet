using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace BackupDotNet.Core.Update;

/// <summary>
/// Обновления из GitHub Releases.
///
/// Почему отдельно от локального каталога: локальный источник работает в контуре
/// без интернета и остаётся основным. GitHub — дополнительный, для машин, у
/// которых интернет есть. Включается явно (`updates.source = "github"`).
///
/// Что здесь сознательно не делается:
///  • <b>установка не запускается сама</b> — подмена файлов работающего процесса
///    на Windows почти гарантированно даёт сломанный агент;
///  • <b>архив проверяется до распаковки</b> — контрольная сумма от GitHub,
///    если она есть, и обязательное наличие exe внутри;
///  • <b>прежняя версия сохраняется</b> — откат должен быть дешёвым.
/// </summary>
public static class GitHubUpdater
{
	private const string DefaultApiBase = "https://api.github.com";

	/// <summary>Адрес API: настройка или публичный GitHub.</summary>
	private static string ApiBaseOf(Settings settings)
	{
		string b = (settings == null) ? "" : (settings.UpdatesGitHubApiBase ?? "").Trim();
		if (b.Length == 0)
		{
			return DefaultApiBase;
		}
		return b.TrimEnd('/');
	}

	/// <summary>Результат проверки релиза.</summary>
	public class Release
	{
		public string Version = "";

		public string TagName = "";

		public string Notes = "";

		public DateTime? PublishedAt;

		public string AssetName = "";

		public string DownloadUrl = "";

		public long SizeBytes;

		/// <summary>Контрольная сумма от GitHub в виде <c>sha256:...</c>, если есть.</summary>
		public string Digest = "";

		public bool Prerelease;

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["version"] = Version;
			d["tagName"] = TagName;
			d["notes"] = Notes;
			d["publishedAt"] = PublishedAt;
			d["assetName"] = AssetName;
			d["downloadUrl"] = DownloadUrl;
			d["sizeBytes"] = SizeBytes;
			d["digest"] = Digest;
			d["prerelease"] = Prerelease;
			return d;
		}
	}

	/// <summary>Состояние загрузки и установки.</summary>
	public class State
	{
		public string Status = "idle"; // idle | checking | downloading | ready | installing | error

		public int Progress;

		public string Message = "";

		public string Error = "";

		public string DownloadedPath = "";

		public string StagedPath = "";

		public Release Latest;

		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> d = Json.Obj();
			d["status"] = Status;
			d["progress"] = Progress;
			d["message"] = Message;
			d["error"] = Error;
			d["downloadedPath"] = DownloadedPath;
			d["stagedPath"] = StagedPath;
			if (Latest != null)
			{
				d["latest"] = Latest.ToJson();
			}
			return d;
		}
	}

	private static readonly object Sync = new object();

	private static State _state = new State();

	public static State Current
	{
		get
		{
			lock (Sync)
			{
				return _state;
			}
		}
	}

	public static void Reset()
	{
		lock (Sync)
		{
			_state = new State();
		}
	}

	// ------------------------------------------------------------------ проверка

	/// <summary>
	/// Проверка обновлений при старте — в фоне. Запуск службы не ждёт сеть:
	/// без интернета агент просто пишет предупреждение в журнал. Результат
	/// попадает в <see cref="Current"/>, поэтому интерфейс сразу показывает
	/// доступную версию.
	/// </summary>
	public static void StartBootCheck(Settings settings, string currentVersion)
	{
		if (settings == null || !settings.UpdatesEnabled || !settings.UpdatesCheckOnStart)
		{
			return;
		}
		if (string.IsNullOrWhiteSpace(settings.UpdatesGitHubRepo))
		{
			return;
		}

		Thread t = new Thread(() =>
		{
			try
			{
				Release r = Check(settings, currentVersion, out string error);
				if (error.Length > 0)
				{
					Log.Warn("Проверка обновлений при старте: " + error);
					return;
				}
				if (r == null)
				{
					return;
				}
				if (UpdateChecker.Compare(r.Version, currentVersion) > 0)
				{
					Log.Info("Доступна новая версия " + r.Version + " (установлена " + currentVersion + ")");
				}
				else
				{
					Log.Info("Обновлений нет: " + currentVersion + " — последняя версия");
				}
			}
			catch (Exception ex)
			{
				Log.Warn("Проверка обновлений при старте: " + ex.Message);
			}
		});
		t.IsBackground = true;
		t.Name = "update-boot-check";
		t.Start();
	}

	/// <summary>
	/// Читает последний релиз из GitHub. Ничего не скачивает.
	/// </summary>
	public static Release Check(Settings settings, string currentVersion, out string error)
	{
		error = "";
		if (settings == null || !settings.UpdatesEnabled)
		{
			error = "проверка обновлений выключена (updates.enabled = false)";
			return null;
		}
		string repo = (settings.UpdatesGitHubRepo ?? "").Trim().Trim('/');
		if (repo.Length == 0)
		{
			error = "не задан репозиторий (updates.githubRepo), например dpteam/BackupDotNet";
			return null;
		}
		if (repo.Split('/').Length != 2)
		{
			error = "репозиторий должен быть в виде владелец/название";
			return null;
		}

		lock (Sync)
		{
			_state = new State { Status = "checking", Message = "запрашиваю релиз…" };
		}

		try
		{
			string url = ApiBaseOf(settings) + "/repos/" + repo + "/releases/latest";
			string body = HttpGet(url, settings, out string contentType);
			Dictionary<string, object> root = Json.ParseObject(body);
			if (root.Count == 0)
			{
				error = "GitHub вернул пустой ответ";
				return null;
			}

			Release r = new Release();
			r.TagName = Json.GetStr(root, "tag_name", "");
			r.Version = NormalizeVersion(r.TagName);
			r.Notes = TrimNotes(Json.GetStr(root, "body", ""));
			r.Prerelease = Json.Bool(Json.Get(root, "prerelease"), dflt: false);

			string published = Json.GetStr(root, "published_at", "");
			if (published.Length > 0 &&
				DateTime.TryParse(published, CultureInfo.InvariantCulture,
					DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
			{
				r.PublishedAt = dt.ToLocalTime();
			}

			// Ищем архив в приложениях релиза: первый .zip.
			List<object> assets = Json.AsArray(Json.Get(root, "assets"));
			foreach (object o in assets)
			{
				Dictionary<string, object> a = Json.AsObject(o);
				string name = Json.GetStr(a, "name", "");
				if (!name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				r.AssetName = name;
				r.DownloadUrl = Json.GetStr(a, "browser_download_url", "");
				r.SizeBytes = Json.Long(Json.Get(a, "size"), 0L);
				r.Digest = Json.GetStr(a, "digest", "");
				break;
			}

			if (r.AssetName.Length == 0)
			{
				error = "в релизе " + r.TagName + " нет ZIP-архива";
				return null;
			}

			lock (Sync)
			{
				_state.Status = "idle";
				_state.Message = "последний релиз: " + r.Version;
				_state.Latest = r;
			}
			return r;
		}
		catch (Exception ex)
		{
			error = DescribeHttpError(ex);
			lock (Sync)
			{
				_state.Status = "error";
				_state.Error = error;
			}
			return null;
		}
	}

	// ---------------------------------------------------------------- скачивание

	/// <summary>
	/// Скачивает архив релиза в каталог обновлений. Возвращает путь к файлу.
	/// </summary>
	public static string Download(Settings settings, Release release, out string error)
	{
		error = "";
		if (release == null || release.DownloadUrl.Length == 0)
		{
			error = "нет ссылки на архив";
			return null;
		}

		string dir = Path.Combine(settings.DataDir, "updates", "download");
		try
		{
			Directory.CreateDirectory(dir);
		}
		catch (Exception ex)
		{
			error = "не удалось создать каталог загрузок: " + ex.Message;
			return null;
		}

		string path = Path.Combine(dir, SafeFileName(release.AssetName));
		lock (Sync)
		{
			_state.Status = "downloading";
			_state.Progress = 0;
			_state.Message = "скачиваю " + release.AssetName + "…";
			_state.Error = "";
			_state.Latest = release;
		}

		try
		{
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
			HttpWebRequest req = (HttpWebRequest)WebRequest.Create(release.DownloadUrl);
			req.Method = "GET";
			req.Timeout = 60000;
			req.ReadWriteTimeout = 60000;
			req.UserAgent = Branding.Current.ProductName + "-Updater";
			req.AllowAutoRedirect = true;
			string token = ResolveToken(settings);
			if (token.Length > 0)
			{
				// Для приватных репозиториев GitHub отдаёт 404 без токена.
				req.Headers["Authorization"] = "Bearer " + token;
			}

			using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
			using (Stream net = resp.GetResponseStream())
			using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				long total = resp.ContentLength > 0 ? resp.ContentLength : release.SizeBytes;
				byte[] buffer = new byte[81920];
				long done = 0L;
				int n;
				DateTime lastReport = DateTime.MinValue;
				while ((n = net.Read(buffer, 0, buffer.Length)) > 0)
				{
					fs.Write(buffer, 0, n);
					done += n;
					if (total > 0 && (DateTime.Now - lastReport).TotalMilliseconds > 200)
					{
						lastReport = DateTime.Now;
						int pct = (int)Math.Min(99.0, done * 100.0 / total);
						lock (Sync)
						{
							_state.Progress = pct;
							_state.Message = "скачано " + Fmt.Size(done) + " из " + Fmt.Size(total);
						}
					}
				}
			}

			// --- проверка целостности до того, как файл будет распакован
			string verifyError = Verify(path, release);
			if (verifyError.Length > 0)
			{
				try
				{
					File.Delete(path);
				}
				catch
				{
				}
				error = verifyError;
				lock (Sync)
				{
					_state.Status = "error";
					_state.Error = verifyError;
				}
				return null;
			}

			lock (Sync)
			{
				_state.Status = "ready";
				_state.Progress = 100;
				_state.Message = "архив скачан и проверен";
				_state.DownloadedPath = path;
			}
			Log.Info("Обновление: скачан " + release.AssetName + " (" + Fmt.Size(new FileInfo(path).Length) + ")");
			return path;
		}
		catch (Exception ex)
		{
			error = DescribeHttpError(ex);
			lock (Sync)
			{
				_state.Status = "error";
				_state.Error = error;
			}
			return null;
		}
	}

	/// <summary>
	/// Проверяет архив: контрольная сумма (если GitHub её дал) и наличие exe внутри.
	/// </summary>
	private static string Verify(string path, Release release)
	{
		try
		{
			if (!File.Exists(path))
			{
				return "файл не скачался";
			}
			long size = new FileInfo(path).Length;
			if (size < 1024)
			{
				return "архив подозрительно мал: " + size + " байт";
			}

			// Контрольная сумма: GitHub отдаёт её в поле digest для новых релизов.
			if (!string.IsNullOrEmpty(release.Digest) &&
				release.Digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
			{
				string expected = release.Digest.Substring("sha256:".Length).Trim().ToLowerInvariant();
				string actual = Sha256Of(path);
				if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
				{
					return "контрольная сумма не совпала: ожидалась " + Short(expected) + ", получена " + Short(actual);
				}
				Log.Info("Обновление: контрольная сумма совпала");
			}
			else
			{
				Log.Warn("Обновление: GitHub не предоставил контрольную сумму, проверяю только структуру архива");
			}

			// Структура: архив обязан содержать exe, иначе это не наш релиз.
			using System.IO.Compression.ZipArchive zip =
				System.IO.Compression.ZipFile.OpenRead(path);
			bool hasExe = false;
			bool hasPublic = false;
			foreach (System.IO.Compression.ZipArchiveEntry e in zip.Entries)
			{
				string name = e.FullName.Replace('\\', '/').ToLowerInvariant();
				if (name.EndsWith("/backupdotnet.exe") || name == "backupdotnet.exe")
				{
					hasExe = true;
				}
				if (name.Contains("public/index.html"))
				{
					hasPublic = true;
				}
			}
			if (!hasExe)
			{
				return "в архиве нет BackupDotNet.exe — это не релиз продукта";
			}
			if (!hasPublic)
			{
				return "в архиве нет public/index.html — релиз неполный";
			}
			return "";
		}
		catch (Exception ex)
		{
			return "архив повреждён: " + ex.Message;
		}
	}

	// ------------------------------------------------------------------ HTTP

	private static string HttpGet(string url, Settings settings, out string contentType)
	{
		ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
		HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
		req.Method = "GET";
		req.Timeout = 30000;
		req.ReadWriteTimeout = 30000;
		req.UserAgent = Branding.Current.ProductName + "-Updater";
		req.Accept = "application/vnd.github+json";
		req.Headers["X-GitHub-Api-Version"] = "2022-11-28";
		string token = ResolveToken(settings);
		if (token.Length > 0)
		{
			req.Headers["Authorization"] = "Bearer " + token;
		}
		using HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
		contentType = resp.ContentType ?? "";
		using StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8);
		return sr.ReadToEnd();
	}

	private static string ResolveToken(Settings settings)
	{
		if (settings == null)
		{
			return "";
		}
		return (settings.UpdatesGitHubToken ?? "").Trim();
	}

	/// <summary>
	/// Превращает исключение HTTP в понятную фразу: «нет сети» и «репозиторий
	/// не найден» требуют совершенно разных действий.
	/// </summary>
	private static string DescribeHttpError(Exception ex)
	{
		if (ex is WebException wex)
		{
			if (wex.Response is HttpWebResponse resp)
			{
				int code = (int)resp.StatusCode;
				switch (code)
				{
				case 401:
					return "GitHub: нужен токен (401). Приватный репозиторий?";
				case 403:
					return "GitHub: доступ запрещён (403). Анонимный лимит — 60 запросов в час на IP; " +
						"либо он исчерпан (подождите до сброса), либо нужен токен в updates.githubTokenSecret.";
				case 404:
					return "GitHub: репозиторий или релиз не найден (404). Проверьте updates.githubRepo.";
				default:
					return "GitHub ответил HTTP " + code;
				}
			}
			switch (wex.Status)
			{
			case WebExceptionStatus.NameResolutionFailure:
			case WebExceptionStatus.ConnectFailure:
			case WebExceptionStatus.ProxyNameResolutionFailure:
				return "нет связи с github.com — проверьте доступ в интернет или прокси";
			case WebExceptionStatus.Timeout:
				return "таймаут: github.com не ответил вовремя";
			case WebExceptionStatus.TrustFailure:
				return "не удалось проверить сертификат github.com";
			default:
				return wex.Message;
			}
		}
		return ex.Message;
	}

	private static string Sha256Of(string path)
	{
		using SHA256 sha = SHA256.Create();
		using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		byte[] hash = sha.ComputeHash(fs);
		StringBuilder sb = new StringBuilder(hash.Length * 2);
		foreach (byte b in hash)
		{
			sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
		}
		return sb.ToString();
	}

	private static string Short(string s)
	{
		return (s.Length > 12) ? s.Substring(0, 12) + "…" : s;
	}

	/// <summary>Убирает префикс «v» из тега: v1.2.0 → 1.2.0.</summary>
	private static string NormalizeVersion(string tag)
	{
		string v = (tag ?? "").Trim();
		if (v.StartsWith("v", StringComparison.OrdinalIgnoreCase) ||
			v.StartsWith("V", StringComparison.OrdinalIgnoreCase))
		{
			v = v.Substring(1);
		}
		return v;
	}

	/// <summary>Тело релиза может быть огромным — оставляем первые 4000 символов.</summary>
	private static string TrimNotes(string body)
	{
		if (string.IsNullOrEmpty(body))
		{
			return "";
		}
		string s = body.Trim();
		if (s.Length <= 4000)
		{
			return s;
		}
		return s.Substring(0, 4000) + "\n… (заметки обрезаны)";
	}

	private static string SafeFileName(string name)
	{
		string baseName = Path.GetFileName(name ?? "update.zip");
		if (baseName.Length == 0)
		{
			baseName = "update.zip";
		}
		StringBuilder sb = new StringBuilder(baseName.Length);
		char[] invalid = Path.GetInvalidFileNameChars();
		foreach (char ch in baseName)
		{
			sb.Append(Array.IndexOf(invalid, ch) >= 0 ? '_' : ch);
		}
		return sb.ToString();
	}
}
