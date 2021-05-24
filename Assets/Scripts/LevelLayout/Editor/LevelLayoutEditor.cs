using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

// This is a custom editor for the HouseLayout class
[CustomEditor(typeof(LevelLayout))]
public class HouseLayoutEditor : Editor
{
    private LevelLayout _levelLayout;

    private SerializedProperty _serializedRooms;
    private SerializedProperty _serializedConnections;

    private void OnEnable()
    {
        // Get the HouseLayout that is being inspected
        _levelLayout = (LevelLayout)target;

        _serializedRooms = serializedObject.FindProperty("_rooms");
        _serializedConnections = serializedObject.FindProperty("_connections");
    }
}
