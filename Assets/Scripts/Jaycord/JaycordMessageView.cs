using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One chat bubble (self / other / system).
/// The bubble grows with its text up to maxTextWidth, then the text wraps.
/// </summary>
public class JaycordMessageView : MonoBehaviour
{
    [SerializeField] private TMP_Text body;
    [SerializeField] private LayoutElement bubbleLayout;
    [Tooltip("Widest the text inside the bubble may get before it wraps.")]
    [SerializeField] private float maxTextWidth = 440f;
    [Tooltip("Left + right padding of the bubble.")]
    [SerializeField] private float horizontalPadding = 32f;

    public void SetText(string text)
    {
        body.text = text;
        if (bubbleLayout == null) return;

        // Width of the text on a single line; clamp it so long messages wrap.
        float singleLine = body.GetPreferredValues(text).x;
        bubbleLayout.preferredWidth = Mathf.Min(singleLine + 2f, maxTextWidth) + horizontalPadding;
    }
}
