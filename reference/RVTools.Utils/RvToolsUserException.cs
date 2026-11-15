using System;

namespace RVTools.Utils;

public class RvToolsUserException : Exception
{
	private RvToolsUserException(string message)
		: base(message)
	{
	}
}
