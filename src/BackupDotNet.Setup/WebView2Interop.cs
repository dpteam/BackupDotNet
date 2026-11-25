using System;
using System.Runtime.InteropServices;

using BackupDotNet.Core;
namespace BackupDotNet.Setup;

/// <summary>
/// Объявления COM-интерфейсов WebView2.
///
/// Почему руками, а не пакетом: в проекте запрещены сторонние NuGet-пакеты,
/// а Microsoft.Web.WebView2 — это пакет. API у WebView2 целиком COM-based,
/// поэтому достаточно объявить нужные интерфейсы и загрузить
/// WebView2Loader.dll, который ставится вместе с Edge WebView2 Runtime.
///
/// Главная ловушка при ручном объявлении — <b>порядок методов</b>. Слот в
/// vtable считается по порядку объявления: пропустишь один метод — все
/// следующие вызовут чужую функцию и приложение упадёт с access violation.
/// Поэтому методы перечислены строго как в заголовке WebView2.h, включая те,
/// что не нужны.
/// </summary>
public static class WebView2Interop
{
	/// <summary>Путь к загрузчику WebView2 относительно установленного рантайма.</summary>
	public const string LoaderName = "WebView2Loader.dll";

	/// <summary>
	/// Внутренняя точка входа рантайма. Документирована, но с оговоркой:
	/// «может быть изменена или удалена в будущих версиях». Поэтому вызывающий
	/// код обязан иметь запасной путь (см. <c>WebView2Host</c>).
	///
	/// Сигнатура из документации Microsoft:
	/// <code>
	/// STDAPI CreateWebViewEnvironmentWithOptionsInternal(
	///     bool checkRunningInstance, int runtimeType, PCWSTR userDataFolder,
	///     IUnknown* environmentOptions,
	///     ICoreWebView2CreateCoreWebView2EnvironmentCompletedHandler* handler)
	/// </code>
	/// runtimeType: 0 — evergreen, 1 — фиксированная версия, -1 — неизвестно.
	/// </summary>
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
	public delegate int CreateEnvironmentInternalDelegate(
		[MarshalAs(UnmanagedType.Bool)] bool checkRunningInstance,
		int runtimeType,
		[MarshalAs(UnmanagedType.LPWStr)] string userDataFolder,
		IntPtr environmentOptions,
		IntPtr environmentCreatedHandler);

	/// <summary>Тип рантайма для <see cref="CreateEnvironmentInternalDelegate"/>.</summary>
	public const int RuntimeTypeEvergreen = 0;

	[StructLayout(LayoutKind.Sequential)]
	public struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;

