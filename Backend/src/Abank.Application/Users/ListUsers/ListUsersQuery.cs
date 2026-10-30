using Abank.Domain.Common.Results;
using Mediator;

namespace Abank.Application.Users.ListUsers;

/// <param name="Search">Busca en nombres, apellidos, email y teléfono.</param>
/// <param name="Status">A o I; nulo trae ambos.</param>
public sealed record ListUsersQuery(string? Search, string? Status, int Page, int PageSize)
    : IQuery<Result<UsersPage>>
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 100;

    // Nombres de los parámetros en la URL, para que el error apunte al que el cliente mandó.
    public const string PageField = "pagina";
    public const string PageSizeField = "tamano";
}
