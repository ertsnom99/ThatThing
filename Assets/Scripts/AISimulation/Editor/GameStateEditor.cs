using System.Collections.Generic;
using System.IO;
using BehaviorDesigner.Runtime;
using GraphCreator;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

// This is a custom editor for the GameState class
[CustomEditor(typeof(GameState))]
public class GameStateEditor : Editor
{
    private GameState _gameState;

    private SerializedProperty _script;
    private SerializedProperty _serializedPlayerState;
    private SerializedProperty _levelStatesByBuildIndex;
    private SerializedProperty _characterIdCount;
    private SerializedProperty _displayCharacterCounters;
    private ReorderableList _levelStateByBuildIndexList;
    private List<ReorderableList> _characterStates = new List<ReorderableList>();

    private const int _sectionSpacing = 15;

    private List<int> _buildIndexes = new List<int>();

    private Color _originalTextColor;
    private Color _originalBackgroundColor;

    private SimulationSettings _simulationSettings;

    private GUIStyle _invalidStyle = new GUIStyle();

    private Vector3 _characterCounterOffset = new Vector3(-0.6f, 2.0f, .0f);
    private GUIStyle _counterStyle = new GUIStyle();
    private GUIStyle _selectedCounterStyle = new GUIStyle();

    private string _duplicateAssetName = "GameState";

    private const float _reorderableListElementSpaceRatio = .14f;
    private const float _foldoutArrowOffset = 10.0f;
    private const float _foldoutArrowWidth = 12.0f;

    private const float _fullOffsetDistance = 20.0f;

    private void OnEnable()
    {
        _gameState = (GameState)target;

        _script = serializedObject.FindProperty("m_Script");
        _serializedPlayerState = serializedObject.FindProperty("_playerState");
        _levelStatesByBuildIndex = serializedObject.FindProperty("_levelStatesByBuildIndex");
        _characterIdCount = serializedObject.FindProperty("CharacterIdCount");
        _displayCharacterCounters = serializedObject.FindProperty("DisplayCharacterCounters");
        
        _levelStateByBuildIndexList = new ReorderableList(serializedObject, _levelStatesByBuildIndex, false, true, true, true);
        CreateCharacterStates();

        _invalidStyle.normal.textColor = Color.red;
        _invalidStyle.fontSize = 20;
        _invalidStyle.wordWrap = true;
        _invalidStyle.fontStyle = FontStyle.Bold;

        _counterStyle.normal.textColor = Color.white;
        _counterStyle.fontSize = 20;
        _counterStyle.fontStyle = FontStyle.Bold;

        _selectedCounterStyle.normal.textColor = Color.cyan;
        _selectedCounterStyle.fontSize = 20;
        _selectedCounterStyle.fontStyle = FontStyle.Bold;

        _originalTextColor = GUI.color;
        _originalBackgroundColor = GUI.backgroundColor;

        // Remove delegate listener if it has previously
        // been assigned.
        SceneView.duringSceneGui -= OnSceneGUI;
        // Add (or re-add) the delegate.
        SceneView.duringSceneGui += OnSceneGUI;

        SceneView.RepaintAll();
    }

    private void CreateCharacterStates()
    {
        for (int i = 0; i < _levelStateByBuildIndexList.count; i++)
        {
            _characterStates.Add(new ReorderableList(serializedObject, _levelStatesByBuildIndex.GetArrayElementAtIndex(i).FindPropertyRelative("_characterStates"), false, true, true, true));
        }
    }

    private void OnDisable()
    {
        // When the window is destroyed, remove the delegate
        // so that it will no longer do any drawing.
        SceneView.duringSceneGui -= OnSceneGUI;

        SceneView.RepaintAll();
    }

    public override void OnInspectorGUI()
    {
        // TODO: Remove these lines
        //DrawDefaultInspector();
        //return;

        // Add the default Script field
        GUI.enabled = false;
        EditorGUILayout.PropertyField(_script);
        GUI.enabled = true;

        EditorGUILayout.Space(_sectionSpacing);

        if (!_simulationSettings)
        {
            _simulationSettings = SimulationSettings.LoadFromAsset();

            if (!_simulationSettings)
            {
                EditorGUILayout.LabelField("No SimulationSettings were found!", _invalidStyle);
                return;
            }
        }

        if (!_simulationSettings.CharactersSettingsUsed)
        {
            EditorGUILayout.LabelField("SimulationSettings doesn't specify a CharactersSettings to use!", _invalidStyle);
            return;
        }

        EditorGUI.BeginChangeCheck();

        // Field for the PlayerState
        bool playerStateSelected = _serializedPlayerState.objectReferenceValue;
        GUI.color = !playerStateSelected ? Color.red : _originalTextColor;
        GUI.backgroundColor = !playerStateSelected ? Color.red : _originalBackgroundColor;
        EditorGUILayout.PropertyField(_serializedPlayerState);
        GUI.color = Color.white;
        GUI.backgroundColor = _originalBackgroundColor;

        EditorGUILayout.Space(_sectionSpacing);

        // Add the list of Graph by build indexes
        HandleLevelStatesByBuildIndex(_levelStateByBuildIndexList);
        _levelStateByBuildIndexList.DoLayoutList();

        EditorGUILayout.PropertyField(_displayCharacterCounters);

        // TODO: Add the list of connections

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }

