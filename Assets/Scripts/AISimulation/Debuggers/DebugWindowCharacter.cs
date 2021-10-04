using UnityEngine;
using UnityEngine.UI;

public class DebugWindowCharacter : MonoBehaviour
{
    [SerializeField]
    private Text _levelText;

    private void Awake()
    {
        DisplayText(false);
    }

    public void DisplayText(bool display)
    {
        _levelText.enabled = display;
    }

    public void SetLevelText(string level)
    {
        if (_levelText)
        {
            _levelText.text = level;
        }
    }
}
