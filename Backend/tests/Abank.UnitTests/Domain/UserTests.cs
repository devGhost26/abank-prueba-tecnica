using Abank.Domain.Common.Results;
using Abank.Domain.Users;
using Abank.UnitTests.Fakes;

namespace Abank.UnitTests.Domain;

public sealed class UserTests
{
    private readonly FakePasswordHasher _hasher = new();

    [Fact]
    public void Create_con_datos_validos_normaliza_y_guarda_el_hash()
    {
        var data = new UserData("  Ana   Sofía ", "Rivas", new DateOnly(1996, 5, 10), " San Salvador ", "7000-1234", " Ana@Abank.TEST ");

        var result = User.Create(data, "Clave2026", TestUsers.Today, _hasher);

        Assert.True(result.IsSuccess);
        var user = result.Value;
        Assert.Equal("Ana Sofía", user.FirstName);
        Assert.Equal("San Salvador", user.Address);
        Assert.Equal("70001234", user.Phone);
        Assert.Equal("ana@abank.test", user.Email);
        Assert.Equal("hash:Clave2026", user.PasswordHash);
        Assert.Equal(UserStatus.Active, user.Status);
    }

    [Fact]
    public void Create_vacio_devuelve_un_error_por_cada_campo_obligatorio()
    {
        var result = User.Create(new UserData(null, null, null, null, null, null), null, TestUsers.Today, _hasher);

        var errors = Assert.IsType<ValidationError>(result.Error).Errors;
        Assert.Equal(
            [UserFields.FirstName, UserFields.LastName, UserFields.BirthDate, UserFields.Address,
             UserFields.Phone, UserFields.Email, UserFields.Password],
            errors.Keys);
    }

    [Theory]
    [InlineData("2027-01-01", "futuro")]
    [InlineData("2010-01-01", "18 años")]
    [InlineData("1899-12-31", "no es válida")]
    public void Create_rechaza_fechas_de_nacimiento_fuera_de_rango(string birthDate, string fragment)
    {
        var data = TestUsers.ValidData() with { BirthDate = DateOnly.Parse(birthDate) };

        var result = User.Create(data, "Clave2026", TestUsers.Today, _hasher);

        var message = Assert.Single(Assert.IsType<ValidationError>(result.Error).Errors[UserFields.BirthDate]);
        Assert.Contains(fragment, message);
    }

    [Fact]
    public void Create_acepta_a_quien_cumple_18_hoy()
    {
        var data = TestUsers.ValidData() with { BirthDate = TestUsers.Today.AddYears(-18) };

        Assert.True(User.Create(data, "Clave2026", TestUsers.Today, _hasher).IsSuccess);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("solo-letras")]
    [InlineData("12345678")]
    public void Create_rechaza_contrasenas_debiles(string password)
    {
        var result = User.Create(TestUsers.ValidData(), password, TestUsers.Today, _hasher);

        Assert.True(Assert.IsType<ValidationError>(result.Error).Errors.ContainsKey(UserFields.Password));
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("1234567890123456")]
    [InlineData("70OO1234")]
    public void Create_rechaza_telefonos_invalidos(string phone)
    {
        var result = User.Create(TestUsers.ValidData(phone), "Clave2026", TestUsers.Today, _hasher);

        Assert.True(Assert.IsType<ValidationError>(result.Error).Errors.ContainsKey(UserFields.Phone));
    }

    [Theory]
    [InlineData("sin-arroba")]
    [InlineData("a@b")]
    [InlineData("a b@c.com")]
    public void Create_rechaza_emails_invalidos(string email)
    {
        var data = TestUsers.ValidData() with { Email = email };

        var result = User.Create(data, "Clave2026", TestUsers.Today, _hasher);

        Assert.True(Assert.IsType<ValidationError>(result.Error).Errors.ContainsKey(UserFields.Email));
    }

    [Fact]
    public void Update_sin_contrasena_ni_estado_conserva_ambos()
    {
        var user = TestUsers.NewUser();
        var hashBefore = user.PasswordHash;

        var result = user.Update(TestUsers.ValidData() with { FirstName = "Ana" }, null, null, TestUsers.Today, _hasher);

        Assert.True(result.IsSuccess);
        Assert.Equal("Ana", user.FirstName);
        Assert.Equal(hashBefore, user.PasswordHash);
        Assert.Equal(UserStatus.Active, user.Status);
    }

    [Fact]
    public void Update_con_contrasena_la_rehashea_y_con_estado_lo_cambia()
    {
        var user = TestUsers.NewUser();

        var result = user.Update(TestUsers.ValidData(), "Nueva2026", "i", TestUsers.Today, _hasher);

        Assert.True(result.IsSuccess);
        Assert.Equal("hash:Nueva2026", user.PasswordHash);
        Assert.Equal(UserStatus.Inactive, user.Status);
    }

    [Fact]
    public void Update_invalido_no_modifica_nada()
    {
        var user = TestUsers.NewUser();

        var result = user.Update(TestUsers.ValidData() with { FirstName = "" }, null, "X", TestUsers.Today, _hasher);

        var errors = Assert.IsType<ValidationError>(result.Error).Errors;
        Assert.Contains(UserFields.FirstName, errors.Keys);
        Assert.Contains(UserFields.Status, errors.Keys);
        Assert.Equal("Ana Sofía", user.FirstName);
        Assert.Equal(UserStatus.Active, user.Status);
    }

    [Fact]
    public void Deactivate_deja_al_usuario_inactivo()
    {
        var user = TestUsers.NewUser();

        user.Deactivate();

        Assert.False(user.IsActive);
        Assert.Equal("I", user.Status.ToCode());
    }
}
