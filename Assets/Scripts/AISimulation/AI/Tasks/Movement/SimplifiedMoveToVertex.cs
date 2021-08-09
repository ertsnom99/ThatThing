using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using GraphCreator;

[TaskCategory("AI Simulation")]
public class SimplifiedMoveToVertex : Action
{
    public SharedLevelGraph LevelGraph;
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

        int from = CharacterState.Value.CurrentVertex;
        int next = CharacterState.Value.NextVertex;
        float distance = 0;

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

        if (LevelGraph.Value.CalculatePathWithDijkstra(from, TargetVertex.Value, out _path))
        {
            PathSegment pathSection;

            // Adjust the path if necessary
            // Path wasn't calculated starting at CurrentVertex (in case edge was not traversable)
            if (CharacterState.Value.CurrentVertex != from)
            {
                _fixedPath.Clear();
                _fixedPath.InsertRange(0, _path);

                // Adjust distance                
                for (int i = 0; i < _fixedPath.Count; i++)
                {
                    pathSection.VertexIndex = _fixedPath[i].VertexIndex;
                    pathSection.Distance = _fixedPath[i].Distance + distance;
                    pathSection.Position = _fixedPath[i].Position;
                    _fixedPath[i] = pathSection;
                }
                
                // Add missing path section
                pathSection.VertexIndex = CharacterState.Value.CurrentVertex;
                pathSection.Distance = 0;
                pathSection.Position = LevelGraph.Value.Vertices[CharacterState.Value.CurrentVertex].Position;
                _fixedPath.Insert(0, pathSection);

                _path = _fixedPath.ToArray();
            }
            // Character is going in the wrong direction (ex: TargetVertex changed)
            else if (CharacterState.Value.NextVertex >= 0 && CharacterState.Value.NextVertex != _path[1].VertexIndex)
            {
                from = CharacterState.Value.CurrentVertex;
                next = CharacterState.Value.NextVertex;
                distance = (LevelGraph.Value.Vertices[next].Position - LevelGraph.Value.Vertices[from].Position).magnitude;

                // Reverse current and next vertex and also the progress
                CharacterState.Value.CurrentVertex = CharacterState.Value.NextVertex;
                CharacterState.Value.NextVertex = from;
                CharacterState.Value.Progress = distance - CharacterState.Value.Progress;

                _fixedPath.Clear();
                _fixedPath.InsertRange(0, _path);

                // Adjust distance                
                for (int i = 0; i < _fixedPath.Count; i++)
                {
                    pathSection.VertexIndex = _fixedPath[i].VertexIndex;
                    pathSection.Distance = _fixedPath[i].Distance + distance;
                    pathSection.Position = _fixedPath[i].Position;
                    _fixedPath[i] = pathSection;
                }

                // Add missing path section
                pathSection.VertexIndex = CharacterState.Value.CurrentVertex;
                pathSection.Distance = 0;
                pathSection.Position = LevelGraph.Value.Vertices[CharacterState.Value.CurrentVertex].Position;
                _fixedPath.Insert(0, pathSection);

                _path = _fixedPath.ToArray();
            }

            _simplifiedMovement.MoveOnGraph(_path, CharacterState.Value);

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
