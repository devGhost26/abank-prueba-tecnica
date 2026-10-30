using Abank.Application.Users.CreateUser;
using Abank.Application.Users.DeactivateUser;
using Abank.Application.Users.ListUsers;
using Abank.Application.Users.UpdateUser;
using Abank.Domain.Common.Results;
using Abank.Domain.Users;
using Abank.UnitTests.Fakes;

namespace Abank.UnitTests.Application;

public sealed class UserCommandHandlerTests
{
    private readonly InMemoryUserRepository _repository = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly FixedTimeProvider _time = new(new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Crear_devuelve_el_usuario_con_el_id_asignado()
    {
        var handler = new CreateUserCommandHandler(_repository, _hasher, _time);

        var result = await handler.Handle(new CreateUserCommand(TestUsers.ValidData(), "Clave2026"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Id);
        Assert.Equal("A", result.Value.Status);
    }

    [Fact]
    public async Task Crear_con_telefono_repetido_es_conflicto()
    {
        _repository.Seed(TestUsers.NewUser("70001234"));
        var handler = new CreateUserCommandHandler(_repository, _hasher, _time);

        var result = await handler.Handle(new CreateUserCommand(TestUsers.ValidData("70001234"), "Clave2026"), default);

        Assert.Equal(UserErrors.PhoneAlreadyExists, result.Error);
    }

    [Fact]
    public async Task Nadie_puede_desactivarse_a_si_mismo()
    {
        var me = _repository.Seed(TestUsers.NewUser());
        var handler = new DeactivateUserCommandHandler(_repository, new FakeCurrentUser(me.Id));

        var result = await handler.Handle(new DeactivateUserCommand(me.Id), default);

        Assert.Equal(UserErrors.CannotDeactivateSelf, result.Error);
    }

    [Fact]
    public async Task Ni_por_PUT_con_estado_I()
    {
        var me = _repository.Seed(TestUsers.NewUser());
        var handler = new UpdateUserCommandHandler(_repository, _hasher, new FakeCurrentUser(me.Id), _time);

        var result = await handler.Handle(new UpdateUserCommand(me.Id, TestUsers.ValidData(), null, "I"), default);

        Assert.Equal(UserErrors.CannotDeactivateSelf, result.Error);
        Assert.Equal(0, _repository.UpdateCalls);
    }

    [Fact]
    public async Task Desactivar_dos_veces_responde_igual_y_escribe_una_sola_vez()
    {
        var other = _repository.Seed(TestUsers.NewUser());
        var handler = new DeactivateUserCommandHandler(_repository, new FakeCurrentUser(999));

        var first = await handler.Handle(new DeactivateUserCommand(other.Id), default);
        var second = await handler.Handle(new DeactivateUserCommand(other.Id), default);

        Assert.Equal("I", first.Value.Status);
        Assert.Equal("I", second.Value.Status);
        Assert.Equal(1, _repository.UpdateCalls);
    }

    [Fact]
    public async Task Actualizar_un_usuario_inexistente_es_404()
    {
        var handler = new UpdateUserCommandHandler(_repository, _hasher, new FakeCurrentUser(1), _time);

        var result = await handler.Handle(new UpdateUserCommand(42, TestUsers.ValidData(), null, null), default);

        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Theory]
    [InlineData(0, 10, null, ListUsersQuery.PageField)]
    [InlineData(1, 101, null, ListUsersQuery.PageSizeField)]
    [InlineData(1, 10, "X", UserFields.Status)]
    public async Task Listar_valida_paginacion_y_estado(int page, int pageSize, string? status, string field)
    {
        var handler = new ListUsersQueryHandler(null!);

        var result = await handler.Handle(new ListUsersQuery(null, status, page, pageSize), default);

        Assert.Contains(field, Assert.IsType<ValidationError>(result.Error).Errors.Keys);
    }
}
