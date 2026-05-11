using Xunit;

namespace Tamp.Turbo.V2.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void Turbo_Facade_Is_Reachable() => Assert.NotNull(typeof(Turbo));
}
