using UnityEngine;

namespace GraphCreator
{
    public class PathfindingDebugger : MonoBehaviour
    {
        private enum PathFindingAlgorithm { Dijkstra, AStar }

        [SerializeField]
        private Graph Graph;
        [SerializeField]
        private PathFindingAlgorithm AlgorithmUsed;
        [SerializeField]
        private Transform From;
        [SerializeField]
        private Transform To;
        [SerializeField]
        private LayerMask _wallMask;

private GameObject sphereA;
private GameObject sphereB;

        private void Awake()
        {
            if (Graph)
            {
                Graph.Initialize();
            }

sphereA = GameObject.CreatePrimitive(PrimitiveType.Sphere);
sphereB = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        }

        private void Update()
        {
            if (!Graph || !From || !To)
            {
                return;
            }

            PositionOnGraph fromPosition;
            PositionOnGraph toPosition;

            if(!Graph.ConvertPositionToGraph(From.position, _wallMask, out fromPosition) || !Graph.ConvertPositionToGraph(To.position, _wallMask, out toPosition))
            {
                return;
            }

            if (fromPosition.VertexA < 0 || fromPosition.VertexA >= Graph.Vertices.Length || fromPosition.VertexB >= Graph.Vertices.Length ||
                toPosition.VertexA < 0 || toPosition.VertexA >= Graph.Vertices.Length || toPosition.VertexB >= Graph.Vertices.Length)
            {
                return;    
            }

            // Place sphere A
            if (fromPosition.VertexB == -1)
            {
                sphereA.transform.position = Graph.Vertices[fromPosition.VertexA].Position;
            }
            else
            {
                sphereA.transform.position = (Graph.Vertices[fromPosition.VertexB].Position - Graph.Vertices[fromPosition.VertexA].Position).normalized * fromPosition.Progress + Graph.Vertices[fromPosition.VertexA].Position;
            }

            // Place sphere B
            if (toPosition.VertexB == -1)
            {
                sphereB.transform.position = Graph.Vertices[toPosition.VertexA].Position;
            }
            else
            {
                sphereB.transform.position = (Graph.Vertices[toPosition.VertexB].Position - Graph.Vertices[toPosition.VertexA].Position).normalized * toPosition.Progress + Graph.Vertices[toPosition.VertexA].Position;
            }

            bool pathFound = false;
            PathSegment[] path = new PathSegment[0];

            switch (AlgorithmUsed)
            {
                case PathFindingAlgorithm.Dijkstra:
                    pathFound = Graph.CalculatePathWithDijkstra(fromPosition, toPosition, out path);
                    break;
                case PathFindingAlgorithm.AStar:
                    pathFound = Graph.CalculatePathWithAStar(fromPosition, toPosition, out path);
                    break;
            }

            if (pathFound)
            {
                if (path.Length == 1)
                {
                    Debug.Log("Already at destination");
                    return;
                }

                float pathLength = 0;

                for(int i = 1; i < path.Length; i++)
                {
                    pathLength += (path[i].Position - path[i - 1].Position).magnitude;
                    Debug.DrawLine(path[i-1].Position, path[i].Position, Color.red);
                }

                Debug.Log("Path length: " + pathLength);
            }
            else
            {
                Debug.Log("No path with " + AlgorithmUsed + "!");
            }
        }
    }
}
