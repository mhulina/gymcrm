using GymCRM.IdentityAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymCRM.IdentityAPI.Infrastructure.Configurations;

public class AccountsConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> modelBuilder)
    {
        modelBuilder.ToTable("Accounts", "identity_db");

        modelBuilder.HasKey(x => x.Id);
        modelBuilder.HasIndex(x => x.Email, "IX_Email").IsUnique();
        
        modelBuilder.Property(x => x.Email).IsRequired().HasMaxLength(250);
        modelBuilder.Property(x => x.DateCreated).IsRequired();
        modelBuilder.Property(x => x.HashSalt).IsRequired();
        modelBuilder.Property(x => x.HashedPassword).IsRequired();
    }
}