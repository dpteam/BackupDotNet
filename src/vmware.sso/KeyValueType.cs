using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[DesignerCategory("code")]
[GeneratedCode("svcutil", "4.0.30319.1")]
[XmlType(Namespace = "http://www.w3.org/2000/09/xmldsig#")]
[DebuggerStepThrough]
public class KeyValueType
{
	private object itemField;

	private string[] textField;

	[XmlElement("RSAKeyValue", typeof(RSAKeyValueType), Order = 0)]
	[XmlElement("DSAKeyValue", typeof(DSAKeyValueType), Order = 0)]
	[XmlAnyElement(Order = 0)]
	public object Item
	{
		get
		{
			return itemField;
		}
		set
		{
			itemField = value;
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
}
