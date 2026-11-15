using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceModel;

namespace vmware.sso;

[EditorBrowsable(EditorBrowsableState.Advanced)]
[GeneratedCode("System.ServiceModel", "4.0.0.0")]
[MessageContract(IsWrapped = false)]
[DebuggerStepThrough]
public class IssueRequest
{
	[MessageBodyMember(Namespace = "http://docs.oasis-open.org/ws-sx/ws-trust/200512", Order = 0)]
	public RequestSecurityTokenType RequestSecurityToken;

	public IssueRequest()
	{
	}

	public IssueRequest(RequestSecurityTokenType RequestSecurityToken)
	{
		this.RequestSecurityToken = RequestSecurityToken;
	}
}
