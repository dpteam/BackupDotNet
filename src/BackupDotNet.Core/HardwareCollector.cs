using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management;

namespace BackupDotNet.Core;

public class HardwareCollector : ICollector
{
	public string Type => "hardware";

	public List<CollectResult> Collect(Company company, Source source, AgentCtx ctx)
	{
		List<CollectResult> list = new List<CollectResult>();
		string job = source.GetStr("jobName", "Железо");
		bool useIpmi = source.GetBool("useIpmi", dflt: true);
		int tempWarn = source.GetInt("cpuTempWarn", 75);
		int tempFail = source.GetInt("cpuTempFail", 90);
		int fanMin = source.GetInt("fanMinRpm", 600);
		bool psuCritical = source.GetBool("psuFailIsCritical", dflt: true);
		string bmcHost = source.GetStr("bmcHost", "").Trim();
		string bmcUser = source.GetStr("bmcUser", "").Trim();
		string ipmiIface = source.GetStr("ipmiInterface", "lanplus").Trim();
		string bmcPassword = source.GetStr("password", "");
		if (bmcPassword.Length == 0)
		{
			string key = source.GetStr("passwordSecret", "");
			if (key.Length > 0)
			{
				bmcPassword = ctx.Config.Secret(key);
			}
		}
		Hw.IpmiOptions ipmi = new Hw.IpmiOptions();
		ipmi.Use = useIpmi;
		ipmi.Host = bmcHost;
		ipmi.User = bmcUser;
		ipmi.Password = bmcPassword;
		ipmi.Interface = ((ipmiIface.Length > 0) ? ipmiIface : "lanplus");
		string bmcTransport = source.GetStr("bmcTransport", "ipmi").Trim().ToLowerInvariant();
		Hw.HealthInfo info = ((!(bmcTransport == "ssh") || bmcHost.Length <= 0) ? ReadHealth(ipmi) : ReadHealthViaSsh(ipmi, ctx.Settings.DataDir));
		CollectResult r = Make(company, source, job);
		r.Target = info.Source;
		if (info.Sensors.Count == 0)
		{
			r.NoData = true;
			r.Message = ((info.Note.Length > 0) ? info.Note : "датчики не обнаружены (нет ipmitool, WMI пуст или это виртуальная машина)");
			list.Add(r);
			return list;
		}
		List<string> problems = new List<string>();
		List<string> warnings = new List<string>();
		List<string> okParts = new List<string>();
		foreach (Hw.Sensor s in info.Sensors)
		{
			if (s.Kind == "temp")
			{
				if (s.Value >= tempFail)
				{
					problems.Add(s.Name + " " + s.Value + " °C (порог " + tempFail + ")");
				}
				else if (s.Value >= tempWarn)
				{
					warnings.Add(s.Name + " " + s.Value + " °C");
				}
				else
				{
					okParts.Add(s.Name + " " + s.Value + " °C");
				}
			}
			else if (s.Kind == "fan")
			{
				if (s.Value <= 0)
				{
					problems.Add(s.Name + ": 0 об/мин (вентилятор стоит)");
				}
				else if (s.Value < fanMin)
				{
					warnings.Add(s.Name + " " + s.Value + " об/мин (минимум " + fanMin + ")");
				}
				else
				{
					okParts.Add(s.Name + " " + s.Value + " об/мин");
				}
			}
			else if (s.Kind == "psu")
			{
				if (s.Bad)
				{
					if (psuCritical)
					{
						problems.Add(s.Name + ": " + s.Text);
					}
					else
					{
						warnings.Add(s.Name + ": " + s.Text);
					}
				}
				else
				{
					okParts.Add(s.Name + ": OK");
				}
			}
			else if (s.Bad)
			{
				warnings.Add(s.Name + ": " + s.Text);
			}
		}
		if (problems.Count > 0)
		{
			r.Fail(string.Join("; ", problems.ToArray()));
		}
		else if (warnings.Count > 0)
		{
			r.Message = string.Join("; ", warnings.ToArray());
			r.Slow = true;
		}
		else
		{
			r.Message = string.Join(", ", okParts.ToArray());
		}
		list.Add(r);
		return list;
	}

	private static CollectResult Make(Company c, Source s, string job)
	{
		CollectResult r = new CollectResult();
		r.CompanyId = c.Id;
		r.CompanyName = c.Name;
		r.SourceId = s.Id;
		r.Kind = "hardware";
		r.Job = job;
		r.Reachable = true;
		r.IsHealth = true;
		return r;
	}

