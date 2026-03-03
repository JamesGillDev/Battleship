using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls.Shapes;

namespace BattleshipMaui.ViewModels;

public enum GameThemePreset
{
    RetroWave80s = 0,
    NeonHarbor,
    CrimsonStrike,
    ToxicGrid,
    SolarFlare,
    ArcticPulse,
    DesertStorm,
    VioletNoir,
    MonochromeAmber,
    CandyShock
}

public sealed record ThemeOption(GameThemePreset Theme, string DisplayName);

public static class ThemeTokenService
{
    private static readonly ThemeOption[] ThemeChoices =
    {
        new(GameThemePreset.RetroWave80s, "RetroWave 80s"),
        new(GameThemePreset.NeonHarbor, "Neon Harbor"),
        new(GameThemePreset.CrimsonStrike, "Crimson Strike"),
        new(GameThemePreset.ToxicGrid, "Toxic Grid"),
        new(GameThemePreset.SolarFlare, "Solar Flare"),
        new(GameThemePreset.ArcticPulse, "Arctic Pulse"),
        new(GameThemePreset.DesertStorm, "Desert Storm"),
        new(GameThemePreset.VioletNoir, "Violet Noir"),
        new(GameThemePreset.MonochromeAmber, "Monochrome Amber"),
        new(GameThemePreset.CandyShock, "Candy Shock")
    };

    public static IReadOnlyList<ThemeOption> ThemeOptions => ThemeChoices;

    public static ThemeOption GetOption(GameThemePreset theme)
    {
        return ThemeChoices.FirstOrDefault(choice => choice.Theme == theme)
            ?? ThemeChoices[0];
    }

    public static void Apply(GameThemePreset theme, bool highContrast, bool largeText)
    {
        var resources = Application.Current?.Resources;
        if (resources is null)
            return;

        var palette = highContrast
            ? CreateHighContrastPalette(GetThemePalette(theme))
            : GetThemePalette(theme);

        SetColor(resources, "GameColorBackground", palette.Background);
        SetColor(resources, "GameColorSurface", palette.Surface);
        SetColor(resources, "GameColorSurfaceAlt", palette.SurfaceAlt);
        SetColor(resources, "GameColorPanel", palette.Panel);
        SetColor(resources, "GameColorBorder", palette.Border);
        SetColor(resources, "GameColorAccent", palette.Accent);
        SetColor(resources, "GameColorAccentSoft", palette.AccentSoft);
        SetColor(resources, "GameColorTextPrimary", palette.TextPrimary);
        SetColor(resources, "GameColorTextSecondary", palette.TextSecondary);
        SetColor(resources, "GameColorTextMuted", palette.TextMuted);
        SetColor(resources, "GameColorSuccess", palette.Success);
        SetColor(resources, "GameColorWarning", palette.Warning);
        SetColor(resources, "GameColorDanger", palette.Danger);
        SetColor(resources, "GameColorHeaderStart", palette.HeaderStart);
        SetColor(resources, "GameColorHeaderEnd", palette.HeaderEnd);
        SetColor(resources, "GameColorTransitionCard", palette.TransitionCard);
        SetColor(resources, "GameColorSettingsCard", palette.SettingsCard);
        SetColor(resources, "GameColorThinkingPulseA", palette.Accent);
        SetColor(resources, "GameColorThinkingPulseB", palette.AccentSoft);
        SetColor(resources, "GameColorThinkingPulseC", palette.Warning);

        var shapes = GetShapeProfile(theme);
        SetShape(resources, "GameCardStrokeShape", shapes.CardCornerRadius);
        SetShape(resources, "GameShipCardShape", shapes.ShipCardCornerRadius);
        SetShape(resources, "GameBoardFrameShape", shapes.BoardFrameCornerRadius);
        SetShape(resources, "GameBoardCellShape", shapes.BoardCellCornerRadius);
        SetShape(resources, "GameShipSpriteShape", shapes.ShipSpriteCornerRadius);
        SetShape(resources, "GameMissPegShape", shapes.MissPegCornerRadius);
        SetShape(resources, "GameMissPegCapShape", shapes.MissPegCapCornerRadius);

        double scale = largeText ? 1.18 : 1.0;
        SetDouble(resources, "GameTypeDisplay", 34 * scale);
        SetDouble(resources, "GameTypeTitle", 22 * scale);
        SetDouble(resources, "GameTypeBody", 15 * scale);
        SetDouble(resources, "GameTypeCaption", 12 * scale);
    }

