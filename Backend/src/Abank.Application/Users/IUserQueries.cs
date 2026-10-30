using Abank.Application.Users.ListUsers;

namespace Abank.Application.Users;

/// <summary>Lado de lectura: proyecciones directas a <see cref="UserDetails"/>, sin pasar por la entidad.</summary>
public interface IUserQueries
{
    Task<UserDetails?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<UsersPage> ListAsync(ListUsersQuery query, CancellationToken cancellationToken);
}
