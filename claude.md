# TrackFi - Portfolio Tracking Platform

## Project Overview

**TrackFi** is a multi-chain cryptocurrency portfolio tracking platform built with .NET 9 and ASP.NET Core. It aggregates wallet balances, NFTs, and DeFi positions across 50+ blockchain networks, providing real-time pricing and verification through multiple data providers.

### Core Value Proposition
- **Anonymous & Authenticated Modes**: Track any wallet address without signup (anonymous) or create authenticated profiles
- **Multi-Chain Support**: 50+ EVM networks (Ethereum, Polygon, Base, Arbitrum, etc.)
- **DeFi Position Tracking**: Lending, staking, farming, liquidity pools, vaults, and yield positions
- **Provider Abstraction**: Swappable data providers (Zerion, Moralis) without business logic changes
- **Token Verification**: CoinMarketCap + whitelist system to filter scam tokens
- **Real-Time Pricing**: Alchemy-powered pricing with multi-layer caching (memory + Redis)

---

## Architecture

### Technology Stack
- **.NET 9** - Latest LTS with Native AOT support
- **ASP.NET Core 9** - Minimal APIs with OpenAPI (Swagger/Scalar/ReDoc)
- **.NET Aspire** - Distributed application orchestration
- **PostgreSQL** - Primary database (via Npgsql)
- **Redis** - Distributed caching layer
- **Entity Framework Core** - ORM with code-first migrations

### Project Structure (Clean Architecture)

```
TrackFi.Domain/           # Core business entities & interfaces
├── Entities/             # User, Wallet, Token, NetworkMetadata, etc.
├── Enums/                # BlockchainNetwork, Currency, AssetType, etc.
└── Interfaces/           # Repository contracts

TrackFi.Application/      # Application business logic
├── Portfolio/DTOs/       # Response models (TokenBalanceDto, MultiNetworkWalletDto)
└── Common/Behaviors/     # MediatR pipelines, validation

TrackFi.Infrastructure/   # External integrations & data access
├── Blockchain/           # AlchemyService, CoinMarketCapService, TokenVerificationService
├── DeFi/                 # ZerionService, MoralisService, DeFiPriceEnrichmentService
├── Portfolio/            # AnonymousPortfolioService, DeFiPortfolioService
├── Persistence/          # DbContext, Repositories, Migrations
│   ├── Repositories/     # VerifiedTokenRepository, NetworkMetadataRepository
│   └── Migrations/       # EF Core migrations
└── Caching/              # DistributedCacheService (Redis wrapper)

TrackFi.Api/              # HTTP API layer
├── Endpoints/            # Minimal API endpoint definitions
├── wwwroot/              # Static files (network logos)
└── DependencyInjection.cs

TrackFi.AppHost/          # .NET Aspire orchestration
└── Program.cs            # Service registration & configuration
```

---

## Key Design Patterns & Principles

### 1. **Provider Abstraction Pattern**
All external data sources implement interfaces, allowing easy swapping without changing business logic:
- `IDeFiDataProvider` → `ZerionService` | `MoralisService`
- `IAssetDataProvider` → `PlaceholderAssetDataProvider` (future: Alchemy, Moralis)
- `IPriceProvider` → `PlaceholderPriceProvider` (future: CoinGecko, CoinMarketCap)

**Configuration:** Set provider in `appsettings.json`:
```json
"DeFi": {
  "Provider": "Zerion",  // or "Moralis"
  "SupportedNetworks": ["Ethereum", "Polygon", "Base"]
}
```

### 2. **Layered Caching Strategy**
- **Memory Cache** (NetworkMetadataRepository): Static data, never expires
- **Memory Cache** (VerifiedTokenRepository): 15-min TTL, single-instance cache
- **Distributed Cache (Redis)**: Token balances (3 min), Prices (1 min), NFTs (3 min)

### 3. **Token Verification Workflow**
```
1. Fetch balances → AlchemyService.GetTokenBalancesAsync()
2. Classify tokens:
   - Verified: In VerifiedToken database → SHOW to user
   - Unlisted: In UnlistedToken database → HIDE from user (scam)
   - Unknown: Not in cache → Verify via CoinMarketCap
3. Auto-verify unknown tokens:
   - TokenVerificationService.VerifyTokensAsync()
   - Store results in VerifiedToken or UnlistedToken tables
4. Return only verified tokens to user
```

