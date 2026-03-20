namespace SharedKernel.Common;

public class Result
{
    protected Result(bool isSuccess, string? error = null)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }

    public static Result Success() => new(true);
    public static Result Failure(string error) => new(false, error);
    public static Result<T> Success<T>(T value) => new(true, value);
    public static Result<T> Failure<T>(string error) => new(false, default!, error);
}

public class Result<T> : Result
{
    private readonly T _value;

    internal Result(bool isSuccess, T value, string? error = null)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public T Value => IsSuccess
        ? _value
        : throw new InvalidOperationException("Cannot access Value of a failed result.");
}
