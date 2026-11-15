using System;
using System.Linq;
using System.Reflection;

namespace FastMember;

internal static class TypeHelpers
{
	public static int Min(int x, int y)
	{
		if (x >= y)
		{
			return y;
		}
		return x;
	}

	public static PropertyInfo[] GetTypeAndInterfaceProperties(this Type type, BindingFlags flags)
	{
		if (type.IsInterface)
		{
			return new Type[1] { type }.Concat(type.GetInterfaces()).SelectMany((Type i) => i.GetProperties(flags)).ToArray();
		}
		return type.GetProperties(flags);
	}
}
