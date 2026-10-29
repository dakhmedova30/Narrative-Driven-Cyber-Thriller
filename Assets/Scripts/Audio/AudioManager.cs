using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Plays every sound in the game by name (see <see cref="SoundLibrary"/>).
///
///   SFX      one-shots          Yarn: &lt;&lt;sfx knock&gt;&gt;
///   UI       interface clicks   used by Jaycord (typing, send, receive…)
///   Ambience looping background Yarn: &lt;&lt;ambience subway_ambience&gt;&gt;  &lt;&lt;ambience_stop 1&gt;&gt;
///   Music    looping music      Yarn: &lt;&lt;music theme_1999&gt;&gt;       &lt;&lt;music_stop 2&gt;&gt;
///
/// Put one in each scene (empty GameObject "Audio" ▸ Add Component ▸ AudioManager) and assign the Sound Library.
/// Missing sounds only log a warning once, so the story keeps running while sounds are still being made.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private SoundLibrary library;

    [Header("Volume per channel")]
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float uiVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float ambienceVolume = 0.6f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;

    [Header("Audio Mixer groups (optional, for a settings menu later)")]
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup uiGroup;
    [SerializeField] private AudioMixerGroup ambienceGroup;
    [SerializeField] private AudioMixerGroup musicGroup;

    [Tooltip("Keep playing (and keep this object) when another scene loads.")]
    [SerializeField] private bool keepBetweenScenes;

    private const int PoolSize = 8;
    private readonly List<AudioSource> sfxPool = new List<AudioSource>();
    private readonly List<AudioSource> uiPool = new List<AudioSource>();
    private int sfxNext, uiNext;
    private readonly AudioSource[] ambience = new AudioSource[2];
    private readonly AudioSource[] music = new AudioSource[2];
    private int ambienceCurrent, musicCurrent;
    private readonly Dictionary<AudioSource, Coroutine> fades = new Dictionary<AudioSource, Coroutine>();
    private readonly HashSet<string> warned = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (keepBetweenScenes) DontDestroyOnLoad(gameObject);

        for (int i = 0; i < PoolSize; i++)
        {
            sfxPool.Add(NewSource($"SFX {i + 1}", sfxGroup, false));
            uiPool.Add(NewSource($"UI {i + 1}", uiGroup, false));
        }
        ambience[0] = NewSource("Ambience A", ambienceGroup, true);
        ambience[1] = NewSource("Ambience B", ambienceGroup, true);
        music[0] = NewSource("Music A", musicGroup, true);
        music[1] = NewSource("Music B", musicGroup, true);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    public void PlaySfx(string soundName) => PlayOneShot(sfxPool, ref sfxNext, soundName, sfxVolume);

    public void PlayUI(string soundName) => PlayOneShot(uiPool, ref uiNext, soundName, uiVolume);

    /// <summary>Start a looping ambience, cross-fading from the previous one.</summary>
    public void PlayAmbience(string soundName, float fadeSeconds = 1f) =>
        Crossfade(ambience, ref ambienceCurrent, soundName, ambienceVolume, fadeSeconds);

    public void StopAmbience(float fadeSeconds = 1f) => FadeOutAll(ambience, fadeSeconds);

    /// <summary>Start looping music, cross-fading from the previous track.</summary>
    public void PlayMusic(string soundName, float fadeSeconds = 2f) =>
        Crossfade(music, ref musicCurrent, soundName, musicVolume, fadeSeconds);

    public void StopMusic(float fadeSeconds = 2f) => FadeOutAll(music, fadeSeconds);

    // ------------------------------------------------------------------

    private void PlayOneShot(List<AudioSource> pool, ref int next, string soundName, float channelVolume)
    {
        var sound = Lookup(soundName);
        if (sound == null) return;

        var source = pool[next];
        next = (next + 1) % pool.Count;
        source.clip = sound.clips[Random.Range(0, sound.clips.Length)];
        source.volume = sound.volume * channelVolume;
        source.pitch = 1f + Random.Range(-sound.pitchVariation, sound.pitchVariation);
        source.Play();
    }

    private void Crossfade(AudioSource[] pair, ref int current, string soundName, float channelVolume, float fadeSeconds)
    {
        var sound = Lookup(soundName);
        if (sound == null) return;
        var clip = sound.clips[0];

        var from = pair[current];
        if (from.isPlaying && from.clip == clip) return; // already playing

        current = 1 - current;
        var to = pair[current];
        to.clip = clip;
        to.volume = 0f;
        to.pitch = 1f;
        to.Play();

        StartFade(to, sound.volume * channelVolume, fadeSeconds);
        StartFade(from, 0f, fadeSeconds);
    }

    private void FadeOutAll(AudioSource[] pair, float fadeSeconds)
    {
        foreach (var source in pair) StartFade(source, 0f, fadeSeconds);
    }

    private void StartFade(AudioSource source, float target, float seconds)
    {
        if (fades.TryGetValue(source, out var running) && running != null) StopCoroutine(running);
        fades[source] = StartCoroutine(Fade(source, target, seconds));
    }

    private IEnumerator Fade(AudioSource source, float target, float seconds)
    {
        float start = source.volume;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(start, target, t / seconds);
            yield return null;
        }
        source.volume = target;
        if (target <= 0f) source.Stop();
        fades.Remove(source);
    }

    private SoundLibrary.Sound Lookup(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return null;
        var sound = library != null ? library.Find(soundName) : null;
        if (sound != null && sound.clips != null && sound.clips.Length > 0 && sound.clips[0] != null) return sound;

        if (warned.Add(soundName))
        {
            Debug.LogWarning(library == null
                ? "[Audio] No Sound Library assigned on the AudioManager."
                : $"[Audio] No clip for sound '{soundName}'. Add it to the Sound Library.", this);
        }
        return null;
    }

    private AudioSource NewSource(string sourceName, AudioMixerGroup group, bool loop)
    {
        var go = new GameObject(sourceName);
        go.transform.SetParent(transform, false);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f; // 2D
        source.outputAudioMixerGroup = group;
        return source;
    }
}
