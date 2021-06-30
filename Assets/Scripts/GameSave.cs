using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct CharacterSave
{
    public int CurrentVertex;
    public int NextVertex;
    public float Progress;
    public Vector3 Position;
    public Vector3 Rotation;
    public int Settings;

    public CharacterSave(int currentVertex, int nextVertex, float progress, Vector3 position, Vector3 rotation, int settings)
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
    public List<CharacterSave> CharacterSaves;

    public LevelStateSave(LevelGraph graph, CharacterSave[] characters)
    {
        Graph = graph;
        CharacterSaves = new List<CharacterSave>(characters);
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
        CharacterSave[] characterSaves;
        LevelGraph graph;
        LevelStateSave levelState;

        foreach (LevelStateByBuildIndex levelStateByBuildIndex in gameState.LevelStatesByBuildIndex)
        {
            // TODO: Check if doesn t affect scriptable objects
            vertices = levelStateByBuildIndex.LevelState.GetVerticesCopy();
            edges = levelStateByBuildIndex.LevelState.GetEdgesCopy();
            levelStateCharacters = levelStateByBuildIndex.LevelState.GetCharacters();

            // Convert levelStateCharacters to characterSaves
            characterSaves = new CharacterSave[levelStateCharacters.Length];

            for (int i = 0; i < levelStateCharacters.Length; i++)
            {
                characterSaves[i] = new CharacterSave(levelStateCharacters[i].Vertex,
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
