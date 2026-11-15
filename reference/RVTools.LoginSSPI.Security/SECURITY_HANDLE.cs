namespace RVTools.LoginSSPI.Security;

public struct SECURITY_HANDLE
{
	public uint LowPart;

	public uint HighPart;

	public SECURITY_HANDLE(int dummy)
	{
		LowPart = (HighPart = 0u);
	}
}
