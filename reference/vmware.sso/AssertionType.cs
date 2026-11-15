using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DebuggerStepThrough]
[DesignerCategory("code")]
[XmlType(Namespace = "urn:oasis:names:tc:SAML:2.0:assertion")]
public class AssertionType
{
	private NameIDType issuerField;

	private SignatureType signatureField;

	private SubjectType subjectField;

	private ConditionsType conditionsField;

	private AdviceType1 adviceField;

	private AuthnStatementType authnStatementField;

	private AttributeStatementType attributeStatementField;

	private string versionField;

	private string idField;

	private DateTime issueInstantField;

	[XmlElement(Order = 0)]
	public NameIDType Issuer
	{
		get
		{
			return issuerField;
		}
		set
		{
			issuerField = value;
		}
	}

	[XmlElement(Namespace = "http://www.w3.org/2000/09/xmldsig#", Order = 1)]
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

	[XmlElement(Order = 2)]
	public SubjectType Subject
	{
		get
		{
			return subjectField;
		}
		set
		{
			subjectField = value;
		}
	}

	[XmlElement(Order = 3)]
	public ConditionsType Conditions
	{
		get
		{
			return conditionsField;
		}
		set
		{
			conditionsField = value;
		}
	}

	[XmlElement(Order = 4)]
	public AdviceType1 Advice
	{
		get
		{
			return adviceField;
		}
		set
		{
			adviceField = value;
		}
	}

	[XmlElement(Order = 5)]
	public AuthnStatementType AuthnStatement
	{
		get
		{
			return authnStatementField;
		}
		set
		{
			authnStatementField = value;
		}
	}

	[XmlElement(Order = 6)]
	public AttributeStatementType AttributeStatement
	{
		get
		{
			return attributeStatementField;
		}
		set
		{
			attributeStatementField = value;
		}
	}

	[XmlAttribute]
	public string Version
	{
		get
		{
			return versionField;
		}
		set
		{
			versionField = value;
		}
	}

	[XmlAttribute(DataType = "ID")]
	public string ID
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

	[XmlAttribute]
	public DateTime IssueInstant
	{
		get
		{
			return issueInstantField;
		}
		set
		{
			issueInstantField = value;
		}
	}

	public AssertionType()
	{
		versionField = "2.0";
	}
}
