using Abank.Domain.Users;

namespace Abank.Application.Abstractions;

public interface IAccessTokenIssuer
{
    AccessToken Issue(User user);
}

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);
