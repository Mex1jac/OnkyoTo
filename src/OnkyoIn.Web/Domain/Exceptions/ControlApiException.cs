namespace OnkyoIn.Web.Domain.Exceptions;

/// <summary>
/// Thrown when the OnkyoIn.Control API returns an error response.
/// Carries the original HTTP status code and the detail from ProblemDetails.
/// </summary>
public class ControlApiException : Exception
{
    public int StatusCode { get; }

    public ControlApiException(int statusCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
