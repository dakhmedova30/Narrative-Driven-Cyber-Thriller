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

    // ---- Updated layout (Figma "Chat – Cyberpunk", Oct 29) ----
    public static readonly Color BgRail         = Hex("#1B1F29"); // server rail
    public static readonly Color BgSide         = Hex("#222733"); // chat list + profile panel
    public static readonly Color BgConversation = Hex("#191C24"); // message area
    public static readonly Color BgHeader       = Hex("#12131E"); // chat header
    public static readonly Color Orange         = Hex("#E58D78"); // JAYCORD title
    public static readonly Color BubbleSelf     = Hex("#E6E8EF"); // player's bubbles
    public static readonly Color BubbleOther    = Hex("#A3ABBD"); // NPC bubbles + input bar
    public static readonly Color InkDark        = Hex("#191C24"); // text/icons on the light surfaces

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
