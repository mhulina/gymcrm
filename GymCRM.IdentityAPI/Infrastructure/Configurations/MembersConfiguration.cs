using GymCRM.IdentityAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymCRM.IdentityAPI.Models.Configurations
{
	public class MembersConfiguration : IEntityTypeConfiguration<Member>
	{
		public void Configure(EntityTypeBuilder<Member> modelBuilder)
		{
			modelBuilder.ToTable("Members");

			modelBuilder.HasKey(x => x.Id);
			modelBuilder.HasIndex(x => x.Email, "IX_Email").IsUnique();

			modelBuilder.Property(x => x.AccountGuid).IsRequired();
			modelBuilder.Property(x => x.AccountType).IsRequired();
			modelBuilder.Property(x => x.FirstName).HasMaxLength(70);
			modelBuilder.Property(x => x.MiddleName).HasMaxLength(70);
			modelBuilder.Property(x => x.LastName).HasMaxLength(70);
			modelBuilder.Property(x => x.Email).IsRequired().HasMaxLength(250);
			modelBuilder.Property(x => x.PhoneNumber).HasMaxLength(30);
			modelBuilder.Property(x => x.MobileNumber).HasMaxLength(30);
			modelBuilder.Property(x => x.Gender).IsRequired();
			modelBuilder.Property(x => x.GymSubscriptionType).IsRequired();
			modelBuilder.Property(x => x.DateModified).IsRequired();
			
			modelBuilder
				.HasOne(x => x.Account)
				.WithOne(x => x.Member)
				.HasForeignKey<Member>(x => x.AccountGuid)
				.HasConstraintName("FK_Account_Members")
				.OnDelete(DeleteBehavior.Cascade);
		}
	}
}
