using Abank.Application.Users.ListUsers;

namespace Abank.Api.Contracts.Users;

/// <summary>Todos opcionales: sin parámetros devuelve la primera página de 50.</summary>
public sealed record ListUsersRequest(string? Buscar, string? Estado, int? Pagina, int? Tamano)
{
    public ListUsersQuery ToQuery() =>
        new(Buscar, Estado, Pagina ?? 1, Tamano ?? ListUsersQuery.DefaultPageSize);
}
