namespace Abank.Domain.Users;

public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>
    /// Compara la contraseña con el hash. Con <paramref name="hash"/> nulo igual hace una
    /// comparación completa contra un hash de relleno: así responder «el teléfono no existe»
    /// tarda lo mismo que «la contraseña no coincide» y no se pueden adivinar teléfonos por tiempo.
    /// </summary>
    bool Verify(string password, string? hash);
}
