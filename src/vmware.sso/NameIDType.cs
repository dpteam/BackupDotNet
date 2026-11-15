using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DesignerCategory("code")]
[XmlType(Namespace = "urn:oasis:names:tc:SAML:2.0:assertion")]
[DebuggerStepThrough]
public class NameIDType
{
	private string nameQualifierField;

	private NameIDFormats formatField;

	private string valueField;

	[XmlAttribute]
	public string NameQualifier
	{
		get
		{
			return nameQualifierField;
		}
		set
		{
			nameQualifierField = value;
		}
	}

	[XmlAttribute]
	public NameIDFormats Format
	{
		get
		{
			return formatField;
		}
		set
		{
			formatField = value;
		}
	}

	[XmlText]
	public string Value
	{
		get
		{
			return valueField;
		}
		set
		{
			valueField = value;
		}
	}
}