    private static ThemePalette GetThemePalette(GameThemePreset theme)
    {
        return theme switch
        {
            GameThemePreset.NeonHarbor => new ThemePalette(
                Background: "#02121B",
                Surface: "#0A2638",
                SurfaceAlt: "#11415F",
                Panel: "#145274",
                Border: "#2C84AF",
                Accent: "#3AFBFF",
                AccentSoft: "#10A4D2",
                TextPrimary: "#E6FBFF",
                TextSecondary: "#C0E7F2",
                TextMuted: "#81B5CA",
                Success: "#7AFFC6",
                Warning: "#FFE06A",
                Danger: "#FF6D7A",
                HeaderStart: "#15364E",
                HeaderEnd: "#071826",
                TransitionCard: "#153E54",
                SettingsCard: "#133B53"),
            GameThemePreset.CrimsonStrike => new ThemePalette(
                Background: "#170408",
                Surface: "#2A0A10",
                SurfaceAlt: "#4A1622",
                Panel: "#5C1F2D",
                Border: "#8B3449",
                Accent: "#FF5A7B",
                AccentSoft: "#DB2958",
                TextPrimary: "#FFEAF0",
                TextSecondary: "#F8C7D5",
                TextMuted: "#D290A5",
                Success: "#74F4A8",
                Warning: "#FFCB6C",
                Danger: "#FF3D4F",
                HeaderStart: "#521425",
                HeaderEnd: "#1A070B",
                TransitionCard: "#4B1A2C",
                SettingsCard: "#441726"),
            GameThemePreset.ToxicGrid => new ThemePalette(
                Background: "#051007",
                Surface: "#102713",
                SurfaceAlt: "#1D4722",
                Panel: "#24592B",
                Border: "#3E8B45",
                Accent: "#B6FF38",
                AccentSoft: "#6FCF23",
                TextPrimary: "#EEFFE1",
                TextSecondary: "#D0F5BE",
                TextMuted: "#9CC78B",
                Success: "#8CFF96",
                Warning: "#FFEF66",
                Danger: "#FF7681",
                HeaderStart: "#244B1E",
                HeaderEnd: "#0B1C0A",
                TransitionCard: "#2A5B27",
                SettingsCard: "#245024"),
            GameThemePreset.SolarFlare => new ThemePalette(
                Background: "#1B0F03",
                Surface: "#31210A",
                SurfaceAlt: "#5C3811",
                Panel: "#704617",
                Border: "#A96E2A",
                Accent: "#FFB347",
                AccentSoft: "#FF7B2F",
                TextPrimary: "#FFF1DE",
                TextSecondary: "#F7D8AF",
                TextMuted: "#D1AF7C",
                Success: "#95F29C",
                Warning: "#FFD966",
                Danger: "#FF6E54",
                HeaderStart: "#5F320C",
                HeaderEnd: "#251505",
                TransitionCard: "#663C13",
                SettingsCard: "#5A3511"),
            GameThemePreset.ArcticPulse => new ThemePalette(
                Background: "#040F1A",
                Surface: "#0E2236",
                SurfaceAlt: "#173A57",
                Panel: "#1D4A69",
                Border: "#2F7298",
                Accent: "#79E8FF",
                AccentSoft: "#3AB6E5",
                TextPrimary: "#ECF9FF",
                TextSecondary: "#CAE8F6",
                TextMuted: "#8FB7CC",
                Success: "#96F3D0",
                Warning: "#FFE184",
                Danger: "#FF8EA8",
                HeaderStart: "#1A4A70",
                HeaderEnd: "#0A1624",
                TransitionCard: "#1A3A57",
                SettingsCard: "#18334D"),
            GameThemePreset.DesertStorm => new ThemePalette(
                Background: "#151007",
                Surface: "#2A2212",
                SurfaceAlt: "#44371E",
                Panel: "#544428",
                Border: "#7C6640",
                Accent: "#F4D06C",
                AccentSoft: "#CFA345",
                TextPrimary: "#FDF4DD",
                TextSecondary: "#E7D5A8",
                TextMuted: "#BBA477",
                Success: "#9DEA9A",
                Warning: "#FFD06A",
                Danger: "#F58B66",
                HeaderStart: "#4D3A1F",
                HeaderEnd: "#1E170D",
                TransitionCard: "#4C3B22",
                SettingsCard: "#42341F"),
            GameThemePreset.VioletNoir => new ThemePalette(
                Background: "#0D0817",
                Surface: "#1D1331",
                SurfaceAlt: "#2E1E4D",
                Panel: "#3A2660",
                Border: "#65469E",
                Accent: "#D67CFF",
                AccentSoft: "#9A4EDB",
                TextPrimary: "#F7EDFF",
                TextSecondary: "#DEC8F4",
                TextMuted: "#B092CE",
                Success: "#9BEEB0",
                Warning: "#FFDA80",
                Danger: "#FF85C6",
                HeaderStart: "#3A235F",
                HeaderEnd: "#151022",
                TransitionCard: "#39245A",
                SettingsCard: "#301E4E"),
            GameThemePreset.MonochromeAmber => new ThemePalette(
                Background: "#090909",
                Surface: "#171717",
                SurfaceAlt: "#242424",
                Panel: "#303030",
                Border: "#646464",
                Accent: "#FFC857",
                AccentSoft: "#D7932B",
                TextPrimary: "#F9F9F9",
                TextSecondary: "#E5E5E5",
                TextMuted: "#B2B2B2",
                Success: "#95ECA5",
                Warning: "#FFD96F",
                Danger: "#FF7B66",
                HeaderStart: "#303030",
                HeaderEnd: "#121212",
                TransitionCard: "#2A2A2A",
                SettingsCard: "#252525"),
            GameThemePreset.CandyShock => new ThemePalette(
                Background: "#120B21",
                Surface: "#2A1544",
                SurfaceAlt: "#4E1F74",
                Panel: "#602A88",
                Border: "#8A4AB2",
                Accent: "#FF8FF6",
                AccentSoft: "#5EEBFF",
                TextPrimary: "#FFF0FF",
                TextSecondary: "#F6D5FF",
                TextMuted: "#C69CD8",
                Success: "#8DF4B7",
                Warning: "#FFE685",
                Danger: "#FF88A8",
                HeaderStart: "#5C2A8D",
                HeaderEnd: "#1A1030",
                TransitionCard: "#5A2A85",
                SettingsCard: "#4E2375"),
            _ => new ThemePalette(
                Background: "#060717",
                Surface: "#151937",
                SurfaceAlt: "#262D63",
                Panel: "#2D3575",
                Border: "#4A5AB8",
                Accent: "#35F4FF",
                AccentSoft: "#FF4FD8",
                TextPrimary: "#F0F4FF",
                TextSecondary: "#D7E1FF",
                TextMuted: "#9CB1E6",
                Success: "#8DF6A6",
                Warning: "#FFD86B",
                Danger: "#FF7A9F",
                HeaderStart: "#2D347A",
                HeaderEnd: "#12162E",
                TransitionCard: "#2B2F66",
                SettingsCard: "#262B59")
        };
    }

