namespace Abank.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HS256 exige una llave de al menos 256 bits.</summary>
    public const int MinimumKeyLength = 32;

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKey { get; init; } = string.Empty;
    public int ExpirationMinutes { get; init; } = 60;
}
