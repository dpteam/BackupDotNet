using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace BackupDotNet.Core;

public static class MtApi
{
	public static MikroTikCollector.MtInfo Read(string host, int port, bool tls, string user, string password, int timeoutSec)
	{
		using TcpClient client = new TcpClient();
		IAsyncResult ar = client.BeginConnect(host, port, null, null);
		if (!ar.AsyncWaitHandle.WaitOne(timeoutSec * 1000))
		{
			throw new MtException(MtErrorKind.NoConnection, "таймаут подключения");
		}
		try
		{
			client.EndConnect(ar);
		}
		catch (Exception ex)
		{
			throw new MtException(MtErrorKind.NoConnection, ex.Message, ex);
		}
		client.ReceiveTimeout = timeoutSec * 1000;
		client.SendTimeout = timeoutSec * 1000;
		Stream stream = client.GetStream();
		if (tls)
		{
			SslStream ssl = new SslStream(stream, leaveInnerStreamOpen: false, (object param0, X509Certificate param1, X509Chain param2, SslPolicyErrors param3) => true);
			ssl.AuthenticateAsClient(host);
			stream = ssl;
		}
		WriteSentence(stream, new string[3]
		{
			"/login",
			"=name=" + user,
			"=password=" + password
		});
		List<Dictionary<string, string>> loginReply = ReadReply(stream);
		bool ok = false;
		foreach (Dictionary<string, string> s in loginReply)
		{
			if (s.TryGetValue("!done", out var _))
			{
				ok = true;
				break;
			}
			if (s.TryGetValue("!trap", out var _))
			{
				throw new MtException(MtErrorKind.Auth, "логин отклонён: " + TrapMessage(s));
			}
		}
		if (!ok)
		{
			throw new MtException(MtErrorKind.Auth, "логин отклонён");
		}
		MikroTikCollector.MtInfo info = new MikroTikCollector.MtInfo();
		info.Transport = "API";
		Dictionary<string, string> res = FirstRecord(stream, "/system/resource/print");
		if (res == null)
		{
			return null;
		}
		info.Version = Get(res, "version");
		info.Architecture = Get(res, "architecture-name");
		info.BoardName = Get(res, "board-name");
		info.UptimeMinutes = MikroTikCollector.ParseUptime(Get(res, "uptime"));
		if (int.TryParse(Get(res, "cpu-load"), out var cpu))
		{
			info.CpuLoad = cpu;
		}
		if (long.TryParse(Get(res, "free-memory"), out var mem))
		{
			info.FreeMemoryMb = mem / 1048576;
		}
		if (long.TryParse(Get(res, "total-memory"), out mem))
		{
			info.TotalMemoryMb = mem / 1048576;
		}
		if (int.TryParse(Get(res, "cpu-count"), out var cnt))
		{
			info.CpuCount = cnt;
		}
		Dictionary<string, string> ident = FirstRecord(stream, "/system/identity/print");
		if (ident != null)
		{
			info.Identity = Get(ident, "name");
		}
		try
		{
			Dictionary<string, string> rb = FirstRecord(stream, "/system/routerboard/print");
			if (rb != null)
			{
				string model = Get(rb, "model");
				if (model.Length > 0)
				{
					info.BoardName = model;
				}
				info.Serial = Get(rb, "serial-number");
			}
		}
		catch
		{
		}
		try
		{
			Dictionary<string, string> h = FirstRecord(stream, "/system/health/print");
			if (h != null)
			{
				if (int.TryParse(Get(h, "temperature"), out var t) && t > 0)
				{
					info.Temperature = t;
				}
				else if (int.TryParse(Get(h, "cpu-temperature"), out t) && t > 0)
				{
					info.Temperature = t;
				}
			}
		}
		catch
		{
		}
		return info;
	}

	private static string Get(Dictionary<string, string> rec, string key)
	{
		if (!rec.TryGetValue(key, out var v))
		{
			return "";
		}
		return v;
	}

	private static string TrapMessage(Dictionary<string, string> sentence)
	{
		if (!sentence.TryGetValue("=message=", out var msg))
		{
			return "неизвестная ошибка";
		}
		return msg;
	}

