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
        PositionOnGraph positionOnGraph;

        if (_graph == null || !_graph.ConvertPositionToGraph(_testPos.position, _wallMask, out positionOnGraph))
        {
            return;
        }
        
        if (positionOnGraph.VertexA > -1 && positionOnGraph.Progress > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(_graph.Vertices[positionOnGraph.VertexA].Position, 1.5f);
        }

        if (positionOnGraph.VertexB > -1 && positionOnGraph.Progress > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_graph.Vertices[positionOnGraph.VertexB].Position, 1.5f);
        }

        if (positionOnGraph.Progress == 0)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_graph.Vertices[positionOnGraph.VertexA].Position, 1.5f);
        }
        else if (positionOnGraph.VertexA > -1 && positionOnGraph.VertexB > -1)
        {
            Vector3 closestToSecondVertex = (_graph.Vertices[positionOnGraph.VertexB].Position - _graph.Vertices[positionOnGraph.VertexA].Position).normalized;
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_graph.Vertices[positionOnGraph.VertexA].Position + closestToSecondVertex * positionOnGraph.Progress, 1.5f);
        }
    }
}
