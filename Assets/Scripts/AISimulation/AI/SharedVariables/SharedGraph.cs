using System;
using BehaviorDesigner.Runtime;
using GraphCreator;

[Serializable]
public class SharedGraph : SharedVariable<Graph>
{
    public static implicit operator SharedGraph(Graph value)
    {
        return new SharedGraph { Value = value };
    }
}
