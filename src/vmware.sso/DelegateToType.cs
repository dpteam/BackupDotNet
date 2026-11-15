using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[DesignerCategory("code")]
[GeneratedCode("svcutil", "4.0.30319.1")]
[XmlType(Namespace = "http://docs.oasis-open.org/ws-sx/ws-trust/200512")]
[DebuggerStepThrough]
public class DelegateToType
{
	private UsernameTokenType usernameTokenField;

	[XmlElement(Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd", Order = 0)]
	public UsernameTokenType UsernameToken
	{
		get
		{
			return usernameTokenField;
		}
		set
		{
			usernameTokenField = value;
		}
	}
}
