using Abank.Api.Common.Errors;
using Abank.Api.Contracts.Users;
using Abank.Application.Users.CreateUser;
using Abank.Application.Users.DeactivateUser;
using Abank.Application.Users.GetUserById;
using Abank.Application.Users.ListUsers;
using Abank.Application.Users.UpdateUser;
using Abank.Domain.Common.Results;
using Mediator;

namespace Abank.Api.Endpoints.Users;

public static class UserEndpoints
{
    public const string TotalCountHeader = "X-Total-Count";

    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        // Todo el grupo exige token. El enunciado no marca GetUsers como protegido, pero
        // listar los datos personales de todos los usuarios sin autenticación sería una fuga.
        var group = app.MapGroup("/api/v1/users")
            .WithTags("Users")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/", ListUsers)
            .WithSummary("Lista los usuarios, paginado")
            .WithDescription($"Devuelve un arreglo, como el ejemplo del enunciado. El total va en la cabecera {TotalCountHeader}.")
            .Produces<UserResponse[]>()
            .ProducesValidationProblem();

        group.MapGet("/{id_user:int}", GetUser)
            .WithSummary("Obtiene un usuario")
            .Produces<UserResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateUser)
            .WithSummary("Crea un usuario")
            .Produces<UserResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id_user:int}", UpdateUser)
            .WithSummary("Actualiza un usuario")
            .WithDescription("La contraseña es opcional: si no viene, se conserva. El estado (A o I) también.")
            .Produces<UserResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id_user:int}", DeleteUser)
            .WithSummary("Da de baja un usuario")
            .WithDescription("Baja lógica: el usuario pasa a estado I y ya no puede iniciar sesión. Devuelve el usuario.")
            .Produces<UserResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ListUsers(
        [AsParameters] ListUsersRequest request, HttpResponse response, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToQuery(), cancellationToken);
        if (result.IsFailure)
            return result.Error.ToProblem();

        response.Headers[TotalCountHeader] = result.Value.TotalCount.ToString();

        return Results.Ok(result.Value.Items.Select(UserResponse.From));
    }

    private static async Task<IResult> GetUser(int id_user, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUserByIdQuery(id_user), cancellationToken);

        return result.IsSuccess ? Results.Ok(UserResponse.From(result.Value)) : result.Error.ToProblem();
    }

    private static async Task<IResult> CreateUser(CreateUserRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateUserCommand(request.ToUserData(), request.Password), cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/users/{result.Value.Id}", UserResponse.From(result.Value))
            : result.Error.ToProblem();
    }

    private static async Task<IResult> UpdateUser(
        int id_user, UpdateUserRequest request, ISender sender, CancellationToken cancellationToken)
    {
        if (request.Id is { } bodyId && bodyId != id_user)
            return Error.Validation("User.IdMismatch", "El id del cuerpo no coincide con el de la ruta.").ToProblem();

        var command = new UpdateUserCommand(id_user, request.ToUserData(), request.Password, request.Estado);
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess ? Results.Ok(UserResponse.From(result.Value)) : result.Error.ToProblem();
    }

    private static async Task<IResult> DeleteUser(int id_user, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeactivateUserCommand(id_user), cancellationToken);

        return result.IsSuccess ? Results.Ok(UserResponse.From(result.Value)) : result.Error.ToProblem();
    }
}
