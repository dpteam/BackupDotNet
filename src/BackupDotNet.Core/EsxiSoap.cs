using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace BackupDotNet.Core;

public static class EsxiSoap
{
	public class VmData
	{
		public string Name = "";

		public string PowerState = "";

		public int Cpu = -1;

		public long MemoryMb = -1L;

		public string GuestFullName = "";

		public bool IsTemplate;

		public int SnapshotCount;

		public DateTime? OldestSnapshot;

		public string Summary()
		{
			List<string> parts = new List<string>();
			parts.Add((PowerState == "poweredOn") ? "Running" : PowerState);
			if (Cpu > 0)
			{
				parts.Add(Cpu + " vCPU");
			}
			if (MemoryMb > 0)
			{
				parts.Add(Fmt.Size(MemoryMb * 1024 * 1024));
			}
			if (SnapshotCount > 0)
			{
				parts.Add("снимков " + SnapshotCount);
			}
			return string.Join(", ", parts.ToArray());
		}
	}

	public class DsData
	{
		public string Name = "";

		public long Capacity;

		public long Free = -1L;

		public bool Accessible = true;

		public string Type = "";
	}

	public class HostData
	{
		public string Name = "";

		public string PowerState = "";

		public string ConnectionState = "";

		public string Version = "";

		public string Build = "";

		public string Model = "";

		public int CpuCores = -1;

		public int CpuMhz = -1;

		public long MemoryMb = -1L;

		public long CpuUsageMhz = -1L;

		public long MemoryUsedMb = -1L;

		public double UptimeHours = -1.0;

		public string Summary()
		{
			List<string> parts = new List<string>();
			if (Version.Length > 0)
			{
				parts.Add("ESXi " + Version);
			}
			if (Model.Length > 0)
			{
				parts.Add(Model);
			}
			if (CpuCores > 0)
			{
				parts.Add(CpuCores + " ядер");
			}
			if (MemoryMb > 0)
			{
				parts.Add(Fmt.Size(MemoryMb * 1024 * 1024));
			}
			if (UptimeHours >= 0.0)
			{
				parts.Add("uptime " + Uptime(UptimeHours));
			}
			if (parts.Count <= 0)
			{
				return "ESXi";
			}
			return string.Join(", ", parts.ToArray());
		}

		private static string Uptime(double hours)
		{
			if (hours < 1.0)
			{
				return (int)(hours * 60.0) + " мин";
			}
			if (hours < 48.0)
			{
				return (int)hours + " ч";
			}
			return (int)(hours / 24.0) + " дн";
		}
	}

	public class Conn
	{
		public string BaseUrl = "";

		public string SoapAction = "urn:vim25/6.0";

		public string VersionText = "";

		public CookieContainer Cookies = new CookieContainer();

		public string SessionId = "";

		public List<HostData> Hosts = new List<HostData>();

		public List<VmData> Vms = new List<VmData>();

		public List<DsData> Datastores = new List<DsData>();
	}

	public class Props
	{
		public string Type = "";

		public string Moref = "";

		private readonly Dictionary<string, List<XmlElement>> _vals = new Dictionary<string, List<XmlElement>>(StringComparer.Ordinal);

		public void Add(string name, XmlElement val)
		{
			if (!_vals.TryGetValue(name, out var list))
			{
				list = new List<XmlElement>();
				_vals[name] = list;
			}
			list.Add(val);
		}

		public string Get(string name)
		{
			if (!_vals.TryGetValue(name, out var list) || list.Count == 0)
			{
				return "";
			}
			return (list[0].InnerText ?? "").Trim();
		}

