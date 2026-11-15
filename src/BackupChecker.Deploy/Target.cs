using System.Collections.Generic;
using BackupChecker.Core;

namespace BackupChecker.Deploy;

public class Target
{
	public string Host = "";

	public string Ip = "";

	public string Os = "";

	public string Source = "";

	public bool Online;

	public bool WinRm;

	public bool Smb;

	public bool Http;

	public bool HasAgent;

	public string AgentVersion = "";

	public string Error = "";

	public string Note = "";

	public Dictionary<string, object> ToJson()
	{
		Dictionary<string, object> d = Json.Obj();
		d["host"] = Host;
		d["ip"] = Ip;
		if (Os.Length > 0)
		{
			d["os"] = Os;
		}
		d["source"] = Source;
		d["online"] = Online;
		d["winrm"] = WinRm;
		d["smb"] = Smb;
		d["http"] = Http;
		d["hasAgent"] = HasAgent;
		if (AgentVersion.Length > 0)
		{
			d["agentVersion"] = AgentVersion;
		}
		if (Error.Length > 0)
		{
			d["error"] = Error;
		}
		if (Note.Length > 0)
		{
			d["note"] = Note;
		}
		return d;
	}
}
