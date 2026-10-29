using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Jaycord app shell: chat list, chat header and profile panel.
/// Contacts are edited in the Inspector. The JaycordPresenter calls into this
/// when Yarn opens a conversation or a message arrives.
/// </summary>
public class JaycordApp : MonoBehaviour
{
    public List<JaycordContact> contacts = new List<JaycordContact>();
    [Tooltip("Conversation shown when the app opens.")]
    public string startContact = "Max";

    [Header("Chat list")]
    [SerializeField] private RectTransform contactList;
    [SerializeField] private JaycordContactRow contactRowPrefab;

    [Header("Chat header")]
    [SerializeField] private Image headerAvatar;
    [SerializeField] private TMP_Text headerName;
    [SerializeField] private TMP_Text headerStatus;

    [Header("Profile")]
    [SerializeField] private JaycordProfilePanel profile;

    private readonly Dictionary<string, JaycordContactRow> rows = new Dictionary<string, JaycordContactRow>();

    /// <summary>The conversation currently on screen.</summary>
    public JaycordContact Current { get; private set; }

    /// <summary>Raised when the player clicks a row in the chat list.</summary>
    public event Action<string> ContactClicked;

    private void Awake()
    {
        BuildList();
        if (!string.IsNullOrEmpty(startContact)) Select(startContact);
    }

    /// <summary>Rebuild the chat list from <see cref="contacts"/>.</summary>
    public void BuildList()
    {
        ClearChildren(contactList);
        rows.Clear();
        foreach (var contact in contacts) CreateRow(contact);
    }

    /// <summary>Open a conversation: highlights the row, fills the header and profile.</summary>
    public void Select(string contactName)
    {
        var contact = GetOrAdd(contactName);
        Current = contact;
        contact.unread = 0;

        foreach (var pair in rows) pair.Value.SetSelected(pair.Key == contact.name);
        rows[contact.name].Bind(contact);

        if (contact.avatar != null)
        {
            headerAvatar.sprite = contact.avatar;
            headerAvatar.color = Color.white;
        }
        headerName.text = contact.name;
        headerStatus.text = contact.status.ToUpperInvariant();
        if (profile != null) profile.Bind(contact);
    }

    public void SetStatus(string contactName, string status)
    {
        var contact = GetOrAdd(contactName);
        contact.status = status;
        if (contact != Current) return;
        headerStatus.text = status.ToUpperInvariant();
        if (profile != null) profile.SetStatus(status);
    }

    /// <summary>Update a contact's last-message preview (and unread count if it isn't on screen).</summary>
    public void OnMessage(string contactName, string text, bool countAsUnread)
    {
        var contact = GetOrAdd(contactName);
        contact.preview = text;
        if (countAsUnread && contact != Current) contact.unread++;
        if (rows.TryGetValue(contact.name, out var row)) row.Bind(contact);
    }

    public JaycordContact Find(string contactName) => contacts.Find(c => c.name == contactName);

    // ------------------------------------------------------------------
    // Runtime edits of the profile card (used by Yarn commands, see JaycordCommands)
    // ------------------------------------------------------------------

    public void SetTrust(string contactName, int trust)
    {
        var contact = GetOrAdd(contactName);
        contact.trust = Mathf.Clamp(trust, 0, 100);
        Refresh(contact);
    }

    public void AddTrust(string contactName, int delta) => SetTrust(contactName, GetOrAdd(contactName).trust + delta);

    /// <summary>Changes one dossier line (adds it if there is no line with that label).</summary>
    public void SetDossier(string contactName, string label, string value)
    {
        var contact = GetOrAdd(contactName);
        var fields = new List<JaycordDossierField>(contact.dossier ?? new JaycordDossierField[0]);
        var field = fields.Find(f => string.Equals(f.label, label, StringComparison.OrdinalIgnoreCase));
        if (field == null)
        {
            field = new JaycordDossierField { label = label.ToUpperInvariant() };
            fields.Add(field);
        }
        field.value = value;
        contact.dossier = fields.ToArray();
        Refresh(contact);
    }

    public void AddTag(string contactName, string tag)
    {
        var contact = GetOrAdd(contactName);
        tag = tag.TrimStart('#');
        var tags = (contact.tags ?? new string[0]).ToList();
        if (!tags.Any(t => string.Equals(t.TrimStart('#'), tag, StringComparison.OrdinalIgnoreCase))) tags.Add(tag);
        contact.tags = tags.ToArray();
        Refresh(contact);
    }

    public void RemoveTag(string contactName, string tag)
    {
        var contact = GetOrAdd(contactName);
        tag = tag.TrimStart('#');
        contact.tags = (contact.tags ?? new string[0])
            .Where(t => !string.Equals(t.TrimStart('#'), tag, StringComparison.OrdinalIgnoreCase)).ToArray();
        Refresh(contact);
    }

    public void SetLevel(string contactName, string level)
    {
        var contact = GetOrAdd(contactName);
        contact.level = level;
        Refresh(contact);
    }

    public void SetHandle(string contactName, string handle)
    {
        var contact = GetOrAdd(contactName);
        contact.handle = handle;
        Refresh(contact);
    }

    /// <summary>presence: online / idle / offline</summary>
    public void SetPresence(string contactName, string presence)
    {
        var contact = GetOrAdd(contactName);
        if (Enum.TryParse(presence, true, out JaycordPresence parsed)) contact.presence = parsed;
        else Debug.LogWarning($"[Jaycord] Unknown presence '{presence}'. Use online, idle or offline.");
        Refresh(contact);
    }

    /// <summary>Redraws everything that shows this contact (list row, and profile if it is on screen).</summary>
    private void Refresh(JaycordContact contact)
    {
        if (rows.TryGetValue(contact.name, out var row)) row.Bind(contact);
        if (contact == Current && profile != null) profile.Bind(contact);
    }

    public JaycordContact GetOrAdd(string contactName) => Find(contactName) ?? AddContact(contactName);

    public static Color PresenceColor(JaycordPresence presence)
    {
        switch (presence)
        {
            case JaycordPresence.Online: return JaycordTheme.Online;
            case JaycordPresence.Idle: return JaycordTheme.Yellow;
            default: return JaycordTheme.Offline;
        }
    }

    /// <summary>Destroys all children (works in edit mode and play mode).</summary>
    public static void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
    }

    private JaycordContact AddContact(string contactName)
    {
        var contact = new JaycordContact { name = contactName, status = "", handle = "", preview = "" };
        contacts.Add(contact);
        CreateRow(contact);
        return contact;
    }

    private void CreateRow(JaycordContact contact)
    {
        var row = Instantiate(contactRowPrefab, contactList);
        row.Bind(contact);
        row.onClick = () => ContactClicked?.Invoke(contact.name);
        row.SetSelected(false);
        rows[contact.name] = row;
    }
}
