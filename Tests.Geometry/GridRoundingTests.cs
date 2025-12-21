using Geometry;
using Xunit;

namespace Tests.Geometry;

public class GridRoundingTests
{
    [Fact]
    public void Snap_HalfwayValues_RoundAwayFromZero()
    {
        Assert.Equal(1, GridRounding.Snap(0.5));
        Assert.Equal(-1, GridRounding.Snap(-0.5));
    }

    [Fact]
    public void Snap_NearestIntegerBehavior()
    {
        Assert.Equal(1, GridRounding.Snap(1.49));
        Assert.Equal(2, GridRounding.Snap(1.51));
        Assert.Equal(-1, GridRounding.Snap(-1.49));
        Assert.Equal(-2, GridRounding.Snap(-1.51));
    }

    [Fact]
    public void Snap_RealPoint_SnapsEachComponentIndependently()
    {
        var rp = new RealPoint(1.5, -2.5, 3.49);

        var snapped = GridRounding.Snap(rp);

        Assert.Equal(2, snapped.X);
        Assert.Equal(-3, snapped.Y);
        Assert.Equal(3, snapped.Z);
    }
}


