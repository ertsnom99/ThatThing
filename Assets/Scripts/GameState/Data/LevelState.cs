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
    public int Cost;
    public bool Traversable;
    public EdgeType Type;
}

[Serializable]
public struct LevelGraph
{
    public Vertex[] Vertices;
    public Edge[] Edges;

    public LevelGraph(Vertex[] vertices, Edge[] edges)
    {
        Vertices = vertices;
        Edges = edges;
    }
}

[Serializable]
public struct LevelStateCharacter
{
    // Index of the vertex
    public int Vertex;
}

[CreateAssetMenu(fileName = "LevelState", menuName = "Game State/Level State")]
public class LevelState : ScriptableObject
{
    [SerializeField]
    private LevelGraph _graph;

    [HideInInspector]
    public int VertexIdCount;
    [HideInInspector]
    public int EdgeIdCount;

    [SerializeField]
    private LevelStateCharacter[] _characters;

    [HideInInspector]
    [SerializeField]
    private bool _initialized = false;

    private void OnEnable()
    {
        if (!_initialized)
        {
            Initialize();
            _initialized = true;
        }
    }

    #region Methods for the editor window
    public void Initialize()
    {
        _graph.Vertices = new Vertex[0];
        _graph.Edges = new Edge[0];
        VertexIdCount = 0;
        EdgeIdCount = 0;

        _characters = new LevelStateCharacter[0];
    }

    public void AddVertex(Transform transform = null)
    {
        Vertex newVertex = new Vertex();
        newVertex.Id = GenerateUniqueVertexId();

        if (transform)
        {
            newVertex.Position = transform.position;
        }

        List<Vertex> tempVertices = new List<Vertex>(_graph.Vertices);
        tempVertices.Add(newVertex);

        _graph.Vertices = tempVertices.ToArray();
    }

    private int GenerateUniqueVertexId()
    {
        int newId = VertexIdCount;
        VertexIdCount++;

        return newId;
    }

    public void RemoveVertex(int index)
    {
        for (int i = _graph.Edges.Length; i > 0; i--)
        {
            // Remove any edges that uses the removed vertex
            if (_graph.Edges[i - 1].VertexA == index || _graph.Edges[i - 1].VertexB == index)
            {
                RemoveEdge(i - 1);
                continue;
            }

            // Fix indexes
            if (_graph.Edges[i - 1].VertexA > index)
            {
                _graph.Edges[i - 1].VertexA -= 1;
            }

            if (_graph.Edges[i - 1].VertexB > index)
            {
                _graph.Edges[i - 1].VertexB -= 1;
            }
        }

        for (int i = _characters.Length; i > 0; i--)
        {
            // Remove any character that uses the removed vertex
            if (_characters[i - 1].Vertex == index)
            {
                RemoveCharacter(i - 1);
                continue;
            }

            // Fix indexes
            if (_characters[i - 1].Vertex > index)
            {
                _characters[i - 1].Vertex -= 1;
            }
        }

        List<Vertex> tempVertices = new List<Vertex>(_graph.Vertices);
        tempVertices.Remove(_graph.Vertices[index]);

        _graph.Vertices = tempVertices.ToArray();
    }

    public List<string> GetAllVertexIds()
    {
        List<string> idList = new List<string>();
        
        foreach (Vertex vertex in _graph.Vertices)
        {
            idList.Add(vertex.Id.ToString());
        }

        return idList;
    }

    public void AddEdge(int vertexA, int vertexB)
    {
        Edge newEdge = new Edge();
        newEdge.Id = GenerateUniqueEdgeId();
        newEdge.VertexA = vertexA;
        newEdge.VertexB = vertexB;
        newEdge.Cost = 1;
        newEdge.Traversable = true;

        List<Edge> tempEdges = new List<Edge>(_graph.Edges);
        tempEdges.Add(newEdge);

        _graph.Edges = tempEdges.ToArray();
    }

    private int GenerateUniqueEdgeId()
    {
        int newId = EdgeIdCount;
        EdgeIdCount++;

        return newId;
    }

    public void RemoveEdge(int index)
    {
        List<Edge> tempEdges = new List<Edge>(_graph.Edges);
        tempEdges.Remove(_graph.Edges[index]);

        _graph.Edges = tempEdges.ToArray();
    }

    public void AddCharacter(int vertex)
    {
        LevelStateCharacter newCharacter = new LevelStateCharacter();
        newCharacter.Vertex = vertex;

        List<LevelStateCharacter> tempCharacters = new List<LevelStateCharacter>(_characters);
        tempCharacters.Add(newCharacter);

        _characters = tempCharacters.ToArray();
    }

    public void RemoveCharacter(int index)
    {
        List<LevelStateCharacter> tempCharacters = new List<LevelStateCharacter>(_characters);
        tempCharacters.Remove(_characters[index]);

        _characters = tempCharacters.ToArray();
    }
    #endregion

    // Returns a COPY of the array of vertices
    public Vertex[] GetVerticesCopy()
    {
        Vertex[] verticesCopy = new Vertex[_graph.Vertices.Length];
        _graph.Vertices.CopyTo(verticesCopy, 0);
        return verticesCopy;
    }

    public int GetVerticesLength()
    {
        return _graph.Vertices.Length;
    }

    // Returns a COPY of the array of edges
    public Edge[] GetEdgesCopy()
    {
        Edge[] edgesCopy = new Edge[_graph.Edges.Length];
        _graph.Edges.CopyTo(edgesCopy, 0);
        return edgesCopy;
    }

    public int GetEdgesLength()
    {
        return _graph.Edges.Length;
    }

    public LevelStateCharacter[] GetCharacters()
    {
        return _characters;
    }

    public int GetCharactersLength()
    {
        return _characters.Length;
    }
}