        GUI.enabled = false;
        if (!_gameState.IsValid(_simulationSettings.CharactersSettingsUsed))
        {
            EditorGUILayout.TextArea("GameState is invalid (no PlayerState, build index duplicates or invalid, null Graphs, invalid vertex or invalid settings)", _invalidStyle);
        }
        GUI.enabled = true;

        EditorGUILayout.Space(_sectionSpacing);

        SceneView.RepaintAll();

        #region Duplicate
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Duplicate asset name", GUILayout.MaxWidth(135));
        _duplicateAssetName = EditorGUILayout.TextField(_duplicateAssetName);
        GUILayout.EndHorizontal();

        // Get path to scriptable object
        string path = AssetDatabase.GetAssetPath(target);

        if (path != "")
        {
            int index = path.LastIndexOf("/");
            path = path.Substring(0, index);

            // Can<t duplicate if the name is empty or the file already exist
            GUI.enabled = _duplicateAssetName != "" && !File.Exists(Application.dataPath + path.Remove(0, 6) + "/" + _duplicateAssetName + ".asset");

            if (GUILayout.Button("Duplicate", GUILayout.Width(200)))
            {
                // Duplicate
                ScriptableObject duplicate = Object.Instantiate(target) as ScriptableObject;
                AssetDatabase.CreateAsset(duplicate, path + "/" + _duplicateAssetName + ".asset");
            }
        }

