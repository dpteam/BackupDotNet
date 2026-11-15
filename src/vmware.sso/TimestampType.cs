using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[GeneratedCode("svcutil", "4.0.30319.1")]
[XmlType(Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd")]
[DebuggerStepThrough]
[DesignerCategory("code")]
public class TimestampType
{
	private AttributedDateTime createdField;

	private AttributedDateTime expiresField;

	private XmlElement[] itemsField;

	private string idField;

	private XmlAttribute[] anyAttrField;

	private XmlAttribute[] anyAttr1Field;

	[XmlElement(Order = 0)]
	public AttributedDateTime Created
	{
		get
		{
			return createdField;
		}
		set
		{
			createdField = value;
		}
	}

	[XmlElement(Order = 1)]
	public AttributedDateTime Expires
	{
		get
		{
			return expiresField;
		}
		set
		{
			expiresField = value;
		}
	}

	[XmlAnyElement(Order = 2)]
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

	[XmlAttribute(Form = XmlSchemaForm.Qualified, DataType = "ID")]
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
