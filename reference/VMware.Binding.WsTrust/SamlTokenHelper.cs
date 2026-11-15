using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text;
using System.Xml;
using vmware.sso;

namespace VMware.Binding.WsTrust;

public class SamlTokenHelper
{
	public static string strDateFormat = "{0:yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'}";

	public static string strAssertionId = "ID";

	public static string strIssueInstant = "IssueInstant";

	public static string strSubjectConfirmationNode = "saml2:SubjectConfirmation";

	public static string strSubjectConfirmationMethodValueAttribute = "Method";

	public static string strSubjectConfirmationMethodValueTypeBearer = "urn:oasis:names:tc:SAML:2.0:cm:bearer";

	public static string strSubjectConfirmationMethodValueTypeHoK = "urn:oasis:names:tc:SAML:2.0:cm:holder-of-key";

	private static long maxReceivedMessageSize = 2147483647L;

	private static MessageEncodingBindingElement GetWcfEncoding()
	{
		return new TextMessageEncodingBindingElement(MessageVersion.Soap11, Encoding.UTF8);
	}

	private static HttpsTransportBindingElement GetWcfTransport()
	{
		return new HttpsTransportBindingElement
		{
			RequireClientCertificate = false,
			AllowCookies = true,
			MaxReceivedMessageSize = maxReceivedMessageSize
		};
	}

	public static CustomBinding GetWcfBinding()
	{
		return new CustomBinding(GetWcfEncoding(), GetWcfTransport());
	}

	public static void SetCredentials(string username, string password, X509Certificate2 signingCertificate, ClientCredentials clientCredentials)
	{
		if (clientCredentials == null)
		{
			throw new ArgumentNullException("clientCredentials");
		}
		clientCredentials.UserName.UserName = username;
		clientCredentials.UserName.Password = password;
		clientCredentials.ClientCertificate.Certificate = signingCertificate;
		clientCredentials.SupportInteractive = false;
	}

	public static void SetupServerCertificateValidation()
	{
		string text = ConfigurationManager.AppSettings["ValidateServerCertificate"];
		if (!string.IsNullOrWhiteSpace(text) && text.Equals(bool.TrueString, StringComparison.CurrentCultureIgnoreCase))
		{
			ServicePointManager.ServerCertificateValidationCallback = (RemoteCertificateValidationCallback)Delegate.Combine(ServicePointManager.ServerCertificateValidationCallback, new RemoteCertificateValidationCallback(Validate));
			return;
		}
		ServicePointManager.ServerCertificateValidationCallback = (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;
	}

	public static bool Validate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
	{
		bool flag = true;
		if (sslPolicyErrors == SslPolicyErrors.None)
		{
			return flag;
		}
		if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
		{
			Console.WriteLine("SSL policy error {0}. Make sure that your application is using the correct server host name.", SslPolicyErrors.RemoteCertificateNameMismatch);
			flag = !flag && false;
		}
		if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0)
		{
			List<string> list = new List<string>();
			if (chain != null && chain.ChainStatus != null)
			{
				X509ChainStatus[] chainStatus = chain.ChainStatus;
				foreach (X509ChainStatus x509ChainStatus in chainStatus)
				{
					if (!(certificate.Subject == certificate.Issuer))
					{
						list.Add(x509ChainStatus.Status.ToString());
					}
				}
			}
			if (list.Count > 0)
			{
				Console.WriteLine("SSL policy error {0}. Fix the following errors {1}", SslPolicyErrors.RemoteCertificateChainErrors, string.Join(", ", list));
				flag = !flag && false;
			}
		}
		if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) != 0)
		{
			Console.WriteLine("SSL policy error {0}. The server certificate is not available for validation.", SslPolicyErrors.RemoteCertificateNotAvailable);
			flag = !flag && false;
		}
		return flag;
	}

	public static RequestSecurityTokenType GetHokRequestSecurityTokenType()
	{
		return new RequestSecurityTokenType
		{
			TokenType = TokenTypeEnum.urnoasisnamestcSAML20assertion,
			TokenTypeSpecified = true,
			RequestType = RequestTypeEnum.httpdocsoasisopenorgwssxwstrust200512Issue,
			KeyType = KeyTypeEnum.httpdocsoasisopenorgwssxwstrust200512PublicKey,
			KeyTypeSpecified = true,
			SignatureAlgorithm = SignatureAlgorithmEnum.httpwwww3org200104xmldsigmorersasha512,
			SignatureAlgorithmSpecified = true,
			Delegatable = true,
			DelegatableSpecified = true,
			Lifetime = GetLifetime(new TimeSpan(1, 10, 10)),
			Renewing = new RenewingType
			{
				Allow = true,
				OK = true
			}
		};
	}

	public static LifetimeType GetLifetime(TimeSpan? lifetimeSpan)
	{
		LifetimeType lifetimeType;
		if (lifetimeSpan.HasValue)
		{
			lifetimeType = new LifetimeType();
			AttributedDateTime attributedDateTime = new AttributedDateTime();
			string value = string.Format(strDateFormat, DateTime.Now.ToUniversalTime());
			attributedDateTime.Value = value;
			lifetimeType.Created = attributedDateTime;
			AttributedDateTime attributedDateTime2 = new AttributedDateTime();
			string value2 = string.Format(strDateFormat, DateTime.Now.Add(lifetimeSpan.Value).ToUniversalTime());
			attributedDateTime2.Value = value2;
			lifetimeType.Expires = attributedDateTime2;
		}
		else
		{
			lifetimeType = null;
		}
		return lifetimeType;
	}

	public static void PrintToken(XmlElement token)
	{
		if (token != null)
		{
			string value = token.Attributes.GetNamedItem(strAssertionId).Value;
			string value2 = token.Attributes.GetNamedItem(strIssueInstant).Value;
			string text = "";
			string value3 = token.GetElementsByTagName(strSubjectConfirmationNode).Item(0).Attributes.GetNamedItem(strSubjectConfirmationMethodValueAttribute).Value;
			if (value3 == strSubjectConfirmationMethodValueTypeHoK)
			{
				text = "Holder-Of-Key";
			}
			else if (value3 == strSubjectConfirmationMethodValueTypeBearer)
			{
				text = "Bearer";
			}
			Console.WriteLine("Token Details");
			Console.WriteLine("\tAssertionId =  " + value);
			Console.WriteLine("\tToken Type =  " + text);
			Console.WriteLine("\tIssued On =  " + value2);
		}
	}

	public static X509Certificate2 GetCertificate()
	{
		X509Certificate2 x509Certificate = new X509Certificate2();
		string fileName = ConfigurationManager.AppSettings["PfxCertificateFile"];
		x509Certificate.Import(fileName, "", X509KeyStorageFlags.MachineKeySet);
		return x509Certificate;
	}

	public static STSService_PortTypeClient GetSTSService(string url, string username = null, string password = null, X509Certificate2 signingCertificate = null, XmlElement rawToken = null)
	{
		CustomBinding wcfBinding = GetWcfBinding();
		EndpointAddress remoteAddress = new EndpointAddress(url);
		STSService_PortTypeClient sTSService_PortTypeClient = new STSService_PortTypeClient(wcfBinding, remoteAddress);
		sTSService_PortTypeClient.ChannelFactory.Endpoint.Behaviors.Add(new WsTrustBehavior(rawToken));
		SetCredentials(username, password, signingCertificate, sTSService_PortTypeClient.ClientCredentials);
		return sTSService_PortTypeClient;
	}
}
