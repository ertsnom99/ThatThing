using System;
using System.Collections.Generic;
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
    public int Id;
    // Index of RoomA in LevelGraph.Rooms
    public int RoomA;
    // Index of RoomB in LevelGraph.Rooms
    public int RoomB;
    public int Cost;
    public bool Traversable;
    public ConnectionType Type;
}

[Serializable]
public struct LevelGraph
{
    public Room[] Rooms;
    public Connection[] Connections;

    [HideInInspector]
    public int RoomIdCount;
    [HideInInspector]
    public int ConnectionIdCount;

    public LevelGraph(Room[] rooms, Connection[] connections)
    {
        Rooms = rooms;
        Connections = connections;
        RoomIdCount = 0;
        ConnectionIdCount = 0;
    }
}

[Serializable]
public struct Character
{
    // Index of the room
    public int Room;
    public Vector3 Position;
    public Vector3 Rotation;
}

[CreateAssetMenu(fileName = "LevelState", menuName = "Game State/Level State")]
public class LevelState : ScriptableObject
{
    [SerializeField]
    private LevelGraph _graph;

    [SerializeField]
    private Character[] _characters;

    #region Methods for the editor window
    public void Initialize()
    {
        _graph.Rooms = new Room[0];
        _graph.Connections = new Connection[0];
        _characters = new Character[0];
    }

    public void AddRoom(Transform transform = null)
    {
        Room newRoom = new Room();
        newRoom.Id = GenerateUniqueRoomId();

        if (transform)
        {
            newRoom.Position = transform.position;
        }

        List<Room> tempRooms = new List<Room>(_graph.Rooms);
        tempRooms.Add(newRoom);

        _graph.Rooms = tempRooms.ToArray();
    }

    private int GenerateUniqueRoomId()
    {
        int newId = _graph.RoomIdCount;
        _graph.RoomIdCount++;

        return newId;
    }

    private int GenerateUniqueConnectionId()
    {
        int newId = _graph.ConnectionIdCount;
        _graph.ConnectionIdCount++;

        return newId;
    }

    public void RemoveRoom(int index)
    {
        for (int i = _graph.Connections.Length; i > 0; i--)
        {
            // Remove any connections that uses the removed room
            if (_graph.Connections[i - 1].RoomA == index || _graph.Connections[i - 1].RoomB == index)
            {
                RemoveConnection(i - 1);
                continue;
            }

            // Fix indexes
            if (_graph.Connections[i - 1].RoomA > index)
            {
                _graph.Connections[i - 1].RoomA -= 1;
            }

            if (_graph.Connections[i - 1].RoomB > index)
            {
                _graph.Connections[i - 1].RoomB -= 1;
            }
        }

        for (int i = _characters.Length; i > 0; i--)
        {
            // Remove any character that uses the removed room
            if (_characters[i - 1].Room == index)
            {
                RemoveCharacter(i - 1);
                continue;
            }

            // Fix indexes
            if (_characters[i - 1].Room > index)
            {
                _characters[i - 1].Room -= 1;
            }
        }

        List<Room> tempRooms = new List<Room>(_graph.Rooms);
        tempRooms.Remove(_graph.Rooms[index]);

        _graph.Rooms = tempRooms.ToArray();
    }

    public List<string> GetAllRoomIds()
    {
        List<string> idList = new List<string>();

        foreach (Room room in _graph.Rooms)
        {
            idList.Add(room.Id.ToString());
        }

        return idList;
    }

    public void AddConnection(int roomA, int roomB)
    {
        Connection newConnection = new Connection();
        newConnection.Id = GenerateUniqueConnectionId();
        newConnection.RoomA = roomA;
        newConnection.RoomB = roomB;
        newConnection.Cost = 1;
        newConnection.Traversable = true;

        List<Connection> tempConnections = new List<Connection>(_graph.Connections);
        tempConnections.Add(newConnection);

        _graph.Connections = tempConnections.ToArray();
    }

    public void RemoveConnection(int index)
    {
        List<Connection> tempConnections = new List<Connection>(_graph.Connections);
        tempConnections.Remove(_graph.Connections[index]);

        _graph.Connections = tempConnections.ToArray();
    }

    public void AddCharacter(int room)
    {
        Character newCharacter = new Character();
        newCharacter.Room = room;

        List<Character> tempCharacters = new List<Character>(_characters);
        tempCharacters.Add(newCharacter);

        _characters = tempCharacters.ToArray();
    }

    public void RemoveCharacter(int index)
    {
        List<Character> tempCharacters = new List<Character>(_characters);
        tempCharacters.Remove(_characters[index]);

        _characters = tempCharacters.ToArray();
    }
    #endregion

    // Returns a COPY of the array of rooms
    public Room[] GetRooms()
    {
        Room[] roomsCopy = new Room[_graph.Rooms.Length];
        _graph.Rooms.CopyTo(roomsCopy, 0);
        return roomsCopy;
    }

    public int GetRoomsLength()
    {
        return _graph.Rooms.Length;
    }

    // Returns a COPY of the array of connections
    public Connection[] GetConnections()
    {
        Connection[] connectionsCopy = new Connection[_graph.Connections.Length];
        _graph.Connections.CopyTo(connectionsCopy, 0);
        return connectionsCopy;
    }

    public int GetConnectionsLength()
    {
        return _graph.Connections.Length;
    }

    // Returns a COPY of the array of characters
    public Character[] GetCharacters()
    {
        Character[] charactersCopy = new Character[_characters.Length];
        _characters.CopyTo(charactersCopy, 0);
        return charactersCopy;
    }

    public int GetCharactersLength()
    {
        return _characters.Length;
    }
}
