using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[DesignerCategory("code")]
[DebuggerStepThrough]
[XmlType(Namespace = "http://docs.oasis-open.org/ws-sx/ws-trust/200512")]
[GeneratedCode("svcutil", "4.0.30319.1")]
public class RequestSecurityTokenResponseType
{
	private TokenTypeEnum tokenTypeField;

	private LifetimeType lifetimeField;

	private XmlElement requestedSecurityTokenField;

	private RenewingType renewingField;

	private BinaryExchangeType binaryExchangeField;

	private KeyTypeEnum keyTypeField;

	private bool keyTypeFieldSpecified;

	private SignatureAlgorithmEnum signatureAlgorithmField;

	private bool signatureAlgorithmFieldSpecified;

	private bool delegatableField;

	private bool delegatableFieldSpecified;

	private StatusType statusField;

	private string contextField;

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

	public XmlElement RequestedSecurityToken
	{
		get
		{
			return requestedSecurityTokenField;
		}
		set
		{
			requestedSecurityTokenField = value;
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

	public StatusType Status
	{
		get
		{
			return statusField;
		}
		set
		{
			statusField = value;
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
}