	public static Hw.HealthInfo ReadHealthViaSsh(Hw.IpmiOptions ipmi, string dataDir)
	{
		Hw.HealthInfo info = new Hw.HealthInfo();
		info.Source = "BMC SSH " + ipmi.Host;
		int sshPort = 22;
		BmcSsh.BmcInfo bmc = BmcSsh.Read(ipmi.Host, sshPort, ipmi.User, ipmi.Password, dataDir, 60);
		if (bmc.Error.Length > 0 && bmc.Sensors.Count == 0)
		{
			info.Note = "BMC SSH: " + bmc.Error;
			return info;
		}
		info.Sensors = BmcSsh.ToHwSensors(bmc.Sensors, out var skippedFans);
		List<string> note = new List<string>();
		if (bmc.PowerState == 1)
		{
			note.Add("питание включено");
		}
		else if (bmc.PowerState == 0)
		{
			note.Add("питание выключено");
		}
		if (skippedFans > 0)
		{
			note.Add("вентиляторов без вращения: " + skippedFans + " (незадействованные разъёмы)");
		}
		if (bmc.ShellVersion.Length > 0)
		{
			note.Add(bmc.ShellVersion);
		}
		info.Note = string.Join("; ", note.ToArray());
		return info;
	}

	public static Hw.HealthInfo ReadHealth(Hw.IpmiOptions ipmi)
	{
		Hw.HealthInfo info = new Hw.HealthInfo();
		if (ipmi != null && ipmi.Use)
		{
			string tool = Hw.FindTool("ipmitool");
			if (tool.Length > 0)
			{
				try
				{
					if (ReadIpmi(tool, ipmi, info))
					{
						info.Source = ((ipmi.Host.Length > 0) ? ("IPMI " + ipmi.Host) : "IPMI (локально)");
						return info;
					}
				}
				catch (Exception ex)
				{
					Log.Debug("ipmitool: " + ex.Message);
				}
			}
		}
		try
		{
			ReadAcpi(info);
		}
		catch (Exception ex2)
		{
			Log.Debug("ACPI: " + ex2.Message);
		}
		if (info.Sensors.Count > 0)
		{
			info.Source = "WMI ACPI";
			return info;
		}
		try
		{
			ReadLhm(info);
		}
		catch (Exception ex3)
		{
			Log.Debug("LHM: " + ex3.Message);
		}
		if (info.Sensors.Count > 0)
		{
			info.Source = "LibreHardwareMonitor";
			return info;
		}
		info.Note = DescribeAbsence(ipmi);
		return info;
	}

	private static string DescribeAbsence(Hw.IpmiOptions ipmi)
	{
		if (IsVirtualMachine())
		{
			return "виртуальная машина — датчиков температуры и вентиляторов нет";
		}
		bool hasTool = ipmi != null && ipmi.Use && Hw.FindTool("ipmitool").Length > 0;
		bool hasBmc = ipmi != null && ipmi.Host.Length > 0;
		if (!hasTool)
		{
			return "датчики не обнаружены: ipmitool не найден, WMI ACPI пуст. Положите ipmitool.exe в " + Branding.Current.InstallDir + "\\tools или укажите BMC в настройках источника";
		}
		if (!hasBmc)
		{
			return "ipmitool найден, но адрес BMC не задан, а локальный доступ на Windows недоступен. Укажите bmcHost / bmcUser / пароль в источнике";
		}
		return "ipmitool не смог подключиться к BMC " + ipmi.Host + " (проверьте адрес, логин и пароль)";
	}

