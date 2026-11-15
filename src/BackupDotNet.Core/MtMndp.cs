using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace BackupDotNet.Core;

public static class MtMndp
{
	private const int MndpPort = 5678;

	private static readonly object _sync = new object();

	public static MikroTikCollector.MtInfo Read(string host, int timeoutSec)
	{
		lock (_sync)
		{
			int wait = Math.Max(3, Math.Min(timeoutSec, 15));
			using (UdpClient udp = new UdpClient())
			{
				udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, optionValue: true);
				udp.Client.Bind(new IPEndPoint(IPAddress.Any, 5678));
				udp.Client.ReceiveTimeout = 500;
				try
				{
					udp.EnableBroadcast = true;
				}
				catch
				{
				}
				IPAddress target = null;
				if (!IPAddress.TryParse(host, out target))
				{
					try
					{
						IPAddress[] all = Dns.GetHostAddresses(host);
						IPAddress[] array = all;
						foreach (IPAddress a in array)
						{
							if (a.AddressFamily == AddressFamily.InterNetwork)
							{
								target = a;
								break;
							}
						}
					}
					catch
					{
					}
				}
				byte[] array2 = new byte[8];
				byte[] hello = array2;
				if (target != null)
				{
					TrySend(udp, hello, SubnetBroadcast(target));
					TrySend(udp, hello, target);
				}
				TrySend(udp, hello, IPAddress.Broadcast);
				DateTime deadline = DateTime.Now.AddSeconds(wait);
				while (DateTime.Now < deadline)
				{
					IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
					byte[] data;
					try
					{
						data = udp.Receive(ref remote);
					}
					catch (SocketException)
					{
						continue;
					}
					if (data != null && data.Length > 8 && !IsLocalAddress(remote.Address) && (target == null || target.Equals(remote.Address)))
					{
						MikroTikCollector.MtInfo info = Parse(data);
						if (info != null && info.Version.Length != 0)
						{
							info.Transport = "MNDP";
							info.Limited = true;
							info.IpAddress = remote.Address.ToString();
							return info;
						}
					}
				}
			}
			return null;
		}
	}

	private static void TrySend(UdpClient udp, byte[] data, IPAddress to)
	{
		try
		{
			udp.Send(data, data.Length, new IPEndPoint(to, 5678));
		}
		catch
		{
		}
	}

	private static IPAddress SubnetBroadcast(IPAddress addr)
	{
		byte[] b = addr.GetAddressBytes();
		return new IPAddress(new byte[4]
		{
			b[0],
			b[1],
			b[2],
			255
		});
	}

	private static bool IsLocalAddress(IPAddress addr)
	{
		try
		{
			NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
			foreach (NetworkInterface ni in allNetworkInterfaces)
			{
				foreach (UnicastIPAddressInformation ua in ni.GetIPProperties().UnicastAddresses)
				{
					if (ua.Address.Equals(addr))
					{
						return true;
					}
				}
			}
		}
		catch
		{
		}
		return false;
	}

	public static MikroTikCollector.MtInfo Parse(byte[] data)
	{
		if (data == null || data.Length < 12)
		{
			return null;
		}
		MikroTikCollector.MtInfo info = new MikroTikCollector.MtInfo();
		List<byte[]> raw = new List<byte[]>();
		int pos = 4;
		int guard = 0;
		while (pos + 4 <= data.Length && guard++ < 200)
		{
			int len = (data[pos + 2] << 8) | data[pos + 3];
			pos += 4;
			if (len <= 0 || pos + len > data.Length)
			{
				break;
			}
			byte[] value = new byte[len];
			Buffer.BlockCopy(data, pos, value, 0, len);
			pos += len;
			raw.Add(value);
		}
		if (raw.Count == 0)
		{
			return null;
		}
		foreach (byte[] v in raw)
		{
			if (v.Length == 6 && !IsPrintable(v))
			{
				info.Mac = FormatMac(v);
			}
			else if (v.Length == 4 && !IsPrintable(v))
			{
				long seconds = (long)(v[0] | ((ulong)v[1] << 8) | ((ulong)v[2] << 16) | ((ulong)v[3] << 24));
				if (seconds > 0 && seconds < 1000000000 && info.UptimeMinutes < 0.0)
				{
					info.UptimeMinutes = (double)seconds / 60.0;
				}
			}
			else
			{
				if (v.Length == 1 || !IsPrintable(v))
				{
					continue;
				}
				string s = Encoding.UTF8.GetString(v).Trim();
				if (s.Length == 0)
				{
					continue;
				}
				if (info.Version.Length == 0 && Regex.IsMatch(s, "^\\d+\\.\\d+"))
				{
					Match m = Regex.Match(s, "^\\d+\\.\\d+(?:\\.\\d+)?(?:\\s*\\([^)]*\\))?");
					info.Version = (m.Success ? m.Value.Trim() : s);
					Match d = Regex.Match(s, "(\\d{4}-\\d{2}-\\d{2})");
					if (d.Success)
					{
						info.BuildDate = d.Groups[1].Value;
					}
				}
				else if (Regex.IsMatch(s, "^\\d{1,3}(\\.\\d{1,3}){3}$"))
				{
					if (info.IpAddress.Length == 0)
					{
						info.IpAddress = s;
					}
				}
				else if (info.License.Length == 0 && Regex.IsMatch(s, "^[A-Z0-9]{4}-[A-Z0-9]{4}$"))
				{
					info.License = s;
				}
				else if (info.UptimeMinutes < 0.0 && Regex.IsMatch(s, "^\\d+[wdhms]"))
				{
					double mins = MikroTikCollector.ParseUptime(s);
					if (mins >= 0.0)
					{
						info.UptimeMinutes = mins;
					}
				}
				else if (s.IndexOf('/') > 0 && (s.Contains("ether") || s.Contains("bridge") || s.Contains("wlan") || s.Contains("sfp")))
				{
					if (info.Interface.Length == 0)
					{
						info.Interface = s;
					}
				}
				else if (info.BoardName.Length == 0 && Regex.IsMatch(s, "^(RB|CCR|CRS|CRS3|hEX|hAP|CHR|RBM|SXT|LHG|DISC|Groove|Metal|PowerBox|Cube|mAP|wAP|cAP)", RegexOptions.IgnoreCase))
				{
					info.BoardName = s;
				}
				else if (!string.Equals(s, "MikroTik", StringComparison.OrdinalIgnoreCase) && info.Identity.Length == 0 && s.IndexOf(' ') < 0 && s.Length <= 64)
				{
					info.Identity = s;
				}
			}
		}
		if (info.Version.Length <= 0)
		{
			return null;
		}
		return info;
	}

	private static bool IsPrintable(byte[] v)
	{
		foreach (byte b in v)
		{
			if (b < 32 || b > 126)
			{
				return false;
			}
		}
		return true;
	}

	private static string FormatMac(byte[] v)
	{
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < v.Length; i++)
		{
			if (i > 0)
			{
				sb.Append(':');
			}
			sb.Append(v[i].ToString("X2"));
		}
		return sb.ToString();
	}
}
