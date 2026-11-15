using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[XmlType(Namespace = "urn:oasis:names:tc:SAML:2.0:conditions:delegation")]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DebuggerStepThrough]
[DesignerCategory("code")]
public class DelegateType
{
	private NameIDType nameIDField;

	private DateTime delegationInstantField;

	private bool delegationInstantFieldSpecified;

	[XmlElement(Namespace = "urn:oasis:names:tc:SAML:2.0:assertion", Order = 0)]
	public NameIDType NameID
	{
		get
		{
			return nameIDField;
		}
		set
		{
			nameIDField = value;
		}
	}

	[XmlAttribute]
	public DateTime DelegationInstant
	{
		get
		{
			return delegationInstantField;
		}
		set
		{
			delegationInstantField = value;
		}
	}

	[XmlIgnore]
	public bool DelegationInstantSpecified
	{
		get
		{
			return delegationInstantFieldSpecified;
		}
		set
		{
			delegationInstantFieldSpecified = value;
		}
	}
}
