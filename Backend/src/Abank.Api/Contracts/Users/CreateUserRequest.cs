using System.Text.Json.Serialization;
using Abank.Domain.Users;

namespace Abank.Api.Contracts.Users;

/// <summary>Todo es nulable a propósito: el dominio valida y responde con cada campo que falta.</summary>
public sealed record CreateUserRequest(
    string? Nombres,
    string? Apellidos,
    [property: JsonPropertyName("fechanacimiento")] DateOnly? FechaNacimiento,
    string? Telefono,
    string? Email,
    string? Password,
    string? Direccion)
{
    public UserData ToUserData() => new(Nombres, Apellidos, FechaNacimiento, Direccion, Telefono, Email);
}
