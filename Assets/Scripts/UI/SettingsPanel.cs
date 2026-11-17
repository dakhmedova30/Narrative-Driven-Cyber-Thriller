using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Settings screen (template). Built by GameUI. Everything is saved immediately (PlayerPrefs).
///   Audio:    master / music / sound effects
///   Text:     text speed, auto play delay, default speed, skip mode, confirm quick load
///   Controls: two keys per action, click to rebind (right-click cancels), reset to defaults
/// </summary>
public class SettingsPanel
{
    private readonly RectTransform root;
    private readonly RectTransform content;
    private readonly List<(InputAction action, int index, Button button)> keyButtons = new List<(InputAction, int, Button)>();

    public bool IsOpen => root.gameObject.activeSelf;

    public SettingsPanel(Transform parent)
    {
        root = UIKit.Overlay(parent, "Settings Panel");

        Image box = UIKit.Image(root, "Box", UIKit.PanelColor);
        UIKit.Place(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1100f, 960f));
        UIKit.Vertical(box.gameObject, 16f, 30);

        UIKit.Label(box.transform, "SETTINGS", 44f, UIKit.AccentColor, TextAlignmentOptions.Center, 64f);

        content = UIKit.ScrollList(box.transform, 12f);
        UIKit.Size(content.parent.parent.gameObject, -1f, 740f); // the scroll view

        RectTransform bottom = UIKit.Rect("Bottom", box.transform);
        UIKit.Horizontal(bottom.gameObject, 20f, 0, TextAnchor.MiddleCenter);
        UIKit.Size(bottom.gameObject, -1f, 56f);
        UIKit.Button(bottom, "Back", () => GameUI.Instance?.CloseSettings(), 260f);

