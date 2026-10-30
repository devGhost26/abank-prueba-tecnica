using Abank.Application.Abstractions;
using Abank.Domain.Common.Results;
using Abank.Domain.Users;
using Mediator;

namespace Abank.Application.Users.UpdateUser;

internal sealed class UpdateUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<UpdateUserCommand, Result<UserDetails>>
{
    public async ValueTask<Result<UserDetails>> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.Id, cancellationToken);
        if (user is null)
            return UserErrors.NotFound(command.Id);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var updateResult = user.Update(command.Data, command.Password, command.Status, today, passwordHasher);
        if (updateResult.IsFailure)
            return updateResult.Error;

        if (user.Id == currentUser.Id && !user.IsActive)
            return UserErrors.CannotDeactivateSelf;

        var saved = await userRepository.UpdateAsync(user, cancellationToken);

        return saved.IsSuccess ? UserDetails.From(saved.Value) : saved.Error;
    }
}
