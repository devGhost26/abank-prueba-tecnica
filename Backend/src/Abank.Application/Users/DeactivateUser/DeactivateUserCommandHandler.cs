using Abank.Application.Abstractions;
using Abank.Domain.Common.Results;
using Abank.Domain.Users;
using Mediator;

namespace Abank.Application.Users.DeactivateUser;

/// <summary>
/// El DELETE es una baja lógica: el usuario pasa a estado I y conserva su historial.
/// Repetirlo sobre un usuario ya inactivo responde lo mismo sin tocar la fila.
/// </summary>
internal sealed class DeactivateUserCommandHandler(
    IUserRepository userRepository,
    ICurrentUser currentUser) : ICommandHandler<DeactivateUserCommand, Result<UserDetails>>
{
    public async ValueTask<Result<UserDetails>> Handle(DeactivateUserCommand command, CancellationToken cancellationToken)
    {
        if (command.Id == currentUser.Id)
            return UserErrors.CannotDeactivateSelf;

        var user = await userRepository.GetByIdAsync(command.Id, cancellationToken);
        if (user is null)
            return UserErrors.NotFound(command.Id);

        if (!user.IsActive)
            return UserDetails.From(user);

        user.Deactivate();
        var saved = await userRepository.UpdateAsync(user, cancellationToken);

        return saved.IsSuccess ? UserDetails.From(saved.Value) : saved.Error;
    }
}
