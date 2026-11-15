using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[XmlType(Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DebuggerStepThrough]
[DesignerCategory("code")]
public class UsernameTokenType
{
	private AttributedString usernameField;

	private PasswordString passwordField;

	private string idField;

	[XmlElement(Order = 0)]
	public AttributedString Username
	{
		get
		{
			return usernameField;
		}
		set
		{
			usernameField = value;
		}
	}

	[XmlElement(Order = 1)]
	public PasswordString Password
	{
		get
		{
			return passwordField;
		}
		set
		{
			passwordField = value;
		}
	}

	[XmlAttribute(Form = XmlSchemaForm.Qualified, Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd", DataType = "ID")]
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
