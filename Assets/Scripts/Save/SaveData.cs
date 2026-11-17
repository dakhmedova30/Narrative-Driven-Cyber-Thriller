using System;
using System.Collections.Generic;

/// <summary>Everything stored in one save slot. Serialized with JsonUtility.</summary>
[Serializable]
public class SaveData
{
    public int version = 1;

    // ---- Slot info (shown in the save/load screen) ----
    public string slotId;
    public string savedAt;          // ISO 8601, local time
    public long savedAtTicks;       // for "Continue" = newest save
    public float playTime;          // seconds
    public int chapter;
    public string sceneName;
    public int flowStep = -1;       // Game Flow step index
    public string progressLabel;    // Game Flow step name, e.g. "Chapter 0"

    // ---- Player ----
    public bool hasPlayer;
    public float playerX;
    public bool playerFacingRight = true;

    // ---- Dialogue (empty node = not in a dialogue; otherwise it restarts from this node) ----
    public string dialogueRunner;
    public string dialogueNode;

    // ---- Yarn variables ----
    public List<FloatVar> floats = new List<FloatVar>();
    public List<StringVar> strings = new List<StringVar>();
    public List<BoolVar> bools = new List<BoolVar>();

    // ---- Presentation ----
    public string background;
    public string music;
    public string ambience;

    // ---- Jaycord ----
    public bool hasJaycord;
    public string jaycordCurrent;
    public List<JaycordContactState> jaycordContacts = new List<JaycordContactState>();
}

[Serializable] public class FloatVar { public string name; public float value; }
[Serializable] public class StringVar { public string name; public string value; }
[Serializable] public class BoolVar { public string name; public bool value; }

/// <summary>Jaycord contact data without the avatar sprite (the scene keeps its own sprite).</summary>
[Serializable]
public class JaycordContactState
{
    public string name;
    public int presence;
    public string status;
    public string handle;
    public string preview;
    public int unread;
    public string level;
    public List<JaycordDossierField> dossier = new List<JaycordDossierField>();
    public int trust;
    public string[] tags = new string[0];
    public List<JaycordRecord> history = new List<JaycordRecord>();
}