	public static bool IsVirtualMachine()
	{
		try
		{
			ManagementObjectCollection cs = Hw.Query("root\\cimv2", "SELECT Manufacturer, Model FROM Win32_ComputerSystem");
			if (cs == null)
			{
				return false;
			}
			foreach (ManagementBaseObject o in cs)
			{
				using (o)
				{
					string m = (Hw.Prop(o, "Manufacturer") + " " + Hw.Prop(o, "Model")).ToLowerInvariant();
					if (m.Contains("virtual") || m.Contains("vmware") || m.Contains("virtualbox") || m.Contains("kvm") || m.Contains("xen") || m.Contains("hyper-v") || m.Contains("qemu") || m.Contains("parallels"))
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

	private static bool ReadIpmi(string path, Hw.IpmiOptions ipmi, Hw.HealthInfo info)
	{
		bool any = false;
		any |= RunIpmi(path, ipmi, "sdr type Temperature", "temp", info);
		any |= RunIpmi(path, ipmi, "sdr type Fan", "fan", info);
		any |= RunIpmi(path, ipmi, "sdr type \"Power Supply\"", "psu", info);
		if (!any)
		{
			any |= RunIpmi(path, ipmi, "sdr", "", info);
		}
		return any;
	}

	private static string ConnectionArgs(Hw.IpmiOptions ipmi)
	{
		if (ipmi.Host.Length == 0)
		{
			return "";
		}
		string args = "-I " + ipmi.Interface + " -H " + ipmi.Host;
		if (ipmi.User.Length > 0)
		{
			args = args + " -U " + ipmi.User;
		}
		return args + " -E";
	}

	private static Dictionary<string, string> Env(Hw.IpmiOptions ipmi)
	{
		if (ipmi.Password.Length == 0)
		{
			return null;
		}
		Dictionary<string, string> env = new Dictionary<string, string>();
		env["IPMI_PASSWORD"] = ipmi.Password;
		return env;
	}

	private static bool RunIpmi(string path, Hw.IpmiOptions ipmi, string args, string kind, Hw.HealthInfo info)
	{
		string conn = ConnectionArgs(ipmi);
		string full = ((conn.Length > 0) ? (conn + " " + args) : args);
		Hw.ExecResult r = Hw.Run(path, full, 40, Env(ipmi));
		if (r.TimedOut || string.IsNullOrEmpty(r.Output))
		{
			return false;
		}
		string low = r.Output.ToLowerInvariant();
		if (low.Contains("error:") || low.Contains("unable to") || low.Contains("invalid user"))
		{
			return false;
		}
		bool found = false;
		string[] lines = r.Output.Replace("\r\n", "\n").Split('\n');
		string[] array = lines;
		foreach (string raw in array)
		{
			string line = raw.Trim();
			if (line.Length == 0 || line.StartsWith("ID", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			string[] parts = line.Split('|');
			if (parts.Length < 4)
			{
				continue;
			}
			string name = parts[0].Trim();
			string status = parts[2].Trim();
			string reading = parts[3].Trim();
			if (name.Length == 0)
			{
				continue;
			}
			Hw.Sensor s = new Hw.Sensor();
			s.Name = name;
			string k = kind;
			if (k.Length == 0)
			{
				k = GuessKind(name, reading);
			}
			if (k.Length == 0)
			{
				continue;
			}
			s.Kind = k;
			if (k == "psu")
			{
				s.Text = ((reading.Length > 0) ? reading : status);
				s.Bad = !IsOkStatus(status);
			}
			else
			{
				long v = Hw.FirstNumber(reading);
				if (v < 0)
				{
					continue;
				}
				s.Value = v;
				s.Text = reading;
				if (k == "temp" && (v < -40 || v > 150))
				{
					continue;
				}
			}
			info.Sensors.Add(s);
			found = true;
		}
		return found;
	}

	private static string GuessKind(string name, string reading)
	{
		string n = name.ToLowerInvariant();
		string rd = (reading ?? "").ToLowerInvariant();
		if (n.Contains("fan") || rd.Contains("rpm"))
		{
			return "fan";
		}
		if (n.Contains("temp") || rd.Contains("degrees"))
		{
			return "temp";
		}
		if (n.Contains("psu") || n.Contains("power supply") || n.Contains("power"))
		{
			return "psu";
		}
		return "";
	}

	private static bool IsOkStatus(string status)
	{
		string s = (status ?? "").ToLowerInvariant();
		if (s.Length != 0 && !(s == "ok") && !(s == "ns"))
		{
			return s == "na";
		}
		return true;
	}

	private static void ReadAcpi(Hw.HealthInfo info)
	{
		ManagementObjectCollection zones = Hw.Query("root\\wmi", "SELECT * FROM MSAcpi_ThermalZoneTemperature");
		if (zones == null)
		{
			return;
		}
		int index = 0;
		foreach (ManagementBaseObject o in zones)
		{
			using (o)
			{
				index++;
				long tenthKelvin = Hw.PropLong(o, "CurrentTemperature", -1L);
				if (tenthKelvin > 0)
				{
					double c = (double)tenthKelvin / 10.0 - 273.15;
					if (!(c < -40.0) && !(c > 150.0))
					{
						Hw.Sensor s = new Hw.Sensor();
						s.Kind = "temp";
						s.Name = "Зона " + index;
						s.Value = (long)Math.Round(c);
						s.Text = s.Value + " °C";
						info.Sensors.Add(s);
					}
				}
			}
		}
	}

	private static void ReadLhm(Hw.HealthInfo info)
	{
		ManagementObjectCollection sensors = Hw.Query("root\\LibreHardwareMonitor", "SELECT * FROM Sensor");
		if (sensors == null)
		{
			return;
		}
		foreach (ManagementBaseObject o in sensors)
		{
			using (o)
			{
				string type = Hw.Prop(o, "SensorType");
				string name = Hw.Prop(o, "Name");
				double value = 0.0;
				try
				{
					value = Convert.ToDouble(o["Value"], CultureInfo.InvariantCulture);
				}
				catch
				{
					goto end_IL_002c;
				}
				Hw.Sensor s = new Hw.Sensor();
				s.Name = name;
				if (string.Equals(type, "Temperature", StringComparison.OrdinalIgnoreCase))
				{
					s.Kind = "temp";
					s.Value = (long)Math.Round(value);
					s.Text = s.Value + " °C";
					goto IL_010f;
				}
				if (string.Equals(type, "Fan", StringComparison.OrdinalIgnoreCase))
				{
					s.Kind = "fan";
					s.Value = (long)Math.Round(value);
					s.Text = s.Value + " об/мин";
					goto IL_010f;
				}
				goto end_IL_002c;
				IL_010f:
				if (s.Value > 0)
				{
					info.Sensors.Add(s);
				}
				end_IL_002c:;
			}
		}
	}
}
