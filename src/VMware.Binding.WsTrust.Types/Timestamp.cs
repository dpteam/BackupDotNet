using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace VMware.Binding.WsTrust.Types;

[Serializable]
[XmlType(Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd", TypeName = "Timestamp")]
public class Timestamp
{
	[XmlElement(Order = 0)]
	public AttributedDateTime Created { get; set; }

	[XmlElement(Order = 1)]
	public AttributedDateTime Expires { get; set; }

	[XmlAnyElement(Order = 2)]
	public XmlElement[] Items { get; set; }

	[XmlAttribute(Form = XmlSchemaForm.Qualified, DataType = "ID")]
	public string Id { get; set; }

	[XmlAnyAttribute]
	public XmlAttribute[] AnyAttr { get; set; }
}
