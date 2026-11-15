using System.Collections.Generic;

namespace ExcelNumberFormat;

internal class ExponentialSection
{
	public List<string> BeforeDecimal { get; set; }

	public bool DecimalSeparator { get; set; }

	public List<string> AfterDecimal { get; set; }

	public string ExponentialToken { get; set; }

	public List<string> Power { get; set; }

	public static bool TryParse(List<string> tokens, out ExponentialSection format)
	{
		format = null;
		int num = Parser.ParseNumberTokens(tokens, 0, out var beforeDecimal, out var decimalSeparator, out var afterDecimal);
		if (num == 0)
		{
			return false;
		}
		int num2 = num;
		if (num2 < tokens.Count && Token.IsExponent(tokens[num2]))
		{
			string exponentialToken = tokens[num2];
			num2++;
			format = new ExponentialSection
			{
				BeforeDecimal = beforeDecimal,
				DecimalSeparator = decimalSeparator,
				AfterDecimal = afterDecimal,
				ExponentialToken = exponentialToken,
				Power = tokens.GetRange(num2, tokens.Count - num2)
			};
			return true;
		}
		return false;
	}
}
