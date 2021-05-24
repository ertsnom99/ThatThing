using UnityEngine;

[CreateAssetMenu(fileName = "LevelLayoutEditorSettings", menuName = "Level Layout/Level Layout Editor Settings")]
public class LevelLayoutEditorSettings : ScriptableObject
{
    public LevelLayout CurrentLevelLayout;

    public GUISkin Skin;
}
