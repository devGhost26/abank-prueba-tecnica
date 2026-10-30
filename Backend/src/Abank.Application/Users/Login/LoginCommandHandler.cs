using Abank.Application.Abstractions;
using Abank.Domain.Common.Results;
using Abank.Domain.Users;
using Mediator;

namespace Abank.Application.Users.Login;

internal sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IAccessTokenIssuer tokenIssuer) : ICommandHandler<LoginCommand, Result<LoginResult>>
{
    public async ValueTask<Result<LoginResult>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Phone) || string.IsNullOrEmpty(command.Password))
            return UserErrors.InvalidCredentials;

        var user = await userRepository.GetByPhoneAsync(User.NormalizePhone(command.Phone), cancellationToken);

        // Se verifica aunque el usuario no exista (contra un hash de relleno) para que ambos
        // casos tarden lo mismo y respondan lo mismo: no se puede saber qué teléfonos existen.
        if (!passwordHasher.Verify(command.Password, user?.PasswordHash) || user is null)
            return UserErrors.InvalidCredentials;

        // El estado se revisa después de la contraseña: solo quien la conoce sabe que está inactivo.
        if (!user.IsActive)
            return UserErrors.Inactive;

        return new LoginResult(UserDetails.From(user), tokenIssuer.Issue(user));
    }
}