		public static RECT FromSize(int width, int height)
		{
			return new RECT { Left = 0, Top = 0, Right = width, Bottom = height };
		}
	}

	/// <summary>Точка входа загрузчика: создать окружение.</summary>
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
	public delegate int CreateEnvironmentDelegate(
		[MarshalAs(UnmanagedType.LPWStr)] string browserExecutableFolder,
		[MarshalAs(UnmanagedType.LPWStr)] string userDataFolder,
		IntPtr environmentOptions,
		IntPtr environmentCreatedHandler);

	// ------------------------------------------------------------------ потребляемые интерфейсы

	[ComImport]
	[Guid("B96D755E-0319-4E92-A296-23436F46A1FC")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface ICoreWebView2Environment
	{
		[PreserveSig]
		int CreateCoreWebView2Controller(IntPtr parentWindow, IntPtr handler);

		[PreserveSig]
		int CreateWebResourceResponse(IntPtr content, int statusCode, IntPtr reasonPhrase,
			IntPtr headers, IntPtr response);

		[PreserveSig]
		int get_BrowserVersionString([MarshalAs(UnmanagedType.LPWStr)] out string versionInfo);

		[PreserveSig]
		int add_NewBrowserVersionAvailable(IntPtr handler, out long token);

		[PreserveSig]
		int remove_NewBrowserVersionAvailable(long token);
	}

	[ComImport]
	[Guid("4D00C0D1-9434-4EB6-8078-8697A560334F")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface ICoreWebView2Controller
	{
		[PreserveSig]
		int get_IsVisible([MarshalAs(UnmanagedType.Bool)] out bool value);

		[PreserveSig]
		int put_IsVisible([MarshalAs(UnmanagedType.Bool)] bool value);

		[PreserveSig]
		int get_Bounds(out RECT bounds);

		[PreserveSig]
		int put_Bounds(RECT bounds);

		[PreserveSig]
		int get_ZoomFactor(out double zoom);

		[PreserveSig]
		int put_ZoomFactor(double zoom);

		[PreserveSig]
		int add_ZoomFactorChanged(IntPtr handler, out long token);

		[PreserveSig]
		int remove_ZoomFactorChanged(long token);

		[PreserveSig]
		int SetBoundsAndZoomFactor(RECT bounds, double zoom);

		[PreserveSig]
		int MoveFocus(int reason);

		[PreserveSig]
		int add_MoveFocusRequested(IntPtr handler, out long token);

		[PreserveSig]
		int remove_MoveFocusRequested(long token);

		[PreserveSig]
		int add_GotFocus(IntPtr handler, out long token);

		[PreserveSig]
		int remove_GotFocus(long token);

		[PreserveSig]
		int add_LostFocus(IntPtr handler, out long token);

		[PreserveSig]
		int remove_LostFocus(long token);

		[PreserveSig]
		int add_AcceleratorKeyPressed(IntPtr handler, out long token);

		[PreserveSig]
		int remove_AcceleratorKeyPressed(long token);

		[PreserveSig]
		int get_ParentWindow(out IntPtr window);

		[PreserveSig]
		int put_ParentWindow(IntPtr window);

		[PreserveSig]
		int NotifyParentWindowPositionChanged();

		[PreserveSig]
		int Close();

		[PreserveSig]
		int get_CoreWebView2(out ICoreWebView2 webview);
	}

	[ComImport]
	[Guid("76ECEACB-0462-4D94-AC83-423A6793775E")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface ICoreWebView2
	{
		[PreserveSig]
		int get_Settings(out IntPtr settings);

		[PreserveSig]
		int get_Source([MarshalAs(UnmanagedType.LPWStr)] out string uri);

		[PreserveSig]
		int Navigate([MarshalAs(UnmanagedType.LPWStr)] string uri);

		[PreserveSig]
		int NavigateToString([MarshalAs(UnmanagedType.LPWStr)] string htmlContent);

		[PreserveSig]
		int add_NavigationStarting(IntPtr handler, out long token);

		[PreserveSig]
		int remove_NavigationStarting(long token);

		[PreserveSig]
		int add_ContentLoading(IntPtr handler, out long token);

		[PreserveSig]
		int remove_ContentLoading(long token);

		[PreserveSig]
		int add_SourceChanged(IntPtr handler, out long token);

		[PreserveSig]
		int remove_SourceChanged(long token);

		[PreserveSig]
		int add_HistoryChanged(IntPtr handler, out long token);

		[PreserveSig]
		int remove_HistoryChanged(long token);

		[PreserveSig]
		int add_NavigationCompleted(IntPtr handler, out long token);

		[PreserveSig]
		int remove_NavigationCompleted(long token);

		[PreserveSig]
		int add_FrameNavigationStarting(IntPtr handler, out long token);

		[PreserveSig]
		int remove_FrameNavigationStarting(long token);

		[PreserveSig]
		int add_FrameNavigationCompleted(IntPtr handler, out long token);

		[PreserveSig]
		int remove_FrameNavigationCompleted(long token);

		[PreserveSig]
		int add_ScriptDialogOpening(IntPtr handler, out long token);

		[PreserveSig]
		int remove_ScriptDialogOpening(long token);

		[PreserveSig]
		int add_PermissionRequested(IntPtr handler, out long token);

		[PreserveSig]
		int remove_PermissionRequested(long token);

		[PreserveSig]
		int add_ProcessFailed(IntPtr handler, out long token);

		[PreserveSig]
		int remove_ProcessFailed(long token);

		[PreserveSig]
		int AddScriptToExecuteOnDocumentCreated([MarshalAs(UnmanagedType.LPWStr)] string script,
			IntPtr handler);

		[PreserveSig]
		int RemoveScriptToExecuteOnDocumentCreated([MarshalAs(UnmanagedType.LPWStr)] string id);

		[PreserveSig]
		int ExecuteScript([MarshalAs(UnmanagedType.LPWStr)] string script, IntPtr handler);

		[PreserveSig]
		int CapturePreview(int imageFormat, IntPtr stream, IntPtr handler);

		[PreserveSig]
		int Reload();

		[PreserveSig]
		int PostWebMessageAsJson([MarshalAs(UnmanagedType.LPWStr)] string json);

		[PreserveSig]
		int PostWebMessageAsString([MarshalAs(UnmanagedType.LPWStr)] string text);

		[PreserveSig]
		int add_WebMessageReceived(IntPtr handler, out long token);

		[PreserveSig]
		int remove_WebMessageReceived(long token);

		[PreserveSig]
		int CallDevToolsProtocolMethod([MarshalAs(UnmanagedType.LPWStr)] string methodName,
			[MarshalAs(UnmanagedType.LPWStr)] string parametersAsJson, IntPtr handler);

		[PreserveSig]
		int get_BrowserProcessId(out uint value);

		[PreserveSig]
		int get_CanGoBack([MarshalAs(UnmanagedType.Bool)] out bool value);

		[PreserveSig]
		int get_CanGoForward([MarshalAs(UnmanagedType.Bool)] out bool value);

		[PreserveSig]
		int GoBack();

		[PreserveSig]
		int GoForward();

		[PreserveSig]
		int GetDevToolsProtocolEventReceiver([MarshalAs(UnmanagedType.LPWStr)] string eventName,
			out IntPtr receiver);

		[PreserveSig]
		int Stop();

		[PreserveSig]
		int add_NewWindowRequested(IntPtr handler, out long token);

		[PreserveSig]
		int remove_NewWindowRequested(long token);

		[PreserveSig]
		int add_DocumentTitleChanged(IntPtr handler, out long token);

		[PreserveSig]
		int remove_DocumentTitleChanged(long token);

		[PreserveSig]
		int get_DocumentTitle([MarshalAs(UnmanagedType.LPWStr)] out string title);

		[PreserveSig]
		int ContainsFullScreenElement([MarshalAs(UnmanagedType.Bool)] out bool value);

		[PreserveSig]
		int add_ContainsFullScreenElementChanged(IntPtr handler, out long token);

		[PreserveSig]
		int remove_ContainsFullScreenElementChanged(long token);

		[PreserveSig]
		int add_WebResourceRequested(IntPtr handler, out long token);

		[PreserveSig]
		int remove_WebResourceRequested(long token);

		[PreserveSig]
		int AddWebResourceRequestedFilter([MarshalAs(UnmanagedType.LPWStr)] string uri,
			int resourceContext);

		[PreserveSig]
		int RemoveWebResourceRequestedFilter([MarshalAs(UnmanagedType.LPWStr)] string uri,
			int resourceContext);

		[PreserveSig]
		int add_WindowCloseRequested(IntPtr handler, out long token);

		[PreserveSig]
		int remove_WindowCloseRequested(long token);
	}

	// ------------------------------------------------------------------ реализуемые обработчики

	/// <summary>
	/// Колбэк создания окружения. Реализуется в управляемом коде, поэтому
	/// ComVisible + настоящий IID: иначе нативная сторона не увидит объект.
	/// </summary>
	[ComVisible(true)]
	[Guid("4E8A3389-C9D8-4BD2-B6B5-124FEE6CC14D")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface ICreateEnvironmentCompletedHandler
	{
		[PreserveSig]
		int Invoke(int result, [MarshalAs(UnmanagedType.Interface)] ICoreWebView2Environment environment);
	}

	[ComVisible(true)]
	[Guid("6C4819F3-C9B7-4260-8127-C9F5BDE687E3")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface ICreateControllerCompletedHandler
	{
		[PreserveSig]
		int Invoke(int result, [MarshalAs(UnmanagedType.Interface)] ICoreWebView2Controller controller);
	}

	[ComVisible(true)]
	[Guid("E92147CC-1B41-4C9A-9C4B-9D5C5C2A1B11")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface INavigationCompletedHandler
	{
		[PreserveSig]
		int Invoke(IntPtr sender, IntPtr args);
	}

	/// <summary>Обёртки над обработчиками: хранят результат для вызывающего кода.</summary>
	public sealed class EnvironmentHandler : ICreateEnvironmentCompletedHandler
	{
		public int Result = -1;

		public ICoreWebView2Environment Environment;

		public int Invoke(int result, ICoreWebView2Environment environment)
		{
			Result = result;
			Environment = environment;
			return 0;
		}
	}

	public sealed class ControllerHandler : ICreateControllerCompletedHandler
	{
		public int Result = -1;

		public ICoreWebView2Controller Controller;

		public int Invoke(int result, ICoreWebView2Controller controller)
		{
			Result = result;
			Controller = controller;
			return 0;
		}
	}

	/// <summary>Признак успеха COM-вызова.</summary>
	public static bool Succeeded(int hr)
	{
		return hr >= 0;
	}
}
