using Abank.Domain.Common.Results;

namespace Abank.Api.Common.Errors;

public static class ErrorExtensions
{
    /// <summary>El único punto que traduce un error del negocio a HTTP, en formato ProblemDetails (RFC 9457).</summary>
    public static IResult ToProblem(this Error error)
    {
        var extensions = new Dictionary<string, object?> { ["errorCode"] = error.Code };

        if (error is ValidationError validation)
            return Results.ValidationProblem(
                validation.Errors, detail: error.Message, title: error.Code, extensions: extensions);

        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(statusCode: statusCode, title: error.Code, detail: error.Message, extensions: extensions);
    }
}
