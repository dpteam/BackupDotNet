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
public class StatusType
{
	private StatusCodeEnum codeField;

	private string reasonField;

	[XmlElement(Order = 0)]
	public StatusCodeEnum Code
	{
		get
		{
			return codeField;
		}
		set
		{
			codeField = value;
		}
	}

	[XmlElement(Order = 1)]
	public string Reason
	{
		get
		{
			return reasonField;
		}
		set
		{
			reasonField = value;
		}
	}
}
