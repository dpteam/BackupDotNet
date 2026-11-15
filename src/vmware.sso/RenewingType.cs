using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace vmware.sso;

[Serializable]
[GeneratedCode("svcutil", "4.0.30319.1")]
[XmlType(Namespace = "http://docs.oasis-open.org/ws-sx/ws-trust/200512")]
[DesignerCategory("code")]
[DebuggerStepThrough]
public class RenewingType
{
	private bool allowField;

	private bool allowFieldSpecified;

	private bool okField;

	private bool okFieldSpecified;

	[XmlAttribute]
	public bool Allow
	{
		get
		{
			return allowField;
		}
		set
		{
			allowField = value;
		}
	}

	[XmlIgnore]
	public bool AllowSpecified
	{
		get
		{
			return allowFieldSpecified;
		}
		set
		{
			allowFieldSpecified = value;
		}
	}

	[XmlAttribute]
	public bool OK
	{
		get
		{
			return okField;
		}
		set
		{
			okField = value;
		}
	}

	[XmlIgnore]
	public bool OKSpecified
	{
		get
		{
			return okFieldSpecified;
		}
		set
		{
			okFieldSpecified = value;
		}
	}

	public RenewingType()
	{
		okField = false;
	}
}
