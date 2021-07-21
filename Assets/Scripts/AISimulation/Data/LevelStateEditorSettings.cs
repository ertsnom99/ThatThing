using UnityEngine;

[CreateAssetMenu(fileName = "LevelStateEditorSettings", menuName = "AI Simulation/Settings/Level State Editor Settings")]
public class LevelStateEditorSettings : ScriptableObject
{
    public LevelState CurrentLevelState;

    [Header("Styles")]
    public GUIStyle InvalidStyle = new GUIStyle();

    [Header("Graph")]
    public Vector3 DefaultAddedOffset;
    public LayerMask DefaultClickMask;

    [Header("Graph Debug")]
    public Color DebugVertexColor = new Color(.0f, .0f, 1.0f, .5f);
    public Color DebugSelectedVertexColor = new Color(1.0f, .0f, .0f, .5f);

    public Color DebugVertexForEdgeColor = new Color(.0f, 1.0f, .0f, .5f);
    public Color DebugEdgeCorridorColor = new Color(1.0f, 1.0f, .0f, 1.0f);
    public Color DebugEdgeDoorColor = new Color(.0f, 1.0f, .0f, 1.0f);
    public Color DebugEdgeVentColor = new Color(.0f, 1.0f, 1.0f, 1.0f);
    public Color DebugSelectedEdgeColor = new Color(1.0f, .0f, 1.0f, 1.0f);

    [Min(.0f)]
    public float DebugVertexDiscRadius = 1.0f;
    [Min(.0f)]
    public float DebugEdgeThickness = .5f;
    public GUIStyle VertexIdStyle = new GUIStyle();

    [Header("Characters Debug")]
    public Texture CharacterIcon;
    public Texture SelectedCharacterIcon;
    public float FullOffsetDistance = 20.0f;
    public Vector3 IconOffset = new Vector3(-0.75f, 2.5f, .0f);
    public Vector3 CharacterCounterOffset = new Vector3(-0.2f, 1.4f, .0f);
    public GUIStyle CharacterCounterStyle = new GUIStyle();

    [Header("GUI Debug")]
    public Color GUIClickTextBoxColor = new Color(.0f, .0f, .0f, 1.0f);
    public Color GUIClickTextColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
}
