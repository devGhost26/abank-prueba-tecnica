namespace Abank.Domain.Common.Results;

/// <summary>
/// Error de validación con el detalle por campo. Devuelve todos los problemas
/// de una vez para que un formulario los marque juntos, no de uno en uno.
/// </summary>
public sealed record ValidationError(IReadOnlyDictionary<string, string[]> Errors)
    : Error("Validation.Failed", "Uno o más campos no son válidos.", ErrorType.Validation);
