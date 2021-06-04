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
    private ReorderableList _rooms;
    private ReorderableList _connections;

    private int _selectedRoom = -1;
    private Transform _selectedTransform = null;
    private int _selectedConnection = -1;
    private int _selectedRoomA = 0;
    private int _selectedRoomB = 0;

    // Characters variables
    private ReorderableList _characters;

    private int _selectedCharacter = -1;
    private int _selectedRoomForCharacter = 0;

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
    private void OnGUI()
    {
        DrawEditor();
    }

    private void DrawEditor()
    {
        // Field for the LevelState
        //EditorGUILayout.LabelField(" ", GUILayout.Width(100));
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
                _rooms = new ReorderableList(_serializedLevelState, _serializedLevelState.FindProperty("_graph.Rooms"), false, true, false, false);
                _connections = new ReorderableList(_serializedLevelState, _serializedLevelState.FindProperty("_graph.Connections"), false, true, false, false);

                // Characters serialization
                _characters = new ReorderableList(_serializedLevelState, _serializedLevelState.FindProperty("_characters"), false, true, false, false);

                // Reset selections
                _selectedRoom = -1;
                _selectedConnection = -1;
                _selectedRoomA = 0;
                _selectedRoomB = 0;
                _selectedCharacter = -1;
                _selectedRoomForCharacter = 0;
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
                    DrawCharacterSection();
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
        DrawRoomsSection();
        EditorGUILayout.Space(15);
        DrawConnectionsSection();
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
            Settings.CurrentLevelState.AddRoom(_selectedTransform);

            // Most be set dirty because the changes where made directly inside the ScriptableObject
            EditorUtility.SetDirty(Settings.CurrentLevelState);
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
            Settings.CurrentLevelState.RemoveRoom(_selectedRoom);

            // Most be set dirty because the changes where made directly inside the ScriptableObject
            EditorUtility.SetDirty(Settings.CurrentLevelState);

            _selectedRoom = -1;
        }

        GUI.enabled = true;

        EditorGUILayout.Space(15);
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
        List<string> ids = Settings.CurrentLevelState.GetAllRoomIds();
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
            Settings.CurrentLevelState.AddConnection(_selectedRoomA - 1, _selectedRoomB - 1);

            // Most be set dirty because the changes where made directly inside the ScriptableObject
            EditorUtility.SetDirty(Settings.CurrentLevelState);

            _selectedRoomA = 0;
            _selectedRoomB = 0;
        }

        EditorGUILayout.LabelField("*Two different rooms must be selected to create a connection");

        EditorGUILayout.Space(10);

        GUI.enabled = _selectedConnection > -1;

        // "Remove selected connection" button
        if (GUILayout.Button("Remove selected connection"))
        {
            Settings.CurrentLevelState.RemoveConnection(_selectedConnection);

            // Most be set dirty because the changes where made directly inside the ScriptableObject
            EditorUtility.SetDirty(Settings.CurrentLevelState);

            _selectedConnection = -1;
        }

        GUI.enabled = true;

        EditorGUILayout.Space(15);
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
            SerializedProperty roomA = ReorderableConnections.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("RoomA");
            SerializedProperty roomB = ReorderableConnections.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("RoomB");
            SerializedProperty cost = ReorderableConnections.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Cost");
            SerializedProperty traversable = ReorderableConnections.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Traversable");
            SerializedProperty type = ReorderableConnections.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Type");

            Room[] rooms = Settings.CurrentLevelState.GetRooms();

            if (rooms.Length > roomA.intValue && rooms.Length > roomB.intValue)
            {
                string roomAToRoomB = "Room " + rooms[roomA.intValue].Id.ToString() + " to room " + rooms[roomB.intValue].Id.ToString();

                // Draw the necessary fields
                EditorGUI.LabelField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * .1f, rect.width * .2f, EditorGUIUtility.singleLineHeight), new GUIContent("Id: " + id.intValue.ToString()));
                EditorGUI.LabelField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * .1f, rect.width * .8f, EditorGUIUtility.singleLineHeight), new GUIContent(roomAToRoomB));
                EditorGUI.LabelField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * 1.14f, rect.width * .2f, EditorGUIUtility.singleLineHeight), new GUIContent("Cost"));
                EditorGUI.PropertyField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * 1.14f, rect.width * .8f, EditorGUIUtility.singleLineHeight), cost, GUIContent.none);
                EditorGUI.LabelField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * 2.14f, rect.width * .2f, EditorGUIUtility.singleLineHeight), new GUIContent("Traversable"));
                EditorGUI.PropertyField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * 2.14f, rect.width * .8f, EditorGUIUtility.singleLineHeight), traversable, GUIContent.none);
                EditorGUI.LabelField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * 3.14f, rect.width * .2f, EditorGUIUtility.singleLineHeight), new GUIContent("Type"));
                EditorGUI.PropertyField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * 3.14f, rect.width * .8f, EditorGUIUtility.singleLineHeight), type, GUIContent.none);
            }
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

    private void DrawCharacterSection()
    {
        // Add list of connections
        HandleCharacters(_characters);
        _characters.DoLayoutList();

        // Add dropdown to select a room
        List<string> ids = Settings.CurrentLevelState.GetAllRoomIds();
        ids.Insert(0, "None");

        GUILayout.BeginHorizontal();
        // "Add Character" button (only available if selected 2 different rooms)
        GUI.enabled = _selectedRoomForCharacter > 0;

        if (GUILayout.Button("Add Character"))
        {
            Settings.CurrentLevelState.AddCharacter(_selectedRoomForCharacter - 1);

            // Most be set dirty because the changes where made directly inside the ScriptableObject
            EditorUtility.SetDirty(Settings.CurrentLevelState);

            _selectedRoomForCharacter = 0;
        }

        GUI.enabled = true;

        _selectedRoomForCharacter = EditorGUILayout.Popup(_selectedRoomForCharacter, ids.ToArray());

        GUILayout.EndHorizontal();

        GUI.enabled = _selectedCharacter > -1;

        // "Remove selected connection" button
        if (GUILayout.Button("Remove selected connection"))
        {
            Settings.CurrentLevelState.RemoveCharacter(_selectedCharacter);

            // Most be set dirty because the changes where made directly inside the ScriptableObject
            EditorUtility.SetDirty(Settings.CurrentLevelState);

            _selectedCharacter = -1;
        }

        GUI.enabled = true;
    }

    // Add the correct callbacks for the _characters ReorderableList
    private void HandleCharacters(ReorderableList ReorderableConnections)
    {
        ReorderableConnections.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Characters");
        };

        ReorderableConnections.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            // Get the connection data
            SerializedProperty room = ReorderableConnections.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Room");

            Room[] rooms = Settings.CurrentLevelState.GetRooms();

            if (rooms.Length > room.intValue)
            {
                // Draw the necessary fields
                EditorGUI.LabelField(new Rect(rect.x + rect.width * .2f, rect.y + EditorGUIUtility.singleLineHeight * .1f, rect.width * .8f, EditorGUIUtility.singleLineHeight), new GUIContent(rooms[room.intValue].Id.ToString()));
            }
        };

        ReorderableConnections.elementHeightCallback = (int index) =>
        {
            return EditorGUIUtility.singleLineHeight + 2 * EditorGUIUtility.standardVerticalSpacing;
        };

        ReorderableConnections.onSelectCallback = (ReorderableList connections) =>
        {
            _selectedCharacter = connections.index;
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
            Room[] rooms = Settings.CurrentLevelState.GetRooms();

            // Draw room debugs
            if (_rooms != null)
            {
                for (int i = 0; i < rooms.Length; i++)
                {
                    // If the room is selected during the creation of a connection
                    if ((toolbarSelection == 0 && (_selectedRoomA > 0 || _selectedRoomB > 0) && (i == _selectedRoomA - 1 || i == _selectedRoomB - 1))
                     || (toolbarSelection == 1 && _selectedRoomForCharacter > 0 && i == _selectedRoomForCharacter - 1))
                    {
                        Handles.color = Settings.DebugRoomForConnectionColor;
                    }
                    // If the room is selected in the list of room
                    else if (toolbarSelection == 0 && (_selectedRoomA == 0 && _selectedRoomB == 0) && i == _rooms.index)
                    {
                        Handles.color = Settings.DebugSelectedRoomColor;
                    }
                    else
                    {
                        Handles.color = Settings.DebugRoomColor;
                    }

                    Handles.DrawSolidDisc(rooms[i].Position + Vector3.up * .1f, Vector3.up, Settings.DebugRoomDiscRadius);
                }
            }

            // Draw connection debugs
            if (_connections != null)
            {
                Connection[] connections = Settings.CurrentLevelState.GetConnections();

                int roomAIndex;
                int roomBIndex;

                for (int i = 0; i < connections.Length; i++)
                {
                    if (toolbarSelection == 0 && _selectedRoomA == 0 && _selectedRoomB == 0 && i == _connections.index)
                    {
                        Handles.color = Settings.DebugSelectedConnectionColor;
                    }
                    else
                    {
                        Handles.color = Settings.DebugConnectionColor;
                    }

                    roomAIndex = connections[i].RoomA;
                    roomBIndex = connections[i].RoomB;

                    Handles.DrawLine(rooms[roomAIndex].Position + Vector3.up * .1f, rooms[roomBIndex].Position + Vector3.up * .1f, .5f);
                }
            }

            // Draw character debugs
            if (toolbarSelection == 1 && _characters != null)
            {
                Character[] characters = Settings.CurrentLevelState.GetCharacters();

                Dictionary<int, int> characterCountByRoom = new Dictionary<int, int>();
                int selectedCharacterRoom = -1;

                if (_selectedCharacter > -1)
                {
                    selectedCharacterRoom = characters[_selectedCharacter].Room;
                }

                foreach (Character character in characters)
                {
                    if(!characterCountByRoom.ContainsKey(character.Room))
                    {
                        characterCountByRoom.Add(character.Room, 1);
                    }
                    else
                    {
                        characterCountByRoom[character.Room] += 1;
                    }
                }

                foreach (KeyValuePair<int, int> room in characterCountByRoom)
                {
                    if (selectedCharacterRoom != room.Key)
                    {
                        Handles.Label(rooms[room.Key].Position + Vector3.up * 2.0f, Settings.CharacterIcon);
                    }
                    else
                    {
                        Handles.Label(rooms[room.Key].Position + Vector3.up * 2.0f, Settings.SelectedCharacterIcon);
                    }

                    Handles.Label(rooms[room.Key].Position + Vector3.up * 1.0f, "X" + room.Value.ToString(), Settings.CharacterCounterStyle);
                }
            }
        }
    }
    #endregion
}
