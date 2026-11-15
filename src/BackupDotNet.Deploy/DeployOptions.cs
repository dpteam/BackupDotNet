namespace BackupDotNet.Deploy;

public class DeployOptions
{
	public string Mode = "agent";

	public string CentralUrl = "";

	public string InstallDir = "";

	public string ServiceName = "";

	public bool Https;

	public int WinRmPort;

	public bool KeepFiles;

	public bool RemoveAgent = true;
}
