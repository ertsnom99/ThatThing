using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("AI Simulation")]
public class SimplifiedMoveToVertex : Action
{
    public SharedLevelGraph LevelGraph;
    public SharedCharacterState CharacterState;
    public SharedInt TargetVertex;

    private int[] path = new int[0];

    private SimplifiedCharacterMovement _simplifiedMovement;

    public override void OnAwake()
    {
        _simplifiedMovement = GetComponent<SimplifiedCharacterMovement>();
    }

    public override TaskStatus OnUpdate()
    {
        if (_simplifiedMovement && TargetVertex.Value >= 0 && LevelGraph.Value.CalculatePath(CharacterState.Value.CurrentVertex, TargetVertex.Value, out path))
        {
            _simplifiedMovement.MoveOnGraph(LevelGraph.Value, path, CharacterState.Value);

            // Target reached
            if (CharacterState.Value.CurrentVertex == TargetVertex.Value)
            {
                return TaskStatus.Success;
            }

            return TaskStatus.Running;
        }

        return TaskStatus.Failure;
    }
}
