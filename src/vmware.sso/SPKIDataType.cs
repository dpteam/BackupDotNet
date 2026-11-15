using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[XmlType(Namespace = "http://www.w3.org/2000/09/xmldsig#")]
[DesignerCategory("code")]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DebuggerStepThrough]
public class SPKIDataType
{
	private object[] itemsField;

	[XmlAnyElement(Order = 0)]
	[XmlElement("SPKISexp", typeof(byte[]), DataType = "base64Binary", Order = 0)]
	public object[] Items
	{
		get
		{
			return itemsField;
		}
		set
		{
			itemsField = value;
		}
	}
}
