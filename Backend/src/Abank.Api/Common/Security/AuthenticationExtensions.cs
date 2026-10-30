using System.Text;
using System.Threading.RateLimiting;
using Abank.Application.Abstractions;
using Abank.Application.Users;
using Abank.Domain.Users;
using Abank.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Abank.Api.Common.Security;

public static class AuthenticationExtensions
{
    public const string LoginRateLimitPolicy = "login";

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Conserva "sub" tal cual en lugar de renombrarlo al claim largo de .NET.
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtRegisteredClaimNames.Name
                };
                options.Events = new JwtBearerEvents
                {
                    // Un token firmado sigue siendo válido hasta que vence. Esta consulta por clave
                    // primaria lo invalida en cuanto el usuario se desactiva, sin esperar a que expire.
                    OnTokenValidated = async context =>
                    {
                        var userId = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                        var queries = context.HttpContext.RequestServices.GetRequiredService<IUserQueries>();

                        var user = int.TryParse(userId, out var id)
                            ? await queries.GetByIdAsync(id, context.HttpContext.RequestAborted)
                            : null;

                        if (user?.Status != UserStatusCodes.Active)
                            context.Fail("El usuario ya no está activo.");
                    }
                };
            });

        services.AddAuthorization();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        return services;
    }

    /// <summary>Limita los intentos de login por IP para frenar ataques de fuerza bruta.</summary>
    public static IServiceCollection AddLoginRateLimit(this IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue("RateLimiting:LoginPermitLimit", 10);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(LoginRateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
        });

        return services;
    }
}
