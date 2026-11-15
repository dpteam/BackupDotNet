using System;
using System.Collections.Generic;
using System.Globalization;

namespace ExcelNumberFormat;

public class NumberFormat
{
	public bool IsValid { get; }

	public string FormatString { get; }

	public bool IsDateTimeFormat { get; }

	public bool IsTimeSpanFormat { get; }

	internal List<Section> Sections { get; }

	public NumberFormat(string formatString)
	{
		List<Section> list = Parser.ParseSections(formatString, out var syntaxError);
		IsValid = !syntaxError;
		FormatString = formatString;
		if (IsValid)
		{
			Sections = list;
			IsDateTimeFormat = Evaluator.GetFirstSection(Sections, SectionType.Date) != null;
			IsTimeSpanFormat = Evaluator.GetFirstSection(Sections, SectionType.Duration) != null;
		}
		else
		{
			Sections = new List<Section>();
		}
	}

	public string Format(object value, CultureInfo culture, bool isDate1904 = false)
	{
		Section section = Evaluator.GetSection(Sections, value);
		if (section == null)
		{
			return CompatibleConvert.ToString(value, culture);
		}
		try
		{
			return Formatter.Format(value, section, culture, isDate1904);
		}
		catch (InvalidCastException)
		{
			return CompatibleConvert.ToString(value, culture);
		}
		catch (FormatException)
		{
			return CompatibleConvert.ToString(value, culture);
		}
	}
}
