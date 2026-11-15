namespace ExcelNumberFormat;

internal class Condition
{
	public string Operator { get; set; }

	public double Value { get; set; }

	public bool Evaluate(double lhs)
	{
		return Operator switch
		{
			"<" => lhs < Value, 
			"<=" => lhs <= Value, 
			">" => lhs > Value, 
			">=" => lhs >= Value, 
			"<>" => lhs != Value, 
			"=" => lhs == Value, 
			_ => false, 
		};
	}
}
