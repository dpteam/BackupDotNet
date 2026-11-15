using System.Globalization;

namespace BackupDotNet.Core;

public static class Fmt
{
	public static string Size(long bytes)
	{
		if (bytes <= 0)
		{
			return "—";
		}
		double b = bytes;
		string[] units = new string[6] { "Б", "КБ", "МБ", "ГБ", "ТБ", "ПБ" };
		int i = 0;
		while (b >= 1024.0 && i < units.Length - 1)
		{
			b /= 1024.0;
			i++;
		}
		return string.Format(CultureInfo.InvariantCulture, (i == 0) ? "{0:0} {1}" : "{0:0.##} {1}", b, units[i]);
	}
}
