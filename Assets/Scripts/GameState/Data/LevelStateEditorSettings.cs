using UnityEngine;

[CreateAssetMenu(fileName = "LevelStateEditorSettings", menuName = "Game State/Level State Editor Settings")]
public class LevelStateEditorSettings : ScriptableObject
{
    public LevelState CurrentLevelState;

    [Header("Graph")]
    public Vector3 DefaultAddedOffset;
    public LayerMask DefaultClickMask;

    // Debug for graph
    [Header("Graph Debug")]
    public Color DebugVertexColor = new Color(.0f, .0f, 1.0f, .5f);
    public Color DebugSelectedVertexColor = new Color(1.0f, .0f, .0f, .5f);

    public Color DebugVertexForEdgeColor = new Color(.0f, 1.0f, .0f, .5f);
    public Color DebugEdgeCorridorColor = new Color(1.0f, 1.0f, .0f, 1.0f);
    public Color DebugEdgeDoorColor = new Color(.0f, 1.0f, .0f, 1.0f);
    public Color DebugEdgeVentColor = new Color(.0f, 1.0f, 1.0f, 1.0f);
    public Color DebugSelectedEdgeColor = new Color(1.0f, .0f, 1.0f, 1.0f);

    public float DebugVertexDiscRadius = 1.0f;

    // Debug for characters
    [Header("Characters Debug")]
    public Texture CharacterIcon;
    public Texture SelectedCharacterIcon;
    public GUIStyle CharacterCounterStyle = new GUIStyle();

    // Debug for the GUI
    [Header("GUI Debug")]
    public Color GUIClickTextBoxColor = new Color(.0f, .0f, .0f, 1.0f);
    public Color GUIClickTextColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
}
