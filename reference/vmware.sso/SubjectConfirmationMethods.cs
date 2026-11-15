using System;
using System.CodeDom.Compiler;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[XmlType(Namespace = "http://www.rsa.com/names/2010/04/std-prof/SAML2.0")]
[GeneratedCode("svcutil", "4.0.30319.1")]
public enum SubjectConfirmationMethods
{
	[XmlEnum("urn:oasis:names:tc:SAML:2.0:cm:bearer")]
	urnoasisnamestcSAML20cmbearer,
	[XmlEnum("urn:oasis:names:tc:SAML:2.0:cm:holder-of-key")]
	urnoasisnamestcSAML20cmholderofkey
}
