using UnityEngine;

namespace GraphCreator
{
    [CreateAssetMenu(fileName = "GraphEditorWindowSettings", menuName = "Graph Creator/Graph Editor Window Settings", order = 1)]
    public class GraphEditorWindowSettings : ScriptableObject
    {
        [HideInInspector]
        public Graph CurrentGraph;

        [Header("Defaults")]
        public Vector3 DefaultAddedOffset = new Vector3(0, .1f, 0);
        public EdgeDirection DefaultEdgeDirection = EdgeDirection.Bidirectional;
        public bool DefaultEdgeTraversable = true;
        public LayerMask DefaultClickMask;

        [Header("Styles")]
        public GUIStyle InvalidStyle = new GUIStyle();

        [Header("Vertex Debug")]
        public Color DebugVertexColor = new Color(.0f, .0f, 1.0f, .5f);
        public Color DebugSelectedVertexColor = new Color(1.0f, .0f, .0f, .5f);
        [Min(.0f)]
        public float DebugVertexDiscRadius = 1.0f;
        public GUIStyle VertexIdStyle = new GUIStyle();

        [Header("Edge Debug")]
        public Color DebugEdgeColor = new Color(1.0f, 1.0f, .0f, 1.0f);
        public Color DebugIntraversableEdgeColor = new Color(1.0f, .0f, .0f, 1.0f);
        public Color DebugSelectedEdgeColor = new Color(1.0f, .0f, 1.0f, 1.0f);
        public Color DebugSelectedEdgeVertexColor = new Color(1.0f, .5f, .0f, .5f);
        [Min(.0f)]
        public float DebugEdgeThickness = .5f;
        [Min(1)]
        public int DebugEdgeArrowCount = 5;
        public float DebugEdgeArrowHeadLength = 1.0f;
        public float DebugEdgeArrowHeadAngle = 40.0f;

        [Header("GUI Debug")]
        public Color GUIClickTextBoxColor = new Color(.0f, .0f, .0f, 1.0f);
        public Color GUIClickTextColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    }
}