using Abank.Domain.Users;
using Abank.Infrastructure.Security;
using Abank.UnitTests.Fakes;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Abank.UnitTests.Infrastructure;

public sealed class SecurityTests
{
    [Fact]
    public void BCrypt_verifica_su_propio_hash_y_rechaza_otra_contrasena()
    {
        var hasher = new BCryptPasswordHasher();

        var hash = hasher.Hash("Clave2026");

        Assert.StartsWith("$2", hash);
        Assert.True(hash.Length <= 120, "El hash debe caber en la columna password (120).");
        Assert.True(hasher.Verify("Clave2026", hash));
        Assert.False(hasher.Verify("Otra2026", hash));
    }

    [Fact]
    public void BCrypt_verifica_los_hashes_de_pgcrypto_del_script_de_datos()
    {
        // Generado con crypt('Abank2026*', gen_salt('bf', 12)), como en 02_datos_iniciales.sql.
        const string pgcryptoHash = "$2a$12$X/GV8h6cyqej9x3vv5z16eXCJSa3MqGDkqJcdJNPcraiSGChNWhpC";

        Assert.True(new BCryptPasswordHasher().Verify("Abank2026*", pgcryptoHash));
    }

    [Fact]
    public void Sin_hash_no_verifica_nunca()
    {
        Assert.False(new BCryptPasswordHasher().Verify("Clave2026", null));
    }

    [Fact]
    public void El_token_lleva_el_id_en_sub_y_vence_segun_la_configuracion()
    {
        var now = new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);
        var options = Options.Create(new JwtOptions
        {
            Issuer = "abank-api",
            Audience = "abank-clientes",
            SigningKey = new string('k', JwtOptions.MinimumKeyLength),
            ExpirationMinutes = 30
        });
        var issuer = new JwtAccessTokenIssuer(options, new FixedTimeProvider(now));
        var user = User.Restore(7, "Ana", "Rivas", new DateOnly(1996, 5, 10), "SS", "70001234",
            "ana@abank.test", "hash", UserStatus.Active, now, null);

        var token = issuer.Issue(user);
        var jwt = new JsonWebToken(token.Token);

        Assert.Equal(now.AddMinutes(30), token.ExpiresAt);
        Assert.Equal("7", jwt.Subject);
        Assert.Equal("abank-api", jwt.Issuer);
        Assert.Contains("abank-clientes", jwt.Audiences);
        Assert.Equal("HS256", jwt.Alg);
        Assert.DoesNotContain(jwt.Claims, claim => claim.Value == "hash");
    }
}
