using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct SerializableLevelState
{
    public LevelGraph Graph;
    public List<Room> Rooms;
    public List<Character> Characters;

    public SerializableLevelState(LevelGraph graph, Room[] rooms, Character[] characters)
    {
        Graph = graph;
        Rooms = new List<Room>(rooms);
        Characters = new List<Character>(characters);
    }
}

[Serializable]
public class GameSave
{
    // Player info
    public int PlayerLevel = 0;
    public Vector3 PlayerPosition;
    public Vector3 PlayerRotatin;

    public Dictionary<int, SerializableLevelState> LevelStatesByBuildIndex = new Dictionary<int, SerializableLevelState>();

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
        Room[] rooms;
        Character[] characters;
        LevelGraph graph;
        SerializableLevelState levelState;

        foreach (LevelStateByBuildIndex levelStateByBuildIndex in gameState.LevelStatesByBuildIndex)
        {
            vertices = levelStateByBuildIndex.LevelState.GetVertices();
            edges = levelStateByBuildIndex.LevelState.GetEdges();
            rooms = levelStateByBuildIndex.LevelState.GetRooms();
            characters = levelStateByBuildIndex.LevelState.GetCharacters();

            // Set characters position to vertex Position
            for (int i = 0; i < characters.Length; i++)
            {
                characters[i].Position = vertices[characters[i].Vertex].Position;
            }

            graph = new LevelGraph(vertices, edges);
            levelState = new SerializableLevelState(graph, rooms, characters);

            LevelStatesByBuildIndex.Add(levelStateByBuildIndex.BuildIndex, levelState);
        }
    }
}
