using GraphCreator;
using UnityEngine;

public class ConvertPositionToGraphDebugger : MonoBehaviour
{
    [SerializeField]
    private Graph _graph;
    [SerializeField]
    private Transform _testPos;
    [SerializeField]
    private LayerMask _wallMask;

    private void OnDrawGizmos()
    {
        int vertexA;
        int vertexB;
        float progress;

        if (_graph == null || !_graph.ConvertPositionToGraph(_testPos.position, _wallMask, out vertexA, out vertexB, out progress))
        {
            return;
        }
        
        if (vertexA > -1 && progress > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(_graph.Vertices[vertexA].Position, 1.5f);
        }

        if (vertexB > -1 && progress> 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_graph.Vertices[vertexB].Position, 1.5f);
        }

        if (progress == 0)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_graph.Vertices[vertexA].Position, 1.5f);
        }
        else if (vertexA > -1 && vertexB > -1)
        {
            Vector3 closestToSecondVertex = (_graph.Vertices[vertexB].Position - _graph.Vertices[vertexA].Position).normalized;
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_graph.Vertices[vertexA].Position + closestToSecondVertex * progress, 1.5f);
        }
    }
}
