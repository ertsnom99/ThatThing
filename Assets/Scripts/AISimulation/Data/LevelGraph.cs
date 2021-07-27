using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct Vertex
{
    public int Id;
    public Vector3 Position;
}

public enum EdgeType { Corridor, Door, Vent };

[Serializable]
public struct Edge
{
    public int Id;
    // Index of VertexA in LevelGraph.Vertices
    public int VertexA;
    // Index of VertexB in LevelGraph.Vertices
    public int VertexB;
    public bool Traversable;
    public EdgeType Type;
}

[Serializable]
public partial class LevelGraph
{
    [SerializeField]
    private Vertex[] _vertices;

    public Vertex[] Vertices
    {
        get { return _vertices; }
        private set { _vertices = value; }
    }

    [SerializeField]
    private Edge[] _edges;

    public Edge[] Edges
    {
        get { return _edges; }
        private set { _edges = value; }
    }

    private float[,] _adjMatrix;

    public float[,] AdjMatrix
    {
        get { return _adjMatrix; }
        private set { _adjMatrix = value; }
    }

    public LevelGraph()
    {
        Vertices = new Vertex[0];
        Edges = new Edge[0];
        _adjMatrix = new float[0, 0];
    }

    public LevelGraph(Vertex[] vertices, Edge[] edges)
    {
        _vertices = vertices;
        _edges = edges;
        _adjMatrix = new float[vertices.Length, vertices.Length];
    }

    public void GenerateAdjMatrix()
    {
        if (Vertices == null || Edges == null)
        {
            return;
        }

        // Initialize _adjMatrix
        _adjMatrix = new float[Vertices.Length, Vertices.Length];
        
        for (int i = 0; i < Vertices.Length; i++)
        {
            for (int j = 0; j < Vertices.Length; j++)
            {
                _adjMatrix[i, j] = -1;
            }
        }

        // Populate _adjMatrix
        float distance;

        for (int i = 0; i < Edges.Length; i++)
        {
            distance = Edges[i].Traversable ? (Vertices[Edges[i].VertexB].Position - Vertices[Edges[i].VertexA].Position).magnitude : -1;
            _adjMatrix[Edges[i].VertexA, Edges[i].VertexB] = distance;
            _adjMatrix[Edges[i].VertexB, Edges[i].VertexA] = distance;
        }
    }

    public bool CalculatePath(int sourceVertex, int targetVertex, out int[] path)
    {
        path = new int[0];

        if (_adjMatrix == null)
        {
            return false;
        }

        // Initialise the distances
        float[] distances = new float[Vertices.Length];
        List<int> q = new List<int>(0);

        for (int i = 0; i < Vertices.Length; i++)
        {
            distances[i] = 999999;
            q.Add(i);
        }

        distances[sourceVertex] = 0;

        int vertexIndex;
        float alt;

        while(q.Count > 0)
        {
            // Find the closest vertex to source
            vertexIndex = q[0];

            for(int i = 1; i < q.Count; i++)
            {
                if (distances[q[i]] < distances[vertexIndex])
                {
                    vertexIndex = q[i];
                }

            }
            
            // Remove closest vertex
            q.Remove(vertexIndex);

            // Set shortest distance for all neighbors of v
            for (int i = 0; i < Vertices.Length; i++)
            {
                // Skip vertex i if not a neighbor or already removed from Q
                if (_adjMatrix[vertexIndex, i] < 0 || !q.Contains(i))
                {
                    continue;
                }

                // Update distance if shorter path found
                alt = distances[vertexIndex] + _adjMatrix[vertexIndex, i];

                if (alt < distances[i])
                {
                    distances[i] = alt;
                }
            }
        }
        
        // Find shortest path
        int currentVertex = targetVertex;
        List<int> shortestPath = new List<int>();
        shortestPath.Add(targetVertex);

        // Start from to target vertex and find path back to the source vertex
        while (currentVertex != sourceVertex)
        {
            alt = 999999;
            vertexIndex = -1;

            for (int i = 0; i < Vertices.Length; i++)
            {
                // Can't move to a vertex at distance of 0, because it may cause infinite loops
                if (_adjMatrix[currentVertex, i] <= 0)
                {
                    continue;
                }

                if (distances[i] < alt)
                {
                    alt = distances[i];
                    vertexIndex = i;
                }
            }
            
            if (vertexIndex != -1)
            {
                shortestPath.Insert(0, vertexIndex);
                currentVertex = vertexIndex;
                continue;
            }

            return false;
        }

        path = shortestPath.ToArray();
        return true;
    }

