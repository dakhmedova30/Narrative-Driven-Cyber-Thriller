using UnityEngine;

/// <summary>
/// Simple frame-by-frame walk animation for a pixel-art sprite. No Animator needed.
/// Standing still -> shows Idle Frame. Walking -> loops Walk Frames in order.
///
/// Setup (on the player, next to PlayerMover):
///   1. Idle Frame  = frame 1 (standing).
///   2. Walk Frames = drag in this order: 1, 2, 1, 3   (size 4).
///   3. Frames Per Second: 8 is a normal walk. If the feet look like they slide,
///      change this or PlayerMover's Move Speed until they match.
/// Facing left/right is handled by PlayerMover (flipX), not here.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteWalkAnimator : MonoBehaviour
{
    [Tooltip("Found on this object or a parent if empty.")]
    [SerializeField] private PlayerMover mover;
    [SerializeField] private Sprite idleFrame;
    [Tooltip("Played in order while walking. For a 3-frame walk: 1, 2, 1, 3.")]
    [SerializeField] private Sprite[] walkFrames = new Sprite[0];
    [SerializeField, Min(0.1f)] private float framesPerSecond = 8f;

    private SpriteRenderer sr;
    private int index;
    private float timer;
    private bool wasWalking;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (mover == null) mover = GetComponentInParent<PlayerMover>();
        if (idleFrame != null) sr.sprite = idleFrame;
    }

    private void Update()
    {
        bool walking = mover != null && mover.IsMoving && walkFrames.Length > 0;

        if (!walking)
        {
            if (idleFrame != null) sr.sprite = idleFrame;
            wasWalking = false;
            return;
        }

        if (!wasWalking)
        {
            // Start on a stepping frame (not the standing one) so the first step shows right away.
            index = walkFrames.Length > 1 ? 1 : 0;
            timer = 0f;
            wasWalking = true;
        }
        else
        {
            timer += Time.deltaTime;
            float frameTime = 1f / framesPerSecond;
            while (timer >= frameTime)
            {
                timer -= frameTime;
                index = (index + 1) % walkFrames.Length;
            }
        }

        if (walkFrames[index] != null) sr.sprite = walkFrames[index];
    }
}
