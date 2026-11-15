using System;
using System.Linq;
using System.Reflection;

namespace FastMember;

public sealed class Member
{
	private readonly MemberInfo member;

	public int Ordinal
	{
		get
		{
			CustomAttributeData customAttributeData = member.CustomAttributes.FirstOrDefault((CustomAttributeData p) => p.AttributeType == typeof(OrdinalAttribute));
			if (customAttributeData == null)
			{
				return -1;
			}
			return Convert.ToInt32(customAttributeData.ConstructorArguments.Single().Value);
		}
	}

	public string Name => member.Name;

	public Type Type
	{
		get
		{
			if (member is FieldInfo)
			{
				return ((FieldInfo)member).FieldType;
			}
			if (member is PropertyInfo)
			{
				return ((PropertyInfo)member).PropertyType;
			}
			throw new NotSupportedException(member.GetType().Name);
		}
	}

	public bool CanWrite
	{
		get
		{
			if (member.MemberType == MemberTypes.Property)
			{
				return ((PropertyInfo)member).CanWrite;
			}
			throw new NotSupportedException(member.MemberType.ToString());
		}
	}

	public bool CanRead
	{
		get
		{
			if (member.MemberType == MemberTypes.Property)
			{
				return ((PropertyInfo)member).CanRead;
			}
			throw new NotSupportedException(member.MemberType.ToString());
		}
	}

	internal Member(MemberInfo member)
	{
		this.member = member;
	}

	public bool IsDefined(Type attributeType)
	{
		if (attributeType == null)
		{
			throw new ArgumentNullException("attributeType");
		}
		return Attribute.IsDefined(member, attributeType);
	}

	public Attribute GetAttribute(Type attributeType, bool inherit)
	{
		return Attribute.GetCustomAttribute(member, attributeType, inherit);
	}
}
