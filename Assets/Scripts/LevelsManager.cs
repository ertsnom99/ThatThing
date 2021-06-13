using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using BehaviorDesigner.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

// This script requires thoses components and will be added if they aren't already there
[RequireComponent(typeof(BehaviorManager))]
public class LevelsManager : MonoBehaviour
{
    [SerializeField]
    private GameState _DebugGameState;

    private int _buildIndex = -1;

    private BehaviorManager _behaviorManager;

    private static GameSave _gameSave;

    public static GameSave GetGameSave()
    {
        return _gameSave;
    }

    // Takes a world position and finds the closest vertexA, the possible edge it's on (given by vertexB) and the progress on that edge.
    // Returns true if the conversion was successful. Even if the conversion is successful, vertexB could be -1, if the position
    // was considered exactly at vertexA. 
    public static bool ConvertPositionToGraph(LevelGraph graph, Vector3 position, LayerMask blockingMask, out int vertexA, out int vertexB, out float progress)
    {
        // Reset variables
        vertexA = -1;
        vertexB = -1;
        progress = .0f;

        Vector3 vertexToPos;
        float distanceToPosition;
        const float infinity = 99999;
        float smallestDistance = infinity;

        // Find closest Vertex
        RaycastHit hit;

        // TODO: Sort all vertices by distance (closest to fartess) in an other array?

        for (int i = 0; i < graph.Vertices.Length; i++)
        {
            vertexToPos = position - graph.Vertices[i].Position;
            distanceToPosition = vertexToPos.magnitude;

            // Skip check if the previously found closest vertex is already closer then this vertex (avoid unnecessary raycast)
            if (vertexA > -1 && distanceToPosition >= smallestDistance)
            {
                continue;
            }

            // TODO: Use sphere sweep instead?
            // Check if a raycast can reach vertex
            if (!Physics.Raycast(graph.Vertices[i].Position, vertexToPos.normalized, out hit, distanceToPosition, blockingMask))
            {
                vertexA = i;
                smallestDistance = distanceToPosition;
            }
        }

        // The position can't be converted to graph if no vertexA could be found
        if (vertexA == -1)
        {
            return false;
        }

        // Reset smallest distance
        smallestDistance = infinity;

        int secondVertex;
        Vector3 vertexAToSecond;
        Vector3 secondToVertexA;
        float dotA;
        float dotB;
        Vector3 projectedPosition;

        // Find closest edge connected to the first vertexA
        foreach (Edge edge in graph.Edges)
        {
            if (edge.VertexA == vertexA)
            {
                secondVertex = edge.VertexB;
            }
            else if (edge.VertexB == vertexA)
            {
                secondVertex = edge.VertexA;
            }
            else
            {
                continue;
            }

            // Find progress along the edge
            vertexAToSecond = graph.Vertices[secondVertex].Position - graph.Vertices[vertexA].Position;
            vertexToPos = position - graph.Vertices[vertexA].Position;
            dotA = Vector3.Dot(vertexAToSecond, vertexToPos);

            secondToVertexA = graph.Vertices[vertexA].Position - graph.Vertices[secondVertex].Position;
            vertexToPos = position - graph.Vertices[secondVertex].Position;
            dotB = Vector3.Dot(secondToVertexA, vertexToPos);

            // Check if position is aligned with the edge
            if (dotA * dotB > .0f)
            {
                // Calculate projection directly since we already calculated the dot product
                projectedPosition = (dotA / vertexAToSecond.sqrMagnitude) * vertexAToSecond;

                // Calculate the distance between the position and the projected position 
                distanceToPosition = (position - graph.Vertices[vertexA].Position - projectedPosition).sqrMagnitude;

                if (distanceToPosition < smallestDistance)
                {
                    vertexB = secondVertex;
                    progress = projectedPosition.magnitude;
                    smallestDistance = distanceToPosition;
                }
            }
        }

        return true;
    }

    private void Awake()
    {
        _behaviorManager = GetComponent<BehaviorManager>();

        // Store current scene buildIndex
        _buildIndex = SceneManager.GetActiveScene().buildIndex;

#if UNITY_EDITOR
        // Use debug GameState if _gameSave wasn't created yet
        if (_gameSave == null)
        {
            CreateGameSave(_DebugGameState);
            _gameSave.PlayerLevel = _buildIndex;
        }
#endif

        UpdateScene();
        CreateAIs();

        // TODO: Set player (position, rotation, etc.)
    }

    private void UpdateScene()
    {
        // TODO: Change state of scene based on LevelState
    }

    // TODO: how are AI managed?
    private void CreateAIs()
    {
        foreach (KeyValuePair<int, SerializableLevelState> LevelState in _gameSave.LevelStatesByBuildIndex)
        {
            if (_buildIndex == LevelState.Key)
            {
                foreach(Character character in LevelState.Value.Characters)
                {
                    // TODO: Create characters in scene
                    Debug.Log("In scene: " + character.Vertex);
                }
            }
            else
            {
                foreach (Character character in LevelState.Value.Characters)
                {
                    // TODO: Create characters in other scenes
                    Debug.Log("Scene " + LevelState.Key + " : " + character.Vertex);
                }
            }
        }
    }

    #region Static Methods
    public static void CreateGameSave(GameState gameState)
    {
        // Create a new instance with copy constructor
        _gameSave = new GameSave(gameState);
    }

    // Returns if _gameSave was saved
    public static bool SaveGameSave()
    {
        if (_gameSave == null)
        {
            return false;
        }

        BinaryFormatter bf = new BinaryFormatter();
        AddSurrogateSelector(bf);

        FileStream file = File.Create(Application.persistentDataPath + "/GameState.dat");

        bf.Serialize(file, _gameSave);
        file.Close();

        return true;
    }

    public static void LoadGameSave()
    {
        if (File.Exists(Application.persistentDataPath + "/GameState.dat"))
        {
            BinaryFormatter bf = new BinaryFormatter();
            AddSurrogateSelector(bf);

            FileStream file = File.Open(Application.persistentDataPath + "/GameState.dat", FileMode.Open);

            _gameSave = (GameSave)bf.Deserialize(file);
            file.Close();

            return;
        }
        
        Debug.LogError("There is no save data!");
    }

    private static void AddSurrogateSelector(BinaryFormatter bf)
    {
        // Tell the formatter how to serialize Vector3
        SurrogateSelector ss = new SurrogateSelector();
        Vector3SerializationSurrogate v3ss = new Vector3SerializationSurrogate();
        ss.AddSurrogate(typeof(Vector3), new StreamingContext(StreamingContextStates.All), v3ss);
        bf.SurrogateSelector = ss;
    }

    public static void DeleteGameSave()
    {
        if (File.Exists(Application.persistentDataPath + "/GameState.dat"))
        {
            File.Delete(Application.persistentDataPath + "/GameState.dat");
            return;
        }

        Debug.LogError("No save data to delete.");
    }
    #endregion
}
