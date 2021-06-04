using System.IO;
using UnityEditor;
using UnityEngine;

// This is a custom editor for the LevelState class
[CustomEditor(typeof(LevelState))]
public class LevelStateEditor : Editor
{
    private const int _sectionSpacing = 15;

    private string _duplicateAssetName = "LevelState";

    public override void OnInspectorGUI()
    {
        GUI.enabled = false;
        DrawDefaultInspector();
        GUI.enabled = true;

        EditorGUILayout.Space(_sectionSpacing);

        #region Duplicate
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Duplicate asset name", GUILayout.MaxWidth(135));
        _duplicateAssetName = EditorGUILayout.TextField(_duplicateAssetName);
        GUILayout.EndHorizontal();

        // Get path to scriptable object
        string path = AssetDatabase.GetAssetPath(target);
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

        GUI.enabled = true;
        #endregion
    }
}
