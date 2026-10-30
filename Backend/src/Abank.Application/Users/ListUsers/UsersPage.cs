namespace Abank.Application.Users.ListUsers;

public sealed record UsersPage(IReadOnlyList<UserDetails> Items, int TotalCount);
