using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[XmlType(Namespace = "http://docs.oasis-open.org/ws-sx/ws-trust/200512")]
[DebuggerStepThrough]
[DesignerCategory("code")]
[GeneratedCode("svcutil", "4.0.30319.1")]
public class LifetimeType
{
	private AttributedDateTime createdField;

	private AttributedDateTime expiresField;

	[XmlElement(Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd", Order = 0)]
	public AttributedDateTime Created
	{
		get
		{
			return createdField;
		}
		set
		{
			createdField = value;
		}
	}

	[XmlElement(Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd", Order = 1)]
	public AttributedDateTime Expires
	{
		get
		{
			return expiresField;
		}
		set
		{
			expiresField = value;
		}
	}
}
