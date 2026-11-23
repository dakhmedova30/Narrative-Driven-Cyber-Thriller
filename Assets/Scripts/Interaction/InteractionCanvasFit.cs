using UnityEngine;
namespace Exploration
{
    public sealed class InteractionCanvasFit : MonoBehaviour
    {
        void LateUpdate() => transform.localScale = Vector3.one * Mathf.Min(Screen.width/1600f, Screen.height/900f);
    }
}
