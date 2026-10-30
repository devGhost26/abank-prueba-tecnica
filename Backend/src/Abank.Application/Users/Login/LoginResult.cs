using Abank.Application.Abstractions;

namespace Abank.Application.Users.Login;

public sealed record LoginResult(UserDetails User, AccessToken AccessToken);
