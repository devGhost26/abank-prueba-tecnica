using Abank.Application.Abstractions;
using Abank.Domain.Common.Results;
using Abank.Domain.Users;

namespace Abank.UnitTests.Fakes;

/// <summary>Hash legible y sin costo: las pruebas de reglas no necesitan BCrypt real.</summary>
internal sealed class FakePasswordHasher : IPasswordHasher
{
    public int VerifyCalls { get; private set; }

    public string Hash(string password) => $"hash:{password}";

    public bool Verify(string password, string? hash)
    {
        VerifyCalls++;
        return hash == Hash(password);
    }
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

internal sealed class FakeCurrentUser(int? id) : ICurrentUser
{
    public int? Id { get; } = id;
}

internal sealed class FakeTokenIssuer : IAccessTokenIssuer
{
    public AccessToken Issue(User user) => new($"token-{user.Id}", DateTimeOffset.UnixEpoch);
}

/// <summary>Repositorio en memoria que imita la secuencia y la restricción única de teléfono.</summary>
internal sealed class InMemoryUserRepository : IUserRepository
{
    private readonly Dictionary<int, User> _users = [];
    private int _sequence;

    public int UpdateCalls { get; private set; }

    public User Seed(User user)
    {
        var stored = Copy(user, ++_sequence, user.Status);
        _users[stored.Id] = stored;
        return stored;
    }

    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(_users.GetValueOrDefault(id));

    public Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken) =>
        Task.FromResult(_users.Values.FirstOrDefault(user => user.Phone == phone));

    public Task<Result<User>> AddAsync(User user, CancellationToken cancellationToken)
    {
        if (_users.Values.Any(existing => existing.Phone == user.Phone))
            return Task.FromResult<Result<User>>(UserErrors.PhoneAlreadyExists);

        return Task.FromResult<Result<User>>(Seed(user));
    }

    public Task<Result<User>> UpdateAsync(User user, CancellationToken cancellationToken)
    {
        UpdateCalls++;
        _users[user.Id] = user;
        return Task.FromResult<Result<User>>(user);
    }

    private static User Copy(User user, int id, UserStatus status) => User.Restore(
        id, user.FirstName, user.LastName, user.BirthDate, user.Address, user.Phone, user.Email,
        user.PasswordHash, status, DateTimeOffset.UnixEpoch, null);
}

internal static class TestUsers
{
    public static readonly DateOnly Today = new(2026, 10, 30);

    public static UserData ValidData(string phone = "70001234") => new(
        "Ana Sofía", "Rivas Castillo", new DateOnly(1996, 5, 10), "San Salvador", phone, "Ana@Abank.test");

    public static User NewUser(string phone = "70001234", string password = "Clave2026") =>
        User.Create(ValidData(phone), password, Today, new FakePasswordHasher()).Value;
}
