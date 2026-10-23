using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

/// <summary>
/// Figma "03 Link Card" — e.g. the transmigration link.
/// The presenter can wait until the player presses OPEN LINK.
/// </summary>
public class JaycordLinkCard : MonoBehaviour
{
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text url;
    [SerializeField] private GameObject warning;
    [SerializeField] private Button openButton;
    [SerializeField] private TMP_Text openLabel;

    private YarnTaskCompletionSource opened;

    public bool IsOpened { get; private set; }

    private void Awake()
    {
        if (openButton != null) openButton.onClick.AddListener(Open);
    }

    public void Setup(string titleText, string urlText, bool unverified)
    {
        title.text = titleText;
        url.text = urlText;
        if (warning != null) warning.SetActive(unverified);
    }

    public void Open()
    {
        if (IsOpened) return;
        IsOpened = true;
        if (openLabel != null) openLabel.text = "LINK OPENED";
        if (openButton != null) openButton.interactable = false;
        opened?.TrySetResult();
    }

    /// <summary>Completes when the player presses OPEN LINK.</summary>
    public YarnTask WaitUntilOpened()
    {
        if (IsOpened) return YarnTask.CompletedTask;
        opened ??= new YarnTaskCompletionSource();
        return opened.Task;
    }
}
