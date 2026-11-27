using UnityEngine;
using UnityEngine.Events;

namespace Exploration
{
    [DisallowMultipleComponent]
    public sealed class Interactable : MonoBehaviour
    {
        public string displayName = "Unknown object";
        public string category = "EVIDENCE";
        [TextArea(3, 8)] public string description = "Describe what the player discovers here.";
        public string actionLabel = "COLLECT";
        public Texture2D artwork;
        public bool oneShot = true;
        public bool hideWhenCompleted;
        [Tooltip("Optional. Leave empty for an immediately available object.")]
        public Interactable prerequisite;
        [Tooltip("0 = click from anywhere. Otherwise requires PlayerMover within this horizontal distance.")]
        [Min(0)] public float interactionDistance;
        public UnityEvent onCompleted = new UnityEvent();
        public bool Completed { get; private set; }
        public bool Available => prerequisite == null || prerequisite.Completed;
        public bool InRange => interactionDistance <= 0 || (PlayerMover.Instance != null &&
            Mathf.Abs(PlayerMover.Instance.transform.position.x - transform.position.x) <= interactionDistance);
        public string BlockReason => !Available ? "First: " + prerequisite.displayName : !InRange ? "Move closer to inspect" : "";
        public bool CanInteract => isActiveAndEnabled && Available && InRange && !(oneShot && Completed);
        public bool Complete()
        {
            if (!CanInteract) return false;
            Completed = true;
            onCompleted.Invoke();
            if (hideWhenCompleted) gameObject.SetActive(false);
            return true;
        }
        public void ResetInteraction() => Completed = false;
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, .2f);
            if (interactionDistance > 0) Gizmos.DrawWireSphere(transform.position, interactionDistance);
        }
    }
}
