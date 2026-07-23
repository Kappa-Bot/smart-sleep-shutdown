namespace Hushward.Infrastructure.Diagnostics;

public sealed record DiagnosticBundle(
    string FileName,
    string ManifestText,
    byte[] Bytes);
