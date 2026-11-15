using System;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;
using log4net;
using RVTools.LoginSSPI.Security;
using Vim25Api;
using VMware.Binding.WsTrust;

namespace RVTools.LoginSSPI.Api;

internal class ClientConnection
{
	private const string Protocol = "https";

	private const int Port = 443;

	private static readonly ILog logger = LogManager.GetLogger(typeof(ClientConnection));

	public VimPortType VimService { get; set; }

	public ServiceContent Sic { get; set; }

	public ManagedObjectReference SvcRef { get; set; }

	public UserSession _session { get; set; }

	public string BuildServerUrl(string protocol, string serverName, int port, string path)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		UriBuilder uriBuilder = new UriBuilder(protocol, serverName, port, path);
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit" + uriBuilder.Uri.ToString());
		}
		return uriBuilder.Uri.ToString();
	}

	private static VimPortType GetVimService(string url, string username = null, string password = null, X509Certificate2 signingCertificate = null, XmlElement rawToken = null)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		CustomBinding wcfBinding = SamlTokenHelper.GetWcfBinding();
		EndpointAddress remoteAddress = new EndpointAddress(url);
		ChannelFactory<VimPortType> channelFactory = new ChannelFactory<VimPortType>(wcfBinding, remoteAddress);
		channelFactory.Endpoint.Behaviors.Add(new WsTrustBehavior(rawToken));
		SamlTokenHelper.SetCredentials(username, password, signingCertificate, channelFactory.Credentials);
		VimPortType result = channelFactory.CreateChannel();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
		return result;
	}

	internal bool ConnectVim(string serverEndPoint, int port, string protocol)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		string url = BuildServerUrl(protocol, serverEndPoint, port, "/sdk");
		SvcRef = new ManagedObjectReference
		{
			type = "ServiceInstance",
			Value = "ServiceInstance"
		};
		VimService = GetVimService(url);
		Sic = VimService.RetrieveServiceContent(SvcRef);
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
		return Sic != null;
	}

	internal void Connect(string serverEndPoint, string username, string password)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		InitializeServicePointManager();
		if (!ConnectVim(serverEndPoint, 443, "https"))
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " throw new ApiException: Service Unavailable");
			}
			throw new ApiException("Service Unavailable");
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
		VimService.Login(Sic.sessionManager, username, password, null);
	}

	internal void ConnectBySspi(string PackageType, string serverEndPoint, string serviceSpn = null)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		InitializeServicePointManager();
		if (!ConnectVim(serverEndPoint, 443, "https"))
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " throw new ApiException: Service Unavailable");
			}
			throw new ApiException("Service Unavailable");
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " PackageType = " + PackageType);
		}
		if (PackageType == "Negotiate")
		{
			_session = LoginBySspiPackage(SspiPackageType.Negotiate, serviceSpn);
		}
		if (PackageType == "Kerberos")
		{
			_session = LoginBySspiPackage(SspiPackageType.Kerberos, serviceSpn);
		}
		if (PackageType == "NTLM")
		{
			_session = LoginBySspiPackage(SspiPackageType.NTLM, serviceSpn);
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	internal void ConnectBySspiCustomException(string PackageType, string serverEndPoint, string serviceSpn = null)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		InitializeServicePointManager();
		if (!ConnectVim(serverEndPoint, 443, "https"))
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug(currentMethod.Name + " throw new ApiException: Service Unavailable");
			}
			throw new ApiException("Service Unavailable");
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " PackageType = " + PackageType);
		}
		if (PackageType == "Negotiate")
		{
			_session = LoginBySspiPackageCustomException(SspiPackageType.Negotiate, serviceSpn);
		}
		if (PackageType == "Kerberos")
		{
			_session = LoginBySspiPackageCustomException(SspiPackageType.Kerberos, serviceSpn);
		}
		if (PackageType == "NTLM")
		{
			_session = LoginBySspiPackageCustomException(SspiPackageType.NTLM, serviceSpn);
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	internal void Disconnect()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		if (VimService != null)
		{
			VimService.Logout(Sic.sessionManager);
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private UserSession LoginBySspiPackage(SspiPackageType sspiPackage, string serviceSpn)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		SspiClient sspiClient = new SspiClient(serviceSpn, sspiPackage);
		bool flag = true;
		UserSession result = null;
		while (flag)
		{
			try
			{
				result = VimService.LoginBySSPI(Sic.sessionManager, Convert.ToBase64String(sspiClient.Token), "en");
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Connected!");
				}
				flag = false;
				VimService.CurrentTime(SvcRef);
			}
			catch (Exception ex)
			{
				MethodFault methodFault = FaultConverter.CreateMethodFault(ex);
				if (methodFault is SSPIChallenge)
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(currentMethod.Name + " fault is SSPIChallenge");
					}
					SSPIChallenge sSPIChallenge = methodFault as SSPIChallenge;
					try
					{
						sspiClient.Initialize(Convert.FromBase64String(sSPIChallenge.base64Token));
						VimService.CurrentTime(SvcRef);
						if (logger.IsDebugEnabled)
						{
							logger.Debug(currentMethod.Name + " Connected!");
						}
						flag = false;
					}
					catch (Exception)
					{
						if (logger.IsDebugEnabled)
						{
							logger.Debug(currentMethod.Name + " serverNotReady");
						}
						flag = true;
					}
					continue;
				}
				if (methodFault is InvalidLogin)
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(currentMethod.Name + " throw new InvalidLoginException: " + ex.Message);
					}
					throw new InvalidLoginException(ex.Message);
				}
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " throw");
				}
				throw;
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
		return result;
	}

	private UserSession LoginBySspiPackageCustomException(SspiPackageType sspiPackage, string serviceSpn)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		SspiClient sspiClient = new SspiClient(serviceSpn, sspiPackage);
		bool flag = true;
		UserSession result = null;
		while (flag)
		{
			try
			{
				result = VimService.LoginBySSPI(Sic.sessionManager, Convert.ToBase64String(sspiClient.Token), "en");
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " Connected!");
				}
				flag = false;
				VimService.CurrentTime(SvcRef);
			}
			catch (FaultException<SSPIChallenge> ex)
			{
				if (ex.Message.Contains("SSPIChallenge"))
				{
					if (logger.IsDebugEnabled)
					{
						logger.Debug(currentMethod.Name + " SSPIChallenge");
					}
					try
					{
						sspiClient.Initialize(Convert.FromBase64String(ex.Detail.base64Token));
						VimService.CurrentTime(SvcRef);
						if (logger.IsDebugEnabled)
						{
							logger.Debug(currentMethod.Name + " Connected!");
						}
						flag = false;
					}
					catch
					{
						if (logger.IsDebugEnabled)
						{
							logger.Debug(currentMethod.Name + " serverNotReady");
						}
						flag = true;
					}
					continue;
				}
				if (logger.IsDebugEnabled)
				{
					logger.Debug(currentMethod.Name + " throw");
				}
				throw;
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
		return result;
	}

	private void InitializeServicePointManager()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		ServicePointManager.ServerCertificateValidationCallback = TrustAllCertificatePolicy.Validate;
		ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}
}
