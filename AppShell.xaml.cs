namespace BattleshipMaui;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Title = AppVariant.PublicAppName;

		if (Items.Count > 0)
			Items[0].Title = AppVariant.PublicAppName;
	}
}
