using System.Security.Cryptography;
using System.Text;
using GymCRM.MembershipAPI.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.Internal;

namespace GymCRM.MembershipAPI.Infrastructure.Configurations;

public class AccountsConfiguration : IEntityTypeConfiguration<Account>
{
    private readonly Guid _accountGuid;
    private readonly DateTime _dateTimeAccountCreated;
    private readonly string _accountEmail;
    
    public AccountsConfiguration(DateTime dateTimeAccountCreated, string accountEmail, Guid accountGuid)
    {
        _dateTimeAccountCreated = dateTimeAccountCreated;
        _accountEmail = accountEmail;
        _accountGuid = accountGuid;
    }
    
    public void Configure(EntityTypeBuilder<Account> modelBuilder)
    {
        modelBuilder.ToTable("Accounts");

        modelBuilder.HasKey(x => x.Guid);
        modelBuilder.HasIndex(x => x.Guid, "IX_Guid").IsUnique();
        modelBuilder.HasIndex(x => x.Email, "IX_Email").IsUnique();

        modelBuilder.Property(x => x.Id).UseIdentityAlwaysColumn();
        modelBuilder.Property(x => x.Guid).IsRequired();
        modelBuilder.Property(x => x.Email).IsRequired().HasMaxLength(250);
        modelBuilder.Property(x => x.DateCreated).IsRequired();
        modelBuilder.Property(x => x.HashSalt).IsRequired();
        modelBuilder.Property(x => x.HashedPassword).IsRequired();

        var hashSalt = RandomNumberGenerator.GetHexString(25);
        var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(hashSalt));
        hmac.Initialize();
        
        modelBuilder.HasData(
            new Account()
            {
                Id = 1,
                Guid = _accountGuid,
                HashSalt = hashSalt,
                Email = _accountEmail,
                DateCreated = _dateTimeAccountCreated,
                HashedPassword = Convert.ToBase64String( 
                    hmac.ComputeHash(
                        Encoding.UTF8.GetBytes(hashSalt + _dateTimeAccountCreated + "admin")))
            });
    }
}