using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

namespace VMware.Binding.WsTrust;

internal class Util
{
	public static string ToXmlDateTime(DateTime value)
	{
		return value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
	}

	public static XmlAttribute CreateXmlAttribute(XmlElement element, string preferedPrefix, string attributeName, string namespaceUri, string value)
	{
		XmlDocument ownerDocument = element.OwnerDocument;
		string text = element.GetPrefixOfNamespace(namespaceUri);
		if (string.IsNullOrEmpty(text))
		{
			XmlAttribute xmlAttribute = ownerDocument.CreateAttribute("xmlns", preferedPrefix, "http://www.w3.org/2000/xmlns/");
			xmlAttribute.Value = namespaceUri;
			element.Attributes.Append(xmlAttribute);
			text = preferedPrefix;
		}
		XmlAttribute xmlAttribute2 = ownerDocument.CreateAttribute(text, attributeName, namespaceUri);
		xmlAttribute2.Value = value;
		return element.Attributes.Append(xmlAttribute2);
	}

	public static XmlAttribute CreateXmlAttribute(string name, string ns, string value)
	{
		XmlAttribute xmlAttribute = new XmlDocument().CreateAttribute(name, ns);
		xmlAttribute.Value = value;
		return xmlAttribute;
	}

	public static XmlElement ComputeSignature(XmlDocument xmlRequestToSign, XmlElement keyIdentiifer, AsymmetricAlgorithm signingKey, params string[] elementsToSign)
	{
		WsuSignedXml wsuSignedXml = new WsuSignedXml(xmlRequestToSign);
		wsuSignedXml.SigningKey = signingKey;
		wsuSignedXml.SignedInfo.CanonicalizationMethod = "http://www.w3.org/2001/10/xml-exc-c14n#";
		foreach (string arg in elementsToSign)
		{
			Reference reference = new Reference($"#{arg}");
			reference.AddTransform(new XmlDsigExcC14NTransform(includeComments: false));
			wsuSignedXml.AddReference(reference);
		}
		wsuSignedXml.KeyInfo.AddClause(new KeyInfoNode(keyIdentiifer));
		wsuSignedXml.ComputeSignature();
		return wsuSignedXml.GetXml();
	}

	public static XmlDocument ReadDocument(Stream stream, bool ownStream)
	{
		using XmlDictionaryReader xmlReader = GetXmlReader(stream, ownStream);
		return ReadDocument(xmlReader);
	}

	private static XmlDictionaryReader GetXmlReader(Stream stream, bool ownStream, Encoding encoding = null)
	{
		encoding = encoding ?? Encoding.UTF8;
		OnXmlDictionaryReaderClose onClose = (ownStream ? ((OnXmlDictionaryReaderClose)((XmlDictionaryReader r) =>
		{
			stream.Close();
		})) : null);
		return XmlDictionaryReader.CreateTextReader(stream, encoding, new XmlDictionaryReaderQuotas(), onClose);
	}

	private static XmlDocument ReadDocument(XmlDictionaryReader xmlReader)
	{
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.PreserveWhitespace = true;
		xmlDocument.Load(xmlReader);
		return xmlDocument;
	}
}
