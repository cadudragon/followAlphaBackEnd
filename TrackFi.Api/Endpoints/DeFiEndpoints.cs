using Microsoft.AspNetCore.Mvc;
using TrackFi.Domain.Enums;
using TrackFi.Infrastructure.Portfolio;

namespace TrackFi.Api.Endpoints;

/// <summary>
/// Endpoints for DeFi portfolio positions powered by Zerion API.
/// </summary>
public static class DeFiEndpoints
{
    /// <summary>
    /// Registers DeFi-related endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapDeFiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/defi")
            .WithTags("DeFi Portfolio")
            .WithOpenApi();

        group.MapGet("/positions", GetPositions)
            .WithName("GetDeFiPositions")
            .WithSummary("Get DeFi positions for a specific network")
            .WithDescription("Returns staking, lending, farming, and yield positions from protocols like Uniswap, Aave, Curve, etc. on a single blockchain network");

        group.MapGet("/positions/all-chains", GetAllChainsPositions)
            .WithName("GetAllChainsDeFiPositions")
            .WithSummary("Get DeFi positions across all configured networks")
            .WithDescription("Aggregates DeFi positions from all networks configured in appsettings DeFi:SupportedNetworks");

        return app;
    }

    /// <summary>
    /// Get DeFi positions for a wallet on a specific network.
    /// </summary>
    /// <param name="address">Wallet address (0x...)</param>
    /// <param name="network">Network name (ethereum, polygon, arbitrum, base, unichain)</param>
    /// <param name="defiService">DeFi portfolio service</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>DeFi positions with total value, protocols, and assets</returns>
    private static async Task<IResult> GetPositions(
        [FromQuery] string address,
        [FromQuery] string network,
        DeFiPortfolioService defiService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Wallet address is required", nameof(address));

        if (string.IsNullOrWhiteSpace(network))
            throw new ArgumentException("Network is required", nameof(network));

        if (!Enum.TryParse<BlockchainNetwork>(network, ignoreCase: true, out var blockchainNetwork))
        {
            throw new ArgumentException(
                $"Invalid network. Supported: {string.Join(", ", Enum.GetNames<BlockchainNetwork>())}",
                nameof(network));
        }

        var portfolio = await defiService.GetDeFiPositionsAsync(
            address,
            blockchainNetwork,
            cancellationToken);

        return Results.Ok(portfolio);
    }

    /// <summary>
    /// Get DeFi positions across all configured networks.
    /// Networks are controlled via appsettings DeFi:SupportedNetworks configuration.
    /// </summary>
    /// <param name="address">Wallet address (0x...)</param>
    /// <param name="defiService">DeFi portfolio service</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Aggregated DeFi positions across configured networks</returns>
    private static async Task<IResult> GetAllChainsPositions(
        [FromQuery] string address,
        DeFiPortfolioService defiService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Wallet address is required", nameof(address));

        var portfolio = await defiService.GetMultiNetworkDeFiPositionsAsync(
            address,
            cancellationToken);

        return Results.Ok(portfolio);
    }
}
