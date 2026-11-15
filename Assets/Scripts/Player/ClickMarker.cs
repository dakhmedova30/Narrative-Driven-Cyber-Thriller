using UnityEngine;

/// <summary>
/// Small marker on the floor showing where the player is walking to after a click.
/// Pops in when a target is set, pulses gently, and fades out on arrival or cancel.
///
/// Setup:
///   1. Create an empty GameObject (e.g. "ClickMarker"), NOT a child of the player.
///   2. Add this script. A SpriteRenderer is added automatically.
///   3. (Optional) Put your own marker art in "Marker Sprite". If empty, a simple ring is drawn.
///   4. Adjust "Y Offset" so the marker sits on the floor under the player's feet.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ClickMarker : MonoBehaviour
{
    [Tooltip("Uses PlayerMover.Instance if empty.")]
    [SerializeField] private PlayerMover player;
    [Tooltip("Leave empty to use a generated ring.")]
    [SerializeField] private Sprite markerSprite;
    [SerializeField] private Color color = new Color(0.3f, 0.95f, 1f, 1f);

    [Header("Placement")]
    [Tooltip("Height relative to the player's pivot. If the sprite pivot is at the feet, 0 is the floor.")]
    [SerializeField] private float yOffset = 0f;
    [Tooltip("Small Z nudge so the marker is drawn in front of / behind the floor.")]
    [SerializeField] private float zOffset = 0f;
    [SerializeField] private float size = 0.5f;
    [SerializeField] private string sortingLayer = "Default";
    [SerializeField] private int sortingOrder = 5;

    [Header("Animation")]
    [SerializeField, Min(0.01f)] private float popSeconds = 0.15f;
    [SerializeField, Min(0.01f)] private float fadeSeconds = 0.25f;
    [SerializeField] private float pulseSpeed = 6f;
    [SerializeField, Range(0f, 0.5f)] private float pulseAmount = 0.08f;

    private SpriteRenderer sr;
    private PlayerMover subscribed;
    private float shownAt;
    private float alpha;
    private bool visible;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = markerSprite != null ? markerSprite : CreateRingSprite();
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = sortingOrder;
        SetAlpha(0f);
    }

    private void OnDisable() => Unsubscribe();

    private void Update()
    {
        // The player may be spawned after the marker, so keep looking until found.
        PlayerMover target = player != null ? player : PlayerMover.Instance;
        if (target != subscribed)
        {
            Unsubscribe();
            if (target != null)
            {
                target.OnTargetSet += Show;
                target.OnTargetCleared += Hide;
                subscribed = target;
            }
        }

        if (visible)
        {
            float t = Mathf.Clamp01((Time.time - shownAt) / popSeconds);
            float pop = Mathf.Lerp(1.6f, 1f, 1f - (1f - t) * (1f - t));          // ease-out
            float pulse = 1f + Mathf.Sin((Time.time - shownAt) * pulseSpeed) * pulseAmount * t;
            transform.localScale = Vector3.one * size * pop * pulse;
            alpha = Mathf.MoveTowards(alpha, 1f, Time.deltaTime / popSeconds);
        }
        else
        {
            alpha = Mathf.MoveTowards(alpha, 0f, Time.deltaTime / fadeSeconds);
        }
        SetAlpha(alpha);
    }

    private void Show(float x)
    {
        if (subscribed == null) return;
        Vector3 p = subscribed.transform.position;
        transform.position = new Vector3(x, p.y + yOffset, p.z + zOffset);
        shownAt = Time.time;
        alpha = 0f;
        visible = true;
    }

    private void Hide() => visible = false;

    private void Unsubscribe()
    {
        if (subscribed == null) return;
        subscribed.OnTargetSet -= Show;
        subscribed.OnTargetCleared -= Hide;
        subscribed = null;
    }

    private void SetAlpha(float a)
    {
        Color c = color;
        c.a *= a;
        sr.color = c;
        sr.enabled = a > 0.001f;
    }

    /// <summary>Flat ellipse ring so it reads as "on the floor" in 2.5D.</summary>
    private static Sprite CreateRingSprite()
    {
        const int w = 128, h = 64;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float nx = (x + 0.5f) / w * 2f - 1f;
            float ny = (y + 0.5f) / h * 2f - 1f;
            float r = Mathf.Sqrt(nx * nx + ny * ny);                // 1 = edge of ellipse
            float ring = 1f - Mathf.Clamp01(Mathf.Abs(r - 0.8f) / 0.12f);
            float dot = 1f - Mathf.Clamp01(r / 0.18f);
            byte a = (byte)(Mathf.Clamp01(Mathf.Max(ring, dot)) * 255f);
            px[y * w + x] = new Color32(255, 255, 255, a);
        }
        tex.SetPixels32(px);
        tex.Apply();
        // pixelsPerUnit = width -> sprite is 1 unit wide, scaled by "size".
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
    }
}
