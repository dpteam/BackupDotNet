using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DesignerCategory("code")]
[XmlType(Namespace = "http://www.w3.org/2000/09/xmldsig#")]
[DebuggerStepThrough]
public class ManifestType
{
	private ReferenceType[] referenceField;

	private string idField;

	[XmlElement("Reference", Order = 0)]
	public ReferenceType[] Reference
	{
		get
		{
			return referenceField;
		}
		set
		{
			referenceField = value;
		}
	}

	[XmlAttribute(DataType = "ID")]
	public string Id
	{
		get
		{
			return idField;
		}
		set
		{
			idField = value;
		}
	}
}
