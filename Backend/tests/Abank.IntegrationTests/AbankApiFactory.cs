using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Abank.IntegrationTests;

/// <summary>
/// La API completa contra un PostgreSQL real en un contenedor, creado con los mismos scripts
/// de la carpeta «Base de datos» que usa Docker Compose: secuencia, CHECK y triggers incluidos.
/// </summary>
public sealed class AbankApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminPhone = "70000001";
    public const string AdminPassword = "Abank2026*";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("abank")
        .Build();

    public string ConnectionString => _database.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        var scripts = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Sql"), "*.sql").Order();
        foreach (var script in scripts)
        {
            await using var command = new NpgsqlCommand(await File.ReadAllTextAsync(script), connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Abank", ConnectionString);
        // Las pruebas inician sesión muchas veces seguidas desde la misma «IP».
        builder.UseSetting("RateLimiting:LoginPermitLimit", "1000");
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string phone = AdminPhone, string password = AdminPassword)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { telefono = phone, password });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginBody>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.access_token);

        return client;
    }

    public async Task<T?> QueryScalarAsync<T>(string sql)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        var value = await command.ExecuteScalarAsync();

        return value is DBNull or null ? default : (T)value;
    }

    private sealed record LoginBody(string access_token);
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<AbankApiFactory>
{
    public const string Name = "Api";
}
