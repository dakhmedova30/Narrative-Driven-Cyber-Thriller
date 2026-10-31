using UnityEngine;
using UnityEngine.UI;

public enum Presence { Online, Away, Busy, Offline }

[RequireComponent(typeof(Image))]
public class StatusIcon : MonoBehaviour
{
    public Sprite online, away, busy, offline;
    Image img;

    public void Set(Presence p)
    {
        if (img == null) img = GetComponent<Image>();
        img.sprite = p switch
        {
            Presence.Online => online,
            Presence.Away => away,
            Presence.Busy => busy,
            _ => offline
        };
    }
}