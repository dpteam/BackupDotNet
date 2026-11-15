using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[XmlType(Namespace = "urn:oasis:names:tc:SAML:2.0:assertion")]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DesignerCategory("code")]
[XmlInclude(typeof(KeyInfoConfirmationDataType))]
[DebuggerStepThrough]
public class SubjectConfirmationDataType
{
	private string[] textField;

	[XmlText]
	public string[] Text
	{
		get
		{
			return textField;
		}
		set
		{
			textField = value;
		}
	}
}
