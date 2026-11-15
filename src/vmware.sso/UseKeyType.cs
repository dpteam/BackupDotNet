using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[XmlType(Namespace = "http://docs.oasis-open.org/ws-sx/ws-trust/200512")]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DebuggerStepThrough]
[DesignerCategory("code")]
public class UseKeyType
{
	private string sigField;

	[XmlAttribute(DataType = "anyURI")]
	public string Sig
	{
		get
		{
			return sigField;
		}
		set
		{
			sigField = value;
		}
	}
}
