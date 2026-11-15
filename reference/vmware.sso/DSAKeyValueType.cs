using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DebuggerStepThrough]
[XmlType(Namespace = "http://www.w3.org/2000/09/xmldsig#")]
[DesignerCategory("code")]
public class DSAKeyValueType
{
	private byte[] pField;

	private byte[] qField;

	private byte[] gField;

	private byte[] yField;

	private byte[] jField;

	private byte[] seedField;

	private byte[] pgenCounterField;

	[XmlElement(DataType = "base64Binary", Order = 0)]
	public byte[] P
	{
		get
		{
			return pField;
		}
		set
		{
			pField = value;
		}
	}

	[XmlElement(DataType = "base64Binary", Order = 1)]
	public byte[] Q
	{
		get
		{
			return qField;
		}
		set
		{
			qField = value;
		}
	}

	[XmlElement(DataType = "base64Binary", Order = 2)]
	public byte[] G
	{
		get
		{
			return gField;
		}
		set
		{
			gField = value;
		}
	}

	[XmlElement(DataType = "base64Binary", Order = 3)]
	public byte[] Y
	{
		get
		{
			return yField;
		}
		set
		{
			yField = value;
		}
	}

	[XmlElement(DataType = "base64Binary", Order = 4)]
	public byte[] J
	{
		get
		{
			return jField;
		}
		set
		{
			jField = value;
		}
	}

	[XmlElement(DataType = "base64Binary", Order = 5)]
	public byte[] Seed
	{
		get
		{
			return seedField;
		}
		set
		{
			seedField = value;
		}
	}

	[XmlElement(DataType = "base64Binary", Order = 6)]
	public byte[] PgenCounter
	{
		get
		{
			return pgenCounterField;
		}
		set
		{
			pgenCounterField = value;
		}
	}
}
