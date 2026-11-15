using System;

namespace RVTools.LoginSSPI.Security;

[Serializable]
public class SspiException : ApplicationException
{
	private readonly int _errorCode;

	public int ErrorCode => _errorCode;

	public SspiException(string message, int errorCode)
		: base($"{message}. Error Code = '{errorCode:X}'.")
	{
		_errorCode = errorCode;
	}
}
