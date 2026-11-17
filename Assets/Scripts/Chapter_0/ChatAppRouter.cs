using System.Linq;
using UnityEngine;
using Yarn.Unity;

public class ChatAppRouter : MonoBehaviour
{
    public DialogueRunner runner;
    public ChatApp[] apps;
    public ChatApp Current { get; private set; }

    public DialoguePresenterBase[] defaultPresenters;   // Line Presenter, Options Presenter, Line Advancer
    public DialoguePresenterBase[] chatPresenters;      // ChatPresenter

    // hooked to the Dialogue Runner's "On Node Start" event
    public void OnNodeStart(string nodeName)
    {
        var tags = runner.Dialogue.GetHeaders(nodeName)
                    .Where(h => h.Key == "tags").SelectMany(h => h.Value.Split(' ')).ToArray();
        bool cutscene = tags.Contains("cutscene");
        bool face = tags.Contains("face");

        Current = null;
        foreach (var app in apps)
        {
            bool show = !cutscene && tags.Contains(app.appTag);
            app.gameObject.SetActive(show);
            if (show) Current = app;
        }

        if (Current != null && !face)
            runner.DialoguePresenters = (Current.presenters != null && Current.presenters.Length > 0)
                ? Current.presenters
                : chatPresenters;           // fallback so MHM keeps working until you fill it in
        else
            runner.DialoguePresenters = defaultPresenters;
    }
}