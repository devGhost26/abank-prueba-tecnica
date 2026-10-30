using System.Text.RegularExpressions;
using Abank.Domain.Common.Results;

namespace Abank.Domain.Users;

/// <summary>
/// Usuario con acceso a la aplicación. Se construye siempre válido: <see cref="Create"/> y
/// <see cref="Update"/> validan todo el perfil de una vez y devuelven cada error por campo.
/// La contraseña nunca se guarda en texto plano; la entidad solo conoce su hash.
/// </summary>
public sealed partial class User
{
    public const int NameMaxLength = 100;
    public const int AddressMaxLength = 250;
    public const int EmailMaxLength = 150;
    public const int PhoneMinDigits = 7;
    public const int PhoneMaxDigits = 15;
    public const int PasswordMinLength = 8;
    // BCrypt solo usa los primeros 72 bytes; un tope menor evita que dos contraseñas
    // distintas con el mismo comienzo terminen siendo la misma.
    public const int PasswordMaxLength = 64;
    public const int MinimumAge = 18;

    private User(
        int id, string firstName, string lastName, DateOnly birthDate, string address, string phone,
        string email, string passwordHash, UserStatus status, DateTimeOffset createdAt, DateTimeOffset? modifiedAt)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        BirthDate = birthDate;
        Address = address;
        Phone = phone;
        Email = email;
        PasswordHash = passwordHash;
        Status = status;
        CreatedAt = createdAt;
        ModifiedAt = modifiedAt;
    }

    /// <summary>Cero hasta que se guarda: el Id lo asigna la secuencia de la base.</summary>
    public int Id { get; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public DateOnly BirthDate { get; private set; }
    public string Address { get; private set; }
    public string Phone { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public UserStatus Status { get; private set; }
    /// <summary>La pone el trigger al insertar; antes de guardar no tiene valor.</summary>
    public DateTimeOffset CreatedAt { get; }
    /// <summary>La pone el trigger al actualizar.</summary>
    public DateTimeOffset? ModifiedAt { get; }

    public bool IsActive => Status == UserStatus.Active;

    public static Result<User> Create(UserData data, string? password, DateOnly today, IPasswordHasher hasher)
    {
        var errors = new FieldErrors();
        var profile = ValidateProfile(data, today, errors);
        ValidatePassword(password, errors);

        if (!errors.IsEmpty)
            return errors.ToError();

        return new User(
            0, profile.FirstName, profile.LastName, profile.BirthDate, profile.Address, profile.Phone,
            profile.Email, hasher.Hash(password!), UserStatus.Active, default, null);
    }

    /// <summary>
    /// Reemplaza el perfil. La contraseña y el estado son opcionales: si no vienen, se conservan.
    /// </summary>
    public Result Update(UserData data, string? newPassword, string? statusCode, DateOnly today, IPasswordHasher hasher)
    {
        var errors = new FieldErrors();
        var profile = ValidateProfile(data, today, errors);

        var changesPassword = !string.IsNullOrEmpty(newPassword);
        if (changesPassword)
            ValidatePassword(newPassword, errors);

        var status = Status;
        if (statusCode is not null)
        {
            if (UserStatusCodes.FromCode(statusCode) is { } parsed)
                status = parsed;
            else
                errors.Add(UserFields.Status, "El estado solo puede ser 'A' (activo) o 'I' (inactivo).");
        }

        if (!errors.IsEmpty)
            return errors.ToError();

        FirstName = profile.FirstName;
        LastName = profile.LastName;
        BirthDate = profile.BirthDate;
        Address = profile.Address;
        Phone = profile.Phone;
        Email = profile.Email;
        Status = status;

        if (changesPassword)
            PasswordHash = hasher.Hash(newPassword!);

        return Result.Success();
    }

    /// <summary>La baja es lógica: el usuario queda inactivo y ya no puede iniciar sesión.</summary>
    public void Deactivate() => Status = UserStatus.Inactive;

    /// <summary>Reconstruye un usuario leído de la base. No valida: lo guardado ya pasó por las reglas.</summary>
    public static User Restore(
        int id, string firstName, string lastName, DateOnly birthDate, string address, string phone,
        string email, string passwordHash, UserStatus status, DateTimeOffset createdAt, DateTimeOffset? modifiedAt) =>
        new(id, firstName, lastName, birthDate, address, phone, email, passwordHash, status, createdAt, modifiedAt);

    /// <summary>Normaliza el teléfono como se guarda: sin espacios, guiones ni paréntesis.</summary>
    public static string NormalizePhone(string? phone) =>
        PhoneSeparators().Replace(phone?.Trim() ?? string.Empty, string.Empty);

    private sealed record Profile(
        string FirstName, string LastName, DateOnly BirthDate, string Address, string Phone, string Email);

    private static Profile ValidateProfile(UserData data, DateOnly today, FieldErrors errors)
    {
        var firstName = ValidateName(data.FirstName, UserFields.FirstName, "nombres", errors);
        var lastName = ValidateName(data.LastName, UserFields.LastName, "apellidos", errors);

        var birthDate = data.BirthDate ?? default;
        if (data.BirthDate is null)
            errors.Add(UserFields.BirthDate, "La fecha de nacimiento es obligatoria.");
        else if (birthDate > today)
            errors.Add(UserFields.BirthDate, "La fecha de nacimiento no puede estar en el futuro.");
        else if (birthDate.AddYears(MinimumAge) > today)
            errors.Add(UserFields.BirthDate, $"El usuario debe tener al menos {MinimumAge} años.");
        else if (birthDate.Year < 1900)
            errors.Add(UserFields.BirthDate, "La fecha de nacimiento no es válida.");

        var address = data.Address?.Trim() ?? string.Empty;
        if (address.Length == 0)
            errors.Add(UserFields.Address, "La dirección es obligatoria.");
        else if (address.Length > AddressMaxLength)
            errors.Add(UserFields.Address, $"La dirección admite como máximo {AddressMaxLength} caracteres.");

        var phone = NormalizePhone(data.Phone);
        if (phone.Length == 0)
            errors.Add(UserFields.Phone, "El teléfono es obligatorio.");
        else if (!PhonePattern().IsMatch(phone))
            errors.Add(UserFields.Phone,
                $"El teléfono debe tener entre {PhoneMinDigits} y {PhoneMaxDigits} dígitos, con un '+' opcional al inicio.");

        var email = data.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (email.Length == 0)
            errors.Add(UserFields.Email, "El email es obligatorio.");
        else if (email.Length > EmailMaxLength)
            errors.Add(UserFields.Email, $"El email admite como máximo {EmailMaxLength} caracteres.");
        else if (!EmailPattern().IsMatch(email))
            errors.Add(UserFields.Email, "El email no tiene un formato válido.");

        return new Profile(firstName, lastName, birthDate, address, phone, email);
    }

    private static string ValidateName(string? value, string field, string label, FieldErrors errors)
    {
        var name = CollapseSpaces(value);

        if (name.Length == 0)
            errors.Add(field, $"Los {label} son obligatorios.");
        else if (name.Length > NameMaxLength)
            errors.Add(field, $"Los {label} admiten como máximo {NameMaxLength} caracteres.");
        else if (!NamePattern().IsMatch(name))
            errors.Add(field, $"Los {label} solo pueden llevar letras, espacios, apóstrofos, puntos y guiones.");

        return name;
    }

    private static void ValidatePassword(string? password, FieldErrors errors)
    {
        if (string.IsNullOrEmpty(password))
        {
            errors.Add(UserFields.Password, "La contraseña es obligatoria.");
            return;
        }

        if (password.Length < PasswordMinLength || password.Length > PasswordMaxLength)
            errors.Add(UserFields.Password,
                $"La contraseña debe tener entre {PasswordMinLength} y {PasswordMaxLength} caracteres.");

        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            errors.Add(UserFields.Password, "La contraseña debe combinar letras y números.");
    }

    private static string CollapseSpaces(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : Spaces().Replace(value.Trim(), " ");

    [GeneratedRegex(@"^[\p{L}\p{M}' .-]+$")]
    private static partial Regex NamePattern();

    [GeneratedRegex(@"^\+?\d{7,15}$")]
    private static partial Regex PhonePattern();

    [GeneratedRegex(@"[\s\-()]")]
    private static partial Regex PhoneSeparators();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();
}
