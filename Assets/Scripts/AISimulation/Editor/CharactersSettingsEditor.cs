using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

// This is a custom editor for the GameState class
[CustomEditor(typeof(CharactersSettings))]

public class CharactersSettingsEditor : Editor
{
    private CharactersSettings _charactersSettings;

    private SerializedProperty _script;
    private ReorderableList _settings;

    private const int _sectionSpacing = 15;

    private List<string> _settingsNames = new List<string>();

    private Color _originalTextColor;
    private Color _originalBackgroundColor;

    private GUIStyle _invalidStyle = new GUIStyle();

    // Dimensions
    private const float _foldoutArrowWidth = 12.0f;
    private const float _reorderableListElementSpaceRatio = .14f;

    private void OnEnable()
    {
        _charactersSettings = (CharactersSettings)target;

        _script = serializedObject.FindProperty("m_Script");

        _settings = new ReorderableList(serializedObject, serializedObject.FindProperty("Settings"), false, true, true, true);
        
        _invalidStyle.normal.textColor = Color.red;
        _invalidStyle.fontSize = 16;
        _invalidStyle.wordWrap = true;
        _invalidStyle.fontStyle = FontStyle.Bold;

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

        _settingsNames.Clear();

        // Add the list of LevelState by build indexes
        HandleSettings(_settings);
        _settings.DoLayoutList();

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }

        string[] gameStateErrors;

