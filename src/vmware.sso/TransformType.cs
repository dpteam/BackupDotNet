using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[DebuggerStepThrough]
[GeneratedCode("svcutil", "4.0.30319.1")]
[XmlType(Namespace = "http://www.w3.org/2000/09/xmldsig#")]
[DesignerCategory("code")]
public class TransformType
{
	private object[] itemsField;

	private string[] textField;

	private string algorithmField;

	[XmlElement("XPath", typeof(string), Order = 0)]
	[XmlAnyElement(Order = 0)]
	public object[] Items
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
