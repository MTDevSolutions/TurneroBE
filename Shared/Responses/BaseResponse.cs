namespace Shared.Responses;

/// <summary>
/// Wrapper estándar de respuesta de la API.
/// </summary>
public sealed class BaseResponse<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }
    public bool ToastShow { get; init; }

    // Ok con data opcional y mensaje opcional
    public static BaseResponse<T> Ok(T? data = default, string? message = null, bool toast = false) => new()
    {
        Success = true,
        Message = message,
        Data = data,
        ToastShow = toast
    };

    public static BaseResponse<T> Fail(string message, T? data = default, bool toast = false) => new()
    {
        Success = false,
        Message = message,
        Data = data,
        ToastShow = toast
    };
}
