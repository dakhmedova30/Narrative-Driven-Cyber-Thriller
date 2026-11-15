using UnityEngine;
using Yarn.Unity;



public class StoryCommands : MonoBehaviour
{
    [YarnCommand("mystatus")]
    public static void MyStatus(string presence, string message = "")
    {
        var profile = FindAnyObjectByType<UserProfileView>();
        if (profile == null) return;

        if (System.Enum.TryParse(presence, true, out Presence p)) profile.SetStatus(p);
        profile.SetStatusMessage(message);
    }
}
