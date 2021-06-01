using System;
using System.Collections.Generic;
using UnityEngine;

// TODO: do tests with struct instead of class
[Serializable]
public struct Room
{
    public int Id;
    public Vector3 Position;
}

public enum ConnectionType { Corridor, Door, Vent };

// TODO: do tests with struct instead of class
[Serializable]
public struct Connection
{
    public int Id;
    // Index of RoomA in _rooms
    public int RoomA;
    // Index of RoomB in _rooms
    public int RoomB;
    public int Cost;
    public bool Traversable;
    public ConnectionType Type;
}

[Serializable]
public class LevelGraph
{
    [SerializeField]
    private Room[] _rooms = new Room[0];

    [SerializeField]
    private Connection[] _connections = new Connection[0];

    [HideInInspector]
    [SerializeField]
    private int _roomIdCount = 0;
    [HideInInspector]
    [SerializeField]
    private int _connectionIdCount = 0;

    #region Methods for the editor window
    public void AddRoom(Transform transform = null)
    {
        Room newRoom = new Room();
        newRoom.Id = GenerateUniqueRoomId();

        if (transform)
        {
            newRoom.Position = transform.position;
        }

        List<Room> tempRooms = new List<Room>(_rooms);
        tempRooms.Add(newRoom);

        _rooms = tempRooms.ToArray();
    }

    private int GenerateUniqueRoomId()
    {
        int newId = _roomIdCount;
        _roomIdCount++;

        return newId;
    }

    private int GenerateUniqueConnectionId()
    {
        int newId = _connectionIdCount;
        _connectionIdCount++;

        return newId;
    }

    public void RemoveRoom(int index)
    {
        for (int i = _connections.Length; i > 0; i--)
        {
            // Remove any connections that uses the removed room
            if (_connections[i - 1].RoomA == index || _connections[i - 1].RoomB == index)
            {
                RemoveConnection(i - 1);
                continue;
            }

            // Fix indexes
            if (_connections[i - 1].RoomA > index)
            {
                _connections[i - 1].RoomA -= 1;
            }

            if (_connections[i - 1].RoomB > index)
            {
                _connections[i - 1].RoomB -= 1;
            }
        }

        List<Room> tempRooms = new List<Room>(_rooms);
        tempRooms.Remove(_rooms[index]);

        _rooms = tempRooms.ToArray();
    }

    public List<string> GetAllRoomIds()
    {
        List<string> idList = new List<string>();

        foreach(Room room in _rooms)
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

        List<Connection> tempConnections = new List<Connection>(_connections);
        tempConnections.Add(newConnection);

        _connections = tempConnections.ToArray();
    }

    public void RemoveConnection(int index)
    {
        List<Connection> tempConnections = new List<Connection>(_connections);
        tempConnections.Remove(_connections[index]);

        _connections = tempConnections.ToArray();
    }
    #endregion

    // Returns a COPY of the array of rooms
    public Room[] GetRooms()
    {
        Room[] roomsCopy = new Room[_rooms.Length];
        _rooms.CopyTo(roomsCopy, 0);
        return roomsCopy;
    }

    public int GetRoomsLength()
    {
        return _rooms.Length;
    }

    // Returns a COPY of the array of connections
    public Connection[] GetConnections()
    {
        Connection[] connectionsCopy = new Connection[_connections.Length];
        _connections.CopyTo(connectionsCopy, 0);
        return connectionsCopy;
    }

    public int GetConnectionsLength()
    {
        return _connections.Length;
    }

    // TODO: Method to create a adjacency matrix
}

// TODO: do tests with struct instead of class
[Serializable]
public class Character
{
    public int Room;
}

public class LevelState : ScriptableObject
{
    [SerializeField]
    private LevelGraph _graph = new LevelGraph();

    public LevelGraph Graph
    {
        get { return _graph; }
        private set { _graph = value; }
    }

    [SerializeField]
    private Character[] _characters = new Character[0];

    public Character[] Characters
    {
        get { return _characters; }
        private set { _characters = value; }
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
}
