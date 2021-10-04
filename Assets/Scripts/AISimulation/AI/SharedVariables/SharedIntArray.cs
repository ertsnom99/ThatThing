using System;
using BehaviorDesigner.Runtime;

[Serializable]
public class SharedIntArray : SharedVariable<int[]>
{
    public static implicit operator SharedIntArray(int[] value)
    {
        return new SharedIntArray { Value = value };
    }
}
