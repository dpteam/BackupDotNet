using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[DesignerCategory("code")]
[DebuggerStepThrough]
[XmlType(Namespace = "http://www.w3.org/2000/09/xmldsig#")]
[GeneratedCode("svcutil", "4.0.30319.1")]
public class ObjectType
{
	private XmlNode[] anyField;

	private string idField;

	private string mimeTypeField;

	private string encodingField;

	[XmlText]
	[XmlAnyElement(Order = 0)]
	public XmlNode[] Any
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

	[XmlAttribute]
	public string MimeType
	{
		get
		{
			return mimeTypeField;
		}
		set
		{
			mimeTypeField = value;
		}
	}

	[XmlAttribute(DataType = "anyURI")]
	public string Encoding
	{
		get
		{
			return encodingField;
		}
		set
		{
			encodingField = value;
		}
	}
}
