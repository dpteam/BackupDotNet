using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[XmlType(Namespace = "http://www.rsa.com/names/2009/12/std-ext/SAML2.0")]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DebuggerStepThrough]
[DesignerCategory("code")]
public class RSAAdviceType
{
	private AttributeType1[] attributeField;

	private string adviceSourceField;

	[XmlElement("Attribute", Namespace = "urn:oasis:names:tc:SAML:2.0:assertion", Order = 0)]
	public AttributeType1[] Attribute
	{
		get
		{
			return attributeField;
		}
		set
		{
			attributeField = value;
		}
	}

	[XmlAttribute(DataType = "anyURI")]
	public string AdviceSource
	{
		get
		{
			return adviceSourceField;
		}
		set
		{
			adviceSourceField = value;
		}
	}
}
