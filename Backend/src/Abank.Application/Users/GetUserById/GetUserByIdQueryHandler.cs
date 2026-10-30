using Abank.Domain.Common.Results;
using Abank.Domain.Users;
using Mediator;

namespace Abank.Application.Users.GetUserById;

internal sealed class GetUserByIdQueryHandler(IUserQueries userQueries)
    : IQueryHandler<GetUserByIdQuery, Result<UserDetails>>
{
    public async ValueTask<Result<UserDetails>> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        var user = await userQueries.GetByIdAsync(query.Id, cancellationToken);

        return user is null ? UserErrors.NotFound(query.Id) : user;
    }
}
