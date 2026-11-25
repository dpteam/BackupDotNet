using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using BackupDotNet.Core;
namespace BackupDotNet.Setup;

/// <summary>
/// Хостинг WebView2 в окне WinForms без SDK и без WebView2Loader.dll.
///
/// Порядок: загрузить EmbeddedBrowserWebView.dll из установленного рантайма,
/// взять из неё CreateWebViewEnvironmentWithOptionsInternal, создать окружение,
/// затем контроллер, привязанный к окну.
///
/// Все COM-объекты держим в полях: если сборщик мусора соберёт обработчик,
/// нативная сторона обратится к освобождённому объекту и приложение упадёт.
/// </summary>
public sealed class WebView2Host : IDisposable
{
	private readonly Form _form;

	private WebView2Interop.ICoreWebView2Environment _environment;

	private WebView2Interop.ICoreWebView2Controller _controller;

	private WebView2Interop.ICoreWebView2 _webView;

	private WebView2Interop.EnvironmentHandler _envHandler;

	private WebView2Interop.ControllerHandler _controllerHandler;

	private IntPtr _module;

	private bool _disposed;

	/// <summary>Версия браузера после успешного создания окружения.</summary>
	public string BrowserVersion { get; private set; } = "";

	/// <summary>Готов ли хост к навигации.</summary>
	public bool IsReady { get; private set; }

	/// <summary>Текст последней ошибки, если создание не удалось.</summary>
	public string Error { get; private set; } = "";

	public WebView2Host(Form form)
	{
		_form = form;
	}

	/// <summary>
	/// Создаёт окружение и контроллер. Возвращает false с текстом в
	/// <see cref="Error"/> — вызывающий код решает, что делать дальше.
	/// </summary>
	public bool Initialize()
	{
		WebView2Runtime.Found runtime = WebView2Runtime.Locate();
		if (runtime == null)
		{
			Error = "WebView2 Runtime не установлен";
			return false;
		}

		_module = NativeMethods.LoadLibrary(runtime.LibraryPath);
		if (_module == IntPtr.Zero)
		{
			Error = "не удалось загрузить рантайм WebView2 (код " + Marshal.GetLastWin32Error() + ")";
			return false;
		}

		IntPtr proc = NativeMethods.GetProcAddress(_module, "CreateWebViewEnvironmentWithOptionsInternal");
		if (proc == IntPtr.Zero)
		{
			// Microsoft предупреждает, что этот экспорт может исчезнуть.
			Error = "рантайм WebView2 " + runtime.Version + " не экспортирует точку создания окружения";
			return false;
		}

		WebView2Interop.CreateEnvironmentInternalDelegate create =
			(WebView2Interop.CreateEnvironmentInternalDelegate)Marshal.GetDelegateForFunctionPointer(
				proc, typeof(WebView2Interop.CreateEnvironmentInternalDelegate));

		// Данные профиля — во временном каталоге пользователя: установщику
		// не нужно ничего хранить между запусками.
		string userData = System.IO.Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"BackupDotNet", "SetupWebView");

		try
		{
			System.IO.Directory.CreateDirectory(userData);
		}
		catch
		{
		}

		_envHandler = new WebView2Interop.EnvironmentHandler();
		IntPtr envHandlerPtr = Marshal.GetComInterfaceForObject(
			_envHandler, typeof(WebView2Interop.ICreateEnvironmentCompletedHandler));

		int hr = create(true, WebView2Interop.RuntimeTypeEvergreen, userData, IntPtr.Zero, envHandlerPtr);
		if (!WebView2Interop.Succeeded(hr))
		{
			Error = "создание окружения WebView2: HRESULT 0x" + hr.ToString("X8");
			return false;
		}

		// Колбэк приходит асинхронно: прокручиваем очередь сообщений окна.
		if (!PumpUntil(() => _envHandler.Result != -1, 20000))
		{
			Error = "окружение WebView2 не ответило за 20 с";
			return false;
		}
		if (_envHandler.Result != 0 || _envHandler.Environment == null)
		{
			Error = "окружение WebView2 не создано: 0x" + _envHandler.Result.ToString("X8");
			return false;
		}

		_environment = _envHandler.Environment;

