using System;

namespace ExcelNumberFormat;

internal static class CompatibleConvert
{
	public static string ToString(object value, IFormatProvider provider)
	{
		if (!(value is double num))
		{
			if (value is float num2)
			{
				return num2.ToString("G7", provider);
			}
			return Convert.ToString(value, provider);
		}
		return num.ToString("G15", provider);
	}
}
