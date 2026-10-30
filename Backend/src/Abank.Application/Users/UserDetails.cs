using Abank.Domain.Users;

namespace Abank.Application.Users;

/// <summary>Lo que se muestra de un usuario. No lleva el hash de la contraseña, nunca.</summary>
public sealed record UserDetails(
    int Id,
    string FirstName,
    string LastName,
    DateOnly BirthDate,
    string Address,
    string Phone,
    string Email,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt)
{
    public static UserDetails From(User user) => new(
        user.Id, user.FirstName, user.LastName, user.BirthDate, user.Address, user.Phone,
        user.Email, user.Status.ToCode(), user.CreatedAt, user.ModifiedAt);
}
