using Microsoft.AspNetCore.Mvc;
using TrackFi.Infrastructure.Portfolio;

namespace TrackFi.Api.Endpoints;

/// <summary>
/// Portfolio endpoints using unified PortfolioService with Zerion provider.
/// Returns original DTO structures: MultiNetworkWalletDto, MultiNetworkDeFiPortfolioDto, FullPortfolioDto.
/// </summary>
public static class PortfolioEndpoints
{
    public static void MapPortfolioEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/portfolio")
            .WithTags("Portfolio")
            .WithOpenApi();

        // Endpoint 1: Wallet positions only (only_simple filter)
        group.MapGet("/{wallet}/positions", async (
            string wallet,
            [FromQuery] string? networks,
            PortfolioService service,
            CancellationToken ct) =>
        {
            var networkList = ParseNetworks(networks);
            var result = await service.GetWalletPositionsAsync(wallet, networkList, ct);
            return Results.Ok(result);
        })
        .WithName("GetWalletPositions")
        .WithSummary("Get wallet token positions (simple positions)")
        .WithDescription("Returns wallet tokens across specified networks using Zerion only_simple filter. Excludes DeFi positions. Returns MultiNetworkWalletDto structure.")
        .Produces(200)
        .Produces(400)
        .Produces(500);

        // Endpoint 2: DeFi positions only (only_complex filter)
        group.MapGet("/{wallet}/defi", async (
            string wallet,
            [FromQuery] string? networks,
            PortfolioService service,
            CancellationToken ct) =>
        {
            var networkList = ParseNetworks(networks);
            var result = await service.GetDeFiPositionsAsync(wallet, networkList, ct);
            return Results.Ok(result);
        })
        .WithName("GetDeFiPositions")
        .WithSummary("Get DeFi positions (complex positions)")
        .WithDescription("Returns DeFi positions (lending, staking, farming, liquidity pools, etc.) across specified networks using Zerion only_complex filter. Returns MultiNetworkDeFiPortfolioDto structure.")
        .Produces(200)
        .Produces(400)
        .Produces(500);

        // Endpoint 3: Full portfolio (no_filter - wallet + DeFi)
        group.MapGet("/{wallet}/full", async (
            string wallet,
            [FromQuery] string? networks,
            PortfolioService service,
            CancellationToken ct) =>
        {
            var networkList = ParseNetworks(networks);
            var result = await service.GetFullPortfolioAsync(wallet, networkList, ct);
            return Results.Ok(result);
        })
        .WithName("GetFullPortfolio")
        .WithSummary("Get full portfolio (wallet + DeFi)")
        .WithDescription("Returns complete portfolio including both wallet tokens and DeFi positions across specified networks using Zerion no_filter. Returns FullPortfolioDto structure.")
        .Produces(200)
        .Produces(400)
        .Produces(500);
    }

    /// <summary>
    /// Parses comma-separated network names into a list.
    /// If null or empty, returns empty list (which triggers ALL networks fetch).
    /// </summary>
    private static List<string> ParseNetworks(string? networks)
    {
        if (string.IsNullOrWhiteSpace(networks))
        {
            // Empty list = fetch ALL supported networks from provider
            return new List<string>();
        }

        return networks.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }
}
