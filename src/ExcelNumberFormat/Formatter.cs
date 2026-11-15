using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ExcelNumberFormat;

internal static class Formatter
{
	public static string Format(object value, string formatString, CultureInfo culture, bool isDate1904)
	{
		NumberFormat numberFormat = new NumberFormat(formatString);
		if (!numberFormat.IsValid)
		{
			return CompatibleConvert.ToString(value, culture);
		}
		Section section = Evaluator.GetSection(numberFormat.Sections, value);
		if (section == null)
		{
			return CompatibleConvert.ToString(value, culture);
		}
		return Format(value, section, culture, isDate1904);
	}

	public static string Format(object value, Section node, CultureInfo culture, bool isDate1904)
	{
		switch (node.Type)
		{
		case SectionType.Number:
		{
			double value2 = Convert.ToDouble(value, culture);
			if ((node.SectionIndex == 0 && node.Condition != null) || node.SectionIndex == 1)
			{
				value2 = Math.Abs(value2);
			}
			return FormatNumber(value2, node.Number, culture);
		}
		case SectionType.Date:
		{
			if (ExcelDateTime.TryConvert(value, isDate1904, culture, out var result))
			{
				return FormatDate(result, node.GeneralTextDateDurationParts, culture);
			}
			throw new FormatException("Unexpected date value");
		}
		case SectionType.Duration:
			if (value is TimeSpan timeSpan)
			{
				return FormatTimeSpan(timeSpan, node.GeneralTextDateDurationParts, culture);
			}
			return FormatTimeSpan(TimeSpan.FromDays(Convert.ToDouble(value)), node.GeneralTextDateDurationParts, culture);
		case SectionType.General:
		case SectionType.Text:
			return FormatGeneralText(CompatibleConvert.ToString(value, culture), node.GeneralTextDateDurationParts);
		case SectionType.Exponential:
			return FormatExponential(Convert.ToDouble(value, culture), node, culture);
		case SectionType.Fraction:
			return FormatFraction(Convert.ToDouble(value, culture), node, culture);
		default:
			throw new InvalidOperationException("Unknown number format section");
		}
	}

