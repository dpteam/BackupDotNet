using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management;

namespace BackupDotNet.Core;

public class NetConfigCollector : ICollector
{
	public class NetAdapter
	{
		public string Name = "";

		public string Mac = "";

		public bool Dhcp;

		public List<string> Ip = new List<string>();

		public List<string> Subnet = new List<string>();

		public List<string> Gateway = new List<string>();

		public List<string> Dns = new List<string>();

		public int SpeedMbps = -1;

		public string Status = "";

		public int Index = -1;

		public long LinkSpeedBps = -1L;

		public int NetStatus = -1;
	}

	public class Route
	{
		public string Gateway = "";

		public string Interface = "";

		public int Metric;
	}

	public string Type => "netconfig";

	public List<CollectResult> Collect(Company company, Source source, AgentCtx ctx)
	{
		List<CollectResult> list = new List<CollectResult>();
		string prefix = source.GetStr("jobName", "Сеть");
		bool checkDns = source.GetBool("checkDns", dflt: true);
		bool checkGateway = source.GetBool("checkGateway", dflt: true);
		int minSpeed = source.GetInt("minLinkSpeedMbps", 0);
		List<NetAdapter> adapters = ReadAdapters();
		CollectResult r = Make(company, source, prefix + ": адаптеры");
		if (adapters.Count == 0)
		{
			r.NoData = true;
			r.Message = "сетевые адаптеры с адресами не найдены";
			list.Add(r);
			return list;
		}
		List<string> problems = new List<string>();
		List<string> warnings = new List<string>();
		List<string> dnsAll = new List<string>();
		List<string> gatewaysAll = new List<string>();
		List<object> items = new List<object>();
		foreach (NetAdapter a in adapters)
		{
			Dictionary<string, object> x = Json.Obj();
			x["name"] = a.Name;
			x["mac"] = a.Mac;
			x["dhcp"] = a.Dhcp;
			if (a.Ip.Count > 0)
			{
				x["ip"] = ToObjList(a.Ip);
			}
			if (a.Subnet.Count > 0)
			{
				x["subnet"] = ToObjList(a.Subnet);
			}
			if (a.Gateway.Count > 0)
			{
				x["gateway"] = ToObjList(a.Gateway);
			}
			if (a.Dns.Count > 0)
			{
				x["dns"] = ToObjList(a.Dns);
			}
			if (a.SpeedMbps > 0)
			{
				x["speedMbps"] = a.SpeedMbps;
			}
			if (a.Status.Length > 0)
			{
				x["status"] = a.Status;
			}
			items.Add(x);
			foreach (string dns in a.Dns)
			{
				if (!dnsAll.Contains(dns))
				{
					dnsAll.Add(dns);
				}
			}
			foreach (string gw in a.Gateway)
			{
				if (!gatewaysAll.Contains(gw))
				{
					gatewaysAll.Add(gw);
				}
			}
			bool apipa = false;
			foreach (string ip in a.Ip)
			{
				if (ip.StartsWith("169.254.", StringComparison.Ordinal))
				{
					apipa = true;
				}
			}
			if (apipa)
			{
				problems.Add(a.Name + ": адрес 169.254.x — DHCP не ответил");
			}
			if (a.Status == "Disconnected" && a.Ip.Count > 0)
			{
				warnings.Add(a.Name + ": канал отключён, но адрес назначен");
			}
			if (minSpeed > 0 && a.SpeedMbps > 0 && a.SpeedMbps < minSpeed && a.Status == "Connected" && a.Gateway.Count > 0)
			{
				warnings.Add(a.Name + ": " + a.SpeedMbps + " Мбит/с (ожидалось от " + minSpeed + ")");
			}
		}
		if (checkGateway && gatewaysAll.Count == 0)
		{
			warnings.Add("нет маршрута по умолчанию");
		}
		if (checkDns && dnsAll.Count == 0)
		{
			warnings.Add("DNS-серверы не заданы");
		}
		Dictionary<string, object> d = Json.Obj();
		d["adapters"] = items;
		d["dns"] = ToObjList(dnsAll);
		d["gateways"] = ToObjList(gatewaysAll);
		r.Details = d;
		List<string> parts = new List<string>();
		foreach (NetAdapter a2 in adapters)
		{
			string ipText = ((a2.Ip.Count > 0) ? a2.Ip[0] : "—");
			string extra = ((a2.Ip.Count > 1) ? (" (+" + (a2.Ip.Count - 1) + ")") : "");
			parts.Add(ShortName(a2.Name) + " " + ipText + extra);
		}
		string summary = string.Join(", ", parts.ToArray());
		if (problems.Count > 0)
		{
			r.Fail(string.Join("; ", problems.ToArray()));
		}
		else if (warnings.Count > 0)
		{
			r.Message = summary + " — " + string.Join("; ", warnings.ToArray());
			r.Slow = true;
		}
		else
		{
			r.Message = summary;
		}
		list.Add(r);
		List<Route> routes = ReadDefaultRoutes();
		CollectResult rt = Make(company, source, prefix + ": маршруты");
		rt.Target = "routes";
		Dictionary<string, object> rd = Json.Obj();
		List<object> rItems = new List<object>();
		foreach (Route ro in routes)
		{
			Dictionary<string, object> x2 = Json.Obj();
			x2["gateway"] = ro.Gateway;
			x2["interface"] = ro.Interface;
			x2["metric"] = ro.Metric;
			rItems.Add(x2);
		}
		rd["count"] = routes.Count;
		rd["items"] = rItems;
		rt.Details = rd;
		if (routes.Count == 0)
		{
			rt.Message = "маршрутов по умолчанию нет";
			if (checkGateway)
			{
				rt.Slow = true;
			}
		}
		else
		{
			List<string> rParts = new List<string>();
			foreach (Route ro2 in routes)
			{
				rParts.Add(ro2.Gateway + " (метрика " + ro2.Metric + ")");
			}
			rt.Message = "по умолчанию: " + string.Join(", ", rParts.ToArray());
		}
		list.Add(rt);
		return list;
	}

