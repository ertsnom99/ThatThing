using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct SerializableLevelState
{
    public LevelGraph Graph;
    public Character[] Characters;

    public SerializableLevelState(LevelGraph graph, Character[] characters)
    {
        Graph = graph;
        Characters = characters;
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
        Room[] rooms;
        Connection[] connections;
        Character[] characters;
        LevelGraph graph;
        SerializableLevelState levelState;

        foreach (LevelStateByBuildIndex levelStateByBuildIndex in gameState.LevelStatesByBuildIndex)
        {
            rooms = levelStateByBuildIndex.LevelState.GetRooms();
            connections = levelStateByBuildIndex.LevelState.GetConnections();
            characters = levelStateByBuildIndex.LevelState.GetCharacters();

            graph = new LevelGraph(rooms, connections);
            levelState = new SerializableLevelState(graph, characters);

            LevelStatesByBuildIndex.Add(levelStateByBuildIndex.BuildIndex, levelState);
        }
    }
}
