namespace BackupDotNet.Manage;

public class OpResult
{
	public bool Ok;

	public string Output = "";

	public string Error = "";

	public string Detail = "";

	public long DurationMs;

	public static OpResult Fail(string message)
	{
		OpResult r = new OpResult();
		r.Ok = false;
		r.Error = message;
		return r;
	}
}