        GUI.enabled = true;
        #endregion
    }

    private void HandleLevelStatesByBuildIndex(ReorderableList levelStatesByBuildIndex)
    {
        // Count how many scenes in the build are active
        int activeBuildIndexCount = 0;

        foreach (EditorBuildSettingsScene editorBuildSettingsScene in EditorBuildSettings.scenes)
        {
            if (editorBuildSettingsScene.enabled)
            {
                activeBuildIndexCount++;
            }
        }

        // Create the array of choices for build index
        string[] buildIndexOptions = new string[activeBuildIndexCount];

        for(int i = 0; i < buildIndexOptions.Length; i++)
        {
            buildIndexOptions[i] = i.ToString();
        }

        levelStatesByBuildIndex.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Level state by build index");
        };

        _buildIndexes.Clear();

        levelStatesByBuildIndex.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            // Get the properties
            SerializedProperty buildIndex = levelStatesByBuildIndex.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_buildIndex");
            SerializedProperty graph = levelStatesByBuildIndex.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_graph");
            
            // Check if the build index is already used
            bool buildIndexAlreadySelected = _buildIndexes.Contains(buildIndex.intValue);

            if (!buildIndexAlreadySelected)
            {
                _buildIndexes.Add(buildIndex.intValue);
            }

            // Field for the build index
            SerializedProperty foldedProperty = levelStatesByBuildIndex.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Folded");

            bool validBuildIndex = !buildIndexAlreadySelected && (buildIndex.intValue >= 0 && buildIndex.intValue < activeBuildIndexCount);
            GUI.color = !validBuildIndex ? Color.red : _originalTextColor;
            GUI.backgroundColor = !validBuildIndex ? Color.red : _originalBackgroundColor;
            foldedProperty.boolValue = !EditorGUI.Foldout(new Rect(rect.x + _foldoutArrowOffset, rect.y + EditorGUIUtility.singleLineHeight * _reorderableListElementSpaceRatio, rect.width * .2f - _foldoutArrowOffset, EditorGUIUtility.singleLineHeight), !foldedProperty.boolValue, "Build Index");
            buildIndex.intValue = EditorGUI.Popup(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * _reorderableListElementSpaceRatio, rect.width * .8f, EditorGUIUtility.singleLineHeight), buildIndex.intValue, buildIndexOptions);
            GUI.color = Color.white;
            GUI.backgroundColor = _originalBackgroundColor;

            // Show all the fields
            if (!foldedProperty.boolValue)
            {
                // Field for the Graph
                bool validGraph = graph.objectReferenceValue;
                GUI.color = !validGraph ? Color.red : _originalTextColor;
                GUI.backgroundColor = !validGraph ? Color.red : _originalBackgroundColor;
                EditorGUI.LabelField(new Rect(rect.x + _foldoutArrowOffset, rect.y + EditorGUIUtility.singleLineHeight * (1.0f + _reorderableListElementSpaceRatio), rect.width * .2f - _foldoutArrowOffset, EditorGUIUtility.singleLineHeight), new GUIContent("Graph"));
                EditorGUI.PropertyField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * (1.0f + _reorderableListElementSpaceRatio), rect.width * .8f, EditorGUIUtility.singleLineHeight), graph, GUIContent.none);
                GUI.color = Color.white;
                GUI.backgroundColor = _originalBackgroundColor;

                // Show character states only if graph is valid
                if (!validGraph)
                {
                    return;
                }

                // Create the array of choices for the vertices
                string[] vertexOptions = new string[_gameState.LevelStatesByBuildIndex[index].Graph.Vertices.Length];

                // Create the array of choices for the settings
                string[] settingsOptions = _simulationSettings.CharactersSettingsUsed.GetSettingsNames();

                for (int i = 0; i < _gameState.LevelStatesByBuildIndex[index].Graph.Vertices.Length; i++)
                {
                    vertexOptions[i] = _gameState.LevelStatesByBuildIndex[index].Graph.Vertices[i].Id.ToString();
                }

                // Add the list of Character States
                HandleCharacterStates(_characterStates[index], vertexOptions, settingsOptions);
                _characterStates[index].DoList(new Rect(rect.x + _foldoutArrowOffset, rect.y + EditorGUIUtility.singleLineHeight * 2.5f, rect.width - _foldoutArrowOffset, rect.height));
            }
        };

        levelStatesByBuildIndex.elementHeightCallback = (int index) =>
        {
            // Space before the CharacterState list (without Graph field)
            float height = EditorGUIUtility.standardVerticalSpacing * 2.0f + EditorGUIUtility.singleLineHeight * 2.0f;

            // If LevelStateByBuildIndex is folded
            if (levelStatesByBuildIndex.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Folded").boolValue)
            {
                return height;
            }

            // Graph field
            height += EditorGUIUtility.singleLineHeight;

            // If graph is invalid
            if (!_gameState.LevelStatesByBuildIndex[index].Graph)
            {
                return height;
            }

            // Minimum space taken by the ReorderabelList
            height += EditorGUIUtility.singleLineHeight * 3.0f;

            if (_characterStates[index].count == 0)
            {
                // Space for empty ReorderabelList
                height += EditorGUIUtility.standardVerticalSpacing * 2.0f + EditorGUIUtility.singleLineHeight;
            }
            else
            {
                // Space based on how many characters there is and if they are folded/valid or not
                for (int i = 0; i < _characterStates[index].count; i++)
                {
                    height += EditorGUIUtility.standardVerticalSpacing * 2.0f;

                    bool folded = _characterStates[index].serializedProperty.GetArrayElementAtIndex(i).FindPropertyRelative("Folded").boolValue;
                    int settings = _characterStates[index].serializedProperty.GetArrayElementAtIndex(i).FindPropertyRelative("_settings").intValue;

                    if (folded || settings < 0 || settings >= _simulationSettings.CharactersSettingsUsed.Settings.Length)
                    {
                        height += EditorGUIUtility.singleLineHeight * 3.11f;
                    }
                    else
                    {
                        height += EditorGUIUtility.singleLineHeight * 6.11f;
                    }
                }
            }
            
            return height;
        };

        levelStatesByBuildIndex.onAddCallback = list =>
        {
            // Add new levelStateByBuildIndex
            ReorderableList.defaultBehaviours.DoAddButton(list);

            // Clear the CharacterStates of the new LevelStatesByBuildIndex
            _levelStatesByBuildIndex.GetArrayElementAtIndex(_levelStatesByBuildIndex.arraySize - 1).FindPropertyRelative("_characterStates").arraySize = 0;
            
            // Create a new ReorderableList and store it
            _characterStates.Add(new ReorderableList(serializedObject, _levelStatesByBuildIndex.GetArrayElementAtIndex(_levelStatesByBuildIndex.arraySize - 1).FindPropertyRelative("_characterStates"), false, true, true, true));
        };

        levelStatesByBuildIndex.onRemoveCallback = list =>
        {
            // Remove selected levelStateByBuildIndex
            ReorderableList.defaultBehaviours.DoRemoveButton(list);

            // Recreate all CharacterState ReorderableLists (necessary to avoid out of bounds error)
            _characterStates.Clear();
            CreateCharacterStates();
        };
    }

    private void HandleCharacterStates(ReorderableList characterStates, string[] vertexOptions, string[] settingsOptions)
    {
        characterStates.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Character States");
        };

        characterStates.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            // Get the properties
            int id = characterStates.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_id").intValue;
            SerializedProperty vertex = characterStates.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_vertex");
            SerializedProperty settings = characterStates.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_settings");

            // Id
            EditorGUI.LabelField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * _reorderableListElementSpaceRatio, rect.width * .2f, EditorGUIUtility.singleLineHeight), new GUIContent("ID"));
            EditorGUI.LabelField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * _reorderableListElementSpaceRatio, rect.width * .8f, EditorGUIUtility.singleLineHeight), new GUIContent(id.ToString()));

            // Dropdown for vertex
            bool validVertex = vertex.intValue >= 0 && vertex.intValue < vertexOptions.Length;
            GUI.color = !validVertex ? Color.red : _originalTextColor;
            GUI.backgroundColor = !validVertex ? Color.red : _originalBackgroundColor;
            EditorGUI.LabelField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * (1.1f + _reorderableListElementSpaceRatio), rect.width * .2f, EditorGUIUtility.singleLineHeight), new GUIContent("Vertex"));
            vertex.intValue = EditorGUI.Popup(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * (1.0f + _reorderableListElementSpaceRatio), rect.width * .8f, EditorGUIUtility.singleLineHeight), vertex.intValue, vertexOptions);
            GUI.color = Color.white;
            GUI.backgroundColor = _originalBackgroundColor;

            // Dropdown for settings
            SerializedProperty foldedProperty = characterStates.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Folded");
            
            bool validSettings = settings.intValue >= 0 && settings.intValue < settingsOptions.Length;
            GUI.color = !validSettings ? Color.red : _originalTextColor;
            GUI.backgroundColor = !validSettings ? Color.red : _originalBackgroundColor;
            foldedProperty.boolValue = !EditorGUI.Foldout(new Rect(rect.x + _foldoutArrowOffset, rect.y + EditorGUIUtility.singleLineHeight * (2.1f + _reorderableListElementSpaceRatio), rect.width * .2f - _foldoutArrowOffset, EditorGUIUtility.singleLineHeight), !foldedProperty.boolValue, "Settings");
            settings.intValue = EditorGUI.Popup(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * (2.0f + _reorderableListElementSpaceRatio), rect.width * .8f, EditorGUIUtility.singleLineHeight), settings.intValue, settingsOptions);
            GUI.color = Color.white;
            GUI.backgroundColor = _originalBackgroundColor;

            // Show settings details
            if (!foldedProperty.boolValue && settings.intValue >= 0 && settings.intValue < _simulationSettings.CharactersSettingsUsed.Settings.Length)
            {
                GUI.enabled = false;
                EditorGUI.LabelField(new Rect(rect.x + _foldoutArrowWidth, rect.y + EditorGUIUtility.singleLineHeight * (3.0f + _reorderableListElementSpaceRatio), rect.width * .25f - _foldoutArrowWidth, EditorGUIUtility.singleLineHeight), new GUIContent("Prefab"));
                EditorGUI.ObjectField(new Rect(rect.x + rect.width * .25f, rect.y + EditorGUIUtility.singleLineHeight * (3.0f + _reorderableListElementSpaceRatio), rect.width * .75f, EditorGUIUtility.singleLineHeight), _simulationSettings.CharactersSettingsUsed.Settings[settings.intValue].Prefab, typeof(GameObject), false);
                EditorGUI.LabelField(new Rect(rect.x + _foldoutArrowWidth, rect.y + EditorGUIUtility.singleLineHeight * (4.0f + _reorderableListElementSpaceRatio), rect.width * .25f - _foldoutArrowWidth, EditorGUIUtility.singleLineHeight), new GUIContent("Prefab Behavior"));
                EditorGUI.ObjectField(new Rect(rect.x + rect.width * .25f, rect.y + EditorGUIUtility.singleLineHeight * (4.0f + _reorderableListElementSpaceRatio), rect.width * .75f, EditorGUIUtility.singleLineHeight), _simulationSettings.CharactersSettingsUsed.Settings[settings.intValue].PrefabBehavior, typeof(ExternalBehaviorTree), false);
                EditorGUI.LabelField(new Rect(rect.x + _foldoutArrowWidth, rect.y + EditorGUIUtility.singleLineHeight * (5.0f + _reorderableListElementSpaceRatio), rect.width * .25f - _foldoutArrowWidth, EditorGUIUtility.singleLineHeight), new GUIContent("Simplified Behavior"));
                EditorGUI.ObjectField(new Rect(rect.x + rect.width * .25f, rect.y + EditorGUIUtility.singleLineHeight * (5.0f + _reorderableListElementSpaceRatio), rect.width * .75f, EditorGUIUtility.singleLineHeight), _simulationSettings.CharactersSettingsUsed.Settings[settings.intValue].SimplifiedBehavior, typeof(ExternalBehaviorTree), false);
                GUI.enabled = true;
            }
        };

        characterStates.elementHeightCallback = (int index) =>
        {
            bool folded = characterStates.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Folded").boolValue;
            int settings = characterStates.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_settings").intValue;

            float height = EditorGUIUtility.standardVerticalSpacing * 2.0f;

            if (folded || settings < 0 || settings >= _simulationSettings.CharactersSettingsUsed.Settings.Length)
            {
                height += EditorGUIUtility.singleLineHeight * 3.0f;
            }
            else
            {
                height += EditorGUIUtility.singleLineHeight * 6.0f;
            }

            return height;
        };

        characterStates.onAddCallback = list =>
        {
            // Add new levelStateByBuildIndex
            ReorderableList.defaultBehaviours.DoAddButton(list);

            list.serializedProperty.GetArrayElementAtIndex(list.count - 1).FindPropertyRelative("_id").intValue = _characterIdCount.intValue;
            _characterIdCount.intValue++;
        };
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        bool characterSettingsValid = _simulationSettings && _simulationSettings.CharactersSettingsUsed && _simulationSettings.CharactersSettingsUsed.Settings.Length > 0;

        if (!_gameState.DisplayCharacterCounters || !characterSettingsValid || _levelStateByBuildIndexList.index < 0)
        {
            // Don't display character counters, CharactersSettings used is invalid or no LevelStateByBuildIndex is selected
            return;
        }

        Graph graph = (Graph)_levelStatesByBuildIndex.GetArrayElementAtIndex(_levelStateByBuildIndexList.index).FindPropertyRelative("_graph").objectReferenceValue;

        if (graph == null)
        {
            // No graph selected in the LevelStateByBuildIndex
            return;
        }

        // Count how many characters at each vertex and which character is selected
        int selectedCharacterVertex = -1;
        Dictionary<int, int> characterCountByVertex = new Dictionary<int, int>();
        
        CharacterState[] characterStates = _gameState.LevelStatesByBuildIndex[_levelStateByBuildIndexList.index].CharacterStates;

        for (int i = 0; i < characterStates.Length; i++)
        {
            if (characterStates[i].Vertex < 0 && characterStates[i].Vertex >= graph.Vertices.Length)
            {
                continue;
            }

            if (_characterStates[_levelStateByBuildIndexList.index].index == i)
            {
                selectedCharacterVertex = characterStates[i].Vertex;
            }

            if (!characterCountByVertex.ContainsKey(characterStates[i].Vertex))
            {
                characterCountByVertex.Add(characterStates[i].Vertex, 1);
            }
            else
            {
                characterCountByVertex[characterStates[i].Vertex] += 1;
            }
        }

        // Display the character counters above the vertices
        Vector3 characterCounterOffset = Camera.current.transform.right * _characterCounterOffset.x;
        characterCounterOffset += Camera.current.transform.up * _characterCounterOffset.y;
        characterCounterOffset += Camera.current.transform.forward * _characterCounterOffset.z;

        Vector3 cameraToIcon;
        float distanceToIcon;

        foreach (KeyValuePair<int, int> vertex in characterCountByVertex)
        {
            cameraToIcon = graph.Vertices[vertex.Key].Position - Camera.current.transform.position;
            distanceToIcon = Vector3.Project(cameraToIcon, Camera.current.transform.forward).magnitude;

            if (selectedCharacterVertex != vertex.Key)
            {
                Handles.Label(graph.Vertices[vertex.Key].Position + characterCounterOffset * (distanceToIcon / _fullOffsetDistance), "X" + vertex.Value.ToString(), _counterStyle);
            }
            else
            {
                Handles.Label(graph.Vertices[vertex.Key].Position + characterCounterOffset * (distanceToIcon / _fullOffsetDistance), "X" + vertex.Value.ToString(), _selectedCounterStyle);
            }
        }
    }
}
