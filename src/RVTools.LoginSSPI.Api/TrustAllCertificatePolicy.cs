using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace RVTools.LoginSSPI.Api;

internal class TrustAllCertificatePolicy
{
	private static bool ignoreServerCertificateErrors = true;

	public static bool Validate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
	{
		if (!ignoreServerCertificateErrors)
		{
			return sslPolicyErrors == SslPolicyErrors.None;
		}
		return true;
	}
}
