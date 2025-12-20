using Contracts.Core;
using Contracts.Testing;

namespace Contracts.Core.Tests;

public sealed class UnitTest1
{
    [Fact]
    public void ContractValidationContext_ThrowsWithStableFirstCode()
    {
        var ctx = new ContractValidationContext();
        ctx.Add(ContractErrorCode.From("BP00.TEST.FIRST"), "First problem", path: "a");
        ctx.Add(ContractErrorCode.From("BP00.TEST.SECOND"), "Second problem", path: "b");

        var ex = Assert.Throws<ContractValidationException>(() => ctx.ThrowIfAny());
        Assert.Equal("BP00.TEST.FIRST", ex.Violations[0].Code.Value);
    }

    [Fact]
    public void GoldenAssert_CanUpdateGoldensWhenEnabled()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "Erratri", "Contracts.Core.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var goldenPath = Path.Combine(tempDir, "sample.golden.txt");
        try
        {
            Environment.SetEnvironmentVariable(GoldenAssert.UpdateGoldensEnvVar, "1");
            GoldenAssert.MatchesFile("hello\r\nworld\r\n", goldenPath);

            Environment.SetEnvironmentVariable(GoldenAssert.UpdateGoldensEnvVar, null);
            GoldenAssert.MatchesFile("hello\nworld\n", goldenPath);
        }
        finally
        {
            Environment.SetEnvironmentVariable(GoldenAssert.UpdateGoldensEnvVar, null);
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ContractErrorCode_ValidExamples_AreAccepted()
    {
        _ = ContractErrorCode.From("CORE.TOLERANCE.DISTANCE_EPSILON_INVALID");
        _ = ContractErrorCode.From("BP05.INTERSECTION_GRAPH.EDGE_MULTIPLICITY_GT_2");
    }

    [Fact]
    public void ContractErrorCode_InvalidExamples_AreRejected()
    {
        Assert.Throws<ArgumentException>(() => ContractErrorCode.From("core.tolerance.distance_epsilon_invalid"));
        Assert.Throws<ArgumentException>(() => ContractErrorCode.From("TEST.NEGATIVE"));
        Assert.Throws<ArgumentException>(() => ContractErrorCode.From("BP5.BAD"));
    }
}
