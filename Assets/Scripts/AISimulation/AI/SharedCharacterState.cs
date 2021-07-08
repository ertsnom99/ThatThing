using System;
using BehaviorDesigner.Runtime;

[Serializable]
public class SharedCharacterState : SharedVariable<CharacterState>
{
    public static implicit operator SharedCharacterState(CharacterState value)
    {
        return new SharedCharacterState { Value = value };
    }
}
