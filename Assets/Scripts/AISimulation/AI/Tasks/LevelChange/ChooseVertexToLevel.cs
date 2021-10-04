using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using GraphCreator;
using UnityEngine;

[TaskCategory("AI Simulation")]
public class ChooseVertexToLevel : Action
{
    public SharedLevelEdgeArray LevelEdges;
    public SharedCharacterState CharacterState;
    public SharedInt SelectedVertex;
    public SharedInt SelectedLevelEdge;

    private int[,] _adjMatrix;

    private int[] _distances;
    private int[] _parents;
    private List<int> _levelIndexes = new List<int>();

    private void Initialize()
    {
        if (LevelEdges.Value != null)
        {
            int levelCount = 0;

            foreach(LevelEdge levelEdge in LevelEdges.Value)
            {
                if (levelEdge.LevelA > levelCount)
                {
                    levelCount = levelEdge.LevelA;
                }

                if (levelEdge.LevelB > levelCount)
                {
                    levelCount = levelEdge.LevelB;
                }
            }

            levelCount++;

            _adjMatrix = new int[levelCount, levelCount];

            for (int i = 0; i < levelCount; i++)
            {
                for (int j = 0; j < levelCount; j++)
                {
                    _adjMatrix[i, j] = -1;
                }
            }

            for (int i = 0; i < LevelEdges.Value.Length; i++)
            {
                int distance = LevelEdges.Value[i].Edge.Traversable ? 1 : -1;

                if (distance == -1)
                {
                    continue;
                }

                switch (LevelEdges.Value[i].Edge.Direction)
                {
                    case EdgeDirection.Bidirectional:
                        _adjMatrix[LevelEdges.Value[i].LevelA, LevelEdges.Value[i].LevelB] = distance;
                        _adjMatrix[LevelEdges.Value[i].LevelB, LevelEdges.Value[i].LevelA] = distance;
                        break;
                    case EdgeDirection.AtoB:
                        _adjMatrix[LevelEdges.Value[i].LevelA, LevelEdges.Value[i].LevelB] = distance;
                        break;
                    case EdgeDirection.BtoA:
                        _adjMatrix[LevelEdges.Value[i].LevelB, LevelEdges.Value[i].LevelA] = distance;
                        break;
                }
            }

            _distances = new int[levelCount];
            _parents = new int[levelCount];
        }
    }

    public override TaskStatus OnUpdate()
    {
        if (_adjMatrix == null)
        {
            Initialize();

            if (_adjMatrix == null)
            {
                return TaskStatus.Failure;
            }
        }

        _levelIndexes.Clear();
        const int infiniteDistance = 999999;

        for (int i = 0; i < _adjMatrix.GetLength(0); i++)
        {
            _distances[i] = infiniteDistance;
            _parents[i] = -1;
            _levelIndexes.Add(i);
        }

        _distances[CharacterState.Value.BuildIndex] = 0;

        // Calculate shortest distances for all levels
        while (_levelIndexes.Count > 0)
        {
            // Find the closest level to source
            int levelIndex = _levelIndexes[0];

            for (int i = 1; i < _levelIndexes.Count; i++)
            {
                if (_distances[_levelIndexes[i]] < _distances[levelIndex])
                {
                    levelIndex = _levelIndexes[i];
                }
            }

            // Remove closest level
            _levelIndexes.Remove(levelIndex);

            // Set shortest distance for all neighbors of the closest level
            for (int i = 0; i < _adjMatrix.GetLength(0); i++)
            {
                // Skip level i if not accessible or already removed from _levelIndexes
                if (_adjMatrix[levelIndex, i] < 0 || !_levelIndexes.Contains(i))
                {
                    continue;
                }

                // Update distance if shorter path found
                int alt = _distances[levelIndex] + _adjMatrix[levelIndex, i];

                if (alt < _distances[i])
                {
                    _distances[i] = alt;
                    _parents[i] = levelIndex;
                }
            }
        }

        int currentLevel = CharacterState.Value.TargetLevel;

        // Path doesn't exist if currentLevel can't be reached
        if (_distances[currentLevel] == infiniteDistance)
        {
            return TaskStatus.Failure;
        }

        // Start from the current level and find what is the next level that needs to be reached
        while (_parents[currentLevel] != -1)
        {
            if (_parents[currentLevel] == CharacterState.Value.BuildIndex)
            {
                for(int i = 0; i < LevelEdges.Value.Length; i++)
                {
                    if (LevelEdges.Value[i].LevelB == currentLevel && LevelEdges.Value[i].LevelA == _parents[currentLevel])
                    {
                        SelectedVertex.Value = LevelEdges.Value[i].Edge.VertexA;
                        SelectedLevelEdge.Value = i;
                        break;
                    }

                    if (LevelEdges.Value[i].LevelA == currentLevel && LevelEdges.Value[i].LevelB == _parents[currentLevel])
                    {
                        SelectedVertex.Value = LevelEdges.Value[i].Edge.VertexB;
                        SelectedLevelEdge.Value = i;
                        break;
                    }
                }

                break;
            }

            currentLevel = _parents[currentLevel];
        }

        return TaskStatus.Success;
    }
}
