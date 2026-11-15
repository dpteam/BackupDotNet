using System;
using System.Collections.Generic;
using System.Text;

namespace BackupChecker.Core;

public static class EsxiSmart
{
	public class Device
	{
		public string Id = "";

		public string Model = "";

		public string Vendor = "";

		public bool IsSsd;

		public long SizeBlocks;

		public string DisplayName = "";
	}

	public class SmartData
	{
		public string Health = "";

		public int Temperature = -1;

		public int MaxTemperature = -1;

		public long PowerOnHours = -1L;

		public long PowerCycles = -1L;

		public long Reallocated = -1L;

		public long ReadErrors = -1L;

		public long WriteErrors = -1L;

		public long Wearout = -1L;

		public Dictionary<string, string> Raw = new Dictionary<string, string>();
	}

	public static List<Device> ListDevices(string host, int sshPort, string user, string password, string dataDir, int timeoutSec)
	{
		List<Device> list = new List<Device>();
		EsxiSsh.SshResult r = EsxiSsh.Run(host, sshPort, user, password, "esxcli --formatter=csv storage core device list", dataDir, timeoutSec);
		if (!r.Ok || r.Output.Length == 0)
		{
			Log.Debug("ESXi SMART: список устройств не получен: " + r.Error);
			return list;
		}
		List<string> lines = SplitLines(r.Output);
		if (lines.Count < 2)
		{
			return list;
		}
		List<string> header = ParseCsvLine(lines[0]);
		if (header.Count == 0)
		{
			return list;
		}
		int iDevice = IndexOf(header, "Device");
		int iModel = IndexOf(header, "Model");
		int iVendor = IndexOf(header, "Vendor");
		int iSsd = IndexOf(header, "IsSSD");
		int iSize = IndexOf(header, "Size");
		int iDisplay = IndexOf(header, "DisplayName");
		int iPseudo = IndexOf(header, "IsPseudo");
		int iDriveType = IndexOf(header, "DriveType");
		if (iDevice < 0)
		{
			return list;
		}
		for (int li = 1; li < lines.Count; li++)
		{
			List<string> cols = ParseCsvLine(lines[li]);
			if (cols.Count <= iDevice)
			{
				continue;
			}
			string id = cols[iDevice].Trim();
			if (id.Length != 0 && (iPseudo < 0 || iPseudo >= cols.Count || !cols[iPseudo].Trim().Equals("true", StringComparison.OrdinalIgnoreCase)) && (iDriveType < 0 || iDriveType >= cols.Count || !cols[iDriveType].Trim().Equals("logical", StringComparison.OrdinalIgnoreCase)))
			{
				Device d = new Device();
				d.Id = id;
				if (iModel >= 0 && iModel < cols.Count)
				{
					d.Model = cols[iModel].Trim();
				}
				if (iVendor >= 0 && iVendor < cols.Count)
				{
					d.Vendor = cols[iVendor].Trim();
				}
				if (iSsd >= 0 && iSsd < cols.Count)
				{
					d.IsSsd = cols[iSsd].Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
				}
				if (iSize >= 0 && iSize < cols.Count && long.TryParse(cols[iSize].Trim(), out var sz))
				{
					d.SizeBlocks = sz;
				}
				if (iDisplay >= 0 && iDisplay < cols.Count)
				{
					d.DisplayName = cols[iDisplay].Trim();
				}
				string lowModel = d.Model.ToLowerInvariant();
				if (!lowModel.Contains("megaraid") && !lowModel.Contains("avago") && !lowModel.Contains("perc") && !lowModel.StartsWith("mr") && !lowModel.Contains("raid"))
				{
					list.Add(d);
				}
			}
		}
		return list;
	}

	private static List<string> SplitLines(string text)
	{
		List<string> lines = new List<string>();
		string[] array = text.Replace("\r\n", "\n").Split('\n');
		foreach (string raw in array)
		{
			string line = raw.TrimEnd();
			if (line.Trim().Length > 0)
			{
				lines.Add(line);
			}
		}
		return lines;
	}

