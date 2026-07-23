using Hushward.Application;

namespace Hushward.Application.Tests;

public sealed class ApplicationAssemblyTests
{
    [Fact]
    public void ApplicationAssemblyUsesHushwardBoundaryName()
    {
        Assert.Equal("Hushward.Application", typeof(ApplicationAssemblyMarker).Assembly.GetName().Name);
    }
}
