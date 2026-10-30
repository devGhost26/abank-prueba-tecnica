namespace Abank.Domain.Common.Results;

/// <summary>Acumula los errores por campo mientras se valida una entrada completa.</summary>
public sealed class FieldErrors
{
    private readonly Dictionary<string, List<string>> _errors = [];

    public bool IsEmpty => _errors.Count == 0;

    public void Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var messages))
            _errors[field] = messages = [];

        messages.Add(message);
    }

    public ValidationError ToError() =>
        new(_errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray()));
}
