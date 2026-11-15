using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moves a 2.5D sprite character left / right along world X only (Y and Z never change).
///
/// This script only knows HOW to move. It does not read the keyboard or mouse:
///   - PlayerInputHandler  -> keyboard (A/D, arrows) and mouse click
///   - PlayerCommands      -> Yarn <<lock_move>> / <<unlock_move>>
///   - any other script    -> PlayerMover.Instance.MoveTo(x), Stop(), Lock("reason")...
///
/// Setup:
///   1. Put this on the player GameObject (the one with the SpriteRenderer, or its parent).
///   2. Drag the SpriteRenderer into "Sprite Renderer" (auto-found in children if empty).
///   3. Set Min X / Max X. Select the player in the Scene view to see the two yellow
///      boundary lines and line them up with the walkable floor.
///   4. (Optional) Animator with a float "Speed" and/or bool "IsWalking" parameter.
/// </summary>
[DisallowMultipleComponent]
public class PlayerMover : MonoBehaviour
{
    public static PlayerMover Instance { get; private set; }

    [Header("Speed")]
    [Tooltip("Walking speed in world units per second.")]
    [SerializeField, Min(0f)] private float moveSpeed = 3f;
    [Tooltip("How fast the character speeds up / slows down (units/s²). 0 = start and stop instantly.")]
    [SerializeField, Min(0f)] private float acceleration = 25f;
    [Tooltip("How close to a click target counts as 'arrived'.")]
    [SerializeField, Min(0.001f)] private float arriveDistance = 0.03f;

