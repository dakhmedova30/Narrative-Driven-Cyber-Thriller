using System;
using System.Collections.Generic;

namespace DopamineMeter
{
    public enum MeterLife { Dormant, Active, Dead }
    public enum MeterBand { Stable, Engaged, Intrusive, Critical, Overload }

    [Serializable]
    public sealed class MeterSnapshot
    {
        public int version = 2;
        public float value;
        public float restartValue;
        public float elapsedSeconds;
        public MeterLife life;
        public string[] consumedIds = Array.Empty<string>();
    }

    /// <summary>Engine-independent authority. UI, scenes and story code only submit events.</summary>
    public sealed class MeterModel
    {
        readonly float initial, rate, interval, deathIncrease, restartCap;
        double elapsed;
        public float RestartValue { get; private set; }
        public float ElapsedSeconds => (float)elapsed;
        readonly float engaged, intrusive, critical;
        readonly HashSet<string> consumed = new HashSet<string>();
        readonly HashSet<string> suspensions = new HashSet<string>();
        public float Value { get; private set; }
        public MeterLife Life { get; private set; }
        public bool DigitalUse { get; private set; }
        public bool Suspended => suspensions.Count > 0;
        public string LastCause { get; private set; } = "Awaiting device activation";
        public MeterBand Band => Value >= 100 ? MeterBand.Overload : Value >= critical ? MeterBand.Critical :
            Value >= intrusive ? MeterBand.Intrusive : Value >= engaged ? MeterBand.Engaged : MeterBand.Stable;
        public event Action Changed;
        public event Action Failed;

        public MeterModel(float initial = 50, float rate = .5f, float engaged = 40, float intrusive = 60, float critical = 80,
            float interval = 20, float deathIncrease = 5, float restartCap = 50)
        {
            if (!Finite(initial) || initial < 0 || initial >= 100 || !Finite(rate) || rate < 0 ||
                !Finite(engaged) || !Finite(intrusive) || !Finite(critical) ||
                !(0 < engaged && engaged < intrusive && intrusive < critical && critical < 100) ||
                !Finite(interval) || interval <= 0 || !Finite(deathIncrease) || deathIncrease < 0 ||
                !Finite(restartCap) || restartCap < 0 || restartCap > 50 || initial > restartCap)
                throw new ArgumentOutOfRangeException(nameof(initial), "Invalid meter tuning");
            this.initial = initial; this.rate = rate;
            this.interval = interval; this.deathIncrease = deathIncrease; this.restartCap = restartCap;
            RestartValue = initial;
            this.engaged = engaged; this.intrusive = intrusive; this.critical = critical;
            Value = initial;
        }
        static bool Finite(float x) => !float.IsNaN(x) && !float.IsInfinity(x);
        public void Activate()
        {
            if (Life != MeterLife.Dormant || Suspended) return;
            Life = MeterLife.Active; LastCause = "Companion paired / monitoring online"; Changed?.Invoke();
        }
        public bool Apply(float delta, string occurrenceId, string cause)
        {
            if (Life != MeterLife.Active || Suspended || !Finite(delta) || string.IsNullOrEmpty(occurrenceId)) return false;
            if (!consumed.Add(occurrenceId)) return false;
            Commit(delta, cause); return true;
        }
        void Commit(float delta, string cause)
        {
            Value = Math.Max(0, Math.Min(100, Value + delta));
            LastCause = cause;
            bool failed = Value >= 100 && Life != MeterLife.Dead;
            if (failed) { Life = MeterLife.Dead; DigitalUse = false; elapsed = 0;
                RestartValue = Math.Min(restartCap, RestartValue + deathIncrease); }
            Changed?.Invoke();
            if (failed) Failed?.Invoke();
        }
        public void Tick(float seconds, bool playerHasControl = true)
        {
            if (!Finite(seconds) || seconds <= 0 || Life != MeterLife.Active || Suspended || !DigitalUse || rate <= 0 || !playerHasControl) return;
            elapsed += seconds;
            while (elapsed >= interval && Life == MeterLife.Active)
            {
                elapsed -= interval;
                Commit(rate, "Timed exposure / +" + rate.ToString("0.##") + " every " + interval.ToString("0.##") + "s");
            }
        }
        public void SetDigitalUse(bool enabled)
        {
            if (enabled && (Life != MeterLife.Active || Suspended)) return;
            if (DigitalUse == enabled) return;
            DigitalUse = enabled; LastCause = enabled ? "Digital feed connected" : "Digital feed disconnected";
            Changed?.Invoke();
        }
        public void Suspend(string reason, bool enabled)
        {
            if (string.IsNullOrEmpty(reason)) return;
            bool changed = enabled ? suspensions.Add(reason) : suspensions.Remove(reason);
            if (changed) Changed?.Invoke();
        }
        public void Reset()
        {
            Value = initial; RestartValue = initial; elapsed = 0; Life = MeterLife.Dormant; DigitalUse = false; consumed.Clear();
            LastCause = "Session reset / awaiting device"; Changed?.Invoke();
        }
        public void Respawn()
        {
            if (Life != MeterLife.Dead) return;
            Value = RestartValue; elapsed = 0; Life = MeterLife.Active; DigitalUse = false;
            // Keep one-shot world events consumed; a retry is not a new game.
            LastCause = "Respawn / starting index " + Value.ToString("0.##"); Changed?.Invoke();
        }
        public MeterSnapshot Capture() => new MeterSnapshot { value = Value, life = Life, restartValue = RestartValue,
            elapsedSeconds = (float)elapsed, consumedIds = new List<string>(consumed).ToArray() };
        public void Restore(MeterSnapshot snapshot)
        {
            if (snapshot == null || (snapshot.version != 1 && snapshot.version != 2) || !Finite(snapshot.value) ||
                snapshot.value < 0 || snapshot.value > 100 || !Enum.IsDefined(typeof(MeterLife), snapshot.life))
                throw new ArgumentException("Invalid or unsupported meter snapshot");
            if (snapshot.version == 2 && (!Finite(snapshot.restartValue) || snapshot.restartValue < 0 || snapshot.restartValue > 50 ||
                !Finite(snapshot.elapsedSeconds) || snapshot.elapsedSeconds < 0 || snapshot.elapsedSeconds >= interval))
                throw new ArgumentException("Invalid restart or timer state");
            Value = snapshot.life == MeterLife.Dead ? 100 : snapshot.value;
            Life = Value >= 100 ? MeterLife.Dead : snapshot.life;
            RestartValue = snapshot.version == 1 ? initial : Math.Min(restartCap, snapshot.restartValue);
            elapsed = Life == MeterLife.Dead || snapshot.version == 1 ? 0 : snapshot.elapsedSeconds;
            DigitalUse = false; consumed.Clear();
            foreach (string id in snapshot.consumedIds ?? Array.Empty<string>()) if (!string.IsNullOrEmpty(id)) consumed.Add(id);
            LastCause = "Snapshot restored"; Changed?.Invoke(); // Restoration is not a new death event.
        }
    }
}
