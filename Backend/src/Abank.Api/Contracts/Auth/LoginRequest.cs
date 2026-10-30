namespace Abank.Api.Contracts.Auth;

public sealed record LoginRequest(string? Telefono, string? Password);
