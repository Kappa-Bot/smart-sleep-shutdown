using Hushward.Application.Abstractions;
using Hushward.Application.Results;
using Microsoft.Win32;

namespace Hushward.Infrastructure.Startup;

internal interface IRunKeyStore
{
    string? Read();

    void Write(string value);

    void Delete();
}

internal sealed class CurrentUserRunKeyStore : IRunKeyStore
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Hushward";

    public string? Read()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) as string;
    }

    public void Write(string value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("Could not open the current-user startup key.");
        key.SetValue(ValueName, value, RegistryValueKind.String);
    }

    public void Delete()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}

public sealed class WindowsStartupRegistration : IStartupRegistration
{
    private readonly string _expectedCommand;
    private readonly IRunKeyStore _store;

    public WindowsStartupRegistration(string stableExecutablePath)
        : this(stableExecutablePath, new CurrentUserRunKeyStore())
    {
    }

    internal WindowsStartupRegistration(string stableExecutablePath, IRunKeyStore store)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stableExecutablePath);
        _expectedCommand = $"\"{Path.GetFullPath(stableExecutablePath)}\" --startup";
        _store = store;
    }

    public Task<OperationResult<StartupRegistrationState>> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var current = _store.Read();
            var enabled = !string.IsNullOrWhiteSpace(current);
            var healthy = !enabled || string.Equals(current, _expectedCommand, StringComparison.OrdinalIgnoreCase);
            return Task.FromResult(OperationResult<StartupRegistrationState>.Success(
                new StartupRegistrationState(enabled, healthy, healthy ? null : "startup.command-mismatch")));
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or InvalidOperationException)
        {
            return Task.FromResult(OperationResult<StartupRegistrationState>.Failure(
                "startup.read-failed",
                "Startup.ReadFailed",
                ex.Message));
        }
    }

    public async Task<OperationResult<StartupRegistrationState>> SetEnabledAsync(
        bool enabled,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (enabled)
            {
                _store.Write(_expectedCommand);
            }
            else
            {
                _store.Delete();
            }

            return await ReadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or InvalidOperationException)
        {
            return OperationResult<StartupRegistrationState>.Failure(
                "startup.write-failed",
                "Startup.WriteFailed",
                ex.Message);
        }
    }
}
