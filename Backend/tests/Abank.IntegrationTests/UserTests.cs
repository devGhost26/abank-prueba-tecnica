using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Abank.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class UserTests(AbankApiFactory factory)
{
    // Cada prueba usa su propio teléfono y email: comparten la misma base.
    private static int _phoneSuffix = 1000;

    private static object NewUserBody(out string phone)
    {
        phone = $"7100{Interlocked.Increment(ref _phoneSuffix):D4}";
        return new
        {
            nombres = "Prueba",
            apellidos = "Integración",
            fechanacimiento = "1996-05-10",
            telefono = phone,
            email = $"prueba{phone}@abank.test",
            password = "Clave2026",
            direccion = "San Salvador"
        };
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    [Fact]
    public async Task Crear_responde_201_con_location_id_de_la_secuencia_y_fecha_del_trigger()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/users", NewUserBody(out _));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await ReadJsonAsync(response);
        var id = user.GetProperty("id").GetInt32();
        Assert.True(id > 5, "Los cinco primeros Id son del script de datos.");
        Assert.Equal($"/api/v1/users/{id}", response.Headers.Location?.ToString());
        Assert.Equal("A", user.GetProperty("estado").GetString());
        Assert.NotEqual(JsonValueKind.Null, user.GetProperty("fechacreacion").ValueKind);
        Assert.Equal(JsonValueKind.Null, user.GetProperty("fechamodificacion").ValueKind);
        Assert.False(user.TryGetProperty("password", out _));

        var storedHash = await factory.QueryScalarAsync<string>($"SELECT password FROM usuarios WHERE id = {id}");
        Assert.StartsWith("$2", storedHash);
    }

    [Fact]
    public async Task El_usuario_creado_puede_iniciar_sesion()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync("/api/v1/users", NewUserBody(out var phone));

        var newUserClient = await factory.CreateAuthenticatedClientAsync(phone, "Clave2026");

        Assert.Equal(HttpStatusCode.OK, (await newUserClient.GetAsync("/api/v1/users")).StatusCode);
    }

    [Fact]
    public async Task Crear_con_datos_invalidos_devuelve_400_con_cada_campo()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/users", new { nombres = "x", password = "123" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await ReadJsonAsync(response)).GetProperty("errors");
        foreach (var field in new[] { "apellidos", "fechanacimiento", "direccion", "telefono", "email", "password" })
            Assert.True(errors.TryGetProperty(field, out _), $"Falta el error de {field}.");
    }

    [Fact]
    public async Task Telefono_o_email_repetidos_son_409_por_la_restriccion_unica()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var body = NewUserBody(out _);
        await client.PostAsJsonAsync("/api/v1/users", body);

        var samePhone = await client.PostAsJsonAsync("/api/v1/users", body);
        var sameEmailOtherCase = await client.PostAsJsonAsync("/api/v1/users", new
        {
            nombres = "Otra", apellidos = "Persona", fechanacimiento = "1990-01-01", telefono = "79998888",
            email = "ADMIN@abank.test", password = "Clave2026", direccion = "Santa Tecla"
        });

        Assert.Equal(HttpStatusCode.Conflict, samePhone.StatusCode);
        Assert.Equal("User.PhoneAlreadyExists", (await ReadJsonAsync(samePhone)).GetProperty("errorCode").GetString());
        Assert.Equal(HttpStatusCode.Conflict, sameEmailOtherCase.StatusCode);
        Assert.Equal("User.EmailAlreadyExists", (await ReadJsonAsync(sameEmailOtherCase)).GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Actualizar_marca_fecha_de_modificacion_y_conserva_la_contrasena_si_no_viene()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var created = await ReadJsonAsync(await client.PostAsJsonAsync("/api/v1/users", NewUserBody(out var phone)));
        var id = created.GetProperty("id").GetInt32();

        var response = await client.PutAsJsonAsync($"/api/v1/users/{id}", new
        {
            id, nombres = "Prueba Editada", apellidos = "Integración", fechanacimiento = "1996-05-10",
            telefono = phone, email = $"prueba{phone}@abank.test", direccion = "Nueva dirección"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await ReadJsonAsync(response);
        Assert.Equal("Prueba Editada", updated.GetProperty("nombres").GetString());
        Assert.Equal(created.GetProperty("fechacreacion").GetString(), updated.GetProperty("fechacreacion").GetString());
        Assert.NotEqual(JsonValueKind.Null, updated.GetProperty("fechamodificacion").ValueKind);
        // La contraseña sigue siendo la misma.
        await factory.CreateAuthenticatedClientAsync(phone, "Clave2026");
    }

    [Fact]
    public async Task Actualizar_con_id_distinto_en_el_cuerpo_es_400()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync("/api/v1/users/2", new { id = 3, nombres = "X" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Eliminar_es_baja_logica_y_revoca_el_acceso_al_momento()
    {
        var admin = await factory.CreateAuthenticatedClientAsync();
        var created = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/v1/users", NewUserBody(out var phone)));
        var id = created.GetProperty("id").GetInt32();
        var userClient = await factory.CreateAuthenticatedClientAsync(phone, "Clave2026");

        var response = await admin.DeleteAsync($"/api/v1/users/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("I", (await ReadJsonAsync(response)).GetProperty("estado").GetString());
        Assert.Equal("I", await factory.QueryScalarAsync<string>($"SELECT estado FROM usuarios WHERE id = {id}"));
        // Su token todavía no vence, pero ya no sirve.
        Assert.Equal(HttpStatusCode.Unauthorized, (await userClient.GetAsync("/api/v1/users")).StatusCode);
    }

    [Fact]
    public async Task Nadie_puede_eliminarse_a_si_mismo()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.DeleteAsync("/api/v1/users/1");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Obtener_un_usuario_inexistente_es_404()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v1/users/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Listar_devuelve_un_arreglo_paginado_con_el_total_en_la_cabecera()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v1/users?estado=A&tamano=2&pagina=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Array, users.ValueKind);
        Assert.Equal(2, users.GetArrayLength());
        Assert.All(users.EnumerateArray(), user => Assert.Equal("A", user.GetProperty("estado").GetString()));
        var total = int.Parse(response.Headers.GetValues("X-Total-Count").Single());
        Assert.True(total >= 4);
    }

    [Fact]
    public async Task Buscar_trata_los_comodines_como_texto()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var all = await ReadJsonAsync(await client.GetAsync("/api/v1/users?buscar=%25"));
        var byName = await ReadJsonAsync(await client.GetAsync("/api/v1/users?buscar=hern%C3%A1ndez"));

        Assert.Equal(0, all.GetArrayLength());
        Assert.Contains(byName.EnumerateArray(), user => user.GetProperty("id").GetInt32() == 2);
    }

    [Fact]
    public async Task La_base_rechaza_un_estado_fuera_del_check()
    {
        var exception = await Assert.ThrowsAsync<Npgsql.PostgresException>(() =>
            factory.QueryScalarAsync<object>("UPDATE usuarios SET estado = 'X' WHERE id = 3"));

        Assert.Equal("ck_usuarios_estado", exception.ConstraintName);
    }
}
