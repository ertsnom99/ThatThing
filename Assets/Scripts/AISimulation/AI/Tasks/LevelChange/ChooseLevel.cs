using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("AI Simulation")]
public class ChooseLevel : Action
{
    public SharedIntArray Levels;
    public SharedCharacterState CharacterState;

    public override TaskStatus OnUpdate()
    {
        if (Levels.Value.Length < 2)
        {
            return TaskStatus.Failure;
        }

        CharacterState.Value.TargetLevel = Random.Range(0, Levels.Value.Length);

        if (Levels.Value[CharacterState.Value.TargetLevel] == CharacterState.Value.BuildIndex)
        {
            CharacterState.Value.TargetLevel++;
            CharacterState.Value.TargetLevel = CharacterState.Value.TargetLevel % Levels.Value.Length;
        }

        CharacterState.Value.TargetLevel = Levels.Value[CharacterState.Value.TargetLevel];

        return TaskStatus.Success;
    }
}
