using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[Serializable]
public struct LevelStateCharacter
{
    // Index of the vertex
    public int Vertex;
    public int Settings;
}

[CreateAssetMenu(fileName = "LevelState", menuName = "AI Simulation/States/Level State")]
public class LevelState : ScriptableObject
{
    [SerializeField]
    private LevelGraph _graph;

    [HideInInspector]
    public int VertexIdCount;
    [HideInInspector]
    public int EdgeIdCount;

    [HideInInspector]
    public List<bool> EdgesFolded;
    [HideInInspector]
    public List<bool> CharactersFolded;

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

    public bool IsValid(CharactersSettings charactersSettings)
    {
        // Check that all characters have valid settings
        foreach (LevelStateCharacter character in _characters)
        {
            if (charactersSettings.Settings.Length <= character.Settings)
            {
                return false;
            }
        }

        return true;
    }

    #region Methods for the editor window
    public void Initialize()
    {
        _graph.Vertices = new Vertex[0];
        _graph.Edges = new Edge[0];

        VertexIdCount = 0;
        EdgeIdCount = 0;
        EdgesFolded = new List<bool>();
        CharactersFolded = new List<bool>();

        _characters = new LevelStateCharacter[0];
    }

    public void AddVertex(Vector3 position)
    {
#if UNITY_EDITOR
        // Record the LevelState before applying change in order to allow undos
        Undo.RecordObject(this, "Added Vertex");
#endif
        Vertex newVertex = new Vertex();
        newVertex.Id = GenerateUniqueVertexId();
        newVertex.Position = position;

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
#if UNITY_EDITOR
        // Record the LevelState before applying change in order to allow undos
        Undo.RecordObject(this, "Removed Vertex");
#endif
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

    // return if a new edge was created
    public bool AddEdge(int vertexA, int vertexB)
    {
        foreach(Edge edge in _graph.Edges)
        {
            if ((edge.VertexA == vertexA && edge.VertexB == vertexB) || (edge.VertexB == vertexA && edge.VertexA == vertexB))
            {
                return false;
            }
        }
#if UNITY_EDITOR
        // Record the LevelState before applying change in order to allow undos
        Undo.RecordObject(this, "Added Edge");
#endif
        // Add the new edge
        Edge newEdge = new Edge();
        newEdge.Id = GenerateUniqueEdgeId();
        newEdge.VertexA = vertexA;
        newEdge.VertexB = vertexB;
        newEdge.Traversable = true;

        List<Edge> tempEdges = new List<Edge>(_graph.Edges);
        tempEdges.Add(newEdge);

        _graph.Edges = tempEdges.ToArray();

        // Add a new entry to the edge foldout list
        EdgesFolded.Add(false);

        return true;
    }

    private int GenerateUniqueEdgeId()
    {
        int newId = EdgeIdCount;
        EdgeIdCount++;

        return newId;
    }

    public void RemoveEdge(int index)
    {
#if UNITY_EDITOR
        // Record the LevelState before applying change in order to allow undos
        Undo.RecordObject(this, "Removed Vertex");
#endif
        // Remove the edge
        List<Edge> tempEdges = new List<Edge>(_graph.Edges);
        tempEdges.Remove(_graph.Edges[index]);

        _graph.Edges = tempEdges.ToArray();

        // Remove the edge foldout list entry
        EdgesFolded.RemoveAt(index);
    }

    public void AddCharacter(int vertex)
    {
#if UNITY_EDITOR
        // Record the LevelState before applying change in order to allow undos
        Undo.RecordObject(this, "Added Character");
#endif

        LevelStateCharacter newCharacter = new LevelStateCharacter();
        newCharacter.Vertex = vertex;

        List<LevelStateCharacter> tempCharacters = new List<LevelStateCharacter>(_characters);
        tempCharacters.Add(newCharacter);

        _characters = tempCharacters.ToArray();

        CharactersFolded.Add(false);
    }

    public void RemoveCharacter(int index)
    {
#if UNITY_EDITOR
        // Record the LevelState before applying change in order to allow undos
        Undo.RecordObject(this, "Removed Character");
#endif
        List<LevelStateCharacter> tempCharacters = new List<LevelStateCharacter>(_characters);
        tempCharacters.Remove(_characters[index]);

        _characters = tempCharacters.ToArray();

        CharactersFolded.RemoveAt(index);
    }
    #endregion

    // Returns a COPY of the array of vertices
    public Vertex[] GetVertices()
    {
        return _graph.Vertices;
    }

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