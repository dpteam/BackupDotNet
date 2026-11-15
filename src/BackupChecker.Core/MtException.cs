using System;

namespace BackupChecker.Core;

public class MtException : Exception
{
	public MtErrorKind Kind;

	public MtException(MtErrorKind kind, string message)
		: base(message)
	{
		Kind = kind;
	}

	public MtException(MtErrorKind kind, string message, Exception inner)
		: base(message, inner)
	{
		Kind = kind;
	}
}
