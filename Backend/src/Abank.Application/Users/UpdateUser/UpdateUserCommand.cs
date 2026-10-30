using Abank.Domain.Common.Results;
using Abank.Domain.Users;
using Mediator;

namespace Abank.Application.Users.UpdateUser;

/// <param name="Password">Opcional: vacío conserva la contraseña actual.</param>
/// <param name="Status">Opcional: A o I; nulo conserva el estado actual.</param>
public sealed record UpdateUserCommand(int Id, UserData Data, string? Password, string? Status)
    : ICommand<Result<UserDetails>>;