	private static string FormatGeneralText(string text, List<string> tokens)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < tokens.Count; i++)
		{
			string text2 = tokens[i];
			if (Token.IsGeneral(text2) || text2 == "@")
			{
				stringBuilder.Append(text);
			}
			else
			{
				FormatLiteral(text2, stringBuilder);
			}
		}
		return stringBuilder.ToString();
	}

	private static string FormatTimeSpan(TimeSpan timeSpan, List<string> tokens, CultureInfo culture)
	{
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		for (int num = tokens.Count - 1; num >= 0; num--)
		{
			if (tokens[num].StartsWith(".0"))
			{
				flag = true;
				break;
			}
		}
		for (int i = 0; i < tokens.Count; i++)
		{
			string text = tokens[i];
			if (text.StartsWith("m", StringComparison.OrdinalIgnoreCase))
			{
				stringBuilder.Append(timeSpan.Minutes.ToString("D" + text.Length));
			}
			else if (text.StartsWith("s", StringComparison.OrdinalIgnoreCase))
			{
				double num2 = (flag ? 0.0 : ((double)timeSpan.Milliseconds / 1000.0));
				stringBuilder.Append(((int)Math.Round((double)timeSpan.Seconds + num2, 0, MidpointRounding.AwayFromZero)).ToString("D" + text.Length));
			}
			else if (text.StartsWith("[h", StringComparison.OrdinalIgnoreCase))
			{
				stringBuilder.Append(((int)timeSpan.TotalHours).ToString("D" + (text.Length - 2)));
				timeSpan = new TimeSpan(0, 0, Math.Abs(timeSpan.Minutes), Math.Abs(timeSpan.Seconds), Math.Abs(timeSpan.Milliseconds));
			}
			else if (text.StartsWith("[m", StringComparison.OrdinalIgnoreCase))
			{
				stringBuilder.Append(((int)timeSpan.TotalMinutes).ToString("D" + (text.Length - 2)));
				timeSpan = new TimeSpan(0, 0, 0, Math.Abs(timeSpan.Seconds), Math.Abs(timeSpan.Milliseconds));
			}
			else if (text.StartsWith("[s", StringComparison.OrdinalIgnoreCase))
			{
				stringBuilder.Append(((int)timeSpan.TotalSeconds).ToString("D" + (text.Length - 2)));
				timeSpan = new TimeSpan(0, 0, 0, 0, Math.Abs(timeSpan.Milliseconds));
			}
			else if (text.StartsWith(".0"))
			{
				stringBuilder.Append("." + timeSpan.Milliseconds.ToString("D" + (text.Length - 1)));
			}
			else
			{
				FormatLiteral(text, stringBuilder);
			}
		}
		return stringBuilder.ToString();
	}

	private static string FormatDate(ExcelDateTime date, List<string> tokens, CultureInfo culture)
	{
		bool flag = ContainsAmPm(tokens);
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < tokens.Count; i++)
		{
			string text = tokens[i];
			if (text.StartsWith("y", StringComparison.OrdinalIgnoreCase))
			{
				int num = text.Length;
				if (num < 2)
				{
					num = 2;
				}
				if (num == 3)
				{
					num = 4;
				}
				int num2 = date.Year;
				if (num == 2)
				{
					num2 %= 100;
				}
				stringBuilder.Append(num2.ToString("D" + num));
			}
			else if (text.StartsWith("m", StringComparison.OrdinalIgnoreCase))
			{
				if (LookBackDatePart(tokens, i - 1, "h") || LookAheadDatePart(tokens, i + 1, "s"))
				{
					int length = text.Length;
					stringBuilder.Append(date.Minute.ToString("D" + length));
					continue;
				}
				int length2 = text.Length;
				switch (length2)
				{
				case 3:
					stringBuilder.Append(culture.DateTimeFormat.AbbreviatedMonthNames[date.Month - 1]);
					break;
				case 4:
					stringBuilder.Append(culture.DateTimeFormat.MonthNames[date.Month - 1]);
					break;
				case 5:
					stringBuilder.Append(culture.DateTimeFormat.MonthNames[date.Month - 1][0]);
					break;
				default:
					stringBuilder.Append(date.Month.ToString("D" + length2));
					break;
				}
			}
			else if (text.StartsWith("d", StringComparison.OrdinalIgnoreCase))
			{
				int length3 = text.Length;
				switch (length3)
				{
				case 3:
					stringBuilder.Append(culture.DateTimeFormat.AbbreviatedDayNames[(int)date.DayOfWeek]);
					break;
				case 4:
					stringBuilder.Append(culture.DateTimeFormat.DayNames[(int)date.DayOfWeek]);
					break;
				default:
					stringBuilder.Append(date.Day.ToString("D" + length3));
					break;
				}
			}
			else if (text.StartsWith("h", StringComparison.OrdinalIgnoreCase))
			{
				int length4 = text.Length;
				if (flag)
				{
					stringBuilder.Append(((date.Hour + 11) % 12 + 1).ToString("D" + length4));
				}
				else
				{
					stringBuilder.Append(date.Hour.ToString("D" + length4));
				}
			}
			else if (text.StartsWith("s", StringComparison.OrdinalIgnoreCase))
			{
				int length5 = text.Length;
				stringBuilder.Append(date.Second.ToString("D" + length5));
			}
			else if (text.StartsWith("g", StringComparison.OrdinalIgnoreCase))
			{
				int era = culture.DateTimeFormat.Calendar.GetEra(date.AdjustedDateTime);
				if (text.Length < 3)
				{
					stringBuilder.Append(culture.DateTimeFormat.GetAbbreviatedEraName(era));
				}
				else
				{
					stringBuilder.Append(culture.DateTimeFormat.GetEraName(era));
				}
			}
			else if (string.Compare(text, "am/pm", StringComparison.OrdinalIgnoreCase) == 0)
			{
				string text2 = date.ToString("tt", CultureInfo.InvariantCulture);
				stringBuilder.Append(text2.ToUpperInvariant());
			}
			else if (string.Compare(text, "a/p", StringComparison.OrdinalIgnoreCase) == 0)
			{
				string text3 = date.ToString("%t", CultureInfo.InvariantCulture);
				if (char.IsUpper(text[0]))
				{
					stringBuilder.Append(text3.ToUpperInvariant());
				}
				else
				{
					stringBuilder.Append(text3.ToLowerInvariant());
				}
			}
			else if (text.StartsWith(".0"))
			{
				stringBuilder.Append("." + date.Millisecond.ToString("D" + (text.Length - 1)));
			}
			else if (text == "/")
			{
				stringBuilder.Append(culture.DateTimeFormat.DateSeparator);
			}
			else if (text == ",")
			{
				for (; i < tokens.Count - 1 && tokens[i + 1] == ","; i++)
				{
				}
				stringBuilder.Append(",");
			}
			else
			{
				FormatLiteral(text, stringBuilder);
			}
		}
		return stringBuilder.ToString();
	}

	private static bool LookAheadDatePart(List<string> tokens, int fromIndex, string startsWith)
	{
		for (int i = fromIndex; i < tokens.Count; i++)
		{
			string text = tokens[i];
			if (text.StartsWith(startsWith, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			if (Token.IsDatePart(text))
			{
				return false;
			}
		}
		return false;
	}

	private static bool LookBackDatePart(List<string> tokens, int fromIndex, string startsWith)
	{
		for (int num = fromIndex; num >= 0; num--)
		{
			string text = tokens[num];
			if (text.StartsWith(startsWith, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			if (Token.IsDatePart(text))
			{
				return false;
			}
		}
		return false;
	}

	private static bool ContainsAmPm(List<string> tokens)
	{
		foreach (string token in tokens)
		{
			if (string.Compare(token, "am/pm", StringComparison.OrdinalIgnoreCase) == 0)
			{
				return true;
			}
			if (string.Compare(token, "a/p", StringComparison.OrdinalIgnoreCase) == 0)
			{
				return true;
			}
		}
		return false;
	}

	private static string FormatNumber(double value, DecimalSection format, CultureInfo culture)
	{
		bool thousandSeparator = format.ThousandSeparator;
		value /= format.ThousandDivisor;
		value *= format.PercentMultiplier;
		StringBuilder stringBuilder = new StringBuilder();
		FormatNumber(value, format.BeforeDecimal, format.DecimalSeparator, format.AfterDecimal, thousandSeparator, culture, stringBuilder);
		return stringBuilder.ToString();
	}

	private static void FormatNumber(double value, List<string> beforeDecimal, bool decimalSeparator, List<string> afterDecimal, bool thousandSeparator, CultureInfo culture, StringBuilder result)
	{
		int num = 0;
		if (afterDecimal != null)
		{
			num = GetDigitCount(afterDecimal);
		}
		string[] array = Math.Abs(value).ToString("F" + num, CultureInfo.InvariantCulture).Split('.');
		string valueString = array[0];
		string valueString2 = ((array.Length > 1) ? array[1].TrimEnd('0') : "");
		if (value < 0.0)
		{
			result.Append("-");
		}
		if (beforeDecimal != null)
		{
			FormatThousands(valueString, thousandSeparator, significantZero: false, beforeDecimal, culture, result);
		}
		if (decimalSeparator)
		{
			result.Append(culture.NumberFormat.NumberDecimalSeparator);
		}
		if (afterDecimal != null)
		{
			FormatDecimals(valueString2, afterDecimal, result);
		}
	}

	public static void FormatThousands(string valueString, bool thousandSeparator, bool significantZero, List<string> tokens, CultureInfo culture, StringBuilder result)
	{
		bool flag = false;
		int digitCount = GetDigitCount(tokens);
		valueString = valueString.PadLeft(digitCount, '0');
		int i;
		for (i = 0; i < tokens.Count; i++)
		{
			string token = tokens[i];
			if (Token.IsPlaceholder(token))
			{
				break;
			}
			FormatLiteral(token, result);
		}
		int j;
		for (j = 0; j < valueString.Length - digitCount; j++)
		{
			flag = true;
			result.Append(valueString[j]);
			if (thousandSeparator)
			{
				FormatThousandSeparator(valueString, j, culture, result);
			}
		}
		for (; i < tokens.Count; i++)
		{
			string text = tokens[i];
			if (Token.IsPlaceholder(text))
			{
				char c = valueString[j];
				if (c != '0' || (significantZero && j == valueString.Length - 1))
				{
					flag = true;
				}
				FormatPlaceholder(text, c, flag, result);
				if (thousandSeparator && (flag || text.Equals("0")))
				{
					FormatThousandSeparator(valueString, j, culture, result);
				}
				j++;
			}
			else
			{
				FormatLiteral(text, result);
			}
		}
	}

	private static void FormatThousandSeparator(string valueString, int digit, CultureInfo culture, StringBuilder result)
	{
		int num = valueString.Length - 1 - digit;
		if (num > 0 && num % 3 == 0)
		{
			result.Append(culture.NumberFormat.NumberGroupSeparator);
		}
	}

	public static void FormatDecimals(string valueString, List<string> tokens, StringBuilder result)
	{
		bool flag = true;
		int length = valueString.Length;
		int digitCount = GetDigitCount(tokens);
		valueString = valueString.PadRight(digitCount, '0');
		int num = 0;
		for (int i = 0; i < tokens.Count; i++)
		{
			string token = tokens[i];
			if (Token.IsPlaceholder(token))
			{
				char c = valueString[num];
				flag = num < length;
				FormatPlaceholder(token, c, flag, result);
				num++;
			}
			else
			{
				FormatLiteral(token, result);
			}
		}
	}

	private static string FormatExponential(double value, Section format, CultureInfo culture)
	{
		int num = 0;
		if (format.Exponential.BeforeDecimal != null)
		{
			num = GetDigitCount(format.Exponential.BeforeDecimal);
		}
		int num2 = (int)Math.Floor(Math.Log10(Math.Abs(value)));
		double num3 = value / Math.Pow(10.0, num2);
		int num4 = Math.Abs(num2) % num;
		if (num4 > 0)
		{
			if (num2 < 0)
			{
				num4 = num - num4;
			}
			num3 *= Math.Pow(10.0, num4);
			num2 -= num4;
		}
		StringBuilder stringBuilder = new StringBuilder();
		FormatNumber(num3, format.Exponential.BeforeDecimal, format.Exponential.DecimalSeparator, format.Exponential.AfterDecimal, thousandSeparator: false, culture, stringBuilder);
		stringBuilder.Append(format.Exponential.ExponentialToken[0]);
		if (format.Exponential.ExponentialToken[1] == '+' && num2 >= 0)
		{
			stringBuilder.Append("+");
		}
		else if (num2 < 0)
		{
			stringBuilder.Append("-");
		}
		FormatThousands(Math.Abs(num2).ToString(CultureInfo.InvariantCulture), thousandSeparator: false, significantZero: false, format.Exponential.Power, culture, stringBuilder);
		return stringBuilder.ToString();
	}

	private static string FormatFraction(double value, Section format, CultureInfo culture)
	{
		int num = 0;
		bool flag = value < 0.0;
		if (format.Fraction.IntegerPart != null)
		{
			num = (int)Math.Truncate(value);
			value = Math.Abs(value - (double)num);
		}
		int den;
		int nom;
		if (format.Fraction.DenominatorConstant != 0)
		{
			den = format.Fraction.DenominatorConstant;
			double num2 = Math.Round(value * (double)den);
			double num3 = Math.Floor(num2 / (double)den);
			nom = (int)(num2 - num3 * (double)den);
		}
		else
		{
			int num4 = Math.Min(GetDigitCount(format.Fraction.Denominator), 7);
			GetFraction(value, (int)Math.Pow(10.0, num4) - 1, out nom, out den);
		}
		int zeroCount = GetZeroCount(format.Fraction.Numerator);
		bool flag2 = format.Fraction.IntegerPart != null && nom == 0 && zeroCount == 0;
		StringBuilder stringBuilder = new StringBuilder();
		if (flag)
		{
			stringBuilder.Append("-");
		}
		if (format.Fraction.IntegerPart != null)
		{
			FormatThousands(Math.Abs(num).ToString("F0", CultureInfo.InvariantCulture), thousandSeparator: false, flag2, format.Fraction.IntegerPart, culture, stringBuilder);
		}
		string valueString = Math.Abs(nom).ToString("F0", CultureInfo.InvariantCulture);
		string valueString2 = den.ToString("F0", CultureInfo.InvariantCulture);
		StringBuilder stringBuilder2 = new StringBuilder();
		FormatThousands(valueString, thousandSeparator: false, significantZero: true, format.Fraction.Numerator, culture, stringBuilder2);
		stringBuilder2.Append("/");
		if (format.Fraction.DenominatorPrefix != null)
		{
			FormatThousands("", thousandSeparator: false, significantZero: false, format.Fraction.DenominatorPrefix, culture, stringBuilder2);
		}
		if (format.Fraction.DenominatorConstant != 0)
		{
			stringBuilder2.Append(format.Fraction.DenominatorConstant.ToString());
		}
		else
		{
			FormatDenominator(valueString2, format.Fraction.Denominator, stringBuilder2);
		}
		if (format.Fraction.DenominatorSuffix != null)
		{
			FormatThousands("", thousandSeparator: false, significantZero: false, format.Fraction.DenominatorSuffix, culture, stringBuilder2);
		}
		if (flag2)
		{
			stringBuilder.Append(new string(' ', stringBuilder2.ToString().Length));
		}
		else
		{
			stringBuilder.Append(stringBuilder2.ToString());
		}
		if (format.Fraction.FractionSuffix != null)
		{
			FormatThousands("", thousandSeparator: false, significantZero: false, format.Fraction.FractionSuffix, culture, stringBuilder);
		}
		return stringBuilder.ToString();
	}

	private static void GetFraction(double x, int D, out int nom, out int den)
	{
		int num = ((!(x < 0.0)) ? 1 : (-1));
		double num2 = x * (double)num;
		double num3 = 0.0;
		double num4 = 1.0;
		double num5 = 0.0;
		double num6 = 1.0;
		double num7 = 0.0;
		double num8 = 0.0;
		double num9 = Math.Floor(num2);
		while (num7 < (double)D)
		{
			num9 = Math.Floor(num2);
			num5 = num9 * num4 + num3;
			num8 = num9 * num7 + num6;
			if (num2 - num9 < 5E-08)
			{
				break;
			}
			num2 = 1.0 / (num2 - num9);
			num3 = num4;
			num4 = num5;
			num6 = num7;
			num7 = num8;
		}
		if (num8 > (double)D)
		{
			if (num7 > (double)D)
			{
				num8 = num6;
				num5 = num3;
			}
			else
			{
				num8 = num7;
				num5 = num4;
			}
		}
		nom = (int)((double)num * num5);
		den = (int)num8;
	}

	public static void FormatDenominator(string valueString, List<string> tokens, StringBuilder result)
	{
		int digitCount = GetDigitCount(tokens);
		valueString = valueString.PadLeft(digitCount, '0');
		bool significant = false;
		int valueIndex = 0;
		for (int i = 0; i < tokens.Count; i++)
		{
			string token = tokens[i];
			char c;
			if (valueIndex < valueString.Length)
			{
				c = GetLeftAlignedValueDigit(token, valueString, valueIndex, significant, out valueIndex);
				if (c != '0')
				{
					significant = true;
				}
			}
			else
			{
				c = '0';
				significant = false;
			}
			FormatPlaceholder(token, c, significant, result);
		}
	}

	private static char GetLeftAlignedValueDigit(string token, string valueString, int startIndex, bool significant, out int valueIndex)
	{
		valueIndex = startIndex;
		char c;
		if (valueIndex < valueString.Length)
		{
			c = valueString[valueIndex];
			valueIndex++;
			if (c != '0')
			{
				significant = true;
			}
			if (token == "?" && !significant)
			{
				while (valueIndex < valueString.Length)
				{
					c = valueString[valueIndex];
					valueIndex++;
					if (c != '0')
					{
						significant = true;
						break;
					}
				}
			}
		}
		else
		{
			c = '0';
			significant = false;
		}
		return c;
	}

	private static void FormatPlaceholder(string token, char c, bool significant, StringBuilder result)
	{
		switch (token)
		{
		case "0":
			if (significant)
			{
				result.Append(c);
			}
			else
			{
				result.Append("0");
			}
			break;
		case "#":
			if (significant)
			{
				result.Append(c);
			}
			break;
		case "?":
			if (significant)
			{
				result.Append(c);
			}
			else
			{
				result.Append(" ");
			}
			break;
		}
	}

	private static int GetDigitCount(List<string> tokens)
	{
		int num = 0;
		foreach (string token in tokens)
		{
			if (Token.IsPlaceholder(token))
			{
				num++;
			}
		}
		return num;
	}

	private static int GetZeroCount(List<string> tokens)
	{
		int num = 0;
		foreach (string token in tokens)
		{
			if (token == "0")
			{
				num++;
			}
		}
		return num;
	}

	private static void FormatLiteral(string token, StringBuilder result)
	{
		string value = string.Empty;
		if (!(token == ","))
		{
			if (token.Length == 2 && (token[0] == '*' || token[0] == '\\'))
			{
				value = token[1].ToString();
			}
			else if (token.Length == 2 && token[0] == '_')
			{
				value = " ";
			}
			else
			{
				value = ((!token.StartsWith("\"")) ? token : token.Substring(1, token.Length - 2));
			}
		}
		result.Append(value);
	}
}
