using Contracts.Core;
using Xunit;

namespace Contracts.Testing;

public static class ContractValidationAssert
{
    public static void Valid(Action validateStrict)
    {
        validateStrict();
    }

    public static ContractValidationException InvalidWithCode(Action validateStrict, string expectedCode)
    {
        var ex = Assert.Throws<ContractValidationException>(validateStrict);
        Assert.NotEmpty(ex.Violations);
        Assert.Equal(expectedCode, ex.Violations[0].Code.Value);
        return ex;
    }
}
