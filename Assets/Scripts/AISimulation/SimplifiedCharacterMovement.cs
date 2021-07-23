using UnityEngine;

public class SimplifiedCharacterMovement : MonoBehaviour
{
    private float _speed = 1.0f;
    private float _lastTickTime = 0;

    private void Awake()
    {
        _lastTickTime = Time.time;
    }

    public void SetSpeed(float speed)
    {
        _speed = speed;

        if (_speed < 0)
        {
            _speed = 0;
        }
    }

    public void MoveOnGraph(LevelGraph levelGraph, int[] path, CharacterState characterState)
    {
        if (path.Length < 2)
        {
            return;
        }

        int pathVertexIndex = 0;
        float distanceToVertex;
        float distanceToTravel = _speed * (Time.time - _lastTickTime);
        _lastTickTime = Time.time;

        // Keep moving to the next vertex until cannot reach next vertex or the last vertex is reached
        while (distanceToTravel > 0 && pathVertexIndex != (path.Length - 1))
        {
            distanceToVertex = (levelGraph.Vertices[path[pathVertexIndex + 1]].Position - levelGraph.Vertices[path[pathVertexIndex]].Position).magnitude;

            // Remove the progress already done for the first edge
            if (pathVertexIndex == 0)
            {
                distanceToVertex -= characterState.Progress;
            }

            if (distanceToVertex <= distanceToTravel)
            {
                distanceToTravel -= distanceToVertex;
                pathVertexIndex += 1;
                continue;
            }

            break;
        }

        // If reached the last vertex
        if (pathVertexIndex == (path.Length - 1))
        {
            characterState.CurrentVertex = path[pathVertexIndex];
            characterState.NextVertex = -1;
            characterState.Progress = 0;
            characterState.Position = levelGraph.Vertices[characterState.CurrentVertex].Position;
            Vector3 direction = (levelGraph.Vertices[characterState.CurrentVertex].Position - levelGraph.Vertices[path[path.Length - 2]].Position).normalized;
            characterState.Rotation = Quaternion.LookRotation(direction, Vector3.up).eulerAngles;
        }
        else
        {
            characterState.CurrentVertex = path[pathVertexIndex];
            characterState.NextVertex = path[pathVertexIndex + 1];
            characterState.Progress = pathVertexIndex == 0 ? distanceToTravel + characterState.Progress : distanceToTravel;
            Vector3 currentVertexPosition = levelGraph.Vertices[characterState.CurrentVertex].Position;
            Vector3 nextVertexPosition = levelGraph.Vertices[characterState.NextVertex].Position;
            float progressRatio = characterState.Progress / (nextVertexPosition - currentVertexPosition).magnitude;
            Vector3 position = (1 - progressRatio) * currentVertexPosition + progressRatio * nextVertexPosition;
            characterState.Position = position;
            characterState.Rotation = Quaternion.LookRotation((nextVertexPosition - currentVertexPosition).normalized, Vector3.up).eulerAngles;
        }
    }
}
