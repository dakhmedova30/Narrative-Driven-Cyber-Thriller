using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lays children out left to right and wraps to a new line when full
/// (used for the #tags in the profile dossier).
/// </summary>
[AddComponentMenu("Layout/Jaycord Flow Layout")]
public class JaycordFlowLayout : LayoutGroup
{
    public float spacingX = 6f;
    public float spacingY = 6f;

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        SetLayoutInputForAxis(padding.horizontal, LayoutUtility.DefaultMaxSize, padding.horizontal, -1, 0);
    }

    public override void CalculateLayoutInputVertical()
    {
        float height = Arrange(false);
        SetLayoutInputForAxis(height, LayoutUtility.DefaultMaxSize, height, -1, 1);
    }

    public override void SetLayoutHorizontal() => Arrange(true);
    public override void SetLayoutVertical() => Arrange(true);

    private float Arrange(bool apply)
    {
        float width = rectTransform.rect.width - padding.horizontal;
        float x = 0f, y = 0f, rowHeight = 0f;

        for (int i = 0; i < rectChildren.Count; i++)
        {
            var child = rectChildren[i];
            float w = Mathf.Min(LayoutUtility.GetPreferredWidth(child), Mathf.Max(0f, width));
            float h = LayoutUtility.GetPreferredHeight(child);

            if (x > 0f && x + w > width)
            {
                x = 0f;
                y += rowHeight + spacingY;
                rowHeight = 0f;
            }

            if (apply)
            {
                SetChildAlongAxis(child, 0, padding.left + x, w);
                SetChildAlongAxis(child, 1, padding.top + y, h);
            }

            x += w + spacingX;
            rowHeight = Mathf.Max(rowHeight, h);
        }

        return padding.vertical + y + rowHeight;
    }
}
