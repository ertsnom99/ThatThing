using System;
using BehaviorDesigner.Runtime;

[Serializable]
public class SharedCharacterSave : SharedVariable<CharacterSave>
{
    public static implicit operator SharedCharacterSave(CharacterSave value)
    {
        return new SharedCharacterSave { Value = value };
    }
}
