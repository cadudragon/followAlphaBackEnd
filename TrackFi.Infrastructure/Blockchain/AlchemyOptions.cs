using TrackFi.Domain.Enums;

namespace TrackFi.Infrastructure.Blockchain;

/// <summary>
/// Configuration options for Alchemy API.
/// Reference: https://docs.alchemy.com/reference/api-overview
/// </summary>
public class AlchemyOptions
{
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Priority networks to query using the multi-network endpoint.
    /// Networks are queried in batches based on BatchLimits configuration.
    /// Add more networks here to expand coverage - they will be automatically batched.
    /// </summary>
    public List<string> MultiNetworkPriorityNetworks { get; set; } = [];

    /// <summary>
    /// Batch size limits for Alchemy API endpoints.
    /// These limits are enforced by Alchemy's API and should not be exceeded.
    /// </summary>
    public AlchemyBatchLimits BatchLimits { get; set; } = new();

    /// <summary>
    /// Gets the priority networks as BlockchainNetwork enum values.
    /// Invalid network names are logged and skipped.
    /// </summary>
    public List<BlockchainNetwork> GetPriorityNetworks()
    {
        var networks = new List<BlockchainNetwork>();

        foreach (var networkName in MultiNetworkPriorityNetworks)
        {
            if (Enum.TryParse<BlockchainNetwork>(networkName, ignoreCase: true, out var network))
            {
                networks.Add(network);
            }
        }

        return networks;
    }
}

/// <summary>
/// Batch size limits for Alchemy API endpoints.
/// These limits are enforced by Alchemy and documented in their API reference.
/// Reference: https://docs.alchemy.com/reference/api-overview
/// </summary>
public class AlchemyBatchLimits
{
    /// <summary>
    /// Maximum number of distinct networks that can be queried in a single request
    /// for the multi-network token balances endpoint (/assets/tokens/by-address).
    /// Alchemy documented limit: 5 networks per request.
    /// Default: 5
    /// </summary>
    public int MultiNetworkBalancesMaxNetworks { get; set; } = 5;

    /// <summary>
    /// Maximum number of distinct networks that can be included in a single request
    /// for the Prices API endpoint (/prices/v1/{apiKey}/tokens/by-address).
    /// Alchemy undocumented limit (discovered via testing): 3 networks per request.
    /// API returns 400 Bad Request with message "Maximum of 3 distinct networks allowed" if exceeded.
    /// Default: 3
    /// </summary>
    public int PricesApiMaxNetworks { get; set; } = 3;

    /// <summary>
    /// Validates that batch limits are within acceptable ranges.
    /// </summary>
    public void Validate()
    {
        if (MultiNetworkBalancesMaxNetworks <= 0)
            throw new InvalidOperationException(
                $"{nameof(MultiNetworkBalancesMaxNetworks)} must be greater than 0. Current value: {MultiNetworkBalancesMaxNetworks}");

        if (PricesApiMaxNetworks <= 0)
            throw new InvalidOperationException(
                $"{nameof(PricesApiMaxNetworks)} must be greater than 0. Current value: {PricesApiMaxNetworks}");

        // Warn if exceeding known API limits (but don't fail - API may have changed)
        if (MultiNetworkBalancesMaxNetworks > 5)
            Console.WriteLine(
                $"WARNING: {nameof(MultiNetworkBalancesMaxNetworks)} is set to {MultiNetworkBalancesMaxNetworks}, " +
                "which exceeds Alchemy's documented limit of 5. This may cause API errors.");

        if (PricesApiMaxNetworks > 3)
            Console.WriteLine(
                $"WARNING: {nameof(PricesApiMaxNetworks)} is set to {PricesApiMaxNetworks}, " +
                "which exceeds Alchemy's known limit of 3. This may cause API errors.");
    }
}
