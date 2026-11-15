using System;
using System.Collections.Generic;
using System.IO;

namespace BackupChecker.Core;

public class FileAgeCollector : ICollector
{
	public string Type => "file-age";

	public List<CollectResult> Collect(Company company, Source source, AgentCtx ctx)
	{
		List<CollectResult> list = new List<CollectResult>();
		CollectResult r = new CollectResult();
		r.CompanyId = company.Id;
		r.CompanyName = company.Name;
		r.SourceId = source.Id;
		r.Kind = "file";
		r.Job = source.GetStr("jobName", source.Id);
		List<string> patterns = new List<string>();
		foreach (object o in source.GetArray("paths"))
		{
			patterns.Add(Json.Str(o));
		}
		if (patterns.Count == 0)
		{
			patterns.Add(source.GetStr("path", ""));
		}
		int maxAgeHours = source.GetInt("maxAgeHours", company.Policy.FreshnessHours);
		DateTime? newest = null;
		long size = 0L;
		string newestPath = "";
		int found = 0;
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
					mask = "*";
				}
				if (!Directory.Exists(dir))
				{
					r.Fail("каталог недоступен: " + dir);
					continue;
				}
				string[] files = Directory.GetFiles(dir, mask);
				string[] array = files;
				foreach (string f in array)
				{
					found++;
					FileInfo fi = new FileInfo(f);
					if (!newest.HasValue || fi.LastWriteTimeUtc > newest.Value)
					{
						newest = fi.LastWriteTimeUtc;
						size = fi.Length;
						newestPath = f;
					}
				}
			}
			catch (Exception ex)
			{
				r.Fail("ошибка доступа к " + pattern + ": " + ex.Message);
			}
		}
		if (found == 0)
		{
			r.Reachable = true;
			r.LastSuccess = null;
			r.Message = "файлы не найдены: " + string.Join(", ", patterns.ToArray());
			list.Add(r);
			return list;
		}
		r.LastSuccess = newest;
		r.LastAttempt = newest;
		r.SizeBytes = size;
		r.Target = newestPath;
		double ageH = (DateTime.UtcNow - newest.Value).TotalHours;
		if (ageH > (double)maxAgeHours)
		{
			r.Fail($"последний файл {ageH:0.#} ч назад (порог {maxAgeHours} ч)");
		}
		list.Add(r);
		return list;
	}
}
