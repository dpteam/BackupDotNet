using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DebuggerStepThrough]
[DesignerCategory("code")]
[XmlType(TypeName = "ReferenceType", Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
public class ReferenceType1
{
	private string uRIField;

	private string valueTypeField;

	private XmlAttribute[] anyAttrField;

	[XmlAttribute(DataType = "anyURI")]
	public string URI
	{
		get
		{
			return uRIField;
		}
		set
		{
			uRIField = value;
		}
	}

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
