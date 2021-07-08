using System;
using BehaviorDesigner.Runtime;
using UnityEditor;
using UnityEngine;

public class SimulationSettingsEditorWindow : EditorWindow
{
    SimulationSettings _simulationSettings;
    private SerializedObject _serializedSimulationSettings;

    private bool _simplifiedAITickRateFixed = false;

    private GUIStyle _createButtonStyle;
    private const int _createButtonStyleFontSize = 30;

    private Color _originalTextColor;
    private Color _originalBackgroundColor;

    private GUIStyle _invalidStyle = new GUIStyle();

    // Dimensions
    private const float _editorMinWidth = 300;
    private const float _editorMinHeight = 300;

    private void OnEnable()
    {
        _invalidStyle.normal.textColor = Color.red;
        _invalidStyle.fontSize = 20;
        _invalidStyle.wordWrap = true;
        _invalidStyle.fontStyle = FontStyle.Bold;

        _originalTextColor = GUI.color;
        _originalBackgroundColor = GUI.backgroundColor;
    }

    // Add menu item to the main menu and inspector context menus and the static function becomes a menu command
    [MenuItem("AI Simulation/Simulation Settings")]
    public static void ShowEditor()
    {
        Type inspectorType = Type.GetType("UnityEditor.InspectorWindow,UnityEditor.dll");

        // Returns the first EditorWindow of type t which is currently on the screen.
        // If there is none, creates and shows new window and returns the instance of it.
        // Will attempt to dock next to inspector window
        SimulationSettingsEditorWindow editor = GetWindow<SimulationSettingsEditorWindow>("Simulation Settings", inspectorType);
        editor.minSize = new Vector2(_editorMinWidth, _editorMinHeight);
    }

    private void OnGUI()
    {
        // Setup style
        _createButtonStyle = new GUIStyle("button");
        _createButtonStyle.fontSize = _createButtonStyleFontSize;

        DrawEditor();
    }

    private void DrawEditor()
    {
        EditorGUILayout.Space(15.0f);

        // Error if there is not simulation settings
        if (!_simulationSettings)
        {
            _simulationSettings = SimulationSettings.LoadFromAsset();

            if (!_simulationSettings)
            {
                if (GUILayout.Button("Create Simulation Settings", _createButtonStyle, GUILayout.Height(100)))
                {
                    _simulationSettings = SimulationSettings.CreateAsset();
                    _serializedSimulationSettings = null;
                }

                return;
            }
        }

        if (_serializedSimulationSettings == null)
        {
            _serializedSimulationSettings = new SerializedObject(_simulationSettings);
        }

        EditorGUI.BeginChangeCheck();

        bool validSettings = DrawSettings();

        if (EditorGUI.EndChangeCheck() || _simplifiedAITickRateFixed)
        {
            _serializedSimulationSettings.ApplyModifiedProperties();
            _simplifiedAITickRateFixed = false;
        }

        // Error message
        if (!validSettings)
        {
            EditorGUILayout.Space(15.0f);

            GUI.enabled = false;
            EditorGUILayout.TextArea("SimulationSettings are invalid (null fields, CharactersSettings not valid, InitialGameState not valid or SimplifiedAIPrefab doesn't have a BehaviorTree component or SImplifiedMovement component)", _invalidStyle);
            GUI.enabled = true;
        }
    }

    // Return if any field was invalid
    private bool DrawSettings()
    {
        SerializedProperty charactersSettingsUsed = _serializedSimulationSettings.FindProperty("_charactersSettingsUsed");
        SerializedProperty initialGameState = _serializedSimulationSettings.FindProperty("_initialGameState");
        SerializedProperty simplifiedAIPrefab = _serializedSimulationSettings.FindProperty("_simplifiedAIPrefab");
        SerializedProperty simplifiedAITickRate = _serializedSimulationSettings.FindProperty("_simplifiedAITickRate");

        EditorGUILayout.LabelField("Settings");
        EditorGUILayout.Space(15.0f);

        // CharactersSettings
        CharactersSettings charactersSettings = (CharactersSettings)charactersSettingsUsed.objectReferenceValue;
        bool validCharactersSettingsUsed = charactersSettingsUsed.objectReferenceValue && charactersSettings.IsValid();
        GUI.color = !validCharactersSettingsUsed ? Color.red : _originalTextColor;
        GUI.backgroundColor = !charactersSettingsUsed.objectReferenceValue ? Color.red : _originalBackgroundColor;
        EditorGUILayout.PropertyField(charactersSettingsUsed);
        GUI.color = Color.white;
        GUI.backgroundColor = _originalBackgroundColor;

        // InitialGameState
        GameState gamestate = (GameState)initialGameState.objectReferenceValue;
        bool validInitialGameState = initialGameState.objectReferenceValue && (!charactersSettings || gamestate.IsValid(charactersSettings));
        GUI.color = !validInitialGameState ? Color.red : _originalTextColor;
        GUI.backgroundColor = !initialGameState.objectReferenceValue ? Color.red : _originalBackgroundColor;
        EditorGUILayout.PropertyField(initialGameState);
        GUI.color = Color.white;
        GUI.backgroundColor = _originalBackgroundColor;

        // SimplifiedAIPrefab
        bool validSimplifiedAIPrefab = simplifiedAIPrefab.objectReferenceValue && ((GameObject)simplifiedAIPrefab.objectReferenceValue).GetComponent<BehaviorTree>() && ((GameObject)simplifiedAIPrefab.objectReferenceValue).GetComponent<SimplifiedCharacterMovement>();
        GUI.color = !validSimplifiedAIPrefab ? Color.red : _originalTextColor;
        GUI.backgroundColor = !validSimplifiedAIPrefab ? Color.red : _originalBackgroundColor;
        EditorGUILayout.PropertyField(simplifiedAIPrefab);
        GUI.color = Color.white;
        GUI.backgroundColor = _originalBackgroundColor;

        // SimplifiedAITickRate can't be smaller then 0
        if (simplifiedAITickRate.floatValue < 0)
        {
            simplifiedAITickRate.floatValue = 0;
            _simplifiedAITickRateFixed = true;
        }

        // SimplifiedAITickRate
        EditorGUILayout.PropertyField(simplifiedAITickRate);

        return validCharactersSettingsUsed && validInitialGameState && validSimplifiedAIPrefab;
    }
}
