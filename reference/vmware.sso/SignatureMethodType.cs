using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[DebuggerStepThrough]
[DesignerCategory("code")]
[GeneratedCode("svcutil", "4.0.30319.1")]
[XmlType(Namespace = "http://www.w3.org/2000/09/xmldsig#")]
public class SignatureMethodType
{
	private string hMACOutputLengthField;

	private XmlNode[] anyField;

	private string algorithmField;

	[XmlElement(DataType = "integer", Order = 0)]
	public string HMACOutputLength
	{
		get
		{
			return hMACOutputLengthField;
		}
		set
		{
			hMACOutputLengthField = value;
		}
	}

	[XmlAnyElement(Order = 1)]
	[XmlText]
	public XmlNode[] Any
	{
		get
		{
			return anyField;
		}
		set
		{
			anyField = value;
		}
	}

	[XmlAttribute(DataType = "anyURI")]
	public string Algorithm
	{
		get
		{
			return algorithmField;
		}
		set
		{
			algorithmField = value;
		}
	}
}
