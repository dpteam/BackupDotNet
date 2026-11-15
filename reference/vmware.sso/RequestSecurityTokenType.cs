using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[XmlType(Namespace = "http://docs.oasis-open.org/ws-sx/ws-trust/200512")]
[DesignerCategory("code")]
[GeneratedCode("svcutil", "4.0.30319.1")]
[DebuggerStepThrough]
public class RequestSecurityTokenType
{
	private TokenTypeEnum tokenTypeField;

	private bool tokenTypeFieldSpecified;

	private RequestTypeEnum requestTypeField;

	private LifetimeType lifetimeField;

	private RenewingType renewingField;

	private BinaryExchangeType binaryExchangeField;

	private KeyTypeEnum keyTypeField;

	private bool keyTypeFieldSpecified;

	private SignatureAlgorithmEnum signatureAlgorithmField;

	private bool signatureAlgorithmFieldSpecified;

	private UseKeyType useKeyField;

	private DelegateToType delegateToField;

	private bool delegatableField;

	private bool delegatableFieldSpecified;

	private ParticipantsType participantsField;

	private AdviceType[] adviceSetField;

	private XmlElement validateTargetField;

	private XmlElement renewTargetField;

	private string contextField;

	private XmlAttribute[] anyAttrField;

	public TokenTypeEnum TokenType
	{
		get
		{
			return tokenTypeField;
		}
		set
		{
			tokenTypeField = value;
		}
	}

	[XmlIgnore]
	public bool TokenTypeSpecified
	{
		get
		{
			return tokenTypeFieldSpecified;
		}
		set
		{
			tokenTypeFieldSpecified = value;
		}
	}

	public RequestTypeEnum RequestType
	{
		get
		{
			return requestTypeField;
		}
		set
		{
			requestTypeField = value;
		}
	}

	public LifetimeType Lifetime
	{
		get
		{
			return lifetimeField;
		}
		set
		{
			lifetimeField = value;
		}
	}

	public RenewingType Renewing
	{
		get
		{
			return renewingField;
		}
		set
		{
			renewingField = value;
		}
	}

	public BinaryExchangeType BinaryExchange
	{
		get
		{
			return binaryExchangeField;
		}
		set
		{
			binaryExchangeField = value;
		}
	}

	public KeyTypeEnum KeyType
	{
		get
		{
			return keyTypeField;
		}
		set
		{
			keyTypeField = value;
		}
	}

	[XmlIgnore]
	public bool KeyTypeSpecified
	{
		get
		{
			return keyTypeFieldSpecified;
		}
		set
		{
			keyTypeFieldSpecified = value;
		}
	}

	public SignatureAlgorithmEnum SignatureAlgorithm
	{
		get
		{
			return signatureAlgorithmField;
		}
		set
		{
			signatureAlgorithmField = value;
		}
	}

	[XmlIgnore]
	public bool SignatureAlgorithmSpecified
	{
		get
		{
			return signatureAlgorithmFieldSpecified;
		}
		set
		{
			signatureAlgorithmFieldSpecified = value;
		}
	}

	public UseKeyType UseKey
	{
		get
		{
			return useKeyField;
		}
		set
		{
			useKeyField = value;
		}
	}

	public DelegateToType DelegateTo
	{
		get
		{
			return delegateToField;
		}
		set
		{
			delegateToField = value;
		}
	}

	public bool Delegatable
	{
		get
		{
			return delegatableField;
		}
		set
		{
			delegatableField = value;
		}
	}

	[XmlIgnore]
	public bool DelegatableSpecified
	{
		get
		{
			return delegatableFieldSpecified;
		}
		set
		{
			delegatableFieldSpecified = value;
		}
	}

	public ParticipantsType Participants
	{
		get
		{
			return participantsField;
		}
		set
		{
			participantsField = value;
		}
	}

	[XmlArrayItem("Advice", IsNullable = false)]
	[XmlArray(Namespace = "http://www.rsa.com/names/2009/12/std-ext/WS-Trust1.4/advice")]
	public AdviceType[] AdviceSet
	{
		get
		{
			return adviceSetField;
		}
		set
		{
			adviceSetField = value;
		}
	}

	public XmlElement ValidateTarget
	{
		get
		{
			return validateTargetField;
		}
		set
		{
			validateTargetField = value;
		}
	}

	public XmlElement RenewTarget
	{
		get
		{
			return renewTargetField;
		}
		set
		{
			renewTargetField = value;
		}
	}

	[XmlAttribute(DataType = "anyURI")]
	public string Context
	{
		get
		{
			return contextField;
		}
		set
		{
			contextField = value;
		}
	}

	[XmlAnyAttribute]
	public XmlAttribute[] AnyAttr
	{
		get
		{
			return anyAttrField;
		}
		set
		{
			anyAttrField = value;
		}
	}
}