    [Header("Walkable range (world X)")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private float minX = -8f;
    [SerializeField] private float maxX = 8f;

    [Header("Facing")]
    [Tooltip("Flipped with flipX when walking left/right. Auto-found in children if empty.")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Tick if the original sprite art looks to the RIGHT. Untick if it looks left.")]
    [SerializeField] private bool spriteFacesRight = true;

    [Header("Animation (optional)")]
    [Tooltip("Auto-found in children if empty. Parameters that don't exist are simply skipped.")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";        // float: 0 .. moveSpeed
    [SerializeField] private string walkingParam = "IsWalking";  // bool

    /// <summary>A click / MoveTo target was set. Argument = target X.</summary>
    public event Action<float> OnTargetSet;
    /// <summary>The target is gone (arrived, cancelled by keyboard, Stop(), or a lock).</summary>
    public event Action OnTargetCleared;
    /// <summary>Reached the MoveTo / click target.</summary>
    public event Action OnArrived;

    public bool CanMove => locks.Count == 0;
    public bool IsMoving => Mathf.Abs(velocity) > 0.001f;
    public bool HasTarget => hasTarget;
    public float TargetX => targetX;
    public bool FacingRight => facingRight;
    public float MinX => minX;
    public float MaxX => maxX;

    private readonly HashSet<string> locks = new HashSet<string>();
    private float inputDir;      // -1, 0, 1 from keyboard
    private bool hasTarget;
    private float targetX;
    private float velocity;
    private bool facingRight = true;

    private int speedHash, walkingHash;
    private bool hasSpeedParam, hasWalkingParam;

    // ------------------------------------------------------------------
    // Unity
    // ------------------------------------------------------------------

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        CacheAnimatorParams();
        if (spriteRenderer != null) facingRight = spriteRenderer.flipX != spriteFacesRight;
    }

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("[PlayerMover] More than one PlayerMover is active. Instance now points to " + name);
        Instance = this;
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    private void OnValidate()
    {
        if (maxX < minX) maxX = minX;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        Vector3 pos = transform.position;
        float desired = 0f;

        if (CanMove)
        {
            if (inputDir != 0f)
            {
                desired = inputDir * moveSpeed;
            }
            else if (hasTarget)
            {
                float dx = targetX - pos.x;
                if (Mathf.Abs(dx) <= arriveDistance)
                {
                    Arrive(ref pos);
                }
                else
                {
                    float speed = moveSpeed;
                    // Slow down near the target so it doesn't overshoot.
                    if (acceleration > 0f) speed = Mathf.Min(speed, Mathf.Sqrt(2f * acceleration * Mathf.Abs(dx)));
                    desired = Mathf.Sign(dx) * speed;
                }
            }
        }

        velocity = acceleration <= 0f ? desired : Mathf.MoveTowards(velocity, desired, acceleration * dt);

        float newX = pos.x + velocity * dt;

        // Don't walk past the target.
        if (hasTarget && velocity != 0f && Mathf.Sign(targetX - pos.x) == Mathf.Sign(velocity)
            && Mathf.Abs(newX - pos.x) >= Mathf.Abs(targetX - pos.x))
        {
            newX = targetX;
        }

        if (useBounds)
        {
            float clamped = Mathf.Clamp(newX, minX, maxX);
            if (clamped != newX) velocity = 0f; // hit the edge
            newX = clamped;
        }

        pos.x = newX;
        transform.position = pos;

        if (hasTarget && Mathf.Abs(targetX - pos.x) <= arriveDistance && inputDir == 0f)
            Arrive(ref pos);

        // Facing: follow where we WANT to go, so turning feels instant.
        float faceSign = desired != 0f ? desired : velocity;
        if (faceSign > 0.001f) SetFacing(true);
        else if (faceSign < -0.001f) SetFacing(false);

        UpdateAnimator();
    }

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    /// <summary>Keyboard-style input: -1 = left, 0 = none, 1 = right. Cancels any click target.</summary>
    public void SetInputDirection(float direction)
    {
        inputDir = direction > 0f ? 1f : direction < 0f ? -1f : 0f;
        if (inputDir != 0f && hasTarget && CanMove) ClearTarget();
    }

    /// <summary>Walk to a world X position (clamped to the walkable range). Ignored while locked.</summary>
    public void MoveTo(float x)
    {
        if (!CanMove) return;
        if (useBounds) x = Mathf.Clamp(x, minX, maxX);
        targetX = x;
        hasTarget = true;
        OnTargetSet?.Invoke(targetX);
    }

    /// <summary>Stop immediately and forget any target.</summary>
    public void Stop()
    {
        inputDir = 0f;
        velocity = 0f;
        if (hasTarget) ClearTarget();
        UpdateAnimator();
    }

    /// <summary>Jump straight to an X position (e.g. when a scene loads).</summary>
    public void Teleport(float x)
    {
        Stop();
        Vector3 pos = transform.position;
        pos.x = useBounds ? Mathf.Clamp(x, minX, maxX) : x;
        transform.position = pos;
    }

    /// <summary>Change the walkable range at runtime (e.g. a new room).</summary>
    public void SetBounds(float newMinX, float newMaxX)
    {
        minX = Mathf.Min(newMinX, newMaxX);
        maxX = Mathf.Max(newMinX, newMaxX);
    }

    /// <summary>Face left or right without moving (e.g. turn to look at someone).</summary>
    public void SetFacing(bool right)
    {
        facingRight = right;
        if (spriteRenderer != null) spriteRenderer.flipX = right != spriteFacesRight;
    }

    /// <summary>
    /// Block movement. Each reason is tracked separately, so "dialogue" and "phone"
    /// can both lock, and the player only moves again when ALL reasons are unlocked.
    /// </summary>
    public void Lock(string reason = "default")
    {
        locks.Add(reason);
        Stop();
    }

    public void Unlock(string reason = "default")
    {
        locks.Remove(reason);
    }

    public void UnlockAll()
    {
        locks.Clear();
    }

    public bool IsLockedBy(string reason) => locks.Contains(reason);

    // ------------------------------------------------------------------
    // Internals
    // ------------------------------------------------------------------

    private void Arrive(ref Vector3 pos)
    {
        pos.x = targetX;
        transform.position = pos;
        velocity = 0f;
        ClearTarget();
        OnArrived?.Invoke();
    }

    private void ClearTarget()
    {
        hasTarget = false;
        OnTargetCleared?.Invoke();
    }

    private void CacheAnimatorParams()
    {
        hasSpeedParam = hasWalkingParam = false;
        if (animator == null || animator.runtimeAnimatorController == null) return;

        speedHash = Animator.StringToHash(speedParam);
        walkingHash = Animator.StringToHash(walkingParam);
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.nameHash == speedHash && p.type == AnimatorControllerParameterType.Float) hasSpeedParam = true;
            if (p.nameHash == walkingHash && p.type == AnimatorControllerParameterType.Bool) hasWalkingParam = true;
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        if (hasSpeedParam) animator.SetFloat(speedHash, Mathf.Abs(velocity));
        if (hasWalkingParam) animator.SetBool(walkingHash, IsMoving);
    }

    private void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        Vector3 p = transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(minX, p.y - 3f, p.z), new Vector3(minX, p.y + 3f, p.z));
        Gizmos.DrawLine(new Vector3(maxX, p.y - 3f, p.z), new Vector3(maxX, p.y + 3f, p.z));
        Gizmos.DrawLine(new Vector3(minX, p.y, p.z), new Vector3(maxX, p.y, p.z));

        if (Application.isPlaying && hasTarget)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(new Vector3(targetX, p.y, p.z), 0.15f);
        }
    }
}
