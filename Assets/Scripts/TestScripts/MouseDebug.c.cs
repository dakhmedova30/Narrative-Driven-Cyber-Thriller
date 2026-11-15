using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class MouseDebug : MonoBehaviour
{
    void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

        Vector2 mouse = Mouse.current.position.ReadValue();
        string ui = "no module";
        var module = EventSystem.current != null
            ? EventSystem.current.currentInputModule as InputSystemUIInputModule : null;
        if (module != null)
            ui = module.point.action.ReadValue<Vector2>().ToString();

        Debug.Log($"Mouse: {mouse}   UI pointer: {ui}   Pointer device: {Pointer.current?.displayName}");
    }
}