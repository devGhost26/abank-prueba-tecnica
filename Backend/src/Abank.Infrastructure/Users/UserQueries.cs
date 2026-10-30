using Abank.Application.Users;
using Abank.Application.Users.ListUsers;
using Abank.Domain.Users;
using Dapper;
using Npgsql;

namespace Abank.Infrastructure.Users;

/// <summary>Lado de lectura con Dapper: proyecta directo a <see cref="UserDetails"/>.</summary>
internal sealed class UserQueries(NpgsqlDataSource dataSource) : IUserQueries
{
    public async Task<UserDetails?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        const string sql = $"SELECT {UserRow.Columns} FROM usuarios WHERE id = @id";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));

        return row?.ToDetails();
    }

    public async Task<UsersPage> ListAsync(ListUsersQuery query, CancellationToken cancellationToken)
    {
        // El filtro se arma solo con las condiciones que aplican, siempre con parámetros.
        // Un "(@x IS NULL OR ...)" fijo le impediría al planificador usar los índices.
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (UserStatusCodes.FromCode(query.Status) is { } status)
        {
            conditions.Add("estado = @Status");
            parameters.Add("Status", status.ToCode());
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            conditions.Add("(nombres ILIKE @Pattern OR apellidos ILIKE @Pattern OR email ILIKE @Pattern OR telefono LIKE @Pattern)");
            parameters.Add("Pattern", $"%{EscapeLike(query.Search.Trim())}%");
        }

        var where = conditions.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", conditions);
        parameters.Add("Limit", query.PageSize);
        parameters.Add("Offset", (query.Page - 1) * query.PageSize);

        // Conteo y página en un solo viaje a la base. El orden termina en id para que
        // la paginación sea estable cuando dos usuarios se llaman igual.
        var sql = $"""
            SELECT count(*) FROM usuarios {where};
            SELECT {UserRow.Columns}
            FROM usuarios
            {where}
            ORDER BY apellidos, nombres, id
            LIMIT @Limit OFFSET @Offset;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var results = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        var total = await results.ReadSingleAsync<long>();
        var rows = await results.ReadAsync<UserRow>();

        return new UsersPage(rows.Select(row => row.ToDetails()).ToList(), (int)total);
    }

    /// <summary>Que un % o un _ escrito por el usuario se busque literal y no como comodín.</summary>
    private static string EscapeLike(string value) =>
        value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
}
