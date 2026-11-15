namespace BackupDotNet.Core;

public class Check
{
	public string Name = "";

	public bool Passed = true;

	public string Detail = "";

	public Check()
	{
	}

	public Check(string name, bool passed, string detail)
	{
		Name = name;
		Passed = passed;
		Detail = detail;
	}
}
