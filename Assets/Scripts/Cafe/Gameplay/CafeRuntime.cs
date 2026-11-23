using UnityEngine;
namespace CafeGameplay
{
    public static class CafeRuntime
    {
        internal static bool AutomationIgnoresFocus;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()=>AutomationIgnoresFocus=false;
        public static bool Paused => (!AutomationIgnoresFocus&&!Application.isFocused) || Time.timeScale<=0 || GameInput.IsRebinding || (GameSession.Instance!=null && (GameSession.Instance.IsPaused||GameSession.Instance.IsBusy||GameSession.Instance.IsOnTitle));
    }
}
