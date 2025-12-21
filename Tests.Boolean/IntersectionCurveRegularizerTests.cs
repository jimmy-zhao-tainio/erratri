using System;
using System.Collections.Generic;
using System.Reflection;
using Geometry;
using Boolean;
using Xunit;

namespace Boolean.Tests;

public class IntersectionCurveRegularizerTests
{
    [Fact]
    public void RegularizeMeshA_PureLoop_ReturnsSingleCurveCoveringAllEdges()
    {
        var (graph, topology) = CreateSyntheticGraphWithPureLoop();

        var result = IntersectionCurveRegularizer.RegularizeMeshA(graph, topology);

        Assert.Single(result.Components);
        Assert.Equal(IntersectionCurveRegularizer.ComponentClassification.StrongLoopCandidate,
            result.Components[0].Classification);

        Assert.Single(result.Curves);
        var curve = result.Curves[0];

        Assert.True(curve.Vertices.Length >= 3);
        Assert.Equal(curve.Vertices[0].Value, curve.Vertices[^1].Value);

        var meshEdgeIds = new HashSet<int>();
        foreach (var edgeId in topology.Edges)
        {
            meshEdgeIds.Add(edgeId.Value);
        }

        var curveEdgeIds = new HashSet<int>();
        foreach (var edgeId in curve.Edges)
        {
            curveEdgeIds.Add(edgeId.Value);
        }

        Assert.Equal(meshEdgeIds.Count, curveEdgeIds.Count);
        Assert.Subset(meshEdgeIds, curveEdgeIds);
        Assert.DoesNotContain(true, curve.IsClosureEdge);
    }

    [Fact]
    public void RegularizeMeshA_LoopPlusTinyChain_NoiseIsIgnored_MainLoopReturned()
    {
        var (graph, topology) = CreateSyntheticGraphWithLoopAndTinyChain();

        var result = IntersectionCurveRegularizer.RegularizeMeshA(graph, topology);

        Assert.Equal(2, result.Components.Count);

        int strongCount = 0;
        int tinyNoiseCount = 0;
        foreach (var stats in result.Components)
        {
            if (stats.Classification == IntersectionCurveRegularizer.ComponentClassification.StrongLoopCandidate)
                strongCount++;
            if (stats.Classification == IntersectionCurveRegularizer.ComponentClassification.TinyNoise)
                tinyNoiseCount++;
        }

        Assert.Equal(1, strongCount);
        Assert.Equal(1, tinyNoiseCount);

        Assert.Single(result.Curves);
        var curve = result.Curves[0];

        // Main loop uses only the four loop edges (ids 0..3).
        foreach (var edge in curve.Edges)
        {
            Assert.InRange(edge.Value, 0, 3);
        }
    }

    [Fact]
    public void RegularizeMeshA_PureChain_ReturnsNoCurves()
    {
        var (graph, topology) = CreateSyntheticGraphWithPureChain();

        var result = IntersectionCurveRegularizer.RegularizeMeshA(graph, topology);

        Assert.Single(result.Components);
        Assert.Equal(IntersectionCurveRegularizer.ComponentClassification.TinyNoise,
            result.Components[0].Classification);

        Assert.Empty(result.Curves);
    }

    private static (IntersectionGraph graph, MeshATopology topology) CreateSyntheticGraphWithPureLoop()
    {
        // Square loop in the XY plane: 0-1-2-3-0, unit edge lengths.
        var vertices = new (int id, double x, double y, double z)[]
        {
            (0, 0.0, 0.0, 0.0),
            (1, 1.0, 0.0, 0.0),
            (2, 1.0, 1.0, 0.0),
            (3, 0.0, 1.0, 0.0)
        };

        var edges = new (int id, int start, int end)[]
        {
            (0, 0, 1),
            (1, 1, 2),
            (2, 2, 3),
            (3, 3, 0)
        };

        return CreateSyntheticGraph(vertices, edges);
    }

    private static (IntersectionGraph graph, MeshATopology topology) CreateSyntheticGraphWithLoopAndTinyChain()
    {
        // Same square loop as above plus a short chain (4-5-6) disconnected
        // from the loop. The chain should be classified as "tiny noise" and
        // ignored in the returned curves.
        var vertices = new (int id, double x, double y, double z)[]
        {
            // Loop vertices (square).
            (0, 0.0, 0.0, 0.0),
            (1, 1.0, 0.0, 0.0),
            (2, 1.0, 1.0, 0.0),
            (3, 0.0, 1.0, 0.0),
            // Chain vertices, close together.
            (4, 10.0, 0.0, 0.0),
            (5, 11.0, 0.0, 0.0),
            (6, 12.0, 0.0, 0.0)
        };

        var edges = new (int id, int start, int end)[]
        {
            // Loop edges.
            (0, 0, 1),
            (1, 1, 2),
            (2, 2, 3),
            (3, 3, 0),
            // Tiny chain edges.
            (4, 4, 5),
            (5, 5, 6)
        };

        return CreateSyntheticGraph(vertices, edges);
    }

