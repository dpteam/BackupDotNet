using System;
using System.Collections.Generic;
using System.IO;

namespace BackupChecker.Core;

public class VeeamFilesCollector : ICollector
{
	public string Type => "veeam-files";

	public List<CollectResult> Collect(Company company, Source source, AgentCtx ctx)
	{
		List<CollectResult> list = new List<CollectResult>();
		CollectResult r = new CollectResult();
		r.CompanyId = company.Id;
		r.CompanyName = company.Name;
		r.SourceId = source.Id;
		r.Kind = "veeam";
		r.Job = source.GetStr("jobName", source.Id);
		List<string> patterns = new List<string>();
		foreach (object o in source.GetArray("paths"))
		{
			patterns.Add(Json.Str(o));
		}
		int maxAgeHours = source.GetInt("maxAgeHours", company.Policy.FreshnessHours);
		FileInfo newest = null;
		int count = 0;
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
					mask = "*.vbk";
				}
				if (!Directory.Exists(dir))
				{
					r.Fail("каталог недоступен: " + dir);
					continue;
				}
				string[] files = Directory.GetFiles(dir, mask);
				foreach (string f in files)
				{
					count++;
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
			r.Message = "файлы Veeam не найдены (" + count + ")";
			list.Add(r);
			return list;
		}
		r.LastSuccess = newest.LastWriteTimeUtc;
		r.LastAttempt = newest.LastWriteTimeUtc;
		r.SizeBytes = newest.Length;
		r.Target = newest.FullName;
		double ageH = (DateTime.UtcNow - newest.LastWriteTimeUtc).TotalHours;
		if (ageH > (double)maxAgeHours)
		{
			r.Fail($"последний файл {ageH:0.#} ч назад (порог {maxAgeHours} ч)");
		}
		list.Add(r);
		return list;
	}
}
