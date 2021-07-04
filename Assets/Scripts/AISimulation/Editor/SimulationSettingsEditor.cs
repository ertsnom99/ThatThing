using UnityEditor;
using UnityEngine;

// This is a custom editor for the SimulationSettings class
[CustomEditor(typeof(SimulationSettings))]
public class SimulationSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GUI.enabled = false;
        DrawDefaultInspector();
    }
}
