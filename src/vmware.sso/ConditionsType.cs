using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[XmlType(Namespace = "urn:oasis:names:tc:SAML:2.0:assertion")]
[DesignerCategory("code")]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DebuggerStepThrough]
public class ConditionsType
{
	private ConditionAbstractType[] itemsField;

	private DateTime notBeforeField;

	private DateTime notOnOrAfterField;

	[XmlElement("Condition", typeof(ConditionAbstractType), Order = 0)]
	[XmlElement("AudienceRestriction", typeof(AudienceRestrictionType), Order = 0)]
	[XmlElement("ProxyRestriction", typeof(ProxyRestrictionType), Order = 0)]
	public ConditionAbstractType[] Items
	{
		get
		{
			return itemsField;
		}
		set
		{
			itemsField = value;
		}
	}

	[XmlAttribute]
	public DateTime NotBefore
	{
		get
		{
			return notBeforeField;
		}
		set
		{
			notBeforeField = value;
		}
	}

	[XmlAttribute]
	public DateTime NotOnOrAfter
	{
		get
		{
			return notOnOrAfterField;
		}
		set
		{
			notOnOrAfterField = value;
		}
	}
}
