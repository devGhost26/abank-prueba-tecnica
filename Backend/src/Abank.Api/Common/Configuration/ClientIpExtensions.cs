using Microsoft.AspNetCore.HttpOverrides;

namespace Abank.Api.Common.Configuration;

/// <summary>
/// Detrás de proxies, la conexión llega desde el último salto y no desde el cliente, y el límite de
/// intentos de login quedaría atado a la IP del proxy. Con <c>Proxy:ClientIpHeader</c> configurado,
/// la IP del cliente se toma de esa cabecera.
/// </summary>
/// <remarks>
/// Solo es seguro si el proxy de adelante <b>reemplaza</b> la cabecera y el servidor no es alcanzable
/// sin pasar por él; si no, el cliente podría inventarse una IP en cada intento. En Render, Cloudflare
/// rechaza las peticiones que ya traen <c>CF-Connecting-IP</c> y la escribe él. Sin la configuración
/// (local y Docker), la API usa la IP de la conexión.
/// </remarks>
public static class ClientIpExtensions
{
    private const string ClientIpHeaderKey = "Proxy:ClientIpHeader";

    public static IServiceCollection AddClientIpFromProxy(this IServiceCollection services, IConfiguration configuration)
    {
        var header = configuration[ClientIpHeaderKey];
        if (string.IsNullOrWhiteSpace(header))
            return services;

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            // El proxy termina el HTTPS: sin X-Forwarded-Proto la API cree que la petición llegó por
            // http y lo anuncia así en el documento OpenAPI, y Scalar no puede llamarla desde https.
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardedForHeaderName = header;
            options.ForwardLimit = 1;
            // Las IPs de los proxies de Render y Cloudflare cambian; la confianza la da la cabecera, no la IP.
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    public static WebApplication UseClientIpFromProxy(this WebApplication app)
    {
        if (!string.IsNullOrWhiteSpace(app.Configuration[ClientIpHeaderKey]))
            app.UseForwardedHeaders();

        return app;
    }
}
