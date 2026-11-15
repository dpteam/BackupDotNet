using System;
using System.Collections.Generic;

namespace BackupDotNet.Core;

public static class Collectors
{
	private static readonly Dictionary<string, ICollector> Registry;

	public static IEnumerable<string> Types => Registry.Keys;

	static Collectors()
	{
		Registry = new Dictionary<string, ICollector>(StringComparer.OrdinalIgnoreCase);
		Register(new FileAgeCollector());
		Register(new ZipCollector());
		Register(new VeeamFilesCollector());
		Register(new VeeamRestCollector());
		Register(new FtpZipCollector());
		Register(new HttpCollector());
		Register(new TcpCollector());
		Register(new PingCollector());
		Register(new DnsCollector());
		Register(new PushCollector());
		Register(new SmartCollector());
		Register(new RaidCollector());
		Register(new HardwareCollector());
		Register(new HyperVCollector());
		Register(new MikroTikCollector());
		Register(new EsxiCollector());
		Register(new InventoryCollector());
		Register(new VssCollector());
		Register(new EventLogCollector());
		Register(new NetConfigCollector());
		Register(new TasksCollector());
	}

	public static void Register(ICollector c)
	{
		Registry[c.Type] = c;
	}

	public static ICollector Get(string type)
	{
		if (Registry.TryGetValue(type, out var c))
		{
			return c;
		}
		return null;
	}
}
