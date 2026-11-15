using System.Collections.Generic;

namespace BackupChecker.Core;

public interface ICollector
{
	string Type { get; }

	List<CollectResult> Collect(Company company, Source source, AgentCtx ctx);
}
