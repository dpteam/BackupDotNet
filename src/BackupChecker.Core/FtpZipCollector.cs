using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace BackupChecker.Core;

public class FtpZipCollector : ICollector
{
	private class FtpEntry
	{
		public string Name = "";

		public long Size;

		public DateTime ModifiedUtc = DateTime.MinValue;

		public bool IsDirectory;
	}

	private static readonly Regex UnixRe = new Regex("^([\\-dl])[rwxsStT\\-]{9}\\s+\\d+\\s+\\S+\\s+\\S+\\s+(\\d+)\\s+(\\w{3})\\s+(\\d{1,2})\\s+(\\d{1,2}:\\d{2}|\\d{4})\\s+(.+)$", RegexOptions.Compiled);

	private static readonly Regex DosRe = new Regex("^(\\d{2})-(\\d{2})-(\\d{2,4})\\s+(\\d{1,2}:\\d{2}\\s*[AP]M)\\s+(<DIR>|\\d+)\\s+(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	public string Type => "iperius-ftp";

	public List<CollectResult> Collect(Company company, Source source, AgentCtx ctx)
	{
		List<CollectResult> list = new List<CollectResult>();
		CollectResult r = new CollectResult();
		r.CompanyId = company.Id;
		r.CompanyName = company.Name;
		r.SourceId = source.Id;
		r.Kind = "ftp";
		r.Job = source.GetStr("jobName", source.Id);
		string host = source.GetStr("ftp.host", "");
		int port = source.GetInt("ftp.port", 21);
		string user = source.GetStr("ftp.username", "anonymous");
		string secretKey = source.GetStr("ftp.passwordSecret", "");
		string password = (string.IsNullOrEmpty(secretKey) ? "" : ctx.Config.Secret(secretKey));
		string remotePath = source.GetStr("ftp.remotePath", "/");
		bool useTls = source.GetBool("ftp.useTls", dflt: false);
		bool keepLocal = source.GetBool("ftp.keepLocalCopy", dflt: false);
		string pattern = source.GetStr("ftp.pattern", "*.zip");
		int maxAgeHours = source.GetInt("ftp.maxFileAgeHours", company.Policy.FreshnessHours);
		if (string.IsNullOrEmpty(host))
		{
			r.Reachable = false;
			r.Fail("не задан ftp.host");
			list.Add(r);
			return list;
		}
		List<FtpEntry> entries = ListDirectory(host, port, user, password, remotePath, useTls, ctx.Settings.FtpTimeoutSeconds, out var error);
		if (entries == null)
		{
			r.Reachable = false;
			r.Fail("не удалось получить список файлов: " + error);
			list.Add(r);
			return list;
		}
		FtpEntry newest = null;
		foreach (FtpEntry e in entries)
		{
			if (!e.IsDirectory && MatchPattern(e.Name, pattern) && (newest == null || e.ModifiedUtc > newest.ModifiedUtc))
			{
				newest = e;
			}
		}
		if (newest == null)
		{
			r.Message = "на FTP нет файлов по маске " + pattern;
			list.Add(r);
			return list;
		}
		r.LastSuccess = newest.ModifiedUtc;
		r.LastAttempt = newest.ModifiedUtc;
		r.SizeBytes = newest.Size;
		r.Target = "ftp://" + host + remotePath + "/" + newest.Name;
		double ageH = (DateTime.UtcNow - newest.ModifiedUtc).TotalHours;
		if (ageH > (double)maxAgeHours)
		{
			r.Fail($"последний архив {ageH:0.#} ч назад (порог {maxAgeHours} ч)");
		}
		string local = Path.Combine(ctx.TempDir, Sanitize(newest.Name));
		try
		{
			if (File.Exists(local))
			{
				File.Delete(local);
			}
			if (!Download(host, port, user, password, remotePath, newest.Name, local, useTls, ctx.Settings.FtpTimeoutSeconds, out var dlErr))
			{
				r.Fail("не удалось скачать «" + newest.Name + "»: " + dlErr);
				list.Add(r);
				return list;
			}
			if (!ZipCollector.VerifyZip(local, Math.Max(64, ctx.Settings.ZipBufferKb) * 1024, out var entriesCount, out var total, out var verr))
			{
				r.Fail("архив повреждён: " + verr);
			}
			else if (string.IsNullOrEmpty(r.Message))
			{
				r.Message = $"скачано и проверено: {entriesCount} записей, {Fmt.Size(total)}";
			}
		}
		catch (Exception ex)
		{
			r.Fail("ошибка проверки: " + ex.Message);
		}
		finally
		{
			if (!keepLocal)
			{
				try
				{
					if (File.Exists(local))
					{
						File.Delete(local);
					}
				}
				catch
				{
				}
			}
		}
		list.Add(r);
		return list;
	}

	private static List<FtpEntry> ListDirectory(string host, int port, string user, string password, string path, bool useTls, int timeoutSec, out string error)
	{
		error = "";
		try
		{
			string uri = "ftp://" + host + ":" + port + NormalizePath(path) + "/";
			FtpWebRequest req = (FtpWebRequest)WebRequest.Create(uri);
			req.Method = "LIST";
			req.Credentials = new NetworkCredential(user, password);
			req.UsePassive = true;
			req.UseBinary = true;
			req.EnableSsl = useTls;
			req.Timeout = timeoutSec * 1000;
			req.ReadWriteTimeout = timeoutSec * 1000;
			req.KeepAlive = false;
			string listing;
			using (FtpWebResponse resp = (FtpWebResponse)req.GetResponse())
			{
				using StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8);
				listing = sr.ReadToEnd();
			}
			return ParseListing(listing);
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return null;
		}
	}

