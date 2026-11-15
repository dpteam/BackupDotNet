using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[XmlType(Namespace = "urn:oasis:names:tc:SAML:2.0:assertion")]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DebuggerStepThrough]
[DesignerCategory("code")]
public class AuthnContextType
{
	private AuthnContextClassRefs authnContextClassRefField;

	[XmlElement(Order = 0)]
	public AuthnContextClassRefs AuthnContextClassRef
	{
		get
		{
			return authnContextClassRefField;
		}
		set
		{
			authnContextClassRefField = value;
		}
	}
}
