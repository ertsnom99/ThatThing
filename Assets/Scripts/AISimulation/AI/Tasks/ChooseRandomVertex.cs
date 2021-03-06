using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("AI Simulation")]
public class ChooseRandomVertex : Action
{
    public SharedGraph LevelGraph;
    public SharedCharacterState CharacterState;
    public SharedInt Vertex;

    public override TaskStatus OnUpdate()
    {
        if (LevelGraph.Value.Vertices.Length <= 1)
        {
            return TaskStatus.Failure;
        }

        int randomVertex = Random.Range(0, LevelGraph.Value.Vertices.Length);

        if (randomVertex == CharacterState.Value.PositionOnGraph.VertexA)
        {
            randomVertex++;
            randomVertex %= LevelGraph.Value.Vertices.Length;
        }

        Vertex.Value = randomVertex;
        return TaskStatus.Success;
    }
}
