using System;
using System.CodeDom.Compiler;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[XmlType(Namespace = "http://docs.oasis-open.org/ws-sx/ws-trust/200512")]
[GeneratedCode("svcutil", "4.0.30319.1")]
public enum RequestTypeEnum
{
	[XmlEnum("http://docs.oasis-open.org/ws-sx/ws-trust/200512/Issue")]
	httpdocsoasisopenorgwssxwstrust200512Issue,
	[XmlEnum("http://docs.oasis-open.org/ws-sx/ws-trust/200512/Renew")]
	httpdocsoasisopenorgwssxwstrust200512Renew,
	[XmlEnum("http://docs.oasis-open.org/ws-sx/ws-trust/200512/Validate")]
	httpdocsoasisopenorgwssxwstrust200512Validate
}
