using System;
using System.Collections.Generic;
using UnityEngine;

// TODO: do tests with struct instead of class
[Serializable]
public class Room
{
    public int Id;
    public Vector3 Position;
}

public enum ConnectionType { Corridor, Door, Vent };

// TODO: do tests with struct instead of class
[Serializable]
public class Connection
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

[CreateAssetMenu(fileName = "LevelLayout", menuName = "Level Layout/Level Layout")]
public class LevelLayout : ScriptableObject
{
    [SerializeField]
    private Room[] _rooms = new Room[0];

    public Room[] Rooms
    {
        get { return _rooms; }
        private set { _rooms = value; }
    }

    [SerializeField]
    private Connection[] _connections = new Connection[0];

    public Connection[] Connections
    {
        get { return _connections; }
        private set { _connections = value; }
    }

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
        List<Room> tempRooms = new List<Room>(_rooms);
        tempRooms.Remove(_rooms[index]);

        _rooms = tempRooms.ToArray();
    }

    public List<string> GetAllRoomIds()
    {
        List<string> idList = new List<string>();

        foreach(Room room in Rooms)
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


    // TODO: Method to create a adjacency matrix
}
