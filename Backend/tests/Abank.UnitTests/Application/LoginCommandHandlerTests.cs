using Abank.Application.Users.Login;
using Abank.Domain.Users;
using Abank.UnitTests.Fakes;

namespace Abank.UnitTests.Application;

public sealed class LoginCommandHandlerTests
{
    private readonly InMemoryUserRepository _repository = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests() =>
        _handler = new LoginCommandHandler(_repository, _hasher, new FakeTokenIssuer());

    [Fact]
    public async Task Con_credenciales_correctas_devuelve_usuario_y_token()
    {
        var user = _repository.Seed(TestUsers.NewUser("70001234", "Clave2026"));

        var result = await _handler.Handle(new LoginCommand("7000-1234", "Clave2026"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value.User.Id);
        Assert.Equal($"token-{user.Id}", result.Value.AccessToken.Token);
    }

    [Fact]
    public async Task Telefono_inexistente_y_contrasena_incorrecta_responden_igual()
    {
        _repository.Seed(TestUsers.NewUser("70001234", "Clave2026"));

        var unknownPhone = await _handler.Handle(new LoginCommand("79999999", "Clave2026"), default);
        var wrongPassword = await _handler.Handle(new LoginCommand("70001234", "Otra2026"), default);

        Assert.Equal(UserErrors.InvalidCredentials, unknownPhone.Error);
        Assert.Equal(UserErrors.InvalidCredentials, wrongPassword.Error);
        // También se verificó la contraseña del teléfono inexistente: mismo costo, mismo tiempo.
        Assert.Equal(2, _hasher.VerifyCalls);
    }

    [Fact]
    public async Task Usuario_inactivo_con_contrasena_correcta_es_rechazado()
    {
        var user = TestUsers.NewUser("70001234", "Clave2026");
        user.Deactivate();
        _repository.Seed(user);

        var result = await _handler.Handle(new LoginCommand("70001234", "Clave2026"), default);

        Assert.Equal(UserErrors.Inactive, result.Error);
    }

    [Theory]
    [InlineData(null, "Clave2026")]
    [InlineData("70001234", "")]
    public async Task Campos_vacios_son_credenciales_invalidas(string? phone, string password)
    {
        var result = await _handler.Handle(new LoginCommand(phone, password), default);

        Assert.Equal(UserErrors.InvalidCredentials, result.Error);
    }
}
