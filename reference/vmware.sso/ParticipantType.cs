using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DesignerCategory("code")]
[XmlType(Namespace = "http://docs.oasis-open.org/ws-sx/ws-trust/200512")]
[DebuggerStepThrough]
public class ParticipantType
{
	private EndpointReferenceType endpointReferenceField;

	[XmlElement(Namespace = "http://www.w3.org/2005/08/addressing", Order = 0)]
	public EndpointReferenceType EndpointReference
	{
		get
		{
			return endpointReferenceField;
		}
		set
		{
			endpointReferenceField = value;
		}
	}
}
