using Abank.Domain.Users;

namespace Abank.Infrastructure.Security;

/// <summary>
/// BCrypt con costo 12, el mismo formato del ejemplo del enunciado ($2b$12$...) y del
/// script de datos iniciales (pgcrypto genera $2a$, que BCrypt verifica igual).
/// </summary>
internal sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    // Hash de relleno para comparar cuando el usuario no existe. Se calcula una sola vez.
    private static readonly Lazy<string> DummyHash =
        new(() => BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), WorkFactor));

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string? hash)
    {
        if (hash is null)
        {
            BCrypt.Net.BCrypt.Verify(password, DummyHash.Value);
            return false;
        }

        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
