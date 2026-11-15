using System.Security.Cryptography.Xml;
using System.Xml;

namespace VMware.Binding.WsTrust;

internal class WsuSignedXml : SignedXml
{
	private const string WsuNamespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

	public WsuSignedXml(XmlDocument xml)
		: base(xml)
	{
	}

	public WsuSignedXml(XmlElement xmlElement)
		: base(xmlElement)
	{
	}

	public override XmlElement GetIdElement(XmlDocument document, string idValue)
	{
		XmlElement xmlElement = base.GetIdElement(document, idValue);
		if (xmlElement == null)
		{
			XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(document.NameTable);
			xmlNamespaceManager.AddNamespace("wsu", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");
			string xpath = $"//*[@wsu:Id=\"{idValue}\"]";
			xmlElement = document.SelectSingleNode(xpath, xmlNamespaceManager) as XmlElement;
		}
		return xmlElement;
	}
}
