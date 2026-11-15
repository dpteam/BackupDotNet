using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceProcess;
using BackupChecker.Core;
using BackupChecker.Web;

namespace BackupChecker;

public class AgentService : ServiceBase
{
	private readonly string _root;

	private Scheduler _scheduler;

	private WebHost _web;

	private DataStore _store;

	public AgentService(string root)
	{
		_root = root;
		ServiceName = Branding.Current.ServiceName;
		CanStop = true;
		CanPauseAndContinue = false;
		AutoLog = true;
	}

	protected override void OnStart(string[] args)
	{
		try
		{
			Program.Diag("OnStart begin");
			AgentCtx ctx = CreateContext();
			ConfigLoader cfg = ctx.Config;
			_store = ctx.Store;
			Program.Diag("OnStart ctx ok: companies=" + cfg.Companies.Count + " configDir=" + cfg.Settings.ConfigDir + " dataDir=" + cfg.Settings.DataDir + " errors=" + cfg.LoadErrors.Count);
			_scheduler = new Scheduler(ctx);
			_scheduler.Version = "1.0.0";
			_scheduler.Start();
			Auth auth = new Auth(cfg.Settings.ConfigDir, cfg.Settings.WebSessionMinutes);
			Program.Diag("OnStart auth ok: users=" + auth.UserCount + " generated=" + (auth.GeneratedPassword.Length > 0));
			if (auth.GeneratedPassword.Length > 0)
			{
				Log.Warn("Создан пользователь admin, пароль: " + auth.GeneratedPassword);
				Program.WriteGeneratedPassword(_root, auth.GeneratedPassword);
			}
			AuditLog audit = new AuditLog(cfg.Settings.DataDir);
			_web = new WebHost(cfg.Settings, cfg, _store, _scheduler, auth, audit);
			_web.Start();
			Program.Diag("OnStart web ok: " + _web.ActualPrefix);
			Log.Info("Служба запущена");
			Program.Diag("OnStart done");
		}
		catch (Exception ex)
		{
			Program.Diag("OnStart FAILED: " + ex.GetType().Name + ": " + ex.Message + " | " + ex.StackTrace);
			Log.Error("Не удалось запустить службу", ex);
			throw;
		}
	}

	private AgentCtx CreateContext()
	{
		ConfigLoader cfg = new ConfigLoader(_root);
		cfg.Load();
		Log.Init(Path.Combine(cfg.Settings.DataDir, "logs"), cfg.Settings.LogLevel, cfg.Settings.LogMaxMb, echo: false);
		DataStore store = new DataStore(cfg.Settings.DataDir);
		store.Load();
		List<string> knownIds = new List<string>();
		foreach (Company c in cfg.Companies)
		{
			knownIds.Add(c.Id);
		}
		store.PruneOrphans(knownIds);
		return new AgentCtx(cfg.Settings, cfg, store);
	}

	protected override void OnStop()
	{
		Log.Info("Остановка службы");
		try
		{
			if (_web != null)
			{
				_web.Stop();
			}
		}
		catch
		{
		}
		try
		{
			if (_scheduler != null)
			{
				_scheduler.Stop();
			}
		}
		catch
		{
		}
	}
}
