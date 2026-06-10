namespace FlashSales.Domain.Shared;

/// <summary>
/// Railway-oriented result type. Replaces null returns and exception-based
/// control flow; composing with Map/Bind keeps use-case pipelines flat and
/// cyclomatic complexity low.
/// </summary>
public readonly record struct Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot access Value of a failed result ({_error}).");

    public Error Error => IsSuccess
        ? throw new InvalidOperationException("Cannot access Error of a successful result.")
        : _error!;

    private Result(T value)
    {
        _value = value;
        _error = null;
        IsSuccess = true;
    }

    private Result(Error error)
    {
        _value = default;
        _error = error;
        IsSuccess = false;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);

    public Result<TOut> Map<TOut>(Func<T, TOut> transform) =>
        IsSuccess ? Result<TOut>.Success(transform(_value!)) : Result<TOut>.Failure(_error!);

    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> next) =>
        IsSuccess ? next(_value!) : Result<TOut>.Failure(_error!);

    public async Task<Result<TOut>> BindAsync<TOut>(Func<T, Task<Result<TOut>>> next) =>
        IsSuccess ? await next(_value!) : Result<TOut>.Failure(_error!);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(_error!);
}

public static class ResultExtensions
{
    /// <summary>Bind over a Task-wrapped result, enabling fluent async pipelines.</summary>
    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask, Func<TIn, Task<Result<TOut>>> next)
    {
        var result = await resultTask;
        return await result.BindAsync(next);
    }

    public static async Task<Result<TOut>> MapAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask, Func<TIn, TOut> transform)
    {
        var result = await resultTask;
        return result.Map(transform);
    }
}
