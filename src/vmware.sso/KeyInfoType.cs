using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[XmlType(Namespace = "http://www.w3.org/2000/09/xmldsig#")]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DebuggerStepThrough]
[DesignerCategory("code")]
public class KeyInfoType
{
	private object[] itemsField;

	private ItemsChoiceType2[] itemsElementNameField;

	private string[] textField;

	private string idField;

	[XmlElement("MgmtData", typeof(string), Order = 0)]
	[XmlElement("RetrievalMethod", typeof(RetrievalMethodType), Order = 0)]
	[XmlChoiceIdentifier("ItemsElementName")]
	[XmlAnyElement(Order = 0)]
	[XmlElement("SPKIData", typeof(SPKIDataType), Order = 0)]
	[XmlElement("X509Data", typeof(X509DataType), Order = 0)]
	[XmlElement("KeyName", typeof(string), Order = 0)]
	[XmlElement("KeyValue", typeof(KeyValueType), Order = 0)]
	[XmlElement("PGPData", typeof(PGPDataType), Order = 0)]
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
	public ItemsChoiceType2[] ItemsElementName
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
