using System;
using System.CodeDom.Compiler;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[XmlType(Namespace = "http://www.w3.org/2000/09/xmldsig#", IncludeInSchema = false)]
[GeneratedCode("svcutil", "4.0.30319.1")]
public enum ItemsChoiceType2
{
	[XmlEnum("##any:")]
	Item,
	KeyName,
	KeyValue,
	MgmtData,
	PGPData,
	RetrievalMethod,
	SPKIData,
	X509Data
}
