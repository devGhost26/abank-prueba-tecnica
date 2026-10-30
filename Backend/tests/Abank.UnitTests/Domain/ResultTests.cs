using Abank.Domain.Common.Results;

namespace Abank.UnitTests.Domain;

public sealed class ResultTests
{
    [Fact]
    public void Un_resultado_exitoso_expone_su_valor()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Leer_el_valor_de_un_fallo_lanza()
    {
        Result<int> result = Error.NotFound("X", "no");

        Assert.True(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void FieldErrors_agrupa_varios_mensajes_del_mismo_campo()
    {
        var errors = new FieldErrors();
        errors.Add("password", "corta");
        errors.Add("password", "sin números");

        var error = errors.ToError();

        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal(["corta", "sin números"], error.Errors["password"]);
    }
}
