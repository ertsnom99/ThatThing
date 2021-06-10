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
                    Debug.Log("In scene: " + character.Room);
                }
            }
            else
            {
                foreach (Character character in LevelState.Value.Characters)
                {
                    // TODO: Create characters in other scenes
                    Debug.Log("Scene " + LevelState.Key + " : " + character.Room);
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
