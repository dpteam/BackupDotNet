using System.IO;

namespace BackupDotNet.Core;

public class AgentCtx
{
	public Settings Settings;

	public ConfigLoader Config;

	public DataStore Store;

	public string TempDir
	{
		get
		{
			string d = Settings.ZipTempDir;
			if (string.IsNullOrEmpty(d))
			{
				d = Path.Combine(Settings.DataDir, "cache");
			}
			Directory.CreateDirectory(d);
			return d;
		}
	}

	public AgentCtx(Settings settings, ConfigLoader config, DataStore store)
	{
		Settings = settings;
		Config = config;
		Store = store;
	}
}
