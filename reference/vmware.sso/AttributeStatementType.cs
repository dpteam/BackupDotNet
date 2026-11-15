using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[XmlType(Namespace = "urn:oasis:names:tc:SAML:2.0:assertion")]
[DesignerCategory("code")]
[DebuggerStepThrough]
[GeneratedCode("svcutil", "4.0.30319.1")]
public class AttributeStatementType : StatementAbstractType
{
	private AttributeType1[] itemsField;

	[XmlElement("Attribute", Order = 0)]
	public AttributeType1[] Items
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
}
