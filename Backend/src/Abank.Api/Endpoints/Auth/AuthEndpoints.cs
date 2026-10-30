using Abank.Api.Common.Errors;
using Abank.Api.Common.Security;
using Abank.Api.Contracts.Auth;
using Abank.Application.Users.Login;
using Mediator;

namespace Abank.Api.Endpoints.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/login", Login)
            .AllowAnonymous()
            .RequireRateLimiting(AuthenticationExtensions.LoginRateLimitPolicy)
            .WithSummary("Inicia sesión con teléfono y contraseña")
            .Produces<LoginResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return app;
    }

    private static async Task<IResult> Login(
        LoginRequest request, HttpContext httpContext, ISender sender, TimeProvider timeProvider,
        ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        // TEMPORAL: diagnóstico de la IP detrás de los proxies de Render. Se quita al ajustar el límite.
        var headers = httpContext.Request.Headers;
        loggerFactory.CreateLogger("Abank.Api.Auth").LogInformation(
            "Login desde {ClientIp} | X-Forwarded-For: {ForwardedFor} | X-Original-For: {OriginalFor} | CF-Connecting-IP: {CfConnectingIp} | True-Client-IP: {TrueClientIp}",
            httpContext.Connection.RemoteIpAddress, headers["X-Forwarded-For"].ToString(),
            headers["X-Original-For"].ToString(), headers["CF-Connecting-IP"].ToString(),
            headers["True-Client-IP"].ToString());

        var result = await sender.Send(new LoginCommand(request.Telefono, request.Password), cancellationToken);

        return result.IsSuccess
            ? Results.Ok(LoginResponse.From(result.Value, timeProvider.GetUtcNow()))
            : result.Error.ToProblem();
    }
}
