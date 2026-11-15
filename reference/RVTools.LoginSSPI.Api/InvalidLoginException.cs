using System;

namespace RVTools.LoginSSPI.Api;

[Serializable]
public class InvalidLoginException : ApplicationException
{
	public InvalidLoginException(string message)
		: base(message)
	{
	}
}
