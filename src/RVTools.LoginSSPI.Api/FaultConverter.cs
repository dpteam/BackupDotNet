using System;
using System.Linq;
using System.Web.Services.Protocols;
using System.Xml;
using System.Xml.Serialization;
using Vim25Api;

namespace RVTools.LoginSSPI.Api;

internal static class FaultConverter
{
	public static MethodFault CreateMethodFault(Exception exception)
	{
		MethodFault result = null;
		if (exception == null)
		{
			return null;
		}
		SoapException ex = (exception as SoapException) ?? (exception.InnerException as SoapException);
		if (ex != null)
		{
			result = ConvertSoapException(ex);
		}
		return result;
	}

	private static MethodFault ConvertSoapException(SoapException soapException)
	{
		if (soapException != null && soapException.Detail != null)
		{
			XmlNode xmlNode = soapException.Detail.ChildNodes.Cast<XmlNode>().FirstOrDefault((XmlNode xmlNode2) => !(xmlNode2 is XmlWhitespace));
			if (xmlNode != null)
			{
				string localName = xmlNode.LocalName;
				return GetMethodFault(xmlNode, typeof(VimPortType), "urn:vim25", localName);
			}
		}
		return null;
	}

	private static MethodFault GetMethodFault(XmlNode faultDetail, Type vimServiceType, string faultNamespace, string faultName)
	{
		if (faultDetail.LocalName.EndsWith("Fault"))
		{
			faultName = faultName.Substring(0, faultName.Length - "Fault".Length);
		}
		try
		{
			string name = vimServiceType.Namespace + "." + faultName;
			XmlSerializer xmlSerializer = new XmlSerializer(vimServiceType.Assembly.GetType(name, throwOnError: false), new XmlRootAttribute(faultDetail.LocalName)
			{
				Namespace = faultNamespace
			});
			XmlReader xmlReader = new XmlNodeReader(faultDetail);
			return xmlSerializer.Deserialize(xmlReader) as MethodFault;
		}
		catch (Exception)
		{
			return null;
		}
	}
}
