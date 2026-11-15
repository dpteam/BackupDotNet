using System;
using System.CodeDom.Compiler;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[GeneratedCode("svcutil", "4.0.30319.1")]
[XmlType(Namespace = "http://www.rsa.com/names/2010/04/std-prof/SAML2.0")]
public enum NameIDFormats
{
	[XmlEnum("http://schemas.xmlsoap.org/claims/UPN")]
	httpschemasxmlsoaporgclaimsUPN,
	[XmlEnum("urn:oasis:names:tc:SAML:1.1:nameid-format:unspecified")]
	urnoasisnamestcSAML11nameidformatunspecified,
	[XmlEnum("urn:oasis:names:tc:SAML:2.0:nameid-format:entity")]
	urnoasisnamestcSAML20nameidformatentity
}
