using System;
using System.Diagnostics;
using System.IdentityModel.Tokens;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Security;
using System.Xml;
using System.Xml.Serialization;
using VMware.Binding.WsTrust.Types;

namespace VMware.Binding.WsTrust;

public class WsTrustClientMessageInspector : IClientMessageInspector
{
	private readonly XmlElement _samlToken;

	private const string SOAP_ENVELOPE_NS = "http://schemas.xmlsoap.org/soap/envelope/";

	private const string WS_SECURITY_NS = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

	private const string WS_UTILITY_NS = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

	private const string SOAP_HEADER_ELEMENT_NAME = "Header";

	private const string SECURITY_TOKEN_ID_PREFIX = "SecurityToken";

	private const string TIMESTAMP_ID_PREFIX = "Timestamp";

	private const string BODY_ID_PREFIX = "Id";

	private const int MAX_SIZE_OF_HEADERS = 1048576;

	private const string SOAP_ENVELOPE_ELEMENT_NAME = "Envelope";

	private static readonly TimeSpan RequestTimestampValidPeriod = new TimeSpan(0, 0, 5, 0);

	private static readonly XmlSerializer SecurityHeaderSerializer = new XmlSerializer(typeof(SecurityHeader), "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");

	private readonly WSSecurityTokenSerializer _tokenSerializer = new WSSecurityTokenSerializer(SecurityVersion.WSSecurity10, emitBspRequiredAttributes: true);

	private readonly X509SecurityToken _certificateToken;

	private readonly UserNameSecurityToken _userNameToken;

	public WsTrustClientMessageInspector(ClientCredentials clientCredentials)
	{
		if (clientCredentials.ClientCertificate.Certificate != null)
		{
			string id = CreateElementId("SecurityToken");
			_certificateToken = new X509SecurityToken(clientCredentials.ClientCertificate.Certificate, id);
		}
		if (clientCredentials.UserName.UserName != null)
		{
			string id2 = CreateElementId("SecurityToken");
			_userNameToken = new UserNameSecurityToken(clientCredentials.UserName.UserName, clientCredentials.UserName.Password, id2);
		}
	}

	public WsTrustClientMessageInspector(ClientCredentials clientCredentials, XmlElement samlToken)
		: this(clientCredentials)
	{
		_samlToken = samlToken;
	}

	private static string CreateElementId(string prefix)
	{
		return $"{prefix}-{Guid.NewGuid():D}";
	}

	public void AfterReceiveReply(ref Message reply, object correlationState)
	{
	}

	public object BeforeSendRequest(ref Message request, IClientChannel channel)
	{
		string empty = string.Empty;
		empty = ((!File.Exists("Vim25Service.dll")) ? FileVersionInfo.GetVersionInfo("STSService.dll").ProductVersion : FileVersionInfo.GetVersionInfo("Vim25Service.dll").ProductVersion);
		string value = string.Format(".Net Management SDK {0} C#/.NET Framework {1} ({2}; {3})", empty, Environment.Version.ToString(), Environment.OSVersion, Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"));
		HttpRequestMessageProperty httpRequestMessageProperty = new HttpRequestMessageProperty();
		httpRequestMessageProperty.Headers["User-Agent"] = value;
		request.Properties.Add(HttpRequestMessageProperty.Name, httpRequestMessageProperty);
		XmlDocument xmlMessage = GetXmlMessage(request);
		string text = CreateElementId("Id");
		SetIdToBodyElement(xmlMessage.DocumentElement, text);
		SecurityHeader unsignedHeader = GetUnsignedHeader();
		XmlDocument xmlDocument = ToXmlDocument(unsignedHeader);
		XmlElement xmlElement = MergeMessageWithHeader(xmlMessage.DocumentElement, xmlDocument.DocumentElement);
		if (_certificateToken != null)
		{
			XmlElement keyIdentiifer = ((_samlToken != null) ? GetKeyIdentifierClause(_samlToken) : GetKeyIdentifierClause(_certificateToken));
			XmlElement newChild = Util.ComputeSignature(xmlMessage, keyIdentiifer, _certificateToken.Certificate.PrivateKey, text, unsignedHeader.Timestamp.Id);
			xmlElement.AppendChild(newChild);
		}
		request = ToMessage(xmlMessage, request.Version, request.Headers.Action, request.Properties);
		return null;
	}

	private XmlElement ToXmlElement(SecurityToken token)
	{
		using XmlDocumentWriterHelper xmlDocumentWriterHelper = new XmlDocumentWriterHelper();
		_tokenSerializer.WriteToken(xmlDocumentWriterHelper.CreateDocumentWriter(), token);
		return xmlDocumentWriterHelper.ReadDocument().DocumentElement;
	}

	private Message ToMessage(XmlDocument xmlRequest, MessageVersion version, string action, MessageProperties messageProperties)
	{
		Message message = Message.CreateMessage(new XmlNodeReader(xmlRequest.DocumentElement), 1048576, version);
		message.Headers.Action = action;
		message.Properties.CopyProperties(messageProperties);
		return message;
	}

