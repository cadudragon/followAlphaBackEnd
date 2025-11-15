# TrackFi - Software Architecture Specification

**Version:** 1.0  
**Last Updated:** October 12, 2025  
**Author:** Architecture Team  
**Status:** Official - Production Ready

---

## Table of Contents

1. [Overview](#1-overview)
2. [Guiding Principles](#2-guiding-principles)
3. [Architectural Pattern](#3-architectural-pattern-clean-architecture--net-aspire)
4. [Solution Structure](#4-solution-structure)
5. [Technology Stack](#5-technology-stack)
6. [Data Strategy](#6-data-strategy)
7. [Authentication & Wallet Connection](#7-authentication--wallet-connection)
8. [Cross-Cutting Concerns](#8-cross-cutting-concerns-aspire-enhanced)
9. [Data Flow Examples](#9-data-flow-examples)
10. [API Design](#10-api-design)
11. [Testing Strategy](#11-testing-strategy)
12. [Configuration Management](#12-configuration-management)
13. [Development Workflow](#13-development-workflow)
14. [Deployment](#14-deployment)
15. [Aspire Benefits Summary](#15-aspire-benefits-summary)
16. [Common Patterns](#16-common-patterns)
17. [Key Architecture Benefits](#17-key-architecture-benefits)
18. [Future Enhancements](#18-future-enhancements)
19. [Success Metrics](#19-success-metrics)
20. [Key Takeaways](#20-key-takeaways)
21. [Resources](#21-resources)
22. [Quick Reference](#quick-reference)

---

## 1. Overview

### 1.1. Project Goal

Build the market's most user-friendly and visually appealing Web3 portfolio tracker with social features. TrackFi provides a unified view of digital assets across EVM-compatible chains (Ethereum, Polygon, Arbitrum) and Solana, with a user experience inspired by modern social media platforms.

**Core Value Proposition:**
- **View any wallet** - No login required for basic portfolio viewing
- **Connect your wallet** - Verify ownership to unlock social features
- **Track interesting wallets** - Build your personal watchlist of notable addresses
- **Show your personality** - Set NFT or custom cover pictures for your profile
- **Build reputation** - Establish wallet credibility for future onchain features

### 1.2. Document Purpose

This document defines the software architecture for TrackFi. It serves as:
- The technical foundation for development
- A reference guide for onboarding new developers
- A specification for AI-assisted development
- A living document that evolves with the project

**Target Audience:** Developers, AI assistants, technical stakeholders

### 1.3. Social-First Evolution Strategy

**V1 (Current MVP - Weeks 1-6):**
- Anonymous portfolio viewing (no login required)
- Optional wallet connection for verified features
- User profiles with cover pictures
- Personal wallet watchlist
- Foundation for social features

**V2 (Social Features - Weeks 7-12):**
- Webhook-driven real-time updates
- Following other wallets
- Portfolio sharing and showcasing
- Reputation system
- Notifications

**V3 (Onchain Integration - Future):**
- Onchain reputation via wallet signatures
- NFT badges for milestones
- Social trading features
- Community-driven insights

### 1.4. Why .NET Aspire?

**.NET Aspire** is an opinionated, cloud-ready stack for building observable, production-ready distributed applications. For solo developers, it's a massive productivity boost:

**Built-In Benefits:**
- Zero-Config Telemetry: OpenTelemetry built-in (logs, metrics, traces)
- Local Orchestration: Run all services with one click
- Development Dashboard: Real-time monitoring of all services
- Service Discovery: Automatic service-to-service communication
- Resilience by Default: Retry, timeout, circuit breaker patterns included
- Service Defaults: Consistent logging, health checks across all services
- Easy Integrations: Redis, PostgreSQL, etc. with one line of code

**Time Saved:**
- No manual OpenTelemetry setup (2-3 days)
- No custom health check infrastructure (1 day)
- No resilience policy boilerplate (1-2 days)
- No local orchestration scripts (1 day)
- **Total: ~1 week of development time saved**

---

## 2. Guiding Principles

### 2.1. Core Values

**Speed of Development**
- Leverage .NET Aspire's conventions and defaults
- Minimize boilerplate and ceremony
- Use proven libraries over custom implementations
- Optimize for MVP delivery speed

**Social-First Design**
- Every feature should enable connection and community
- Optional authentication (don't block anonymous users)
- Wallet reputation is core to future features
- Profile customization builds engagement

**Flexibility & Extensibility**
- Adding new blockchains should require minimal core changes
- New features should integrate seamlessly
- Configuration-driven where possible
- Aspire service defaults apply everywhere

**Observability First**
- Built-in telemetry from day one
- Distributed tracing across all operations
- Real-time metrics and dashboards
- Production-ready monitoring from MVP

**Testability**
- Core business logic fully unit-testable
- No external dependencies in unit tests
- Aspire test containers for integration tests
- Clear test boundaries at each layer

**Maintainability**
- Clear separation of concerns
- Self-documenting code
- Consistent naming conventions
- Service defaults ensure consistency

### 2.2. Pragmatic Approach

This is a **solo developer project** focused on speed to market. We explicitly avoid:
- Full Domain-Driven Design (too complex)
- Over-abstraction and premature optimization
- Enterprise patterns without clear value
- Custom infrastructure (Aspire handles it)

---

## 3. Architectural Pattern: Clean Architecture + .NET Aspire

### 3.1. Pattern Choice

We combine **Clean Architecture** (Hexagonal/Onion) with **.NET Aspire** for cloud-native capabilities.

### 3.2. The Dependency Rule

**Critical:** Dependencies only point inward. Aspire components sit outside all layers.

```
┌─────────────────────────────────────────────────────┐
│           TrackFi.AppHost                           │
│        (Aspire Orchestration)                       │
│  - Service discovery                                │
│  - Resource management (Redis, PostgreSQL)          │
│  - Development dashboard                            │
└──────────────────┬──────────────────────────────────┘
                   │ orchestrates
┌──────────────────▼──────────────────────────────────┐
│         TrackFi.ServiceDefaults                     │
│  (Shared Aspire Configuration)                      │
│  - OpenTelemetry                                    │
│  - Health checks                                    │
│  - Service discovery                                │
│  - Resilience policies                              │
└─────────────────────────────────────────────────────┘
                   │ used by ↓
┌─────────────────────────────────────────┐
│         TrackFi.Api                     │  ← Presentation
│    (Controllers, Middleware, Auth)      │
└──────────────┬──────────────────────────┘
               │ depends on
┌──────────────▼──────────────────────────┐
│      TrackFi.Application                │  ← Application
│   (Use Cases, CQRS Handlers)            │
└──────────────┬──────────────────────────┘
               │ depends on
┌──────────────▼──────────────────────────┐
│        TrackFi.Domain                   │  ← Domain
│   (Entities, Interfaces, Rules)         │  ← No Dependencies!
└─────────────────────────────────────────┘
               ▲
               │ implements
┌──────────────┴──────────────────────────┐
│     TrackFi.Infrastructure              │  ← Infrastructure
│  (External APIs, Persistence, Web3)     │
└─────────────────────────────────────────┘
```

---

## 4. Solution Structure

### 4.1. `TrackFi.AppHost`

**Aspire Orchestration Project**

**Responsibility:** Defines and orchestrates all application resources and services.

**Program.cs**
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add Redis for caching
var cache = builder.AddRedis("cache");

// Add PostgreSQL for user data
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()  // Admin UI for development
    .AddDatabase("trackfidb");

// Add the API project
var api = builder.AddProject<Projects.TrackFi_Api>("api")
    .WithReference(cache)
    .WithReference(postgres);

builder.Build().Run();
```

**Key Features:**
- One-click F5 to run everything
- Automatic service discovery
- Built-in dashboard at http://localhost:15888
- PgAdmin available at http://localhost:5050 (development)

**Dependencies:** References all application projects

---

### 4.2. `TrackFi.ServiceDefaults`

**Aspire Service Defaults Project**

**Responsibility:** Shared configuration that every service inherits.

**Extensions.cs**
```csharp
public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(
        this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();
        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });
        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(
        this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddRuntimeInstrumentation()
                       .AddBuiltInMeters();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();
        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(
        this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(
            builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }
        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(
        this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), 
                tags: new[] { "live" });
        return builder;
    }

    private static MeterProviderBuilder AddBuiltInMeters(
        this MeterProviderBuilder meterProviderBuilder)
    {
        return meterProviderBuilder
            .AddMeter("Microsoft.AspNetCore.Hosting")
            .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
            .AddMeter("System.Net.Http");
    }
}
```

**What This Provides:**
- Consistent telemetry across all services
- Automatic distributed tracing
- Standard health check endpoints
- Built-in resilience patterns
- Service discovery for HTTP clients

**Dependencies:** Aspire packages only

---

### 4.3. `TrackFi.Domain`

**Responsibility:** Pure business logic with zero external dependencies. Includes social features.

**Entities** (Core business objects)
```csharp
// Generic Asset Hierarchy
- Asset.cs                    // Abstract base for ALL asset types
  ├── Token.cs                // Fungible tokens (ERC-20, SPL)
  ├── Nft.cs                  // Non-fungible tokens
  └── DeFiPosition.cs         // Staking, lending, LP positions

// Account (generic container)
- Account.cs                  // Abstract base for all account types
  └── Wallet.cs               // Crypto wallet

// Portfolio
- Portfolio.cs                // Aggregated view of all assets
- AssetAllocation.cs          // Breakdown by type/category

// User/Social Features
- User.cs                     // User profile
  {
    Id: Guid
    PrimaryWalletAddress: WalletAddress
    PrimaryWalletNetwork: BlockchainNetwork
    CoverPictureUrl: string (optional)
    CoverNftContract: string (optional)
    CoverNftTokenId: string (optional)
    CoverNftNetwork: BlockchainNetwork (optional)
    CreatedAt: DateTime
    LastActiveAt: DateTime
  }

- UserWallet.cs               // User's verified wallet
  {
    Id: Guid
    UserId: Guid
    WalletAddress: WalletAddress
    Network: BlockchainNetwork
    IsVerified: bool
    SignatureProof: string
    SignatureMessage: string
    Label: string
    VerifiedAt: DateTime (optional)
    AddedAt: DateTime
  }

- WatchlistEntry.cs           // Wallet in user's watchlist
  {
    Id: Guid
    UserId: Guid
    WalletAddress: WalletAddress
    Network: BlockchainNetwork
    Label: string
    Notes: string
    AddedAt: DateTime
  }
```

**Value Objects** (Immutable concepts)
```csharp
- Money.cs                    // Amount + currency
- Quantity.cs                 // Generic quantity with decimals
- PriceInfo.cs                // Price with currency and timestamp

// Asset identifiers
- AssetIdentifier.cs          // Base class for any identifier
  ├── WalletAddress.cs        // Blockchain address
  ├── ContractAddress.cs      // Token contract address
  └── NftIdentifier.cs        // NFT contract + tokenId

// Metadata
- AssetMetadata.cs            // Logo, name, description
```

**Interfaces** (Generic contracts)
```csharp
// Asset data providers
- IAssetDataProvider.cs       // Fetch holdings from any source
- IPriceProvider.cs           // Fetch prices for any asset type

// Persistence (V1 - User data only)
- IUserRepository.cs          // User profiles
- IUserWalletRepository.cs    // User's verified wallets
- IWatchlistRepository.cs     // User's watchlist

// Persistence (V2 - Portfolio snapshots)
- IPortfolioRepository.cs     // Historical snapshots
```

**Enums & Constants**
```csharp
- AssetCategory.cs            // Crypto, Stock, Bond, Commodity
- AssetType.cs                // Token, NFT, DeFi, etc.
- BlockchainNetwork.cs        // Ethereum, Polygon, Arbitrum, Solana
- TokenStandard.cs            // ERC20, ERC721, ERC1155, SPL
- Currency.cs                 // USD, EUR, GBP, etc.
- TransactionType.cs          // Buy, Sell, Transfer, Stake
```

**Domain Services** (Generic business rules)
```csharp
- PortfolioValuationService.cs
- AssetAllocationCalculator.cs
- WalletSignatureValidator.cs     // Validates Web3 signatures
```

**Dependencies:** None

---

### 4.4. `TrackFi.Application`

**Responsibility:** Orchestrates business workflows. Includes user/social features.

**Features (CQRS with MediatR)**
```
Features/
├── Portfolio/
│   ├── GetPortfolio/
│   │   ├── GetPortfolioQuery.cs
│   │   ├── GetPortfolioQueryHandler.cs
│   │   └── PortfolioDto.cs
│   └── GetAssetAllocation/
│       ├── GetAssetAllocationQuery.cs
│       └── GetAssetAllocationHandler.cs
│
├── Users/
│   ├── ConnectWallet/
│   │   ├── ConnectWalletCommand.cs           // Sign-in with wallet
│   │   ├── ConnectWalletHandler.cs
│   │   └── WalletConnectionDto.cs
│   ├── GetUserProfile/
│   │   ├── GetUserProfileQuery.cs
│   │   └── GetUserProfileHandler.cs
│   ├── UpdateProfile/
│   │   ├── UpdateProfileCommand.cs           // Update cover picture
│   │   └── UpdateProfileHandler.cs
│   └── VerifyWalletOwnership/
│       ├── VerifyWalletCommand.cs            // Add verified wallet
│       └── VerifyWalletHandler.cs
│
├── Watchlist/
│   ├── AddToWatchlist/
│   │   ├── AddToWatchlistCommand.cs
│   │   └── AddToWatchlistHandler.cs
│   ├── RemoveFromWatchlist/
│   │   ├── RemoveFromWatchlistCommand.cs
│   │   └── RemoveFromWatchlistHandler.cs
│   └── GetWatchlist/
│       ├── GetWatchlistQuery.cs
│       └── GetWatchlistHandler.cs
│
└── Assets/
    ├── GetAssetDetails/
    │   ├── GetAssetDetailsQuery.cs
    │   └── GetAssetDetailsHandler.cs
    └── SearchAssets/
        ├── SearchAssetsQuery.cs
        └── SearchAssetsHandler.cs
```

**DTOs (Data Transfer Objects)**
```csharp
// Portfolio DTOs
- PortfolioDto.cs
- AssetDto.cs
- AccountDto.cs

// User/Social DTOs
- UserProfileDto.cs
  {
    UserId: Guid
    PrimaryWallet: string
    CoverPictureUrl: string
    CoverNft: NftDto (optional)
    VerifiedWallets: List<WalletDto>
    WatchlistCount: int
  }

- WalletDto.cs
  {
    Address: string
    Network: string
    Label: string
    IsVerified: bool
  }

- WatchlistEntryDto.cs
  {
    WalletAddress: string
    Network: string
    Label: string
    Notes: string
    AddedAt: DateTime
  }
```

**Services**
```csharp
- PortfolioAggregationService.cs
- NetWorthCalculator.cs
- AssetEnricher.cs
- WalletSignatureService.cs       // Validates SIWE/Solana signatures
- ImageUploadService.cs            // Handles cover picture uploads
```

**Dependencies:** `TrackFi.Domain`

---

### 4.5. `TrackFi.Infrastructure`

**Responsibility:** Implements all external-facing concerns. Includes Web3 authentication.

**Asset Data Providers**
```
Providers/
├── Crypto/
│   ├── Evm/
│   │   ├── AlchemyService.cs
│   │   ├── InfuraService.cs
│   │   └── EvmProviderBase.cs
│   ├── Solana/
│   │   ├── HeliusService.cs
│   │   └── SolscanService.cs
│   └── CryptoAssetProvider.cs
└── Common/
    ├── HttpClientFactory.cs
    └── RateLimiter.cs
```

**Price Providers**
```
Pricing/
├── CoinGeckoService.cs
├── CoinMarketCapService.cs
└── PriceAggregatorService.cs
```

**Persistence (EF Core Code First)**
```
Persistence/
├── TrackFiDbContext.cs              // EF Core context
├── Repositories/
│   ├── UserRepository.cs            // Implements IUserRepository
│   ├── UserWalletRepository.cs      // Implements IUserWalletRepository
│   └── WatchlistRepository.cs       // Implements IWatchlistRepository
└── Configurations/
    ├── UserConfiguration.cs         // EF entity configuration
    ├── UserWalletConfiguration.cs
    └── WatchlistEntryConfiguration.cs
```

**Web3 Authentication**
```
Web3/
├── SignatureValidators/
│   ├── ISignatureValidator.cs
│   ├── SiweSignatureValidator.cs    // Sign-In with Ethereum
│   └── SolanaSignatureValidator.cs  // Solana Wallet Adapter
└── WalletProviders/
    ├── MetaMaskProvider.cs
    ├── WalletConnectProvider.cs
    └── PhantomProvider.cs
```

**File Storage**
```
Storage/
├── IFileStorageService.cs
└── AzureBlobStorageService.cs       // For cover pictures
```

**Caching**
```
Caching/
├── DistributedCacheService.cs
└── CacheKeyGenerator.cs
```

**Dependencies:** `TrackFi.Application`, `TrackFi.Domain`

---

### 4.6. `TrackFi.Api`

**Responsibility:** HTTP entry point. Includes authentication middleware.

**Program.cs**
```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add Redis caching
builder.AddRedisDistributedCache("cache");

// Add PostgreSQL database
builder.AddNpgsqlDbContext<TrackFiDbContext>("trackfidb");

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
        };
    });

builder.Services.AddAuthorization();

// CORS (for frontend)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration["Frontend:Url"]!)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Controllers
builder.Services.AddControllers();

// MediatR
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(GetPortfolioQuery).Assembly));

// FluentValidation
builder.Services.AddValidatorsFromAssembly(
    typeof(GetPortfolioQueryValidator).Assembly);

// Infrastructure services
builder.Services.AddScoped<IAssetDataProvider, CryptoAssetProvider>();
builder.Services.AddScoped<IPriceProvider, PriceAggregatorService>();

// User/Social services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserWalletRepository, UserWalletRepository>();
builder.Services.AddScoped<IWatchlistRepository, WatchlistRepository>();
builder.Services.AddScoped<ISignatureValidator, SiweSignatureValidator>();
builder.Services.AddScoped<IFileStorageService, AzureBlobStorageService>();

// Configure HttpClients
builder.Services.AddHttpClient<AlchemyService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Blockchain:Alchemy:BaseUrl"]!);
});

builder.Services.AddHttpClient<CoinGeckoService>(client =>
{
    client.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Apply migrations automatically in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<TrackFiDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Map Aspire default endpoints
app.MapDefaultEndpoints();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

**Controllers**
```csharp
// Portfolio (public - no auth required)
- PortfolioController.cs
- AssetsController.cs

// User/Social (auth optional/required)
- AuthController.cs           // Wallet connection, JWT generation
- ProfileController.cs        // User profiles, cover pictures
- WatchlistController.cs      // Watchlist management
```

**Middleware**
```csharp
- ExceptionHandlingMiddleware.cs
- RequestLoggingMiddleware.cs
- WalletAuthenticationMiddleware.cs     // Validates wallet signatures
```

**Dependencies:** `TrackFi.Application`, `TrackFi.ServiceDefaults`

---

## 5. Technology Stack

| Category | Technology | Version | Purpose |
|----------|-----------|---------|---------|
| **Cloud Stack** | **.NET Aspire** | **9.0+** | **Orchestration, telemetry, service defaults** |
| **Runtime** | .NET | 9.0 | Modern, performant, cross-platform |
| **API Framework** | ASP.NET Core Web API | 9.0 | RESTful API implementation |
| **CQRS/Mediator** | MediatR | 12.x | Decoupled request handling |
| **Validation** | FluentValidation | 11.x | Input validation |
| **Caching** | Aspire Redis Component | Built-in | Distributed caching |
| **Database** | PostgreSQL + EF Core | 15+ / 9.0 | User data, watchlists |
| **Authentication** | JWT Bearer | Built-in | Stateless auth tokens |
| **Web3 Auth** | Nethereum + Solnet | Latest | Wallet signature verification |
| **Telemetry** | OpenTelemetry | Built-in | Logs, metrics, traces |
| **Resilience** | Standard Resilience Handler | Built-in | Retry, circuit breaker, timeout |
| **File Storage** | Azure Blob Storage | Latest | Cover pictures |
| **JSON** | System.Text.Json | Built-in | Serialization |
| **Testing** | xUnit + Aspire.Testing | 2.x | Unit and integration tests |
| **Mocking** | Moq | 4.x | Test doubles |
| **API Docs** | Swagger/OpenAPI | 6.x | API documentation |

**Aspire Components Used:**
- `Aspire.Hosting` - Orchestration
- `Aspire.Hosting.Redis` - Redis resource
- `Aspire.Hosting.PostgreSQL` - PostgreSQL resource
- `Aspire.StackExchange.Redis` - Redis client
- `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` - EF Core + PostgreSQL

**Additional Libraries:**
- **Nethereum** - Ethereum signature verification
- **Solnet** - Solana signature verification
- **Azure.Storage.Blobs** - Cover picture storage
- **System.IdentityModel.Tokens.Jwt** - JWT token generation/validation

---

## 6. Data Strategy

### 6.1. Overview: The Three-Tier Approach with Social Data

Our data strategy includes **two types of data:**
1. **Portfolio Data** (volatile, cached) - Tier 1 & 2
2. **User/Social Data** (persistent) - Tier 3

**Three-Tier Architecture:**
- **Tier 1 (Real-Time):** Portfolio data from blockchain APIs
- **Tier 2 (Cache):** Short-lived Redis cache for performance
- **Tier 3 (Database):** User profiles, verified wallets, watchlists

### 6.2. What's Different in V1?

**Traditional Approach:**
- No database in MVP
- No user accounts
- Only caching

**TrackFi V1 Approach:**
- PostgreSQL database (lightweight schema)
- User profiles with wallet connection
- Personal watchlists
- Profile customization
- Wallet verification
- Portfolio data still NOT in database (fetched on-demand)
- NO webhooks yet (V2)

### 6.3. Tier 1: Real-Time Layer

**What Lives Here:**
- Current token balances
- Current NFT holdings
- Current DeFi positions
- Recent transaction history

**Why Always Fetch:**
- **Volatility:** Changes with every blockchain transaction
- **Accuracy:** Users expect 100% current data
- **Source of Truth:** Blockchain is the authoritative source

**Implementation:**
```csharp
public class GetPortfolioQueryHandler
{
    public async Task<PortfolioDto> Handle(...)
    {
        // Step 1: Check cache (Tier 2)
        var cached = await _cache.GetAsync($"portfolio:{wallet}");
        if (cached != null) return cached;
        
        // Step 2: Fetch fresh from blockchain (Tier 1)
        var holdings = await _cryptoProvider.GetHoldingsAsync(wallet, ct);
        var prices = await _priceProvider.GetPricesAsync(holdings, ct);
        
        // Step 3: Aggregate and calculate
        var portfolio = _valuationService.CreatePortfolio(holdings, prices);
        
        // Step 4: Cache for next request (Tier 2)
        await _cache.SetAsync($"portfolio:{wallet}", portfolio, 
            TimeSpan.FromSeconds(30));
        
        return portfolio;
    }
}
```

**Cost Optimization:**
- Alchemy Free Tier: 300 compute units/second
- CoinGecko Free: 10-50 calls/minute
- Strategy: Cache aggressively to stay in free tiers

### 6.4. Tier 2: Caching Layer (Redis)

**Cache Strategy by Data Type:**

| Data Type | TTL | Rationale |
|-----------|-----|-----------|
| **Portfolio Result** | 30-60s | Balance between freshness & API costs |
| **Token Prices** | 30-90s | Prices don't need millisecond accuracy |
| **NFT Metadata** | 24 hours | Images, names, descriptions rarely change |
| **Token Metadata** | 24 hours | Contract address, symbol, decimals are static |
| **Transaction History** | 5 minutes | Recent history for quick re-views |

**Cache Key Patterns:**
```
portfolio:{walletAddress}:{hash}              # Full portfolio
price:{tokenSymbol}                           # Token price
nft:metadata:{contractAddress}:{tokenId}      # NFT metadata
token:metadata:{contractAddress}              # Token info
transactions:{walletAddress}:recent           # Last 20 transactions
```

**Implementation Example:**
```csharp
public class DistributedCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService> _logger;

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan expiration,
        CancellationToken ct = default)
    {
        // Try cache first
        var cached = await _cache.GetStringAsync(key, ct);
        if (cached != null)
        {
            _logger.LogInformation("Cache HIT: {Key}", key);
            return JsonSerializer.Deserialize<T>(cached);
        }

        _logger.LogInformation("Cache MISS: {Key}", key);
        
        // Fetch fresh data
        var value = await factory();
        
        // Store in cache
        var json = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, json, 
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            }, ct);

        return value;
    }
}
```

**Cache Invalidation:**
- **Time-based:** TTL handles automatic expiration
- **Manual Refresh:** User can click "Refresh" button to bypass cache
- **V2 Webhooks:** Invalidate cache when webhook received

**Target Metrics:**
- Cache hit rate: > 70% (indicates good TTL settings)
- API cost reduction: 60-80% through caching
- Response time: < 100ms for cache hits

### 6.5. Tier 3: Database Layer (EF Core Code First)

**What Gets Persisted:**

**User Entity**
```csharp
public class User
{
    public Guid Id { get; private set; }
    public string PrimaryWalletAddress { get; private set; }
    public string PrimaryWalletNetwork { get; private set; }
    public string? CoverPictureUrl { get; private set; }
    public string? CoverNftContract { get; private set; }
    public string? CoverNftTokenId { get; private set; }
    public string? CoverNftNetwork { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastActiveAt { get; private set; }
    
    // Navigation properties
    public ICollection<UserWallet> Wallets { get; private set; } = new List<UserWallet>();
    public ICollection<WatchlistEntry> Watchlist { get; private set; } = new List<WatchlistEntry>();
    
    // Constructor
    private User() { } // EF Core
    
    public User(string primaryWalletAddress, string network)
    {
        Id = Guid.NewGuid();
        PrimaryWalletAddress = primaryWalletAddress;
        PrimaryWalletNetwork = network;
        CreatedAt = DateTime.UtcNow;
        LastActiveAt = DateTime.UtcNow;
    }
    
    public void UpdateCoverPicture(string? url, string? nftContract, string? nftTokenId, string? nftNetwork)
    {
        CoverPictureUrl = url;
        CoverNftContract = nftContract;
        CoverNftTokenId = nftTokenId;
        CoverNftNetwork = nftNetwork;
    }
    
    public void UpdateLastActive()
    {
        LastActiveAt = DateTime.UtcNow;
    }
}
```

**UserWallet Entity**
```csharp
public class UserWallet
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string WalletAddress { get; private set; }
    public string Network { get; private set; }
    public string? Label { get; private set; }
    public bool IsVerified { get; private set; }
    public string? SignatureProof { get; private set; }
    public string? SignatureMessage { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    public DateTime AddedAt { get; private set; }
    
    // Navigation
    public User User { get; private set; } = null!;
    
    private UserWallet() { } // EF Core
    
    public UserWallet(Guid userId, string walletAddress, string network, string? label = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        WalletAddress = walletAddress;
        Network = network;
        Label = label;
        IsVerified = false;
        AddedAt = DateTime.UtcNow;
    }
    
    public void Verify(string signature, string message)
    {
        IsVerified = true;
        SignatureProof = signature;
        SignatureMessage = message;
        VerifiedAt = DateTime.UtcNow;
    }
}
```

**WatchlistEntry Entity**
```csharp
public class WatchlistEntry
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string WalletAddress { get; private set; }
    public string Network { get; private set; }
    public string? Label { get; private set; }
    public string? Notes { get; private set; }
    public DateTime AddedAt { get; private set; }
    
    // Navigation
    public User User { get; private set; } = null!;
    
    private WatchlistEntry() { } // EF Core
    
    public WatchlistEntry(Guid userId, string walletAddress, string network, string? label = null, string? notes = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        WalletAddress = walletAddress;
        Network = network;
        Label = label;
        Notes = notes;
        AddedAt = DateTime.UtcNow;
    }
    
    public void Update(string? label, string? notes)
    {
        Label = label;
        Notes = notes;
    }
}
```

**EF Core Configuration**

**UserConfiguration.cs**
```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.Id)
            .HasColumnName("id");
        
        builder.Property(u => u.PrimaryWalletAddress)
            .HasColumnName("primary_wallet_address")
            .HasMaxLength(255)
            .IsRequired();
        
        builder.Property(u => u.PrimaryWalletNetwork)
            .HasColumnName("primary_wallet_network")
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(u => u.CoverPictureUrl)
            .HasColumnName("cover_picture_url");
        
        builder.Property(u => u.CoverNftContract)
            .HasColumnName("cover_nft_contract")
            .HasMaxLength(255);
        
        builder.Property(u => u.CoverNftTokenId)
            .HasColumnName("cover_nft_token_id")
            .HasMaxLength(100);
        
        builder.Property(u => u.CoverNftNetwork)
            .HasColumnName("cover_nft_network")
            .HasMaxLength(50);
        
        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
        
        builder.Property(u => u.LastActiveAt)
            .HasColumnName("last_active_at")
            .IsRequired();
        
        // Indexes
        builder.HasIndex(u => u.PrimaryWalletAddress)
            .IsUnique()
            .HasDatabaseName("idx_users_wallet");
        
        // Relationships
        builder.HasMany(u => u.Wallets)
            .WithOne(w => w.User)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(u => u.Watchlist)
            .WithOne(w => w.User)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

**UserWalletConfiguration.cs**
```csharp
public class UserWalletConfiguration : IEntityTypeConfiguration<UserWallet>
{
    public void Configure(EntityTypeBuilder<UserWallet> builder)
    {
        builder.ToTable("user_wallets");
        
        builder.HasKey(w => w.Id);
        
        builder.Property(w => w.Id)
            .HasColumnName("id");
        
        builder.Property(w => w.UserId)
            .HasColumnName("user_id")
            .IsRequired();
        
        builder.Property(w => w.WalletAddress)
            .HasColumnName("wallet_address")
            .HasMaxLength(255)
            .IsRequired();
        
        builder.Property(w => w.Network)
            .HasColumnName("network")
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(w => w.Label)
            .HasColumnName("label")
            .HasMaxLength(100);
        
        builder.Property(w => w.IsVerified)
            .HasColumnName("is_verified")
            .HasDefaultValue(false);
        
        builder.Property(w => w.SignatureProof)
            .HasColumnName("signature_proof");
        
        builder.Property(w => w.SignatureMessage)
            .HasColumnName("signature_message");
        
        builder.Property(w => w.VerifiedAt)
            .HasColumnName("verified_at");
        
        builder.Property(w => w.AddedAt)
            .HasColumnName("added_at")
            .IsRequired();
        
        // Indexes
        builder.HasIndex(w => w.UserId)
            .HasDatabaseName("idx_user_wallets_user");
        
        builder.HasIndex(w => w.WalletAddress)
            .HasDatabaseName("idx_user_wallets_address");
        
        builder.HasIndex(w => new { w.UserId, w.WalletAddress, w.Network })
            .IsUnique()
            .HasDatabaseName("idx_user_wallets_unique");
    }
}
```

**WatchlistEntryConfiguration.cs**
```csharp
public class WatchlistEntryConfiguration : IEntityTypeConfiguration<WatchlistEntry>
{
    public void Configure(EntityTypeBuilder<WatchlistEntry> builder)
    {
        builder.ToTable("watchlist");
        
        builder.HasKey(w => w.Id);
        
        builder.Property(w => w.Id)
            .HasColumnName("id");
        
        builder.Property(w => w.UserId)
            .HasColumnName("user_id")
            .IsRequired();
        
        builder.Property(w => w.WalletAddress)
            .HasColumnName("wallet_address")
            .HasMaxLength(255)
            .IsRequired();
        
        builder.Property(w => w.Network)
            .HasColumnName("network")
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(w => w.Label)
            .HasColumnName("label")
            .HasMaxLength(100);
        
        builder.Property(w => w.Notes)
            .HasColumnName("notes");
        
        builder.Property(w => w.AddedAt)
            .HasColumnName("added_at")
            .IsRequired();
        
        // Indexes
        builder.HasIndex(w => w.UserId)
            .HasDatabaseName("idx_watchlist_user");
        
        builder.HasIndex(w => w.WalletAddress)
            .HasDatabaseName("idx_watchlist_address");
        
        builder.HasIndex(w => new { w.UserId, w.WalletAddress, w.Network })
            .IsUnique()
            .HasDatabaseName("idx_watchlist_unique");
    }
}
```

**TrackFiDbContext.cs**
```csharp
public class TrackFiDbContext : DbContext
{
    public TrackFiDbContext(DbContextOptions<TrackFiDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserWallet> UserWallets => Set<UserWallet>();
    public DbSet<WatchlistEntry> Watchlist => Set<WatchlistEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new UserWalletConfiguration());
        modelBuilder.ApplyConfiguration(new WatchlistEntryConfiguration());
    }
}
```

**What Gets Stored:**
- User profiles (primary wallet, cover picture)
- User's verified wallets (with signature proofs)
- User's personal watchlist
- Portfolio balances (still fetched on-demand)
- Transaction history (still fetched on-demand)
- Token prices (still cached in Redis)

**Why This Approach:**
- Minimal database writes (only user actions)
- Portfolio data stays fresh (fetched on-demand)
- Enables social features without bloat
- Database only for persistent user preferences
- EF Core Code First enables rapid development
- Migrations handle schema changes automatically

### 6.6. V1 Data Flow: Anonymous vs Authenticated

**Anonymous User (No Login):**
```
GET /api/v1/portfolio?accounts=0x123...
  ↓
Check Cache → Cache Miss?
  ↓
Fetch from APIs → Cache → Return

NO DATABASE INTERACTION
```

**Authenticated User (Wallet Connected):**
```
GET /api/v1/profile
  ↓
Authenticate JWT → Validate
  ↓
Query Database (users table)
  ↓
Return Profile (cover picture, verified wallets, watchlist count)

GET /api/v1/watchlist
  ↓
Authenticate JWT → Validate
  ↓
Query Database (watchlist table)
  ↓
For each wallet in watchlist:
  Check Cache → Fetch if needed
  ↓
Return Aggregated Watchlist View
```

### 6.7. V2: Webhook-Driven (Future)

V2 still adds:
- Portfolio snapshots in database
- Webhook-driven updates
- Real-time notifications
- Following other users

---

## 7. Authentication & Wallet Connection

### 7.1. Web3 Authentication Flow

**Sign-In with Ethereum (SIWE) / Solana Wallet Adapter**

```
┌─────────────────────────────────────────────────┐
│ 1. Frontend: User clicks "Connect Wallet"      │
└─────────────────────────────────────────────────┘
                     ↓
┌─────────────────────────────────────────────────┐
│ 2. Frontend: Wallet popup (MetaMask/Phantom)   │
│    User approves connection                     │
└─────────────────────────────────────────────────┘
                     ↓
┌─────────────────────────────────────────────────┐
│ 3. Frontend: Request signature                 │
│    Message: "Sign in to TrackFi                │
│              Nonce: {random}                    │
│              Timestamp: {now}"                  │
└─────────────────────────────────────────────────┘
                     ↓
┌─────────────────────────────────────────────────┐
│ 4. User signs message in wallet                │
│    Returns: signature                           │
└─────────────────────────────────────────────────┘
                     ↓
┌─────────────────────────────────────────────────┐
│ 5. Frontend sends to backend:                  │
│    POST /api/auth/connect                       │
│    {                                            │
│      walletAddress: "0x123...",                │
│      network: "ethereum",                       │
│      message: "Sign in to TrackFi...",         │
│      signature: "0xabc..."                      │
│    }                                            │
└─────────────────────────────────────────────────┘
                     ↓
┌─────────────────────────────────────────────────┐
│ 6. Backend: Validate signature                 │
│    - Recover signer from signature              │
│    - Verify it matches walletAddress            │
│    - Check nonce hasn't been used               │
└─────────────────────────────────────────────────┘
                     ↓
┌─────────────────────────────────────────────────┐
│ 7. Backend: Create or get user                 │
│    - Check if user exists (by wallet address)  │
│    - If not, create new user                    │
│    - Generate JWT token                         │
└─────────────────────────────────────────────────┘
                     ↓
┌─────────────────────────────────────────────────┐
│ 8. Backend returns:                             │
│    {                                            │
│      token: "eyJhbGc...",                       │
│      userId: "uuid",                            │
│      walletAddress: "0x123..."                  │
│    }                                            │
└─────────────────────────────────────────────────┘
                     ↓
┌─────────────────────────────────────────────────┐
│ 9. Frontend stores JWT in localStorage         │
│    All subsequent requests include:             │
│    Authorization: Bearer {token}                │
└─────────────────────────────────────────────────┘
```

### 7.2. Backend Implementation

**ConnectWalletHandler.cs**
```csharp
public class ConnectWalletHandler 
    : IRequestHandler<ConnectWalletCommand, WalletConnectionDto>
{
    private readonly ISignatureValidator _signatureValidator;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private readonly ILogger<ConnectWalletHandler> _logger;

    public async Task<WalletConnectionDto> Handle(
        ConnectWalletCommand request,
        CancellationToken ct)
    {
        using var activity = Activity.Current?.Source
            .StartActivity("ConnectWallet");
        
        // 1. Validate signature
        var isValid = await _signatureValidator.ValidateAsync(
            request.WalletAddress,
            request.Message,
            request.Signature,
            ct);
        
        if (!isValid)
        {
            throw new InvalidSignatureException(
                "Signature verification failed");
        }
        
        // 2. Check if user exists
        var user = await _userRepository
            .GetByWalletAddressAsync(request.WalletAddress, ct);
        
        if (user == null)
        {
            // Create new user
            user = new User(
                primaryWalletAddress: request.WalletAddress,
                network: request.Network
            );
            
            await _userRepository.AddAsync(user, ct);
            
            _logger.LogInformation(
                "New user created: {UserId} with wallet {Wallet}",
                user.Id, request.WalletAddress);
        }
        else
        {
            // Update last active
            user.UpdateLastActive();
            await _userRepository.UpdateAsync(user, ct);
        }
        
        // 3. Generate JWT token
        var token = _jwtGenerator.GenerateToken(
            userId: user.Id,
            walletAddress: request.WalletAddress,
            expiration: TimeSpan.FromDays(30)
        );
        
        return new WalletConnectionDto
        {
            Token = token,
            UserId = user.Id,
            WalletAddress = request.WalletAddress,
            IsNewUser = user.CreatedAt > DateTime.UtcNow.AddMinutes(-1)
        };
    }
}
```

**SiweSignatureValidator.cs** (Ethereum/EVM)
```csharp
public class SiweSignatureValidator : ISignatureValidator
{
    public async Task<bool> ValidateAsync(
        string walletAddress,
        string message,
        string signature,
        CancellationToken ct)
    {
        try
        {
            var signer = new EthereumMessageSigner();
            var recoveredAddress = signer.EncodeUTF8AndEcRecover(
                message, 
                signature);
            
            return recoveredAddress.Equals(
                walletAddress, 
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, 
                "Signature validation failed for {Wallet}", 
                walletAddress);
            return false;
        }
    }
}
```

**JwtTokenGenerator.cs**
```csharp
public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public string GenerateToken(
        Guid userId, 
        string walletAddress, 
        TimeSpan expiration)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("wallet_address", walletAddress),
            new Claim(ClaimTypes.Name, walletAddress),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!));
        
        var creds = new SigningCredentials(
            key, 
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.Add(expiration),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### 7.3. Protected Endpoints

**Authorization Requirements:**

| Endpoint | Auth Required | Description |
|----------|---------------|-------------|
| `GET /portfolio` | No | Anyone can view any portfolio |
| `GET /assets/{id}` | No | Public asset details |
| `POST /auth/connect` | No | Wallet connection (creates token) |
| `GET /profile` | Yes | Get authenticated user's profile |
| `PUT /profile/cover` | Yes | Update cover picture |
| `GET /watchlist` | Yes | Get user's watchlist |
| `POST /watchlist` | Yes | Add wallet to watchlist |
| `DELETE /watchlist/{id}` | Yes | Remove from watchlist |
| `POST /wallets/verify` | Yes | Add verified wallet to profile |

**Controller Example:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly ISender _sender;

    [HttpGet]
    [Authorize]  // JWT required
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userId = User.GetUserId();  // Extract from JWT claims
        
        var query = new GetUserProfileQuery(userId);
        var profile = await _sender.Send(query, ct);
        
        return Ok(profile);
    }

    [HttpPut("cover")]
    [Authorize]
    public async Task<IActionResult> UpdateCoverPicture(
        [FromBody] UpdateCoverPictureRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        
        var command = new UpdateCoverPictureCommand(
            userId,
            request.PictureUrl,
            request.NftContract,
            request.NftTokenId
        );
        
        await _sender.Send(command, ct);
        
        return Ok();
    }
}
```

### 7.4. Frontend Integration Example

**React/TypeScript Example:**

```typescript
// Web3 connection
import { ethers } from 'ethers';

async function connectWallet() {
  // 1. Request account access
  const provider = new ethers.BrowserProvider(window.ethereum);
  const accounts = await provider.send("eth_requestAccounts", []);
  const walletAddress = accounts[0];
  
  // 2. Create message to sign
  const nonce = generateNonce();
  const message = `Sign in to TrackFi\nNonce: ${nonce}\nTimestamp: ${Date.now()}`;
  
  // 3. Request signature
  const signer = await provider.getSigner();
  const signature = await signer.signMessage(message);
  
  // 4. Send to backend
  const response = await fetch('/api/auth/connect', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      walletAddress,
      network: 'ethereum',
      message,
      signature
    })
  });
  
  const data = await response.json();
  
  // 5. Store JWT token
  localStorage.setItem('trackfi_token', data.token);
  
  return data;
}

// Making authenticated requests
async function getProfile() {
  const token = localStorage.getItem('trackfi_token');
  
  const response = await fetch('/api/profile', {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  return response.json();
}
```

### 7.5. Security Considerations

**Token Security:**
- JWT expires after 30 days
- Stored in localStorage (httpOnly cookies better but more complex)
- Token includes userId and walletAddress claims
- Backend validates token on every protected request

**Signature Validation:**
- Message includes nonce to prevent replay attacks
- Message includes timestamp (check it's recent)
- Signature cryptographically proves wallet ownership
- No password storage needed

**Best Practices:**
- Always validate signatures on backend (NEVER trust frontend)
- Use HTTPS in production
- Implement rate limiting on auth endpoints
- Log all authentication attempts
- Consider implementing token refresh mechanism (V2)

### 7.6. Wallet Reputation Strategy

**Building Trust with Wallet Apps:**

1. **SIWE Standard Compliance**
   - Use standard Sign-In with Ethereum format
   - Compatible with all major wallets
   - Builds trust with MetaMask, Rainbow, etc.

2. **Future Onchain Integration**
   - Verified wallet ownership enables:
     - NFT minting for achievements
     - Onchain reputation scores
     - Social trading features
     - Wallet-based access control

3. **No Custody, Pure Signatures**
   - Never request private keys
   - Never request transaction signing (unless explicitly for feature)
   - Build reputation as secure, read-only analytics tool

---

## 8. Cross-Cutting Concerns (Aspire-Enhanced)

### 8.1. Observability (Built-In with Aspire)

**What Aspire Provides:**
- Structured logging with OpenTelemetry
- Distributed tracing across all services
- Real-time metrics and dashboards
- All visible at http://localhost:15888

**Custom Telemetry:**
```csharp
public class TrackFiMetrics
{
    private readonly Counter<long> _walletConnections;
    private readonly Counter<long> _watchlistAdds;
    private readonly Counter<long> _profileUpdates;

    public TrackFiMetrics(IMeterFactory factory)
    {
        var meter = factory.Create("TrackFi.Application");
        
        _walletConnections = meter.CreateCounter<long>(
            "wallet.connections.total");
        _watchlistAdds = meter.CreateCounter<long>(
            "watchlist.additions.total");
        _profileUpdates = meter.CreateCounter<long>(
            "profile.updates.total");
    }

    public void RecordWalletConnection() => _walletConnections.Add(1);
    public void RecordWatchlistAdd() => _watchlistAdds.Add(1);
    public void RecordProfileUpdate() => _profileUpdates.Add(1);
}
```

### 8.2. Error Handling

**Global Exception Handling:**
```csharp
public class ExceptionHandlingMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (InvalidSignatureException ex)
        {
            _logger.LogWarning(ex, "Invalid wallet signature");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    code = "INVALID_SIGNATURE",
                    message = "Wallet signature verification failed"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    code = "INTERNAL_ERROR",
                    message = "An unexpected error occurred"
                }
            });
        }
    }
}
```

### 8.3. Caching Strategy

See Section 6.4 for Redis caching details.

### 8.4. Resilience (Built-In with Aspire)

Aspire's `AddStandardResilienceHandler()` provides:
- Retry policy (3 attempts, exponential backoff)
- Circuit breaker
- Timeout handling

### 8.5. Health Checks

**Automatic Endpoints:**
- `/health` - Overall health
- `/alive` - Liveness probe
- `/ready` - Readiness probe

**Custom Health Checks:**
```csharp
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddCheck<AlchemyHealthCheck>("alchemy")
    .AddCheck<RedisHealthCheck>("redis")
    .AddNpgSql(
        builder.Configuration.GetConnectionString("trackfidb")!,
        name: "postgres");
```

### 8.6. Security

**V1 (With Authentication):**
- HTTPS only (enforce with HSTS)
- CORS configured for frontend origin
- JWT authentication for protected endpoints
- Wallet signature validation
- Rate limiting on auth endpoints
- SQL injection prevention (EF Core parameterized queries)
- Input validation on all endpoints

---

## 9. Data Flow Examples

### 9.1. V1: Anonymous Portfolio View (No Auth)

```
1. User Request
   GET /api/v1/portfolio?accounts=0x123...

2. NO AUTHENTICATION CHECK (public endpoint)

3. PortfolioController
   - Validates wallet addresses
   - Creates GetPortfolioQuery
   - Sends via MediatR

4. GetPortfolioQueryHandler
   TIER 2: Check Redis Cache
   - Key: portfolio:0x123:hash
   - Result: HIT or MISS
   
   If MISS:
   TIER 1: Fetch Fresh from Blockchain APIs
   - Alchemy API (tokens)
   - CoinGecko (prices)
   - Duration: ~425ms
   
   TIER 2: Store in Redis Cache
   - TTL: 30 seconds

5. Response
   - Return PortfolioDto
   - NO DATABASE ACCESS

Flow: API → Cache → Blockchain → Cache → Response
NO AUTH, NO DATABASE
```

### 9.2. V1: Wallet Connection Flow

```
1. User clicks "Connect Wallet" in frontend

2. Frontend: MetaMask popup
   - User approves connection
   - Gets wallet address: 0x123...

3. Frontend: Request signature
   - Message: "Sign in to TrackFi\nNonce: abc123\nTimestamp: 1234567890"
   - User signs message
   - Signature: 0xdef456...

4. Frontend sends to backend:
   POST /api/auth/connect
   {
     walletAddress: "0x123...",
     network: "ethereum",
     message: "Sign in to TrackFi...",
     signature: "0xdef..."
   }

5. Backend: ConnectWalletHandler
   
   a. Validate Signature
      - Use Nethereum to recover signer
      - Verify signer == walletAddress
      - Duration: ~50ms
   
   b. Check Database (TIER 3)
      - Query: users table by primary_wallet_address
      - Result: NOT FOUND (new user)
   
   c. Create User
      - EF Core: Add new User entity
      - Duration: ~15ms
   
   d. Generate JWT
      - Claims: userId, walletAddress
      - Expiration: 30 days
      - Sign with secret key
      - Duration: ~5ms

6. Backend returns:
   {
     token: "eyJhbGc...",
     userId: "uuid-123",
     walletAddress: "0x123...",
     isNewUser: true
   }

7. Frontend stores token
   - localStorage.setItem('trackfi_token', token)
   - All future requests include: Authorization: Bearer {token}

Total duration: ~70ms
User is now authenticated!
```

### 9.3. V1: Add Wallet to Watchlist

```
1. User Request (Authenticated)
   POST /api/watchlist
   Headers: Authorization: Bearer eyJhbGc...
   Body: {
     walletAddress: "0xvitalik...",
     network: "ethereum",
     label: "Vitalik Buterin",
     notes: "Ethereum founder - interesting to track"
   }

2. API Middleware
   - Extract JWT from header
   - Validate token signature
   - Extract userId from claims
   - Duration: ~10ms

3. AddToWatchlistHandler
   
   a. Check Duplicate (TIER 3)
      - EF Core: Query watchlist table
      - Result: NOT FOUND (OK to add)
   
   b. Insert to Database (TIER 3)
      - EF Core: Add new WatchlistEntry entity
      - SaveChanges
      - Duration: ~20ms
   
   c. Record Metric
      - watchlist_additions_total: +1

4. Response
   - 201 Created
   - Return WatchlistEntryDto

Total duration: ~30ms
Watchlist updated!
```

### 9.4. V1: Get Watchlist with Portfolio Data

```
1. User Request (Authenticated)
   GET /api/watchlist
   Headers: Authorization: Bearer eyJhbGc...

2. Authenticate & Extract userId

3. GetWatchlistHandler
   
   a. Query Database (TIER 3)
      - EF Core: Query watchlist table where UserId = userId
      - Include: eager load relationships
      - Result: 5 wallets in watchlist
      - Duration: ~15ms
   
   b. For Each Wallet in Watchlist:
      
      TIER 2: Check Redis Cache
      - portfolio:0xvitalik:hash → HIT (cached)
      - portfolio:0xwhale1:hash → MISS
      - portfolio:0xwhale2:hash → HIT
      - ...
      
      For cache misses:
      TIER 1: Fetch from Blockchain APIs
      - Parallel fetch for all misses
      - Duration: ~400ms (for 2 wallets)
   
   c. Aggregate Results
      - Combine watchlist data (labels, notes) from DB
      - Combine portfolio data from cache/API
      - Calculate totals across watchlist
      - Duration: ~50ms

4. Response
   - Return List<WatchlistEntryWithPortfolioDto>
   - Each entry includes:
     * Label and notes (from database)
     * Current portfolio value (from API/cache)
     * Asset count
     * Last updated timestamp

Total duration: ~465ms (with 2 cache misses)
Next request: ~80ms (all cached)
```

### 9.5. V1: Update Profile Cover Picture (NFT)

```
1. User Request (Authenticated)
   PUT /api/profile/cover
   Headers: Authorization: Bearer eyJhbGc...
   Body: {
     type: "nft",
     nftContract: "0xcryptopunks...",
     nftTokenId: "1234",
     network: "ethereum"
   }

2. Authenticate & Extract userId

3. UpdateCoverPictureHandler
   
   a. Verify User Owns NFT
      - Query API: Check 0x123... owns CryptoPunk #1234
      - Call AlchemyService.GetNftOwnershipAsync()
      - Duration: ~300ms
      - Result: VERIFIED (user owns it)
   
   b. Update Database (TIER 3)
      - EF Core: Load User entity
      - User.UpdateCoverPicture(...)
      - SaveChanges
      - Duration: ~20ms
   
   c. Record Metric
      - profile_updates_total: +1

4. Response
   - 200 OK
   - Return updated UserProfileDto

Total duration: ~320ms
Profile updated with NFT cover!
```

---

## 10. API Design

### 10.1. Endpoints (V1 MVP)

**Portfolio Endpoints (Public - No Auth)**

```
GET /api/v1/portfolio
Query params:
  - accounts: comma-separated wallet addresses
  - includeNfts (optional): boolean, default false
  - currency (optional): USD, EUR, GBP

Example:
GET /api/v1/portfolio?accounts=0x123...,solana-abc...

Response: 200 OK, PortfolioDto
```

**Authentication Endpoints (Public)**

```
POST /api/v1/auth/connect
Body: {
  "walletAddress": "0x123...",
  "network": "ethereum",
  "message": "Sign in to TrackFi...",
  "signature": "0xabc..."
}

Response: 200 OK
{
  "token": "eyJhbGc...",
  "userId": "uuid",
  "walletAddress": "0x123...",
  "isNewUser": true
}
```

```
POST /api/v1/auth/refresh
Headers: Authorization: Bearer {token}

Response: 200 OK
{
  "token": "new-token..."
}
```

**Profile Endpoints (Authenticated)**

```
GET /api/v1/profile
Headers: Authorization: Bearer {token}

Response: 200 OK, UserProfileDto
{
  "userId": "uuid",
  "primaryWallet": "0x123...",
  "network": "ethereum",
  "coverPictureUrl": "https://...",
  "coverNft": {
    "contract": "0xpunks...",
    "tokenId": "1234",
    "imageUrl": "https://..."
  },
  "verifiedWallets": [
    {
      "address": "0x123...",
      "network": "ethereum",
      "label": "Main Wallet",
      "isVerified": true
    }
  ],
  "watchlistCount": 5,
  "createdAt": "2025-10-12T10:00:00Z"
}
```

```
PUT /api/v1/profile/cover
Headers: Authorization: Bearer {token}
Body: {
  "type": "upload" | "nft",
  "pictureUrl": "https://..." (if type=upload),
  "nftContract": "0x..." (if type=nft),
  "nftTokenId": "123" (if type=nft),
  "network": "ethereum" (if type=nft)
}

Response: 200 OK
```

**Watchlist Endpoints (Authenticated)**

```
GET /api/v1/watchlist
Headers: Authorization: Bearer {token}

Response: 200 OK
[
  {
    "id": "uuid",
    "walletAddress": "0xvitalik...",
    "network": "ethereum",
    "label": "Vitalik Buterin",
    "notes": "Ethereum founder",
    "currentValue": { "amount": 500000000, "currency": "USD" },
    "assetCount": 234,
    "addedAt": "2025-10-12T10:00:00Z"
  }
]
```

```
POST /api/v1/watchlist
Headers: Authorization: Bearer {token}
Body: {
  "walletAddress": "0xvitalik...",
  "network": "ethereum",
  "label": "Vitalik Buterin",
  "notes": "Interesting to track"
}

Response: 201 Created
```

```
DELETE /api/v1/watchlist/{id}
Headers: Authorization: Bearer {token}

Response: 204 No Content
```

**Wallet Verification Endpoints (Authenticated)**

```
POST /api/v1/wallets/verify
Headers: Authorization: Bearer {token}
Body: {
  "walletAddress": "0xsecondary...",
  "network": "polygon",
  "message": "Verify ownership of 0xsecondary... for TrackFi",
  "signature": "0xdef..."
}

Response: 200 OK
{
  "verified": true,
  "wallet": {
    "address": "0xsecondary...",
    "network": "polygon",
    "label": null
  }
}
```

**Asset Endpoints (Public)**

```
GET /api/v1/assets/{identifier}
Query params:
  - network (optional): ethereum | polygon | solana

Response: 200 OK, AssetDto
```

```
GET /api/v1/assets/search
Query params:
  - query: search term
  - type (optional): token | nft | defi
  - network (optional)

Response: 200 OK, List<AssetSearchResultDto>
```

**Health Checks (Public)**

```
GET /health      - Overall health
GET /alive       - Liveness probe
GET /ready       - Readiness probe (checks Redis + PostgreSQL)
```

### 10.2. Response Format

**Success:**
```json
{
  "data": { /* actual data */ },
  "timestamp": "2025-10-12T10:30:00Z",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

**Error:**
```json
{
  "error": {
    "code": "INVALID_SIGNATURE",
    "message": "Wallet signature verification failed",
    "timestamp": "2025-10-12T10:30:00Z",
    "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
  }
}
```

### 10.3. Versioning

Use URL path versioning: `/api/v1/...`

---

## 11. Testing Strategy

### 11.1. Unit Tests

**Target:** Domain and Application layers

**Structure:**
```
TrackFi.Application.Tests/
├── Features/
│   ├── GetPortfolio/
│   │   └── GetPortfolioQueryHandlerTests.cs
│   ├── ConnectWallet/
│   │   └── ConnectWalletHandlerTests.cs
│   └── AddToWatchlist/
│       └── AddToWatchlistHandlerTests.cs
└── Services/
    ├── NetWorthCalculatorTests.cs
    └── WalletSignatureServiceTests.cs

TrackFi.Domain.Tests/
├── Entities/
│   ├── UserTests.cs
│   └── PortfolioTests.cs
└── ValueObjects/
    └── WalletAddressTests.cs
```

**Example Test:**
```csharp
public class ConnectWalletHandlerTests
{
    [Fact]
    public async Task Handle_ValidSignature_CreatesNewUser()
    {
        // Arrange
        var signatureValidator = new Mock<ISignatureValidator>();
        signatureValidator
            .Setup(x => x.ValidateAsync(It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(x => x.GetByWalletAddressAsync(It.IsAny<string>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User)null);
        
        var handler = new ConnectWalletHandler(
            signatureValidator.Object,
            userRepository.Object,
            Mock.Of<IJwtTokenGenerator>(),
            Mock.Of<ILogger<ConnectWalletHandler>>());
        
        var command = new ConnectWalletCommand(
            "0x123...",
            "ethereum",
            "Sign in to TrackFi",
            "0xabc...");
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.NotNull(result.Token);
        Assert.True(result.IsNewUser);
        userRepository.Verify(x => x.AddAsync(
            It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### 11.2. Integration Tests (Aspire Testing)

```csharp
public class AuthIntegrationTests : IClassFixture<AspireWebApplicationFactory>
{
    private readonly AspireWebApplicationFactory _factory;

    [Fact]
    public async Task ConnectWallet_ValidSignature_ReturnsToken()
    {
        // Aspire automatically:
        // - Starts Redis container
        // - Starts PostgreSQL container
        // - Runs migrations
        // - Configures service defaults
        
        var client = _factory.CreateClient();
        
        var response = await client.PostAsJsonAsync("/api/auth/connect", new
        {
            walletAddress = "0x123...",
            network = "ethereum",
            message = "Sign in to TrackFi",
            signature = "0xabc..."
        });
        
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content
            .ReadFromJsonAsync<WalletConnectionDto>();
            
        Assert.NotNull(result.Token);
        Assert.NotEmpty(result.UserId.ToString());
    }
}
```

---

## 12. Configuration Management

### 12.1. Settings Structure

**appsettings.json**
```json
{
  "Blockchain": {
    "Alchemy": {
      "BaseUrl": "https://eth-mainnet.g.alchemy.com/v2/"
    },
    "Helius": {
      "BaseUrl": "https://api.helius.xyz/v0"
    }
  },
  "Jwt": {
    "Issuer": "TrackFi",
    "Audience": "TrackFi-Users"
  },
  "Frontend": {
    "Url": "http://localhost:3000"
  },
  "Azure": {
    "BlobStorage": {
      "ContainerName": "cover-pictures"
    }
  }
}
```

**User Secrets (Development)**
```bash
dotnet user-secrets set "Blockchain:Alchemy:ApiKey" "your-key"
dotnet user-secrets set "Jwt:SecretKey" "super-secret-key-min-256-bits"
dotnet user-secrets set "Azure:BlobStorage:ConnectionString" "connection-string"
```

**Environment Variables (Production)**
```bash
Blockchain__Alchemy__ApiKey=xyz...
Jwt__SecretKey=...
ConnectionStrings__trackfidb=postgresql://...
ConnectionStrings__cache=redis-connection...
```

---

## 13. Development Workflow

### 13.1. Getting Started

**Initial Setup:**
```bash
# Create solution
dotnet new aspire-starter -n TrackFi

# Add additional projects
dotnet new classlib -n TrackFi.Domain
dotnet new classlib -n TrackFi.Application
dotnet new classlib -n TrackFi.Infrastructure

# Add test projects
dotnet new xunit -n TrackFi.Application.Tests

# Install packages
cd TrackFi.Infrastructure
dotnet add package Nethereum.Web3
dotnet add package Solnet.Wallet
dotnet add package Azure.Storage.Blobs

cd ../TrackFi.Api
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Swashbuckle.AspNetCore

# Run migrations
cd TrackFi.Api
dotnet ef migrations add InitialCreate
dotnet ef database update

# Run!
cd ../TrackFi.AppHost
dotnet run
```

### 13.2. Daily Development Flow

1. **Start Aspire AppHost** (F5)
   - PostgreSQL starts automatically
   - Redis starts automatically
   - API starts with all defaults
   - Dashboard opens at http://localhost:15888
   - PgAdmin opens at http://localhost:5050

2. **Develop Feature** (Inside-Out)
   - Domain: Entities, value objects
   - Application: Handlers, DTOs
   - Infrastructure: Repositories, services
   - API: Controllers, middleware

3. **Test**
   - Unit tests: `dotnet test`
   - Integration tests: Aspire handles containers
   - Manual: Swagger + Postman
   - Dashboard: Real-time traces

4. **Database Changes**
   ```bash
   dotnet ef migrations add [MigrationName]
   dotnet ef database update
   ```

---

## 14. Deployment

### 14.1. Local Development

```bash
cd TrackFi.AppHost && dotnet run
```

**Access:**
- Dashboard: http://localhost:15888
- API: https://localhost:7xxx
- Swagger: https://localhost:7xxx/swagger
- PgAdmin: http://localhost:5050

### 14.2. Production (Azure)

**Azure Developer CLI:**
```bash
azd init
azd up
```

**Provisions:**
- Azure Container Apps (API)
- Azure Cache for Redis
- Azure Database for PostgreSQL
- Azure Blob Storage (cover pictures)
- Application Insights
- All networking/security

---

## 15. Aspire Benefits Summary

**Time Saved:**
- OpenTelemetry: 2-3 days → 0 hours
- Database setup: 1 day → 5 minutes
- Redis setup: 0.5 day → 5 minutes
- **Total: ~10 days → 1 hour**

**Features Included:**
- Distributed tracing (production-ready)
- Structured logging
- Custom metrics
- Real-time dashboard
- Retry policies
- Circuit breakers
- Health checks
- Service discovery
- One-command deployment

---

## 16. Common Patterns

### 16.1. Adding a New Blockchain

**Example: Adding Base (Layer 2)**

1. Add enum: `BlockchainNetwork.Base`
2. Create service: `BaseService.cs`
3. Update provider routing
4. Register in DI
5. Done! Everything else automatic.

### 16.2. Adding Social Features (V2)

**Example: Following Other Users**

1. Add `Follower` entity to Domain
2. Create `FollowerConfiguration` for EF Core
3. Create `FollowerRepository` in Infrastructure
4. Add `FollowUser` command in Application
5. Add `/api/social/follow` endpoint in API
6. Database migration: `dotnet ef migrations add AddFollowers`

All generic abstractions still apply!

---

## 17. Key Architecture Benefits

### 17.1. Social-First Design

**Easy to Add Social Features:**
- User profiles (V1)
- Watchlists (V1)
- Following (V2 - 1 day)
- Comments (V2 - 1 day)
- Sharing (V2 - 1 day)

### 17.2. Wallet Reputation

**Foundation for Onchain Products:**
- Verified wallet ownership
- Signature-based authentication
- No custody, pure analytics
- Trust built with wallet apps

### 17.3. EF Core Code First Benefits

**Development Speed:**
- No manual SQL scripts
- Automatic schema generation
- Easy migrations
- Type-safe queries
- Relationships handled automatically
- Change tracking included

---

## 18. Future Enhancements

**V2 (Social Features):**
- Follow other wallets/users
- Portfolio sharing and showcasing
- Comments on portfolios
- Webhook-driven real-time updates
- Notifications

**V3 (Onchain Integration):**
- NFT badges for milestones
- Onchain reputation scores
- Social trading features
- Portfolio challenges

---

## 19. Success Metrics

**Development:**
- Wallet connection flow: < 3 days
- Watchlist feature: < 2 days
- Profile customization: < 2 days

**User Engagement:**
- Wallet connection rate: > 30%
- Average watchlist size: > 3 wallets
- Profile customization rate: > 50%

**Performance:**
- Auth endpoint: < 100ms
- Watchlist load: < 500ms
- Portfolio view: < 500ms (V1)

---

## 20. Key Takeaways

**TrackFi V1 = Social-Ready Foundation**

- Optional authentication (don't block anonymous users)
- Wallet verification builds trust
- Personal watchlists drive engagement
- Profile customization builds identity
- Foundation for social features
- Reputation with wallet apps

**Data Strategy:**
- Portfolio data: Always fresh (API + Cache)
- User data: Persistent (PostgreSQL)
- No portfolio in database (V1)
- Webhooks for real-time (V2)

**Development Approach:**
- EF Core Code First for speed
- Aspire for infrastructure
- Clean Architecture for maintainability
- Social-first for engagement

---

## 21. Resources

**Official:**
- [.NET Aspire Docs](https://learn.microsoft.com/dotnet/aspire/)
- [EF Core Docs](https://learn.microsoft.com/ef/core/)
- [Nethereum Docs](https://docs.nethereum.com/)
- [SIWE Specification](https://eips.ethereum.org/EIPS/eip-4361)

**Community:**
- [GitHub](https://github.com/dotnet/aspire)
- [Discord](https://discord.gg/dotnet)

---

## Quick Reference

### Project Structure
```
TrackFi/
├── TrackFi.AppHost/           # Aspire orchestration
├── TrackFi.ServiceDefaults/   # Shared Aspire config
├── TrackFi.Domain/            # Entities (User, Wallet, Portfolio)
├── TrackFi.Application/       # Handlers (Auth, Watchlist, Portfolio)
├── TrackFi.Infrastructure/    # Repos, Web3, Blockchain APIs
└── TrackFi.Api/               # Controllers, Auth middleware
```

### EF Core Entities
```
User (Id, PrimaryWalletAddress, CoverPictureUrl, CoverNft*)
UserWallet (Id, UserId, WalletAddress, SignatureProof, IsVerified)
WatchlistEntry (Id, UserId, WalletAddress, Label, Notes)
```

### Data Flow
```
Anonymous: API → Cache → Blockchain → Response
Authenticated: JWT → Database → API → Cache → Blockchain → Response
```

### Key Commands
```bash
# Run everything
cd TrackFi.AppHost && dotnet run

# Migrations
dotnet ef migrations add [Name]
dotnet ef database update

# Test
dotnet test

# Deploy
azd up
```

### URLs
- Dashboard: http://localhost:15888
- API: https://localhost:7xxx
- Swagger: https://localhost:7xxx/swagger
- PgAdmin: http://localhost:5050

### Auth Flow
```
1. Frontend: Request signature from wallet
2. Backend: POST /auth/connect with signature
3. Backend: Validate → Create/Get User → Generate JWT
4. Frontend: Store JWT in localStorage
5. Future requests: Authorization: Bearer {token}
```

---

## Document History

| Version | Date | Changes |
|---------|------|--------|
| 1.0 | 2025-10-12 | Official V1 release. Includes wallet connection, authentication, watchlist, profile features with EF Core Code First approach. |

---

**END OF DOCUMENT**

TrackFi V1 - Ready for Production 🚀