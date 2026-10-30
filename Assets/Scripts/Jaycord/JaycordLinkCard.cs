using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Figma "03 Link Card" — e.g. the transmigration link.</summary>
public class JaycordLinkCard : MonoBehaviour
{
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text url;
    [SerializeField] private GameObject warning;
    [SerializeField] private Button openButton;
    [SerializeField] private TMP_Text openLabel;

    /// <summary>Called when the player presses OPEN LINK.</summary>
    public Action onOpened;

    public bool IsOpened { get; private set; }

    private void Awake()
    {
        if (openButton != null) openButton.onClick.AddListener(Open);
    }

    public void Setup(string titleText, string urlText, bool unverified, bool alreadyOpened)
    {
        title.text = titleText;
        url.text = urlText;
        if (warning != null) warning.SetActive(unverified);
        if (alreadyOpened) ShowOpened();
    }

    public void Open()
    {
        if (IsOpened) return;
        ShowOpened();
        onOpened?.Invoke();
    }

    private void ShowOpened()
    {
        IsOpened = true;
        if (openLabel != null) openLabel.text = "LINK OPENED";
        if (openButton != null) openButton.interactable = false;
    }
}
