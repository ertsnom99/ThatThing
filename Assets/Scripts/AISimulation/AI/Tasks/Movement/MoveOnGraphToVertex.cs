using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using GraphCreator;
using UnityEngine;

[TaskCategory("AI Simulation")]
public class MoveOnGraphToVertex : Action
{
    public SharedGraph LevelGraph;
    public SharedCharacterState CharacterState;
    public SharedInt TargetVertex;

    private PathSegment[] _path = new PathSegment[0];
    private List<PathSegment> _fixedPath = new List<PathSegment>();

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

        PositionOnGraph from = CharacterState.Value.PositionOnGraph;
        PositionOnGraph to;
        to.VertexA = TargetVertex.Value;
        to.VertexB = -1;
        to.Progress = 0;

        float distance = 0;

        // Fix CharacterState if the edge became not traversable
        if (from.VertexB > -1 && LevelGraph.Value.AdjMatrix[from.VertexA, from.VertexB] == -1)
        {
            distance = (LevelGraph.Value.Vertices[from.VertexB].Position - LevelGraph.Value.Vertices[from.VertexA].Position).magnitude;
            
            if (CharacterState.Value.PositionOnGraph.Progress / distance < .5f)
            {
                // Reverse current and next vertex and also the progress
                CharacterState.Value.PositionOnGraph.VertexA = from.VertexB;
                CharacterState.Value.PositionOnGraph.VertexB = from.VertexA;
                CharacterState.Value.PositionOnGraph.Progress = distance - from.Progress;
            }

            // Path most be calculate from the next vertex, because the edge, the character is on, is not traversable!
            from.VertexA = CharacterState.Value.PositionOnGraph.VertexB;
            from.VertexB = -1;
            from.Progress = 0;
        }

        if (LevelGraph.Value.CalculatePathWithDijkstra(from, to, out _path))
        {
            PathSegment pathSection;

            // Adjust the path if it wasn't calculated starting at current position (in case edge was not traversable)
            if (CharacterState.Value.PositionOnGraph.VertexA != from.VertexA)
            {
                _fixedPath.Clear();
                _fixedPath.InsertRange(0, _path);

                // Adjust distance
                float progress = CharacterState.Value.PositionOnGraph.Progress / distance;
                distance -= CharacterState.Value.PositionOnGraph.Progress;

                for (int i = 0; i < _fixedPath.Count; i++)
                {
                    pathSection.PositionOnGraph = _fixedPath[i].PositionOnGraph;
                    pathSection.Distance = _fixedPath[i].Distance + distance;
                    pathSection.Position = _fixedPath[i].Position;
                    _fixedPath[i] = pathSection;
                }
                
                // Add missing path section
                pathSection.PositionOnGraph = CharacterState.Value.PositionOnGraph;
                pathSection.Distance = 0;
                pathSection.Position = Vector3.Lerp(LevelGraph.Value.Vertices[CharacterState.Value.PositionOnGraph.VertexA].Position, LevelGraph.Value.Vertices[CharacterState.Value.PositionOnGraph.VertexB].Position, progress);
                _fixedPath.Insert(0, pathSection);

                _path = _fixedPath.ToArray();
            }

            _simplifiedMovement.MoveOnGraph(_path, CharacterState.Value);

            // Target reached
            if (CharacterState.Value.PositionOnGraph.VertexA == TargetVertex.Value)
            {
                return TaskStatus.Success;
            }

            return TaskStatus.Running;
        }

        return TaskStatus.Failure;
    }
}
