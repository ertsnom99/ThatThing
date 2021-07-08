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
public class LevelGraph
{
    public Vertex[] Vertices;
    public Edge[] Edges;

    [NonSerialized]
    public float[,] _adjMatrix;

    public float[,] AdjMatrix
    {
        get { return _adjMatrix; }
        private set { _adjMatrix = value; }
    }

    public LevelGraph(Vertex[] vertices, Edge[] edges)
    {
        Vertices = vertices;
        Edges = edges;
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
}
