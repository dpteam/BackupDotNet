using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using BackupDotNet.Core;
namespace BackupDotNet.Setup;

/// <summary>
/// Окно мастера установки.
///
/// Внутри — WebView2 с локальной страницей: те же стили, что у веб-интерфейса,
/// поэтому установщик выглядит частью продукта, а не отдельной утилитой.
///
/// Если WebView2 завести не удалось, окно не падает: показывает понятную
/// ошибку и предлагает открыть мастер в браузере. Установщик, который не
/// открывается, — худший из возможных.
/// </summary>
public sealed class SetupWindow : Form
{
	private readonly string _url;

	private readonly string _uiDir;

	private WebView2Host _host;

	/// <summary>Куда перейти, если WebView2 не завёлся.</summary>
	public string FallbackUrl { get; private set; } = "";

	public SetupWindow(string url, string uiDir)
	{
		_url = url;
		_uiDir = uiDir;

		Text = "Установка DPT.Backup";
		StartPosition = FormStartPosition.CenterScreen;
		MinimumSize = new Size(900, 620);
		ClientSize = new Size(1000, 700);
		BackColor = Color.FromArgb(11, 18, 32);
		DoubleBuffered = true;

		ApplyIcon();
	}

	private void ApplyIcon()
	{
		try
		{
			string ico = Path.Combine(_uiDir, "setup-icon.ico");
			if (File.Exists(ico))
			{
				Icon = new Icon(ico);
				return;
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Иконка окна: " + ex.Message);
		}

		// Запасной вариант: иконка из самого exe.
		try
		{
			Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
		}
		catch
		{
		}
	}

	protected override void OnShown(EventArgs e)
	{
		base.OnShown(e);

		_host = new WebView2Host(this);
		if (_host.Initialize() && _host.Navigate(_url))
		{
			Log.Info("Мастер: WebView2 " + _host.BrowserVersion + ", адрес " + _url);
			return;
		}

		// WebView2 не поднялся — показываем ошибку прямо в окне и оставляем
		// кнопку «открыть в браузере», чтобы установка не сорвалась.
		string reason = string.IsNullOrEmpty(_host.Error) ? "неизвестная ошибка" : _host.Error;
		Log.Warn("Мастер: WebView2 не запустился — " + reason);
		FallbackUrl = _url;
		ShowFallback(reason);
	}

	private void ShowFallback(string reason)
	{
		Panel panel = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = Color.FromArgb(11, 18, 32),
			Padding = new Padding(48)
		};

		Label title = new Label
		{
			Text = "Не удалось открыть окно мастера",
			ForeColor = Color.FromArgb(232, 238, 252),
			Font = new Font("Segoe UI", 16F, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(48, 48)
		};

		Label detail = new Label
		{
			Text = "Встроенный браузер недоступен: " + reason + "\r\n\r\n" +
				"Мастер можно открыть в обычном браузере — установка работает так же.",
			ForeColor = Color.FromArgb(150, 168, 195),
			Font = new Font("Segoe UI", 10F),
			AutoSize = false,
			Size = new Size(860, 120),
			Location = new Point(48, 96)
		};

		Button open = new Button
		{
			Text = "Открыть в браузере",
			Size = new Size(200, 38),
			Location = new Point(48, 240),
			FlatStyle = FlatStyle.Flat,
			BackColor = Color.FromArgb(0, 136, 255),
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 10F, FontStyle.Bold)
		};
		open.FlatAppearance.BorderSize = 0;
		open.Click += delegate { OpenInBrowser(_url); };

		Button close = new Button
		{
			Text = "Закрыть",
			Size = new Size(120, 38),
			Location = new Point(264, 240),
			FlatStyle = FlatStyle.Flat,
			BackColor = Color.FromArgb(30, 41, 59),
			ForeColor = Color.FromArgb(200, 212, 232),
			Font = new Font("Segoe UI", 10F)
		};
		close.FlatAppearance.BorderSize = 0;
		close.Click += delegate { Close(); };

		panel.Controls.Add(title);
		panel.Controls.Add(detail);
		panel.Controls.Add(open);
		panel.Controls.Add(close);
		Controls.Add(panel);
	}

	/// <summary>Открывает адрес в браузере по умолчанию.</summary>
	public static void OpenInBrowser(string url)
	{
		try
		{
			System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
			{
				FileName = url,
				UseShellExecute = true
			});
		}
		catch (Exception ex)
		{
			MessageBox.Show("Не удалось открыть браузер: " + ex.Message, "Установка DPT.Backup",
				MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		_host?.ApplyBounds();
	}

	protected override void OnMove(EventArgs e)
	{
		base.OnMove(e);
		_host?.NotifyMoved();
	}

	protected override void OnFormClosed(FormClosedEventArgs e)
	{
		base.OnFormClosed(e);
		try
		{
			_host?.Dispose();
		}
		catch
		{
		}
		_host = null;
	}
}
