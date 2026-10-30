using Abank.Domain.Common.Results;

namespace Abank.Domain.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken);

    /// <summary>Inserta y devuelve el usuario con el Id de la secuencia y la fecha que puso el trigger.</summary>
    Task<Result<User>> AddAsync(User user, CancellationToken cancellationToken);

    /// <summary>Actualiza y devuelve el usuario con la fecha de modificación que puso el trigger.</summary>
    Task<Result<User>> UpdateAsync(User user, CancellationToken cancellationToken);
}
