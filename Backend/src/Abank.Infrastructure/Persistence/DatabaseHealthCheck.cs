using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Abank.Infrastructure.Persistence;

/// <summary>La API está sana si puede abrir conexión y ve la tabla de usuarios.</summary>
internal sealed class DatabaseHealthCheck(NpgsqlDataSource dataSource) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = dataSource.CreateCommand("SELECT 1 FROM usuarios LIMIT 1");
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("No hay acceso a la base de datos.", exception);
        }
    }
}
