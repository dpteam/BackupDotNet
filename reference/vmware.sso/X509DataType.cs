using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[DebuggerStepThrough]
[DesignerCategory("code")]
[GeneratedCode("svcutil", "4.0.30319.1")]
[XmlType(Namespace = "http://www.w3.org/2000/09/xmldsig#")]
public class X509DataType
{
	private object[] itemsField;

	private ItemsChoiceType[] itemsElementNameField;

	[XmlChoiceIdentifier("ItemsElementName")]
	[XmlAnyElement(Order = 0)]
	[XmlElement("X509CRL", typeof(byte[]), DataType = "base64Binary", Order = 0)]
	[XmlElement("X509Certificate", typeof(byte[]), DataType = "base64Binary", Order = 0)]
	[XmlElement("X509IssuerSerial", typeof(X509IssuerSerialType), Order = 0)]
	[XmlElement("X509SKI", typeof(byte[]), DataType = "base64Binary", Order = 0)]
	[XmlElement("X509SubjectName", typeof(string), Order = 0)]
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

	[XmlElement("ItemsElementName", Order = 1)]
	[XmlIgnore]
	public ItemsChoiceType[] ItemsElementName
	{
		get
		{
			return itemsElementNameField;
		}
		set
		{
			itemsElementNameField = value;
		}
	}
}
