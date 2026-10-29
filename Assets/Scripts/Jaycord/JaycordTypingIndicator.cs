using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "Max is typing…" — three dots, one lit at a time (Figma: 3 frames, 0.3s each).
/// </summary>
public class JaycordTypingIndicator : MonoBehaviour
{
    [SerializeField] private Image[] dots;
    [SerializeField] private TMP_Text label;
    [SerializeField] private float frameTime = 0.3f;
    [SerializeField] private Color activeDot = new Color(0f, 0.94f, 1f);
    [SerializeField] private Color idleDot = new Color(0.29f, 0.3f, 0.42f);

    private int frame;
    private float timer;

    public void Show(string who)
    {
        if (label != null) label.text = $"{who.ToUpperInvariant()} IS TYPING\u2026";
        frame = 0;
        timer = 0f;
        ApplyFrame();
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void Hide() => gameObject.SetActive(false);

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer < frameTime) return;
        timer -= frameTime;
        frame = (frame + 1) % Mathf.Max(1, dots.Length);
        ApplyFrame();
    }

    private void ApplyFrame()
    {
        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i] != null)
                dots[i].color = i == frame ? activeDot : idleDot;
        }
    }
}
