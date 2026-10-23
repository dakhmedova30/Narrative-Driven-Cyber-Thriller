using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Right-hand profile panel: avatar, name, status, dossier, trust meter and tags.</summary>
public class JaycordProfilePanel : MonoBehaviour
{
    [SerializeField] private Image avatar;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text handleText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private RectTransform dossierRows;
    [SerializeField] private JaycordDossierRow dossierRowPrefab;
    [SerializeField] private TMP_Text trustText;
    [SerializeField] private Image[] trustSegments;
    [SerializeField] private RectTransform tagContainer;
    [SerializeField] private GameObject tagPrefab;

    [Tooltip("Filled segments up to this many are magenta, the rest cyan (matches Figma).")]
    [SerializeField] private int magentaSegments = 3;

    public void Bind(JaycordContact contact)
    {
        if (contact.avatar != null)
        {
            avatar.sprite = contact.avatar;
            avatar.color = Color.white;
        }
        nameText.text = contact.name;
        SetStatus(contact.status);
        handleText.text = contact.handle;
        levelText.text = contact.level;

        JaycordApp.ClearChildren(dossierRows);
        foreach (var field in contact.dossier)
            Instantiate(dossierRowPrefab, dossierRows).Bind(field);

        trustText.text = $"{contact.trust}%";
        int filled = Mathf.RoundToInt(contact.trust / 100f * trustSegments.Length);
        for (int i = 0; i < trustSegments.Length; i++)
        {
            trustSegments[i].color = i >= filled ? JaycordTheme.Border
                                   : i < magentaSegments ? JaycordTheme.Magenta
                                   : JaycordTheme.Cyan;
        }

        JaycordApp.ClearChildren(tagContainer);
        foreach (var tag in contact.tags)
        {
            var go = Instantiate(tagPrefab, tagContainer);
            go.GetComponentInChildren<TMP_Text>().text = tag.StartsWith("#") ? tag : "#" + tag;
        }
    }

    public void SetStatus(string status) => statusText.text = status.ToUpperInvariant();
}
