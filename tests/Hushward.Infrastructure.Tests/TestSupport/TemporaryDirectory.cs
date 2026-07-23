namespace Hushward.Infrastructure.Tests.TestSupport;

public sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        Path = global::System.IO.Path.Combine(global::System.IO.Path.GetTempPath(), $"hushward-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string PathOf(string fileName) => global::System.IO.Path.Combine(Path, fileName);

    public void Dispose()
    {
        Directory.Delete(Path, recursive: true);
    }
}

