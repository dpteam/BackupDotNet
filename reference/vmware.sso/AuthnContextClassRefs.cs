using System;
using System.CodeDom.Compiler;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[GeneratedCode("svcutil", "4.0.30319.1")]
[XmlType(Namespace = "http://www.rsa.com/names/2010/04/std-prof/SAML2.0")]
public enum AuthnContextClassRefs
{
	[XmlEnum("urn:oasis:names:tc:SAML:2.0:ac:classes:Kerberos")]
	urnoasisnamestcSAML20acclassesKerberos,
	[XmlEnum("urn:oasis:names:tc:SAML:2.0:ac:classes:PasswordProtectedTransport")]
	urnoasisnamestcSAML20acclassesPasswordProtectedTransport,
	[XmlEnum("urn:oasis:names:tc:SAML:2.0:ac:classes:XMLDSig")]
	urnoasisnamestcSAML20acclassesXMLDSig,
	[XmlEnum("urn:federation:authentication:windows")]
	urnfederationauthenticationwindows
}
