using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using UnityEngine.AI;

[TaskCategory("AI Simulation")]
public class MoveToVertex : Action
{
    public SharedGraph LevelGraph;
    public SharedInt Vertex;
    public float _pathRefreshRate = .2f;
    public float _stopDistance = 3.0f;
    public float _minSlowDownDistance = 7.0f;
    public bool _returnSuccessOnPartialPath = false;
    public bool _returnSuccessOnInvalidPath = false;
#if UNITY_EDITOR
    [Header("Debug")]
    public bool _debugPath;
#endif
    private float _lastUpdateTime;
    private float _timeSinceLastPathUpdate = .0f;
    private NavMeshPath _path;

    private CharacterMovement _movementScript;

    public override void OnAwake()
    {
        _path = new NavMeshPath();

        _movementScript = GetComponent<CharacterMovement>();
    }

    public override void OnStart()
    {
        _lastUpdateTime = Time.time;
        _timeSinceLastPathUpdate = .0f;
        NavMesh.CalculatePath(transform.position, LevelGraph.Value.Vertices[Vertex.Value].Position, NavMesh.AllAreas, _path);
    }

    public override TaskStatus OnUpdate()
    {
        _timeSinceLastPathUpdate += Time.time - _lastUpdateTime;
        _lastUpdateTime = Time.time;

        if (_timeSinceLastPathUpdate >= _pathRefreshRate)
        {
            NavMesh.CalculatePath(transform.position, LevelGraph.Value.Vertices[Vertex.Value].Position, NavMesh.AllAreas, _path);
            _timeSinceLastPathUpdate -= _pathRefreshRate;
        }
#if UNITY_EDITOR
        if (_debugPath)
        {
            for (int i = 0; i < _path.corners.Length - 1; i++)
            {
                Color pathColor = _path.status == NavMeshPathStatus.PathComplete ? Color.green : Color.red;
                Debug.DrawLine(_path.corners[i], _path.corners[i + 1], pathColor, _timeSinceLastPathUpdate);
            }
        }
#endif
        if (_path.status == NavMeshPathStatus.PathComplete)
        {
            Vector3 transformToCorner = _path.corners[_path.corners.Length - 1] - transform.position;
            transformToCorner.y = 0;
            // We use the squared version to avoid using sqrt. It makes the setting less accurate, but gives a nice natural ease in and out effect
            float distanceToTarget = transformToCorner.sqrMagnitude;

            Inputs inputs = new Inputs();

            if (distanceToTarget > _stopDistance * _stopDistance)
            {
                Vector3 moveDirection = _path.corners[1] - _path.corners[0];
                moveDirection.y = 0;
                moveDirection.Normalize();

                inputs.Horizontal = moveDirection.x;
                inputs.Vertical = moveDirection.z;

                float sqrMinSlowDownDistance = _minSlowDownDistance * _minSlowDownDistance; // This is squared

                // Slow down when close enough to the target
                if (distanceToTarget < sqrMinSlowDownDistance)
                {
                    float sqrStopDistance = _stopDistance * _stopDistance; // This is squared
                    float ratio = (distanceToTarget - sqrStopDistance) / (sqrMinSlowDownDistance - sqrStopDistance);

                    inputs.Horizontal *= ratio;
                    inputs.Vertical *= ratio;
                }

                _movementScript.UpdateInput(inputs);
                return TaskStatus.Running;
            }

            inputs.Horizontal = .0f;
            inputs.Vertical = .0f;

            _movementScript.UpdateInput(inputs);
            return TaskStatus.Success;
        }
        else if (_path.status == NavMeshPathStatus.PathPartial && _returnSuccessOnPartialPath)
        {
            return TaskStatus.Success;
        }
        else if (_path.status == NavMeshPathStatus.PathInvalid && _returnSuccessOnInvalidPath)
        {
            return TaskStatus.Success;
        }

        return TaskStatus.Failure;
    }

    public override void OnEnd()
    {
        _movementScript.UpdateInput(new Inputs());
    }
}
