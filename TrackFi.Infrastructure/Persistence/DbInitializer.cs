using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;

namespace TrackFi.Infrastructure.Persistence;

/// <summary>
/// Database initializer for seeding development data.
/// Should ONLY be called in Development environment.
/// </summary>
public class DbInitializer(TrackFiDbContext context, ILogger<DbInitializer> logger)
{
    private readonly TrackFiDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly ILogger<DbInitializer> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Seeds the database with sample data for development.
    /// WARNING: Only call this in Development environment!
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            // Ensure database is created
            await _context.Database.EnsureCreatedAsync();

            // Always seed network metadata (reference data, not user data)
            await SeedNetworkMetadataAsync();

            // Check if data already exists
            if (await _context.Users.AnyAsync())
            {
                _logger.LogInformation("Database already contains data. Skipping seed.");
                return;
            }

            _logger.LogInformation("Starting database seeding...");

            // Create sample users
            var user1 = new User("0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb", BlockchainNetwork.Ethereum);
            var user2 = new User("0xd8dA6BF26964aF9D7eEd9e03E53415D37aA96045", BlockchainNetwork.Polygon);
            // TODO: Re-enable when adding Solana support (Solana moved to NonEvmBlockchainNetwork enum)
            // var user3 = new User("DYw8jCTfwHNRJhhmFcbXvVDTqWMEVFBX6ZKUmG5CNSKK", NonEvmBlockchainNetwork.Solana);
            var user3 = new User("0x3fC91A3afd70395Cd496C647d5a6CC9D4B2b7FAD", BlockchainNetwork.Base); // Changed to Base for now
            var user4 = new User("0x5aAeb6053F3E94C9b9A09f33669435E7Ef1BeAed", BlockchainNetwork.Arbitrum);

            // Set cover pictures for some users
            user1.UpdateCoverPicture("https://example.com/covers/vitalik.jpg", null, null, null);
            user2.UpdateCoverPicture(null, "0xBC4CA0EdA7647A8aB7C2061c2E118A18a936f13D", "1234", BlockchainNetwork.Ethereum);

            await _context.Users.AddRangeAsync(user1, user2, user3, user4);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created {Count} sample users", 4);

            // Add additional wallets to users
            var wallet1 = new UserWallet(user1.Id, "0x123abc...def", BlockchainNetwork.Polygon, "My Polygon Wallet");
            var wallet2 = new UserWallet(user1.Id, "0x456def...ghi", BlockchainNetwork.Arbitrum, "Arbitrum Trading");
            var wallet3 = new UserWallet(user2.Id, "0x789ghi...jkl", BlockchainNetwork.Ethereum, "Main ETH Wallet");
            // TODO: Re-enable when adding Solana support
            // var wallet4 = new UserWallet(user3.Id, "9xQeWvG816bUx9EPjHmaT23yvVM2ZWbrrpZb9PusVFin", NonEvmBlockchainNetwork.Solana, "Secondary Solana");
            var wallet4 = new UserWallet(user3.Id, "0x4fD91A3afd70395Cd496C647d5a6CC9D4B2b7FAE", BlockchainNetwork.Optimism, "Secondary Optimism");

            // Verify some wallets
            wallet1.Verify("sample_signature_proof", "Sample message");
            wallet3.Verify("another_signature", "Another message");

            await _context.UserWallets.AddRangeAsync(wallet1, wallet2, wallet3, wallet4);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created {Count} sample user wallets", 4);

            // Add watchlist entries
            var watch1 = new WatchlistEntry(user1.Id, "0xd8dA6BF26964aF9D7eEd9e03E53415D37aA96045", BlockchainNetwork.Ethereum, "Vitalik Buterin", "Ethereum co-founder");
            var watch2 = new WatchlistEntry(user1.Id, "0xAb5801a7D398351b8bE11C439e05C5B3259aeC9B", BlockchainNetwork.Ethereum, "Binance Hot Wallet", "Exchange wallet");
            var watch3 = new WatchlistEntry(user2.Id, "0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb", BlockchainNetwork.Polygon, "Whale Tracker #1", "Large holder to monitor");
            // TODO: Re-enable when adding Solana support
            // var watch4 = new WatchlistEntry(user2.Id, "DYw8jCTfwHNRJhhmFcbXvVDTqWMEVFBX6ZKUmG5CNSKK", NonEvmBlockchainNetwork.Solana, "Solana Whale", "Active trader");
            var watch4 = new WatchlistEntry(user2.Id, "0x5fD91A3afd70395Cd496C647d5a6CC9D4B2b7FAF", BlockchainNetwork.Avalanche, "Avalanche Whale", "Active trader");
            var watch5 = new WatchlistEntry(user3.Id, "0x5aAeb6053F3E94C9b9A09f33669435E7Ef1BeAed", BlockchainNetwork.Ethereum, "DeFi Protocol", "Protocol treasury");
            var watch6 = new WatchlistEntry(user4.Id, "0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb", BlockchainNetwork.Arbitrum, "Arbitrum Whale", "Large ARB holder");

