using System;
using System.Collections.Generic;
using GraphCreator;
using UnityEditor;
using UnityEngine;

[Serializable]
public partial struct LevelStateByBuildIndex
{
    [SerializeField]
    private int _buildIndex;

    public int BuildIndex
    {
        get { return _buildIndex; }
        private set { _buildIndex = value; }
    }

    [SerializeField]
    private Graph _graph;

    public Graph Graph
    {
        get { return _graph; }
        private set { _graph = value; }
    }

    [SerializeField]
    private CharacterState[] _characterStates;

    public CharacterState[] CharacterStates
    {
        get { return _characterStates; }
        private set { _characterStates = value; }
    }
}

[Serializable]
public partial class CharacterState
{
    [SerializeField]
    private int _id;

    public int Id
    {
        get { return _id; }
        private set { _id = value; }
    }

    [SerializeField]
    private int _settings = -1;

    public int Settings
    {
        get { return _settings; }
        private set { _settings = value; }
    }

    [NonSerialized]
    public int BuildIndex = -1;
    public PositionOnGraph PositionOnGraph;
    [NonSerialized]
    public Vector3 Position;
    [NonSerialized]
    public Vector3 Rotation;
    [NonSerialized]
    public bool ChangingLevel;
    [NonSerialized]
    public int TargetLevel;

    public CharacterState() { }

    public CharacterState(CharacterState characterState)
    {
        _id = characterState.Id;
        _settings = characterState.Settings;
        BuildIndex = characterState.BuildIndex;
        PositionOnGraph.VertexA = characterState.PositionOnGraph.VertexA;
        PositionOnGraph.VertexB = characterState.PositionOnGraph.VertexB;
        PositionOnGraph.Progress = characterState.PositionOnGraph.Progress;
        Position = characterState.Position;
        Rotation = characterState.Rotation;
        ChangingLevel = characterState.ChangingLevel;
        TargetLevel = characterState.TargetLevel;
    }
}

[Serializable]
public partial class LevelEdge
{
    // Index of the corresponding LevelStateByBuildIndex
    [SerializeField]
    private int _levelA = -1;

    public int LevelA
    {
        get { return _levelA; }
        private set { _levelA = value; }
    }

    // Index of the corresponding LevelStateByBuildIndex
    [SerializeField]
    private int _levelB = -1;

    public int LevelB
    {
        get { return _levelB; }
        private set { _levelB = value; }
    }

    [SerializeField]
    private Edge _edge;

    public Edge Edge
    {
        get { return _edge; }
        private set { _edge = value; }
    }

    public LevelEdge() { }

    public LevelEdge(int levelA, int levelB, Edge edge)
    {
        _levelA = levelA;
        _levelB = levelB;
        _edge = edge;
#if UNITY_EDITOR
        Folded = false;
#endif
    }
}

[CreateAssetMenu(fileName = "SimulationState", menuName = "AI Simulation/States/Game State")]
public partial class SimulationState : ScriptableObject
{
    [SerializeField]
    private PlayerState _playerState;

    public PlayerState PlayerState
    {
        get { return _playerState; }
        private set { _playerState = value; }
    }

    [SerializeField]
    private LevelStateByBuildIndex[] _levelStatesByBuildIndex = new LevelStateByBuildIndex[0];

    public LevelStateByBuildIndex[] LevelStatesByBuildIndex
    {
        get { return _levelStatesByBuildIndex; }
        private set { _levelStatesByBuildIndex = value; }
    }

    [SerializeField]
    private LevelEdge[] _levelEdges = new LevelEdge[0];

    public LevelEdge[] LevelEdges
    {
        get { return _levelEdges; }
        private set { _levelEdges = value; }
    }
}
#if UNITY_EDITOR
public partial struct LevelStateByBuildIndex
{
    [HideInInspector]
    public bool Folded;
}

public partial class CharacterState
{
    [HideInInspector]
    public bool Folded;
}

public partial class LevelEdge
{
    [HideInInspector]
    public bool Folded;
}

public partial class SimulationState
{
    [HideInInspector]
    public int CharacterIdCount = 0;
    [HideInInspector]
    public bool DisplayCharacterCounters;
    [HideInInspector]
    public int LevelEdgeIdCount = 0;

    // Error texts
    private const string _noPlayerStateError = "No PlayerState";
    private const string _invalidIndexError = "Some build indexes are invalid";
    private const string _duplicateBuildIndexError = "Some build indexes are duplicated";
    private const string _nullGraphsError = "Some graphs are null";
    private const string _characterStateVerticesError = "Some CharacterState vertices are invalid";
    private const string _characterStateSettingsError = "Some CharacterState settings are invalid";
    private const string _invalidLevelAError = "Some LevelEdge LevelA are invalid";
    private const string _invalidLevelBError = "Some LevelEdge LevelB are invalid";
    private const string _levelASameHasLevelB = "Some LevelEdge LevelA and LevelB are the same";
    private const string _invalidVertexA = "Some LevelEdge VertexA are invalid";
    private const string _invalidVertexB = "Some LevelEdge VertexB are invalid";

