using UnityEngine;

public class LevelGraphCalculatePathDebugger : MonoBehaviour
{
    public LevelState LevelState;
    public int FromId = 0;
    public int ToId = 1;

    private Vertex[] _vertices;
    private Edge[] _edges;
    private LevelGraph _levelGraph;

    private void Update()
    {
        CreateGraph();

        int from = -1;
        int to = -1;

        for(int i = 0; i < _vertices.Length; i++)
        {
            if (_vertices[i].Id == FromId)
            {
                from = i;
            }

            if (_vertices[i].Id == ToId)
            {
                to = i;
            }
        }

        if (from == -1 || to == -1)
        {
            return;    
        }

        int[] path = new int[0];

        if (_levelGraph.CalculatePath(from, to, out path))
        {
            if (path.Length == 1)
            {
                Debug.Log("Already at destination");
                return;
            }

            float pathLength = 0;

            for(int i = 1; i < path.Length; i++)
            {
                pathLength = (_vertices[path[i]].Position - _vertices[path[i - 1]].Position).magnitude;
                Debug.DrawLine(_vertices[path[i-1]].Position, _vertices[path[i]].Position);
            }

            Debug.Log("PAth length: " + pathLength);
        }
        else
        {
            Debug.Log("No path!");
        }
    }

    private void CreateGraph()
    {
        _vertices = LevelState.GetVerticesCopy();
        _edges = LevelState.GetEdgesCopy();
        _levelGraph = new LevelGraph(_vertices, _edges);
        _levelGraph.GenerateAdjMatrix();
    }
}