            await _context.Watchlist.AddRangeAsync(watch1, watch2, watch3, watch4, watch5, watch6);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created {Count} sample watchlist entries", 6);

            // Seed verified tokens for Base network (user's example)
            var verifiedTokens = new List<VerifiedToken>
            {
                // Native ETH on Base
                new VerifiedToken(
                    contractAddress: "0x0000000000000000000000000000000000000000",
                    network: BlockchainNetwork.Base,
                    symbol: "ETH",
                    name: "Ethereum",
                    decimals: 18,
                    logoUrl: "https://assets.coingecko.com/coins/images/279/small/ethereum.png",
                    coinGeckoId: "ethereum",
                    standard: TokenStandard.ERC20,
                    websiteUrl: "https://ethereum.org",
                    description: "Ethereum is a decentralized platform for applications.",
                    isNative: true,
                    verifiedBy: "system"),

                // Cake on Base
                new VerifiedToken(
                    contractAddress: "0x3055913c90Fcc1A6CE9a358911721eEb942013A1".ToLowerInvariant(),
                    network: BlockchainNetwork.Base,
                    symbol: "CAKE",
                    name: "PancakeSwap",
                    decimals: 18,
                    logoUrl: "https://assets.coingecko.com/coins/images/12632/small/pancakeswap-cake-logo.png",
                    coinGeckoId: "pancakeswap-token",
                    standard: TokenStandard.ERC20,
                    websiteUrl: "https://pancakeswap.finance",
                    description: "PancakeSwap is a decentralized exchange on BSC and other chains.",
                    isNative: false,
                    verifiedBy: "system"),

                // AERO on Base
                new VerifiedToken(
                    contractAddress: "0x940181a94A35A4569E4529A3CDfB74e38FD98631".ToLowerInvariant(),
                    network: BlockchainNetwork.Base,
                    symbol: "AERO",
                    name: "Aerodrome Finance",
                    decimals: 18,
                    logoUrl: null,
                    coinGeckoId: "aerodrome-finance",
                    standard: TokenStandard.ERC20,
                    websiteUrl: "https://aerodrome.finance",
                    description: "Aerodrome is a next-generation AMM designed for Base.",
                    isNative: false,
                    verifiedBy: "system"),

                // WETH on Base
                new VerifiedToken(
                    contractAddress: "0x4200000000000000000000000000000000000006".ToLowerInvariant(),
                    network: BlockchainNetwork.Base,
                    symbol: "WETH",
                    name: "Wrapped Ether",
                    decimals: 18,
                    logoUrl: "https://assets.coingecko.com/coins/images/2518/small/weth.png",
                    coinGeckoId: "weth",
                    standard: TokenStandard.ERC20,
                    websiteUrl: "https://weth.io",
                    description: "Wrapped Ether is the ERC20 version of ETH.",
                    isNative: false,
                    verifiedBy: "system"),

                // USDC on Base
                new VerifiedToken(
                    contractAddress: "0x833589fCD6eDb6E08f4c7C32D4f71b54bdA02913".ToLowerInvariant(),
                    network: BlockchainNetwork.Base,
                    symbol: "USDC",
                    name: "USD Coin",
                    decimals: 6,
                    logoUrl: "https://assets.coingecko.com/coins/images/6319/small/USD_Coin_icon.png",
                    coinGeckoId: "usd-coin",
                    standard: TokenStandard.ERC20,
                    websiteUrl: "https://www.circle.com/usdc",
                    description: "USDC is a fully reserved stablecoin pegged to the US Dollar.",
                    isNative: false,
                    verifiedBy: "system"),

                // cbBTC on Base
                new VerifiedToken(
                    contractAddress: "0xcbB7C0000aB88B473b1f5aFd9ef808440eed33Bf".ToLowerInvariant(),
                    network: BlockchainNetwork.Base,
                    symbol: "cbBTC",
                    name: "Coinbase Wrapped BTC",
                    decimals: 8,
                    logoUrl: null,
                    coinGeckoId: "coinbase-wrapped-btc",
                    standard: TokenStandard.ERC20,
                    websiteUrl: "https://www.coinbase.com",
                    description: "Coinbase Wrapped BTC is a Bitcoin-backed ERC20 token.",
                    isNative: false,
                    verifiedBy: "system")
            };

