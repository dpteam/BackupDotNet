using System;
using System.Globalization;

namespace ExcelNumberFormat;

internal class ExcelDateTime
{
	private static DateTime Excel1900LeapMinDate = new DateTime(1900, 2, 28);

	private static DateTime Excel1900LeapMaxDate = new DateTime(1900, 3, 1);

	private static DateTime Excel1900ZeroethMinDate = new DateTime(1899, 12, 30);

	private static DateTime Excel1900ZeroethMaxDate = new DateTime(1899, 12, 31);

	private const long TicksPerMillisecond = 10000L;

	private const long TicksPerSecond = 10000000L;

	private const long TicksPerMinute = 600000000L;

	private const long TicksPerHour = 36000000000L;

	private const long TicksPerDay = 864000000000L;

	private const int MillisPerSecond = 1000;

	private const int MillisPerMinute = 60000;

	private const int MillisPerHour = 3600000;

	private const int MillisPerDay = 86400000;

	private const int DaysPerYear = 365;

	private const int DaysPer4Years = 1461;

	private const int DaysPer100Years = 36524;

	private const int DaysPer400Years = 146097;

	private const int DaysTo1899 = 693593;

	private const long DoubleDateOffset = 599264352000000000L;

	public DateTime AdjustedDateTime { get; }

	public int AdjustDaysPost { get; }

	public int Year => AdjustedDateTime.Year;

	public int Month => AdjustedDateTime.Month;

	public int Day => AdjustedDateTime.Day + AdjustDaysPost;

	public int Hour => AdjustedDateTime.Hour;

	public int Minute => AdjustedDateTime.Minute;

	public int Second => AdjustedDateTime.Second;

	public int Millisecond => AdjustedDateTime.Millisecond;

	public DayOfWeek DayOfWeek => AdjustedDateTime.DayOfWeek;

	public ExcelDateTime(double numericDate, bool isDate1904)
	{
		if (isDate1904)
		{
			numericDate += 1462.0;
			AdjustedDateTime = new DateTime(DoubleDateToTicks(numericDate), DateTimeKind.Unspecified);
			return;
		}
		DateTime dateTime = new DateTime(DoubleDateToTicks(numericDate), DateTimeKind.Unspecified);
		if (dateTime < Excel1900ZeroethMinDate)
		{
			AdjustDaysPost = 0;
			AdjustedDateTime = dateTime.AddDays(2.0);
		}
		else if (dateTime < Excel1900ZeroethMaxDate)
		{
			AdjustDaysPost = -1;
			AdjustedDateTime = dateTime.AddDays(2.0);
		}
		else if (dateTime < Excel1900LeapMinDate)
		{
			AdjustDaysPost = 0;
			AdjustedDateTime = dateTime.AddDays(1.0);
		}
		else if (dateTime < Excel1900LeapMaxDate)
		{
			AdjustDaysPost = 1;
			AdjustedDateTime = dateTime;
		}
		else
		{
			AdjustDaysPost = 0;
			AdjustedDateTime = dateTime;
		}
	}

	public ExcelDateTime(DateTime value)
	{
		AdjustedDateTime = value;
		AdjustDaysPost = 0;
	}

	public string ToString(string numberFormat, CultureInfo culture)
	{
		return AdjustedDateTime.ToString(numberFormat, culture);
	}

	public static bool TryConvert(object value, bool isDate1904, CultureInfo culture, out ExcelDateTime result)
	{
		if (value is double numericDate)
		{
			result = new ExcelDateTime(numericDate, isDate1904);
			return true;
		}
		if (value is int num)
		{
			result = new ExcelDateTime(num, isDate1904);
			return true;
		}
		if (value is short num2)
		{
			result = new ExcelDateTime(num2, isDate1904);
			return true;
		}
		if (value is DateTime value2)
		{
			result = new ExcelDateTime(value2);
			return true;
		}
		result = null;
		return false;
	}

	internal static long DoubleDateToTicks(double value)
	{
		long num = (long)(value * 86400000.0 + ((value >= 0.0) ? 0.5 : (-0.5)));
		if (num < 0)
		{
			num -= num % 86400000 * 2;
		}
		num += 59926435200000L;
		return num * 10000;
	}
}
