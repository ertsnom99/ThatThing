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
        private int FromId = 0;
        [SerializeField]
        private int ToId = 1;

        private void Awake()
        {
            if (Graph)
            {
                Graph.Initialize();
            }
        }

        private void Update()
        {
            if (!Graph)
            {
                return;
            }

            int from = -1;
            int to = -1;

            for(int i = 0; i < Graph.Vertices.Length; i++)
            {
                if (Graph.Vertices[i].Id == FromId)
                {
                    from = i;
                }

                if (Graph.Vertices[i].Id == ToId)
                {
                    to = i;
                }
            }

            if (from == -1 || to == -1)
            {
                return;    
            }

            bool pathFound = false;
            PathSegment[] path = new PathSegment[0];

            switch (AlgorithmUsed)
            {
                case PathFindingAlgorithm.Dijkstra:
                    pathFound = Graph.CalculatePathWithDijkstra(from, to, out path);
                    break;
                case PathFindingAlgorithm.AStar:
                    pathFound = Graph.CalculatePathWithAStar(from, to, out path);
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
