using System;
using System.Collections.Generic;
using GraphCreator;
using UnityEngine;

[Serializable]
public class CharacterSave
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
    public List<CharacterSave> CharacterSaves;
}

[Serializable]
public class GameSave
{
    // Player info
    public int PlayerLevel = 0;
    public Vector3 PlayerPosition;
    public Vector3 PlayerRotatin;

    public Dictionary<int, LevelStateSave> LevelStatesByBuildIndex = new Dictionary<int, LevelStateSave>();

    // TODO: Copy constructor
    public GameSave(GameState gameState)
    {
        // Copy PlayerState
        PlayerLevel = gameState.PlayerState.Level;
        PlayerPosition = gameState.PlayerState.Position;
        PlayerRotatin = gameState.PlayerState.Rotation;
        
        // Copy LevelStates
        Vertex[] vertices;
        Edge[] edges;
        Graph graph;
        CharacterSave[] characterSaves;
        LevelStateSave levelStateSave;

        foreach (LevelStateByBuildIndex levelStateByBuildIndex in gameState.LevelStatesByBuildIndex)
        {
            // Copy all vertices and edges
            vertices = new Vertex[levelStateByBuildIndex.Graph.Vertices.Length];
            levelStateByBuildIndex.Graph.Vertices.CopyTo(vertices, 0);
            edges = new Edge[levelStateByBuildIndex.Graph.Edges.Length];
            levelStateByBuildIndex.Graph.Edges.CopyTo(edges, 0);
            
            // Copy the graph
            graph = ScriptableObject.CreateInstance<Graph>();
            graph.Initialize(vertices, edges);

            // Convert CharacterStates to characterSaves
            characterSaves = new CharacterSave[levelStateByBuildIndex.CharacterStates.Length];

            for (int i = 0; i < levelStateByBuildIndex.CharacterStates.Length; i++)
            {
                characterSaves[i] = new CharacterSave
                {
                    CurrentVertex = levelStateByBuildIndex.CharacterStates[i].Vertex,
                    NextVertex = -1,
                    Progress = .0f,
                    Position = vertices[levelStateByBuildIndex.CharacterStates[i].Vertex].Position,
                    Rotation = Vector3.zero,
                    Settings = levelStateByBuildIndex.CharacterStates[i].Settings
                };
            }

            // Create a new LevelStateSave in the dictionary
            levelStateSave = new LevelStateSave
            {
                Graph = graph,
                CharacterSaves = new List<CharacterSave>(characterSaves)
            };

            LevelStatesByBuildIndex.Add(levelStateByBuildIndex.BuildIndex, levelStateSave);
        }
    }
}
