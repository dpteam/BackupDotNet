using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[DesignerCategory("code")]
[XmlType(Namespace = "urn:oasis:names:tc:SAML:2.0:assertion")]
[DebuggerStepThrough]
[GeneratedCode("svcutil", "4.0.30319.1")]
public class AuthnStatementType : StatementAbstractType
{
	private AuthnContextType authnContextField;

	private DateTime authnInstantField;

	[XmlElement(Order = 0)]
	public AuthnContextType AuthnContext
	{
		get
		{
			return authnContextField;
		}
		set
		{
			authnContextField = value;
		}
	}

	[XmlAttribute]
	public DateTime AuthnInstant
	{
		get
		{
			return authnInstantField;
		}
		set
		{
			authnInstantField = value;
		}
	}
}
