using UnityEditor;
using UnityEngine;

// This is a custom editor for the LevelState class
[CustomEditor(typeof(LevelState))]
public class LevelStateEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GUI.enabled = false;
        DrawDefaultInspector();
    }
}
