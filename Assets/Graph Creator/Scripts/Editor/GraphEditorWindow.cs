using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GraphCreator
{
    public class GraphEditorWindow : EditorWindow
    {
        #region Variables
        private GraphEditorWindowSettings _editorSettings;
        private Graph _previousGraph;

        private SerializedObject _serializedGraph;

        private GUIStyle _defaultInvalidStyle;
        private GUIStyle _foldoutTitleStyle;

        // Scrolling
        private Vector2 _scrollPos;

        // Graph variables
        private ReorderableList _vertices;
        private ReorderableList _edges;

        private bool _showVertices = true;
        private Vector3 _addedOffset;
        private LayerMask _vertexClickMask;
        private bool _creatingVertexWithClick = false;
        private bool _displayVertices = true;
        private bool _displayIds = true;
        private int _selectedVertex = -1;
        private bool _showEdges = true;
        private LayerMask _edgeClickMask;
        private bool _creatingEdgeWithClick = false;
        private bool _displayEdges = true;
        private int _selectedPopupVertexA = 0;
        private int _selectedPopupVertexB = 0;
        private int _clickedVertexA = -1;
        private int _selectedEdge = -1;

        // Dimensions
        private const float _editorMinWidth = 300;
        private const float _editorMinHeight = 300;

        private const float _foldoutArrowWidth = 12.0f;
        private const float _reorderableListElementSpaceRatio = .14f;
        #endregion

        [MenuItem("Graph Creator/Graph")]
        public static void ShowEditor()
        {
            Type inspectorType = Type.GetType("UnityEditor.InspectorWindow,UnityEditor.dll");

            // Returns the first EditorWindow of type t which is currently on the screen.
            // If there is none, creates and shows new window and returns the instance of it.
            // Will attempt to dock next to inspector window
            GraphEditorWindow editor = GetWindow<GraphEditorWindow>("Graph", inspectorType);
            editor.minSize = new Vector2(_editorMinWidth, _editorMinHeight);
        }

        #region GUI Methods
        // Repaint at 10 frames per second to give the inspector a chance to update (useful for the "Add vertex using selection" button)
        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnGUI()
        {
            InitializeStyles();
            DrawEditor();
        }

        private void InitializeStyles()
        {
            // Setup _defaultInvalidStyle
            if (_defaultInvalidStyle == null)
            {
                _defaultInvalidStyle = new GUIStyle();
                _defaultInvalidStyle.normal.textColor = Color.red;
                _defaultInvalidStyle.fontSize = 16;
                _defaultInvalidStyle.alignment = TextAnchor.MiddleCenter;
                _defaultInvalidStyle.wordWrap = true;
            }

            // Setup _foldoutTitleStyle
            if (_foldoutTitleStyle  == null)
            {
                _foldoutTitleStyle = new GUIStyle(EditorStyles.foldout);
                _foldoutTitleStyle.fontSize = 30;
            }
        }

        private void DrawEditor()
        {
            EditorGUILayout.Space(15);

            // Error if there is not editor settings
            if (!_editorSettings)
            {
                _editorSettings = EditorGUIUtility.Load("Graph Creator/GraphEditorWindowSettings.asset") as GraphEditorWindowSettings;
            
                if (_editorSettings)
                {
                    _addedOffset = _editorSettings.DefaultAddedOffset;
                    _vertexClickMask = _editorSettings.DefaultClickMask;
                    _edgeClickMask = _editorSettings.DefaultClickMask;
                }
                else
                {
                    EditorGUILayout.LabelField("Couldn't find GraphEditorWindowSettings.asset file in Assets\\Editor Default Resources\\Graph Creator. Please, create it.", _defaultInvalidStyle);
                    return;
                }
            }

            // Display the settings used
            GUI.enabled = false;
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("*Settings used:", GUILayout.Width(100));
            EditorGUILayout.ObjectField(_editorSettings, typeof(GraphEditorWindowSettings), false);
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            EditorGUILayout.Space(15);

            // Graph asset
            EditorGUILayout.LabelField("Assign Graph:", GUILayout.Width(200));
            _editorSettings.CurrentGraph = (Graph)EditorGUILayout.ObjectField(_editorSettings.CurrentGraph, typeof(Graph), false, GUILayout.Width(200));

            // When the Graph changed
            if (_previousGraph != _editorSettings.CurrentGraph)
            {
                _serializedGraph = null;
                _previousGraph = _editorSettings.CurrentGraph;
            
                // Save the editor settings
                EditorUtility.SetDirty(_editorSettings);
            }

            if (_editorSettings.CurrentGraph != null)
            {
                if (_serializedGraph == null)
                {
                    _serializedGraph = new SerializedObject(_editorSettings.CurrentGraph);

                    // Graph serialization
                    _vertices = new ReorderableList(_serializedGraph, _serializedGraph.FindProperty("_vertices"), false, false, false, false);
                    HandleVertices(_vertices);
                    _edges = new ReorderableList(_serializedGraph, _serializedGraph.FindProperty("_edges"), false, false, false, false);
                    HandleEdges(_edges);

                    // Reset selections
                    _selectedVertex = -1;
                    _selectedEdge = -1;
                    _selectedPopupVertexA = 0;
                    _selectedPopupVertexB = 0;

                    // Reset variables for click
                    _creatingVertexWithClick = false;
                    _creatingEdgeWithClick = false;
                    _clickedVertexA = -1;
                }
                else
                {
                    // Make sure that the Graph is updated (for when the ScriptableObject was changed outside the editor window)
                    _serializedGraph.Update();
                }

                EditorGUILayout.Space(15);

                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, false, false, GUILayout.Width(position.width), GUILayout.Height(position.height - 120));

                EditorGUI.BeginChangeCheck();

                DrawVerticesSection();
                EditorGUILayout.Space(15);
                DrawEdgesSection();

                if (EditorGUI.EndChangeCheck())
                {
                    _serializedGraph.ApplyModifiedProperties();
                }

                // Must enable so that mouse wheel make GUI scroll
                GUI.enabled = true;

                EditorGUILayout.EndScrollView();

                SceneView.RepaintAll();
            }
        }

        private void DrawVerticesSection()
        {
            EditorGUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUI.enabled = true;
            _showVertices = EditorGUILayout.Foldout(_showVertices, "Vertices", true, _foldoutTitleStyle);
            GUI.enabled = false;
            EditorGUILayout.IntField(_vertices.count, GUILayout.Width(50.0f));
            GUILayout.EndHorizontal();
            EditorGUILayout.Space(10);

            if (_showVertices)
            {
                EditorGUILayout.Space(5);

                GUI.enabled = true;

                // Add list of vertices
                GUILayout.BeginHorizontal();
                GUILayout.Space(_foldoutArrowWidth);
                _vertices.DoLayoutList();
                GUILayout.EndHorizontal();
            
                GUILayout.BeginHorizontal();
                GUILayout.Label("Added offset");
                _addedOffset = EditorGUILayout.Vector3Field("", _addedOffset, GUILayout.Width(EditorGUIUtility.currentViewWidth * .75f));
                GUILayout.EndHorizontal();

                GUI.enabled = !_creatingVertexWithClick;

                GUILayout.BeginHorizontal();
                GUILayout.Label("Mask for click");
                LayerMask tempMask = EditorGUILayout.MaskField(InternalEditorUtility.LayerMaskToConcatenatedLayersMask(_vertexClickMask), InternalEditorUtility.layers, GUILayout.Width(EditorGUIUtility.currentViewWidth * .75f));
                _vertexClickMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);
                GUILayout.EndHorizontal();

                GUI.enabled = true;

                if (GUILayout.Button("Add vertex"))
                {
                    _editorSettings.CurrentGraph.AddVertex(_addedOffset);

                    // Most be set dirty because the changes where made directly inside the ScriptableObject
                    EditorUtility.SetDirty(_editorSettings.CurrentGraph);
                }

                GUI.enabled = Selection.activeTransform;

                if (GUILayout.Button("Add vertex with selection"))
                {
                    _editorSettings.CurrentGraph.AddVertex(Selection.activeTransform.position + _addedOffset);

                    // Most be set dirty because the changes where made directly inside the ScriptableObject
                    EditorUtility.SetDirty(_editorSettings.CurrentGraph);
                }

                GUI.enabled = !_creatingVertexWithClick && !_creatingEdgeWithClick;

                if (GUILayout.Button("Add vertex with click"))
                {
                    _creatingVertexWithClick = true;

                    // Unselect everything
                    Selection.objects = null;
                }

                // Remove button is enabled only if a vertex is selected
                GUI.enabled = _selectedVertex > -1;

                // "Remove selected vertex" button
                if (GUILayout.Button("Remove selected vertex"))
                {
                    _editorSettings.CurrentGraph.RemoveVertex(_selectedVertex);

                    // Most be set dirty because the changes where made directly inside the ScriptableObject
                    EditorUtility.SetDirty(_editorSettings.CurrentGraph);

                    _selectedVertex = -1;
                    _vertices.index = -1;
                }
            }

            GUI.enabled = true;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Display Vertices");
            _displayVertices = EditorGUILayout.Toggle(_displayVertices, GUILayout.Width(EditorGUIUtility.currentViewWidth * .75f));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Display Ids");
            _displayIds = EditorGUILayout.Toggle(_displayIds, GUILayout.Width(EditorGUIUtility.currentViewWidth * .75f));
            GUILayout.EndHorizontal();
        }

        // Add the correct callbacks for the _vertices ReorderableList
        private void HandleVertices(ReorderableList ReorderableVertices)
        {
            ReorderableVertices.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                // Get the data
                SerializedProperty id = ReorderableVertices.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Id");
                SerializedProperty position = ReorderableVertices.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Position");

                EditorGUI.LabelField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * .1f, rect.width * .2f, EditorGUIUtility.singleLineHeight), new GUIContent("Id: " + id.intValue.ToString()));
                EditorGUI.PropertyField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * _reorderableListElementSpaceRatio, rect.width * (1 - .2f), EditorGUIUtility.singleLineHeight), position, GUIContent.none);
            };

            ReorderableVertices.onSelectCallback = (ReorderableList vertices) =>
            {
                _selectedVertex = vertices.index;
            };
        }

        private void DrawEdgesSection()
        {
            EditorGUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUI.enabled = true;
            _showEdges = EditorGUILayout.Foldout(_showEdges, "Edges", true, _foldoutTitleStyle);
            GUI.enabled = false;
            EditorGUILayout.IntField(_edges.count, GUILayout.Width(50.0f));
            GUILayout.EndHorizontal();
            EditorGUILayout.Space(10);

            if (_showEdges)
            {
                EditorGUILayout.Space(5);

                GUI.enabled = true;
            
                // Add list of edges
                GUILayout.BeginHorizontal();
                GUILayout.Space(_foldoutArrowWidth);
                _edges.DoLayoutList();
                GUILayout.EndHorizontal();

                // Add dropdown for vertex
                List<string> ids = _editorSettings.CurrentGraph.GetAllVertexIds();
                ids.Insert(0, "None");

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Vertex A (id)");
                EditorGUILayout.LabelField("Vertex B (id)", GUILayout.Width(EditorGUIUtility.currentViewWidth / 2.0f));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                _selectedPopupVertexA = EditorGUILayout.Popup(_selectedPopupVertexA, ids.ToArray());
                _selectedPopupVertexB = EditorGUILayout.Popup(_selectedPopupVertexB, ids.ToArray(), GUILayout.Width(EditorGUIUtility.currentViewWidth / 2.0f));
                GUILayout.EndHorizontal();

                // "Add edge" button (only available if selected 2 different vertices)
                GUI.enabled = (_selectedPopupVertexA != _selectedPopupVertexB) && _selectedPopupVertexA > 0 && _selectedPopupVertexB > 0;

                EditorGUILayout.LabelField("*Two different vertices must be selected to create an edge");

                GUI.enabled = !_creatingEdgeWithClick;

                GUILayout.BeginHorizontal();
                GUILayout.Label("Mask for click");
                LayerMask tempMask = EditorGUILayout.MaskField(InternalEditorUtility.LayerMaskToConcatenatedLayersMask(_edgeClickMask), InternalEditorUtility.layers, GUILayout.Width(EditorGUIUtility.currentViewWidth * .75f));
                _edgeClickMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);
                GUILayout.EndHorizontal();
            
                GUI.enabled = (_selectedPopupVertexA != _selectedPopupVertexB) && _selectedPopupVertexA > 0 && _selectedPopupVertexB > 0;

                if (GUILayout.Button("Add edge"))
                {
                    if (_editorSettings.CurrentGraph.AddEdge(_selectedPopupVertexA - 1, _selectedPopupVertexB - 1, _editorSettings.DefaultEdgeDirection, _editorSettings.DefaultEdgeTraversable))
                    {
                        // Most be set dirty because the changes where made directly inside the ScriptableObject
                        EditorUtility.SetDirty(_editorSettings.CurrentGraph);
                    }

                    _selectedPopupVertexA = 0;
                    _selectedPopupVertexB = 0;
                }
            
                GUI.enabled = !_creatingEdgeWithClick && !_creatingVertexWithClick;

                if (GUILayout.Button("Add edge with click"))
                {
                    _creatingEdgeWithClick = true;

                    // Unselect everything
                    Selection.objects = null;
                }

                GUI.enabled = _selectedEdge > -1;
            
                // "Remove selected edge" button
                if (GUILayout.Button("Remove selected edge"))
                {
                    _editorSettings.CurrentGraph.RemoveEdge(_selectedEdge);

                    // Most be set dirty because the changes where made directly inside the ScriptableObject
                    EditorUtility.SetDirty(_editorSettings.CurrentGraph);

                    _selectedEdge = -1;
                    _edges.index = -1;
                }
            }

            GUI.enabled = true;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Display Edges");
            _displayEdges = EditorGUILayout.Toggle(_displayEdges, GUILayout.Width(EditorGUIUtility.currentViewWidth * .75f));
            GUILayout.EndHorizontal();
        }

        // Add the correct callbacks for the _edges ReorderableList
        private void HandleEdges(ReorderableList ReorderableEdges)
        {
            ReorderableEdges.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                // Get the edge data
                SerializedProperty id = ReorderableEdges.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Id");
                SerializedProperty vertexA = ReorderableEdges.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("VertexA");
                SerializedProperty vertexB = ReorderableEdges.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("VertexB");
                SerializedProperty direction = ReorderableEdges.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Direction");
                SerializedProperty traversable = ReorderableEdges.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Traversable");

                if (_vertices.count > vertexA.intValue && _vertices.count > vertexB.intValue)
                {
                    int vertexAId = _vertices.serializedProperty.GetArrayElementAtIndex(vertexA.intValue).FindPropertyRelative("Id").intValue;
                    int vertexBId = _vertices.serializedProperty.GetArrayElementAtIndex(vertexB.intValue).FindPropertyRelative("Id").intValue;
                    float distance = (_editorSettings.CurrentGraph.Vertices[vertexA.intValue].Position - _editorSettings.CurrentGraph.Vertices[vertexB.intValue].Position).magnitude;
                    string directionText = "";

                    switch((EdgeDirection)direction.enumValueIndex)
                    {
                        case EdgeDirection.Bidirectional:
                            directionText = "Vertex " + vertexAId + " <<===>> vertex " + vertexBId;
                            break;
                        case EdgeDirection.AtoB:
                            directionText = "Vertex " + vertexAId + " ===>> vertex " + vertexBId;
                            break;
                        case EdgeDirection.BtoA:
                            directionText = "Vertex " + vertexAId + " <<=== vertex " + vertexBId;
                            break;
                    }

                    EditorGUI.BeginChangeCheck();

                    _editorSettings.CurrentGraph.EdgesFolded[index] = !EditorGUI.Foldout(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * .1f, rect.width * .2f, EditorGUIUtility.singleLineHeight), !_editorSettings.CurrentGraph.EdgesFolded[index], "Id: " + id.intValue.ToString());
                    EditorGUI.LabelField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * .1f, rect.width * .4f, EditorGUIUtility.singleLineHeight), new GUIContent(directionText));
                    EditorGUI.LabelField(new Rect(rect.x + rect.width * .6f, rect.y + EditorGUIUtility.singleLineHeight * .1f, rect.width * .4f, EditorGUIUtility.singleLineHeight), new GUIContent("Distance: " + distance));

                    if (EditorGUI.EndChangeCheck())
                    {
                        // Most be set dirty because the changes where made directly to the ScriptableObject
                        EditorUtility.SetDirty(_editorSettings.CurrentGraph);
                    }

                    if (!_editorSettings.CurrentGraph.EdgesFolded[index])
                    {
                        //direction
                        EditorGUI.LabelField(new Rect(rect.x + _foldoutArrowWidth, rect.y + EditorGUIUtility.singleLineHeight * (1.0f + _reorderableListElementSpaceRatio), rect.width * .2f - _foldoutArrowWidth, EditorGUIUtility.singleLineHeight), new GUIContent("Direction"));
                        EditorGUI.PropertyField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * (1.0f + _reorderableListElementSpaceRatio), rect.width * .8f, EditorGUIUtility.singleLineHeight), direction, GUIContent.none);
                        EditorGUI.LabelField(new Rect(rect.x + _foldoutArrowWidth, rect.y + EditorGUIUtility.singleLineHeight * (2.0f + _reorderableListElementSpaceRatio), rect.width * .2f - _foldoutArrowWidth, EditorGUIUtility.singleLineHeight), new GUIContent("Traversable"));
                        EditorGUI.PropertyField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * (2.0f + _reorderableListElementSpaceRatio), rect.width * .8f, EditorGUIUtility.singleLineHeight), traversable, GUIContent.none);
                    }
                }
            };

            ReorderableEdges.elementHeightCallback = (int index) =>
            {
                float height = EditorGUIUtility.standardVerticalSpacing;
            
                if (!_editorSettings.CurrentGraph.EdgesFolded[index])
                {
                    height += EditorGUIUtility.singleLineHeight * 3.0f;
                }
                else
                {
                    height += EditorGUIUtility.singleLineHeight;
                }

                return height + EditorGUIUtility.standardVerticalSpacing;
            };

            ReorderableEdges.onSelectCallback = (ReorderableList edges) =>
            {
                _selectedEdge = edges.index;
            };
        }
        #endregion

        private void OnBecameVisible()
        {
            // Remove delegate listener if it has previously
            // been assigned.
            SceneView.duringSceneGui -= OnSceneGUI;
            // Add (or re-add) the delegate.
            SceneView.duringSceneGui += OnSceneGUI;

            SceneView.RepaintAll();
        }

        void OnBecameInvisible()
        {
            // When the window is destroyed, remove the delegate
            // so that it will no longer do any drawing.
            SceneView.duringSceneGui -= OnSceneGUI;

            _creatingVertexWithClick = false;
            _creatingEdgeWithClick = false;

            SceneView.RepaintAll();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_editorSettings && _editorSettings.CurrentGraph)
            {
                HandleUserInput(Event.current, sceneView);
                DrawSecenDebug();
                DrawGUIDebug();
            }
        }

        private void DrawSecenDebug()
        {
            Vertex[] vertices = _editorSettings.CurrentGraph.Vertices;
        
            // Draw vertex debugs
            if (_vertices != null && _displayVertices)
            {
                for (int i = 0; i < vertices.Length; i++)
                {
                    // If the vertex is selected during the creation of a edge
                    if ((!_creatingEdgeWithClick && (_selectedPopupVertexA > 0 || _selectedPopupVertexB > 0) && (i == _selectedPopupVertexA - 1 || i == _selectedPopupVertexB - 1))
                     || (_creatingEdgeWithClick && _clickedVertexA > -1 && i == _clickedVertexA))
                    {
                        Handles.color = _editorSettings.DebugSelectedEdgeVertexColor;
                    }
                    // If the vertex is selected in the list of vertex
                    else if ((!_creatingEdgeWithClick && (_selectedPopupVertexA == 0 && _selectedPopupVertexB == 0) && i == _vertices.index))
                    {
                        Handles.color = _editorSettings.DebugSelectedVertexColor;

                        EditorGUI.BeginChangeCheck();
                        Vector3 newPosition = Handles.PositionHandle(vertices[i].Position, Quaternion.identity);

                        // Record _editorSettings.CurrentGraph before applying change to allow undos
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(_editorSettings.CurrentGraph, "Changed Vertex Position");
                            vertices[i].Position = newPosition;
                        }
                    }
                    else
                    {
                        Handles.color = _editorSettings.DebugVertexColor;
                    }

                    Handles.DrawSolidDisc(vertices[i].Position, Vector3.up, _editorSettings.DebugVertexDiscRadius);
                }
            }

            // Draw edge debugs
            if (_edges != null && _displayEdges)
            {
                for (int i = 0; i < _editorSettings.CurrentGraph.Edges.Length; i++)
                {
                    // Draw basic line
                    int vertexAIndex = _editorSettings.CurrentGraph.Edges[i].VertexA;
                    int vertexBIndex = _editorSettings.CurrentGraph.Edges[i].VertexB;

                    if (i == _edges.index)
                    {
                        Handles.color = _editorSettings.DebugSelectedEdgeColor;
                    }
                    else if(!_editorSettings.CurrentGraph.Edges[i].Traversable)
                    {
                        Handles.color = _editorSettings.DebugIntraversableEdgeColor;
                    }
                    else
                    {
                        Handles.color = _editorSettings.DebugEdgeColor;
                    }

                    Handles.DrawLine(vertices[vertexAIndex].Position, vertices[vertexBIndex].Position, _editorSettings.DebugEdgeThickness);

                    Vector3 direction = Vector3.zero;
                    Vector3 startVertex = Vector3.zero;

                    // Draw arrows if necessary
                    switch (_editorSettings.CurrentGraph.Edges[i].Direction)
                    {
                        case EdgeDirection.Bidirectional:
                            continue;
                        case EdgeDirection.AtoB:
                            direction = vertices[vertexBIndex].Position - vertices[vertexAIndex].Position;
                            startVertex = vertices[vertexAIndex].Position;
                            break;
                        case EdgeDirection.BtoA:
                            direction = vertices[vertexAIndex].Position - vertices[vertexBIndex].Position;
                            startVertex = vertices[vertexBIndex].Position;
                            break;
                    }

                    for (int j = 0; j < _editorSettings.DebugEdgeArrowCount; j++)
                    {
                        DrawArrow(startVertex + direction * ((j + 1) / (float)_editorSettings.DebugEdgeArrowCount),
                                  direction,
                                  _editorSettings.DebugEdgeArrowHeadLength,
                                  _editorSettings.DebugEdgeArrowHeadAngle);
                    }
                }
            }

            // Display Id above vertices
            if (_displayIds)
            {
                foreach(Vertex vertex in vertices)
                {
                    Handles.Label(vertex.Position, "Id: " + vertex.Id, _editorSettings.VertexIdStyle);
                }
            }
        }

        // Code taken from: https://forum.unity.com/threads/debug-drawarrow.85980/
        private void DrawArrow(Vector3 pos, Vector3 direction, float arrowHeadLength = 1.0f, float arrowHeadAngle = 20.0f)
        {
            Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
            Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
            Handles.DrawLine(pos, pos + right * arrowHeadLength);
            Handles.DrawLine(pos, pos + left * arrowHeadLength);
        }

        private void DrawGUIDebug()
        {
            if (_creatingVertexWithClick || _creatingEdgeWithClick)
            {
                Handles.BeginGUI();

                GUILayout.BeginArea(new Rect(20, 20, 300, 60));
                var rect = EditorGUILayout.BeginVertical();

                GUI.color = _editorSettings.GUIClickTextBoxColor;
                GUI.Box(rect, GUIContent.none);

                GUI.color = _editorSettings.GUIClickTextColor;

                if (_creatingVertexWithClick)
                {
                    GUILayout.Label("Use left click to select where the vertex will be");
                }
                else if (_creatingEdgeWithClick && _clickedVertexA == -1)
                {
                    GUILayout.Label("Use left click to select VertexA");
                }
                else if (_creatingEdgeWithClick && _clickedVertexA > -1)
                {
                    GUILayout.Label("Use left click to select VertexB");
                }

                GUILayout.Label("Use right click to quit click mode");

                EditorGUILayout.EndVertical();
                GUILayout.EndArea();

                Handles.EndGUI();
            }
        }

        private void HandleUserInput(Event e, SceneView sceneView)
        {
            if (!_creatingVertexWithClick && !_creatingEdgeWithClick)
            {
                return;
            }

            // Disable selection in scene view
            int id = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(id);

            // Left mouse button
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                LayerMask layerMask;

                if (_creatingVertexWithClick)
                {
                    layerMask = _vertexClickMask;
                }
                else
                {
                    layerMask = _edgeClickMask;
                }

                Vector3 worldPosition;
                bool foundPosition = GetMouseWorldPosition(e.mousePosition, sceneView, layerMask, out worldPosition);

                // Adding a vertex
                if (_creatingVertexWithClick && foundPosition)
                {
                    _editorSettings.CurrentGraph.AddVertex(worldPosition + _addedOffset);
                
                    // Most be set dirty because the changes where made directly inside the ScriptableObject
                    EditorUtility.SetDirty(_editorSettings.CurrentGraph);
                }
                // Adding an edge
                else if (foundPosition)
                {
                    int selectedVertex = GetVertexAtPosition(worldPosition);

                    // if selected the first vertex
                    if (selectedVertex > -1 && _clickedVertexA == -1)
                    {
                        _clickedVertexA = selectedVertex;
                    }
                    // if selected the second vertex
                    else if (selectedVertex > -1 && _clickedVertexA > -1 && selectedVertex != _clickedVertexA)
                    {
                        if (_editorSettings.CurrentGraph.AddEdge(_clickedVertexA, selectedVertex, _editorSettings.DefaultEdgeDirection, _editorSettings.DefaultEdgeTraversable))
                        {
                            // Most be set dirty because the changes where made directly inside the ScriptableObject
                            EditorUtility.SetDirty(_editorSettings.CurrentGraph);
                        }

                        _clickedVertexA = -1;
                    }
                }
            }
            // Left mouse button
            else if (e.type == EventType.MouseDown && e.button == 1)
            {
                _creatingVertexWithClick = false;
                _creatingEdgeWithClick = false;
                _clickedVertexA = -1;
            }
        }

        private bool GetMouseWorldPosition(Vector3 mousePosition, SceneView sceneView, LayerMask layerMask, out Vector3 worldPosition)
        {
            float ppp = EditorGUIUtility.pixelsPerPoint;
            mousePosition.y = sceneView.camera.pixelHeight - mousePosition.y * ppp;
            mousePosition.x *= ppp;

            Ray ray = sceneView.camera.ScreenPointToRay(mousePosition);
            RaycastHit hit;

            // Create a new vertex if ray hitted
            if (Physics.Raycast(ray, out hit, float.PositiveInfinity, layerMask))
            {
                worldPosition = hit.point;
                return true;
            }

            worldPosition = Vector3.zero;
            return false;
        }

        private int GetVertexAtPosition(Vector3 worldPosition)
        {
            Vertex[] vertices = _editorSettings.CurrentGraph.Vertices;

            for(int i = 0; i < vertices.Length; i++)
            {
                if ((worldPosition - vertices[i].Position).sqrMagnitude <= _editorSettings.DebugVertexDiscRadius)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
