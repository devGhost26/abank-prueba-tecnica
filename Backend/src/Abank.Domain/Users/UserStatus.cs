namespace Abank.Domain.Users;

/// <summary>Estado del usuario. En la base es una letra: el CHECK solo acepta 'A' o 'I'.</summary>
public enum UserStatus
{
    Active,
    Inactive
}

public static class UserStatusCodes
{
    public const string Active = "A";
    public const string Inactive = "I";

    public static string ToCode(this UserStatus status) => status switch
    {
        UserStatus.Active => Active,
        UserStatus.Inactive => Inactive,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    public static UserStatus? FromCode(string? code) => code?.Trim().ToUpperInvariant() switch
    {
        Active => UserStatus.Active,
        Inactive => UserStatus.Inactive,
        _ => null
    };
}
