using Microsoft.AspNetCore.Mvc;
using TrackFi.Domain.Enums;
using TrackFi.Infrastructure.Portfolio;

namespace TrackFi.Api.Endpoints;

public static class PortfolioPreviewEndpoints
{
    public static IEndpointRouteBuilder MapPortfolioPreviewEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/portfolio/preview")
            .WithTags("Portfolio Preview (Anonymous)")
            .WithOpenApi();

        // ===== WALLET BALANCE (Liquid assets in wallet) =====

        group.MapGet("/balance", GetWalletBalance)
            .WithName("GetWalletBalance")
            .WithSummary("Get wallet balance for a specific network")
            .WithDescription("Returns liquid token balances (not staked/deployed) with current prices for a single blockchain network")
            .Produces<List<Application.Portfolio.DTOs.TokenBalanceDto>>()
            .ProducesValidationProblem();

        group.MapGet("/balance/all-chains", GetAllChainsBalance)
            .WithName("GetAllChainsBalance")
            .WithSummary("Get wallet balance across all supported networks")
            .WithDescription("Aggregates liquid token balances from Ethereum, Polygon, Arbitrum, Base, and Unichain")
            .Produces<Application.Portfolio.DTOs.MultiNetworkWalletDto>()
            .ProducesValidationProblem();

        // ===== NFTs =====

        group.MapGet("/nfts", GetNfts)
            .WithName("GetNfts")
            .WithSummary("Get NFTs for a specific network")
            .WithDescription("Returns NFT collection for a single blockchain network")
            .Produces<List<Application.Portfolio.DTOs.NftDto>>()
            .ProducesValidationProblem();

        group.MapGet("/nfts/all-chains", GetAllChainsNfts)
            .WithName("GetAllChainsNfts")
            .WithSummary("Get NFTs across all supported networks")
            .WithDescription("Aggregates NFTs from Ethereum, Polygon, Arbitrum, Base, and Unichain")
            .Produces<Application.Portfolio.DTOs.MultiNetworkNftDto>()
            .ProducesValidationProblem();

        return app;
    }

    private static async Task<IResult> GetWalletBalance(
        [FromQuery] string address,
        [FromQuery] string network,
        AnonymousPortfolioService portfolioService,
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

        var tokens = await portfolioService.GetTokenBalancesAsync(
            address,
            blockchainNetwork,
            cancellationToken);

        return Results.Ok(tokens);
    }

    private static async Task<IResult> GetNfts(
        [FromQuery] string address,
        [FromQuery] string network,
        AnonymousPortfolioService portfolioService,
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

        var nfts = await portfolioService.GetNftsAsync(
            address,
            blockchainNetwork,
            cancellationToken);

        return Results.Ok(nfts);
    }

    private static async Task<IResult> GetAllChainsBalance(
        [FromQuery] string address,
        AnonymousPortfolioService portfolioService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Wallet address is required", nameof(address));

        var wallet = await portfolioService.GetMultiNetworkWalletAsync(
            address,
            cancellationToken);

        return Results.Ok(wallet);
    }

    private static async Task<IResult> GetAllChainsNfts(
        [FromQuery] string address,
        AnonymousPortfolioService portfolioService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Wallet address is required", nameof(address));

        var nfts = await portfolioService.GetMultiNetworkNftsAsync(
            address,
            cancellationToken);

        return Results.Ok(nfts);
    }
}
