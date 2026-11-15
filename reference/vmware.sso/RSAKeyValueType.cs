using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[DebuggerStepThrough]
[DesignerCategory("code")]
[GeneratedCode("svcutil", "4.0.30319.1")]
[XmlType(Namespace = "http://www.w3.org/2000/09/xmldsig#")]
public class RSAKeyValueType
{
	private byte[] modulusField;

	private byte[] exponentField;

	[XmlElement(DataType = "base64Binary", Order = 0)]
	public byte[] Modulus
	{
		get
		{
			return modulusField;
		}
		set
		{
			modulusField = value;
		}
	}

	[XmlElement(DataType = "base64Binary", Order = 1)]
	public byte[] Exponent
	{
		get
		{
			return exponentField;
		}
		set
		{
			exponentField = value;
		}
	}
}
