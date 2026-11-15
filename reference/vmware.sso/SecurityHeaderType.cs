using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[DesignerCategory("code")]
[GeneratedCode("svcutil", "4.0.30319.1")]
[XmlType(Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
[DebuggerStepThrough]
public class SecurityHeaderType
{
	private TimestampType timestampField;

	private UsernameTokenType usernameTokenField;

	private BinarySecurityTokenType binarySecurityTokenField;

	private AssertionType assertionField;

	private SignatureType signatureField;

	private XmlAttribute[] anyAttrField;

	[XmlElement(Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd")]
	public TimestampType Timestamp
	{
		get
		{
			return timestampField;
		}
		set
		{
			timestampField = value;
		}
	}

	public UsernameTokenType UsernameToken
	{
		get
		{
			return usernameTokenField;
		}
		set
		{
			usernameTokenField = value;
		}
	}

	public BinarySecurityTokenType BinarySecurityToken
	{
		get
		{
			return binarySecurityTokenField;
		}
		set
		{
			binarySecurityTokenField = value;
		}
	}

	[XmlElement(Namespace = "urn:oasis:names:tc:SAML:2.0:assertion")]
	public AssertionType Assertion
	{
		get
		{
			return assertionField;
		}
		set
		{
			assertionField = value;
		}
	}

	[XmlElement(Namespace = "http://www.w3.org/2000/09/xmldsig#")]
	public SignatureType Signature
	{
		get
		{
			return signatureField;
		}
		set
		{
			signatureField = value;
		}
	}

	[XmlAnyAttribute]
	public XmlAttribute[] AnyAttr
	{
		get
		{
			return anyAttrField;
		}
		set
		{
			anyAttrField = value;
		}
	}
}
