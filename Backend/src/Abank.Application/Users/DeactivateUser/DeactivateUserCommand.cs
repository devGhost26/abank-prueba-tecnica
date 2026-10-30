using Abank.Domain.Common.Results;
using Mediator;

namespace Abank.Application.Users.DeactivateUser;

public sealed record DeactivateUserCommand(int Id) : ICommand<Result<UserDetails>>;
