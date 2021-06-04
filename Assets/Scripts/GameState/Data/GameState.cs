using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct LevelStateByBuildIndex
{
    public int BuildIndex;
    public LevelState LevelState;
}

[CreateAssetMenu(fileName = "GameState", menuName = "Game State/Game State")]
public class GameState : ScriptableObject
{
    [SerializeField]
    private PlayerState _playerState;

    [SerializeField]
    private LevelStateByBuildIndex[] _levelStatesByBuildIndex = new LevelStateByBuildIndex[0];

    public bool IsValid()
    {
        if (!_playerState)
        {
            return false;
        }

        List<int> _buildIndexes = new List<int>();

        foreach(LevelStateByBuildIndex levelStateByBuildIndex in _levelStatesByBuildIndex)
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
        }

        return true;
    }
}
