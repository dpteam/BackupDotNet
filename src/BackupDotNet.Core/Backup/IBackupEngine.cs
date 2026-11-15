using System;
using System.Collections.Generic;
using System.Threading;

namespace BackupDotNet.Core.Backup;

/// <summary>
/// Движок резервного копирования. Движки регистрируются в <see cref="BackupEngines"/>
/// так же, как коллекторы в <see cref="Collectors"/>: новый движок не требует
/// правок в очереди, интерфейсе или хранилище.
/// </summary>
public interface IBackupEngine
{
	/// <summary>Машинное имя: <c>hyperv-export</c>.</summary>
	string Name { get; }

	/// <summary>Название для интерфейса.</summary>
	string Title { get; }

	/// <summary>Пояснение: что делает и чего не делает.</summary>
	string Description { get; }

	/// <summary>Доступен ли движок здесь и сейчас.</summary>
	bool IsAvailable(out string reason);

	/// <summary>
	/// Выполняет задание. Прогресс сообщается через <paramref name="report"/>
	/// (процент, текст), отмена — через <paramref name="cancel"/>.
	/// При ошибке бросает <see cref="BackupException"/>.
	/// </summary>
	void Run(BackupJob job, Action<int, string> report, CancellationToken cancel);
}

/// <summary>Реестр движков бэкапа.</summary>
public static class BackupEngines
{
	private static readonly Dictionary<string, IBackupEngine> Registry;

	static BackupEngines()
	{
		Registry = new Dictionary<string, IBackupEngine>(StringComparer.OrdinalIgnoreCase);
		Register(new HyperVExportEngine());
	}

	public static IEnumerable<IBackupEngine> All => Registry.Values;

	public static void Register(IBackupEngine engine)
	{
		if (engine != null && !string.IsNullOrEmpty(engine.Name))
		{
			Registry[engine.Name] = engine;
		}
	}

	public static IBackupEngine Get(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		Registry.TryGetValue(name, out var e);
		return e;
	}
}
