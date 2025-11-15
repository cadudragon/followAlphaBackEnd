using MediatR;
using Microsoft.AspNetCore.Mvc;
using TrackFi.Application.UserWallets.Commands.AddUserWallet;
using TrackFi.Application.UserWallets.Commands.VerifyUserWallet;
using TrackFi.Application.UserWallets.Queries.GetUserWallets;

namespace TrackFi.Api.Endpoints;

public static class UserWalletEndpoints
{
    public static IEndpointRouteBuilder MapUserWalletEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users/{userId:guid}/wallets")
            .WithTags("User Wallets")
            .WithOpenApi();

        group.MapPost("/", AddUserWallet)
            .WithName("AddUserWallet")
            .WithSummary("Add a new wallet to user account")
            .Produces<Application.Common.DTOs.UserWalletDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet("/", GetUserWallets)
            .WithName("GetUserWallets")
            .WithSummary("Get all wallets for a user")
            .Produces<IEnumerable<Application.Common.DTOs.UserWalletDto>>();

        group.MapPost("/{walletId:guid}/verify", VerifyUserWallet)
            .WithName("VerifyUserWallet")
            .WithSummary("Verify wallet ownership via signature")
            .Produces<Application.Common.DTOs.UserWalletDto>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        return app;
    }

    private static async Task<IResult> AddUserWallet(
        Guid userId,
        [FromBody] AddUserWalletRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new AddUserWalletCommand
        {
            UserId = userId,
            WalletAddress = request.WalletAddress,
            Network = request.Network,
            Label = request.Label
        };

        var result = await sender.Send(command, cancellationToken);
        return Results.Created($"/api/users/{userId}/wallets/{result.Id}", result);
    }

    private static async Task<IResult> GetUserWallets(
        Guid userId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetUserWalletsQuery { UserId = userId };
        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> VerifyUserWallet(
        Guid userId,
        Guid walletId,
        [FromBody] VerifyWalletRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new VerifyUserWalletCommand
        {
            WalletId = walletId,
            Message = request.Message,
            Signature = request.Signature
        };

        await sender.Send(command, cancellationToken);

        return Results.Ok(new { message = "Wallet verified successfully" });
    }

    private record AddUserWalletRequest(string WalletAddress, string Network, string? Label);
    private record VerifyWalletRequest(string Message, string Signature);
}
