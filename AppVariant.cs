namespace BattleshipMaui;

public static class AppVariant
{
#if ENABLE_LAN
    public static bool IsLanEdition => true;
    public static bool IsSoloEdition => false;
    public const string PublicAppName = "LANBattleshipMAUI";
    public const string HeaderTitle = "LAN Task Force Command";
    public const string HeaderSubtitle = "Same-network tactical duel across two Windows PCs";
    public const string SessionPanelTitle = "LAN Session";
    public const string SoloEditionSummary = "Dedicated LAN build active.";
    public const string SoloEditionHelpText = "LANBattleshipMAUI is built for same-network multiplayer on two Windows PCs.";
#else
    public static bool IsLanEdition => false;
    public static bool IsSoloEdition => true;
    public const string PublicAppName = "BattleshipMaui";
    public const string HeaderTitle = "Task Force Command";
    public const string HeaderSubtitle = "Single-player naval battle against the onboard CPU";
    public const string SessionPanelTitle = "Solo Command";
    public const string SoloEditionSummary = "Dedicated solo build active. Battle the onboard CPU on this PC.";
    public const string SoloEditionHelpText = "Use New Mission to restart the board and Settings to adjust CPU difficulty.";
#endif
}