    // Takes a world position and finds the closest vertexA, the possible edge it's on (given by vertexB) and the progress on that edge.
    // Returns true if the conversion was successful. Even if the conversion is successful, vertexB could be -1, if the position
    // was considered exactly at vertexA. 
    public bool ConvertPositionToGraph(Vector3 position, LayerMask blockingMask, out int vertexA, out int vertexB, out float progress)
    {
        // Reset variables
        vertexA = -1;
        vertexB = -1;
        progress = .0f;

        int[] indexes = new int[Vertices.Length];
        float[] distances = new float[Vertices.Length];

        for (int i = 0; i < Vertices.Length; i++)
        {
            indexes[i] = i;
            distances[i] = (position - Vertices[i].Position).magnitude;
        }

        // Sort all vertices by distance
        QuickSort.QuickSortAlignedArrays(distances, indexes, 0, distances.Length - 1);

        // Find closest Vertex
        RaycastHit hit;

        for (int i = 0; i < indexes.Length; i++)
        {
            // TODO: Use sphere sweep instead?
            // Check if a raycast can reach vertex
            if (!Physics.Raycast(Vertices[indexes[i]].Position, (position - Vertices[indexes[i]].Position).normalized, out hit, distances[i], blockingMask))
            {
                vertexA = indexes[i];
                break;
            }
        }

        // The position can't be converted to graph if no vertexA could be found
        if (vertexA == -1)
        {
            return false;
        }

        int secondVertex;
        Vector3 vertexToPos;
        Vector3 vertexAToSecond;
        Vector3 secondToVertexA;
        float dotA;
        float dotB;
        Vector3 projectedPosition;

        float distanceToPosition;
        const float infinity = 99999;
        float smallestDistance = infinity;

        // Find closest edge connected to the first vertexA
        foreach (Edge edge in Edges)
        {
            if (edge.VertexA == vertexA)
            {
                secondVertex = edge.VertexB;
            }
            else if (edge.VertexB == vertexA)
            {
                secondVertex = edge.VertexA;
            }
            else
            {
                continue;
            }

            // Find progress along the edge
            vertexAToSecond = Vertices[secondVertex].Position - Vertices[vertexA].Position;
            vertexToPos = position - Vertices[vertexA].Position;
            dotA = Vector3.Dot(vertexAToSecond, vertexToPos);

            secondToVertexA = Vertices[vertexA].Position - Vertices[secondVertex].Position;
            vertexToPos = position - Vertices[secondVertex].Position;
            dotB = Vector3.Dot(secondToVertexA, vertexToPos);

            // Check if position is aligned with the edge
            if (dotA * dotB > .0f)
            {
                // Calculate projection directly since we already calculated the dot product
                projectedPosition = (dotA / vertexAToSecond.sqrMagnitude) * vertexAToSecond;

                // Calculate the distance between the position and the projected position 
                distanceToPosition = (position - Vertices[vertexA].Position - projectedPosition).sqrMagnitude;

                if (distanceToPosition < smallestDistance)
                {
                    vertexB = secondVertex;
                    progress = projectedPosition.magnitude;
                    smallestDistance = distanceToPosition;
                }
            }
        }

        return true;
    }
}

#if UNITY_EDITOR
public partial class LevelGraph
{
    public void AddVertex(int id, Vector3 position)
    {
        Vertex newVertex = new Vertex();
        newVertex.Id = id;
        newVertex.Position = position;

        List<Vertex> tempVertices = new List<Vertex>(_vertices);
        tempVertices.Add(newVertex);

        _vertices = tempVertices.ToArray();
    }

    public bool RemoveVertex(int index)
    {
        if (index > _vertices.Length - 1)
        {
            return false;
        }

        for (int i = _edges.Length; i > 0; i--)
        {
            // Remove any edges that uses the removed vertex
            if (_edges[i - 1].VertexA == index || _edges[i - 1].VertexB == index)
            {
                RemoveEdge(i - 1);
                continue;
            }

            // Fix indexes
            if (_edges[i - 1].VertexA > index)
            {
                _edges[i - 1].VertexA -= 1;
            }

            if (_edges[i - 1].VertexB > index)
            {
                _edges[i - 1].VertexB -= 1;
            }
        }

        List<Vertex> tempVertices = new List<Vertex>(_vertices);
        tempVertices.Remove(_vertices[index]);

        _vertices = tempVertices.ToArray();

        return true;
    }

    public void ClearVertices()
    {
        _vertices = new Vertex[0];
    }

    public bool AddEdge(int id, int vertexA, int vertexB, bool traversable, EdgeType type)
    {
        foreach (Edge edge in _edges)
        {
            if ((edge.VertexA == vertexA && edge.VertexB == vertexB) || (edge.VertexB == vertexA && edge.VertexA == vertexB))
            {
                return false;
            }
        }

        Edge newEdge = new Edge();
        newEdge.Id = id;
        newEdge.VertexA = vertexA;
        newEdge.VertexB = vertexB;
        newEdge.Traversable = traversable;
        newEdge.Type = type;

        List<Edge> tempEdges = new List<Edge>(_edges);
        tempEdges.Add(newEdge);

        _edges = tempEdges.ToArray();

        return true;
    }

    public bool RemoveEdge(int index)
    {
        if (index > _edges.Length - 1)
        {
            return false;
        }

        List<Edge> tempEdges = new List<Edge>(_edges);
        tempEdges.Remove(_edges[index]);

        _edges = tempEdges.ToArray();

        return true;
    }

    public void ClearEdges()
    {
        _edges = new Edge[0];
    }
}
#endif