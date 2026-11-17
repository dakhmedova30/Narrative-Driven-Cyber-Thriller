using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Reads player input and drives PlayerMover:
///   - A / D             -> hold to walk
///   - Left / Right arrow -> hold to walk
///   - Left click / tap   -> walk to the X of the clicked point
/// Keyboard always wins: pressing a key cancels a click target.
/// Clicks on UI (dialogue box, Jaycord, buttons) are ignored.
///
/// Setup: put this on the same GameObject as PlayerMover. Camera is Camera.main if left empty.
/// </summary>
[RequireComponent(typeof(PlayerMover))]
public class PlayerInputHandler : MonoBehaviour
{
    [Header("Which controls are on")]
    [SerializeField] private bool useAD = true;
    [SerializeField] private bool useArrowKeys = true;
    [SerializeField] private bool useClickToMove = true;

    [Header("Click to move")]
    [Tooltip("Camera that renders the world. Uses Camera.main if empty.")]
    [SerializeField] private Camera worldCamera;
    [Tooltip("Ignore clicks that land on UI elements.")]
    [SerializeField] private bool ignoreClicksOnUI = true;

    private PlayerMover mover;

    private void Awake()
    {
        mover = GetComponent<PlayerMover>();
    }

    private void OnDisable()
    {
        if (mover != null) mover.SetInputDirection(0f);
    }

    private void Update()
    {
        // ---- Keyboard ----
        bool left = false, right = false;
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (useAD)
            {
                left |= kb.aKey.isPressed;
                right |= kb.dKey.isPressed;
            }
            if (useArrowKeys)
            {
                left |= kb.leftArrowKey.isPressed;
                right |= kb.rightArrowKey.isPressed;
            }
        }
        float dir = (right ? 1f : 0f) - (left ? 1f : 0f);
        mover.SetInputDirection(dir);

        // ---- Click / tap ----
        if (!useClickToMove || dir != 0f || !mover.CanMove) return;

        Pointer pointer = Pointer.current; // mouse, pen or touchscreen
        if (pointer == null || !pointer.press.wasPressedThisFrame) return;
        if (ignoreClicksOnUI && IsPointerOverUI()) return;

        if (TryGetClickX(pointer.position.ReadValue(), out float x))
            mover.MoveTo(x);
    }

    private bool IsPointerOverUI()
    {
        EventSystem es = EventSystem.current;
        if (es == null) return false;
        if (es.IsPointerOverGameObject()) return true;

        // Touchscreens report the pointer per finger.
        Touchscreen ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.isPressed)
            return es.IsPointerOverGameObject(ts.primaryTouch.touchId.ReadValue());
        return false;
    }

    /// <summary>
    /// Shoots a ray from the camera through the screen point and hits the vertical
    /// plane the player stands in (z = player's z). Works for perspective and orthographic cameras.
    /// </summary>
    private bool TryGetClickX(Vector2 screenPos, out float x)
    {
        x = 0f;
        Camera cam = worldCamera != null ? worldCamera : Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[PlayerInputHandler] No camera found (assign World Camera or tag one MainCamera).");
            return false;
        }

        Ray ray = cam.ScreenPointToRay(screenPos);
        Plane walkPlane = new Plane(Vector3.forward, transform.position);
        if (!walkPlane.Raycast(ray, out float distance)) return false;

        x = ray.GetPoint(distance).x;
        return true;
    }
}
