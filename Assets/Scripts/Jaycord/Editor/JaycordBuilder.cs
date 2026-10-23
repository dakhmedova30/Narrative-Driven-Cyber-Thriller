using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Yarn.Unity;
using Object = UnityEngine.Object;

/// <summary>
/// Menu: Tools ▸ Jaycord ▸ Build UI Prefabs + Test Scene
///
/// Rebuilds the whole Jaycord app to match the Figma frame "Chat – Cyberpunk" (1512 × 960):
///   server rail · chat list · conversation · profile panel
/// and the components: chat bubbles, reply options, link card, typing indicator.
///
/// Generates (and OVERWRITES):
///   Assets/Fonts/Jaycord/*SDF.asset        TextMeshPro fonts (Chakra Petch, JetBrains Mono, Orbitron)
///   Assets/Art/UI/Jaycord/Generated/       rounded / glow / dashed / gradient sprites, simple icons
///   Assets/Prefabs/Dialogue/Jaycord/       all prefabs (main one: JaycordApp.prefab)
///   Assets/Scenes/JaycordTest.unity        plays the "Jaycord_Test" Yarn node
///
/// Once you start editing the prefabs by hand, stop re-running this (it would overwrite your edits).
/// </summary>
public static class JaycordBuilder
{
    private const string ArtDir = "Assets/Art/UI/Jaycord";
    private const string GenDir = ArtDir + "/Generated";
    private const string IconDir = ArtDir + "/Icons";
    private const string AvatarDir = ArtDir + "/Avatars";
    private const string FontDir = "Assets/Fonts/Jaycord";
    private const string PrefabDir = "Assets/Prefabs/Dialogue/Jaycord";
    private const string ScenePath = "Assets/Scenes/JaycordTest.unity";
    private const string YarnProjectPath = "Assets/Dialogue/Story.yarnproject";
    private const string TestNode = "Jaycord_Test";

    // Fonts (Figma: Chakra Petch, JetBrains Mono, Orbitron)
    private static TMP_FontAsset chakraRegular, chakraMedium, chakraSemiBold, monoRegular, monoMedium, orbitron;
    private static Material orbitronGlow;

    // Sprites
    private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private static Sprite glowSprite, circleSprite, dashedSmall, dashedServer, gradientServer;
    private static Sprite iconSearch, iconMore, iconSparkle;
    private static Sprite iconMic, iconSmile, iconImage, iconPhone, iconVideo;

    private enum Kind { Self, Other, System }
    private enum ServerStyle { Gradient, Selected, Normal, Dashed }

    private const int GlowSpread = 16;

    [MenuItem("Tools/Jaycord/Build UI Prefabs + Test Scene")]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        spriteCache.Clear();

        EnsureFolder(GenDir);
        EnsureFolder(PrefabDir);
        CleanupOldVersion();

        LoadFonts();
        MakeSprites();
        LoadImportedArt();

        // Build everything in a fresh scene so the scene you had open is untouched.
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var self = BuildMessage("Jaycord_Message_Self", Kind.Self);
        var other = BuildMessage("Jaycord_Message_Other", Kind.Other);
        var system = BuildMessage("Jaycord_Message_System", Kind.System);
        var divider = BuildDateDivider();
        var reply = BuildReplyOption();
        var link = BuildLinkCard();
        var row = BuildContactRow();
        var dossierRow = BuildDossierRow();
        var tag = BuildTag();
        var app = BuildApp(self, other, system, divider, reply, link, row, dossierRow, tag);

