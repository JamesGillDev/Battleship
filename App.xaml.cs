using Microsoft.Extensions.DependencyInjection;

namespace BattleshipMaui;

public partial class App : Application
{
	public App()
	{
		CrashLog.Initialize();
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new MainPage())
		{
			Title = AppVariant.PublicAppName
		};
	}
}
