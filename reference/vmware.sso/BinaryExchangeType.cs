using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[DebuggerStepThrough]
[XmlType(Namespace = "http://docs.oasis-open.org/ws-sx/ws-trust/200512")]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DesignerCategory("code")]
public class BinaryExchangeType
{
	private string valueTypeField;

	private string encodingTypeField;

	private string valueField;

	[XmlAttribute(DataType = "anyURI")]
	public string ValueType
	{
		get
		{
			return valueTypeField;
		}
		set
		{
			valueTypeField = value;
		}
	}

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

	[XmlText]
	public string Value
	{
		get
		{
			return valueField;
		}
		set
		{
			valueField = value;
		}
	}

	public BinaryExchangeType()
	{
		valueTypeField = "http://schemas.xmlsoap.org/ws/2005/02/trust/spnego";
		encodingTypeField = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary";
	}
}
