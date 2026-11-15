using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// An exit to another area (Unity scene), usually at the left or right edge.
///
/// Mode:
///   Auto        -> walking INTO the zone fades to black and loads the target scene.
///   ClickArrow  -> walking into the zone shows a bobbing arrow; clicking it goes there.
///                  Walking away hides the arrow again.
///
/// Setup:
///   1. Empty GameObject at the exit (e.g. near Max X), add AreaExit.
///   2. Target Scene = scene name exactly as in the Project window (e.g. "Street").
///      Target Spawn = Id of a SpawnPoint in that scene (empty = its default spawn).
///   3. Zone Width = how wide the trigger zone is, centred on this object (orange box in Scene view).
///      The zone must be inside the player's Min X / Max X or it can never be reached.
///   4. The scene needs an EventSystem for the arrow to be clickable.
///
/// The player arriving INSIDE a zone does not trigger it; they have to walk out and back in.
/// </summary>
public class AreaExit : MonoBehaviour
{
    public enum Mode { Auto, ClickArrow }
    public enum ArrowDirection { Left, Right, Up }

    [SerializeField] private Mode mode = Mode.ClickArrow;
    [Tooltip("Scene name exactly as in the Project window.")]
    [SerializeField] private string targetScene;
    [Tooltip("SpawnPoint Id in the target scene. Empty = that scene's default spawn.")]
    [SerializeField] private string targetSpawn;

    [Header("Trigger zone (world X, centred on this object)")]
    [SerializeField, Min(0.1f)] private float zoneWidth = 1.5f;

    [Header("Arrow (ClickArrow mode)")]
    [Tooltip("Leave empty for a generated arrow.")]
    [SerializeField] private Sprite arrowSprite;
    [SerializeField] private ArrowDirection arrowDirection = ArrowDirection.Right;
    [Tooltip("Arrow position relative to this object, in world units.")]
    [SerializeField] private Vector3 arrowOffset = new Vector3(0f, 2.5f, 0f);
    [SerializeField] private float arrowSizePixels = 64f;
    [SerializeField] private Color arrowColor = new Color(0.3f, 0.95f, 1f, 1f);
    [SerializeField] private float bobPixels = 8f;
    [SerializeField] private float bobSpeed = 4f;
    [SerializeField, Min(0.01f)] private float arrowFadeSeconds = 0.15f;
    [Tooltip("Uses Camera.main if empty.")]
    [SerializeField] private Camera worldCamera;

    private bool wasInside;
    private bool initialised;

    private RectTransform arrowRect;
    private CanvasGroup arrowGroup;
    private float arrowAlpha;

    private static Sprite generatedArrow;

    public Mode CurrentMode { get => mode; set => mode = value; }

    private void Start()
    {
        if (mode == Mode.ClickArrow) BuildArrow();
    }

    private void Update()
    {
        PlayerMover player = PlayerMover.Instance;
        bool inside = player != null && IsInside(player.transform.position.x);

        // First frame: if the player starts inside, don't fire until they leave and come back.
        if (!initialised) { wasInside = inside; initialised = true; }

        bool canUse = inside && player.CanMove && !SceneTransitioner.IsTransitioning;

        if (mode == Mode.Auto)
        {
            if (inside && !wasInside && canUse) Go();
            SetArrowVisible(false);
        }
        else
        {
            if (arrowRect == null) BuildArrow();
            SetArrowVisible(canUse);
        }

        wasInside = inside;
        UpdateArrow();
    }

    /// <summary>Go through this exit now (also used by the arrow button).</summary>
    public void Go()
    {
        SceneTransitioner.Instance.GoTo(targetScene, targetSpawn);
    }

    private bool IsInside(float x) => Mathf.Abs(x - transform.position.x) <= zoneWidth * 0.5f;

    // ------------------------------------------------------------------
    // Arrow (screen-space UI button that follows a world position)
    // ------------------------------------------------------------------

    private void BuildArrow()
    {
        if (arrowRect != null) return;

        var canvasGo = new GameObject("ExitArrowCanvas", typeof(Canvas), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var go = new GameObject("Arrow", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
        go.transform.SetParent(canvasGo.transform, false);
        arrowRect = (RectTransform)go.transform;
        arrowRect.anchorMin = arrowRect.anchorMax = Vector2.zero; // position in screen pixels
        arrowRect.sizeDelta = new Vector2(arrowSizePixels, arrowSizePixels);

        var img = go.GetComponent<Image>();
        img.sprite = arrowSprite != null ? arrowSprite : GetGeneratedArrow();
        img.color = arrowColor;
        img.preserveAspect = true;

        arrowRect.localRotation = Quaternion.Euler(0f, 0f, arrowDirection switch
        {
            ArrowDirection.Left => 180f,
            ArrowDirection.Up => 90f,
            _ => 0f,
        });

        var button = go.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(Go);

        arrowGroup = go.GetComponent<CanvasGroup>();
        arrowGroup.alpha = 0f;
        arrowGroup.interactable = arrowGroup.blocksRaycasts = false;
        arrowRect.gameObject.SetActive(false);
    }

    private bool arrowWanted;

    private void SetArrowVisible(bool visible) => arrowWanted = visible;

    private void UpdateArrow()
    {
        if (arrowRect == null) return;

        arrowAlpha = Mathf.MoveTowards(arrowAlpha, arrowWanted ? 1f : 0f, Time.deltaTime / arrowFadeSeconds);
        bool show = arrowAlpha > 0.001f;
        if (arrowRect.gameObject.activeSelf != show) arrowRect.gameObject.SetActive(show);
        if (!show) return;

        arrowGroup.alpha = arrowAlpha;
        arrowGroup.interactable = arrowGroup.blocksRaycasts = arrowWanted;

        Camera cam = worldCamera != null ? worldCamera : Camera.main;
        if (cam == null) return;
        Vector3 screen = cam.WorldToScreenPoint(transform.position + arrowOffset);
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobPixels;
        Vector2 bobDir = arrowDirection switch
        {
            ArrowDirection.Left => Vector2.left,
            ArrowDirection.Right => Vector2.right,
            _ => Vector2.up,
        };
        arrowRect.anchoredPosition = (Vector2)screen + bobDir * bob;
    }

    /// <summary>White right-pointing chevron, generated once.</summary>
    private static Sprite GetGeneratedArrow()
    {
        if (generatedArrow != null) return generatedArrow;
        const int s = 64;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[s * s];
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            // Triangle pointing right: x from 12..52, half-height shrinks toward the tip.
            float half = (52f - x) * 0.55f;
            bool inTri = x >= 12 && x <= 52 && Mathf.Abs(y - 31.5f) <= half;
            px[y * s + x] = inTri ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
        }
        tex.SetPixels32(px);
        tex.Apply();
        generatedArrow = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        return generatedArrow;
    }

    private void OnDrawGizmos()
    {
        Vector3 p = transform.position;
        Gizmos.color = mode == Mode.Auto ? new Color(1f, 0.5f, 0.1f, 0.8f) : new Color(0.3f, 0.9f, 1f, 0.8f);
        Gizmos.DrawWireCube(p + Vector3.up * 1f, new Vector3(zoneWidth, 2f, 0.1f));
        if (mode == Mode.ClickArrow) Gizmos.DrawWireSphere(p + arrowOffset, 0.2f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(p + Vector3.up * 2.3f,
            $"exit ({mode}) → {(string.IsNullOrEmpty(targetScene) ? "?" : targetScene)} / {(string.IsNullOrEmpty(targetSpawn) ? "default" : targetSpawn)}");
#endif
    }
}
