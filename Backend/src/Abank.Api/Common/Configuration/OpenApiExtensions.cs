using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;

namespace Abank.Api.Common.Configuration;

public static class OpenApiExtensions
{
    private const string BearerScheme = "Bearer";

    /// <summary>
    /// Declara el esquema Bearer en el documento y lo exige en cada operación protegida,
    /// para que Scalar muestre el candado y deje pegar el token.
    /// </summary>
    public static IServiceCollection AddOpenApiWithJwt(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "Abank · API de usuarios";
                document.Info.Description = "Gestión de usuarios y acceso. Inicia sesión en /api/v1/auth/login y usa el access_token como Bearer.";
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes[BearerScheme] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "El access_token que devuelve el login."
                };
                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, context, _) =>
            {
                var metadata = context.Description.ActionDescriptor.EndpointMetadata;
                var requiresAuth = metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any();

                if (requiresAuth)
                {
                    operation.Security ??= [];
                    operation.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference(BearerScheme, context.Document)] = []
                    });
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }
}
