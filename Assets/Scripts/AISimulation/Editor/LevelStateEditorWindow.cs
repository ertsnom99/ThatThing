using System;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class LevelStateEditorWindow : EditorWindow
{
    #region Variables
    private LevelStateEditorSettings _editorSettings;
    private SimulationSettings _simulationSettings;
    private LevelState _previousLevelState;

    private SerializedObject _serializedLevelState;

    // Toolbar variables
    private int toolbarSelection = 0;
    private string[] toolbarStrings = { "Level Graph", "Characters" };

    // Scrolling
    private Vector2 _scrollPos;

    // Level graph variables
    private ReorderableList _vertices;
    private ReorderableList _edges;

    private bool _showVertices = true;
    private Vector3 _addedOffset;
    private LayerMask _vertexClickMask;
    private bool _creatingVertexWithClick = false;
    private bool _displayIds = true;
    private int _selectedVertex = -1;
    private bool _foldEdges = true;
    private LayerMask _edgeClickMask;
    private bool _creatingEdgeWithClick = false;
    private bool _displayEdges = true;
    private int _selectedPopupVertexA = 0;
    private int _selectedPopupVertexB = 0;
    private int _clickedVertexA = -1;
    private int _selectedEdge = -1;

    // Characters variables
    private ReorderableList _characters;

    private int _selectedCharacter = -1;
    private int _selectedVertexForCharacter = 0;

    private GUIStyle _defaultInvalidStyle = new GUIStyle();

    // Dimensions
    private const float _editorMinWidth = 300;
    private const float _editorMinHeight = 300;

    private const float _foldoutArrowWidth = 12.0f;
    private const float _reorderableListElementSpaceRatio = .14f;
    #endregion

    private void OnEnable()
    {
        // Setup _defaultInvalidStyle
        _defaultInvalidStyle.normal.textColor = Color.red;
        _defaultInvalidStyle.fontSize = 16;
        _defaultInvalidStyle.alignment = TextAnchor.MiddleCenter;
        _defaultInvalidStyle.wordWrap = true;
    }

    // Add menu item to the main menu and inspector context menus and the static function becomes a menu command
    [MenuItem("AI Simulation/Level State Editor")]
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
        EditorGUILayout.Space(15);

        // Error if there is not editor settings
        if (!_editorSettings)
        {
            _editorSettings = EditorGUIUtility.Load("Level State Editor/LevelStateEditorSettings.asset") as LevelStateEditorSettings;
            
            if (_editorSettings)
            {
                _addedOffset = _editorSettings.DefaultAddedOffset;
                _vertexClickMask = _editorSettings.DefaultClickMask;
                _edgeClickMask = _editorSettings.DefaultClickMask;
            }
            else
            {
                EditorGUILayout.LabelField("Couldn't find LevelStateEditorSettings.asset file in Assets\\Editor Default Resources\\Level State!", _defaultInvalidStyle);
                return;
            }
        }

        // Error if there is not simulation settings
        if (!_simulationSettings)
        {
            _simulationSettings = SimulationSettings.LoadFromAsset();

            if (!_simulationSettings)
            {
                EditorGUILayout.LabelField("No SimulationSettings were found!", _editorSettings.InvalidStyle);
                return;
            }
        }
        
        // Field for the LevelState
        EditorGUILayout.LabelField("Assign LevelState:", GUILayout.Width(200));
        _editorSettings.CurrentLevelState = (LevelState)EditorGUILayout.ObjectField(_editorSettings.CurrentLevelState, typeof(LevelState), false, GUILayout.Width(200));
        
        // When the LecelState changed
        if (_previousLevelState != _editorSettings.CurrentLevelState)
        {
            _serializedLevelState = null;
            _previousLevelState = _editorSettings.CurrentLevelState;
            
            // Save the editor settings
            EditorUtility.SetDirty(_editorSettings);
        }

        if (_editorSettings.CurrentLevelState != null)
        {
            if (_serializedLevelState == null)
            {
                _serializedLevelState = new SerializedObject(_editorSettings.CurrentLevelState);

                // Level graph serialization
                _vertices = new ReorderableList(_serializedLevelState, _serializedLevelState.FindProperty("_graph.Vertices"), false, false, false, false);
                _edges = new ReorderableList(_serializedLevelState, _serializedLevelState.FindProperty("_graph.Edges"), false, false, false, false);

                // Characters serialization
                _characters = new ReorderableList(_serializedLevelState, _serializedLevelState.FindProperty("_characters"), false, true, false, false);

                // Reset selections
                _selectedVertex = -1;
                _selectedEdge = -1;
                _selectedPopupVertexA = 0;
                _selectedPopupVertexB = 0;
                _selectedCharacter = -1;
                _selectedVertexForCharacter = 0;

                // Reset variables for click
                _creatingVertexWithClick = false;
                _creatingEdgeWithClick = false;
                _clickedVertexA = -1;
            }
            else
            {
                // Make sure that the LevelState is updated (for when the ScriptableObject was changed outside the editor window)
                _serializedLevelState.Update();
            }

            EditorGUILayout.Space(15);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            toolbarSelection = GUILayout.Toolbar(toolbarSelection, toolbarStrings, GUILayout.Width(300), GUILayout.Height(20));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            EditorGUILayout.Space(15);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, false, false, GUILayout.Width(position.width), GUILayout.Height(position.height - 120));

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

            if (EditorGUI.EndChangeCheck())
            {
                _serializedLevelState.ApplyModifiedProperties();
            }

            // Must enable so that mouse wheel make GUI scroll
            GUI.enabled = true;

            EditorGUILayout.EndScrollView();

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
        GUILayout.BeginHorizontal();
        GUI.enabled = true;
        _showVertices = EditorGUILayout.Foldout(_showVertices, "Vertices");
        GUI.enabled = false;
        EditorGUILayout.IntField(_vertices.count, GUILayout.Width(50.0f));
        GUILayout.EndHorizontal();

        if (_showVertices)
        {
            EditorGUILayout.Space(5);

            GUI.enabled = true;

            // Add list of vertices
            HandleVertices(_vertices);
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
                _editorSettings.CurrentLevelState.AddVertex(_addedOffset);

                // Most be set dirty because the changes where made directly inside the ScriptableObject
                EditorUtility.SetDirty(_editorSettings.CurrentLevelState);
            }

            GUI.enabled = Selection.activeTransform;

            if (GUILayout.Button("Add vertex with selection"))
            {
                _editorSettings.CurrentLevelState.AddVertex(Selection.activeTransform.position + _addedOffset);

                // Most be set dirty because the changes where made directly inside the ScriptableObject
                EditorUtility.SetDirty(_editorSettings.CurrentLevelState);
            }

            GUI.enabled = !_creatingVertexWithClick;

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
                _editorSettings.CurrentLevelState.RemoveVertex(_selectedVertex);

                // Most be set dirty because the changes where made directly inside the ScriptableObject
                EditorUtility.SetDirty(_editorSettings.CurrentLevelState);

                _selectedVertex = -1;
                _vertices.index = -1;
            }
        }

        GUI.enabled = true;

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
        GUILayout.BeginHorizontal();
        GUI.enabled = true;
        _foldEdges = !EditorGUILayout.Foldout(!_foldEdges, "Edges");
        GUI.enabled = false;
        EditorGUILayout.IntField(_edges.count, GUILayout.Width(50.0f));
        GUILayout.EndHorizontal();
        
        if (!_foldEdges)
        {
            EditorGUILayout.Space(5);

            GUI.enabled = true;

            // Add list of edges
            HandleEdges(_edges);
            GUILayout.BeginHorizontal();
            GUILayout.Space(_foldoutArrowWidth);
            _edges.DoLayoutList();
            GUILayout.EndHorizontal();
            
            // Add dropdown for vertex
            List<string> ids = _editorSettings.CurrentLevelState.GetAllVertexIds();
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
            
            GUI.enabled = _selectedPopupVertexA > 0 && _selectedPopupVertexB > 0;

            if (GUILayout.Button("Add edge"))
            {
                if (_editorSettings.CurrentLevelState.AddEdge(_selectedPopupVertexA - 1, _selectedPopupVertexB - 1))
                {
                    // Most be set dirty because the changes where made directly inside the ScriptableObject
                    EditorUtility.SetDirty(_editorSettings.CurrentLevelState);
                }

                _selectedPopupVertexA = 0;
                _selectedPopupVertexB = 0;
            }
            
            GUI.enabled = !_creatingEdgeWithClick;

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
                _editorSettings.CurrentLevelState.RemoveEdge(_selectedEdge);

                // Most be set dirty because the changes where made directly inside the ScriptableObject
                EditorUtility.SetDirty(_editorSettings.CurrentLevelState);

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
            SerializedProperty traversable = ReorderableEdges.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Traversable");
            SerializedProperty type = ReorderableEdges.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Type");

            if (_vertices.count > vertexA.intValue && _vertices.count > vertexB.intValue)
            {
                string vertexAId = _vertices.serializedProperty.GetArrayElementAtIndex(vertexA.intValue).FindPropertyRelative("Id").intValue.ToString();
                string vertexBId = _vertices.serializedProperty.GetArrayElementAtIndex(vertexB.intValue).FindPropertyRelative("Id").intValue.ToString();
                string vertexAToVertexB = "Vertex " + vertexAId + " to vertex " + vertexBId;

                EditorGUI.BeginChangeCheck();

                _editorSettings.CurrentLevelState.EdgesFolded[index] = !EditorGUI.Foldout(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * .1f, rect.width * .2f, EditorGUIUtility.singleLineHeight), !_editorSettings.CurrentLevelState.EdgesFolded[index], "Id: " + id.intValue.ToString());
                EditorGUI.LabelField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * .1f, rect.width * .8f, EditorGUIUtility.singleLineHeight), new GUIContent(vertexAToVertexB));

                if(EditorGUI.EndChangeCheck())
                {
                    // Most be set dirty because the changes where made directly to the ScriptableObject
                    EditorUtility.SetDirty(_editorSettings.CurrentLevelState);
                }

                if (!_editorSettings.CurrentLevelState.EdgesFolded[index])
                {
                    EditorGUI.LabelField(new Rect(rect.x + _foldoutArrowWidth, rect.y + EditorGUIUtility.singleLineHeight * (1.0f + _reorderableListElementSpaceRatio), rect.width * .2f - _foldoutArrowWidth, EditorGUIUtility.singleLineHeight), new GUIContent("Traversable"));
                    EditorGUI.PropertyField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * (1.0f + _reorderableListElementSpaceRatio), rect.width * .8f, EditorGUIUtility.singleLineHeight), traversable, GUIContent.none);
                    EditorGUI.LabelField(new Rect(rect.x + _foldoutArrowWidth, rect.y + EditorGUIUtility.singleLineHeight * (2.0f + _reorderableListElementSpaceRatio), rect.width * .2f - _foldoutArrowWidth, EditorGUIUtility.singleLineHeight), new GUIContent("Type"));
                    EditorGUI.PropertyField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * (2.0f + _reorderableListElementSpaceRatio), rect.width * .8f, EditorGUIUtility.singleLineHeight), type, GUIContent.none);
                }
            }
        };

        ReorderableEdges.elementHeightCallback = (int index) =>
        {
            float height = EditorGUIUtility.standardVerticalSpacing;

            if (!_editorSettings.CurrentLevelState.EdgesFolded[index])
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

    private void DrawCharactersSection()
    {
        if (!_simulationSettings.CharactersSettingsUsed)
        {
            EditorGUILayout.LabelField("No CharactersSetting is specified in the SimulationSettings!", _editorSettings.InvalidStyle);
            return;
        }

        GUI.enabled = false;

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("CharactersSettings used:", GUILayout.Width(EditorGUIUtility.currentViewWidth * .26f - _foldoutArrowWidth));
        EditorGUILayout.ObjectField(_simulationSettings.CharactersSettingsUsed, typeof(CharactersSettings), false);
        GUILayout.EndHorizontal();

        GUI.enabled = true;

        if (_simulationSettings.CharactersSettingsUsed.Settings.Length <= 0)
        {
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("The CharactersSetting used doesn't have any settings!", _editorSettings.InvalidStyle);
            return;
        }

        EditorGUILayout.Space(15);

        // Add list of edges
        HandleCharacters(_characters, _simulationSettings.CharactersSettingsUsed.GetSettingsNames());
        _characters.DoLayoutList();

        GUILayout.BeginHorizontal();
        // "Add Character" button
        GUI.enabled = _selectedVertexForCharacter > 0;

        if (GUILayout.Button("Add Character", GUILayout.Width(EditorGUIUtility.currentViewWidth / 2.0f)))
        {
            _editorSettings.CurrentLevelState.AddCharacter(_selectedVertexForCharacter - 1);

            // Most be set dirty because the changes where made directly inside the ScriptableObject
            EditorUtility.SetDirty(_editorSettings.CurrentLevelState);

            _selectedVertexForCharacter = 0;
        }

        GUI.enabled = true;

        // Add dropdown to select a vertex
        List<string> ids = _editorSettings.CurrentLevelState.GetAllVertexIds();
        ids.Insert(0, "None");

        _selectedVertexForCharacter = EditorGUILayout.Popup(_selectedVertexForCharacter, ids.ToArray());

        GUILayout.EndHorizontal();

        GUI.enabled = _selectedCharacter > -1;

        // "Remove selected edge" button
        if (GUILayout.Button("Remove selected character"))
        {
            _editorSettings.CurrentLevelState.RemoveCharacter(_selectedCharacter);

            // Most be set dirty because the changes where made directly inside the ScriptableObject
            EditorUtility.SetDirty(_editorSettings.CurrentLevelState);

            _selectedCharacter = -1;
            _characters.index = -1;
        }
    }

    // Add the correct callbacks for the _characters ReorderableList
    private void HandleCharacters(ReorderableList ReorderableCharacters, string[] settingsOptions)
    {
        ReorderableCharacters.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Characters");
        };

        ReorderableCharacters.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            // Get the vertex data
            SerializedProperty vertex = ReorderableCharacters.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Vertex");
            SerializedProperty settings = ReorderableCharacters.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Settings");

            // Reset the selected settings in case it doesn't point to a valid one
            if (settings.intValue >= _simulationSettings.CharactersSettingsUsed.Settings.Length)
            {
                settings.intValue = 0;
                _serializedLevelState.ApplyModifiedProperties();
            }

            EditorGUI.BeginChangeCheck();

            string vertexId = _vertices.serializedProperty.GetArrayElementAtIndex(vertex.intValue).FindPropertyRelative("Id").intValue.ToString();
            _editorSettings.CurrentLevelState.CharactersFolded[index] = !EditorGUI.Foldout(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * .1f, rect.width * .25f, EditorGUIUtility.singleLineHeight), !_editorSettings.CurrentLevelState.CharactersFolded[index], "Vertex(id): " + vertexId);
            settings.intValue = EditorGUI.Popup(new Rect(rect.x + rect.width * .25f, rect.y + EditorGUIUtility.singleLineHeight * _reorderableListElementSpaceRatio, rect.width * (1 - .25f), EditorGUIUtility.singleLineHeight), settings.intValue, settingsOptions);

            if (EditorGUI.EndChangeCheck())
            {
                // Most be set dirty because the changes where made directly to the ScriptableObject
                EditorUtility.SetDirty(_editorSettings.CurrentLevelState);
            }

            if (!_editorSettings.CurrentLevelState.CharactersFolded[index])
            {
                GUI.enabled = false;
                EditorGUI.LabelField(new Rect(rect.x + _foldoutArrowWidth, rect.y + EditorGUIUtility.singleLineHeight * (1.0f + _reorderableListElementSpaceRatio), rect.width * .25f - _foldoutArrowWidth, EditorGUIUtility.singleLineHeight), new GUIContent("Prefab"));
                EditorGUI.ObjectField(new Rect(rect.x + rect.width * .25f, rect.y + EditorGUIUtility.singleLineHeight * (1.0f + _reorderableListElementSpaceRatio), rect.width * .75f, EditorGUIUtility.singleLineHeight), _simulationSettings.CharactersSettingsUsed.Settings[settings.intValue].Prefab, typeof(GameObject), false);
                EditorGUI.LabelField(new Rect(rect.x + _foldoutArrowWidth, rect.y + EditorGUIUtility.singleLineHeight * (2.0f + _reorderableListElementSpaceRatio), rect.width * .25f - _foldoutArrowWidth, EditorGUIUtility.singleLineHeight), new GUIContent("Prefab Behavior"));
                EditorGUI.ObjectField(new Rect(rect.x + rect.width * .25f, rect.y + EditorGUIUtility.singleLineHeight * (2.0f + _reorderableListElementSpaceRatio), rect.width * .75f, EditorGUIUtility.singleLineHeight), _simulationSettings.CharactersSettingsUsed.Settings[settings.intValue].PrefabBehavior, typeof(ExternalBehaviorTree), false);
                EditorGUI.LabelField(new Rect(rect.x + _foldoutArrowWidth, rect.y + EditorGUIUtility.singleLineHeight * (3.0f + _reorderableListElementSpaceRatio), rect.width * .25f - _foldoutArrowWidth, EditorGUIUtility.singleLineHeight), new GUIContent("Simplified Behavior"));
                EditorGUI.ObjectField(new Rect(rect.x + rect.width * .25f, rect.y + EditorGUIUtility.singleLineHeight * (3.0f + _reorderableListElementSpaceRatio), rect.width * .75f, EditorGUIUtility.singleLineHeight), _simulationSettings.CharactersSettingsUsed.Settings[settings.intValue].SimplifiedBehavior, typeof(ExternalBehaviorTree), false);
                GUI.enabled = true;
            }
        };

        ReorderableCharacters.elementHeightCallback = (int index) =>
        {
            float height = EditorGUIUtility.standardVerticalSpacing;

            if (!_editorSettings.CurrentLevelState.CharactersFolded[index])
            {
                height += EditorGUIUtility.singleLineHeight * 4.0f;
            }
            else
            {
                height += EditorGUIUtility.singleLineHeight;
            }

            return height + EditorGUIUtility.standardVerticalSpacing;
        };

        ReorderableCharacters.onSelectCallback = (ReorderableList edges) =>
        {
            _selectedCharacter = edges.index;
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
        if (_editorSettings && _editorSettings.CurrentLevelState && _simulationSettings)
        {
            HandleUserInput(Event.current, sceneView);
            DrawSecenDebug();
            DrawGUIDebug();
        }
    }

    private void DrawSecenDebug()
    {
        Vertex[] vertices = _editorSettings.CurrentLevelState.GetVertices();
        
        // Draw vertex debugs
        if (_vertices != null)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                // If the vertex is selected during the creation of a edge
                if ((toolbarSelection == 0 && !_creatingEdgeWithClick && (_selectedPopupVertexA > 0 || _selectedPopupVertexB > 0) && (i == _selectedPopupVertexA - 1 || i == _selectedPopupVertexB - 1))
                 || (toolbarSelection == 0 && _creatingEdgeWithClick && _clickedVertexA > -1 && i == _clickedVertexA)
                 || (toolbarSelection == 1 && _selectedVertexForCharacter > 0 && i == _selectedVertexForCharacter - 1))
                {
                    Handles.color = _editorSettings.DebugVertexForEdgeColor;
                }
                // If the vertex is selected in the list of vertex
                else if ((toolbarSelection == 0 && !_creatingEdgeWithClick && (_selectedPopupVertexA == 0 && _selectedPopupVertexB == 0) && i == _vertices.index))
                {
                    Handles.color = _editorSettings.DebugSelectedVertexColor;

                    EditorGUI.BeginChangeCheck();
                    Vector3 newPosition = Handles.PositionHandle(vertices[i].Position, Quaternion.identity);

                    // Record _editorSettings.CurrentLevelState before applying change in order to allow undos
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_editorSettings.CurrentLevelState, "Changed Vertex Position");
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
            Edge[] edges = _editorSettings.CurrentLevelState.GetEdgesCopy();

            int vertexAIndex;
            int vertexBIndex;

            for (int i = 0; i < edges.Length; i++)
            {
                if (toolbarSelection != 0 || _selectedPopupVertexA != 0 || _selectedPopupVertexB != 0 || i != _edges.index)
                {
                    switch (edges[i].Type)
                    {
                        case EdgeType.Corridor:
                            Handles.color = _editorSettings.DebugEdgeCorridorColor;
                            break;
                        case EdgeType.Door:
                            Handles.color = _editorSettings.DebugEdgeDoorColor;
                            break;
                        case EdgeType.Vent:
                            Handles.color = _editorSettings.DebugEdgeVentColor;
                            break;
                    }
                }
                else
                {
                    Handles.color = _editorSettings.DebugSelectedEdgeColor;
                }

                vertexAIndex = edges[i].VertexA;
                vertexBIndex = edges[i].VertexB;

                Handles.DrawLine(vertices[vertexAIndex].Position, vertices[vertexBIndex].Position, _editorSettings.DebugEdgeThickness);
            }
        }

        // Display Id above vertices
        if (toolbarSelection == 0 && _displayIds)
        {
            foreach(Vertex vertex in vertices)
            {
                Handles.Label(vertex.Position, "Id: " + vertex.Id, _editorSettings.VertexIdStyle);
            }
        }

        // Draw character debugs
        bool characterSettingsValid = _simulationSettings.CharactersSettingsUsed && _simulationSettings.CharactersSettingsUsed.Settings.Length > 0;
        
        if (characterSettingsValid && toolbarSelection == 1 && _characters != null)
        {
            LevelStateCharacter[] characters = _editorSettings.CurrentLevelState.GetCharacters();

            Dictionary<int, int> characterCountByVertex = new Dictionary<int, int>();
            int selectedCharacterVertex = -1;

            if (_selectedCharacter > -1)
            {
                selectedCharacterVertex = characters[_selectedCharacter].Vertex;
            }

            foreach (LevelStateCharacter character in characters)
            {
                if (!characterCountByVertex.ContainsKey(character.Vertex))
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
                    Handles.Label(vertices[vertex.Key].Position + Vector3.up * 2.0f, _editorSettings.CharacterIcon);
                }
                else
                {
                    Handles.Label(vertices[vertex.Key].Position + Vector3.up * 2.0f, _editorSettings.SelectedCharacterIcon);
                }

                Handles.Label(vertices[vertex.Key].Position + Vector3.up * 1.0f, "X" + vertex.Value.ToString(), _editorSettings.CharacterCounterStyle);
            }
        }
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
                _editorSettings.CurrentLevelState.AddVertex(worldPosition + _addedOffset);
                
                // Most be set dirty because the changes where made directly inside the ScriptableObject
                EditorUtility.SetDirty(_editorSettings.CurrentLevelState);
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
                    if (_editorSettings.CurrentLevelState.AddEdge(_clickedVertexA, selectedVertex))
                    {
                        // Most be set dirty because the changes where made directly inside the ScriptableObject
                        EditorUtility.SetDirty(_editorSettings.CurrentLevelState);
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
        Vertex[] vertices = _editorSettings.CurrentLevelState.GetVertices();

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