using UnityEngine;
using UnityEngine.Events;

namespace DopamineMeter
{
    [DefaultExecutionOrder(-100)]
    public sealed class MeterService : MonoBehaviour
    {
        public static MeterService Instance { get; private set; }
        public MeterConfig config;
        [Header("Timed growth (optional)")]
        [Tooltip("Off: only interaction events change the value. On: add the configured amount every 20 seconds of player control.")]
        public bool enableTimedGrowth;
        [Tooltip("The game adapter sets this false during dialogue, menus, loading or when the player cannot be controlled.")]
        public bool playerHasControl = true;
        [Header("Failure response - connect your Game Over UI here")]
        public UnityEvent onDeath = new UnityEvent();
        public MeterModel Model { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearStatics() => Instance = null;
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            Model = config != null ? config.CreateModel() : new MeterModel();
            Model.Failed += () => onDeath.Invoke();
        }
        void LateUpdate()
        {
            if (Model == null) return;
            Model.SetDigitalUse(enableTimedGrowth);
            Model.Tick(Time.deltaTime, playerHasControl);
        }
        void OnApplicationFocus(bool focus) => Model?.Suspend("focus", !focus);
        void OnApplicationPause(bool paused) => Model?.Suspend("application", paused);
        void OnDestroy() { if (Instance == this) Instance = null; }
        public void Activate() => Model.Activate();
        public void SetPaused(bool paused) => Model?.Suspend("host", paused);
        public void SetTimedGrowthEnabled(bool enabled) => enableTimedGrowth = enabled;
        public void SetPlayerControl(bool allowed) => playerHasControl = allowed;
        public void Respawn() { enableTimedGrowth = false; Model.Respawn(); }
        public void NewGame() { enableTimedGrowth = false; Model.Reset(); }
        public void DigitalAction(string id) => Model.Apply(config != null ? config.queryCost : 5, id, "AI insight / +5 exposure");
        public void ReadBook(string id) => Model.Apply(-(config != null ? config.readingRelief : 10), id, "Reading complete / attention recovered");
    }
}
