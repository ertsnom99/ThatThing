using System;
using BehaviorDesigner.Runtime;

[Serializable]
public class SharedLevelGraph : SharedVariable<LevelGraph>
{
    public static implicit operator SharedLevelGraph(LevelGraph value)
    {
        return new SharedLevelGraph { Value = value };
    }
}
