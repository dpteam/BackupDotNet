using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceModel;

namespace vmware.sso;

[DebuggerStepThrough]
[EditorBrowsable(EditorBrowsableState.Advanced)]
[MessageContract(IsWrapped = false)]
[GeneratedCode("System.ServiceModel", "4.0.0.0")]
public class RenewResponse
{
	[MessageBodyMember(Namespace = "http://docs.oasis-open.org/ws-sx/ws-trust/200512", Order = 0)]
	public RequestSecurityTokenResponseType RequestSecurityTokenResponse;

	public RenewResponse()
	{
	}

	public RenewResponse(RequestSecurityTokenResponseType RequestSecurityTokenResponse)
	{
		this.RequestSecurityTokenResponse = RequestSecurityTokenResponse;
	}
}