	private static int IndexOf(List<string> header, string name)
	{
		for (int i = 0; i < header.Count; i++)
		{
			if (string.Equals(header[i].Trim(), name, StringComparison.OrdinalIgnoreCase))
			{
				return i;
			}
		}
		return -1;
	}

	private static List<string> ParseCsvLine(string line)
	{
		List<string> cols = new List<string>();
		StringBuilder cur = new StringBuilder();
		bool inQuotes = false;
		for (int i = 0; i < line.Length; i++)
		{
			char c = line[i];
			if (inQuotes)
			{
				if (c == '"')
				{
					if (i + 1 < line.Length && line[i + 1] == '"')
					{
						cur.Append('"');
						i++;
					}
					else
					{
						inQuotes = false;
					}
				}
				else
				{
					cur.Append(c);
				}
				continue;
			}
			switch (c)
			{
			case '"':
				inQuotes = true;
				break;
			case ',':
				cols.Add(cur.ToString());
				cur.Length = 0;
				break;
			default:
				cur.Append(c);
				break;
			}
		}
		cols.Add(cur.ToString());
		return cols;
	}

	public static SmartData ReadSmart(string host, int sshPort, string user, string password, string deviceId, string dataDir, int timeoutSec)
	{
		EsxiSsh.SshResult r = EsxiSsh.Run(host, sshPort, user, password, "esxcli --formatter=csv storage core device smart get -d " + deviceId, dataDir, timeoutSec);
		if (!r.Ok || r.Output.Length == 0)
		{
			return null;
		}
		if (r.Output.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0 && r.Output.IndexOf("Parameter", StringComparison.OrdinalIgnoreCase) < 0)
		{
			return null;
		}
		SmartData sd = new SmartData();
		string[] array = r.Output.Replace("\r\n", "\n").Split('\n');
		foreach (string raw in array)
		{
			string line = raw.Trim();
			if (line.Length == 0)
			{
				continue;
			}
			string[] parts = line.Split(',');
			if (parts.Length < 3)
			{
				continue;
			}
			string name = parts[0].Trim();
			string value = parts[2].Trim();
			if (name.Length == 0 || name.Equals("Parameter", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			sd.Raw[name] = value;
			if (!value.Equals("N/A", StringComparison.OrdinalIgnoreCase))
			{
				bool isNum = long.TryParse(value, out var num);
				if (name.Equals("Health Status", StringComparison.OrdinalIgnoreCase))
				{
					sd.Health = value;
				}
				else if (name.Equals("Drive Temperature", StringComparison.OrdinalIgnoreCase) && isNum)
				{
					sd.Temperature = (int)num;
				}
				else if (name.Equals("Driver Rated Max Temperature", StringComparison.OrdinalIgnoreCase) && isNum)
				{
					sd.MaxTemperature = (int)num;
				}
				else if (name.Equals("Power-on Hours", StringComparison.OrdinalIgnoreCase) && isNum)
				{
					sd.PowerOnHours = num;
				}
				else if (name.Equals("Power Cycle Count", StringComparison.OrdinalIgnoreCase) && isNum)
				{
					sd.PowerCycles = num;
				}
				else if (name.Equals("Reallocated Sector Count", StringComparison.OrdinalIgnoreCase) && isNum)
				{
					sd.Reallocated = num;
				}
				else if (name.Equals("Read Error Count", StringComparison.OrdinalIgnoreCase) && isNum)
				{
					sd.ReadErrors = num;
				}
				else if (name.Equals("Write Error Count", StringComparison.OrdinalIgnoreCase) && isNum)
				{
					sd.WriteErrors = num;
				}
				else if (name.Equals("Media Wearout Indicator", StringComparison.OrdinalIgnoreCase) && isNum)
				{
					sd.Wearout = num;
				}
			}
		}
		if (sd.Health.Length <= 0)
		{
			return null;
		}
		return sd;
	}
}
