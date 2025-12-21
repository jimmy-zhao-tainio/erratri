using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Geometry;

namespace Kernel;

// Higher-level helper for extracting reasonably clean closed intersection
// curves on a single mesh (A or B) from the raw IntersectionGraph /
// mesh-local topology data.
//
// This layer:
//   - works per connected component on a chosen mesh,
//   - classifies components using simple degree/length heuristics,
//   - optionally repairs a single small gap in strong loop candidates by
//     adding a synthetic closure edge between nearby degree-1 endpoints,
//   - returns 2-regular simple cycles as IntersectionCurve instances.
public static class IntersectionCurveRegularizer
{
    public enum ComponentClassification
    {
        StrongLoopCandidate,
        TinyNoise,
        Ambiguous
    }

    public sealed class ComponentStats
    {
        public int Index { get; }
        public int VertexCount { get; }
        public int EdgeCount { get; }
        public int Degree1Count { get; }
        public int Degree2Count { get; }
        public int DegreeMoreCount { get; }
        public double TotalLength { get; }
        public double MedianEdgeLength { get; }
        public ComponentClassification Classification { get; }

        internal ComponentStats(
            int index,
            int vertexCount,
            int edgeCount,
            int degree1Count,
            int degree2Count,
            int degreeMoreCount,
            double totalLength,
            double medianEdgeLength,
            ComponentClassification classification)
        {
            Index = index;
            VertexCount = vertexCount;
            EdgeCount = edgeCount;
            Degree1Count = degree1Count;
            Degree2Count = degree2Count;
            DegreeMoreCount = degreeMoreCount;
            TotalLength = totalLength;
            MedianEdgeLength = medianEdgeLength;
            Classification = classification;
        }
    }

    public sealed class Result
    {
        public IReadOnlyList<IntersectionCurve> Curves { get; }
        public IReadOnlyList<ComponentStats> Components { get; }

        internal Result(
            IReadOnlyList<IntersectionCurve> curves,
            IReadOnlyList<ComponentStats> components)
        {
            Curves = curves ?? throw new ArgumentNullException(nameof(curves));
            Components = components ?? throw new ArgumentNullException(nameof(components));
        }
    }

    private sealed class Component
    {
        public HashSet<int> Vertices { get; }
        public HashSet<int> Edges { get; }

        public Component(HashSet<int> vertices, HashSet<int> edges)
        {
            Vertices = vertices;
            Edges = edges;
        }
    }

    public static Result RegularizeMeshA(IntersectionGraph graph, MeshATopology topology)
    {
        if (graph is null) throw new ArgumentNullException(nameof(graph));
        if (topology is null) throw new ArgumentNullException(nameof(topology));

        return RegularizeInternal(graph, topology.VertexEdges, topology.Edges);
    }

    public static Result RegularizeMeshB(IntersectionGraph graph, MeshBTopology topology)
    {
        if (graph is null) throw new ArgumentNullException(nameof(graph));
        if (topology is null) throw new ArgumentNullException(nameof(topology));

        return RegularizeInternal(graph, topology.VertexEdges, topology.Edges);
    }

