using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionView : MonoBehaviour
{
    public Toggle toggle;
    public TMP_Text label;
    public int index;

    public void Setup(string text, int idx, ToggleGroup group)
    {
        label.text = ">> " + text;
        index = idx;
        toggle.group = group;
        toggle.onValueChanged.AddListener(on =>
            label.fontStyle = on ? FontStyles.Bold : FontStyles.Normal);
    }
}