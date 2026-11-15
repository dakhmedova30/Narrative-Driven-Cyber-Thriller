using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Cinemachine extension for the 2.5D side view: stops the camera at the left / right
/// edges of the scene, so the player never sees past the end of the background art.
///
/// Put this on the CinemachineCamera that follows the player (the menu
/// Tools > Camera > Create Player Follow Camera does it for you).
///
/// Left Edge / Right Edge are world X positions measured at the PLAYER's depth.
/// Select the camera in the Scene view to see them as two red lines.
/// If the scene is narrower than the screen, the camera stays centred between the edges.
/// Assumes the camera looks straight along +Z (normal side view).
/// </summary>
[AddComponentMenu("Cinemachine/Procedural/Extensions/Camera X Limits (2.5D)")]
[ExecuteAlways]
[SaveDuringPlay]
[DisallowMultipleComponent]
public class CameraXLimits : CinemachineExtension
{
    [Tooltip("World X of the left end of the scene. The camera never shows anything left of this.")]
    public float leftEdge = -10f;
    [Tooltip("World X of the right end of the scene. The camera never shows anything right of this.")]
    public float rightEdge = 10f;
    [Tooltip("Turn off to let the camera go anywhere (e.g. while testing).")]
    public bool limitsEnabled = true;

    /// <summary>Change the limits at runtime, e.g. when entering a new room.</summary>
    public void SetEdges(float left, float right)
    {
        leftEdge = Mathf.Min(left, right);
        rightEdge = Mathf.Max(left, right);
    }

    private void OnValidate()
    {
        if (rightEdge < leftEdge) rightEdge = leftEdge;
    }

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (!limitsEnabled || stage != CinemachineCore.Stage.Body) return;

        Vector3 pos = state.GetCorrectedPosition();
        float halfWidth = HalfViewWidth(vcam, state, pos);

        float min = leftEdge + halfWidth;
        float max = rightEdge - halfWidth;
        float x = min > max ? (leftEdge + rightEdge) * 0.5f : Mathf.Clamp(pos.x, min, max);

        state.PositionCorrection += new Vector3(x - pos.x, 0f, 0f);
    }

    /// <summary>Half of how wide the view is (in world units) at the player's depth.</summary>
    private static float HalfViewWidth(CinemachineVirtualCameraBase vcam, CameraState state, Vector3 camPos)
    {
        float aspect = state.Lens.Aspect > 0.01f ? state.Lens.Aspect : 16f / 9f;
        if (state.Lens.Orthographic) return state.Lens.OrthographicSize * aspect;

        float depth = vcam.Follow != null ? Mathf.Abs(vcam.Follow.position.z - camPos.z) : Mathf.Abs(camPos.z);
        return depth * Mathf.Tan(state.Lens.FieldOfView * 0.5f * Mathf.Deg2Rad) * aspect;
    }

    private void OnDrawGizmosSelected()
    {
        float y = transform.position.y, z = 0f;
        var vcam = GetComponent<CinemachineVirtualCameraBase>();
        if (vcam != null && vcam.Follow != null)
        {
            y = vcam.Follow.position.y;
            z = vcam.Follow.position.z;
        }
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(leftEdge, y - 5f, z), new Vector3(leftEdge, y + 5f, z));
        Gizmos.DrawLine(new Vector3(rightEdge, y - 5f, z), new Vector3(rightEdge, y + 5f, z));
    }
}
