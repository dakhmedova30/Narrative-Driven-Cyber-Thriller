using UnityEngine;

/// <summary>
/// Colour tokens copied from the Figma file (Cyberpunk-Jaycord → Variables).
/// Change a colour here and every Jaycord UI built by the builder will use it.
/// </summary>
public static class JaycordTheme
{
    public static readonly Color BgBase       = Hex("#07070D"); // bg/base
    public static readonly Color BgPanel      = Hex("#0E0E1A"); // bg/panel
    public static readonly Color BgElevated   = Hex("#171729"); // bg/elevated
    public static readonly Color Border       = Hex("#2A2A48"); // border/default
    public static readonly Color Cyan         = Hex("#00F0FF"); // accent/cyan
    public static readonly Color Magenta      = Hex("#FF2E88"); // accent/magenta
    public static readonly Color Yellow       = Hex("#F5E663"); // accent/yellow
    public static readonly Color TextPrimary  = Hex("#E6E8FF"); // text/primary
    public static readonly Color TextMuted    = Hex("#7C7FA6"); // text/muted
    public static readonly Color TextOnAccent = Hex("#07070D"); // text/on-accent
    public static readonly Color Online       = Hex("#39FF88"); // status/online
    public static readonly Color Offline      = Hex("#4A4C6A"); // status/offline

    public static Color WithAlpha(Color c, float alpha)
    {
        c.a = alpha;
        return c;
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }
}
