namespace TripFlow.BLL;

/// <summary>
/// Результат операції сервісу: успіх або зрозуміле користувачеві повідомлення про помилку.
/// Невалідні дані є звичайним сценарієм, а не винятком (див. ARCHITECTURE.md, розділ 5).
/// </summary>
public class OperationResult
{
    protected OperationResult(bool isSuccess, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }

    /// <summary>Текст помилки для користувача. Заповнений лише тоді, коли IsSuccess == false.</summary>
    public string? ErrorMessage { get; }

    public static OperationResult Success() => new(true, null);

    public static OperationResult Failure(string errorMessage) => new(false, errorMessage);
}

/// <summary>Результат операції, яка в разі успіху повертає значення (наприклад, Id нової поїздки).</summary>
public sealed class OperationResult<T> : OperationResult
{
    private OperationResult(bool isSuccess, T? value, string? errorMessage)
        : base(isSuccess, errorMessage)
    {
        Value = value;
    }

    public T? Value { get; }

    public static OperationResult<T> Success(T value) => new(true, value, null);

    public static new OperationResult<T> Failure(string errorMessage) => new(false, default, errorMessage);
}