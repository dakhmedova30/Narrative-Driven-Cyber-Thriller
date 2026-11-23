using System;
using UnityEngine;

namespace DopamineMeter
{
    /// <summary>Drag onto a pickup, button or trigger. Connect its public methods in UnityEvents.</summary>
    public sealed class MeterInteraction : MonoBehaviour
    {
        [Min(0)] public float amount = 5;
        [Tooltip("Enable for repeatable actions; disable for a pickup/reward that should only count once per new game.")]
        public bool repeatable = true;
        [SerializeField] string eventId = Guid.NewGuid().ToString();
        MeterService Service
        {
            get
            {
                var s = MeterService.Instance;
                if (s == null) Debug.LogWarning("Place DopamineMeterForGame.prefab in the first scene before using this interaction.", this);
                return s;
            }
        }
        public void ActivateMeter() { Service?.Activate(); }
        public void AddPressure() { Apply(amount); }
        public void RecoverPressure() { Apply(-amount); }
        void Apply(float delta)
        {
            var s = Service;
            if (s != null) s.Model.Apply(delta, repeatable ? Guid.NewGuid().ToString() : eventId, name + " / " + delta.ToString("+0.##;-0.##;0"));
        }
        public void EnableTimedGrowth() { Service?.SetTimedGrowthEnabled(true); }
        public void DisableTimedGrowth() { Service?.SetTimedGrowthEnabled(false); }
        public void RespawnMeter() { Service?.Respawn(); }
        public void StartNewGame() { Service?.NewGame(); }
    }
}
