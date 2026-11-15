using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[XmlType(Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
[DesignerCategory("code")]
[XmlInclude(typeof(KeyIdentifierType))]
[XmlInclude(typeof(BinarySecurityTokenType))]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DebuggerStepThrough]
public class EncodedString : AttributedString
{
	private string encodingTypeField;

	[XmlAttribute(DataType = "anyURI")]
	public string EncodingType
	{
		get
		{
			return encodingTypeField;
		}
		set
		{
			encodingTypeField = value;
		}
	}

	public EncodedString()
	{
		encodingTypeField = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary";
	}
}
