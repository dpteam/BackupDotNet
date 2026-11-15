using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace BackupChecker.Core;

public class ZabbixSender
{
	private readonly string _server;

	private readonly int _port;

	private readonly int _timeoutSec;

	private readonly int _batchSize;

	private readonly int _batchBytes;

	private readonly string _queuePath;

	public ZabbixSender(string server, int port, int timeoutSec, int batchSize, int batchBytes, string queuePath)
	{
		_server = server;
		_port = ((port <= 0) ? 10051 : port);
		_timeoutSec = ((timeoutSec <= 0) ? 5 : timeoutSec);
		_batchSize = ((batchSize <= 0) ? 500 : batchSize);
		_batchBytes = ((batchBytes <= 0) ? 65536 : batchBytes);
		_queuePath = queuePath;
	}

	public string Send(List<ZabbixValue> values)
	{
		if (values == null || values.Count == 0)
		{
			return "";
		}
		if (string.IsNullOrEmpty(_server))
		{
			return "не задан сервер Zabbix";
		}
		StringBuilder errors = new StringBuilder();
		int index = 0;
		while (index < values.Count)
		{
			List<ZabbixValue> batch = new List<ZabbixValue>();
			int bytes = 0;
			for (; index < values.Count; index++)
			{
				if (batch.Count >= _batchSize)
				{
					break;
				}
				if (bytes >= _batchBytes)
				{
					break;
				}
				batch.Add(values[index]);
				bytes += values[index].Key.Length + values[index].Value.Length + values[index].Host.Length + 64;
			}
			string err = SendBatch(batch);
			if (!string.IsNullOrEmpty(err))
			{
				if (errors.Length > 0)
				{
					errors.Append("; ");
				}
				errors.Append(err);
				Enqueue(batch);
			}
		}
		return errors.ToString();
	}

