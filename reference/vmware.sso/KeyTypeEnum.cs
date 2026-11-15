using System;
using System.CodeDom.Compiler;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[GeneratedCode("svcutil", "4.0.30319.1")]
[XmlType(Namespace = "http://docs.oasis-open.org/ws-sx/ws-trust/200512")]
public enum KeyTypeEnum
{
	[XmlEnum("http://docs.oasis-open.org/ws-sx/ws-trust/200512/PublicKey")]
	httpdocsoasisopenorgwssxwstrust200512PublicKey,
	[XmlEnum("http://docs.oasis-open.org/ws-sx/ws-trust/200512/Bearer")]
	httpdocsoasisopenorgwssxwstrust200512Bearer
}