### 4. **Price Enrichment Separation**
DeFi positions use two-layer pricing:
1. **Provider prices** (Zerion/Moralis): Cached for 3 minutes
2. **Alchemy prices** (global pricing layer): Cached for 1 minute, enriched on-demand

**Why?** Allows consistent pricing across all position types while leveraging provider-aggregated data.

### 5. **Enum Storage as Strings**
All enums (`BlockchainNetwork`, `AssetType`, etc.) are stored as strings in PostgreSQL for:
- Readability in database queries
- Easy enum expansion without migrations
- API responses include human-readable network names

**Configuration:** `TrackFiDbContext.ConfigureConventions()` sets global string conversion.

---

## Critical Components

### 🔹 NetworkMetadataRepository (Singleton)
**Purpose:** Provides network logos, colors, explorer URLs for 50+ networks
**Location:** `TrackFi.Infrastructure/Persistence/Repositories/NetworkMetadataRepository.cs`
**Caching:** In-memory, never expires (static reference data)
**Usage:**
```csharp
var metadata = await _networkMetadataRepository.GetByNetworkAsync(BlockchainNetwork.Ethereum);
var logoUrl = metadata?.LogoUrl; // "/images/networks/Ethereum.svg"
```

### 🔹 VerifiedTokenRepository (Scoped)
**Purpose:** Fast token verification lookups with memory + Redis caching
**Location:** `TrackFi.Infrastructure/Persistence/Repositories/VerifiedTokenRepository.cs`
**Caching:** Memory (15 min) + Redis (15 min), per-network dictionaries
**Pattern:** Double-check locking with `SemaphoreSlim` for thread-safety

### 🔹 AnonymousPortfolioService
**Purpose:** Fetch wallet balances, NFTs, and aggregate across networks
**Location:** `TrackFi.Infrastructure/Portfolio/AnonymousPortfolioService.cs`
**Key Methods:**
- `GetMultiNetworkWalletAsync()` - Parallel fetching across 5+ networks
- `GetTokenBalancesAsync()` - Single network with verification + pricing
- `GetMultiNetworkNftsAsync()` - NFTs from 8 major networks

### 🔹 DeFiPortfolioService
**Purpose:** Transform provider-aggregated DeFi positions into category-based DTOs
**Location:** `TrackFi.Infrastructure/Portfolio/DeFiPortfolioService.cs`
**Categories:** Farming, Lending, LiquidityPools, Staking, Yield, Rewards, Vaults
**Provider-Agnostic:** Works with any `IDeFiDataProvider` implementation

### 🔹 AlchemyService
**Purpose:** Blockchain data (balances, prices, metadata) via Alchemy API
**Location:** `TrackFi.Infrastructure/Blockchain/AlchemyService.cs`
**Key Features:**
- Multi-network batching (max 5 networks per request)
- Token metadata caching (via `TokenMetadataRepository`)
- Price batching with error tracking (`TokenPriceError`)

---

## Configuration Guide

### Required Secrets (User Secrets)
```bash
dotnet user-secrets set "Zerion:ApiKey" "your-zerion-key"
dotnet user-secrets set "Alchemy:ApiKey" "your-alchemy-key"
dotnet user-secrets set "CoinMarketCap:ApiKey" "your-cmc-key"
dotnet user-secrets set "Kestrel:Endpoints:Https:Certificate:Password" "cert-password"
```

### appsettings.Development.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=trackfi_dev;Username=postgres;Password=***",
    "Redis": "localhost:6379"
  },
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://api.followalpha-dev.com:443",
        "Certificate": {
          "Path": "C:\\path\\to\\certificate.pfx"
        }
      }
    }
  },
  "Cache": {
    "Enabled": true,
    "DefaultTtlMinutes": 15
  },
  "DeFi": {
    "Provider": "Zerion",
    "SupportedNetworks": ["Ethereum", "Polygon", "Arbitrum", "Base", "Optimism"]
  },
  "Alchemy": {
    "MultiNetworkPriorityNetworks": ["Ethereum", "Polygon", "Arbitrum", "Base", "Optimism"]
  }
}
```

### Running the Application
```bash
# Using Aspire (recommended)
cd TrackFi.AppHost
dotnet run

