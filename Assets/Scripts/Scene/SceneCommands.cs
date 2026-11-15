using System.Collections;
using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Yarn command to change area from a dialogue:
///
///   <<goto Street from_home>>     go to scene "Street", stand on SpawnPoint "from_home"
///   <<goto Street default>>       use Street's default SpawnPoint
///
/// Put it as the LAST line of a node: the dialogue in the old scene stops when that scene unloads.
/// Setup: put this on the same GameObject as the DialogueRunner (like YarnCommands).
/// </summary>
public class SceneCommands : MonoBehaviour
{
    [SerializeField] private DialogueRunner runner;

    private void Awake()
    {
        if (runner == null) runner = GetComponent<DialogueRunner>();
        if (runner == null)
        {
            Debug.LogError("[SceneCommands] No DialogueRunner assigned or found on this GameObject.");
            return;
        }
        runner.AddCommandHandler<string, string>("goto", GoTo);   // <<goto Street from_home>>
    }

    // Returns IEnumerator so the dialogue waits instead of showing the next line during the fade.
    private IEnumerator GoTo(string sceneName, string spawnId)
    {
        if (!SceneTransitioner.Instance.GoTo(sceneName, spawnId)) yield break;
        while (SceneTransitioner.IsTransitioning) yield return null;
    }
}
