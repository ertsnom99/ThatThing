using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("AI Simulation")]
public class IsChangingLevel : Conditional
{
    public SharedCharacterState CharacterState;

    public override TaskStatus OnUpdate()
    {
        if (CharacterState.Value.ChangingLevel)
        {
            return TaskStatus.Success;
        }

        return TaskStatus.Failure;
    }
}
