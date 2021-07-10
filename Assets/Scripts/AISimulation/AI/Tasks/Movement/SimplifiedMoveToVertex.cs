using System.Collections.Generic;
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
        if (!_simplifiedMovement || TargetVertex.Value < 0)
        {
            return TaskStatus.Failure;
        }

        int from = CharacterState.Value.CurrentVertex;
        int next = CharacterState.Value.NextVertex;
        float distance;

        // Fix CharacterState if the edge became not traversable
        if (next >= 0 && LevelGraph.Value.AdjMatrix[from, next] < 0)
        {
            distance = (LevelGraph.Value.Vertices[next].Position - LevelGraph.Value.Vertices[from].Position).magnitude;
            
            if (CharacterState.Value.Progress / distance < .5f)
            {
                // Reverse current and next vertex and also the progress
                CharacterState.Value.CurrentVertex = CharacterState.Value.NextVertex;
                CharacterState.Value.NextVertex = from;
                CharacterState.Value.Progress = distance - CharacterState.Value.Progress;
            }

            // Path most be calculate from the next vertex, because the edge, the character is on, is not traversable!
            from = CharacterState.Value.NextVertex;
        }

        if (LevelGraph.Value.CalculatePath(from, TargetVertex.Value, out path))
        {
            // Adjust the path if necessary
            // Path wasn't calculated starting at CurrentVertex (in case edge was not traversable)
            if (CharacterState.Value.CurrentVertex != from)
            {
                List<int> fixedPath = new List<int>(path);
                fixedPath.Insert(0, CharacterState.Value.CurrentVertex);
                path = fixedPath.ToArray();
            }
            // Character is going in the wrong direction
            else if (CharacterState.Value.NextVertex >= 0 && CharacterState.Value.NextVertex != path[1])
            {
                from = CharacterState.Value.CurrentVertex;
                next = CharacterState.Value.NextVertex;
                distance = (LevelGraph.Value.Vertices[next].Position - LevelGraph.Value.Vertices[from].Position).magnitude;

                // Reverse current and next vertex and also the progress
                CharacterState.Value.CurrentVertex = CharacterState.Value.NextVertex;
                CharacterState.Value.NextVertex = from;
                CharacterState.Value.Progress = distance - CharacterState.Value.Progress;

                List<int> fixedPath = new List<int>(path);
                fixedPath.Insert(0, CharacterState.Value.CurrentVertex);
                path = fixedPath.ToArray();
            }

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
