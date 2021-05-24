using System;
using UnityEditor;
using UnityEngine;

public class LevelLayoutEditorWindow : EditorWindow
{
    #region Variables
    private Vector3 _mousePosition;

    public static LevelLayoutEditorSettings EditorSettings;

    //private GUIStyle _windowStyle;

    //private readonly Rect _backgroundArea = new Rect(0, 0, 500, 500);

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
        Event e = Event.current;
        _mousePosition = e.mousePosition;

        HandleUserInput(e);

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
        EditorGUILayout.LabelField("Level Layout:", GUILayout.Width(100));
        EditorSettings.CurrentLevelLayout = (LevelLayout)EditorGUILayout.ObjectField(EditorSettings.CurrentLevelLayout, typeof(LevelLayout), false, GUILayout.Width(200));

        //GUILayout.EndArea();
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
        // How to draw in scene...
        Color blue = Color.blue;
        blue.a = .4f;
        Handles.color = blue;
        Handles.DrawSolidDisc(Vector3.zero + Vector3.up * .1f, Vector3.up, .5f);
        Handles.DrawSolidDisc(new Vector3(20, 0,0) + Vector3.up * .1f, Vector3.up, .5f);
        Handles.color = Color.red;
        Handles.DrawLine(Vector3.zero + Vector3.up * .1f, new Vector3(20, 0, 0) + Vector3.up * .1f);

        Handles.BeginGUI();
        // Do your drawing here using GUI. (2D stuff)
        Handles.EndGUI();
    }
    #endregion
}
