using World;
using Xunit;

namespace Tests.Boolean.Operation;

public class WorldExportScaleTests
{
    [Theory]
    [InlineData(1.0)]
    [InlineData(0.01)]
    public void ExportScalesCoordinatesWithoutChangingTheModel(double scale)
    {
        var world = new global::World.World();
        var box = new Box(1400, 1400, 150);
        world.Add(box);
        string path = Path.GetTempFileName();
        try
        {
            if (scale == 1) world.Save(path);
            else world.Save(path, coordinateScale: scale);
            using var reader = new BinaryReader(File.OpenRead(path));
            reader.ReadBytes(80);
            uint count = reader.ReadUInt32();
            Assert.Equal((uint)box.Mesh.Count, count);
            var max = new float[3];
            for (int i = 0; i < count; i++)
            {
                reader.ReadBytes(12); // Unit normals do not scale.
                for (int vertex = 0; vertex < 3; vertex++)
                    for (int axis = 0; axis < 3; axis++)
                        max[axis] = Math.Max(max[axis], reader.ReadSingle());
                reader.ReadUInt16();
            }
            Assert.Equal((float)(1400 * scale), max[0]);
            Assert.Equal((float)(1400 * scale), max[1]);
            Assert.Equal((float)(150 * scale), max[2]);
            Assert.Equal(1400L, box.Mesh.Triangles.Max(t => Math.Max(t.P0.X, Math.Max(t.P1.X, t.P2.X))));
        }
        finally { File.Delete(path); }
    }
}
