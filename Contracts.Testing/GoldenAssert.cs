using Contracts.Core;
using Xunit;

namespace Contracts.Testing;

public static class GoldenAssert
{
    public const string UpdateGoldensEnvVar = "ERRATRI_UPDATE_GOLDENS";

    public static void MatchesFile(string actual, string goldenPath)
    {
        if (goldenPath is null) throw new ArgumentNullException(nameof(goldenPath));

        actual = GoldenNormalization.NormalizeNewlines(actual);

        var update = string.Equals(
            Environment.GetEnvironmentVariable(UpdateGoldensEnvVar),
            "1",
            StringComparison.OrdinalIgnoreCase);

        if (update)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(goldenPath)!);
            File.WriteAllText(goldenPath, actual);
            return;
        }

        if (!File.Exists(goldenPath))
        {
            Assert.Fail($"Golden file missing: {goldenPath}. Set {UpdateGoldensEnvVar}=1 to write/update goldens.");
            return;
        }

        var expected = GoldenNormalization.NormalizeNewlines(File.ReadAllText(goldenPath));
        Assert.Equal(expected, actual);
    }
}
