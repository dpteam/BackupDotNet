using System;
using System.Runtime.Serialization;

namespace HyperVBackUp.Engine;

public class BackupCancelledException : Exception
{
	public BackupCancelledException()
		: base("Backup cancelled")
	{
	}

	public BackupCancelledException(string message)
		: base(message)
	{
	}

	public BackupCancelledException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	protected BackupCancelledException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
