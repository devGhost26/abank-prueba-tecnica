using System.Text.Json.Serialization;
using Abank.Application.Users;

namespace Abank.Api.Contracts.Users;

/// <summary>
/// Los campos del ejemplo del enunciado, más el estado y las fechas de auditoría: con la baja
/// lógica, quien consulta necesita saber si el usuario está activo. Las fechas se nombran como
/// en el ejemplo, en minúscula y pegadas (<c>fechanacimiento</c>).
/// </summary>
public sealed record UserResponse(
    int Id,
    string Nombres,
    string Apellidos,
    [property: JsonPropertyName("fechanacimiento")] DateOnly FechaNacimiento,
    string Telefono,
    string Email,
    string Direccion,
    string Estado,
    [property: JsonPropertyName("fechacreacion")] DateTimeOffset FechaCreacion,
    [property: JsonPropertyName("fechamodificacion")] DateTimeOffset? FechaModificacion)
{
    public static UserResponse From(UserDetails user) => new(
        user.Id, user.FirstName, user.LastName, user.BirthDate, user.Phone, user.Email,
        user.Address, user.Status, user.CreatedAt, user.ModifiedAt);
}
