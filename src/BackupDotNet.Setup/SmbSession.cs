using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace BackupDotNet.Setup;

/// <summary>
/// Доступ к админ-шаре чужой машины: <c>\\host\ADMIN$</c> и <c>\\host\C$</c>.
///
/// Зачем отдельный транспорт: в домене часто бывает, что WinRM выключен
/// групповой политикой, а админ-шары живы. Копирование через ADMIN$ и запуск
/// службы через <c>sc.exe \\host</c> работают там, где PowerShell Remoting
/// запрещён.
///
/// Учётные данные передаются в WNetAddConnection2 и живут только на время
/// соединения: в реестр и в профиль пользователя они не попадают.
/// </summary>
public sealed class SmbSession : IDisposable
{
	private readonly List<string> _connected = new List<string>();

	private bool _disposed;

	/// <summary>
	/// Подключается к шаре с явными учётными данными.
	/// </summary>
	/// <param name="uncPath">Например <c>\\server\ADMIN$</c>.</param>
	/// <param name="user">Пусто — текущий пользователь.</param>
	/// <param name="password">Пароль.</param>
	public static SmbSession Open(string uncPath, string user, string password, out string error)
	{
		error = "";
		SmbSession session = new SmbSession();
		if (!session.Connect(uncPath, user, password, out error))
		{
			session.Dispose();
			return null;
		}
		return session;
	}

	/// <summary>Подключает ещё одну шару в этом же сеансе.</summary>
	public bool Connect(string uncPath, string user, string password, out string error)
	{
		error = "";
		if (string.IsNullOrWhiteSpace(uncPath))
		{
			error = "не указан путь";
			return false;
		}

		// Без учётных данных работаем под текущим пользователем: если доступ
		// уже есть (домен, сохранённое соединение), WNetAddConnection2 не нужен
		// и только мешает — с пустым именем он возвращает «доступ запрещён».
		if (string.IsNullOrEmpty(user))
		{
			try
			{
				if (Directory.Exists(uncPath))
				{
					return true;
				}
				error = "нет доступа к " + uncPath + " под текущей учётной записью";
				return false;
			}
			catch (Exception ex)
			{
				error = ex.Message;
				return false;
			}
		}

		NETRESOURCE resource = new NETRESOURCE
		{
			dwType = 1, // RESOURCETYPE_DISK
			lpLocalName = null,
			lpRemoteName = uncPath,
			lpProvider = null
		};

		// 0 — соединение запоминается до перезагрузки; для установщика
		// важнее не оставлять следов, поэтому CONNECT_TEMPORARY.
		uint flags = 0x00000004; // CONNECT_TEMPORARY

		int code = WNetAddConnection2(ref resource, password ?? "", user, flags);
		if (code == 0 || code == 1219)
		{
			// 1219 — уже есть соединение с этим сервером под другими кредами.
			// Это не ошибка: работать будем под тем, что есть.
			_connected.Add(uncPath);
			if (code == 1219)
			{
				error = "";
			}
			return true;
		}

		error = DescribeNetError(code);
		return false;
	}

	/// <summary>Отключает все шары, открытые этим сеансом.</summary>
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		foreach (string path in _connected)
		{
			try
			{
				WNetCancelConnection2(path, 0, true);
			}
			catch
			{
			}
		}
		_connected.Clear();
	}

	/// <summary>Переводит код ошибки Win32 в понятный текст.</summary>
	public static string DescribeNetError(int code)
	{
		switch (code)
		{
		case 5:
			return "доступ запрещён: нужны права администратора на целевой машине";
		case 53:
			return "компьютер не найден в сети";
		case 67:
			return "сетевая папка не найдена";
		case 86:
			return "неверный пароль";
		case 1326:
			return "неверное имя пользователя или пароль";
		case 1219:
			return "к этому серверу уже есть подключение под другими учётными данными";
		case 1231:
			return "сеть недоступна";
		case 1272:
			return "администратор заблокировал доступ к админ-шарам политикой";
		case 1909:
			return "учётная запись заблокирована";
		case 2202:
			return "указано неверное имя пользователя";
		default:
			return "ошибка Win32 " + code;
		}
	}

	/// <summary>
	/// Копирует каталог на удалённую машину.
	/// Возвращает число скопированных файлов или -1 с текстом ошибки.
	/// </summary>
	public static int CopyTree(string localDir, string remoteDir, out string error, out long bytes)
	{
		error = "";
		bytes = 0;
		int count = 0;
		try
		{
			string localFull = Path.GetFullPath(localDir);
			Directory.CreateDirectory(remoteDir);

			foreach (string file in Directory.GetFiles(localFull, "*", SearchOption.AllDirectories))
			{
				string relative = file.Substring(localFull.Length).TrimStart('\\');
				string dest = Path.Combine(remoteDir, relative);
				string dir = Path.GetDirectoryName(dest);
				if (!string.IsNullOrEmpty(dir))
				{
					Directory.CreateDirectory(dir);
				}
				File.Copy(file, dest, overwrite: true);
				count++;
				try
				{
					bytes += new FileInfo(dest).Length;
				}
				catch
				{
				}
			}
			return count;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return -1;
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct NETRESOURCE
	{
		public int dwScope;

		public int dwType;

		public int dwDisplayType;

		public int dwUsage;

		[MarshalAs(UnmanagedType.LPWStr)]
		public string lpLocalName;

		[MarshalAs(UnmanagedType.LPWStr)]
		public string lpRemoteName;

		[MarshalAs(UnmanagedType.LPWStr)]
		public string lpProvider;
	}

	[DllImport("mpr.dll", CharSet = CharSet.Unicode)]
	private static extern int WNetAddConnection2(ref NETRESOURCE netResource, string password,
		string username, uint flags);

	[DllImport("mpr.dll", CharSet = CharSet.Unicode)]
	private static extern int WNetCancelConnection2(string name, uint flags, bool force);
}
