using System.CodeDom.Compiler;
using System.ServiceModel;

namespace vmware.sso;

[ServiceContract(Namespace = "http://www.rsa.com/names/2009/12/product/riat/wsdl", ConfigurationName = "vmware.sso.STSService_PortType")]
[GeneratedCode("System.ServiceModel", "4.0.0.0")]
public interface STSService_PortType
{
	[ServiceKnownType(typeof(SignaturePropertiesType))]
	[ServiceKnownType(typeof(AllowPostdatingType))]
	[XmlSerializerFormat]
	[ServiceKnownType(typeof(TransformationParametersType))]
	[ServiceKnownType(typeof(SecurityTokenReferenceType))]
	[ServiceKnownType(typeof(ReferenceType1))]
	[ServiceKnownType(typeof(SecurityHeaderType))]
	[ServiceKnownType(typeof(ProblemActionType))]
	[ServiceKnownType(typeof(StatementAbstractType))]
	[OperationContract(Action = "http://docs.oasis-open.org/ws-sx/ws-trust/200512/RST/Issue", ReplyAction = "*")]
	[ServiceKnownType(typeof(ManifestType))]
	[ServiceKnownType(typeof(ObjectType))]
	IssueResponse Issue(IssueRequest request);

	[ServiceKnownType(typeof(ObjectType))]
	[ServiceKnownType(typeof(SecurityHeaderType))]
	[ServiceKnownType(typeof(StatementAbstractType))]
	[ServiceKnownType(typeof(SignaturePropertiesType))]
	[ServiceKnownType(typeof(ReferenceType1))]
	[ServiceKnownType(typeof(ManifestType))]
	[ServiceKnownType(typeof(SecurityTokenReferenceType))]
	[ServiceKnownType(typeof(ProblemActionType))]
	[OperationContract(Action = "http://docs.oasis-open.org/ws-sx/ws-trust/200512/RST/Renew", ReplyAction = "*")]
	[ServiceKnownType(typeof(AllowPostdatingType))]
	[XmlSerializerFormat]
	[ServiceKnownType(typeof(TransformationParametersType))]
	RenewResponse Renew(IssueRequest request);

	[ServiceKnownType(typeof(SignaturePropertiesType))]
	[OperationContract(Action = "http://docs.oasis-open.org/ws-sx/ws-trust/200512/RST/Validate", ReplyAction = "*")]
	[ServiceKnownType(typeof(ObjectType))]
	[ServiceKnownType(typeof(AllowPostdatingType))]
	[XmlSerializerFormat]
	[ServiceKnownType(typeof(ManifestType))]
	[ServiceKnownType(typeof(TransformationParametersType))]
	[ServiceKnownType(typeof(SecurityTokenReferenceType))]
	[ServiceKnownType(typeof(ReferenceType1))]
	[ServiceKnownType(typeof(SecurityHeaderType))]
	[ServiceKnownType(typeof(ProblemActionType))]
	[ServiceKnownType(typeof(StatementAbstractType))]
	RenewResponse Validate(IssueRequest request);

	[ServiceKnownType(typeof(StatementAbstractType))]
	[ServiceKnownType(typeof(ObjectType))]
	[ServiceKnownType(typeof(SecurityTokenReferenceType))]
	[ServiceKnownType(typeof(ReferenceType1))]
	[ServiceKnownType(typeof(SecurityHeaderType))]
	[ServiceKnownType(typeof(TransformationParametersType))]
	[ServiceKnownType(typeof(ManifestType))]
	[ServiceKnownType(typeof(AllowPostdatingType))]
	[XmlSerializerFormat]
	[ServiceKnownType(typeof(ProblemActionType))]
	[OperationContract(Action = "http://docs.oasis-open.org/ws-sx/ws-trust/200512/RSTR/Issue", ReplyAction = "*")]
	[ServiceKnownType(typeof(SignaturePropertiesType))]
	IssueResponse Challenge(RenewResponse request);
}
