using Abank.Api.Common.Configuration;
using Abank.Api.Common.Errors;
using Abank.Api.Common.Security;
using Abank.Api.Endpoints.Auth;
using Abank.Api.Endpoints.Users;
using Abank.Application;
using Abank.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Que un JSON mal formado llegue al GlobalExceptionHandler y responda con un detalle, no con un 400 vacío.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

builder.Services.AddOpenApiWithJwt();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddLoginRateLimit(builder.Configuration);
builder.Services.AddClientIpFromProxy(builder.Configuration);

var app = builder.Build();

// Primero: todo lo que sigue, incluido el límite de logins, debe ver la IP real del cliente.
app.UseClientIpFromProxy();

app.UseExceptionHandler();
// Las respuestas sin cuerpo (401 del token, 404 de ruta, 429) también salen como ProblemDetails.
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// La documentación queda expuesta para facilitar la revisión de la prueba;
// en un despliegue real del banco se limitaría a Development.
app.MapOpenApi();
app.MapScalarApiReference(options => options.WithTitle("Abank · API de usuarios"));
app.MapHealthChecks("/health");

app.MapAuthEndpoints();
app.MapUserEndpoints();

app.Run();

// Visible para WebApplicationFactory en las pruebas de integración.
public partial class Program;
