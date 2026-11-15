using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web.Script.Serialization;

namespace BackupChecker.Core;

public static class Json
{
	private static readonly JavaScriptSerializer Parser = CreateParser();

	private static JavaScriptSerializer CreateParser()
	{
		JavaScriptSerializer s = new JavaScriptSerializer();
		s.MaxJsonLength = int.MaxValue;
		s.RecursionLimit = 256;
		return s;
	}

	public static object Parse(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		return Parser.DeserializeObject(text);
	}

	public static Dictionary<string, object> ParseObject(string text)
	{
		return AsObject(Parse(text));
	}

	public static List<object> ParseArray(string text)
	{
		return AsArray(Parse(text));
	}

	public static Dictionary<string, object> AsObject(object value)
	{
		if (value is Dictionary<string, object> d)
		{
			return d;
		}
		return new Dictionary<string, object>();
	}

	public static List<object> AsArray(object value)
	{
		if (value == null)
		{
			return new List<object>();
		}
		if (value is object[] arr)
		{
			return new List<object>(arr);
		}
		if (value is ArrayList al)
		{
			return new List<object>(al.ToArray());
		}
		if (value is List<object> l)
		{
			return l;
		}
		return new List<object>();
	}

	public static string Str(object v, string dflt)
	{
		if (v == null)
		{
			return dflt;
		}
		if (v is string)
		{
			return (string)v;
		}
		return Convert.ToString(v, CultureInfo.InvariantCulture);
	}

	public static string Str(object v)
	{
		return Str(v, "");
	}

