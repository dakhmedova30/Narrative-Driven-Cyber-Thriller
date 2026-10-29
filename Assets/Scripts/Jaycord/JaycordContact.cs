using System;
using System.Collections.Generic;
using UnityEngine;

public enum JaycordPresence { Online, Idle, Offline }

[Serializable]
public class JaycordDossierField
{
    public string label = "FACTION";
    public string value = "A";
    [Tooltip("Show the value in yellow (e.g. STATUS: Unverified).")]
    public bool highlight;
}

/// <summary>One entry in a conversation's history.</summary>
[Serializable]
public class JaycordRecord
{
    public enum Kind { Message, Date, Link }

    public Kind kind = Kind.Message;
    [Tooltip("Who sent it. The player's name (e.g. Skye) = your own message, 'System' = system line.")]
    public string sender = "";
    [TextArea] public string text = "";
    [Tooltip("Link cards only: title (e.g. STOP THEM) and address shown on the card.")]
    public string title = "";
    public string url = "";
    [HideInInspector] public bool linkOpened;
}

/// <summary>
/// Everything Jaycord shows about one person: chat list row, chat header and profile panel.
/// Edit these on the JaycordApp component.
/// </summary>
[Serializable]
public class JaycordContact
{
    [Tooltip("Must match the character name used in Yarn, e.g. 'Max' for lines written as 'Max: ...'.")]
    public string name = "Max";
    public Sprite avatar;
    public JaycordPresence presence = JaycordPresence.Online;
    [Tooltip("Shown under the name, e.g. 'Active 20m ago'. Yarn: <<jaycord_status \"...\">>")]
    public string status = "Active 20m ago";
    public string handle = "@max  ·  ID 0x7F3A";
    [Tooltip("Last message shown in the chat list. Updated automatically when messages arrive.")]
    public string preview = "";
    public int unread;

    [Header("Profile dossier")]
    public string level = "LV.07";
    public JaycordDossierField[] dossier = new JaycordDossierField[0];
    [Range(0, 100)] public int trust = 50;
    public string[] tags = new string[0];

    [Header("Chat history")]
    [Tooltip("Messages already in this chat when the game starts. New messages from Yarn are added at runtime.")]
    public List<JaycordRecord> history = new List<JaycordRecord>();
}
