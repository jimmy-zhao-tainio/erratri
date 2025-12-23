using Geometry;
using Geometry.Topology;

namespace IO;

// Shared STL writer for Mesh and triangle collections (binary little-endian)
public static class StlWriter
{
    public static void Write(Mesh surface, string path)
        => Write(surface.Triangles, path);

    public static void Write(IReadOnlyList<Triangle> triangles, string path)
    {
        if (triangles is null) throw new ArgumentNullException(nameof(triangles));
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using var bw = new BinaryWriter(fs);

        var header = new byte[80];
        var tag = System.Text.Encoding.ASCII.GetBytes("Seaharp STL");
        Array.Copy(tag, header, Math.Min(header.Length, tag.Length));
        bw.Write(header);

        bw.Write((uint)triangles.Count);
        for (int i = 0; i < triangles.Count; i++)
        {
            var t = triangles[i];
            bw.Write((float)t.Normal.X);
            bw.Write((float)t.Normal.Y);
            bw.Write((float)t.Normal.Z);
            bw.Write((float)t.P0.X); bw.Write((float)t.P0.Y); bw.Write((float)t.P0.Z);
            bw.Write((float)t.P1.X); bw.Write((float)t.P1.Y); bw.Write((float)t.P1.Z);
            bw.Write((float)t.P2.X); bw.Write((float)t.P2.Y); bw.Write((float)t.P2.Z);
            bw.Write((ushort)0);
        }
    }
}

