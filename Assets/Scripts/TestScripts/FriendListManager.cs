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
    public class FriendData
    {
        public string username;
        public Presence presence;
        public Sprite avatar;          // NEW
    }
    public FriendData[] startingFriends;

    void Start()
    {
        foreach (var f in startingFriends) AddFriend(f.username, f.presence, f.avatar);   // changed
    }

    Dictionary<string, Sprite> avatars = new Dictionary<string, Sprite>();

    void Awake() { chat.avatarLookup = GetAvatar; }   // NEW

    public Sprite GetAvatar(string username) =>
        avatars.TryGetValue(username, out var s) ? s : null;

    public void AddFriend(string username, Presence p, Sprite pic = null)
    {
        avatars[username] = pic;                       // store BEFORE Setup (see note)
        FriendView f = Instantiate(friendPrefab, content);
        f.Setup(username, p, group, OnFriendSelected, pic);
        friends[username] = f;

        if (friends.Count == 1) f.toggle.isOn = true;
    }

    public void SetAvatar(string username, Sprite pic)
    {
        avatars[username] = pic;
        if (friends.TryGetValue(username, out FriendView f)) f.SetAvatar(pic);
    }

    public ChatManager chat;

    void OnFriendSelected(string username) { chat.OpenRoom(username); }

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