using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The list of every sound in the game: name → audio clip(s).
/// Create one with: Project window ▸ right-click ▸ Create ▸ Audio ▸ Sound Library.
/// Yarn and code only use the names, e.g. &lt;&lt;sfx knock&gt;&gt;.
/// </summary>
[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [Serializable]
    public class Sound
    {
        [Tooltip("Name used in Yarn / code, e.g. knock for <<sfx knock>>.")]
        public string name;
        [Tooltip("One or more clips. With several, a random one is picked each time (great for key presses).")]
        public AudioClip[] clips = new AudioClip[0];
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Random pitch change each time it plays, e.g. 0.05 = ±5%. Makes repeated sounds less robotic.")]
        [Range(0f, 0.5f)] public float pitchVariation;
    }

    public List<Sound> sounds = new List<Sound>();

    public Sound Find(string soundName) =>
        sounds.Find(s => string.Equals(s.name, soundName, StringComparison.OrdinalIgnoreCase));
}
