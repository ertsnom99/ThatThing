using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using BehaviorDesigner.Runtime;
using GraphCreator;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class SimulationManager : MonoSingleton<SimulationManager>
{
    private int _buildIndex = -1;

    private Dictionary<int, BehaviorTree> _characterBehaviorsById = new Dictionary<int, BehaviorTree>();
    private Dictionary<int, BehaviorTree> _simplifiedCharacterBehaviorsById = new Dictionary<int, BehaviorTree>();
    private float _timeSinceLastTick = .0f;

    private const string _levelGraphVariableName = "LevelGraph";
    private const string _characterStateVariableName = "CharacterState";

    [SerializeField]
    private LayerMask _wallMask;

    private static SimulationSettings _simulationSettings;
    private static Dictionary<int, Graph> _levelGraphsByBuildIndex = new Dictionary<int, Graph>();
    private static List<CharacterState> _characters = new List<CharacterState>();
    private static LevelEdge[] _levelEdges = new LevelEdge[0];

    protected override void Awake()
    {
        base.Awake();

        // Store current scene buildIndex
        _buildIndex = SceneManager.GetActiveScene().buildIndex;
#if UNITY_EDITOR
        AwakeInUnityEditor();
#endif
        UpdateScene();
        CreateAIs();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        CreateDebugWindow();
#endif
    }

    private void UpdateScene()
    {
        // TODO: Change state of scene based on LevelState
    }

    private void CreateAIs()
    {
        // Prepare containers for the AIs
        Dictionary<int, GameObject> AIContainers = new Dictionary<int, GameObject>();

        foreach(int buildIndex in _levelGraphsByBuildIndex.Keys)
        {
            AIContainers.Add(buildIndex, new GameObject("level " + buildIndex));
            AIContainers[buildIndex].transform.parent = transform;
        }

        BehaviorTree AI;

        foreach(CharacterState character in _characters)
        {
            if (_buildIndex == character.BuildIndex)
            {
                // Create AIs
                AI = CreateAI(_simulationSettings.CharactersSettingsUsed.Settings[character.Settings].Prefab,
                              character.Position,
                              Quaternion.Euler(character.Rotation),
                              null,
                              _simulationSettings.CharactersSettingsUsed.Settings[character.Settings].PrefabBehavior,
                              _levelGraphsByBuildIndex[character.BuildIndex],
                              character);

                AI.gameObject.GetComponent<CharacterMovement>().SetMaxWalkSpeed(_simulationSettings.CharactersSettingsUsed.Settings[character.Settings].MaxWalkSpeed);
                _characterBehaviorsById.Add(character.ID, AI);
            }
            else
            {
                // Create simplified AIs
                AI = CreateAI(_simulationSettings.SimplifiedAIPrefab,
                              Vector3.zero,
                              Quaternion.identity,
                              AIContainers[character.BuildIndex].transform,
                              _simulationSettings.CharactersSettingsUsed.Settings[character.Settings].SimplifiedBehavior,
                              _levelGraphsByBuildIndex[character.BuildIndex],
                              character);

                AI.GetComponent<SimplifiedCharacterMovement>().SetSpeed(_simulationSettings.CharactersSettingsUsed.Settings[character.Settings].MaxWalkSpeed);
                _simplifiedCharacterBehaviorsById.Add(character.ID, AI);
            }
        }
    }

    private BehaviorTree CreateAI(GameObject prefab, Vector3 position, Quaternion rotation, Transform AIContainer, ExternalBehavior behavior, Graph levelGraph, CharacterState characterState)
    {
        GameObject AI = Instantiate(prefab, position, rotation, AIContainer);
        BehaviorTree behaviorTree = AI.GetComponent<BehaviorTree>();
        behaviorTree.ExternalBehavior = behavior;
        behaviorTree.SetVariableValue(_levelGraphVariableName, levelGraph);
        behaviorTree.SetVariableValue(_characterStateVariableName, characterState);

        return behaviorTree;
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!_simulationSettings)
        {
            return;
        }
#endif
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        UpdateDebugWindow();
#endif
        // Update AIs
        foreach (BehaviorTree behaviorTree in _characterBehaviorsById.Values)
        {
            BehaviorManager.instance.Tick(behaviorTree);
        }

        // Update simplified AIs
        _timeSinceLastTick += Time.deltaTime;

        while (_timeSinceLastTick >= _simulationSettings.SimplifiedAITickRate)
        {
            _timeSinceLastTick -= _simulationSettings.SimplifiedAITickRate;
            foreach (BehaviorTree behaviorTree in _simplifiedCharacterBehaviorsById.Values)
            {
                BehaviorManager.instance.Tick(behaviorTree);
            }
        }
    }

    // Update the CharacterState of all characters in the level
    public void UpdateCharactersState()
    {
        Graph graph = _levelGraphsByBuildIndex[_buildIndex];

        foreach(CharacterState character in _characters)
        {
            if (character.BuildIndex != _buildIndex)
            {
                continue;
            }

            int vertexA;
            int vertexB;
            float progress;

            if (graph.ConvertPositionToGraph(_characterBehaviorsById[character.ID].transform.position, _wallMask, out vertexA, out vertexB, out progress))
            {
                character.CurrentVertex = vertexA;
                character.NextVertex = vertexB;
                character.Progress = progress;

                if (vertexB > -1)
                {
                    Vector3 AtoB = graph.Vertices[vertexB].Position - graph.Vertices[vertexA].Position;
                    character.Position = Vector3.Lerp(graph.Vertices[vertexA].Position, graph.Vertices[vertexB].Position, progress / (AtoB).magnitude);
                    character.Rotation = Quaternion.LookRotation(AtoB, Vector3.up).eulerAngles;
                }
                else
                {
                    character.Position = graph.Vertices[vertexA].Position;
                    character.Rotation = Vector3.zero;
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            else
            {
                Debug.LogError("Couldn't find the position on the graph!");
            }
#endif
        }
    }

    public static int[] GetBuildIndexes()
    {
        List<int> buildIndexes = new List<int>();

        foreach (int buildIndex in _levelGraphsByBuildIndex.Keys)
        {
            buildIndexes.Add(buildIndex);
        }

        return buildIndexes.ToArray();
    }

    public static void Initialize(GameSave gameSave = null)
    {
        _simulationSettings = SimulationSettings.LoadFromResources();
#if UNITY_EDITOR
        if (!_simulationSettings)
        {
            Debug.LogError("No SimulationSettings found!");
            return;
        }

        string[] simualtionSettingsErrors;

        if (!_simulationSettings.IsValid(out simualtionSettingsErrors))
        {
            Debug.LogError("The SimulationSettings aren't valid!");
            return;
        }
#endif
        SetSimulationState(_simulationSettings.InitialSimulationState);

        // TODO: Consider if a gameSave is given in parameter
    }

    private static void SetSimulationState(SimulationState simulationState)
    {
        // Store graphs and characters
        _levelGraphsByBuildIndex.Clear();
        _characters.Clear();

        Vertex[] vertices;
        Edge[] edges;
        Graph graph;

        foreach (LevelStateByBuildIndex levelStateByBuildIndex in simulationState.LevelStatesByBuildIndex)
        {
            // Copy all vertices and edges
            vertices = new Vertex[levelStateByBuildIndex.Graph.Vertices.Length];
            levelStateByBuildIndex.Graph.Vertices.CopyTo(vertices, 0);
            edges = new Edge[levelStateByBuildIndex.Graph.Edges.Length];
            levelStateByBuildIndex.Graph.Edges.CopyTo(edges, 0);

            // Copy the graph
            graph = ScriptableObject.CreateInstance<Graph>();
            graph.Initialize(vertices, edges);

            // Add the graph in the dictionary
            _levelGraphsByBuildIndex.Add(levelStateByBuildIndex.BuildIndex, graph);

            // Store all characters in the level
            foreach(CharacterState characterState in levelStateByBuildIndex.CharacterStates)
            {
                _characters.Add(new CharacterState(characterState)
                {
                    BuildIndex = levelStateByBuildIndex.BuildIndex,
                    NextVertex = -1,
                    Progress = .0f,
                    Position = vertices[characterState.CurrentVertex].Position,
                    Rotation = Vector3.zero
                });
            }
        }

        // Store the levelEdges
        _levelEdges = new LevelEdge[simulationState.LevelEdges.Length];

        for (int i = 0; i < simulationState.LevelEdges.Length; i++)
        {
            int levelABuildIndex = simulationState.LevelStatesByBuildIndex[simulationState.LevelEdges[i].LevelA].BuildIndex;
            int levelBBuildIndex = simulationState.LevelStatesByBuildIndex[simulationState.LevelEdges[i].LevelB].BuildIndex;
            _levelEdges[i] = new LevelEdge(levelABuildIndex, levelBBuildIndex, simulationState.LevelEdges[i].Edge);
        }
    }

    #region GameSave Methods
    public static void LoadGameSave()
    {
        if (File.Exists(Application.persistentDataPath + "/SimulationState.dat"))
        {
            BinaryFormatter bf = new BinaryFormatter();
            AddSurrogateSelector(bf);

            FileStream file = File.Open(Application.persistentDataPath + "/SimulationState.dat", FileMode.Open);

            //_gameSave = (GameSave)bf.Deserialize(file);
            file.Close();
            
            // TODO: Apply game save / maybe load initial state

            return;
        }

        Debug.LogError("There is no save data!");
    }

    // Returns if _gameSave was saved
    public static bool SaveGameSave()
    {
        /*if (_gameSave == null)
        {
            return false;
        }*/

        BinaryFormatter bf = new BinaryFormatter();
        AddSurrogateSelector(bf);

        FileStream file = File.Create(Application.persistentDataPath + "/SimulationState.dat");

        //bf.Serialize(file, _gameSave);
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
        if (File.Exists(Application.persistentDataPath + "/SimulationState.dat"))
        {
            File.Delete(Application.persistentDataPath + "/SimulationState.dat");
            return;
        }

        Debug.LogError("No save data to delete.");
    }
    #endregion
}
#if UNITY_EDITOR
public partial class SimulationManager
{
    [Header("Debug Game")]
    [SerializeField]
    private bool _useDebugSimulationState;
    [SerializeField]
    private SimulationState _debugSimulationState;

    private void AwakeInUnityEditor()
    {
        if (_levelGraphsByBuildIndex.Count <= 0)
        {
            // Use debug
            if (_useDebugSimulationState)
            {
                if (_debugSimulationState == null)
                {
                    Debug.LogError("No debug SimulationState is set!");
                    return;
                }

                Initialize(_debugSimulationState);
            }
            else
            {
                Initialize();
            }
        }
    }

    private void Initialize(SimulationState initialState)
    {
        // Search for a valid SimulationSettings
        _simulationSettings = SimulationSettings.LoadFromResources();

        if (!_simulationSettings)
        {
            Debug.LogError("No SimulationSettings found!");
            return;
        }

        string[] simualtionSettingsErrors;

        if (!_simulationSettings.IsValid(out simualtionSettingsErrors))
        {
            Debug.LogError("The SimulationSettings aren't valid!");
            return;
        }

        string[] simualtionStateErrors;

        if (!initialState.IsValid(_simulationSettings.CharactersSettingsUsed, out simualtionStateErrors))
        {
            Debug.LogError("The given initialState isn't valid!");
            return;
        }

        SetSimulationState(initialState);
    }
}
#endif
#if DEVELOPMENT_BUILD || UNITY_EDITOR
public partial class SimulationManager
{
    [Header("Debug Window")]
    [SerializeField]
    private GameObject _windowPrefab;
    [SerializeField]
    private GameObject _parent;
    private DebugWindow _debugWindow;
    [SerializeField]
    private KeyCode _toggleKey;

    private void CreateDebugWindow()
    {
        if (!_windowPrefab || _levelGraphsByBuildIndex.Count <= 0)
        {
            return;
        }

        _debugWindow = Instantiate(_windowPrefab, _parent.transform).GetComponent<DebugWindow>();
        _debugWindow.Initialize(_levelGraphsByBuildIndex, _characters, _buildIndex);
    }

    private void UpdateDebugWindow()
    {
        // Toggle window
        if (Input.GetKeyDown(_toggleKey))
        {
            _debugWindow.gameObject.SetActive(!_debugWindow.gameObject.activeSelf);
            _debugWindow.SetContainerActive(_debugWindow.gameObject.activeSelf);
        }
    }
}
#endif