	private static string NormalizePath(string p)
	{
		if (string.IsNullOrEmpty(p))
		{
			return "";
		}
		if (!p.StartsWith("/"))
		{
			p = "/" + p;
		}
		if (p.Length > 1 && p.EndsWith("/"))
		{
			p = p.TrimEnd('/');
		}
		return p;
	}

	private static List<FtpEntry> ParseListing(string listing)
	{
		List<FtpEntry> result = new List<FtpEntry>();
		string[] lines = listing.Replace("\r\n", "\n").Split('\n');
		string[] array = lines;
		foreach (string raw in array)
		{
			string line = raw.TrimEnd();
			if (line.Length == 0)
			{
				continue;
			}
			Match m = UnixRe.Match(line);
			if (m.Success)
			{
				FtpEntry e = new FtpEntry();
				e.IsDirectory = m.Groups[1].Value == "d";
				e.Size = Json.Long(m.Groups[2].Value);
				string mon = m.Groups[3].Value;
				int day = Json.Int(m.Groups[4].Value, 1);
				string timeOrYear = m.Groups[5].Value;
				e.Name = m.Groups[6].Value.Trim();
				int month = MonthFromName(mon);
				int year = DateTime.UtcNow.Year;
				if (timeOrYear.IndexOf(':') < 0)
				{
					year = Json.Int(timeOrYear, year);
					e.ModifiedUtc = SafeDate(year, month, day, 0, 0);
				}
				else
				{
					string[] hm = timeOrYear.Split(':');
					e.ModifiedUtc = SafeDate(year, month, day, Json.Int(hm[0]), Json.Int(hm[1]));
					if (e.ModifiedUtc > DateTime.UtcNow.AddDays(1.0))
					{
						e.ModifiedUtc = e.ModifiedUtc.AddYears(-1);
					}
				}
				result.Add(e);
				continue;
			}
			Match d = DosRe.Match(line);
			if (d.Success)
			{
				FtpEntry e2 = new FtpEntry();
				int mm = Json.Int(d.Groups[1].Value);
				int dd = Json.Int(d.Groups[2].Value);
				int yy = Json.Int(d.Groups[3].Value);
				if (yy < 100)
				{
					yy += 2000;
				}
				string sizeOrDir = d.Groups[4].Value.Trim();
				e2.IsDirectory = sizeOrDir.Equals("<DIR>", StringComparison.OrdinalIgnoreCase);
				e2.Size = (e2.IsDirectory ? 0 : Json.Long(sizeOrDir));
				e2.Name = d.Groups[5].Value.Trim();
				if (DateTime.TryParse(yy + "-" + mm + "-" + dd + " " + d.Groups[4].Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
				{
					e2.ModifiedUtc = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
				}
				result.Add(e2);
			}
			else if (line.IndexOf(' ') < 0)
			{
				FtpEntry e3 = new FtpEntry();
				e3.Name = line.Trim();
				e3.ModifiedUtc = DateTime.UtcNow;
				result.Add(e3);
			}
		}
		return result;
	}

	private static DateTime SafeDate(int y, int m, int d, int hh, int mm)
	{
		try
		{
			if (m < 1 || m > 12)
			{
				m = 1;
			}
			if (d < 1 || d > 31)
			{
				d = 1;
			}
			if (hh < 0 || hh > 23)
			{
				hh = 0;
			}
			if (mm < 0 || mm > 59)
			{
				mm = 0;
			}
			return new DateTime(y, m, d, hh, mm, 0, DateTimeKind.Utc);
		}
		catch
		{
			return DateTime.UtcNow;
		}
	}

	private static int MonthFromName(string s)
	{
		return s.ToLowerInvariant() switch
		{
			"jan" => 1, 
			"feb" => 2, 
			"mar" => 3, 
			"apr" => 4, 
			"may" => 5, 
			"jun" => 6, 
			"jul" => 7, 
			"aug" => 8, 
			"sep" => 9, 
			"oct" => 10, 
			"nov" => 11, 
			"dec" => 12, 
			_ => 1, 
		};
	}

	private static bool Download(string host, int port, string user, string password, string remotePath, string fileName, string localPath, bool useTls, int timeoutSec, out string error)
	{
		error = "";
		try
		{
			string uri = "ftp://" + host + ":" + port + NormalizePath(remotePath) + "/" + fileName;
			FtpWebRequest req = (FtpWebRequest)WebRequest.Create(uri);
			req.Method = "RETR";
			req.Credentials = new NetworkCredential(user, password);
			req.UsePassive = true;
			req.UseBinary = true;
			req.EnableSsl = useTls;
			req.Timeout = timeoutSec * 1000;
			req.ReadWriteTimeout = timeoutSec * 1000;
			req.KeepAlive = false;
			using (FtpWebResponse resp = (FtpWebResponse)req.GetResponse())
			{
				using Stream src = resp.GetResponseStream();
				using FileStream dst = File.Create(localPath);
				byte[] buf = new byte[262144];
				while (true)
				{
					int n = src.Read(buf, 0, buf.Length);
					if (n > 0)
					{
						dst.Write(buf, 0, n);
						continue;
					}
					break;
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	private static bool MatchPattern(string name, string pattern)
	{
		if (string.IsNullOrEmpty(pattern))
		{
			pattern = "*";
		}
		string rx = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
		return Regex.IsMatch(name, rx, RegexOptions.IgnoreCase);
	}

	private static string Sanitize(string name)
	{
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (char c in invalidFileNameChars)
		{
			name = name.Replace(c, '_');
		}
		return name;
	}
}
