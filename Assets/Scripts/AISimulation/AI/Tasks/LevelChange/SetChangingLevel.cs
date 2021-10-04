using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("AI Simulation")]
public class SetChangingLevel : Action
{
    public SharedCharacterState CharacterState;
    public bool ChangingLevel;

    public override TaskStatus OnUpdate()
    {
        CharacterState.Value.ChangingLevel = ChangingLevel;
        return TaskStatus.Success;
    }
}
