using System;
using System.IO;
using BackupDotNet.Core;

namespace BackupDotNet.Setup;

/// <summary>
/// Брендинг установщика — из <c>config\branding.json</c> рядом с exe.
///
/// Зачем: установщик раздаётся как есть, и в нём не должно быть имени
/// конкретной инсталляции. Раньше <c>DPT.Backup</c> было захардкожено
/// в двух десятках мест, и любой релиз уезжал с чужим брендом. Теперь
/// все имена и пути берутся отсюда: положили другой branding.json —
/// получили другой продукт, без пересборки.
///
/// Если файла нет, работает <see cref="Branding.Default"/>: BackupDotNet,
/// <c>C:\BackupDotNet</c>, служба <c>BackupDotNetService</c>.
/// </summary>
public static class SetupBrand
{
	private static readonly object Sync = new object();

	private static Branding _brand;

	/// <summary>Каталог конфигурации рядом с exe.</summary>
	public static string ConfigDir
	{
		get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config"); }
	}

	/// <summary>Текущий брендинг. Никогда не возвращает null и не бросает.</summary>
	public static Branding Current
	{
		get
		{
			lock (Sync)
			{
				if (_brand == null)
				{
					try
					{
						_brand = Branding.For(ConfigDir);
					}
					catch
					{
						_brand = Branding.Default();
					}
					if (string.IsNullOrWhiteSpace(_brand.ProductName))
					{
						_brand.ProductName = "BackupDotNet";
					}
				}
				return _brand;
			}
		}
	}

	/// <summary>Имя продукта: «DPT.Backup», «BackupDotNet» — что записано в брендинге.</summary>
	public static string ProductName
	{
		get
		{
			string name = Current.ProductName;
			return string.IsNullOrWhiteSpace(name) ? "BackupDotNet" : name.Trim();
		}
	}

	/// <summary>Каталог программы по умолчанию, без хвостовой косой.</summary>
	public static string InstallDir
	{
		get
		{
			string dir = Current.InstallDir;
			if (string.IsNullOrWhiteSpace(dir))
			{
				dir = @"C:\" + ProductName;
			}
			return dir.TrimEnd('\\', '/');
		}
	}

	/// <summary>Каталог данных по умолчанию, без хвостовой косой.</summary>
	public static string DataDir
	{
		get
		{
			string dir = Current.DataDir;
			if (string.IsNullOrWhiteSpace(dir))
			{
				dir = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), ProductName);
			}
			return dir.TrimEnd('\\', '/');
		}
	}

	/// <summary>Имя службы агента.</summary>
	public static string ServiceName
	{
		get
		{
			string name = Current.ServiceName;
			if (string.IsNullOrWhiteSpace(name))
			{
				name = ProductName.Replace(".", "").Replace(" ", "") + "Service";
			}
			return name.Trim();
		}
	}

	/// <summary>Имя службы удалённого агента.</summary>
	public static string ServiceNameRemote
	{
		get
		{
			string name = Current.ServiceNameRemote;
			if (string.IsNullOrWhiteSpace(name))
			{
				name = ProductName.Replace(".", "").Replace(" ", "") + "Remote";
			}
			return name.Trim();
		}
	}

	/// <summary>Имя правила firewall: по продукту, чтобы правила разных продуктов не сливались.</summary>
	public static string FirewallRule
	{
		get { return ProductName; }
	}

	/// <summary>
	/// Обновляет <see cref="Branding.Current"/> — от него зависят имена
	/// в журналах и в веб-интерфейсе установщика.
	/// </summary>
	public static void Apply()
	{
		try
		{
			Branding.SetCurrent(Current);
		}
		catch
		{
		}
	}
}
