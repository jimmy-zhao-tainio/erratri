using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Boolean;
using Geometry;
using Xunit;

namespace Tests.Boolean.Assembly;

public class AssemblyValidateChainTests
{
    [Fact]
    public void Validate_AllowsBoundaryEdgeWithGraphChain()
    {
        var vertices = new[]
        {
            (new IntersectionVertexId(0), new RealPoint(0.0, 0.0, 0.0)),
            (new IntersectionVertexId(1), new RealPoint(0.5, 0.0, 0.0)),
            (new IntersectionVertexId(2), new RealPoint(1.0, 0.0, 0.0))
        };

        var edges = new[]
        {
            (new IntersectionEdgeId(0), new IntersectionVertexId(0), new IntersectionVertexId(1)),
            (new IntersectionEdgeId(1), new IntersectionVertexId(1), new IntersectionVertexId(2))
        };

        var graph = CreateGraph(vertices, edges);
        var selected = new BooleanPatchSet(
            new[] { new RealTriangle(new RealPoint(0.0, 0.0, 0.0), new RealPoint(1.0, 0.0, 0.0), new RealPoint(0.0, 1.0, 0.0)) },
            Array.Empty<RealTriangle>(),
            new[] { new TriangleVertexIds(0, 2, -1) },
            Array.Empty<TriangleVertexIds>());

        var ex = Record.Exception(() => InvokeValidate(graph, selected));
        Assert.Null(ex);
    }

    [Fact]
    public void Validate_AllowsBoundaryEdgeWithMultiSegmentChain()
    {
        var vertices = new[]
        {
            (new IntersectionVertexId(0), new RealPoint(0.0, 0.0, 0.0)),
            (new IntersectionVertexId(1), new RealPoint(0.3, 0.0, 0.0)),
            (new IntersectionVertexId(2), new RealPoint(0.6, 0.0, 0.0)),
            (new IntersectionVertexId(3), new RealPoint(1.0, 0.0, 0.0))
        };

        var edges = new[]
        {
            (new IntersectionEdgeId(0), new IntersectionVertexId(0), new IntersectionVertexId(1)),
            (new IntersectionEdgeId(1), new IntersectionVertexId(1), new IntersectionVertexId(2)),
            (new IntersectionEdgeId(2), new IntersectionVertexId(2), new IntersectionVertexId(3))
        };

        var graph = CreateGraph(vertices, edges);
        var selected = new BooleanPatchSet(
            new[] { new RealTriangle(new RealPoint(0.0, 0.0, 0.0), new RealPoint(1.0, 0.0, 0.0), new RealPoint(0.0, 1.0, 0.0)) },
            Array.Empty<RealTriangle>(),
            new[] { new TriangleVertexIds(0, 3, -1) },
            Array.Empty<TriangleVertexIds>());

        var ex = Record.Exception(() => InvokeValidate(graph, selected));
        Assert.Null(ex);
    }

    [Fact]
    public void Validate_RejectsBoundaryEdgeWithoutGraphChain()
    {
        var vertices = new[]
        {
            (new IntersectionVertexId(0), new RealPoint(0.0, 0.0, 0.0)),
            (new IntersectionVertexId(1), new RealPoint(0.5, 0.0, 0.0)),
            (new IntersectionVertexId(2), new RealPoint(1.0, 0.0, 0.0))
        };

        var edges = new[]
        {
            (new IntersectionEdgeId(0), new IntersectionVertexId(0), new IntersectionVertexId(1))
        };

        var graph = CreateGraph(vertices, edges);
        var selected = new BooleanPatchSet(
            new[] { new RealTriangle(new RealPoint(0.0, 0.0, 0.0), new RealPoint(1.0, 0.0, 0.0), new RealPoint(0.0, 1.0, 0.0)) },
            Array.Empty<RealTriangle>(),
            new[] { new TriangleVertexIds(0, 2, -1) },
            Array.Empty<TriangleVertexIds>());

        Assert.Throws<InvalidOperationException>(() => InvokeValidate(graph, selected));
    }

    [Fact]
    public void Selection_PreservesIntersectionVertexIdsIntoValidate()
    {
        var vertices = new[]
        {
            (new IntersectionVertexId(0), new RealPoint(0.0, 0.0, 0.0)),
            (new IntersectionVertexId(1), new RealPoint(1.0, 0.0, 0.0))
        };

        var edges = new[]
        {
            (new IntersectionEdgeId(0), new IntersectionVertexId(0), new IntersectionVertexId(1))
        };

        var graph = CreateGraph(vertices, edges);

        var tri = new RealTriangle(new RealPoint(0.0, 0.0, 0.0), new RealPoint(1.0, 0.0, 0.0), new RealPoint(0.0, 1.0, 0.0));
        var patchInfo = new PatchInfo(
            tri,
            faceId: 0,
            new TriangleVertexIds(0, 1, -1),
            CoplanarOwner.None,
            Containment.Outside);
        var classification = new PatchClassification(
            new[] { new[] { patchInfo } },
            new[] { Array.Empty<PatchInfo>() });

        var selected = Selection.Run(BooleanOperationType.Union, classification, graph);
        Assert.NotNull(selected.IntersectionVertexIdsFromMeshA);
        Assert.Equal(0, selected.IntersectionVertexIdsFromMeshA![0].V0);
        Assert.Equal(1, selected.IntersectionVertexIdsFromMeshA![0].V1);

        var ex = Record.Exception(() => InvokeValidate(graph, selected));
        Assert.Null(ex);
    }

    private static IntersectionGraph CreateGraph(
        IReadOnlyList<(IntersectionVertexId Id, RealPoint Position)> vertices,
        IReadOnlyList<(IntersectionEdgeId Id, IntersectionVertexId Start, IntersectionVertexId End)> edges)
    {
        var set = new IntersectionSet(Array.Empty<Triangle>(), Array.Empty<Triangle>());
        var pairs = Array.Empty<PairFeatures>();

        var ctor = typeof(IntersectionGraph)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single();

        return (IntersectionGraph)ctor.Invoke(new object[] { set, vertices, edges, pairs });
    }

    private static void InvokeValidate(IntersectionGraph graph, BooleanPatchSet selected)
    {
        var method = typeof(global::Boolean.Assembly)
            .GetMethod("Validate", BindingFlags.NonPublic | BindingFlags.Static);

        if (method is null)
        {
            throw new InvalidOperationException("Assembly.Validate not found via reflection.");
        }

        try
        {
            method.Invoke(null, new object[] { graph, selected });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }
}
