using System;
using UnityEngine;
namespace CafeGameplay
{
    [Serializable]
    public sealed class PhonePost
    {
        public string id="post-01",source="NIGHT FEED",title="A new message";
        public bool advertisement;
        [TextArea(3,8)] public string body="Content shown on the phone.";
        [Min(0)] public float meterIncrease=3;
    }
    [CreateAssetMenu(menuName="Cafe/Phone Content")]
    public sealed class PhoneContent : ScriptableObject
    {
        [Min(1)] public float notificationInterval=8;
        [Range(1,20)] public int maxUnread=4;
        [Range(.1f,2)] public float lookDownSeconds=.55f;
        public AudioClip notificationSound;
        public PhonePost[] posts=Array.Empty<PhonePost>();
        public bool IsValid
        {
            get
            {
                if(posts==null||notificationInterval<=0||maxUnread<=0)return false;
                var ids=new System.Collections.Generic.HashSet<string>();
                foreach(var p in posts)if(p==null||string.IsNullOrWhiteSpace(p.id)||!ids.Add(p.id)||p.meterIncrease<0)return false;
                return true;
            }
        }
    }
}
