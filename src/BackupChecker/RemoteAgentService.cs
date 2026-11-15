using System;
using System.ServiceProcess;
using System.Threading;
using BackupChecker.Core;

namespace BackupChecker;

public class RemoteAgentService : ServiceBase
{
	private readonly string _root;

	private Thread _thread;

	private volatile bool _stopping;

	public bool Stopping => _stopping;

	public RemoteAgentService(string root)
	{
		_root = root;
		ServiceName = Branding.Current.ServiceNameRemote;
		CanStop = true;
		CanPauseAndContinue = false;
		AutoLog = true;
	}

	protected override void OnStart(string[] args)
	{
		Program.Diag("RemoteAgentService OnStart");
		_stopping = false;
		_thread = new Thread(Loop);
		_thread.IsBackground = true;
		_thread.Name = "remote";
		_thread.Start();
	}

	private void Loop()
	{
		try
		{
			RemoteRunner.Run(_root);
		}
		catch (Exception ex)
		{
			Program.Diag("RemoteAgentService loop failed: " + ex.Message);
			Log.Error("Удалённый агент остановлен", ex);
		}
	}

	protected override void OnStop()
	{
		_stopping = true;
		Log.Info("Удалённый агент: остановка");
	}
}
