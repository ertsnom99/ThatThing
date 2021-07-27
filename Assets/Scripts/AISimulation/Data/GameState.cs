using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct LevelStateByBuildIndex
{
    [SerializeField]
    private int _buildIndex;

    public int BuildIndex
    {
        get { return _buildIndex; }
        private set { _buildIndex = value; }
    }

    [SerializeField]
    private LevelState _levelState;

    public LevelState LevelState
    {
        get { return _levelState; }
        private set { _levelState = value; }
    }
}

[CreateAssetMenu(fileName = "GameState", menuName = "AI Simulation/States/Game State")]
public partial class GameState : ScriptableObject
{
    [SerializeField]
    private PlayerState _playerState;

    public PlayerState PlayerState
    {
        get { return _playerState; }
        private set { _playerState = value; }
    }

    [SerializeField]
    private LevelStateByBuildIndex[] _levelStatesByBuildIndex = new LevelStateByBuildIndex[0];

    public LevelStateByBuildIndex[] LevelStatesByBuildIndex
    {
        get { return _levelStatesByBuildIndex; }
        private set { _levelStatesByBuildIndex = value; }
    }
}
#if UNITY_EDITOR
public partial class GameState
{
    public bool IsValid(CharactersSettings charactersSettings)
    {
        if (!_playerState)
        {
            return false;
        }

        List<int> _buildIndexes = new List<int>();

        foreach (LevelStateByBuildIndex levelStateByBuildIndex in _levelStatesByBuildIndex)
        {
            if (!_buildIndexes.Contains(levelStateByBuildIndex.BuildIndex))
            {
                _buildIndexes.Add(levelStateByBuildIndex.BuildIndex);
            }
            else
            {
                return false;
            }

            if (!levelStateByBuildIndex.LevelState)
            {
                return false;
            }
            else if (!levelStateByBuildIndex.LevelState.IsValid(charactersSettings))
            {
                return false;
            }
        }

        return true;
    }
}
#endif
