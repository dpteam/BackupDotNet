using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace BackupDotNet.Core;

public static class RemoteRunner
{
	public class RemoteConfig
	{
		public string CentralUrl = "";

		public string Token = "";

		public string AgentId = "";

		public int IntervalSeconds = 300;

		public bool IgnoreCertErrors = true;

		public int TimeoutSeconds = 60;
	}

	public static int Run(string root)
	{
		string path = Path.Combine(root, "config", "remote.json");
		if (!Load(path, out var cfg, out var error))
		{
			Console.Error.WriteLine("Конфигурация удалённого агента: " + error);
			Console.Error.WriteLine("Ожидался файл: " + path);
			return 2;
		}
		Settings local = LocalSettings(root);
		Log.Init(Path.Combine(local.DataDir, "logs"), local.LogLevel, local.LogMaxMb,
			echo: Environment.UserInteractive, publicEnabled: local.LogPublicEnabled);
		Log.Cleanup(Path.Combine(local.DataDir, "logs"), local.LogKeepDays);
		Log.Info("Удалённый агент " + cfg.AgentId + " → " + cfg.CentralUrl);
		Console.WriteLine("Удалённый агент: " + cfg.AgentId);
		Console.WriteLine("Центральный:     " + cfg.CentralUrl);
		Console.WriteLine("Интервал:        " + cfg.IntervalSeconds + " с");
		Console.WriteLine();
		bool once = Array.IndexOf(Environment.GetCommandLineArgs(), "--once") > 0;
		int pollSeconds = Math.Max(15, Math.Min(60, cfg.IntervalSeconds));
		DateTime lastRun = DateTime.MinValue;
		bool first = true;
		while (true)
		{
			try
			{
				Dictionary<string, object> payload = FetchConfig(cfg);
				bool force = Json.Bool(Json.Get(payload, "runNow"));
				double sinceRun = (DateTime.Now - lastRun).TotalSeconds;
				bool due = sinceRun >= (double)Math.Max(30, cfg.IntervalSeconds);
				if (first || force || due)
				{
					if (force)
					{
						Console.WriteLine("Запрошен внеочередной прогон.");
					}
					RunWithConfig(root, cfg, payload);
					lastRun = DateTime.Now;
					first = false;
				}
			}
			catch (Exception ex)
			{
				Log.Warn("Цикл не удался: " + ex.Message);
				Console.WriteLine("ОШИБКА: " + ex.Message);
			}
			if (once)
			{
				break;
			}
			Thread.Sleep(pollSeconds * 1000);
		}
		return 0;
	}

	/// <summary>
	/// Настройки конкретной машины (только секция agent/logging) — чтобы удалённый
	/// агент писал журнал туда же, куда и обычный, и с тем же уровнем.
	/// </summary>
	private static Settings LocalSettings(string root)
	{
		try
		{
			string p = Path.Combine(root, "config", "settings.json");
			if (File.Exists(p))
			{
				Settings s = Settings.FromJson(Json.ParseObject(File.ReadAllText(p, Encoding.UTF8)));
				if (!Path.IsPathRooted(s.DataDir))
				{
					s.DataDir = Path.GetFullPath(Path.Combine(root, s.DataDir));
				}
				return s;
			}
		}
		catch
		{
		}
		return new Settings
		{
			DataDir = Path.Combine(root, "data")
		};
	}

	public static bool Load(string path, out RemoteConfig cfg, out string error)	{
		cfg = new RemoteConfig();
		error = "";
		if (!File.Exists(path))
		{
			error = "файл не найден";
			return false;
		}
		try
		{
			Dictionary<string, object> root = Json.ParseObject(File.ReadAllText(path, Encoding.UTF8));
			cfg.CentralUrl = Json.GetStr(root, "centralUrl", "").Trim().TrimEnd('/');
			cfg.Token = Json.GetStr(root, "token", "").Trim();
			cfg.AgentId = Json.GetStr(root, "agentId", "").Trim();
			cfg.IntervalSeconds = Json.Int(Json.Get(root, "intervalSeconds"), 300);
			cfg.IgnoreCertErrors = Json.Bool(Json.Get(root, "ignoreCertErrors"), dflt: true);
			cfg.TimeoutSeconds = Json.Int(Json.Get(root, "timeoutSeconds"), 60);
		}
		catch (Exception ex)
		{
			error = "некорректный JSON: " + ex.Message;
			return false;
		}
		if (cfg.CentralUrl.Length == 0)
		{
			error = "не задан centralUrl";
			return false;
		}
		if (cfg.Token.Length < 16)
		{
			error = "токен короче 16 символов";
			return false;
		}
		if (cfg.AgentId.Length == 0)
		{
			error = "не задан agentId";
			return false;
		}
		return true;
	}

	public static void RunCycle(string root, RemoteConfig cfg)
	{
		RunWithConfig(root, cfg, FetchConfig(cfg));
	}