    public bool IsValid(CharactersSettings charactersSettings, out string[] errors)
    {
        List<string> errorList = new List<string>();

        if (!_playerState)
        {
            errorList.Add(_noPlayerStateError);
        }

        // Count how many scenes in the build are active
        int activeBuildIndexCount = 0;

        foreach (EditorBuildSettingsScene editorBuildSettingsScene in EditorBuildSettings.scenes)
        {
            if (editorBuildSettingsScene.enabled)
            {
                activeBuildIndexCount++;
            }
        }

        // Check all LevelStatesByBuildIndex
        List<int> _buildIndexes = new List<int>();

        foreach (LevelStateByBuildIndex levelStateByBuildIndex in _levelStatesByBuildIndex)
        {
            if ((levelStateByBuildIndex.BuildIndex < 0 || levelStateByBuildIndex.BuildIndex >= activeBuildIndexCount) && !errorList.Contains(_invalidIndexError))
            {
                errorList.Add(_invalidIndexError);
            }

            if (!_buildIndexes.Contains(levelStateByBuildIndex.BuildIndex))
            {
                _buildIndexes.Add(levelStateByBuildIndex.BuildIndex);
            }
            else if (!errorList.Contains(_duplicateBuildIndexError))
            {
                errorList.Add(_duplicateBuildIndexError);
            }

            if (!levelStateByBuildIndex.Graph && !errorList.Contains(_nullGraphsError))
            {
                errorList.Add(_nullGraphsError);
                continue;
            }

            foreach(CharacterState characterState in levelStateByBuildIndex.CharacterStates)
            {
                if ((characterState.PositionOnGraph.VertexA < 0 || characterState.PositionOnGraph.VertexA >= levelStateByBuildIndex.Graph.Vertices.Length) && !errorList.Contains(_characterStateVerticesError))
                {
                    errorList.Add(_characterStateVerticesError);
                }

                if ((characterState.Settings < 0 || characterState.Settings >= charactersSettings.Settings.Length) && !errorList.Contains(_characterStateSettingsError))
                {
                    errorList.Add(_characterStateSettingsError);
                }
            }
        }

        // Stop validation if an error was already found
        if (errorList.Count > 0)
        {
            errors = errorList.ToArray();
            return errors.Length == 0;
        }

        bool validA, validB;

        // List used to find duplicate LevelEdges
        List<string> levelEdgeIds = new List<string>();

        // Check all LevelEdges
        foreach(LevelEdge levelEdge in _levelEdges)
        {
            validA = true;
            validB = true;

            if ((levelEdge.LevelA < 0 || levelEdge.LevelA >= _levelStatesByBuildIndex.Length) && !errorList.Contains(_invalidLevelAError))
            {
                validA = false;
                errorList.Add(_invalidLevelAError);
            }

            if ((levelEdge.LevelB < 0 || levelEdge.LevelB >= _levelStatesByBuildIndex.Length) && !errorList.Contains(_invalidLevelBError))
            {
                validB = false;
                errorList.Add(_invalidLevelBError);
            }

            if (!validA || !validB)
            {
                continue;
            }

            if (levelEdge.LevelA == levelEdge.LevelB && !errorList.Contains(_levelASameHasLevelB))
            {
                errorList.Add(_levelASameHasLevelB);
            }

            if ((levelEdge.Edge.VertexA < 0 || levelEdge.Edge.VertexA >= _levelStatesByBuildIndex[levelEdge.LevelA].Graph.Vertices.Length) && !errorList.Contains(_invalidVertexA))
            {
                errorList.Add(_invalidVertexA);
            }

            if ((levelEdge.Edge.VertexB < 0 || levelEdge.Edge.VertexB >= _levelStatesByBuildIndex[levelEdge.LevelB].Graph.Vertices.Length) && !errorList.Contains(_invalidVertexB))
            {
                errorList.Add(_invalidVertexB);
            }

            string idForward = "" + levelEdge.LevelA + levelEdge.Edge.VertexA + levelEdge.LevelB + levelEdge.Edge.VertexB;
            string idBackward = "" + levelEdge.LevelB + levelEdge.Edge.VertexB + levelEdge.LevelA + levelEdge.Edge.VertexA;

            if (levelEdgeIds.IndexOf(idForward) == -1 && levelEdgeIds.IndexOf(idBackward) == -1)
            {
                levelEdgeIds.Add(idForward);
                levelEdgeIds.Add(idBackward);
            }
            else
            {
                errorList.Add("Duplicate LevelEdge: LevelA (Build index " + levelEdge.LevelA + " -> Vertex " + levelEdge.Edge.VertexA + ") and LevelB (Build index " + levelEdge.LevelB + " -> Vertex " + levelEdge.Edge.VertexB + ") DUPLICATE COULD BE REVERSED");
            }
        }

        errors = errorList.ToArray();
        return errors.Length == 0;
    }
}
#endif
