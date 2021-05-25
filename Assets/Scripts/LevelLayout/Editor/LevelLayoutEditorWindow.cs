using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class LevelLayoutEditorWindow : EditorWindow
{
    #region Variables
    //private Vector3 _mousePosition;

    private LevelLayout _previousLevelLayout;
    public static LevelLayoutEditorSettings EditorSettings;

    private SerializedObject _serializedLevelLayout;
    
    ReorderableList _rooms;

    //private GUIStyle _windowStyle;

    //private readonly Rect _backgroundArea = new Rect(0, 0, 500, 500);

    private Transform _selectedTransform = null;
    private int _selectedRoom = -1;

    private Color _debugRoomColor = new Color(.0f, .0f, 1.0f, .5f);
    private Color _debugSelectedRoomColor = new Color(1.0f, .0f, .0f, .5f);

    // Dimensions
    private const float _editorMinWidth = 300;
    private const float _editorMinHeight = 300;
    #endregion

    #region Init
    // Add menu item to the main menu and inspector context menus and the static function becomes a menu command
    [MenuItem("Level Layout/Editor")]
    public static void ShowEditor()
    {
        Type inspectorType = Type.GetType("UnityEditor.InspectorWindow,UnityEditor.dll");

        // Returns the first EditorWindow of type t which is currently on the screen.
        // If there is none, creates and shows new window and returns the instance of it.
        // Will attempt to dock next to inspector window
        LevelLayoutEditorWindow editor = GetWindow<LevelLayoutEditorWindow>("Level Layout", inspectorType);
        editor.minSize = new Vector2(_editorMinWidth, _editorMinHeight);
    }

    // Called when a new window is created
    private void OnEnable()
    {
        EditorSettings = EditorGUIUtility.Load("Level Layout/LevelLayoutEditorSettings.asset") as LevelLayoutEditorSettings;

        if (EditorSettings != null)
        {
            //_windowStyle = EditorSettings.Skin.GetStyle("window");
        }
        else
        {
            Debug.LogError("Couldn't find LevelLayoutEditorSettings.asset file in Assets\\Editor Default Resources\\Level Layout");
        }
    }

    private void OnDestroy()
    {

    }
    #endregion

    #region GUI Methods
    private void OnGUI()
    {
        // The current event that's being processed right now
        /*Event e = Event.current;
        _mousePosition = e.mousePosition;

        HandleUserInput(e);*/

        DrawEditor();

        // Save the editor settings
        EditorUtility.SetDirty(EditorSettings);
    }

    private void HandleUserInput(Event e)
    {

    }

    private void DrawEditor()
    {
        // Begin to draw the background area
        //GUILayout.BeginArea(_backgroundArea, _windowStyle);

        // Field for the LevelLayout
        EditorGUILayout.Space(15);
        EditorSettings.CurrentLevelLayout = (LevelLayout)EditorGUILayout.ObjectField("Data:", EditorSettings.CurrentLevelLayout, typeof(LevelLayout), false);
        EditorGUILayout.Space(15);

        // When the LevelLayout changes
        if (_previousLevelLayout != EditorSettings.CurrentLevelLayout)
        {
            _serializedLevelLayout = null;
            _previousLevelLayout = EditorSettings.CurrentLevelLayout;
        }

        if (EditorSettings.CurrentLevelLayout != null)
        {
            // Create a new serialized version of the LevelLayout
            if (_serializedLevelLayout == null)
            {
                _serializedLevelLayout = new SerializedObject(EditorSettings.CurrentLevelLayout);
                _rooms = new ReorderableList(_serializedLevelLayout, _serializedLevelLayout.FindProperty("_rooms"), false, true, false, false);
            }
            else
            {
                // Make sure the the LevelLayout is updated (for when the ScriptableObject was changed outside the editor window)
                _serializedLevelLayout.Update();
            }

            DrawRoomSection();

            // Apply changes
            _serializedLevelLayout.ApplyModifiedProperties();
        }
        else
        {
            EditorGUILayout.LabelField("Choose a level layout to edit");
        }

        //GUILayout.EndArea();
    }

    private void DrawRoomSection()
    {
        // Add list of rooms
        HandleRooms(_rooms);
        _rooms.DoLayoutList();

        // "Add room" button
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Add room", GUILayout.Width(200)))
        {
            EditorSettings.CurrentLevelLayout.AddRoom(_selectedTransform);
        }

        // Field to select a transform
        _selectedTransform = (Transform)EditorGUILayout.ObjectField(_selectedTransform, typeof(Transform), true);

        GUILayout.EndHorizontal();

        EditorGUILayout.LabelField("*Room position set to the transform position (if set)");

        // Remove button is enabled only if a room is selected
        GUI.enabled = _selectedRoom > -1;
        
        // "Remove selected room" button
        if (GUILayout.Button("Remove selected room"))
        {
            EditorSettings.CurrentLevelLayout.RemoveRoom(_selectedRoom);
            _selectedRoom = -1;
        }

        GUI.enabled = true;

        EditorGUILayout.Space(15);

        // Debug colors for rooms
        EditorGUILayout.LabelField("Room debug");
        _debugRoomColor = EditorGUILayout.ColorField("Room color", _debugRoomColor);
        _debugSelectedRoomColor = EditorGUILayout.ColorField("Selected room color", _debugSelectedRoomColor);
    }

    // Add the correct callbacks for the _rooms ReorderableList
    private void HandleRooms(ReorderableList ReorderableRequirements)
    {
        ReorderableRequirements.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Rooms");
        };

        ReorderableRequirements.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            // Get the requirement data
            SerializedProperty id = ReorderableRequirements.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Id");
            SerializedProperty position = ReorderableRequirements.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Position");

            // Draw the necessary fields
            EditorGUI.LabelField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * .1f, rect.width * .2f, EditorGUIUtility.singleLineHeight), new GUIContent("Id: " + id.intValue.ToString()));
            EditorGUI.PropertyField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * .14f, rect.width * (1 - .2f), EditorGUIUtility.singleLineHeight), position, GUIContent.none);
        };

        ReorderableRequirements.onSelectCallback = (ReorderableList rooms) =>
        {
            _selectedRoom = rooms.index;
        };
    }
    #endregion

    #region SceneDebug
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
        if (EditorSettings.CurrentLevelLayout != null)
        {
            if (_rooms != null)
            {
                for (int i = 0; i < EditorSettings.CurrentLevelLayout.Rooms.Length; i++)
                {
                    if (i == _rooms.index)
                    {
                        Handles.color = _debugSelectedRoomColor;
                    }
                    else
                    {
                        Handles.color = _debugRoomColor;
                    }

                    Handles.DrawSolidDisc(EditorSettings.CurrentLevelLayout.Rooms[i].Position + Vector3.up * .1f, Vector3.up, .5f);
                }
            }

            // TODO: Draw debug line
            //Handles.DrawLine(Vector3.zero + Vector3.up * .1f, new Vector3(20, 0, 0) + Vector3.up * .1f);
        }

        Handles.BeginGUI();
        // Do your drawing here using GUI. (2D stuff)
        Handles.EndGUI();
    }
    #endregion
}
