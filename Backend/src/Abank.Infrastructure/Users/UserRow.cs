using Abank.Application.Users;
using Abank.Domain.Users;

namespace Abank.Infrastructure.Users;

/// <summary>Una fila de <c>usuarios</c> tal como la materializa Dapper.</summary>
internal sealed class UserRow
{
    /// <summary>
    /// Columnas con alias a las propiedades de esta clase. Una sola lista para el repositorio y
    /// las consultas, así un cambio de columna no se olvida en uno de los dos lados.
    /// </summary>
    public const string Columns = """
        id                 AS Id,
        nombres            AS FirstName,
        apellidos          AS LastName,
        fecha_nacimiento   AS BirthDate,
        direccion          AS Address,
        telefono           AS Phone,
        email              AS Email,
        password           AS PasswordHash,
        estado             AS Status,
        fecha_creacion     AS CreatedAt,
        fecha_modificacion AS ModifiedAt
        """;

    public int Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateOnly BirthDate { get; init; }
    public string Address { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PasswordHash { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }

    public User ToUser() => User.Restore(
        Id, FirstName, LastName, BirthDate, Address, Phone, Email, PasswordHash,
        UserStatusCodes.FromCode(Status) ?? throw new InvalidOperationException($"Estado desconocido '{Status}'."),
        ToUtc(CreatedAt), ModifiedAt is { } modifiedAt ? ToUtc(modifiedAt) : null);

    public UserDetails ToDetails() => new(
        Id, FirstName, LastName, BirthDate, Address, Phone, Email, Status,
        ToUtc(CreatedAt), ModifiedAt is { } modifiedAt ? ToUtc(modifiedAt) : null);

    // Npgsql entrega timestamptz como DateTime en UTC.
    private static DateTimeOffset ToUtc(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
