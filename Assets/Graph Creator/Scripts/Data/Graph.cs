using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GraphCreator
{
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
        // Index of VertexA in Vertices
        public int VertexA;
        // Index of VertexB in Vertices
        public int VertexB;
        public EdgeDirection Direction;
        public bool Traversable;
    }

    public enum EdgeDirection { Bidirectional, AtoB, BtoA };

    public struct PathSegment
    {
        public int VertexIndex;
        public float Distance;
        public Vector3 Position;
    }

    [CreateAssetMenu(fileName = "Graph", menuName = "Graph Creator/Graph", order = 2)]
    public partial class Graph : ScriptableObject
    {

        [SerializeField]
        private Vertex[] _vertices = new Vertex[0];

        public Vertex[] Vertices
        {
            get { return _vertices; }
            private set { _vertices = value; }
        }

        [SerializeField]
        private Edge[] _edges = new Edge[0];

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
        private struct AStarNode
        {
            public int VertexIndex;
            public float GCost;
            public float HCost;
            public float FCost;
            public int Parent;
        }

        // Variables used in path calculation and ConvertPositionToGraph()
        private List<PathSegment> _shortestPath = new List<PathSegment>();
        private float[] _distances;
        private int[] _parents;
        private List<int> _vertexIndices = new List<int>();
        private List<AStarNode> _visitedVertex = new List<AStarNode>();
        private List<AStarNode> _activeVertex = new List<AStarNode>();
        private int[] _indexes;

        public void Initialize()
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

            for (int i = 0; i < _edges.Length; i++)
            {
                float distance = _edges[i].Traversable ? (_vertices[_edges[i].VertexB].Position - _vertices[_edges[i].VertexA].Position).magnitude : -1;

                if (distance == -1)
                {
                    continue;
                }

                switch (_edges[i].Direction)
                {
                    case EdgeDirection.Bidirectional:
                        _adjMatrix[_edges[i].VertexA, _edges[i].VertexB] = distance;
                        _adjMatrix[_edges[i].VertexB, _edges[i].VertexA] = distance;
                        break;
                    case EdgeDirection.AtoB:
                        _adjMatrix[_edges[i].VertexA, _edges[i].VertexB] = distance;
                        break;
                    case EdgeDirection.BtoA:
                        _adjMatrix[_edges[i].VertexB, _edges[i].VertexA] = distance;
                        break;
                }
            }

            // Create necessary arrays for CalculatePath() and ConvertPositionToGraph()
            _distances = new float[_vertices.Length];
            _parents = new int[_vertices.Length];
            _indexes = new int[_vertices.Length];
        }

        public void Initialize(Vertex[] vertices, Edge[] edges)
        {
            _vertices = vertices;
            _edges = edges;

            Initialize();
        }

        public bool CalculatePathWithDijkstra(int sourceVertexIndex, int targetVertexIndex, out PathSegment[] path)
        {
            path = new PathSegment[0];
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (_adjMatrix == null)
            {
                Debug.LogError("The AdjMatrix is null! Was the Graph Initialized?");
                return false;
            }
#endif
            // Clear the distances
            _vertexIndices.Clear();

            for (int i = 0; i < _vertices.Length; i++)
            {
                _distances[i] = 999999;
                _parents[i] = -1;
                _vertexIndices.Add(i);
            }

            _distances[sourceVertexIndex] = 0;

            // Calculate shortest distances for all vertices
            while(_vertexIndices.Count > 0)
            {
                // Find the closest vertex to source
                int vertexIndex = _vertexIndices[0];

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
                    // Skip vertex i if not accessible or already removed from Q
                    if (_adjMatrix[vertexIndex, i] < 0 || !_vertexIndices.Contains(i))
                    {
                        continue;
                    }

                    // Update distance if shorter path found
                    float alt = _distances[vertexIndex] + _adjMatrix[vertexIndex, i];

                    if (alt < _distances[i])
                    {
                        _distances[i] = alt;
                        _parents[i] = vertexIndex;
                    }
                }
            }
        
            // Find shortest path
            int currentVertex = targetVertexIndex;
            _shortestPath.Clear();

            PathSegment pathSection;
            pathSection.VertexIndex = targetVertexIndex;
            pathSection.Distance = _distances[targetVertexIndex];
            pathSection.Position = _vertices[targetVertexIndex].Position;
            _shortestPath.Add(pathSection);

            // Start from to target vertex and find path back to the source vertex
            while (_parents[currentVertex] != -1)
            {
                int vertexIndex = _parents[currentVertex];

                pathSection.VertexIndex = vertexIndex;
                pathSection.Distance = _distances[vertexIndex];
                pathSection.Position = _vertices[vertexIndex].Position;
                _shortestPath.Insert(0, pathSection);

                currentVertex = vertexIndex;
            }

            if (currentVertex == sourceVertexIndex)
            {
                path = _shortestPath.ToArray();
                return true;
            }

            return false;
        }

        public bool CalculatePathWithAStar(int sourceVertexIndex, int targetVertexIndex, out PathSegment[] path)
        {
            path = new PathSegment[0];
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (_adjMatrix == null)
            {
                Debug.LogError("The AdjMatrix is null! Was the Graph Initialized?");
                return false;
            }
#endif
            _visitedVertex.Clear();
            _activeVertex.Clear();

            // Add starting node
            _activeVertex.Add(new AStarNode { VertexIndex = sourceVertexIndex, GCost = 0, HCost = 0, FCost = 0, Parent = -1 });

            while(_activeVertex.Count > 0)
            {
                int current = 0;

                // Find active node with lowest F cost
                for (int i = 1; i < _activeVertex.Count; i++)
                {
                    if (_activeVertex[i].FCost < _activeVertex[current].FCost
                    || (_activeVertex[i].FCost == _activeVertex[current].FCost || _activeVertex[i].HCost < _activeVertex[current].HCost))
                    {
                        current = i;
                    }
                }

                // Add node to visited ones and remove it from active ones
                _visitedVertex.Add(_activeVertex[current]);
                _activeVertex.RemoveAt(current);
                current = _visitedVertex.Count - 1;

                // Create the path if reached the target vertex
                if (_visitedVertex[current].VertexIndex == targetVertexIndex)
                {
                    PathSegment pathSection;
                    _shortestPath.Clear();

                    while(true)
                    {
                        pathSection.VertexIndex = _visitedVertex[current].VertexIndex;
                        pathSection.Distance = _visitedVertex[current].GCost;
                        pathSection.Position = _vertices[_visitedVertex[current].VertexIndex].Position;
                        _shortestPath.Insert(0, pathSection);

                        if (_visitedVertex[current].VertexIndex == sourceVertexIndex)
                        {
                            path = _shortestPath.ToArray();
                            return true;
                        }

                        current = _visitedVertex[current].Parent;
                    }
                }

                // Check all neighbours
                for (int i = 0; i < _vertices.Length; i++)
                {
                    // Skip neighbour if not traversable or already visited
                    if (_adjMatrix[_visitedVertex[current].VertexIndex, i] == -1 || _visitedVertex.FindIndex(node => node.VertexIndex == i) > -1)
                    {
                        continue;
                    }

                    int activeIndex = _activeVertex.FindIndex(node => node.VertexIndex == i);
                    float GCost = _adjMatrix[_visitedVertex[current].VertexIndex, i] + _visitedVertex[current].GCost;
                    float HCost = (_vertices[i].Position - _vertices[_visitedVertex[current].VertexIndex].Position).magnitude;

                    // Add the neighbour 
                    if (activeIndex == -1)
                    {
                        _activeVertex.Add(new AStarNode { VertexIndex = i,
                                                          GCost = GCost,
                                                          HCost = HCost,
                                                          FCost = GCost+ HCost,
                                                          Parent = current });

                        continue;
                    }

                    // Update the neighbour 
                    if (_activeVertex[activeIndex].FCost > GCost + HCost)
                    {
                        _activeVertex[activeIndex] = new AStarNode { VertexIndex = i,
                                                                     GCost = GCost,
                                                                     HCost = HCost,
                                                                     FCost = GCost + HCost,
                                                                     Parent = current };
                    }
                }
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
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (_indexes == null || _distances == null)
            {
                Debug.LogError("Some variables are null! Was the Graph Initialized?");
                return false;
            }
#endif
            for (int i = 0; i < _vertices.Length; i++)
            {
                _indexes[i] = i;
                _distances[i] = (position - _vertices[i].Position).magnitude;
            }

            QuickSortIndexesByDistance(_distances, _indexes, 0, _distances.Length - 1);

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

            const float infinity = 99999;
            float smallestDistance = infinity;

            // Find closest edge connected to the first vertexA
            foreach (Edge edge in _edges)
            {
                int secondVertex;

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
                Vector3 vertexAToSecond = _vertices[secondVertex].Position - _vertices[vertexA].Position;
                Vector3 vertexToPos = position - _vertices[vertexA].Position;
                float dotA = Vector3.Dot(vertexAToSecond, vertexToPos);

                Vector3 secondToVertexA = _vertices[vertexA].Position - _vertices[secondVertex].Position;
                vertexToPos = position - _vertices[secondVertex].Position;
                float dotB = Vector3.Dot(secondToVertexA, vertexToPos);

                // Check if position is aligned with the edge
                if (dotA * dotB > .0f)
                {
                    // Calculate projection directly since we already calculated the dot product
                    Vector3 projectedPosition = (dotA / vertexAToSecond.sqrMagnitude) * vertexAToSecond;

                    // Calculate the distance between the position and the projected position 
                    float distanceToPosition = (position - _vertices[vertexA].Position - projectedPosition).sqrMagnitude;

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

        private void QuickSortIndexesByDistance(float[] toSort, int[] aligned, int left, int right)
        {
            if (left < right)
            {
                int pivot = PartitionAlignedArrays(toSort, aligned, left, right);

                if (pivot > 1)
                {
                    QuickSortIndexesByDistance(toSort, aligned, left, pivot - 1);
                }

                if (pivot + 1 < right)
                {
                    QuickSortIndexesByDistance(toSort, aligned, pivot + 1, right);
                }
            }
        }

        private int PartitionAlignedArrays(float[] toSort, int[] aligned, int left, int right)
        {
            float pivot = toSort[left];

            while (true)
            {
                while (toSort[left] < pivot)
                {
                    left++;
                }

                while (toSort[right] > pivot)
                {
                    right--;
                }

                if (left < right)
                {
                    if (toSort[left] == toSort[right])
                    {
                        right--;
                    }

                    float temp = toSort[right];
                    toSort[right] = toSort[left];
                    toSort[left] = temp;

                    temp = aligned[right];
                    aligned[right] = aligned[left];
                    aligned[left] = (int)temp;
                }
                else
                {
                    return right;
                }
            }
        }
    }

    #if UNITY_EDITOR
    public partial class Graph
    {
        [HideInInspector]
        public int VertexIdCount = 0;
        [HideInInspector]
        public int EdgeIdCount = 0;

        [HideInInspector]
        public List<bool> EdgesFolded = new List<bool>();

        public void AddVertex(Vector3 position)
        {
            // Record the Graph before applying change to allow undo
            Undo.RecordObject(this, "Added Vertex");

            Vertex newVertex = new Vertex();
            newVertex.Id = GenerateUniqueVertexId();
            newVertex.Position = position;

            List<Vertex> tempVertices = new List<Vertex>(_vertices);
            tempVertices.Add(newVertex);

            _vertices = tempVertices.ToArray();
        }

        private int GenerateUniqueVertexId()
        {
            int newId = VertexIdCount;
            VertexIdCount++;

            return newId;
        }

        public bool RemoveVertex(int index)
        {
            if (index > _vertices.Length - 1)
            {
                return false;
            }

            // Record the Graph before applying change to allow undo
            Undo.RecordObject(this, "Removed Vertex");

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

        public List<string> GetAllVertexIds()
        {
            List<string> idList = new List<string>();

            foreach (Vertex vertex in Vertices)
            {
                idList.Add(vertex.Id.ToString());
            }

            return idList;
        }

        public bool AddEdge(int vertexA, int vertexB, EdgeDirection direction, bool traversable)
        {
            foreach (Edge edge in _edges)
            {
                if ((edge.VertexA == vertexA && edge.VertexB == vertexB) || (edge.VertexB == vertexA && edge.VertexA == vertexB))
                {
                    return false;
                }
            }

            // Record the Graph before applying change to allow undo
            Undo.RecordObject(this, "Added Edge");

            Edge newEdge = new Edge();
            newEdge.Id = GenerateUniqueEdgeId();
            newEdge.VertexA = vertexA;
            newEdge.VertexB = vertexB;
            newEdge.Direction = direction;
            newEdge.Traversable = traversable;

            List<Edge> tempEdges = new List<Edge>(_edges);
            tempEdges.Add(newEdge);

            _edges = tempEdges.ToArray();

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
            if (index > _edges.Length - 1)
            {
                return false;
            }

            // Record the Graph before applying change to allow undos
            Undo.RecordObject(this, "Removed Vertex");

            List<Edge> tempEdges = new List<Edge>(_edges);
            tempEdges.Remove(_edges[index]);

            _edges = tempEdges.ToArray();

            // Remove the edge foldout list entry
            EdgesFolded.RemoveAt(index);

            return true;
        }

        public void ClearEdges()
        {
            _edges = new Edge[0];
            EdgesFolded.Clear();
        }
    }
    #endif
}