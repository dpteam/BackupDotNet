using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[GeneratedCode("svcutil", "4.0.30319.1")]
[XmlType(Namespace = "http://www.w3.org/2000/09/xmldsig#")]
[DebuggerStepThrough]
[DesignerCategory("code")]
public class PGPDataType
{
	private object[] itemsField;

	private ItemsChoiceType1[] itemsElementNameField;

	[XmlElement("PGPKeyPacket", typeof(byte[]), DataType = "base64Binary", Order = 0)]
	[XmlAnyElement(Order = 0)]
	[XmlElement("PGPKeyID", typeof(byte[]), DataType = "base64Binary", Order = 0)]
	[XmlChoiceIdentifier("ItemsElementName")]
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
	public ItemsChoiceType1[] ItemsElementName
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
