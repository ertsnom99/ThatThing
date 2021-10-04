using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("AI Simulation")]
public class IsInTargetLevel : Conditional
{
    public SharedCharacterState CharacterState;

    public override TaskStatus OnUpdate()
    {
        if (CharacterState.Value.BuildIndex == CharacterState.Value.TargetLevel)
        {
            return TaskStatus.Success;
        }
        return TaskStatus.Failure;
    }
}
