using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[XmlType(Namespace = "http://www.w3.org/2000/09/xmldsig#")]
[DesignerCategory("code")]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DebuggerStepThrough]
public class SignatureType
{
	private SignedInfoType signedInfoField;

	private SignatureValueType signatureValueField;

	private KeyInfoType keyInfoField;

	private string idField;

	[XmlElement(Order = 0)]
	public SignedInfoType SignedInfo
	{
		get
		{
			return signedInfoField;
		}
		set
		{
			signedInfoField = value;
		}
	}

	[XmlElement(Order = 1)]
	public SignatureValueType SignatureValue
	{
		get
		{
			return signatureValueField;
		}
		set
		{
			signatureValueField = value;
		}
	}

	[XmlElement(Order = 2)]
	public KeyInfoType KeyInfo
	{
		get
		{
			return keyInfoField;
		}
		set
		{
			keyInfoField = value;
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
