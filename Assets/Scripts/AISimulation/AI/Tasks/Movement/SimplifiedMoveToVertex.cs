using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using GraphCreator;

[TaskCategory("AI Simulation")]
public class SimplifiedMoveToVertex : Action
{
    public SharedGraph LevelGraph;
    public SharedCharacterSave CharacterSave;
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

        int from = CharacterSave.Value.CurrentVertex;
        int next = CharacterSave.Value.NextVertex;
        float distance = 0;

        // Fix CharacterState if the edge became not traversable
        if (next >= 0 && LevelGraph.Value.AdjMatrix[from, next] < 0)
        {
            distance = (LevelGraph.Value.Vertices[next].Position - LevelGraph.Value.Vertices[from].Position).magnitude;
            
            if (CharacterSave.Value.Progress / distance < .5f)
            {
                // Reverse current and next vertex and also the progress
                CharacterSave.Value.CurrentVertex = CharacterSave.Value.NextVertex;
                CharacterSave.Value.NextVertex = from;
                CharacterSave.Value.Progress = distance - CharacterSave.Value.Progress;
            }

            // Path most be calculate from the next vertex, because the edge, the character is on, is not traversable!
            from = CharacterSave.Value.NextVertex;
        }

        if (LevelGraph.Value.CalculatePathWithDijkstra(from, TargetVertex.Value, out _path))
        {
            PathSegment pathSection;

            // Adjust the path if necessary
            // Path wasn't calculated starting at CurrentVertex (in case edge was not traversable)
            if (CharacterSave.Value.CurrentVertex != from)
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
                pathSection.VertexIndex = CharacterSave.Value.CurrentVertex;
                pathSection.Distance = 0;
                pathSection.Position = LevelGraph.Value.Vertices[CharacterSave.Value.CurrentVertex].Position;
                _fixedPath.Insert(0, pathSection);

                _path = _fixedPath.ToArray();
            }
            // Character is going in the wrong direction (ex: TargetVertex changed)
            else if (CharacterSave.Value.NextVertex >= 0 && CharacterSave.Value.NextVertex != _path[1].VertexIndex)
            {
                from = CharacterSave.Value.CurrentVertex;
                next = CharacterSave.Value.NextVertex;
                distance = (LevelGraph.Value.Vertices[next].Position - LevelGraph.Value.Vertices[from].Position).magnitude;

                // Reverse current and next vertex and also the progress
                CharacterSave.Value.CurrentVertex = CharacterSave.Value.NextVertex;
                CharacterSave.Value.NextVertex = from;
                CharacterSave.Value.Progress = distance - CharacterSave.Value.Progress;

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
                pathSection.VertexIndex = CharacterSave.Value.CurrentVertex;
                pathSection.Distance = 0;
                pathSection.Position = LevelGraph.Value.Vertices[CharacterSave.Value.CurrentVertex].Position;
                _fixedPath.Insert(0, pathSection);

                _path = _fixedPath.ToArray();
            }

            _simplifiedMovement.MoveOnGraph(_path, CharacterSave.Value);

            // Target reached
            if (CharacterSave.Value.CurrentVertex == TargetVertex.Value)
            {
                return TaskStatus.Success;
            }

            return TaskStatus.Running;
        }

        return TaskStatus.Failure;
    }
}
