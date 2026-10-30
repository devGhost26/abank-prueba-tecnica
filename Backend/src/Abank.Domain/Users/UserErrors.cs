using Abank.Domain.Common.Results;

namespace Abank.Domain.Users;

public static class UserErrors
{
    public static Error NotFound(int id) =>
        Error.NotFound("User.NotFound", $"No existe un usuario con Id {id}.");

    public static readonly Error PhoneAlreadyExists =
        Error.Conflict("User.PhoneAlreadyExists", "Ya existe un usuario con ese teléfono.");

    public static readonly Error EmailAlreadyExists =
        Error.Conflict("User.EmailAlreadyExists", "Ya existe un usuario con ese email.");

    public static readonly Error CannotDeactivateSelf =
        Error.Conflict("User.CannotDeactivateSelf", "No puedes desactivar tu propia cuenta.");

    public static readonly Error InvalidCredentials =
        Error.Unauthorized("Auth.InvalidCredentials", "Teléfono o contraseña incorrectos.");

    public static readonly Error Inactive =
        Error.Forbidden("Auth.UserInactive", "El usuario está inactivo. Pide a un administrador que lo reactive.");
}
