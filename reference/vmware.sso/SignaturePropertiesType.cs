using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[DesignerCategory("code")]
[XmlType(Namespace = "http://www.w3.org/2000/09/xmldsig#")]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DebuggerStepThrough]
public class SignaturePropertiesType
{
	private SignaturePropertyType[] signaturePropertyField;

	private string idField;

	[XmlElement("SignatureProperty", Order = 0)]
	public SignaturePropertyType[] SignatureProperty
	{
		get
		{
			return signaturePropertyField;
		}
		set
		{
			signaturePropertyField = value;
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
