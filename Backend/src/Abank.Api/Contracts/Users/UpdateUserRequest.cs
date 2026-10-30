using System.Text.Json.Serialization;
using Abank.Domain.Users;

namespace Abank.Api.Contracts.Users;

/// <param name="Id">El ejemplo del enunciado lo manda en el cuerpo. Es opcional; si viene, debe coincidir con la ruta.</param>
/// <param name="Password">Opcional: vacío conserva la contraseña actual.</param>
/// <param name="Estado">Opcional: A o I. Permite reactivar a un usuario dado de baja.</param>
public sealed record UpdateUserRequest(
    int? Id,
    string? Nombres,
    string? Apellidos,
    [property: JsonPropertyName("fechanacimiento")] DateOnly? FechaNacimiento,
    string? Telefono,
    string? Email,
    string? Password,
    string? Direccion,
    string? Estado)
{
    public UserData ToUserData() => new(Nombres, Apellidos, FechaNacimiento, Direccion, Telefono, Email);
}
