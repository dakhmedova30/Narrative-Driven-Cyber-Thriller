using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Title screen (template): New Game, Continue, Load Game, Settings, Quit.
/// Put this on one GameObject in the Title scene (the menu Tools > Game > Create Title Scene does it).
/// New Game starts step 1 of the Game Flow (Tools > Game > Open Game Flow).
/// While this component is in the scene, GameSession knows it's the title: no pause menu, no autosave,
/// play time doesn't count.
/// </summary>
public class TitleScreen : MonoBehaviour
{
    [SerializeField] private string gameTitle = "NARRATIVE-DRIVEN CYBER THRILLER";
    [Tooltip("Optional background picture.")]
    [SerializeField] private Sprite background;

    private Button continueButton, loadButton;

    private void Start()
    {
        Canvas canvas = UIKit.CreateCanvas("Title Canvas", transform, 10);

        Image bg = UIKit.Image(canvas.transform, "Background", background != null ? Color.white : new Color(0.03f, 0.04f, 0.07f, 1f));
        bg.sprite = background;
        bg.preserveAspect = false;
        UIKit.Stretch(bg.rectTransform);

        TextMeshProUGUI title = UIKit.Label(canvas.transform, gameTitle, 72f, UIKit.AccentColor, TextAlignmentOptions.Center);
        UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.78f), new Vector2(1600f, 200f));

        RectTransform column = UIKit.Rect("Menu", canvas.transform);
        UIKit.Place(column, new Vector2(0.5f, 0.38f), new Vector2(420f, 400f));
        UIKit.Vertical(column.gameObject, 16f);

        UIKit.Button(column, "New Game", NewGame, -1f, 60f, 28f);
        continueButton = UIKit.Button(column, "Continue", Continue, -1f, 60f, 28f);
        loadButton = UIKit.Button(column, "Load Game", () => GameUI.Instance?.OpenSlotsFromTitle(SlotPanel.Mode.Load), -1f, 60f, 28f);
        UIKit.Button(column, "Settings", () => GameUI.Instance?.OpenSettingsFromTitle(), -1f, 60f, 28f);
        UIKit.Button(column, "Quit", GameSession.QuitGame, -1f, 60f, 28f);

        RefreshButtons();
        GameSession.SlotsChanged += RefreshButtons;
    }

    private void OnDestroy()
    {
        GameSession.SlotsChanged -= RefreshButtons;
    }

    private void RefreshButtons()
    {
        bool any = SaveManager.AnyExists();
        if (continueButton != null) continueButton.interactable = any;
        if (loadButton != null) loadButton.interactable = any;
    }

    private void NewGame()
    {
        if (GameSession.Instance == null) return;
        if (SaveManager.AnyExists() && GameUI.Instance != null)
            GameUI.Instance.Confirm("Start a new game? Your saves are kept.", () => GameSession.Instance.NewGame());
        else
            GameSession.Instance.NewGame();
    }

    private void Continue()
    {
        string slot = SaveManager.NewestSlot();
        if (slot != null) GameSession.Instance?.LoadFrom(slot);
    }
}
