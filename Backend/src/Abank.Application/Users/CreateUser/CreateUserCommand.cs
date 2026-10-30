using Abank.Domain.Common.Results;
using Abank.Domain.Users;
using Mediator;

namespace Abank.Application.Users.CreateUser;

public sealed record CreateUserCommand(UserData Data, string? Password) : ICommand<Result<UserDetails>>;
