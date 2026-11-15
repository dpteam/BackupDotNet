using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Xml;

namespace VMware.Binding.WsTrust;

public class WsTrustBehavior : IEndpointBehavior
{
	private readonly XmlElement _samlToken;

	public WsTrustBehavior(XmlElement samlToken)
	{
		_samlToken = samlToken;
	}

	public WsTrustBehavior()
	{
	}

	public void Validate(ServiceEndpoint endpoint)
	{
	}

	public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
	{
	}

	public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
	{
	}

	public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
	{
		ClientCredentials clientCredentials = endpoint.Behaviors.Find<ClientCredentials>();
		clientRuntime.MessageInspectors.Add(new WsTrustClientMessageInspector(clientCredentials, _samlToken));
	}
}
