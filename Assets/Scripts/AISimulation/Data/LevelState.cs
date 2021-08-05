using System;
using System.Collections.Generic;
using UnityEngine;
using GraphCreator;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public struct LevelStateCharacter
{
    // Index of the vertex
    public int Vertex;
    public int Settings;
}

[CreateAssetMenu(fileName = "LevelState", menuName = "AI Simulation/States/Level State")]
public partial class LevelState : ScriptableObject
{
    [SerializeField]
    private Graph _graph;

    public Graph Graph
    {
        get { return _graph; }
        private set { _graph = value; }
    }

    [SerializeField]
    private LevelStateCharacter[] _characters;

    public LevelStateCharacter[] Characters
    {
        get { return _characters; }
        private set { _characters = value; }
    }
}
#if UNITY_EDITOR
public partial class LevelState
{
    [HideInInspector]
    public int VertexIdCount;
    [HideInInspector]
    public int EdgeIdCount;

    [HideInInspector]
    public List<bool> EdgesFolded;
    [HideInInspector]
    public List<bool> CharactersFolded;

    [HideInInspector]
    [SerializeField]
    private bool _initialized = false;

    public bool IsValid(CharactersSettings charactersSettings)
    {
        // Check that all characters have valid settings
        foreach (LevelStateCharacter character in _characters)
        {
            if (character.Settings >= charactersSettings.Settings.Length)
            {
                return false;
            }
        }

        return true;
    }

    private void OnEnable()
    {
        if (!_initialized)
        {
            Initialize();
        }
    }

    public void Initialize()
    {
        _graph = new Graph();
        _graph.ClearVertices();
        _graph.ClearEdges();

        VertexIdCount = 0;
        EdgeIdCount = 0;
        EdgesFolded = new List<bool>();
        CharactersFolded = new List<bool>();

        _characters = new LevelStateCharacter[0];

        _initialized = true;
    }

    public void AddVertex(Vector3 position)
    {
        // Record the LevelState before applying change in order to allow undo
        Undo.RecordObject(this, "Added Vertex");

        _graph.AddVertex(position);
    }

    private int GenerateUniqueVertexId()
    {
        int newId = VertexIdCount;
        VertexIdCount++;

        return newId;
    }

    public bool RemoveVertex(int index)
    {
        // Record the LevelState before applying change in order to allow undo
        Undo.RecordObject(this, "Removed Vertex");

        if (_graph.RemoveVertex(index))
        {
            return false;
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

        return true;
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
        foreach (Edge edge in _graph.Edges)
        {
            if ((edge.VertexA == vertexA && edge.VertexB == vertexB) || (edge.VertexB == vertexA && edge.VertexA == vertexB))
            {
                return false;
            }
        }

        // Record the LevelState before applying change in order to allow undo
        Undo.RecordObject(this, "Added Edge");

        if (!_graph.AddEdge(vertexA, vertexB, EdgeDirection.Bidirectional, true))
        {
            return false;
        }

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

    public bool RemoveEdge(int index)
    {
        // Record the LevelState before applying change in order to allow undos
        Undo.RecordObject(this, "Removed Vertex");

        if (_graph.RemoveEdge(index))
        {
            return false;
        }

        // Remove the edge foldout list entry
        EdgesFolded.RemoveAt(index);

        return true;
    }

    public void AddCharacter(int vertex)
    {
        // Record the LevelState before applying change in order to allow undo
        Undo.RecordObject(this, "Added Character");

        LevelStateCharacter newCharacter = new LevelStateCharacter();
        newCharacter.Vertex = vertex;

        List<LevelStateCharacter> tempCharacters = new List<LevelStateCharacter>(_characters);
        tempCharacters.Add(newCharacter);

        _characters = tempCharacters.ToArray();

        CharactersFolded.Add(false);
    }

    public void RemoveCharacter(int index)
    {
        // Record the LevelState before applying change in order to allow undos
        Undo.RecordObject(this, "Removed Character");

        List<LevelStateCharacter> tempCharacters = new List<LevelStateCharacter>(_characters);
        tempCharacters.Remove(_characters[index]);

        _characters = tempCharacters.ToArray();

        CharactersFolded.RemoveAt(index);
    }
}
#endif