# Direct API (development)
cd TrackFi.Api
dotnet run

# Run tests
cd TrackFi.Tests
dotnet test
```

---

## Database Schema

### Key Tables

**Users**
- `Id`, `WalletAddress`, `PrimaryWalletNetwork`, `DisplayName`, `Bio`, `ProfilePictureUrl`, `CoverPictureUrl`

**UserWallets**
- `Id`, `UserId`, `WalletAddress`, `Network`, `Label`, `IsVerified`, `SignatureProof`

**VerifiedTokens** (Token whitelist)
- `Id`, `ContractAddress`, `Network`, `Symbol`, `Name`, `Decimals`, `LogoUrl`, `CoinGeckoId`, `Status`
- **Unique Index:** `(ContractAddress, Network)`

**UnlistedTokens** (Scam token blacklist)
- `Id`, `ContractAddress`, `Network`, `Reason`, `LastCheckedAt`
- **Unique Index:** `(ContractAddress, Network)`

**NetworkMetadata** (Static reference data)
- `Id`, `Network` (enum), `Name`, `LogoUrl`, `Color`, `ExplorerUrl`, `WebsiteUrl`
- **Unique Index:** `Network`
- **Seeded automatically** on first startup (50+ networks)

**TokenMetadata** (Encounter-based caching)
- `Id`, `ContractAddress`, `Network`, `Symbol`, `Name`, `Decimals`, `Logo`, `EncounterCount`
- **Purpose:** Cache token metadata from Alchemy to reduce API calls

---

## API Endpoints

### Portfolio (Anonymous)

**Balance Endpoints**
- `GET /api/portfolio/preview/balance?address={wallet}&network={network}`
  Returns tokens for single network (verified only)

- `GET /api/portfolio/preview/balance/all-chains?address={wallet}`
  Returns aggregated tokens across 5 priority networks

**NFT Endpoints**
- `GET /api/portfolio/preview/nfts?address={wallet}&network={network}`
  Returns NFTs for single network

- `GET /api/portfolio/preview/nfts/all-chains?address={wallet}`
  Returns aggregated NFTs from 8 major networks

### DeFi Positions

- `GET /api/defi/positions?address={wallet}&network={network}`
  Returns DeFi positions (farming, lending, staking, etc.) for single network

- `GET /api/defi/positions/all-chains?address={wallet}`
  Returns aggregated DeFi positions across configured networks

### Response Structure Example
```json
{
  "walletAddress": "0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb",
  "isAnonymous": true,
  "summary": {
    "totalValueUsd": 15234.50,
    "totalTokens": 12,
    "lastUpdated": "2024-01-15T10:30:00Z"
  },
  "networks": [
    {
      "network": "Ethereum",
      "networkLogoUrl": "/images/networks/Ethereum.svg",
      "totalValueUsd": 10500.25,
      "tokens": [
        {
          "contractAddress": "0xC02aaA39b223FE8D0A0e5C4F27eAD9083C756Cc2",
          "network": "Ethereum",
          "symbol": "WETH",
          "name": "Wrapped Ether",
          "balance": "1500000000000000000",
          "decimals": 18,
          "balanceFormatted": 1.5,
          "price": { "usd": 2300.50 },
          "valueUsd": 3450.75,
          "logoUrl": "https://assets.coingecko.com/coins/images/2518/small/weth.png"
        }
      ]
    }
  ]
}
```

---

## Common Development Tasks

### Adding a New Network
1. Add enum value to `BlockchainNetwork` (`TrackFi.Domain/Enums/BlockchainNetwork.cs`)
2. Download network logo SVG to `TrackFi.Api/wwwroot/images/networks/{NetworkName}.svg`
3. Add network metadata to `DbInitializer.SeedNetworkMetadataAsync()` (`TrackFi.Infrastructure/Persistence/DbInitializer.cs`)
4. Update Alchemy network mapping in `AlchemyService` if needed
5. Run migration: `dotnet ef migrations add AddNetworkX --project TrackFi.Infrastructure --startup-project TrackFi.Api`

### Adding a New DeFi Provider
1. Create provider class implementing `IDeFiDataProvider` in `TrackFi.Infrastructure/DeFi/`
2. Add provider enum value to `DeFiProvider` enum
3. Register in DI: `TrackFi.Api/DependencyInjection.cs` → `AddInfrastructureServices()`
4. Add configuration section to `appsettings.json`
5. Update factory method in `DependencyInjection.cs`

### Running Database Migrations
```bash
# Add migration
dotnet ef migrations add MigrationName --project TrackFi.Infrastructure --startup-project TrackFi.Api

