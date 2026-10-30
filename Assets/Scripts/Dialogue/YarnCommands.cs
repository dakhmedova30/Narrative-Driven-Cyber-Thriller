using System.Collections;
using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Registers every custom Yarn command used in the story (<<bg>>, <<camera>>, ...).
///
/// STEP 0 (current): every command only logs to the Console, so the whole
/// chapter runs without "command not found" errors.
/// Later, each stub gets replaced by a call into the real system
/// (CameraDirector, AudioManager, SceneDirector, ScreenFader...).
///
/// Rule of thumb:
///   - method returns void        -> dialogue continues immediately
///   - method returns IEnumerator -> dialogue waits until it finishes
///
/// Put this component on the same GameObject as the DialogueRunner.
/// </summary>
public class YarnCommands : MonoBehaviour
{
    [SerializeField] private DialogueRunner runner;

    [Tooltip("Log every command to the Console. Turn off once the real systems are in.")]
    [SerializeField] private bool logCommands = true;

    private void Awake()
    {
        if (runner == null) runner = GetComponent<DialogueRunner>();
        if (runner == null)
        {
            Debug.LogError("[YarnCommands] No DialogueRunner assigned or found on this GameObject.");
            return;
        }

        // ---- Scene / props -------------------------------------------------
        runner.AddCommandHandler<string>("bg", SetBackground);           // <<bg room_2027>>
        runner.AddCommandHandler<string>("show", Show);                  // <<show crow>>
        runner.AddCommandHandler<string>("hide", Hide);                  // <<hide crow>>

        // ---- Camera / screen (dialogue waits) -----------------------------
        runner.AddCommandHandler<string, string>("camera", MoveCamera);  // <<camera zoom leo_desk>>
        runner.AddCommandHandler<float>("fade_out", FadeOut);            // <<fade_out 1>>
        runner.AddCommandHandler<float>("fade_in", FadeIn);              // <<fade_in 1>>

        // ---- Audio ---------------------------------------------------------
        runner.AddCommandHandler<string>("sfx", PlaySfx);                // <<sfx knock>>

        // ---- Flow ----------------------------------------------------------
        runner.AddCommandHandler<int>("chapter_end", EndChapter);        // <<chapter_end 0>>
    }

    // ===================================================================
    // Stubs. Replace the body of each one when its system is ready.
    // ===================================================================

    private void SetBackground(string name)
    {
        Log($"bg -> {name}");
        if (BackgroundManager.Instance != null) BackgroundManager.Instance.Show(name);
        else Debug.LogWarning("[Yarn] <<bg>>: there is no BackgroundManager in this scene.");
    }
    private void Show(string name)          => Log($"show -> {name}");
    private void Hide(string name)          => Log($"hide -> {name}");
    private void PlaySfx(string name)       => Log($"sfx -> {name}");

    private IEnumerator MoveCamera(string action, string target)
    {
        Log($"camera -> {action} {target}");
        yield return null; // TODO: CameraDirector. Dialogue will wait for the move.
    }

    private IEnumerator FadeOut(float seconds)
    {
        Log($"fade_out -> {seconds}s");
        yield return new WaitForSeconds(seconds); // TODO: ScreenFader
    }

    private IEnumerator FadeIn(float seconds)
    {
        Log($"fade_in -> {seconds}s");
        yield return new WaitForSeconds(seconds); // TODO: ScreenFader
    }

    private void EndChapter(int chapter) => Log($"chapter_end -> {chapter}");

    private void Log(string message)
    {
        if (logCommands) Debug.Log($"<color=#7fd3ff>[Yarn]</color> {message}");
    }
}
