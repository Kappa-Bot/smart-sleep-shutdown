namespace Hushward.Infrastructure.Persistence;

public static class AtomicFileWriter
{
    public static async Task WriteAsync(
        string path,
        string content,
        CancellationToken cancellationToken)
    {
        var directory = global::System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = global::System.IO.Path.Combine(directory ?? ".", "config.tmp.json");
        var backupPath = global::System.IO.Path.Combine(directory ?? ".", "config.backup.json");
        await using (var stream = new FileStream(
            tempPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            FileOptions.WriteThrough))
        await using (var writer = new StreamWriter(stream))
        {
            await writer.WriteAsync(content.AsMemory(), cancellationToken).ConfigureAwait(false);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            stream.Flush(flushToDisk: true);
        }

        if (File.Exists(path))
        {
            File.Copy(path, backupPath, overwrite: true);
            File.Replace(tempPath, path, destinationBackupFileName: null, ignoreMetadataErrors: true);
        }
        else
        {
            File.Move(tempPath, path, overwrite: true);
        }
    }
}
