using System;
using System.Collections.Generic;
using UnityEngine;

// TODO: How and where is store the initial state of the player?
// TODO: Check if GameState saves correctly

/*[CreateAssetMenu(fileName = "PlayerInitialState", menuName = "GameState/PlayerInitialState")]
public class PlayerInitialState : ScriptableObject
{
    public int _playerLevel = 0;
    public Vector3 _playerPosition = Vector3.zero;
    public Vector3 _playerRotatin = Vector3.zero;
}*/

public class GameState : ScriptableObject, ISerializationCallbackReceiver
{
    private Dictionary<int, LevelState> _levelStates = new Dictionary<int, LevelState>();

    [SerializeField]
    private List<int> _keys = new List<int>();
    [SerializeField]
    private List<LevelState> _values = new List<LevelState>();

    [SerializeField]
    private int _playerLevel = 0;

    public int PlayerLevel
    {
        get { return _playerLevel; }
        private set { _playerLevel = value; }
    }

    [SerializeField]
    private Vector3 _playerPosition = Vector3.zero;

    public Vector3 PlayerPosition
    {
        get { return _playerPosition; }
        private set { _playerPosition = value; }
    }

    [SerializeField]
    private Vector3 _playerRotatin = Vector3.zero;

    public Vector3 PlayerRotatin
    {
        get { return _playerRotatin; }
        private set { _playerRotatin = value; }
    }

    public LevelState GetLevelState(int index)
    {
        if (_levelStates.ContainsKey(index))
        {
            return _levelStates[index];
        }

        return null;
    }

    public void AddLevelState(int index, LevelState levelState)
    {
        if (!_levelStates.ContainsKey(index))
        {
            _levelStates.Add(index, levelState);
        }
        else
        {
            _levelStates[index] = levelState;
        }
    }

    // Methods of the ISerializationCallbackReceiver interface
    public void OnBeforeSerialize()
    {
        _keys.Clear();
        _values.Clear();

        foreach (var kvp in _levelStates)
        {
            _keys.Add(kvp.Key);
            _values.Add(kvp.Value);
        }
    }

    public void OnAfterDeserialize()
    {
        _levelStates = new Dictionary<int, LevelState>();

        for (int i = 0; i != Math.Min(_keys.Count, _values.Count); i++)
        {
            _levelStates.Add(_keys[i], _values[i]);
        }
    }
}
