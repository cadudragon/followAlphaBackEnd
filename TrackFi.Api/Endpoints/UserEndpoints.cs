using MediatR;
using Microsoft.AspNetCore.Mvc;
using TrackFi.Application.Users.Commands.CreateUser;
using TrackFi.Application.Users.Commands.UpdateUser;
using TrackFi.Application.Users.Queries.GetUser;
using TrackFi.Application.Users.Queries.GetUserByWallet;

namespace TrackFi.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .WithOpenApi();

        group.MapPost("/", CreateUser)
            .WithName("CreateUser")
            .WithSummary("Create a new user with a Web3 wallet")
            .Produces<Application.Common.DTOs.UserDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet("/{id:guid}", GetUser)
            .WithName("GetUser")
            .WithSummary("Get user by ID")
            .Produces<Application.Common.DTOs.UserDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/wallet/{walletAddress}", GetUserByWallet)
            .WithName("GetUserByWallet")
            .WithSummary("Get user by wallet address")
            .Produces<Application.Common.DTOs.UserDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/cover", UpdateUserCover)
            .WithName("UpdateUserCover")
            .WithSummary("Update user cover NFT")
            .Produces<Application.Common.DTOs.UserDto>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        return app;
    }

    private static async Task<IResult> CreateUser(
        [FromBody] CreateUserCommand command,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Results.Created($"/api/users/{result.Id}", result);
    }

    private static async Task<IResult> GetUser(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetUserQuery { UserId = id };
        var result = await sender.Send(query, cancellationToken);

        return result is not null
            ? Results.Ok(result)
            : Results.NotFound(new { message = "User not found" });
    }

    private static async Task<IResult> GetUserByWallet(
        string walletAddress,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetUserByWalletQuery { WalletAddress = walletAddress };
        var result = await sender.Send(query, cancellationToken);

        return result is not null
            ? Results.Ok(result)
            : Results.NotFound(new { message = "User not found" });
    }

    private static async Task<IResult> UpdateUserCover(
        Guid id,
        [FromBody] UpdateUserCoverRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUserCoverCommand
        {
            UserId = id,
            CoverPictureUrl = request.CoverPictureUrl,
            CoverNftContract = request.CoverNftContract,
            CoverNftTokenId = request.CoverNftTokenId,
            CoverNftNetwork = request.CoverNftNetwork
        };

        await sender.Send(command, cancellationToken);

        return Results.Ok(new { message = "User cover updated successfully" });
    }

    private record UpdateUserCoverRequest(string? CoverPictureUrl, string? CoverNftContract, string? CoverNftTokenId, string? CoverNftNetwork);
}