# Apply migrations (development)
dotnet ef database update --project TrackFi.Infrastructure --startup-project TrackFi.Api

# Migrations run automatically on startup in development via DbInitializer
```

### Debugging Token Verification
1. Check `VerifiedTokens` table for existing whitelist
2. Check `UnlistedTokens` table for blacklisted tokens
3. Enable detailed logging: `"Logging": { "LogLevel": { "TrackFi": "Debug" } }`
4. Watch logs for "Token classification" and "CMC verification" messages
5. TokenVerificationService calls CoinMarketCap API - check quota limits

---

## Important Conventions

### Enum Storage
- **All enums stored as strings** in database (not integers)
- Configured globally in `TrackFiDbContext.ConfigureConventions()`
- Migration-friendly: Adding enum values doesn't require schema changes

### Error Handling
- Infrastructure failures propagate to `ExceptionHandlingMiddleware`
- Returns RFC 7807 ProblemDetails responses
- Example: `400 Bad Request`, `404 Not Found`, `500 Internal Server Error`

### Caching Keys
Format: `{prefix}:{param1}:{param2}`
```csharp
// Example from DistributedCacheService.GenerateKey()
"tokens_balances:ethereum:0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb"
"verified_tokens:polygon"
```

### Logging Patterns
```csharp
_logger.LogInformation("Operation completed: {Count} items processed", count);
_logger.LogWarning("Potential issue: {Message}", issue);
_logger.LogError(ex, "Operation failed: {Reason}", reason);
```

---

## Testing Strategy

### Test Projects
- **TrackFi.Tests** - Unit and integration tests
- Test files mirror source structure: `TrackFi.Tests/Infrastructure/Portfolio/AnonymousPortfolioServiceTests.cs`

### Key Test Patterns
```csharp
[Fact]
public async Task GetTokenBalances_ShouldReturnVerifiedTokensOnly()
{
    // Arrange: Setup mocks, test data
    // Act: Call service method
    // Assert: Verify expected behavior
}
```

### Integration Tests
- Use in-memory database for fast tests
- Mock external APIs (Alchemy, Zerion, CoinMarketCap)
- Test caching behavior with real Redis container (optional)

---

## Performance Considerations

### Bottlenecks & Solutions
1. **Token Metadata Fetching** - Solved with `TokenMetadataRepository` caching
2. **Price API Calls** - Batched requests + 1-min cache (Alchemy has generous limits)
3. **Multi-Network Queries** - Parallel fetching with `Task.WhenAll()`
4. **Verification Overhead** - In-memory cache + Redis for verified tokens

### Caching TTLs (Configurable)
- **Token Balances:** 3 minutes (blockchain state changes frequently)
- **Prices:** 1 minute (dynamic market data)
- **NFTs:** 3 minutes (change infrequently)
- **Verified Tokens:** 15 minutes (rarely changes after initial verification)
- **Network Metadata:** Never expires (static reference data)

---

## Known Issues & Workarounds

### Aspire Dashboard Localhost Display
**Issue:** Aspire dashboard shows `localhost:443` instead of custom domain `api.followalpha-dev.com`
**Status:** Known limitation in Aspire 9.x for local development
**Workaround:** None needed - functionality works, dashboard display is cosmetic
**Tracking:** Multiple GitHub issues on dotnet/aspire repository

### Port 443 Requires Admin Privileges
**Issue:** Binding to port 443 on Windows requires administrator mode
**Solution 1:** Run Visual Studio as Administrator
**Solution 2:** Reserve port with `netsh http add urlacl url=https://api.followalpha-dev.com:443/ user=Everyone`