        root.gameObject.SetActive(false);
        GameInput.BindingsChanged += RefreshKeys;
    }

    public void Open()
    {
        Build();
        root.gameObject.SetActive(true);
        root.SetAsLastSibling();
    }

    public void Close()
    {
        if (!IsOpen) return;
        if (GameInput.IsRebinding) GameInput.CancelRebind();
        root.gameObject.SetActive(false);
    }

    private void Build()
    {
        UIKit.Clear(content);
        keyButtons.Clear();

        // ---- Audio ----
        Header("Audio");
        SliderRow("Master volume", 0f, 1f, GameSettings.MasterVolume, v => { GameSettings.MasterVolume = v; GameSettings.Apply(); }, "0%");
        SliderRow("Music", 0f, 1f, GameSettings.MusicVolume, v => { GameSettings.MusicVolume = v; GameSettings.Apply(); }, "0%");
        SliderRow("Sound effects", 0f, 1f, GameSettings.SfxVolume, v => { GameSettings.SfxVolume = v; GameSettings.Apply(); }, "0%");

        // ---- Text ----
        Header("Text & dialogue");
        SliderRow("Text speed (face mode)", 0.5f, 2f, GameSettings.TextSpeed, v => { GameSettings.TextSpeed = v; GameSettings.Apply(); }, "x");
        SliderRow("Auto play delay", 0.5f, 4f, GameSettings.AutoDelay, v => { GameSettings.AutoDelay = v; GameSettings.Apply(); }, "s");

        RectTransform speedRow = UIKit.Row(content, "Default speed");
        var speedButtons = new List<Button>();
        for (int i = 0; i < DialoguePlayback.Speeds.Length; i++)
        {
            int index = i;
            Button b = UIKit.Button(speedRow, DialoguePlayback.Speeds[i].ToString("0") + "x", () =>
            {
                GameSettings.DefaultSpeedIndex = index;
                GameSettings.Apply();
                DialoguePlayback.SetSpeedIndex(index);
                for (int k = 0; k < speedButtons.Count; k++) UIKit.SetOn(speedButtons[k], k == index);
            }, 100f, 44f, 22f);
            UIKit.SetOn(b, i == GameSettings.DefaultSpeedIndex);
            speedButtons.Add(b);
        }

        RectTransform skipRow = UIKit.Row(content, "Skip");
        Button readOnly = null, all = null;
        readOnly = UIKit.Button(skipRow, "Read text only", () => SetSkipAll(false, readOnly, all), 220f, 44f, 22f);
        all = UIKit.Button(skipRow, "Everything", () => SetSkipAll(true, readOnly, all), 220f, 44f, 22f);
        UIKit.SetOn(readOnly, !GameSettings.SkipModeAll);
        UIKit.SetOn(all, GameSettings.SkipModeAll);
        UIKit.Label(content, "\"Everything\" only works in chapters you have finished at least once. Unfinished chapters always skip read text only.",
            19f, UIKit.MutedColor, TextAlignmentOptions.Left, 52f);

        RectTransform qlRow = UIKit.Row(content, "Ask before Quick Load");
        Button ql = null;
        ql = UIKit.Button(qlRow, GameSettings.ConfirmQuickLoad ? "On" : "Off", () =>
        {
            GameSettings.ConfirmQuickLoad = !GameSettings.ConfirmQuickLoad;
            GameSettings.Apply();
            UIKit.SetText(ql, GameSettings.ConfirmQuickLoad ? "On" : "Off");
            UIKit.SetOn(ql, GameSettings.ConfirmQuickLoad);
        }, 120f, 44f, 22f);
        UIKit.SetOn(ql, GameSettings.ConfirmQuickLoad);

        // ---- Controls ----
        Header("Controls");
        UIKit.Label(content, "Click a key to change it, then press the new key. Right-click cancels.",
            19f, UIKit.MutedColor, TextAlignmentOptions.Left, 30f);
        foreach (InputAction action in GameInput.Rebindable)
        {
            RectTransform row = UIKit.Row(content, action.name);
            for (int i = 0; i < 2; i++)
            {
                int index = i;
                InputAction a = action;
                Button b = null;
                b = UIKit.Button(row, GameInput.Display(a, index), () => StartRebind(a, index, b), 200f, 44f, 22f);
                keyButtons.Add((a, index, b));
            }
        }

        // ---- Reset ----
        Header("Reset");
        RectTransform resetRow = UIKit.Rect("Reset Row", content);
        UIKit.Horizontal(resetRow.gameObject, 16f);
        UIKit.Size(resetRow.gameObject, -1f, 52f);
        UIKit.Button(resetRow, "Reset settings", () =>
            GameUI.Instance?.Confirm("Reset all settings (not keys) to default?", () =>
            {
                GameSettings.ResetToDefaults();
                DialoguePlayback.SetSpeedIndex(GameSettings.DefaultSpeedIndex);
                Build();
            }), 260f, 48f, 22f);
        UIKit.Button(resetRow, "Reset keys", () =>
            GameUI.Instance?.Confirm("Reset all keys to default?", GameInput.ResetToDefaults), 260f, 48f, 22f);
    }

    private void Header(string text)
    {
        UIKit.Label(content, text.ToUpperInvariant(), 26f, UIKit.AccentColor, TextAlignmentOptions.Left, 48f);
    }

    private void SliderRow(string label, float min, float max, float value, System.Action<float> onChange, string unit)
    {
        RectTransform row = UIKit.Row(content, label);
        TextMeshProUGUI valueText = null;
        UIKit.Slider(row, min, max, value, v =>
        {
            onChange(v);
            valueText.text = Format(v, unit);
        });
        valueText = UIKit.Label(row, Format(value, unit), 22f, UIKit.TextColor, TextAlignmentOptions.Left);
        UIKit.Size(valueText.gameObject, 100f, 40f);
    }

    private static string Format(float v, string unit) => unit switch
    {
        "0%" => Mathf.RoundToInt(v * 100f) + "%",
        "x" => v.ToString("0.0") + "x",
        "s" => v.ToString("0.0") + " s",
        _ => v.ToString("0.00"),
    };

    private static void SetSkipAll(bool on, Button readOnly, Button all)
    {
        GameSettings.SkipModeAll = on;
        GameSettings.Apply();
        UIKit.SetOn(readOnly, !on);
        UIKit.SetOn(all, on);
        DialoguePlayback.SetSpeedIndex(DialoguePlayback.SpeedIndex); // refresh the control bar label
    }

    private void StartRebind(InputAction action, int index, Button button)
    {
        if (GameInput.IsRebinding) return;
        UIKit.SetText(button, "Press a key...");
        UIKit.SetOn(button, true);
        GameInput.StartRebind(action, index, RefreshKeys);
    }

    private void RefreshKeys()
    {
        foreach (var (action, index, button) in keyButtons)
        {
            if (button == null) continue;
            UIKit.SetText(button, GameInput.Display(action, index));
            UIKit.SetOn(button, false);
        }
    }
}
