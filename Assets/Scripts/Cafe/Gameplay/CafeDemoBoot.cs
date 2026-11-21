using DopamineMeter;
using UnityEngine;
namespace CafeGameplay
{
    // Only in the standalone demonstration. Production games own their Meter lifecycle.
    [DefaultExecutionOrder(-50)]
    public sealed class CafeDemoBoot : MonoBehaviour
    {
        public CafePhone phone;
        void Start(){if(MeterService.Instance!=null){MeterService.Instance.NewGame();MeterService.Instance.Activate();MeterService.Instance.SetPlayerControl(false);}}
        void Update(){if(MeterService.Instance!=null)MeterService.Instance.SetPaused(CafeRuntime.Paused);}
    }
}
