using Abank.Domain.Common.Results;
using Mediator;

namespace Abank.Application.Users.GetUserById;

public sealed record GetUserByIdQuery(int Id) : IQuery<Result<UserDetails>>;
