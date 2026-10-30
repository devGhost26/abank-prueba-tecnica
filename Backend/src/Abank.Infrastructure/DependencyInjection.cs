using Abank.Application.Abstractions;
using Abank.Application.Users;
using Abank.Domain.Users;
using Abank.Infrastructure.Persistence;
using Abank.Infrastructure.Security;
using Abank.Infrastructure.Users;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace Abank.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Abank")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'Abank'.");

        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

        // Un solo data source para toda la aplicación: administra el pool de conexiones.
        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserQueries, UserQueries>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(jwt => jwt.SigningKey.Length >= JwtOptions.MinimumKeyLength,
                $"Jwt:SigningKey debe tener al menos {JwtOptions.MinimumKeyLength} caracteres.")
            .Validate(jwt => jwt.ExpirationMinutes > 0, "Jwt:ExpirationMinutes debe ser mayor que cero.")
            .ValidateOnStart();

        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

        return services;
    }
}