	public static void RunWithConfig(string root, RemoteConfig cfg, Dictionary<string, object> payload)
	{
		List<object> companyNodes = Json.AsArray(Json.Get(payload, "companies"));
		if (companyNodes.Count == 0)
		{
			Console.WriteLine("Заданий нет (центральный агент не назначил компаний).");
			return;
		}
		ConfigLoader cfgLoader = new ConfigLoader(root);
		foreach (object o in Json.AsArray(Json.Get(payload, "secrets")))
		{
			Dictionary<string, object> s = Json.AsObject(o);
			cfgLoader.SetSecretInMemory(Json.GetStr(s, "key", ""), Json.GetStr(s, "value", ""));
		}
		AgentCtx ctx = new AgentCtx(new Settings(), cfgLoader, new DataStore(Path.Combine(root, "data")));
		List<object> results = new List<object>();
		int ok = 0;
		int failed = 0;
		foreach (object co in companyNodes)
		{
			Dictionary<string, object> cd = Json.AsObject(co);
			Company company;
			try
			{
				company = Company.FromJson(cd, "(remote)");
			}
			catch (Exception ex)
			{
				Console.WriteLine("  компания пропущена: " + ex.Message);
				continue;
			}
			Console.WriteLine("  " + company.Id + ": источников " + company.Sources.Count);
			foreach (Source source in company.Sources)
			{
				if (!source.Enabled)
				{
					continue;
				}
				ICollector collector = Collectors.Get(source.Type);
				if (collector == null)
				{
					Console.WriteLine("    " + source.Type + ": неизвестный тип");
					continue;
				}
				List<CollectResult> collected;
				try
				{
					collected = collector.Collect(company, source, ctx);
				}
				catch (Exception ex2)
				{
					CollectResult cr = new CollectResult();
					cr.Job = source.GetStr("jobName", source.Id);
					cr.Kind = source.Type;
					cr.SourceId = source.Id;
					cr.Reachable = false;
					cr.Fail(ex2.Message);
					collected = new List<CollectResult>();
					collected.Add(cr);
				}
				foreach (CollectResult cr2 in collected)
				{
					Dictionary<string, object> d = cr2.ToJson();
					d["company"] = company.Id;
					results.Add(d);
					if (cr2.Errors.Count > 0)
					{
						failed++;
					}
					else
					{
						ok++;
					}
					Console.WriteLine("    " + cr2.Job + ": " + ((cr2.Errors.Count > 0) ? ("ошибка — " + cr2.Message) : "ok"));
				}
			}
		}
		PostResults(cfg, results);
		Console.WriteLine("Итог: " + results.Count + " результатов (ошибок " + failed + ")");
	}

	private static Dictionary<string, object> FetchConfig(RemoteConfig cfg)
	{
		string url = cfg.CentralUrl + "/api/agent/config?token=" + Uri.EscapeDataString(cfg.Token) + "&agent=" + Uri.EscapeDataString(cfg.AgentId);
		string body = Http(cfg, url, null);
		Dictionary<string, object> root = Json.ParseObject(body);
		if (root.Count == 0)
		{
			throw new Exception("центральный агент вернул пустой ответ");
		}
		if (Json.Get(root, "error") != null)
		{
			throw new Exception("центральный агент: " + Json.GetStr(root, "error", "?"));
		}
		return root;
	}

	private static void PostResults(RemoteConfig cfg, List<object> results)
	{
		Dictionary<string, object> payload = Json.Obj();
		payload["agentId"] = cfg.AgentId;
		payload["version"] = "1.0.0";
		payload["results"] = results;
		string url = cfg.CentralUrl + "/api/agent/results?token=" + Uri.EscapeDataString(cfg.Token);
		string body = Http(cfg, url, Json.Write(payload));
		Dictionary<string, object> root = Json.ParseObject(body);
		if (Json.Get(root, "error") != null)
		{
			throw new Exception("центральный агент: " + Json.GetStr(root, "error", "?"));
		}
		int accepted = Json.Int(Json.Get(root, "accepted"));
		if (accepted != results.Count)
		{
			Log.Warn("Центральный агент принял " + accepted + " из " + results.Count);
		}
	}

	private static string Http(RemoteConfig cfg, string url, string postBody)
	{
		try
		{
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
			HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
			req.Method = ((postBody == null) ? "GET" : "POST");
			req.Timeout = cfg.TimeoutSeconds * 1000;
			req.ReadWriteTimeout = cfg.TimeoutSeconds * 1000;
			req.UserAgent = Branding.Current.UserAgent + "-Remote/1.0.0";
			req.KeepAlive = false;
			if (cfg.IgnoreCertErrors)
			{
				req.ServerCertificateValidationCallback = (object param0, X509Certificate param1, X509Chain param2, SslPolicyErrors param3) => true;
			}
			if (postBody != null)
			{
				byte[] data = Encoding.UTF8.GetBytes(postBody);
				req.ContentType = "application/json; charset=utf-8";
				req.ContentLength = data.Length;
				using Stream s = req.GetRequestStream();
				s.Write(data, 0, data.Length);
			}
			using HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
			using StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8);
			return sr.ReadToEnd();
		}
		catch (WebException ex)
		{
			if (ex.Response is HttpWebResponse resp2)
			{
				try
				{
					using StreamReader sr2 = new StreamReader(resp2.GetResponseStream(), Encoding.UTF8);
					string body = sr2.ReadToEnd();
					Dictionary<string, object> err = Json.ParseObject(body);
					string msg = Json.GetStr(err, "error", "");
					if (msg.Length > 0)
					{
						throw new Exception(msg);
					}
				}
				catch (Exception ex2)
				{
					if (ex2.Message.Length > 0 && !(ex2 is WebException))
					{
						throw;
					}
				}
				if (resp2.StatusCode == HttpStatusCode.Unauthorized)
				{
					throw new Exception("токен не принят (проверьте config/remote.json)");
				}
				throw new Exception("HTTP " + (int)resp2.StatusCode);
			}
			switch (ex.Status)
			{
			case WebExceptionStatus.Timeout:
				throw new Exception("таймаут — центральный агент не отвечает");
			case WebExceptionStatus.ConnectFailure:
				throw new Exception("не удалось подключиться к " + cfg.CentralUrl);
			default:
				throw new Exception(ex.Message);
			}
		}
	}
}
