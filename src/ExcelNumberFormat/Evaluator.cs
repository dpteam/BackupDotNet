using System;
using System.Collections.Generic;

namespace ExcelNumberFormat;

internal static class Evaluator
{
	public static Section GetSection(List<Section> sections, object value)
	{
		if (!(value is string))
		{
			if (value is DateTime)
			{
				_ = (DateTime)value;
				return GetFirstSection(sections, SectionType.Date);
			}
			if (!(value is TimeSpan timeSpan))
			{
				if (!(value is double value2))
				{
					if (!(value is int num))
					{
						if (value is short num2)
						{
							return GetNumericSection(sections, num2);
						}
						return null;
					}
					return GetNumericSection(sections, num);
				}
				return GetNumericSection(sections, value2);
			}
			return GetNumericSection(sections, timeSpan.TotalDays);
		}
		if (sections.Count >= 4)
		{
			return sections[3];
		}
		return null;
	}

	public static Section GetFirstSection(List<Section> sections, SectionType type)
	{
		foreach (Section section in sections)
		{
			if (section.Type == type)
			{
				return section;
			}
		}
		return null;
	}

	private static Section GetNumericSection(List<Section> sections, double value)
	{
		if (sections.Count < 1)
		{
			return null;
		}
		Section section = sections[0];
		if (section.Condition != null)
		{
			if (section.Condition.Evaluate(value))
			{
				return section;
			}
		}
		else if (sections.Count == 1 || (sections.Count == 2 && value >= 0.0) || (sections.Count >= 2 && value > 0.0))
		{
			return section;
		}
		if (sections.Count < 2)
		{
			return null;
		}
		Section section2 = sections[1];
		if (section2.Condition != null)
		{
			if (section2.Condition.Evaluate(value))
			{
				return section2;
			}
		}
		else if (value < 0.0 || (sections.Count == 2 && section.Condition != null))
		{
			return section2;
		}
		if (sections.Count < 3)
		{
			return null;
		}
		return sections[2];
	}
}
