using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DesignerCategory("code")]
[DebuggerStepThrough]
[XmlType(Namespace = "http://docs.oasis-open.org/ws-sx/ws-trust/200512")]
public class ParticipantsType
{
	private ParticipantType primaryField;

	private ParticipantType[] participantField;

	[XmlElement(Order = 0)]
	public ParticipantType Primary
	{
		get
		{
			return primaryField;
		}
		set
		{
			primaryField = value;
		}
	}

	[XmlElement("Participant", Order = 1)]
	public ParticipantType[] Participant
	{
		get
		{
			return participantField;
		}
		set
		{
			participantField = value;
		}
	}
}
