# NetZerion

[![NuGet](https://img.shields.io/nuget/v/NetZerion.svg)](https://www.nuget.org/packages/NetZerion)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A first-class .NET wrapper for the [Zerion API](https://developers.zerion.io/), providing strongly-typed, async-first access to DeFi positions, token balances, and transaction history across multiple blockchain networks.

## Features

- 🎯 **Strongly-typed** - Full IntelliSense support with comprehensive XML documentation
- ⚡ **Async/await** - Built for modern .NET with cancellation token support
- 🔄 **Resilient** - Built-in retry policies and rate limiting
- 🧪 **Well-tested** - >85% code coverage with unit and integration tests
- 📦 **Easy integration** - Dependency injection support for ASP.NET Core
- 🛡️ **Production-ready** - Comprehensive error handling and logging

## Installation

```bash
dotnet add package NetZerion --version 1.0.0-preview.1
```

## Quick Start

### Basic Usage

```csharp
using NetZerion;

// Create client with API key
using var client = new NetZerionClient("your-api-key");

// Get DeFi positions
var positions = await client.Wallet.GetPositionsAsync(
    "0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb",
    ChainId.Ethereum);

foreach (var position in positions.Data)
{
    Console.WriteLine($"{position.Protocol.Name}: ${position.ValueUsd:N2}");
}

// Get token portfolio
var portfolio = await client.Wallet.GetPortfolioAsync(
    "0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb",
    ChainId.Ethereum);

foreach (var token in portfolio.Data)
{
    Console.WriteLine($"{token.Symbol}: {token.BalanceDecimal} (${token.ValueUsd:N2})");
}

// Get transaction history
var transactions = await client.Transactions.GetHistoryAsync(
    "0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb",
    ChainId.Ethereum,
    limit: 10);

foreach (var tx in transactions.Data)
{
    Console.WriteLine($"{tx.Type}: {tx.Description} - ${tx.ValueUsd:N2}");
}
```

### Dependency Injection (ASP.NET Core)

```csharp
// Program.cs
builder.Services.AddNetZerion(options =>
{
    options.Timeout = TimeSpan.FromSeconds(30);
    options.MaxRetries = 3;
    options.RateLimits = new RateLimitOptions
    {
        RequestsPerDay = 3000,
        RequestsPerMinute = 100
    };
});

// Configure API key from user secrets
builder.Configuration.AddUserSecrets<Program>();

// Usage in service
public class DeFiService
{
    private readonly IWalletClient _walletClient;

    public DeFiService(IWalletClient walletClient)
    {
        _walletClient = walletClient;
    }

    public async Task<decimal> GetTotalDeFiValue(string address)
    {
        var positions = await _walletClient.GetPositionsAsync(address, ChainId.Ethereum);
        return positions.Data.Sum(p => p.ValueUsd);
    }
}
```

## Supported Networks

- Ethereum (`ChainId.Ethereum`)
- Polygon (`ChainId.Polygon`)
- Arbitrum (`ChainId.Arbitrum`)
- Base (`ChainId.Base`)
- Unichain (`ChainId.Unichain`)
- _More chains coming soon_

## API Overview

### IWalletClient

Get wallet-related data across DeFi protocols:

```csharp
// Get DeFi positions (LP, Staking, Lending, etc.)
Task<PositionsResponse> GetPositionsAsync(string address, ChainId chainId);

// Get fungible token portfolio with prices
Task<PortfolioResponse> GetPortfolioAsync(string address, ChainId chainId);

// Get positions across multiple chains
Task<Dictionary<ChainId, PositionsResponse>> GetMultiChainPositionsAsync(
    string address,
    IEnumerable<ChainId> chainIds);
```

### ITransactionClient

Get decoded transaction history:

```csharp
// Get transaction history with pagination
Task<TransactionHistoryResponse> GetHistoryAsync(
    string address,
    ChainId chainId,
    int limit = 50,
    string? cursor = null);

// Get detailed information about a specific transaction
Task<TransactionDetail> GetTransactionAsync(string txHash, ChainId chainId);
```

## Configuration

```csharp
var options = new NetZerionOptions
{
    BaseUrl = "https://api.zerion.io/v1",
    Timeout = TimeSpan.FromSeconds(30),
    MaxRetries = 3,
    RetryStrategy = RetryStrategy.ExponentialBackoff,
    RateLimits = new RateLimitOptions
    {
        RequestsPerDay = 3000,       // Zerion free tier limit
        RequestsPerMinute = 100,
        EnableAutoThrottling = true,
        ThrowOnRateLimit = false
    },
    EnableLogging = true
};

using var client = new NetZerionClient("your-api-key", options);
```

## Error Handling

```csharp
try
{
    var positions = await client.Wallet.GetPositionsAsync(address, ChainId.Ethereum);
}
catch (RateLimitException ex)
{
    // Rate limit hit - retry after {ex.RetryAfterSeconds} seconds
    Console.WriteLine($"Rate limited. Retry after {ex.RetryAfterSeconds}s");
}
catch (AuthenticationException ex)
{
    // Invalid API key
    Console.WriteLine("Invalid API key");
}
catch (ApiException ex)
{
    // Other API errors (4xx, 5xx)
    Console.WriteLine($"API error {ex.StatusCode}: {ex.Message}");
}
catch (NetZerionException ex)
{
    // Base exception for all NetZerion errors
    Console.WriteLine($"Error: {ex.Message}");
}
```

## Rate Limiting

Zerion API free tier provides **3,000 requests per day**. NetZerion includes built-in rate limiting:

- Tracks requests automatically
- Throws `RateLimitException` when limits are exceeded
- Supports auto-throttling to stay within limits
- Configurable per-day and per-minute limits

## Documentation

- [Getting Started Guide](docs/GETTING_STARTED.md)
- [API Reference](docs/API_REFERENCE.md)
- [Examples](docs/EXAMPLES.md)
- [Zerion API Documentation](docs/ZERION_API_DOCS.md)

## Development Status

**Current Version:** 1.0.0-preview.1

This is a preview release. The API is stable but may have minor changes before v1.0.0 final release.

### Implemented Features
- ✅ Wallet positions (DeFi protocols)
- ✅ Fungible portfolio (tokens with prices)
- ✅ Transaction history with decoding
- ✅ Multi-chain support
- ✅ Retry policies and rate limiting
- ✅ Dependency injection
- ✅ Comprehensive error handling

### Roadmap
- [ ] NFT endpoints
- [ ] WebSocket support for real-time updates
- [ ] Response caching
- [ ] Request batching
- [ ] Additional chain support

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run only unit tests
dotnet test --filter Category=Unit

# Run only integration tests (requires API key)
dotnet test --filter Category=Integration
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [Zerion](https://zerion.io/) for providing the excellent DeFi API
- Built for use in [TrackFi](https://github.com/trackfi/trackfi) - DeFi Portfolio Tracker

## Support

- 📖 [Documentation](docs/)
- 🐛 [Issue Tracker](https://github.com/trackfi/NetZerion/issues)
- 💬 [Discussions](https://github.com/trackfi/NetZerion/discussions)

---

**⚠️ Note:** This library is not officially affiliated with Zerion. It's an independent open-source project.