        if (!_charactersSettings.IsValid(out gameStateErrors))
        {
            GUI.enabled = false;
            foreach (string error in gameStateErrors)
            {
                EditorGUILayout.TextArea("-" + error, _invalidStyle);
            }
            GUI.enabled = true;
        }
    }

    private void HandleSettings(ReorderableList settings)
    {
        settings.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Settings");
        };

        settings.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            // Get the properties
            SerializedProperty folded = serializedObject.FindProperty("SettingsFolded").GetArrayElementAtIndex(index);
            SerializedProperty name = settings.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_name");
            SerializedProperty prefabPro = settings.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_prefab");
            SerializedProperty maxWalkSpeed = settings.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_maxWalkSpeed");
            SerializedProperty prefabBehavior = settings.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_prefabBehavior");
            SerializedProperty simplifiedBehavior = settings.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_simplifiedBehavior");

            // Max walk speed can't be smaller then 0
            if (maxWalkSpeed.floatValue < 0)
            {
                maxWalkSpeed.floatValue = 0;
            }

            // Check if the settings name is already used
            bool settingsNameAlreadyUsed = _settingsNames.Contains(name.stringValue);

            if (!settingsNameAlreadyUsed)
            {
                _settingsNames.Add(name.stringValue);
            }

            // Field for fold and name
            folded.boolValue = !EditorGUI.Foldout(new Rect(rect.x + _foldoutArrowWidth, rect.y + EditorGUIUtility.singleLineHeight * .1f, rect.width * .2f, EditorGUIUtility.singleLineHeight), !folded.boolValue, "Name");
            GUI.color = name.stringValue == "" || settingsNameAlreadyUsed ? Color.red : _originalTextColor;
            GUI.backgroundColor = name.stringValue == "" ? Color.red : _originalBackgroundColor;
            EditorGUI.PropertyField(new Rect(rect.x + rect.width * .25f, rect.y + EditorGUIUtility.singleLineHeight * _reorderableListElementSpaceRatio, rect.width * (1.0f - .25f), EditorGUIUtility.singleLineHeight), name, GUIContent.none);
            GUI.color = Color.white;
            GUI.backgroundColor = _originalBackgroundColor;

            if (!_charactersSettings.SettingsFolded[index])
            {
                // Field for the prefab
                GameObject prefab = (GameObject)prefabPro.objectReferenceValue;
                bool valid = prefabPro.objectReferenceValue && prefab.GetComponent<BehaviorTree>() && prefab.GetComponent<CharacterMovement>();
                GUI.color = !valid ? Color.red : _originalTextColor;
                GUI.backgroundColor = !valid ? Color.red : _originalBackgroundColor;
                EditorGUI.LabelField(new Rect(rect.x + _foldoutArrowWidth, rect.y + EditorGUIUtility.singleLineHeight * (1.0f + _reorderableListElementSpaceRatio), rect.width * .25f - _foldoutArrowWidth, EditorGUIUtility.singleLineHeight), new GUIContent("Prefab"));
                EditorGUI.PropertyField(new Rect(rect.x + rect.width * .25f, rect.y + EditorGUIUtility.singleLineHeight * (1.0f + _reorderableListElementSpaceRatio), rect.width * (1.0f - .25f), EditorGUIUtility.singleLineHeight), prefabPro, GUIContent.none);
                GUI.color = Color.white;
                GUI.backgroundColor = _originalBackgroundColor;

                // Field for the max walk speed
                EditorGUI.LabelField(new Rect(rect.x + _foldoutArrowWidth, rect.y + EditorGUIUtility.singleLineHeight * (2.0f + _reorderableListElementSpaceRatio), rect.width * .25f - _foldoutArrowWidth, EditorGUIUtility.singleLineHeight), new GUIContent("Max Walk Speed"));
                EditorGUI.PropertyField(new Rect(rect.x + rect.width * .25f, rect.y + EditorGUIUtility.singleLineHeight * (2.0f + _reorderableListElementSpaceRatio), rect.width * (1.0f - .25f), EditorGUIUtility.singleLineHeight), maxWalkSpeed, GUIContent.none);

                // Field for the prefab behavior
                GUI.color = !prefabBehavior.objectReferenceValue ? Color.red : _originalTextColor;
                GUI.backgroundColor = !prefabBehavior.objectReferenceValue ? Color.red : _originalBackgroundColor;
                EditorGUI.LabelField(new Rect(rect.x + _foldoutArrowWidth, rect.y + EditorGUIUtility.singleLineHeight * (3.0f + _reorderableListElementSpaceRatio), rect.width * .25f - _foldoutArrowWidth, EditorGUIUtility.singleLineHeight), new GUIContent("Prefab Behavior"));
                EditorGUI.PropertyField(new Rect(rect.x + rect.width * .25f, rect.y + EditorGUIUtility.singleLineHeight * (3.0f + _reorderableListElementSpaceRatio), rect.width * (1.0f - .25f), EditorGUIUtility.singleLineHeight), prefabBehavior, GUIContent.none);
                GUI.color = Color.white;
                GUI.backgroundColor = _originalBackgroundColor;

                // Field for the simplified behavior
                GUI.color = !simplifiedBehavior.objectReferenceValue ? Color.red : _originalTextColor;
                GUI.backgroundColor = !simplifiedBehavior.objectReferenceValue ? Color.red : _originalBackgroundColor;
                EditorGUI.LabelField(new Rect(rect.x + _foldoutArrowWidth, rect.y + EditorGUIUtility.singleLineHeight * (4.0f + _reorderableListElementSpaceRatio), rect.width * .25f - _foldoutArrowWidth, EditorGUIUtility.singleLineHeight), new GUIContent("Simplified Behavior"));
                EditorGUI.PropertyField(new Rect(rect.x + rect.width * .25f, rect.y + EditorGUIUtility.singleLineHeight * (4.0f + _reorderableListElementSpaceRatio), rect.width * (1.0f - .25f), EditorGUIUtility.singleLineHeight), simplifiedBehavior, GUIContent.none);
                GUI.color = Color.white;
                GUI.backgroundColor = _originalBackgroundColor;
            }
        };

        settings.onAddCallback = (ReorderableList list) =>
        {
            serializedObject.FindProperty("SettingsFolded").arraySize++;

            // Add one element
            ReorderableList.defaultBehaviours.DoAddButton(list);
        };

        settings.onRemoveCallback = (ReorderableList list) =>
        {
            serializedObject.FindProperty("SettingsFolded").DeleteArrayElementAtIndex(list.index);

            // Remove element
            ReorderableList.defaultBehaviours.DoRemoveButton(list);
        };

        settings.elementHeightCallback = (int index) =>
        {
            float height = EditorGUIUtility.standardVerticalSpacing;

            if (!_charactersSettings.SettingsFolded[index])
            {
                height += EditorGUIUtility.singleLineHeight * 5.0f;
            }
            else
            {
                height += EditorGUIUtility.singleLineHeight;
            }

            return height + EditorGUIUtility.standardVerticalSpacing;
        };
    }
}
