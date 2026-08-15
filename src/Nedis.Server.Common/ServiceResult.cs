namespace Nedis.Server.Common;

public readonly struct ServiceResult<T, TError>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public TError? Error { get; }

    private ServiceResult(bool ok, T? value, TError? error) => (IsSuccess, Value, Error) = (ok, value, error);

    public static ServiceResult<T, TError> Success(T value) => new(true, value, default);

    public static ServiceResult<T, TError> Failure(TError error) => new(false, default, error);
}