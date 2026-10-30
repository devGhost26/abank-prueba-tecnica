using Abank.Domain.Common.Results;
using Abank.Domain.Users;
using Dapper;
using Npgsql;

namespace Abank.Infrastructure.Users;

/// <summary>
/// Escritura del agregado <see cref="User"/> con Dapper. Cada operación es una sola sentencia,
/// atómica por sí misma, así que no hace falta abrir una transacción explícita.
/// </summary>
internal sealed class UserRepository(NpgsqlDataSource dataSource) : IUserRepository
{
    private const string UniqueViolation = "23505";
    private const string PhoneConstraint = "uq_usuarios_telefono";
    private const string EmailConstraint = "uq_usuarios_email";

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        const string sql = $"SELECT {UserRow.Columns} FROM usuarios WHERE id = @id";

        return await QuerySingleAsync(sql, new { id }, cancellationToken);
    }

    public async Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken)
    {
        // Usa el índice de la restricción única uq_usuarios_telefono.
        const string sql = $"SELECT {UserRow.Columns} FROM usuarios WHERE telefono = @phone";

        return await QuerySingleAsync(sql, new { phone }, cancellationToken);
    }

    public async Task<Result<User>> AddAsync(User user, CancellationToken cancellationToken)
    {
        // Ni el Id ni las fechas van en el INSERT: los ponen la secuencia y el trigger,
        // y RETURNING los trae de vuelta en el mismo viaje.
        const string sql = $"""
            INSERT INTO usuarios (nombres, apellidos, fecha_nacimiento, direccion, password, telefono, email, estado)
            VALUES (@FirstName, @LastName, @BirthDate, @Address, @PasswordHash, @Phone, @Email, @Status)
            RETURNING {UserRow.Columns}
            """;

        return await WriteAsync(sql, ToParameters(user), cancellationToken);
    }

    public async Task<Result<User>> UpdateAsync(User user, CancellationToken cancellationToken)
    {
        const string sql = $"""
            UPDATE usuarios
            SET nombres = @FirstName,
                apellidos = @LastName,
                fecha_nacimiento = @BirthDate,
                direccion = @Address,
                password = @PasswordHash,
                telefono = @Phone,
                email = @Email,
                estado = @Status
            WHERE id = @Id
            RETURNING {UserRow.Columns}
            """;

        return await WriteAsync(sql, ToParameters(user), cancellationToken);
    }

    private async Task<User?> QuerySingleAsync(string sql, object parameters, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        return row?.ToUser();
    }

    private async Task<Result<User>> WriteAsync(string sql, object parameters, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        try
        {
            var row = await connection.QuerySingleOrDefaultAsync<UserRow>(
                new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

            return row is null
                ? Error.NotFound("User.NotFound", "El usuario ya no existe.")
                : row.ToUser();
        }
        catch (PostgresException exception) when (exception is { SqlState: UniqueViolation, ConstraintName: PhoneConstraint })
        {
            return UserErrors.PhoneAlreadyExists;
        }
        catch (PostgresException exception) when (exception is { SqlState: UniqueViolation, ConstraintName: EmailConstraint })
        {
            return UserErrors.EmailAlreadyExists;
        }
    }

    private static object ToParameters(User user) => new
    {
        user.Id,
        user.FirstName,
        user.LastName,
        user.BirthDate,
        user.Address,
        user.PasswordHash,
        user.Phone,
        user.Email,
        Status = user.Status.ToCode()
    };
}
