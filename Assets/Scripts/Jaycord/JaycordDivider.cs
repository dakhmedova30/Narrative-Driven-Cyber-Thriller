using TMPro;
using UnityEngine;

/// <summary>Centered label between two lines, e.g. "NOV 30, 2026 · 11:42 PM".</summary>
public class JaycordDivider : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    public void SetLabel(string text) => label.text = text.ToUpperInvariant();
}
