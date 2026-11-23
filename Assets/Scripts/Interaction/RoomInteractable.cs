using UnityEngine;

namespace RoomInteraction
{
    /// <summary>A clickable region on a background. Story text stays in Yarn.</summary>
    public sealed class RoomInteractable : MonoBehaviour
    {
        public string itemId = "calendar";
        public string roomId = "bedroom";
        public string displayName = "Calendar";
        [Tooltip("Normalized background coordinates, measured from bottom left.")]
        public Rect region = new Rect(.328f, .639f, .146f, .278f);
        public Texture2D inspectionImage;
        public Rect inspectionCrop = new Rect(0, 0, 1, 1);
    }
}
