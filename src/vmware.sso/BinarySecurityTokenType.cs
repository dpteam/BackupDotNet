using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[DebuggerStepThrough]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DesignerCategory("code")]
[XmlType(Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
public class BinarySecurityTokenType : EncodedString
{
	private string valueTypeField;

	private XmlAttribute[] anyAttr1Field;

	[XmlAttribute(DataType = "anyURI")]
	public string ValueType
	{
		get
		{
			return valueTypeField;
		}
		set
		{
			valueTypeField = value;
		}
	}

	[XmlAnyAttribute]
	public XmlAttribute[] AnyAttr1
	{
		get
		{
			return anyAttr1Field;
		}
		set
		{
			anyAttr1Field = value;
		}
	}
}
