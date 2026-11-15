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
public class SubjectConfirmationType
{
	private NameIDType nameIDField;

	private SubjectConfirmationDataType subjectConfirmationDataField;

	private SubjectConfirmationMethods methodField;

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
	public SubjectConfirmationDataType SubjectConfirmationData
	{
		get
		{
			return subjectConfirmationDataField;
		}
		set
		{
			subjectConfirmationDataField = value;
		}
	}

	[XmlAttribute]
	public SubjectConfirmationMethods Method
	{
		get
		{
			return methodField;
		}
		set
		{
			methodField = value;
		}
	}
}
