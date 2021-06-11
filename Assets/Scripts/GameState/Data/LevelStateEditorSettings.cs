using UnityEngine;

[CreateAssetMenu(fileName = "LevelStateEditorSettings", menuName = "Game State/Level State Editor Settings")]
public class LevelStateEditorSettings : ScriptableObject
{
    public LevelState CurrentLevelState;

    // Debug for graph
    [Header("Graph Debug")]
    public Color DebugVertexColor = new Color(.0f, .0f, 1.0f, .5f);
    public Color DebugSelectedVertexColor = new Color(1.0f, .0f, .0f, .5f);

    public Color DebugVertexForEdgeColor = new Color(.0f, 1.0f, .0f, .5f);
    public Color DebugEdgeColor = new Color(1.0f, 1.0f, .0f, 1.0f);
    public Color DebugSelectedEdgeColor = new Color(1.0f, .0f, 1.0f, 1.0f);

    public float DebugVertexDiscRadius = 1.0f;

    // Debug for characters
    [Header("Characters Debug")]
    public Texture CharacterIcon;
    public Texture SelectedCharacterIcon;
    public GUIStyle CharacterCounterStyle = new GUIStyle();
}
