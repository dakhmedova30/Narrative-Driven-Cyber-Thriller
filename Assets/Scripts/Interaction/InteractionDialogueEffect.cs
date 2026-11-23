using UnityEngine;
using Yarn.Unity;

namespace Exploration
{
    [RequireComponent(typeof(Interactable))]
    public sealed class InteractionDialogueEffect : MonoBehaviour
    {
        public DialogueRunner runner;
        [Tooltip("The title of an existing Yarn conversation, not the filename.")]
        public string conversationName;
        void OnEnable() => GetComponent<Interactable>().onCompleted.AddListener(Play);
        void OnDisable() => GetComponent<Interactable>().onCompleted.RemoveListener(Play);
        public void Play()
        {
            if (runner == null || string.IsNullOrWhiteSpace(conversationName))
            {
                Debug.LogWarning("Assign a DialogueRunner and conversation name on " + name, this); return;
            }
            if (runner.IsDialogueRunning) return;
            if (!runner.Dialogue.NodeExists(conversationName))
            {
                Debug.LogWarning("Conversation is not in the assigned Yarn project: " + conversationName, this); return;
            }
            runner.StartDialogue(conversationName);
        }
    }
}
