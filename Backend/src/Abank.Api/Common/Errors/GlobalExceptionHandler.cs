using Microsoft.AspNetCore.Diagnostics;

namespace Abank.Api.Common.Errors;

/// <summary>Una excepción no controlada se registra completa y al cliente solo le llega un 500 genérico.</summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Un JSON mal formado o de tipos equivocados no es un error del servidor.
        if (exception is BadHttpRequestException badRequest)
        {
            httpContext.Response.StatusCode = badRequest.StatusCode;
            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails =
                {
                    Status = badRequest.StatusCode,
                    Title = "Request.Invalid",
                    Detail = "El cuerpo de la petición no es un JSON válido o algún campo tiene un tipo incorrecto."
                }
            });
        }

        logger.LogError(exception, "Error no controlado en {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Server.Error",
                Detail = "Ocurrió un error inesperado. Usa el traceId para reportarlo."
            }
        });
    }
}
