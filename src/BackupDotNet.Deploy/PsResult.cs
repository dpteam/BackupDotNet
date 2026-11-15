namespace BackupDotNet.Deploy;

public class PsResult
{
	public bool Ok = true;

	public string Output = "";

	public string Error = "";

	public string Text
	{
		get
		{
			if (!Ok)
			{
				if (Error.Length <= 0)
				{
					return Output;
				}
				return Error;
			}
			return Output;
		}
	}
}
