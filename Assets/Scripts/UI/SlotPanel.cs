using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Save / Load screen (template). Built by GameUI.
///   Save mode: 6 manual slots. Click = save (asks before overwriting).
///   Load mode: 6 manual slots + Quick Save + Auto Save. Click = load (asks first during gameplay).
/// Every filled slot has a Delete button. Each card: thumbnail, slot name, chapter · area, date, play time.
/// </summary>
public class SlotPanel
{
    public enum Mode { Save, Load }

    private readonly GameUI ui;
    private readonly RectTransform root;
    private readonly TextMeshProUGUI title;
    private readonly RectTransform grid;
    private readonly List<Texture2D> thumbnails = new List<Texture2D>();

    private Mode mode;
    private byte[] shot;

    public bool IsOpen => root.gameObject.activeSelf;

    public SlotPanel(Transform parent, GameUI owner)
    {
        ui = owner;
        root = UIKit.Overlay(parent, "Save Load Panel");

        Image box = UIKit.Image(root, "Box", UIKit.PanelColor);
        UIKit.Place(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1760f, 960f));
        UIKit.Vertical(box.gameObject, 16f, 30);

        title = UIKit.Label(box.transform, "SAVE", 44f, UIKit.AccentColor, TextAlignmentOptions.Center, 64f);

        grid = UIKit.Rect("Slots", box.transform);
        var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(400f, 350f);
        layout.spacing = new Vector2(20f, 20f);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 4;
        layout.childAlignment = TextAnchor.UpperCenter;
        UIKit.Size(grid.gameObject, -1f, 740f);

        RectTransform bottom = UIKit.Rect("Bottom", box.transform);
        UIKit.Horizontal(bottom.gameObject, 20f, 0, TextAnchor.MiddleCenter);
        UIKit.Size(bottom.gameObject, -1f, 56f);
        UIKit.Button(bottom, "Back", () => ui.CloseSlots(), 260f);

        root.gameObject.SetActive(false);
        GameSession.SlotsChanged += () => { if (IsOpen) Refresh(); };
    }

    public void Open(Mode openMode, byte[] screenshot)
    {
        mode = openMode;
        shot = screenshot;
        title.text = mode == Mode.Save ? "SAVE GAME" : "LOAD GAME";
        root.gameObject.SetActive(true);
        root.SetAsLastSibling();
        Refresh();
    }

    public void Close()
    {
        if (!IsOpen) return;
        root.gameObject.SetActive(false);
        UIKit.Clear(grid);
        FreeThumbnails();
    }

    private void Refresh()
    {
        UIKit.Clear(grid);
        FreeThumbnails();

        for (int i = 1; i <= SaveManager.ManualSlotCount; i++) AddCard(SaveManager.ManualSlot(i));
        if (mode == Mode.Load)
        {
            AddCard(SaveManager.QuickSlot);
            AddCard(SaveManager.AutoSlot);
        }
    }

    private void AddCard(string slotId)
    {
        SaveData data = SaveManager.Read(slotId);
        bool filled = data != null;
        bool inGame = GameSession.Instance != null && !GameSession.Instance.IsOnTitle;

        Image card = UIKit.Image(grid, "Card " + slotId, UIKit.ButtonColor);
        var button = card.gameObject.AddComponent<Button>();
        UIKit.Vertical(card.gameObject, 6f, 10);

        // Thumbnail
        RectTransform thumbRt = UIKit.Rect("Thumbnail", card.transform);
        var thumb = thumbRt.gameObject.AddComponent<RawImage>();
        thumb.raycastTarget = false;
        UIKit.Size(thumbRt.gameObject, 380f, 214f);
        Texture2D tex = filled ? SaveManager.ReadThumbnail(slotId) : null;
        if (tex != null) { thumb.texture = tex; thumbnails.Add(tex); }
        else thumb.color = new Color(0f, 0f, 0f, 0.4f);

        UIKit.Label(card.transform, SaveManager.DisplayName(slotId), 26f, UIKit.TextColor, TextAlignmentOptions.Left, 32f);
        string info = filled
            ? $"{(string.IsNullOrEmpty(data.progressLabel) ? "Chapter " + data.chapter : data.progressLabel)}  ·  {data.sceneName}\n{data.savedAt}\nPlay time {SaveManager.FormatPlayTime(data.playTime)}"
            : "Empty";
        UIKit.Label(card.transform, info, 19f, UIKit.MutedColor, TextAlignmentOptions.TopLeft, 70f);

        // Delete (top-right, outside the layout)
        if (filled)
        {
            Button del = UIKit.Button(card.transform, "Delete", () =>
                ui.Confirm($"Delete {SaveManager.DisplayName(slotId)}?", () => GameSession.Instance?.DeleteSave(slotId)),
                -1f, 36f, 18f);
            del.GetComponent<Image>().color = UIKit.DangerColor;
            del.GetComponent<LayoutElement>().ignoreLayout = true;
            UIKit.Place((RectTransform)del.transform, new Vector2(1f, 1f), new Vector2(100f, 36f), new Vector2(-14f, -14f));
        }

        // Click
        if (mode == Mode.Save)
        {
            button.interactable = inGame;
            button.onClick.AddListener(() =>
            {
                if (filled)
                    ui.Confirm($"Overwrite {SaveManager.DisplayName(slotId)}?", () => GameSession.Instance?.SaveTo(slotId, shot));
                else
                    GameSession.Instance?.SaveTo(slotId, shot);
            });
        }
        else
        {
            button.interactable = filled;
            button.onClick.AddListener(() =>
            {
                if (inGame)
                    ui.Confirm($"Load {SaveManager.DisplayName(slotId)}? Unsaved progress will be lost.",
                        () => GameSession.Instance?.LoadFrom(slotId));
                else
                    GameSession.Instance?.LoadFrom(slotId);
            });
        }
    }

    private void FreeThumbnails()
    {
        foreach (Texture2D t in thumbnails) if (t != null) Object.Destroy(t);
        thumbnails.Clear();
    }
}
