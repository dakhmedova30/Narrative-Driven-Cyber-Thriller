using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu: Tools > Camera > Create Player Follow Camera
///
/// One click in the open scene:
///   - Main Camera gets a CinemachineBrain (creates a Main Camera if there is none)
///   - new "CM Player Follow" CinemachineCamera that follows the PlayerMover
///       * Position Composer: dead zone in the middle of the screen, soft damping
///       * CameraXLimits: stops at the scene edges (starts at the player's Min/Max X + 2)
///   - keeps the current camera's angle, lens and how the player is framed
/// Undo (Ctrl+Z) removes everything it made.
/// </summary>
public static class PlayerCameraBuilder
{
    private const string Title = "Player Follow Camera";

    [MenuItem("Tools/Camera/Create Player Follow Camera")]
    public static void Create()
    {
        PlayerMover player = Object.FindAnyObjectByType<PlayerMover>();
        if (player == null)
        {
            EditorUtility.DisplayDialog(Title, "No PlayerMover found in the open scene.\nAdd PlayerMover to the player first.", "OK");
            return;
        }

        CameraXLimits existing = Object.FindAnyObjectByType<CameraXLimits>();
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorUtility.DisplayDialog(Title, $"This scene already has a follow camera: \"{existing.name}\" (now selected).", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Create Player Follow Camera");
        int group = Undo.GetCurrentGroup();

        // ---- Real camera + brain ----
        Camera cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            Undo.RegisterCreatedObjectUndo(camGo, "Create Main Camera");
            cam = camGo.AddComponent<Camera>();
            camGo.transform.position = player.transform.position + new Vector3(0f, 1f, -10f);
        }
        if (cam.GetComponent<CinemachineBrain>() == null)
            Undo.AddComponent<CinemachineBrain>(cam.gameObject);

        // ---- Follow camera ----
        var go = new GameObject("CM Player Follow");
        Undo.RegisterCreatedObjectUndo(go, "Create CM Player Follow");
        go.transform.SetPositionAndRotation(cam.transform.position, cam.transform.rotation);

        var vcam = go.AddComponent<CinemachineCamera>();
        vcam.Target.TrackingTarget = player.transform;
        vcam.Priority = 10;

        LensSettings lens = vcam.Lens;
        lens.FieldOfView = cam.fieldOfView;
        lens.OrthographicSize = cam.orthographicSize;
        lens.NearClipPlane = cam.nearClipPlane;
        lens.FarClipPlane = cam.farClipPlane;
        vcam.Lens = lens;

        var composer = go.AddComponent<CinemachinePositionComposer>();
        float distance = Mathf.Abs(player.transform.position.z - cam.transform.position.z);
        composer.CameraDistance = distance > 0.5f ? distance : 10f;
        composer.Damping = new Vector3(0.6f, 0.6f, 0f);

        ScreenComposerSettings comp = composer.Composition;
        comp.ScreenPosition = CurrentFraming(cam, player.transform.position);
        comp.DeadZone.Enabled = true;
        comp.DeadZone.Size = new Vector2(0.3f, 1f); // walk freely in the middle 30% of the screen; ignore height
        composer.Composition = comp;

        var limits = go.AddComponent<CameraXLimits>();
        limits.SetEdges(player.MinX - 2f, player.MaxX + 2f);

        Undo.CollapseUndoOperations(group);
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
        Debug.Log("[PlayerCameraBuilder] Created \"CM Player Follow\". Adjust Left/Right Edge on Camera X Limits to match the background.");
    }

    /// <summary>Keep the player where the current camera already shows them (if visible).</summary>
    private static Vector2 CurrentFraming(Camera cam, Vector3 playerPos)
    {
        Vector3 vp = cam.WorldToViewportPoint(playerPos);
        bool visible = vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
        if (!visible) return new Vector2(0f, 0.15f); // Cinemachine screen Y points DOWN: +0.15 = a bit below centre
        return new Vector2(0f, Mathf.Clamp(0.5f - vp.y, -0.45f, 0.45f));
    }
}
