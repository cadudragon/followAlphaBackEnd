using System.Reflection;
using TrackFi.Api.Endpoints;

namespace TrackFi.Tests.Api.Endpoints;

public class PortfolioPreviewEndpointsTests
{
    [Fact]
    public async Task GetTokenBalances_WithEmptyAddress_ThrowsArgumentException()
    {
        var act = async () => await InvokeTokenBalancesAsync(address: "", network: "Ethereum");

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task GetTokenBalances_WithInvalidNetwork_ThrowsArgumentException()
    {
        var act = async () => await InvokeTokenBalancesAsync(address: "0xa3660aBb49644876714611122b1618faA07e0281", network: "invalid");

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task GetNfts_WithMissingNetwork_ThrowsArgumentException()
    {
        var act = async () => await InvokeNftsAsync(address: "0xa3660aBb49644876714611122b1618faA07e0281", network: "");

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task GetMultiNetworkWallet_WithMissingAddress_ThrowsArgumentException()
    {
        var act = async () => await InvokeMultiNetworkWalletAsync(address: "");

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task GetMultiNetworkNfts_WithMissingAddress_ThrowsArgumentException()
    {
        var act = async () => await InvokeMultiNetworkNftsAsync(address: "");

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    private static async Task InvokeTokenBalancesAsync(string address, string network)
    {
        var method = typeof(PortfolioPreviewEndpoints).GetMethod(
            "GetWalletBalance",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(nameof(PortfolioPreviewEndpoints), "GetWalletBalance");

        await InvokeEndpointAsync(method, address, network, portfolioService: null!);
    }

    private static async Task InvokeNftsAsync(string address, string network)
    {
        var method = typeof(PortfolioPreviewEndpoints).GetMethod(
            "GetNfts",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(nameof(PortfolioPreviewEndpoints), "GetNfts");

        await InvokeEndpointAsync(method, address, network, portfolioService: null!);
    }

    private static async Task InvokeMultiNetworkWalletAsync(string address)
    {
        var method = typeof(PortfolioPreviewEndpoints).GetMethod(
            "GetAllChainsBalance",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(nameof(PortfolioPreviewEndpoints), "GetAllChainsBalance");

        await InvokeEndpointAsync(method, address, portfolioService: null!);
    }

    private static async Task InvokeMultiNetworkNftsAsync(string address)
    {
        var method = typeof(PortfolioPreviewEndpoints).GetMethod(
            "GetAllChainsNfts",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(nameof(PortfolioPreviewEndpoints), "GetAllChainsNfts");

        await InvokeEndpointAsync(method, address, portfolioService: null!);
    }

    private static async Task InvokeEndpointAsync(
        MethodInfo method,
        string address,
        string? network = null,
        object? portfolioService = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = method.GetParameters();
        var arguments = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            arguments[i] = parameter.Name switch
            {
                "address" => address,
                "network" => network,
                "portfolioService" => portfolioService,
                "cancellationToken" => cancellationToken,
                _ => null
            };
        }

        try
        {
            var result = method.Invoke(null, arguments);
            if (result is Task task)
            {
                await task;
            }
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }
}
