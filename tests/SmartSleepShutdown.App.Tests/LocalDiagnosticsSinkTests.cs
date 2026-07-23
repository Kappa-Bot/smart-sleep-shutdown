using SmartSleepShutdown.App.Diagnostics;

namespace SmartSleepShutdown.App.Tests;

public sealed class LocalDiagnosticsSinkTests
{
    [Fact]
    public void WritesLocalJsonLinesWithoutNetworkState()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.jsonl");
        var sink = new LocalDiagnosticsSink(path);

        try
        {
            sink.Write("decision", new { state = "Monitoring" });

            var text = File.ReadAllText(path);
            Assert.Contains("\"eventName\":\"decision\"", text);
            Assert.Contains("Monitoring", text);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
