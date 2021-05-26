using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class LevelLayoutEditorWindow : EditorWindow
{
    #region Variables
    private LevelLayout _previousLevelLayout;
    public static LevelLayoutEditorSettings EditorSettings;

    private SerializedObject _serializedLevelLayout;
    
    ReorderableList _rooms;
    ReorderableList _connections;

    private int _selectedRoom = -1;
    private Transform _selectedTransform = null;

    private int _selectedConnection = -1;
    private int _selectedRoomA = 0;
    private int _selectedRoomB = 0;

    private Color _debugRoomColor = new Color(.0f, .0f, 1.0f, .5f);
    private Color _debugSelectedRoomColor = new Color(1.0f, .0f, .0f, .5f);

    private Color _debugRoomForConnectionColor = new Color(.0f, 1.0f, .0f, .5f);
    private Color _debugConnectionColor = new Color(1.0f, 1.0f, .0f, 1.0f);
    private Color _debugSelectedConnectionColor = new Color(1.0f, .0f, 1.0f, 1.0f);

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

        if (EditorSettings == null)
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
        DrawEditor();

        // Save the editor settings
        EditorUtility.SetDirty(EditorSettings);
    }

    private void DrawEditor()
    {
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
                _connections = new ReorderableList(_serializedLevelLayout, _serializedLevelLayout.FindProperty("_connections"), false, true, false, false);
            }
            else
            {
                // Make sure the the LevelLayout is updated (for when the ScriptableObject was changed outside the editor window)
                _serializedLevelLayout.Update();
            }

            DrawRoomsSection();

            EditorGUILayout.Space(15);

            DrawConnectionsSection();

            // Apply changes
            if (_serializedLevelLayout.ApplyModifiedProperties()) Debug.Log("SAVED!");
        }
        else
        {
            EditorGUILayout.LabelField("Choose a level layout to edit");
        }
    }

    private void DrawRoomsSection()
    {
        // Add list of rooms
        HandleRooms(_rooms);
        _rooms.DoLayoutList();

        // "Add room" button
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Add room", GUILayout.Width(200)))
        {
            EditorSettings.CurrentLevelLayout.AddRoom(_selectedTransform);

            // Most be set dirty because the changes where made directly inside the ScriptableObject
            EditorUtility.SetDirty(EditorSettings.CurrentLevelLayout);
        }

        // Field to select a transform
        _selectedTransform = (Transform)EditorGUILayout.ObjectField(_selectedTransform, typeof(Transform), true);

        GUILayout.EndHorizontal();

        EditorGUILayout.LabelField("*Room position set to the transform position (if set)");

        EditorGUILayout.Space(10);

        // Remove button is enabled only if a room is selected
        GUI.enabled = _selectedRoom > -1;
        
        // "Remove selected room" button
        if (GUILayout.Button("Remove selected room"))
        {
            EditorSettings.CurrentLevelLayout.RemoveRoom(_selectedRoom);

            // Most be set dirty because the changes where made directly inside the ScriptableObject
            EditorUtility.SetDirty(EditorSettings.CurrentLevelLayout);

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
    private void HandleRooms(ReorderableList ReorderableRooms)
    {
        ReorderableRooms.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Rooms");
        };

        ReorderableRooms.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            // Get the requirement data
            SerializedProperty id = ReorderableRooms.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Id");
            SerializedProperty position = ReorderableRooms.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Position");

            // Draw the necessary fields
            EditorGUI.LabelField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * .1f, rect.width * .2f, EditorGUIUtility.singleLineHeight), new GUIContent("Id: " + id.intValue.ToString()));
            EditorGUI.PropertyField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * .14f, rect.width * (1 - .2f), EditorGUIUtility.singleLineHeight), position, GUIContent.none);
        };

        ReorderableRooms.onSelectCallback = (ReorderableList rooms) =>
        {
            _selectedRoom = rooms.index;
        };
    }

    private void DrawConnectionsSection()
    {
        // Add list of connections
        HandleConnections(_connections);
        _connections.DoLayoutList();

        // Add dropdown for room
        List<string> ids = EditorSettings.CurrentLevelLayout.GetAllRoomIds();
        ids.Insert(0, "None");

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Room A (id)");
        EditorGUILayout.LabelField("Room B (id)");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        _selectedRoomA = EditorGUILayout.Popup(_selectedRoomA, ids.ToArray());
        _selectedRoomB = EditorGUILayout.Popup(_selectedRoomB, ids.ToArray());
        GUILayout.EndHorizontal();

        // "Add connection" button (only available if selected 2 different rooms)
        GUI.enabled = (_selectedRoomA != _selectedRoomB) && _selectedRoomA > 0 && _selectedRoomB > 0;

        if (GUILayout.Button("Add connection"))
        {
            EditorSettings.CurrentLevelLayout.AddConnection(_selectedRoomA - 1, _selectedRoomB - 1);

            // Most be set dirty because the changes where made directly inside the ScriptableObject
            EditorUtility.SetDirty(EditorSettings.CurrentLevelLayout);

            _selectedRoomA = 0;
            _selectedRoomB = 0;
        }

        EditorGUILayout.LabelField("*Two different rooms must be selected to create a connection");

        EditorGUILayout.Space(10);

        GUI.enabled = _selectedConnection > -1;

        // "Remove selected connection" button
        if (GUILayout.Button("Remove selected connection"))
        {
            EditorSettings.CurrentLevelLayout.RemoveConnection(_selectedConnection);

            // Most be set dirty because the changes where made directly inside the ScriptableObject
            EditorUtility.SetDirty(EditorSettings.CurrentLevelLayout);

            _selectedConnection = -1;
        }

        GUI.enabled = true;

        EditorGUILayout.Space(15);

        // Debug colors for collections
        EditorGUILayout.LabelField("Connection debug");
        _debugRoomForConnectionColor = EditorGUILayout.ColorField("Room for connection color", _debugRoomForConnectionColor);
        _debugConnectionColor = EditorGUILayout.ColorField("Connection color", _debugConnectionColor);
        _debugSelectedConnectionColor = EditorGUILayout.ColorField("Selected connection color", _debugSelectedConnectionColor);
    }

    // Add the correct callbacks for the _connections ReorderableList
    private void HandleConnections(ReorderableList ReorderableConnections)
    {
        ReorderableConnections.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Connections");
        };

        ReorderableConnections.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            // Get the connection data
            SerializedProperty id = ReorderableConnections.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Id");
            SerializedProperty RoomA = ReorderableConnections.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("RoomA");
            SerializedProperty RoomB = ReorderableConnections.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("RoomB");
            SerializedProperty Cost = ReorderableConnections.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Cost");
            SerializedProperty Traversable = ReorderableConnections.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Traversable");
            SerializedProperty Type = ReorderableConnections.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Type");
            
            string roomAToRoomB = "Room " + EditorSettings.CurrentLevelLayout.Rooms[RoomA.intValue].Id.ToString() + " to room " + EditorSettings.CurrentLevelLayout.Rooms[RoomB.intValue].Id.ToString();

            // Draw the necessary fields
            EditorGUI.LabelField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * .1f, rect.width * .2f, EditorGUIUtility.singleLineHeight), new GUIContent("Id: " + id.intValue.ToString()));
            EditorGUI.LabelField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * .1f, rect.width * .8f, EditorGUIUtility.singleLineHeight), new GUIContent(roomAToRoomB));
            EditorGUI.LabelField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * 1.14f, rect.width * .2f, EditorGUIUtility.singleLineHeight), new GUIContent("Cost"));
            EditorGUI.PropertyField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * 1.14f, rect.width * .8f, EditorGUIUtility.singleLineHeight), Cost, GUIContent.none);
            EditorGUI.LabelField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * 2.14f, rect.width * .2f, EditorGUIUtility.singleLineHeight), new GUIContent("Traversable"));
            EditorGUI.PropertyField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * 2.14f, rect.width * .8f, EditorGUIUtility.singleLineHeight), Traversable, GUIContent.none);
            EditorGUI.LabelField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * 3.14f, rect.width * .2f, EditorGUIUtility.singleLineHeight), new GUIContent("Type"));
            EditorGUI.PropertyField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * 3.14f, rect.width * .8f, EditorGUIUtility.singleLineHeight), Type, GUIContent.none);
        };

        ReorderableConnections.elementHeightCallback = (int index) =>
        {
            float height = EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUIUtility.singleLineHeight * 4.0f;
            return height + EditorGUIUtility.standardVerticalSpacing;
        };

        ReorderableConnections.onSelectCallback = (ReorderableList connections) =>
        {
            _selectedConnection = connections.index;
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
            // Draw room debugs
            if (_rooms != null)
            {
                for (int i = 0; i < EditorSettings.CurrentLevelLayout.Rooms.Length; i++)
                {
                    // If the room is selected during the creation of a connection
                    if ((_selectedRoomA > 0 || _selectedRoomB > 0) && (i == _selectedRoomA - 1 || i == _selectedRoomB - 1))
                    {
                        Handles.color = _debugRoomForConnectionColor;
                    }
                    // If the room is selected in the list of room
                    else if ((_selectedRoomA == 0 && _selectedRoomB == 0) && i == _rooms.index)
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

            // Draw connection debugs
            if (_connections != null)
            {
                int roomAIndex;
                int roomBIndex;

                for (int i = 0; i < EditorSettings.CurrentLevelLayout.Connections.Length; i++)
                {
                    if (_selectedRoomA == 0 && _selectedRoomB == 0 && i == _connections.index)
                    {
                        Handles.color = _debugSelectedConnectionColor;
                    }
                    else
                    {
                        Handles.color = _debugConnectionColor;
                    }

                    roomAIndex = EditorSettings.CurrentLevelLayout.Connections[i].RoomA;
                    roomBIndex = EditorSettings.CurrentLevelLayout.Connections[i].RoomB;

                    Handles.DrawLine(EditorSettings.CurrentLevelLayout.Rooms[roomAIndex].Position + Vector3.up * .1f, EditorSettings.CurrentLevelLayout.Rooms[roomBIndex].Position + Vector3.up * .1f, .5f);
                }
            }
        }

        Handles.BeginGUI();
        // Do your drawing here using GUI. (2D stuff)
        Handles.EndGUI();
    }
    #endregion
}
