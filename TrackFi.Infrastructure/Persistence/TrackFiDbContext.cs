using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TrackFi.Domain.Entities;
using TrackFi.Domain.Enums;

namespace TrackFi.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for TrackFi.
/// All enums are stored as strings in the database for better readability and maintainability.
/// </summary>
public class TrackFiDbContext : DbContext
{
    public TrackFiDbContext(DbContextOptions<TrackFiDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserWallet> UserWallets => Set<UserWallet>();
    public DbSet<WatchlistEntry> Watchlist => Set<WatchlistEntry>();
    // REMOVED: Token verification tables (not needed with Zerion)
    // public DbSet<VerifiedToken> VerifiedTokens => Set<VerifiedToken>();
    // public DbSet<UnlistedToken> UnlistedTokens => Set<UnlistedToken>();
    // public DbSet<TokenMetadata> TokenMetadata => Set<TokenMetadata>();
    public DbSet<NetworkMetadata> NetworkMetadata => Set<NetworkMetadata>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TrackFiDbContext).Assembly);

        // Configure all enums to be stored as strings
        ConfigureEnumConversions(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Global configuration: All enums should be stored as strings
        configurationBuilder
            .Properties<BlockchainNetwork>()
            .HaveConversion<string>();

        configurationBuilder
            .Properties<Currency>()
            .HaveConversion<string>();

        configurationBuilder
            .Properties<AssetCategory>()
            .HaveConversion<string>();

        configurationBuilder
            .Properties<AssetType>()
            .HaveConversion<string>();

        configurationBuilder
            .Properties<TokenStandard>()
            .HaveConversion<string>();

        configurationBuilder
            .Properties<TransactionType>()
            .HaveConversion<string>();

        configurationBuilder
            .Properties<VerificationStatus>()
            .HaveConversion<string>();
    }

    private void ConfigureEnumConversions(ModelBuilder modelBuilder)
    {
        // Explicit enum to string conversions for all enum properties
        // This ensures enums are stored as strings in the database

        var converterBlockchainNetwork = new EnumToStringConverter<BlockchainNetwork>();
        var converterCurrency = new EnumToStringConverter<Currency>();
        var converterAssetCategory = new EnumToStringConverter<AssetCategory>();
        var converterAssetType = new EnumToStringConverter<AssetType>();
        var converterTokenStandard = new EnumToStringConverter<TokenStandard>();
        var converterTransactionType = new EnumToStringConverter<TransactionType>();
        var converterVerificationStatus = new EnumToStringConverter<VerificationStatus>();

        // User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.PrimaryWalletNetwork)
                .HasConversion(converterBlockchainNetwork);

            entity.Property(e => e.CoverNftNetwork)
                .HasConversion(converterBlockchainNetwork);
        });

        // UserWallet entity
        modelBuilder.Entity<UserWallet>(entity =>
        {
            entity.Property(e => e.Network)
                .HasConversion(converterBlockchainNetwork);
        });

        // WatchlistEntry entity
        modelBuilder.Entity<WatchlistEntry>(entity =>
        {
            entity.Property(e => e.Network)
                .HasConversion(converterBlockchainNetwork);
        });

        // REMOVED: Token verification entity configurations (not needed with Zerion)

        // NetworkMetadata entity
        modelBuilder.Entity<NetworkMetadata>(entity =>
        {
            entity.Property(e => e.Network)
                .HasConversion(converterBlockchainNetwork);

            // Create unique index on Network for fast lookups
            entity.HasIndex(e => e.Network)
                .IsUnique();

            // Required fields
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.LogoUrl)
                .IsRequired()
                .HasMaxLength(500);
        });
    }
}
