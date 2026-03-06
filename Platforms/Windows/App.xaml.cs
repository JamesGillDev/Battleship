using Microsoft.UI.Xaml;

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
		this.UnhandledException += OnUnhandledException;
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
	{
		if (e.Exception is LayoutCycleException)
			e.Handled = true;
	}
}

