using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using BackupDotNet.Core;
using BackupDotNet.Core.Backup;
using BackupDotNet.Deploy;
using BackupDotNet.Manage;

namespace BackupDotNet.Web;

public class WebHost
{
	private readonly Settings _settings;

	private readonly ConfigLoader _config;

	private readonly DataStore _store;

	private readonly Scheduler _scheduler;

	private readonly Auth _auth;

	private readonly AuditLog _audit;

	private HttpListener _listener;

	private Thread _thread;

	private volatile bool _stopping;

	public string ActualPrefix = "";

	public readonly AgentRegistry Agents;

	public readonly DeployManager Deploy;

	public readonly OpQueue Ops;

	public readonly BackupJobQueue Backups;

	public WebHost(Settings settings, ConfigLoader config, DataStore store, Scheduler scheduler, Auth auth, AuditLog audit)
	{
		_settings = settings;
		_config = config;
		_store = store;
		_scheduler = scheduler;
		_auth = auth;
		_audit = audit;
		Agents = new AgentRegistry(settings.ConfigDir);
		Agents.Load();
		foreach (string err in Agents.LoadErrors)
		{
			Log.Warn("Агенты: " + err);
		}
		Deploy = new DeployManager(settings, config, Agents);
		Ops = new OpQueue(settings, config);
		Backups = new BackupJobQueue(settings);
	}

	public void Start()
	{
		List<string> candidates = new List<string>();
		foreach (string p in new string[3]
		{
			_settings.BindAddress,
			"http://+:" + _settings.HttpPort + "/",
			"http://localhost:" + _settings.HttpPort + "/"
		})
		{
			if (!string.IsNullOrEmpty(p) && !candidates.Contains(p))
			{
				candidates.Add(p);
			}
		}
		Exception last = null;
		foreach (string prefix in candidates)
		{
			try
			{
				HttpListener l = new HttpListener();
				l.Prefixes.Add(prefix);
				l.Start();
				_listener = l;
				ActualPrefix = prefix;
				Log.Info("HTTP-сервер слушает " + prefix);
			}
			catch (Exception ex)
			{
				last = ex;
				Log.Warn("Не удалось занять " + prefix + ": " + ex.Message);
				continue;
			}
			break;
		}
		if (_listener == null)
		{
			throw new Exception("Не удалось запустить HTTP-сервер. Скорее всего нужен urlacl: netsh http add urlacl url=http://+:" + _settings.HttpPort + "/ user=\"NT AUTHORITY\\SYSTEM\". Ошибка: " + ((last == null) ? "" : last.Message));
		}
		_stopping = false;
		_thread = new Thread(Loop);
		_thread.IsBackground = true;
		_thread.Name = "web";
		_thread.Start();
		Backups.Start();
	}

	public void Stop()
	{
		_stopping = true;
		try
		{
			Backups.Stop();
		}
		catch
		{
		}
		try
		{
			if (_listener != null)
			{
				_listener.Stop();
			}
		}
		catch
		{
		}
		try
		{
			if (_listener != null)
			{
				_listener.Close();
			}
		}
		catch
		{
		}
	}

	private void Loop()
	{
		while (!_stopping)
		{
			HttpListenerContext ctx = null;
			try
			{
				ctx = _listener.GetContext();
			}
			catch
			{
				if (_stopping)
				{
					break;
				}
				continue;
			}
			HttpListenerContext captured = ctx;
			ThreadPool.QueueUserWorkItem((object param0) =>
			{
				Handle(captured);
			});
		}
	}

	private void Handle(HttpListenerContext ctx)
	{
		try
		{
			string path = ctx.Request.Url.AbsolutePath;
			if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
			{
				new ApiHandler(_settings, _config, _store, _scheduler, _auth, _audit, Agents, Deploy, Ops, ActualPrefix, Backups).Handle(ctx);
			}
			else
			{
				ServeStatic(ctx, path);
			}
		}
		catch (Exception ex)
		{
			Log.Error("HTTP " + ctx.Request.Url, ex);
			try
			{
				byte[] body = Encoding.UTF8.GetBytes("{\"error\":\"" + ex.Message.Replace("\"", "'") + "\"}");
				ctx.Response.StatusCode = 500;
				ctx.Response.ContentType = "application/json; charset=utf-8";
				ctx.Response.OutputStream.Write(body, 0, body.Length);
				ctx.Response.Close();
			}
			catch
			{
			}
		}
	}

	private void ServeStatic(HttpListenerContext ctx, string path)
	{
		string root = Path.GetFullPath(_settings.PublicDir);
		if (path == "/" || path.Length == 0)
		{
			path = "/index.html";
		}
		string rel = path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
		string full = Path.GetFullPath(Path.Combine(root, rel));
		if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
		{
			ctx.Response.StatusCode = 403;
			ctx.Response.Close();
			return;
		}
		if (!File.Exists(full))
		{
			string index = Path.Combine(root, "index.html");
			if (!File.Exists(index) || path.IndexOf('.') >= 0)
			{
				ctx.Response.StatusCode = 404;
				ctx.Response.Close();
				return;
			}
			full = index;
		}
		byte[] data = File.ReadAllBytes(full);
		ctx.Response.ContentType = ContentType(Path.GetExtension(full));
		ctx.Response.ContentLength64 = data.Length;
		ctx.Response.Headers["Cache-Control"] = "no-cache";
		ctx.Response.OutputStream.Write(data, 0, data.Length);
		ctx.Response.Close();
	}

	private static string ContentType(string ext)
	{
		switch ((ext ?? "").ToLowerInvariant())
		{
		case ".html":
		case ".htm":
			return "text/html; charset=utf-8";
		case ".css":
			return "text/css; charset=utf-8";
		case ".js":
			return "application/javascript; charset=utf-8";
		case ".json":
			return "application/json; charset=utf-8";
		case ".png":
			return "image/png";
		case ".jpg":
		case ".jpeg":
			return "image/jpeg";
		case ".svg":
			return "image/svg+xml";
		case ".ico":
			return "image/x-icon";
		case ".woff":
			return "font/woff";
		case ".woff2":
			return "font/woff2";
		case ".ttf":
			return "font/ttf";
		case ".map":
			return "application/json";
		default:
			return "application/octet-stream";
		}
	}
}
