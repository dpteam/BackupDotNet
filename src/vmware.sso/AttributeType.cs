using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DesignerCategory("code")]
[XmlType(Namespace = "http://www.rsa.com/names/2009/12/std-ext/WS-Trust1.4/advice")]
[DebuggerStepThrough]
public class AttributeType
{
	private object[] attributeValueField;

	private string nameField;

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

	[XmlAttribute(DataType = "normalizedString")]
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
