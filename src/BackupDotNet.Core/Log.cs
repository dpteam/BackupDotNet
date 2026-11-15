using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace BackupDotNet.Core;

/// <summary>
/// Единая точка логирования.
///
/// Требования, которые закрывает этот класс:
///  • работает через <see cref="System.Diagnostics.Trace"/> и не требует App.Config —
///    все листенеры подключаются программно;
///  • пишет одновременно в консоль (если процесс интерактивный) и в файл
///    <c>&lt;каталог&gt;\&lt;Продукт&gt;_&lt;дата&gt;_&lt;время&gt;.log</c>;
///  • идемпотентен: повторный <see cref="Init"/> не плодит дубликаты листенеров
///    (классический баг, когда каждый вызов Init добавляет ещё один
///    TextWriterTraceListener и каждая строка пишется N раз);
///  • переживает ротацию файла по размеру и чистит старые логи;
///  • никогда не бросает исключений наружу — логирование не должно ронять агент.
/// </summary>
public static class Log
{
	/// <summary>Метка листенеров агента: по ней они снимаются при повторной инициализации.</summary>
	private const string ListenerTag = "BackupDotNet";

	private static readonly object Sync = new object();

	private static TraceSource _source;

	private static ConsoleLogListener _console;

	private static FileTraceListener _file;

	private static string _dir;

	private static string _product = "BackupDotNet";

	private static LogLevel _min = LogLevel.Info;

	private static long _maxBytes = 32L * 1024 * 1024;

	private static bool _echo = true;

	private static bool _initialized;

	/// <summary>Полный путь к текущему файлу журнала (пустая строка, если файл не ведётся).</summary>
	public static string CurrentFile
	{
		get
		{
			lock (Sync)
			{
				return _file?.Path ?? "";
			}
		}
	}

	/// <summary>Каталог журналов.</summary>
	public static string Dir
	{
		get
		{
			lock (Sync)
			{
				return _dir ?? "";
			}
		}
	}

	/// <summary>Текущий порог: сообщения ниже него отбрасываются.</summary>
	public static LogLevel Level
	{
		get
		{
			lock (Sync)
			{
				return _min;
			}
		}
	}

	/// <summary>
	/// Инициализирует журналирование. Безопасно вызывать многократно:
	/// повторный вызов переиспользует уже подключённые листенеры и лишь
	/// обновляет каталог, порог, лимит размера и режим эха.
	/// </summary>
	/// <param name="dir">Каталог журналов. Пустая строка — писать только в консоль.</param>
	/// <param name="level">debug | info | warn | error.</param>
	/// <param name="maxMb">Предел размера файла в мегабайтах до ротации.</param>
	/// <param name="echo">Дублировать вывод в консоль.</param>
	public static void Init(string dir, string level, int maxMb, bool echo)
	{
		lock (Sync)
		{
			_min = ParseLevel(level);
			_maxBytes = Math.Max(1, maxMb) * 1024L * 1024L;
			_echo = echo;
			_product = SafeProductName();

			bool dirChanged = !string.Equals(_dir, dir, StringComparison.OrdinalIgnoreCase);

			if (dirChanged)
			{
				_dir = dir;
				if (!string.IsNullOrEmpty(_dir))
				{
					try
					{
						Directory.CreateDirectory(_dir);
					}
					catch
					{
					}
				}
			}

			EnsureSource();

			// --- консоль -------------------------------------------------------
			if (_echo && Environment.UserInteractive)
			{
				if (_console == null)
				{
					try
					{
						_console = new ConsoleLogListener
						{
							Name = ListenerTag + ":console"
						};
						_source.Listeners.Add(_console);
					}
					catch
					{
						_console = null;
					}
				}
			}
			else if (_console != null)
			{
				_source.Listeners.Remove(_console);
				try
				{
					_console.Dispose();
				}
				catch
				{
				}
				_console = null;
			}

			// --- файл ----------------------------------------------------------
			if (string.IsNullOrEmpty(_dir))
			{
				DetachFile();
			}
			else if (_file == null || dirChanged)
			{
				DetachFile();
				try
				{
					_file = new FileTraceListener(_dir, _product, _maxBytes);
					_file.Name = ListenerTag + ":file";
					_file.TraceOutputOptions = TraceOptions.None;
					_source.Listeners.Add(_file);
				}
				catch
				{
					_file = null;
				}
			}
			else
			{
				_file.MaxBytes = _maxBytes;
			}

			_source.Switch.Level = ToTraceLevel(_min);
			_initialized = true;
		}
	}

