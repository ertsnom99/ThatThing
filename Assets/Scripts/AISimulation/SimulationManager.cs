using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using BehaviorDesigner.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class SimulationManager : MonoBehaviour
{
    private int _buildIndex = -1;
    private BehaviorTree[] _AIs;
    private BehaviorTree[] _simplifiedAIs;
    private float _timeSinceLastTick = .0f;

    private static SimulationSettings _simulationSettings;
    private static GameSave _gameSave;

    private const string _levelGraphVariableName = "LevelGraph";
    private const string _characterStateVariableName = "CharacterState";

    private void Awake()
    {
        // Store current scene buildIndex
        _buildIndex = SceneManager.GetActiveScene().buildIndex;
#if UNITY_EDITOR
        AwakeInUnityEditor();
#endif
        UpdateScene();
        CreateAIs();

        // TODO: Set player (position, rotation, etc.)
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
        GameObject AIContainer;
        GameObject AI;

        List<BehaviorTree> AIs = new List<BehaviorTree>();
        List<BehaviorTree> simplifiedAIs = new List<BehaviorTree>();

        foreach (KeyValuePair<int, LevelStateSave> levelState in _gameSave.LevelStatesByBuildIndex)
        {
            if (_buildIndex == levelState.Key)
            {
                // Create AIs
                foreach(CharacterState character in levelState.Value.CharacterSaves)
                {
                    AI = CreateAI(_simulationSettings.CharactersSettingsUsed.Settings[character.Settings].Prefab,
                             character.Position,
                             Quaternion.Euler(character.Rotation),
                             null,
                             _simulationSettings.CharactersSettingsUsed.Settings[character.Settings].PrefabBehavior,
                             levelState.Value.Graph,
                             character,
                             AIs);

                    AI.GetComponent<CharacterMovement>().SetMaxWalkSpeed(_simulationSettings.CharactersSettingsUsed.Settings[character.Settings].MaxWalkSpeed);
                }
            }
            else
            {
                AIContainer = new GameObject("level " + levelState.Key);
                AIContainer.transform.parent = transform;

                // Create simplified AIs
                foreach (CharacterState character in levelState.Value.CharacterSaves)
                {
                    AI = CreateAI(_simulationSettings.SimplifiedAIPrefab,
                             Vector3.zero, 
                             Quaternion.identity, 
                             AIContainer.transform,
                             _simulationSettings.CharactersSettingsUsed.Settings[character.Settings].SimplifiedBehavior,
                             levelState.Value.Graph,
                             character, 
                             simplifiedAIs);

                    AI.GetComponent<SimplifiedCharacterMovement>().SetSpeed(_simulationSettings.CharactersSettingsUsed.Settings[character.Settings].MaxWalkSpeed);
                }
            }
        }

        _AIs = AIs.ToArray();
        _simplifiedAIs = simplifiedAIs.ToArray();
    }

    private GameObject CreateAI(GameObject prefab, Vector3 position, Quaternion rotation, Transform AIContainer, ExternalBehavior behavior, LevelGraph levelGraph, CharacterState characterState, List<BehaviorTree> behaviorTreeList)
    {
        GameObject AI = Instantiate(prefab, position, rotation, AIContainer);
        BehaviorTree behaviorTree = AI.GetComponent<BehaviorTree>();
        behaviorTree.ExternalBehavior = behavior;
        behaviorTree.SetVariableValue(_levelGraphVariableName, levelGraph);
        behaviorTree.SetVariableValue(_characterStateVariableName, characterState);
        behaviorTreeList.Add(behaviorTree);

        return AI;
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (_AIs == null || !_simulationSettings || _simplifiedAIs == null)
        {
            return;
        }
#endif
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        UpdateDebugWindow();
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
        
        foreach (KeyValuePair<int, LevelStateSave> levelState in _gameSave.LevelStatesByBuildIndex)
        {
            levelState.Value.Graph.GenerateAdjMatrix();
        }
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
            
            foreach (KeyValuePair<int, LevelStateSave> levelState in _gameSave.LevelStatesByBuildIndex)
            {
                levelState.Value.Graph.GenerateAdjMatrix();
            }

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
#if UNITY_EDITOR
public partial class SimulationManager
{
    [Header("Debug Game")]
    [SerializeField]
    private bool _useDebugGameState;
    [SerializeField]
    private GameState _debugGameState;

    private void AwakeInUnityEditor()
    {
        // Use debug
        if (_useDebugGameState)
        {
            if (_debugGameState == null)
            {
                Debug.LogError("No debug GameState is set!");
                return;
            }

            _gameSave = new GameSave(_debugGameState);
            _gameSave.PlayerLevel = _buildIndex;

            foreach (KeyValuePair<int, LevelStateSave> levelState in _gameSave.LevelStatesByBuildIndex)
            {
                levelState.Value.Graph.GenerateAdjMatrix();
            }
        }

        // Search for a valid SimulationSettings
        if (!_simulationSettings)
        {
            _simulationSettings = SimulationSettings.LoadFromAsset();

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

            foreach (KeyValuePair<int, LevelStateSave> levelState in _gameSave.LevelStatesByBuildIndex)
            {
                levelState.Value.Graph.GenerateAdjMatrix();
            }
        }
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
    private GameObject _window;
    [SerializeField]
    private KeyCode _toggleKey;

    private void CreateDebugWindow()
    {
        if (!_windowPrefab || _gameSave == null)
        {
            return;
        }

        _window = Instantiate(_windowPrefab, _parent.transform);
        _window.GetComponent<DebugWindow>().SetLevelStates(_gameSave.LevelStatesByBuildIndex, _buildIndex);
    }

    private void UpdateDebugWindow()
    {
        // Toggle window
        if (Input.GetKeyDown(_toggleKey))
        {
            _window.SetActive(!_window.activeSelf);
        }
    }
}
#endif