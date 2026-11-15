using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DesignerCategory("code")]
[XmlType(Namespace = "http://www.rsa.com/names/2009/12/std-ext/WS-Trust1.4/advice")]
[DebuggerStepThrough]
public class AdviceType
{
	private AttributeType[] attributeField;

	private string adviceSourceField;

	[XmlElement("Attribute", Order = 0)]
	public AttributeType[] Attribute
	{
		get
		{
			return attributeField;
		}
		set
		{
			attributeField = value;
		}
	}

	[XmlAttribute(DataType = "anyURI")]
	public string AdviceSource
	{
		get
		{
			return adviceSourceField;
		}
		set
		{
			adviceSourceField = value;
		}
	}
}
