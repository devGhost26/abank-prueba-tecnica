namespace Abank.Domain.Users;

/// <summary>
/// Nombres de los campos tal como los define el diccionario de datos del enunciado.
/// Son las claves de los errores de validación, así el cliente los ubica en su formulario.
/// </summary>
public static class UserFields
{
    public const string FirstName = "nombres";
    public const string LastName = "apellidos";
    public const string BirthDate = "fechanacimiento";
    public const string Address = "direccion";
    public const string Phone = "telefono";
    public const string Email = "email";
    public const string Password = "password";
    public const string Status = "estado";
}
