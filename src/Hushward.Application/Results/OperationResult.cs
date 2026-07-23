namespace Hushward.Application.Results;

public readonly record struct Unit;

public sealed record OperationError(
    string Code,
    string MessageKey,
    string? TechnicalDetail);

public sealed record OperationResult<T>(
    T? Value,
    OperationError? Error)
{
    public bool IsSuccess => Error is null;

    public static OperationResult<T> Success(T value) => new(value, null);

    public static OperationResult<T> Failure(
        string code,
        string messageKey,
        string? detail = null) =>
        new(default, new OperationError(code, messageKey, detail));
}
