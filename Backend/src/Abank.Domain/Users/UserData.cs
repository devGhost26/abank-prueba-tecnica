namespace Abank.Domain.Users;

/// <summary>Datos del perfil tal como llegan, todavía sin validar ni normalizar.</summary>
public sealed record UserData(
    string? FirstName,
    string? LastName,
    DateOnly? BirthDate,
    string? Address,
    string? Phone,
    string? Email);
