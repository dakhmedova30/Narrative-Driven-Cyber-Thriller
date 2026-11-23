using System;
using UnityEngine;

namespace CafeGameplay
{
    public enum CoffeeInput { Timing, Hold, Rhythm }
    public enum CoffeeGrade { Miss, Good, Perfect, Cancelled }
    [Serializable]
    public sealed class CoffeeStep
    {
        public string title="GRIND";
        [TextArea] public string instruction="Press in the bright zone.";
        public CoffeeInput input;
        [Min(.2f)] public float travelSeconds=2;
        [Range(.1f,.9f)] public float target=.65f;
        [Range(.01f,.2f)] public float perfectWindow=.06f;
        [Range(.02f,.4f)] public float goodWindow=.16f;
        [Min(2)] public float timeout=12;
        [Min(.4f)] public float beatInterval=1;
        [Range(2,8)] public int beats=3;
    }
    [CreateAssetMenu(menuName="Cafe/Coffee Recipe")]
    public sealed class CoffeeRecipe : ScriptableObject
    {
        public string drinkName="MIDNIGHT LATTE";
        public Texture2D illustration;
        [Min(0)] public float stepFeedbackSeconds=.8f;
        public CoffeeStep[] steps={
            new CoffeeStep{title="GRIND",instruction="Tap SPACE or the button when the marker reaches the bright zone.",input=CoffeeInput.Timing},
            new CoffeeStep{title="EXTRACT",instruction="Hold SPACE or the button. Release inside the bright zone.",input=CoffeeInput.Hold,travelSeconds=3},
            new CoffeeStep{title="POUR",instruction="Tap SPACE or the button as each beat reaches the centre line.",input=CoffeeInput.Rhythm,perfectWindow=.1f,goodWindow=.23f}};
        public bool IsValid => steps!=null && steps.Length>0 && Array.TrueForAll(steps,s=>s!=null && s.travelSeconds>0 && s.timeout>0 && s.beatInterval>0 && s.beats>0 && s.perfectWindow>0 && s.goodWindow>=s.perfectWindow);
    }
}
