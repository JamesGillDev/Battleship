using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace BattleshipMaui.ViewModels;

public static class ThemeTokenService
{
    public static void Apply(bool highContrast, bool largeText)
    {
        var resources = Application.Current?.Resources;
        if (resources is null)
            return;

        var palette = highContrast
            ? new ThemePalette(
                Background: "#000000",
                Surface: "#0f0f0f",
                SurfaceAlt: "#161616",
                Panel: "#1a1a1a",
                Border: "#ffffff",
                Accent: "#00d4ff",
                AccentSoft: "#00a0c1",
                TextPrimary: "#ffffff",
                TextSecondary: "#e8e8e8",
                TextMuted: "#b7b7b7",
                Success: "#59ffad",
                Warning: "#ffd66b",
                Danger: "#ff7f7f")
            : new ThemePalette(
                Background: "#040C18",
                Surface: "#0C1B2E",
                SurfaceAlt: "#12324D",
                Panel: "#163E60",
                Border: "#2E5A84",
                Accent: "#61D4FF",
                AccentSoft: "#3196CE",
                TextPrimary: "#EEF8FF",
                TextSecondary: "#D6E9F8",
                TextMuted: "#9EC3E1",
                Success: "#8AE7B7",
                Warning: "#FFD07B",
                Danger: "#FF8A69");

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

        double scale = largeText ? 1.18 : 1.0;
        SetDouble(resources, "GameTypeDisplay", 34 * scale);
        SetDouble(resources, "GameTypeTitle", 22 * scale);
        SetDouble(resources, "GameTypeBody", 15 * scale);
        SetDouble(resources, "GameTypeCaption", 12 * scale);
    }

    private static void SetColor(ResourceDictionary resources, string key, string value)
    {
        resources[key] = Color.FromArgb(value);
    }

    private static void SetDouble(ResourceDictionary resources, string key, double value)
    {
        resources[key] = value;
    }
}

file sealed record ThemePalette(
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
    string Danger);
