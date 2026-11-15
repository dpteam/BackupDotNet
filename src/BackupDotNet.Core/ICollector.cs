using System.Collections.Generic;

namespace BackupDotNet.Core;

public interface ICollector
{
	string Type { get; }

	List<CollectResult> Collect(Company company, Source source, AgentCtx ctx);
}
