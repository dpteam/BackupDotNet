using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using BackupDotNet.Core;

namespace BackupDotNet.Setup;

/// <summary>
/// Фоновая установка с прогрессом для страницы мастера.
///
/// Установка занимает секунды, а HTTP-запрос не должен висеть: страница
/// запускает её и опрашивает состояние. Так прогресс виден, а браузер не
/// таймаутит на медленном диске.
/// </summary>
public sealed class InstallRunner
{
	private readonly object _sync = new object();

	private readonly StringBuilder _log = new StringBuilder();

	private string _step = "";

	private int _percent;

	private bool _done;

	private bool _ok;

	private SetupEngine.Result _result;

	/// <summary>Идёт ли установка прямо сейчас.</summary>
	public bool Busy
	{
		get
		{
			lock (_sync)
			{
				return !_done && _step.Length > 0;
			}
		}
	}

	/// <summary>Запускает установку в фоне. Повторный вызов игнорируется.</summary>
	public bool Start(SetupEngine.Request request)
	{
		lock (_sync)
		{
			if (!_done && _step.Length > 0)
			{
				return false;
			}
			_step = "Подготовка";
			_percent = 0;
			_done = false;
			_ok = false;
			_result = null;
			_log.Clear();
		}

		Thread t = new Thread(delegate ()
		{
			try
			{
				SetupEngine engine = new SetupEngine();
				SetupEngine.Result result = engine.InstallLocal(request, delegate (string step, int percent)
				{
					lock (_sync)
					{
						_step = step;
						_percent = percent;
						_log.AppendLine("[" + percent.ToString().PadLeft(3) + "%] " + step);
					}
				});

				lock (_sync)
				{
					_done = true;
					_ok = result.Ok;
					_result = result;
					_step = result.Ok ? "Готово" : "Ошибка";
					_percent = 100;
					_log.AppendLine(result.Ok ? "Готово: " + result.Message : "Ошибка: " + result.Message);
					foreach (string w in result.Warnings)
					{
						_log.AppendLine("Замечание: " + w);
					}
				}
			}
			catch (Exception ex)
			{
				lock (_sync)
				{
					_done = true;
					_ok = false;
					_result = new SetupEngine.Result { Message = ex.Message };
					_step = "Ошибка";
					_percent = 100;
					_log.AppendLine("Исключение: " + ex.Message);
				}
			}
		});
		t.IsBackground = true;
		t.Name = "setup-install";
		t.Start();
		return true;
	}

	/// <summary>
	/// Запускает удалённую установку в фоне. Режим берётся из <c>mode</c>:
	/// <c>smb</c> или <c>winrm</c>.
	/// </summary>
	public bool StartRemote(Dictionary<string, object> d)
	{
		lock (_sync)
		{
			if (!_done && _step.Length > 0)
			{
				return false;
			}
			_step = "Подготовка";
			_percent = 0;
			_done = false;
			_ok = false;
			_result = null;
			_log.Clear();
		}

		Thread t = new Thread(delegate ()
		{
			try
			{
				string mode = Json.GetStr(d, "mode", "smb");
				SetupEngine.ProgressHandler progress = delegate (string step, int percent)
				{
					lock (_sync)
					{
						_step = step;
						_percent = percent;
						_log.AppendLine("[" + percent.ToString().PadLeft(3) + "%] " + step);
					}
				};

				SmbInstaller.Outcome outcome;
				if (mode == "winrm")
				{
					WinRmInstaller.Options opt = new WinRmInstaller.Options
					{
						Host = Json.GetStr(d, "host", "").Trim(),
						User = Json.GetStr(d, "user", "").Trim(),
						Password = Json.GetStr(d, "password", ""),
						Port = Json.Int(Json.Get(d, "port"), 5985),
						Https = Json.Bool(Json.Get(d, "https"), dflt: false),
						InstallDir = Json.GetStr(d, "installDir", @"C:\DPT.Backup"),
						DataDir = Json.GetStr(d, "dataDir", @"C:\ProgramData\DPT.Backup"),
						ServiceName = Json.GetStr(d, "serviceName", "DPTBackupService"),
						WebPort = Json.Int(Json.Get(d, "webPort"), 8080)
					};
					outcome = WinRmInstaller.Install(opt, progress);
				}
				else
				{
					SmbInstaller.Options opt = new SmbInstaller.Options
					{
						Host = Json.GetStr(d, "host", "").Trim(),
						User = Json.GetStr(d, "user", "").Trim(),
						Password = Json.GetStr(d, "password", ""),
						InstallDir = Json.GetStr(d, "installDir", @"C:\DPT.Backup"),
						DataDir = Json.GetStr(d, "dataDir", @"C:\ProgramData\DPT.Backup"),
						ServiceName = Json.GetStr(d, "serviceName", "DPTBackupService"),
						Port = Json.Int(Json.Get(d, "webPort"), 8080)
					};
					outcome = SmbInstaller.Install(opt, progress);
				}

				lock (_sync)
				{
					_done = true;
					_ok = outcome.Ok;
					_result = new SetupEngine.Result
					{
						Ok = outcome.Ok,
						Message = outcome.Message,
						Steps = outcome.Steps,
						Warnings = outcome.Warnings
					};
					_step = outcome.Ok ? "Готово" : "Ошибка";
					_percent = 100;
					_log.AppendLine(outcome.Ok ? "Готово: " + outcome.Message : "Ошибка: " + outcome.Message);
					foreach (string w in outcome.Warnings)
					{
						_log.AppendLine("Замечание: " + w);
					}
				}
			}
			catch (Exception ex)
			{
				lock (_sync)
				{
					_done = true;
					_ok = false;
					_result = new SetupEngine.Result { Message = ex.Message };
					_step = "Ошибка";
					_percent = 100;
					_log.AppendLine("Исключение: " + ex.Message);
				}
			}
		});
		t.IsBackground = true;
		t.Name = "setup-install-remote";
		t.Start();
		return true;
	}

	/// <summary>Состояние для страницы.</summary>
	public Dictionary<string, object> Snapshot()
	{
		lock (_sync)
		{
			Dictionary<string, object> d = Json.Obj();
			d["step"] = _step;
			d["percent"] = _percent;
			d["done"] = _done;
			d["ok"] = _ok;
			d["log"] = _log.ToString().TrimEnd();
			if (_result != null)
			{
				d["result"] = _result.ToJson();
			}
			return d;
		}
	}
}