		try
		{
			_environment.get_BrowserVersionString(out string version);
			BrowserVersion = version ?? "";
		}
		catch
		{
		}

		_controllerHandler = new WebView2Interop.ControllerHandler();
		IntPtr controllerHandlerPtr = Marshal.GetComInterfaceForObject(
			_controllerHandler, typeof(WebView2Interop.ICreateControllerCompletedHandler));

		hr = _environment.CreateCoreWebView2Controller(_form.Handle, controllerHandlerPtr);
		if (!WebView2Interop.Succeeded(hr))
		{
			Error = "создание контроллера WebView2: HRESULT 0x" + hr.ToString("X8");
			return false;
		}

		if (!PumpUntil(() => _controllerHandler.Result != -1, 20000))
		{
			Error = "контроллер WebView2 не ответил за 20 с";
			return false;
		}
		if (_controllerHandler.Result != 0 || _controllerHandler.Controller == null)
		{
			Error = "контроллер WebView2 не создан: 0x" + _controllerHandler.Result.ToString("X8");
			return false;
		}

		_controller = _controllerHandler.Controller;

		try
		{
			_controller.put_IsVisible(true);
			_controller.get_CoreWebView2(out _webView);
		}
		catch (Exception ex)
		{
			Error = "WebView2: " + ex.Message;
			return false;
		}

		if (_webView == null)
		{
			Error = "WebView2 вернул пустой объект браузера";
			return false;
		}

		ApplyBounds();
		IsReady = true;
		return true;
	}

	/// <summary>Переходит по адресу.</summary>
	public bool Navigate(string url)
	{
		if (_webView == null)
		{
			return false;
		}
		try
		{
			int hr = _webView.Navigate(url);
			return WebView2Interop.Succeeded(hr);
		}
		catch (Exception ex)
		{
			Error = "навигация: " + ex.Message;
			return false;
		}
	}

	/// <summary>Показывает HTML из памяти (для страниц ошибок).</summary>
	public bool ShowHtml(string html)
	{
		if (_webView == null)
		{
			return false;
		}
		try
		{
			return WebView2Interop.Succeeded(_webView.NavigateToString(html ?? ""));
		}
		catch
		{
			return false;
		}
	}

	/// <summary>Подгоняет область под клиентскую часть окна.</summary>
	public void ApplyBounds()
	{
		if (_controller == null)
		{
			return;
		}
		try
		{
			WebView2Interop.RECT r = WebView2Interop.RECT.FromSize(
				_form.ClientSize.Width, _form.ClientSize.Height);
			_controller.put_Bounds(r);
		}
		catch
		{
		}
	}

	/// <summary>Сообщает WebView2, что окно сдвинулось (перетаскивание, ресайз).</summary>
	public void NotifyMoved()
	{
		if (_controller == null)
		{
			return;
		}
		try
		{
			_controller.NotifyParentWindowPositionChanged();
		}
		catch
		{
		}
	}

	/// <summary>
	/// Крутит очередь сообщений окна, пока условие не выполнится.
	/// Без этого асинхронные колбэки WebView2 не доставляются: они приходят
	/// через оконные сообщения, а не в отдельном потоке.
	/// </summary>
	private bool PumpUntil(Func<bool> condition, int timeoutMs)
	{
		DateTime deadline = DateTime.Now.AddMilliseconds(timeoutMs);
		while (DateTime.Now < deadline)
		{
			Application.DoEvents();
			if (condition())
			{
				return true;
			}
			System.Threading.Thread.Sleep(20);
		}
		return condition();
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;

		try
		{
			_controller?.Close();
		}
		catch
		{
		}

		_controller = null;
		_webView = null;
		_environment = null;
		_controllerHandler = null;
		_envHandler = null;

		if (_module != IntPtr.Zero)
		{
			try
			{
				NativeMethods.FreeLibrary(_module);
			}
			catch
			{
			}
			_module = IntPtr.Zero;
		}
	}

	private static class NativeMethods
	{
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern IntPtr LoadLibrary(string path);

		[DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern IntPtr GetProcAddress(IntPtr module, string name);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool FreeLibrary(IntPtr module);
	}
}
