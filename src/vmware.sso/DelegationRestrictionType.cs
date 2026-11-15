using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[DebuggerStepThrough]
[DesignerCategory("code")]
[XmlType(Namespace = "urn:oasis:names:tc:SAML:2.0:conditions:delegation")]
[GeneratedCode("svcutil", "4.0.30319.1")]
public class DelegationRestrictionType : ConditionAbstractType
{
	private DelegateType[] delegateField;

	[XmlElement("Delegate", Order = 0)]
	public DelegateType[] Delegate
	{
		get
		{
			return delegateField;
		}
		set
		{
			delegateField = value;
		}
	}
}
