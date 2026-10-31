using UnityEngine;
using UnityEngine.InputSystem;

public class MouseDebug : MonoBehaviour
{
    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
            Debug.Log("Mouse: " + Mouse.current.position.ReadValue() +
                      "  Screen: " + Screen.width + "x" + Screen.height);
    }
}