	public static long Long(object v, long dflt)
	{
		if (v == null)
		{
			return dflt;
		}
		try
		{
			if (!(v is long result))
			{
				if (v is int)
				{
					return (int)v;
				}
				if (v is double)
				{
					return (long)(double)v;
				}
				if (v is decimal)
				{
					return (long)(decimal)v;
				}
				if (v is bool)
				{
					return ((bool)v) ? 1 : 0;
				}
				string s = Convert.ToString(v, CultureInfo.InvariantCulture);
				if (long.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var r))
				{
					return r;
				}
				if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
				{
					return (long)d;
				}
				return dflt;
			}
			return result;
		}
		catch
		{
			return dflt;
		}
	}

	public static long Long(object v)
	{
		return Long(v, 0L);
	}

	public static int Int(object v, int dflt)
	{
		long r = Long(v, dflt);
		if (r > int.MaxValue)
		{
			return int.MaxValue;
		}
		if (r < int.MinValue)
		{
			return int.MinValue;
		}
		return (int)r;
	}

	public static int Int(object v)
	{
		return Int(v, 0);
	}

	public static double Dbl(object v, double dflt)
	{
		if (v == null)
		{
			return dflt;
		}
		try
		{
			if (!(v is double result))
			{
				if (v is long)
				{
					return (long)v;
				}
				if (v is int)
				{
					return (int)v;
				}
				string s = Convert.ToString(v, CultureInfo.InvariantCulture);
				if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var r))
				{
					return r;
				}
				return dflt;
			}
			return result;
		}
		catch
		{
			return dflt;
		}
	}

	public static double Dbl(object v)
	{
		return Dbl(v, 0.0);
	}

	public static bool Bool(object v, bool dflt)
	{
		if (v == null)
		{
			return dflt;
		}
		if (v is bool)
		{
			return (bool)v;
		}
		string s = Convert.ToString(v, CultureInfo.InvariantCulture);
		if (string.IsNullOrEmpty(s))
		{
			return dflt;
		}
		s = s.Trim().ToLowerInvariant();
		switch (s)
		{
		default:
			return s == "on";
		case "true":
		case "1":
		case "yes":
		case "y":
			return true;
		}
	}

	public static bool Bool(object v)
	{
		return Bool(v, dflt: false);
	}

	public static object Get(Dictionary<string, object> obj, string path)
	{
		if (obj == null || string.IsNullOrEmpty(path))
		{
			return null;
		}
		string[] parts = path.Split('.');
		object cur = obj;
		for (int i = 0; i < parts.Length; i++)
		{
			if (!(cur is Dictionary<string, object> d))
			{
				return null;
			}
			if (!d.ContainsKey(parts[i]))
			{
				return null;
			}
			cur = d[parts[i]];
		}
		return cur;
	}

	public static string GetStr(Dictionary<string, object> obj, string path, string dflt)
	{
		return Str(Get(obj, path), dflt);
	}

	public static string Write(object value)
	{
		StringBuilder sb = new StringBuilder(256);
		WriteValue(sb, value, -1);
		return sb.ToString();
	}

	public static string WritePretty(object value)
	{
		StringBuilder sb = new StringBuilder(512);
		WriteValue(sb, value, 0);
		return sb.ToString();
	}

	private static void WriteValue(StringBuilder sb, object v, int indent)
	{
		if (v == null)
		{
			sb.Append("null");
		}
		else if (v is string s)
		{
			WriteString(sb, s);
		}
		else if (v is bool)
		{
			sb.Append(((bool)v) ? "true" : "false");
		}
		else if (v is DateTime dt)
		{
			WriteString(sb, ((dt.Kind == DateTimeKind.Utc) ? dt : dt.ToUniversalTime()).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
		}
		else if (v is long || v is int || v is short || v is byte)
		{
			sb.Append(Convert.ToInt64(v, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
		}
		else if (v is double d)
		{
			if (double.IsNaN(d) || double.IsInfinity(d))
			{
				sb.Append("0");
			}
			else
			{
				sb.Append(d.ToString("R", CultureInfo.InvariantCulture));
			}
		}
		else if (v is decimal)
		{
			sb.Append(((decimal)v).ToString(CultureInfo.InvariantCulture));
		}
		else if (v is BackupStatus)
		{
			sb.Append(((int)(BackupStatus)v).ToString(CultureInfo.InvariantCulture));
		}
		else if (v is IDictionary dict)
		{
			WriteObject(sb, dict, indent);
		}
		else if (v is IEnumerable en)
		{
			WriteArray(sb, en, indent);
		}
		else
		{
			WriteString(sb, Convert.ToString(v, CultureInfo.InvariantCulture));
		}
	}

	private static void WriteObject(StringBuilder sb, IDictionary dict, int indent)
	{
		sb.Append('{');
		bool first = true;
		foreach (DictionaryEntry e in dict)
		{
			if (!first)
			{
				sb.Append(',');
			}
			first = false;
			NewLine(sb, indent, 1);
			WriteString(sb, Convert.ToString(e.Key, CultureInfo.InvariantCulture));
			sb.Append(':');
			if (indent >= 0)
			{
				sb.Append(' ');
			}
			WriteValue(sb, e.Value, (indent < 0) ? (-1) : (indent + 1));
		}
		if (!first)
		{
			NewLine(sb, indent, 0);
		}
		sb.Append('}');
	}

	private static void WriteArray(StringBuilder sb, IEnumerable items, int indent)
	{
		sb.Append('[');
		bool first = true;
		foreach (object item in items)
		{
			if (!first)
			{
				sb.Append(',');
			}
			first = false;
			NewLine(sb, indent, 1);
			WriteValue(sb, item, (indent < 0) ? (-1) : (indent + 1));
		}
		if (!first)
		{
			NewLine(sb, indent, 0);
		}
		sb.Append(']');
	}

	private static void NewLine(StringBuilder sb, int indent, int extra)
	{
		if (indent >= 0)
		{
			sb.Append('\n');
			sb.Append(' ', (indent + extra) * 2);
		}
	}

	private static void WriteString(StringBuilder sb, string s)
	{
		sb.Append('"');
		foreach (char c in s)
		{
			switch (c)
			{
			case '"':
				sb.Append("\\\"");
				continue;
			case '\\':
				sb.Append("\\\\");
				continue;
			case '\b':
				sb.Append("\\b");
				continue;
			case '\f':
				sb.Append("\\f");
				continue;
			case '\n':
				sb.Append("\\n");
				continue;
			case '\r':
				sb.Append("\\r");
				continue;
			case '\t':
				sb.Append("\\t");
				continue;
			}
			if (c < ' ')
			{
				StringBuilder stringBuilder = sb.Append("\\u");
				int num = c;
				stringBuilder.Append(num.ToString("x4", CultureInfo.InvariantCulture));
			}
			else
			{
				sb.Append(c);
			}
		}
		sb.Append('"');
	}

	public static Dictionary<string, object> Obj()
	{
		return new Dictionary<string, object>();
	}

	public static Dictionary<string, object> Obj(params object[] kv)
	{
		Dictionary<string, object> d = new Dictionary<string, object>();
		for (int i = 0; i + 1 < kv.Length; i += 2)
		{
			d[Convert.ToString(kv[i], CultureInfo.InvariantCulture)] = kv[i + 1];
		}
		return d;
	}
}
