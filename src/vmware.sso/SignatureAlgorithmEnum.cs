using System;
using System.CodeDom.Compiler;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[XmlType(Namespace = "http://docs.oasis-open.org/ws-sx/ws-trust/200512")]
[GeneratedCode("svcutil", "4.0.30319.1")]
public enum SignatureAlgorithmEnum
{
	[XmlEnum("http://www.w3.org/2000/09/xmldsig#rsa-sha1")]
	httpwwww3org200009xmldsigrsasha1,
	[XmlEnum("http://www.w3.org/2001/04/xmldsig-more#rsa-sha256")]
	httpwwww3org200104xmldsigmorersasha256,
	[XmlEnum("http://www.w3.org/2001/04/xmldsig-more#rsa-sha384")]
	httpwwww3org200104xmldsigmorersasha384,
	[XmlEnum("http://www.w3.org/2001/04/xmldsig-more#rsa-sha512")]
	httpwwww3org200104xmldsigmorersasha512
}
