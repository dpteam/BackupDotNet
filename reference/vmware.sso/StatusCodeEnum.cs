using System;
using System.CodeDom.Compiler;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[XmlType(Namespace = "http://docs.oasis-open.org/ws-sx/ws-trust/200512")]
[GeneratedCode("svcutil", "4.0.30319.1")]
public enum StatusCodeEnum
{
	[XmlEnum("http://docs.oasis-open.org/ws-sx/ws-trust/200512/status/valid")]
	httpdocsoasisopenorgwssxwstrust200512statusvalid,
	[XmlEnum("http://docs.oasis-open.org/ws-sx/ws-trust/200512/status/invalid")]
	httpdocsoasisopenorgwssxwstrust200512statusinvalid
}
