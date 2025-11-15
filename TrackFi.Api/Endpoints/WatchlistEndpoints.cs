using MediatR;
using Microsoft.AspNetCore.Mvc;
using TrackFi.Application.Watchlist.Commands.AddToWatchlist;
using TrackFi.Application.Watchlist.Commands.RemoveFromWatchlist;
using TrackFi.Application.Watchlist.Queries.GetWatchlist;

namespace TrackFi.Api.Endpoints;

public static class WatchlistEndpoints
{
    public static IEndpointRouteBuilder MapWatchlistEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/watchlist")
            .WithTags("Watchlist")
            .WithOpenApi();

        group.MapPost("/", AddToWatchlist)
            .WithName("AddToWatchlist")
            .WithSummary("Add a wallet to watchlist")
            .Produces<Application.Common.DTOs.WatchlistEntryDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet("/user/{userId:guid}", GetWatchlist)
            .WithName("GetWatchlist")
            .WithSummary("Get user's watchlist")
            .Produces<IEnumerable<Application.Common.DTOs.WatchlistEntryDto>>();

        group.MapDelete("/{entryId:guid}", RemoveFromWatchlist)
            .WithName("RemoveFromWatchlist")
            .WithSummary("Remove entry from watchlist")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> AddToWatchlist(
        [FromBody] AddToWatchlistCommand command,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Results.Created($"/api/watchlist/{result.Id}", result);
    }

    private static async Task<IResult> GetWatchlist(
        Guid userId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetWatchlistQuery { UserId = userId };
        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> RemoveFromWatchlist(
        Guid entryId,
        [FromBody] RemoveFromWatchlistRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new RemoveFromWatchlistCommand
        {
            UserId = request.UserId,
            EntryId = entryId
        };

        await sender.Send(command, cancellationToken);
        return Results.NoContent();
    }

    private record RemoveFromWatchlistRequest(Guid UserId);
}
