using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[DebuggerStepThrough]
[XmlType(TypeName = "AttributeType", Namespace = "urn:oasis:names:tc:SAML:2.0:assertion")]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DesignerCategory("code")]
public class AttributeType1
{
	private object[] attributeValueField;

	private string nameField;

	private string nameFormatField;

	private string friendlyNameField;

	[XmlElement("AttributeValue", Order = 0)]
	public object[] AttributeValue
	{
		get
		{
			return attributeValueField;
		}
		set
		{
			attributeValueField = value;
		}
	}

	[XmlAttribute]
	public string Name
	{
		get
		{
			return nameField;
		}
		set
		{
			nameField = value;
		}
	}

	[XmlAttribute(DataType = "anyURI")]
	public string NameFormat
	{
		get
		{
			return nameFormatField;
		}
		set
		{
			nameFormatField = value;
		}
	}

	[XmlAttribute]
	public string FriendlyName
	{
		get
		{
			return friendlyNameField;
		}
		set
		{
			friendlyNameField = value;
		}
	}
}
