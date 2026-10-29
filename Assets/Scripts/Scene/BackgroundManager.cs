using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows full-screen background images for Yarn's &lt;&lt;bg NAME&gt;&gt; command.
///
/// Setup:
///   1. Put this on an empty GameObject in the scene (e.g. "Backgrounds").
///   2. Fill the Backgrounds list: Name = the name used in Yarn, Image = the sprite.
///      (Import the PNG with Texture Type = Sprite (2D and UI).)
///   3. In Yarn: &lt;&lt;bg subway_2027&gt;&gt;
///
/// It builds its own canvas behind the dialogue UI, keeps the picture's aspect ratio
/// (fills the screen, cropping the edges if needed) and cross-fades between backgrounds.
/// </summary>
public class BackgroundManager : MonoBehaviour
{
    [Serializable]
    public class Background
    {
        [Tooltip("Name used in Yarn, e.g. subway_2027 for <<bg subway_2027>>.")]
        public string name;
        public Sprite image;
    }

    [SerializeField] private List<Background> backgrounds = new List<Background>();
    [Tooltip("Cross-fade time when switching backgrounds (0 = instant).")]
    [SerializeField, Min(0f)] private float fadeSeconds = 0.4f;
    [Tooltip("Draw order of the background canvas. Must be lower than the dialogue UI's canvas.")]
    [SerializeField] private int sortingOrder = -100;

    public static BackgroundManager Instance { get; private set; }

    private Image current;
    private Image previous;
    private Coroutine fading;

    private void Awake()
    {
        Instance = this;
        BuildCanvas();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Switch to the background with this name (case-insensitive).</summary>
    public void Show(string backgroundName)
    {
        var entry = backgrounds.Find(b => string.Equals(b.name, backgroundName, StringComparison.OrdinalIgnoreCase));
        if (entry == null || entry.image == null)
        {
            Debug.LogWarning($"[Background] No background named '{backgroundName}'. Add it to the Backgrounds list on '{gameObject.name}'.", this);
            return;
        }

        // The old picture stays underneath while the new one fades in.
        if (current.enabled)
        {
            SetSprite(previous, current.sprite);
            previous.color = Color.white;
        }
        SetSprite(current, entry.image);

        if (fading != null) StopCoroutine(fading);
        fading = StartCoroutine(FadeIn());
    }

    /// <summary>Remove the background (shows whatever the camera renders).</summary>
    public void Clear()
    {
        if (fading != null) StopCoroutine(fading);
        current.enabled = false;
        previous.enabled = false;
    }

    private IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.deltaTime;
            current.color = new Color(1f, 1f, 1f, Mathf.Clamp01(t / fadeSeconds));
            yield return null;
        }
        current.color = Color.white;
        previous.enabled = false;
        fading = null;
    }

    private void BuildCanvas()
    {
        var canvasGo = new GameObject("Background Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        previous = CreateLayer("Previous", canvasGo.transform);
        current = CreateLayer("Current", canvasGo.transform);
    }

    private static Image CreateLayer(string layerName, Transform parent)
    {
        var go = new GameObject(layerName, typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
        go.transform.SetParent(parent, false);
        var fitter = go.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; // fill the screen, keep proportions
        var image = go.GetComponent<Image>();
        image.raycastTarget = false;
        image.enabled = false;
        return image;
    }

    private static void SetSprite(Image image, Sprite sprite)
    {
        image.sprite = sprite;
        image.enabled = sprite != null;
        if (sprite != null)
            image.GetComponent<AspectRatioFitter>().aspectRatio = sprite.rect.width / sprite.rect.height;
    }
}
