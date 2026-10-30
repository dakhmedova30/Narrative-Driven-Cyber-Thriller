using TMPro;
using UnityEngine;

/// <summary>"FACTION ........ A" line in the profile dossier.</summary>
public class JaycordDossierRow : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_Text value;

    public void Bind(JaycordDossierField field)
    {
        label.text = field.label.ToUpperInvariant();
        value.text = field.value;
        value.color = field.highlight ? JaycordTheme.Yellow : JaycordTheme.TextPrimary;
    }
}
