using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CharacterState
{
    public int CurrentVertex;
    public int NextVertex;
    public float Progress;
    public Vector3 Position;
    public Vector3 Rotation;
    public int Settings;

    public CharacterState()
    {
        CurrentVertex = 0;
        NextVertex = -1;
        Progress = 0;
        Position = Vector3.zero;
        Rotation = Vector3.zero;
        Settings = 0;
    }

    public CharacterState(int currentVertex, int nextVertex, float progress, Vector3 position, Vector3 rotation, int settings)
    {
        CurrentVertex = currentVertex;
        NextVertex = nextVertex;
        Progress = progress;
        Position = position;
        Rotation = rotation;
        Settings = settings;
    }
}

[Serializable]
public struct LevelStateSave
{
    public LevelGraph Graph;
    public List<CharacterState> CharacterSaves;

    public LevelStateSave(LevelGraph graph, CharacterState[] characters)
    {
        Graph = graph;
        CharacterSaves = new List<CharacterState>(characters);
    }
}

[Serializable]
public class GameSave
{
    // Player info
    public int PlayerLevel = 0;
    public Vector3 PlayerPosition;
    public Vector3 PlayerRotatin;

    public Dictionary<int, LevelStateSave> LevelStatesByBuildIndex = new Dictionary<int, LevelStateSave>();

    // Copy constructor.
    public GameSave(GameState gameState)
    {
        // Copy PlayerState
        PlayerLevel = gameState.PlayerState.Level;
        PlayerPosition = gameState.PlayerState.Position;
        PlayerRotatin = gameState.PlayerState.Rotation;

        // Copy LevelStates
        Vertex[] vertices;
        Edge[] edges;
        LevelStateCharacter[] levelStateCharacters;
        CharacterState[] characterSaves;
        LevelGraph graph;
        LevelStateSave levelState;

        foreach (LevelStateByBuildIndex levelStateByBuildIndex in gameState.LevelStatesByBuildIndex)
        {
            // TODO: Check if doesn t affect scriptable objects
            vertices = levelStateByBuildIndex.LevelState.GetVerticesCopy();
            edges = levelStateByBuildIndex.LevelState.GetEdgesCopy();
            levelStateCharacters = levelStateByBuildIndex.LevelState.GetCharacters();

            // Convert levelStateCharacters to characterSaves
            characterSaves = new CharacterState[levelStateCharacters.Length];

            for (int i = 0; i < levelStateCharacters.Length; i++)
            {
                characterSaves[i] = new CharacterState(levelStateCharacters[i].Vertex,
                                                      -1,
                                                      .0f,
                                                      vertices[levelStateCharacters[i].Vertex].Position,
                                                      Vector3.zero,
                                                      levelStateCharacters[i].Settings);
            }

            graph = new LevelGraph(vertices, edges);
            levelState = new LevelStateSave(graph, characterSaves);

            LevelStatesByBuildIndex.Add(levelStateByBuildIndex.BuildIndex, levelState);
        }
        // TODO: Check if GameSave is correct
    }
}
