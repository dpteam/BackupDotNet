using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[XmlType(Namespace = "http://www.w3.org/2000/09/xmldsig#")]
[DesignerCategory("code")]
[DebuggerStepThrough]
[GeneratedCode("svcutil", "4.0.30319.1")]
public class DigestMethodType
{
	private XmlNode[] anyField;

	private string algorithmField;

	[XmlText]
	[XmlAnyElement(Order = 0)]
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
