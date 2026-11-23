using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yarn.Unity;

namespace RoomInteraction
{
    // Explicit opt-in player test. Never attaches during normal Play.
    public sealed class RoomExplorationSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-room-smoke") >= 0)
                new GameObject("Room verification").AddComponent<RoomExplorationSmoke>();
        }
        RoomExploration room;
        DialogueRunner runner;
        string dir;
        void Finish(int code)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(code);
#else
            Application.Quit(code);
#endif
        }
        void Assert(bool ok, string message)
        {
            if (!ok) { Debug.LogError("[Room smoke] FAIL: " + message); Finish(1); throw new Exception(message); }
            Debug.Log("[Room smoke] PASS: " + message);
        }
        IEnumerator Reach(Func<bool> condition)
        {
            float deadline = Time.realtimeSinceStartup + 30;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                runner.RequestNextLine();
                yield return new WaitForSecondsRealtime(.4f);
            }
            Assert(condition(), "Reached expected Yarn state");
        }
        IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"));
            yield return new WaitForEndOfFrame();
        }
        void Click(string name)
        {
            var button = Array.Find(room.GetComponentsInChildren<Button>(), b => b.name == name);
            Assert(button != null, "Visible button: " + name);
            var rect = (RectTransform)button.transform;
            var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)) };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button, "Pointer reaches " + name);
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
        IEnumerator Start()
        {
            Application.runInBackground = true;
            dir = Application.isEditor ? Path.Combine(Directory.GetParent(Application.dataPath).FullName, "output/chapter0-interaction/Captures") : Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Captures"); Directory.CreateDirectory(dir);
            yield return new WaitForSecondsRealtime(2);
            room = FindFirstObjectByType<RoomExploration>(); runner = room.runner;
            yield return Reach(() => room.Waiting);
            yield return Capture("01-room");
            GameSession.Instance.SetPaused(true); room.Select("calendar");
            Assert(room.Waiting, "Pause menu blocks room selection"); GameSession.Instance.SetPaused(false);
            yield return null;
            Click("Calendar");
            yield return new WaitForSecondsRealtime(1.8f);
            Assert(room.InspectionVisible && !room.Waiting, "Calendar enlarged while original Yarn line plays");
            Assert(FindFirstObjectByType<LinePresenter>().lineText.text.Contains("2027"), "Existing LinePresenter shows object dialogue");
            yield return Capture("02-calendar");
            yield return Reach(() => room.Waiting);
            Assert(!room.InspectionVisible, "Inspection closes after dialogue");
            Assert(runner.VariableStorage.TryGetValue<bool>("$room_calendar_seen", out var seen) && seen, "Yarn remembers calendar visit");
            Click("Calendar"); yield return new WaitForSecondsRealtime(1.8f);
            Assert(FindFirstObjectByType<LinePresenter>().lineText.text.Contains("Still June"), "Repeat inspection uses alternate line");
            yield return Reach(() => room.Waiting);
            foreach (var name in new[] { "Computer", "Bed", "Door" }) { Click(name); yield return new WaitForSecondsRealtime(.8f); yield return Reach(() => room.Waiting); }
            Click("Continue story");
            yield return new WaitForSecondsRealtime(1);
            Assert(runner.Dialogue.CurrentNode == "C0_RoomContinue", "Continue returns to original choices");
            yield return Reach(() => Array.Exists(FindObjectsByType<OptionItem>(FindObjectsSortMode.None), o => o.gameObject.activeInHierarchy && o.Option.Line.Text.Text.Contains("Lie down")));
            yield return Capture("03-original-choices");
            var option = Array.Find(FindObjectsByType<OptionItem>(FindObjectsSortMode.None), o => o.Option.Line.Text.Text.Contains("Lie down"));
            Assert(option != null, "Original Lie down choice exists"); option.InvokeOptionSelected();
            yield return Reach(() => runner.Dialogue.CurrentNode == "C0_Knock");
            Assert(!room.Waiting && !room.InspectionVisible, "Original story resumes without room overlay");
            // Restart only in the test harness to exercise skipping all optional objects.
            runner.Stop(); yield return new WaitForSecondsRealtime(.5f); runner.StartDialogue("C0_GoHome");
            yield return Reach(() => room.Waiting); Click("Continue story"); yield return new WaitForSecondsRealtime(1);
            Assert(runner.Dialogue.CurrentNode == "C0_RoomContinue", "All inspections can be skipped");
            File.WriteAllText(Path.Combine(dir, "verification.txt"), "PASS: pointer targeting, pause gate, calendar zoom, existing presenter, repeat variable, all four objects, original choices, story continuation, optional skip.");
            Debug.Log("[Room smoke] ALL PASSED"); Finish(0);
        }
    }
}
