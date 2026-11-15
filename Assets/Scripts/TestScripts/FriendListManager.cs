using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FriendListManager : MonoBehaviour
{
    public FriendView friendPrefab;
    public Transform content;
    public ToggleGroup group;

    Dictionary<string, FriendView> friends = new Dictionary<string, FriendView>();

    [System.Serializable]
    public class FriendData { public string username; public Presence presence; }
    public FriendData[] startingFriends;

    void Start()
    {
        foreach (var f in startingFriends) AddFriend(f.username, f.presence);
    }

    // public void AddFriend(string username, Presence p)
    // {
    //     FriendView f = Instantiate(friendPrefab, content);
    //     f.Setup(username, p, group);
    //     friends[username] = f;
    // }

    public ChatManager chat;

    void OnFriendSelected(string username) { chat.OpenRoom(username); }

    public void AddFriend(string username, Presence p)
    {
        FriendView f = Instantiate(friendPrefab, content);
        f.Setup(username, p, group, OnFriendSelected);
        friends[username] = f;

        if (friends.Count == 1) f.toggle.isOn = true;   // first friend opens by default
    }

    public void SetStatus(string username, Presence p)
    {
        if (friends.TryGetValue(username, out FriendView f)) f.SetStatus(p);
    }

    void OnEnable()  { chat.RoomUpdated += OnRoomUpdated; }
    void OnDisable() { chat.RoomUpdated -= OnRoomUpdated; }

    void OnRoomUpdated(string room, string preview, int unread)
    {
        if (friends.TryGetValue(room, out FriendView f))
        {
            f.SetPreview(preview);
            f.SetUnread(unread);
        }
    }
}