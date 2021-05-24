using System;
using UnityEngine;

[Serializable]
public struct Room
{
    public int Id;
    public Vector3 Position;
}

public enum ConnectionType { Corridor, Door, Vent };

[Serializable]
public struct Connection
{
    // Index of RoomA in _rooms
    public int RoomA;
    // Index of RoomB in _rooms
    public int RoomB;
    public int Cost;
    public bool Traversable;
    public ConnectionType Type;
}

[CreateAssetMenu(fileName = "LevelLayout", menuName = "Level Layout/Level Layout")]
public class LevelLayout : ScriptableObject
{
    [SerializeField]
    private Room[] _rooms = new Room[0];
    [SerializeField]
    private Connection[] _connections = new Connection[0];

    // TODO: Method to create a adjacency matrix
}