	private static Dictionary<string, string> FirstRecord(Stream stream, string command)
	{
		WriteSentence(stream, new string[1] { command });
		List<Dictionary<string, string>> reply = ReadReply(stream);
		foreach (Dictionary<string, string> s in reply)
		{
			if (s.TryGetValue("!trap", out var _))
			{
				throw new MtException(MtErrorKind.Unknown, TrapMessage(s));
			}
			if (s.ContainsKey("!re"))
			{
				return s;
			}
		}
		return null;
	}

	private static void WriteSentence(Stream stream, string[] words)
	{
		foreach (string w in words)
		{
			WriteWord(stream, w);
		}
		stream.WriteByte(0);
		stream.Flush();
	}

	private static void WriteWord(Stream stream, string word)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(word);
		WriteLength(stream, bytes.Length);
		stream.Write(bytes, 0, bytes.Length);
	}

	private static void WriteLength(Stream stream, int len)
	{
		if (len < 128)
		{
			stream.WriteByte((byte)len);
		}
		else if (len < 16384)
		{
			len |= 0x8000;
			stream.WriteByte((byte)(len >> 8));
			stream.WriteByte((byte)len);
		}
		else if (len < 2097152)
		{
			len |= 0xC00000;
			stream.WriteByte((byte)(len >> 16));
			stream.WriteByte((byte)(len >> 8));
			stream.WriteByte((byte)len);
		}
		else if (len < 268435456)
		{
			len |= -536870912;
			stream.WriteByte((byte)(len >> 24));
			stream.WriteByte((byte)(len >> 16));
			stream.WriteByte((byte)(len >> 8));
			stream.WriteByte((byte)len);
		}
		else
		{
			stream.WriteByte(240);
			stream.WriteByte((byte)(len >> 24));
			stream.WriteByte((byte)(len >> 16));
			stream.WriteByte((byte)(len >> 8));
			stream.WriteByte((byte)len);
		}
	}

	private static int ReadLength(Stream stream)
	{
		int b = stream.ReadByte();
		if (b < 0)
		{
			throw new EndOfStreamException();
		}
		if ((b & 0x80) == 0)
		{
			return b;
		}
		if ((b & 0xC0) == 128)
		{
			return ((b & 0x3F) << 8) | ReadByte(stream);
		}
		if ((b & 0xE0) == 192)
		{
			return ((b & 0x1F) << 16) | (ReadByte(stream) << 8) | ReadByte(stream);
		}
		if ((b & 0xF0) == 224)
		{
			return ((b & 0xF) << 24) | (ReadByte(stream) << 16) | (ReadByte(stream) << 8) | ReadByte(stream);
		}
		return (ReadByte(stream) << 24) | (ReadByte(stream) << 16) | (ReadByte(stream) << 8) | ReadByte(stream);
	}

	private static int ReadByte(Stream stream)
	{
		int b = stream.ReadByte();
		if (b < 0)
		{
			throw new EndOfStreamException();
		}
		return b;
	}

	private static Dictionary<string, string> ReadSentence(Stream stream)
	{
		Dictionary<string, string> sentence = new Dictionary<string, string>(StringComparer.Ordinal);
		while (true)
		{
			int len = ReadLength(stream);
			if (len == 0)
			{
				break;
			}
			byte[] buf = new byte[len];
			int n;
			for (int read = 0; read < len; read += n)
			{
				n = stream.Read(buf, read, len - read);
				if (n <= 0)
				{
					throw new EndOfStreamException();
				}
			}
			string word = Encoding.UTF8.GetString(buf);
			if (word.Length == 0)
			{
				continue;
			}
			if (word[0] == '!' || word[0] == '.')
			{
				sentence[word] = "";
			}
			else if (word[0] == '=')
			{
				int eq = word.IndexOf('=', 1);
				if (eq > 0)
				{
					sentence[word.Substring(1, eq - 1)] = word.Substring(eq + 1);
				}
				else
				{
					sentence[word.Substring(1)] = "";
				}
			}
		}
		return sentence;
	}

	private static List<Dictionary<string, string>> ReadReply(Stream stream)
	{
		List<Dictionary<string, string>> reply = new List<Dictionary<string, string>>();
		Dictionary<string, string> s;
		do
		{
			s = ReadSentence(stream);
			reply.Add(s);
		}
		while (!s.ContainsKey("!done") && !s.ContainsKey("!fatal") && reply.Count <= 500);
		return reply;
	}
}
