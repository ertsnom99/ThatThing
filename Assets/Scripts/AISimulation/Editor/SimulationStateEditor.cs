using System.Collections.Generic;
using System.IO;
using BehaviorDesigner.Runtime;
using GraphCreator;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

// This is a custom editor for the SimulationState class
[CustomEditor(typeof(SimulationState))]
public class SimulationStateEditor : Editor
{
    private SimulationState _simulationState;

    private SerializedProperty _script;
    private SerializedProperty _serializedPlayerState;
    private SerializedProperty _levelStatesByBuildIndex;
    private SerializedProperty _characterIdCount;
    private SerializedProperty _displayCharacterCounters;
    private SerializedProperty _levelEdgeIdCount;

    private ReorderableList _levelStateByBuildIndexList;
    private List<ReorderableList> _characterStateLists = new List<ReorderableList>();
    private ReorderableList _levelEdgeList;

    private const int _sectionSpacing = 15;

    private List<int> _buildIndexes = new List<int>();

    private Color _originalTextColor;
    private Color _originalBackgroundColor;

    private SimulationSettings _simulationSettings;

    private GUIStyle _invalidStyle = new GUIStyle();

    private Vector3 _characterCounterOffset = new Vector3(-0.6f, 2.0f, .0f);
    private GUIStyle _counterStyle = new GUIStyle();
    private GUIStyle _selectedCounterStyle = new GUIStyle();

    private string _duplicateAssetName = "SimulationState";

    private const float _reorderableListElementSpaceRatio = .14f;
    private const float _foldoutArrowOffset = 10.0f;
    private const float _foldoutArrowWidth = 12.0f;

    private const float _fullOffsetDistance = 20.0f;

    private void OnEnable()
    {
        _simulationState = (SimulationState)target;

        _script = serializedObject.FindProperty("m_Script");
        _serializedPlayerState = serializedObject.FindProperty("_playerState");
        _levelStatesByBuildIndex = serializedObject.FindProperty("_levelStatesByBuildIndex");
        _characterIdCount = serializedObject.FindProperty("CharacterIdCount");
        _displayCharacterCounters = serializedObject.FindProperty("DisplayCharacterCounters");
        _levelEdgeIdCount = serializedObject.FindProperty("LevelEdgeIdCount");

        _levelStateByBuildIndexList = new ReorderableList(serializedObject, _levelStatesByBuildIndex, false, true, true, true);
        CreateCharacterStates();
        _levelEdgeList = new ReorderableList(serializedObject, serializedObject.FindProperty("_levelEdges"), false, true, true, true);

        _invalidStyle.normal.textColor = Color.red;
        _invalidStyle.fontSize = 16;
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
            _characterStateLists.Add(new ReorderableList(serializedObject, _levelStatesByBuildIndex.GetArrayElementAtIndex(i).FindPropertyRelative("_characterStates"), false, true, true, true));
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
        HandleLevelStateByBuildIndexList(_levelStateByBuildIndexList);
        _levelStateByBuildIndexList.DoLayoutList();

        EditorGUILayout.PropertyField(_displayCharacterCounters);

        EditorGUILayout.Space(_sectionSpacing);

        // Add the list of connections
        HandleLevelEdgeList(_levelEdgeList);
        _levelEdgeList.DoLayoutList();

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }

        string[] simulationStateErrors;
        bool validSimulationState = _simulationState.IsValid(_simulationSettings.CharactersSettingsUsed, out simulationStateErrors);

        if (!validSimulationState)
        {
            GUI.enabled = false;
            foreach (string error in simulationStateErrors)
            {
                EditorGUILayout.TextArea("-" + error, _invalidStyle);
            }
            GUI.enabled = true;
        }

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
            GUI.enabled = validSimulationState && _duplicateAssetName != "" && !File.Exists(Application.dataPath + path.Remove(0, 6) + "/" + _duplicateAssetName + ".asset");

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

