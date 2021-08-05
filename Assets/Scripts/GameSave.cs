using System;
using System.Collections.Generic;
using GraphCreator;
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
}

[Serializable]
public struct LevelStateSave
{
    public Graph Graph;
    public List<CharacterState> CharacterSaves;
}

[Serializable]
public class GameSave
{
    // Player info
    public int PlayerLevel = 0;
    public Vector3 PlayerPosition;
    public Vector3 PlayerRotatin;

    public Dictionary<int, LevelStateSave> LevelStatesByBuildIndex = new Dictionary<int, LevelStateSave>();

    // Copy constructor
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
        Graph graph;
        LevelStateSave levelState;

        foreach (LevelStateByBuildIndex levelStateByBuildIndex in gameState.LevelStatesByBuildIndex)
        {
            // Copy all vertices and edges
            vertices = new Vertex[levelStateByBuildIndex.LevelState.Graph.Vertices.Length];
            levelStateByBuildIndex.LevelState.Graph.Vertices.CopyTo(vertices, 0);
            edges = new Edge[levelStateByBuildIndex.LevelState.Graph.Edges.Length];
            levelStateByBuildIndex.LevelState.Graph.Edges.CopyTo(edges, 0);
            
            // Copy the graph
            graph = ScriptableObject.CreateInstance<Graph>();
            graph.Initialize(vertices, edges);

            // Convert levelStateCharacters to characterSaves
            levelStateCharacters = levelStateByBuildIndex.LevelState.Characters;
            characterSaves = new CharacterState[levelStateCharacters.Length];

            for (int i = 0; i < levelStateCharacters.Length; i++)
            {
                characterSaves[i] = new CharacterState
                {
                    CurrentVertex = levelStateCharacters[i].Vertex,
                    NextVertex = -1,
                    Progress = .0f,
                    Position = vertices[levelStateCharacters[i].Vertex].Position,
                    Rotation = Vector3.zero,
                    Settings = levelStateCharacters[i].Settings
                };
            }

            // Add a new LevelStateSave to the dictionary
            levelState = new LevelStateSave
            {
                Graph = graph,
                CharacterSaves = new List<CharacterState>(characterSaves)
            };

            LevelStatesByBuildIndex.Add(levelStateByBuildIndex.BuildIndex, levelState);
        }
    }
}
