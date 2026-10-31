using UnityEngine;
using TMPro;

public class ChatTest : MonoBehaviour
{
    public GameObject messagePrefab;   // your bubble cookie cutter
    public Transform chatContent;      // the Content object from the Scroll View

    public void AddMessage()
    {
        GameObject msg = Instantiate(messagePrefab, chatContent);
        msg.GetComponentInChildren<TMP_Text>().text = "hello!";
    }
}