using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace BackupDotNet.Core;

public static class MtRest
{
	public static MikroTikCollector.MtInfo Read(string host, int port, bool tls, string user, string password, int timeoutSec)
	{
		string scheme = (tls ? "https" : "http");
		string baseUrl = scheme + "://" + host + ":" + port + "/rest";
		Dictionary<string, object> resource = Get(baseUrl + "/system/resource", user, password, timeoutSec);
		if (resource == null || resource.Count == 0)
		{
			return null;
		}
		MikroTikCollector.MtInfo info = new MikroTikCollector.MtInfo();
		info.Transport = "REST";
		info.Version = Json.GetStr(resource, "version", "");
		info.Architecture = Json.GetStr(resource, "architecture-name", "");
		info.BoardName = Json.GetStr(resource, "board-name", "");
		info.UptimeMinutes = MikroTikCollector.ParseUptime(Json.GetStr(resource, "uptime", ""));
		info.CpuLoad = Json.Int(Json.Get(resource, "cpu-load"), -1);
		info.CpuCount = Json.Int(Json.Get(resource, "cpu-count"), -1);
		long freeMem = Json.Long(Json.Get(resource, "free-memory"), -1L);
		if (freeMem >= 0)
		{
			info.FreeMemoryMb = freeMem / 1048576;
		}
		long totalMem = Json.Long(Json.Get(resource, "total-memory"), -1L);
		if (totalMem > 0)
		{
			info.TotalMemoryMb = totalMem / 1048576;
		}
		try
		{
			Dictionary<string, object> id = Get(baseUrl + "/system/identity", user, password, timeoutSec);
			if (id != null)
			{
				info.Identity = Json.GetStr(id, "name", "");
			}
		}
		catch
		{
		}
		try
		{
			Dictionary<string, object> rb = Get(baseUrl + "/system/routerboard", user, password, timeoutSec);
			if (rb != null)
			{
				string model = Json.GetStr(rb, "model", "");
				if (model.Length > 0)
				{
					info.BoardName = model;
				}
				info.Serial = Json.GetStr(rb, "serial-number", "");
			}
		}
		catch
		{
		}
		try
		{
			Dictionary<string, object> health = Get(baseUrl + "/system/health", user, password, timeoutSec);
			if (health != null)
			{
				int t = Json.Int(Json.Get(health, "temperature"), -1);
				if (t <= 0)
				{
					t = Json.Int(Json.Get(health, "cpu-temperature"), -1);
				}
				if (t > 0)
				{
					info.Temperature = t;
				}
			}
		}
		catch
		{
		}
		return info;
	}

	private static Dictionary<string, object> Get(string url, string user, string password, int timeoutSec)
	{
		HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
		req.Method = "GET";
		req.Timeout = timeoutSec * 1000;
		req.ReadWriteTimeout = timeoutSec * 1000;
		req.Accept = "application/json";
		req.UserAgent = Branding.Current.UserAgent;
		if (user.Length > 0)
		{
			string auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(user + ":" + password));
			req.Headers["Authorization"] = "Basic " + auth;
		}
		ServicePointManager.ServerCertificateValidationCallback = (object param0, X509Certificate param1, X509Chain param2, SslPolicyErrors param3) => true;
		try
		{
			using HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
			using StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8);
			string body = sr.ReadToEnd();
			if (body.TrimStart().StartsWith("["))
			{
				List<object> arr = Json.ParseArray(body);
				if (arr.Count == 0)
				{
					return null;
				}
				return Json.AsObject(arr[0]);
			}
			return Json.ParseObject(body);
		}
		catch (WebException ex)
		{
			if (ex.Response is HttpWebResponse er)
			{
				if (er.StatusCode == HttpStatusCode.Unauthorized)
				{
					throw new MtException(MtErrorKind.Auth, "неверный логин или пароль", ex);
				}
				if (er.StatusCode == HttpStatusCode.NotFound)
				{
					throw new MtException(MtErrorKind.ServiceDisabled, "REST не включён (нет /rest)", ex);
				}
				throw new MtException(MtErrorKind.Unknown, "HTTP " + (int)er.StatusCode, ex);
			}
			MtErrorKind kind = MtErrorKind.NoConnection;
			if (ex.Status == WebExceptionStatus.ProtocolError)
			{
				kind = MtErrorKind.Unknown;
			}
			throw new MtException(kind, ex.Message, ex);
		}
	}
}
