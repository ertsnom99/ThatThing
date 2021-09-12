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

    [Serializable]
    public struct PositionOnGraph
    {
        // Index of the vertex
        public int VertexA;
        // Index of the vertex
        public int VertexB;
        public float Progress;
    }

    public struct PathSegment
    {
        public PositionOnGraph PositionOnGraph;
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

        public bool CalculatePathWithDijkstra(PositionOnGraph sourcePosition, PositionOnGraph targetPosition, out PathSegment[] path)
        {
            path = new PathSegment[0];
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (_adjMatrix == null)
            {
                Debug.LogError("The AdjMatrix is null! Was the Graph Initialized?");
                return false;
            }
#endif
            // Stop if the target is unreachable
            if (targetPosition.VertexB > -1 && _adjMatrix[targetPosition.VertexA, targetPosition.VertexB] == -1 && _adjMatrix[targetPosition.VertexB, targetPosition.VertexA] == -1)
            {
                return false;
            }

            // Rearrange positions if both are on the same edge
            if (targetPosition.VertexB > -1 && sourcePosition.VertexA == targetPosition.VertexB && sourcePosition.VertexB == targetPosition.VertexA)
            {
                sourcePosition.VertexA = targetPosition.VertexA;
                sourcePosition.VertexB = targetPosition.VertexB;
                sourcePosition.Progress = (_vertices[sourcePosition.VertexA].Position - _vertices[sourcePosition.VertexB].Position).magnitude - sourcePosition.Progress;
            }

            // Special case where sourcePosition and targetPosition are both on the same edge and the path can simply go from sourcePosition to targetPosition
            if (sourcePosition.VertexA == targetPosition.VertexA && sourcePosition.VertexB == targetPosition.VertexB) 
            {
                if (sourcePosition.Progress <= targetPosition.Progress && _adjMatrix[sourcePosition.VertexA, sourcePosition.VertexB] > -1)
                {
                    path = new PathSegment[2];
                    float AtoBMagnitude = (_vertices[sourcePosition.VertexA].Position - _vertices[sourcePosition.VertexB].Position).magnitude;

                    path[0].PositionOnGraph.VertexA = sourcePosition.VertexA;
                    path[0].PositionOnGraph.VertexB = sourcePosition.VertexB;
                    path[0].PositionOnGraph.Progress = sourcePosition.Progress;
                    path[0].Distance = 0;
                    path[0].Position = Vector3.Lerp(_vertices[sourcePosition.VertexA].Position, _vertices[sourcePosition.VertexB].Position, sourcePosition.Progress / AtoBMagnitude);

                    path[1].PositionOnGraph.VertexA = targetPosition.VertexA;
                    path[1].PositionOnGraph.VertexB = targetPosition.VertexB;
                    path[1].PositionOnGraph.Progress = targetPosition.Progress;
                    path[1].Distance = targetPosition.Progress - sourcePosition.Progress;
                    path[1].Position = Vector3.Lerp(_vertices[targetPosition.VertexA].Position, _vertices[targetPosition.VertexB].Position, targetPosition.Progress / AtoBMagnitude);

                    return true;
                }
                else if (sourcePosition.Progress > targetPosition.Progress && _adjMatrix[sourcePosition.VertexB, sourcePosition.VertexA] > -1)
                {
                    path = new PathSegment[2];
                    float AtoBMagnitude = (_vertices[sourcePosition.VertexA].Position - _vertices[sourcePosition.VertexB].Position).magnitude;

                    path[0].PositionOnGraph.VertexA = sourcePosition.VertexB;
                    path[0].PositionOnGraph.VertexB = sourcePosition.VertexA;
                    path[0].PositionOnGraph.Progress = AtoBMagnitude - sourcePosition.Progress;
                    path[0].Distance = 0;
                    path[0].Position = Vector3.Lerp(_vertices[sourcePosition.VertexA].Position, _vertices[sourcePosition.VertexB].Position, sourcePosition.Progress / AtoBMagnitude);

                    path[1].PositionOnGraph.VertexA = targetPosition.VertexB;
                    path[1].PositionOnGraph.VertexB = targetPosition.VertexA;
                    path[1].PositionOnGraph.Progress = AtoBMagnitude - targetPosition.Progress;
                    path[1].Distance = sourcePosition.Progress - targetPosition.Progress;
                    path[1].Position = Vector3.Lerp(_vertices[targetPosition.VertexA].Position, _vertices[targetPosition.VertexB].Position, targetPosition.Progress / AtoBMagnitude);

                    return true;
                }
            }

            _vertexIndices.Clear();
            const int infiniteDistance = 999999;

            for (int i = 0; i < _vertices.Length; i++)
            {
                _distances[i] = infiniteDistance;
                _parents[i] = -1;
                _vertexIndices.Add(i);
            }

            // Add the distances of the source position vertices
            if (sourcePosition.VertexB > -1)
            {
                if (_adjMatrix[sourcePosition.VertexA, sourcePosition.VertexB] > -1)
                {
                    _distances[sourcePosition.VertexB] = (_vertices[sourcePosition.VertexA].Position - _vertices[sourcePosition.VertexB].Position).magnitude - sourcePosition.Progress;
                }

                if (_adjMatrix[sourcePosition.VertexB, sourcePosition.VertexA] > -1)
                {
                    _distances[sourcePosition.VertexA] = sourcePosition.Progress;
                }
            }
            else
            {
                _distances[sourcePosition.VertexA] = 0;
            }

            // Calculate shortest distances for all vertices
            while (_vertexIndices.Count > 0)
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

            _shortestPath.Clear();
            PathSegment pathSection;
            int currentVertex = targetPosition.VertexA;

            // Add an extra position if the target position is along an edge
            if (targetPosition.VertexB > -1)
            {
                float AtoBMagnitude = (_vertices[targetPosition.VertexA].Position - _vertices[targetPosition.VertexB].Position).magnitude;

                if (_adjMatrix[targetPosition.VertexB, targetPosition.VertexA] == -1 || (_adjMatrix[targetPosition.VertexA, targetPosition.VertexB] > -1 && _distances[targetPosition.VertexA] + targetPosition.Progress <= _distances[targetPosition.VertexB] + AtoBMagnitude - targetPosition.Progress))
                {
                    currentVertex = targetPosition.VertexA;
                    pathSection.PositionOnGraph.VertexB = targetPosition.VertexB;
                    pathSection.PositionOnGraph.Progress = targetPosition.Progress;
                }
                else
                {
                    currentVertex = targetPosition.VertexB;
                    pathSection.PositionOnGraph.VertexB = targetPosition.VertexA;
                    pathSection.PositionOnGraph.Progress = AtoBMagnitude - targetPosition.Progress;
                }

                pathSection.PositionOnGraph.VertexA = currentVertex;
                pathSection.Distance = _distances[currentVertex] + pathSection.PositionOnGraph.Progress;
                pathSection.Position = Vector3.Lerp(_vertices[targetPosition.VertexA].Position, _vertices[targetPosition.VertexB].Position, targetPosition.Progress / AtoBMagnitude);

                _shortestPath.Add(pathSection);
            }

            // Path doesn't exist if currentVertex can't be reached
            if (_distances[currentVertex] == infiniteDistance)
            {
                return false;
            }

            // Add the last vertex of the path
            pathSection.PositionOnGraph.VertexA = currentVertex;
            pathSection.PositionOnGraph.VertexB = -1;
            pathSection.PositionOnGraph.Progress = 0;
            pathSection.Distance = _distances[currentVertex];
            pathSection.Position = _vertices[currentVertex].Position;

            _shortestPath.Insert(0, pathSection);

            // Start from the current vertex and find a path back to the vertex closest to the source
            while (_parents[currentVertex] != -1)
            {
                int vertexIndex = _parents[currentVertex];

                pathSection.PositionOnGraph.VertexA = vertexIndex;
                pathSection.PositionOnGraph.VertexB = -1;
                pathSection.PositionOnGraph.Progress = 0;
                pathSection.Distance = _distances[vertexIndex];
                pathSection.Position = _vertices[vertexIndex].Position;
                _shortestPath.Insert(0, pathSection);

                currentVertex = vertexIndex;
            }

            // Add an extra position if the source position is along an edge
            if (_shortestPath[0].Distance > 0)
            {
                float AtoBMagnitude = (_vertices[sourcePosition.VertexA].Position - _vertices[sourcePosition.VertexB].Position).magnitude;

                if (currentVertex == sourcePosition.VertexA)
                {
                    pathSection.PositionOnGraph.VertexA = sourcePosition.VertexB;
                    pathSection.PositionOnGraph.VertexB = sourcePosition.VertexA;
                    pathSection.PositionOnGraph.Progress = AtoBMagnitude - sourcePosition.Progress;
                    pathSection.Distance = 0;
                    pathSection.Position = Vector3.Lerp(_vertices[sourcePosition.VertexA].Position, _vertices[sourcePosition.VertexB].Position, sourcePosition.Progress / AtoBMagnitude); ;
                    
                    _shortestPath.Insert(0, pathSection);
                }
                else
                {
                    pathSection.PositionOnGraph.VertexA = sourcePosition.VertexA;
                    pathSection.PositionOnGraph.VertexB = sourcePosition.VertexB;
                    pathSection.PositionOnGraph.Progress = sourcePosition.Progress;
                    pathSection.Distance = 0;
                    pathSection.Position = Vector3.Lerp(_vertices[sourcePosition.VertexA].Position, _vertices[sourcePosition.VertexB].Position, sourcePosition.Progress / AtoBMagnitude);
                    
                    _shortestPath.Insert(0, pathSection);
                }
            }

            // If a path was found
            path = _shortestPath.ToArray();
            return true;
        }

        public bool CalculatePathWithAStar(PositionOnGraph sourcePosition, PositionOnGraph targetPosition, out PathSegment[] path)
        {
            path = new PathSegment[0];
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (_adjMatrix == null)
            {
                Debug.LogError("The AdjMatrix is null! Was the Graph Initialized?");
                return false;
            }
#endif
            bool canEndAtVertexA = true;
            bool canEndAtVertexB = false;

            // Store at which vertex A* can end and stop if the target is unreachable
            if (targetPosition.VertexB > -1)
            {
                canEndAtVertexA = _adjMatrix[targetPosition.VertexA, targetPosition.VertexB] > -1;
                canEndAtVertexB = _adjMatrix[targetPosition.VertexB, targetPosition.VertexA] > -1;

                if (!canEndAtVertexA && !canEndAtVertexB)
                {
                    return false;
                }
            }

            // Rearrange positions if both are on the same edge
            if (targetPosition.VertexB > -1 && sourcePosition.VertexA == targetPosition.VertexB && sourcePosition.VertexB == targetPosition.VertexA)
            {
                sourcePosition.VertexA = targetPosition.VertexA;
                sourcePosition.VertexB = targetPosition.VertexB;
                sourcePosition.Progress = (_vertices[sourcePosition.VertexA].Position - _vertices[sourcePosition.VertexB].Position).magnitude - sourcePosition.Progress;
            }

            // Special case where sourcePosition and targetPosition are both on the same edge and the path can simply go from sourcePosition to targetPosition
            if (sourcePosition.VertexA == targetPosition.VertexA && sourcePosition.VertexB == targetPosition.VertexB)
            {
                if (sourcePosition.Progress <= targetPosition.Progress && _adjMatrix[sourcePosition.VertexA, sourcePosition.VertexB] > -1)
                {
                    path = new PathSegment[2];
                    float AtoBMagnitude = (_vertices[sourcePosition.VertexA].Position - _vertices[sourcePosition.VertexB].Position).magnitude;

                    path[0].PositionOnGraph.VertexA = sourcePosition.VertexA;
                    path[0].PositionOnGraph.VertexB = sourcePosition.VertexB;
                    path[0].PositionOnGraph.Progress = sourcePosition.Progress;
                    path[0].Distance = 0;
                    path[0].Position = Vector3.Lerp(_vertices[sourcePosition.VertexA].Position, _vertices[sourcePosition.VertexB].Position, sourcePosition.Progress / AtoBMagnitude);

                    path[1].PositionOnGraph.VertexA = targetPosition.VertexA;
                    path[1].PositionOnGraph.VertexB = targetPosition.VertexB;
                    path[1].PositionOnGraph.Progress = targetPosition.Progress;
                    path[1].Distance = targetPosition.Progress - sourcePosition.Progress;
                    path[1].Position = Vector3.Lerp(_vertices[targetPosition.VertexA].Position, _vertices[targetPosition.VertexB].Position, targetPosition.Progress / AtoBMagnitude);

                    return true;
                }
                else if (sourcePosition.Progress > targetPosition.Progress && _adjMatrix[sourcePosition.VertexB, sourcePosition.VertexA] > -1)
                {
                    path = new PathSegment[2];
                    float AtoBMagnitude = (_vertices[sourcePosition.VertexA].Position - _vertices[sourcePosition.VertexB].Position).magnitude;

                    path[0].PositionOnGraph.VertexA = sourcePosition.VertexB;
                    path[0].PositionOnGraph.VertexB = sourcePosition.VertexA;
                    path[0].PositionOnGraph.Progress = AtoBMagnitude - sourcePosition.Progress;
                    path[0].Distance = 0;
                    path[0].Position = Vector3.Lerp(_vertices[sourcePosition.VertexA].Position, _vertices[sourcePosition.VertexB].Position, sourcePosition.Progress / AtoBMagnitude);

                    path[1].PositionOnGraph.VertexA = targetPosition.VertexB;
                    path[1].PositionOnGraph.VertexB = targetPosition.VertexA;
                    path[1].PositionOnGraph.Progress = AtoBMagnitude - targetPosition.Progress;
                    path[1].Distance = sourcePosition.Progress - targetPosition.Progress;
                    path[1].Position = Vector3.Lerp(_vertices[targetPosition.VertexA].Position, _vertices[targetPosition.VertexB].Position, targetPosition.Progress / AtoBMagnitude);

                    return true;
                }
            }

            Vector3 endPosition;

            // Store the end position
            if (targetPosition.VertexB > -1)
            {
                float progress = targetPosition.Progress / (_vertices[targetPosition.VertexB].Position - _vertices[targetPosition.VertexA].Position).magnitude;
                endPosition = Vector3.Lerp(_vertices[targetPosition.VertexA].Position, _vertices[targetPosition.VertexB].Position, progress);
            }
            else
            {
                endPosition = _vertices[targetPosition.VertexA].Position;
            }

            _visitedVertex.Clear();
            _activeVertex.Clear();

            // Add starting nodes
            if (sourcePosition.VertexB > -1)
            {
                if (_adjMatrix[sourcePosition.VertexA, sourcePosition.VertexB] > -1)
                {
                    float GCost = (_vertices[sourcePosition.VertexA].Position - _vertices[sourcePosition.VertexB].Position).magnitude - sourcePosition.Progress;
                    float HCost = (endPosition - _vertices[sourcePosition.VertexB].Position).magnitude;

                    _activeVertex.Add(new AStarNode { VertexIndex = sourcePosition.VertexB, GCost = GCost, HCost = HCost, FCost = GCost + HCost, Parent = -1 });
                }

                if (_adjMatrix[sourcePosition.VertexB, sourcePosition.VertexA] > -1)
                {
                    float GCost = sourcePosition.Progress;
                    float HCost = (endPosition - _vertices[sourcePosition.VertexA].Position).magnitude;

                    _activeVertex.Add(new AStarNode { VertexIndex = sourcePosition.VertexA, GCost = GCost, HCost = HCost, FCost = GCost + HCost, Parent = -1 });
                }
            }
            else
            {
                _activeVertex.Add(new AStarNode { VertexIndex = sourcePosition.VertexA, GCost = 0, HCost = 0, FCost = 0, Parent = -1 });
            }

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
                if ((_visitedVertex[current].VertexIndex == targetPosition.VertexA && canEndAtVertexA) ||
                    (_visitedVertex[current].VertexIndex == targetPosition.VertexB && canEndAtVertexB))
                {
                    PathSegment pathSection;
                    _shortestPath.Clear();

                    // Add an extra position if the target position is along an edge
                    if (targetPosition.VertexB > -1)
                    {
                        float AtoBMagnitude = (_vertices[targetPosition.VertexA].Position - _vertices[targetPosition.VertexB].Position).magnitude;

                        if (_visitedVertex[current].VertexIndex == targetPosition.VertexA)
                        {
                            pathSection.PositionOnGraph.VertexA = targetPosition.VertexA;
                            pathSection.PositionOnGraph.VertexB = targetPosition.VertexB;
                            pathSection.PositionOnGraph.Progress = targetPosition.Progress;
                        }
                        else
                        {
                            pathSection.PositionOnGraph.VertexA = targetPosition.VertexB;
                            pathSection.PositionOnGraph.VertexB = targetPosition.VertexA;
                            pathSection.PositionOnGraph.Progress = AtoBMagnitude - targetPosition.Progress;
                        }

                        pathSection.Distance = _visitedVertex[current].GCost + pathSection.PositionOnGraph.Progress;
                        pathSection.Position = Vector3.Lerp(_vertices[targetPosition.VertexA].Position, _vertices[targetPosition.VertexB].Position, targetPosition.Progress / AtoBMagnitude);

                        _shortestPath.Add(pathSection);
                    }

                    // Start from the current visited vertex and find a path back to the vertex closest to the source
                    while (true)
                    {
                        pathSection.PositionOnGraph.VertexA = _visitedVertex[current].VertexIndex;
                        pathSection.PositionOnGraph.VertexB = -1;
                        pathSection.PositionOnGraph.Progress = 0;
                        pathSection.Distance = _visitedVertex[current].GCost;
                        pathSection.Position = _vertices[_visitedVertex[current].VertexIndex].Position;
                        _shortestPath.Insert(0, pathSection);

                        if (_visitedVertex[current].Parent == -1)
                        {
                            break;
                        }
                        
                        current = _visitedVertex[current].Parent;
                    }

                    // Add an extra position if the source position is along an edge
                    if (_visitedVertex[current].FCost > 0)
                    {
                        float AtoBMagnitude = (_vertices[sourcePosition.VertexA].Position - _vertices[sourcePosition.VertexB].Position).magnitude;

                        if (_visitedVertex[current].VertexIndex == sourcePosition.VertexA)
                        {
                            pathSection.PositionOnGraph.VertexA = sourcePosition.VertexB;
                            pathSection.PositionOnGraph.VertexB = sourcePosition.VertexA;
                            pathSection.PositionOnGraph.Progress = AtoBMagnitude - sourcePosition.Progress;
                            pathSection.Distance = 0;
                            pathSection.Position = Vector3.Lerp(_vertices[sourcePosition.VertexA].Position, _vertices[sourcePosition.VertexB].Position, sourcePosition.Progress / AtoBMagnitude); ;

                            _shortestPath.Insert(0, pathSection);
                        }
                        else
                        {
                            pathSection.PositionOnGraph.VertexA = sourcePosition.VertexA;
                            pathSection.PositionOnGraph.VertexB = sourcePosition.VertexB;
                            pathSection.PositionOnGraph.Progress = sourcePosition.Progress;
                            pathSection.Distance = 0;
                            pathSection.Position = Vector3.Lerp(_vertices[sourcePosition.VertexA].Position, _vertices[sourcePosition.VertexB].Position, sourcePosition.Progress / AtoBMagnitude);

                            _shortestPath.Insert(0, pathSection);
                        }
                    }

                    path = _shortestPath.ToArray();
                    return true;
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
                    float HCost = (endPosition - _vertices[_visitedVertex[current].VertexIndex].Position).magnitude;

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

        // Takes a world position and finds the closest PositionOnGraph.
        // Returns true if the conversion was successful. 
        public bool ConvertPositionToGraph(Vector3 position, LayerMask blockingMask, out PositionOnGraph positionOnGraph)
        {
            // Reset variables
            positionOnGraph.VertexA = -1;
            positionOnGraph.VertexB = -1;
            positionOnGraph.Progress = .0f;
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
                    positionOnGraph.VertexA = _indexes[i];
                    break;
                }
            }

            // The position can't be converted to graph if no vertexA could be found
            if (positionOnGraph.VertexA == -1)
            {
                return false;
            }

            const float infinity = 99999;
            float smallestDistance = infinity;

            // Find closest edge connected to the first vertexA
            foreach (Edge edge in _edges)
            {
                int secondVertex;

                if (edge.VertexA == positionOnGraph.VertexA)
                {
                    secondVertex = edge.VertexB;
                }
                else if (edge.VertexB == positionOnGraph.VertexA)
                {
                    secondVertex = edge.VertexA;
                }
                else
                {
                    continue;
                }

                // Find progress along the edge
                Vector3 vertexAToSecond = _vertices[secondVertex].Position - _vertices[positionOnGraph.VertexA].Position;
                Vector3 vertexToPos = position - _vertices[positionOnGraph.VertexA].Position;
                float dotA = Vector3.Dot(vertexAToSecond, vertexToPos);

                Vector3 secondToVertexA = _vertices[positionOnGraph.VertexA].Position - _vertices[secondVertex].Position;
                vertexToPos = position - _vertices[secondVertex].Position;
                float dotB = Vector3.Dot(secondToVertexA, vertexToPos);

                // Check if position is aligned with the edge
                if (dotA * dotB > .0f)
                {
                    // Calculate projection directly since we already calculated the dot product
                    Vector3 projectedPosition = (dotA / vertexAToSecond.sqrMagnitude) * vertexAToSecond;

                    // Calculate the distance between the position and the projected position 
                    float distanceToPosition = (position - _vertices[positionOnGraph.VertexA].Position - projectedPosition).sqrMagnitude;

                    if (distanceToPosition < smallestDistance)
                    {
                        positionOnGraph.VertexB = secondVertex;
                        positionOnGraph.Progress = projectedPosition.magnitude;
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

            foreach (Vertex vertex in _vertices)
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