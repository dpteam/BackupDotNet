using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace vmware.sso;

[DebuggerStepThrough]
[GeneratedCode("System.ServiceModel", "4.0.0.0")]
public class STSService_PortTypeClient : ClientBase<STSService_PortType>, STSService_PortType
{
	public STSService_PortTypeClient()
	{
	}

	public STSService_PortTypeClient(string endpointConfigurationName)
		: base(endpointConfigurationName)
	{
	}

	public STSService_PortTypeClient(string endpointConfigurationName, string remoteAddress)
		: base(endpointConfigurationName, remoteAddress)
	{
	}

	public STSService_PortTypeClient(string endpointConfigurationName, EndpointAddress remoteAddress)
		: base(endpointConfigurationName, remoteAddress)
	{
	}

	public STSService_PortTypeClient(Binding binding, EndpointAddress remoteAddress)
		: base(binding, remoteAddress)
	{
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	IssueResponse STSService_PortType.Issue(IssueRequest request)
	{
		return Channel.Issue(request);
	}

	public RequestSecurityTokenResponseCollectionType Issue(RequestSecurityTokenType RequestSecurityToken)
	{
		IssueRequest issueRequest = new IssueRequest();
		issueRequest.RequestSecurityToken = RequestSecurityToken;
		IssueResponse issueResponse = ((STSService_PortType)this).Issue(issueRequest);
		return issueResponse.RequestSecurityTokenResponseCollection;
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	RenewResponse STSService_PortType.Renew(IssueRequest request)
	{
		return Channel.Renew(request);
	}

	public RequestSecurityTokenResponseType Renew(RequestSecurityTokenType RequestSecurityToken)
	{
		IssueRequest issueRequest = new IssueRequest();
		issueRequest.RequestSecurityToken = RequestSecurityToken;
		RenewResponse renewResponse = ((STSService_PortType)this).Renew(issueRequest);
		return renewResponse.RequestSecurityTokenResponse;
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	RenewResponse STSService_PortType.Validate(IssueRequest request)
	{
		return Channel.Validate(request);
	}

	public RequestSecurityTokenResponseType Validate(RequestSecurityTokenType RequestSecurityToken)
	{
		IssueRequest issueRequest = new IssueRequest();
		issueRequest.RequestSecurityToken = RequestSecurityToken;
		RenewResponse renewResponse = ((STSService_PortType)this).Validate(issueRequest);
		return renewResponse.RequestSecurityTokenResponse;
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	IssueResponse STSService_PortType.Challenge(RenewResponse request)
	{
		return Channel.Challenge(request);
	}

	public RequestSecurityTokenResponseCollectionType Challenge(RequestSecurityTokenResponseType RequestSecurityTokenResponse)
	{
		RenewResponse renewResponse = new RenewResponse();
		renewResponse.RequestSecurityTokenResponse = RequestSecurityTokenResponse;
		IssueResponse issueResponse = ((STSService_PortType)this).Challenge(renewResponse);
		return issueResponse.RequestSecurityTokenResponseCollection;
	}
}
