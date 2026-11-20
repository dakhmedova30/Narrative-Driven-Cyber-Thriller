using TMPro;
using UnityEngine;

public class DynamicSpeechBubble : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text speechText;
    public RectTransform speechPanel;
    public RectTransform speechGroup;
    public RectTransform speechTail;

    [Header("Sizing")]
    public float minimumHeight = 124f;
    public float topPadding = 14f;
    public float bottomPadding = 15f;
    public float sidePadding = 18f;
    public float tailOverlap = 5f;

    private string lastText;
    private float lastWidth = -1f;

    void LateUpdate()
    {
        if (!speechText || !speechPanel ||
            !speechGroup || !speechTail)
            return;

        float width = speechPanel.rect.width;

        if (speechText.text == lastText &&
            Mathf.Approximately(width, lastWidth))
            return;

        lastText = speechText.text;
        lastWidth = width;

        Canvas.ForceUpdateCanvases();

        float availableWidth = Mathf.Max(
            1f, speechText.rectTransform.rect.width
        );

        speechText.ForceMeshUpdate();

        float textHeight = speechText.GetPreferredValues(
            speechText.text,
            availableWidth,
            Mathf.Infinity
        ).y;

        float panelHeight = Mathf.Max(
            minimumHeight,
            Mathf.Ceil(textHeight + topPadding + bottomPadding)
        );

        speechPanel.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical, panelHeight
        );

        float tailHeight = speechTail.rect.height;

        speechGroup.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            panelHeight + tailHeight - tailOverlap
        );

        // Anchor the tail to the panel's bottom edge.
        speechTail.anchorMin = new Vector2(0.5f, 1f);
        speechTail.anchorMax = new Vector2(0.5f, 1f);
        speechTail.pivot = new Vector2(0.5f, 1f);

        speechTail.anchoredPosition = new Vector2(
            20f, -panelHeight + tailOverlap
        );
    }
}