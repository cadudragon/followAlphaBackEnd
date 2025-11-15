using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrackFi.Domain.Entities;

namespace TrackFi.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for User entity.
/// Enums are stored as strings.
/// </summary>
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
            .IsRequired()
            .HasConversion<string>(); // Store as string

        builder.Property(u => u.CoverPictureUrl)
            .HasColumnName("cover_picture_url")
            .HasMaxLength(500);

        builder.Property(u => u.CoverNftContract)
            .HasColumnName("cover_nft_contract")
            .HasMaxLength(255);

        builder.Property(u => u.CoverNftTokenId)
            .HasColumnName("cover_nft_token_id")
            .HasMaxLength(100);

        builder.Property(u => u.CoverNftNetwork)
            .HasColumnName("cover_nft_network")
            .HasMaxLength(50)
            .HasConversion<string>(); // Store as string

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

        builder.HasIndex(u => u.CreatedAt)
            .HasDatabaseName("idx_users_created_at");

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
