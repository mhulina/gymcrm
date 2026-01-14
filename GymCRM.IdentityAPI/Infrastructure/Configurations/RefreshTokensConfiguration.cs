using GymCRM.IdentityAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymCRM.IdentityAPI.Infrastructure.Configurations;

public class RefreshTokensConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> modelBuilder)
    {
        modelBuilder.ToTable("RefreshTokens");
        
        modelBuilder.HasKey(x => x.Id);
        modelBuilder.HasIndex(x => x.Token).IsUnique();
        modelBuilder.HasIndex(x => x.AccountId);
        
        modelBuilder.Property(x => x.Token).IsRequired().HasMaxLength(500);
        modelBuilder.Property(x => x.ExpiresAt).IsRequired();
        modelBuilder.Property(x => x.CreatedAt).IsRequired();
        modelBuilder.Property(x => x.IsRevoked).IsRequired().HasDefaultValue(false);
        
        modelBuilder
            .HasOne<Account>(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}