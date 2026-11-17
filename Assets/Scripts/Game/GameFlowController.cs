using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;

/// <summary>
/// Runs the GameFlow at play time. Created by GameSession; reach it with GameSession.Instance.Flow.
/// Knows which step the player is on, moves to the next one, and starts each step's dialogue.
/// </summary>
public class GameFlowController : MonoBehaviour
{
    public GameFlow Flow { get; private set; }
    public int CurrentIndex { get; private set; } = -1;

    public GameFlow.Step CurrentStep =>
        Flow != null && CurrentIndex >= 0 && CurrentIndex < Flow.steps.Count ? Flow.steps[CurrentIndex] : null;

    public string CurrentStepName => CurrentStep != null ? CurrentStep.name : "";

    private int pendingIndex = -1;
    private bool pendingFromSave;
    private Scene handledScene;
    private readonly Dictionary<DialogueRunner, string> lastNode = new Dictionary<DialogueRunner, string>();

    private void Awake()
    {
        Flow = GameFlow.Load();
        if (Flow == null)
            Debug.LogWarning("[GameFlow] No Assets/Resources/GameFlow.asset. Create it with Tools > Game > Open Game Flow.");

#if UNITY_EDITOR
        // ▶ button in the Game Flow inspector: start at that step.
        int debugStep = UnityEditor.EditorPrefs.GetInt(GameFlow.DebugStartKey, -1);
        if (debugStep >= 0)
        {
            UnityEditor.EditorPrefs.DeleteKey(GameFlow.DebugStartKey);
            pendingIndex = debugStep;
        }
#endif
    }

    // ------------------------------------------------------------------
    // Moving between steps
    // ------------------------------------------------------------------

    /// <summary>Go to the next step (after the last one: back to the title).</summary>
    public void Next()
    {
        if (Flow == null) { GameSession.ShowToast("No Game Flow set up (Tools > Game > Open Game Flow)"); return; }
        if (SceneTransitioner.IsTransitioning) return;
        int next = CurrentIndex + 1;
        GoToStep(next < Flow.steps.Count ? next : 0);
    }

    public bool GoToStep(int index)
    {
        if (Flow == null || index < 0 || index >= Flow.steps.Count)
        {
            Debug.LogWarning($"[GameFlow] There is no step {index}.");
            return false;
        }
        GameFlow.Step step = Flow.steps[index];
        pendingIndex = index;
        pendingFromSave = false;
        if (!SceneTransitioner.Instance.GoTo(step.scene, step.spawn))
        {
            pendingIndex = -1;
            return false;
        }
        return true;
    }

    /// <summary>GameSession calls this right before loading a save.</summary>
    public void PrepareLoad(int savedStep)
    {
        pendingIndex = savedStep;
        pendingFromSave = true;
    }

    // ------------------------------------------------------------------
    // Called by GameSession
    // ------------------------------------------------------------------

    /// <summary>A scene was loaded and its runners hooked (before Start()).</summary>
    public void OnSceneReady(Scene scene, List<DialogueRunner> runners)
    {
        if (scene == handledScene) return;
        handledScene = scene;
        lastNode.Clear();
        if (Flow == null) return;

        bool fromSave = pendingFromSave;
        pendingFromSave = false;

        if (pendingIndex >= 0)
        {
            CurrentIndex = pendingIndex;
            pendingIndex = -1;
        }
        else if (CurrentIndex < 0)
        {
            // Pressed Play in some scene: guess the step from the scene.
            CurrentIndex = Flow.steps.FindIndex(s => s.scene == scene.name);
        }
        // (Walking into a non-flow area through a normal AreaExit keeps the current step.)

        GameFlow.Step step = CurrentStep;
        if (fromSave || step == null || step.scene != scene.name || string.IsNullOrEmpty(step.startNode)) return;

        // The step decides which dialogue starts, not the scene's Auto Start.
        foreach (DialogueRunner r in runners) if (r != null) r.autoStart = false;
        StartCoroutine(StartNodeNextFrame(runners, step.startNode));
    }

    public void OnNodeStart(DialogueRunner runner, string node) => lastNode[runner] = node;

    public void OnDialogueComplete(DialogueRunner runner)
    {
        GameFlow.Step step = CurrentStep;
        if (step == null || string.IsNullOrEmpty(step.nextAfterNode)) return;
        if (lastNode.TryGetValue(runner, out string node) && node == step.nextAfterNode) Next();
    }

    private IEnumerator StartNodeNextFrame(List<DialogueRunner> runners, string node)
    {
        yield return null; // after every Start()
        DialogueRunner runner = runners.FirstOrDefault(r => r != null && NodeExists(r, node));
        if (runner == null)
        {
            Debug.LogWarning($"[GameFlow] Step \"{CurrentStepName}\": no DialogueRunner in this scene has node \"{node}\".");
            yield break;
        }
        if (!runner.IsDialogueRunning) runner.StartDialogue(node);
    }

    private static bool NodeExists(DialogueRunner r, string node)
    {
        try { return r.Dialogue.NodeExists(node); } catch { return false; }
    }
}
