using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[XmlType(Namespace = "http://www.w3.org/2005/08/addressing")]
[DesignerCategory("code")]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DebuggerStepThrough]
public class AttributedURIType
{
	private XmlAttribute[] anyAttrField;

	private string valueField;

	[XmlAnyAttribute]
	public XmlAttribute[] AnyAttr
	{
		get
		{
			return anyAttrField;
		}
		set
		{
			anyAttrField = value;
		}
	}

	[XmlText(DataType = "anyURI")]
	public string Value
	{
		get
		{
			return valueField;
		}
		set
		{
			valueField = value;
		}
	}
}
