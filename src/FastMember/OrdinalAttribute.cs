using System;

namespace FastMember;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class OrdinalAttribute : Attribute
{
	public ushort Ordinal { get; private set; }

	public OrdinalAttribute(ushort ordinal)
	{
		Ordinal = ordinal;
	}
}
