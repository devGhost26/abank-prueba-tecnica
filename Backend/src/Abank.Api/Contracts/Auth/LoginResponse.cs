using System.Text.Json.Serialization;
using Abank.Application.Users.Login;

namespace Abank.Api.Contracts.Auth;

/// <summary>
/// La forma del ejemplo del enunciado, con dos diferencias a propósito: no devuelve el hash
/// de la contraseña y la dirección se llama <c>direccion</c>, como en el resto de endpoints.
/// </summary>
public sealed record LoginResponse(
    LoginUserResponse User,
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn)
{
    public static LoginResponse From(LoginResult result, DateTimeOffset now)
    {
        var user = result.User;

        return new LoginResponse(
            new LoginUserResponse(
                user.Id, user.FirstName, user.LastName, SessionActive: true,
                user.BirthDate, user.Email, user.Phone, user.Address),
            result.AccessToken.Token,
            "bearer",
            (int)(result.AccessToken.ExpiresAt - now).TotalSeconds);
    }
}

public sealed record LoginUserResponse(
    int Id,
    string Nombres,
    string Apellidos,
    [property: JsonPropertyName("session_active")] bool SessionActive,
    [property: JsonPropertyName("fechanacimiento")] DateOnly FechaNacimiento,
    string Email,
    string Telefono,
    string Direccion);