	/// <summary>Снимает листенеры и закрывает файл. Вызывать при штатном завершении.</summary>
	public static void Shutdown()
	{
		lock (Sync)
		{
			DetachFile();
			if (_console != null)
			{
				try
				{
					if (_source != null)
					{
						_source.Listeners.Remove(_console);
					}
					_console.Flush();
					_console.Dispose();
				}
				catch
				{
				}
				_console = null;
			}
			if (_source != null)
			{
				try
				{
					_source.Flush();
				}
				catch
				{
				}
			}
			_initialized = false;
		}
	}

	public static LogLevel ParseLevel(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return LogLevel.Info;
		}
		switch (s.Trim().ToLowerInvariant())
		{
		case "debug":
		case "verbose":
		case "trace":
			return LogLevel.Debug;
		case "info":
		case "information":
			return LogLevel.Info;
		case "warn":
		case "warning":
			return LogLevel.Warn;
		case "error":
		case "err":
		case "fatal":
			return LogLevel.Error;
		default:
			return LogLevel.Info;
		}
	}

	public static void Debug(string msg)
	{
		Write(LogLevel.Debug, msg, null);
	}

	public static void Info(string msg)
	{
		Write(LogLevel.Info, msg, null);
	}

	public static void Warn(string msg)
	{
		Write(LogLevel.Warn, msg, null);
	}

	public static void Error(string msg)
	{
		Write(LogLevel.Error, msg, null);
	}

	public static void Error(string msg, Exception ex)
	{
		Write(LogLevel.Error, msg, ex);
	}

	private static void Write(LogLevel lvl, string msg, Exception ex)
	{
		try
		{
			lock (Sync)
			{
				if (lvl < _min)
				{
					return;
				}
				EnsureSource();

				string line = string.Format(
					CultureInfo.InvariantCulture,
					"{0:yyyy-MM-dd HH:mm:ss.fff} [{1,-5}] {2}{3}",
					DateTime.Now,
					lvl.ToString().ToUpperInvariant(),
					msg,
					(ex == null) ? "" : (" | " + ex.GetType().Name + ": " + ex.Message));

				_source.TraceEvent(ToTraceEventType(lvl), 0, line);

				// Стек — только для ошибок и только на уровне debug:
				// иначе файл журнала раздувается до неприличия.
				if (ex != null && lvl >= LogLevel.Error && _min == LogLevel.Debug)
				{
					string stack = ex.ToString();
					foreach (string raw in stack.Split('\n'))
					{
						string s = raw.TrimEnd('\r');
						if (s.Length > 0)
						{
							_source.TraceEvent(TraceEventType.Verbose, 0, "    " + s);
						}
					}
				}
			}
		}
		catch
		{
			// Логирование не имеет права ломать работу агента.
		}
	}

	/// <summary>Создаёт TraceSource с пустым набором листенеров (без App.Config).</summary>
	private static void EnsureSource()
	{
		if (_source != null)
		{
			return;
		}
		_source = new TraceSource(ListenerTag, SourceLevels.All);
		_source.Switch = new SourceSwitch(ListenerTag, "All");
		// Ключевая строка: убираем листенеры по умолчанию (DefaultTraceListener),
		// иначе на каждый Debug.WriteLine прилетает окно Assert/OutputDebugString.
		_source.Listeners.Clear();
		Trace.AutoFlush = true;
	}

	private static void DetachFile()
	{
		if (_file == null)
		{
			return;
		}
		try
		{
			_source?.Listeners.Remove(_file);
		}
		catch
		{
		}
		try
		{
			_file.Flush();
			_file.Close();
		}
		catch
		{
		}
		_file = null;
	}

	private static string SafeProductName()
	{
		string name = "";
		try
		{
			name = Branding.Current?.ProductName ?? "";
		}
		catch
		{
		}
		if (string.IsNullOrWhiteSpace(name))
		{
			name = "BackupDotNet";
		}
		StringBuilder sb = new StringBuilder(name.Length);
		foreach (char ch in name.Trim())
		{
			sb.Append(Array.IndexOf(Path.GetInvalidFileNameChars(), ch) >= 0 || ch == ' ' ? '_' : ch);
		}
		return sb.Length > 0 ? sb.ToString() : "BackupDotNet";
	}

	private static SourceLevels ToTraceLevel(LogLevel lvl)
	{
		switch (lvl)
		{
		case LogLevel.Debug:
			return SourceLevels.All;
		case LogLevel.Info:
			return SourceLevels.Information;
		case LogLevel.Warn:
			return SourceLevels.Warning;
		default:
			return SourceLevels.Error;
		}
	}

	private static TraceEventType ToTraceEventType(LogLevel lvl)
	{
		switch (lvl)
		{
		case LogLevel.Debug:
			return TraceEventType.Verbose;
		case LogLevel.Info:
			return TraceEventType.Information;
		case LogLevel.Warn:
			return TraceEventType.Warning;
		default:
			return TraceEventType.Error;
		}
	}

	/// <summary>
	/// Удаляет журналы старше <paramref name="keepDays"/> дней.
	/// Имена файлов: <c>&lt;Продукт&gt;_*.log</c>.
	/// </summary>
	public static void Cleanup(string dir, int keepDays)
	{
		if (string.IsNullOrEmpty(dir) || keepDays <= 0 || !Directory.Exists(dir))
		{
			return;
		}
		try
		{
			DateTime cutoff = DateTime.Now.AddDays(-keepDays);
			string[] files = Directory.GetFiles(dir, "*.log");
			foreach (string f in files)
			{
				try
				{
					string name = Path.GetFileName(f);
					if (string.Equals(name, Path.GetFileName(CurrentFile), StringComparison.OrdinalIgnoreCase))
					{
						continue; // не трогаем активный файл
					}
					if (File.GetLastWriteTime(f) < cutoff)
					{
						File.Delete(f);
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}

	/// <summary>Список журналов каталога, свежие сверху. Для веб-интерфейса.</summary>
	public static List<FileInfo> ListFiles(string dir)
	{
		List<FileInfo> list = new List<FileInfo>();
		if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
		{
			return list;
		}
		try
		{
			foreach (string f in Directory.GetFiles(dir, "*.log"))
			{
				try
				{
					list.Add(new FileInfo(f));
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		list.Sort((a, b) => b.LastWriteTimeUtc.CompareTo(a.LastWriteTimeUtc));
		return list;
	}

	/// <summary>
	/// Читает хвост файла журнала. <paramref name="maxBytes"/> ограничивает объём,
	/// чтобы веб-интерфейс не вытягивал гигабайты в память.
	/// </summary>
	public static string Tail(string path, int maxLines, int maxBytes)
	{
		if (string.IsNullOrEmpty(path) || !File.Exists(path))
		{
			return "";
		}
		try
		{
			using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			long from = Math.Max(0L, fs.Length - Math.Max(4096, maxBytes));
			fs.Seek(from, SeekOrigin.Begin);
			using StreamReader sr = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
			string text = sr.ReadToEnd();
			if (from > 0)
			{
				int nl = text.IndexOf('\n');
				if (nl >= 0)
				{
					text = text.Substring(nl + 1);
				}
			}
			if (maxLines <= 0)
			{
				return text;
			}
			string[] lines = text.Replace("\r\n", "\n").Split('\n');
			if (lines.Length <= maxLines)
			{
				return text;
			}
			return string.Join(Environment.NewLine, lines, lines.Length - maxLines, maxLines);
		}
		catch
		{
			return "";
		}
	}

	/// <summary>
	/// Базовый листенер без «шапки» Trace. Стандартный TextWriterTraceListener
	/// печатает «Источник EventType: id : » перед каждым сообщением и добавляет
	/// лишние переводы строки — нам нужен чистый текст.
	/// </summary>
	private abstract class PlainTraceListener : TraceListener
	{
		protected PlainTraceListener()
		{
			TraceOutputOptions = TraceOptions.None;
		}

		public sealed override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
		{
			WriteLine(message, FromEventType(eventType));
		}

		public sealed override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
		{
			string text = (args == null || args.Length == 0) ? format : string.Format(CultureInfo.InvariantCulture, format, args);
			WriteLine(text, FromEventType(eventType));
		}

		public sealed override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
		{
			WriteLine(data?.ToString() ?? "", FromEventType(eventType));
		}

		public sealed override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
		{
			if (data == null)
			{
				return;
			}
			foreach (object o in data)
			{
				WriteLine(o?.ToString() ?? "", FromEventType(eventType));
			}
		}

		private static LogLevel FromEventType(TraceEventType type)
		{
			switch (type)
			{
			case TraceEventType.Verbose:
			case TraceEventType.Start:
			case TraceEventType.Stop:
			case TraceEventType.Suspend:
			case TraceEventType.Resume:
			case TraceEventType.Transfer:
				return LogLevel.Debug;
			case TraceEventType.Information:
				return LogLevel.Info;
			case TraceEventType.Warning:
				return LogLevel.Warn;
			default:
				return LogLevel.Error;
			}
		}

		public override void Write(string message)
		{
			if (!string.IsNullOrEmpty(message))
			{
				WriteLine(message);
			}
		}

		public override void WriteLine(string message)
		{
			WriteLine(message, LogLevel.Info);
		}

		public abstract void WriteLine(string message, LogLevel level);
	}

	/// <summary>Вывод в консоль: цвет по уровню, ошибки — в stderr.</summary>
	private sealed class ConsoleLogListener : PlainTraceListener
	{
		private static readonly object ConsoleLock = new object();

		public override void WriteLine(string message, LogLevel level)
		{
			if (string.IsNullOrEmpty(message))
			{
				return;
			}
			lock (ConsoleLock)
			{
				try
				{
					ConsoleColor prev = Console.ForegroundColor;
					if (level == LogLevel.Warn)
					{
						Console.ForegroundColor = ConsoleColor.Yellow;
					}
					else if (level >= LogLevel.Error)
					{
						Console.ForegroundColor = ConsoleColor.Red;
					}
					else if (level == LogLevel.Debug)
					{
						Console.ForegroundColor = ConsoleColor.DarkGray;
					}
					if (level >= LogLevel.Error)
					{
						Console.Error.WriteLine(message);
					}
					else
					{
						Console.Out.WriteLine(message);
					}
					Console.ForegroundColor = prev;
				}
				catch
				{
				}
			}
		}
	}

	/// <summary>
	/// Файловый листенер: один открытый поток, UTF-8 без BOM, построчная запись,
	/// ротация по размеру. Держит файл с FileShare.ReadWrite, чтобы журнал можно
	/// было читать (и открыть в блокноте) во время работы агента.
	/// </summary>
	private sealed class FileTraceListener : PlainTraceListener
	{
		private readonly object _lock = new object();

		private readonly string _dir;

		private readonly string _prefix;

		private FileStream _stream;

		private StreamWriter _writer;

		private long _written;

		public long MaxBytes { get; set; }

		public string Path { get; private set; }

		public FileTraceListener(string dir, string prefix, long maxBytes)
		{
			_dir = dir;
			_prefix = prefix;
			MaxBytes = maxBytes;
			OpenNew();
		}

		private void OpenNew()
		{
			Directory.CreateDirectory(_dir);
			string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
			string path = System.IO.Path.Combine(_dir, _prefix + "_" + stamp + ".log");
			int n = 1;
			while (File.Exists(path))
			{
				path = System.IO.Path.Combine(_dir, _prefix + "_" + stamp + "_" + n.ToString(CultureInfo.InvariantCulture) + ".log");
				n++;
			}
			_stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
			_writer = new StreamWriter(_stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
			{
				AutoFlush = true
			};
			Path = path;
			_written = 0L;
		}

		public override void Write(string message)
		{
			if (string.IsNullOrEmpty(message))
			{
				return;
			}
			WriteLine(message);
		}

		public override void WriteLine(string message, LogLevel level)
		{
			lock (_lock)
			{
				if (_writer == null)
				{
					return;
				}
				try
				{
					_writer.WriteLine(message);
					_written += Encoding.UTF8.GetByteCount(message) + 2;
					if (MaxBytes > 0 && _written >= MaxBytes)
					{
						Rotate();
					}
				}
				catch
				{
				}
			}
		}

		private void Rotate()
		{
			try
			{
				_writer.Flush();
				_writer.Dispose();
			}
			catch
			{
			}
			_writer = null;
			try
			{
				_stream?.Dispose();
			}
			catch
			{
			}
			_stream = null;
			try
			{
				OpenNew();
			}
			catch
			{
				_writer = null;
			}
		}

		public override void Flush()
		{
			lock (_lock)
			{
				try
				{
					_writer?.Flush();
				}
				catch
				{
				}
			}
		}

		public override void Close()
		{
			lock (_lock)
			{
				try
				{
					_writer?.Flush();
					_writer?.Dispose();
				}
				catch
				{
				}
				_writer = null;
				try
				{
					_stream?.Dispose();
				}
				catch
				{
				}
				_stream = null;
			}
			base.Close();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Close();
			}
			base.Dispose(disposing);
		}
	}
}