    private static Result RegularizeInternal(
        IntersectionGraph graph,
        IReadOnlyDictionary<IntersectionVertexId, IReadOnlyList<IntersectionEdgeId>> adjacency,
        IReadOnlyList<IntersectionEdgeId> meshEdgeList)
    {
        var positions = BuildVertexPositionMap(graph);

        var edgeById = new Dictionary<int, (IntersectionVertexId Start, IntersectionVertexId End)>();
        foreach (var (id, start, end) in graph.Edges)
        {
            edgeById[id.Value] = (start, end);
        }

        var meshEdges = new HashSet<int>();
        for (int i = 0; i < meshEdgeList.Count; i++)
        {
            var edgeId = meshEdgeList[i];
            meshEdges.Add(edgeId.Value);
        }

        var components = BuildComponentsOnMeshA(adjacency, edgeById, meshEdges);

        var curves = new List<IntersectionCurve>();
        var statsList = new List<ComponentStats>(components.Count);

        int componentIndex = 0;
        int nextSyntheticEdgeId = -1;

        foreach (var component in components)
        {
            var stats = ComputeComponentStats(
                componentIndex,
                component,
                adjacency,
                positions,
                edgeById,
                out var degrees);

            statsList.Add(stats);

            if (stats.Classification == ComponentClassification.StrongLoopCandidate)
            {
                if (stats.Degree1Count == 0)
                {
                    if (TryBuildCurveFromTwoRegularComponent(
                            component.Vertices,
                            component.Edges,
                            edgeById,
                            positions,
                            syntheticEdgesForComponent: null,
                            out var curve))
                    {
                        curves.Add(curve!);
                    }
                }
                else if (stats.Degree1Count == 2)
                {
                    int endpointA = -1;
                    int endpointB = -1;

                    foreach (var kvp in degrees)
                    {
                        if (kvp.Value == 1)
                        {
                            if (endpointA == -1)
                            {
                                endpointA = kvp.Key;
                            }
                            else
                            {
                                endpointB = kvp.Key;
                            }
                        }
                    }

                    if (endpointA != -1 && endpointB != -1 && endpointA != endpointB)
                    {
                        if (positions.TryGetValue(endpointA, out var pA) &&
                            positions.TryGetValue(endpointB, out var pB))
                        {
                            double distance = pA.Distance(in pB);

                            double median = stats.MedianEdgeLength;
                            double total = stats.TotalLength;
                            if (median > 0.0 && total > 0.0)
                            {
                                double threshold = Math.Max(3.0 * median, 0.25 * total);
                                if (distance <= threshold)
                                {
                                    var verticesWithClosure = new HashSet<int>(component.Vertices);
                                    var edgesWithClosure = new HashSet<int>(component.Edges);

                                    int closureId = nextSyntheticEdgeId--;
                                    edgesWithClosure.Add(closureId);

                                    var vA = new IntersectionVertexId(endpointA);
                                    var vB = new IntersectionVertexId(endpointB);
                                    edgeById[closureId] = (vA, vB);

                                    var syntheticEdgesForComponent = new HashSet<int> { closureId };

                                    if (TryBuildCurveFromTwoRegularComponent(
                                            verticesWithClosure,
                                            edgesWithClosure,
                                            edgeById,
                                            positions,
                                            syntheticEdgesForComponent,
                                            out var curve))
                                    {
                                        curves.Add(curve!);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            componentIndex++;
        }

        return new Result(curves, statsList);
    }

    private static List<Component> BuildComponentsOnMeshA(
        IReadOnlyDictionary<IntersectionVertexId, IReadOnlyList<IntersectionEdgeId>> adjacency,
        Dictionary<int, (IntersectionVertexId Start, IntersectionVertexId End)> edgeById,
        HashSet<int> meshEdges)
    {
        var components = new List<Component>();
        var visitedVertices = new HashSet<int>();

        foreach (var kvp in adjacency)
        {
            int rootId = kvp.Key.Value;
            if (!visitedVertices.Add(rootId))
            {
                continue;
            }

            var vertexSet = new HashSet<int>();
            var edgeSet = new HashSet<int>();
            var queue = new Queue<IntersectionVertexId>();
            queue.Enqueue(kvp.Key);

            while (queue.Count > 0)
            {
                var v = queue.Dequeue();
                vertexSet.Add(v.Value);

                if (!adjacency.TryGetValue(v, out var incidentEdges))
                {
                    continue;
                }

                for (int i = 0; i < incidentEdges.Count; i++)
                {
                    var edgeId = incidentEdges[i];
                    if (!meshEdges.Contains(edgeId.Value))
                    {
                        continue;
                    }

                    if (edgeSet.Add(edgeId.Value))
                    {
                        var endpoints = edgeById[edgeId.Value];
                        var other = endpoints.Start.Value == v.Value ? endpoints.End : endpoints.Start;

                        if (visitedVertices.Add(other.Value))
                        {
                            queue.Enqueue(other);
                        }
                    }
                }
            }

            if (vertexSet.Count > 0 && edgeSet.Count > 0)
            {
                components.Add(new Component(vertexSet, edgeSet));
            }
        }

        return components;
    }

    private static Dictionary<int, RealPoint> BuildVertexPositionMap(IntersectionGraph graph)
    {
        var positions = new Dictionary<int, RealPoint>();
        foreach (var (id, position) in graph.Vertices)
        {
            positions[id.Value] = position;
        }

        return positions;
    }

    private static ComponentStats ComputeComponentStats(
        int index,
        Component component,
        IReadOnlyDictionary<IntersectionVertexId, IReadOnlyList<IntersectionEdgeId>> adjacency,
        Dictionary<int, RealPoint> positions,
        Dictionary<int, (IntersectionVertexId Start, IntersectionVertexId End)> edgeById,
        out Dictionary<int, int> degrees)
    {
        degrees = new Dictionary<int, int>(component.Vertices.Count);

        foreach (var vId in component.Vertices)
        {
            var vertex = new IntersectionVertexId(vId);
            int d = 0;

            if (adjacency.TryGetValue(vertex, out var incident))
            {
                for (int i = 0; i < incident.Count; i++)
                {
                    var edgeId = incident[i];
                    if (component.Edges.Contains(edgeId.Value))
                    {
                        d++;
                    }
                }
            }

            degrees[vId] = d;
        }

        int degree1Count = 0;
        int degree2Count = 0;
        int degreeMoreCount = 0;

        foreach (var kvp in degrees)
        {
            int d = kvp.Value;
            if (d == 1)
            {
                degree1Count++;
            }
            else if (d == 2)
            {
                degree2Count++;
            }
            else if (d > 2)
            {
                degreeMoreCount++;
            }
        }

        var edgeLengths = new List<double>(component.Edges.Count);
        double totalLength = 0.0;

        foreach (var edgeValue in component.Edges)
        {
            var endpoints = edgeById[edgeValue];
            if (!positions.TryGetValue(endpoints.Start.Value, out var p0) ||
                !positions.TryGetValue(endpoints.End.Value, out var p1))
            {
                continue;
            }

            double length = p0.Distance(in p1);

            if (length > 0.0)
            {
                edgeLengths.Add(length);
                totalLength += length;
            }
        }

        double medianEdgeLength = edgeLengths.Median();

        ComponentClassification classification;

        if (component.Edges.Count <= 3 && medianEdgeLength > 0.0 && totalLength <= 2.0 * medianEdgeLength)
        {
            classification = ComponentClassification.TinyNoise;
        }
        else if (degreeMoreCount == 0 &&
                 degree1Count <= 2 &&
                 component.Edges.Count >= 4 &&
                 medianEdgeLength > 0.0 &&
                 totalLength >= 4.0 * medianEdgeLength)
        {
            classification = ComponentClassification.StrongLoopCandidate;
        }
        else
        {
            classification = ComponentClassification.Ambiguous;
        }

        return new ComponentStats(
            index,
            component.Vertices.Count,
            component.Edges.Count,
            degree1Count,
            degree2Count,
            degreeMoreCount,
            totalLength,
            medianEdgeLength,
            classification);
    }

    private static bool TryBuildCurveFromTwoRegularComponent(
        HashSet<int> componentVertices,
        HashSet<int> componentEdges,
        Dictionary<int, (IntersectionVertexId Start, IntersectionVertexId End)> edgeById,
        Dictionary<int, RealPoint> positions,
        HashSet<int>? syntheticEdgesForComponent,
        out IntersectionCurve? curve)
    {
        curve = null;

        if (componentVertices.Count < 3 || componentEdges.Count < 3)
        {
            return false;
        }

        double totalLength = 0.0;
        var adjacency = new Dictionary<IntersectionVertexId, List<IntersectionEdgeId>>();

        static void AddEdgeToAdjacency(
            Dictionary<IntersectionVertexId, List<IntersectionEdgeId>> adj,
            IntersectionVertexId vertex,
            IntersectionEdgeId edge)
        {
            if (!adj.TryGetValue(vertex, out var list))
            {
                list = new List<IntersectionEdgeId>();
                adj[vertex] = list;
            }

            list.Add(edge);
        }

        foreach (var edgeValue in componentEdges)
        {
            var endpoints = edgeById[edgeValue];
            var edgeId = new IntersectionEdgeId(edgeValue);
            AddEdgeToAdjacency(adjacency, endpoints.Start, edgeId);
            AddEdgeToAdjacency(adjacency, endpoints.End, edgeId);
        }

        if (adjacency.Count == 0)
        {
            return false;
        }

        var remainingEdges = new HashSet<int>(componentEdges);
        int seedEdgeValue = 0;
        foreach (var value in remainingEdges)
        {
            seedEdgeValue = value;
            break;
        }

        if (!edgeById.TryGetValue(seedEdgeValue, out var seedEndpoints))
        {
            return false;
        }

        var startVertex = seedEndpoints.Start;
        var currentVertex = seedEndpoints.End;

        var vertexLoop = new List<IntersectionVertexId>
        {
            startVertex,
            currentVertex
        };

        var edgeLoop = new List<IntersectionEdgeId>
        {
            new IntersectionEdgeId(seedEdgeValue)
        };

        var closureFlags = new List<bool>
        {
            syntheticEdgesForComponent != null && syntheticEdgesForComponent.Contains(seedEdgeValue)
        };

        remainingEdges.Remove(seedEdgeValue);

        while (true)
        {
            if (!adjacency.TryGetValue(currentVertex, out var incidentEdges))
            {
                break;
            }

            IntersectionEdgeId nextEdge = default;
            bool foundNext = false;

            for (int i = 0; i < incidentEdges.Count; i++)
            {
                var candidate = incidentEdges[i];
                if (!componentEdges.Contains(candidate.Value))
                {
                    continue;
                }

                if (!remainingEdges.Contains(candidate.Value))
                {
                    continue;
                }

                nextEdge = candidate;
                foundNext = true;
                break;
            }

            if (!foundNext)
            {
                break;
            }

            remainingEdges.Remove(nextEdge.Value);

            var nextEndpoints = edgeById[nextEdge.Value];
            var nextVertex = nextEndpoints.Start.Value == currentVertex.Value
                ? nextEndpoints.End
                : nextEndpoints.Start;

            edgeLoop.Add(nextEdge);
            bool isClosure = syntheticEdgesForComponent != null && syntheticEdgesForComponent.Contains(nextEdge.Value);
            closureFlags.Add(isClosure);

            vertexLoop.Add(nextVertex);
            currentVertex = nextVertex;

            if (currentVertex.Value == startVertex.Value)
            {
                break;
            }
        }

        if (vertexLoop.Count < 3 ||
            vertexLoop[0].Value != vertexLoop[^1].Value ||
            remainingEdges.Count != 0)
        {
            return false;
        }

        for (int i = 0; i < edgeLoop.Count; i++)
        {
            int edgeValue = edgeLoop[i].Value;
            var endpoints = edgeById[edgeValue];

            if (!positions.TryGetValue(endpoints.Start.Value, out var p0) ||
                !positions.TryGetValue(endpoints.End.Value, out var p1))
            {
                continue;
            }

            totalLength += p0.Distance(in p1);
        }

        var verticesArray = ImmutableArray.Create(vertexLoop.ToArray());
        var edgesArray = ImmutableArray.Create(edgeLoop.ToArray());
        var closureArray = ImmutableArray.Create(closureFlags.ToArray());

        curve = new IntersectionCurve(verticesArray, edgesArray, closureArray, totalLength);
        return true;
    }
}
