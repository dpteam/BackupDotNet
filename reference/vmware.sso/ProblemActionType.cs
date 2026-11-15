using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[DebuggerStepThrough]
[DesignerCategory("code")]
[XmlType(Namespace = "http://www.w3.org/2005/08/addressing")]
[GeneratedCode("svcutil", "4.0.30319.1")]
public class ProblemActionType
{
	private AttributedURIType actionField;

	private string soapActionField;

	private XmlAttribute[] anyAttrField;

	[XmlElement(Order = 0)]
	public AttributedURIType Action
	{
		get
		{
			return actionField;
		}
		set
		{
			actionField = value;
		}
	}

	[XmlElement(DataType = "anyURI", Order = 1)]
	public string SoapAction
	{
		get
		{
			return soapActionField;
		}
		set
		{
			soapActionField = value;
		}
	}

	[XmlAnyAttribute]
	public XmlAttribute[] AnyAttr
	{
		get
		{
			return anyAttrField;
		}
		set
		{
			anyAttrField = value;
		}
	}
}
