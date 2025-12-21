using System;
using System.Collections.Generic;
using Geometry;
using Kernel;
using World;
using Kernel.Intersection.Graph.Index;

namespace Diagnostics.Kernel;







internal static class Program



{



    private static void Main()



    {



        RunSphereIntersectionDiagnostics();



    }







    private static void RunSphereIntersectionDiagnostics()



    {



        long r = 200;



        var aCenter = new Point(0, 0, 0);



        var bCenter = new Point(150, 50, -30);







        var sphereA = new Sphere(r, subdivisions: 3, center: aCenter);



        var sphereB = new Sphere(r, subdivisions: 3, center: bCenter);







        var set = new IntersectionSet(



            sphereA.Mesh.Triangles,



            sphereB.Mesh.Triangles);







        var graph = IntersectionGraph.FromIntersectionSet(set);



        var index = TriangleIntersectionIndex.Build(graph);



        var meshATopology = MeshATopology.Build(graph, index);



        var meshBTopology = MeshBTopology.Build(graph, index);







        Console.WriteLine("=== Sphere-Sphere Intersection Diagnostics (Mesh A) ===");



        Console.WriteLine($"Triangles A: {set.TrianglesA.Count}");



        Console.WriteLine($"Triangles B: {set.TrianglesB.Count}");



        Console.WriteLine($"Intersecting pairs: {set.Intersections.Count}");



        Console.WriteLine($"Global vertices: {graph.Vertices.Count}");



        Console.WriteLine($"Global edges: {graph.Edges.Count}");



        Console.WriteLine($"Mesh-A edges: {meshATopology.Edges.Count}");



        Console.WriteLine($"Mesh-A components (Loops entries): {meshATopology.Loops.Count}");



        Console.WriteLine();







        // Degree histogram for mesh A vertices.



        var degreeHistogram = new Dictionary<int, int>();



        foreach (var kvp in meshATopology.VertexEdges)



        {



            int degree = kvp.Value.Count;



            degreeHistogram.TryGetValue(degree, out var count);



            degreeHistogram[degree] = count + 1;



        }







        Console.WriteLine("Vertex degree histogram on mesh A (using mesh-A edges only):");



        foreach (var kvp in degreeHistogram)



        {



            Console.WriteLine($"  degree {kvp.Key}: {kvp.Value} vertices");



        }



        Console.WriteLine();







        // Map global vertex ids to positions for length estimates.



        var globalPositions = new Dictionary<int, RealPoint>();



        foreach (var (id, position) in graph.Vertices)



        {



            globalPositions[id.Value] = position;



        }







        Console.WriteLine("Components on mesh A (from MeshATopology.Loops):");



        const int maxPrintedComponents = 10;



        for (int i = 0; i < meshATopology.Loops.Count; i++)



        {



            var chain = meshATopology.Loops[i];



            double length = 0.0;



            for (int j = 0; j < chain.Length - 1; j++)



            {



                var v0 = chain[j];



                var v1 = chain[j + 1];



                if (!globalPositions.TryGetValue(v0.Value, out var p0) ||



                    !globalPositions.TryGetValue(v1.Value, out var p1))



                {



                    continue;



                }







                double dx = p1.X - p0.X;



                double dy = p1.Y - p0.Y;



                double dz = p1.Z - p0.Z;



                length += Math.Sqrt(dx * dx + dy * dy + dz * dz);



            }







            if (i < maxPrintedComponents)



            {



                bool closed = chain.Length > 1 && chain[0].Value == chain[^1].Value;



                Console.WriteLine($"  Component {i}: vertices={chain.Length}, approxLength={length:F3}, closed={closed}");



            }



        }







        if (meshATopology.Loops.Count > maxPrintedComponents)



        {



            Console.WriteLine($"  ... {meshATopology.Loops.Count - maxPrintedComponents} more components not shown");



        }







        Console.WriteLine();







        // Run the intersection curve regularizer and report its output on mesh A.



        var regularizationA = IntersectionCurveRegularizer.RegularizeMeshA(graph, meshATopology);



        Console.WriteLine("Regularizer component stats on mesh A:");



        for (int i = 0; i < regularizationA.Components.Count; i++)



        {



            var s = regularizationA.Components[i];



            Console.WriteLine(



                $"  Component {i}: vertices={s.VertexCount}, edges={s.EdgeCount}, " +



                $"deg1={s.Degree1Count}, deg2={s.Degree2Count}, degMore={s.DegreeMoreCount}, " +



                $"totalLength={s.TotalLength:F3}, medianEdge={s.MedianEdgeLength:F3}, classification={s.Classification}");



        }







        Console.WriteLine();



        Console.WriteLine("Regularized curves on mesh A:");



        Console.WriteLine($"  curveCount = {regularizationA.Curves.Count}");



        for (int i = 0; i < regularizationA.Curves.Count; i++)



        {



            var curve = regularizationA.Curves[i];



            bool closed = curve.Vertices.Length > 1 && curve.Vertices[0].Value == curve.Vertices[^1].Value;







            bool hasSyntheticClosure = false;



            foreach (var flag in curve.IsClosureEdge)



            {



                if (flag)



                {



                    hasSyntheticClosure = true;



                    break;



                }



            }







            Console.WriteLine(



                $"  Curve {i}: vertices={curve.Vertices.Length}, edges={curve.Edges.Length}, " +



                $"totalLength={curve.TotalLength:F3}, closed={closed}, hasSyntheticClosure={hasSyntheticClosure}");



        }







        Console.WriteLine();







        // Mesh B summary.



        Console.WriteLine("=== Sphere-Sphere Intersection Diagnostics (Mesh B) ===");



        Console.WriteLine($"Mesh-B edges: {meshBTopology.Edges.Count}");



        Console.WriteLine($"Mesh-B components (Loops entries): {meshBTopology.Loops.Count}");



        Console.WriteLine();







        var regularizationB = IntersectionCurveRegularizer.RegularizeMeshB(graph, meshBTopology);



        Console.WriteLine("Regularizer component stats on mesh B:");



        for (int i = 0; i < regularizationB.Components.Count; i++)



        {



            var s = regularizationB.Components[i];



            Console.WriteLine(



                $"  Component {i}: vertices={s.VertexCount}, edges={s.EdgeCount}, " +



                $"deg1={s.Degree1Count}, deg2={s.Degree2Count}, degMore={s.DegreeMoreCount}, " +



                $"totalLength={s.TotalLength:F3}, medianEdge={s.MedianEdgeLength:F3}, classification={s.Classification}");



        }







        Console.WriteLine();



        Console.WriteLine("Regularized curves on mesh B:");



        Console.WriteLine($"  curveCount = {regularizationB.Curves.Count}");



        for (int i = 0; i < regularizationB.Curves.Count; i++)



        {



            var curve = regularizationB.Curves[i];



            bool closed = curve.Vertices.Length > 1 && curve.Vertices[0].Value == curve.Vertices[^1].Value;







            bool hasSyntheticClosure = false;



            foreach (var flag in curve.IsClosureEdge)



            {



                if (flag)



                {



                    hasSyntheticClosure = true;



                    break;



                }



            }







            Console.WriteLine(



                $"  Curve {i}: vertices={curve.Vertices.Length}, edges={curve.Edges.Length}, " +



                $"totalLength={curve.TotalLength:F3}, closed={closed}, hasSyntheticClosure={hasSyntheticClosure}");



        }







        Console.WriteLine();



        Console.WriteLine("Done. Press any key to exit.");



        Console.ReadKey();



    }



}
