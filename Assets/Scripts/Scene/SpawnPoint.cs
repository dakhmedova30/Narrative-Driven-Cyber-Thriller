using UnityEngine;

/// <summary>
/// Where the player appears when entering this area.
/// Only the X position is used (the player keeps the height it has in this scene).
///
/// Setup: empty GameObject at the spot, add SpawnPoint, give it an Id (e.g. "from_street").
/// An AreaExit in ANOTHER scene uses this Id as its Target Spawn.
/// Keep spawn points OUTSIDE exit zones, or the player lands right on an exit.
/// Tick "Is Default" on one spawn per scene: used when no / an unknown Id is given.
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private string id = "default";
    [SerializeField] private bool faceRight = true;
    [SerializeField] private bool isDefault;

    public string Id => id;
    public bool FaceRight => faceRight;
    public bool IsDefault => isDefault;

    private void OnDrawGizmos()
    {
        Vector3 p = transform.position;
        Gizmos.color = new Color(0.3f, 1f, 0.4f);
        Gizmos.DrawWireSphere(p, 0.25f);
        Vector3 dir = faceRight ? Vector3.right : Vector3.left;
        Gizmos.DrawLine(p, p + dir * 0.7f);
        Gizmos.DrawLine(p + dir * 0.7f, p + dir * 0.5f + Vector3.up * 0.15f);
        Gizmos.DrawLine(p + dir * 0.7f, p + dir * 0.5f + Vector3.down * 0.15f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(p + Vector3.up * 0.5f, "spawn: " + id + (isDefault ? " (default)" : ""));
#endif
    }
}
