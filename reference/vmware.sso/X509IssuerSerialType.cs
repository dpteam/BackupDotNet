using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[XmlType(Namespace = "http://www.w3.org/2000/09/xmldsig#")]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DebuggerStepThrough]
[DesignerCategory("code")]
public class X509IssuerSerialType
{
	private string x509IssuerNameField;

	private string x509SerialNumberField;

	[XmlElement(Order = 0)]
	public string X509IssuerName
	{
		get
		{
			return x509IssuerNameField;
		}
		set
		{
			x509IssuerNameField = value;
		}
	}

	[XmlElement(DataType = "integer", Order = 1)]
	public string X509SerialNumber
	{
		get
		{
			return x509SerialNumberField;
		}
		set
		{
			x509SerialNumberField = value;
		}
	}
}
