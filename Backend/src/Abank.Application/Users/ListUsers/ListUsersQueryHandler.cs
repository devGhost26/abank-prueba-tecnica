using Abank.Domain.Common.Results;
using Abank.Domain.Users;
using Mediator;

namespace Abank.Application.Users.ListUsers;

internal sealed class ListUsersQueryHandler(IUserQueries userQueries)
    : IQueryHandler<ListUsersQuery, Result<UsersPage>>
{
    public async ValueTask<Result<UsersPage>> Handle(ListUsersQuery query, CancellationToken cancellationToken)
    {
        var errors = new FieldErrors();

        if (query.Page < 1)
            errors.Add(ListUsersQuery.PageField, "La página empieza en 1.");

        if (query.PageSize is < 1 or > ListUsersQuery.MaxPageSize)
            errors.Add(ListUsersQuery.PageSizeField, $"El tamaño de página va de 1 a {ListUsersQuery.MaxPageSize}.");

        if (query.Status is not null && UserStatusCodes.FromCode(query.Status) is null)
            errors.Add(UserFields.Status, "El estado solo puede ser A o I.");

        if (!errors.IsEmpty)
            return errors.ToError();

        return await userQueries.ListAsync(query, cancellationToken);
    }
}
