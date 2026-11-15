using System;
using System.CodeDom.Compiler;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[GeneratedCode("svcutil", "4.0.30319.1")]
[XmlType(Namespace = "http://www.w3.org/2000/09/xmldsig#", IncludeInSchema = false)]
public enum ItemsChoiceType
{
	[XmlEnum("##any:")]
	Item,
	X509CRL,
	X509Certificate,
	X509IssuerSerial,
	X509SKI,
	X509SubjectName
}
