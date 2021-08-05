using System;
using BehaviorDesigner.Runtime;
using GraphCreator;

[Serializable]
public class SharedLevelGraph : SharedVariable<Graph>
{
    public static implicit operator SharedLevelGraph(Graph value)
    {
        return new SharedLevelGraph { Value = value };
    }
}
