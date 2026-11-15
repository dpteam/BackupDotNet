using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[DesignerCategory("code")]
[GeneratedCode("svcutil", "4.0.30319.1")]
[XmlType(Namespace = "urn:oasis:names:tc:SAML:2.0:assertion")]
[XmlInclude(typeof(DelegationRestrictionType))]
[XmlInclude(typeof(ProxyRestrictionType))]
[XmlInclude(typeof(AudienceRestrictionType))]
[XmlInclude(typeof(RenewRestrictionType))]
[DebuggerStepThrough]
public abstract class ConditionAbstractType
{
}
