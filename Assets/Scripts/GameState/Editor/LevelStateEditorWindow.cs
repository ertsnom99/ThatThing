using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class LevelStateEditorWindow : EditorWindow
{
    #region Variables
    private LevelState _previousLevelState;
    public static LevelStateEditorSettings Settings;

    private SerializedObject _serializedLevelState;

    // Toolbar variables
    private int toolbarSelection = 0;
    private string[] toolbarStrings = { "Level Graph", "Characters" };

    // Scrolling
    private Vector2 _scrollPos;

    // Level graph variables
    private ReorderableList _vertices;
    private ReorderableList _edges;

    private int _selectedVertex = -1;
    private int _selectedEdge = -1;
    private int _selectedVertexA = 0;
    private int _selectedVertexB = 0;

    // Characters variables
    private ReorderableList _characters;

    private int _selectedCharacter = -1;
    private int _selectedVertexForCharacter = 0;

    // Dimensions
    private const float _editorMinWidth = 300;
    private const float _editorMinHeight = 300;
    #endregion

    private void OnEnable()
    {
        Settings = EditorGUIUtility.Load("Level State/LevelStateEditorSettings.asset") as LevelStateEditorSettings;

        if (Settings == null)
        {
            Debug.LogError("Couldn't find LevelStateEditorSettings.asset file in Assets\\Editor Default Resources\\Level State");
        }
    }

    // Add menu item to the main menu and inspector context menus and the static function becomes a menu command
    [MenuItem("Level State/Editor")]
    public static void ShowEditor()
    {
        Type inspectorType = Type.GetType("UnityEditor.InspectorWindow,UnityEditor.dll");

        // Returns the first EditorWindow of type t which is currently on the screen.
        // If there is none, creates and shows new window and returns the instance of it.
        // Will attempt to dock next to inspector window
        LevelStateEditorWindow editor = GetWindow<LevelStateEditorWindow>("Level State", inspectorType);
        editor.minSize = new Vector2(_editorMinWidth, _editorMinHeight);
    }

    #region GUI Methods
    // Repaint at 10 frames per second to give the inspector a chance to update (usefull for the "Add vertex using selection" button)
    private void OnInspectorUpdate()
    {
        Repaint();
    }

    private void OnGUI()
    {
        DrawEditor();
    }

    private void DrawEditor()
    {
        // Field for the LevelState
        EditorGUILayout.LabelField("Assign LevelState:", GUILayout.Width(200));
        Settings.CurrentLevelState = (LevelState)EditorGUILayout.ObjectField(Settings.CurrentLevelState, typeof(LevelState), false, GUILayout.Width(200));
        
        // When the LecelState changed
        if (_previousLevelState != Settings.CurrentLevelState)
        {
            _serializedLevelState = null;
            _previousLevelState = Settings.CurrentLevelState;
            
            // Save the settings
            EditorUtility.SetDirty(Settings);
        }

        if (Settings.CurrentLevelState != null)
        {
            if (_serializedLevelState == null)
            {
                _serializedLevelState = new SerializedObject(Settings.CurrentLevelState);

                // Level graph serialization
                _vertices = new ReorderableList(_serializedLevelState, _serializedLevelState.FindProperty("_graph.Vertices"), false, true, false, false);
                _edges = new ReorderableList(_serializedLevelState, _serializedLevelState.FindProperty("_graph.Edges"), false, true, false, false);

                // Characters serialization
                _characters = new ReorderableList(_serializedLevelState, _serializedLevelState.FindProperty("_characters"), false, true, false, false);

                // Reset selections
                _selectedVertex = -1;
                _selectedEdge = -1;
                _selectedVertexA = 0;
                _selectedVertexB = 0;
                _selectedCharacter = -1;
                _selectedVertexForCharacter = 0;
            }
            else
            {
                // Make sure that the LevelState is updated (for when the ScriptableObject was changed outside the editor window)
                _serializedLevelState.Update();
            }

            EditorGUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            toolbarSelection = GUILayout.Toolbar(toolbarSelection, toolbarStrings, GUILayout.Width(300), GUILayout.Height(20));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            EditorGUILayout.Space(15);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, false, false, GUILayout.Width(position.width), GUILayout.Height(position.height - 85));

            EditorGUI.BeginChangeCheck();

            switch (toolbarSelection)
            {
                case 0:
                    DrawLevelGraphSection();
                    break;
                case 1:
                    DrawCharactersSection();
                    break;
            }

            EditorGUILayout.EndScrollView();

            if (EditorGUI.EndChangeCheck())
            {
                _serializedLevelState.ApplyModifiedProperties();
            }

            SceneView.RepaintAll();
        }
    }

    private void DrawLevelGraphSection()
    {
        DrawVerticesSection();
        EditorGUILayout.Space(15);
        DrawEdgesSection();
    }

    private void DrawVerticesSection()
    {
        // Add list of vertices
        HandleVertices(_vertices);
        _vertices.DoLayoutList();

        // "Add vertex" button
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Add vertex"))
        {
            Settings.CurrentLevelState.AddVertex();

            // Most be set dirty because the changes where made directly inside the ScriptableObject
            EditorUtility.SetDirty(Settings.CurrentLevelState);
        }

        GUI.enabled = Selection.activeTransform;

        if (GUILayout.Button("Add vertex using selection", GUILayout.Width(Screen.width / 2.0f)))
        {
            Settings.CurrentLevelState.AddVertex(Selection.activeTransform);

            // Most be set dirty because the changes where made directly inside the ScriptableObject
            EditorUtility.SetDirty(Settings.CurrentLevelState);
        }

        GUILayout.EndHorizontal();

        // Remove button is enabled only if a vertex is selected
        GUI.enabled = _selectedVertex > -1;

        // "Remove selected vertex" button
        if (GUILayout.Button("Remove selected vertex"))
        {
            Settings.CurrentLevelState.RemoveVertex(_selectedVertex);

            // Most be set dirty because the changes where made directly inside the ScriptableObject
            EditorUtility.SetDirty(Settings.CurrentLevelState);

            _selectedVertex = -1;
        }

        GUI.enabled = true;

        EditorGUILayout.Space(15);
    }

    // Add the correct callbacks for the _vertices ReorderableList
    private void HandleVertices(ReorderableList ReorderableVertices)
    {
        ReorderableVertices.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Vertices");
        };

        ReorderableVertices.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            // Get the requirement data
            SerializedProperty id = ReorderableVertices.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Id");
            SerializedProperty position = ReorderableVertices.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Position");

            // Draw the necessary fields
            EditorGUI.LabelField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * .1f, rect.width * .2f, EditorGUIUtility.singleLineHeight), new GUIContent("Id: " + id.intValue.ToString()));
            EditorGUI.PropertyField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * .14f, rect.width * (1 - .2f), EditorGUIUtility.singleLineHeight), position, GUIContent.none);
        };

        ReorderableVertices.onSelectCallback = (ReorderableList vertices) =>
        {
            _selectedVertex = vertices.index;
        };
    }

    private void DrawEdgesSection()
    {
        // Add list of edges
        HandleEdges(_edges);
        _edges.DoLayoutList();

        // Add dropdown for vertex
        List<string> ids = Settings.CurrentLevelState.GetAllVertexIds();
        ids.Insert(0, "None");

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Vertex A (id)");
        EditorGUILayout.LabelField("Vertex B (id)", GUILayout.Width(Screen.width / 2.0f));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        _selectedVertexA = EditorGUILayout.Popup(_selectedVertexA, ids.ToArray());
        _selectedVertexB = EditorGUILayout.Popup(_selectedVertexB, ids.ToArray(), GUILayout.Width(Screen.width / 2.0f));
        GUILayout.EndHorizontal();

        // "Add edge" button (only available if selected 2 different vertices)
        GUI.enabled = (_selectedVertexA != _selectedVertexB) && _selectedVertexA > 0 && _selectedVertexB > 0;

        if (GUILayout.Button("Add edge"))
        {
            Settings.CurrentLevelState.AddEdge(_selectedVertexA - 1, _selectedVertexB - 1);

            // Most be set dirty because the changes where made directly inside the ScriptableObject
            EditorUtility.SetDirty(Settings.CurrentLevelState);

            _selectedVertexA = 0;
            _selectedVertexB = 0;
        }

        EditorGUILayout.LabelField("*Two different vertices must be selected to create an edge");

        EditorGUILayout.Space(10);

        GUI.enabled = _selectedEdge > -1;

        // "Remove selected edge" button
        if (GUILayout.Button("Remove selected edge"))
        {
            Settings.CurrentLevelState.RemoveEdge(_selectedEdge);

            // Most be set dirty because the changes where made directly inside the ScriptableObject
            EditorUtility.SetDirty(Settings.CurrentLevelState);

            _selectedEdge = -1;
        }

        GUI.enabled = true;

        EditorGUILayout.Space(15);
    }

    // Add the correct callbacks for the _edges ReorderableList
    private void HandleEdges(ReorderableList ReorderableEdges)
    {
        ReorderableEdges.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Edges");
        };

        ReorderableEdges.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            // Get the edge data
            SerializedProperty id = ReorderableEdges.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Id");
            SerializedProperty vertexA = ReorderableEdges.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("VertexA");
            SerializedProperty vertexB = ReorderableEdges.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("VertexB");
            SerializedProperty cost = ReorderableEdges.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Cost");
            SerializedProperty traversable = ReorderableEdges.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Traversable");
            SerializedProperty type = ReorderableEdges.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Type");

            Vertex[] vertices = Settings.CurrentLevelState.GetVerticesCopy();

            if (vertices.Length > vertexA.intValue && vertices.Length > vertexB.intValue)
            {
                string vertexAToVertexB = "Vertex " + vertices[vertexA.intValue].Id.ToString() + " to vertex " + vertices[vertexB.intValue].Id.ToString();

                // Draw the necessary fields
                EditorGUI.LabelField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * .1f, rect.width * .2f, EditorGUIUtility.singleLineHeight), new GUIContent("Id: " + id.intValue.ToString()));
                EditorGUI.LabelField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * .1f, rect.width * .8f, EditorGUIUtility.singleLineHeight), new GUIContent(vertexAToVertexB));
                EditorGUI.LabelField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * 1.14f, rect.width * .2f, EditorGUIUtility.singleLineHeight), new GUIContent("Cost"));
                EditorGUI.PropertyField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * 1.14f, rect.width * .8f, EditorGUIUtility.singleLineHeight), cost, GUIContent.none);
                EditorGUI.LabelField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * 2.14f, rect.width * .2f, EditorGUIUtility.singleLineHeight), new GUIContent("Traversable"));
                EditorGUI.PropertyField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * 2.14f, rect.width * .8f, EditorGUIUtility.singleLineHeight), traversable, GUIContent.none);
                EditorGUI.LabelField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * 3.14f, rect.width * .2f, EditorGUIUtility.singleLineHeight), new GUIContent("Type"));
                EditorGUI.PropertyField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * 3.14f, rect.width * .8f, EditorGUIUtility.singleLineHeight), type, GUIContent.none);
            }
        };

        ReorderableEdges.elementHeightCallback = (int index) =>
        {
            float height = EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUIUtility.singleLineHeight * 4.0f;
            return height + EditorGUIUtility.standardVerticalSpacing;
        };

        ReorderableEdges.onSelectCallback = (ReorderableList edges) =>
        {
            _selectedEdge = edges.index;
        };
    }

    private void DrawCharactersSection()
    {
        // Add list of edges
        HandleCharacters(_characters);
        _characters.DoLayoutList();

        GUILayout.BeginHorizontal();
        // "Add Character" button
        GUI.enabled = _selectedVertexForCharacter > 0;

        if (GUILayout.Button("Add Character", GUILayout.Width(Screen.width / 2.0f)))
        {
            Settings.CurrentLevelState.AddCharacter(_selectedVertexForCharacter - 1);

            // Most be set dirty because the changes where made directly inside the ScriptableObject
            EditorUtility.SetDirty(Settings.CurrentLevelState);

            _selectedVertexForCharacter = 0;
        }

        GUI.enabled = true;

        // Add dropdown to select a vertex
        List<string> ids = Settings.CurrentLevelState.GetAllVertexIds();
        ids.Insert(0, "None");

        _selectedVertexForCharacter = EditorGUILayout.Popup(_selectedVertexForCharacter, ids.ToArray());

        GUILayout.EndHorizontal();

        GUI.enabled = _selectedCharacter > -1;

        // "Remove selected edge" button
        if (GUILayout.Button("Remove selected edge"))
        {
            Settings.CurrentLevelState.RemoveCharacter(_selectedCharacter);

            // Most be set dirty because the changes where made directly inside the ScriptableObject
            EditorUtility.SetDirty(Settings.CurrentLevelState);

            _selectedCharacter = -1;
        }

        GUI.enabled = true;
    }

    // Add the correct callbacks for the _characters ReorderableList
    private void HandleCharacters(ReorderableList ReorderableCharacters)
    {
        ReorderableCharacters.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Characters");
        };

        ReorderableCharacters.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            // Get the vertex data
            SerializedProperty vertex = ReorderableCharacters.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Vertex");

            Vertex[] vertices = Settings.CurrentLevelState.GetVerticesCopy();

            if (vertices.Length > vertex.intValue)
            {
                // Draw the necessary fields
                EditorGUI.LabelField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * .1f, rect.width * .8f, EditorGUIUtility.singleLineHeight), new GUIContent(vertices[vertex.intValue].Id.ToString()));
            }
        };

        ReorderableCharacters.elementHeightCallback = (int index) =>
        {
            return EditorGUIUtility.singleLineHeight + 2 * EditorGUIUtility.standardVerticalSpacing;
        };

        ReorderableCharacters.onSelectCallback = (ReorderableList edges) =>
        {
            _selectedCharacter = edges.index;
        };
    }
    #endregion

    #region Scene Debug Methods
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

        SceneView.RepaintAll();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (Settings.CurrentLevelState != null)
        {
            Vertex[] vertices = Settings.CurrentLevelState.GetVerticesCopy();

            // Draw vertex debugs
            if (_vertices != null)
            {
                for (int i = 0; i < vertices.Length; i++)
                {
                    // If the vertex is selected during the creation of a edge
                    if ((toolbarSelection == 0 && (_selectedVertexA > 0 || _selectedVertexB > 0) && (i == _selectedVertexA - 1 || i == _selectedVertexB - 1))
                     || (toolbarSelection == 1 && _selectedVertexForCharacter > 0 && i == _selectedVertexForCharacter - 1))
                    {
                        Handles.color = Settings.DebugVertexForEdgeColor;
                    }
                    // If the vertex is selected in the list of vertex
                    else if ((toolbarSelection == 0 && (_selectedVertexA == 0 && _selectedVertexB == 0) && i == _vertices.index))
                    {
                        Handles.color = Settings.DebugSelectedVertexColor;
                    }
                    else
                    {
                        Handles.color = Settings.DebugVertexColor;
                    }

                    Handles.DrawSolidDisc(vertices[i].Position + Vector3.up * .1f, Vector3.up, Settings.DebugVertexDiscRadius);
                }
            }

            // Draw edge debugs
            if (_edges != null)
            {
                Edge[] edges = Settings.CurrentLevelState.GetEdgesCopy();

                int vertexAIndex;
                int vertexBIndex;

                for (int i = 0; i < edges.Length; i++)
                {
                    if (toolbarSelection == 0 && _selectedVertexA == 0 && _selectedVertexB == 0 && i == _edges.index)
                    {
                        Handles.color = Settings.DebugSelectedEdgeColor;
                    }
                    else
                    {
                        Handles.color = Settings.DebugEdgeColor;
                    }

                    vertexAIndex = edges[i].VertexA;
                    vertexBIndex = edges[i].VertexB;

                    Handles.DrawLine(vertices[vertexAIndex].Position + Vector3.up * .1f, vertices[vertexBIndex].Position + Vector3.up * .1f, .5f);
                }
            }

            // Draw character debugs
            if (toolbarSelection == 1 && _characters != null)
            {
                LevelStateCharacter[] characters = Settings.CurrentLevelState.GetCharacters();

                Dictionary<int, int> characterCountByVertex = new Dictionary<int, int>();
                int selectedCharacterVertex = -1;

                if (_selectedCharacter > -1)
                {
                    selectedCharacterVertex = characters[_selectedCharacter].Vertex;
                }

                foreach (LevelStateCharacter character in characters)
                {
                    if(!characterCountByVertex.ContainsKey(character.Vertex))
                    {
                        characterCountByVertex.Add(character.Vertex, 1);
                    }
                    else
                    {
                        characterCountByVertex[character.Vertex] += 1;
                    }
                }

                foreach (KeyValuePair<int, int> vertex in characterCountByVertex)
                {
                    if (selectedCharacterVertex != vertex.Key)
                    {
                        Handles.Label(vertices[vertex.Key].Position + Vector3.up * 2.0f, Settings.CharacterIcon);
                    }
                    else
                    {
                        Handles.Label(vertices[vertex.Key].Position + Vector3.up * 2.0f, Settings.SelectedCharacterIcon);
                    }

                    Handles.Label(vertices[vertex.Key].Position + Vector3.up * 1.0f, "X" + vertex.Value.ToString(), Settings.CharacterCounterStyle);
                }
            }
        }
    }
    #endregion
}
