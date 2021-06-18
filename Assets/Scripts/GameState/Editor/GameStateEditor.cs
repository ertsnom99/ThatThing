using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

// This is a custom editor for the GameState class
[CustomEditor(typeof(GameState))]
public class GameStateEditor : Editor
{
    private GameState _gameState;

    private SerializedProperty _script;
    private ReorderableList _levelStateByBuildIndexes;
    private SerializedProperty _serializedPlayerState;

    private const int _sectionSpacing = 15;

    private List<int> _buildIndexes = new List<int>();

    private Color _originalTextColor;
    private Color _originalBackgroundColor;

    private GUIStyle _invalidStyle = new GUIStyle();

    private string _duplicateAssetName = "GameState";

    private void OnEnable()
    {
        _gameState = (GameState)target;

        _script = serializedObject.FindProperty("m_Script");
        _levelStateByBuildIndexes = new ReorderableList(serializedObject, serializedObject.FindProperty("_levelStatesByBuildIndex"), false, true, true, true);
        _serializedPlayerState = serializedObject.FindProperty("_playerState");

        _invalidStyle.normal.textColor = Color.red;
        _invalidStyle.fontSize = 16;

        _originalTextColor = GUI.color;
        _originalBackgroundColor = GUI.backgroundColor;
    }
    
    public override void OnInspectorGUI()
    {
        // Add the default Script field
        GUI.enabled = false;
        EditorGUILayout.PropertyField(_script);
        GUI.enabled = true;

        EditorGUILayout.Space(_sectionSpacing);

        EditorGUI.BeginChangeCheck();

        // Field for the PlayerState
        bool playerStateSelected = _serializedPlayerState.objectReferenceValue;
        GUI.color = !playerStateSelected ? Color.red : _originalTextColor;
        GUI.backgroundColor = !playerStateSelected ? Color.red : _originalBackgroundColor;
        EditorGUILayout.PropertyField(_serializedPlayerState);
        GUI.color = Color.white;
        GUI.backgroundColor = _originalBackgroundColor;

        EditorGUILayout.Space(_sectionSpacing);

        // Initialize variables needed to detect errors
        _buildIndexes.Clear();

        // Add the list of LevelState by build indexes
        HandleLevelStateByBuildIndexes(_levelStateByBuildIndexes);
        _levelStateByBuildIndexes.DoLayoutList();

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }

        if (!_gameState.IsValid())
        {
            GUI.enabled = false;
            EditorGUILayout.TextArea("GameState is invalid (build index duplicates or null LevelStates)", _invalidStyle);
            GUI.enabled = true;
        }

        EditorGUILayout.Space(_sectionSpacing);

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

    private void HandleLevelStateByBuildIndexes(ReorderableList LevelStateByBuildIndexes)
    {
        LevelStateByBuildIndexes.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "LevelSTate by build index");
        };

        LevelStateByBuildIndexes.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            // Get the properties
            SerializedProperty buildIndex = LevelStateByBuildIndexes.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_buildIndex");
            SerializedProperty levelState = LevelStateByBuildIndexes.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_levelState");
            
            // Build index can't be smaller then 0
            if (buildIndex.intValue < 0)
            {
                buildIndex.intValue = 0;
            }

            // Check if the build index is already used
            bool buildIndexAlreadySelected = _buildIndexes.Contains(buildIndex.intValue);

            if (!buildIndexAlreadySelected)
            {
                _buildIndexes.Add(buildIndex.intValue);
            }

            // Check if the LevelState is null
            bool levelStateSelected = levelState.objectReferenceValue;

            // Field for the build index
            GUI.color = buildIndexAlreadySelected ? Color.red : _originalTextColor;
            GUI.backgroundColor = buildIndexAlreadySelected ? Color.red : _originalBackgroundColor;
            EditorGUI.LabelField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * 0.14f, rect.width * .2f, EditorGUIUtility.singleLineHeight), new GUIContent("Build Index"));
            EditorGUI.PropertyField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * 0.14f, rect.width * .8f, EditorGUIUtility.singleLineHeight), buildIndex, GUIContent.none);
            GUI.color = Color.white;
            GUI.backgroundColor = _originalBackgroundColor;

            // Field for the LevelState
            GUI.color = !levelStateSelected ? Color.red : _originalTextColor;
            GUI.backgroundColor = !levelStateSelected ? Color.red : _originalBackgroundColor;
            EditorGUI.LabelField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * 1.14f, rect.width * .2f, EditorGUIUtility.singleLineHeight), new GUIContent("State"));
            EditorGUI.PropertyField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * 1.14f, rect.width * .8f, EditorGUIUtility.singleLineHeight), levelState, GUIContent.none);
            GUI.color = Color.white;
            GUI.backgroundColor = _originalBackgroundColor;
        };

        LevelStateByBuildIndexes.elementHeightCallback = (int index) =>
        {
            float height = EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUIUtility.singleLineHeight * 2.0f;
            return height + EditorGUIUtility.standardVerticalSpacing;
        };
    }
}
