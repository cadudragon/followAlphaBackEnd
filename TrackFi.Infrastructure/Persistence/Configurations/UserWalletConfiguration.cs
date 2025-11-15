using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrackFi.Domain.Entities;

namespace TrackFi.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for UserWallet entity.
/// Enums are stored as strings.
/// </summary>
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
            .IsRequired()
            .HasConversion<string>(); // Store as string

        builder.Property(w => w.Label)
            .HasColumnName("label")
            .HasMaxLength(100);

        builder.Property(w => w.IsVerified)
            .HasColumnName("is_verified")
            .HasDefaultValue(false);

        builder.Property(w => w.SignatureProof)
            .HasColumnName("signature_proof")
            .HasMaxLength(500);

        builder.Property(w => w.SignatureMessage)
            .HasColumnName("signature_message")
            .HasMaxLength(1000);

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
