using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[DebuggerStepThrough]
[XmlType(Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DesignerCategory("code")]
public class SecurityTokenReferenceType
{
	private XmlElement[] itemsField;

	private string idField;

	private string[] usageField;

	private XmlAttribute[] anyAttrField;

	[XmlAnyElement(Order = 0)]
	public XmlElement[] Items
	{
		get
		{
			return itemsField;
		}
		set
		{
			itemsField = value;
		}
	}

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

	[XmlAttribute(Form = XmlSchemaForm.Qualified, DataType = "anyURI")]
	public string[] Usage
	{
		get
		{
			return usageField;
		}
		set
		{
			usageField = value;
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
