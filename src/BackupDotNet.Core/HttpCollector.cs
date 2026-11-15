using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace BackupDotNet.Core;

public class HttpCollector : UptimeCollectorBase
{
	public override string Type => "http";

	public override List<CollectResult> Collect(Company company, Source source, AgentCtx ctx)
	{
		List<CollectResult> list = new List<CollectResult>();
		string url = source.GetStr("url", "").Trim();
		string job = UptimeCollectorBase.JobName(source, (url.Length > 0) ? Shorten(url) : "HTTP");
		CollectResult r = Make(company, source, job);
		r.Target = url;
		if (string.IsNullOrEmpty(url))
		{
			r.Reachable = false;
			r.Fail("не задан url");
			list.Add(r);
			return list;
		}
		string method = source.GetStr("method", "GET").ToUpperInvariant();
		if (method != "HEAD" && method != "POST")
		{
			method = "GET";
		}
		string keyword = source.GetStr("keyword", "");
		bool keywordRegex = source.GetBool("keywordRegex", dflt: false);
		string keywordInverse = source.GetStr("keywordMode", "").ToLowerInvariant();
		bool expectAbsent = keywordInverse == "absent";
		bool ignoreCert = source.GetBool("ignoreCertErrors", dflt: false);
		int timeoutSec = source.GetInt("timeoutSeconds", 10);
		if (timeoutSec <= 0)
		{
			timeoutSec = 10;
		}
		List<int> expected = new List<int>();
		foreach (object o in source.GetArray("expectedStatus"))
		{
			int code = Json.Int(o);
			if (code > 0)
			{
				expected.Add(code);
			}
		}
		if (expected.Count == 0)
		{
			expected.Add(200);
		}
		string body = source.GetStr("body", "");
		string contentType = source.GetStr("contentType", "");
		string userAgent = source.GetStr("userAgent", Branding.Current.UserAgent + "/1.0");
		Stopwatch sw = Stopwatch.StartNew();
		int status = 0;
		string respBody = "";
		string error = "";
		string finalUrl = url;
		bool ok = Perform(url, method, body, contentType, userAgent, ignoreCert, timeoutSec, out status, out respBody, out finalUrl, out error);
		sw.Stop();
		long elapsed = sw.ElapsedMilliseconds;
		if (!ok)
		{
			r.Reachable = false;
			r.Fail(error);
			r.ResponseMs = elapsed;
			list.Add(r);
			return list;
		}
		r.LastSuccess = DateTime.UtcNow;
		r.LastAttempt = DateTime.UtcNow;
		r.Target = finalUrl;
		if (!expected.Contains(status))
		{
			r.Fail("код ответа " + status + " (ожидался " + string.Join("/", expected.ConvertAll((int x) => x.ToString()).ToArray()) + ")");
			UptimeCollectorBase.ApplyLatency(r, elapsed, source);
			list.Add(r);
			return list;
		}
		if (keyword.Length > 0 && method != "HEAD")
		{
			bool found;
			if (keywordRegex)
			{
				try
				{
					found = Regex.IsMatch(respBody, keyword, RegexOptions.IgnoreCase);
				}
				catch (Exception ex)
				{
					r.Fail("некорректное регулярное выражение: " + ex.Message);
					list.Add(r);
					return list;
				}
			}
			else
			{
				found = respBody.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
			}
			if (!(expectAbsent ? (!found) : found))
			{
				r.Fail(expectAbsent ? ("в ответе найдено запрещённое «" + keyword + "»") : ("в ответе нет «" + keyword + "»"));
				UptimeCollectorBase.ApplyLatency(r, elapsed, source);
				list.Add(r);
				return list;
			}
		}
		UptimeCollectorBase.ApplyLatency(r, elapsed, source);
		if (string.IsNullOrEmpty(r.Message))
		{
			r.Message = "HTTP " + status + " за " + elapsed + " мс";
		}
		list.Add(r);
		return list;
	}

	private static bool Perform(string url, string method, string body, string contentType, string userAgent, bool ignoreCert, int timeoutSec, out int status, out string responseBody, out string finalUrl, out string error)
	{
		status = 0;
		responseBody = "";
		finalUrl = url;
		error = "";
		try
		{
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
			HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
			req.Method = method;
			req.Timeout = timeoutSec * 1000;
			req.ReadWriteTimeout = timeoutSec * 1000;
			req.AllowAutoRedirect = true;
			req.MaximumAutomaticRedirections = 5;
			req.UserAgent = userAgent;
			req.KeepAlive = false;
			if (ignoreCert)
			{
				req.ServerCertificateValidationCallback = (object param0, X509Certificate param1, X509Chain param2, SslPolicyErrors param3) => true;
			}
			if (method == "POST" && body.Length > 0)
			{
				byte[] data = Encoding.UTF8.GetBytes(body);
				req.ContentType = ((contentType.Length > 0) ? contentType : "application/x-www-form-urlencoded");
				req.ContentLength = data.Length;
				using Stream s = req.GetRequestStream();
				s.Write(data, 0, data.Length);
			}
			using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
			{
				status = (int)resp.StatusCode;
				finalUrl = ((resp.ResponseUri != null) ? resp.ResponseUri.ToString() : url);
				if (method != "HEAD")
				{
					using StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8);
					responseBody = sr.ReadToEnd();
				}
			}
			return true;
		}
		catch (WebException ex)
		{
			if (ex.Response is HttpWebResponse resp2)
			{
				status = (int)resp2.StatusCode;
				finalUrl = ((resp2.ResponseUri != null) ? resp2.ResponseUri.ToString() : url);
				try
				{
					using StreamReader sr2 = new StreamReader(resp2.GetResponseStream(), Encoding.UTF8);
					responseBody = sr2.ReadToEnd();
				}
				catch
				{
				}
				return true;
			}
			error = ((ex.Status == WebExceptionStatus.Timeout) ? ("таймаут " + timeoutSec + " с") : ex.Message);
			return false;
		}
		catch (Exception ex2)
		{
			error = ex2.Message;
			return false;
		}
	}

	private static string Shorten(string url)
	{
		string s = url.Replace("https://", "").Replace("http://", "").TrimEnd('/');
		if (s.Length > 40)
		{
			return s.Substring(0, 40) + "…";
		}
		return s;
	}
}