	private static CollectResult Make(Company c, Source s, string job)
	{
		CollectResult r = new CollectResult();
		r.CompanyId = c.Id;
		r.CompanyName = c.Name;
		r.SourceId = s.Id;
		r.Kind = "netconfig";
		r.Job = job;
		r.IsHealth = true;
		r.Reachable = true;
		return r;
	}

	private static string ShortName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return "адаптер";
		}
		if (name.Length <= 28)
		{
			return name;
		}
		return name.Substring(0, 27) + "…";
	}

	private static List<object> ToObjList(List<string> src)
	{
		List<object> result = new List<object>();
		foreach (string s in src)
		{
			result.Add(s);
		}
		return result;
	}

	public static List<NetAdapter> ReadAdapters()
	{
		List<NetAdapter> result = new List<NetAdapter>();
		Dictionary<int, long> speeds = new Dictionary<int, long>();
		Dictionary<int, int> statuses = new Dictionary<int, int>();
		ManagementObjectCollection na = Hw.Query("root\\cimv2", "SELECT Index, Speed, NetConnectionStatus FROM Win32_NetworkAdapter");
		if (na != null)
		{
			foreach (ManagementBaseObject o in na)
			{
				using (o)
				{
					int idx = Hw.PropInt(o, "Index", -1);
					if (idx >= 0)
					{
						speeds[idx] = Hw.PropLong(o, "Speed", -1L);
						statuses[idx] = Hw.PropInt(o, "NetConnectionStatus", -1);
					}
				}
			}
		}
		ManagementObjectCollection col = Hw.Query("root\\cimv2", "SELECT Index, Description, MACAddress, DHCPEnabled, IPAddress, IPSubnet, DefaultIPGateway, DNSServerSearchOrder FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = TRUE");
		if (col == null)
		{
			return result;
		}
		foreach (ManagementBaseObject o2 in col)
		{
			using (o2)
			{
				NetAdapter a = new NetAdapter();
				a.Index = Hw.PropInt(o2, "Index", -1);
				a.Name = Hw.Prop(o2, "Description");
				a.Mac = Hw.Prop(o2, "MACAddress");
				a.Dhcp = Hw.Prop(o2, "DHCPEnabled") == "True";
				a.Ip = Hw.PropStrings(o2, "IPAddress");
				a.Subnet = Hw.PropStrings(o2, "IPSubnet");
				a.Gateway = Hw.PropStrings(o2, "DefaultIPGateway");
				a.Dns = Hw.PropStrings(o2, "DNSServerSearchOrder");
				if (speeds.TryGetValue(a.Index, out var sp) && sp > 0 && sp < 1000000000000L)
				{
					a.LinkSpeedBps = sp;
				}
				if (a.LinkSpeedBps > 0)
				{
					a.SpeedMbps = (int)(a.LinkSpeedBps / 1000000);
				}
				if (statuses.TryGetValue(a.Index, out var st))
				{
					a.NetStatus = st;
					a.Status = StatusName(st);
				}
				if (a.Name.Length > 0)
				{
					result.Add(a);
				}
			}
		}
		result.Sort((NetAdapter x, NetAdapter y) =>
		{
			int num = ((x.Gateway.Count <= 0) ? 1 : 0);
			int num2 = ((y.Gateway.Count <= 0) ? 1 : 0);
			return (num != num2) ? num.CompareTo(num2) : string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
		});
		return result;
	}

	public static List<Route> ReadDefaultRoutes()
	{
		List<Route> result = new List<Route>();
		ManagementObjectCollection col = Hw.Query("root\\cimv2", "SELECT Destination, Mask, NextHop, Metric1, InterfaceIndex FROM Win32_IP4RouteTable WHERE Destination = '0.0.0.0' AND Mask = '0.0.0.0'");
		if (col == null)
		{
			return result;
		}
		foreach (ManagementBaseObject o in col)
		{
			using (o)
			{
				Route r = new Route();
				r.Gateway = Hw.Prop(o, "NextHop");
				r.Interface = Hw.Prop(o, "InterfaceIndex");
				r.Metric = Hw.PropInt(o, "Metric1", 0);
				if (r.Gateway.Length > 0)
				{
					result.Add(r);
				}
			}
		}
		result.Sort((Route a, Route b) => a.Metric.CompareTo(b.Metric));
		return result;
	}

	public static string StatusName(int status)
	{
		return status switch
		{
			0 => "Disconnected", 
			1 => "Connecting", 
			2 => "Connected", 
			3 => "Disconnecting", 
			4 => "Hardware not present", 
			5 => "Hardware disabled", 
			6 => "Hardware malfunction", 
			7 => "Media disconnected", 
			8 => "Authenticating", 
			9 => "Authentication succeeded", 
			10 => "Authentication failed", 
			11 => "Invalid address", 
			12 => "Credentials required", 
			_ => "status " + status.ToString(CultureInfo.InvariantCulture), 
		};
	}
}
