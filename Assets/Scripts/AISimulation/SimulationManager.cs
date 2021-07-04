using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using BehaviorDesigner.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SimulationManager : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField]
    private bool _useDebug;
    [SerializeField]
    private GameState _debugGameState;

    private int _buildIndex = -1;
    private BehaviorTree[] _AIs;
    private BehaviorTree[] _simplifiedAIs;
    private float _timeSinceLastTick = .0f;

    private static SimulationSettings _simulationSettings;
    private static GameSave _gameSave;

    private void Awake()
    {
        // Store current scene buildIndex
        _buildIndex = SceneManager.GetActiveScene().buildIndex;
#if UNITY_EDITOR
        // Use debug
        if (_useDebug)
        {
            if (_debugGameState == null)
            {
                Debug.LogError("No debug GameState is set!");
                return;
            }

            _gameSave = new GameSave(_debugGameState);
            _gameSave.PlayerLevel = _buildIndex;
        }
        
        // Search for a valid SimulationSettings
        if (!_simulationSettings)
        {
            _simulationSettings = SimulationSettings.LoadFromResources();

            if (!_simulationSettings)
            {
                Debug.LogError("No SimulationSettings found!");
                return;
            }
            else if (!_simulationSettings.IsValid())
            {
                Debug.LogError("The SimulationSettings aren't valid!");
                return;
            }
        }

        // Create a GameSave if none exist
        if (_gameSave == null)
        {
            _gameSave = new GameSave(_simulationSettings.InitialGameState);
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

    private void CreateAIs()
    {
        GameObject AIContainer;

        List<BehaviorTree> AIs = new List<BehaviorTree>();
        List<BehaviorTree> simplifiedAIs = new List<BehaviorTree>();

        foreach (KeyValuePair<int, LevelStateSave> levelState in _gameSave.LevelStatesByBuildIndex)
        {
            if (_buildIndex == levelState.Key)
            {
                // Create AIs
                foreach(CharacterSave character in levelState.Value.CharacterSaves)
                {
                    CreateAI(_simulationSettings.CharactersSettingsUsed.Settings[character.Settings].Prefab,
                             character.Position,
                             Quaternion.Euler(character.Rotation),
                             null,
                             _simulationSettings.CharactersSettingsUsed.Settings[character.Settings].PrefabBehavior,
                             AIs);
                }
            }
            else
            {
                AIContainer = new GameObject("level " + levelState.Key);
                AIContainer.transform.parent = transform;

                // Create simplified AIs
                foreach (CharacterSave character in levelState.Value.CharacterSaves)
                {
                    CreateAI(_simulationSettings.SimplifiedAIPrefab,
                             Vector3.zero, 
                             Quaternion.identity, 
                             AIContainer.transform,
                             _simulationSettings.CharactersSettingsUsed.Settings[character.Settings].SimplifiedBehavior,
                             simplifiedAIs);
                }
            }
        }

        _AIs = AIs.ToArray();
        _simplifiedAIs = simplifiedAIs.ToArray();
    }

    private void CreateAI(GameObject prefab, Vector3 position, Quaternion rotation, Transform AIContainer, ExternalBehavior behavior, List<BehaviorTree> behaviorTreeList)
    {
        GameObject AI = Instantiate(prefab, position, rotation, AIContainer);
        BehaviorTree behaviorTree = AI.GetComponent<BehaviorTree>();
        behaviorTree.ExternalBehavior = behavior;
        behaviorTreeList.Add(behaviorTree);
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (_AIs == null || !_simulationSettings || _simplifiedAIs == null)
        {
            return;
        }
#endif
        // Update AIs
        foreach (BehaviorTree AI in _AIs)
        {
            BehaviorManager.instance.Tick(AI);
        }

        // Update simplified AIs
        _timeSinceLastTick += Time.deltaTime;

        while (_timeSinceLastTick >= _simulationSettings.SimplifiedAITickRate)
        {
            _timeSinceLastTick -= _simulationSettings.SimplifiedAITickRate;

            foreach (BehaviorTree simplifiedAI in _simplifiedAIs)
            {
                BehaviorManager.instance.Tick(simplifiedAI);
            }
        }
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

        int[] indexes = new int[graph.Vertices.Length];
        float[] distances = new float[graph.Vertices.Length];

        for (int i = 0; i < graph.Vertices.Length; i++)
        {
            indexes[i] = i;
            distances[i] = (position - graph.Vertices[i].Position).magnitude;
        }

        // Sort all vertices by distance
        QuickSort.QuickSortAlignedArrays(distances, indexes, 0, distances.Length - 1);

        // Find closest Vertex
        RaycastHit hit;

        for (int i = 0; i < indexes.Length; i++)
        {
            // TODO: Use sphere sweep instead?
            // Check if a raycast can reach vertex
            if (!Physics.Raycast(graph.Vertices[indexes[i]].Position, (position - graph.Vertices[indexes[i]].Position).normalized, out hit, distances[i], blockingMask))
            {
                vertexA = indexes[i];
                break;
            }
        }

        // The position can't be converted to graph if no vertexA could be found
        if (vertexA == -1)
        {
            return false;
        }

        int secondVertex;
        Vector3 vertexToPos;
        Vector3 vertexAToSecond;
        Vector3 secondToVertexA;
        float dotA;
        float dotB;
        Vector3 projectedPosition;

        float distanceToPosition;
        const float infinity = 99999;
        float smallestDistance = infinity;

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

    public static void SetSimulationSettings(SimulationSettings simulationSettings)
    {
        _simulationSettings = simulationSettings;
    }

    #region GameSave Methods
    public static GameSave GetGameSave()
    {
        return _gameSave;
    }

    public static void SetGameSave(GameSave gameSave)
    {
        _gameSave = gameSave;
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
