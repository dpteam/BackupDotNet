using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[DebuggerStepThrough]
[XmlType(Namespace = "http://www.rsa.com/names/2009/12/std-ext/SAML2.0")]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DesignerCategory("code")]
public class RenewRestrictionType : ConditionAbstractType
{
	private string countField;

	private bool postdatableField;

	private bool renewExpiredField;

	[DefaultValue("0")]
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

	[XmlAttribute]
	[DefaultValue(false)]
	public bool Postdatable
	{
		get
		{
			return postdatableField;
		}
		set
		{
			postdatableField = value;
		}
	}

	[DefaultValue(false)]
	[XmlAttribute]
	public bool RenewExpired
	{
		get
		{
			return renewExpiredField;
		}
		set
		{
			renewExpiredField = value;
		}
	}

	public RenewRestrictionType()
	{
		countField = "0";
		postdatableField = false;
		renewExpiredField = false;
	}
}
