using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using BackupDotNet.Core;

namespace BackupDotNet.Deploy;

public static class Scanner
{
	public static List<string> LocalSubnets()
	{
		List<string> result = new List<string>();
		try
		{
			NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
			foreach (NetworkInterface ni in allNetworkInterfaces)
			{
				if (ni.OperationalStatus != OperationalStatus.Up || ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
				{
					continue;
				}
				foreach (UnicastIPAddressInformation ua in ni.GetIPProperties().UnicastAddresses)
				{
					if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
					{
						continue;
					}
					int prefix = PrefixFromMask(ua.IPv4Mask);
					if (prefix > 0 && prefix <= 30)
					{
						string cidr = ToCidr(ua.Address, prefix);
						if (!result.Contains(cidr))
						{
							result.Add(cidr);
						}
					}
				}
			}
		}
		catch
		{
		}
		return result;
	}

	private static int PrefixFromMask(IPAddress mask)
	{
		if (mask == null)
		{
			return 0;
		}
		byte[] b = mask.GetAddressBytes();
		int bits = 0;
		byte[] array = b;
		foreach (byte x in array)
		{
			int i2 = 7;
			while (i2 >= 0)
			{
				if ((x & (1 << i2)) != 0)
				{
					bits++;
					i2--;
					continue;
				}
				return bits;
			}
		}
		return bits;
	}

	private static string ToCidr(IPAddress ip, int prefix)
	{
		uint a = ToUInt(ip);
		uint mask = ((prefix >= 32) ? uint.MaxValue : ((uint)(-1 << 32 - prefix)));
		return FromUInt(a & mask).ToString() + "/" + prefix;
	}

	private static uint ToUInt(IPAddress ip)
	{
		byte[] b = ip.GetAddressBytes();
		return (uint)((b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3]);
	}

	private static IPAddress FromUInt(uint v)
	{
		return new IPAddress(new byte[4]
		{
			(byte)((v >> 24) & 0xFF),
			(byte)((v >> 16) & 0xFF),
			(byte)((v >> 8) & 0xFF),
			(byte)(v & 0xFF)
		});
	}

	public static List<string> Expand(string spec, int maxHosts, out string error)
	{
		error = "";
		List<string> result = new List<string>();
		if (string.IsNullOrEmpty(spec))
		{
			error = "не задан диапазон";
			return result;
		}
		char[] seps = new char[6] { ',', ';', ' ', '\n', '\r', '\t' };
		string[] array = spec.Split(seps);
		foreach (string raw in array)
		{
			string part = raw.Trim();
			if (part.Length == 0)
			{
				continue;
			}
			try
			{
				if (part.IndexOf('/') > 0)
				{
					ExpandCidr(part, result);
				}
				else if (part.IndexOf('-') > 0)
				{
					ExpandRange(part, result);
				}
				else
				{
					if (!IPAddress.TryParse(part, out var ip))
					{
						error = "не разобрать адрес: " + part;
						return new List<string>();
					}
					if (!result.Contains(ip.ToString()))
					{
						result.Add(ip.ToString());
					}
				}
			}
			catch (Exception ex)
			{
				error = "не разобрать «" + part + "»: " + ex.Message;
				return new List<string>();
			}
			if (result.Count > maxHosts)
			{
				error = "слишком много адресов (" + result.Count + "), максимум " + maxHosts + " — сузьте диапазон";
				return new List<string>();
			}
		}
		if (result.Count == 0)
		{
			error = "не найдено ни одного адреса";
		}
		return result;
	}

	private static void ExpandCidr(string cidr, List<string> into)
	{
		string[] parts = cidr.Split('/');
		if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var ip))
		{
			throw new ArgumentException("ожидается вид 192.168.0.0/24");
		}
		if (!int.TryParse(parts[1], out var prefix) || prefix < 16 || prefix > 32)
		{
			throw new ArgumentException("маска должна быть от /16 до /32");
		}
		uint baseNum = ToUInt(ip);
		uint mask = ((prefix == 32) ? uint.MaxValue : ((uint)(-1 << 32 - prefix)));
		uint network = baseNum & mask;
		uint broadcast = network | ~mask;
		uint first = ((prefix >= 31) ? network : (network + 1));
		uint last = ((prefix >= 31) ? broadcast : (broadcast - 1));
		for (uint v = first; v <= last; v++)
		{
			into.Add(FromUInt(v).ToString());
			if (into.Count > 4096)
			{
				throw new ArgumentException("диапазон слишком велик");
			}
			if (v == uint.MaxValue)
			{
				break;
			}
		}
	}

	private static void ExpandRange(string range, List<string> into)
	{
		string[] parts = range.Split('-');
		if (parts.Length != 2)
		{
			throw new ArgumentException(range);
		}
		string startText = parts[0].Trim();
		string endText = parts[1].Trim();
		if (!IPAddress.TryParse(startText, out var start))
		{
			throw new ArgumentException(startText);
		}
		if (endText.IndexOf('.') < 0)
		{
			string[] octets = startText.Split('.');
			if (octets.Length != 4)
			{
				throw new ArgumentException(range);
			}
			endText = octets[0] + "." + octets[1] + "." + octets[2] + "." + endText;
		}
		if (!IPAddress.TryParse(endText, out var end))
		{
			throw new ArgumentException(endText);
		}
		uint a = ToUInt(start);
		uint b = ToUInt(end);
		if (b < a)
		{
			throw new ArgumentException("конец диапазона меньше начала");
		}
		for (uint v = a; v <= b; v++)
		{
			into.Add(FromUInt(v).ToString());
			if (into.Count > 4096)
			{
				throw new ArgumentException("диапазон слишком велик");
			}
			if (v == uint.MaxValue)
			{
				break;
			}
		}
	}

	public static List<Target> Scan(List<string> addresses, int threads, int timeoutMs)
	{
		List<Target> result = new List<Target>();
		if (addresses == null || addresses.Count == 0)
		{
			return result;
		}
		if (threads < 1)
		{
			threads = 1;
		}
		if (threads > 128)
		{
			threads = 128;
		}
		Target[] slots = new Target[addresses.Count];
		int next = -1;
		Thread[] workers = new Thread[Math.Min(threads, addresses.Count)];
		for (int w = 0; w < workers.Length; w++)
		{
			workers[w] = new Thread(() =>
			{
				while (true)
				{
					int num3 = Interlocked.Increment(ref next);
					if (num3 >= addresses.Count)
					{
						break;
					}
					Target target = new Target
					{
						Ip = addresses[num3],
						Host = addresses[num3],
						Source = "subnet"
					};
					try
					{
						Probe(target, timeoutMs);
					}
					catch (Exception ex)
					{
						target.Error = ex.Message;
					}
					slots[num3] = target;
				}
			});
			workers[w].IsBackground = true;
			workers[w].Start();
		}
		Thread[] array = workers;
		foreach (Thread th in array)
		{
			th.Join();
		}
		Target[] array2 = slots;
		foreach (Target t in array2)
		{
			if (t != null)
			{
				result.Add(t);
			}
		}
		return result;
	}

	public static void Probe(Target t, int timeoutMs)
	{
		if (t == null || string.IsNullOrEmpty(t.Ip))
		{
			return;
		}
		bool ping = false;
		try
		{
			using Ping p = new Ping();
			PingReply reply = p.Send(t.Ip, Math.Max(300, timeoutMs));
			ping = reply != null && reply.Status == IPStatus.Success;
		}
		catch
		{
		}
		t.Online = ping;
		t.WinRm = PortOpen(t.Ip, 5985, timeoutMs) || PortOpen(t.Ip, 5986, timeoutMs);
		t.Smb = PortOpen(t.Ip, 445, timeoutMs);
		t.Http = PortOpen(t.Ip, 8080, timeoutMs);
		if (t.Http)
		{
			string version = HttpHealth(t.Ip);
			if (version.Length > 0)
			{
				t.HasAgent = true;
				t.AgentVersion = version;
			}
		}
		if (!t.Online && (t.WinRm || t.Smb || t.Http))
		{
			t.Online = true;
		}
	}

	private static bool PortOpen(string host, int port, int timeoutMs)
	{
		try
		{
			using TcpClient c = new TcpClient();
			IAsyncResult ar = c.BeginConnect(host, port, null, null);
			if (!ar.AsyncWaitHandle.WaitOne(timeoutMs))
			{
				return false;
			}
			c.EndConnect(ar);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static string HttpHealth(string host)
	{
		try
		{
			HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://" + host + ":8080/api/health");
			req.Timeout = 1500;
			req.ReadWriteTimeout = 1500;
			req.Proxy = null;
			req.Method = "GET";
			using HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
			using StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8);
			string body = sr.ReadToEnd();
			Dictionary<string, object> d = Json.ParseObject(body);
			if (!Json.Bool(Json.Get(d, "ok")))
			{
				return "";
			}
			return Json.GetStr(d, "version", "?");
		}
		catch
		{
			return "";
		}
	}

	public static List<Target> DiscoverAd(string domain, string baseDn, string user, string password, int maxHosts, out string error)
	{
		error = "";
		List<Target> result = new List<Target>();
		DirectoryEntry root = null;
		try
		{
			string path = "LDAP://" + (string.IsNullOrEmpty(domain) ? "" : domain);
			root = (string.IsNullOrEmpty(user) ? new DirectoryEntry(path) : new DirectoryEntry(path, user, password, AuthenticationTypes.Secure));
			DirectorySearcher ds = new DirectorySearcher(root);
			ds.PageSize = 500;
			ds.SizeLimit = maxHosts + 1;
			ds.Filter = "(&(objectCategory=computer)(!(userAccountControl:1.2.840.113556.1.4.803:=2)))";
			ds.PropertiesToLoad.Add("name");
			ds.PropertiesToLoad.Add("dNSHostName");
			ds.PropertiesToLoad.Add("operatingSystem");
			ds.PropertiesToLoad.Add("operatingSystemVersion");
			ds.Sort = new SortOption("name", SortDirection.Ascending);
			if (!string.IsNullOrEmpty(baseDn))
			{
				ds.SearchRoot = new DirectoryEntry(path + "/" + baseDn, user, password, AuthenticationTypes.Secure);
			}
			using SearchResultCollection found = ds.FindAll();
			foreach (SearchResult sr in found)
			{
				Target t = new Target();
				t.Source = "ad";
				t.Host = Prop(sr, "name");
				t.Os = Prop(sr, "operatingSystem");
				string dns = Prop(sr, "dNSHostName");
				if (dns.Length > 0 && !string.Equals(dns, t.Host, StringComparison.OrdinalIgnoreCase))
				{
					t.Note = dns;
				}
				if (t.Host.Length != 0)
				{
					result.Add(t);
					if (result.Count > maxHosts)
					{
						error = "в AD больше " + maxHosts + " компьютеров — задайте OU или увеличьте лимит";
						return result;
					}
				}
			}
			return result;
		}
		catch (Exception ex)
		{
			error = WinRmSession.Describe(ex);
			return result;
		}
		finally
		{
			try
			{
				root?.Dispose();
			}
			catch
			{
			}
		}
	}

	private static string Prop(SearchResult sr, string name)
	{
		try
		{
			if (!sr.Properties.Contains(name))
			{
				return "";
			}
			if (sr.Properties[name].Count == 0)
			{
				return "";
			}
			object v = sr.Properties[name][0];
			return (v == null) ? "" : Convert.ToString(v, CultureInfo.InvariantCulture).Trim();
		}
		catch
		{
			return "";
		}
	}
}
