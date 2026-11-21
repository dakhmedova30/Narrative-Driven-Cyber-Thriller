using System;
namespace CafeGameplay
{
    // Pure timing/score logic; UI, Yarn and Unity input are adapters around it.
    public sealed class CoffeeStepModel
    {
        public CoffeeStep Settings {get;}
        public float Elapsed {get;private set;}
        public float HeldSeconds {get;private set;}
        public bool Holding {get;private set;}
        public bool Done {get;private set;}
        public CoffeeGrade Grade {get;private set;}
        public int BeatIndex {get;private set;}
        CoffeeGrade worst=CoffeeGrade.Perfect;
        public float Position => Settings.input==CoffeeInput.Hold ? Math.Min(1,HeldSeconds/Settings.travelSeconds) : PingPong(Elapsed/Settings.travelSeconds);
        public float NextBeatTime => (BeatIndex+1)*Settings.beatInterval;
        public CoffeeStepModel(CoffeeStep settings){Settings=settings;}
        static float PingPong(float x){float t=x%2;return t<=1?t:2-t;}
        public static CoffeeGrade Judge(float error,float perfect,float good) => error<=perfect+.00001f ? CoffeeGrade.Perfect : error<=good+.00001f ? CoffeeGrade.Good : CoffeeGrade.Miss;
        public void Tick(float dt)
        {
            if(Done||dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;
            Elapsed+=dt;
            if(Holding)HeldSeconds+=dt;
            if(Settings.input==CoffeeInput.Rhythm)
                while(!Done && Elapsed>NextBeatTime+Settings.goodWindow)RecordBeat(CoffeeGrade.Miss);
            if(!Done && (Elapsed>=Math.Max(Settings.timeout,Settings.input==CoffeeInput.Rhythm?(Settings.beats+1)*Settings.beatInterval:0) || (Settings.input==CoffeeInput.Hold&&HeldSeconds>Settings.travelSeconds)))Finish(CoffeeGrade.Miss);
        }
        public void Press()
        {
            if(Done)return;
            if(Settings.input==CoffeeInput.Hold){Holding=true;return;}
            if(Settings.input==CoffeeInput.Timing){Finish(Judge(Math.Abs(Position-Settings.target),Settings.perfectWindow,Settings.goodWindow));return;}
            float delta=Elapsed-NextBeatTime;
            // Extra early taps do not advance the beat; they spoil perfection, preventing button spam.
            if(delta < -Settings.goodWindow){worst=CoffeeGrade.Miss;return;}
            RecordBeat(Judge(Math.Abs(delta),Settings.perfectWindow,Settings.goodWindow));
        }
        public void Release()
        {
            if(Done||!Holding||Settings.input!=CoffeeInput.Hold)return;
            Holding=false;Finish(Judge(Math.Abs(Position-Settings.target),Settings.perfectWindow,Settings.goodWindow));
        }
        void RecordBeat(CoffeeGrade grade){if(grade<worst)worst=grade;BeatIndex++;if(BeatIndex>=Settings.beats)Finish(worst);}
        void Finish(CoffeeGrade grade){Done=true;Holding=false;Grade=grade;}
    }
}
