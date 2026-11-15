using System;
using System.CodeDom.Compiler;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[XmlType(Namespace = "http://docs.oasis-open.org/ws-sx/ws-trust/200512")]
[GeneratedCode("svcutil", "4.0.30319.1")]
public enum TokenTypeEnum
{
	[XmlEnum("urn:oasis:names:tc:SAML:2.0:assertion")]
	urnoasisnamestcSAML20assertion,
	[XmlEnum("http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLV2.0")]
	httpdocsoasisopenorgwssoasiswsssamltokenprofile11SAMLV20,
	[XmlEnum("http://docs.oasis-open.org/ws-sx/ws-trust/200512/RSTR/Status")]
	httpdocsoasisopenorgwssxwstrust200512RSTRStatus
}
