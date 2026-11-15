using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DebuggerStepThrough]
[DesignerCategory("code")]
[XmlType(Namespace = "urn:oasis:names:tc:SAML:2.0:assertion")]
public class ProxyRestrictionType : ConditionAbstractType
{
	private string countField;

	[XmlAttribute(DataType = "nonNegativeInteger")]
	public string Count
	{
		get
		{
			return countField;
		}
		set
		{
			countField = value;
		}
	}
}
