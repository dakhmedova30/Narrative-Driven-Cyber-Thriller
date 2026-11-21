using System;
using DopamineMeter;
using UnityEngine;

namespace Exploration
{
    [RequireComponent(typeof(Interactable))]
    public sealed class InteractionMeterEffect : MonoBehaviour
    {
        public float amount = 5;
        void OnEnable() => GetComponent<Interactable>().onCompleted.AddListener(Apply);
        void OnDisable() => GetComponent<Interactable>().onCompleted.RemoveListener(Apply);
        void Apply()
        {
            var meter = MeterService.Instance;
            if (meter != null) meter.Model.Apply(amount, Guid.NewGuid().ToString(), GetComponent<Interactable>().displayName);
        }
    }
}
