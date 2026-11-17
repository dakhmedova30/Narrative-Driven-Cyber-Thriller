using UnityEngine;
using Yarn.Unity;

/// <summary>
/// When this DialogueRunner's dialogue finishes, fade to black and load another scene.
/// Example: put it on the DialogueSystem in SampleScene, Target Scene = Home
/// -> after Chapter 0 ends, the player is in Home and can walk around.
///
/// The Yarn script does not need to change.
/// Setup: Add Component on the GameObject that has the DialogueRunner (the DialogueSystem object).
/// </summary>
public class GoToSceneAfterDialogue : MonoBehaviour
{
    [SerializeField] private DialogueRunner runner;
    [Tooltip("Scene to load when the dialogue ends. Must be in File > Build Profiles > Scene List.")]
    [SerializeField] private string targetScene = "Home";
    [Tooltip("SpawnPoint Id in the target scene. Empty = its default spawn (or where the player was placed).")]
    [SerializeField] private string targetSpawn = "";
    [Tooltip("Only go if this was the last node that ran (e.g. C0_End). Empty = whenever the dialogue ends.")]
    [SerializeField] private string onlyAfterNode = "C0_End";

    private string lastNode = "";

    private void Awake()
    {
        if (runner == null) runner = GetComponent<DialogueRunner>();
        if (runner == null) Debug.LogError("[GoToSceneAfterDialogue] No DialogueRunner on " + name, this);
    }

    private void OnEnable()
    {
        if (runner == null) return;
        runner.onNodeStart?.AddListener(RememberNode);
        runner.onDialogueComplete?.AddListener(OnDialogueComplete);
    }

    private void OnDisable()
    {
        if (runner == null) return;
        runner.onNodeStart?.RemoveListener(RememberNode);
        runner.onDialogueComplete?.RemoveListener(OnDialogueComplete);
    }

    private void RememberNode(string node) => lastNode = node;

    private void OnDialogueComplete()
    {
        if (!string.IsNullOrEmpty(onlyAfterNode) && lastNode != onlyAfterNode) return;
        SceneTransitioner.Instance.GoTo(targetScene, targetSpawn);
    }
}
