using UnityEditor;
using UnityEngine;

// This is a custom editor for the HouseLayout class
[CustomEditor(typeof(LevelLayout))]
public class HouseLayoutEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GUI.enabled = false;
        DrawDefaultInspector();
    }
}
