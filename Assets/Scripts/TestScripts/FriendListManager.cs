using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FriendListManager : MonoBehaviour
{
    public FriendView friendPrefab;
    public Transform content;
    public ToggleGroup group;

    Dictionary<string, FriendView> friends = new Dictionary<string, FriendView>();

    void Start()
    {
        AddFriend("Matt", Presence.Offline);   // test row
        AddFriend("Skye", Presence.Online);   // test row
    }

    public void AddFriend(string username, Presence p)
    {
        FriendView f = Instantiate(friendPrefab, content);
        f.Setup(username, p, group);
        friends[username] = f;
    }

    public void SetStatus(string username, Presence p)
    {
        if (friends.TryGetValue(username, out FriendView f)) f.SetStatus(p);
    }
}