### CoinMarketCap API Rate Limits
**Issue:** Free tier has 10,000 calls/month limit
**Solution:** Token verification caches results in database, only new tokens consume quota
**Monitoring:** Add logging for CMC API usage

---

## External Dependencies & APIs

### Alchemy
- **Purpose:** Blockchain data (balances, prices, metadata, NFTs)
- **Limits:** Generous free tier (300M compute units/month)
- **Docs:** https://docs.alchemy.com/reference/api-overview

### Zerion (via NetZerion package)
- **Purpose:** DeFi position aggregation
- **Limits:** 3,000 requests/day, 100 requests/minute
- **Docs:** https://developers.zerion.io/reference/
- **Package:** Custom NetZerion library in sibling folder

### Moralis (Alternative DeFi Provider)
- **Purpose:** DeFi positions, token balances
- **Limits:** 40,000 compute units/day (free tier)
- **Docs:** https://docs.moralis.io/web3-data-api

### CoinMarketCap
- **Purpose:** Token verification (identify legitimate tokens)
- **Limits:** 10,000 calls/month (free tier)
- **Docs:** https://coinmarketcap.com/api/documentation/v1/

---

## Deployment Considerations

### Environment Variables
Set these in production:
```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<postgres-connection-string>
ConnectionStrings__Redis=<redis-connection-string>
Zerion__ApiKey=<api-key>
Alchemy__ApiKey=<api-key>
CoinMarketCap__ApiKey=<api-key>
```

### Database Migrations
- Migrations run automatically on startup in Development
- In Production: Run migrations manually or via CI/CD pipeline
- Command: `dotnet ef database update --project TrackFi.Infrastructure --startup-project TrackFi.Api`

### Static Files
- Network logos in `wwwroot/images/networks/` must be deployed
- Configure reverse proxy (nginx/Caddy) to serve static files efficiently

### Caching Infrastructure
- **Redis required** for distributed caching in production
- Connection string format: `"host:port,password=yourpassword,ssl=true"`
- Consider Redis Cluster or Azure Cache for Redis for high availability

---

## Useful Commands

```bash
# Build entire solution
dotnet build

# Run API directly
dotnet run --project TrackFi.Api

# Run with Aspire orchestration
dotnet run --project TrackFi.AppHost

# Add package
dotnet add TrackFi.Infrastructure package PackageName

# Create migration
dotnet ef migrations add MigrationName --project TrackFi.Infrastructure --startup-project TrackFi.Api

# Apply migrations
dotnet ef database update --project TrackFi.Infrastructure --startup-project TrackFi.Api

# Run tests
dotnet test

# Watch mode (auto-reload on code changes)
dotnet watch --project TrackFi.Api

# Generate OpenAPI spec
curl https://api.followalpha-dev.com/openapi/v1.json > openapi.json
```

---

## Contact & Resources

- **GitHub:** (Add repository URL)
- **Documentation:** See markdown files in repository root
- **API Documentation:** `/swagger`, `/scalar`, or `/redoc` endpoints
- **.NET Aspire Docs:** https://learn.microsoft.com/en-us/dotnet/aspire/
- **Entity Framework Core:** https://learn.microsoft.com/en-us/ef/core/

---

## Future Roadmap

### Phase 1 (Completed)
✅ Multi-chain balance aggregation
✅ DeFi position tracking
✅ Token verification system
✅ Network metadata with logos
✅ Provider abstraction (Zerion/Moralis)

### Phase 2 (In Progress)
- Historical balance tracking
- Transaction history
- Portfolio analytics (ROI, P&L)
- Price alerts

### Phase 3 (Planned)
- User authentication & profiles
- Wallet connection (Web3 sign-in)
- Custom watchlists
- Portfolio sharing

---

**Last Updated:** 2025-01-12
**Version:** 1.0.0
**Target Framework:** .NET 9.0
