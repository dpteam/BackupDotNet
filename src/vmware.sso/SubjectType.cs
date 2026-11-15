using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[DebuggerStepThrough]
[DesignerCategory("code")]
[XmlType(Namespace = "urn:oasis:names:tc:SAML:2.0:assertion")]
[GeneratedCode("svcutil", "4.0.30319.1")]
public class SubjectType
{
	private NameIDType nameIDField;

	private SubjectConfirmationType subjectConfirmationField;

	[XmlElement(Order = 0)]
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

	[XmlElement(Order = 1)]
	public SubjectConfirmationType SubjectConfirmation
	{
		get
		{
			return subjectConfirmationField;
		}
		set
		{
			subjectConfirmationField = value;
		}
	}
}
