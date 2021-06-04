using UnityEngine;

[CreateAssetMenu(fileName = "LevelStateEditorSettings", menuName = "Game State/Level State Editor Settings")]
public class LevelStateEditorSettings : ScriptableObject
{
    public LevelState CurrentLevelState;

    // Debug for graph
    [Header("Graph Debug")]
    public Color DebugRoomColor = new Color(.0f, .0f, 1.0f, .5f);
    public Color DebugSelectedRoomColor = new Color(1.0f, .0f, .0f, .5f);

    public Color DebugRoomForConnectionColor = new Color(.0f, 1.0f, .0f, .5f);
    public Color DebugConnectionColor = new Color(1.0f, 1.0f, .0f, 1.0f);
    public Color DebugSelectedConnectionColor = new Color(1.0f, .0f, 1.0f, 1.0f);

    public float DebugRoomDiscRadius = 1.0f;

    // Debug for characters
    [Header("Characters Debug")]
    public Texture CharacterIcon;
    public Texture SelectedCharacterIcon;
    public GUIStyle CharacterCounterStyle = new GUIStyle();
}
