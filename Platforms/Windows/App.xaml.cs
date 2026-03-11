using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BattleshipMaui.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
	/// <summary>
	/// Initializes the singleton application object.  This is the first line of authored code
	/// executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public App()
	{
		CrashLog.Initialize();
		this.InitializeComponent();
		CrashLog.HookWinUiUnhandledException(this);
	}

	protected override void OnLaunched(LaunchActivatedEventArgs args)
	{
		base.OnLaunched(args);
		TryShowMainWindow();
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	private static void TryShowMainWindow()
	{
		try
		{
			if (Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView is not Microsoft.UI.Xaml.Window window)
				return;

			ShowWindowCore(window);

			_ = Task.Run(async () =>
			{
				try
				{
					await Task.Delay(240).ConfigureAwait(false);
					window.DispatcherQueue?.TryEnqueue(() => ShowWindowCore(window));
				}
				catch (Exception ex)
				{
					CrashLog.Write("WinUI.App.TryShowMainWindow.Delayed", ex);
				}
			});
		}
		catch (Exception ex)
		{
			CrashLog.Write("WinUI.App.TryShowMainWindow", ex);
		}
	}

	private static void ShowWindowCore(Microsoft.UI.Xaml.Window window)
	{
		window.Activate();

		nint hwnd = WindowNative.GetWindowHandle(window);
		if (hwnd == 0)
			return;

		ShowWindow(hwnd, 9);
		ShowWindow(hwnd, 5);
		SetForegroundWindow(hwnd);
	}

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(nint hWnd, int nCmdShow);

	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(nint hWnd);
}
