using System;
using System.Collections.Generic;
namespace CafeGameplay
{
    public sealed class PhoneInbox
    {
        readonly PhoneContent content;
        readonly HashSet<string> read=new HashSet<string>();
        public readonly List<PhonePost> delivered=new List<PhonePost>();
        public int Unread {get;private set;}
        public float Elapsed {get;private set;}
        public PhoneInbox(PhoneContent content){this.content=content;}
        public bool IsRead(PhonePost post)=>read.Contains(post.id);
        public PhonePost Tick(float dt,bool enabled)
        {
            if(!enabled){Elapsed=0;return null;}
            if(dt<=0||float.IsNaN(dt)||float.IsInfinity(dt)||Unread>=content.maxUnread||delivered.Count>=content.posts.Length)return null;
            Elapsed+=dt;if(Elapsed<content.notificationInterval)return null;
            Elapsed=0;return Deliver();
        }
        public PhonePost Deliver()
        {
            if(Unread>=content.maxUnread||delivered.Count>=content.posts.Length)return null;
            var post=content.posts[delivered.Count];delivered.Add(post);Unread++;return post;
        }
        public bool MarkRead(PhonePost post)
        {
            if(!delivered.Contains(post)||!read.Add(post.id))return false;
            Unread--;return true;
        }
    }
}
