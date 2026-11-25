using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using BackupDotNet.Core;
namespace BackupDotNet.Setup;

/// <summary>
/// Минимальный HTTP-сервер для страницы мастера.
///
/// Почему не <see cref="HttpListener"/>: он требует резервирования URL
/// (netsh http add urlacl) для любого префикса, кроме отдельных случаев, и
/// падает с «доступ запрещён» на машинах с урезанными правами. Установщик
/// должен работать всегда, поэтому здесь свой сервер на TcpListener —
/// несколько десятков строк и никаких внешних условий.
///
/// Сервер слушает только 127.0.0.1: снаружи страница мастера недоступна.
/// </summary>
public sealed class LocalServer : IDisposable
{
	private readonly string _rootDir;

	private readonly Dictionary<string, Func<string, string>> _api;

	private TcpListener _listener;

	private Thread _thread;

	private volatile bool _stopping;

	/// <summary>Порт, на котором поднялся сервер.</summary>
	public int Port { get; private set; }

	/// <summary>Адрес страницы мастера.</summary>
	public string Url => "http://127.0.0.1:" + Port + "/";

	/// <param name="rootDir">Каталог со статикой мастера (setup-ui).</param>
	/// <param name="api">
	/// Обработчики API: путь → функция, получающая тело запроса и возвращающая
	/// текст ответа. Ответ считается JSON, если начинается с '{' или '['.
	/// </param>
	public LocalServer(string rootDir, Dictionary<string, Func<string, string>> api)
	{
		_rootDir = rootDir;
		_api = api ?? new Dictionary<string, Func<string, string>>(StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>Поднимает сервер на свободном порту.</summary>
	public bool Start()
	{
		try
		{
			// Порт 0 — система сама выберет свободный: конфликтов не будет.
			_listener = new TcpListener(IPAddress.Loopback, 0);
			_listener.Start();
			Port = ((IPEndPoint)_listener.LocalEndpoint).Port;

			_thread = new Thread(Loop);
			_thread.IsBackground = true;
			_thread.Name = "setup-http";
			_thread.Start();
			return true;
		}
		catch (Exception ex)
		{
			Log.Warn("Сервер мастера: " + ex.Message);
			return false;
		}
	}

	private void Loop()
	{
		while (!_stopping)
		{
			try
			{
				TcpClient client = _listener.AcceptTcpClient();
				// Каждый запрос — в своём потоке: установка долгая, а страница
				// в это время опрашивает прогресс.
				ThreadPool.QueueUserWorkItem(delegate { Handle(client); });
			}
			catch (SocketException)
			{
				if (_stopping)
				{
					return;
				}
			}
			catch (ObjectDisposedException)
			{
				return;
			}
			catch (Exception ex)
			{
				Log.Warn("Мастер, приём соединения: " + ex.Message);
			}
		}
	}

	private void Handle(TcpClient client)
	{
		try
		{
			using (client)
			{
				client.ReceiveTimeout = 15000;
				client.SendTimeout = 15000;

				using NetworkStream stream = client.GetStream();

				string requestLine = ReadLine(stream);
				if (string.IsNullOrEmpty(requestLine))
				{
					return;
				}

				string[] parts = requestLine.Split(' ');
				if (parts.Length < 2)
				{
					return;
				}
				string method = parts[0].ToUpperInvariant();
				string path = parts[1];
				int q = path.IndexOf('?');
				if (q >= 0)
				{
					path = path.Substring(0, q);
				}
				path = Uri.UnescapeDataString(path);

				// Заголовки: нужны Content-Length и Content-Type.
				int contentLength = 0;
				string header;
				while (!string.IsNullOrEmpty(header = ReadLine(stream)))
				{
					int colon = header.IndexOf(':');
					if (colon <= 0)
					{
						continue;
					}
					string name = header.Substring(0, colon).Trim();
					string value = header.Substring(colon + 1).Trim();
					if (string.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase) &&
						int.TryParse(value, out int len))
					{
						contentLength = len;
					}
				}

				string body = "";
				if (contentLength > 0)
				{
					byte[] buffer = new byte[contentLength];
					int read = 0;
					while (read < contentLength)
					{
						int n = stream.Read(buffer, read, contentLength - read);
						if (n <= 0)
						{
							break;
						}
						read += n;
					}
					body = Encoding.UTF8.GetString(buffer, 0, read);
				}

				// API.
				if (_api.TryGetValue(path, out Func<string, string> handler))
				{
					string result;
					try
					{
						result = handler(body) ?? "";
					}
					catch (Exception ex)
					{
						Log.Warn("Мастер, " + path + ": " + ex.Message);
						result = "{\"ok\":false,\"error\":" + Json.Str(ex.Message) + "}";
					}
					bool isJson = result.TrimStart().StartsWith("{") || result.TrimStart().StartsWith("[");
					Write(stream, 200, isJson ? "application/json; charset=utf-8" : "text/plain; charset=utf-8", result);
					return;
				}

				// Статика.
				if (method != "GET")
				{
					Write(stream, 405, "text/plain; charset=utf-8", "метод не поддерживается");
					return;
				}
				ServeStatic(stream, path);
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Мастер, запрос: " + ex.Message);
		}
	}

	private void ServeStatic(NetworkStream stream, string path)
	{
		if (path == "/" || path.Length == 0)
		{
			path = "/index.html";
		}

		// Защита от выхода за каталог: только имя файла и подкаталоги внутри root.
		string relative = path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
		string full = Path.GetFullPath(Path.Combine(_rootDir, relative));
		string rootFull = Path.GetFullPath(_rootDir);

		if (!full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
		{
			Write(stream, 403, "text/plain; charset=utf-8", "доступ запрещён");
			return;
		}

		if (!File.Exists(full))
		{
			Write(stream, 404, "text/plain; charset=utf-8", "не найдено: " + path);
			return;
		}

		byte[] data;
		try
		{
			data = File.ReadAllBytes(full);
		}
		catch (Exception ex)
		{
			Write(stream, 500, "text/plain; charset=utf-8", "ошибка чтения: " + ex.Message);
			return;
		}

		WriteBytes(stream, 200, ContentTypeOf(full), data);
	}

	private static string ContentTypeOf(string path)
	{
		switch (Path.GetExtension(path).ToLowerInvariant())
		{
		case ".html":
			return "text/html; charset=utf-8";
		case ".css":
			return "text/css; charset=utf-8";
		case ".js":
			return "application/javascript; charset=utf-8";
		case ".json":
			return "application/json; charset=utf-8";
		case ".png":
			return "image/png";
		case ".ico":
			return "image/x-icon";
		case ".svg":
			return "image/svg+xml";
		case ".woff2":
			return "font/woff2";
		case ".woff":
			return "font/woff";
		case ".ttf":
			return "font/ttf";
		default:
			return "application/octet-stream";
		}
	}

	private static void Write(NetworkStream stream, int status, string contentType, string text)
	{
		WriteBytes(stream, status, contentType, Encoding.UTF8.GetBytes(text ?? ""));
	}

	private static void WriteBytes(NetworkStream stream, int status, string contentType, byte[] data)
	{
		StringBuilder head = new StringBuilder();
		head.Append("HTTP/1.1 ").Append(status).Append(' ').Append(ReasonOf(status)).Append("\r\n");
		head.Append("Content-Type: ").Append(contentType).Append("\r\n");
		head.Append("Content-Length: ").Append(data.Length).Append("\r\n");
		head.Append("Cache-Control: no-store\r\n");
		// Мастер — локальное приложение: встраивать его во фреймы незачем.
		head.Append("X-Frame-Options: DENY\r\n");
		head.Append("Connection: close\r\n\r\n");

		byte[] headBytes = Encoding.ASCII.GetBytes(head.ToString());
		stream.Write(headBytes, 0, headBytes.Length);
		if (data.Length > 0)
		{
			stream.Write(data, 0, data.Length);
		}
		stream.Flush();
	}

	private static string ReasonOf(int status)
	{
		switch (status)
		{
		case 200: return "OK";
		case 403: return "Forbidden";
		case 404: return "Not Found";
		case 405: return "Method Not Allowed";
		case 500: return "Internal Server Error";
		default: return "OK";
		}
	}

	/// <summary>Читает строку до CRLF. Пустая строка — конец заголовков.</summary>
	private static string ReadLine(NetworkStream stream)
	{
		List<byte> bytes = new List<byte>(128);
		while (true)
		{
			int b = stream.ReadByte();
			if (b < 0)
			{
				return bytes.Count > 0 ? Encoding.ASCII.GetString(bytes.ToArray()) : "";
			}
			if (b == '\n')
			{
				int count = bytes.Count;
				if (count > 0 && bytes[count - 1] == '\r')
				{
					count--;
				}
				return Encoding.ASCII.GetString(bytes.ToArray(), 0, count);
			}
			bytes.Add((byte)b);
			if (bytes.Count > 16384)
			{
				// Заголовок такой длины — это не наш клиент.
				return "";
			}
		}
	}

	public void Dispose()
	{
		_stopping = true;
		try
		{
			_listener?.Stop();
		}
		catch
		{
		}
		_listener = null;
	}
}
