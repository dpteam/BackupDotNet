using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[XmlInclude(typeof(KeyIdentifierType))]
[XmlInclude(typeof(EncodedString))]
[GeneratedCode("svcutil", "4.0.30319.1")]
[XmlType(Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
[XmlInclude(typeof(PasswordString))]
[XmlInclude(typeof(BinarySecurityTokenType))]
[DebuggerStepThrough]
[DesignerCategory("code")]
public class AttributedString
{
	private string idField;

	private XmlAttribute[] anyAttrField;

	private string valueField;

	[XmlAttribute(Form = XmlSchemaForm.Qualified, Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd", DataType = "ID")]
	public string Id
	{
		get
		{
			return idField;
		}
		set
		{
			idField = value;
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

	[XmlText]
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
