using UnityEngine;
using UnityEngine.SceneManagement;

public class ConvertPositionToGraphDebugger : MonoBehaviour
{
    [SerializeField]
    private LevelState _levelState;
    [SerializeField]
    private Transform _testPos;
    [SerializeField]
    private LayerMask _wallMask;

    private void OnDrawGizmos()
    {
        GameSave gameSave = SimulationManager.GetGameSave();
        int buildIndex = SceneManager.GetActiveScene().buildIndex;

        int vertexA;
        int vertexB;
        float progress;

        if (gameSave == null || !SimulationManager.ConvertPositionToGraph(gameSave.LevelStatesByBuildIndex[buildIndex].Graph, _testPos.position, _wallMask, out vertexA, out vertexB, out progress))
        {
            return;
        }

        Vertex[] vertices = gameSave.LevelStatesByBuildIndex[buildIndex].Graph.Vertices;
        
        if (vertexA > -1 && progress > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(vertices[vertexA].Position, 1.5f);
        }

        if (vertexB > -1 && progress> 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(vertices[vertexB].Position, 1.5f);
        }

        if (progress == 0)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(vertices[vertexA].Position, 1.5f);
        }
        else if (vertexA > -1 && vertexB > -1)
        {
            Vector3 closestToSecondVertex = (vertices[vertexB].Position - vertices[vertexA].Position).normalized;
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(vertices[vertexA].Position + closestToSecondVertex * progress, 1.5f);
        }
    }
}
