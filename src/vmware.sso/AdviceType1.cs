using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DesignerCategory("code")]
[XmlType(TypeName = "AdviceType", Namespace = "urn:oasis:names:tc:SAML:2.0:assertion")]
[DebuggerStepThrough]
public class AdviceType1
{
	private RSAAdviceType[] rSAAdviceField;

	[XmlElement("RSAAdvice", Namespace = "http://www.rsa.com/names/2009/12/std-ext/SAML2.0", Order = 0)]
	public RSAAdviceType[] RSAAdvice
	{
		get
		{
			return rSAAdviceField;
		}
		set
		{
			rSAAdviceField = value;
		}
	}
}
