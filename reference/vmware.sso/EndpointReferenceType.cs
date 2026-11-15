using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[XmlType(Namespace = "http://www.w3.org/2005/08/addressing")]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DebuggerStepThrough]
[DesignerCategory("code")]
public class EndpointReferenceType
{
	private AttributedURIType addressField;

	private ReferenceParametersType referenceParametersField;

	private MetadataType metadataField;

	private XmlElement[] anyField;

	private XmlAttribute[] anyAttrField;

	[XmlElement(Order = 0)]
	public AttributedURIType Address
	{
		get
		{
			return addressField;
		}
		set
		{
			addressField = value;
		}
	}

	[XmlElement(Order = 1)]
	public ReferenceParametersType ReferenceParameters
	{
		get
		{
			return referenceParametersField;
		}
		set
		{
			referenceParametersField = value;
		}
	}

	[XmlElement(Order = 2)]
	public MetadataType Metadata
	{
		get
		{
			return metadataField;
		}
		set
		{
			metadataField = value;
		}
	}

	[XmlAnyElement(Order = 3)]
	public XmlElement[] Any
	{
		get
		{
			return anyField;
		}
		set
		{
			anyField = value;
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