		public long GetLong(string name)
		{
			if (long.TryParse(Get(name), NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
			{
				return v;
			}
			return -1L;
		}

		public int GetInt(string name)
		{
			long v = GetLong(name);
			if (v < 0)
			{
				return -1;
			}
			if (v > int.MaxValue)
			{
				return int.MaxValue;
			}
			return (int)v;
		}

		public List<XmlElement> GetElements(string name)
		{
			if (!_vals.TryGetValue(name, out var list))
			{
				return new List<XmlElement>();
			}
			return list;
		}
	}

	public static bool Connect(string host, int port, string user, string password, bool ignoreCert, int timeoutSec, out Conn conn, out string error)
	{
		conn = new Conn();
		error = "";
		conn.BaseUrl = "https://" + host + ":" + port + "/sdk";
		conn.SoapAction = "urn:vim25/" + DetectVersion(host, port, ignoreCert, timeoutSec);
		string loginXml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\"><soapenv:Body><Login xmlns=\"urn:vim25\"><_this type=\"SessionManager\">ha-sessionmgr</_this><userName>" + Esc(user) + "</userName><password>" + Esc(password) + "</password></Login></soapenv:Body></soapenv:Envelope>";
		if (!Call(conn, loginXml, ignoreCert, timeoutSec, out var body, out error))
		{
			return false;
		}
		if (body.IndexOf("LoginResponse", StringComparison.Ordinal) < 0)
		{
			error = ExtractFault(body);
			if (error.Length == 0)
			{
				error = "логин не принят";
			}
			return false;
		}
		try
		{
			ReadAbout(conn, ignoreCert, timeoutSec);
		}
		catch
		{
		}
		try
		{
			ReadHosts(conn, ignoreCert, timeoutSec);
			ReadVms(conn, ignoreCert, timeoutSec);
			ReadDatastores(conn, ignoreCert, timeoutSec);
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
		return true;
	}

	public static void Disconnect(Conn conn)
	{
		if (conn == null)
		{
			return;
		}
		try
		{
			string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\"><soapenv:Body><Logout xmlns=\"urn:vim25\"><_this type=\"SessionManager\">ha-sessionmgr</_this></Logout></soapenv:Body></soapenv:Envelope>";
			Call(conn, xml, ignoreCert: true, 10, out var _, out var _);
		}
		catch
		{
		}
	}

	private static string DetectVersion(string host, int port, bool ignoreCert, int timeoutSec)
	{
		try
		{
			string url = "https://" + host + ":" + port + "/sdk/vimServiceVersions.xml";
			HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
			req.Method = "GET";
			req.Timeout = Math.Min(timeoutSec, 15) * 1000;
			if (ignoreCert)
			{
				req.ServerCertificateValidationCallback = (object param0, X509Certificate param1, X509Chain param2, SslPolicyErrors param3) => true;
			}
			using HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
			using StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8);
			string text = sr.ReadToEnd();
			Match m = Regex.Match(text, "<version>([\\d.]+)</version>");
			if (m.Success && m.Groups[1].Value.Length > 0)
			{
				return m.Groups[1].Value;
			}
		}
		catch
		{
		}
		return "6.0";
	}

	private static void ReadAbout(Conn conn, bool ignoreCert, int timeoutSec)
	{
		string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\"><soapenv:Body><RetrieveServiceContent xmlns=\"urn:vim25\"><_this type=\"ServiceInstance\">ServiceInstance</_this></RetrieveServiceContent></soapenv:Body></soapenv:Envelope>";
		if (Call(conn, xml, ignoreCert, timeoutSec, out var body, out var _))
		{
			Match m = Regex.Match(body, "<fullName>([^<]+)</fullName>");
			if (m.Success)
			{
				conn.VersionText = m.Groups[1].Value;
			}
		}
	}

	private static void ReadHosts(Conn conn, bool ignoreCert, int timeoutSec)
	{
		string props = PropSet("HostSystem", new string[12]
		{
			"name", "runtime.powerState", "runtime.connectionState", "summary.hardware.numCpuCores", "summary.hardware.cpuMhz", "summary.hardware.memorySize", "summary.hardware.model", "summary.quickStats.overallCpuUsage", "summary.quickStats.overallMemoryUsage", "summary.quickStats.uptime",
			"summary.config.product.version", "summary.config.product.build"
		});
		string traversal = "<selectSet xsi:type=\"TraversalSpec\"><name>tsRoot</name><type>Folder</type><path>childEntity</path><skip>false</skip><selectSet xsi:type=\"TraversalSpec\"><name>tsDChost</name><type>Datacenter</type><path>hostFolder</path><skip>false</skip><selectSet xsi:type=\"TraversalSpec\"><name>tsHostFolder</name><type>Folder</type><path>childEntity</path><skip>false</skip><selectSet xsi:type=\"TraversalSpec\"><name>tsCompute</name><type>ComputeResource</type><path>host</path><skip>false</skip></selectSet></selectSet></selectSet></selectSet>";
		List<Props> objects = Retrieve(conn, props, traversal, ignoreCert, timeoutSec);
		foreach (Props p in objects)
		{
			if (!(p.Type != "HostSystem"))
			{
				HostData h = new HostData();
				h.Name = p.Get("name");
				h.PowerState = p.Get("runtime.powerState");
				h.ConnectionState = p.Get("runtime.connectionState");
				h.CpuCores = p.GetInt("summary.hardware.numCpuCores");
				h.CpuMhz = p.GetInt("summary.hardware.cpuMhz");
				long memBytes = p.GetLong("summary.hardware.memorySize");
				if (memBytes > 0)
				{
					h.MemoryMb = memBytes / 1048576;
				}
				h.Model = p.Get("summary.hardware.model");
				h.CpuUsageMhz = p.GetLong("summary.quickStats.overallCpuUsage");
				h.MemoryUsedMb = p.GetLong("summary.quickStats.overallMemoryUsage");
				h.Version = p.Get("summary.config.product.version");
				h.Build = p.Get("summary.config.product.build");
				long uptimeSec = p.GetLong("summary.quickStats.uptime");
				if (uptimeSec > 0)
				{
					h.UptimeHours = (double)uptimeSec / 3600.0;
				}
				conn.Hosts.Add(h);
			}
		}
	}

	private static void ReadVms(Conn conn, bool ignoreCert, int timeoutSec)
	{
		string props = PropSet("VirtualMachine", new string[7] { "name", "runtime.powerState", "config.hardware.numCPU", "config.hardware.memoryMB", "config.template", "config.guestFullName", "snapshot.rootSnapshotList" });
		string traversal = "<selectSet xsi:type=\"TraversalSpec\"><name>tsRoot</name><type>Folder</type><path>childEntity</path><skip>false</skip><selectSet xsi:type=\"TraversalSpec\"><name>tsDCvm</name><type>Datacenter</type><path>vmFolder</path><skip>false</skip><selectSet xsi:type=\"TraversalSpec\"><name>tsVmFolder</name><type>Folder</type><path>childEntity</path><skip>false</skip></selectSet></selectSet></selectSet>";
		List<Props> objects = Retrieve(conn, props, traversal, ignoreCert, timeoutSec);
		foreach (Props p in objects)
		{
			if (p.Type != "VirtualMachine")
			{
				continue;
			}
			VmData vm = new VmData();
			vm.Name = p.Get("name");
			vm.PowerState = p.Get("runtime.powerState");
			vm.Cpu = p.GetInt("config.hardware.numCPU");
			vm.MemoryMb = p.GetLong("config.hardware.memoryMB");
			vm.GuestFullName = p.Get("config.guestFullName");
			vm.IsTemplate = p.Get("config.template") == "true";
			List<XmlElement> snaps = p.GetElements("snapshot.rootSnapshotList");
			vm.SnapshotCount = snaps.Count;
			DateTime oldest = DateTime.MaxValue;
			foreach (XmlElement sn in snaps)
			{
				foreach (XmlNode ch in sn.ChildNodes)
				{
					if (ch is XmlElement el && !(el.LocalName != "createTime") && DateTime.TryParse(el.InnerText, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt) && dt < oldest)
					{
						oldest = dt;
					}
				}
			}
			if (oldest != DateTime.MaxValue)
			{
				vm.OldestSnapshot = oldest.ToLocalTime();
			}
			conn.Vms.Add(vm);
		}
	}

	private static void ReadDatastores(Conn conn, bool ignoreCert, int timeoutSec)
	{
		string props = PropSet("Datastore", new string[5] { "name", "summary.capacity", "summary.freeSpace", "summary.accessible", "summary.type" });
		string traversal = "<selectSet xsi:type=\"TraversalSpec\"><name>tsRoot</name><type>Folder</type><path>childEntity</path><skip>false</skip><selectSet xsi:type=\"TraversalSpec\"><name>tsDCds</name><type>Datacenter</type><path>datastoreFolder</path><skip>false</skip><selectSet xsi:type=\"TraversalSpec\"><name>tsDsFolder</name><type>Folder</type><path>childEntity</path><skip>false</skip></selectSet></selectSet></selectSet>";
		List<Props> objects = Retrieve(conn, props, traversal, ignoreCert, timeoutSec);
		foreach (Props p in objects)
		{
			if (!(p.Type != "Datastore"))
			{
				DsData ds = new DsData();
				ds.Name = p.Get("name");
				ds.Capacity = p.GetLong("summary.capacity");
				ds.Free = p.GetLong("summary.freeSpace");
				ds.Accessible = p.Get("summary.accessible") != "false";
				ds.Type = p.Get("summary.type");
				conn.Datastores.Add(ds);
			}
		}
	}

	private static string PropSet(string type, string[] paths)
	{
		StringBuilder sb = new StringBuilder();
		sb.Append("<propSet><type>").Append(type).Append("</type><all>false</all>");
		foreach (string path in paths)
		{
			sb.Append("<pathSet>").Append(Esc(path)).Append("</pathSet>");
		}
		sb.Append("</propSet>");
		return sb.ToString();
	}

	private static List<Props> Retrieve(Conn conn, string propSets, string traversal, bool ignoreCert, int timeoutSec)
	{
		string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><soapenv:Body><RetrievePropertiesEx xmlns=\"urn:vim25\"><_this type=\"PropertyCollector\">ha-property-collector</_this><specSet>" + propSets + "<objectSet><obj type=\"Folder\">ha-folder-root</obj><skip>true</skip>" + traversal + "</objectSet></specSet><options/></RetrievePropertiesEx></soapenv:Body></soapenv:Envelope>";
		if (!Call(conn, xml, ignoreCert, timeoutSec, out var body, out var err))
		{
			throw new Exception(err);
		}
		string fault = ExtractFault(body);
		if (fault.Length > 0)
		{
			throw new Exception(fault);
		}
		return ParseObjects(body);
	}

	private static List<Props> ParseObjects(string body)
	{
		List<Props> list = new List<Props>();
		XmlDocument doc = new XmlDocument();
		try
		{
			doc.LoadXml(body);
		}
		catch
		{
			return list;
		}
		XmlNodeList objectNodes = doc.GetElementsByTagName("objects");
		foreach (XmlNode node in objectNodes)
		{
			Props p = new Props();
			foreach (XmlNode child in node.ChildNodes)
			{
				if (!(child is XmlElement el))
				{
					continue;
				}
				if (el.LocalName == "obj")
				{
					p.Type = el.GetAttribute("type");
					p.Moref = el.InnerText;
				}
				else
				{
					if (!(el.LocalName == "propSet"))
					{
						continue;
					}
					string name = "";
					XmlElement val = null;
					foreach (XmlNode pc in el.ChildNodes)
					{
						if (pc is XmlElement pe)
						{
							if (pe.LocalName == "name")
							{
								name = pe.InnerText;
							}
							else if (pe.LocalName == "val")
							{
								val = pe;
							}
						}
					}
					if (name.Length > 0 && val != null)
					{
						p.Add(name, val);
					}
				}
			}
			if (p.Type.Length > 0)
			{
				list.Add(p);
			}
		}
		return list;
	}

	private static bool Call(Conn conn, string xml, bool ignoreCert, int timeoutSec, out string body, out string error)
	{
		body = "";
		error = "";
		try
		{
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
			HttpWebRequest req = (HttpWebRequest)WebRequest.Create(conn.BaseUrl);
			req.Method = "POST";
			req.ContentType = "text/xml; charset=utf-8";
			req.Headers["SOAPAction"] = conn.SoapAction;
			req.CookieContainer = conn.Cookies;
			req.Timeout = timeoutSec * 1000;
			req.ReadWriteTimeout = timeoutSec * 1000;
			req.KeepAlive = false;
			if (ignoreCert)
			{
				req.ServerCertificateValidationCallback = (object param0, X509Certificate param1, X509Chain param2, SslPolicyErrors param3) => true;
			}
			byte[] data = Encoding.UTF8.GetBytes(xml);
			req.ContentLength = data.Length;
			using (Stream s = req.GetRequestStream())
			{
				s.Write(data, 0, data.Length);
			}
			using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
			{
				using StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8);
				body = sr.ReadToEnd();
			}
			return true;
		}
		catch (WebException ex)
		{
			if (ex.Response is HttpWebResponse resp2)
			{
				try
				{
					using StreamReader sr2 = new StreamReader(resp2.GetResponseStream(), Encoding.UTF8);
					body = sr2.ReadToEnd();
				}
				catch
				{
				}
				string fault = ExtractFault(body);
				error = ((fault.Length > 0) ? fault : ("HTTP " + (int)resp2.StatusCode));
				return false;
			}
			error = DescribeNetworkError(ex);
			return false;
		}
		catch (Exception ex2)
		{
			error = ex2.Message;
			return false;
		}
	}

