using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[GeneratedCode("svcutil", "4.0.30319.1")]
[XmlType(Namespace = "http://www.w3.org/2000/09/xmldsig#")]
[DebuggerStepThrough]
[DesignerCategory("code")]
public class RetrievalMethodType
{
	private TransformType[] transformsField;

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
