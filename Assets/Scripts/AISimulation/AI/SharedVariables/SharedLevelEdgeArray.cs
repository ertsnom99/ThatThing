using System;
using BehaviorDesigner.Runtime;

[Serializable]
public class SharedLevelEdgeArray : SharedVariable<LevelEdge[]>
{
    public static implicit operator SharedLevelEdgeArray(LevelEdge[] value)
    {
        return new SharedLevelEdgeArray { Value = value };
    }
}