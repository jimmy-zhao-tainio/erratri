using System;
using Delaunay2D;
using Xunit;

namespace Delaunay2D.Tests
{
    public class Edge2DTests
    {
        [Fact]
        public void Constructor_Throws_OnDuplicateIndices()
        {
            var ex = Assert.Throws<ArgumentException>(() => new Edge2D(1, 1));
            Assert.Contains("distinct vertex indices", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
