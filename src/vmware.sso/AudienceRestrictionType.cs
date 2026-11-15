using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[XmlType(Namespace = "urn:oasis:names:tc:SAML:2.0:assertion")]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DebuggerStepThrough]
[DesignerCategory("code")]
public class AudienceRestrictionType : ConditionAbstractType
{
	private string[] audienceField;

	[XmlElement("Audience", DataType = "anyURI", Order = 0)]
	public string[] Audience
	{
		get
		{
			return audienceField;
		}
		set
		{
			audienceField = value;
		}
	}
}
