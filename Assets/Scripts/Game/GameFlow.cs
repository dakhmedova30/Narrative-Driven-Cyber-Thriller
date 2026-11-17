using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The main story order of the whole game, in ONE place: Title → Chapter 0 → Street → …
/// Edit it with Tools > Game > Open Game Flow (asset lives at Assets/Resources/GameFlow.asset).
///
/// Each step = a scene + where to stand + which dialogue starts when you arrive.
/// Moving to the next step:
///   - "Next After Node": that Yarn node finished → next step automatically
///   - Yarn: &lt;&lt;next&gt;&gt;      (any step)
///   - an AreaExit with "Advances Game Flow" ticked (any step)
///   - code: GameSession.Instance.Flow.Next()
/// Step 0 is the title screen. After the last step, the game returns to the title.
/// </summary>
[CreateAssetMenu(fileName = "GameFlow", menuName = "Game/Game Flow")]
public class GameFlow : ScriptableObject
{
    public const string ResourcePath = "GameFlow";              // Assets/Resources/GameFlow.asset
    public const string DebugStartKey = "GameFlow.debugStartStep"; // editor ▶ buttons

    [Serializable]
    public class Step
    {
        [Tooltip("Your name for this step. Also shown as the progress in save slots.")]
        public string name = "New Step";
        [Tooltip("Scene to load for this step.")]
        public string scene = "";
        [Tooltip("SpawnPoint Id in that scene. Empty = the scene's default spawn.")]
        public string spawn = "";
        [Tooltip("Yarn node that starts when the step begins. Empty = nothing starts (the scene's own Auto Start still works).")]
        public string startNode = "";
        [Tooltip("When this Yarn node finishes, go to the next step automatically. Empty = only <<next>> / a flow exit moves on.")]
        public string nextAfterNode = "";
    }

    public List<Step> steps = new List<Step>();

    public static GameFlow Load() => Resources.Load<GameFlow>(ResourcePath);
}
