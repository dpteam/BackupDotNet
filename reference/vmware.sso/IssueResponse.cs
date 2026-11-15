using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceModel;

namespace vmware.sso;

[GeneratedCode("System.ServiceModel", "4.0.0.0")]
[MessageContract(IsWrapped = false)]
[DebuggerStepThrough]
[EditorBrowsable(EditorBrowsableState.Advanced)]
public class IssueResponse
{
	[MessageBodyMember(Namespace = "http://docs.oasis-open.org/ws-sx/ws-trust/200512", Order = 0)]
	public RequestSecurityTokenResponseCollectionType RequestSecurityTokenResponseCollection;

	public IssueResponse()
	{
	}

	public IssueResponse(RequestSecurityTokenResponseCollectionType RequestSecurityTokenResponseCollection)
	{
		this.RequestSecurityTokenResponseCollection = RequestSecurityTokenResponseCollection;
	}
}
