using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct Vertex
{
    public int Id;
    public Vector3 Position;
}

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

public enum EdgeType { Corridor, Door, Vent };

public struct PathSegment
{
    public int VertexIndex;
    public float Distance;
    public Vector3 Position;
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

    // Variables used in CalculatePath() and ConvertPositionToGraph()
    private List<int> _vertexIndices = new List<int>();
    private float[] _distances;
    private int[] _parents;
    private List<PathSegment> _shortestPath = new List<PathSegment>();
    private int[] _indexes;

    public LevelGraph()
    {
        _vertices = new Vertex[0];
        _edges = new Edge[0];
        _adjMatrix = new float[0, 0];
    }

    public LevelGraph(Vertex[] vertices, Edge[] edges)
    {
        _vertices = vertices;
        _edges = edges;
        _adjMatrix = new float[vertices.Length, vertices.Length];
    }

    public void InitializeForPathCalculation()
    {
        if (_vertices == null || _edges == null)
        {
            return;
        }

        // Initialize _adjMatrix
        _adjMatrix = new float[_vertices.Length, _vertices.Length];
        
        for (int i = 0; i < _vertices.Length; i++)
        {
            for (int j = 0; j < _vertices.Length; j++)
            {
                _adjMatrix[i, j] = -1;
            }
        }

        // Populate _adjMatrix
        float distance;

        for (int i = 0; i < _edges.Length; i++)
        {
            distance = _edges[i].Traversable ? (_vertices[_edges[i].VertexB].Position - _vertices[_edges[i].VertexA].Position).magnitude : -1;
            _adjMatrix[_edges[i].VertexA, _edges[i].VertexB] = distance;
            _adjMatrix[_edges[i].VertexB, _edges[i].VertexA] = distance;
        }

        // Create necessary arrays for CalculatePath() and ConvertPositionToGraph()
        _distances = new float[_vertices.Length];
        _parents = new int[_vertices.Length];
        _indexes = new int[_vertices.Length];
    }

    public bool CalculatePath(int sourceVertex, int targetVertex, out PathSegment[] path)
    {
        path = new PathSegment[0];

        if (_adjMatrix == null)
        {
            return false;
        }

        // Clear the distances
        _vertexIndices.Clear();

        for (int i = 0; i < _vertices.Length; i++)
        {
            _distances[i] = 999999;
            _parents[i] = -1;
            _vertexIndices.Add(i);
        }

        _distances[sourceVertex] = 0;

        int vertexIndex;
        PathSegment pathSection;
        float alt;

        // Calculate shortest distances for all vertices
        while(_vertexIndices.Count > 0)
        {
            // Find the closest vertex to source
            vertexIndex = _vertexIndices[0];

            for(int i = 1; i < _vertexIndices.Count; i++)
            {
                if (_distances[_vertexIndices[i]] < _distances[vertexIndex])
                {
                    vertexIndex = _vertexIndices[i];
                }
            }
            
            // Remove closest vertex
            _vertexIndices.Remove(vertexIndex);

            // Set shortest distance for all neighbors of the closest vertex
            for (int i = 0; i < _vertices.Length; i++)
            {
                // Skip vertex i if not a neighbor or already removed from Q
                if (_adjMatrix[vertexIndex, i] < 0 || !_vertexIndices.Contains(i))
                {
                    continue;
                }

                // Update distance if shorter path found
                alt = _distances[vertexIndex] + _adjMatrix[vertexIndex, i];

                if (alt < _distances[i])
                {
                    _distances[i] = alt;
                    _parents[i] = vertexIndex;
                }
            }
        }
        
        // Find shortest path
        int currentVertex = targetVertex;
        _shortestPath.Clear();

        pathSection.VertexIndex = targetVertex;
        pathSection.Distance = _distances[targetVertex];
        pathSection.Position = _vertices[targetVertex].Position;
        _shortestPath.Add(pathSection);

        // Start from to target vertex and find path back to the source vertex
        while (_parents[currentVertex] != -1)
        {
            vertexIndex = _parents[currentVertex];

            pathSection.VertexIndex = vertexIndex;
            pathSection.Distance = _distances[vertexIndex];
            pathSection.Position = _vertices[vertexIndex].Position;
            _shortestPath.Insert(0, pathSection);

            currentVertex = vertexIndex;
        }

        if (currentVertex == sourceVertex)
        {
            path = _shortestPath.ToArray();
            return true;
        }

        return false;
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

        for (int i = 0; i < _vertices.Length; i++)
        {
            _indexes[i] = i;
            _distances[i] = (position - _vertices[i].Position).magnitude;
        }

        // Sort all vertices by distance
        FastAlgorithms.QuickSortAlignedArrays(_distances, _indexes, 0, _distances.Length - 1);

        // Find closest Vertex
        RaycastHit hit;

        for (int i = 0; i < _indexes.Length; i++)
        {
            // TODO: Use sphere sweep instead?
            // Check if a raycast can reach vertex
            if (!Physics.Raycast(_vertices[_indexes[i]].Position, (position - _vertices[_indexes[i]].Position).normalized, out hit, _distances[i], blockingMask))
            {
                vertexA = _indexes[i];
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
        foreach (Edge edge in _edges)
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
            vertexAToSecond = _vertices[secondVertex].Position - _vertices[vertexA].Position;
            vertexToPos = position - _vertices[vertexA].Position;
            dotA = Vector3.Dot(vertexAToSecond, vertexToPos);

            secondToVertexA = _vertices[vertexA].Position - _vertices[secondVertex].Position;
            vertexToPos = position - _vertices[secondVertex].Position;
            dotB = Vector3.Dot(secondToVertexA, vertexToPos);

            // Check if position is aligned with the edge
            if (dotA * dotB > .0f)
            {
                // Calculate projection directly since we already calculated the dot product
                projectedPosition = (dotA / vertexAToSecond.sqrMagnitude) * vertexAToSecond;

                // Calculate the distance between the position and the projected position 
                distanceToPosition = (position - _vertices[vertexA].Position - projectedPosition).sqrMagnitude;

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