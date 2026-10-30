using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Abank.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class AuthTests(AbankApiFactory factory)
{
    [Fact]
    public async Task Login_responde_con_la_forma_del_enunciado_y_sin_el_password()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { telefono = AbankApiFactory.AdminPhone, password = AbankApiFactory.AdminPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var user = json.GetProperty("user");
        Assert.Equal(1, user.GetProperty("id").GetInt32());
        Assert.True(user.GetProperty("session_active").GetBoolean());
        Assert.Equal("1990-01-15", user.GetProperty("fechanacimiento").GetString());
        Assert.False(user.TryGetProperty("password", out _));
        Assert.Equal("bearer", json.GetProperty("token_type").GetString());
        Assert.Equal(3, json.GetProperty("access_token").GetString()!.Split('.').Length);
    }

    [Fact]
    public async Task Login_con_contrasena_incorrecta_es_401_con_problem_details()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { telefono = AbankApiFactory.AdminPhone, password = "Incorrecta1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Auth.InvalidCredentials", problem.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Un_usuario_inactivo_del_script_no_puede_entrar()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { telefono = "70000005", password = "Usuario2026*" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/v1/users")]
    [InlineData("GET", "/api/v1/users/1")]
    [InlineData("POST", "/api/v1/users")]
    [InlineData("PUT", "/api/v1/users/1")]
    [InlineData("DELETE", "/api/v1/users/1")]
    public async Task Los_endpoints_de_usuarios_exigen_token(string method, string url)
    {
        var client = factory.CreateClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Un_token_alterado_es_rechazado()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.firma-falsa");

        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
