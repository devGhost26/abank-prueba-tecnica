using Abank.Domain.Common.Results;
using Mediator;

namespace Abank.Application.Users.Login;

public sealed record LoginCommand(string? Phone, string? Password) : ICommand<Result<LoginResult>>;
