using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace BackupChecker.Core;

public static class MtSnmp
{
	private static readonly string[] VersionOids = new string[2] { "1.3.6.1.4.1.14988.1.1.4.1.0", "1.3.6.1.2.1.1.1.0" };

	public static MikroTikCollector.MtInfo Read(string host, int port, string community, int timeoutSec)
	{
		MikroTikCollector.MtInfo info = new MikroTikCollector.MtInfo();
		info.Transport = "SNMP";
		string[] versionOids = VersionOids;
		foreach (string oid in versionOids)
		{
			if (TryGet(host, port, community, oid, timeoutSec, out var value) && value.Length > 0)
			{
				Match m = Regex.Match(value, "\\d+\\.\\d+(?:\\.\\d+)?");
				if (m.Success)
				{
					info.Version = m.Value;
					break;
				}
			}
		}
		if (info.Version.Length <= 0)
		{
			return null;
		}
		return info;
	}

	private static bool TryGet(string host, int port, string community, string oid, int timeoutSec, out string value)
	{
		value = "";
		byte[] oidBytes = EncodeOid(oid);
		byte[] request = BuildGetRequest(community, oidBytes);
		using UdpClient udp = new UdpClient();
		udp.Client.ReceiveTimeout = timeoutSec * 1000;
		udp.Connect(host, port);
		udp.Send(request, request.Length);
		IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
		byte[] response;
		try
		{
			response = udp.Receive(ref remote);
		}
		catch (SocketException)
		{
			return false;
		}
		value = ExtractStringValue(response);
		return value.Length > 0;
	}

	private static byte[] BuildGetRequest(string community, byte[] oidBytes)
	{
		byte[] varbind = Tlv(48, Concat(oidBytes, new byte[2] { 5, 0 }));
		byte[] varbindList = Tlv(48, varbind);
		byte[][] array = new byte[4][]
		{
			Tlv(2, new byte[4] { 1, 0, 0, 1 }),
			null,
			null,
			null
		};
		byte[] value = new byte[1];
		array[1] = Tlv(2, value);
		byte[] value2 = new byte[1];
		array[2] = Tlv(2, value2);
		array[3] = varbindList;
		byte[] pduBody = Concat(array);
		byte[] pdu = Tlv(160, pduBody);
		byte[] version = Tlv(2, new byte[1] { 1 });
		byte[] comm = Tlv(4, Encoding.UTF8.GetBytes(community));
		return Tlv(48, Concat(version, comm, pdu));
	}

	private static byte[] EncodeOid(string oid)
	{
		string[] parts = oid.Split('.');
		List<byte> bytes = new List<byte>();
		bytes.Add((byte)(int.Parse(parts[0]) * 40 + int.Parse(parts[1])));
		for (int i = 2; i < parts.Length; i++)
		{
			long n = long.Parse(parts[i]);
			if (n < 128)
			{
				bytes.Add((byte)n);
				continue;
			}
			List<byte> chunk = new List<byte>();
			chunk.Add((byte)(n & 0x7F));
			for (n >>= 7; n > 0; n >>= 7)
			{
				chunk.Insert(0, (byte)((n & 0x7F) | 0x80));
			}
			bytes.AddRange(chunk);
		}
		return Tlv(6, bytes.ToArray());
	}

	private static byte[] Tlv(byte tag, byte[] value)
	{
		List<byte> result = new List<byte>();
		result.Add(tag);
		int len = value.Length;
		if (len < 128)
		{
			result.Add((byte)len);
		}
		else if (len < 256)
		{
			result.Add(129);
			result.Add((byte)len);
		}
		else
		{
			result.Add(130);
			result.Add((byte)(len >> 8));
			result.Add((byte)len);
		}
		result.AddRange(value);
		return result.ToArray();
	}

	private static byte[] Concat(params byte[][] arrays)
	{
		int total = 0;
		foreach (byte[] a in arrays)
		{
			total += a.Length;
		}
		byte[] result = new byte[total];
		int pos = 0;
		foreach (byte[] a2 in arrays)
		{
			Buffer.BlockCopy(a2, 0, result, pos, a2.Length);
			pos += a2.Length;
		}
		return result;
	}

	private static string ExtractStringValue(byte[] response)
	{
		for (int i = 0; i < response.Length - 2; i++)
		{
			if (response[i] != 4)
			{
				continue;
			}
			int len = response[i + 1];
			int start = i + 2;
			if ((len & 0x80) != 0 || len <= 0 || start + len > response.Length)
			{
				continue;
			}
			bool printable = true;
			for (int j = start; j < start + len; j++)
			{
				byte b = response[j];
				if (b < 32 || b > 126)
				{
					printable = false;
					break;
				}
			}
			if (printable)
			{
				string s = Encoding.ASCII.GetString(response, start, len);
				if (s.Length >= 3 && s.IndexOf('.') > 0)
				{
					return s;
				}
			}
		}
		return "";
	}
}