        BuildTestScene(scene, app);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"<color=#00F0FF>[Jaycord]</color> Built {PrefabDir}/JaycordApp.prefab and {ScenePath}. Press Play to test.");
    }

    // ==================================================================
    // Messages & components
    // ==================================================================

    private static JaycordMessageView BuildMessage(string name, Kind kind)
    {
        var row = UI(name, null);
        H(row, 0, 0, 0, 0, 0, kind == Kind.Self ? TextAnchor.UpperRight
                             : kind == Kind.Other ? TextAnchor.UpperLeft
                             : TextAnchor.UpperCenter);

        var bubble = UI("Bubble", row.transform);
        TextMeshProUGUI text;
        int padX, padY;

        switch (kind)
        {
            case Kind.Self: // cyan, sharp top-right corner, cyan glow
                Glow(bubble, JaycordTheme.WithAlpha(JaycordTheme.Cyan, 0.35f));
                Fill(bubble, Rounded(12, 2, 12, 12), JaycordTheme.Cyan);
                padX = 16; padY = 12;
                break;
            case Kind.Other: // dark, magenta border, sharp top-left corner
                Img(bubble, Rounded(2, 12, 12, 12), JaycordTheme.BgElevated);
                Border(bubble, Rounded(2, 12, 12, 12, 1f), JaycordTheme.Magenta);
                padX = 16; padY = 12;
                break;
            default: // system: dashed cyan box
                Img(bubble, Rounded(2, 2, 2, 2), JaycordTheme.WithAlpha(JaycordTheme.Cyan, 0.08f));
                var dashed = Border(bubble, dashedSmall, JaycordTheme.Cyan);
                dashed.type = Image.Type.Tiled;
                padX = 12; padY = 6;
                break;
        }

        V(bubble, padX, padX, padY, padY, 0);
        var bubbleLe = bubble.AddComponent<LayoutElement>();

        if (kind == Kind.Self)
            text = Text(bubble.transform, "Text", "Message", chakraSemiBold, 16, JaycordTheme.TextOnAccent, wrap: true);
        else if (kind == Kind.Other)
            text = Text(bubble.transform, "Text", "Message", chakraMedium, 16, JaycordTheme.TextPrimary, wrap: true);
        else
            text = Text(bubble.transform, "Text", "Message", monoRegular, 12, JaycordTheme.Cyan, 0.96f,
                        TextAlignmentOptions.Center, wrap: true, upper: true);

        var view = row.AddComponent<JaycordMessageView>();
        Set(view, "body", text);
        Set(view, "bubbleLayout", bubbleLe);
        SetFloat(view, "maxTextWidth", kind == Kind.System ? 520f : 440f);
        SetFloat(view, "horizontalPadding", padX * 2f);
        return Save(row, name).GetComponent<JaycordMessageView>();
    }

    /// <summary>Centered date like "NOV 30, 2023, 9:41 AM" (35px gap above and below).</summary>
    private static JaycordDivider BuildDateDivider()
    {
        var row = UI("Jaycord_DateDivider", null);
        V(row, 0, 0, 27, 27, 0, TextAnchor.UpperCenter);
        var label = Text(row.transform, "Label", "TODAY", monoRegular, 12, JaycordTheme.TextMuted, 1.44f,
                         TextAlignmentOptions.Center, upper: true);
        var divider = row.AddComponent<JaycordDivider>();
        Set(divider, "label", label);
        return Save(row, "Jaycord_DateDivider").GetComponent<JaycordDivider>();
    }

    private static JaycordReplyOption BuildReplyOption()
    {
        var root = UI("Jaycord_ReplyOption", null);
        var glow = Glow(root, JaycordTheme.WithAlpha(JaycordTheme.Cyan, 0f));
        var fill = Fill(root, Rounded(2, 2, 2, 2), JaycordTheme.BgElevated, raycast: true);
        var border = Border(root, Rounded(2, 2, 2, 2, 1f), JaycordTheme.Cyan);
        H(root, 14, 14, 12, 12, 12, TextAnchor.MiddleLeft);
        LE(root, minH: 44);

        var index = Text(root.transform, "Key", "[1]", monoMedium, 12, JaycordTheme.Cyan);
        var label = Text(root.transform, "Text", "Reply text", chakraMedium, 15, JaycordTheme.TextPrimary, wrap: true);
        LE(label.gameObject, flexW: 1, minW: 0);
        var locked = Text(root.transform, "Locked", "LOCKED", monoRegular, 10, JaycordTheme.Yellow, 1f);

        var option = root.AddComponent<JaycordReplyOption>();
        Set(option, "fill", fill);
        Set(option, "border", border);
        Set(option, "glow", glow);
        Set(option, "indexText", index);
        Set(option, "label", label);
        Set(option, "lockedTag", locked.gameObject);
        return Save(root, "Jaycord_ReplyOption").GetComponent<JaycordReplyOption>();
    }

    private static JaycordLinkCard BuildLinkCard()
    {
        var row = UI("Jaycord_LinkCard", null);
        H(row, 0, 0, 0, 0, 0, TextAnchor.UpperLeft);

        var card = UI("Card", row.transform);
        Glow(card, JaycordTheme.WithAlpha(JaycordTheme.Cyan, 0.18f));
        Fill(card, Rounded(4, 4, 4, 4), JaycordTheme.BgElevated);
        Border(card, Rounded(4, 4, 4, 4, 1f), JaycordTheme.Cyan);
        V(card, 16, 16, 16, 16, 12);
        LE(card, w: 320);

        var top = UI("Top", card.transform);
        H(top, 0, 0, 0, 0, 12, TextAnchor.MiddleLeft);

        var icon = UI("Icon", top.transform);
        LE(icon, w: 40, h: 40);
        Glow(icon, JaycordTheme.WithAlpha(JaycordTheme.Cyan, 0.3f));
        Fill(icon, Rounded(4, 4, 4, 4), JaycordTheme.WithAlpha(JaycordTheme.Cyan, 0.1f));
        Border(icon, Rounded(4, 4, 4, 4, 1f), JaycordTheme.Cyan);
        var star = UI("Sparkle", icon.transform);
        Center((RectTransform)star.transform, 20, 20);
        Img(star, iconSparkle, JaycordTheme.Cyan, type: Image.Type.Simple);

        var texts = UI("Text", top.transform);
        V(texts, 0, 0, 0, 0, 4);
        LE(texts, flexW: 1, minW: 0);
        var title = Text(texts.transform, "Title", "TRANSMIGRATION LINK", orbitron, 12, JaycordTheme.Cyan, 0.96f);
        var url = Text(texts.transform, "Url", "jx://transmigrate/0x7F3A", monoRegular, 11, JaycordTheme.TextMuted);

        var warning = UI("Warning", card.transform);
        Img(warning, Rounded(2, 2, 2, 2), JaycordTheme.WithAlpha(JaycordTheme.Yellow, 0.08f));
        Border(warning, Rounded(2, 2, 2, 2, 1f), JaycordTheme.WithAlpha(JaycordTheme.Yellow, 0.6f));
        V(warning, 10, 10, 8, 8, 0);
        Text(warning.transform, "Text", "!! SOURCE UNVERIFIED — proceed at your own risk", monoRegular, 11,
             JaycordTheme.Yellow, wrap: true);

        var buttonGo = UI("Button", card.transform);
        var buttonFill = Img(buttonGo, Rounded(2, 2, 2, 2), JaycordTheme.Cyan, raycast: true);
        Border(buttonGo, Rounded(2, 2, 2, 2, 1f), JaycordTheme.Cyan);
        H(buttonGo, 14, 14, 10, 10, 0, TextAnchor.MiddleCenter);
        var button = buttonGo.AddComponent<Button>();
        button.targetGraphic = buttonFill;
        var colors = button.colors;
        // The fill is cyan; ColorTint multiplies it, so it is invisible until hovered/pressed.
        colors.normalColor = new Color(1f, 1f, 1f, 0f);
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.25f);
        colors.selectedColor = new Color(1f, 1f, 1f, 0f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.45f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0f);
        button.colors = colors;
        var openLabel = Text(buttonGo.transform, "Label", "OPEN LINK  →", monoMedium, 12, JaycordTheme.Cyan, 1.2f,
                             TextAlignmentOptions.Center);

        var linkCard = row.AddComponent<JaycordLinkCard>();
        Set(linkCard, "title", title);
        Set(linkCard, "url", url);
        Set(linkCard, "warning", warning);
        Set(linkCard, "openButton", button);
        Set(linkCard, "openLabel", openLabel);
        return Save(row, "Jaycord_LinkCard").GetComponent<JaycordLinkCard>();
    }

    private static JaycordTypingIndicator BuildTypingIndicator(Transform parent)
    {
        var root = UI("TypingIndicator", parent);
        V(root, 0, 0, 0, 0, 6, TextAnchor.UpperLeft, expandWidth: false);

        var bubble = UI("Bubble", root.transform);
        LE(bubble, w: 70, h: 38);
        Img(bubble, Rounded(2, 12, 12, 12), JaycordTheme.BgElevated);
        Border(bubble, Rounded(2, 12, 12, 12, 1f), JaycordTheme.Magenta);
        H(bubble, 0, 0, 0, 0, 6, TextAnchor.MiddleCenter);

        var dots = new Object[3];
        for (int i = 0; i < 3; i++)
        {
            var dot = UI($"Dot{i + 1}", bubble.transform);
            LE(dot, w: 8, h: 8);
            dots[i] = Img(dot, circleSprite, JaycordTheme.Offline, type: Image.Type.Simple);
        }

        var label = Text(root.transform, "Label", "MAX IS TYPING…", monoRegular, 10, JaycordTheme.TextMuted, 1f);

        var indicator = root.AddComponent<JaycordTypingIndicator>();
        SetArray(indicator, "dots", dots);
        Set(indicator, "label", label);
        root.SetActive(false);
        return indicator;
    }

    private static JaycordContactRow BuildContactRow()
    {
        var root = UI("Jaycord_ContactRow", null);
        LE(root, h: 88);
        var background = Img(root, null, new Color(0, 0, 0, 0), raycast: true); // receives clicks / hover
        var accent = UI("SelectedAccent", root.transform);
        IgnoreLayout(accent);
        var accentRt = (RectTransform)accent.transform;
        accentRt.anchorMin = new Vector2(0, 0);
        accentRt.anchorMax = new Vector2(0, 1);
        accentRt.pivot = new Vector2(0, 0.5f);
        accentRt.sizeDelta = new Vector2(3, 0);
        accentRt.anchoredPosition = Vector2.zero;
        var accentImage = Img(accent, null, JaycordTheme.Cyan);
        H(root, 16, 16, 12, 12, 16, TextAnchor.MiddleLeft);

        var avatar = Avatar(root.transform, 64, 10, 1.5f, out var avatarImage);
        var dot = PresenceDot(avatar.transform, new Vector2(45.5f, -45.5f), 14, out var dotImage);

        var texts = UI("Text", root.transform);
        V(texts, 0, 0, 0, 0, 0);
        LE(texts, flexW: 1, minW: 0);
        var name = Text(texts.transform, "Name", "Max", chakraSemiBold, 20, JaycordTheme.TextPrimary);
        var preview = Text(texts.transform, "Preview", "", chakraRegular, 15, JaycordTheme.TextMuted);
        preview.overflowMode = TextOverflowModes.Ellipsis;
        name.overflowMode = TextOverflowModes.Ellipsis;

        var badge = UI("UnreadBadge", root.transform);
        Glow(badge, JaycordTheme.WithAlpha(JaycordTheme.Magenta, 0.45f));
        Fill(badge, Rounded(2, 2, 2, 2), JaycordTheme.Magenta);
        H(badge, 8, 8, 3, 3, 0, TextAnchor.MiddleCenter);
        var badgeText = Text(badge.transform, "Count", "3", monoMedium, 12, JaycordTheme.TextOnAccent, align: TextAlignmentOptions.Center);

        var rowView = root.AddComponent<JaycordContactRow>();
        Set(rowView, "background", background);
        Set(rowView, "selectedAccent", accentImage);
        Set(rowView, "avatar", avatarImage);
        Set(rowView, "presenceDot", dotImage);
        Set(rowView, "nameText", name);
        Set(rowView, "previewText", preview);
        Set(rowView, "unreadBadge", badge);
        Set(rowView, "unreadText", badgeText);
        return Save(root, "Jaycord_ContactRow").GetComponent<JaycordContactRow>();
    }

    private static JaycordDossierRow BuildDossierRow()
    {
        var root = UI("Jaycord_DossierRow", null);
        H(root, 0, 0, 6, 6, 8, TextAnchor.MiddleLeft);
        LE(root, h: 28);
        BottomLine(root, JaycordTheme.Border);
        var label = Text(root.transform, "Label", "FACTION", monoRegular, 11, JaycordTheme.TextMuted, 1.1f);
        LE(label.gameObject, flexW: 1);
        var value = Text(root.transform, "Value", "A", chakraMedium, 14, JaycordTheme.TextPrimary, align: TextAlignmentOptions.Right);
        var view = root.AddComponent<JaycordDossierRow>();
        Set(view, "label", label);
        Set(view, "value", value);
        return Save(root, "Jaycord_DossierRow").GetComponent<JaycordDossierRow>();
    }

    private static GameObject BuildTag()
    {
        var root = UI("Jaycord_Tag", null);
        Img(root, Rounded(2, 2, 2, 2), JaycordTheme.WithAlpha(JaycordTheme.Magenta, 0.12f));
        Border(root, Rounded(2, 2, 2, 2, 1f), JaycordTheme.Magenta);
        H(root, 8, 8, 3, 3, 0, TextAnchor.MiddleCenter);
        Text(root.transform, "Text", "#tag", monoRegular, 11, JaycordTheme.Magenta);
        return Save(root, "Jaycord_Tag");
    }

    // ==================================================================
    // The app (Figma "Chat – Cyberpunk", 1512 × 960)
    // ==================================================================

    private static GameObject BuildApp(JaycordMessageView self, JaycordMessageView other, JaycordMessageView system,
        JaycordDivider divider, JaycordReplyOption reply, JaycordLinkCard link,
        JaycordContactRow rowPrefab, JaycordDossierRow dossierRowPrefab, GameObject tagPrefab)
    {
        var root = UI("JaycordApp", null);
        ((RectTransform)root.transform).sizeDelta = new Vector2(1512, 960);
        Img(root, null, JaycordTheme.BgBase, raycast: true);

        BuildServerRail(root.transform);
        var contactList = BuildListPanel(root.transform);
        var conversation = BuildConversation(root.transform, out var headerAvatar, out var headerName, out var headerStatus,
            out var scroll, out var content, out var typing, out var inputBar, out var inputText, out var inputHint,
            out var replyPanel, out var replyOptions);
        var profile = BuildProfile(root.transform, dossierRowPrefab, tagPrefab);

        // ---- App shell (contacts, header, profile) ----
        var app = root.AddComponent<JaycordApp>();
        Set(app, "contactList", contactList);
        Set(app, "contactRowPrefab", rowPrefab);
        Set(app, "headerAvatar", headerAvatar);
        Set(app, "headerName", headerName);
        Set(app, "headerStatus", headerStatus);
        Set(app, "profile", profile);
        app.contacts = DefaultContacts();
        app.startContact = "Max";
        app.BuildList();          // so the prefab already shows the list in the editor
        app.Select("Max");

        // ---- Yarn presenter ----
        var presenter = root.AddComponent<JaycordPresenter>();
        Set(presenter, "app", app);
        Set(presenter, "scrollRect", scroll);
        Set(presenter, "messageContainer", content);
        Set(presenter, "typingIndicator", typing);
        Set(presenter, "selfMessagePrefab", self);
        Set(presenter, "otherMessagePrefab", other);
        Set(presenter, "systemMessagePrefab", system);
        Set(presenter, "dividerPrefab", divider);
        Set(presenter, "linkCardPrefab", link);
        Set(presenter, "replyOptionPrefab", reply);
        Set(presenter, "inputBar", inputBar);
        Set(presenter, "inputText", inputText);
        Set(presenter, "inputHint", inputHint);
        Set(presenter, "replyPanel", replyPanel);
        Set(presenter, "replyContainer", replyOptions);

        return Save(root, "JaycordApp");
    }

    private static List<JaycordContact> DefaultContacts()
    {
        Sprite LoadAvatar(string file) => AssetDatabase.LoadAssetAtPath<Sprite>($"{AvatarDir}/{file}");
        return new List<JaycordContact>
        {
            new JaycordContact
            {
                name = "Max", avatar = LoadAvatar("avatar_max.png"), presence = JaycordPresence.Online,
                status = "Active 20m ago", handle = "@max.vx  ·  ID 0x7F3A", preview = "",
                level = "LV.07", trust = 62, tags = new[] { "informant", "hacker", "owes_you" },
                dossier = new[]
                {
                    new JaycordDossierField { label = "FACTION", value = "A" },
                    new JaycordDossierField { label = "ROLE", value = "A" },
                    new JaycordDossierField { label = "DISTRICT", value = "Sector 9" },
                    new JaycordDossierField { label = "STATUS", value = "Unverified", highlight = true },
                },
            },
            new JaycordContact { name = "MCNPC_A", avatar = LoadAvatar("avatar_mcnpc_a.png"), presence = JaycordPresence.Online,
                                 status = "Online", handle = "", preview = "Let’s go", unread = 3,
                                 history = History(("Leo", "you free tonight?"), ("MCNPC_A", "yeah"), ("MCNPC_A", "where"), ("MCNPC_A", "Let’s go")) },
            new JaycordContact { name = "MCNPC_B", avatar = LoadAvatar("avatar_mcnpc_b.png"), presence = JaycordPresence.Idle,
                                 status = "Idle", handle = "", preview = "Trueeeeee",
                                 history = History(("Leo", "this city never sleeps huh"), ("MCNPC_B", "Trueeeeee")) },
            new JaycordContact { name = "MCNPC_C", avatar = LoadAvatar("avatar_mcnpc_c.png"), presence = JaycordPresence.Offline,
                                 status = "Offline", handle = "", preview = "lol yeah, are you coming to the lunch on the 13th?", unread = 12,
                                 history = History(("MCNPC_C", "lol yeah, are you coming to the lunch on the 13th?")) },
        };
    }

    private static List<JaycordRecord> History(params (string sender, string text)[] lines) =>
        lines.Select(l => new JaycordRecord { kind = JaycordRecord.Kind.Message, sender = l.sender, text = l.text }).ToList();

    // ---- Server rail (72 px) -------------------------------------------
    private static void BuildServerRail(Transform parent)
    {
        var rail = UI("ServerRail", parent);
        var rt = (RectTransform)rail.transform;
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 0.5f);
        rt.sizeDelta = new Vector2(72, 0);
        rt.anchoredPosition = Vector2.zero;
        Img(rail, null, JaycordTheme.BgBase);
        RightLine(rail, JaycordTheme.Border);
        V(rail, 12, 12, 20, 20, 12, TextAnchor.UpperCenter, expandWidth: false);

        ServerItem(rail.transform, "J", ServerStyle.Gradient, false);
        var divider = UI("Divider", rail.transform);
        LE(divider, w: 32, h: 2);
        Img(divider, null, JaycordTheme.Border);
        ServerItem(rail.transform, "N7", ServerStyle.Selected, false);
        ServerItem(rail.transform, "VX", ServerStyle.Normal, true);
        ServerItem(rail.transform, "Ø", ServerStyle.Normal, false);
        ServerItem(rail.transform, "+", ServerStyle.Dashed, false);

        var active = UI("ActiveIndicator", rail.transform);
        IgnoreLayout(active);
        TopLeft((RectTransform)active.transform, 0, 104, 4, 28);
        Glow(active, JaycordTheme.WithAlpha(JaycordTheme.Cyan, 0.6f));
        Fill(active, null, JaycordTheme.Cyan);
    }

    private static void ServerItem(Transform parent, string label, ServerStyle style, bool unread)
    {
        var item = UI($"Server {label}", parent);
        LE(item, w: 48, h: 48);
        Color textColor = JaycordTheme.TextPrimary;
        switch (style)
        {
            case ServerStyle.Gradient:
                Glow(item, JaycordTheme.WithAlpha(JaycordTheme.Magenta, 0.4f));
                Fill(item, gradientServer, Color.white, type: Image.Type.Simple);
                textColor = JaycordTheme.TextOnAccent;
                break;
            case ServerStyle.Selected:
                Glow(item, JaycordTheme.WithAlpha(JaycordTheme.Cyan, 0.35f));
                Fill(item, Rounded(4, 4, 4, 4), JaycordTheme.BgElevated);
                Border(item, Rounded(4, 4, 4, 4, 1.5f), JaycordTheme.Cyan);
                break;
            case ServerStyle.Normal:
                Img(item, Rounded(12, 12, 12, 12), JaycordTheme.BgElevated);
                break;
            case ServerStyle.Dashed:
                Img(item, dashedServer, JaycordTheme.TextMuted, type: Image.Type.Simple);
                break;
        }
        var text = Text(item.transform, "Label", label, orbitron, 15, textColor, align: TextAlignmentOptions.Center);
        Stretch((RectTransform)text.transform);

        if (unread)
        {
            var dot = UI("Unread", item.transform);
            TopLeft((RectTransform)dot.transform, 39, -3, 12, 12);
            Img(dot, circleSprite, JaycordTheme.BgBase, type: Image.Type.Simple);
            var inner = UI("Dot", dot.transform);
            Center((RectTransform)inner.transform, 8, 8);
            Img(inner, circleSprite, JaycordTheme.Magenta, type: Image.Type.Simple);
        }
    }

    // ---- Chat list (400 px) --------------------------------------------
    private static RectTransform BuildListPanel(Transform parent)
    {
        var panel = UI("ChatList", parent);
        var rt = (RectTransform)panel.transform;
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 0.5f);
        rt.sizeDelta = new Vector2(400, 0);
        rt.anchoredPosition = new Vector2(72, 0);
        Img(panel, null, JaycordTheme.BgPanel);
        RightLine(panel, JaycordTheme.Border);

        var title = Text(panel.transform, "Title", "JAYCORD", orbitron, 22, JaycordTheme.Cyan, 2.2f);
        if (orbitronGlow != null) title.fontSharedMaterial = orbitronGlow;
        TopLeft((RectTransform)title.transform, 24, 23, 340, 33);

        var search = UI("Search", panel.transform);
        TopStretch((RectTransform)search.transform, 24, 24, 70, 40);
        Img(search, Rounded(4, 4, 4, 4), JaycordTheme.BgElevated);
        Border(search, Rounded(4, 4, 4, 4, 1f), JaycordTheme.Border);
        H(search, 12, 16, 8, 8, 12, TextAnchor.MiddleLeft);
        Icon(search.transform, "SearchIcon", iconSearch, JaycordTheme.TextMuted);
        var searchText = Text(search.transform, "Label", "// search chats", monoRegular, 14, JaycordTheme.TextMuted);
        LE(searchText.gameObject, flexW: 1);

        var list = UI("Conversations", panel.transform);
        var listRt = (RectTransform)list.transform;
        listRt.anchorMin = Vector2.zero;
        listRt.anchorMax = Vector2.one;
        listRt.offsetMin = Vector2.zero;
        listRt.offsetMax = new Vector2(0, -134);
        V(list, 0, 0, 0, 0, 0);
        return listRt;
    }

    // ---- Conversation (center) -----------------------------------------
    private static GameObject BuildConversation(Transform parent,
        out Image headerAvatar, out TextMeshProUGUI headerName, out TextMeshProUGUI headerStatus,
        out ScrollRect scroll, out RectTransform content, out JaycordTypingIndicator typing,
        out GameObject inputBar, out TextMeshProUGUI inputText, out TextMeshProUGUI inputHint,
        out GameObject replyPanel, out RectTransform replyOptions)
    {
        var conv = UI("Conversation", parent);
        var rt = (RectTransform)conv.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(472, 0);
        rt.offsetMax = new Vector2(-256, 0);
        Img(conv, null, JaycordTheme.BgBase);
        V(conv, 0, 0, 0, 0, 0);

        // Header (80 px)
        var header = UI("Header", conv.transform);
        LE(header, h: 80);
        Img(header, null, JaycordTheme.BgPanel);
        BottomLine(header, JaycordTheme.Border);
        H(header, 24, 24, 16, 16, 16, TextAnchor.MiddleLeft);
        Avatar(header.transform, 48, 10, 1.5f, out headerAvatar);
        var headerTexts = UI("Text", header.transform);
        V(headerTexts, 0, 0, 0, 0, 0);
        LE(headerTexts, flexW: 1, minW: 0);
        headerName = Text(headerTexts.transform, "Name", "Max", chakraMedium, 16, JaycordTheme.TextPrimary);
        headerStatus = Text(headerTexts.transform, "Status", "ACTIVE 20M AGO", monoRegular, 13, JaycordTheme.Cyan, upper: true);
        var icons = UI("Icons", header.transform);
        H(icons, 0, 0, 0, 0, 24, TextAnchor.MiddleRight);
        Icon(icons.transform, "Phone", iconPhone, Color.white);
        Icon(icons.transform, "Video", iconVideo, Color.white);

        // Messages
        var messages = UI("Messages", conv.transform);
        LE(messages, flexH: 1, minH: 120);
        Img(messages, null, new Color(0, 0, 0, 0), raycast: true); // catches the mouse wheel
        scroll = messages.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30;
        var viewport = UI("Viewport", messages.transform);
        Stretch((RectTransform)viewport.transform);
        viewport.AddComponent<RectMask2D>();
        var contentGo = UI("Content", viewport.transform);
        content = (RectTransform)contentGo.transform;
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.sizeDelta = Vector2.zero;
        V(contentGo, 24, 24, 24, 16, 8);
        contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = (RectTransform)viewport.transform;
        scroll.content = content;
        typing = BuildTypingIndicator(contentGo.transform);

        // Bottom: input bar, replaced by the reply choices while the player chooses
        var bottom = UI("Bottom", conv.transform);
        V(bottom, 24, 24, 16, 24, 0);

        inputBar = UI("InputBar", bottom.transform);
        LE(inputBar, h: 40);
        Glow(inputBar, JaycordTheme.WithAlpha(JaycordTheme.Cyan, 0.15f));
        Fill(inputBar, Rounded(4, 4, 4, 4), JaycordTheme.BgElevated);
        Border(inputBar, Rounded(4, 4, 4, 4, 1f), JaycordTheme.Cyan);
        H(inputBar, 16, 16, 8, 8, 16, TextAnchor.MiddleLeft);
        inputText = Text(inputBar.transform, "Text", "> enter message_", monoRegular, 14, JaycordTheme.TextMuted);
        inputText.overflowMode = TextOverflowModes.Ellipsis;
        LE(inputText.gameObject, flexW: 1, minW: 0);
        inputHint = Text(inputBar.transform, "Hint", "", monoMedium, 11, JaycordTheme.TextMuted, 1.1f, TextAlignmentOptions.Right);
        Icon(inputBar.transform, "Mic", iconMic, Color.white);
        Icon(inputBar.transform, "Smile", iconSmile, Color.white);
        Icon(inputBar.transform, "Image", iconImage, Color.white);

        replyPanel = UI("ReplyChoices", bottom.transform);
        Img(replyPanel, Rounded(4, 4, 4, 4), JaycordTheme.BgPanel);
        Border(replyPanel, Rounded(4, 4, 4, 4, 1f), JaycordTheme.Border);
        V(replyPanel, 16, 16, 16, 16, 8);
        var replyHeader = UI("Header", replyPanel.transform);
        H(replyHeader, 0, 0, 0, 4, 8, TextAnchor.MiddleLeft);
        var choose = Text(replyHeader.transform, "Label", "// CHOOSE RESPONSE", monoMedium, 11, JaycordTheme.Cyan, 1.32f);
        LE(choose.gameObject, flexW: 1);
        var optionsGo = UI("Options", replyPanel.transform);
        V(optionsGo, 0, 0, 0, 0, 8);
        replyOptions = (RectTransform)optionsGo.transform;
        replyPanel.SetActive(false);

        return conv;
    }

    // ---- Profile (256 px) ----------------------------------------------
    private static JaycordProfilePanel BuildProfile(Transform parent, JaycordDossierRow dossierRowPrefab, GameObject tagPrefab)
    {
        var panel = UI("Profile", parent);
        var rt = (RectTransform)panel.transform;
        rt.anchorMin = new Vector2(1, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 0.5f);
        rt.sizeDelta = new Vector2(256, 0);
        rt.anchoredPosition = Vector2.zero;
        Img(panel, null, JaycordTheme.BgPanel);
        LeftLine(panel, JaycordTheme.Border);
        V(panel, 8, 8, 24, 16, 0, TextAnchor.UpperCenter);

        // Person
        var person = UI("Person", panel.transform);
        V(person, 16, 16, 0, 0, 24, TextAnchor.UpperCenter, expandWidth: false);
        Avatar(person.transform, 128, 16, 1.5f, out var avatar);
        var personTexts = UI("Text", person.transform);
        V(personTexts, 0, 0, 0, 0, 0, TextAnchor.UpperCenter);
        LE(personTexts, w: 208);
        var name = Text(personTexts.transform, "Name", "Max", chakraSemiBold, 20, JaycordTheme.TextPrimary, align: TextAlignmentOptions.Center);
        var status = Text(personTexts.transform, "Status", "ACTIVE 20M AGO", monoRegular, 13, JaycordTheme.Cyan, align: TextAlignmentOptions.Center, upper: true);
        var handle = Text(personTexts.transform, "Handle", "@max", monoRegular, 11, JaycordTheme.TextMuted, align: TextAlignmentOptions.Center);

        Spacer(panel.transform, 24);

        // Dossier
        var dossierWrap = UI("DossierWrap", panel.transform);
        V(dossierWrap, 7, 9, 0, 0, 0);
        var dossier = UI("Dossier", dossierWrap.transform);
        Img(dossier, Rounded(4, 4, 4, 4), JaycordTheme.BgElevated);
        Border(dossier, Rounded(4, 4, 4, 4, 1f), JaycordTheme.Cyan);
        V(dossier, 16, 16, 16, 16, 4);

        var dossierHeader = UI("Header", dossier.transform);
        H(dossierHeader, 0, 0, 0, 6, 8, TextAnchor.MiddleLeft);
        var dossierTitle = Text(dossierHeader.transform, "Title", "// DOSSIER", monoMedium, 11, JaycordTheme.Cyan, 1.32f);
        LE(dossierTitle.gameObject, flexW: 1);
        var level = Text(dossierHeader.transform, "Level", "LV.07", monoRegular, 11, JaycordTheme.TextMuted);

        var rows = UI("Rows", dossier.transform);
        V(rows, 0, 0, 0, 0, 4);

        var trust = UI("Trust", dossier.transform);
        V(trust, 0, 0, 12, 0, 8);
        var trustLabel = UI("Label", trust.transform);
        H(trustLabel, 0, 0, 0, 0, 8, TextAnchor.MiddleLeft);
        var trustTitle = Text(trustLabel.transform, "Title", "TRUST", monoRegular, 11, JaycordTheme.TextMuted, 1.1f);
        LE(trustTitle.gameObject, flexW: 1);
        var trustValue = Text(trustLabel.transform, "Value", "62%", monoMedium, 11, JaycordTheme.Magenta);
        var bar = UI("Bar", trust.transform);
        H(bar, 0, 0, 0, 0, 3, TextAnchor.MiddleLeft, expandWidth: true);
        var segments = new Object[10];
        for (int i = 0; i < segments.Length; i++)
        {
            var seg = UI($"Seg{i + 1}", bar.transform);
            LE(seg, h: 8, flexW: 1);
            segments[i] = Img(seg, null, JaycordTheme.Border);
        }

        var tags = UI("Tags", dossier.transform);
        var flow = tags.AddComponent<JaycordFlowLayout>();
        flow.padding = new RectOffset(0, 0, 12, 0);

        Spacer(panel.transform, 18);

        // Actions
        var actionsLabelWrap = UI("ActionsLabel", panel.transform);
        H(actionsLabelWrap, 15, 0, 0, 0, 0, TextAnchor.MiddleLeft);
        Text(actionsLabelWrap.transform, "Label", "// ACTIONS", monoMedium, 11, JaycordTheme.TextMuted, 1.32f);
        Spacer(panel.transform, 7);
        var menu = UI("Menu", panel.transform);
        V(menu, 0, 0, 0, 0, 0);
        MenuItem(menu.transform, iconMore, "More options", JaycordTheme.TextPrimary, 40);
        MenuItem(menu.transform, iconSearch, "files history", JaycordTheme.TextPrimary, 40);
        MenuItem(menu.transform, null, "transmigration link", JaycordTheme.Cyan, 35);

        var view = panel.AddComponent<JaycordProfilePanel>();
        Set(view, "avatar", avatar);
        Set(view, "nameText", name);
        Set(view, "statusText", status);
        Set(view, "handleText", handle);
        Set(view, "levelText", level);
        Set(view, "dossierRows", rows.transform);
        Set(view, "dossierRowPrefab", dossierRowPrefab);
        Set(view, "trustText", trustValue);
        SetArray(view, "trustSegments", segments);
        Set(view, "tagContainer", tags.transform);
        Set(view, "tagPrefab", tagPrefab);
        return view;
    }

    private static void MenuItem(Transform parent, Sprite icon, string label, Color color, float height)
    {
        var item = UI(label, parent);
        LE(item, h: height);
        H(item, 16, 16, 0, 0, 16, TextAnchor.MiddleLeft);
        if (icon != null) Icon(item.transform, "Icon", icon, JaycordTheme.Cyan);
        var text = Text(item.transform, "Label", label, chakraMedium, 16, color);
        text.overflowMode = TextOverflowModes.Ellipsis;
        LE(text.gameObject, flexW: 1, minW: 0);
    }

    // ==================================================================
    // Test scene
    // ==================================================================

    private static void BuildTestScene(UnityEngine.SceneManagement.Scene scene, GameObject appPrefab)
    {
        var cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        var camera = cameraGo.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = JaycordTheme.BgBase;
        camera.orthographic = true;

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();

        var canvasGo = new GameObject("Canvas", typeof(RectTransform));
        canvasGo.layer = 5;
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1512, 960);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand; // whole app always visible
        canvasGo.AddComponent<GraphicRaycaster>();

        var background = UI("Background", canvasGo.transform);
        Img(background, null, JaycordTheme.BgBase);
        Stretch((RectTransform)background.transform);

        var appGo = (GameObject)PrefabUtility.InstantiatePrefab(appPrefab, canvasGo.transform);
        Center((RectTransform)appGo.transform, 1512, 960);
        var presenter = appGo.GetComponent<JaycordPresenter>();

        var runnerGo = new GameObject("Jaycord Dialogue Runner");
        var storage = runnerGo.AddComponent<InMemoryVariableStorage>();
        var runner = runnerGo.AddComponent<DialogueRunner>();
        var commands = runnerGo.AddComponent<JaycordCommands>();

        var project = AssetDatabase.LoadAllAssetsAtPath(YarnProjectPath).OfType<YarnProject>().FirstOrDefault();
        if (project == null)
            Debug.LogWarning($"[Jaycord] Could not find a YarnProject at {YarnProjectPath}. Assign it on the DialogueRunner by hand.");

        var so = new SerializedObject(runner);
        so.FindProperty("yarnProject").objectReferenceValue = project;
        so.FindProperty("variableStorage").objectReferenceValue = storage;
        var presenters = so.FindProperty("dialoguePresenters");
        presenters.arraySize = 1;
        presenters.GetArrayElementAtIndex(0).objectReferenceValue = presenter;
        so.FindProperty("autoStart").boolValue = true;
        so.FindProperty("startNode").stringValue = TestNode;
        so.ApplyModifiedPropertiesWithoutUndo();

        Set(commands, "runner", runner);
        Set(commands, "presenter", presenter);

        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    // ==================================================================
    // Fonts
    // ==================================================================

    private static void LoadFonts()
    {
        chakraRegular = FontAsset("ChakraPetch-Regular");
        chakraMedium = FontAsset("ChakraPetch-Medium");
        chakraSemiBold = FontAsset("ChakraPetch-SemiBold");
        monoRegular = FontAsset("JetBrainsMono-Regular");
        monoMedium = FontAsset("JetBrainsMono-Medium");
        orbitron = FontAsset("Orbitron-Bold");

        // Glowing title material (Figma: text-shadow 0 0 12px cyan 60%)
        orbitronGlow = null;
        if (orbitron != null && orbitron.material != null)
        {
            string path = $"{FontDir}/Orbitron-Bold SDF Glow.mat";
            AssetDatabase.DeleteAsset(path);
            var mat = new Material(orbitron.material) { name = "Orbitron-Bold SDF Glow" };
            mat.EnableKeyword("GLOW_ON");
            mat.SetColor("_GlowColor", JaycordTheme.WithAlpha(JaycordTheme.Cyan, 0.6f));
            mat.SetFloat("_GlowOuter", 0.6f);
            mat.SetFloat("_GlowPower", 0.5f);
            AssetDatabase.CreateAsset(mat, path);
            orbitronGlow = mat;
        }
    }

    /// <summary>Finds "&lt;name&gt; SDF.asset" next to the .ttf, creating it with TextMeshPro if needed.</summary>
    private static TMP_FontAsset FontAsset(string fileName)
    {
        string ttf = $"{FontDir}/{fileName}.ttf";
        string assetPath = $"{FontDir}/{fileName} SDF.asset";

        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null) return existing;

        var font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
        if (font == null)
        {
            Debug.LogWarning($"[Jaycord] Font not found: {ttf}. Using the default TMP font.");
            return TMP_Settings.defaultFontAsset;
        }

        // Same as right-clicking the .ttf ▸ Create ▸ TextMeshPro ▸ Font Asset ▸ SDF
        var previous = Selection.objects;
        Selection.objects = new Object[] { font };
        EditorApplication.ExecuteMenuItem("Assets/Create/TextMeshPro/Font Asset/SDF");
        Selection.objects = previous;
        AssetDatabase.SaveAssets();

        var created = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (created == null)
        {
            Debug.LogWarning($"[Jaycord] Could not create a TMP font for {ttf}. Right-click it ▸ Create ▸ TextMeshPro ▸ Font Asset ▸ SDF, then run the builder again.");
            return TMP_Settings.defaultFontAsset;
        }
        return created;
    }

    // ==================================================================
    // Sprites (drawn here so the corners/glows match Figma exactly)
    // ==================================================================

    private static void MakeSprites()
    {
        glowSprite = Paint("glow", 52, 52, 25, (x, y) =>
        {
            float d = SdRoundRect(x - 26, y - 26, 26 - GlowSpread, 26 - GlowSpread, 8, 8, 8, 8);
            float a = d <= 0 ? 1f : Mathf.Exp(-(d * d) / (2f * 5f * 5f));
            return new Color(1, 1, 1, a);
        });

        circleSprite = Paint("circle", 64, 64, 0, (x, y) =>
            new Color(1, 1, 1, Mathf.Clamp01(0.5f - (new Vector2(x - 32, y - 32).magnitude - 31.5f))));

        // 16 px tile, 2 px corners, 1 px dashed stroke (4 on / 4 off), used with Image.Type.Tiled
        dashedSmall = Paint("dashed_r2", 16, 16, 4, (x, y) =>
        {
            float d = SdRoundRect(x - 8, y - 8, 8, 8, 2, 2, 2, 2);
            float a = Mathf.Clamp01(0.5f - d) - Mathf.Clamp01(0.5f - (d + 1f));
            bool horizontalEdge = x >= 4 && x < 12;
            bool verticalEdge = y >= 4 && y < 12;
            if (horizontalEdge && ((int)(x - 4) % 8) >= 4) a = 0;
            if (verticalEdge && ((int)(y - 4) % 8) >= 4) a = 0;
            return new Color(1, 1, 1, a);
        }, fullRect: true);

        // 48 px "+" server button: dashed 1.5 px border, 12 px corners
        dashedServer = Paint("dashed_server", 48, 48, 0, (x, y) =>
        {
            float d = SdRoundRect(x - 24, y - 24, 24, 24, 12, 12, 12, 12);
            float a = Mathf.Clamp01(0.5f - d) - Mathf.Clamp01(0.5f - (d + 1.5f));
            float s = PerimeterParam(Mathf.Abs(x - 24), Mathf.Abs(y - 24), 24, 24, 12);
            if (s % 8f >= 4f) a = 0;
            return new Color(1, 1, 1, a);
        });

        // 48 px "J" server: 135° gradient magenta → cyan (cyan reached at 71%)
        gradientServer = Paint("gradient_server", 48, 48, 0, (x, y) =>
        {
            float d = SdRoundRect(x - 24, y - 24, 24, 24, 12, 12, 12, 12);
            float t = Mathf.Clamp01(((x / 48f) + (1f - y / 48f)) / 2f / 0.714f);
            var c = Color.Lerp(JaycordTheme.Magenta, JaycordTheme.Cyan, t);
            c.a = Mathf.Clamp01(0.5f - d);
            return c;
        });

        // Simple line icons, drawn at 48 px and shown at 24 px (white, tinted by Image.color)
        iconSearch = Paint("icon_search", 48, 48, 0, (x, y) =>
        {
            float ring = Mathf.Abs(new Vector2(x - 22, y - 26).magnitude - 16f) - 2f;
            float handle = SegmentDistance(new Vector2(x, y), new Vector2(33.3f, 14.7f), new Vector2(42, 6)) - 2f;
            return new Color(1, 1, 1, Mathf.Clamp01(0.5f - Mathf.Min(ring, handle)));
        });
        iconMore = Paint("icon_more", 48, 48, 0, (x, y) =>
        {
            float d = Mathf.Min(new Vector2(x - 10, y - 24).magnitude,
                      Mathf.Min(new Vector2(x - 24, y - 24).magnitude, new Vector2(x - 38, y - 24).magnitude)) - 3.5f;
            return new Color(1, 1, 1, Mathf.Clamp01(0.5f - d));
        });
        iconSparkle = Paint("icon_sparkle", 48, 48, 0, (x, y) =>
        {
            // 4-point star (astroid), 4×4 supersampled
            int inside = 0;
            for (int sy = 0; sy < 4; sy++)
            for (int sx = 0; sx < 4; sx++)
            {
                float px = Mathf.Abs(x - 0.5f + (sx + 0.5f) / 4f - 24f) / 22f;
                float py = Mathf.Abs(y - 0.5f + (sy + 0.5f) / 4f - 24f) / 22f;
                if (Mathf.Pow(px, 0.5f) + Mathf.Pow(py, 0.5f) <= 1f) inside++;
            }
            return new Color(1, 1, 1, inside / 16f);
        });
    }

    /// <summary>Rounded rectangle sprite, filled (stroke = 0) or outline. Radii: top-left, top-right, bottom-left, bottom-right.</summary>
    private static Sprite Rounded(float tl, float tr, float bl, float br, float stroke = 0f)
    {
        string name = $"rect_{tl}_{tr}_{bl}_{br}_s{stroke}".Replace('.', 'p');
        if (spriteCache.TryGetValue(name, out var cached)) return cached;

        float maxR = Mathf.Max(Mathf.Max(tl, tr), Mathf.Max(bl, br));
        int slice = Mathf.CeilToInt(maxR) + 2;
        int size = slice * 2 + 4;
        float half = size / 2f;

        var sprite = Paint(name, size, size, slice, (x, y) =>
        {
            float d = SdRoundRect(x - half, y - half, half, half, tl, tr, bl, br);
            float a = Mathf.Clamp01(0.5f - d);
            if (stroke > 0f) a -= Mathf.Clamp01(0.5f - (d + stroke));
            return new Color(1, 1, 1, a);
        });
        spriteCache[name] = sprite;
        return sprite;
    }

    /// <summary>Signed distance to a rounded box centred at 0 (y up). Negative = inside.</summary>
    private static float SdRoundRect(float px, float py, float hx, float hy, float tl, float tr, float bl, float br)
    {
        float r = px < 0 ? (py > 0 ? tl : bl) : (py > 0 ? tr : br);
        float qx = Mathf.Abs(px) - hx + r;
        float qy = Mathf.Abs(py) - hy + r;
        return Mathf.Min(Mathf.Max(qx, qy), 0f) + new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude - r;
    }

    /// <summary>Distance travelled along a rounded-rect outline in one quadrant (for dashes).</summary>
    private static float PerimeterParam(float qx, float qy, float hx, float hy, float r)
    {
        float straightTop = hx - r;
        float arc = Mathf.PI * r / 2f;
        if (qx <= hx - r) return qx;
        if (qy <= hy - r) return straightTop + arc + (hy - r - qy);
        float angle = Mathf.Atan2(qy - (hy - r), qx - (hx - r));
        return straightTop + (Mathf.PI / 2f - angle) * r;
    }

    private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return (p - (a + ab * t)).magnitude;
    }

    /// <summary>Draws a texture pixel by pixel (coords are pixel centres, y up), saves it and imports it as a sprite.</summary>
    private static Sprite Paint(string name, int width, int height, int sliceBorder, Func<float, float, Color> pixel, bool fullRect = false)
    {
        string path = $"{GenDir}/{name}.png";
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                texture.SetPixel(x, y, pixel(x + 0.5f, y + 0.5f));
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        return ImportSprite(path, sliceBorder, fullRect);
    }

    private static Sprite ImportSprite(string path, int sliceBorder, bool fullRect)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null) return null;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = fullRect || sliceBorder > 0 ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
        importer.SetTextureSettings(settings);
        importer.spriteBorder = new Vector4(sliceBorder, sliceBorder, sliceBorder, sliceBorder);
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    /// <summary>Icons exported from Figma and the placeholder avatars.</summary>
    private static void LoadImportedArt()
    {
        iconMic = ImportSprite($"{IconDir}/icon_mic.png", 0, false);
        iconSmile = ImportSprite($"{IconDir}/icon_smile.png", 0, false);
        iconImage = ImportSprite($"{IconDir}/icon_image.png", 0, false);
        iconPhone = ImportSprite($"{IconDir}/icon_phone.png", 0, false);
        iconVideo = ImportSprite($"{IconDir}/icon_video.png", 0, false);
        foreach (var file in new[] { "avatar_max.png", "avatar_mcnpc_a.png", "avatar_mcnpc_b.png", "avatar_mcnpc_c.png" })
            ImportSprite($"{AvatarDir}/{file}", 0, false);
    }

    private static void CleanupOldVersion()
    {
        // First version of the builder made a single "JaycordWindow" and three sprites at the art root.
        foreach (var path in new[]
                 {
                     $"{PrefabDir}/JaycordWindow.prefab", $"{PrefabDir}/Jaycord_Divider.prefab",
                     $"{ArtDir}/jaycord_rounded_fill.png", $"{ArtDir}/jaycord_rounded_border.png", $"{ArtDir}/jaycord_circle.png",
                 })
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null) AssetDatabase.DeleteAsset(path);
        }
    }

    // ==================================================================
    // UI helpers
    // ==================================================================

    private static GameObject UI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    private static Image Img(GameObject go, Sprite sprite, Color color, bool raycast = false, Image.Type type = Image.Type.Sliced)
    {
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.type = sprite != null ? type : Image.Type.Simple;
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }

    /// <summary>
    /// Background as its own child layer. Needed whenever there is a Glow: in Unity UI a child
    /// always draws on top of its parent, so glow, fill and border must all be children (in that order).
    /// </summary>
    private static Image Fill(GameObject parent, Sprite sprite, Color color, bool raycast = false, Image.Type type = Image.Type.Sliced)
    {
        var go = UI("Fill", parent.transform);
        IgnoreLayout(go);
        Stretch((RectTransform)go.transform);
        return Img(go, sprite, color, raycast, type);
    }

    /// <summary>Outline drawn on top of the parent (child that ignores layout).</summary>
    private static Image Border(GameObject parent, Sprite sprite, Color color)
    {
        var go = UI("Border", parent.transform);
        IgnoreLayout(go);
        Stretch((RectTransform)go.transform);
        return Img(go, sprite, color);
    }

    /// <summary>Soft glow behind the parent (Figma drop-shadow). Added as the first child.</summary>
    private static Image Glow(GameObject parent, Color color)
    {
        var go = UI("Glow", parent.transform);
        go.transform.SetAsFirstSibling();
        IgnoreLayout(go);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-GlowSpread, -GlowSpread);
        rt.offsetMax = new Vector2(GlowSpread, GlowSpread);
        return Img(go, glowSprite, color);
    }

    /// <summary>Avatar with rounded corners (masked photo) and a cyan border.</summary>
    private static GameObject Avatar(Transform parent, float size, float radius, float stroke, out Image photo)
    {
        var avatar = UI("Avatar", parent);
        LE(avatar, w: size, h: size);
        ((RectTransform)avatar.transform).sizeDelta = new Vector2(size, size);

        var mask = UI("Mask", avatar.transform);
        Stretch((RectTransform)mask.transform);
        Img(mask, Rounded(radius, radius, radius, radius), JaycordTheme.BgElevated);
        mask.AddComponent<Mask>().showMaskGraphic = true;

        var photoGo = UI("Photo", mask.transform);
        Stretch((RectTransform)photoGo.transform);
        photo = Img(photoGo, null, JaycordTheme.Border);

        var border = UI("Border", avatar.transform);
        Stretch((RectTransform)border.transform);
        Img(border, Rounded(radius, radius, radius, radius, stroke), JaycordTheme.Cyan);
        return avatar;
    }

    private static GameObject PresenceDot(Transform avatar, Vector2 topLeft, float size, out Image dot)
    {
        var ring = UI("Presence", avatar);
        TopLeft((RectTransform)ring.transform, topLeft.x, -topLeft.y, size, size);
        Img(ring, circleSprite, JaycordTheme.BgPanel, type: Image.Type.Simple);
        var inner = UI("Dot", ring.transform);
        Center((RectTransform)inner.transform, size - 4, size - 4);
        dot = Img(inner, circleSprite, JaycordTheme.Online, type: Image.Type.Simple);
        return ring;
    }

    private static Image Icon(Transform parent, string name, Sprite sprite, Color color)
    {
        var go = UI(name, parent);
        LE(go, w: 24, h: 24);
        var image = Img(go, sprite, color, type: Image.Type.Simple);
        image.preserveAspect = true;
        return image;
    }

    private static TextMeshProUGUI Text(Transform parent, string name, string text, TMP_FontAsset font, float size, Color color,
        float trackingPx = 0f, TextAlignmentOptions align = TextAlignmentOptions.Left, bool wrap = false, bool upper = false)
    {
        var go = UI(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.characterSpacing = trackingPx / size * 100f; // Figma px → TMP em/100
        tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        if (upper) tmp.fontStyle = FontStyles.UpperCase;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static HorizontalLayoutGroup H(GameObject go, int left, int right, int top, int bottom, float spacing,
        TextAnchor align, bool expandWidth = false)
    {
        var g = go.AddComponent<HorizontalLayoutGroup>();
        g.padding = new RectOffset(left, right, top, bottom);
        g.spacing = spacing;
        g.childAlignment = align;
        g.childControlWidth = true;
        g.childControlHeight = true;
        g.childForceExpandWidth = expandWidth;
        g.childForceExpandHeight = false;
        return g;
    }

    private static VerticalLayoutGroup V(GameObject go, int left, int right, int top, int bottom, float spacing,
        TextAnchor align = TextAnchor.UpperLeft, bool expandWidth = true)
    {
        var g = go.AddComponent<VerticalLayoutGroup>();
        g.padding = new RectOffset(left, right, top, bottom);
        g.spacing = spacing;
        g.childAlignment = align;
        g.childControlWidth = true;
        g.childControlHeight = true;
        g.childForceExpandWidth = expandWidth;
        g.childForceExpandHeight = false;
        return g;
    }

    private static LayoutElement LE(GameObject go, float w = -1, float h = -1, float flexW = -1, float flexH = -1,
        float minW = -1, float minH = -1)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        if (w >= 0) le.preferredWidth = w;
        if (h >= 0) le.preferredHeight = h;
        if (flexW >= 0) le.flexibleWidth = flexW;
        if (flexH >= 0) le.flexibleHeight = flexH;
        if (minW >= 0) le.minWidth = minW;
        if (minH >= 0) le.minHeight = minH;
        return le;
    }

    private static void IgnoreLayout(GameObject go) => LE(go).ignoreLayout = true;

    private static void Spacer(Transform parent, float height) => LE(UI("Spacer", parent), h: height);

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void Center(RectTransform rt, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = Vector2.zero;
    }

    /// <summary>Position like Figma: x/y from the parent's top-left corner.</summary>
    private static void TopLeft(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, -y);
    }

    private static void TopStretch(RectTransform rt, float left, float right, float top, float height)
    {
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(left, -top - height);
        rt.offsetMax = new Vector2(-right, -top);
    }

    private static void EdgeLine(GameObject parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size)
    {
        var go = UI("Line", parent.transform);
        IgnoreLayout(go);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        Img(go, null, color);
    }

    private static void BottomLine(GameObject p, Color c) => EdgeLine(p, c, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 1));
    private static void RightLine(GameObject p, Color c) => EdgeLine(p, c, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(1, 0));
    private static void LeftLine(GameObject p, Color c) => EdgeLine(p, c, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(1, 0));

    private static void Set(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogError($"[Jaycord] {target.GetType().Name} has no field '{field}'.");
            return;
        }
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Object target, string field, float value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray(Object target, string field, Object[] values)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject Save(GameObject root, string name)
    {
        var asset = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/{name}.prefab");
        Object.DestroyImmediate(root);
        return asset;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
