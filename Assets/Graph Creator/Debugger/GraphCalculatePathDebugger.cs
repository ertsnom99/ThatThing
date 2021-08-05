using UnityEngine;

namespace GraphCreator
{
    public class GraphCalculatePathDebugger : MonoBehaviour
    {
        public Graph Graph;
        public int FromId = 0;
        public int ToId = 1;

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

            PathSegment[] path = new PathSegment[0];

            if (Graph.CalculatePath(from, to, out path))
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
                Debug.Log("No path!");
            }
        }
    }
}
