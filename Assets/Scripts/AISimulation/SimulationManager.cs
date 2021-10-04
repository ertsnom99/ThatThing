using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using BehaviorDesigner.Runtime;
using GraphCreator;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class SimulationManager : MonoSingleton<SimulationManager>
{
    private int _buildIndex = -1;

    private Dictionary<int, GameObject> _AIContainers = new Dictionary<int, GameObject>();
    private Dictionary<int, BehaviorTree> _characterBehaviorsById = new Dictionary<int, BehaviorTree>();
    private Dictionary<int, BehaviorTree> _simplifiedCharacterBehaviorsById = new Dictionary<int, BehaviorTree>();
    private float _timeSinceLastTick = .0f;

    private const string _levelIndexesVariableName = "LevelIndexes";
    private const string _levelEdgesVariableName = "LevelEdges";
    private const string _levelGraphVariableName = "LevelGraph";
    private const string _characterStateVariableName = "CharacterState";
    
    private const string _changingLevelEvent = "ChangingLevel";

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
        foreach(int buildIndex in _levelGraphsByBuildIndex.Keys)
        {
            _AIContainers.Add(buildIndex, new GameObject("level " + buildIndex));
            _AIContainers[buildIndex].transform.parent = transform;
        }

        // Create each AI
        foreach(CharacterState character in _characters)
        {
            CreateAI(character);
        }
    }

    private void CreateAI(CharacterState character)
    {
        GameObject AI;
        ExternalBehavior externalBehavior;
        Dictionary<int, BehaviorTree> behaviorsById;

        if (character.BuildIndex == _buildIndex)
        {
            AI = Instantiate(_simulationSettings.CharactersSettingsUsed.Settings[character.Settings].Prefab,
                             character.Position,
                             Quaternion.Euler(character.Rotation),
                             null);

            AI.GetComponent<CharacterMovement>().SetMaxWalkSpeed(_simulationSettings.CharactersSettingsUsed.Settings[character.Settings].MaxWalkSpeed);

            externalBehavior = _simulationSettings.CharactersSettingsUsed.Settings[character.Settings].PrefabBehavior;
            behaviorsById = _characterBehaviorsById;
        }
        else
        {
            AI = Instantiate(_simulationSettings.SimplifiedAIPrefab,
                             Vector3.zero,
                             Quaternion.identity,
                             _AIContainers[character.BuildIndex].transform);

            AI.GetComponent<SimplifiedCharacterMovement>().SetSpeed(_simulationSettings.CharactersSettingsUsed.Settings[character.Settings].MaxWalkSpeed);

            externalBehavior = _simulationSettings.CharactersSettingsUsed.Settings[character.Settings].SimplifiedBehavior;
            behaviorsById = _simplifiedCharacterBehaviorsById;

        }

        BehaviorTree behaviorTreeComponent = AI.GetComponent<BehaviorTree>();
        behaviorTreeComponent.ExternalBehavior = externalBehavior;
        behaviorTreeComponent.SetVariableValue(_characterStateVariableName, character);
        behaviorTreeComponent.SetVariableValue(_levelIndexesVariableName, _levelGraphsByBuildIndex.Keys.ToArray());
        behaviorTreeComponent.SetVariableValue(_levelEdgesVariableName, _levelEdges);
        behaviorTreeComponent.SetVariableValue(_levelGraphVariableName, _levelGraphsByBuildIndex[character.BuildIndex]);

        behaviorsById.Add(character.Id, behaviorTreeComponent);
        behaviorTreeComponent.RegisterEvent<object, object>(_changingLevelEvent, OnChangingLevel);
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
        // Must use for instead of foreach, since _characterBehaviorsById may change
        for (int i = _characterBehaviorsById.Values.Count; i > 0; i--)
        {
            BehaviorManager.instance.Tick(_characterBehaviorsById.ElementAt(i - 1).Value);
        }

        // Update simplified AIs
        _timeSinceLastTick += Time.deltaTime;
        
        // Must use for instead of foreach, since _characterBehaviorsById may change
        while (_timeSinceLastTick >= _simulationSettings.SimplifiedAITickRate)
        {
            _timeSinceLastTick -= _simulationSettings.SimplifiedAITickRate;
            for (int i = _simplifiedCharacterBehaviorsById.Values.Count; i > 0; i--)
            {
                BehaviorManager.instance.Tick(_simplifiedCharacterBehaviorsById.ElementAt(i - 1).Value);
            }
        }
    }

    private void OnChangingLevel(object characterState, object targetLevelEdge)
    {
        CharacterState state = (CharacterState)characterState;
        int levelEdge = (int)targetLevelEdge;
        int targetLevel = ((CharacterState)characterState).BuildIndex == _levelEdges[levelEdge].LevelA ? _levelEdges[levelEdge].LevelB : _levelEdges[levelEdge].LevelA;
        int targetVertex = targetLevel == _levelEdges[levelEdge].LevelB ? _levelEdges[levelEdge].Edge.VertexB : _levelEdges[levelEdge].Edge.VertexA;

        ChangeAILevel(state, targetLevel, targetVertex);
    }

    private void ChangeAILevel(CharacterState characterState, int targetLevel, int targetVertex)
    {
        // Already in the target level
        if (targetLevel == characterState.BuildIndex)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.LogError("Trying the change AI level, but he's already in the target level!");
#endif
            return;
        }

        int previousBuildIndex = characterState.BuildIndex;
        UpdateCharacterState(characterState, targetLevel, targetVertex, -1, 0);

        // The AI goes from a simplified level to an other
        if (previousBuildIndex != _buildIndex && characterState.BuildIndex != _buildIndex)
        {
            // Update parent
            _simplifiedCharacterBehaviorsById[characterState.Id].gameObject.transform.parent = _AIContainers[characterState.BuildIndex].transform;
            // Update levelGraph of the behavior tree
            _simplifiedCharacterBehaviorsById[characterState.Id].SetVariableValue(_levelGraphVariableName, _levelGraphsByBuildIndex[characterState.BuildIndex]);
            
            return;
        }

        // The AI goes from the current level to a simplified level or the opposite
        Dictionary<int, BehaviorTree> behaviorsById = characterState.BuildIndex != _buildIndex ? _characterBehaviorsById : _simplifiedCharacterBehaviorsById;
        behaviorsById[characterState.Id].UnregisterEvent<object, object>(_changingLevelEvent, OnChangingLevel);
        Destroy(behaviorsById[characterState.Id].gameObject);
        behaviorsById.Remove(characterState.Id);
        CreateAI(characterState);
    }

    // Update the CharacterState of all characters in the level
    public void UpdateCharacterStatesInCurrentLevel()
    {
        Graph graph = _levelGraphsByBuildIndex[_buildIndex];

        foreach(CharacterState character in _characters)
        {
            if (character.BuildIndex != _buildIndex)
            {
                continue;
            }

            PositionOnGraph positionOnGraph;

            if (graph.ConvertPositionToGraph(_characterBehaviorsById[character.Id].transform.position, _wallMask, out positionOnGraph))
            {
                UpdateCharacterState(character, _buildIndex, positionOnGraph.VertexA, positionOnGraph.VertexB, positionOnGraph.Progress);
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            else
            {
                Debug.LogError("Couldn't find the position on the graph!");
            }
#endif
        }
    }

    private void UpdateCharacterState(CharacterState characterState, int buildIndex, int vertexA, int vertexB, float progress)
    {
        characterState.BuildIndex = buildIndex;
        characterState.PositionOnGraph.VertexA = vertexA;
        characterState.PositionOnGraph.VertexB = vertexB;
        characterState.PositionOnGraph.Progress = progress;

        Graph graph = _levelGraphsByBuildIndex[characterState.BuildIndex];

        if (characterState.PositionOnGraph.VertexB > -1)
        {
            Vector3 AtoB = graph.Vertices[characterState.PositionOnGraph.VertexB].Position - graph.Vertices[characterState.PositionOnGraph.VertexA].Position;
            characterState.Position = Vector3.Lerp(graph.Vertices[characterState.PositionOnGraph.VertexA].Position, graph.Vertices[characterState.PositionOnGraph.VertexB].Position, characterState.PositionOnGraph.Progress / (AtoB).magnitude);
            characterState.Rotation = Quaternion.LookRotation(AtoB, Vector3.up).eulerAngles;
        }
        else
        {
            characterState.Position = graph.Vertices[characterState.PositionOnGraph.VertexA].Position;
            characterState.Rotation = Vector3.zero;
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
                CharacterState character = new CharacterState(characterState);
                character.BuildIndex = levelStateByBuildIndex.BuildIndex;
                character.PositionOnGraph.VertexB = -1;
                character.PositionOnGraph.Progress = .0f;
                character.Position = vertices[characterState.PositionOnGraph.VertexA].Position;
                character.Rotation = Vector3.zero;
                _characters.Add(character);
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
        if (!_windowPrefab || _levelGraphsByBuildIndex.Count <= 2)
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