	private string SendBatch(List<ZabbixValue> batch)
	{
		string json = BuildPayload(batch);
		byte[] body = Encoding.UTF8.GetBytes(json);
		byte[] packet = new byte[13 + body.Length];
		packet[0] = 90;
		packet[1] = 66;
		packet[2] = 88;
		packet[3] = 68;
		packet[4] = 1;
		long len = body.Length;
		for (int i = 0; i < 8; i++)
		{
			packet[5 + i] = (byte)((len >> 8 * i) & 0xFF);
		}
		Buffer.BlockCopy(body, 0, packet, 13, body.Length);
		try
		{
			using TcpClient client = new TcpClient();
			IAsyncResult ar = client.BeginConnect(_server, _port, null, null);
			if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(_timeoutSec)))
			{
				try
				{
					client.Close();
				}
				catch
				{
				}
				return "таймаут подключения к " + _server + ":" + _port;
			}
			client.EndConnect(ar);
			client.ReceiveTimeout = _timeoutSec * 1000;
			using NetworkStream ns = client.GetStream();
			ns.Write(packet, 0, packet.Length);
			ns.Flush();
			byte[] header = new byte[13];
			int read = ReadFully(ns, header, 13);
			if (read < 13)
			{
				return "короткий ответ от Zabbix";
			}
			if (header[0] != 90 || header[1] != 66 || header[2] != 88 || header[3] != 68)
			{
				return "некорректный ответ Zabbix (нет сигнатуры ZBXD)";
			}
			long respLen = 0L;
			for (int j = 0; j < 8; j++)
			{
				respLen |= (long)((ulong)header[5 + j] << 8 * j);
			}
			if (respLen <= 0 || respLen > 4194304)
			{
				return "некорректная длина ответа Zabbix";
			}
			byte[] respBody = new byte[respLen];
			ReadFully(ns, respBody, (int)respLen);
			string text = Encoding.UTF8.GetString(respBody);
			Dictionary<string, object> resp = Json.ParseObject(text);
			string response = Json.GetStr(resp, "response", "");
			string info = Json.GetStr(resp, "info", "");
			if (response == "success")
			{
				return "";
			}
			if (info.IndexOf("failed: 0", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return "";
			}
			return "Zabbix: " + ((info.Length > 0) ? info : text);
		}
		catch (Exception ex)
		{
			return "Zabbix: " + ex.Message;
		}
	}

	private static int ReadFully(Stream s, byte[] buffer, int count)
	{
		int total;
		int n;
		for (total = 0; total < count; total += n)
		{
			n = s.Read(buffer, total, count - total);
			if (n <= 0)
			{
				break;
			}
		}
		return total;
	}

	private static string BuildPayload(List<ZabbixValue> batch)
	{
		StringBuilder sb = new StringBuilder(256 + batch.Count * 96);
		sb.Append("{\"request\":\"sender data\",\"data\":[");
		for (int i = 0; i < batch.Count; i++)
		{
			if (i > 0)
			{
				sb.Append(',');
			}
			ZabbixValue v = batch[i];
			sb.Append("{\"host\":");
			AppendString(sb, v.Host);
			sb.Append(",\"key\":");
			AppendString(sb, v.Key);
			sb.Append(",\"value\":");
			AppendString(sb, v.Value);
			if (v.Clock > 0)
			{
				sb.Append(",\"clock\":");
				sb.Append(v.Clock.ToString(CultureInfo.InvariantCulture));
			}
			sb.Append('}');
		}
		sb.Append("],\"clock\":");
		sb.Append(Now());
		sb.Append('}');
		return sb.ToString();
	}

	public static long Now()
	{
		return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
	}

	private static void AppendString(StringBuilder sb, string s)
	{
		sb.Append('"');
		if (s != null)
		{
			foreach (char c in s)
			{
				if (c == '"' || c == '\\')
				{
					sb.Append('\\').Append(c);
				}
				else if (c == '\n')
				{
					sb.Append("\\n");
				}
				else if (c == '\r')
				{
					sb.Append("\\r");
				}
				else if (c == '\t')
				{
					sb.Append("\\t");
				}
				else if (c < ' ')
				{
					StringBuilder stringBuilder = sb.Append("\\u");
					int num = c;
					stringBuilder.Append(num.ToString("x4", CultureInfo.InvariantCulture));
				}
				else
				{
					sb.Append(c);
				}
			}
		}
		sb.Append('"');
	}

	private void Enqueue(List<ZabbixValue> batch)
	{
		if (string.IsNullOrEmpty(_queuePath))
		{
			return;
		}
		try
		{
			string dir = Path.GetDirectoryName(_queuePath);
			if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}
			StringBuilder sb = new StringBuilder();
			foreach (ZabbixValue v in batch)
			{
				Dictionary<string, object> d = Json.Obj();
				d["host"] = v.Host;
				d["key"] = v.Key;
				d["value"] = v.Value;
				d["clock"] = v.Clock;
				sb.Append(Json.Write(d)).Append('\n');
			}
			File.AppendAllText(_queuePath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		}
		catch (Exception ex)
		{
			Log.Warn("Не удалось поставить значения в очередь Zabbix: " + ex.Message);
		}
	}

	public int FlushQueue()
	{
		if (string.IsNullOrEmpty(_queuePath) || !File.Exists(_queuePath))
		{
			return 0;
		}
		int sent = 0;
		try
		{
			string[] lines = File.ReadAllLines(_queuePath);
			List<ZabbixValue> values = new List<ZabbixValue>();
			string[] array = lines;
			foreach (string line in array)
			{
				if (line.Length >= 3)
				{
					Dictionary<string, object> d = Json.ParseObject(line);
					values.Add(new ZabbixValue(Json.GetStr(d, "host", ""), Json.GetStr(d, "key", ""), Json.GetStr(d, "value", ""), Json.Long(Json.Get(d, "clock"))));
				}
			}
			if (values.Count == 0)
			{
				File.Delete(_queuePath);
				return 0;
			}
			int index = 0;
			while (index < values.Count)
			{
				List<ZabbixValue> batch = new List<ZabbixValue>();
				for (; index < values.Count; index++)
				{
					if (batch.Count >= _batchSize)
					{
						break;
					}
					batch.Add(values[index]);
				}
				string err = SendBatch(batch);
				if (!string.IsNullOrEmpty(err))
				{
					return sent;
				}
				sent += batch.Count;
			}
			File.Delete(_queuePath);
		}
		catch (Exception ex)
		{
			Log.Warn("Не удалось дослать очередь Zabbix: " + ex.Message);
		}
		return sent;
	}
}
