using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[DebuggerStepThrough]
[XmlType(Namespace = "http://www.w3.org/2005/08/addressing")]
[DesignerCategory("code")]
[GeneratedCode("svcutil", "4.0.30319.1")]
public class MetadataType
{
	private XmlElement[] anyField;

	private XmlAttribute[] anyAttrField;

	[XmlAnyElement(Order = 0)]
	public XmlElement[] Any
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
}
