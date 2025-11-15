using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrackFi.Domain.Entities;

namespace TrackFi.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for WatchlistEntry entity.
/// Enums are stored as strings.
/// </summary>
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
            .IsRequired()
            .HasConversion<string>(); // Store as string

        builder.Property(w => w.Label)
            .HasColumnName("label")
            .HasMaxLength(100);

        builder.Property(w => w.Notes)
            .HasColumnName("notes")
            .HasMaxLength(1000);

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

        builder.HasIndex(w => w.AddedAt)
            .HasDatabaseName("idx_watchlist_added_at");
    }
}
