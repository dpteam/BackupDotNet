using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DesignerCategory("code")]
[DebuggerStepThrough]
[XmlType(Namespace = "http://www.w3.org/2000/09/xmldsig#")]
public class SignaturePropertyType
{
	private XmlElement[] itemsField;

	private string[] textField;

	private string targetField;

	private string idField;

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

	[XmlText]
	public string[] Text
	{
		get
		{
			return textField;
		}
		set
		{
			textField = value;
		}
	}

	[XmlAttribute(DataType = "anyURI")]
	public string Target
	{
		get
		{
			return targetField;
		}
		set
		{
			targetField = value;
		}
	}

	[XmlAttribute(DataType = "ID")]
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
}
