using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[GeneratedCode("svcutil", "4.0.30319.1")]
[XmlType(Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
[DesignerCategory("code")]
[DebuggerStepThrough]
public class KeyIdentifierType : EncodedString
{
	private string valueTypeField;

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
}
