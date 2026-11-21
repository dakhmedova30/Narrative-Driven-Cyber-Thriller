using UnityEngine;
namespace Exploration
{
    public sealed class InteractionDemoCamera : MonoBehaviour
    {
        Camera cam;
        void Awake()=>cam=GetComponent<Camera>();
        void LateUpdate()
        {
            float s=Mathf.Min(Screen.width/1600f,Screen.height/900f);
            float left=(Screen.width-1600*s)/2, bottom=(Screen.height-900*s)/2;
            cam.rect=new Rect((left+40*s)/Screen.width,(bottom+90*s)/Screen.height,1120*s/Screen.width,630*s/Screen.height);
        }
    }
}