    private static ThemePalette CreateHighContrastPalette(ThemePalette source)
    {
        return source with
        {
            Background = "#040404",
            Surface = "#101010",
            SurfaceAlt = "#161616",
            Panel = "#1D1D1D",
            Border = "#F2F2F2",
            TextPrimary = "#FFFFFF",
            TextSecondary = "#F3F3F3",
            TextMuted = "#D8D8D8",
            AccentSoft = source.Accent,
            HeaderStart = "#1A1A1A",
            HeaderEnd = "#050505",
            TransitionCard = "#121212",
            SettingsCard = "#171717"
        };
    }

    private static ThemeShapeProfile GetShapeProfile(GameThemePreset theme)
    {
        return theme switch
        {
            GameThemePreset.NeonHarbor => new ThemeShapeProfile(14, 8, 10, 4, 5, 10, 4),
            GameThemePreset.CrimsonStrike => new ThemeShapeProfile(10, 6, 6, 2, 3, 6, 2),
            GameThemePreset.ToxicGrid => new ThemeShapeProfile(4, 2, 2, 0.8, 1.2, 3, 1.2),
            GameThemePreset.SolarFlare => new ThemeShapeProfile(12, 7, 9, 3.5, 4.5, 9, 3.5),
            GameThemePreset.ArcticPulse => new ThemeShapeProfile(16, 10, 12, 5, 6, 11, 4),
            GameThemePreset.DesertStorm => new ThemeShapeProfile(9, 5, 7, 2.5, 3, 7, 2.5),
            GameThemePreset.VioletNoir => new ThemeShapeProfile(18, 12, 14, 6, 7, 12, 5),
            GameThemePreset.MonochromeAmber => new ThemeShapeProfile(3, 2, 2, 0.6, 1, 3, 1),
            GameThemePreset.CandyShock => new ThemeShapeProfile(20, 13, 15, 7, 8, 13, 5),
            _ => new ThemeShapeProfile(12, 8, 8, 3, 5, 8, 3)
        };
    }

    private static void SetColor(ResourceDictionary resources, string key, string value)
    {
        resources[key] = Color.FromArgb(value);
    }

    private static void SetDouble(ResourceDictionary resources, string key, double value)
    {
        resources[key] = value;
    }

    private static void SetShape(ResourceDictionary resources, string key, double cornerRadius)
    {
        resources[key] = new RoundRectangle { CornerRadius = new CornerRadius(cornerRadius) };
    }
}

sealed record ThemePalette(
    string Background,
    string Surface,
    string SurfaceAlt,
    string Panel,
    string Border,
    string Accent,
    string AccentSoft,
    string TextPrimary,
    string TextSecondary,
    string TextMuted,
    string Success,
    string Warning,
    string Danger,
    string HeaderStart,
    string HeaderEnd,
    string TransitionCard,
    string SettingsCard);

sealed record ThemeShapeProfile(
    double CardCornerRadius,
    double ShipCardCornerRadius,
    double BoardFrameCornerRadius,
    double BoardCellCornerRadius,
    double ShipSpriteCornerRadius,
    double MissPegCornerRadius,
    double MissPegCapCornerRadius);
