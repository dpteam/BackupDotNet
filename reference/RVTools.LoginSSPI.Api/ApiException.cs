using System;

namespace RVTools.LoginSSPI.Api;

[Serializable]
public class ApiException : ApplicationException
{
	public ApiException(string message)
		: base(message)
	{
	}
}
