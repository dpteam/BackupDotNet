using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[DebuggerStepThrough]
[DesignerCategory("code")]
[XmlType(Namespace = "http://www.w3.org/2000/09/xmldsig#")]
[GeneratedCode("svcutil", "4.0.30319.1")]
public class ReferenceType
{
	private TransformType[] transformsField;

	private DigestMethodType digestMethodField;

	private byte[] digestValueField;

	private string idField;

	private string uRIField;

	private string typeField;

	[XmlArrayItem("Transform", IsNullable = false)]
	[XmlArray(Order = 0)]
	public TransformType[] Transforms
	{
		get
		{
			return transformsField;
		}
		set
		{
			transformsField = value;
		}
	}

	[XmlElement(Order = 1)]
	public DigestMethodType DigestMethod
	{
		get
		{
			return digestMethodField;
		}
		set
		{
			digestMethodField = value;
		}
	}

	[XmlElement(DataType = "base64Binary", Order = 2)]
	public byte[] DigestValue
	{
		get
		{
			return digestValueField;
		}
		set
		{
			digestValueField = value;
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

	[XmlAttribute(DataType = "anyURI")]
	public string URI
	{
		get
		{
			return uRIField;
		}
		set
		{
			uRIField = value;
		}
	}

	[XmlAttribute(DataType = "anyURI")]
	public string Type
	{
		get
		{
			return typeField;
		}
		set
		{
			typeField = value;
		}
	}
}