	private static string DescribeNetworkError(WebException wex)
	{
		return wex.Status switch
		{
			WebExceptionStatus.Timeout => "таймаут — проверьте адрес, порт 443 и firewall", 
			WebExceptionStatus.ConnectFailure => "не удалось подключиться — порт 443 закрыт или адрес неверный", 
			WebExceptionStatus.TrustFailure => "ошибка сертификата — включите «игнорировать сертификат»", 
			WebExceptionStatus.NameResolutionFailure => "не удалось разрешить имя хоста", 
			_ => wex.Message, 
		};
	}

	private static string ExtractFault(string body)
	{
		if (string.IsNullOrEmpty(body))
		{
			return "";
		}
		if (body.IndexOf("Fault", StringComparison.Ordinal) < 0)
		{
			return "";
		}
		Match m = Regex.Match(body, "<faultstring>([^<]*)</faultstring>");
		if (m.Success)
		{
			return m.Groups[1].Value.Trim();
		}
		Match m2 = Regex.Match(body, "<localizedMessage>([^<]*)</localizedMessage>");
		if (m2.Success)
		{
			return m2.Groups[1].Value.Trim();
		}
		return "ошибка SOAP";
	}

	private static string Esc(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return "";
		}
		return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
			.Replace("\"", "&quot;")
			.Replace("'", "&apos;");
	}
}
