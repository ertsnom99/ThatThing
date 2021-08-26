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
public partial struct CharacterState
{
    [SerializeField]
    private int _id;

    public int ID
    {
        get { return _id; }
        private set { _id = value; }
    }

    // Index of the vertex
    [SerializeField]
    private int _vertex;

    public int Vertex
    {
        get { return _vertex; }
        private set { _vertex = value; }
    }

    [SerializeField]
    private int _settings;

    public int Settings
    {
        get { return _settings; }
        private set { _settings = value; }
    }
}

[Serializable]
public struct LevelEdge
{
    // Index of the corresponding LevelStateByBuildIndex
    [SerializeField]
    private int _graphA;

    public int GraphA
    {
        get { return _graphA; }
        private set { _graphA = value; }
    }

    // Index of the corresponding LevelStateByBuildIndex
    [SerializeField]
    private int _graphB;

    public int GraphB
    {
        get { return _graphB; }
        private set { _graphB = value; }
    }

    [SerializeField]
    private Edge _edge;

    public Edge Edge
    {
        get { return _edge; }
        private set { _edge = value; }
    }
}

[CreateAssetMenu(fileName = "GameState", menuName = "AI Simulation/States/Game State")]
public partial class GameState : ScriptableObject
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

public partial struct CharacterState
{
    [HideInInspector]
    public bool Folded;
}

public partial class GameState
{
    [HideInInspector]
    public int CharacterIdCount = 0;
    [HideInInspector]
    public bool DisplayCharacterCounters;

    public bool IsValid(CharactersSettings charactersSettings)
    {
        if (!_playerState)
        {
            // No PlayerState
            return false;
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
            if (levelStateByBuildIndex.BuildIndex < 0 || levelStateByBuildIndex.BuildIndex >= activeBuildIndexCount)
            {
                // BuildIndex not in build
                return false;
            }

            if (!_buildIndexes.Contains(levelStateByBuildIndex.BuildIndex))
            {
                _buildIndexes.Add(levelStateByBuildIndex.BuildIndex);
            }
            else
            {
                // Duplicated index
                return false;
            }

            if (!levelStateByBuildIndex.Graph)
            {
                // No Graph
                return false;
            }

            foreach(CharacterState characterState in levelStateByBuildIndex.CharacterStates)
            {
                if (characterState.Vertex < 0 || characterState.Vertex >= levelStateByBuildIndex.Graph.Vertices.Length)
                {
                    // Vertex doesn't exist
                    return false;
                }

                if (characterState.Settings < 0 || characterState.Settings >= charactersSettings.Settings.Length)
                {
                    // Settings don't exist
                    return false;
                }
            }
        }

        // Check all LevelEdges
        foreach(LevelEdge levelEdge in _levelEdges)
        {
            if (levelEdge.GraphA < 0 || levelEdge.GraphA >= _levelStatesByBuildIndex.Length)
            {
                // GraphA doesn't exist
                return false;
            }

            if (levelEdge.GraphB < 0 || levelEdge.GraphB >= _levelStatesByBuildIndex.Length)
            {
                // GraphB doesn't exist
                return false;
            }

            if (levelEdge.Edge.VertexA < 0 || levelEdge.Edge.VertexA >= _levelStatesByBuildIndex[levelEdge.GraphA].Graph.Vertices.Length)
            {
                // VertexA doesn't exist
                return false;
            }

            if (levelEdge.Edge.VertexB < 0 || levelEdge.Edge.VertexB >= _levelStatesByBuildIndex[levelEdge.GraphB].Graph.Vertices.Length)
            {
                // VertexA doesn't exist
                return false;
            }
        }

        return true;
    }
}
#endif