    private void HandleLevelStateByBuildIndexList(ReorderableList levelStateByBuildIndexList)
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

        levelStateByBuildIndexList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Level states by build index");
        };

        _buildIndexes.Clear();

        levelStateByBuildIndexList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            // Get the properties
            SerializedProperty foldedProperty = levelStateByBuildIndexList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Folded");
            SerializedProperty buildIndex = levelStateByBuildIndexList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_buildIndex");
            
            // Check if the build index is already used
            bool buildIndexAlreadySelected = _buildIndexes.Contains(buildIndex.intValue);

            if (!buildIndexAlreadySelected)
            {
                _buildIndexes.Add(buildIndex.intValue);
            }

            // Field for the build index
            bool validBuildIndex = !buildIndexAlreadySelected && (buildIndex.intValue >= 0 && buildIndex.intValue < activeBuildIndexCount);
            GUI.color = !validBuildIndex ? Color.red : _originalTextColor;
            GUI.backgroundColor = !validBuildIndex ? Color.red : _originalBackgroundColor;
            foldedProperty.boolValue = !EditorGUI.Foldout(new Rect(rect.x + _foldoutArrowOffset, rect.y + EditorGUIUtility.singleLineHeight * _reorderableListElementSpaceRatio, rect.width * .2f - _foldoutArrowOffset, EditorGUIUtility.singleLineHeight), !foldedProperty.boolValue, "Build Index");
            buildIndex.intValue = EditorGUI.Popup(new Rect(rect.x + _foldoutArrowWidth + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * _reorderableListElementSpaceRatio, rect.width * .8f - _foldoutArrowWidth, EditorGUIUtility.singleLineHeight), buildIndex.intValue, buildIndexOptions);
            GUI.color = Color.white;
            GUI.backgroundColor = _originalBackgroundColor;

            // Show all the fields
            if (!foldedProperty.boolValue)
            {
                SerializedProperty graph = levelStateByBuildIndexList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_graph");

                // Field for the Graph
                bool validGraph = graph.objectReferenceValue;
                GUI.color = !validGraph ? Color.red : _originalTextColor;
                GUI.backgroundColor = !validGraph ? Color.red : _originalBackgroundColor;
                EditorGUI.LabelField(new Rect(rect.x + _foldoutArrowWidth, rect.y + EditorGUIUtility.singleLineHeight * (1.0f + _reorderableListElementSpaceRatio), rect.width * .2f - _foldoutArrowOffset, EditorGUIUtility.singleLineHeight), new GUIContent("Graph"));
                EditorGUI.PropertyField(new Rect(rect.x + _foldoutArrowWidth + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * (1.0f + _reorderableListElementSpaceRatio), rect.width * .8f - _foldoutArrowWidth, EditorGUIUtility.singleLineHeight), graph, GUIContent.none);
                GUI.color = Color.white;
                GUI.backgroundColor = _originalBackgroundColor;

                // Show character states only if graph is valid
                if (!validGraph)
                {
                    return;
                }

                // Create the array of choices for the vertices
                string[] vertexOptions = new string[_simulationState.LevelStatesByBuildIndex[index].Graph.Vertices.Length];

                // Create the array of choices for the settings
                string[] settingsOptions = _simulationSettings.CharactersSettingsUsed.GetSettingsNames();

                for (int i = 0; i < _simulationState.LevelStatesByBuildIndex[index].Graph.Vertices.Length; i++)
                {
                    vertexOptions[i] = _simulationState.LevelStatesByBuildIndex[index].Graph.Vertices[i].Id.ToString();
                }

                // Add the list of Character States
                HandleCharacterStateList(_characterStateLists[index], vertexOptions, settingsOptions);
                _characterStateLists[index].DoList(new Rect(rect.x + _foldoutArrowWidth, rect.y + EditorGUIUtility.singleLineHeight * 2.5f, rect.width - _foldoutArrowWidth, rect.height));
            }
        };

        levelStateByBuildIndexList.elementHeightCallback = (int index) =>
        {
            // Space before the CharacterState list (without Graph field)
            float height = EditorGUIUtility.standardVerticalSpacing * 2.0f + EditorGUIUtility.singleLineHeight * 1.0f;

            // If LevelStateByBuildIndex is folded
            if (_simulationState.LevelStatesByBuildIndex[index].Folded)
            {
                return height;
            }

            // Graph field
            height += EditorGUIUtility.singleLineHeight;

            // If graph is invalid
            if (!_simulationState.LevelStatesByBuildIndex[index].Graph)
            {
                return height;
            }

            // Minimum space taken by the ReorderabelList
            height += EditorGUIUtility.singleLineHeight * 4.0f;

            if (_simulationState.LevelStatesByBuildIndex[index].CharacterStates.Length == 0)
            {
                // Space for empty ReorderabelList
                height += EditorGUIUtility.standardVerticalSpacing * 2.0f + EditorGUIUtility.singleLineHeight;
            }
            else
            {
                // Space based on how many characters there is and if they are folded/valid or not
                for (int i = 0; i < _characterStateLists[index].count; i++)
                {
                    height += EditorGUIUtility.standardVerticalSpacing * 2.0f + EditorGUIUtility.singleLineHeight * 3.11f;

                    bool folded = _simulationState.LevelStatesByBuildIndex[index].CharacterStates[i].Folded;
                    int settings = _simulationState.LevelStatesByBuildIndex[index].CharacterStates[i].Settings;

                    if (!folded && settings >= 0 && settings < _simulationSettings.CharactersSettingsUsed.Settings.Length)
                    {
                        height += EditorGUIUtility.singleLineHeight * 3.0f;
                    }
                }
            }
            
            return height;
        };

        levelStateByBuildIndexList.onAddCallback = list =>
        {
            // Add new levelStateByBuildIndex
            ReorderableList.defaultBehaviours.DoAddButton(list);

            // Clear the CharacterStates of the new LevelStatesByBuildIndex
            SerializedProperty newCharacterState = _levelStatesByBuildIndex.GetArrayElementAtIndex(_levelStatesByBuildIndex.arraySize - 1).FindPropertyRelative("_characterStates");
            newCharacterState.arraySize = 0;
            
            // Create a new ReorderableList and store it
            _characterStateLists.Add(new ReorderableList(serializedObject, newCharacterState, false, true, true, true));
        };

        levelStateByBuildIndexList.onRemoveCallback = list =>
        {
            // Remove selected levelStateByBuildIndex
            ReorderableList.defaultBehaviours.DoRemoveButton(list);

            // Recreate all CharacterState ReorderableLists (necessary to avoid out of bounds error)
            _characterStateLists.Clear();
            CreateCharacterStates();
        };
    }

    private void HandleCharacterStateList(ReorderableList characterStateList, string[] vertexOptions, string[] settingsOptions)
    {
        characterStateList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Character States");
        };

        characterStateList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            // Get the properties
            string id = characterStateList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_id").intValue.ToString();
            SerializedProperty vertex = characterStateList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("PositionOnGraph").FindPropertyRelative("VertexA");
            SerializedProperty folded = characterStateList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Folded");
            SerializedProperty settings = characterStateList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_settings");

            // Id
            EditorGUI.LabelField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * _reorderableListElementSpaceRatio, rect.width * .2f, EditorGUIUtility.singleLineHeight), new GUIContent("Id: " + id));

            // Dropdown for vertex
            bool validVertex = vertex.intValue >= 0 && vertex.intValue < vertexOptions.Length;
            GUI.color = !validVertex ? Color.red : _originalTextColor;
            GUI.backgroundColor = !validVertex ? Color.red : _originalBackgroundColor;
            EditorGUI.LabelField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * (1.1f + _reorderableListElementSpaceRatio), rect.width * .2f, EditorGUIUtility.singleLineHeight), new GUIContent("Vertex (id)"));
            vertex.intValue = EditorGUI.Popup(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * (1.0f + _reorderableListElementSpaceRatio), rect.width * .8f, EditorGUIUtility.singleLineHeight), vertex.intValue, vertexOptions);
            GUI.color = Color.white;
            GUI.backgroundColor = _originalBackgroundColor;

            // Dropdown for settings
            bool validSettings = settings.intValue >= 0 && settings.intValue < settingsOptions.Length;
            GUI.color = !validSettings ? Color.red : _originalTextColor;
            GUI.backgroundColor = !validSettings ? Color.red : _originalBackgroundColor;
            folded.boolValue = !EditorGUI.Foldout(new Rect(rect.x + _foldoutArrowOffset, rect.y + EditorGUIUtility.singleLineHeight * (2.1f + _reorderableListElementSpaceRatio), rect.width * .2f - _foldoutArrowOffset, EditorGUIUtility.singleLineHeight), !folded.boolValue, "Settings");
            settings.intValue = EditorGUI.Popup(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * (2.0f + _reorderableListElementSpaceRatio), rect.width * .8f, EditorGUIUtility.singleLineHeight), settings.intValue, settingsOptions);
            GUI.color = Color.white;
            GUI.backgroundColor = _originalBackgroundColor;

            // Show settings details
            if (!folded.boolValue && settings.intValue >= 0 && settings.intValue < _simulationSettings.CharactersSettingsUsed.Settings.Length)
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

        characterStateList.elementHeightCallback = (int index) =>
        {
            bool folded = characterStateList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Folded").boolValue;
            int settings = characterStateList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_settings").intValue;

            float height = EditorGUIUtility.standardVerticalSpacing * 2.0f + EditorGUIUtility.singleLineHeight * 3.0f;

            if (!folded && settings >= 0 && settings < _simulationSettings.CharactersSettingsUsed.Settings.Length)
            {
                height += EditorGUIUtility.singleLineHeight * 3.0f;
            }

            return height;
        };

        characterStateList.onAddCallback = list =>
        {
            // Add new levelStateByBuildIndex
            ReorderableList.defaultBehaviours.DoAddButton(list);

            list.serializedProperty.GetArrayElementAtIndex(list.count - 1).FindPropertyRelative("_id").intValue = _characterIdCount.intValue;
            _characterIdCount.intValue++;
        };
    }

    private void HandleLevelEdgeList(ReorderableList levelEdgeList)
    {
        // Create the array of choices for the levels
        string[] levelOptions = new string[_simulationState.LevelStatesByBuildIndex.Length];

        for (int i = 0; i < levelOptions.Length; i++)
        {
            levelOptions[i] = "Build index " + _simulationState.LevelStatesByBuildIndex[i].BuildIndex.ToString() + " -> " + (_simulationState.LevelStatesByBuildIndex[i].Graph ? _simulationState.LevelStatesByBuildIndex[i].Graph.name : "no graph!");
        }
        
        levelEdgeList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Level edges");
        };

        levelEdgeList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            // Get the properties
            SerializedProperty folded = levelEdgeList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Folded");
            SerializedProperty levelA = levelEdgeList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_levelA");
            SerializedProperty levelB = levelEdgeList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_levelB");

            folded.boolValue = !EditorGUI.Foldout(new Rect(rect.x + _foldoutArrowOffset, rect.y + EditorGUIUtility.singleLineHeight * _reorderableListElementSpaceRatio, rect.width * .1f - _foldoutArrowOffset, EditorGUIUtility.singleLineHeight), !folded.boolValue, "");

            // Field for the level A
            bool validLevelA = levelA.intValue >= 0 && levelA.intValue < _simulationState.LevelStatesByBuildIndex.Length;
            GUI.color = !validLevelA ? Color.red : _originalTextColor;
            GUI.backgroundColor = !validLevelA ? Color.red : _originalBackgroundColor;
            EditorGUI.LabelField(new Rect(rect.x + _foldoutArrowWidth, rect.y + EditorGUIUtility.singleLineHeight * _reorderableListElementSpaceRatio, (rect.width - _foldoutArrowWidth) * .16f, EditorGUIUtility.singleLineHeight),"Level A");
            levelA.intValue = EditorGUI.Popup(new Rect(rect.x + _foldoutArrowWidth + (rect.width - _foldoutArrowWidth) * .16f, rect.y + EditorGUIUtility.singleLineHeight * _reorderableListElementSpaceRatio, (rect.width - _foldoutArrowWidth) * .32f, EditorGUIUtility.singleLineHeight), levelA.intValue, levelOptions);
            GUI.color = Color.white;
            GUI.backgroundColor = _originalBackgroundColor;

            // Field for the level B
            bool validLevelB = levelB.intValue != levelA.intValue && levelB.intValue >= 0 && levelB.intValue < _simulationState.LevelStatesByBuildIndex.Length;
            GUI.color = !validLevelB ? Color.red : _originalTextColor;
            GUI.backgroundColor = !validLevelB ? Color.red : _originalBackgroundColor;
            EditorGUI.LabelField(new Rect(rect.x + _foldoutArrowWidth + (rect.width - _foldoutArrowWidth) * .52f, rect.y + EditorGUIUtility.singleLineHeight * _reorderableListElementSpaceRatio, (rect.width - _foldoutArrowWidth) * .16f, EditorGUIUtility.singleLineHeight), "Level B");
            levelB.intValue = EditorGUI.Popup(new Rect(rect.x + _foldoutArrowWidth + (rect.width - _foldoutArrowWidth) * .68f, rect.y + EditorGUIUtility.singleLineHeight * _reorderableListElementSpaceRatio, (rect.width - _foldoutArrowWidth) * .32f, EditorGUIUtility.singleLineHeight), levelB.intValue, levelOptions);
            GUI.color = Color.white;
            GUI.backgroundColor = _originalBackgroundColor;
            
            if (!folded.boolValue && validLevelA && validLevelB)
            {
                Graph graphA = _simulationState.LevelStatesByBuildIndex[levelA.intValue].Graph;

                if (graphA)
                {
                    SerializedProperty vertexA = levelEdgeList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_edge").FindPropertyRelative("VertexA");

                    // Create the array of choices for the vertices
                    string[] vertexOptionsA = new string[graphA.Vertices.Length];

                    for (int i = 0; i < vertexOptionsA.Length; i++)
                    {
                        vertexOptionsA[i] = "Id " + graphA.Vertices[i].Id.ToString();
                    }

                    // Field for the vertex A
                    bool validVertexA = vertexA.intValue >= 0 && vertexA.intValue < graphA.Vertices.Length;
                    GUI.color = !validVertexA ? Color.red : _originalTextColor;
                    GUI.backgroundColor = !validVertexA ? Color.red : _originalBackgroundColor;
                    EditorGUI.LabelField(new Rect(rect.x + _foldoutArrowWidth, rect.y + EditorGUIUtility.singleLineHeight * (1.0f + _reorderableListElementSpaceRatio), (rect.width - _foldoutArrowWidth) * .16f, EditorGUIUtility.singleLineHeight), "Vertex A");
                    vertexA.intValue = EditorGUI.Popup(new Rect(rect.x + _foldoutArrowWidth + (rect.width - _foldoutArrowWidth) * .16f, rect.y + EditorGUIUtility.singleLineHeight * (1.0f + _reorderableListElementSpaceRatio), (rect.width - _foldoutArrowWidth) * .32f, EditorGUIUtility.singleLineHeight), vertexA.intValue, vertexOptionsA);
                    GUI.color = Color.white;
                    GUI.backgroundColor = _originalBackgroundColor;
                }
                else
                {
                    GUI.color = Color.red;
                    GUI.backgroundColor = Color.red;
                    EditorGUI.LabelField(new Rect(rect.x + _foldoutArrowWidth, rect.y + EditorGUIUtility.singleLineHeight * (1.0f + _reorderableListElementSpaceRatio), (rect.width - _foldoutArrowWidth) * .48f, EditorGUIUtility.singleLineHeight), "Level A doesn't specify a graph");
                    GUI.color = Color.white;
                    GUI.backgroundColor = _originalBackgroundColor;
                }

                Graph graphB = _simulationState.LevelStatesByBuildIndex[levelB.intValue].Graph;

                if (graphB)
                {
                    SerializedProperty vertexB = levelEdgeList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_edge").FindPropertyRelative("VertexB");

                    // Create the array of choices for the vertices
                    string[] vertexOptionsB = new string[graphB.Vertices.Length];

                    for (int i = 0; i < vertexOptionsB.Length; i++)
                    {
                        vertexOptionsB[i] = "Id " + graphB.Vertices[i].Id.ToString();
                    }

                    // Field for the vertex B
                    bool validVertexB = vertexB.intValue >= 0 && vertexB.intValue < graphB.Vertices.Length;
                    GUI.color = !validVertexB ? Color.red : _originalTextColor;
                    GUI.backgroundColor = !validVertexB ? Color.red : _originalBackgroundColor;
                    EditorGUI.LabelField(new Rect(rect.x + _foldoutArrowWidth + (rect.width - _foldoutArrowWidth) * .52f, rect.y + EditorGUIUtility.singleLineHeight * (1.0f + _reorderableListElementSpaceRatio), (rect.width - _foldoutArrowWidth) * .16f, EditorGUIUtility.singleLineHeight), "Vertex A");
                    vertexB.intValue = EditorGUI.Popup(new Rect(rect.x + _foldoutArrowWidth + (rect.width - _foldoutArrowWidth) * .68f, rect.y + EditorGUIUtility.singleLineHeight * (1.0f + _reorderableListElementSpaceRatio), (rect.width - _foldoutArrowWidth) * .32f, EditorGUIUtility.singleLineHeight), vertexB.intValue, vertexOptionsB);
                    GUI.color = Color.white;
                    GUI.backgroundColor = _originalBackgroundColor;
                }
                else
                {
                    GUI.color = Color.red;
                    GUI.backgroundColor = Color.red;
                    EditorGUI.LabelField(new Rect(rect.x + _foldoutArrowWidth + (rect.width - _foldoutArrowWidth) * .52f, rect.y + EditorGUIUtility.singleLineHeight * (1.0f + _reorderableListElementSpaceRatio), (rect.width - _foldoutArrowWidth) * .48f, EditorGUIUtility.singleLineHeight), "Level A doesn't specify a graph");
                    GUI.color = Color.white;
                    GUI.backgroundColor = _originalBackgroundColor;
                }

                SerializedProperty direction = levelEdgeList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_edge").FindPropertyRelative("Direction");
                SerializedProperty traversable = levelEdgeList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_edge").FindPropertyRelative("Traversable");

                EditorGUI.LabelField(new Rect(rect.x + _foldoutArrowWidth, rect.y + EditorGUIUtility.singleLineHeight * (2.0f + _reorderableListElementSpaceRatio), (rect.width - _foldoutArrowWidth) * .16f - _foldoutArrowWidth, EditorGUIUtility.singleLineHeight), new GUIContent("Direction"));
                EditorGUI.PropertyField(new Rect(rect.x + _foldoutArrowWidth + (rect.width - _foldoutArrowWidth) * .16f, rect.y + EditorGUIUtility.singleLineHeight * (2.0f + _reorderableListElementSpaceRatio), (rect.width - _foldoutArrowWidth) * .84f, EditorGUIUtility.singleLineHeight), direction, GUIContent.none);
                EditorGUI.LabelField(new Rect(rect.x + _foldoutArrowWidth, rect.y + EditorGUIUtility.singleLineHeight * (3.0f + _reorderableListElementSpaceRatio), (rect.width - _foldoutArrowWidth) * .16f - _foldoutArrowWidth, EditorGUIUtility.singleLineHeight), new GUIContent("Traversable"));
                EditorGUI.PropertyField(new Rect(rect.x + _foldoutArrowWidth + (rect.width - _foldoutArrowWidth) * .16f, rect.y + EditorGUIUtility.singleLineHeight * (3.0f + _reorderableListElementSpaceRatio), (rect.width - _foldoutArrowWidth) * .84f, EditorGUIUtility.singleLineHeight), traversable, GUIContent.none);
            }
        };

        levelEdgeList.elementHeightCallback = (int index) =>
        {
            float height = EditorGUIUtility.standardVerticalSpacing * 2.0f + EditorGUIUtility.singleLineHeight;

            bool validLevelA = _simulationState.LevelEdges[index].LevelA >= 0 && _simulationState.LevelEdges[index].LevelA < _simulationState.LevelStatesByBuildIndex.Length;
            bool validLevelB = _simulationState.LevelEdges[index].LevelB >= 0 && _simulationState.LevelEdges[index].LevelB < _simulationState.LevelStatesByBuildIndex.Length;

            if (!validLevelA || !validLevelB || _simulationState.LevelEdges[index].LevelA == _simulationState.LevelEdges[index].LevelB)
            {
                return height;
            }

            if (!_simulationState.LevelEdges[index].Folded)
            {
                height += EditorGUIUtility.singleLineHeight * 3.0f;
            }

            return height;
        };

        levelEdgeList.onAddCallback = list =>
        {
            // Add new levelStateByBuildIndex
            ReorderableList.defaultBehaviours.DoAddButton(list);

            list.serializedProperty.GetArrayElementAtIndex(list.count - 1).FindPropertyRelative("_edge").FindPropertyRelative("_id").intValue = _levelEdgeIdCount.intValue;
            list.serializedProperty.GetArrayElementAtIndex(list.count - 1).FindPropertyRelative("_edge").FindPropertyRelative("Traversable").boolValue = true;
            
            _levelEdgeIdCount.intValue++;
        };
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        bool characterSettingsValid = _simulationSettings && _simulationSettings.CharactersSettingsUsed && _simulationSettings.CharactersSettingsUsed.Settings.Length > 0;

        if (!_simulationState.DisplayCharacterCounters || !characterSettingsValid || _levelStateByBuildIndexList.index < 0)
        {
            // Don't display character counters, CharactersSettings used is invalid or no LevelStateByBuildIndex is selected
            return;
        }
        
        Graph graph = _simulationState.LevelStatesByBuildIndex[_levelStateByBuildIndexList.index].Graph;

        if (graph == null)
        {
            // No graph selected in the LevelStateByBuildIndex
            return;
        }

        // Count how many characters at each vertex and which character is selected
        int selectedCharacterVertex = -1;
        Dictionary<int, int> characterCountByVertex = new Dictionary<int, int>();
        
        CharacterState[] characterStates = _simulationState.LevelStatesByBuildIndex[_levelStateByBuildIndexList.index].CharacterStates;

        for (int i = 0; i < characterStates.Length; i++)
        {
            if (characterStates[i].PositionOnGraph.VertexA < 0 && characterStates[i].PositionOnGraph.VertexA >= graph.Vertices.Length)
            {
                continue;
            }

            if (_characterStateLists[_levelStateByBuildIndexList.index].index == i)
            {
                selectedCharacterVertex = characterStates[i].PositionOnGraph.VertexA;
            }

            if (!characterCountByVertex.ContainsKey(characterStates[i].PositionOnGraph.VertexA))
            {
                characterCountByVertex.Add(characterStates[i].PositionOnGraph.VertexA, 1);
            }
            else
            {
                characterCountByVertex[characterStates[i].PositionOnGraph.VertexA] += 1;
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
