using GraphCreator;
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

    public void MoveOnGraph(PathSegment[] path, CharacterSave characterState)
    {
        if (path.Length < 2)
        {
            return;
        }

        float distanceToTravel = _speed * (Time.time - _lastTickTime);
        distanceToTravel += characterState.Progress;

        _lastTickTime = Time.time;

        if (distanceToTravel >= path[path.Length - 1].Distance)
        {
            int pathVertexIndex = path.Length - 1;

            characterState.CurrentVertex = path[pathVertexIndex].VertexIndex;
            characterState.NextVertex = -1;
            characterState.Progress = 0;
            characterState.Position = path[pathVertexIndex].Position;
            Vector3 direction = (path[pathVertexIndex].Position - path[pathVertexIndex - 1].Position).normalized;
            characterState.Rotation = Quaternion.LookRotation(direction, Vector3.up).eulerAngles;
        }
        else
        {
            int pathVertexIndex = BinarySearchPathSection(path, distanceToTravel);
            
            characterState.CurrentVertex = path[pathVertexIndex].VertexIndex;
            characterState.NextVertex = path[pathVertexIndex + 1].VertexIndex;
            characterState.Progress = distanceToTravel - path[pathVertexIndex].Distance;
            Vector3 currentVertexPosition = path[pathVertexIndex].Position;
            Vector3 nextVertexPosition = path[pathVertexIndex + 1].Position;
            float progressRatio = characterState.Progress / (nextVertexPosition - currentVertexPosition).magnitude;
            Vector3 position = (1 - progressRatio) * currentVertexPosition + progressRatio * nextVertexPosition;
            characterState.Position = position;
            characterState.Rotation = Quaternion.LookRotation((nextVertexPosition - currentVertexPosition).normalized, Vector3.up).eulerAngles;
        }
    }

    private int BinarySearchPathSection(PathSegment[] path, float dist)
    {
        int l = 0;
        int r = path.Length - 1;

        while (l <= r)
        {
            int mid = l + (r - l) / 2;

            if (path[mid].Distance == dist || (mid < path.Length - 1 && path[mid].Distance < dist && dist < path[mid + 1].Distance))
            {
                return mid;
            }

            if (path[mid].Distance > dist)
            {
                // element is in left subarray
                r = mid - 1;
            }
            else
            {
                // element is in right subarray
                l = mid + 1;
            }
        }

        return -1;
    }
}
