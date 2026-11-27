using UnityEngine;
using UnityEngine.EventSystems;
namespace CafeGameplay
{
    public sealed class CoffeePressControl : MonoBehaviour,IPointerDownHandler,IPointerUpHandler
    {
        public CoffeeQTE owner;
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)owner.Press("pointer");}
        public void OnPointerUp(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)owner.Release("pointer");}
        void OnDisable(){if(owner!=null)owner.Release("pointer");}
    }
}