	private static XmlDocument GetXmlMessage(Message request)
	{
		using XmlDocumentWriterHelper xmlDocumentWriterHelper = new XmlDocumentWriterHelper();
		request.WriteMessage(xmlDocumentWriterHelper.CreateDocumentWriter());
		return xmlDocumentWriterHelper.ReadDocument();
	}

	private static XmlElement MergeMessageWithHeader(XmlElement xmlRequest, XmlElement ssoHeaderXml)
	{
		XmlElement orCreateHeadersNode = GetOrCreateHeadersNode(xmlRequest);
		XmlNode newChild = xmlRequest.OwnerDocument.ImportNode(ssoHeaderXml, deep: true);
		return (XmlElement)orCreateHeadersNode.AppendChild(newChild);
	}

	private static XmlElement GetOrCreateHeadersNode(XmlElement envelopeElement)
	{
		XmlNodeList elementsByTagName = envelopeElement.GetElementsByTagName("Header", "http://schemas.xmlsoap.org/soap/envelope/");
		XmlElement xmlElement;
		if (elementsByTagName.Count == 0)
		{
			XmlDocument ownerDocument = envelopeElement.OwnerDocument;
			string prefixOfNamespace = envelopeElement.GetPrefixOfNamespace("http://schemas.xmlsoap.org/soap/envelope/");
			xmlElement = ownerDocument.CreateElement(prefixOfNamespace, "Header", "http://schemas.xmlsoap.org/soap/envelope/");
			envelopeElement.AppendChild(xmlElement);
		}
		else
		{
			xmlElement = (XmlElement)elementsByTagName[0];
		}
		return xmlElement;
	}

	private static XmlDocument ToXmlDocument(SecurityHeader value)
	{
		using XmlDocumentWriterHelper xmlDocumentWriterHelper = new XmlDocumentWriterHelper();
		SecurityHeaderSerializer.Serialize(xmlDocumentWriterHelper.CreateDocumentWriter(), value);
		return xmlDocumentWriterHelper.ReadDocument();
	}

	private SecurityHeader GetUnsignedHeader()
	{
		XmlAttribute xmlAttribute = Util.CreateXmlAttribute("mustUnderstand", "http://schemas.xmlsoap.org/soap/envelope/", "1");
		DateTime now = DateTime.Now;
		DateTime value = now + RequestTimestampValidPeriod;
		string id = CreateElementId("Timestamp");
		SecurityHeader securityHeader = new SecurityHeader();
		securityHeader.Timestamp = new Timestamp
		{
			Id = id,
			Created = new AttributedDateTime
			{
				Value = now
			},
			Expires = new AttributedDateTime
			{
				Value = value
			}
		};
		securityHeader.AnyAttr = new XmlAttribute[1] { xmlAttribute };
		SecurityHeader securityHeader2 = securityHeader;
		if (_userNameToken != null)
		{
			securityHeader2.UsernameToken = ToXmlElement(_userNameToken);
		}
		if (_samlToken != null)
		{
			securityHeader2.SamlToken = _samlToken;
		}
		else if (_certificateToken != null)
		{
			XmlElement binarySecurityToken = ToXmlElement(_certificateToken);
			securityHeader2.BinarySecurityToken = binarySecurityToken;
		}
		return securityHeader2;
	}

	private static void SetIdToBodyElement(XmlElement envelopeElement, string bodyId)
	{
		SetIdToElement((XmlElement)envelopeElement.GetElementsByTagName("Body", "http://schemas.xmlsoap.org/soap/envelope/")[0], bodyId, "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");
	}

	private static void SetIdToElement(XmlElement element, string value, string namespaceUri)
	{
		XmlAttribute xmlAttribute = element.GetAttributeNode("Id", namespaceUri);
		if (xmlAttribute == null)
		{
			xmlAttribute = Util.CreateXmlAttribute(element, "wsu", "Id", namespaceUri, value);
		}
		xmlAttribute.Value = value;
	}

	private XmlElement GetKeyIdentifierClause(X509SecurityToken certToken)
	{
		using XmlDocumentWriterHelper xmlDocumentWriterHelper = new XmlDocumentWriterHelper();
		LocalIdKeyIdentifierClause keyIdentifierClause = certToken.CreateKeyIdentifierClause<LocalIdKeyIdentifierClause>();
		_tokenSerializer.WriteKeyIdentifierClause(xmlDocumentWriterHelper.CreateDocumentWriter(), keyIdentifierClause);
		return xmlDocumentWriterHelper.ReadDocument().DocumentElement;
	}

	private XmlElement GetKeyIdentifierClause(XmlElement samlToken)
	{
		if (samlToken == null)
		{
			throw new ArgumentNullException("samlToken");
		}
		using XmlDocumentWriterHelper xmlDocumentWriterHelper = new XmlDocumentWriterHelper();
		SecurityKeyIdentifierClause keyIdentifierClause = new Saml2AssertionKeyIdentifierClause((samlToken.Attributes.GetNamedItem("ID") ?? throw new InvalidDataException("The SAML token does not have an ID attribute.")).Value);
		new Saml2SecurityTokenHandler().KeyInfoSerializer.WriteKeyIdentifierClause(xmlDocumentWriterHelper.CreateDocumentWriter(), keyIdentifierClause);
		return xmlDocumentWriterHelper.ReadDocument().DocumentElement;
	}
}
