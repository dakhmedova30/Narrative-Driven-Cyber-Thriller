using UnityEngine;

namespace DopamineMeter
{
    [CreateAssetMenu(menuName = "Dopamine Meter/Tuning")]
    public sealed class MeterConfig : ScriptableObject
    {
        [Header("Starting value and death penalty")]
        [Range(0, 50)] public float initialValue = 50;
        [Min(0)] public float increaseAfterDeath = 5;
        [Range(0, 50)] public float respawnStartCap = 50;
        [Header("Timed growth - only while player has control")]
        [Min(.1f)] public float growthIntervalSeconds = 20;
        [Min(0)] public float growthPerInterval = .5f;
        [Range(1, 99)] public float engaged = 40, intrusive = 60, critical = 80;
        [Min(0)] public float queryCost = 5, notificationCost = 3, readingRelief = 10, conversationRelief = 8;
        public MeterModel CreateModel() => new MeterModel(initialValue, growthPerInterval, engaged, intrusive, critical,
            growthIntervalSeconds, increaseAfterDeath, respawnStartCap);
    }
}
