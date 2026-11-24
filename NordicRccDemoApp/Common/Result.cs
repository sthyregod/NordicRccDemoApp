namespace NordicRccDemoApp.Common;

public enum Outcome
{
    Success,
    BadRequest,
    NotFound,
    Conflict
}

public record Result(Outcome Outcome, string? Error = null)
{
    public bool IsSuccess => Outcome == Outcome.Success;
    
    public static Result Ok() => new(Outcome.Success);
    public static Result Fail(Outcome outcome, string? error = null) => new(outcome, error);
}

public record Result<T>(Outcome Outcome, T? Value = default, string? Error = null)
{
    public bool IsSuccess => Outcome == Outcome.Success;

    public static Result<T> Ok(T value) => new(Outcome.Success, value);
    public static Result<T> Fail(Outcome outcome, string? error = null) => new(outcome, default, error);
}