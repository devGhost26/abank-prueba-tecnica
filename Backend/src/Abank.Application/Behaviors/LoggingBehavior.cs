using System.Diagnostics;
using Abank.Domain.Common.Results;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Abank.Application.Behaviors;

/// <summary>Registra cada caso de uso con su duración y, si falló, el código de error.</summary>
internal sealed class LoggingBehavior<TMessage, TResponse>(ILogger<LoggingBehavior<TMessage, TResponse>> logger)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : notnull, IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var messageName = typeof(TMessage).Name;
        var startedAt = Stopwatch.GetTimestamp();
        var response = await next(message, cancellationToken);
        var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

        if (response is Result { IsFailure: true } result)
            logger.LogWarning("{MessageName} terminó con error {ErrorCode} en {ElapsedMs:0.0} ms",
                messageName, result.Error.Code, elapsedMs);
        else
            logger.LogInformation("{MessageName} terminó en {ElapsedMs:0.0} ms", messageName, elapsedMs);

        return response;
    }
}