    private static (IntersectionGraph graph, MeshATopology topology) CreateSyntheticGraphWithPureChain()
    {
        // Simple three-vertex chain: 0-1-2.
        var vertices = new (int id, double x, double y, double z)[]
        {
            (0, 0.0, 0.0, 0.0),
            (1, 1.0, 0.0, 0.0),
            (2, 2.0, 0.0, 0.0)
        };

        var edges = new (int id, int start, int end)[]
        {
            (0, 0, 1),
            (1, 1, 2)
        };

        return CreateSyntheticGraph(vertices, edges);
    }

    private static (IntersectionGraph graph, MeshATopology topology) CreateSyntheticGraph(
        (int id, double x, double y, double z)[] vertices,
        (int id, int start, int end)[] edges)
    {
        var emptyTriangles = new List<Triangle>();
        var set = new IntersectionSet(emptyTriangles, emptyTriangles);

        var vertexTuples = new List<(IntersectionVertexId, RealPoint)>(vertices.Length);
        foreach (var (id, x, y, z) in vertices)
        {
            vertexTuples.Add((new IntersectionVertexId(id), new RealPoint(x, y, z)));
        }

        var edgeTuples = new List<(IntersectionEdgeId, IntersectionVertexId, IntersectionVertexId)>(edges.Length);
        foreach (var (id, start, end) in edges)
        {
            var eId = new IntersectionEdgeId(id);
            var vStart = new IntersectionVertexId(start);
            var vEnd = new IntersectionVertexId(end);
            edgeTuples.Add((eId, vStart, vEnd));
        }

        var graphCtor = typeof(IntersectionGraph).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            new[]
            {
                typeof(IntersectionSet),
                typeof(IReadOnlyList<(IntersectionVertexId, RealPoint)>),
                typeof(IReadOnlyList<(IntersectionEdgeId, IntersectionVertexId, IntersectionVertexId)>),
                typeof(IReadOnlyList<PairFeatures>)
            },
            modifiers: null);

        if (graphCtor == null)
        {
            throw new InvalidOperationException("IntersectionGraph constructor not found.");
        }

        var graph = (IntersectionGraph)graphCtor.Invoke(new object[]
        {
            set,
            vertexTuples,
            edgeTuples,
            Array.Empty<PairFeatures>()
        });

        // Build a minimal MeshATopology that exposes the same vertices and
        // edges on mesh A via VertexEdges / Edges. TriangleEdges and Loops
        // are unused by the regularizer, so we can provide simple placeholders.
        var triangleEdges = new[]
        {
            CreateEdgeIdArray(edges)
        };

        var meshEdges = CreateEdgeIdArray(edges);

        var adjacencyTemp = new Dictionary<IntersectionVertexId, List<IntersectionEdgeId>>();

        void AddEdgeToAdjacency(IntersectionVertexId v, IntersectionEdgeId e)
        {
            if (!adjacencyTemp.TryGetValue(v, out var list))
            {
                list = new List<IntersectionEdgeId>();
                adjacencyTemp[v] = list;
            }

            list.Add(e);
        }

        foreach (var (id, start, end) in edges)
        {
            var eId = new IntersectionEdgeId(id);
            var vStart = new IntersectionVertexId(start);
            var vEnd = new IntersectionVertexId(end);

            AddEdgeToAdjacency(vStart, eId);
            AddEdgeToAdjacency(vEnd, eId);
        }

        var vertexEdges = new Dictionary<IntersectionVertexId, IReadOnlyList<IntersectionEdgeId>>(adjacencyTemp.Count);
        foreach (var kvp in adjacencyTemp)
        {
            vertexEdges.Add(kvp.Key, kvp.Value.AsReadOnly());
        }

        var loops = Array.Empty<IntersectionVertexId[]>();

        var meshCtor = typeof(MeshATopology).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            new[]
            {
                typeof(IntersectionEdgeId[][]),
                typeof(IntersectionEdgeId[]),
                typeof(Dictionary<IntersectionVertexId, IReadOnlyList<IntersectionEdgeId>>),
                typeof(IntersectionVertexId[][])
            },
            modifiers: null);

        if (meshCtor == null)
        {
            throw new InvalidOperationException("MeshATopology constructor not found.");
        }

        var topology = (MeshATopology)meshCtor.Invoke(new object[]
        {
            triangleEdges,
            meshEdges,
            vertexEdges,
            loops
        });

        return (graph, topology);
    }

    private static IntersectionEdgeId[] CreateEdgeIdArray((int id, int start, int end)[] edges)
    {
        var result = new IntersectionEdgeId[edges.Length];
        for (int i = 0; i < edges.Length; i++)
        {
            result[i] = new IntersectionEdgeId(edges[i].id);
        }

        return result;
    }
}