            await _context.VerifiedTokens.AddRangeAsync(verifiedTokens);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created {Count} verified tokens for Base network", verifiedTokens.Count);

            _logger.LogInformation("Database seeding completed successfully!");
            _logger.LogInformation("Sample data summary:");
            _logger.LogInformation("  - Users: 4 (Ethereum, Polygon, Solana, Arbitrum)");
            _logger.LogInformation("  - User Wallets: 4 (2 verified, 2 unverified)");
            _logger.LogInformation("  - Watchlist Entries: 6");
            _logger.LogInformation("  - Verified Tokens: {Count} (Base network)", verifiedTokens.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    /// <summary>
    /// Seeds network metadata (reference data) for all supported networks.
    /// This data is static and should be seeded regardless of environment.
    /// </summary>
    private async Task SeedNetworkMetadataAsync()
    {
        // Skip if network metadata already exists
        if (await _context.NetworkMetadata.AnyAsync())
        {
            _logger.LogDebug("Network metadata already seeded. Skipping.");
            return;
        }

        _logger.LogInformation("Seeding network metadata...");

        var networkMetadata = new List<NetworkMetadata>
        {
            // LAYER 1 CHAINS
            new() { Network = BlockchainNetwork.Ethereum, Name = "Ethereum", LogoUrl = "/images/networks/Ethereum.svg", Color = "#627EEA", ExplorerUrl = "https://etherscan.io" },
            new() { Network = BlockchainNetwork.BNBChain, Name = "BNB Smart Chain", LogoUrl = "/images/networks/BNB-Smart-Chain.svg", Color = "#F3BA2F", ExplorerUrl = "https://bscscan.com" },
            new() { Network = BlockchainNetwork.Avalanche, Name = "Avalanche", LogoUrl = "/images/networks/Avalanche.svg", Color = "#E84142", ExplorerUrl = "https://snowtrace.io" },
            new() { Network = BlockchainNetwork.Fantom, Name = "Fantom", LogoUrl = "/images/networks/Fantom.svg", Color = "#1969FF", ExplorerUrl = "https://ftmscan.com" },
            new() { Network = BlockchainNetwork.Polygon, Name = "Polygon", LogoUrl = "/images/networks/Polygon-PoS.svg", Color = "#8247E5", ExplorerUrl = "https://polygonscan.com" },
            new() { Network = BlockchainNetwork.Gnosis, Name = "Gnosis", LogoUrl = "/images/networks/Gnosis.svg", Color = "#04795B", ExplorerUrl = "https://gnosisscan.io" },
            new() { Network = BlockchainNetwork.Celo, Name = "Celo", LogoUrl = "/images/networks/Celo.svg", Color = "#FCFF52", ExplorerUrl = "https://celoscan.io" },
            new() { Network = BlockchainNetwork.Moonbeam, Name = "Moonbeam", LogoUrl = "/images/networks/Moonbeam.svg", Color = "#53CBC9", ExplorerUrl = "https://moonscan.io" },
            new() { Network = BlockchainNetwork.Moonriver, Name = "Moonriver", LogoUrl = "/images/networks/Moonriver.svg", Color = "#F2B705", ExplorerUrl = "https://moonriver.moonscan.io" },
            new() { Network = BlockchainNetwork.Astar, Name = "Astar", LogoUrl = "/images/networks/Astar.svg", Color = "#0AE2FF", ExplorerUrl = "https://astar.subscan.io" },

            // LAYER 2 CHAINS
            new() { Network = BlockchainNetwork.Arbitrum, Name = "Arbitrum", LogoUrl = "/images/networks/Arbitrum.svg", Color = "#28A0F0", ExplorerUrl = "https://arbiscan.io" },
            new() { Network = BlockchainNetwork.ArbitrumNova, Name = "Arbitrum Nova", LogoUrl = "/images/networks/Arbitrum-Nova.svg", Color = "#E8663D", ExplorerUrl = "https://nova.arbiscan.io" },
            new() { Network = BlockchainNetwork.Optimism, Name = "Optimism", LogoUrl = "/images/networks/Ethereum.svg", Color = "#FF0420", ExplorerUrl = "https://optimistic.etherscan.io" }, // Using Ethereum logo as placeholder
            new() { Network = BlockchainNetwork.Base, Name = "Base", LogoUrl = "/images/networks/Base.svg", Color = "#0052FF", ExplorerUrl = "https://basescan.org" },
            new() { Network = BlockchainNetwork.PolygonZkEVM, Name = "Polygon zkEVM", LogoUrl = "/images/networks/Polygon-zkEVM.svg", Color = "#7B3FE4", ExplorerUrl = "https://zkevm.polygonscan.com" },
            new() { Network = BlockchainNetwork.ZkSync, Name = "zkSync Era", LogoUrl = "/images/networks/Ethereum.svg", Color = "#8C8DFC", ExplorerUrl = "https://explorer.zksync.io" }, // Placeholder logo
            new() { Network = BlockchainNetwork.Linea, Name = "Linea", LogoUrl = "/images/networks/Linea.svg", Color = "#121212", ExplorerUrl = "https://lineascan.build" },
            new() { Network = BlockchainNetwork.Scroll, Name = "Scroll", LogoUrl = "/images/networks/Scroll.svg", Color = "#EFDAA0", ExplorerUrl = "https://scrollscan.com" },
            new() { Network = BlockchainNetwork.Mantle, Name = "Mantle", LogoUrl = "/images/networks/Mantle.svg", Color = "#000000", ExplorerUrl = "https://explorer.mantle.xyz" },
            new() { Network = BlockchainNetwork.Blast, Name = "Blast", LogoUrl = "/images/networks/Blast.svg", Color = "#FCFC03", ExplorerUrl = "https://blastscan.io" },
            new() { Network = BlockchainNetwork.Metis, Name = "Metis", LogoUrl = "/images/networks/Metis.svg", Color = "#00DACC", ExplorerUrl = "https://andromeda-explorer.metis.io" },
            new() { Network = BlockchainNetwork.Zora, Name = "Zora", LogoUrl = "/images/networks/Zora.svg", Color = "#000000", ExplorerUrl = "https://explorer.zora.energy" },
            new() { Network = BlockchainNetwork.Mode, Name = "Mode", LogoUrl = "/images/networks/Mode.svg", Color = "#DFFE00", ExplorerUrl = "https://explorer.mode.network" },
            new() { Network = BlockchainNetwork.Fraxtal, Name = "Fraxtal", LogoUrl = "/images/networks/Ethereum.svg", Color = "#000000", ExplorerUrl = "https://fraxscan.com" }, // Placeholder logo
            new() { Network = BlockchainNetwork.Unichain, Name = "Unichain", LogoUrl = "/images/networks/Unichain.svg", Color = "#FF007A", ExplorerUrl = "https://unichain.org" },

            // ADDITIONAL EVM CHAINS
            new() { Network = BlockchainNetwork.Harmony, Name = "Harmony", LogoUrl = "/images/networks/Ethereum.svg", Color = "#00AEE9", ExplorerUrl = "https://explorer.harmony.one" }, // Placeholder
            new() { Network = BlockchainNetwork.Aurora, Name = "Aurora", LogoUrl = "/images/networks/Aurora.svg", Color = "#70D44B", ExplorerUrl = "https://aurorascan.dev" },
            new() { Network = BlockchainNetwork.Cronos, Name = "Cronos", LogoUrl = "/images/networks/Cronos.svg", Color = "#002D74", ExplorerUrl = "https://cronoscan.com" },
            new() { Network = BlockchainNetwork.Boba, Name = "Boba", LogoUrl = "/images/networks/Boba.svg", Color = "#CBFF00", ExplorerUrl = "https://bobascan.com" },
            new() { Network = BlockchainNetwork.Evmos, Name = "Evmos", LogoUrl = "/images/networks/Evmos.svg", Color = "#ED4E33", ExplorerUrl = "https://escan.live" },
            new() { Network = BlockchainNetwork.Kava, Name = "Kava", LogoUrl = "/images/networks/Kava.svg", Color = "#FF433E", ExplorerUrl = "https://explorer.kava.io" },
            new() { Network = BlockchainNetwork.Fuse, Name = "Fuse", LogoUrl = "/images/networks/Fuse.svg", Color = "#B7FAA1", ExplorerUrl = "https://explorer.fuse.io" },
            new() { Network = BlockchainNetwork.Klaytn, Name = "Klaytn", LogoUrl = "/images/networks/Klaytn.svg", Color = "#FF5722", ExplorerUrl = "https://scope.klaytn.com" },
            new() { Network = BlockchainNetwork.OKXChain, Name = "OKX Chain", LogoUrl = "/images/networks/Ethereum.svg", Color = "#000000", ExplorerUrl = "https://www.oklink.com/oktc" }, // Placeholder
            new() { Network = BlockchainNetwork.Heco, Name = "Heco", LogoUrl = "/images/networks/Ethereum.svg", Color = "#02A26F", ExplorerUrl = "https://hecoinfo.com" }, // Placeholder
            new() { Network = BlockchainNetwork.Palm, Name = "Palm", LogoUrl = "/images/networks/Palm.svg", Color = "#1A1A1A", ExplorerUrl = "https://explorer.palm.io" },
            new() { Network = BlockchainNetwork.ShimmerEVM, Name = "Shimmer EVM", LogoUrl = "/images/networks/ShimmerEVM.svg", Color = "#27E2BA", ExplorerUrl = "https://explorer.evm.shimmer.network" },
            new() { Network = BlockchainNetwork.Rootstock, Name = "Rootstock", LogoUrl = "/images/networks/Rootstock.svg", Color = "#00AB7E", ExplorerUrl = "https://explorer.rsk.co" },
            new() { Network = BlockchainNetwork.Velas, Name = "Velas", LogoUrl = "/images/networks/Velas.svg", Color = "#0055FF", ExplorerUrl = "https://evmexplorer.velas.com" },
            new() { Network = BlockchainNetwork.IoTeX, Name = "IoTeX", LogoUrl = "/images/networks/IoTeX.svg", Color = "#00D4D5", ExplorerUrl = "https://iotexscan.io" },
            new() { Network = BlockchainNetwork.Syscoin, Name = "Syscoin", LogoUrl = "/images/networks/Syscoin.svg", Color = "#0082C6", ExplorerUrl = "https://explorer.syscoin.org" },
            new() { Network = BlockchainNetwork.TelosEVM, Name = "Telos EVM", LogoUrl = "/images/networks/Telos.svg", Color = "#571AFF", ExplorerUrl = "https://teloscan.io" },
            new() { Network = BlockchainNetwork.ThunderCore, Name = "ThunderCore", LogoUrl = "/images/networks/ThunderCore.svg", Color = "#FCC600", ExplorerUrl = "https://viewblock.io/thundercore" },
            new() { Network = BlockchainNetwork.Wanchain, Name = "Wanchain", LogoUrl = "/images/networks/Wanchain.svg", Color = "#136AAD", ExplorerUrl = "https://www.wanscan.org" },
            new() { Network = BlockchainNetwork.Redstone, Name = "Redstone", LogoUrl = "/images/networks/Redstone.svg", Color = "#EA3431", ExplorerUrl = "https://explorer.redstone.xyz" },
            new() { Network = BlockchainNetwork.OasisSapphire, Name = "Oasis Sapphire", LogoUrl = "/images/networks/Oasis.svg", Color = "#0092F6", ExplorerUrl = "https://explorer.sapphire.oasis.io" },
            new() { Network = BlockchainNetwork.OasisEmerald, Name = "Oasis Emerald", LogoUrl = "/images/networks/Oasis.svg", Color = "#0092F6", ExplorerUrl = "https://explorer.emerald.oasis.dev" },
            new() { Network = BlockchainNetwork.Cyber, Name = "Cyber", LogoUrl = "/images/networks/Ethereum.svg", Color = "#000000", ExplorerUrl = "https://cyberscan.co" }, // Placeholder
            new() { Network = BlockchainNetwork.DegenChain, Name = "Degen Chain", LogoUrl = "/images/networks/Ethereum.svg", Color = "#A36EFD", ExplorerUrl = "https://explorer.degen.tips" } // Placeholder
        };

        await _context.NetworkMetadata.AddRangeAsync(networkMetadata);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} network metadata entries", networkMetadata.Count);
    }

    /// <summary>
    /// Clears all data from the database.
    /// WARNING: Only use in Development environment for testing!
    /// </summary>
    public async Task ClearAsync()
    {
        _logger.LogWarning("Clearing all data from database...");

        _context.Watchlist.RemoveRange(_context.Watchlist);
        _context.UserWallets.RemoveRange(_context.UserWallets);
        _context.Users.RemoveRange(_context.Users);
        _context.VerifiedTokens.RemoveRange(_context.VerifiedTokens);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Database cleared successfully");
    }
}
