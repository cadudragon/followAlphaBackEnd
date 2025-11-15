using NetZerion.Exceptions;
using NetZerion.Http;
using NetZerion.Models.Enums;
using NetZerion.Models.JsonApi;
using NetZerion.Models.Responses;
using NetZerion.Utilities;

namespace NetZerion.Clients;

/// <summary>
/// Implementation of wallet-related Zerion API endpoints.
/// </summary>
public class WalletClient : IWalletClient
{
    private readonly ZerionHttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="WalletClient"/> class.
    /// </summary>
    /// <param name="httpClient">Configured Zerion HTTP client.</param>
    public WalletClient(ZerionHttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<PositionsResponse> GetPositionsAsync(
        string address,
        IEnumerable<ChainId> chainIds,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ValidationException(nameof(address), "Wallet address cannot be empty");

        if (!address.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException(nameof(address), "Wallet address must start with 0x");

        var chainIdList = chainIds?.ToList() ?? throw new ArgumentNullException(nameof(chainIds));
        if (!chainIdList.Any())
            throw new ValidationException(nameof(chainIds), "At least one chain ID must be provided");

        // Build comma-separated chain IDs string (e.g., "polygon,base,arbitrum")
        var chainIdsString = string.Join(",", chainIdList.Select(c => c.ToApiString()));
        var endpoint = $"wallets/{address}/positions/?filter[positions]=only_complex&filter[chain_ids]={chainIdsString}&filter[trash]=only_non_trash&currency=usd&sort=-value";

        try
        {
            var apiResponse = await _httpClient.GetAsync<JsonApiResponse<ZerionPositionAttributes>>(
                endpoint,
                cancellationToken);

            return ZerionMapper.MapPositionsResponse(apiResponse);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            // 404 means wallet has no positions on these chains - return empty response
            return new PositionsResponse
            {
                Data = new List<Models.Entities.Position>(),
                Links = null,
                TotalCount = 0
            };
        }
    }

    /// <inheritdoc />
    public async Task<PortfolioResponse> GetPortfolioAsync(
        string address,
        ChainId chainId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ValidationException(nameof(address), "Wallet address cannot be empty");

        if (!address.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException(nameof(address), "Wallet address must start with 0x");

        var chainIdString = chainId.ToApiString();
        var endpoint = $"wallets/{address}/portfolio/?filter[chain_ids]={chainIdString}";

        try
        {
            var apiResponse = await _httpClient.GetAsync<JsonApiResponse<ZerionFungibleAttributes>>(
                endpoint,
                cancellationToken);

            return ZerionMapper.MapPortfolioResponse(apiResponse);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            // 404 means wallet has no portfolio on this chain - return empty response
            return new PortfolioResponse
            {
                Data = new List<Models.Entities.Fungible>(),
                Links = null,
                TotalCount = 0
            };
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<ChainId, PositionsResponse>> GetMultiChainPositionsAsync(
        string address,
        IEnumerable<ChainId> chainIds,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ValidationException(nameof(address), "Wallet address cannot be empty");

        var chainIdList = chainIds?.ToList() ?? throw new ArgumentNullException(nameof(chainIds));
        if (!chainIdList.Any())
            throw new ValidationException(nameof(chainIds), "At least one chain ID must be provided");

        // Make a single API call with all chains
        var allPositions = await GetPositionsAsync(address, chainIdList, cancellationToken);

        // Group positions by chain using the Chain property populated by the mapper
        var positionsByChain = allPositions.Data
            .Where(p => p.Chain != null && !string.IsNullOrEmpty(p.Chain.Id))
            .GroupBy(p => ChainIdMapper.FromApiString(p.Chain!.Id))
            .Where(g => g.Key.HasValue)
            .ToDictionary(
                g => g.Key!.Value,
                g => new PositionsResponse
                {
                    Data = g.ToList(),
                    Links = allPositions.Links,
                    TotalCount = g.Count()
                });

        // Ensure all requested chains are in the result (even if empty)
        foreach (var chainId in chainIdList)
        {
            if (!positionsByChain.ContainsKey(chainId))
            {
                positionsByChain[chainId] = new PositionsResponse
                {
                    Data = new List<Models.Entities.Position>(),
                    Links = null,
                    TotalCount = 0
                };
            }
        }

        return positionsByChain;
    }
}
