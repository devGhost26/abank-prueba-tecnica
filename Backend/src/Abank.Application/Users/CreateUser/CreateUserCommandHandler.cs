using Abank.Domain.Common.Results;
using Abank.Domain.Users;
using Mediator;

namespace Abank.Application.Users.CreateUser;

internal sealed class CreateUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider) : ICommandHandler<CreateUserCommand, Result<UserDetails>>
{
    public async ValueTask<Result<UserDetails>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var userResult = User.Create(command.Data, command.Password, today, passwordHasher);
        if (userResult.IsFailure)
            return userResult.Error;

        // Teléfono y email repetidos los detecta la restricción única de la base, no una
        // consulta previa: así no hay carrera entre dos altas simultáneas.
        var saved = await userRepository.AddAsync(userResult.Value, cancellationToken);

        return saved.IsSuccess ? UserDetails.From(saved.Value) : saved.Error;
    }
}
