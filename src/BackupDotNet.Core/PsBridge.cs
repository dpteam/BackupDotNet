using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading;

namespace BackupDotNet.Core;

/// <summary>
/// Вызов Windows PowerShell 5.1 из процесса агента.
///
/// Нужен там, где WMI работает, но неудобно: например, VLAN на порту ВМ
/// Hyper-V через WMI <c>ModifyFeatureSettings</c> отвечает «ошибка не относится
/// к параметрам настройки коммутатора», а командлет <c>Set-VMNetworkAdapterVlan</c>
/// делает то же самое с первого раза.
///
/// Сторонних пакетов нет: <c>System.Management.Automation</c> берётся из GAC,
/// как и разрешено правилами проекта. Ничего не бросает наружу.
/// </summary>
public static class PsBridge
{
	/// <summary>Результат выполнения.</summary>
	public class Result
	{
		public bool Ok;

		public string Output = "";

		public string Error = "";

		public List<string> Lines = new List<string>();
	}

	/// <summary>
	/// Экранирует строку для подстановки в PowerShell-код:
	/// оборачивает в одинарные кавычки, удваивая внутренние.
	/// </summary>
	public static string Quote(string value)
	{
		if (value == null)
		{
			return "''";
		}
		return "'" + value.Replace("'", "''") + "'";
	}

	/// <summary>
	/// Выполняет скрипт. Возвращает вывод и ошибки; исключения не выходят наружу.
	/// </summary>
	/// <param name="script">Текст скрипта.</param>
	/// <param name="timeoutSeconds">Сколько ждать; по истечении скрипт останавливается.</param>
	/// <param name="module">Модуль, который надо загрузить (например, Hyper-V).</param>
	public static Result Run(string script, int timeoutSeconds, string module)
	{
		Result res = new Result();
		if (string.IsNullOrWhiteSpace(script))
		{
			res.Ok = true;
			return res;
		}

		try
		{
			using Runspace rs = RunspaceFactory.CreateRunspace();
			rs.Open();
			using PowerShell ps = PowerShell.Create();
			ps.Runspace = rs;

			if (!string.IsNullOrWhiteSpace(module))
			{
				ps.AddCommand("Import-Module").AddParameter("Name", module).AddParameter("ErrorAction", "Stop");
				ps.Invoke();
				ps.Commands.Clear();
			}

			ps.AddScript(script);
			IAsyncResult ar = ps.BeginInvoke();
			int timeout = Math.Max(5, timeoutSeconds) * 1000;
			if (!ar.AsyncWaitHandle.WaitOne(timeout))
			{
				try
				{
					ps.Stop();
				}
				catch
				{
				}
				res.Error = "превышено время ожидания (" + timeoutSeconds + " с)";
				return res;
			}

			StringBuilder sb = new StringBuilder();
			foreach (PSObject o in ps.EndInvoke(ar))
			{
				if (o == null)
				{
					continue;
				}
				string line = o.ToString();
				res.Lines.Add(line);
				sb.AppendLine(line);
			}
			res.Output = sb.ToString().TrimEnd();

			StringBuilder err = new StringBuilder();
			foreach (ErrorRecord e in ps.Streams.Error)
			{
				if (e == null)
				{
					continue;
				}
				string msg = e.Exception != null ? e.Exception.Message : e.ToString();
				if (err.Length > 0)
				{
					err.Append("; ");
				}
				err.Append(msg);
			}
			res.Error = err.ToString();
			res.Ok = res.Error.Length == 0;
			return res;
		}
		catch (Exception ex)
		{
			res.Ok = false;
			res.Error = ex.Message;
			Log.Warn("PowerShell: " + ex.Message);
			return res;
		}
	}
}
