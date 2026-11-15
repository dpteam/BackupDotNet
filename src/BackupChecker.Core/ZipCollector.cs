using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace BackupChecker.Core;

public class ZipCollector : ICollector
{
	public string Type => "zip";

	public List<CollectResult> Collect(Company company, Source source, AgentCtx ctx)
	{
		List<CollectResult> list = new List<CollectResult>();
		CollectResult r = new CollectResult();
		r.CompanyId = company.Id;
		r.CompanyName = company.Name;
		r.SourceId = source.Id;
		r.Kind = "zip";
		r.Job = source.GetStr("jobName", source.Id);
		List<string> patterns = new List<string>();
		foreach (object o in source.GetArray("paths"))
		{
			patterns.Add(Json.Str(o));
		}
		string mode = source.GetStr("checkCrc", "always").ToLowerInvariant();
		bool deep = mode == "always";
		if (mode == "weekly")
		{
			deep = DateTime.Now.DayOfWeek == DayOfWeek.Sunday;
		}
		FileInfo newest = null;
		foreach (string pattern in patterns)
		{
			if (string.IsNullOrEmpty(pattern))
			{
				continue;
			}
			try
			{
				string dir = Path.GetDirectoryName(pattern);
				string mask = Path.GetFileName(pattern);
				if (string.IsNullOrEmpty(dir))
				{
					dir = ".";
				}
				if (string.IsNullOrEmpty(mask))
				{
					mask = "*.zip";
				}
				if (!Directory.Exists(dir))
				{
					r.Fail("каталог недоступен: " + dir);
					continue;
				}
				string[] files = Directory.GetFiles(dir, mask);
				foreach (string f in files)
				{
					FileInfo fi = new FileInfo(f);
					if (newest == null || fi.LastWriteTimeUtc > newest.LastWriteTimeUtc)
					{
						newest = fi;
					}
				}
			}
			catch (Exception ex)
			{
				r.Fail("ошибка доступа к " + pattern + ": " + ex.Message);
			}
		}
		if (newest == null)
		{
			r.Message = "архивы не найдены";
			list.Add(r);
			return list;
		}
		r.LastSuccess = newest.LastWriteTimeUtc;
		r.LastAttempt = newest.LastWriteTimeUtc;
		r.SizeBytes = newest.Length;
		r.Target = newest.FullName;
		if (deep)
		{
			if (!VerifyZip(newest.FullName, Math.Max(64, ctx.Settings.ZipBufferKb) * 1024, out var entries, out var total, out var err))
			{
				r.Fail("архив повреждён: " + err);
			}
			else
			{
				r.Message = $"проверено записей: {entries}, распаковано {Fmt.Size(total)}";
			}
		}
		else
		{
			r.Message = "проверка по размеру и дате (глубокая — по расписанию)";
		}
		list.Add(r);
		return list;
	}

	public static bool VerifyZip(string path, int bufferSize, out int entries, out long totalBytes, out string error)
	{
		entries = 0;
		totalBytes = 0L;
		error = "";
		try
		{
			using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize))
			{
				using ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read, leaveOpen: false, Encoding.UTF8);
				byte[] buf = new byte[bufferSize];
				foreach (ZipArchiveEntry entry in zip.Entries)
				{
					if (entry.FullName.EndsWith("/"))
					{
						continue;
					}
					entries++;
					try
					{
						using Stream s = entry.Open();
						long n = 0L;
						while (true)
						{
							int read = s.Read(buf, 0, buf.Length);
							if (read <= 0)
							{
								break;
							}
							n += read;
						}
						if (entry.Length > 0 && n != entry.Length)
						{
							error = $"«{entry.FullName}»: распаковано {n} из {entry.Length} байт";
							return false;
						}
						totalBytes += n;
					}
					catch (Exception ex)
					{
						error = "«" + entry.FullName + "»: " + ex.Message;
						return false;
					}
				}
			}
			return true;
		}
		catch (Exception ex2)
		{
			error = ex2.Message;
			return false;
		}
	}
}
