namespace Abank.Application.Abstractions;

/// <summary>El usuario autenticado de la petición, leído del token.</summary>
public interface ICurrentUser
{
    int? Id { get; }
}
